#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrowdDefense.EditorTools
{
    /// <summary>
    /// Auto-runs the V3 11-step validator on Editor startup when EditorPrefs key
    /// "cd_v3loop_auto_on_load" is true. Survives domain reloads by persisting
    /// state in SessionState.
    /// </summary>
    [InitializeOnLoad]
    public static class V3LoopAutoRunner
    {
        private const string PrefAuto    = "cd_v3loop_auto_on_load";
        private const string PrefQuit    = "cd_v3loop_quit_on_done";
        private const string SsActive    = "cd_v3loop_active";
        private const string SsPhase     = "cd_v3loop_phase";
        private const string SsLog       = "cd_v3loop_log";
        private const string SsHpBefore  = "cd_v3loop_hp_before";
        private const string SsLoop8     = "cd_v3loop_loop8";
        private const string SsLoop9     = "cd_v3loop_loop9";
        private const string SsLoop10    = "cd_v3loop_loop10";
        private const string SsIter11    = "cd_v3loop_iter11";
        private const string SsTickCount = "cd_v3loop_tick_count";

        private static bool _hooked;

        static V3LoopAutoRunner()
        {
            Debug.Log($"[V3LoopAuto] ctor batchMode={Application.isBatchMode} pref={EditorPrefs.GetBool(PrefAuto, false)} ssActive={SessionState.GetBool(SsActive, false)}");
            if (Application.isBatchMode) return;

            bool prefSet = EditorPrefs.GetBool(PrefAuto, false);
            bool ssActive = SessionState.GetBool(SsActive, false);

            if (prefSet)
            {
                // First launch: consume pref, activate session
                EditorPrefs.SetBool(PrefAuto, false);
                SessionState.SetBool(SsActive, true);
                SessionState.SetInt(SsPhase, 0);
                SessionState.SetString(SsLog, "");
                SessionState.SetInt(SsTickCount, 0);
                Debug.Log("[V3LoopAuto] activated session for run");
            }

            if (SessionState.GetBool(SsActive, false))
            {
                Hook();
            }
        }

        [MenuItem("Tools/CrowdDefense/QA/V3Loop/Auto/Enable+Restart")]
        public static void EnableAndRestart()
        {
            EditorPrefs.SetBool(PrefAuto, true);
            EditorPrefs.SetBool(PrefQuit, true);
            Debug.Log("[V3LoopAuto] Enabled. Restart Unity for auto-run.");
        }

        [MenuItem("Tools/CrowdDefense/QA/V3Loop/Auto/Run-Now")]
        public static void RunNow()
        {
            SessionState.SetBool(SsActive, true);
            SessionState.SetInt(SsPhase, 0);
            SessionState.SetString(SsLog, "");
            SessionState.SetInt(SsTickCount, 0);
            Hook();
        }

        [MenuItem("Tools/CrowdDefense/QA/V3Loop/Auto/Stop")]
        public static void Stop()
        {
            SessionState.SetBool(SsActive, false);
            Unhook();
            WriteReport(SessionState.GetString(SsLog, ""));
            Debug.Log("[V3LoopAuto] Stopped");
        }

        // ----------------------------------------------------------- Internals

        private static void Hook()
        {
            if (_hooked) return;
            _hooked = true;
            EditorApplication.update += Tick;
            Debug.Log("[V3LoopAuto] hooked update");
        }

        private static void Unhook()
        {
            if (!_hooked) return;
            _hooked = false;
            EditorApplication.update -= Tick;
        }

        private static void Append(string s)
        {
            string log = SessionState.GetString(SsLog, "") + s + "\n";
            SessionState.SetString(SsLog, log);
            Debug.Log("[V3LoopAuto] " + s);
        }

        private static void WriteReport(string log)
        {
            try
            {
                string dir = "Library/V3LoopBatchReports";
                Directory.CreateDirectory(dir);
                string ts = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                string path = Path.Combine(dir, $"auto-{ts}.txt");
                File.WriteAllText(path, log);
                File.WriteAllText(Path.Combine(dir, "latest-auto.txt"), log);
                Debug.Log($"[V3LoopAuto] report: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void Finish(string note)
        {
            Append(note);
            Append($"=== V3LoopAutoRunner END {DateTime.UtcNow:O} ===");
            SessionState.SetBool(SsActive, false);
            Unhook();
            string log = SessionState.GetString(SsLog, "");
            WriteReport(log);
            if (EditorPrefs.GetBool(PrefQuit, false))
            {
                EditorApplication.Exit(0);
            }
        }

        private static void Tick()
        {
            try
            {
                if (!SessionState.GetBool(SsActive, false)) { Unhook(); return; }
                int tick = SessionState.GetInt(SsTickCount, 0) + 1;
                SessionState.SetInt(SsTickCount, tick);
                StepImpl(tick);
            }
            catch (Exception ex)
            {
                Append($"EXCEPTION {ex.GetType().Name}: {ex.Message}");
                Debug.LogException(ex);
                Finish("ABORTED");
            }
        }

        private static int Phase => SessionState.GetInt(SsPhase, 0);
        private static void NextPhase(int n) { SessionState.SetInt(SsPhase, n); }

        private static void StepImpl(int tick)
        {
            switch (Phase)
            {
                case 0: EnsureScene();          break;
                case 1: EnterPlay();            break;
                case 2: WaitForPlayActive();    break;
                case 3: PostPlayWarmup();       break;
                case 4: ResolveSingletons();    break;
                case 5: PlaceTowers();          break;
                case 6: StartWave1();           break;
                case 7: WatchWave1Damage();     break;
                case 8: WatchWave1Clear();      break;
                case 9: StartWave2();           break;
                case 10: WatchWave2Clear();     break;
                case 11: DriveRemainingWaves(); break;
                default: Finish($"phase={Phase}"); break;
            }
        }

        private static int _phaseLocalCounter = 0; // Resets after domain reload, but only used inside one phase

        // ---- Phase implementations ------------------------------------------

        private static void EnsureScene()
        {
            // Clear mid-level save state so we start wave 1 fresh (avoid 119/200 castle from prior test runs)
            try
            {
                CrowdDefense.Systems.SaveSystem.ClearMidLevelState();
                Append("phase0: ClearMidLevelState done");
            }
            catch (Exception ex)
            {
                Append($"phase0: save clear FAILED {ex.Message}");
            }

            var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (s.name != "Main")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
                Append("phase0: scene Main opened");
            }
            else
            {
                Append("phase0: scene Main already active");
            }
            NextPhase(1);
        }

        private static void EnterPlay()
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.EnterPlaymode();
                Append("phase1: EnterPlaymode requested");
            }
            else
            {
                Append($"phase1: already playing? isPlaying={EditorApplication.isPlaying}");
            }
            NextPhase(2);
        }

        private static void WaitForPlayActive()
        {
            // Domain reload during EnterPlaymode resets _phaseLocalCounter; OK
            _phaseLocalCounter++;
            if (EditorApplication.isPlaying)
            {
                Append($"phase2: isPlaying=true after local {_phaseLocalCounter} ticks");
                _phaseLocalCounter = 0;
                NextPhase(3);
            }
            else if (_phaseLocalCounter > 1200)
            {
                Append("phase2 FAIL isPlaying never true");
                Finish("FAIL: play mode never entered");
            }
        }

        private static void PostPlayWarmup()
        {
            _phaseLocalCounter++;
            // Step() advances player loop by exactly one frame each call.
            for (int i = 0; i < 10; i++) EditorApplication.Step();
            if (_phaseLocalCounter >= 60) // 600 frames warmup
            {
                Append($"phase3 warmup done time={Time.time:F2} frame={Time.frameCount} timeScale={Time.timeScale} isPaused={EditorApplication.isPaused}");
                _phaseLocalCounter = 0;
                NextPhase(4);
            }
        }

        // Cached refs - rebuilt each tick (cheap)
        private static CrowdDefense.Systems.LevelRunner? Lr => CrowdDefense.Systems.LevelRunner.Instance;
        private static CrowdDefense.Systems.WaveManager? Wm => CrowdDefense.Systems.WaveManager.Instance;
        private static CrowdDefense.Systems.PlacementController? Pc => CrowdDefense.Systems.PlacementController.Instance;
        private static CrowdDefense.Entities.Castle? CastleI => CrowdDefense.Entities.Castle.Instance;
        private static CrowdDefense.Systems.Economy? Econ => CrowdDefense.Systems.Economy.Instance;
        private static CrowdDefense.Data.TowerType? Archer
        {
            get
            {
                var reg = Resources.Load<CrowdDefense.Data.TowerRegistry>("TowerRegistry");
                if (reg == null) return null;
                foreach (var t in reg.Towers) if (t != null && t.Id == "archer") return t;
                return null;
            }
        }

        // N26: Position Hero on the path mid-point so it intercepts stragglers
        // that slip through tower coverage. Hero auto-attacks any enemy in range.
        private static void PositionHeroOnPath()
        {
            var hero = CrowdDefense.Entities.Hero.Current;
            if (hero == null) return;
            // N32: skip if hero is dead (during respawn coroutine); avoids forcing teleport
            // while internal state is mid-transition.
            if (!hero.IsAlive) return;
            var pm = CrowdDefense.Systems.PathManager.Instance;
            if (pm == null || pm.Paths.Count == 0) return;
            var wps = pm.Paths[0];
            if (wps.Count < 4) return;
            // Position hero ~25% from castle end (path is portal→castle so castle is last waypoint)
            // We pick the waypoint at 75% of the path length — close enough to castle that hero
            // covers stragglers but far enough to engage mobs before they hit castle.
            int idx = Mathf.Clamp(Mathf.RoundToInt(wps.Count * 0.75f), 0, wps.Count - 1);
            var target = wps[idx];
            target.y = 0.5f; // ensure not underground
            hero.transform.position = target;
        }

        private static void ResolveSingletons()
        {
            if (Lr == null || Wm == null || Pc == null || CastleI == null || Econ == null)
            {
                Append($"phase4 FAIL singletons: lr={Lr!=null} wm={Wm!=null} pc={Pc!=null} castle={CastleI!=null} econ={Econ!=null}");
                Finish("FAIL singletons");
                return;
            }
            if (Archer == null) { Append("phase4 FAIL no archer"); Finish("FAIL no archer"); return; }
            Append($"phase4 step3 PASS level={Lr.CurrentLevel?.Id} waves={Wm.TotalWaves} castle={CastleI.HP}/{CastleI.HPMax}");
            // N26: Reposition hero on path mid-route so it intercepts stragglers
            PositionHeroOnPath();
            var heroDbg = CrowdDefense.Entities.Hero.Current;
            Append($"phase4 hero pos={(heroDbg!=null ? heroDbg.transform.position.ToString("F2") : "null")}");
            NextPhase(5);
        }

        private static void PlaceTowers()
        {
            // Cheat: enough gold for 30+ towers
            Econ!.AddGold(50000);
            var bpsAll = UnityEngine.Object.FindObjectsByType<CrowdDefense.Entities.BuildPoint>(FindObjectsSortMode.None);
            // Sort build points by distance to nearest path waypoint so we cover the path first
            var pm = CrowdDefense.Systems.PathManager.Instance;
            var bps = SortByPathProximity(bpsAll, pm);
            int placed = 0;
            int target = Mathf.Min(30, bps.Length);
            for (int i = 0; i < bps.Length && placed < target; i++)
            {
                Pc!.OpenBuildPointPicker(bps[i].Cell);
                if (Pc.TryPlaceAtActiveBuildCell(Archer!)) placed++;
            }
            Append($"phase5 step5 placed={placed} total={Pc!.PlacedTowers.Count} gold={Econ.Gold}");
            NextPhase(6);
        }

        private static CrowdDefense.Entities.BuildPoint[] SortByPathProximity(
            CrowdDefense.Entities.BuildPoint[] bps, CrowdDefense.Systems.PathManager? pm)
        {
            if (pm == null || pm.Paths.Count == 0) return bps;
            var wps = pm.Paths[0];
            float DistToPath(Vector3 pos)
            {
                float best = float.MaxValue;
                for (int i = 0; i < wps.Count; i++)
                {
                    float d = (pos - wps[i]).sqrMagnitude;
                    if (d < best) best = d;
                }
                return best;
            }
            var arr = new (CrowdDefense.Entities.BuildPoint bp, float d)[bps.Length];
            for (int i = 0; i < bps.Length; i++)
                arr[i] = (bps[i], DistToPath(bps[i].transform.position));
            System.Array.Sort(arr, (a, b) => a.d.CompareTo(b.d));
            var sorted = new CrowdDefense.Entities.BuildPoint[bps.Length];
            for (int i = 0; i < arr.Length; i++) sorted[i] = arr[i].bp;
            return sorted;
        }

        private static void StartWave1()
        {
            if (Wm!.IsWaitingForPlayerStart)
            {
                PositionHeroOnPath();
                Wm.StartNextWave();
                for (int i = 0; i < 60; i++) EditorApplication.Step();
                Append($"phase6 step6 PASS waveActive={Wm.IsWaveActive} pending={Wm.PendingSpawnCount} time={Time.time:F2} frame={Time.frameCount}");
            }
            else
            {
                Append("phase6 step6 SKIP not waiting");
            }
            SessionState.SetInt(SsHpBefore, CastleI?.HP ?? 0);
            SessionState.SetInt(SsLoop8, 0);
            NextPhase(7);
        }

        private static void WatchWave1Damage()
        {
            int loop = SessionState.GetInt(SsLoop8, 0) + 1;
            SessionState.SetInt(SsLoop8, loop);
            for (int i = 0; i < 120; i++) EditorApplication.Step();
            int hpBefore = SessionState.GetInt(SsHpBefore, 200);
            if (CastleI!.HP < hpBefore || CastleI.IsDead || !Wm!.IsWaveActive)
            {
                Append($"phase7 step8 hp {hpBefore}->{CastleI.HP} loop={loop}");
                SessionState.SetInt(SsLoop9, 0);
                NextPhase(8);
            }
            else if (loop > 100)
            {
                Append($"phase7 step8 INFO no damage during wave 1 loop={loop}");
                SessionState.SetInt(SsLoop9, 0);
                NextPhase(8);
            }
        }

        private static void WatchWave1Clear()
        {
            int loop = SessionState.GetInt(SsLoop9, 0) + 1;
            SessionState.SetInt(SsLoop9, loop);
            for (int i = 0; i < 120; i++) EditorApplication.Step();
            if (Wm!.IsWaitingForPlayerStart || CastleI!.IsDead
                || Lr!.State == CrowdDefense.Systems.GameState.Summary
                || Lr.State == CrowdDefense.Systems.GameState.Lost)
            {
                Append($"phase8 step9 idx={Wm.CurrentWaveIdx} state={Lr!.State} waiting={Wm.IsWaitingForPlayerStart} castleHP={CastleI.HP}");
                NextPhase(9);
            }
            else if (loop > 600)
            {
                int active = Wm.ActiveEnemies?.Count ?? -1;
                int pending = Wm.PendingSpawnCount;
                int spawned = Wm.WaveTotalSpawned;
                int kills = Wm.WaveKillCount;
                Append($"phase8 step9 FAIL stuck loop={loop} active={active} pending={pending} spawned={spawned} kills={kills} castleHP={CastleI.HP}");
                Finish("FAIL wave 1 never cleared");
            }
            // Diagnostic every 50 loops
            else if (loop % 50 == 0)
            {
                int active = Wm.ActiveEnemies?.Count ?? -1;
                Append($"phase8 step9 progress loop={loop} active={active} kills={Wm.WaveKillCount} castleHP={CastleI.HP}");
            }
        }

        private static void StartWave2()
        {
            if (CastleI!.IsDead || Lr!.State == CrowdDefense.Systems.GameState.Summary
                || Lr.State == CrowdDefense.Systems.GameState.Lost
                || Wm!.CurrentWaveIdx >= Wm.TotalWaves - 1)
            {
                Append($"phase9 step10 SKIP castleDead={CastleI.IsDead} state={Lr.State} idx={Wm.CurrentWaveIdx}");
                NextPhase(11); // skip to phase 11
                return;
            }

            // Upgrade all existing towers to L3 before placing new ones (massive DPS boost)
            Econ!.AddGold(200000);
            int upgraded = 0;
            foreach (var t in Pc!.PlacedTowers)
            {
                if (t == null || t.UpgradeLevel >= 3) continue;
                try
                {
                    if (t.UpgradeTo(2)) upgraded++;
                    if (t.UpgradeLevel < 3 && t.UpgradeTo(3)) upgraded++;
                }
                catch { /* ignore upgrade exceptions for dead towers */ }
            }
            Append($"phase9 upgraded={upgraded} towers to L3");

            // Place as many towers as possible (different types for AoE diversity)
            Econ.AddGold(50000);
            var bpsAll = UnityEngine.Object.FindObjectsByType<CrowdDefense.Entities.BuildPoint>(FindObjectsSortMode.None);
            var bps = SortByPathProximity(bpsAll, CrowdDefense.Systems.PathManager.Instance);
            var reg = Resources.Load<CrowdDefense.Data.TowerRegistry>("TowerRegistry");
            CrowdDefense.Data.TowerType? cannon = null;
            CrowdDefense.Data.TowerType? mage = null;
            CrowdDefense.Data.TowerType? ballista = null;
            CrowdDefense.Data.TowerType? frost = null;     // N30: slow stragglers
            CrowdDefense.Data.TowerType? skyguard = null;  // N30: anti-flyer / dot
            if (reg != null)
            {
                foreach (var t in reg.Towers)
                {
                    if (t == null) continue;
                    if (t.Id == "cannon") cannon = t;
                    if (t.Id == "mage") mage = t;
                    if (t.Id == "ballista") ballista = t;
                    if (t.Id == "frost") frost = t;
                    if (t.Id == "skyguard") skyguard = t;
                }
            }

            int extra = 0;
            for (int i = 0; i < bps.Length; i++)
            {
                if (Pc!.PlacedTowers.Count >= 60) break;
                Pc.OpenBuildPointPicker(bps[i].Cell);
                // Closest to path: cannon (AoE for swarms). Mid: mage (fast/burst). Then frost (slow), ballista (pierce). Outer: archer.
                CrowdDefense.Data.TowerType pick;
                if (i < 6 && cannon != null) pick = cannon;
                else if (i < 12 && mage != null) pick = mage;
                else if (i < 16 && frost != null) pick = frost;   // N30: slow stragglers in mid-zone
                else if (i < 22 && ballista != null) pick = ballista;
                else if (i < 26 && skyguard != null) pick = skyguard;
                else pick = Archer!;
                if (Pc.TryPlaceAtActiveBuildCell(pick)) extra++;
            }
            Append($"phase9 step10-pre extra={extra} total={Pc!.PlacedTowers.Count} (cannon={cannon!=null} mage={mage!=null} frost={frost!=null} skyguard={skyguard!=null})");

            // N26: Reposition hero on path mid-route before each wave start
            PositionHeroOnPath();
            if (Wm!.IsWaitingForPlayerStart) Wm.StartNextWave();
            for (int i = 0; i < 60; i++) EditorApplication.Step();
            SessionState.SetInt(SsLoop10, 0);
            Append($"phase9 step10-start active={Wm.IsWaveActive} pending={Wm.PendingSpawnCount}");
            NextPhase(10);
        }

        private static void WatchWave2Clear()
        {
            int loop = SessionState.GetInt(SsLoop10, 0) + 1;
            SessionState.SetInt(SsLoop10, loop);
            for (int i = 0; i < 120; i++) EditorApplication.Step();
            if (Wm!.IsWaitingForPlayerStart || CastleI!.IsDead
                || Lr!.State == CrowdDefense.Systems.GameState.Summary
                || Lr.State == CrowdDefense.Systems.GameState.Lost)
            {
                Append($"phase10 step10 idx={Wm.CurrentWaveIdx} state={Lr.State} castleHP={CastleI.HP} kills={Wm.WaveKillCount} loop={loop}");
                SessionState.SetInt(SsIter11, 0);
                NextPhase(11);
            }
            else if (loop > 600)
            {
                int active = Wm.ActiveEnemies?.Count ?? -1;
                int pending = Wm.PendingSpawnCount;
                int spawned = Wm.WaveTotalSpawned;
                int kills = Wm.WaveKillCount;
                Append($"phase10 step10 FAIL stuck loop={loop} active={active} pending={pending} spawned={spawned} kills={kills} castleHP={CastleI.HP}");
                Finish("FAIL wave 2 never cleared");
            }
        }

        private static void DriveRemainingWaves()
        {
            if (CastleI!.IsDead || Lr!.State == CrowdDefense.Systems.GameState.Summary
                || Lr.State == CrowdDefense.Systems.GameState.Lost)
            {
                string result = (Lr.State == CrowdDefense.Systems.GameState.Summary && !CastleI.IsDead)
                    ? "VICTORY PASS"
                    : (CastleI.IsDead || Lr.State == CrowdDefense.Systems.GameState.Lost)
                        ? "DEFEAT castle destroyed"
                        : "INCONCLUSIVE";
                Append($"phase11 FINAL {result} state={Lr.State} idx={Wm!.CurrentWaveIdx}/{Wm.TotalWaves} castleHP={CastleI.HP}/{CastleI.HPMax}");
                Finish("DONE");
                return;
            }

            if (Wm!.IsWaitingForPlayerStart)
            {
                // Upgrade towers and add a few more between waves
                Econ!.AddGold(200000);
                int upgraded = 0;
                foreach (var t in Pc!.PlacedTowers)
                {
                    if (t == null || t.UpgradeLevel >= 3) continue;
                    try
                    {
                        if (t.UpgradeTo(2)) upgraded++;
                        if (t.UpgradeLevel < 3 && t.UpgradeTo(3)) upgraded++;
                    }
                    catch { }
                }
                Append($"phase11 between-waves upgraded={upgraded} towers");
                // N26: Reposition hero on path mid-route before each new wave
                PositionHeroOnPath();
                Append($"phase11 startWave#{Wm.CurrentWaveIdx + 1} (towers={Pc.PlacedTowers.Count} castle={CastleI.HP} hero={(CrowdDefense.Entities.Hero.Current!=null ? CrowdDefense.Entities.Hero.Current.transform.position.ToString("F1") : "null")})");
                Wm.StartNextWave();
            }

            for (int i = 0; i < 240; i++) EditorApplication.Step();
            int iter = SessionState.GetInt(SsIter11, 0) + 1;
            SessionState.SetInt(SsIter11, iter);
            // Diagnostic every 10 iters
            if (iter % 10 == 0)
            {
                Append($"phase11 iter={iter} idx={Wm.CurrentWaveIdx}/{Wm.TotalWaves} state={Lr.State} castleHP={CastleI.HP} active={Wm.ActiveEnemies?.Count} pending={Wm.PendingSpawnCount} kills={Wm.WaveKillCount}");
            }
            if (iter > 500)
            {
                int active = Wm.ActiveEnemies?.Count ?? -1;
                int pending = Wm.PendingSpawnCount;
                Append($"phase11 FAIL too many iterations idx={Wm.CurrentWaveIdx}/{Wm.TotalWaves} state={Lr.State} active={active} pending={pending}");
                Finish("FAIL phase 11 timeout");
            }
        }
    }
}
