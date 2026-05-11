#nullable enable
using System;
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    public enum GameState { Play, GameOver, Victory }

    public class LevelRunner : MonoBehaviour
    {
        public static LevelRunner? Instance { get; private set; }

        [SerializeField] private LevelData? currentLevel;
        [SerializeField] private GameObject? castlePrefab;

        public GameState State { get; private set; } = GameState.Play;
        public LevelData? CurrentLevel => currentLevel;

        public Castle? PrimaryCastle { get; private set; }
        public int TotalCastleHP => PrimaryCastle?.HP ?? 0;
        public int TotalCastleHPMax => PrimaryCastle?.HPMax ?? 0;

        public event Action<GameState>? OnStateChanged;
        public event Action<int, int>? OnTotalHPChanged;

        private float targetSpeed = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ApplyTimeScale();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnAllWavesCompleted -= OnVictory;
        }

        private void Start()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnAllWavesCompleted += OnVictory;

            SpawnCastle();
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
        }

        private void SpawnCastle()
        {
            if (currentLevel == null || PathManager.Instance?.Grid == null) return;

            var grid = PathManager.Instance.Grid;
            if (grid.Castles.Count == 0) return;

            int hp = ResolveCastleHP();
            Castle castle;
            if (castlePrefab != null)
            {
                var go = Instantiate(castlePrefab);
                castle = go.GetComponent<Castle>() ?? go.AddComponent<Castle>();
            }
            else
            {
                var go = new GameObject("Castle");
                castle = go.AddComponent<Castle>();
            }

            castle.Init(hp);
            castle.OnCastleDied += _ => SetState(GameState.GameOver);
            castle.OnHPChanged += (h, hMax) => OnTotalHPChanged?.Invoke(h, hMax);
            PrimaryCastle = castle;

#if UNITY_EDITOR
            Debug.Log($"[LevelRunner] spawned castle hp={hp}");
#endif

            OnTotalHPChanged?.Invoke(TotalCastleHP, TotalCastleHPMax);
        }

        public int ResolveCastleHP() => currentLevel?.CastleHP ?? 120;

        public void SetState(GameState s)
        {
            if (State == s) return;
            State = s;
            ApplyTimeScale();
            OnStateChanged?.Invoke(State);
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
            SetState(GameState.Victory);
        }
    }
}
