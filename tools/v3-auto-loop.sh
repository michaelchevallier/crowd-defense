#!/usr/bin/env bash
# V3AutoLoop — full QA cycle: validate + screenshot + pixel diff
# Usage: bash tools/v3-auto-loop.sh [--keep-unity-open]
set -euo pipefail

UNITY_PATH="/Applications/Unity/Hub/Editor/6000.0.47f1/Unity.app/Contents/MacOS/Unity"
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
AUTOLOOP_JSON="$PROJECT/Library/V3AutoLoop/latest.json"
PIXELDIFF_JSON="$PROJECT/Library/V3PixelDiff/report.json"
KEEP_OPEN="${1:-}"

log() { echo "[v3-auto-loop] $*"; }

# ── 1. Kill any running Unity Editor for this project ─────────────────────────
log "Stopping Unity Editor..."
pkill -f "Unity.*$PROJECT" 2>/dev/null || true
sleep 2

# ── 2. Run Unity batch: V3AutoLoop ───────────────────────────────────────────
log "Launching Unity batch mode (V3AutoLoop)..."
"$UNITY_PATH" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PROJECT" \
  -executeMethod CrowdDefense.EditorTools.V3AutoLoop.AutoLoop \
  -logFile "$PROJECT/Library/V3AutoLoop/batch.log" \
  ; UNITY_EXIT=$?

if [ "$UNITY_EXIT" -ne 0 ]; then
  log "WARNING: Unity exited with code $UNITY_EXIT (may indicate test failures)"
fi

# ── 3. Parse AutoLoop JSON verdict ───────────────────────────────────────────
AUTOLOOP_VERDICT="UNKNOWN"
if [ -f "$AUTOLOOP_JSON" ]; then
  AUTOLOOP_VERDICT=$(python3 -c "
import json,sys
d=json.load(open('$AUTOLOOP_JSON'))
s=d.get('summary','')
print('PASS' if 'ALL PASS' in s else 'FAIL')
" 2>/dev/null || echo "PARSE_ERROR")
  log "AutoLoop summary: $(python3 -c "import json; print(json.load(open('$AUTOLOOP_JSON'))['summary'])" 2>/dev/null || echo 'n/a')"
else
  log "WARNING: $AUTOLOOP_JSON not found"
  AUTOLOOP_VERDICT="MISSING"
fi

# ── 4. Pixel diff (Python, no Unity needed) ──────────────────────────────────
log "Running pixel diff..."
PIXELDIFF_VERDICT="SKIP"
if python3 "$PROJECT/tools/v3-pixel-diff.py"; then
  PIXELDIFF_VERDICT="PASS"
else
  PIXELDIFF_VERDICT="FAIL"
fi

# ── 5. Global verdict ─────────────────────────────────────────────────────────
if [ "$AUTOLOOP_VERDICT" = "PASS" ] && [ "$PIXELDIFF_VERDICT" != "FAIL" ]; then
  GLOBAL="PASS"
else
  GLOBAL="FAIL"
fi

echo ""
log "=============================="
log "  AutoLoop  : $AUTOLOOP_VERDICT"
log "  PixelDiff : $PIXELDIFF_VERDICT"
log "  GLOBAL    : $GLOBAL"
log "=============================="
log "Reports:"
log "  $AUTOLOOP_JSON"
log "  $PIXELDIFF_JSON"

# ── 6. Relaunch Unity Editor (unless --keep-unity-open) ──────────────────────
if [ "$KEEP_OPEN" != "--keep-unity-open" ]; then
  log "Relaunching Unity Editor..."
  open -a Unity "$PROJECT" &
fi

[ "$GLOBAL" = "PASS" ] && exit 0 || exit 1
