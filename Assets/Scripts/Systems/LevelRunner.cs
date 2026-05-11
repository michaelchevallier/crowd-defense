#nullable enable
using System;
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

    public class LevelRunner : MonoSingleton<LevelRunner>
    {
        [SerializeField] private LevelData? currentLevel;
        [SerializeField] private GameObject? castlePrefab;

        [Header("Hero")]
        [SerializeField] private HeroType? heroType;
        [SerializeField] private GameObject? heroPrefab;
        [SerializeField] private bool spawnHero = true;

        public GameState State { get; private set; } = GameState.Lobby;
        public LevelData? CurrentLevel => currentLevel;

        public Castle? PrimaryCastle { get; private set; }
        public Hero?   Hero           { get; private set; }
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

        // ── Speed / pause state ────────────────────────────────────────────────
        private float _targetSpeed = 1f;
        private bool  _paused;

        private Vector3 _castleWorldPos;

        protected override void OnAwakeSingleton()
        {
            if (!string.IsNullOrEmpty(LevelLoader.NextLevelId))
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

            SpawnCastle();
            SpawnHero();
            TryPlayOpeningCutscene();

            if (currentLevel != null && PathManager.Instance?.Grid != null)
            {
                var grid = PathManager.Instance.Grid;
                float halfW = (grid.Width - 1) / 2f * grid.CellSize;
                float halfH = (grid.Height - 1) / 2f * grid.CellSize;
                var bounds = new Bounds(Vector3.zero, new Vector3(halfW * 2f, 100f, halfH * 2f));
                LevelEvents.RaiseLevelStart(currentLevel, bounds);
            }

            TransitionTo(GameState.Lobby);
        }

        private void Update()
        {
            if (IsTerminalState()) return;

            // Accumulate real playtime (unscaled so pause doesn't skew it)
            if (State == GameState.WaveActive || State == GameState.WaveBreak)
                _playtimeAccum += Time.unscaledDeltaTime;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _targetSpeed = Mathf.Approximately(_targetSpeed, 1f) ? 10f : 1f;
                ApplyTimeScale();
#if UNITY_EDITOR
                Debug.Log($"[LevelRunner] speed cheat → {_targetSpeed}x");
#endif
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();

            UpdateHeroInput();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public void SetGameSpeed(int multiplier)
        {
            _targetSpeed = Mathf.Clamp(multiplier, 1, 3);
            ApplyTimeScale();
        }

        public void Pause()
        {
            if (_paused) return;
            _paused = true;
            ApplyTimeScale();
            OnPauseChanged?.Invoke();
        }

        public void Resume()
        {
            if (!_paused) return;
            _paused = false;
            ApplyTimeScale();
            OnPauseChanged?.Invoke();
        }

        public void TogglePause()
        {
            if (_paused) Resume(); else Pause();
        }

        public bool IsPaused => _paused;

        // Dev shortcut: restart from wave 1. Only in WaveActive / WaveBreak.
        public void RestartLevel()
        {
            if (currentLevel == null) return;
            LevelLoader.LoadLevel(currentLevel.Id);
        }

        public int ResolveCastleHP() => currentLevel?.CastleHP ?? 120;

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
            JuiceFX.Instance?.Flash(new Color(1f, 0.84f, 0f, 0.4f), 500);
            JuiceFX.Instance?.SlowMo(0.5f, 1200);

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
            OnLevelLost?.Invoke();
            SaveLostResult();
            EnterSummary(isVictory: false);
        }

        private void EnterSummary(bool isVictory)
        {
            var result = BuildResult(isVictory);
            PersistResult(result);
            // Switch to Summary state (may already be there if called from HandleLostEntry)
            if (State != GameState.Summary) State = GameState.Summary;
            ApplyTimeScale();
            OnSummaryReady?.Invoke(result);
        }

        // ── Wave event handlers ─────────────────────────────────────────────────

        private void HandleWaveStart(int waveIdx)
        {
            if (State == GameState.Lobby || State == GameState.WaveBreak)
                TransitionTo(GameState.WaveActive);
            OnWaveStarted?.Invoke(waveIdx + 1);
        }

        private void HandleWaveCleared(int waveIdx)
        {
            Hero?.OnWaveEnd();
            TransitionTo(GameState.WaveBreak);
            OnWaveEnded?.Invoke(waveIdx + 1);

            int waveNumber = waveIdx + 1;
            if (waveNumber % 5 == 0)
                UI.Toast.Show($"Vague {waveNumber} franchie !", string.Empty, 3000, null);
        }

        private void HandleAllWavesCompleted()
        {
            if (currentLevel != null)
                SaveSystem.MarkLevelCleared(currentLevel.Id);

            TransitionTo(GameState.LevelComplete);
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

            return new LevelResult
            {
                IsVictory         = isVictory,
                StarsEarned       = stars,
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
            // Record defeat for support-mode offer logic (mirrors V5 SaveSystem.incrementDefeat).
            // SaveSystem has no IncrementDefeat yet; stub preserved for future extension.
        }

        // ── Castle / Hero spawning ──────────────────────────────────────────────

        private void SpawnCastle()
        {
            if (currentLevel == null || PathManager.Instance?.Grid == null) return;

            var grid = PathManager.Instance.Grid;
            if (grid.Castles.Count == 0) return;

            var castleCell = grid.Castles[0];
            _castleWorldPos = GridCoords.CellToWorld(castleCell.x, castleCell.y, grid.Width, grid.Height, grid.CellSize);

            int hp = ResolveCastleHP();
            Castle castle;
            if (castlePrefab != null)
            {
                var go = Instantiate(castlePrefab);
                castle = go.GetComponent<Castle>() ?? go.AddComponent<Castle>();
            }
            else
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Castle";
                go.transform.localScale = Vector3.one * 2f;
                Destroy(go.GetComponent<BoxCollider>());
                var rend = go.GetComponent<MeshRenderer>();
                if (rend != null)
                {
                    rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    rend.material.color = new Color(0.4f, 0.25f, 0.1f, 1f);
                }
                castle = go.AddComponent<Castle>();
            }

            castle.Init(hp);
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
            if (!spawnHero || heroType == null) return;

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
            Hero.Init(heroType, spawnPos, maxX, maxZ);

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

#if UNITY_EDITOR
            Debug.Log($"[LevelRunner] spawned hero '{heroType.Id}' at {spawnPos}");
#endif
        }

        // ── Input helpers ───────────────────────────────────────────────────────

        private void TryPlayOpeningCutscene()
        {
            if (currentLevel == null) return;
            string id = currentLevel.CutsceneIdAtStart;
            if (string.IsNullOrEmpty(id)) return;

            var ctrl = UnityEngine.Object.FindFirstObjectByType<CutsceneController>();
            if (ctrl == null) return;

            Time.timeScale = 0f;
            ctrl.Play(id, () => ApplyTimeScale());
        }

        private void UpdateHeroInput()
        {
            if (Hero == null || IsTerminalState() || _paused) return;

            float dx = Input.GetAxisRaw("Horizontal");
            float dz = Input.GetAxisRaw("Vertical");
            Hero.SetMove(dx, dz);

            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            Hero.SetRunning(shiftHeld);

            if (Input.GetKeyDown(KeyCode.B) && _castleWorldPos != Vector3.zero)
                Hero.transform.position = _castleWorldPos + Vector3.up * 0.5f;

            if (Input.GetKeyDown(KeyCode.U))
                Hero.TryUlt();
        }

        private void ApplyTimeScale()
        {
            if (IsTerminalState() || _paused)
                Time.timeScale = 0f;
            else
                Time.timeScale = _targetSpeed;
        }
    }
}
