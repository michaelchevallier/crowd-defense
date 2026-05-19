#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.UI;
using CrowdDefense.Visual;

namespace CrowdDefense.Systems
{
    // Full level state machine ported from V5 LevelRunner.js:
    // Lobby → WaveActive → WaveBreak → LevelComplete → Summary (win) or Lost (game-over)
    public enum GameState
    {
        Lobby,
        WaveActive,
        WaveBreak,
        LevelComplete,
        Summary,
        Lost
    }

    // Snapshot assembled at end of level for summary display and persistence.
    public sealed class LevelResult
    {
        public bool IsVictory;
        public int StarsEarned;          // 1-3
        public int Score;                // composite score from ScoreCalc
        public int GoldEarned;           // lifetime gold this run level
        public int PerksAcquired;
        public int Kills;
        public int TowersPlaced;
        public float PlaytimeSeconds;
        public int WaveReached;
        public int CastleHPRemaining;
        public int CastleHPMax;
        public string LevelId = "";
        public bool IsFirstClear;
        public int GemsRewarded;
    }

    // Stateless score formula — deterministic, callable from UI, tests, leaderboards.
    public static class ScoreCalc
    {
        public static int ComputeScore(int wavesCleared, int totalWaves, float castleHpPct, float timeSec)
        {
            int baseScore     = wavesCleared * 100;
            float hpBonus     = castleHpPct * 50f;
            float timePenalty = Mathf.Max(0, timeSec - 120) * 0.5f;
            return Mathf.Max(0, (int)(baseScore + hpBonus - timePenalty));
        }
    }

    // V8H FIX: ExecutionOrder -200 (was -50) so LevelRunner.Awake runs BEFORE
    // PathManager (-100). PathManager.Build reads LevelRunner.CurrentLevel as fallback;
    // if LevelRunner Awake hasn't fired yet, CurrentLevel is null → no map mesh spawned.
    [DefaultExecutionOrder(-200)]
    public class LevelRunner : MonoSingleton<LevelRunner>
    {
        [SerializeField] private LevelData? currentLevel;
        [SerializeField] private GameObject? castlePrefab;

        [Header("Hero")]
        [SerializeField] private HeroType? heroType;
        [SerializeField] private GameObject? heroPrefab;
        [SerializeField] private bool spawnHero = true;

        // Set at runtime when a DailyLevelSpec drives this run (no LevelData asset).
        public bool IsDailyRun { get; private set; }
        private DailyLevelSpec? _dailySpec;

        public GameState State { get; private set; } = GameState.Lobby;
        public LevelData? CurrentLevel => currentLevel;

        public Castle?   PrimaryCastle { get; private set; }
        public Hero?     Hero           { get; private set; }
        public HeroType? HeroTypeDef    => heroType;
        public int TotalCastleHP    => PrimaryCastle?.HP    ?? 0;
        public int TotalCastleHPMax => PrimaryCastle?.HPMax ?? 0;

        // ── Events ─────────────────────────────────────────────────────────────
        public event Action<GameState>? OnStateChanged;
        public event Action<int, int>?  OnTotalHPChanged;
        // Fired on level victory before state transitions — subscribe to show perk picker.
        public event Action?            OnLevelComplete;
        // Fired with final result struct once Summary state is entered.
        public event Action<LevelResult>? OnSummaryReady;
        // Fired on each wave start/end for HUD/audio hooks.
        public event Action<int>?       OnWaveStarted;
        public event Action<int>?       OnWaveEnded;
        public event Action?            OnLevelLost;
        public event Action?            OnPauseChanged;

        // ── Run-level tracking ─────────────────────────────────────────────────
        private int   _killsThisLevel;
        private int   _towersPlacedThisLevel;
        private int   _perksThisLevel;
        private float _playtimeAccum;
        private int   _goldEarned;

        public int KillsThisLevel => _killsThisLevel;

        // ── Speed / pause state ────────────────────────────────────────────────
        private float _targetSpeed = 1f;
        private bool  _paused;
        private bool  _autoPaused;

        private Vector3 _castleWorldPos;

        // True while an Endless run is active in this session.
        public bool IsEndlessRun { get; private set; }

        protected override void OnAwakeSingleton()
        {
            if (LevelLoader.NextEndlessSpec != null)
            {
                currentLevel             = LevelLoader.NextEndlessSpec;
                IsEndlessRun             = true;
                LevelLoader.NextEndlessSpec = null;
                LevelLoader.NextLevelId  = null;
                EndlessMode.Instance?.OnRunStarted();
            }
            else if (LevelLoader.NextDailySpec != null)
            {
                _dailySpec  = LevelLoader.NextDailySpec;
                IsDailyRun  = true;
                LevelLoader.NextDailySpec = null;
            }
            else if (!string.IsNullOrEmpty(LevelLoader.NextLevelId))
            {
                var reg = Data.LevelRegistry.Get();
                if (reg != null)
                {
                    var found = reg.FindById(LevelLoader.NextLevelId!);
                    if (found != null) currentLevel = found;
#if UNITY_EDITOR
                    else Debug.LogWarning($"[LevelRunner] LevelLoader.NextLevelId='{LevelLoader.NextLevelId}' not found in LevelRegistry.");
#endif
                }
            }

            // Fallback: if no level loaded and not in an Editor scene, load W1-1 (bootstrap default).
            if (currentLevel == null)
            {
                var reg = Data.LevelRegistry.Get();
                if (reg != null) currentLevel = reg.FindById("W1-1");
            }

            ApplyTimeScale();
        }

        protected override void OnDestroySingleton()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnAllWavesCompleted -= HandleAllWavesCompleted;
                WaveManager.Instance.OnWaveCleared       -= HandleWaveCleared;
                WaveManager.Instance.OnWaveStart         -= HandleWaveStart;
            }

            if (Economy.Instance != null)
                Economy.Instance.OnGoldChanged -= HandleGoldChanged;

            BalanceConfig.ClearRuntimeOverride();
        }

        private void Start()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnAllWavesCompleted += HandleAllWavesCompleted;
                WaveManager.Instance.OnWaveCleared       += HandleWaveCleared;
                WaveManager.Instance.OnWaveStart         += HandleWaveStart;
            }

            if (Economy.Instance != null)
                Economy.Instance.OnGoldChanged += HandleGoldChanged;

            // Wire doctrine modifiers into runtime balance config (all 7 doctrines now have gameplay effect)
            var runtimeBalance = DoctrineSystem.Instance?.BuildRunConfig(BalanceConfig.Get());
            if (runtimeBalance != null && runtimeBalance != BalanceConfig.Get())
            {
                BalanceConfig.SetRuntimeOverride(runtimeBalance);
#if UNITY_EDITOR
                Debug.Log($"[LevelRunner] Applied doctrine: {DoctrineSystem.Instance!.ActiveDoctrine?.displayName}");
#endif
            }

            SpawnCastle();
            SpawnHero();
            UI.HeroPortraitController.Instance?.Wire();
            SpawnTreasureSystem();
            SpawnPathPreview();
            SpawnBuildPoints();
            if (gameObject.GetComponent<EnemyAmbientChatter>() == null)
                gameObject.AddComponent<EnemyAmbientChatter>();
            TryPlayOpeningCutscene();
            UI.TutorialIntroPanel.TryShow(currentLevel?.Id);
            UI.TutorialPopupController.TryShow(currentLevel?.Id);
            UI.TutorialArrowGuide.TryStart(currentLevel?.Id);
            RestoreMidLevelStateIfPending();

            var bounds = default(Bounds);
            var grid = PathManager.Instance?.Grid;
            if (grid != null)
            {
                float halfW = (grid.Width - 1) / 2f * grid.CellSize;
                float halfH = (grid.Height - 1) / 2f * grid.CellSize;
                bounds = new Bounds(Vector3.zero, new Vector3(halfW * 2f, 100f, halfH * 2f));
            }

            SetGameSpeed(1);
            TransitionTo(GameState.Lobby);

            // Defer LevelStart event until next frame so all OnEnable() subscribers (MinimapController, etc.) are wired.
            StartCoroutine(RaiseLevelStartDeferred(currentLevel, bounds));

            // P1-UI-6: briefing modal before waves are available
            if (UI.BriefingModalController.Instance != null && currentLevel != null)
                StartCoroutine(UI.BriefingModalController.Instance.ShowAndCountdown(
                    currentLevel.DisplayName, currentLevel.Briefing));
        }

        private void Update()
        {
            if (IsTerminalState()) return;

            // Accumulate real playtime (unscaled so pause doesn't skew it)
            if (State == GameState.WaveActive || State == GameState.WaveBreak)
            {
                float dt = Time.unscaledDeltaTime;
                _playtimeAccum += dt;
                LifetimeStats.Instance?.AddTime(dt);
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _targetSpeed = Mathf.Approximately(_targetSpeed, 1f) ? 10f : 1f;
                ApplyTimeScale();
#if UNITY_EDITOR
                Debug.Log($"[LevelRunner] speed cheat → {_targetSpeed}x");
#endif
            }

            if (Input.GetKeyDown(KeyBindings.GetKey("pause")) || Input.GetKeyDown(KeyBindings.GetKey("pause_alt")))
            {
                if (IsAnyModalOpen())
                    CloseTopModal();
                else
                    TogglePause();
            }

            UpdateHeroInput();
            EnemyPathingSystem.Instance?.Tick();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        // index 0=0.5x 1=1x 2=2x 3=3x (matches SpeedControlController._speeds)
        private static readonly float[] SpeedTable = { 0.5f, 1f, 2f, 3f };

        public void SetGameSpeed(int index)
        {
            _targetSpeed = SpeedTable[Mathf.Clamp(index, 0, SpeedTable.Length - 1)];
            ApplyTimeScale();
        }

        public void Pause()
        {
            if (_paused) return;
            _paused = true;
            ApplyTimeScale();
            OnPauseChanged?.Invoke();
            int waveIdx = (WaveManager.Instance?.CurrentWaveIdx ?? 1) - 1;
            if (waveIdx >= 0) SnapshotMidLevel(waveIdx);
        }

        public void Resume()
        {
            if (!_paused) return;
            _paused = false;
            _autoPaused = false;
            ApplyTimeScale();
            OnPauseChanged?.Invoke();
        }

        public void TogglePause()
        {
            if (_paused) Resume(); else Pause();
        }

        public bool IsPaused => _paused;

        private void OnApplicationFocus(bool hasFocus)
        {
            if (IsTerminalState()) return;
            bool autoPauseEnabled = UI.SettingsRegistry.Instance?.AutoPauseOnBlur ?? true;
            if (!autoPauseEnabled) return;

            if (!hasFocus && !_paused)
            {
                Pause();
                _autoPaused = true;
            }
            else if (hasFocus && _autoPaused)
            {
                _autoPaused = false;
                Resume();
            }
        }

        // Dev shortcut: restart from wave 1. Only in WaveActive / WaveBreak.
        public void RestartLevel()
        {
            if (currentLevel == null) return;
            LevelLoader.LoadLevel(currentLevel.Id);
        }

        public int ResolveCastleHP() =>
            _dailySpec != null
                ? _dailySpec.CastleHP
                : currentLevel?.CastleHP ?? BalanceConfig.GetCastleMaxHp(1, 1);

        // Called externally (e.g. perk picker done) to proceed from LevelComplete → Summary.
        public void ConfirmLevelComplete()
        {
            if (State != GameState.LevelComplete) return;
            EnterSummary(isVictory: true);
        }

        // Record enemy kill (called from Enemy death callback or EnemyPool).
        public void NotifyEnemyKilled() => _killsThisLevel++;

        // Record tower placed (called from PlacementController).
        public void NotifyTowerPlaced() => _towersPlacedThisLevel++;

        // Record perk acquired (called from PerkPickerController on selection).
        public void NotifyPerkAcquired() => _perksThisLevel++;

        // ── Legacy compat state setter (some UI still calls SetState(GameState.GameOver)) ──
        [Obsolete("Use TransitionTo(GameState) for new code.")]
        public void SetState(GameState s) => TransitionTo(s);

        // ── Internal state machine ──────────────────────────────────────────────

        private void TransitionTo(GameState next)
        {
            if (State == next) return;
            State = next;
            ApplyTimeScale();
            OnStateChanged?.Invoke(State);

#if UNITY_EDITOR
            Debug.Log($"[LevelRunner] → {next}");
#endif

            switch (next)
            {
                case GameState.Lost:
                    HandleLostEntry();
                    break;
                case GameState.LevelComplete:
                    HandleLevelCompleteEntry();
                    break;
                case GameState.Summary:
                    // EnterSummary handles the rest; don't double-fire here.
                    break;
            }

            if (next == GameState.Lost || next == GameState.Summary)
                LevelEvents.RaiseLevelEnd();
        }

        private bool IsTerminalState() =>
            State == GameState.Lost || State == GameState.Summary;

        private void HandleLevelCompleteEntry()
        {
            AudioController.Instance?.Play("level_up", 1f);
            var jcVictory = JuiceConfig.Get();
            JuiceFX.Instance?.Flash(
                new Color(1f, 0.84f, 0f, jcVictory.VictoryFlashAlpha),
                jcVictory.VictoryFlashMs);
            JuiceFX.Instance?.Shake(jcVictory.VictoryShakeAmp, jcVictory.VictoryFlashMs);
            JuiceFX.Instance?.SlowMo(0.5f, 1200);

            bool isWorldEnd = currentLevel?.Level == 1; // Level 1-of-world = last level (world gate)
            float confettiMul = isWorldEnd ? 4f : 1f;
            VfxPool.Instance?.SpawnConfetti(_castleWorldPos, confettiMul);

            string worldAchId = $"world{currentLevel?.World ?? 1}_complete";
            Achievements.Instance?.Unlock(worldAchId);

            // Victory banner floats up from castle position (scale+fade 2 s).
            // N43c: explicit Unity != null check
            var pc = PrimaryCastle;
            if (pc != null) pc.SpawnVictoryBanner();

            // PerkPickerController subscribes to OnLevelComplete and calls ConfirmLevelComplete() when done.
            OnLevelComplete?.Invoke();

            // If nothing is listening (no perk picker in scene), go straight to summary.
            if (OnLevelComplete == null || OnLevelComplete.GetInvocationList().Length == 0)
                EnterSummary(isVictory: true);
        }

        private void HandleLostEntry()
        {
            AudioController.Instance?.Play("castle_lost", 1f);
            JuiceFX.Instance?.Flash(new Color(1f, 0.1f, 0.1f, 0.5f), 600);
            JuiceFX.Instance?.SlowMo(0.3f, 1500);
            // N43b: explicit Unity != null check (Hero is a UnityEngine.Object)
            var heroCurr = Hero.Current;
            if (heroCurr != null) heroCurr.TriggerDeathCinematic();
            OnLevelLost?.Invoke();
            SaveLostResult();
            StartCoroutine(DelayedEnterSummary(3f));
        }

        private IEnumerator DelayedEnterSummary(float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
            EnterSummary(isVictory: false);
        }

        private void EnterSummary(bool isVictory)
        {
            SaveSystem.ClearMidLevelState();
            var result = BuildResult(isVictory);
            PersistResult(result);
            // Switch to Summary state (may already be there if called from HandleLostEntry)
            if (State != GameState.Summary) State = GameState.Summary;
            ApplyTimeScale();
            OnSummaryReady?.Invoke(result);

            // If RunSummaryController is wired in scene, it consumes OnSummaryReady and stays
            // visible in the Main scene until the player clicks Continue/Rejouer (which then
            // navigates explicitly). Skip the auto-fade-to-Loader fallback so the modal isn't
            // destroyed before the player can read it.
            bool hasRunSummary = UnityEngine.Object.FindAnyObjectByType<UI.RunSummaryController>() != null;

            if (hasRunSummary) return;

            if (isVictory)
            {
                UI.EndScreenController.Instance?.ShowVictory(result);
                LevelLoader.FadeVictory("Loader");
            }
            else
            {
                UI.EndScreenController.Instance?.ShowDefeat(result);
                LevelLoader.FadeDefeat("Loader");
            }
        }

        // ── Wave event handlers ─────────────────────────────────────────────────

        private void HandleWaveStart(int waveIdx)
        {
            if (State == GameState.Lobby || State == GameState.WaveBreak)
                TransitionTo(GameState.WaveActive);
            OnWaveStarted?.Invoke(waveIdx + 1);
            WaveHistoryLog.Instance?.Log("wave", $"Vague {waveIdx + 1} lancee");

            int total = WaveManager.Instance?.TotalWaves is > 0 ? WaveManager.Instance.TotalWaves : 10;
            int intensity = Mathf.Clamp(waveIdx * 3 / total, 0, 2);
            MusicManager.Instance?.SetIntensity(intensity);
        }

        private void HandleWaveCleared(int waveIdx)
        {
            // N41: explicit Unity != null check (?. doesn't use Unity's overloaded ==)
            if (Hero != null) Hero.OnWaveEnd();
            TransitionTo(GameState.WaveBreak);
            OnWaveEnded?.Invoke(waveIdx + 1);
            TalentSystem.EarnTalentPoint(1);

            int waveNumber = waveIdx + 1;
            if (IsEndlessRun)
                EndlessMode.Instance?.NotifyWaveReached(waveNumber);

            if (waveNumber % 5 == 0)
                Toast.Show($"Vague {waveNumber} franchie !", string.Empty, 3000, null, ToastType.Generic);

            SnapshotMidLevel(waveIdx);
        }

        private void RestoreMidLevelStateIfPending()
        {
            var mid = SaveSystem.LoadRunState();
            if (mid == null || mid.levelId != (currentLevel?.Id ?? "")) return;

            if (Economy.Instance != null)
            {
                int delta = mid.gold - Economy.Instance.Gold;
                if (delta > 0)      Economy.Instance.AddGold(delta);
                else if (delta < 0) Economy.Instance.TrySpend(-delta);
            }

            // Reduce castle HP to saved value via simulated damage (avoids SetHP dependency).
            if (PrimaryCastle != null && mid.castleHP > 0 && mid.castleHP < PrimaryCastle.HP)
            {
                int dmg = PrimaryCastle.HP - mid.castleHP;
                PrimaryCastle.TakeDamage(dmg);
            }

            PlacementController.Instance?.RestoreTowers(mid.towers);

            // Restore hero level + XP from mid-level snapshot (overrides RunState defaults).
            if (Hero != null && mid.heroLevel > 1)
                Hero.ApplyRunContext(mid.heroPerks, mid.heroLevel, mid.heroXP);

            // Synergy snapshot is informational; Synergies re-evaluates from placed towers automatically.
            // synergyActiveIds preserved in DTO for future validation or display in restore toast.

            // Restore wave progress: sync WaveManager to saved waveIdx (B-WAVE-RESTORE).
            if (WaveManager.Instance != null && mid.waveIdx > 0)
            {
                WaveManager.Instance.SkipToWave(mid.waveIdx);
            }

#if UNITY_EDITOR
            Debug.Log($"[LevelRunner] mid-level restore: level={mid.levelId} wave={mid.waveIdx} gold={mid.gold} hp={mid.castleHP} towers={mid.towers?.Count ?? 0} heroLv={mid.heroLevel} synergies={mid.synergyActiveIds?.Count ?? 0}");
#endif
        }

        private void SnapshotMidLevel(int waveIdx)
        {
            var rs    = SaveSystem.GetRunState();
            int score = ScoreCalc.ComputeScore(
                waveIdx + 1,
                WaveManager.Instance?.TotalWaves is > 0 ? WaveManager.Instance.TotalWaves : 10,
                TotalCastleHPMax > 0 ? (float)TotalCastleHP / TotalCastleHPMax : 0f,
                _playtimeAccum);

            var data = new MidLevelStateData
            {
                levelId         = currentLevel?.Id ?? "",
                waveIdx         = waveIdx + 1,
                currentSpawnIdx = 0,
                gold            = Economy.Instance?.Gold ?? 0,
                score           = score,
                castleHP        = TotalCastleHP,
                heroLevel       = Hero?.Level ?? rs.heroLevel,
                heroXP          = Hero?.Xp    ?? rs.heroXP,
                heroPerks       = new System.Collections.Generic.List<string>(rs.heroPerks),
            };

            var placed = PlacementController.Instance?.PlacedTowers;
            if (placed != null)
            {
                foreach (var t in placed)
                {
                    if (t == null || t.Config == null) continue;
                    var pos = t.transform.position;
                    data.towers.Add(new PlacedTowerEntry
                    {
                        typeId = t.Config.Id,
                        posX   = pos.x,
                        posY   = pos.y,
                        posZ   = pos.z,
                        level  = t.UpgradeLevel,
                        branch = t.UpgradeBranch.ToString(),
                    });
                }
            }

            var badges = Synergies.Instance?.ActiveBadges;
            if (badges != null)
                foreach (var b in badges)
                    data.synergyActiveIds.Add(b.TowerId);

            SaveSystem.SaveRunState(data);
        }

        private void HandleAllWavesCompleted()
        {
            if (BossRushMode.Instance != null && BossRushMode.Instance.IsActive)
            {
                BossRushMode.Instance.OnBossDefeated(PrimaryCastle?.HP ?? 0);
                return;
            }

            if (IsDailyRun)
            {
                int score = _killsThisLevel * 10 + (WaveManager.Instance?.TotalWaves ?? 5) * 100;
                Daily.SetScore(score);
                DailyChallenge.Instance?.MarkCompleted();
                DailyChallenge.Instance?.OnDailyVictory();
            }
            else if (currentLevel != null)
            {
                SaveSystem.MarkLevelCleared(currentLevel.Id);
                if (currentLevel.World == 10 && currentLevel.Level == 10)
                    SaveSystem.UnlockHardcore();
            }

            LifetimeStats.Instance?.AddLevelWon();
            ClearDefeatStreak(currentLevel?.Id ?? "");

            if (Hero != null)
                RunContext.Instance?.SnapshotHero(Hero);

            TryPlayWorldCutscene(() => TransitionTo(GameState.LevelComplete));
        }

        private void TryPlayWorldCutscene(Action onDone)
        {
            if (currentLevel == null || currentLevel.Level != 1)
            {
                onDone();
                return;
            }

            var ctrl = UnityEngine.Object.FindAnyObjectByType<UI.CutsceneController>();
            if (ctrl == null)
            {
                onDone();
                return;
            }

            string cutsceneId = $"world{currentLevel.World}";
            var reg = Data.CutsceneRegistry.Get();
            if (reg == null || reg.FindById(cutsceneId) == null)
            {
                onDone();
                return;
            }

            Time.timeScale = 0f;
            ctrl.Play(cutsceneId, () =>
            {
                ApplyTimeScale();
                onDone();
            });
        }

        // ── Economy tracking ────────────────────────────────────────────────────

        private int _lastKnownGold;

        private void HandleGoldChanged(int newGold)
        {
            int delta = newGold - _lastKnownGold;
            if (delta > 0) _goldEarned += delta;
            _lastKnownGold = newGold;
        }

        // ── Score / result building ─────────────────────────────────────────────

        private LevelResult BuildResult(bool isVictory)
        {
            int hpNow  = TotalCastleHP;
            int hpMax  = TotalCastleHPMax;
            float hpRatio = hpMax > 0 ? (float)hpNow / hpMax : 0f;
            int stars = isVictory
                ? (hpRatio >= 0.8f ? 3 : hpRatio >= 0.5f ? 2 : 1)
                : 0;

            string levelId = currentLevel?.Id ?? "";
            bool firstClear = isVictory && !SaveSystem.IsLevelCleared(levelId);
            int gems = isVictory ? SaveSystem.ComputeGemReward(levelId, stars, firstClear) : 0;

            var rs = SaveSystem.GetRunState();

            int wavesCleared = isVictory
                ? (WaveManager.Instance?.TotalWaves ?? 0)
                : Mathf.Max(0, (WaveManager.Instance?.WaveDisplayNumber ?? 1) - 1);
            int totalWaves = WaveManager.Instance?.TotalWaves is > 0 ? WaveManager.Instance.TotalWaves : 10;

            return new LevelResult
            {
                IsVictory         = isVictory,
                StarsEarned       = stars,
                Score             = ScoreCalc.ComputeScore(wavesCleared, totalWaves, hpRatio, _playtimeAccum),
                GoldEarned        = _goldEarned,
                PerksAcquired     = _perksThisLevel + (rs?.runPerksAcquired ?? 0),
                Kills             = _killsThisLevel,
                TowersPlaced      = _towersPlacedThisLevel,
                PlaytimeSeconds   = _playtimeAccum,
                WaveReached       = WaveManager.Instance?.WaveDisplayNumber ?? 1,
                CastleHPRemaining = hpNow,
                CastleHPMax       = hpMax,
                LevelId           = levelId,
                IsFirstClear      = firstClear,
                GemsRewarded      = gems
            };
        }

        private void PersistResult(LevelResult r)
        {
            if (r.IsVictory)
            {
                SaveSystem.SetStars(r.LevelId, r.StarsEarned);
                if (r.GemsRewarded > 0) SaveSystem.AddGems(r.GemsRewarded);
                SaveSystem.AddLevelsCompleted(1);
                SaveSystem.AddStarsEarned(r.StarsEarned);
                HighScores.Instance?.Record(r.LevelId, r.PlaytimeSeconds, r.WaveReached, r.Kills);
            }

            SaveSystem.AddKills(r.Kills);
            SaveSystem.AddGoldEarned(r.GoldEarned);
            SaveSystem.AddTowersPlaced(r.TowersPlaced);
            SaveSystem.AddPerksAcquired(r.PerksAcquired);
            SaveSystem.AddPlaytime(r.PlaytimeSeconds);
            SaveSystem.AddWavesCleared(r.IsVictory
                ? (WaveManager.Instance?.TotalWaves ?? 0)
                : Mathf.Max(0, (WaveManager.Instance?.WaveDisplayNumber ?? 1) - 1));
        }

        private void SaveLostResult()
        {
            string levelId = currentLevel?.Id ?? "";
            if (string.IsNullOrEmpty(levelId)) return;

            var streakKey   = $"defeats_streak_{levelId}";
            var proposedKey = $"support_proposed_{levelId}";
            int count       = PlayerPrefs.GetInt(streakKey, 0) + 1;
            PlayerPrefs.SetInt(streakKey, count);
            PlayerPrefs.Save();

            if (count >= 2 && !SupportMode.IsActive && !PlayerPrefs.HasKey(proposedKey))
            {
                PlayerPrefs.SetInt(proposedKey, 1);
                PlayerPrefs.Save();
                // Deferred so the dialog appears after the defeat animation finishes.
                StartCoroutine(ShowSupportDialogDeferred(levelId, 3.2f));
            }
        }

        private System.Collections.IEnumerator ShowSupportDialogDeferred(string levelId, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            UI.SupportProposalDialog.Instance?.Show(levelId);
        }

        private void ClearDefeatStreak(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return;
            PlayerPrefs.DeleteKey($"defeats_streak_{levelId}");
            PlayerPrefs.Save();
        }

        // ── Castle / Hero spawning ──────────────────────────────────────────────

        private void SpawnCastle()
        {
            var grid = PathManager.Instance?.Grid;
            if (currentLevel == null || grid == null) return;
            if (grid.Castles.Count == 0) return;

            var castleCell = grid.Castles[0];
            _castleWorldPos = GridCoords.CellToWorld(castleCell.x, castleCell.y, grid.Width, grid.Height, grid.CellSize);

            int hp = ResolveCastleHP();
            Castle castle;
            GameObject castleGo;
            if (castlePrefab != null)
            {
                castleGo = Instantiate(castlePrefab);
                castle = castleGo.GetComponent<Castle>() ?? castleGo.AddComponent<Castle>();
            }
            else
            {
                castleGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                castleGo.name = "Castle";
                castleGo.transform.localScale = Vector3.one * 2f;
                Destroy(castleGo.GetComponent<BoxCollider>());
                var rend = castleGo.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    rend.material.color = new Color(0.4f, 0.25f, 0.1f, 1f);
                }
                castle = castleGo.AddComponent<Castle>();
            }

            castleGo.transform.position = _castleWorldPos;

            int world = currentLevel?.World ?? 1;
            castle.Init(hp, world);
            castle.OnCastleDied += _ => TransitionTo(GameState.Lost);
            castle.OnHPChanged  += (h, hMax) => OnTotalHPChanged?.Invoke(h, hMax);
            PrimaryCastle = castle;

#if UNITY_EDITOR
            Debug.Log($"[LevelRunner] spawned castle hp={hp} at {_castleWorldPos}");
#endif

            OnTotalHPChanged?.Invoke(TotalCastleHP, TotalCastleHPMax);
        }

        private void SpawnHero()
        {
            var resolvedHeroType = RunContext.GetSelectedHero() ?? heroType;
            if (!spawnHero || resolvedHeroType == null) return;

            var grid = PathManager.Instance?.Grid;
            float maxX = grid != null ? grid.Width  * grid.CellSize * 0.5f : 29.5f;
            float maxZ = grid != null ? grid.Height * grid.CellSize * 0.5f : 29.5f;

            Vector3 spawnPos = _castleWorldPos != Vector3.zero
                ? _castleWorldPos + new Vector3(1.5f, 0f, 0f)
                : Vector3.zero;

            GameObject heroGo;
            if (heroPrefab != null)
                heroGo = Instantiate(heroPrefab, spawnPos, Quaternion.identity);
            else
            {
                heroGo = new GameObject("Hero");
                heroGo.transform.position = spawnPos;
            }

            Hero = heroGo.GetComponent<Hero>() ?? heroGo.AddComponent<Hero>();
            Hero.Init(resolvedHeroType, spawnPos, maxX, maxZ);

            Hero.OnLevelUp += (_, _, _) =>
                VfxPool.Instance?.SpawnLevelUp(Hero.transform.position + Vector3.up * 1.2f);

            UI.PerkChoiceOverlay.EnsureInstance();

            var rs = SaveSystem.GetRunState();
            Hero.ApplyRunContext(
                rs?.heroPerks ?? new List<string>(),
                rs?.heroLevel ?? 1,
                rs?.heroXP    ?? 0);

            if (rs?.schoolId is { Length: > 0 } sid)
                PerkSystem.Instance?.ApplyFreeSetBonus(Hero, sid);

            if (PrimaryCastle != null && Hero.CastleHPMaxMul > 1f)
            {
                int bonus = Mathf.RoundToInt(PrimaryCastle.HPMax * (Hero.CastleHPMaxMul - 1f));
                PrimaryCastle.GrantBonusHP(bonus);
            }

            // Apply active hero skin at spawn (visual swap + stat bonuses)
            SkinSystem.Instance?.ApplyToHero(Hero);

            // Apply meta-upgrades bonuses (damage, range, fire-rate multipliers)
            var metaBonuses = MetaUpgradeSystem.Instance?.ActiveBonuses;
            if (metaBonuses != null)
                Hero.ApplyMetaBonuses(
                    heroDamageMul: metaBonuses.heroDamageMul,
                    heroRangeMul: metaBonuses.heroRangeMul,
                    heroFireRateMul: metaBonuses.heroFireRateMul);

            // Wire camera to follow the freshly spawned hero (and orbit around castle if present).
            var camCtrl = CrowdDefense.Visual.CameraController.Instance;
            if (camCtrl != null)
            {
                camCtrl.SetHero(Hero.transform);
                if (PrimaryCastle != null) camCtrl.SetCastle(PrimaryCastle.transform);

                var grid2 = PathManager.Instance?.Grid;
                if (grid2 != null)
                {
                    float halfX = grid2.Width  * grid2.CellSize * 0.5f;
                    float halfZ = grid2.Height * grid2.CellSize * 0.5f;
                    camCtrl.SetMapBounds(halfX, halfZ);
                }

                camCtrl.FollowHero = true;
            }

#if UNITY_EDITOR
            Debug.Log($"[LevelRunner] spawned hero '{resolvedHeroType.Id}' at {spawnPos}");
#endif
        }

        private void SpawnTreasureSystem()
        {
            var grid = PathManager.Instance?.Grid;
            if (grid == null || grid.Treasures.Count == 0) return;

            var go = new GameObject("TreasureSpawner");
            go.AddComponent<TreasureSpawner>();
        }

        private void SpawnPathPreview()
        {
            var go = new GameObject("PathPreviewRenderer");
            go.AddComponent<Visual.PathPreviewRenderer>();
        }

        private void SpawnBuildPoints()
        {
            var grid = PathManager.Instance?.Grid;
            if (grid == null) return;
            var buildPointPrefab = Resources.Load<GameObject>("Prefabs/BuildPoint");
            if (buildPointPrefab == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[LevelRunner] BuildPoint prefab not found at Resources/Prefabs/BuildPoint");
#endif
                return;
            }
            for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                if (!grid.IsBuildable(x, y)) continue;
                var pos = GridCoords.CellToWorld(x, y, grid.Width, grid.Height, grid.CellSize);
                pos.y = 0.1f;
                var bpGo = Instantiate(buildPointPrefab, pos, Quaternion.identity);
                bpGo.GetComponent<Entities.BuildPoint>()?.Init(new Vector2Int(x, y));
            }
        }

        // ── Input helpers ───────────────────────────────────────────────────────

        private void TryPlayOpeningCutscene()
        {
            if (currentLevel == null) return;
            string id = currentLevel.CutsceneIdAtStart;
            if (string.IsNullOrEmpty(id)) return;

            var ctrl = UnityEngine.Object.FindAnyObjectByType<CutsceneController>();
            if (ctrl == null) return;

            Time.timeScale = 0f;
            ctrl.Play(id, () => ApplyTimeScale());
        }

        private void UpdateHeroInput()
        {
            if (Hero == null || IsTerminalState() || _paused) return;

            float dx = Input.GetAxisRaw("Horizontal");
            float dz = Input.GetAxisRaw("Vertical");
            bool moving = dx * dx + dz * dz > 0.01f;

            Hero.SetMove(dx, dz);

            // Notify BluePill of movement so it can cancel the channel.
            if (moving) BluePill.Instance?.NotifyHeroMoved();

            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            Hero.SetRunning(shiftHeld);

            // B key: start BluePill channel (teleport to castle after 2s stationary).
            if (Input.GetKeyDown(KeyCode.B))
                BluePill.Instance?.StartChannel(Hero.transform);

            if (Input.GetKeyDown(KeyCode.U))
                Hero.TryUlt();
        }

        private static bool IsAnyModalOpen() =>
            (UI.SettingsPanelController.Instance?.IsOpen    ?? false) ||
            (UI.AchievementsPanel.Instance?.IsOpen          ?? false) ||
            (UI.BestiaryPanel.Instance?.IsOpen              ?? false) ||
            (UI.StatsLifetimePanel.Instance?.IsOpen         ?? false);

        private static void CloseTopModal()
        {
            if (UI.SettingsPanelController.Instance?.IsOpen  ?? false) { UI.SettingsPanelController.Instance!.Hide(); return; }
            if (UI.AchievementsPanel.Instance?.IsOpen        ?? false) { UI.AchievementsPanel.Instance!.Hide();        return; }
            if (UI.BestiaryPanel.Instance?.IsOpen            ?? false) { UI.BestiaryPanel.Instance!.Hide();            return; }
            if (UI.StatsLifetimePanel.Instance?.IsOpen       ?? false) { UI.StatsLifetimePanel.Instance!.Hide();       return; }
        }

        private void ApplyTimeScale()
        {
            if (IsTerminalState() || _paused)
                Time.timeScale = 0f;
            else
                Time.timeScale = _targetSpeed;
        }

        private System.Collections.IEnumerator RaiseLevelStartDeferred(Data.LevelData? level, Bounds bounds)
        {
            yield return null;
            if (level != null)
                LevelEvents.RaiseLevelStart(level, bounds);
        }
    }
}
