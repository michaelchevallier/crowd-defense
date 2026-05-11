#nullable enable
using CrowdDefense.Common;
using UnityEngine;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Subscribes to EventManager events and keeps RunState run-scoped stats in sync.
    /// Lifetime stats are persisted by LevelRunner.PersistResult on level end.
    /// Place on a persistent GameObject alongside EventBridge.
    /// </summary>
    public class StatsTracker : MonoBehaviour
    {
        private void OnEnable()
        {
            var em = EventManager.Instance;
            if (em == null) return;
            em.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            em.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
            em.Subscribe<TowerPlacedEvent>(OnTowerPlaced);
            em.Subscribe<CoinsChangedEvent>(OnCoinsChanged);
        }

        private void OnDisable()
        {
            var em = EventManager.Instance;
            if (em == null) return;
            em.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            em.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
            em.Unsubscribe<TowerPlacedEvent>(OnTowerPlaced);
            em.Unsubscribe<CoinsChangedEvent>(OnCoinsChanged);
        }

        private static void OnEnemyKilled(EnemyKilledEvent _)
        {
            var rs = SaveSystem.GetRunState();
            rs.runKills++;
            SaveSystem.SetRunState(rs);
        }

        private static void OnWaveCompleted(WaveCompletedEvent _)
        {
            var rs = SaveSystem.GetRunState();
            rs.runWavesCleared++;
            SaveSystem.SetRunState(rs);
        }

        private static void OnTowerPlaced(TowerPlacedEvent _)
        {
            var rs = SaveSystem.GetRunState();
            rs.runTowersPlaced++;
            SaveSystem.SetRunState(rs);
        }

        private static void OnCoinsChanged(CoinsChangedEvent evt)
        {
            if (evt.Delta <= 0) return;
            var rs = SaveSystem.GetRunState();
            rs.runGoldEarned += evt.Delta;
            SaveSystem.SetRunState(rs);
        }
    }
}
