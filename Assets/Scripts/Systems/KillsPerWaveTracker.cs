#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    [DefaultExecutionOrder(-40)]
    public class KillsPerWaveTracker : MonoSingleton<KillsPerWaveTracker>
    {
        private readonly Dictionary<int, int> _kills = new();
        private int _maxKills;

        public IReadOnlyDictionary<int, int> KillsByWave => _kills;
        public int MaxKillsInAnyWave => _maxKills;

        protected override void OnAwakeSingleton()
        {
            Enemy.OnDeathStatic += HandleEnemyDeath;
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveStart += _ => { };  // ensure WaveManager ticks
            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnStateChanged += HandleStateChanged;
        }

        protected override void OnDestroySingleton()
        {
            Enemy.OnDeathStatic -= HandleEnemyDeath;
            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleEnemyDeath(Enemy e, bool isBoss)
        {
            int waveIdx = WaveManager.Instance?.CurrentWaveIdx ?? 0;
            _kills.TryGetValue(waveIdx, out int prev);
            int next = prev + 1;
            _kills[waveIdx] = next;
            if (next > _maxKills) _maxKills = next;
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.Lobby) Reset();
        }

        public void Reset()
        {
            _kills.Clear();
            _maxKills = 0;
        }
    }
}
