# v6 Post-12Agent Smoke Test Report

**Date**: 2026-05-11 19:15 UTC  
**URL**: https://michaelchevallier.github.io/crowd-defense/v6/  
**Build Context**: Phase 3 SWARM shipped (14 new systems), safety net fix `b4888bf` applied

---

## Test Results Summary

### Deployment Status: ✅ PASSED

#### HTTP & Asset Delivery
| Asset | Size | HTTP Status | Notes |
|-------|------|-------------|-------|
| index.html | ~5KB | 200 | ✅ Unity loader template correct |
| WebGL.loader.js | 110KB | 200 | ✅ Entry point present |
| WebGL.framework.js.unityweb | 70KB | 200 | ✅ Runtime framework loaded |
| WebGL.data.unityweb | 17MB | 200 | ✅ Game data archive present |
| WebGL.wasm.unityweb | 9MB | 200 | ✅ WebAssembly binary present |
| TemplateData/style.css | ~3KB | 200 | ✅ UI stylesheet loaded |
| **Total** | **~26MB** | All 200 | ✅ Complete deployment |

#### Canvas & Loading UI
- ✅ Canvas element present: `<canvas id="unity-canvas" width=960 height=600>`
- ✅ Loading bar UI present: progress bar structure found
- ✅ Unity warning/error banner present: `<div id="unity-warning">`
- ✅ Viewport dimensions: 960×600 (16:10 aspect ratio)

---

## Runtime Testing: ⚠️ MANUAL VERIFICATION REQUIRED

Since this is a dynamic WebGL application, the following tests **require live browser execution**. Static HTML analysis is complete; runtime behavior cannot be verified without Chrome MCP or manual browser access.

### Test Checklist (Manual Browser Required)

#### 1. Game Boot & HUD Visibility
- [ ] Game loads without white/black screen (< 15s load time)
- [ ] Console: no red error messages (F12 → Console tab)
- [ ] Gold pill visible in HUD (shows starting gold amount)
- [ ] Wave counter visible in HUD (e.g., "Wave 1/4")
- [ ] Castle HP visible in HUD (shows "HP: 120")
- [ ] Doctrines button visible top-right

#### 2. Canvas Rendering
- [ ] Canvas fills viewport without black bars
- [ ] 3D terrain visible (grid-based 7×15 map)
- [ ] Toon Water shader visible (blue tinted water tile at center)
- [ ] Toon Lava shader visible (red/orange lava borders, or animation)
- [ ] No WebGL rendering errors in console

#### 3. Input Tests (Keyboard)
- [ ] Press `1` → expected: tower type 1 selected (no UI visible, but hotkey registered)
- [ ] Press `2-9` → expected: cycle through tower types
- [ ] Press `0` → expected: deselect tower
- [ ] Press `-` / `=` → expected: speed control hotkeys work
- [ ] Press `N` → expected: wave launches, enemies spawn at spawn gate
- [ ] Press `Shift` → expected: Hero hotkey activates (hero may not spawn if not in scene)
- [ ] Press `B` → expected: Hero ability B (no visible UI if hero not present)
- [ ] Press `U` → expected: Hero utility (no visible UI if hero not present)

#### 4. Tower Placement
- [ ] Click on valid terrain → expected: tower places (if tower type selected via hotkey 1-9)
- [ ] Check if placement feedback works (tower appears, gold deducted)
- [ ] **Known limitation**: Toolbar UI not visible, so placement may require hotkey pre-selection

#### 5. Wave Mechanics
- [ ] Press `N` to launch wave 1 → enemies spawn
- [ ] Enemies follow path (stream → bridge → lava, visible path progression)
- [ ] Enemy count increases as wave progresses
- [ ] Wave counter updates to "Wave 2/4" after completing wave 1
- [ ] **Expected**: 4 waves total (W1-1 POC design: 35, 76, 87, 90 enemies)

#### 6. UI Responsiveness & Zoom
- [ ] Browser zoom 50% → canvas scales correctly
- [ ] Browser zoom 100% → normal 960×600 fit
- [ ] Browser zoom 150% → canvas scales, UI readable
- [ ] Mobile viewport (< 900px width) → check if touch buttons appear (if `?mobile=1` param in URL)
- [ ] Fullscreen button (bottom-right corner) → toggles fullscreen correctly

#### 7. Console Diagnostics
Open F12 Developer Tools → Console tab and look for:
- [ ] No red `Error:` or `Exception` messages
- [ ] No `NullReferenceException` (most critical indicator of init race condition)
- [ ] Expected logs: Unity loader messages, scene load messages
- [ ] **Check for the fix**: If you see "WaveManager late-init in HudController.Update()", safety net is working

---

## Known Issues & Context (from STATUS.md)

### Pre-Smoke Issues (Documented)
| Issue | Status | Impact | Notes |
|-------|--------|--------|-------|
| Wave launch button invisible at runtime | **Fix applied** (commit `b4888bf`) | Medium | Safety net in HudController.Update() → late-init WaveManager.Instance |
| Toolbar selection UI not visible | By design | Low | Input hotkeys 1-9 still functional, no UI toolbar yet |
| 4 waves only (not 10) | Design POC | Low | LevelData W1-1 intentionally has 4 waves (35+76+87+90=288 total mobs) |
| 353 build warnings | Non-blocking | Info | gltfast GLTF import + Roboto font fallback + Sentis shader imports |

### Newly Deployed Systems (14 total)
- ✅ Toolbar (input hotkeys 1-9,0,-,=)
- ✅ Hero (Shift/B/U hotkeys, may not be in scene yet)
- ✅ Boss (wave 4 culmination, mechanics TBD)
- ✅ Perks (passive modifiers, UI TBD)
- ✅ Shop (tower/perk purchase, UI TBD)
- ✅ Doctrine (selector button top-right visible)
- ✅ Speed (time scale modifier, hotkey -)
- ✅ Cutscenes (intro/outro sequences, triggers TBD)
- ✅ Skins (tower/enemy cosmetics, UI TBD)
- ✅ Minimap (HUD miniature map, visibility TBD)
- ✅ Combo (multi-kill streak system, HUD display TBD)
- ✅ Achievements (unlock toasts, display TBD)
- ✅ Tutorial (FSM 6-step, overlay TBD)
- ✅ WorldMap (meta-progression, UI TBD)

---

## Static Analysis Findings: ✅ PASSED

1. **Deployment integrity**: All critical files present and served via HTTPS
2. **Build artifacts**: Complete WebGL build with data, framework, loader, and WASM
3. **HTML structure**: Correct Unity template, loading UI intact
4. **Canvas setup**: 960×600 viewport, correct aspect ratio for gameplay
5. **Asset sizes**: 26MB total (reasonable for multi-system WebGL build)

---

## Recommendations for Full QA-5 E2E

### Next Steps
1. **Run manual gameplay test** with Chrome/Edge/Safari browser
2. **Take screenshots** at key moments:
   - Game boot screen (loading bar)
   - Main gameplay (terrain visible, HUD readable)
   - After pressing N (enemy wave spawning)
   - After clicking terrain (tower placement attempt)
3. **Record console logs** and any errors
4. **Test zoom levels** (50%, 100%, 150%)
5. **Test mobile simulation** (`?mobile=1` URL param or DevTools viewport)

### Critical Path Tests
```
1. Boot (< 15s) → HUD visible → N key (enemies spawn) → Click terrain (placement)
2. If all 4 work → PASS QA-5, proceed to next sprint
3. If any fail → capture console error, escalate to feature-dev
```

### Escalation Checklist
- [ ] Wave launch still fails → re-apply safety net or check HudController.Instance
- [ ] Enemies don't spawn → check WaveManager.SpawnWave() in console
- [ ] Canvas black/white → check WebGL context init in loader
- [ ] Tower placement broken → check Raycasting + placement validation logic

---

## Test Environment

**Browser**: Chrome/Edge/Safari recommended (WebGL 2.0 support required)  
**Platform**: Desktop (Windows/Mac/Linux) or tablet  
**Network**: Stable internet (26MB download)  
**DevTools**: F12 console open for error diagnostics  

---

## Conclusion

**Deployment Status**: ✅ All static checks passed, deployment is complete and serving all assets correctly.

**Next Action**: Launch browser and follow manual test checklist above. If all 4 critical path tests pass, QA-5 is satisfied and ready for next sprint.

**Estimated Manual Test Duration**: 10-15 minutes for full coverage.

---

*Report generated by v6-post-12agent-smoketest.md (2026-05-11)*
