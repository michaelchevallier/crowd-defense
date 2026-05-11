// Sprint-gate scenario : stress test 200 simultaneous enemies.
//
// Goal : spawn 200 enemies via EnemyPool/WaveManager test hooks, sample FPS over
// 5s window, assert avg FPS >= 30 (mobile floor) and >= 60 (desktop optional).
//
// Canonical impl path : Assets/Scripts/Tests/Scenarios/ScenarioStress200Enemies.cs
//
// Assertions :
// - 200 enemies spawned, all active
// - Average FPS over 5s window >= 30
// - No NullReferenceException in console
//
// Timeout : 30s real time.
