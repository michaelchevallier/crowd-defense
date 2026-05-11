# Axis QA-AUTOMATION — Plan évolutif

**Sub-Opus** : SO-QA
**Branch** : `axis/qa`
**Worktree** : `/Users/mike/Work/crowd-defense/.claude/worktrees/agent-a3b743c8b42ac7187`
**Start** : 2026-05-11
**Budget** : 5-7h

## QA-1 self-check (DONE)

- [x] Lecture critical reads : CLAUDE.md, STATUS.md, file-ownership.md, api-contracts.md, qa-gates.md, integration-spec.md
- [x] Zone exclusive write confirmée : `.claude/qa/`, `Assets/Tests/`, `Assets/Editor/TestRunner.cs`, `Assets/Scripts/Tests/`
- [x] Hot zones inviolables : Tower.cs, Enemy.cs, Castle.cs, WaveManager.cs, LevelRunner.cs, Economy.cs, BalanceConfig.cs (read-only)
- [x] API contracts à valider : C1 AudioController.Play(string, float), C2 JuiceFX.Shake/Flash/SlowMo, C3 VfxPool.SpawnImpact/Death/Aura, C8 SaveSystem
- [x] Branche `axis/qa` créée depuis main HEAD (`a6540cb`)

## État infra Unity

- Branch HEAD : `a6540cb chore(coord): API contracts + QA gates + Integration spec for 7-axis swarm`
- Unity 6.3 LTS (`6000.3.15f1`)
- Existing tests : Aucun (Assets/Tests/ vide)
- `Packages/manifest.json` : Unity Test Framework **NOT INSTALLED** — à ajouter

## Deliverables Stage A

### G.1 — Test infrastructure setup
- [ ] Add `com.unity.test-framework` to manifest.json (due diligence : version stable Unity 6)
- [ ] `Assets/Tests/Runtime/CrowdDefense.Tests.Runtime.asmdef` (PlayMode)
- [ ] `Assets/Tests/Editor/CrowdDefense.Tests.Editor.asmdef` (EditMode)
- [ ] `Assets/Editor/TestRunner.cs` : MenuItem `Tools/CrowdDefense/QA/Run All Tests` + JSON report output

### G.2 — Smoke tests core systems (10-15 tests)
- [ ] `Assets/Tests/Runtime/AudioControllerTests.cs` : Play null clip, anti-replay 28ms, SetMasterVolume clamp
- [ ] `Assets/Tests/Runtime/JuiceFXTests.cs` : Shake duration+decay, Flash overlay, SlowMo timeScale restore
- [ ] `Assets/Tests/Runtime/VfxPoolTests.cs` : SpawnImpact return, pool reuse
- [ ] `Assets/Tests/Editor/AssetRegistryTests.cs` : Get existing key, Get missing key null, Has

### G.3 — Sprint-gate auto-qa-runner Unity port
- [ ] `.claude/qa/scenarios/` dossier
- [ ] `scenario-w1-1-complete.cs` (C# PlayMode scenario : load Main.unity, navigate W1-1, place Archer, run waves, expect victory)
- [ ] `scenario-w5-1-boss.cs` (C# PlayMode : W5-1 boss spawn + die)
- [ ] `scenario-stress-200-enemies.cs` (C# PlayMode : spawn 200 enemies, verify 30+ fps)
- [ ] `Assets/Editor/SprintGateRunner.cs` : MenuItem `Tools/CrowdDefense/QA/Run Sprint Gate` + report `.claude/qa/reports/sprint-{timestamp}.md`

### G.4 — Perf audit baseline
- [ ] `Assets/Tests/Runtime/PerfBaselineTests.cs` : FPS > 60 desktop avec 50 enemies + 10 towers
- [ ] FPS > 30 mobile-simulated
- [ ] Build size WebGL < 30 MB
- [ ] Metrics outputs `.claude/qa/reports/perf-baseline.md`

### G.5 — QA-2 Sonnet checkpoint template
- [ ] `.claude/qa/scripts/qa-checkpoint.sh` : args = axis + sha, run git diff + compile check + hot zone grep + API contracts check
- [ ] `.claude/qa/README.md` : usage doc

## Sonnets à spawn (5 max simultanés)

- **Sonnet G1** : test infra (asmdef + TestRunner) — bloque G2
- **Sonnet G2** : smoke tests (10-15) — start après G1 done
- **Sonnet G3** : sprint-gate scenarios + SprintGateRunner — parallèle avec G2
- **Sonnet G4** : perf baseline tests — parallèle avec G2/G3
- **Sonnet G5** : QA-2 checkpoint script + README — totalement parallèle

## QA self-test (meta)

- [ ] Mes tests passent eux-mêmes via `mcp__UnityMCP__run_tests`
- [ ] Mon QA-2 script tourne sur axis/qa et produit un report PASS

## Critères de fin

- Unity Test Framework installé + asmdefs OK
- 10-15 smoke tests pass
- 3 sprint-gate scenarios écrits + SprintGateRunner functional
- Perf baseline mesuré + documenté
- QA-2 checkpoint script reusable + README
- Push axis/qa + rapport final `.claude/coordination/axis-qa-report.md`
