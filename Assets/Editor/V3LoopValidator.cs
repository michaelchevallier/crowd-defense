#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CrowdDefense.EditorTools
{
    /// <summary>
    /// Headless validator for the 11-step V3 gameplay loop driven by Unity-MCP.
    /// Runs as a coroutine off EditorApplication.update so it never blocks the
    /// MCP request thread. Posts results to EditorPrefs so a follow-up MCP call
    /// can pick them up at its own pace.
    ///
    /// Menu: Tools/CrowdDefense/QA/V3Loop/Run
    ///       Tools/CrowdDefense/QA/V3Loop/Status
    ///       Tools/CrowdDefense/QA/V3Loop/Stop
    /// EditorPrefs keys:
    ///   cd_v3loop_status  = "idle" | "running" | "done" | "error" | "stopped"
    ///   cd_v3loop_step    = current step (1..11) being worked on
    ///   cd_v3loop_log     = JSON string with per-step pass/fail + diagnostics
    /// </summary>
    public static class V3LoopValidator
    {
        private const string PrefStatus = "cd_v3loop_status";
        private const string PrefStep   = "cd_v3loop_step";
        private const string PrefLog    = "cd_v3loop_log";

        private static bool _running;
        private static IEnumerator? _coro;
        private static readonly StringBuilder _log = new();

        // --------------------------------------------------------------- Menu

        [MenuItem("Tools/CrowdDefense/QA/V3Loop/Run")]
        public static void Run()
        {
            if (_running)
            {
                Debug.LogWarning("[V3LoopValidator] Already running.");
                return;
            }
            _running  = true;
            _log.Clear();
            EditorPrefs.SetString(PrefStatus, "running");
            EditorPrefs.SetInt(PrefStep, 0);
            EditorPrefs.SetString(PrefLog, "");
            _coro = ValidateRoutine();
            EditorApplication.update += Pump;
        }

        [MenuItem("Tools/CrowdDefense/QA/V3Loop/Status")]
        public static void Status()
        {
            Debug.Log($"[V3LoopValidator] status={EditorPrefs.GetString(PrefStatus, "idle")} step={EditorPrefs.GetInt(PrefStep, 0)} log={EditorPrefs.GetString(PrefLog, "")}");
        }

        [MenuItem("Tools/CrowdDefense/QA/V3Loop/Stop")]
        public static void Stop()
        {
            if (!_running) return;
            _running = false;
            EditorApplication.update -= Pump;
            _coro = null;
            EditorPrefs.SetString(PrefStatus, "stopped");
            Debug.Log("[V3LoopValidator] Stopped.");
        }

        // ----------------------------------------------------------- Internals

        private static void Pump()
        {
            if (!_running || _coro == null) return;

            try
            {
                if (!_coro.MoveNext())
                {
                    Finish("done");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"ERROR {ex.GetType().Name}: {ex.Message}");
                Debug.LogException(ex);
                Finish("error");
            }
        }

        private static void Finish(string status)
        {
            _running = false;
            EditorApplication.update -= Pump;
            _coro = null;
            EditorPrefs.SetString(PrefStatus, status);
            EditorPrefs.SetString(PrefLog, _log.ToString());
            Debug.Log($"[V3LoopValidator] FINISH status={status}\n{_log}");
        }

        private static void AppendLog(string s)
        {
            _log.AppendLine(s);
            EditorPrefs.SetString(PrefLog, _log.ToString());
            Debug.Log("[V3LoopValidator] " + s);
        }

        private static void SetStep(int n)
        {
            EditorPrefs.SetInt(PrefStep, n);
        }

        // Step the player loop a few times each Pump tick so we don't hog the
        // main thread. Yield (`yield return null`) returns control to Pump.
        private static IEnumerator StepFrames(int n, int batch = 30)
        {
            for (int i = 0; i < n; i += batch)
            {
                int b = Mathf.Min(batch, n - i);
                for (int j = 0; j < b; j++)
                    EditorApplication.Step();
                yield return null;
            }
        }

        private static IEnumerator ValidateRoutine()
        {
            AppendLog("=== V3LoopValidator START ===");

            // Pre-requisites
            if (!EditorApplication.isPlaying)
            {
                AppendLog("FAIL precondition: editor not in Play mode.");
                yield break;
            }

            // -----  Resolve singletons ---------------------------------------
            var lr     = CrowdDefense.Systems.LevelRunner.Instance;
            var wm     = CrowdDefense.Systems.WaveManager.Instance;
            var pc     = CrowdDefense.Systems.PlacementController.Instance;
            var castle = CrowdDefense.Entities.Castle.Instance;
            var econ   = CrowdDefense.Systems.Economy.Instance;

            if (lr == null || wm == null || pc == null || castle == null || econ == null)
            {
                AppendLog($"FAIL singletons: lr={(lr!=null)} wm={(wm!=null)} pc={(pc!=null)} castle={(castle!=null)} econ={(econ!=null)}");
                yield break;
            }
            AppendLog($"PRE Setup: level={lr.CurrentLevel?.Id} waves={wm.TotalWaves} castleHP={castle.HP}/{castle.HPMax} gold={econ.Gold}");

            // Tower registry
            var reg = Resources.Load<CrowdDefense.Data.TowerRegistry>("TowerRegistry");
            CrowdDefense.Data.TowerType? archer = null;
            if (reg != null)
                foreach (var t in reg.Towers) if (t != null && t.Id == "archer") { archer = t; break; }
            if (archer == null) { AppendLog("FAIL no archer tower."); yield break; }

            // -----  STEP 3 (already pre-validated): main scene loaded --------
            SetStep(3);
            AppendLog($"step3 PASS scene+singletons OK");

            // -----  STEP 5: place towers ------------------------------------
            SetStep(5);
            // Cheat gold to place many towers
            econ.AddGold(5000);
            var bps = UnityEngine.Object.FindObjectsByType<CrowdDefense.Entities.BuildPoint>(FindObjectsSortMode.None);
            int placedBefore = pc.PlacedTowers.Count;
            int placedNow    = 0;
            int targetPlace  = 10;
            for (int i = 0; i < bps.Length && placedNow < targetPlace; i++)
            {
                pc.OpenBuildPointPicker(bps[i].Cell);
                if (pc.TryPlaceAtActiveBuildCell(archer)) placedNow++;
            }
            int placed = pc.PlacedTowers.Count - placedBefore;
            AppendLog($"step5 placed={placed} totalNow={pc.PlacedTowers.Count}");
            if (placed < 5) { AppendLog("step5 FAIL not enough towers"); }

            yield return null;

            // -----  STEP 6: start wave 1 --------------------------------------
            SetStep(6);
            if (!wm.IsWaitingForPlayerStart)
            {
                AppendLog("step6 SKIP wave not waiting for player");
            }
            else
            {
                wm.StartNextWave();
                yield return StepFrames(60);
                AppendLog($"step6 waveActive={wm.IsWaveActive} pending={wm.PendingSpawnCount} spawned={wm.WaveTotalSpawned}");
                if (!wm.IsWaveActive) { AppendLog("step6 FAIL"); }
            }

            // -----  STEP 7: mob walk + tower shoot ---------------------------
            SetStep(7);
            int killsBefore = wm.WaveKillCount;
            yield return StepFrames(600);
            int killsAfter = wm.WaveKillCount;
            AppendLog($"step7 killsBefore={killsBefore} killsAfter={killsAfter} dKills={killsAfter - killsBefore} active={wm.ActiveEnemies.Count}");
            if (killsAfter - killsBefore < 1) AppendLog("step7 FAIL no kills observed");

            // -----  STEP 8: castle damage (gated by WaitForSeconds) ---------
            SetStep(8);
            int hpBeforeDmg = castle.HP;
            // Drive until either (a) mob reaches castle and damage > 0, (b) wave clear, or (c) timeout
            int loopGuard = 0;
            while (castle.HP == hpBeforeDmg
                   && wm.IsWaveActive
                   && !castle.IsDead
                   && loopGuard < 100)
            {
                yield return StepFrames(120);
                loopGuard++;
            }
            int hpAfterDmg = castle.HP;
            AppendLog($"step8 hpBefore={hpBeforeDmg} hpAfter={hpAfterDmg} dmgTaken={hpBeforeDmg - hpAfterDmg} loopGuard={loopGuard}");
            if (hpAfterDmg < hpBeforeDmg) AppendLog("step8 PASS castle took damage");
            else AppendLog("step8 INFO no damage during wave 1 (towers cleared all mobs)");

            // -----  STEP 9: wave 1 clears + WaveBreak ----------------------
            SetStep(9);
            int waveIdxBefore = wm.CurrentWaveIdx;
            // Drive until WaitingForPlayerStart, castle dead, or timeout
            loopGuard = 0;
            while (!wm.IsWaitingForPlayerStart
                   && !castle.IsDead
                   && lr.State != CrowdDefense.Systems.GameState.Summary
                   && loopGuard < 200)
            {
                yield return StepFrames(120);
                loopGuard++;
            }
            AppendLog($"step9 waveIdx={wm.CurrentWaveIdx} lrState={lr.State} waiting={wm.IsWaitingForPlayerStart} castleDead={castle.IsDead} loopGuard={loopGuard}");
            if (wm.IsWaitingForPlayerStart && lr.State == CrowdDefense.Systems.GameState.WaveBreak)
                AppendLog("step9 PASS wave cleared, WaveBreak entered");
            else if (lr.State == CrowdDefense.Systems.GameState.Summary && !castle.IsDead)
                AppendLog("step9 PASS already reached summary (victory)");
            else
                AppendLog("step9 FAIL stuck");

            // -----  STEP 10: wave 2 starts + clears -------------------------
            SetStep(10);
            if (wm.CurrentWaveIdx >= wm.TotalWaves - 1)
            {
                AppendLog("step10 SKIP no more waves (only 1 wave configured?)");
            }
            else if (lr.State == CrowdDefense.Systems.GameState.Summary)
            {
                AppendLog("step10 N/A already at summary");
            }
            else
            {
                wm.StartNextWave();
                yield return StepFrames(60);
                int wave2KillsBefore = wm.WaveKillCount;
                AppendLog($"step10 startWave2 active={wm.IsWaveActive} pending={wm.PendingSpawnCount}");
                loopGuard = 0;
                while (!wm.IsWaitingForPlayerStart && !castle.IsDead && lr.State != CrowdDefense.Systems.GameState.Summary && loopGuard < 200)
                {
                    yield return StepFrames(120);
                    loopGuard++;
                }
                int wave2KillsAfter = wm.WaveKillCount;
                AppendLog($"step10 wave2: kills={wave2KillsAfter} dKills={wave2KillsAfter - wave2KillsBefore} waveIdx={wm.CurrentWaveIdx} lrState={lr.State}");
                if ((wm.IsWaitingForPlayerStart || lr.State == CrowdDefense.Systems.GameState.Summary) && !castle.IsDead)
                    AppendLog("step10 PASS wave 2 progressed cleanly");
                else
                    AppendLog("step10 FAIL");
            }

            // -----  STEP 11: drive remaining waves to victory ---------------
            SetStep(11);
            loopGuard = 0;
            int maxWaveRuns = 10;
            while (lr.State != CrowdDefense.Systems.GameState.Summary
                   && !castle.IsDead
                   && loopGuard < maxWaveRuns)
            {
                if (wm.IsWaitingForPlayerStart)
                {
                    AppendLog($"step11 startWave#{wm.CurrentWaveIdx + 1}");
                    wm.StartNextWave();
                }
                yield return StepFrames(60);
                int safety = 0;
                while (!wm.IsWaitingForPlayerStart && !castle.IsDead && lr.State != CrowdDefense.Systems.GameState.Summary && safety < 200)
                {
                    yield return StepFrames(120);
                    safety++;
                }
                AppendLog($"step11 after iter{loopGuard}: idx={wm.CurrentWaveIdx} lrState={lr.State} castleHP={castle.HP}");
                loopGuard++;
            }
            AppendLog($"step11 FINAL lrState={lr.State} idx={wm.CurrentWaveIdx} castleDead={castle.IsDead} castleHP={castle.HP}");
            if (lr.State == CrowdDefense.Systems.GameState.Summary && !castle.IsDead)
                AppendLog("step11 PASS VICTORY reached");
            else if (castle.IsDead)
                AppendLog("step11 INFO castle died (defeat path)");
            else
                AppendLog("step11 FAIL not at summary");

            AppendLog("=== V3LoopValidator END ===");
        }
    }
}
