// Sprint-gate scenario : W1-1 complete victory.
//
// Goal : load Main scene, force currentLevel = W1-1, place 1 Archer tower,
// run all waves at 10x speed, verify victory state reached before timeout.
//
// Run via Tools/CrowdDefense/QA/Run Sprint Gate (SprintGateRunner.cs) OR
// directly as a PlayMode test (NUnit pickup). The file lives under .claude/qa/scenarios/
// so it is NOT compiled as part of Assets/Tests/ asmdefs — the SprintGateRunner copies
// or references the scenario implementation in Assets/Scripts/Tests/Scenarios/.
//
// Canonical impl path : Assets/Scripts/Tests/Scenarios/ScenarioW1_1Complete.cs
//
// Assertions :
// - W1-1 LevelData resolved from registry
// - Castle spawned with HP > 0
// - Tower placement succeeds at chosen cell
// - WaveManager advances through all waves
// - LevelRunner.State transitions to Victory before 60s real time
//
// Timeout : 60s real time (game runs at 10x via LevelRunner.targetSpeed cheat).
