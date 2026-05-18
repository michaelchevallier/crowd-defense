#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrowdDefense.EditorTools
{
    /// <summary>
    /// Headless V3 11-step validator runnable from -batchmode -executeMethod.
    /// Writes a JSON report to .claude/qa/reports/v3loop-batch-{timestamp}.json
    /// and exits. Avoids the WaitForSeconds problem by using EditorApplication.Step()
    /// to advance frames synchronously.
    /// </summary>
    public static class V3LoopValidatorBatch
    {
        private static readonly StringBuilder _log = new();
        private static readonly Dictionary<string, object> _steps = new();

        // Entry point: launched via -executeMethod CrowdDefense.EditorTools.V3LoopValidatorBatch.Run
        public static void Run()
        {
            try
            {
                Debug.Log("[V3LoopBatch] === START ===");
                _log.AppendLine($"start {DateTime.UtcNow:O}");

                // Force the Loader/Main scene
                var mainScene = "Assets/Scenes/Main.unity";
                if (!System.IO.File.Exists(mainScene))
                {
                    LogStep("setup", "FAIL Main.unity missing");
                    WriteAndExit(2);
                    return;
                }

                EditorSceneManager.OpenScene(mainScene, OpenSceneMode.Single);
                LogStep("setup", "scene opened");

                // Enter Play mode synchronously
                EditorApplication.EnterPlaymode();

                // Pump player loop until isPlaying becomes true (Step works only after enter)
                int sanity = 0;
                while (!EditorApplication.isPlaying && sanity < 200)
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                    System.Threading.Thread.Sleep(50);
                    sanity++;
                }
                _log.AppendLine($"isPlaying={EditorApplication.isPlaying} after {sanity} iters");

                // Now use Step() to drive frames; first run a few steps to let scene initialize
                StepNFrames(120);

                var lr     = CrowdDefense.Systems.LevelRunner.Instance;
                var wm     = CrowdDefense.Systems.WaveManager.Instance;
                var pc     = CrowdDefense.Systems.PlacementController.Instance;
                var castle = CrowdDefense.Entities.Castle.Instance;
                var econ   = CrowdDefense.Systems.Economy.Instance;

                if (lr == null || wm == null || pc == null || castle == null || econ == null)
                {
                    LogStep("step3-prereq", $"FAIL singletons: lr={lr!=null} wm={wm!=null} pc={pc!=null} castle={castle!=null} econ={econ!=null}");
                    WriteAndExit(3);
                    return;
                }
                LogStep("step3", $"PASS scene+singletons OK level={lr.CurrentLevel?.Id} waves={wm.TotalWaves} castleHP={castle.HP}/{castle.HPMax}");

                // Step 5: place towers
                var reg = Resources.Load<CrowdDefense.Data.TowerRegistry>("TowerRegistry");
                CrowdDefense.Data.TowerType? archer = null;
                if (reg != null) foreach (var t in reg.Towers) if (t != null && t.Id == "archer") { archer = t; break; }
                if (archer == null) { LogStep("step5", "FAIL no archer tower"); WriteAndExit(5); return; }

                econ.AddGold(5000);
                var bps = UnityEngine.Object.FindObjectsByType<CrowdDefense.Entities.BuildPoint>(FindObjectsSortMode.None);
                int placedBefore = pc.PlacedTowers.Count;
                int placedNow = 0;
                int targetPlace = 10;
                for (int i = 0; i < bps.Length && placedNow < targetPlace; i++)
                {
                    pc.OpenBuildPointPicker(bps[i].Cell);
                    if (pc.TryPlaceAtActiveBuildCell(archer)) placedNow++;
                }
                int placed = pc.PlacedTowers.Count - placedBefore;
                LogStep("step5", $"placed={placed} total={pc.PlacedTowers.Count} (target {targetPlace})");

                // Step 6: start wave
                if (!wm.IsWaitingForPlayerStart)
                {
                    LogStep("step6", "SKIP wave not waiting");
                }
                else
                {
                    wm.StartNextWave();
                    StepNFrames(60);
                    LogStep("step6", $"waveActive={wm.IsWaveActive} pending={wm.PendingSpawnCount} spawned={wm.WaveTotalSpawned}");
                }

                // Step 7: tower shoots
                int killsBefore = wm.WaveKillCount;
                StepNFrames(600);
                int killsAfter = wm.WaveKillCount;
                LogStep("step7", $"kills {killsBefore}->{killsAfter} dKills={killsAfter - killsBefore} active={wm.ActiveEnemies.Count}");

                // Step 8: castle damage (gated by WaitForSeconds)
                int hpBefore = castle.HP;
                int loopGuard = 0;
                while (castle.HP == hpBefore && wm.IsWaveActive && !castle.IsDead && loopGuard < 100)
                {
                    StepNFrames(120);
                    loopGuard++;
                }
                LogStep("step8", $"hp {hpBefore}->{castle.HP} (dmg={hpBefore - castle.HP}) loopGuard={loopGuard}");

                // Step 9: wave 1 clears
                int waveIdxBefore = wm.CurrentWaveIdx;
                loopGuard = 0;
                while (!wm.IsWaitingForPlayerStart && !castle.IsDead
                       && lr.State != CrowdDefense.Systems.GameState.Summary
                       && lr.State != CrowdDefense.Systems.GameState.Lost
                       && loopGuard < 200)
                {
                    StepNFrames(120);
                    loopGuard++;
                }
                LogStep("step9", $"idx {waveIdxBefore}->{wm.CurrentWaveIdx} state={lr.State} waiting={wm.IsWaitingForPlayerStart} castleHP={castle.HP}");

                // Step 10: drive wave 2 if exists
                if (wm.CurrentWaveIdx < wm.TotalWaves - 1
                    && lr.State != CrowdDefense.Systems.GameState.Summary
                    && lr.State != CrowdDefense.Systems.GameState.Lost
                    && !castle.IsDead)
                {
                    // Place more towers for wave 2
                    placedNow = 0;
                    for (int i = 0; i < bps.Length && placedNow < 10; i++)
                    {
                        if (pc.PlacedTowers.Count >= 20) break;
                        pc.OpenBuildPointPicker(bps[i].Cell);
                        pc.TryPlaceAtActiveBuildCell(archer);
                        placedNow++;
                    }
                    LogStep("step10-pre", $"more towers placed -> total {pc.PlacedTowers.Count}");

                    wm.StartNextWave();
                    StepNFrames(60);
                    int kBefore = wm.WaveKillCount;
                    loopGuard = 0;
                    while (!wm.IsWaitingForPlayerStart && !castle.IsDead
                           && lr.State != CrowdDefense.Systems.GameState.Summary
                           && lr.State != CrowdDefense.Systems.GameState.Lost
                           && loopGuard < 300)
                    {
                        StepNFrames(120);
                        loopGuard++;
                    }
                    LogStep("step10", $"wave2 kills {kBefore}->{wm.WaveKillCount} idx={wm.CurrentWaveIdx} state={lr.State} castleHP={castle.HP}");
                }
                else
                {
                    LogStep("step10", $"SKIP castleDead={castle.IsDead} state={lr.State}");
                }

                // Step 11: drive remaining waves to victory/defeat
                int maxIter = 10;
                int iter = 0;
                while (lr.State != CrowdDefense.Systems.GameState.Summary
                       && lr.State != CrowdDefense.Systems.GameState.Lost
                       && !castle.IsDead
                       && iter < maxIter)
                {
                    if (wm.IsWaitingForPlayerStart)
                    {
                        LogStep($"step11-iter{iter}", $"start wave #{wm.CurrentWaveIdx + 1}");
                        wm.StartNextWave();
                    }
                    StepNFrames(60);
                    int safety = 0;
                    while (!wm.IsWaitingForPlayerStart && !castle.IsDead
                           && lr.State != CrowdDefense.Systems.GameState.Summary
                           && lr.State != CrowdDefense.Systems.GameState.Lost
                           && safety < 300)
                    {
                        StepNFrames(120);
                        safety++;
                    }
                    LogStep($"step11-iter{iter}", $"idx={wm.CurrentWaveIdx} state={lr.State} castleHP={castle.HP}");
                    iter++;
                }
                LogStep("step11-final", $"state={lr.State} idx={wm.CurrentWaveIdx} castleDead={castle.IsDead} castleHP={castle.HP}/{castle.HPMax}");

                if (lr.State == CrowdDefense.Systems.GameState.Summary && !castle.IsDead)
                    LogStep("VICTORY", "PASS");
                else if (lr.State == CrowdDefense.Systems.GameState.Lost || castle.IsDead)
                    LogStep("DEFEAT", "INFO castle destroyed");
                else
                    LogStep("INCONCLUSIVE", "FAIL state stuck");

                WriteAndExit(0);
            }
            catch (Exception ex)
            {
                _log.AppendLine($"EXCEPTION {ex.GetType().Name}: {ex.Message}");
                _log.AppendLine(ex.StackTrace);
                Debug.LogException(ex);
                WriteAndExit(99);
            }
        }

        private static void LogStep(string key, string msg)
        {
            _steps[key] = msg;
            _log.AppendLine($"{key}: {msg}");
            Debug.Log($"[V3LoopBatch] {key}: {msg}");
        }

        private static void StepNFrames(int n)
        {
            for (int i = 0; i < n; i++) EditorApplication.Step();
        }

        private static void WriteAndExit(int code)
        {
            try
            {
                string dir = "Library/V3LoopBatchReports";
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, $"report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt");
                File.WriteAllText(path, _log.ToString());
                Debug.Log($"[V3LoopBatch] report written: {path}");
                File.WriteAllText(Path.Combine(dir, "latest.txt"), _log.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            EditorApplication.Exit(code);
        }
    }
}
