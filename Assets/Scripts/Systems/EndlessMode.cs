#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    [Serializable]
    public class EndlessRecord
    {
        public int    wave;
        public string date = "";
    }

    [Serializable]
    internal class EndlessRecordStore
    {
        public List<EndlessRecord> records = new();
    }

    /// <summary>
    /// Endless mode: generates a procedural LevelData with 40 seed waves, then appends more
    /// on demand. HP and Damage scale *= 1.15^waveIndex. Best wave record saved via HighScores.
    /// Top-10 wave history saved via PlayerPrefs key cd.endless.records.
    /// </summary>
    public class EndlessMode : MonoSingleton<EndlessMode>
    {
        public const string LevelId = "endless";
        // Scaling regimes: W0-29 base exponential, W30-49 +10% HP /+8% dmg per wave, W50+ +15%/+12%
        public const float ScaleMulHpW30  = 1.10f;
        public const float ScaleMulDmgW30 = 1.08f;
        public const float ScaleMulHpW50  = 1.15f;
        public const float ScaleMulDmgW50 = 1.12f;
        public const int   WaveThresholdMid  = 30;
        public const int   WaveThresholdHard = 50;
        private const int SeedWaveCount = 40;
        private const int BaseSpawnRateMs = 900;
        private const string RecordsKey = "cd.endless.records";
        private const int MaxRecords = 10;

        [SerializeField] private EnemyRegistry? enemyRegistry;

        // Static flag: survives scene transitions because MonoSingleton is recreated.
        private static bool _pendingRun;

        public bool IsActive { get; private set; }

        private int _bestWave;
        private EnemyType[] _pool = Array.Empty<EnemyType>();
        private List<EndlessRecord> _records = new();

        protected override void OnAwakeSingleton()
        {
            var hs = HighScores.Instance?.GetHighScore(LevelId);
            _bestWave = hs?.maxWaveReached ?? 0;
            LoadRecords();

            // Consume the pending-run flag set by StartEndless() before the scene transition.
            if (_pendingRun)
            {
                IsActive = true;
                _pendingRun = false;
            }

            // Resolve enemy pool: serialized registry, or fallback to Resources
            if (enemyRegistry != null)
                _pool = enemyRegistry.Enemies;
            else
            {
                var loaded = Resources.LoadAll<EnemyType>("ScriptableObjects/Enemies");
                if (loaded.Length > 0) _pool = loaded;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void StartEndless()
        {
            if (_pool.Length == 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[EndlessMode] No EnemyTypes found — assign an EnemyRegistry to EndlessMode or place assets in Resources/ScriptableObjects/Enemies.");
#endif
            }

            var levelData = ScriptableObject.CreateInstance<LevelData>();
            var waves = BuildWaves(0, SeedWaveCount);
            levelData.SetEndlessWaves(waves);

            // Set static flag before scene transition (Instance gets recreated in new scene).
            _pendingRun = true;

            LevelLoader.NextEndlessSpec = levelData;
            LevelLoader.NextLevelId = LevelId;
            LevelLoader.Fade("Main");
        }

        /// Called by LevelRunner.OnAwakeSingleton when it detects NextEndlessSpec.
        public void OnRunStarted() => IsActive = true;

        /// Called by LevelRunner.HandleWaveCleared each time a wave is cleared.
        public void NotifyWaveReached(int waveOneBased)
        {
            if (!IsActive) return;
            if (waveOneBased > _bestWave)
            {
                _bestWave = waveOneBased;
                HighScores.Instance?.Record(LevelId, 0f, waveOneBased, 0);
            }
            AddRecord(waveOneBased);
        }

        /// Called by WaveManager.StartNextWave when it reaches the end of the wave list.
        /// Appends one more wave so the game never stops.
        public void AppendNextWave(LevelData levelData, int nextIdx)
        {
            var extra = BuildWaves(nextIdx, 1);
            levelData.AppendWaves(extra);
        }

        public int BestWave => _bestWave;

        public IReadOnlyList<EndlessRecord> GetTopRecords() => _records;

        // ── Records persistence ───────────────────────────────────────────────────

        private void AddRecord(int wave)
        {
            _records.Add(new EndlessRecord
            {
                wave = wave,
                date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            });
            _records.Sort((a, b) => b.wave.CompareTo(a.wave));
            if (_records.Count > MaxRecords)
                _records.RemoveRange(MaxRecords, _records.Count - MaxRecords);
            SaveRecords();
        }

        private void LoadRecords()
        {
            string json = PlayerPrefs.GetString(RecordsKey, "");
            if (string.IsNullOrEmpty(json)) return;
            var store = JsonUtility.FromJson<EndlessRecordStore>(json);
            if (store?.records != null) _records = store.records;
        }

        private void SaveRecords()
        {
            var store = new EndlessRecordStore { records = _records };
            PlayerPrefs.SetString(RecordsKey, JsonUtility.ToJson(store));
            PlayerPrefs.Save();
        }

        // ── Wave generation ───────────────────────────────────────────────────────

        private List<WaveDef> BuildWaves(int startIdx, int count)
        {
            var waves = new List<WaveDef>(count);
            for (int i = 0; i < count; i++)
            {
                int waveIdx = startIdx + i;
                // WaveDef.scaleMul is bypassed for endless runs (WaveManager.ApplyEndlessScaling takes over).
                int baseCount = Mathf.RoundToInt(Mathf.Lerp(4f, 22f, Mathf.Min(waveIdx, 39) / 39f));

                var entries = new List<EnemySpawnEntry>();
                if (_pool.Length > 0)
                {
                    int typeCount = Mathf.Min(_pool.Length, 1 + waveIdx / 10);
                    for (int t = 0; t < typeCount; t++)
                    {
                        var type = _pool[(waveIdx + t) % _pool.Length];
                        entries.Add(new EnemySpawnEntry
                        {
                            type = type,
                            count = Mathf.Max(1, baseCount / typeCount),
                        });
                    }
                }

                waves.Add(new WaveDef
                {
                    entries = entries,
                    spawnRateMs = Mathf.Max(250, BaseSpawnRateMs - waveIdx * 10),
                    breakMs = 3000,
                    portalIdx = -1,
                    scaleMul = 1f,
                });
            }
            return waves;
        }
    }
}
