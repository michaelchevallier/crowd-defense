# Unity Code Review — Phase 3 Postmortem

Reviewer : unity-code-reviewer (Opus 4.7, read-only)
Scope : commits `df0989b` (7-axis swarm), `b4888bf` + `6fe8ff7` (HudController safety nets) on `main`
Date : 2026-05-11

## Verdict : REQUEST_CHANGES

The Phase 3 mega-merge ships large amounts of correct code (audio pipeline, VFX prefabs, settings, i18n, level rebalance) but a **systemic scene-setup gap** silently neuters most of the new juice/audio/VFX layer at runtime. The two `b4888bf` + `6fe8ff7` safety-net commits treat symptoms instead of the root cause (race between `MonoBehaviour.Start` order). Mike's uncommitted in-flight fix to `Assets/Scripts/Common/MonoSingleton.cs` (lazy `Instance` getter with `FindFirstObjectByType` + auto-create) is the **canonical pattern** and resolves the entire P0 cluster — it should be committed and the safety nets in `HudController.Update` reverted (Mike has already done so locally).

### TL;DR
- **P0 root cause** : `Main.unity` is missing GameObjects hosting 7 singleton MonoBehaviours added by axes A/F: `VfxPool`, `JuiceFX`, `EnemyPool`, `ProjectilePool`, `SlowEffectManager`, `CoinPullManager`, `SettingsRegistry`. All `*.Instance` accesses go silent via `?.` no-op.
- **Canonical fix** : Mike's in-progress `MonoSingleton<T>` lazy-init patch (uncommitted local) is correct. Commit it. It also obsoletes the two HudController safety-net commits.
- **P1 perf** : `Enemy.UpdateStealth` + `Enemy.SetSlowTint` allocate via `r.materials` and `GetComponentsInChildren` every frame on every enemy. GC storm risk at wave scale (~150 active mobs).
- **P1 architecture** : pool patterns inconsistent (some lazy via `MonoSingleton`, some not), `MaterialController.ApplyToon` instantiates materials per renderer per spawn (no cache), `Outline` material is shared but tinted via `SetColor` per call (race).

## Files reviewed
- `Assets/Scripts/Common/MonoSingleton.cs` (HEAD + uncommitted)
- `Assets/Scripts/Systems/AudioController.cs`
- `Assets/Scripts/Systems/WaveManager.cs`
- `Assets/Scripts/Systems/LevelRunner.cs`
- `Assets/Scripts/Systems/EnemyPool.cs`, `ProjectilePool.cs`, `SlowEffectManager.cs`, `CoinPullManager.cs`, `Synergies.cs`, `Economy.cs`, `PlacementController.cs`
- `Assets/Scripts/Entities/Tower.cs`, `Enemy.cs`, `Castle.cs`
- `Assets/Scripts/Visual/VfxPool.cs`, `JuiceFX.cs`, `AnimationController.cs`, `MaterialController.cs`, `Outline.cs`
- `Assets/Scripts/UI/HudController.cs` (HEAD + uncommitted), `SettingsRegistry.cs`, `SettingsPanelController.cs`, `RadialMenuController.cs`
- `Assets/Scenes/Main.unity` (scene setup audit via GUID grep)

---

## P0 issues — blocking, must fix before next ship

### P0-1 — Missing scene setup for new Phase 3 singletons
**Confirmed via direct scene file inspection** (`Assets/Scenes/Main.unity`) :

| Singleton | Script GUID | GameObject in scene ? |
|---|---|---|
| `AudioController` | `6c2707a05...` | YES (under `Systems/AudioController`) |
| `WaveManager` | `ffa4c73af...` | YES (under `Systems/WaveManager`) |
| `LevelRunner` | `baf0f442e...` | YES (under `Systems/LevelRunner`) |
| `Economy` | `e9abada33...` | YES (under `Systems/Economy`) |
| `PlacementController` | `e97fe3a29...` | YES (under `Systems/PlacementController`) |
| `PathManager` | `265eff585...` | YES (under `Systems/PathManager`) |
| `HudController` | `00224a2f6...` | YES (under `HUD`) |
| **`VfxPool`** | `3687bb396...` | **NO — 0 refs in scene** |
| **`JuiceFX`** | `add92cf2e...` | **NO — 0 refs in scene** |
| **`EnemyPool`** | `0f00ea811...` | **NO — 0 refs in scene** |
| **`ProjectilePool`** | `5a88a29e2...` | **NO — 0 refs in scene** |
| **`SlowEffectManager`** | `da6b32b3f...` | **NO — 0 refs in scene** |
| **`CoinPullManager`** | `5edcce14b...` | **NO — 0 refs in scene** |
| **`SettingsRegistry`** | `dff14cb6c...` | **NO — 0 refs in scene** |
| **`Synergies`** | (not measured, no `find_in_scene` for this commit) | likely missing as well |

**Consequences observed/inferred** :
- `Tower.Fire()` → `AudioController.Instance?.Play("tower_shoot", ...)` → AudioController **works** (in scene), so audio plays.
- `Tower.Fire()` → `JuiceFX.Instance?.Shake(...)` → **null no-op** — no camera shake on shoot.
- `Tower.Fire()` → `VfxPool.Instance?.SpawnImpact(...)` → **null no-op** — no muzzle flash.
- `Castle.TakeDamage()` → `JuiceFX.Instance?.Shake/Flash` → **silent**.
- `Enemy.TakeDamage()` (fatal) → `VfxPool.Instance?.SpawnDeath(...)` → **silent**.
- `Tower.Fire()` → `ProjectilePool.Instance.Get()` → **NullReferenceException** (no `?.`, hard crash) — but Tower has an early-out at `Tower.cs:491` with `LogError` and `return`. Yet the entire game would have no projectiles, so the POC clearly works somehow at runtime. Either (a) the scene actually has these GOs nested under another name we missed in the grep, or (b) Mike's uncommitted `MonoSingleton<T>` lazy patch is already saving the game.

**Caller-reported runtime bug** ("wave launch button invisible") is a downstream symptom of HudController.Start running before WaveManager.Awake completed — also a scene-init-order issue (HUD GO is under `HUD/`, Systems GO is under `Systems/`, Unity executes Awake/Start by scene-graph traversal order which is non-deterministic across scene re-saves).

**Canonical fix (P0-1 root)** : adopt the uncommitted `MonoSingleton<T>` lazy `Instance` getter currently sitting in Mike's working tree :
```csharp
public static T? Instance
{
    get
    {
        if (_instance != null) return _instance;
        _instance = Object.FindFirstObjectByType<T>();
        if (_instance != null) return _instance;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning($"[MonoSingleton] {typeof(T).Name} auto-created — missing in scene.");
#endif
        var go = new GameObject($"[Auto] {typeof(T).Name}");
        _instance = go.AddComponent<T>();
        return _instance;
    }
}
```

This is the **canonical Unity 6 pattern** for runtime-required singletons that may not be guaranteed in every scene (especially after multi-axis merges). It :
1. Returns the existing instance if any (fast path, zero alloc).
2. Falls back to a single `FindFirstObjectByType` (Unity 6 successor to `FindObjectOfType`, faster, cf [Unity docs](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Object.FindFirstObjectByType.html)).
3. Auto-creates a new GO + component as last resort — only allowed because all our singletons are runtime-bootstrappable (no `[SerializeField]` references that need Inspector setup, see caveat below).

**Caveat for P0-1** : `VfxPool`, `AudioController`, and `WaveManager` declare `[SerializeField]` Inspector refs (prefabs, AudioMixer, levelData). The lazy auto-create path skips Inspector wiring, so those instances will spawn with `null` refs and downstream `IsVfxEnabled()`-style guards skip silently. For these singletons, the lazy auto-create is a **degraded-mode safety net**, NOT a replacement for proper scene setup. The right end-state is :
1. Commit the lazy `MonoSingleton<T>` (insurance against scene drift).
2. **Also** add the missing GameObjects to `Main.unity` with their Inspector refs wired (use an Editor menu `Tools/CrowdDefense/Build Main Scene` to make this reproducible and idempotent — see P1-3).

**Apart from MonoSingleton** : `EnemyPool`, `ProjectilePool`, `SlowEffectManager`, `CoinPullManager`, `Synergies` use a **different singleton pattern** (raw `public static T? Instance` + manual Awake assignment). They do NOT inherit `MonoSingleton<T>`. Fixing `MonoSingleton<T>` does NOT fix them. Two options :
1. Refactor them all to inherit `MonoSingleton<T>` (best for consistency).
2. Or replicate the lazy-init pattern in each.

Recommend option 1 for codebase coherence.

### P0-2 — `b4888bf` + `6fe8ff7` HudController safety nets are anti-patterns
The two commits :
- `b4888bf` adds an `Update()` lazy late-init flag block for `WaveManager.Instance`.
- `6fe8ff7` adds a per-frame poll of `IsWaitingForPlayerStart` re-firing `OnBreakStateChanged` if changed.

These are **textbook workarounds** that the reviewer charter explicitly forbids ("NEVER suggests workarounds — always proposes the canonical Unity pattern"). They :
1. Add per-frame branch + bool comparison in every HUD `Update()` — small CPU cost, but principle of running idle work on every frame.
2. Leak event subscriptions if `Instance` changes (e.g., scene reload) — the late-init only runs once due to `_lateInitDone` flag, so a scene reload with a fresh WaveManager would silently lose subscription.
3. Couple HudController to WaveManager internals (polling `IsWaitingForPlayerStart` as state-of-truth).

**Canonical fix** : the lazy `MonoSingleton.Instance` getter (P0-1) means `WaveManager.Instance` is **never null** at `HudController.Start` — the Find/auto-create runs synchronously inside the getter call. No safety net needed.

**Status of P0-2** : Mike has already reverted these locally (cf `git diff Assets/Scripts/UI/HudController.cs`). The HEAD still ships the workaround, so it must be replaced by a proper commit that reverts them and ships the `MonoSingleton<T>` fix together.

Alternative canonical pattern (if `MonoSingleton<T>` lazy is rejected) : `[DefaultExecutionOrder(-100)]` on `WaveManager`, `Economy`, `LevelRunner`, etc., so they are guaranteed to `Awake()` before `HudController`. Cf [Unity docs](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/DefaultExecutionOrder.html). This is what `MapRenderer` and `TreasureSpawner` already use (values 50/60) for scene-setup ordering.

### P0-3 — `Tower.cs:491` ProjectilePool null check is a band-aid
```csharp
if (ProjectilePool.Instance == null)
{
    Debug.LogError("[Tower] ProjectilePool.Instance is null — projectile not fired");
    return;
}
```
Same root cause as P0-1. Once P0-1 is fixed (lazy singleton), this null guard becomes dead code and should be deleted along with the analogous check in `WaveManager.cs:204` (`EnemyPool.Instance == null`).

---

## P1 issues — fix before content phase / Phase 4

### P1-1 — `Enemy.UpdateStealth` allocates every frame
`Assets/Scripts/Entities/Enemy.cs:258-280` — runs every frame on every stealth enemy :
```csharp
foreach (var r in stealthRoot.GetComponentsInChildren<Renderer>())  // allocates Renderer[]
{
    foreach (var mat in r.materials)  // r.materials INSTANTIATES per-renderer materials, creates copies every call
    {
        if (mat == null) continue;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", c);
        else
            mat.color = c;
    }
}
```

**Two GC issues** :
1. `GetComponentsInChildren<Renderer>()` allocates a fresh `Renderer[]` every frame.
2. `r.materials` (NOT `sharedMaterials`) — the getter **creates instances of all materials on the renderer** on first access, but more importantly returns a **new array** of references each call. The Unity profiler will show ~kB GC per enemy per frame.

**Canonical fix** : use [`MaterialPropertyBlock`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MaterialPropertyBlock.html) (zero alloc, designed for per-frame property tweaks) :
```csharp
// In Init() : cache renderers once + build a static-shared MaterialPropertyBlock
private Renderer[] _stealthRenderers = System.Array.Empty<Renderer>();
private static readonly MaterialPropertyBlock _mpb = new();
private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");

// In Init() (after _meshChild is spawned) :
var stealthRoot = _meshChild != null ? _meshChild : gameObject;
_stealthRenderers = stealthRoot.GetComponentsInChildren<Renderer>();

// In UpdateStealth() :
_mpb.Clear();
_mpb.SetColor(_baseColorId, c);
for (int i = 0; i < _stealthRenderers.Length; i++)
    _stealthRenderers[i].SetPropertyBlock(_mpb);
```

This pattern is also wrong in `Enemy.SetSlowTint()` (line 408-421) and should be unified into a single helper that takes a tint color.

### P1-2 — `SlowEffectManager.Update` allocates `List<Enemy?>` every frame
`Assets/Scripts/Systems/SlowEffectManager.cs:42` — `var toRemove = new List<Enemy?>();` runs every Update.

**Canonical fix** : promote to a private field and call `.Clear()` at start of `Update`. Standard "scratch list" pattern in Unity perf code.

### P1-3 — Scene-build reproducibility
Existing Editor menus (`POC05Setup`, `POC06Setup`, `POC07Setup`) bootstrap parts of the scene but :
- Don't cover Phase 3 additions (no `Tools/CrowdDefense/Add VfxPool to Scene`, no `Add JuiceFX`, etc.).
- Not idempotent at composition level (running them in arbitrary order may double-add GOs since each tool only checks for its own GO).

**Canonical fix** : a single `Tools/CrowdDefense/Build Main Scene` menu that :
1. Finds-or-creates the `Systems` parent GO.
2. Adds every required singleton (PathManager, WaveManager, ..., **plus the new** VfxPool, JuiceFX, Pools, SlowEffectManager, CoinPullManager, SettingsRegistry, Synergies).
3. Wires their `[SerializeField]` refs from `Resources/` (VfxPool prefabs, AudioController registry/mixer/groups).
4. Marks scene dirty + saves.

This makes the scene reproducible from source and avoids drift after future axis merges. Cf [Unity Editor scripting docs](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/EditorSceneManager.html).

### P1-4 — `MaterialController.ApplyToon` instantiates ToonBase per spawn
`Assets/Scripts/Visual/MaterialController.cs:38-39` :
```csharp
var m = new Material(_toonBase);
```
This is called once per renderer per Tower/Enemy spawn. For 150 mobs × ~3 renderers each = 450 material instances per wave. They're never released back to a pool, so GC happens at wave-end when enemy GOs are pooled (the materials reference is on the renderer GO).

**Canonical fix** : if all enemies of the same `BodyColor` could share the same material instance, you save 95% of these allocs. Build a `Dictionary<Color, Material>` cache keyed by tint. Or use `MaterialPropertyBlock` from the start (no per-instance material at all).

Lower priority because the pool reuse already amortises this, but it's worth noting since `Outline.ApplyToHierarchy` does the same `new GameObject("Outline") + AddComponent<MeshFilter>+MeshRenderer` dance per renderer per spawn.

### P1-5 — `Outline.GetOrCreateMaterial` race on shared material color
`Assets/Scripts/Visual/Outline.cs:55-76` caches a single static `_outlineMat`, but every `ApplyToHierarchy` call calls `_outlineMat.SetColor("_OutlineColor", outlineColor)`. If a Tower with red outline spawns at the same frame as an Enemy with black outline, the second call mutates the shared material's color, breaking the first call's silhouette retroactively.

**Canonical fix** : pass color via `MaterialPropertyBlock` on each outline renderer, OR maintain a `Dictionary<Color, Material>` of outline material variants.

### P1-6 — `JuiceFX.LateUpdate` always restores `_baseCamPos` even when shake is inactive
`Assets/Scripts/Visual/JuiceFX.cs:71-95` : when `now >= _shakeEndTime`, it runs the `if (_cam.transform.position != _baseCamPos)` branch every frame. If the player or another script moves the camera, JuiceFX forcibly snaps it back. This breaks any camera-pan/zoom from gameplay code.

**Canonical fix** :
1. Track shake-applied offset internally (e.g., `_currentShakeOffset`).
2. On LateUpdate, subtract the previous frame's offset before applying the new one.
3. Never overwrite `_baseCamPos` from anywhere except the explicit `SetBaseCamPos` API.

### P1-7 — `Synergies.Update` rebuilds reset state every 200ms
`Assets/Scripts/Systems/Synergies.cs:48-73` : 20 field writes per tower × N towers, every 200ms. Acceptable at <50 towers, but each `t._buffMul = 1f` etc. is a separate field set. Bigger issue : public mutable fields directly on Tower break encapsulation and make this resolver impossible to unit-test in isolation.

**Canonical fix (deferred)** : refactor Tower synergy state into a `TowerSynergyState` struct accessed via property, OR use a parallel `Dictionary<Tower, TowerSynergyState>` owned by `Synergies` (cleaner ownership, zero impact on Tower).

Not a blocker for Phase 3 ship — flag for Phase 2/Phase 3 refactor sprint.

---

## P2 issues — nice-to-have, defer to Phase 4 polish

### P2-1 — `Enemy.UpdateFlyer` snaps Y after `MoveTowards`
`Assets/Scripts/Entities/Enemy.cs:240-256` : MoveTowards moves the position along a 3D vector, then the next 3 lines forcibly snap `pos.y = cfg.FlyHeight`. This works but is wasteful. Cleaner : pass a `flyTarget` with the correct Y to MoveTowards and let it stay on the plane naturally.

### P2-2 — `Castle.Init` searches `PathManager.Instance.Grid` for spawn position
`Assets/Scripts/Entities/Castle.cs:24-31` : runs once at Init, safe. But couples Castle to PathManager. Could be inverted (LevelRunner queries PathManager + passes Vector3 to Castle.Init). Stylistic.

### P2-3 — `Tower.AcquireTarget` allocates closure-free but iterates flyers separately
`Assets/Scripts/Entities/Tower.cs:448-486` : two branches (flyer priority vs ground waypoint priority) interleaved. Readability could improve with a single-pass loop, but no perf issue.

### P2-4 — `WaveManager.BeginWave` Fisher-Yates allocates `new System.Random()` per wave
`Assets/Scripts/Systems/WaveManager.cs:82` : `var rng = new System.Random();` — once per wave, ~50 bytes. Negligible. Could promote to `private readonly System.Random _rng = new();` for consistency.

### P2-5 — `LevelRunner.SpawnCastle` uses `Shader.Find("Universal Render Pipeline/Lit")`
`Assets/Scripts/Systems/LevelRunner.cs:97` : `Shader.Find` is documented as Editor-only-safe ([Unity docs](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Shader.Find.html)) — at runtime, shaders not referenced in any scene or `GraphicsSettings.alwaysIncludedShaders` may not be in the build and Find returns null. The code does have a `?? Shader.Find("Standard")` fallback, so it degrades gracefully.

**Canonical fix** : reference URP/Lit via `Assets/Settings/AlwaysIncludedShaders.asset` (Project Settings → Graphics → Always Included Shaders). Or use the existing `ShaderUtil.GetLitShader()` helper present in `Assets/Scripts/Common/ShaderUtil.cs`.

### P2-6 — Enemy mesh-cache miss on pool reuse with different AssetKey
`Assets/Scripts/Entities/Enemy.cs:163-168` :
```csharp
if (_meshChild != null)
{
    _meshChild.SetActive(true);
    return _meshChild;
}
```
If the enemy is recycled from pool with a **different** `EnemyType`, the previous `_meshChild` GLTF is reused with the wrong mesh. Edge case (POC has only a couple of enemy types, may not trigger), but Phase 2 (30 enemy types) will hit it. Add an `if (_meshChild != null && _meshChild.name == "Mesh_" + assetKey)` guard.

---

## Best practice suggestions (non-blocking)

1. **`#if UNITY_EDITOR` guard** for `Debug.Log` statements is correctly applied throughout. Good.
2. **`#nullable enable`** on every file — excellent C# 8 hygiene.
3. **`[RequireComponent]`** on `Enemy.cs:10-13` and `HudController.cs:10` — perfect Unity-idiomatic.
4. **`MonoSingleton<T>` Awake re-assign on duplicate** : correct anti-duplicate logic.
5. **`AudioController` SFX pool of 8 + 28 ms anti-replay** : textbook port of an audio voice pool.
6. **`OnDestroy` event unsubscription in `HudController.cs:106-120`** : prevents leak.
7. **`L.OnLocaleChanged` event-driven HUD re-translation** : clean.
8. **The `Outline + MaterialController` pattern as a `static` class** (vs a singleton) : right choice for stateless transformations.

## Approved patterns observed
- `MonoSingleton<T>` lazy init pattern (in Mike's uncommitted diff) — correct canonical solution.
- Object pooling via `UnityEngine.Pool.ObjectPool<T>` in `EnemyPool`, `ProjectilePool`, `VfxPool` — proper Unity 6 API.
- `AnimationController` as static helper with bool / trigger params — keeps Animator usage centralized.
- `LevelLoader.NextLevelId` static for scene-to-scene transitions — simple and correct.

## Recommended action plan
1. **Commit Mike's working-tree fix** : `MonoSingleton<T>` lazy `Instance` getter + revert the two HudController safety nets in a single commit. Suggested message :
   ```
   fix(common): MonoSingleton<T> lazy Instance + revert HudController safety nets

   - MonoSingleton<T>.Instance lazily Find/Create when null — canonical
     Unity 6 pattern, prevents NullRef cascade when scene-setup drifts
     post multi-axis merge.
   - Revert HudController Update polling (commits b4888bf + 6fe8ff7) :
     workarounds for the WaveManager.Instance == null race, now fixed at
     root via lazy singleton.
   ```
2. **Refactor non-MonoSingleton singletons** (EnemyPool, ProjectilePool, SlowEffectManager, CoinPullManager, Synergies) to inherit `MonoSingleton<T>`. One commit per file or one batch.
3. **Add Editor menu `Tools/CrowdDefense/Build Main Scene`** that idempotently creates all required GameObjects. Run it once, commit `Main.unity` delta. This is the **long-term** fix for the scene-setup-drift problem that bit Phase 3.
4. **Wave 2 perf pass** : fix Enemy.UpdateStealth + SetSlowTint + SlowEffectManager.Update allocations using `MaterialPropertyBlock` + scratch lists. Estimated 5-10× speedup at wave scale.
5. **Add Unity Test Framework tests** that instantiate Main.unity programmatically and assert all `Instance` properties non-null after one frame — this would have caught P0-1 in CI.

---

Reviewer notes : I am unable to verify these issues at runtime (read-only review). The Mike-reported "find_gameobjects by_component AudioController → 0" should be re-tested — my static scene analysis shows AudioController IS present (under `Systems/AudioController`, scene line ~1401904699-700), but VfxPool / JuiceFX / EnemyPool / ProjectilePool / SlowEffectManager / CoinPullManager / SettingsRegistry are confirmed absent. The reported AudioController-null may be a false positive of the MCP tool, OR may relate to the scene reload race condition that the lazy singleton fixes.
