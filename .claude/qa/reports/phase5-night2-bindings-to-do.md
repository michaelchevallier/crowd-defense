# Phase 5 Night 2 — Bindings TODO (Mike)
**Owner**: Mike (Inspector / Scene edits required — cannot be done headless)
**Priority order**: P0 first, then P1.

## P0 — Wire BossDef registry (blocks 11/11 victory)

### Symptom
On W1-1 wave 5 (boss wave with 2 enemies = boss + midboss), `BossSystem.OnEnemySpawned` logs:
```
[BossSystem] No BossDef for enemy id='boss' — add to registry
[BossSystem] No BossDef for enemy id='midboss' — add to registry
```
Every frame, generating ~9849 warnings/5min and slowing the V3LoopAutoRunner Tick callback to a crawl. The boss may still die from tower fire but the log spam interferes with my validator's progress polling.

### Files involved
- `Assets/Scripts/Systems/BossSystem.cs` — line 14 has `[SerializeField] private List<BossDef> registry`
- `Assets/Scripts/Data/BossDef.cs` — the SO class
- `Assets/ScriptableObjects/Enemies/Boss.asset` exists (EnemyType, id=boss, hp=60)
- **No BossDef ScriptableObjects found** under `Assets/ScriptableObjects/Bosses/`

### Likely fix (best to worst)
1. **Inspector wire-up** (recommended per Mike's memories — wire-as-you-go):
   - Locate the BossSystem GameObject in `Assets/Scenes/Main.unity`
   - Create BossDef SO assets for `boss`, `midboss`, and any other wave-5+ bosses (apocalypse, magic, lava, carnival)
   - Drag them into the BossSystem `registry` List<BossDef> field in Inspector
   - Save scene
2. **Resource folder + auto-load**: refactor BossSystem to `Resources.LoadAll<BossDef>("Bosses/")` in `OnAwakeSingleton`. Mike confirmed wire-Inspector is preferred but this is OK fallback.
3. **Skip gracefully**: in `BossSystem.OnEnemySpawned` (line 69), change `LogWarning` to `Log` and only emit once per id (cache `_warnedIds` HashSet). Doesn't fix the gameplay but stops the log spam.

### Verification once fixed
```
defaults write com.unity3d.UnityEditor5.x "cd_v3loop_auto_on_load" -bool true
open -a /Applications/Unity/Hub/Editor/6000.4.6f1-arm64/Unity.app --args -projectPath /Users/mike/Work/crowd-defense
# Wait ~10 min for completion
cat Library/V3LoopBatchReports/latest-auto.txt
```
Expected: last line is `phase11 FINAL VICTORY PASS state=Summary idx=5/5 castleHP=>0`.

## P1 — MissingReferenceException 'Tower has been destroyed'

### Symptom
During wave 2-3, exceptions like:
```
MissingReferenceException: The object of type 'CrowdDefense.Entities.Tower' has been destroyed but you are still trying to access it.
```
Towers being destroyed during a wave (presumably from siege/AoE enemies splash-damaging them) but a subscriber still holds a reference.

### Likely culprit
- `Synergies` or `PlacementController` event subscriber not unregistering on tower destroy
- Likely `OnTowerSold` / `OnTowerPlaced` cached lookup of destroyed Tower instance

### Fix
- Grep `Synergies.cs`, `PlacementController.cs`, `EventManager.cs` for `Tower` field references
- Add null-guard via `tower == null` (Unity overload) or explicit `OnDestroy → Unsubscribe` pattern

## P2 — Particle Velocity warning spam

### Symptom
~200,000 occurrences of `Particle Velocity curves must all be in the same mode` log entry, eating disk space + slowing Editor.

### Likely culprit
- A ParticleSystem prefab (probably VFX/explosion or hit-flash) has mixed velocity curve modes (constant + curve)

### Fix
- Open `Assets/Prefabs/VFX/` and `Assets/Prefabs/Effects/` ParticleSystem components
- Set all velocity curves to same mode (constant or curve, not mixed)
- Common fix: in PS module → Velocity over Lifetime → ensure all 3 axes are same mode

## P2 — WorldMapController misplaced warning

### Symptom
```
[WorldMapController] Misplaced controller in scene 'Main' — disabling component
```
Expected behavior (per R2-recovery comment in script). Only fires once per scene load. Cosmetic; not blocking.

## What I (V3LoopAutoRunner) need from Inspector

None beyond the BossDef registry above. The V3LoopAutoRunner accesses:
- `CrowdDefense.Systems.LevelRunner.Instance` — auto-instantiates
- `CrowdDefense.Systems.WaveManager.Instance` — auto-instantiates
- `CrowdDefense.Systems.PlacementController.Instance` — auto-instantiates
- `CrowdDefense.Entities.Castle.Instance` — auto-instantiates
- `CrowdDefense.Systems.Economy.Instance` — auto-instantiates
- `Resources.Load<TowerRegistry>("TowerRegistry")` — already in Resources

All confirmed PASS via the runner's `phase4 step3 PASS` line on every run.
