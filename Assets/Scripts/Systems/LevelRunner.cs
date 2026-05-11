#nullable enable
using System;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    public enum GameState { Play, GameOver, Victory }

    public class LevelRunner : MonoBehaviour
    {
        public static LevelRunner? Instance { get; private set; }

        [SerializeField] private LevelData? currentLevel;

        public GameState State { get; private set; } = GameState.Play;
        public LevelData? CurrentLevel => currentLevel;
        public event Action<GameState>? OnStateChanged;

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

        private void OnVictory() => SetState(GameState.Victory);
    }
}
