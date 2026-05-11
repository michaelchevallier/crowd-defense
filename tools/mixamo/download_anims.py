#!/usr/bin/env python3
"""
Mixamo animation batch downloader for Crowd Defense humanoid enemies.

Usage:
    python3 tools/mixamo/download_anims.py [--dry-run] [--batch-size N] [--resume]

Requirements:
    pip3 install playwright requests
    python3 -m playwright install chromium

Adobe login:
    The script launches a visible Chromium window for first-time login.
    After login, session cookies are saved to tools/mixamo/.session.json.
    Subsequent runs reuse the saved session (headless).
    If session expires: delete .session.json and re-run for fresh login.
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
PROJECT_ROOT = SCRIPT_DIR.parent.parent  # crowd-defense/
OUTPUT_DIR = PROJECT_ROOT / "Assets" / "Animations" / "Mixamo"
SESSION_FILE = SCRIPT_DIR / ".session.json"
PROGRESS_FILE = SCRIPT_DIR / ".progress.json"

OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

# ---------------------------------------------------------------------------
# Mixamo API endpoints (unofficial but stable)
# ---------------------------------------------------------------------------
MIXAMO_BASE = "https://www.mixamo.com"
MIXAMO_API = "https://api.mixamo.com"
MIXAMO_ANIMATIONS_API = f"{MIXAMO_API}/v1/animations"
MIXAMO_CHARACTER_API = f"{MIXAMO_API}/v1/characters"
MIXAMO_PRODUCT_API = f"{MIXAMO_API}/v1/products"
MIXAMO_EXPORT_API = f"{MIXAMO_API}/v1/animations/export"

# ---------------------------------------------------------------------------
# Humanoid enemy list — 15 unique asset keys + animation mappings
# Priority order (most-used first per ticket brief):
#   zombie (Basic), goblin (Imp/Assassin/Runner), soldier (Brute),
#   knightgolden (Shielded), mob_skeleton, mob_orc, mob_cyberpunk_character,
#   mob_cyberpunk_large, mob_cyberpunk_2legs, pirate, wizard,
#   boss_medieval_sorcier_roi, boss_apocalypse, boss_espace_ghost, boss_cyberpunk_hub_ia
# ---------------------------------------------------------------------------
HUMANOID_ENEMIES = [
    # (enemy_key, mixamo_character_name, notes)
    ("zombie",                      "Zombie",      "Basic grunt"),
    ("goblin",                      "Goblin",      "Imp/Assassin/Runner"),
    ("soldier",                     "Soldier",     "Brute — heavy infantry"),
    ("knightgolden",                "Knight",      "Shielded enemy"),
    ("mob_skeleton",                "Skeleton",    "SkeletonMinion summon"),
    ("mob_orc",                     "Orc",         "ForestBrute"),
    ("mob_cyberpunk_character",     "Mannequin",   "CyberBasic"),
    ("mob_cyberpunk_large",         "X Bot",       "CyberBrute"),
    ("mob_cyberpunk_2legs",         "X Bot",       "CyberRunner"),
    ("pirate",                      "Pirate",      "Boss/BrigandBoss/CorsairBoss"),
    ("wizard",                      "Mage",        "Midboss/WarlordBoss"),
    ("boss_medieval_sorcier_roi",   "Mage",        "WizardKing"),
    ("boss_apocalypse",             "Y Bot",       "ApocalypseBoss"),
    ("boss_espace_ghost",           "Mannequin",   "CosmicBoss"),
    ("boss_cyberpunk_hub_ia",       "Y Bot",       "AiHub"),
]

# 4 animations to fetch per character
ANIMATIONS = [
    ("Walking",  "walking"),   # idle-moving
    ("Running",  "running"),   # fast-moving
    ("Punch",    "attack"),    # combat attack
    ("Dying",    "dying"),     # death
]

# Mixamo animation search queries
ANIM_QUERIES = {
    "walking": "Walking",
    "running": "Running",
    "attack":  "Sword And Shield Attack",
    "dying":   "Dying",
}

# ---------------------------------------------------------------------------
# Rate limit settings
# ---------------------------------------------------------------------------
WAIT_BETWEEN_DOWNLOADS = 5   # seconds between each download
RATE_LIMIT_WAIT = 60         # seconds to wait on rate limit
MAX_RETRIES = 3
DAILY_LIMIT = 50             # Mixamo soft daily limit

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
    return f"{enemy_key}_{anim_key}" in progress["downloaded"]


def mark_done(progress: dict, enemy_key: str, anim_key: str) -> None:
    entry = f"{enemy_key}_{anim_key}"
    if entry not in progress["downloaded"]:
        progress["downloaded"].append(entry)
    progress["total_today"] = progress.get("total_today", 0) + 1
    save_progress(progress)


def mark_failed(progress: dict, enemy_key: str, anim_key: str, reason: str) -> None:
    entry = {"key": f"{enemy_key}_{anim_key}", "reason": reason}
    progress["failed"].append(entry)
    save_progress(progress)


# ---------------------------------------------------------------------------
# Session management — Adobe IMS cookies
# ---------------------------------------------------------------------------

def load_session() -> dict | None:
    if SESSION_FILE.exists():
        with open(SESSION_FILE) as f:
            data = json.load(f)
        print(f"[auth] Loaded session from {SESSION_FILE}")
        return data
    return None


def save_session(cookies: list[dict]) -> None:
    with open(SESSION_FILE, "w") as f:
        json.dump(cookies, f, indent=2)
    print(f"[auth] Session cookies saved to {SESSION_FILE}")


def cookies_to_jar(cookies: list[dict]) -> dict:
    """Convert playwright cookie list to requests-compatible dict."""
    return {c["name"]: c["value"] for c in cookies}


def do_browser_login() -> list[dict]:
    """
    Open a visible Chromium window for Adobe/Mixamo login.
    Uses a fresh temp profile (avoids conflict with running Chrome instance).
    Returns session cookies after successful login.
    """
    from playwright.sync_api import sync_playwright
    import tempfile
    import shutil

    print("\n[auth] No saved session found. Opening browser for Mixamo login...")
    print("[auth] Please log in to https://www.mixamo.com with your Adobe account.")
    print("[auth] The script continues automatically once logged in (max 5 min).\n")

    # Use a fresh temp profile to avoid Chrome SingletonLock conflict
    tmp_profile = tempfile.mkdtemp(prefix="playwright-mixamo-")

    try:
        with sync_playwright() as p:
            context = p.chromium.launch_persistent_context(
                user_data_dir=tmp_profile,
                headless=False,
                slow_mo=150,
                args=["--no-first-run", "--disable-extensions"],
            )
            page = context.new_page()
            page.goto(f"{MIXAMO_BASE}/#/")
            page.wait_for_timeout(3000)

            print("[auth] Waiting for login completion (max 5 minutes)...")
            try:
                page.wait_for_selector(
                    "div.character-picker, .user-avatar, [data-id='character-picker'], "
                    ".characters-container, .nav-user-info, canvas",
                    timeout=300_000,
                )
                print("[auth] Login detected.")
            except Exception:
                print("[auth] Login wait timed out — capturing cookies anyway.")

            cookies = context.cookies()
            context.close()
    finally:
        shutil.rmtree(tmp_profile, ignore_errors=True)

    save_session(cookies)
    return cookies


def get_session_cookies() -> dict:
    """Return cookies dict, triggering browser login if no saved session."""
    saved = load_session()
    if saved:
        return cookies_to_jar(saved)
    cookies_list = do_browser_login()
    return cookies_to_jar(cookies_list)


# ---------------------------------------------------------------------------
# Mixamo API helpers
# ---------------------------------------------------------------------------

def make_session(cookies: dict) -> requests.Session:
    s = requests.Session()
    s.cookies.update(cookies)
    s.headers.update({
        "Accept": "application/json",
        "User-Agent": "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36",
        "X-Api-Key": "mixamo2",
        "Origin": MIXAMO_BASE,
        "Referer": f"{MIXAMO_BASE}/",
    })
    return s


def search_animation(session: requests.Session, query: str, page: int = 1) -> list[dict]:
    """Search for animations by name. Returns list of product dicts."""
    params = {
        "query": query,
        "page": page,
        "limit": 10,
        "order": "popularity",
        "type": "Motion%2CMotionPack",
        "character_id": "00000000-0000-0000-0000-000000000002",
    }
    resp = session.get(MIXAMO_ANIMATIONS_API, params=params)
    if resp.status_code != 200:
        print(f"[search] HTTP {resp.status_code} for query '{query}'")
        return []
    data = resp.json()
    return data.get("results", [])


def find_character_id(session: requests.Session, char_name: str) -> str | None:
    """Find the Mixamo character ID by display name."""
    params = {
        "order": "name",
        "limit": 20,
        "page": 1,
        "query": char_name,
        "type": "Character",
    }
    resp = session.get(f"{MIXAMO_API}/v1/products", params=params)
    if resp.status_code != 200:
        return None
    results = resp.json().get("results", [])
    for r in results:
        if r.get("name", "").lower() == char_name.lower():
            return r.get("id") or r.get("character_id")
    if results:
        return results[0].get("id") or results[0].get("character_id")
    return None


def request_export(
    session: requests.Session,
    character_id: str,
    product_id: str,
) -> str | None:
    """Request an FBX export. Returns the export job URL or None."""
    payload = {
        "gms_hash": [{
            "model_id": character_id,
            "trim_end_ms": 0,
            "trim_start_ms": 0,
            "overdrive": 0,
            "mirror": False,
            "inplace": False,
        }],
        "preferences": {
            "format": "fbx7_unity",
            "skin": "false",
            "fps": "30",
            "reducekeyframes": "false",
        },
        "product_name": product_id,
        "type": "Motion",
    }
    resp = session.post(MIXAMO_EXPORT_API, json=payload)
    if resp.status_code not in (200, 201, 202):
        print(f"[export] HTTP {resp.status_code}: {resp.text[:200]}")
        return None
    data = resp.json()
    return data.get("job_result")


def poll_export(session: requests.Session, job_url: str, timeout: int = 120) -> str | None:
    """Poll an export job until ready. Returns the download URL."""
    deadline = time.time() + timeout
    while time.time() < deadline:
        resp = session.get(job_url)
        if resp.status_code != 200:
            time.sleep(3)
            continue
        data = resp.json()
        status = data.get("status", "")
        if status == "completed":
            return data.get("job_result")
        if status in ("failed", "error"):
            print(f"[export] Job failed: {data}")
            return None
        time.sleep(3)
    print("[export] Timeout waiting for export job")
    return None


def download_fbx(
    session: requests.Session,
    download_url: str,
    out_path: Path,
) -> bool:
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
        with zipfile.ZipFile(tmp_zip, "r") as zf:
            for name in zf.namelist():
                if name.endswith(".fbx"):
                    zf.extract(name, out_path.parent)
                    extracted = out_path.parent / name
                    extracted.rename(out_path)
                    break
        tmp_zip.unlink(missing_ok=True)
    else:
        out_path.write_bytes(raw)

    return out_path.exists() and out_path.stat().st_size > 1024


# ---------------------------------------------------------------------------
# Main download flow
# ---------------------------------------------------------------------------

def download_animation(
    session: requests.Session,
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
        print(f"  [skip] {out_filename} already exists ({out_path.stat().st_size // 1024} KB)")
        return True

    if dry_run:
        print(f"  [dry-run] Would download → {out_filename}")
        return True

    # Find character ID
    char_id = find_character_id(session, mixamo_char_name)
    if not char_id:
        char_id = "00000000-0000-0000-0000-000000000002"
        print(f"  [warn] Could not find '{mixamo_char_name}', using generic humanoid")

    # Search animation
    results = search_animation(session, anim_query)
    if not results:
        print(f"  [fail] No results for '{anim_query}'")
        return False
    product_id = results[0].get("id") or results[0].get("product_id") or results[0].get("name", "")
    print(f"  [found] {results[0].get('name', '?')} (id={product_id})")

    # Request export
    job_url = request_export(session, char_id, product_id)
    if not job_url:
        print(f"  [fail] Export request failed")
        return False

    # Poll until ready
    download_url = poll_export(session, job_url)
    if not download_url:
        print(f"  [fail] Export job did not complete")
        return False

    # Download
    ok = download_fbx(session, download_url, out_path)
    if ok:
        print(f"  [ok] Saved {out_filename} ({out_path.stat().st_size // 1024} KB)")
    else:
        print(f"  [fail] Download failed for {out_filename}")
    return ok


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

    cookies = get_session_cookies()
    session = make_session(cookies)

    # Verify session is live
    test_resp = session.get(f"{MIXAMO_API}/v1/characters?limit=1")
    if test_resp.status_code == 401:
        print("\n[auth] ERROR: Session expired (HTTP 401).")
        print("       Delete .session.json and re-run to trigger fresh browser login.")
        print("       BLOCKER: Mike must re-login to Adobe/Mixamo.")
        sys.exit(2)
    elif test_resp.status_code not in (200, 201):
        print(f"\n[auth] WARNING: Unexpected status {test_resp.status_code} — proceeding.")

    downloaded_this_run = 0
    skipped = 0
    failed = 0
    deferred = []

    for enemy_key, mixamo_char_name, _notes in HUMANOID_ENEMIES:
        for anim_display, anim_key in ANIMATIONS:
            out_filename = f"{enemy_key}_{anim_key}.fbx"
            out_path = OUTPUT_DIR / out_filename

            # Already on disk
            if out_path.exists() and out_path.stat().st_size > 1024:
                skipped += 1
                continue

            # Already in progress log
            if resume and is_done(progress, enemy_key, anim_key):
                skipped += 1
                continue

            # Daily limit guard
            total_so_far = progress.get("total_today", 0) + downloaded_this_run
            if total_so_far >= batch_size:
                deferred.append(f"{enemy_key}_{anim_key}")
                continue

            print(f"\n[{downloaded_this_run + 1}/{batch_size}] {enemy_key} / {anim_display} ({mixamo_char_name})")

            for attempt in range(1, MAX_RETRIES + 1):
                ok = download_animation(
                    session=session,
                    enemy_key=enemy_key,
                    mixamo_char_name=mixamo_char_name,
                    anim_key=anim_key,
                    anim_query=ANIM_QUERIES[anim_key],
                    dry_run=dry_run,
                )
                if ok:
                    if not dry_run:
                        mark_done(progress, enemy_key, anim_key)
                    downloaded_this_run += 1
                    break
                else:
                    if attempt < MAX_RETRIES:
                        print(f"  [retry {attempt}/{MAX_RETRIES}] Waiting {RATE_LIMIT_WAIT}s...")
                        time.sleep(RATE_LIMIT_WAIT)
                    else:
                        mark_failed(progress, enemy_key, anim_key, f"failed after {MAX_RETRIES} attempts")
                        failed += 1

            if downloaded_this_run > 0 and not dry_run:
                time.sleep(WAIT_BETWEEN_DOWNLOADS)

    # Summary
    print("\n" + "=" * 60)
    print("BATCH COMPLETE")
    print(f"  Downloaded this run : {downloaded_this_run}")
    print(f"  Skipped (done)      : {skipped}")
    print(f"  Failed              : {failed}")
    print(f"  Deferred (limit)    : {len(deferred)}")
    if deferred:
        print(f"\n  Deferred to next run ({len(deferred)} anims):")
        for d in deferred:
            print(f"    - {d}")
        print(f"\n  Re-run tomorrow: python3 tools/mixamo/download_anims.py --resume")
    print("=" * 60)

    progress["deferred"] = deferred
    save_progress(progress)


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Mixamo humanoid animation batch downloader")
    parser.add_argument("--dry-run", action="store_true", help="List what would be downloaded")
    parser.add_argument("--batch-size", type=int, default=DAILY_LIMIT,
                        help=f"Max downloads this run (default: {DAILY_LIMIT})")
    parser.add_argument("--resume", action="store_true", default=True,
                        help="Skip already-downloaded anims (default: True)")
    parser.add_argument("--no-resume", dest="resume", action="store_false")
    parser.add_argument("--reset-progress", action="store_true",
                        help="Clear progress and restart from scratch")
    args = parser.parse_args()

    run(
        batch_size=args.batch_size,
        dry_run=args.dry_run,
        resume=args.resume,
        reset_progress=args.reset_progress,
    )
