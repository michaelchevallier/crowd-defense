"""
Mixamo auto-downloader stub — Crowd Defense ASSET-GEN axis.

STATUS : stub. Login Adobe non implémenté (Mike fait manuellement 1× puis
fournit le cookie session via ~/.mixamo-session.json — cf README.md).

Usage prévu (Phase 3 post-axis-merge ou Phase 4) :
    python3 tools/mixamo/download_anims.py \
        --char goblin \
        --anims idle walk run attack die \
        --target Assets/Animations/Mixamo/

    # OR batch all enemies (reads list from --enemies-file):
    python3 tools/mixamo/download_anims.py \
        --enemies-file tools/mixamo/enemies_humanoid.txt \
        --anims idle walk run attack die \
        --target Assets/Animations/Mixamo/

Args :
    --char <name>          : single char base (e.g. "goblin", "knight")
    --enemies-file <path>  : alt to --char, file with one char name per line
    --anims <list>         : Mixamo anim labels to fetch (idle, walk, ...)
    --target <dir>         : output dir, .fbx files dropped here
    --check-auth           : test cookie session validity, no downloads
    --dry-run              : list what WOULD be downloaded, do not fetch

Auth : reads ~/.mixamo-session.json with XSRF-TOKEN + mixamo2-session-id.
"""
from __future__ import annotations

import argparse
import json
import os
import sys
from pathlib import Path

SESSION_FILE = Path.home() / ".mixamo-session.json"
MIXAMO_BASE_URL = "https://www.mixamo.com"

ANIM_KEYWORDS = {
    "idle": "Idle",
    "walk": "Walking",
    "run": "Running",
    "attack": "Attacking",
    "die": "Dying Backwards",
}


def load_session() -> dict | None:
    if not SESSION_FILE.exists():
        return None
    try:
        return json.loads(SESSION_FILE.read_text())
    except json.JSONDecodeError:
        print(f"[mixamo] ERROR malformed {SESSION_FILE}", file=sys.stderr)
        return None


def check_auth(session: dict) -> bool:
    """Stub : ping Mixamo with cookie, verify HTTP 200."""
    # TODO : implement requests.get(MIXAMO_BASE_URL, cookies=session) check
    required = ["XSRF-TOKEN", "mixamo2-session-id"]
    missing = [k for k in required if k not in session]
    if missing:
        print(f"[mixamo] ERROR missing cookie keys: {missing}", file=sys.stderr)
        return False
    print("[mixamo] cookie keys present — full validation requires network call (not implemented yet)")
    return True


def download_anim(char: str, anim: str, target_dir: Path, session: dict, dry_run: bool) -> bool:
    """Stub : would POST to Mixamo's animation export endpoint then GET the FBX."""
    keyword = ANIM_KEYWORDS.get(anim.lower(), anim)
    out_path = target_dir / f"{char}_{anim}.fbx"

    if dry_run:
        print(f"[mixamo] DRY {char} + {keyword} -> {out_path}")
        return True

    # TODO : real implementation
    #   1. Upload char mesh if needed (POST /api/v1/characters)
    #   2. Search anim by keyword (GET /api/v1/animations?q={keyword})
    #   3. POST export job (binding the anim onto the char)
    #   4. Poll job status until ready
    #   5. GET signed download URL, write to out_path
    print(f"[mixamo] STUB cannot download {char}+{anim} yet — implement Phase 3 post-merge", file=sys.stderr)
    return False


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--char", help="single char name")
    parser.add_argument("--enemies-file", help="file with char names, one per line")
    parser.add_argument("--anims", nargs="+", default=["idle", "walk", "run", "attack", "die"])
    parser.add_argument("--target", default="Assets/Animations/Mixamo/")
    parser.add_argument("--check-auth", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    session = load_session()
    if session is None:
        print(f"[mixamo] FATAL no session file {SESSION_FILE} — cf README.md", file=sys.stderr)
        sys.exit(1)

    if not check_auth(session):
        sys.exit(2)

    if args.check_auth:
        print("[mixamo] auth check OK")
        sys.exit(0)

    target_dir = Path(args.target)
    target_dir.mkdir(parents=True, exist_ok=True)

    chars: list[str] = []
    if args.char:
        chars.append(args.char)
    elif args.enemies_file:
        chars.extend(
            line.strip() for line in Path(args.enemies_file).read_text().splitlines() if line.strip()
        )
    else:
        print("[mixamo] FATAL --char or --enemies-file required", file=sys.stderr)
        sys.exit(3)

    ok = 0
    fail = 0
    for char in chars:
        for anim in args.anims:
            if download_anim(char, anim, target_dir, session, args.dry_run):
                ok += 1
            else:
                fail += 1

    print(f"[mixamo] done — ok={ok} fail={fail} target={target_dir}")
    sys.exit(0 if fail == 0 else 4)


if __name__ == "__main__":
    main()
