#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Endless mode: generates a procedural LevelData with 40 seed waves, then appends more
    /// on demand. HP and Damage scale *= 1.15^waveIndex. Best wave record saved via HighScores.
    /// </summary>
    public class EndlessMode : MonoSingleton<EndlessMode>
    {
        public const string LevelId = "endless";
        private const float ScaleMul = 1.15f;
        private const int SeedWaveCount = 40;
        private const int BaseSpawnRateMs = 900;

        [SerializeField] private EnemyRegistry? enemyRegistry;

        // Static flag: survives scene transitions because MonoSingleton is recreated.
        private static bool _pendingRun;

        public bool IsActive { get; private set; }

        private int _bestWave;
        private EnemyType[] _pool = System.Array.Empty<EnemyType>();

        protected override void OnAwakeSingleton()
        {
            var hs = HighScores.Instance?.GetHighScore(LevelId);
            _bestWave = hs?.maxWaveReached ?? 0;

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
        }

        /// Called by WaveManager.StartNextWave when it reaches the end of the wave list.
        /// Appends one more wave so the game never stops.
        public void AppendNextWave(LevelData levelData, int nextIdx)
        {
            var extra = BuildWaves(nextIdx, 1);
            levelData.AppendWaves(extra);
        }

        public int BestWave => _bestWave;

        // ── Wave generation ───────────────────────────────────────────────────────

        private List<WaveDef> BuildWaves(int startIdx, int count)
        {
            var waves = new List<WaveDef>(count);
            for (int i = 0; i < count; i++)
            {
                int waveIdx = startIdx + i;
                float mul = Mathf.Pow(ScaleMul, waveIdx);
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
                    scaleMul = mul,
                });
            }
            return waves;
        }
    }
}
