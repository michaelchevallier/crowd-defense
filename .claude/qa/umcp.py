#!/usr/bin/env python3
"""Minimal MCP client for Unity-MCP via Streamable HTTP transport (uvicorn).

Usage:
    umcp.py <method> [json-args]
    umcp.py tools/list
    umcp.py tools/call '{"name":"manage_editor","arguments":{"action":"get_play_mode"}}'
    umcp.py resources/list
    umcp.py resources/read '{"uri":"unity://editor_state"}'
"""
import json
import os
import sys
import time
import urllib.request
import urllib.error

URL = os.environ.get("UMCP_URL", "http://127.0.0.1:8080/mcp")
SESSION_FILE = "/tmp/umcp_session.json"


def parse_sse(body: bytes) -> dict:
    # Server returns text/event-stream. We expect a single message event.
    text = body.decode("utf-8", errors="replace")
    out = None
    for line in text.splitlines():
        if line.startswith("data: "):
            data = line[6:]
            try:
                out = json.loads(data)
            except json.JSONDecodeError:
                pass
    return out or {"_raw": text}


def post(payload: dict, session_id: str | None, accept_stream=True):
    headers = {
        "Content-Type": "application/json",
        "Accept": "application/json, text/event-stream" if accept_stream else "application/json",
    }
    if session_id:
        headers["Mcp-Session-Id"] = session_id
    req = urllib.request.Request(URL, data=json.dumps(payload).encode("utf-8"), headers=headers, method="POST")
    try:
        with urllib.request.urlopen(req, timeout=300) as resp:
            sid = resp.headers.get("mcp-session-id")
            body = resp.read()
            ct = resp.headers.get("content-type", "")
            if "event-stream" in ct:
                return sid, parse_sse(body), resp.status
            try:
                return sid, json.loads(body.decode("utf-8")), resp.status
            except Exception:
                return sid, {"_raw": body.decode("utf-8", errors="replace")}, resp.status
    except urllib.error.HTTPError as e:
        return None, {"_error": e.code, "_body": e.read().decode("utf-8", errors="replace")}, e.code


def ensure_session() -> str:
    # Load saved session, otherwise initialize.
    try:
        with open(SESSION_FILE) as f:
            data = json.load(f)
            if data.get("session_id") and time.time() - data.get("ts", 0) < 3600:
                return data["session_id"]
    except Exception:
        pass
    init_payload = {
        "jsonrpc": "2.0", "id": 1, "method": "initialize",
        "params": {
            "protocolVersion": "2024-11-05",
            "capabilities": {},
            "clientInfo": {"name": "umcp-py", "version": "1.0"}
        }
    }
    sid, result, status = post(init_payload, None)
    if not sid:
        print(json.dumps({"_init_failed": True, "status": status, "result": result}, indent=2), file=sys.stderr)
        sys.exit(2)
    # Send notification 'initialized'
    notify = {"jsonrpc": "2.0", "method": "notifications/initialized"}
    headers = {
        "Content-Type": "application/json",
        "Accept": "application/json, text/event-stream",
        "Mcp-Session-Id": sid,
    }
    req = urllib.request.Request(URL, data=json.dumps(notify).encode("utf-8"), headers=headers, method="POST")
    try:
        urllib.request.urlopen(req, timeout=30).read()
    except Exception:
        pass
    with open(SESSION_FILE, "w") as f:
        json.dump({"session_id": sid, "ts": time.time()}, f)
    return sid


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    method = sys.argv[1]
    params = json.loads(sys.argv[2]) if len(sys.argv) > 2 else {}
    sid = ensure_session()
    payload = {"jsonrpc": "2.0", "id": int(time.time()), "method": method, "params": params}
    new_sid, result, status = post(payload, sid)
    # If session expired, retry once with fresh session
    if isinstance(result, dict) and result.get("_error") in (400, 404) and "session" in str(result).lower():
        try:
            os.remove(SESSION_FILE)
        except Exception:
            pass
        sid = ensure_session()
        new_sid, result, status = post(payload, sid)
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
