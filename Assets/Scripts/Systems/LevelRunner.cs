#nullable enable
using System;
using System.Collections.Generic;
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

        public IReadOnlyList<Castle> Castles => castles;
        public Castle? PrimaryCastle => castles.Count > 0 ? castles[0] : null;

        public int TotalCastleHP => SumHP();
        public int TotalCastleHPMax => SumHPMax();

        public event Action<GameState>? OnStateChanged;
        /// <summary>Fired whenever any castle takes damage. args: (currentTotalHP, maxTotalHP)</summary>
        public event Action<int, int>? OnTotalHPChanged;

        private readonly List<Castle> castles = new();
        private float targetSpeed = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ApplyTimeScale();
        }

        private void Start()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnAllWavesCompleted += OnVictory;

            SpawnCastles();
        }

        private void OnDestroy()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnAllWavesCompleted -= OnVictory;
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

        private void SpawnCastles()
        {
            if (currentLevel == null || PathManager.Instance?.Grid == null) return;

            var grid = PathManager.Instance.Grid;
            int count = grid.Castles.Count;
            if (count == 0) return;

            // Distribute HP evenly across castles (mirrors Phaser loadCastlesFromGrid logic)
            int totalHP = ResolveCastleHP();
            int perCastle = count > 1 ? Mathf.RoundToInt((float)totalHP / count) : totalHP;

            for (int i = 0; i < count; i++)
            {
                Castle castle;
                if (castlePrefab != null)
                {
                    var go = Instantiate(castlePrefab);
                    castle = go.GetComponent<Castle>() ?? go.AddComponent<Castle>();
                }
                else
                {
                    // Fallback : create bare GameObject with Castle component
                    var go = new GameObject($"Castle_{i}");
                    castle = go.AddComponent<Castle>();
                }

                castle.Init(i, perCastle);
                castle.OnCastleDied += OnCastleDied;
                castle.OnHPChanged += (_, _) => OnTotalHPChanged?.Invoke(TotalCastleHP, TotalCastleHPMax);
                castles.Add(castle);
            }

#if UNITY_EDITOR
            Debug.Log($"[LevelRunner] spawned {count} castles totalHP={totalHP} perCastle={perCastle}");
#endif

            // Fire initial HP event so HUD initialises correctly
            OnTotalHPChanged?.Invoke(TotalCastleHP, TotalCastleHPMax);
        }

        private void OnCastleDied(Castle dead)
        {
            var lossMode = currentLevel?.LossMode ?? CastleLossMode.Any;
            bool triggerGameOver = lossMode == CastleLossMode.Any
                || (lossMode == CastleLossMode.All && castles.TrueForAll(c => c.IsDead));

            if (triggerGameOver)
                SetState(GameState.GameOver);

            OnTotalHPChanged?.Invoke(TotalCastleHP, TotalCastleHPMax);
        }

        public int ResolveCastleHP()
        {
            if (currentLevel == null) return 120;
            return currentLevel.CastleHP;
        }

        public Castle? GetCastle(int idx)
        {
            if (idx < 0 || idx >= castles.Count) return null;
            return castles[idx];
        }

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

        private int SumHP()
        {
            int sum = 0;
            foreach (var c in castles) sum += c.HP;
            return sum;
        }

        private int SumHPMax()
        {
            int sum = 0;
            foreach (var c in castles) sum += c.HPMax;
            return sum;
        }

        private void OnVictory() => SetState(GameState.Victory);
    }
}
