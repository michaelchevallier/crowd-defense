# Smoke Test Build #28 (v6/?cb=28500)

**Date**: 2026-05-11  
**Build URL**: https://michaelchevallier.github.io/crowd-defense/v6/?cb=28500  
**Status**: ✅ LIVE (HTTP 200)

## Deployment Check
- ✅ Page deployed & accessible
- ✅ Unity WebGL loader detected (`UnityLoader` present)
- ✅ Canvas element initialized (`#unity-canvas` 960x600)
- ✅ Build title: "crowd-defense"

## Manual Test Checklist (requires Chrome MCP execution)

| Check | Status | Notes |
|-------|--------|-------|
| Theme rendered | PENDING | Canvas detected, needs 30s Unity load |
| HUD visible (toolbar/hero/wave button) | PENDING | Check DOM after load |
| Hero spawned | PENDING | Verify via `window.__getStats()` |
| Wave button clickable | PENDING | Simulate KeyboardEvent or click |
| Enemies spawn after wave start | PENDING | Monitor entity count delta |
| Towers placeable | PENDING | Test click + drag on map |
| Combat (towers fire) | PENDING | Verify tower bullets/FX |
| Console clean (no errors) | PENDING | Read via `mcp__claude-in-chrome__read_console_messages` |

## Next Steps
Execute via Chrome MCP when available:
1. Navigate to build URL
2. Wait 30s for Unity init
3. Capture console output
4. Run interaction tests (wave button, tower placement)
5. Verify enemy/tower entity counts via `__getStats()`

## Verdict
Build #28 deployment: **✅ CONFIRMED LIVE**  
Functional smoke test: **⏳ PENDING** (requires Chrome MCP interactive session)

---
*Auto-generated 2026-05-11 20:00 UTC*
