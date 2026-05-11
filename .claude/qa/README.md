# Crowd Defense — QA Infrastructure

> Owned by **Axis QA-AUTOMATION** (Sub-Opus SO-QA, branch `axis/qa`).
> Tests + scenarios + checkpoint scripts for the multi-axis swarm.

## Layout

```
.claude/qa/
├── README.md                       # this file
├── scenarios/                      # sprint-gate scenario *specs* (markdown / docs)
│   ├── scenario-w1-1-complete.cs   # spec doc — impl in Assets/Tests/Runtime/Scenarios/
│   ├── scenario-w5-1-boss.cs
│   └── scenario-stress-200-enemies.cs
├── scripts/
│   └── qa-checkpoint.sh            # QA-2 per-commit checkpoint script
└── reports/                        # auto-generated reports
    ├── perf-baseline.md            # appended by PerfBaselineTests
    ├── sprint-{timestamp}.md       # appended by SprintGateRunner
    ├── sprint-latest.md            # always the latest sprint-gate run
    ├── test-run-{timestamp}.json   # appended by TestRunner (full pass/fail)
    └── test-run-latest.md          # markdown summary, latest
```

The implementation files live under Unity-asmdef paths to be compiled :

```
Assets/Tests/
├── Runtime/                                            # PlayMode tests
│   ├── CrowdDefense.Tests.Runtime.asmdef               # asmdef (Editor platform)
│   ├── AudioControllerTests.cs                         # smoke tests
│   ├── JuiceFXTests.cs
│   ├── VfxPoolTests.cs
│   ├── PerfBaselineTests.cs                            # G.4 perf baseline
│   └── Scenarios/
│       ├── ScenarioW1_1Complete.cs                     # G.3 sprint-gate scenarios
│       ├── ScenarioW5_1Boss.cs
│       └── ScenarioStress200Enemies.cs
└── Editor/                                             # EditMode tests
    ├── CrowdDefense.Tests.Editor.asmdef
    ├── AssetRegistryTests.cs
    └── AudioClipRegistryTests.cs

Assets/Editor/
├── TestRunner.cs                   # MenuItem Tools/CrowdDefense/QA/Run All Tests
└── SprintGateRunner.cs             # MenuItem Tools/CrowdDefense/QA/Run Sprint Gate
```

## Test framework dependencies

`com.unity.test-framework` v1.6.0 + `com.unity.test-framework.performance` v3.4.0 are
already installed transitively via `com.coplaydev.unity-mcp` — **no manifest changes
required**. Verified `Packages/packages-lock.json` 2026-05-11.

## How to run

### From Unity Editor (interactive)

- **Window → General → Test Runner** : the standard Unity Test Runner window. Open it,
  see all tests under both EditMode and PlayMode tabs, click "Run All".
- **Tools → CrowdDefense → QA → Run All Tests** : programmatic run via
  `Assets/Editor/TestRunner.cs`. Writes JSON + markdown reports to `.claude/qa/reports/`.
- **Tools → CrowdDefense → QA → Run Sprint Gate** : runs only the 3 scenarios + perf
  baseline. Writes `sprint-{timestamp}.md` + `sprint-latest.md`.

### From CLI (batch mode, CI)

```bash
# Run all tests, exit code 0 on pass / 1 on fail.
Unity -batchmode -nographics -projectPath . \
      -runTests -testPlatform PlayMode \
      -testResults Build/test-results-playmode.xml -quit

Unity -batchmode -nographics -projectPath . \
      -runTests -testPlatform EditMode \
      -testResults Build/test-results-editmode.xml -quit

# Sprint gate only.
Unity -batchmode -nographics -projectPath . \
      -executeMethod CrowdDefense.Editor.SprintGateRunner.RunCli -quit
```

## QA-2 checkpoint script (per-commit hot zone / ownership gate)

Every Sub-Opus must run `qa-checkpoint.sh` after each commit, before continuing.

```bash
# From repo root.
./.claude/qa/scripts/qa-checkpoint.sh <axis-name> [<commit-sha>]

# Examples:
./.claude/qa/scripts/qa-checkpoint.sh audio
./.claude/qa/scripts/qa-checkpoint.sh visual-core HEAD~2
./.claude/qa/scripts/qa-checkpoint.sh qa f89ba3f
```

Valid axis names : `visual-core`, `audio`, `asset-gen`, `content`, `build`, `ux`, `qa`.

Exit codes :
- `0` — PASS (no issues)
- `1` — FAIL (hot zone or ownership violation, BLOCK merge)
- `2` — WARN (lint issues only, review)

Report written to `.claude/coordination/qa-reports/{axis}-{sha}.md`.

### What it checks

1. **Hot zone violation** : the commit must NOT touch any file in `file-ownership.md`
   hot zones (Tower.cs, Enemy.cs, Castle.cs, WaveManager.cs, LevelRunner.cs, Economy.cs,
   BalanceConfig.cs, STATUS.md, Packages/manifest.json).
2. **Ownership scope** : every changed file must match the axis's exclusive write zone
   OR be in always-allowed cross-axis paths (`.claude/plans/`, `.claude/coordination/requests/`).
3. **Lint** : `Debug.Log` without `#if UNITY_EDITOR` or `#if DEVELOPMENT_BUILD` guard.

### What it does NOT check (manual / other tools)

- **Compilation errors** : run `mcp__UnityMCP__refresh_unity` + `mcp__UnityMCP__read_console`
  in the Unity Editor instance. The shell script alone is too slow (Unity boot ~30s).
- **Test pass/fail** : run the Test Runner separately.
- **API contract conformance** : grep `api-contracts.md` for the new public signatures.

## Reports

Reports live in `.claude/qa/reports/` (axis QA outputs) and
`.claude/coordination/qa-reports/` (QA-2 per-commit outputs from other axes).

- `perf-baseline.md` — perf measurements, appended cumulatively.
- `sprint-latest.md` — most recent sprint-gate run.
- `sprint-{timestamp}.md` — historical sprint-gate snapshots.
- `test-run-latest.md` — most recent full test-runner result.

## Workflow for a new axis Sub-Opus

1. Create your branch : `git checkout -b axis/{name}` from main HEAD.
2. Write your plan : `.claude/plans/axis-{name}.md`.
3. Spawn Sonnets for the deliverables.
4. After each Sonnet commits, run :
   ```bash
   ./.claude/qa/scripts/qa-checkpoint.sh {name}
   ```
   Inspect the report in `.claude/coordination/qa-reports/{axis}-{sha}.md`. If FAIL,
   either revert or write to `.claude/coordination/requests/` for arbitration.
5. At end of Stage A, run sprint-gate from Unity Editor :
   - `Tools → CrowdDefense → QA → Run Sprint Gate`
   - Inspect `.claude/qa/reports/sprint-latest.md`.
6. Push your axis branch. Main Orchestrator merges to integration branch.

## Future extensions (Phase 4+)

- Chrome MCP integration for live `/v6/` E2E smoke tests (port `auto-qa-runner` agent
  from milan project — its `.mjs` scenario format would live in `.claude/qa/scenarios/`
  alongside the `.cs` ones, and a node runner would drive Chrome MCP).
- Snapshot screenshot diffs for HUD rendering regression.
- Performance.Measurement / SampleGroup integration with `Unity.PerformanceTesting`
  to track perf regression as time series across commits.
