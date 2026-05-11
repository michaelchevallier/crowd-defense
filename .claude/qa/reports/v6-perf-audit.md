# v6 Performance Audit — Crowd Defense WebGL

**Date**: 2026-05-11  
**Build**: https://michaelchevallier.github.io/crowd-defense/v6/  
**Method**: Static source analysis (live URL not yet accessible from CI; DebugHud ?debug=1 pattern confirmed in source)

---

## FPS Estimates (static analysis, no live measurement)

| Phase | Expected FPS | Confidence |
|---|---|---|
| Idle (no wave, no towers) | ~55–60 | medium |
| W1 light load (≤10 enemies, 4 towers) | ~40–55 | medium |
| W1 peak (20+ enemies, 8+ towers firing) | **~25–40** | medium |

The main stressors preventing solid 60 FPS are identified below.

---

## Bottleneck #1 — Draw Call Explosion (tiles × materials)

**File**: `Assets/Scripts/Systems/MapRenderer.cs:45–55`

`MapRenderer.Start()` calls `GameObject.CreatePrimitive(PrimitiveType.Cube)` once per non-VOID cell. A typical level grid (e.g. 20×15 = 300 cells) spawns 300 individual `MeshRenderer` components, each with its own material instance created via `new Material(ShaderUtil.GetToonShader())` for every unique char/theme combination. These are neither GPU-instanced nor batched.

- 300 cubes = 300 draw calls minimum (Unity SRP Batcher can reduce this to ~N unique shader variants, but only if `sharedMaterial` is truly shared — here each fallback `new Material()` call creates a **unique** material instance, defeating SRP batching).
- Toon shader on every tile adds overdraw cost.

**Severity**: Critical. This alone accounts for the largest draw call budget.

---

## Bottleneck #2 — Outline Double Draw Call per Entity

**File**: `Assets/Scripts/Visual/Outline.cs:25–32`

`Outline.ApplyToHierarchy()` is called from both `Tower.Init()` and `Enemy.Init()`. For every `MeshFilter` in the entity subtree it creates an extra child `GameObject("Outline")` with a separate `MeshRenderer`. Each active entity thus renders twice (toon pass + outline pass). With 20 enemies + 8 towers this adds 28+ extra draw calls that cannot be SRP-batched against the main pass since they use a different back-face culling material.

A single shared `_outlineMat` is correctly cached (line 65), but the per-entity extra MeshRenderer objects are not — every unique `MeshFilter` in a complex GLTF hierarchy gets its own outline GO.

**Severity**: High.

---

## Bottleneck #3 — Tower.AcquireTarget O(T×E) per frame

**File**: `Assets/Scripts/Entities/Tower.cs:485–521`

`AcquireTarget()` is called every `Update()` when `target == null || target.IsDead || OutOfRange(target)`. It iterates `WaveManager.Instance.ActiveEnemies` in full. With T towers and E enemies this is O(T×E) per frame. There is a fast-path (target cached until dead/out-of-range) but during dense waves — every tower firing, targets dying — nearly all towers reacquire every frame.

At 8 towers × 20 enemies = 160 iterations per frame, not catastrophic, but the `sqrMagnitude` allocation + list traversal inside a hot `Update()` adds up.

**Severity**: Medium.

---

## Bottleneck #4 — SlowEffectManager allocates List<Enemy?> every frame

**File**: `Assets/Scripts/Systems/SlowEffectManager.cs:34`

```csharp
var toRemove = new List<Enemy?>();
```

This runs inside `Update()`, allocating a new `List<Enemy?>` every single frame regardless of whether any slows are expiring. At 60 FPS this is 60 heap allocations/second, generating constant GC pressure in WebGL (which uses Mono/IL2CPP with a conservative GC).

**Severity**: Medium.

---

## Bottleneck #5 — WaveManager.OnBreakStateChanged fires every frame during skip window

**File**: `Assets/Scripts/Systems/WaveManager.cs:154`

```csharp
OnBreakStateChanged?.Invoke();
```

This fires on every `Update()` tick while `skipWindowTimer > 0f`, which is for 5 seconds between waves. Every subscriber (HudController, etc.) repaints UI every frame for no reason — UI state changes at most once per second for the countdown display.

**Severity**: Low–Medium.

---

## Bottleneck #6 — VfxPool PreWarm spawns 90 ParticleSystem instances at boot

**File**: `Assets/Scripts/Visual/VfxPool.cs:149–156`

Boot pre-warms: Impact×20, Death×20, Explosion×10, CoinBurst×20, HitFlash×20 = **90 ParticleSystem GameObjects** deactivated in scene. These inactive PS objects still consume scene overhead (Unity iterates all PS to check if playing). Additionally each `PlayAndAutoRelease()` call starts a Coroutine, so during a busy wave 10+ coroutines may be active per frame.

**Severity**: Low (pool pattern is correct; 90 is acceptable for desktop, borderline for WebGL).

---

## Bottleneck #7 — MaterialController.ApplyToon allocates new Material per renderer per entity Init

**File**: `Assets/Scripts/Visual/MaterialController.cs:47`

```csharp
var m = new Material(_toonBase);
```

Called inside a `foreach` over all renderers in the entity subtree. On pool reuse the enemy is re-Init'd — if `_meshChild` is reused the toon material is re-applied, creating new Material instances and leaking the old ones until GC. This defeats the pooling benefit for materials.

**Severity**: Medium.

---

## Bottleneck #8 — Minimap redraws grid path cells on every repaint

**File**: `Assets/Scripts/UI/MinimapController.cs:138–155`

`OnGenerateVisualContent()` iterates `grid.Width × grid.Height` cells (e.g. 300 iterations) on every 10 Hz repaint call, issuing one `BeginPath/Fill` per walkable cell. At 10 Hz this is ~3000 Painter2D draw commands per second just for static path geometry. The path tiles never change at runtime — they should be drawn once to a cached `RenderTexture` or `VectorImage`.

**Severity**: Medium (10 Hz throttle helps; still ~30ms of Painter2D work per second on large grids).

---

## Recommended Fixes (priority order)

1. **MapRenderer — GPU instancing + shared materials** (`MapRenderer.cs`)  
   Use `Graphics.DrawMeshInstanced` or mark tile materials `enableInstancing = true`. Ensure all cells with the same char/theme share **exactly one** material instance (already cached in `_matCache` by key — the fallback `new Material()` path must also use this cache, not create new instances).  
   Target: 300 draw calls → ~5 (one per char type per theme).

2. **Outline — defer to shader or use CommandBuffer** (`Outline.cs`)  
   Replace the per-MeshFilter child GO approach with a single-pass outline via URP `RenderObjects` feature (stencil outline) or a full-screen edge detection pass. Eliminates the N×outline draw calls entirely.  
   Alternatively: cap to one outline GO per entity root (not per MeshFilter), reducing N mesh-GLTF hierarchy cost.

3. **SlowEffectManager — reuse list** (`SlowEffectManager.cs:34`)  
   Promote `toRemove` to a class field (`private readonly List<Enemy?> _toRemove = new();`), call `_toRemove.Clear()` instead of reallocating.

4. **Tower.UpdateSlow — already throttled at 150ms, but Tower.AcquireTarget is not**  
   Cache `WaveManager.Instance.ActiveEnemies` ref once per tower Update (it's a list property access each loop iteration). Throttle `AcquireTarget()` to 100ms intervals if target is alive but at range boundary.

5. **WaveManager — throttle OnBreakStateChanged** (`WaveManager.cs:154`)  
   Fire only when `skipWindowTimer` crosses a whole-second boundary, or fire once per 0.5s via a `_lastBreakFireTime` guard.

6. **MaterialController — cache toon material instances per tint color** (`MaterialController.cs`)  
   Maintain a `Dictionary<Color, Material>` cache so pool-reused enemies with identical configs reuse the same material instance. Cuts GC pressure on wave spawns.

7. **Minimap — cache static grid to RenderTexture** (`MinimapController.cs:138–155`)  
   Bake path/portal/castle cells once on `OnLevelStart` into a `Texture2D` or `RenderTexture`. Only redraw dynamic dots (towers, enemies) at 10 Hz.

---

## Entity Count Estimates at W1 Peak

| Entity type | Count | Draw calls (current) |
|---|---|---|
| Map tiles | ~250–350 | 250–350 (no batching) |
| Enemies | ~15–25 | 30–50 (×2 outline) |
| Towers | ~6–10 | 12–20 (×2 outline) |
| Projectiles | ~10–20 | 10–20 |
| VFX (PS) | up to 90 inactive + active | ~10–20 active |
| **Total** | — | **~310–460** |

Unity's SRP Batcher reduces shader-variant batches but cannot batch entities with unique material instances. Fixing MapRenderer alone would cut draw calls by ~70%.

---

## Debug URL

`https://michaelchevallier.github.io/crowd-defense/v6/?debug=1`

DebugHud activates via `Application.absoluteURL.Contains("debug=1")` — confirms FPS/entity counters visible in top-left corner once the scene loads.
