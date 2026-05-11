#nullable enable
using CrowdDefense.Common;
using CrowdDefense.Data;
using UnityEngine;

namespace CrowdDefense.Systems
{
    // Tracks multi-kill streaks within a rolling time window.
    // On each EnemyKilledEvent: increments kill count, resets window timer.
    // Publishes ComboUpdatedEvent when combo level changes, ComboResetEvent on expiry.
    // Economy listens to ActiveMultiplier to scale coin rewards.
    public class ComboSystem : MonoSingleton<ComboSystem>
    {
        public int KillCount { get; private set; }
        public int ComboLevel { get; private set; }
        public float ActiveMultiplier { get; private set; } = 1f;

        private float _windowTimer;
        private bool _active;

        protected override void OnAwakeSingleton()
        {
            EventManager.Instance?.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        protected override void OnDestroySingleton()
        {
            EventManager.Instance?.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        private void Update()
        {
            if (!_active) return;
            _windowTimer -= Time.deltaTime;
            if (_windowTimer <= 0f)
                ResetCombo();
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            KillCount++;
            _windowTimer = BalanceConfig.Get().ComboWindowSeconds;
            _active = true;

            int newLevel = ComputeLevel();
            if (newLevel != ComboLevel)
            {
                ComboLevel = newLevel;
                ActiveMultiplier = MultiplierFor(newLevel);
                if (ComboLevel >= BalanceConfig.Get().ComboMinKills)
                    EventManager.Instance?.Publish(new ComboUpdatedEvent(ComboLevel, ActiveMultiplier));
            }
        }

        private void ResetCombo()
        {
            if (KillCount == 0) return;
            bool wasActive = ComboLevel >= BalanceConfig.Get().ComboMinKills;
            KillCount = 0;
            ComboLevel = 0;
            ActiveMultiplier = 1f;
            _active = false;
            if (wasActive)
                EventManager.Instance?.Publish(new ComboResetEvent());
        }

        // KillCount of 2 → level 2 (first visible combo), 3 → level 3, etc.
        // Capped at ComboMultipliers.Length - 1
        private int ComputeLevel()
        {
            var cfg = BalanceConfig.Get();
            return Mathf.Clamp(KillCount, 0, cfg.ComboMultipliers.Length - 1);
        }

        private static float MultiplierFor(int level)
        {
            var muls = BalanceConfig.Get().ComboMultipliers;
            if (muls == null || muls.Length == 0) return 1f;
            return muls[Mathf.Clamp(level, 0, muls.Length - 1)];
        }
    }
}
