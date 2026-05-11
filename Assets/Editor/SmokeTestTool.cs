#nullable enable
using System.Linq;
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Editor
{
    public static class SmokeTestTool
    {
        [MenuItem("Tools/CrowdDefense/Smoke Test 10 Levels")]
        public static void RunSmokeTest()
        {
            var registry = LevelRegistry.Get();
            if (registry == null || registry.Levels == null || registry.Levels.Count == 0)
            {
                Debug.LogError("[SmokeTest] LevelRegistry empty. Run 'Tools/CrowdDefense/Build LevelRegistry' first.");
                return;
            }

            var rng = new System.Random();
            var picks = registry.Levels
                .OrderBy(_ => rng.Next())
                .Take(10)
                .ToList();

            int pass = 0, fail = 0;
            foreach (var level in picks)
            {
                try
                {
                    if (level == null) { fail++; Debug.LogError("[SmokeTest] FAIL: null level entry in registry"); continue; }

                    var grid = GridData.Parse(level);
                    if (grid.Portals.Count == 0) { fail++; Debug.LogError($"[SmokeTest] FAIL {level.Id}: no portal"); continue; }
                    if (grid.Castles.Count == 0) { fail++; Debug.LogError($"[SmokeTest] FAIL {level.Id}: no castle"); continue; }

                    var path = grid.BfsShortestPath(grid.Portals[0], grid.Castles[0]);
                    if (path == null || path.Count < 2) { fail++; Debug.LogError($"[SmokeTest] FAIL {level.Id}: no BFS path"); continue; }

                    bool waveOk = true;
                    foreach (var wave in level.Waves)
                    {
                        if (wave.entries == null) continue;
                        foreach (var entry in wave.entries)
                        {
                            if (entry.type == null) { waveOk = false; Debug.LogError($"[SmokeTest] FAIL {level.Id}: wave entry with null EnemyType"); break; }
                        }
                        if (!waveOk) break;
                    }
                    if (!waveOk) { fail++; continue; }

                    var balance = BalanceConfig.Get();
                    if (balance == null) { fail++; Debug.LogError($"[SmokeTest] FAIL {level.Id}: BalanceConfig not found in Resources/"); continue; }

                    int hp = balance.CastleHPFor(level.World, level.Level);
                    if (hp <= 0) { fail++; Debug.LogError($"[SmokeTest] FAIL {level.Id}: CastleHPFor returns {hp}"); continue; }

                    Debug.Log($"[SmokeTest] PASS {level.Id} (w{level.World}-l{level.Level}, {path.Count} waypoints, hp={hp}, {level.Waves.Count} waves)");
                    pass++;
                }
                catch (System.Exception e)
                {
                    fail++;
                    Debug.LogError($"[SmokeTest] EXCEPTION {level?.Id ?? "?"}: {e.Message}");
                }
            }

            Debug.Log($"[SmokeTest] Final: {pass}/10 PASS, {fail}/10 FAIL");
        }
    }
}
