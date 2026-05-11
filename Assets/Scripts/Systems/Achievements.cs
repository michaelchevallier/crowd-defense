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
        }

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

        public void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_unlocked.Contains(id)) return;

            _unlocked.Add(id);
            SaveToPrefs();

            AudioController.Instance?.Play("achievement");

            // AchievementToastController subscribes to OnUnlocked and renders the toast.
            OnUnlocked?.Invoke(id);
        }

        public bool IsUnlocked(string id) => _unlocked.Contains(id);

        public int UnlockedCount => _unlocked.Count;

        public int TotalCount => registry?.All.Length ?? 0;

        public float CompletionRatio =>
            TotalCount > 0 ? (float)UnlockedCount / TotalCount : 0f;

        // Event-based hook for deferred counters — called by game systems for cumulative tracking.
        // Phase 5.B will wire these calls in hot zones:
        //   Enemy.Die          → Achievements.Instance?.TrackEvent("enemy_killed", 1)
        //   Tower.OnPlaced     → Achievements.Instance?.TrackEvent("tower_placed", 1)
        //   WaveManager        → Achievements.Instance?.TrackEvent("wave_cleared", 1)
        //   Economy            → Achievements.Instance?.TrackEvent("gold_earned", amount)
        //   LevelRunner.Win    → Achievements.Instance?.TrackEvent("level_complete", 1, levelId)
        //   Synergies.Activate → Achievements.Instance?.TrackEvent("synergy_activated", 1)
        // Keep counters in PlayerPrefs under "cd.ach.counter.<eventKey>" for persistence.
        public void TrackEvent(string eventKey, int delta, string? context = null)
        {
            string prefsKey = $"cd.ach.counter.{eventKey}";
            int current = PlayerPrefs.GetInt(prefsKey, 0) + delta;
            PlayerPrefs.SetInt(prefsKey, current);

            // TODO Phase 5.B: evaluate achievement predicates against updated counters
            // e.g. CheckCounterAchievements(eventKey, current, context);
        }

        public int GetEventCount(string eventKey) =>
            PlayerPrefs.GetInt($"cd.ach.counter.{eventKey}", 0);

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
