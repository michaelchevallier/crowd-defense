# V4 vs V6 Visual Comparison Plan

**Objective**: Compare V4 (Phaser/Three.js legacy) vs V6 (Unity WebGL) for visual feature parity and identify migration gaps.

**Test Date**: 2026-05-12  
**Tester**: Claude Code  
**Method**: Live Chrome MCP navigation + screenshot capture + DOM inspection

---

## URLs to Compare

| Version | URL | Tech Stack | Status |
|---------|-----|-----------|--------|
| **V4** | https://michaelchevallier.github.io/lava_game/v4/?debug=1 | Phaser 3 + Three.js | Frozen (reference) |
| **V6** | https://michaelchevallier.github.io/crowd-defense/v6/?cb=37800 | Unity 6 WebGL | Active (under test) |

---

## Inspection Checklist

### 1. Layout & HUD Structure
- [ ] Tool toolbar position (left/bottom/right)
- [ ] Tower selector: 13 buttons visible or hidden by design?
- [ ] Hero panel: position, elements visible (health, ability slots)
- [ ] Gold counter: format, visibility
- [ ] Wave counter: format, visibility
- [ ] Castle HP: format, visibility
- [ ] Doctrines button: position (expected: top-right)
- [ ] Minimap: present? position?

### 2. Canvas Rendering
- [ ] Canvas resolution: expected 960×600 or responsive
- [ ] Terrain visible (grid 7×15 map)
- [ ] Path visualization (white/yellow line from spawn to exit)
- [ ] Water tiles: shader quality (Phaser vs Unity comparison)
- [ ] Lava borders: animated or static
- [ ] Grid overlay: debug mode visible?
- [ ] Sprite quality: towers, enemies, effects

### 3. Game State
- [ ] Hero present in scene (yes/no) — expected: no hero in V6 W1-1
- [ ] Initial gold shown (expected: 450 in both)
- [ ] Wave counter (expected: "Wave 1/4" in both)
- [ ] Castle HP (expected: 120 in both)

### 4. Input Responsiveness
- [ ] Hotkey `1`: tower selection 1 (no UI in V6 by design)
- [ ] Hotkey `N`: wave launch (enemies spawn)
- [ ] Hotkey `Shift`: hero activation (may not visible if hero not in scene)
- [ ] Mouse click: terrain hover effect, placement feedback

### 5. Console Diagnostics
- [ ] Error count: 0 (both versions)
- [ ] Warning count: expected ~10 (V6 has build warnings)
- [ ] Critical red errors: none expected
- [ ] Key log messages: "Scene loaded", "WaveManager initialized"

---

## Visual Gaps to Document

Format: **[Gap Name]** (Priority: HIGH/MEDIUM/LOW)
- Observation: what's different
- V4 behavior: expected baseline
- V6 behavior: current state
- Criticality: blocks gameplay or cosmetic?

### Expected Gap Examples

1. **Toolbar Visibility** (Priority: MEDIUM)
   - Observation: V4 shows 13 tower buttons, V6 hides them (input hotkey only)
   - V4 behavior: Visual tower selector in toolbar
   - V6 behavior: Hotkey-based selection (1-9, 0 to deselect)
   - Criticality: Gameplay unaffected, UX reduced

2. **Hero Panel** (Priority: LOW)
   - Observation: V4 shows hero in right panel, V6 hero not in W1-1 scene
   - V4 behavior: Hero stats visible, portrait shown
   - V6 behavior: Hero gameplay system in place, hero not spawned in level yet
   - Criticality: Cosmetic, hero mechanics designed for later sprint

3. **Map Water/Lava Shaders** (Priority: MEDIUM)
   - Observation: Water/lava visual fidelity difference
   - V4 behavior: Three.js custom shaders
   - V6 behavior: Unity URP Toon shaders (Outline system)
   - Criticality: Visual polish, doesn't block gameplay

---

## Screenshots to Capture

### Per Version (V4, V6)
1. **Boot Screen** (5s after load) — HUD layout, canvas size
2. **Gameplay Ready** (30s after load) — full scene, terrain visible
3. **Console Tab** (F12 open) — error/warning counts
4. **After Wave Launch** (press N) — enemy spawn, path follow
5. **Canvas DataURL Info** — resolution, rendering status

### Comparison Table
| Aspect | V4 Screenshot | V6 Screenshot | Match? |
|--------|---------------|---------------|--------|
| HUD Layout | [img] | [img] | yes/no |
| Canvas Size | 960×600 | 960×600 | yes |
| Terrain Visible | yes | yes | yes |
| Path Visual | white/yellow | ? | ? |
| Hero Panel | yes | no | expected |
| Wave Button | yes | hidden | expected |
| Console Errors | 0 | ? | ? |

---

## Execution Steps

1. **Open V4 in Chrome MCP tab A**
   ```
   mcp__Claude_in_Chrome__navigate(tabId=A, url="https://michaelchevallier.github.io/lava_game/v4/?debug=1")
   ```

2. **Wait 30s** for full load (Three.js + Phaser boot)

3. **Capture V4 state**
   ```js
   window.__getStats?.() // entity counts
   document.querySelector('canvas').toDataURL('image/png') // screenshot
   document.body.innerHTML.substring(0, 1000) // DOM snippet
   ```

4. **Open V6 in Chrome MCP tab B**
   ```
   mcp__Claude_in_Chrome__navigate(tabId=B, url="https://michaelchevallier.github.io/crowd-defense/v6/?cb=37800")
   ```

5. **Clear Service Worker cache** (Unity builds may be cached)
   ```js
   caches.keys().then(ks => ks.forEach(k => caches.delete(k)));
   navigator.serviceWorker.getRegistrations().then(rs => rs.forEach(r => r.unregister()));
   setTimeout(() => location.reload(), 300);
   ```

6. **Wait 30s** for full load (Unity WebGL boot)

7. **Capture V6 state** (same as V4)

8. **Compare & Document**
   - Side-by-side DOM structure
   - Canvas sizes
   - Console message diff
   - Visual gaps list

---

## Expected Outcomes (from Sprint MIGRATE context)

### V4 (Reference/Frozen)
- Phaser 3 engine, Three.js for 3D water/terrain
- Full toolbar UI (13 tower buttons)
- Hero panel right-side
- 4 waves (W1-1 POC)
- No doctrines, no perks (V4 is baseline)

### V6 (Current/Target)
- Unity 6 WebGL URP
- Toolbar hidden by design (hotkey input: 1-9,0,-,=)
- Hero system exists but hero not spawned in W1-1 scene
- 4 waves (same W1-1 POC)
- Doctrines system deployed (button visible)
- Perks system deployed (mechanics TBD UI)
- 14 new systems (shop, boss, skins, minimap, combo, achievements, tutorial, worldmap)

### % Matching ISO (expected)
- **Gameplay Mechanics**: 95% (same economy, wave, tower cost/damage)
- **Visual Layout**: 75% (toolbar hidden, hero panel hidden, same terrain)
- **Input Behavior**: 90% (hotkeys work, UI selection hidden)
- **Performance**: TBD (need FPS measurement in both)

---

## Troubleshooting

### V4 loads black screen
→ Three.js context init failed, check WebGL 2.0 support in DevTools

### V6 loads white screen
→ Unity loader issue, check console for "WebGL context lost" or loader errors

### Canvas not rendering
→ Check `<canvas id="unity-canvas">` in DevTools → Elements tab

### Hotkeys not working
→ Check Console for "UnityInstance not ready" or input event listener registration

---

## Next Actions After Comparison

1. **High Priority Gaps** → escalate to feature-dev (if blocking gameplay)
2. **Visual Polish Gaps** → add to next sprint's visual refinement
3. **UI UX Gaps** → plan for post-alpha UI overhaul (Wave 4+)
4. **Documentation** → update MIGRATION.md with v4→v6 known differences

