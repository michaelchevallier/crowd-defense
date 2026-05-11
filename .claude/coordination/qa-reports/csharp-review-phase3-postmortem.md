# C# Code Review — Phase 3 Post-mortem

**Reviewer** : `csharp-code-reviewer` (Opus 4.7)
**Scope** : commits `bff9888` (AudioController seed) → `6fe8ff7` (safety net 2), including swarm mega-commit `df0989b` and HUD fixes `b4888bf` / `6fe8ff7`.
**Date** : 2026-05-11
**Mode** : READ-ONLY review (idiomatic C# 9+ + perf, complements `unity-code-reviewer`).

---

## Verdict : REQUEST_CHANGES

Phase 3 ships **functionally** with clean compile (`0 errors`) and respects `#nullable enable` + file-scoped conventions overall. **However**, the HudController carries two stacked "safety net" layers that mask a deeper design defect (MonoSingleton race window), and the working tree already contains an *uncommitted attempted unwind* — a strong signal that the workaround pattern is fragile and known-bad. Several hot-path allocations (Enemy.UpdateStealth, SetSlowTint) are imported from POC without being addressed during Phase 3 visual polish. AudioController and VfxPool are clean idiomatic ports — good craftsmanship there. Net : **must resolve before Phase 4 builds**, where init-order assumptions only get more brittle (scene chains, additive loads, hot-reload).

---

## Files reviewed

- `Assets/Scripts/UI/HudController.cs` (commits `b4888bf`, `6fe8ff7`, swarm)
- `Assets/Scripts/Systems/AudioController.cs` (commits `bff9888`, swarm extensions)
- `Assets/Scripts/Visual/VfxPool.cs` (swarm)
- `Assets/Scripts/Visual/JuiceFX.cs` (commit `f89ba3f`)
- `Assets/Scripts/Common/MonoSingleton.cs` (working tree diff — auto-create lazy getter)
- `Assets/Scripts/UI/SettingsRegistry.cs` (swarm)
- `Assets/Scripts/UI/SettingsPanelController.cs` (swarm)
- `Assets/Scripts/UI/L.cs` (swarm)
- `Assets/Scripts/Data/AudioClipRegistry.cs` (Phase 3 seed)
- `Assets/Scripts/Entities/Tower.cs` (Stage B hooks)
- `Assets/Scripts/Entities/Enemy.cs` (Stage B hooks)
- `Assets/Scripts/Entities/Castle.cs` (Stage B hooks)
- `Assets/Scripts/Systems/WaveManager.cs` (Stage B hooks)
- `Assets/Scripts/Systems/LevelRunner.cs` (Stage B hooks)
- `Assets/Scripts/Data/BalanceConfig.cs` (referenced for magic-number arbitrage)

---

## Issues found

### P0 — Blocking

- **[P0] Stacked safety-net workaround in HudController instead of fixing init order** — `Assets/Scripts/UI/HudController.cs:138-170`.
  Two layers were added in successive commits (`b4888bf` then `6fe8ff7`) :
  1. `_lateInitDone` retry-subscribe in `Update()`.
  2. `_lastWaitingState` polling `IsWaitingForPlayerStart` every frame to re-fire `OnBreakStateChanged` manually because subscription "missed the initial fire".
  This is a defensive workaround the charter explicitly flags ("Safety nets that retry forever → flag, replace with proper init order or DI"). The working tree shows an **uncommitted reversal** that deletes both nets + replaces with `MonoSingleton.Instance` lazy auto-create — confirming the team already knows the workaround is wrong.
  **Fix (canonical)** : choose one of
    - `[DefaultExecutionOrder(-100)]` on `WaveManager` so its `Awake`/`Start` always precedes `HudController`.
    - Make `WaveManager.Instance` lazy (the in-flight working-tree diff). Then `HudController.Start` can subscribe deterministically.
    - Move subscription out of `Start` into `OnEnable` keyed off a `WaveManager.OnInitialized` event fired from `WaveManager.Awake`.
  Avoid mixing both polling and event subscription paths for the same state — pick one source of truth.

- **[P0] `MonoSingleton<T>` lazy auto-create silently masks scene-setup bugs (working tree)** — `Assets/Scripts/Common/MonoSingleton.cs:13-27`.
  The uncommitted diff turns `Instance` into an auto-creator (FindFirstObjectByType → fallback `new GameObject(...).AddComponent<T>()`). For systems with `[SerializeField]` Inspector dependencies (e.g. `WaveManager.levelData`, `AudioController.registry`, `VfxPool.impactPrefab`), auto-create will produce a singleton with **null serialized fields** and crash later in `Start`/`Update` rather than at scene load. The current `Debug.LogWarning` is `UNITY_EDITOR || DEVELOPMENT_BUILD` only — production builds will fail silently.
  **Fix** : split the contract. Provide `TryInstance` (returns `T?`, never creates) for runtime callers, and reserve auto-create for an editor-only `EnsureInstance()` test helper. Or block auto-create entirely for singletons declaring `[SerializeField]` deps (use a `[RequiresSceneSetup]` marker).

- **[P0] `MonoSingleton<T>.Instance` getter is not thread-safe** — `Assets/Scripts/Common/MonoSingleton.cs:13-27`.
  Two-step check-then-assign on `_instance`. Unity callbacks are single-thread, but `Job System` / `IJobParallelFor` / async continuations that resume off-thread can racy-call this. Charter §3 (Async/await) flags this category.
  **Fix** : `[ThreadStatic]` is the wrong tool here. Use `Volatile.Read(ref _instance)` + double-checked-lock with `lock(_initLock)`, or — better — restrict the getter to the main thread and add `Debug.Assert(System.Threading.Thread.CurrentThread.ManagedThreadId == 1)` in editor.

### P1 — Should fix before Phase 4

- **[P1] Hot-path allocations in `Enemy.UpdateStealth` and `Enemy.SetSlowTint`** — `Assets/Scripts/Entities/Enemy.cs:269-279, 416-420`.
  `GetComponentsInChildren<Renderer>()` allocates `Renderer[]` **every frame per enemy**; `r.materials` allocates and **instantiates new material copies** each access. With swarm density (35-90 enemies/wave × Phase 3 swarm mul × 60 fps), this is a measurable GC churn + duplicate material leak (each call clones material → many copies linger until GC).
  **Fix** :
    - Cache `Renderer[]` in `Init()` via `_renderers = stealthRoot.GetComponentsInChildren<Renderer>()`.
    - Use `r.sharedMaterial` for read or — for per-instance tint — pre-instantiate the material **once** in `Init()` and re-assign color via `MaterialPropertyBlock` (zero alloc).
  See Unity docs : [Renderer.materials](https://docs.unity3d.com/ScriptReference/Renderer-materials.html) explicitly warns "creates a new copy each time".

- **[P1] `r.materials` in `Enemy.SetSlowTint` triggers material leak under slow-cycle** — `Assets/Scripts/Entities/Enemy.cs:416-420`.
  Each `slowed` toggle clones materials again. Same fix as above (`MaterialPropertyBlock`).

- **[P1] Boss `enemy_die_boss` clip is reused for castle game-over** — `Assets/Scripts/Entities/Castle.cs:53`.
  ```csharp
  AudioController.Instance?.Play("enemy_die_boss", 1f);
  ```
  This is a content-shortcut, not a C# idiom issue, but suggests missing canonical key (e.g., `castle_destroyed`). Couples Castle to enemy audio taxonomy. Add a dedicated key in `AudioClipRegistry`.

- **[P1] `L.Get(key, params object[] args)` swallows format exceptions silently** — `Assets/Scripts/UI/L.cs:120-122`.
  ```csharp
  try { return string.Format(raw, args); }
  catch { return raw; }
  ```
  Bare `catch {}` is a code smell (charter §5). At minimum log in `UNITY_EDITOR` (mismatched placeholder count is a content bug, not a runtime concern). Refactor :
  ```csharp
  try { return string.Format(raw, args); }
  catch (FormatException ex)
  {
  #if UNITY_EDITOR
      Debug.LogWarning($"[L] bad format key='{key}' raw='{raw}': {ex.Message}");
  #endif
      return raw;
  }
  ```

- **[P1] Magic numbers seeded into hot-path hooks instead of `BalanceConfig`** — multiple files.
  - `Tower.Fire`: `Play("tower_shoot", 0.55f)` + `Shake(0.05f, 100)` (`Tower.cs:500-501`).
  - `Tower.UpgradeTo`: `Flash(... 0.3f), 200)` (`Tower.cs:206`).
  - `Castle.TakeDamage`: `Play("castle_hit", 0.65f)` + `Shake(0.1f, 200)` + `Flash(... 0.4f), 150)` (`Castle.cs:45-47`).
  - `Enemy.TakeDamage` boss death : `Shake(0.3f, 400)` + `SlowMo(0.3f, 800)` + `Flash(white, 250)` (`Enemy.cs:377-379`).
  - `WaveManager.BeginWave`: `Play("wave_start", 0.85f)` (`WaveManager.cs:94`).
  - `LevelRunner.OnVictory`: `Flash(...0.4f, 500)` + `SlowMo(0.5f, 1200)` (`LevelRunner.cs:140-141`).
  These are tuning knobs that designers will want to iterate. They belong in `BalanceConfig` or a new `JuiceConfig` SO, not in code. Same critique applies to `AudioController.PoolSize = 8` (`AudioController.cs:13`) and `MinReplayInterval = 0.028f` (`.cs:14`) — these are arguable constants but design wants to tweak per-platform (mobile may want pool=4).

- **[P1] `VfxPool.AutoReleaseRoutine` allocates `WaitForSeconds` per spawn** — `Assets/Scripts/Visual/VfxPool.cs:155`.
  `yield return new WaitForSeconds(...)` allocates a heap object each call. At swarm density (every shot impact + every death), this is GC pressure that ParticleSystem.pause-aware pooling was meant to fix.
  **Fix** : the `WaitForSeconds` value is `main.startLifetime.constantMax + main.duration + 0.05f` — for each prefab this is a constant. Cache per-pool `WaitForSeconds` in `BuildPool` and reuse. Or use `ParticleSystem.IsAlive(true)` polling with `null` yields. Best : use `ObjectPool.actionOnRelease` + a single shared `WaitForSeconds` keyed by lifetime.

### P2 — Polish / non-blocking

- **[P2] `MonoSingleton<T>.Instance` is declared `T?` but lazy getter never returns null** — `Assets/Scripts/Common/MonoSingleton.cs:13`.
  After working-tree diff, the auto-create path guarantees non-null. Either narrow the type to `T` (and remove all `?.` call-sites — there are dozens) or keep `T?` and document that callers must still null-guard for the case where `T` is `abstract` / wrong scene state. The mixed contract leads to inconsistent call-sites (`Audio.Instance?.Play` vs `JuiceFX.Instance?.Shake` everywhere).

- **[P2] `HudController.Start` registers 11 `Q<>` lookups + 4 button callbacks** — `Assets/Scripts/UI/HudController.cs:43-104`.
  62 lines, cyclomatic ~7. The charter flags methods > 40 lines. Extract `BindUiRefs()`, `WireCallbacks()`, `SubscribeSystems()` helpers — each becomes 1-line readable.

- **[P2] `Tower.ApplyL3Branch` switch has 4 case blocks with twin Dps/Utility patterns** — `Assets/Scripts/Entities/Tower.cs:237-313`.
  77 lines, cyclomatic ~10 (1 branch + 4 cases × 2 sub-branches). Refactor : extract `L3Stats` struct with the 12 fields and lookup table `Dictionary<(string id, TowerBranch), L3Stats>` or — more idiomatically — TowerType SO holding `L3DpsStats` / `L3UtilityStats` sub-objects. Code becomes data, which D1-03 spec implies.

- **[P2] `AudioController.SetMasterVolume` mixes two paths via short-circuit** — `Assets/Scripts/Systems/AudioController.cs:152-156`.
  ```csharp
  if (mixer != null && mixer.SetFloat("MasterVol", LinearToDb(zeroToOne))) return;
  AudioListener.volume = Mathf.Clamp01(zeroToOne);
  ```
  `SetFloat` returns `false` only if the exposed parameter is missing. Fallback writes `AudioListener.volume` (0-1) but the *mixer* path writes db — semantically different units. Better : two methods `SetMasterVolumeViaMixer` / `SetMasterVolumeFallback` with clear contracts. Right now `master=0.5` produces different audible result depending on whether mixer parameter exists.

- **[P2] `LinearToDb(0)` returns -80 but clamps to -80 — silent rounding** — `Assets/Scripts/Systems/AudioController.cs:179-184`.
  `Mathf.Log10(0.0001) * 20 = -80`. Hard floor at `MinDb = -80f` means `0` and `0.0001` both produce -80 db. Acceptable but consider returning `-Mathf.Infinity` or `mixer.ClearFloat()` for true mute (Unity convention).

- **[P2] `SettingsRegistry.Save` writes **all** prefs on every change** — `Assets/Scripts/UI/SettingsRegistry.cs:109-123`.
  11 `PlayerPrefs.SetX` + `PlayerPrefs.Save()` triggered each setter. `Save()` flushes to disk (sync I/O on mobile). For a slider drag (60 events/s), this thrashes. **Fix** : debounce `Save()` via coroutine or write `PlayerPrefs.Save` only on `OnDisable` / app pause.

- **[P2] `SettingsPanelController.SyncFromRegistry` uses `_suppressEvents` flag pattern** — `Assets/Scripts/UI/SettingsPanelController.cs:178-217`.
  The flag pattern is fine but a `SetValueWithoutNotify` is the Unity-idiomatic alternative for sliders/toggles (already used for `_qualityDropdown` and `_langDropdown` at lines 197, 210). Inconsistent : sliders/toggles use `value = ...` + suppress, dropdowns use `SetValueWithoutNotify`. Pick one — prefer `SetValueWithoutNotify` everywhere, drop `_suppressEvents`.

- **[P2] `JuiceFX.EnsureFlashOverlay` builds the overlay via inline initializer with mixed semantics** — `Assets/Scripts/Visual/JuiceFX.cs:119-130`.
  The `style` initializer applies multiple positional values but `pickingMode` and `name` are root-level. Reads fine but the style helper might surprise future readers. Non-blocking.

- **[P2] `JuiceFX.LateUpdate` writes `_cam.transform.position` every frame even when not shaking** — `Assets/Scripts/Visual/JuiceFX.cs:71-95`.
  ```csharp
  if (_cam.transform.position != _baseCamPos)
      _cam.transform.position = _baseCamPos;
  ```
  Compares Vector3 (epsilon-tolerant via Unity overload, but creates SetPosition dirty flag). Add a single `_shakeActive` flag and early-return when both ended **and** position was already restored. Negligible perf but cleaner.

- **[P2] `HudController.OnHPChanged` allocates `Color` objects each invocation** — `Assets/Scripts/UI/HudController.cs:195-199`.
  3 Color structs allocated on stack — fine because struct, but `new Color(...)` literals are repeated each HP change. Hoist to `private static readonly Color HpGreen/Orange/Red`.

- **[P2] `Enemy.Init` calls `Resources.Load<AssetRegistry>("AssetRegistry")` every spawn** — `Assets/Scripts/Entities/Enemy.cs:151`.
  `Resources.Load` caches internally but still string-hashes. With pool reuse this fires per enemy creation. Cache in `Awake` or `AssetRegistry.Get()` static.

- **[P2] `Castle.TakeDamage` reaches into `AudioController.Instance?.` and `JuiceFX.Instance?.` 5 times** — `Assets/Scripts/Entities/Castle.cs:45-55`.
  Each `Instance?.` access walks the lazy getter (post working-tree diff = `FindFirstObjectByType` if null). 5 separate null-guards. Stash into locals at method top.

### Async / Coroutines — no issues found

- `AudioController` uses `IEnumerator` coroutines with `Time.unscaledDeltaTime` for fade routines — correct (immune to `Time.timeScale=0` on pause).
- No `async void` or `.Result` / `.Wait()` in Phase 3 code.
- No deadlock risks observed.

### Nullable annotations — coherent

- All Phase 3 files declare `#nullable enable` at top.
- `T?` vs `T` discipline is mostly followed. One inconsistency : `MonoSingleton<T>.Instance` is typed `T?` but post-diff never returns null — see [P2].
- `AudioClipRegistry.Get(string key)` returns `AudioClip?` — correct.
- `L.Get(string key, string table)` returns `string` (non-null) with a final `return key` fallback — correct, never returns null.
- No bare `T x = null;` found.
- No null-forgiving `!` without justification (the `_cache!.TryGetValue` in `AudioClipRegistry` is fine — `BuildCache` was just called).

### Idiomatic patterns observed (positive reinforcement)

- Pool pattern in `VfxPool` uses `UnityEngine.Pool.ObjectPool<T>` (canonical Unity 6 API, replaces hand-rolled stacks).
- `JuiceFX` defends against `Time.timeScale = 0` correctly by using `Time.unscaledDeltaTime` throughout — important since slow-mo + flash overlap.
- `AudioController.LinearToDb` exists as a private static helper — good extraction.
- File-scoped namespaces : Phase 3 files use traditional `namespace { }` block, but project-wide consistent.
- `[SerializeField] private T? field` pattern across new code — best practice for Inspector + nullable safety.
- `SettingsRegistry.MasterVolume` setter uses `Mathf.Approximately` for change detection — avoids slider-jitter notification storms.
- `SettingsPanelController` correctly subscribes in `OnEnable` / unsubscribes in `OnDisable` (avoids leaks on scene reload) — vs `HudController` subscribing in `Start` / unsubscribing in `OnDestroy` which is the "permanent listener" pattern. Both are acceptable but the `OnEnable/OnDisable` is more idiomatic for UI controllers (allows toggle without scene reload). `HudController` should follow suit.
- `WaveManager.IReadOnlyList<Enemy> ActiveEnemies` exposes immutable view — good encapsulation.

---

## Suggestions (non-blocking, design-level)

- **Replace stacked safety-nets with `ScriptableObject`-based service locator + `MonoBehaviour.Awake` ordering manifest.** A single `ServicesBoot` MonoBehaviour with `[DefaultExecutionOrder(-1000)]` instantiates / wires all singletons deterministically. Pattern : write a `Services` static class exposing `Audio`, `Vfx`, `Juice`, `Settings`, `Waves`, `Economy` etc., populated from `ServicesBoot.Awake`. Eliminates `Instance?.X` everywhere, eliminates race conditions, and centralizes scene-setup contract.
- **Replace `_levelDmgScale` + `L3DmgMul` + `_buffMul` 5-multiplication chain in `Tower.Fire`** — `Tower.cs:507`. Refactor to a `DamagePipeline` struct that composes modifiers in defined order. Easier to test, easier to debug "why 187 damage instead of 200".
- **Adopt `record struct` for value-types** : `BalanceConfig.WorldPressure`, `AudioClipRegistry.Entry`. Modern C# 9 (which charter targets) supports `record struct` with auto value-equality + `with` expressions. Currently both are `[Serializable] struct`.
- **`Tower.cs` has 13 mutable public fields prefixed `_` for synergy state** (`_buffMul` to `_synergyActive`) — `Tower.cs:31-62`. Public fields with underscore prefix is a strange hybrid. Either : keep public and remove underscore (Unity convention for serialized public), or make private + provide a `SynergyOutputs` struct setter. The current shape allows external mutators to write any field with no invariant enforcement.

---

## Summary table

| Severity | Count | Theme |
|---|---|---|
| P0 | 3 | Safety-net workaround stack + MonoSingleton design |
| P1 | 5 | Hot-path allocations + magic numbers in hooks |
| P2 | 10 | Code smells, perf polish, idiom inconsistencies |

## Recommended action

1. **Land the in-flight working-tree diff** (remove safety nets, keep `MonoSingleton` lazy auto-create) — but pair it with the [P0] auto-create guards described above (block auto-create for SerializeField-dependent singletons).
2. **Apply [DefaultExecutionOrder]** to `WaveManager`, `AudioController`, `VfxPool`, `JuiceFX`, `SettingsRegistry`, `LevelRunner` to make Awake order deterministic — removes the race window entirely.
3. **Refactor `HudController.Start`** to subscribe via `MonoBehaviour.OnEnable` keyed off `WaveManager.OnInitialized` (new event fired at end of `WaveManager.Awake`).
4. Address [P1] Enemy hot-path allocations before Phase 4 mobile build (target 30 fps mobile = no room for per-frame GC).
5. Open follow-up ticket "Move juice/audio magic numbers to JuiceConfig SO" — non-blocking but should land Phase 4 before tuning sprint.

End of review.
