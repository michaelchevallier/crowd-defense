#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager? Instance { get; private set; }

        [SerializeField] private LevelData? levelData;
        [SerializeField] private GameObject? enemyPrefab;

        private int currentWaveIdx = 0;
        private float spawnTimerMs = 0f;
        private float breakTimerMs = 0f;
        private bool waveActive = false;
        private Queue<EnemyType> pendingSpawns = new();
        private List<Enemy> activeEnemies = new();

        public IReadOnlyList<Enemy> ActiveEnemies => activeEnemies;
        public int CurrentWaveIdx => currentWaveIdx;
        public int WaveDisplayNumber => currentWaveIdx + 1;
        public int TotalWaves => levelData?.Waves.Count ?? 0;

        public event Action<int>? OnWaveStart;
        public event Action<int>? OnWaveCleared;
        public event Action? OnAllWavesCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (levelData == null || levelData.Waves.Count == 0)
            {
                Debug.LogError("[WaveManager] No LevelData or no waves");
                return;
            }
            BeginWave(0);
        }

        private void BeginWave(int idx)
        {
            currentWaveIdx = idx;
            var wave = levelData!.Waves[idx];
            var list = new List<EnemyType>();
            foreach (var entry in wave.entries)
            {
                if (entry.type == null) continue;
                for (int i = 0; i < entry.count; i++) list.Add(entry.type);
            }
            // Fisher-Yates
            var rng = new System.Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            pendingSpawns = new Queue<EnemyType>(list);
            spawnTimerMs = 0f;
            waveActive = true;
#if UNITY_EDITOR
            Debug.Log($"[WaveManager] Wave {idx + 1}/{TotalWaves} start : {list.Count} enemies, spawnRate {wave.spawnRateMs}ms");
#endif
            OnWaveStart?.Invoke(idx);
        }

        private void Update()
        {
            if (levelData == null) return;
            float dtMs = Time.deltaTime * 1000f;

            if (waveActive)
            {
                spawnTimerMs += dtMs;
                var wave = levelData.Waves[currentWaveIdx];
                if (spawnTimerMs >= wave.spawnRateMs && pendingSpawns.Count > 0)
                {
                    spawnTimerMs = 0f;
                    SpawnEnemy(pendingSpawns.Dequeue());
                }
                if (pendingSpawns.Count == 0 && activeEnemies.Count == 0)
                {
                    waveActive = false;
                    breakTimerMs = 0f;
#if UNITY_EDITOR
                    Debug.Log($"[WaveManager] Wave {currentWaveIdx + 1} cleared");
#endif
                    OnWaveCleared?.Invoke(currentWaveIdx);
                }
            }
            else
            {
                breakTimerMs += dtMs;
                int breakMs = levelData.Waves[currentWaveIdx].breakMs;
                if (breakMs <= 0) breakMs = 4000;
                if (breakTimerMs >= breakMs)
                {
                    if (currentWaveIdx + 1 < levelData.Waves.Count)
                    {
                        BeginWave(currentWaveIdx + 1);
                    }
                    else
                    {
#if UNITY_EDITOR
                        Debug.Log("[WaveManager] All waves completed — victory");
#endif
                        OnAllWavesCompleted?.Invoke();
                        enabled = false;
                    }
                }
            }
        }

        private void SpawnEnemy(EnemyType type)
        {
            if (enemyPrefab == null || PathManager.Instance == null) return;
            Vector3 spawnPos = PathManager.Instance.GetWaypoint(0) + Vector3.up * 0.5f;
            var go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            var enemy = go.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Init(type);
                activeEnemies.Add(enemy);
            }
        }

        public void NotifyEnemyDied(Enemy e) => activeEnemies.Remove(e);
    }
}
