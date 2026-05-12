# V4→V6 Parity Fixes — Auto-Dispatch Queue

**Master coordinator** : Opus orchestrates 5 tickets in sequence (can parallelize T-VISUAL-01 + T-VISUAL-02 if dual agent).
**Estimated total** : ~20h execution → ~2.5 business days Sonnet pipeline.
**Target delivery** : Phase 3 visual polish complete, pre-merge to main.

---

## Ticket T-VISUAL-01 : Animated Terrain Shaders

**Type** : feature-dev (visual system)  
**Estimé** : 3-4 commits, 4 hours  
**Bloqué par** : none  

### Brief

V4 Shaders.js has 4-frame lava/water keyframe animation loop (0.5s cycle). V6 MapRenderer uses static URP Standard material. Goal: implement animated terrain via Shader Graph.

**Current state** : MapRenderer.cs applies PlainShader to terrain quad; no animation.

**Deliverables** :
1. **Shader Graph** : URP-compatible animated material (time-driven frame interpolation)
   - Frames: 4 textures per theme (lava_0..3, water_0..3)
   - Cycle: 0.5s per full loop (8 FPS frame advance)
   - Blend: Lerp adjacent frames for smoothness
2. **Texture atlas** : Re-export 4 keyframe PNGs per terrain theme from Blender ComfyUI
3. **MapRenderer.cs update** : Load animated material variant, set theme-driven texture refs
4. **Integration test** : Play W1-1, verify lava animates on spawn

**Files to create/modify** :
- Create: `Assets/Shaders/AnimatedTerrain.shadergraph` (Shader Graph)
- Create: `Assets/Materials/Terrain_Lava_Animated.mat`
- Create: `Assets/Materials/Terrain_Water_Animated.mat`
- Modify: `Assets/Scripts/Systems/MapRenderer.cs` — ApplyThemeMaterial() hook
- Modify: `Assets/ScriptableObjects/Themes/` — add animated texture refs to ThemeData SO

**Verification** :
```bash
unity -batchmode -projectPath . -executeMethod CrowdDefense.Build.BuildScript.BuildWebGL -quit
# Test /v6/ Chrome: W1-1 lava terrain animates smoothly (no stuttering)
# Screenshot: lava tiles rotating frame-by-frame visible
```

---

## Ticket T-VISUAL-02 : Dynamic Weather System

**Type** : feature-dev (visual system)  
**Estimé** : 3-4 commits, 5 hours  
**Bloqué par** : none (parallel with T-VISUAL-01 ok)  

### Brief

V4 Weather.js emits particles (spores/sand/embers/confetti) based on current theme + frequency. V6 has no weather. Goal: implement WeatherSystem singleton.

**Current state** : No weather particles; themes have weather type but not used.

**Deliverables** :
1. **WeatherSystem.cs singleton** : LateUpdate() ticks weather emitter
   - Init: theme-driven particle config (color, speed, lifetime, scale, yLift)
   - Emit: random pos overhead camera frustum edge, velocity toward center
   - Frequencies: spores 120ms, sand 80ms, embers 60ms, confetti 120ms (V4 parity)
2. **Particle config per weather type** :
   - `spores` : 0x9bff7a green, speed 0.3, life 4s, scale 0.15, yLift 0.2
   - `sand` : 0xffe0b0 tan, speed 1.5, life 2.5s, scale 0.18+rnd, yLift 0
   - `embers` : 0xff5520 orange, speed 1, life 2s, scale 0.18, yLift 1.5
   - `confetti` : multicolor (rainbow), speed 0.8, life 3s, scale 0.12, yLift 0.5
3. **Integration** : LevelRunner.cs calls WeatherSystem.Tick(dt, themeId) in gameplay loop
4. **VFX pool hook** : WeatherSystem.Emit() → Particles.Emit() (reuse particles pool from T-VISUAL-05)

**Files to create/modify** :
- Create: `Assets/Scripts/Systems/WeatherSystem.cs` (singleton)
- Create: `Assets/ScriptableObjects/Themes/WeatherConfig.cs` (data class)
- Modify: `Assets/Scripts/Entities/LevelRunner.cs` — call WeatherSystem.Tick() per frame
- Modify: `Assets/ScriptableObjects/Themes/ThemeData.cs` — add weatherType + config refs

**Verification** :
```bash
# Play W3-1 (desert theme): sand particles drift across screen
# Play W5-1 (forest theme): spores float gently
# Screenshot: particles visible, moving overhead
```

---

## Ticket T-VISUAL-03 : Boss Shader Validation (Jellyfish/Hologram)

**Type** : feature-dev (visual system)  
**Estimé** : 2 commits, 2 hours  
**Bloqué par** : none  

### Brief

V6 has Shaders.cs with jellyfish + hologram shader code, but **untested in play mode**. Goal: validate shaders applied correctly to bosses Kraken + AI Hub.

**Current state** : Shader code exists (createJellyfishMaterial, createHologramMaterial), no enemy integration.

**Deliverables** :
1. **Enemy.cs integration** : Boss phase logic applies shader overlay
   - Kraken (W7 boss): jellyfish shader on spawn
   - AI Hub (W8 boss): hologram shader on spawn
   - Apply via ApplyThemeMaterial() or direct Material assignment
2. **Shader animation** : Verify uTime uniform updates per frame
3. **Play-mode validation** : Screenshot Kraken + AI Hub with overlay effect visible
4. **Fallback testing** : Disable shader, verify material reverts to base color

**Files to modify** :
- Modify: `Assets/Scripts/Entities/Enemy.cs` — Boss.Init() → ApplyBossShader() hook
- Modify: `Assets/Scripts/Visual/Shaders.cs` — ensure createJellyfish/Hologram() auto-called at boss spawn

**Verification** :
```bash
# Unit test: instantiate Kraken, check _meshChild material is jellyfish
# Play test: W7 Kraken spawns with wobbly jellyfish overlay (visual diff vs standard mesh)
# Screenshot: Kraken has distinct visual signature
```

---

## Ticket T-VISUAL-04 : Floating Damage Popups

**Type** : feature-dev (visual system)  
**Estimé** : 3 commits, 3 hours  
**Bloqué par** : T-VISUAL-05 (particle pool, optional)  

### Brief

V4 has canvas-rendered damage number sprites (makeDamageSprite). V6 has text labels only. Goal: implement 3D billboard popups matching V4 style.

**Current state** : Enemy.OnHit() logs damage but doesn't spawn popup.

**Deliverables** :
1. **DamagePopup.cs MonoBehaviour** : 3D quad (camera-facing billboard)
   - Canvas-rendered texture with damage number (matching V4 style)
   - Lifetime: 0.5s fade-out
   - Movement: float upward 1.5 units, fade alpha
2. **DamagePopupPool** : Object pool 50× instances (reuse)
3. **Enemy.cs hook** : OnHit() → DamagePopupPool.Spawn(dmg, color, pos)
   - Color by damage type (white=default, yellow=crit, red=armor break)
4. **Prefab** : `Assets/Prefabs/DamagePopup.prefab`

**Files to create/modify** :
- Create: `Assets/Scripts/UI/DamagePopup.cs`
- Create: `Assets/Prefabs/DamagePopup.prefab`
- Modify: `Assets/Scripts/Entities/Enemy.cs` — RegisterDamage(amount) → spawn popup
- Modify: `Assets/Scripts/Systems/LevelRunner.cs` — init DamagePopupPool

**Verification** :
```bash
# Play W1-1: place tower, kill enemies → damage numbers float upward
# Screenshot: popups visible, fade smoothly, colors distinct
```

---

## Ticket T-VISUAL-05 : Full Particle Pool + VFX Engine

**Type** : feature-dev (visual/performance)  
**Estimé** : 4-5 commits, 6 hours  
**Bloqué par** : none  

### Brief

V6 JuiceFX.cs covers shake/flash/slowmo; full particle pool pending. Goal: implement 400-particle object pool + integrate into gameplay.

**Current state** : Particles.Emit() calls exist but limited pool. Tower death, hit bursts sparse.

**Deliverables** :
1. **VfxPool.cs singleton** : 400-particle pool, circular buffer cursor (V4 parity)
   - Particle struct: pos, vel, life, scale, color, texture
   - Spawn: set properties, mark alive
   - Update: advance life, apply gravity/drag, mark dead when life < 0
   - Render: batch render via Graphics.DrawMeshInstanced()
2. **Particle usage** :
   - Tower death explosion (radial burst, 10 particles)
   - Tower hit feedback (small burst, 3 particles)
   - Enemy death (enemy color burst, 8 particles)
   - Knockback dust (at feet, 2 particles)
   - Slow effect aura (cyan ring, 1 loop)
3. **Performance baseline** : <1ms particle tick @ 60 FPS
4. **Integration** : Tower.OnHit, Enemy.HandleDeath, Synergies.OnKnockback hooks

**Files to create/modify** :
- Create: `Assets/Scripts/Systems/VfxPool.cs`
- Create: `Assets/Scripts/Common/Particle.cs` (struct)
- Modify: `Assets/Scripts/Entities/Tower.cs` — OnHit() → VfxPool.Emit(position, type)
- Modify: `Assets/Scripts/Entities/Enemy.cs` — HandleDeath() → VfxPool.Emit(..., type)
- Modify: `Assets/Scripts/Systems/LevelRunner.cs` — init VfxPool

**Verification** :
```bash
# Play W1-1: kill 50 enemies, place + sell 10 towers
# Profiler: Particles.Update < 1ms per frame
# Screenshot: particle effects visible throughout level
# Stress test: endless mode W10, verify no particle stutter
```

---

## Master Timeline

| Week | Ticket | Assignee | Status |
|------|--------|----------|--------|
| W1 Mon | T-VISUAL-01 (Shaders) | Sonnet-A | Ready |
| W1 Mon–Tue | T-VISUAL-02 (Weather) | Sonnet-B | Parallel |
| W1 Tue | T-VISUAL-03 (Boss shaders) | Sonnet-A | After 01 |
| W1 Wed | T-VISUAL-04 (Damage popups) | Sonnet-B | After 02 |
| W1 Thu | T-VISUAL-05 (VFX pool) | Sonnet-A | After 03 |
| W1 Fri | QA + merge to main | Opus | All 5 |

**Critical path** : T-VISUAL-01 and T-VISUAL-05 are dependency bottlenecks; parallelize T-01/02 + 03/04 where possible.

---

## QA Gate Checklist (Pre-merge)

- [ ] All 5 shaders render without errors
- [ ] All 5 features play-tested in W1-1
- [ ] No particle stutter at 60 FPS
- [ ] Boss shaders visually distinct (Kraken ≠ AI Hub)
- [ ] Damage popups fade correctly
- [ ] Weather particles fit theme
- [ ] WebGL build passes (no shader compilation errors)
- [ ] Save file not corrupted (round-trip gameplay)

---

**Coordinator notes** :
- Sonnet agents: share shader + common utility functions (Particles.Emit wrapper)
- Code review focus: particle pool thread-safety + shader perf (especially animated terrain)
- Rollback plan: disable shader features via #if feature flags if compile fails

