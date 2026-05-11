# V5 → Unity Gap Analysis

**Survey Date:** 2026-05-11  
**V5 Source:** `/Users/mike/Work/milan project/src-v3/`  
**Unity Port:** `/Users/mike/Work/crowd-defense/Assets/Scripts/`  

---

## 1. Missing Systems

| System | Purpose | Priority |
|--------|---------|----------|
| **Particles.js** | 3D sprite pool for VFX (400-sprite pool, color tinting, physics) | H |
| **Shaders.js** | Animated water/lava tiles, custom materials (14 shaders: water, lava, terrain) | H |
| **ToonMaterial.js** | Toon shader application + scene background tinting | H |
| **AssetVariants.js** | Procedural skin/theme swapping (tower models by skin, enemy colors) | H |
| **PathTiles.js** | Dynamic tile placement: water/lava streams, path variants (21KB system) | M |
| **PathVariant.js** | Path layout variations (3 variants per world) | M |
| **MapPathfinder.js** | A* pathfinding for enemies (not currently used, depth). | L |
| **Tutorial.js** | Interactive tutorial system (7 tutorial phases + overlay) | M |
| **Device.js** | Mobile device detection, orientation change events, touch flag | M |
| **RunMap.js** | Roguelike map generation, node graph, boss selection | M |
| **AssetLoader.js** | Asset preloading system (gltf, audio, textures) | L |

---

## 2. Missing UI Panels / Scenes

| V5 Scene | Phaser-based | Unity Equivalent Needed | Priority |
|----------|--------------|------------------------|----------|
| **RunMode.js** | Perk selection overlay, run stats, victory/defeat screens, school picker | School Select Modal, Perk Choice UI, Run Summary | H |
| **WorldMap.js** | Roguelike map display (nodes, edges, boss indicators) | Roguelike Map Canvas | H |
| **Minimap.js** | Canvas-based 2D level minimap (96–240px) | In-Game Minimap Overlay | M |
| **BossUI.js** | Boss health bar, boss name banner, enrage vignette flash | Boss Health Bar Panel | M |
| **Shop.js** | Cosmetic shop (skins, tower models, themes, seasonal pass) | Cosmetics Shop UI | M |
| **Toolbar.js** | Top-right UI bar (pause, settings, menu icons) | HUD Toolbar Buttons | M |
| **TickMetrics.js** | FPS meter, combo tracker, wave countdown timer, ult cooldown | Debug FPS Meter + Timers | L |
| **Popups.js** | Toast notifications (coin gain, damage, gemstone pickups) | Toast/Popup Manager | M |

---

## 3. Missing Data Files

| File | Contents | Priority |
|------|----------|----------|
| **cutscenes.js** | 10 world intros (MD text, icons). Shown on level 1 of each world. | M |
| **events.js** | Random event tables (6 event types, 20+ entries) | M |
| **metaUpgrades.js** | Meta progression tree (9 upgrades × 3 tiers, cost/reward) | H |
| **modifiers.js** | Curse/blessing modifiers (8 total: -30% range curse, +50% gold blessing) | M |
| **skins.js** | Cosmetic skins (unlock rules, tower model refs) | M |
| **themes.js** | Environment themes (colors, skybox configs, 10 worlds) | M |

---

## 4. Partially Ported / Incomplete

| File | Status | Gap |
|------|--------|-----|
| **WeatherController.cs** | Partial | PlayAmbientAudio() + StopAmbientAudio() stubs only; no actual weather VFX (rain, fog, storms) |
| **VfxPool.cs** | Stub | TODO: replace VFX enabled check; no particle effect pooling |
| **Achievements.cs** | Partial | TODO: toast notification UI; predicates not evaluated |
| **Hero.cs** | Partial | Hero projectiles stub; needs full integration with pool |
| **TutorialState.cs** | Partial | TutorialOverlay exists but no Tutorial.js multi-phase system |

---

## 5. System Architecture Gaps

### Rendering Pipeline
- **Missing:** Post-processing effects (EffectComposer, OutlinePass integration)
- **Missing:** Weather layer (clouds, rain particles, atmospheric effects)
- **Missing:** Scene background gradient (makeSkyGradient in Weather.js)

### Meta Systems
- **Cosmetics:** Skins, themes, tower model variants not wired to save/shop
- **Meta Progression:** Upgrade tree + tier unlock logic not in Unity
- **Roguelike Runs:** Map generation + node selection UI not ported

### Audio Integration
- **Missing:** Music transitions (boss/intense phases in BossUI.js)
- **Partial:** Audio mute/volume in SaveSystem; playback hooks incomplete

### Mobile Support
- **Missing:** Device.js orientation detection + responsive UI adjustments
- **Missing:** Touch-aware UI scaling (minimap resizes to 96px on touch)

---

## 6. Priority Ranking: Next 5 Ports for Parallel Execution

1. **Particles.js + VfxPool Rework** — Unlock visual juice (VFX pooling, colored death explosions). Blocks RunMode.js visuals.
   - *Effort:* 1 day | *Blocks:* 3 others

2. **Shaders.js + ToonMaterial.js** — Toon shading, water/lava tile animations. Core visual identity.
   - *Effort:* 1.5 days | *Blocks:* PathTiles, Scene rendering

3. **Cutscenes + RunMode.js (School Select Modal)** — Narrative beats + early roguelike UI. Minimal deps.
   - *Effort:* 1 day | *Blocks:* Run flow

4. **MetaUpgrades + SaveSystem Extension** — Meta progression tree. Needed for shop + progression loops.
   - *Effort:* 0.5 day | *Blocks:* Shop, Economy loops

5. **Device.js + Minimap Canvas** — Mobile-responsive UI + in-game minimap. Improves UX across all levels.
   - *Effort:* 1 day | *Blocks:* RunMode, BossUI refinements

**Parallel stream (if 2+ devs):**
- PathTiles.js + PathVariant.js (visual map variety)
- Tutorial.js multi-phase system (player onboarding)
- Shop.js cosmetics UI (monetization readiness)

---

## Notes

- **V5 uses Phaser 3 + Three.js:** Most systems are JS-only (no Three.js model ports needed for code logic).
- **Data-driven:** All enemy types, perks, towers defined in `/data/*.js`; port as ScriptableObject registries.
- **Multiplayer hazard:** No multiplayer code detected; all systems single-player safe.
- **Test coverage:** V5 has `systems/__tests__/` (light coverage); Unity has none yet.

---

**Generated:** 2026-05-11 | **Analyst:** File Search Agent
