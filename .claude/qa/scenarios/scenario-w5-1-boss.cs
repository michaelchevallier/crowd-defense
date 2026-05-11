// Sprint-gate scenario : W5-1 boss spawn + die.
//
// Goal : load Main scene, force currentLevel = W5-1, place towers to outclass
// boss waves, verify boss enemy spawned at expected wave index and dies before
// reaching castle.
//
// Canonical impl path : Assets/Scripts/Tests/Scenarios/ScenarioW5_1Boss.cs
//
// Assertions :
// - W5-1 LevelData resolved
// - Boss tier enemy is spawned during wave run
// - Boss eventually dies (HP <= 0)
// - LevelRunner state is Victory by timeout
//
// Timeout : 120s real time (boss levels longer).
