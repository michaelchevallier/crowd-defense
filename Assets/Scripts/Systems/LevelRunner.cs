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
    public enum GameState { Play, GameOver, Victory }

    public class LevelRunner : MonoSingleton<LevelRunner>
    {
        [SerializeField] private LevelData? currentLevel;
        [SerializeField] private GameObject? castlePrefab;

        [Header("Hero")]
        [SerializeField] private HeroType? heroType;
        [SerializeField] private GameObject? heroPrefab;
        [SerializeField] private bool spawnHero = true;

        public GameState State { get; private set; } = GameState.Play;
        public LevelData? CurrentLevel => currentLevel;

        public Castle? PrimaryCastle { get; private set; }
        public Hero?   Hero           { get; private set; }
        public int TotalCastleHP => PrimaryCastle?.HP ?? 0;
        public int TotalCastleHPMax => PrimaryCastle?.HPMax ?? 0;

        public event Action<GameState>? OnStateChanged;
        public event Action<int, int>? OnTotalHPChanged;
        // Fired on level victory before state transitions — subscribe to show perk picker.
        public event Action? OnLevelComplete;

        private float targetSpeed = 1f;
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
                WaveManager.Instance.OnAllWavesCompleted -= OnVictory;
                WaveManager.Instance.OnWaveCleared      -= OnWaveCleared;
            }
        }

        private void Start()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnAllWavesCompleted += OnVictory;
                WaveManager.Instance.OnWaveCleared      += OnWaveCleared;
            }

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
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                targetSpeed = Mathf.Approximately(targetSpeed, 1f) ? 10f : 1f;
                ApplyTimeScale();
#if UNITY_EDITOR
                Debug.Log($"[LevelRunner] speed cheat → {targetSpeed}x");
#endif
            }

            UpdateHeroInput();
        }

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

        public void SetGameSpeed(int multiplier)
        {
            targetSpeed = Mathf.Clamp(multiplier, 1, 3);
            ApplyTimeScale();
        }

        private void UpdateHeroInput()
        {
            if (Hero == null || State != GameState.Play) return;

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
            castle.OnCastleDied += _ => SetState(GameState.GameOver);
            castle.OnHPChanged += (h, hMax) => OnTotalHPChanged?.Invoke(h, hMax);
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

        public int ResolveCastleHP() => currentLevel?.CastleHP ?? 120;

        public void SetState(GameState s)
        {
            if (State == s) return;
            State = s;
            ApplyTimeScale();
            OnStateChanged?.Invoke(State);
            if (s == GameState.GameOver || s == GameState.Victory)
                LevelEvents.RaiseLevelEnd();
#if UNITY_EDITOR
            Debug.Log($"[LevelRunner] state → {s}");
#endif
        }

        private void ApplyTimeScale()
        {
            Time.timeScale = State == GameState.Play ? targetSpeed : 0f;
        }

        private void OnVictory()
        {
            if (currentLevel != null)
                SaveSystem.MarkLevelCleared(currentLevel.Id);

            AudioController.Instance?.Play("level_up", 1f);
            JuiceFX.Instance?.Flash(new Color(1f, 0.84f, 0f, 0.4f), 500);
            JuiceFX.Instance?.SlowMo(0.5f, 1200);

            OnLevelComplete?.Invoke();
            SetState(GameState.Victory);
        }

        private void OnWaveCleared(int waveIdx)
        {
            Hero?.OnWaveEnd();
            int waveNumber = waveIdx + 1;
            if (waveNumber % 5 == 0)
                UI.Toast.Show($"Wave {waveNumber} cleared!", string.Empty, 3000, null);
        }
    }
}
