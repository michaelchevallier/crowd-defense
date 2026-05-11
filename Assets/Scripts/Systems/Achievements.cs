#nullable enable
using System;
using System.Collections.Generic;
using CrowdDefense.Common;
using CrowdDefense.Data;
using UnityEngine;

namespace CrowdDefense.Systems
{
    public class Achievements : MonoSingleton<Achievements>
    {
        private const string PrefsKey = "cd.achievements.unlocked";

        [Header("Registry (auto-loaded from Resources/AchievementRegistry if null)")]
        [SerializeField] private AchievementRegistry? registry;

        private readonly HashSet<string> _unlocked = new();

        public static event Action<string>? OnUnlocked;

        protected override void OnAwakeSingleton()
        {
            LoadRegistry();
            LoadFromPrefs();
            Subscribe();
        }

        protected override void OnDestroySingleton()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            var em = EventManager.Instance;
            if (em == null) return;
            em.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            em.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
            em.Subscribe<TowerPlacedEvent>(OnTowerPlaced);
            em.Subscribe<CoinsChangedEvent>(OnCoinsChanged);
            em.Subscribe<LevelEndedEvent>(OnLevelEnded);
            em.Subscribe<BossDefeatedEvent>(OnBossDefeated);
        }

        private void Unsubscribe()
        {
            var em = EventManager.Instance;
            if (em == null) return;
            em.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            em.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
            em.Unsubscribe<TowerPlacedEvent>(OnTowerPlaced);
            em.Unsubscribe<CoinsChangedEvent>(OnCoinsChanged);
            em.Unsubscribe<LevelEndedEvent>(OnLevelEnded);
            em.Unsubscribe<BossDefeatedEvent>(OnBossDefeated);
        }

        private void OnEnemyKilled(EnemyKilledEvent _)   => TrackEvent("enemy_killed", 1);
        private void OnWaveCompleted(WaveCompletedEvent e) => TrackEvent("wave_cleared", 1, e.Index.ToString());
        private void OnTowerPlaced(TowerPlacedEvent _)    => TrackEvent("tower_placed", 1);
        private void OnBossDefeated(BossDefeatedEvent _)  => TrackEvent("boss_killed", 1);

        private void OnCoinsChanged(CoinsChangedEvent e)
        {
            if (e.Delta > 0)
                TrackEvent("gold_earned", e.Delta);
        }

        private void OnLevelEnded(LevelEndedEvent e)
        {
            if (e.Victory)
                TrackEvent("level_complete", 1, e.LevelIndex.ToString());
        }

        // Called externally for events without a dedicated GameEvent (e.g. synergy activation).
        public void TrackEvent(string eventKey, int delta, string? context = null)
        {
            string prefsKey = $"cd.ach.counter.{eventKey}";
            int current = PlayerPrefs.GetInt(prefsKey, 0) + delta;
            PlayerPrefs.SetInt(prefsKey, current);

            if (registry == null) return;
            foreach (var def in registry.All)
            {
                if (def == null || def.eventKey != eventKey) continue;
                if (_unlocked.Contains(def.id)) continue;

                bool triggered = def.predicateType switch
                {
                    AchievementPredicateType.Counter => current >= def.threshold,
                    AchievementPredicateType.Event   => true,
                    _ => false
                };

                if (triggered)
                    Unlock(def.id);
            }
        }

        public void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_unlocked.Contains(id)) return;

            _unlocked.Add(id);
            SaveToPrefs();

            AudioController.Instance?.Play("achievement");
            OnUnlocked?.Invoke(id);
        }

        public bool IsUnlocked(string id) => _unlocked.Contains(id);

        public int UnlockedCount => _unlocked.Count;

        public int TotalCount => registry?.All.Length ?? 0;

        public float CompletionRatio =>
            TotalCount > 0 ? (float)UnlockedCount / TotalCount : 0f;

        public int GetEventCount(string eventKey) =>
            PlayerPrefs.GetInt($"cd.ach.counter.{eventKey}", 0);

        private void LoadRegistry()
        {
            if (registry != null) return;
            registry = Resources.Load<AchievementRegistry>("AchievementRegistry");
#if UNITY_EDITOR
            if (registry == null)
                Debug.LogWarning("[Achievements] AchievementRegistry not assigned and not found in Resources/.");
#endif
        }

        private void LoadFromPrefs()
        {
            string csv = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(csv)) return;
            foreach (string id in csv.Split(','))
            {
                string trimmed = id.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    _unlocked.Add(trimmed);
            }
        }

        private void SaveToPrefs()
        {
            PlayerPrefs.SetString(PrefsKey, string.Join(",", _unlocked));
            PlayerPrefs.Save();
        }

#if UNITY_EDITOR
        public void ResetAllForTesting()
        {
            _unlocked.Clear();
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }
#endif
    }
}
