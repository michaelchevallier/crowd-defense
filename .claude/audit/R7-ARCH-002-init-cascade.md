# R7-ARCH-002 — Init Cascade Audit: Awake/Start Order + NRE Risk

**Date:** 2026-05-13  
**Scope:** All `Assets/Scripts/**/*.cs` — MonoBehaviours, MonoSingleton subclasses  
**Type:** Read-only analysis — zero code changes

---

## 1. Singleton Table

| Class | Namespace | DEO | Init Phase | Instance Pattern | Key Required-By (downstream) |
|---|---|---|---|---|---|
| `PathManager` | Systems | -100 | **Awake** (OnAwakeSingleton → Build) | MonoSingleton | LevelRunner.Start, MapRenderer.Start, WaveManager.Start, EnemyPool, EnemyPathingSystem |
| `Economy` | Systems | -100 | **Start** | MonoSingleton | LevelRunner.Start (event sub), WaveManager (ResetWaveDamageFlag, ProcessInterestBank), PlacementController (TrySpend), Tower.Sell |
| `ComboSystem` | Systems | -100 | MonoSingleton base | MonoSingleton | Economy.AddGoldFromKill |
| `AudioController` | Systems | -50 | **Awake** (OnAwakeSingleton: pool + music source alloc) | MonoSingleton | LevelRunner (Play), WaveManager (Play), PlacementController (Play3D), Synergies |
| `EventManager` | Systems | -50 | Awake (base only) | MonoSingleton | MusicManager.Start (Subscribe), DynamicEventManager |
| `EnemyPool` | Systems | -50 | Awake (base only) | MonoSingleton | WaveManager.BeginWave (PrewarmType, SpawnFromType), EnemyPathingSystem.Tick |
| `HeroProjectilePool` | Systems | -50 | Awake (base only) | MonoSingleton | Hero.OnDestroy (Return) |
| `ProjectilePool` | Systems | -50 | Awake (base only) | MonoSingleton | Tower.Combat |
| `LootSpawner` | Systems | -50 | Awake (base only) | MonoSingleton | Enemy.Lifecycle |
| `MetaUpgradeSystem` | Systems | -50 | **Awake** (OnAwakeSingleton → ComputeBonuses), **Start** (event sub to LevelEvents) | MonoSingleton | Economy.Start (startCoinsBonus), LevelRunner.SpawnHero (ApplyMetaBonuses) |
| `LevelRunner` | Systems | -50 | **Awake** (OnAwakeSingleton: level resolution), **Start** (SpawnCastle, SpawnHero, wire events) | MonoSingleton | WaveManager.Start (LevelRunner.CurrentLevel fallback), HudController.Start, MapRenderer.Start |
| `WaveManager` | Systems | -50 | **Start** (levelData fallback from LevelRunner, begin wait state) | MonoSingleton | LevelRunner.Start (event sub), MusicManager.Start (event sub), HudController |
| `AudioMixerController` | Systems | -45 | Awake (base only) | MonoSingleton | AudioController (mixer routing) |
| `DynamicEventManager` | Systems | -40 | Awake (base only) | MonoSingleton | LevelRunner (dynamic events) |
| `EnemyPathingSystem` | Systems | -40 | Awake (base only) | MonoSingleton | LevelRunner.Update (Tick) |
| `KillsPerWaveTracker` | Systems | -40 | Awake (base only) | MonoSingleton | WaveManager events |
| `WaveRewardSpawner` | Systems | -40 | Awake (base only) | MonoSingleton | WaveManager.OnWaveCleared |
| `LifetimeStats` | Systems | -90 | Awake (base only) | MonoSingleton | Economy.AddGold, LevelRunner.Update |
| `MapRenderer` | Systems | +50 | **Start** (reads PathManager.Instance + LevelRunner.Instance) | plain MonoBehaviour | Needs grid + theme from singletons |
| `PathTilesController` | Systems | +55 | n/a (called by MapRenderer) | plain MonoBehaviour | MapRenderer |
| `SceneDecorController` | Systems | +60 | Awake (base only) | plain MonoBehaviour | MapRenderer |
| `TreasureSpawner` | Systems | +60 | Awake/Start | MonoBehaviour | LevelRunner.SpawnTreasureSystem |
| `MusicManager` | Systems | 0 | **Awake** (OnAwakeSingleton: RegisterTrack + BuildSources), **Start** (sub to WaveManager + EventManager) | MonoSingleton | AudioController (MusicSource routing) |
| `Synergies` | Systems | 0 | **Awake** (OnAwakeSingleton: sub OnSynergyActivated) | MonoSingleton | PlacementController (MarkDirty), LateUpdate tick |
| `PlacementController` | Systems | 0 | **Awake** (OnAwakeSingleton: cam cache + trigger TowerHoverController/GhostPreviewController/PlacementHighlight) | MonoSingleton | LevelRunner.Start (RestoreTowers), Synergies.LateUpdate |
| `Castle` | Entities | 0 | Init() called from LevelRunner.Start → SpawnCastle | MonoSingleton (partial) | Economy.ProcessInterestBank, LevelRunner (HP events), MetaUpgradeSystem.HandleLevelStart |
| `Hero` | Entities | 0 | `Awake` sets `Hero.Current`; Init() called from LevelRunner.Start → SpawnHero | plain MonoBehaviour (static `Current`) | Synergies.ApplyHeroAuraBuff, Economy.AddGoldFromKill |
| `VfxPool` | Visual | 0 | Awake (base only) | MonoSingleton (partial) | LevelRunner, Tower.Combat, JuiceFX |
| `JuiceFX` | Visual | 0 | Awake (base only) | MonoSingleton | LevelRunner, WaveManager (flash) |
| `CameraController` | Visual | 0 | Awake (base only) | MonoSingleton | Hero follow |
| `HudController` | UI | 0 | **Start** (sub to LevelRunner + WaveManager) | plain MonoBehaviour | Reads LevelRunner.Instance + WaveManager.Instance in Start |

*DEO = `DefaultExecutionOrder` attribute value. 0 = not set (Unity default order).*

---

## 2. Dependency Graph (ASCII)

Boot order flows top-to-bottom. Arrows = "requires Instance to be ready".

```
AWAKE PHASE (-100 first, then -90, -50, 0, +50…)
═══════════════════════════════════════════════════
PathManager(-100) ──builds Grid──────────────────────┐
Economy(-100)     ──no deps in Awake                  │
LifetimeStats(-90)──no deps in Awake                  │
AudioController(-50)──allocs pool/music source        │
EventManager(-50) ──no deps in Awake                  │
EnemyPool(-50)    ──no deps in Awake                  │
MetaUpgradeSystem(-50)──ComputeBonuses (SaveSystem)   │
LevelRunner(-50)  ──OnAwakeSingleton:                 │
    reads LevelLoader static fields (no singleton)    │
    reads LevelRegistry.Get() (ScriptableObject)      │
MusicManager(0)   ──BuildSources (no singleton deps)  │
PlacementController(0)──triggers TowerHoverCtrl,      │
    GhostPreviewCtrl, PlacementHighlight (MonoSingleton
    auto-create fallback if missing in scene)         │
Synergies(0)      ──registers own OnSynergyActivated  │
                                                      │
START PHASE (order mirrors DEO from Awake)            │
═══════════════════════════════════════════════════   │
Economy.Start ◄──── needs LevelRunner.Instance        │
    (reads CurrentLevel.StartCoins)                   │
    (reads MetaUpgradeSystem.ActiveBonuses)           │
WaveManager.Start ◄── needs LevelRunner.Instance      │
    (reads CurrentLevel as fallback)                  │
MetaUpgradeSystem.Start ◄── subs LevelEvents          │
LevelRunner.Start ◄── needs PathManager.Grid          │
    SpawnCastle ◄── needs PathManager.Grid ──────────►│
    SpawnHero   ◄── needs Castle (HP bonus)           │
    wire WaveManager events ◄── needs WaveManager.Instance
    wire Economy events ◄── needs Economy.Instance    │
MusicManager.Start ◄── subs WaveManager + EventManager│
MapRenderer.Start(+50) ◄── needs PathManager.Grid     │
    ◄── reads LevelRunner.CurrentLevel (theme)        │
HudController.Start(0) ◄── subs LevelRunner + WaveManager
    (defensive null-check on both before subscribe)   │
```

---

## 3. Risk Cascade — NRE Scenarios

### Risk A — HIGH: `PathManager.Grid` missing in LevelRunner.Start  
**Trigger:** PathManager not placed in scene, or Awake ordering race.  
**Cascade:**  
- `LevelRunner.Start → SpawnCastle` dereferences `PathManager.Instance.Grid` (line 672) **without null check on `.Grid`** — `NullReferenceException` thrown.  
- `PathManager.Instance` itself is safe (MonoSingleton auto-creates), but `Grid` can be null if `Build()` failed (missing `LevelData` on PathManager).  
- Same unsafe access at LevelRunner line 192 in `Start` bounds computation block.

**Pattern to fix:** Guard `PathManager.Instance?.Grid` at both call sites.

### Risk B — MEDIUM: `Economy.Start` reads `LevelRunner.Instance.CurrentLevel` (StartCoins)  
**Trigger:** `Economy` DEO=-100, `LevelRunner` DEO=-50 → Economy.Start runs **before** LevelRunner.Start sets up state.  
**Reality check:** Both are at Start phase, but `Start` execution order within the same DEO bucket depends on GameObject creation order in scene. If Economy is on a different GO that runs Start first, `LevelRunner.CurrentLevel` is already set from Awake (OnAwakeSingleton), so this is safe **in practice** — but depends on LevelRunner.Awake having run first.  
**Residual risk:** After scene reload (Additive), LevelRunner.Awake may not have fired yet when Economy.Start executes if GOs activate in a different order.

### Risk C — MEDIUM: `WaveManager.Start` reads `LevelRunner.Instance.CurrentLevel` as fallback  
Same pattern as Risk B. If LevelRunner did not set `currentLevel` in Awake (e.g., LevelRegistry asset missing), WaveManager falls back to `null` and logs error — does not crash, but level is unplayable.

### Risk D — MEDIUM: `PlacementController.OnAwakeSingleton` triggers MonoSingleton auto-create for TowerHoverController, GhostPreviewController, PlacementHighlight  
These three are accessed via `_ = Instance` in PlacementController.Awake to force early creation. If they aren't in the scene, the `MonoSingleton` base creates auto-GameObjects with a `Debug.LogWarning`. Safe, but noisy and the warning indicates a scene setup gap.

### Risk E — LOW: `LevelRunner.OnDestroySingleton` unsubscribes `WaveManager.Instance` events **without null-safe operator**  
Lines 154–160: `WaveManager.Instance.OnWaveStart -= ...` — if WaveManager was already destroyed (scene unload order), this throws NRE. The wrapping `if (WaveManager.Instance != null)` block **is present** but uses `!= null` (Unity override), so it's safe as long as WaveManager's `OnDestroy` sets its `_destroying` flag properly via MonoSingleton.

### Risk F — LOW: `MusicManager.Start` subscribes to `WaveManager.Instance` with null check  
Pattern is safe: `var wm = WaveManager.Instance; if (wm != null) { wm.OnWaveStart += ... }` — defensive. However, if MusicManager.Start runs before WaveManager exists in scene (WaveManager absent from Main.unity), music won't react to waves and no error is logged.

### Risk G — LOW: `Synergies.LateUpdate` reads `PlacementController.Instance` without null  
Line 82: `if (PlacementController.Instance == null) return;` — guarded. But calls `WaveManager.Instance?.ActiveEnemies` with ?., so safe.

### Risk H — LOW (scene reload): `MonoSingleton._destroying` flag is static  
The `_destroying = true` set in `OnDestroy` is a static field on the generic type. On scene reload without domain reload (Unity 2022+ fast enter play mode), `_destroying` persists as `true`, and `MonoSingleton.Instance` returns `null` for **all subsequent accesses** until a new Awake registers the instance and resets `_destroying`. Current code does **not** reset `_destroying = false` in Awake — the reset happens implicitly via `_instance = (T)this` but the flag is never cleared.

---

## 4. Recommendations

### R1 — Guard `PathManager.Grid` in LevelRunner (Risk A, HIGH)
```csharp
// LevelRunner.Start lines 192 and 672
var grid = PathManager.Instance?.Grid;
if (grid == null) { Debug.LogError("[LevelRunner] PathManager.Grid null — abort spawn"); return; }
```
This is the single highest-impact NRE risk currently in the codebase.

### R2 — Reset `_destroying` flag in MonoSingleton.Awake (Risk H)
```csharp
protected virtual void Awake()
{
    _destroying = false;   // <-- ADD: reset for scene reload without domain reload
    if (_instance != null && _instance != this) { Destroy(this); return; }
    _instance = (T)this;
    OnAwakeSingleton();
}
```
Without this, fast-enter-play-mode scene reloads in Editor silently break all MonoSingleton instances.

### R3 — Explicit ScriptExecutionOrder in Project Settings
Current ordering is declared via `[DefaultExecutionOrder]` attributes which is good. Recommend adding explicit entries in **Project Settings → Script Execution Order** for the critical chain to make it visually auditable without reading code:
```
PathManager       -100
Economy           -100
LifetimeStats      -90
AudioController    -50
EnemyPool          -50
EventManager       -50
LevelRunner        -50
WaveManager        -50
MapRenderer        +50
```

### R4 — Single GameLoop entry-point (DI pattern, future work)
Consider a `GameBootstrap` MonoBehaviour with the lowest `DefaultExecutionOrder` (e.g., -200) that explicitly constructs or validates all critical singletons in order, replacing the implicit "auto-create" fallback. This would make the init sequence deterministic and testable. Not urgent given current MonoSingleton safety net, but would eliminate Risks B, C, D entirely.

### R5 — Defensive null log in MusicManager when WaveManager absent (Risk F)
```csharp
var wm = WaveManager.Instance;
if (wm == null)
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.LogWarning("[MusicManager] WaveManager not found — music won't react to waves");
#endif
    return;
}
```

---

## 5. Summary

**Total MonoSingleton subclasses:** 46 (Systems: 35, Entities: 1 Castle, Visual: 10, UI: 19 of which 6 are MonoSingleton)  
**Classes with explicit DEO:** 24 of 46  
**Classes without DEO (Unity default = 0):** 22  
**Critical path Awake chain:** PathManager(-100) → AudioController(-50) → LevelRunner(-50) → [Start phase] Economy.Start + WaveManager.Start + LevelRunner.Start  
**Highest NRE risk:** `PathManager.Grid` unsafe dereference in `LevelRunner.Start` at lines 192 and 672  
**Highest structural risk:** `MonoSingleton._destroying` not reset on Awake breaks scene reload without domain reload (Editor fast-mode regression)
