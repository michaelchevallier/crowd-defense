#!/usr/bin/env bash
# Helper: call Unity-MCP tool. Usage: unity-mcp-call.sh <tool_name> <json_args>
# Reuses session ID stored in /tmp/unity-mcp-session.txt; creates one if missing.
set -euo pipefail
SID_FILE="/tmp/unity-mcp-session.txt"
TOOL="${1:?tool name required}"
ARGS_JSON="${2-}"
if [ -z "$ARGS_JSON" ]; then
  ARGS_JSON='{}'
fi

create_session() {
  local resp
  resp=$(curl -s -i -X POST http://127.0.0.1:8080/mcp \
    -H "Content-Type: application/json" \
    -H "Accept: application/json, text/event-stream" \
    -H "MCP-Protocol-Version: 2025-06-18" \
    -d '{"jsonrpc":"2.0","method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"claude-night2","version":"1.0"}},"id":1}')
  local sid
  sid=$(echo "$resp" | grep -i '^mcp-session-id:' | tr -d '\r' | awk '{print $2}')
  if [ -z "$sid" ]; then
    echo "FAILED to obtain session id" >&2
    echo "$resp" >&2
    exit 1
  fi
  echo "$sid" > "$SID_FILE"
  # send initialized notification
  curl -s -X POST http://127.0.0.1:8080/mcp \
    -H "Content-Type: application/json" \
    -H "Accept: application/json, text/event-stream" \
    -H "MCP-Protocol-Version: 2025-06-18" \
    -H "Mcp-Session-Id: $sid" \
    -d '{"jsonrpc":"2.0","method":"notifications/initialized","params":{}}' >/dev/null
  echo "$sid"
}

get_session() {
  if [ -s "$SID_FILE" ]; then
    cat "$SID_FILE"
  else
    create_session
  fi
}

SID=$(get_session)

# Validate ARGS_JSON
if ! echo "$ARGS_JSON" | jq -e . >/dev/null 2>&1; then
  echo "ERROR: invalid JSON in ARGS: $ARGS_JSON" >&2
  exit 2
fi

# Build the call request
REQ=$(echo "$ARGS_JSON" | jq -c --arg tool "$TOOL" \
  '{jsonrpc:"2.0",id:42,method:"tools/call",params:{name:$tool,arguments:.}}')

RESP=$(curl -s -X POST http://127.0.0.1:8080/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -H "MCP-Protocol-Version: 2025-06-18" \
  -H "Mcp-Session-Id: $SID" \
  -d "$REQ")

# If session expired, retry once
if echo "$RESP" | grep -q "Missing session ID\|Invalid session"; then
  SID=$(create_session)
  RESP=$(curl -s -X POST http://127.0.0.1:8080/mcp \
    -H "Content-Type: application/json" \
    -H "Accept: application/json, text/event-stream" \
    -H "MCP-Protocol-Version: 2025-06-18" \
    -H "Mcp-Session-Id: $SID" \
    -d "$REQ")
fi

# Parse SSE format -> just the data: line(s)
echo "$RESP" | grep '^data:' | sed 's/^data: //'
