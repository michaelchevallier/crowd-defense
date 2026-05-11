#!/usr/bin/env python3
"""
Mixamo animation batch downloader for Crowd Defense humanoid enemies.

Usage:
    python3 tools/mixamo/download_anims.py [--dry-run] [--batch-size N] [--resume]

Requirements:
    pip3 install playwright requests
    python3 -m playwright install chromium

Auth (3 options, priority order):
  1. MIXAMO_TOKEN env var with Adobe IMS access_token
  2. tools/mixamo/.token file (one line, no quotes)
  3. tools/mixamo/.session.json (Playwright login flow)

Fastest path (30s): in Chrome devtools on mixamo.com console, run
  copy(localStorage.access_token)
Then:
  pbpaste > tools/mixamo/.token
  python3 tools/mixamo/download_anims.py
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import time
import zipfile
from pathlib import Path

import requests

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
SCRIPT_DIR = Path(__file__).parent.resolve()
PROJECT_ROOT = SCRIPT_DIR.parent.parent
OUTPUT_DIR = PROJECT_ROOT / "Assets" / "Animations" / "Mixamo"
SESSION_FILE = SCRIPT_DIR / ".session.json"
PROGRESS_FILE = SCRIPT_DIR / ".progress.json"

OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

# ---------------------------------------------------------------------------
# Mixamo API endpoints — current as of 2026 (reverse-engineered)
# Base: www.mixamo.com/api/v1/*  (api.mixamo.com is deprecated/NXDOMAIN)
# Auth: x-api-key header + Authorization: Bearer <Adobe IMS access_token>
# Download flow per animation:
#   1. GET /products/{motion_id}?character_id={char_id} — fetch gms_hash
#   2. POST /animations/stream — retarget motion to character
#   3. POST /animations/export — request FBX generation
#   4. GET /characters/{char_id}/monitor — poll until ready
#   5. GET response.job_result (S3 presigned URL) — download FBX
# ---------------------------------------------------------------------------
MIXAMO_BASE = "https://www.mixamo.com"
MIXAMO_API = "https://www.mixamo.com/api/v1"
MIXAMO_PRODUCTS_API = f"{MIXAMO_API}/products"
MIXAMO_CHARACTERS_API = f"{MIXAMO_API}/characters"
MIXAMO_STREAM_API = f"{MIXAMO_API}/animations/stream"
MIXAMO_EXPORT_API = f"{MIXAMO_API}/animations/export"

API_KEY = "mixamo2"

# Generic Mixamo humanoid character ID (fallback = X Bot)
GENERIC_HUMANOID_ID = "2dee24f8-3b49-48af-b735-c6377509eaac"

# ---------------------------------------------------------------------------
# Humanoid enemy list — 15 unique asset keys + Mixamo character mapping
# ---------------------------------------------------------------------------
HUMANOID_ENEMIES = [
    # (enemy_key, mixamo_character_name, notes)
    ("zombie",                      "Zombie",       "Basic grunt"),
    ("goblin",                      "Goblin",       "Imp/Assassin/Runner"),
    ("soldier",                     "Soldier",      "Brute — heavy infantry"),
    ("knightgolden",                "Knight",       "Shielded enemy"),
    ("mob_skeleton",                "Skeleton",     "SkeletonMinion summon"),
    ("mob_orc",                     "Orc",          "ForestBrute"),
    ("mob_cyberpunk_character",     "Mannequin",    "CyberBasic"),
    ("mob_cyberpunk_large",         "X Bot",        "CyberBrute"),
    ("mob_cyberpunk_2legs",         "X Bot",        "CyberRunner"),
    ("pirate",                      "Pirate",       "Boss/BrigandBoss/CorsairBoss"),
    ("wizard",                      "Mage",         "Midboss/WarlordBoss"),
    ("boss_medieval_sorcier_roi",   "Mage",         "WizardKing"),
    ("boss_apocalypse",             "Y Bot",        "ApocalypseBoss"),
    ("boss_espace_ghost",           "Mannequin",    "CosmicBoss"),
    ("boss_cyberpunk_hub_ia",       "Y Bot",        "AiHub"),
]

# 4 animations per character
ANIMATIONS = [
    ("Walking",  "walking"),
    ("Running",  "running"),
    ("Punch",    "attack"),
    ("Dying",    "dying"),
]

ANIM_QUERIES = {
    "walking": "Walking",
    "running": "Running",
    "attack":  "Sword And Shield Attack",
    "dying":   "Dying",
}

# Rate limit
WAIT_BETWEEN_DOWNLOADS = 5
RATE_LIMIT_WAIT = 60
MAX_RETRIES = 3
DAILY_LIMIT = 50

# ---------------------------------------------------------------------------
# Progress tracking
# ---------------------------------------------------------------------------

def load_progress() -> dict:
    if PROGRESS_FILE.exists():
        with open(PROGRESS_FILE) as f:
            return json.load(f)
    return {"downloaded": [], "failed": [], "total_today": 0}


def save_progress(progress: dict) -> None:
    with open(PROGRESS_FILE, "w") as f:
        json.dump(progress, f, indent=2)


def is_done(progress: dict, enemy_key: str, anim_key: str) -> bool:
    return f"{enemy_key}_{anim_key}" in progress.get("downloaded", [])


def mark_done(progress: dict, enemy_key: str, anim_key: str) -> None:
    entry = f"{enemy_key}_{anim_key}"
    if entry not in progress["downloaded"]:
        progress["downloaded"].append(entry)
    progress["total_today"] = progress.get("total_today", 0) + 1
    save_progress(progress)


def mark_failed(progress: dict, enemy_key: str, anim_key: str, reason: str) -> None:
    progress["failed"].append({"key": f"{enemy_key}_{anim_key}", "reason": reason})
    save_progress(progress)


# ---------------------------------------------------------------------------
# Auth — bearer token from env / .token / .session.json
# ---------------------------------------------------------------------------

def load_session() -> dict | None:
    """Token sources in priority order: MIXAMO_TOKEN env, .token, .session.json."""
    env_token = os.environ.get("MIXAMO_TOKEN")
    if env_token:
        print("[auth] Using MIXAMO_TOKEN from environment")
        return {"bearer_token": env_token.strip()}

    token_file = SCRIPT_DIR / ".token"
    if token_file.exists():
        with open(token_file) as f:
            tok = f.read().strip()
        if tok:
            print(f"[auth] Loaded bearer token from {token_file}")
            return {"bearer_token": tok}

    if SESSION_FILE.exists():
        with open(SESSION_FILE) as f:
            data = json.load(f)
        if isinstance(data, dict) and data.get("bearer_token"):
            print(f"[auth] Loaded bearer token from {SESSION_FILE}")
            return data
    return None


def save_session(data: dict) -> None:
    with open(SESSION_FILE, "w") as f:
        json.dump(data, f, indent=2)
    print(f"[auth] Session saved to {SESSION_FILE}")


def do_browser_login() -> dict:
    """Open Chromium for Adobe login + capture OAuth bearer token from network."""
    from playwright.sync_api import sync_playwright
    import tempfile
    import shutil

    print("\n" + "=" * 60)
    print("MIXAMO LOGIN REQUIRED")
    print("=" * 60)
    print("FASTER ALTERNATIVE: copy token from Chrome devtools.")
    print("  1. Chrome → https://www.mixamo.com (logged in)")
    print("  2. DevTools (Cmd+Opt+I) → Console")
    print("  3. Run: copy(localStorage.access_token)")
    print("  4. pbpaste > tools/mixamo/.token")
    print("  5. Re-run this script")
    print("=" * 60)
    print()
    print("Or wait for Chromium window (max 10 min)...")
    print()

    tmp_profile = tempfile.mkdtemp(prefix="playwright-mixamo-")
    token = None

    try:
        with sync_playwright() as p:
            context = p.chromium.launch_persistent_context(
                user_data_dir=tmp_profile,
                headless=False,
                slow_mo=100,
                args=["--no-first-run", "--disable-extensions"],
            )
            page = context.new_page()

            captured = {"token": None}

            def on_request(request):
                if "mixamo.com/api" in request.url:
                    auth = request.headers.get("authorization", "")
                    if auth.lower().startswith("bearer "):
                        tok = auth[7:]
                        if tok and tok != captured["token"]:
                            captured["token"] = tok

            page.on("request", on_request)
            page.goto(f"{MIXAMO_BASE}/#/")
            page.wait_for_timeout(3000)

            print("[auth] Waiting for login + bearer token (max 10 min)...")
            deadline = time.time() + 600
            while time.time() < deadline:
                if captured["token"]:
                    print("[auth] Bearer token captured!")
                    page.wait_for_timeout(2000)
                    break
                time.sleep(1)

            if not captured["token"]:
                # Fallback: localStorage
                try:
                    tk = page.evaluate("() => localStorage.getItem('access_token')")
                    if tk:
                        captured["token"] = tk
                        print("[auth] Token from localStorage")
                except Exception as e:
                    print(f"[auth] localStorage failed: {e}")

            token = captured["token"]
            context.close()
    finally:
        shutil.rmtree(tmp_profile, ignore_errors=True)

    if not token:
        print("\n[auth] FATAL: could not capture bearer token.")
        sys.exit(2)

    data = {"bearer_token": token}
    save_session(data)
    return data


def get_session() -> dict:
    return load_session() or do_browser_login()


# ---------------------------------------------------------------------------
# Mixamo API helpers
# ---------------------------------------------------------------------------

def make_session(token: str) -> requests.Session:
    s = requests.Session()
    s.headers.update({
        "Accept": "application/json",
        "Content-Type": "application/json",
        "User-Agent": "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36",
        "x-api-key": API_KEY,
        "Origin": MIXAMO_BASE,
        "Referer": f"{MIXAMO_BASE}/",
        "Authorization": f"Bearer {token}",
    })
    return s


def search_animation(session: requests.Session, query: str) -> list[dict]:
    """Search for motions by name. GET /products?type=Motion,MotionPack&query=X"""
    params = {
        "type": "Motion,MotionPack",
        "query": query,
        "limit": 10,
        "page": 1,
        "order": "popularity",
    }
    resp = session.get(MIXAMO_PRODUCTS_API, params=params)
    if resp.status_code != 200:
        print(f"[search] HTTP {resp.status_code} for '{query}': {resp.text[:200]}")
        return []
    return resp.json().get("results", [])


def find_character_id(session: requests.Session, char_name: str) -> str | None:
    """Find Mixamo character ID by name. GET /products?type=Character&query=X"""
    params = {"type": "Character", "query": char_name, "limit": 10, "page": 1}
    resp = session.get(MIXAMO_PRODUCTS_API, params=params)
    if resp.status_code != 200:
        return None
    results = resp.json().get("results", [])
    for r in results:
        if r.get("name", "").lower() == char_name.lower():
            return r.get("id")
    if results:
        return results[0].get("id")
    return None


def get_motion_details(session: requests.Session, motion_id: str, character_id: str) -> dict | None:
    """GET /products/{motion_id}?character_id={char} — returns gms_hash for retarget."""
    url = f"{MIXAMO_PRODUCTS_API}/{motion_id}"
    params = {"character_id": character_id}
    resp = session.get(url, params=params)
    if resp.status_code != 200:
        print(f"[motion] HTTP {resp.status_code}: {resp.text[:200]}")
        return None
    return resp.json()


def request_export(
    session: requests.Session,
    character_id: str,
    motion_details: dict,
    product_name: str = "Walking",
) -> bool:
    """
    Two-step FBX generation:
      1. POST /animations/stream — retarget
      2. POST /animations/export — generate FBX
    gms_hash comes from motion_details (NOT motion_id UUID).
    """
    gms_hash = motion_details.get("gms_hash")
    if not gms_hash:
        # Try alternate paths in the response
        gms_hash = motion_details.get("details", {}).get("gms_hash") if motion_details.get("details") else None
    if not gms_hash:
        print(f"[export] No gms_hash in motion details. Keys: {list(motion_details.keys())}")
        return False

    # 1. Stream (retarget)
    stream_payload = {
        "gms_hash": gms_hash,
        "character_id": character_id,
        "retargeting_payload": "",
        "target_type": "skin",
    }
    r1 = session.post(MIXAMO_STREAM_API, json=stream_payload)
    if r1.status_code not in (200, 201, 202):
        print(f"[stream] HTTP {r1.status_code}: {r1.text[:200]}")
        return False

    # 2. Export (request FBX)
    export_payload = {
        "gms_hash": gms_hash,
        "preferences": {
            "format": "fbx7_unity",
            "skin": "true",
            "fps": "30",
            "reducekf": "0",
        },
        "character_id": character_id,
        "type": "Motion",
        "product_name": product_name,
    }
    r2 = session.post(MIXAMO_EXPORT_API, json=export_payload)
    if r2.status_code not in (200, 201, 202):
        print(f"[export] HTTP {r2.status_code}: {r2.text[:200]}")
        return False
    return True


def poll_export(session: requests.Session, character_id: str, timeout: int = 180) -> str | None:
    """Poll GET /characters/{char_id}/monitor until status=completed. Returns S3 URL."""
    monitor_url = f"{MIXAMO_CHARACTERS_API}/{character_id}/monitor"
    deadline = time.time() + timeout
    while time.time() < deadline:
        resp = session.get(monitor_url)
        if resp.status_code != 200:
            time.sleep(3)
            continue
        data = resp.json()
        status = data.get("status", "")
        if status == "completed":
            return data.get("job_result")
        if status in ("failed", "error", "Error"):
            print(f"[export] Job failed: {data.get('message', '')}")
            return None
        time.sleep(3)
    print("[export] Timeout waiting for export job")
    return None


def download_fbx(session: requests.Session, download_url: str, out_path: Path) -> bool:
    """Download FBX (possibly ZIP-wrapped) to out_path."""
    resp = session.get(download_url, stream=True)
    if resp.status_code != 200:
        print(f"[download] HTTP {resp.status_code}")
        return False

    content_type = resp.headers.get("Content-Type", "")
    raw = b""
    for chunk in resp.iter_content(chunk_size=65536):
        raw += chunk

    if "zip" in content_type or raw[:2] == b"PK":
        tmp_zip = out_path.with_suffix(".zip")
        tmp_zip.write_bytes(raw)
        try:
            with zipfile.ZipFile(tmp_zip, "r") as zf:
                for name in zf.namelist():
                    if name.endswith(".fbx"):
                        zf.extract(name, out_path.parent)
                        (out_path.parent / name).rename(out_path)
                        break
        finally:
            tmp_zip.unlink(missing_ok=True)
    else:
        out_path.write_bytes(raw)

    return out_path.exists() and out_path.stat().st_size > 1024


def download_animation(
    session: requests.Session | None,
    enemy_key: str,
    mixamo_char_name: str,
    anim_key: str,
    anim_query: str,
    dry_run: bool = False,
) -> bool:
    """Download one animation. Returns True on success."""
    out_filename = f"{enemy_key}_{anim_key}.fbx"
    out_path = OUTPUT_DIR / out_filename

    if out_path.exists() and out_path.stat().st_size > 1024:
        print(f"  [skip] {out_filename} ({out_path.stat().st_size // 1024} KB)")
        return True

    if dry_run:
        print(f"  [dry-run] {out_filename}")
        return True

    assert session is not None

    char_id = find_character_id(session, mixamo_char_name)
    if not char_id:
        char_id = GENERIC_HUMANOID_ID
        print(f"  [warn] '{mixamo_char_name}' not found, using generic humanoid")

    results = search_animation(session, anim_query)
    if not results:
        print(f"  [fail] No results for '{anim_query}'")
        return False
    motion = next((r for r in results if r.get("name", "").lower() == anim_query.lower()), results[0])
    motion_id = motion.get("id")
    motion_name = motion.get("name", "?")
    print(f"  [found] {motion_name} (id={motion_id})")

    details = get_motion_details(session, motion_id, char_id)
    if not details:
        return False

    if not request_export(session, char_id, details, motion_name):
        return False

    download_url = poll_export(session, char_id)
    if not download_url:
        return False

    ok = download_fbx(session, download_url, out_path)
    if ok:
        print(f"  [ok] {out_filename} ({out_path.stat().st_size // 1024} KB)")
    else:
        print(f"  [fail] {out_filename}")
    return ok


# ---------------------------------------------------------------------------
# Main run
# ---------------------------------------------------------------------------

def run(
    batch_size: int = DAILY_LIMIT,
    dry_run: bool = False,
    resume: bool = True,
    reset_progress: bool = False,
) -> None:
    progress = load_progress()
    if reset_progress:
        progress = {"downloaded": [], "failed": [], "total_today": 0}
        save_progress(progress)

    session: requests.Session | None = None
    if not dry_run:
        sess_data = get_session()
        session = make_session(sess_data["bearer_token"])

        test = session.get(f"{MIXAMO_CHARACTERS_API}?limit=1")
        if test.status_code == 401:
            print("\n[auth] ERROR: bearer token expired or invalid (HTTP 401).")
            print("       Re-extract token from Chrome devtools:")
            print("         copy(localStorage.access_token)")
            print("         pbpaste > tools/mixamo/.token")
            sys.exit(2)
        elif test.status_code not in (200, 201):
            print(f"\n[auth] WARNING: status {test.status_code}: {test.text[:150]}")

    downloaded = 0
    skipped = 0
    failed = 0
    deferred: list[str] = []

    for enemy_key, char_name, _notes in HUMANOID_ENEMIES:
        for anim_display, anim_key in ANIMATIONS:
            out_path = OUTPUT_DIR / f"{enemy_key}_{anim_key}.fbx"
            if out_path.exists() and out_path.stat().st_size > 1024:
                skipped += 1
                continue
            if resume and is_done(progress, enemy_key, anim_key):
                skipped += 1
                continue
            total = progress.get("total_today", 0) + downloaded
            if total >= batch_size:
                deferred.append(f"{enemy_key}_{anim_key}")
                continue

            print(f"\n[{downloaded + 1}/{batch_size}] {enemy_key} / {anim_display} ({char_name})")

            ok = False
            for attempt in range(1, MAX_RETRIES + 1):
                if download_animation(
                    session, enemy_key, char_name, anim_key,
                    ANIM_QUERIES[anim_key], dry_run=dry_run,
                ):
                    if not dry_run:
                        mark_done(progress, enemy_key, anim_key)
                    downloaded += 1
                    ok = True
                    break
                if attempt < MAX_RETRIES:
                    print(f"  [retry {attempt}/{MAX_RETRIES}] wait {RATE_LIMIT_WAIT}s...")
                    time.sleep(RATE_LIMIT_WAIT)

            if not ok:
                mark_failed(progress, enemy_key, anim_key, f"failed after {MAX_RETRIES} attempts")
                failed += 1

            if downloaded > 0 and not dry_run:
                time.sleep(WAIT_BETWEEN_DOWNLOADS)

    print("\n" + "=" * 60)
    print("BATCH COMPLETE")
    print(f"  Downloaded this run    : {downloaded}")
    print(f"  Skipped (already done) : {skipped}")
    print(f"  Failed                 : {failed}")
    print(f"  Deferred (limit)       : {len(deferred)}")
    if deferred:
        print(f"\n  Deferred to next run ({len(deferred)}):")
        for d in deferred:
            print(f"    - {d}")
        print("\n  Re-run tomorrow: python3 tools/mixamo/download_anims.py --resume")
    print("=" * 60)

    progress["deferred"] = deferred
    save_progress(progress)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Mixamo humanoid animation batch downloader")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--batch-size", type=int, default=DAILY_LIMIT)
    parser.add_argument("--resume", action="store_true", default=True)
    parser.add_argument("--no-resume", dest="resume", action="store_false")
    parser.add_argument("--reset-progress", action="store_true")
    args = parser.parse_args()
    run(
        batch_size=args.batch_size,
        dry_run=args.dry_run,
        resume=args.resume,
        reset_progress=args.reset_progress,
    )
