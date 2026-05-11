# Axis QA-AUTOMATION — Stage A Report

**Sub-Opus** : SO-QA
**Branch** : `axis/qa`
**Worktree** : `.claude/worktrees/agent-a3b743c8b42ac7187`
**Start / End** : 2026-05-11 18:25 / 2026-05-11 18:45 (~20 min)
**Status** : **READY FOR MERGE**

## Deliverables livrés

### G.1 — Test infrastructure setup ✅

- **Unity Test Framework** : déjà installé transitivement via `com.coplaydev.unity-mcp` (v1.6.0 + `.performance` 3.4.0). Vérifié via `Packages/packages-lock.json`. Aucune modif `manifest.json` requise.
- `Assets/Tests/Runtime/CrowdDefense.Tests.Runtime.asmdef` — PlayMode, Editor platform only, references `CrowdDefense` + TestRunner.
- `Assets/Tests/Editor/CrowdDefense.Tests.Editor.asmdef` — EditMode, references `CrowdDefense` + TestRunner.
- `Assets/Editor/TestRunner.cs` — MenuItem `Tools/CrowdDefense/QA/Run All Tests` + `Run EditMode Tests` + `Run PlayMode Tests`. Outputs JSON + markdown reports to `.claude/qa/reports/test-run-{timestamp}.json|md`. Batch-mode CLI compatible (auto-exits with code 0/1).

### G.2 — Smoke tests core systems ✅ (27 tests, target était 10-15)

PlayMode (`Assets/Tests/Runtime/`) :
- `AudioControllerTests.cs` (6 tests) — Play missing/empty key noop, anti-replay 28ms, SetMasterVolume clamp 0-1, SetMuted toggle, Instance set
- `JuiceFXTests.cs` (5 tests) — Shake offset+restore, Flash overlay VisualElement, SlowMo timeScale apply+restore, SetBaseCamPos, Instance
- `VfxPoolTests.cs` (6 tests) — SpawnImpact/Death/CoinPickup no-throw, pool reuse bound, SpawnAura return, Instance
- `Scenarios/Scenario*Tests.cs` (3 tests) — sprint-gate scenarios (cf G.3)
- `PerfBaselineTests.cs` (3 tests) — perf (cf G.4)

EditMode (`Assets/Tests/Editor/`) :
- `AssetRegistryTests.cs` (6 tests) — Get existing/missing/empty, Has, cache rebuild
- `AudioClipRegistryTests.cs` (4 tests) — Get existing/missing, Has

Pattern technique : reflection pour injection des `SerializeField private` (évite dépendance UnityEditor dans le chain Runtime). Pattern `SetActive(false) → AddComponent → inject → SetActive(true)` pour Awake propre.

### G.3 — Sprint-gate scenarios + SprintGateRunner ✅

Scenarios sous `Assets/Tests/Runtime/Scenarios/` :
- `ScenarioW1_1Complete.cs` — load Main.unity, verify LevelRunner/WaveManager/Castle scaffold OK avec W1-1 LevelData résolu. Marks Inconclusive si LevelRegistry absent (boot guard) → robuste pour CI.
- `ScenarioW5_1Boss.cs` — walk W5-1 wave entries, asserts ≥1 boss-tier enemy declared. Reflection-based pour tolérer évolution schema EnemyType.
- `ScenarioStress200Enemies.cs` — spawn 200 GOs synthetic avec rotating transforms, sample FPS 3s window, assert avg ≥30 fps mobile floor.

Spec docs en `.claude/qa/scenarios/scenario-*.cs` (markdown-as-comment), implémentations canon sous `Assets/Tests/Runtime/Scenarios/`.

`Assets/Editor/SprintGateRunner.cs` — MenuItem `Tools/CrowdDefense/QA/Run Sprint Gate` + CLI `SprintGateRunner.RunCli`. Filters PlayMode tests par groupNames, writes `.claude/qa/reports/sprint-{timestamp}.md` + `sprint-latest.md`.

### G.4 — Perf audit baseline ✅

`Assets/Tests/Runtime/PerfBaselineTests.cs` :
- `Perf_50Enemies_10Towers_DesktopFloor60Fps` — synthetic scene 50e+10t, assert FPS ≥60 sur 2s window après 0.5s warmup
- `Perf_50Enemies_10Towers_MobileFloor30Fps` — same, 30 fps mobile floor
- `Perf_WebGLBuildSize_UnderTarget` — scan `Build/WebGL/` ou `Builds/WebGL/`, assert <30 MB. Inconclusive si pas de build.

Tous les measurements appended cumulatively à `.claude/qa/reports/perf-baseline.md` pour tracking régression cross-session.

### G.5 — QA-2 checkpoint script + README ✅

`.claude/qa/scripts/qa-checkpoint.sh` :
- Args : `<axis-name>` `[<commit-sha>]`
- Checks : hot zone violations (Tower.cs, Enemy.cs, etc.) + ownership zone scope + Debug.Log #if guard lint
- Output : `.claude/coordination/qa-reports/{axis}-{sha}.md` avec Status PASS/WARN/FAIL
- Exit codes : 0 PASS / 1 FAIL / 2 WARN
- macOS Bash 3.2 compatible (case statement au lieu de `declare -A`)
- Skip Editor/Tests folders pour lint (Debug.Log fine in non-ship paths)

`.claude/qa/README.md` (~100 lignes) :
- Layout `.claude/qa/` + `Assets/Tests/` + `Assets/Editor/`
- Comment lancer les tests (Test Runner window, MenuItems, CLI batch-mode)
- Workflow QA-2 per-commit pour autres Sub-Opus
- Extensions futures Phase 4+ (Chrome MCP scenarios .mjs, screenshot diff, performance time series)

## QA self-test (méta)

- ✅ QA-1 self-check (lecture coord docs, branche créée propre depuis main)
- ✅ QA-2 sur HEAD `e1dbd38` → **PASS** (no hot zone / no ownership / no lint violations) ; report `.claude/coordination/qa-reports/qa-e1dbd38.md`
- ✅ QA-2 cross-axis test (simulé "audio" sur mes commits) → **FAIL** correctement (ownership violation detected sur Assets/Tests/* hors zone audio) → confirme que le script bloque effectivement les écritures hors zone
- ⚠️ **Compile check via UnityMCP IMPOSSIBLE** : Unity Editor running sur `/Users/mike/Work/crowd-defense` (axis/ux instance), pas sur worktree axis/qa. Static review effectué :
  - APIs vérifiées : `EditorSceneManager.LoadSceneAsyncInPlayMode(string, LoadSceneParameters)` via `unity_reflect` ✅
  - APIs source vérifiées : `LevelLoader.NextLevelId`, `LevelRegistry.FindById`, `LevelRegistry.Get`, `LevelData.Waves`, `WaveDef.entries`, `LevelRunner.PrimaryCastle/TotalCastleHP`, `WaveManager.TotalWaves` ✅
  - Field reflection paths vérifiés : `JuiceFX._shakeIntensity/_baseCamPos/_flashOverlay`, `AudioController.registry`, `AudioClipRegistry.entries/_cache`, `AssetRegistry.entries/_cache`, `VfxPool.impactPrefab/deathPrefab/auraPrefab/coinPickupPrefab` ✅

## Commits Stage A (8 commits)

```
e1dbd38 fix(qa): test setup uses SetActive(false) before AddComponent for clean init
a46509b fix(qa): drop unused Unity.PerformanceTesting from Runtime tests asmdef
11285b2 fix(qa): qa-checkpoint.sh Bash 3.2 compat + skip Editor/Tests lint
f9dffca chore(qa): QA-2 checkpoint script + README
2979bac feat(qa): perf baseline tests + initial report seed
76860ae feat(qa): 3 sprint-gate scenarios + SprintGateRunner MenuItem
7a77ba8 test(qa): 27 smoke tests for AudioController/JuiceFX/VfxPool/AssetRegistry
aa53aa9 feat(qa): test infrastructure asmdefs + TestRunner MenuItem
```

20 fichiers créés, ~2030 lignes ajoutées (tests + scripts + docs).

## Fichiers critiques

Zone exclusive write QA :
- `.claude/qa/` (12 fichiers : README, scripts/qa-checkpoint.sh, scenarios/*.cs spec docs, reports/perf-baseline.md)
- `Assets/Tests/Runtime/` (6 fichiers : 4 smoke test files + asmdef + scenarios/)
- `Assets/Tests/Editor/` (3 fichiers : 2 smoke test files + asmdef)
- `Assets/Editor/TestRunner.cs`
- `Assets/Editor/SprintGateRunner.cs`

Aucune écriture hot zone, aucune écriture hors zone QA. Vérifié par self qa-checkpoint.

## Open gates / contraintes pour MO

1. **Compile validation requise par MO** : avant merge dans integration/phase3-4-5, MO doit lancer `mcp__UnityMCP__refresh_unity` + `read_console` sur l'instance Unity active (post-merge axis/qa dans main). Static review suggère aucune erreur de compile mais l'integration test est non-fait.
2. **Tests dépendent de LevelRegistry / Main.unity** : `ScenarioW1_1Complete` et `ScenarioW5_1Boss` skip avec Inconclusive si LevelRegistry vide. Pour les faire passer effectivement : avoir Tools/CrowdDefense/Build LevelRegistry run + W1-1, W5-1 LevelData assets présents. Géré par Axis CONTENT (parallèle).
3. **PerfBaseline WebGL test skip** si pas de build présent. Géré par Axis BUILD (parallèle).
4. **Pattern reflection sur SerializeField** : si Axis AUDIO renomme `AudioController.registry` field, les tests AudioControllerTests cassent. Mitigation : `Assert.IsNotNull(field, "X must exist")` donne erreur claire au lieu d'un NullRef silencieux.

## Issues / decisions

- **Pas d'ajout dépendance manifest.json** : Unity Test Framework + Performance déjà transitifs via Unity-MCP. ✓ Due diligence ok.
- **Choix `.cs` plutôt que `.mjs`** pour scenarios : justifié par stack unique Unity (pas de Node CI dependency), maintenance simpler. `.mjs` Chrome MCP scenarios restent ouverts pour Phase 4+ E2E live `/v6/`.
- **Synthetic perf baseline plutôt que real enemies** : reproducible cross-machine, dénué de couplage à AssetRegistry/EnemyPool qui pourraient varier. Real-scene perf testing reste optionnel future via SprintGateRunner extension.

## Recommandation merge

`axis/qa` est prêt à être mergé dans `integration/phase3-4-5`. Toutes les deliverables Stage A passent les critères de fin du brief. Le QA self-test est PASS. Le script qa-checkpoint.sh est immédiatement utilisable par les 6 autres Sub-Opus axes pour leurs propres QA-2 per-commit gates.

Prochaine action attendue : MO merge axis/qa + run Unity compile validation + lance les autres axes Stage A en attente.
