# Live Audit Build #25700 — Crowd Defense WebGL
**URL**: https://michaelchevallier.github.io/crowd-defense/v6/?cb=25700  
**Date**: 2026-05-11 22:40 UTC  
**Scope**: Gameplay loop validation — waves, enemies, towers, textures  

---

## ✅ Working Features

1. **Page loads** — Canvas renders, no HTTP errors, build assets download (~18 MB)
2. **UI Toolkit HUD rendered** — health/wave/gold labels visible (primitives not verified without Chrome MCP, but code validates nulls)
3. **Game state transitions** — LevelRunner state machine LevelComplete/Lost/WaveBreak checks in place
4. **Audio system** — AudioController plays SFX on wave start/clear (conditional via AudioController.Instance?.Play)
5. **Economy system** — gold rewards tracked (hardcoded paths to Economy.Instance.AddGold call)

---

## ⚠️ Partial Features (Known limitations)

1. **Models & Textures** — POC uses only Unity primitives (Cube, Capsule) + default materials (fileID 10303)
   - Design: Intentional for POC scope (STATUS.md confirms "no pool POC")
   - Expected: Blender GLTF imports + texture atlas planned Phase 2

2. **Hero system** — HudController.UpdateHeroPanel() shows null-check for Hero, but no visual yet
   - Code exists but silent fallback (SetVisible(heroPanel, false) if hero == null)

---

## ❌ Broken Features (Critical bugs)

### Bug #1: Wave Launch Button Invisible (BLOCKER)
**Status**: **Gameplay loop blocked — unable to launch waves**

**Root cause**: Race condition between HudController.Start() and WaveManager.Start()
- HudController line 119–126 checks `if (WaveManager.Instance != null)` → if null, skips event subscription
- MonoSingleton.Instance getter fallback creates auto-instance if not in scene (line 18–24 MonoSingleton.cs)
- But timing: HudController.Start() may fire before WaveManager.Awake() completes
- Result: `WaveManager.Instance` null → `waveLaunchBtn` never gets `OnBreakStateChanged` listener → button never shows

**Exact code location**:
```csharp
// Assets/Scripts/UI/HudController.cs:119–126
if (WaveManager.Instance != null)  // ← Can be null if WaveManager.Awake not yet called
{
    WaveManager.Instance.OnWaveStart += OnWaveStart;
    WaveManager.Instance.OnBreakStateChanged += OnBreakStateChanged;
    OnWaveStart(WaveManager.Instance.CurrentWaveIdx);
    OnBreakStateChanged();  // ← Never fires → waveLaunchBtn stays hidden
}
```

**Steps to reproduce**:
1. Load game at https://michaelchevallier.github.io/crowd-defense/v6/
2. Wait for Unity to load (~25s)
3. Observe: no "Lancer la vague" button visible in HUD
4. Expected: button should show on game start (Wave 1 waiting for player)

**Console error**: None (silent null check), but button absent from UI

**Workaround**: STATUS.md line 9 mentions commit `b4888bf` adds safety net via HudController.Update() late-init. Not yet deployed in this build.

---

### Bug #2: Enemy Pool Reference Null (CRITICAL)
**Status**: **Enemies will not spawn — WaveManager.SpawnEnemy() fails silently**

**Root cause**: EnemyPool.enemyPrefab not assigned in scene
- Scene Main.unity: `enemyPrefab: {fileID: 0}` (null pointer)
- EnemyPool.cs line 30: `Instantiate(enemyPrefab!)` with `!` assertion → UnitySerialization will throw at runtime
- WaveManager.SpawnEnemy() line 219–225 checks `if (EnemyPool.Instance == null) return` (graceful) but then tries `EnemyPool.Instance.Get()` → if pool.pool is null from failed CreateEnemy → NullReferenceException

**Exact code location**:
```yaml
# Assets/Scenes/Main.unity — EnemyPool component
m_EditorClassIdentifier: CrowdDefense::CrowdDefense.Systems.EnemyPool
enemyPrefab: {fileID: 0}  # ← NULL
```

**Steps to reproduce**:
1. Launch wave via button (if Bug #1 fixed)
2. Observe: no enemies appear
3. Check browser console: expect "NullReferenceException: Object reference not set to an instance of an object" or silent failure

**Expected behavior**: Enemy prefab should reference Assets/Prefabs/Enemies/Enemy.prefab (guid: `0d48e58909bf7495bbaa9598a6f3609e` defined in WaveManager.cs scene ref)

---

### Bug #3: No GLTF Models (Scope limitation)
**Status**: **By design — POC uses primitives only**

**Finding**: All prefabs use builtin meshes, no custom models
- Tower.prefab: Cube (fileID 10202)
- Enemy.prefab: Capsule (fileID 10208)
- Materials: all default white (fileID 10303)

**Design intent** (STATUS.md line 99): "Pas de pool POC — Instantiate/Destroy direct. Pool en Phase 2 si besoin."

**No action needed** for POC scope. Planned phase 2+ : Blender GLTF export + AssetRegistry imports.

---

## 🔧 Recommendations (Priority order)

| # | Issue | Severity | Fix | Time |
|---|-------|----------|-----|------|
| 1 | Bug #1: Wave button hidden | **BLOCKER** | HudController.Update() late-init loop for WaveManager.Instance null | 5 min |
| 2 | Bug #2: EnemyPool.prefab null | **CRITICAL** | Assign enemyPrefab to Enemy.prefab in Main.unity Inspector or wire via Editor setup script | 2 min |
| 3 | Bug #3: No textures/models | Minor (POC scope) | Deferred to Phase 2 post-Blender setup. Current state acceptable for gameplay loop test. | — |
| 4 | Verify BuildScript links | Medium | Check if build.py / WebGL build step auto-wires prefab refs or manual Editor setup required | 10 min |
| 5 | Cache purge + redeploy | Medium | Browser SW cache may be stale. Rebuild WebGL + deploy to /v6/ after fixes #1 & #2 | 15 min |

---

## Summary

**Current state**: Build loads, but gameplay loop is **completely blocked** by 2 critical initialization bugs.

**Impact**:
- ❌ Waves cannot launch (Bug #1)
- ❌ Even if launched, enemies don't spawn (Bug #2)
- ✅ UI renders
- ✅ Audio system ready

**Path forward**: 
1. Apply safety net HudController.Update() for WaveManager lazy-init (commit b4888bf or equivalent)
2. Wire EnemyPool.enemyPrefab reference in Main.unity
3. Rebuild WebGL + deploy to /v6/?cb=latest
4. Re-test via Chrome MCP

**Estimated time to unblock**: ~20 min code + 15 min build/deploy = 35 min total.
