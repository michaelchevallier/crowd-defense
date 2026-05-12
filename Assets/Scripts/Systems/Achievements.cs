#nullable enable
using System;
using System.Collections.Generic;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.UI;
using UnityEngine;

namespace CrowdDefense.Systems
{
    public class Achievements : MonoSingleton<Achievements>
    {
        private bool _legendaryPerkGranted;
        private const string PrefsKey       = "cd.achievements.unlocked";
        private const string PrefsOrderKey  = "cd.achievements.order";

        [Header("Registry (auto-loaded from Resources/AchievementRegistry if null)")]
        [SerializeField] private AchievementRegistry? registry;

        private readonly HashSet<string>  _unlocked = new();
        private readonly List<string>     _unlockOrder = new();

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

            // Load chronological order (separate key, appended on each unlock).
            string orderCsv = PlayerPrefs.GetString(PrefsOrderKey, "");
            _unlockOrder.Clear();
            if (!string.IsNullOrEmpty(orderCsv))
            {
                foreach (string id in orderCsv.Split(','))
                {
                    string trimmed = id.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && _unlocked.Contains(trimmed))
                        _unlockOrder.Add(trimmed);
                }
            }
            // Fallback: if order list missing, seed from unlocked set (no ordering guarantee).
            if (_unlockOrder.Count == 0 && _unlocked.Count > 0)
                _unlockOrder.AddRange(_unlocked);
        }

        private void SaveToPrefs()
        {
            PlayerPrefs.SetString(PrefsKey,      string.Join(",", _unlocked));
            PlayerPrefs.SetString(PrefsOrderKey, string.Join(",", _unlockOrder));
            PlayerPrefs.Save();
        }

        public void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_unlocked.Contains(id)) return;

            _unlocked.Add(id);
            _unlockOrder.Add(id);
            SaveToPrefs();

            var def = registry?.Get(id);
            if (def != null && def.rewardGold > 0)
            {
                int pending = PlayerPrefs.GetInt("cd.gold.pending", 0) + def.rewardGold;
                PlayerPrefs.SetInt("cd.gold.pending", pending);
                PlayerPrefs.Save();
            }

            AudioController.Instance?.Play("achievement");

            // AchievementToastController subscribes to OnUnlocked and renders the toast.
            OnUnlocked?.Invoke(id);

            if (!_legendaryPerkGranted && IsAllUnlocked())
            {
                _legendaryPerkGranted = true;
                PerkSystem.Instance?.UnlockLegendaryPerk();
                Toast.Show("Achievement", "Perk Legendaire debloque !", 5000, "trophy", ToastType.Achievement);
            }
        }

        public bool IsUnlocked(string id) => _unlocked.Contains(id);

        public bool IsAllUnlocked()
        {
            if (registry == null || registry.All.Length == 0) return false;
            foreach (var def in registry.All)
            {
                if (def == null) continue;
                if (!_unlocked.Contains(def.id)) return false;
            }
            return true;
        }

        public int UnlockedCount => _unlocked.Count;

        public int TotalCount => registry?.All.Length ?? 0;

        public float CompletionRatio =>
            TotalCount > 0 ? (float)UnlockedCount / TotalCount : 0f;

        // Event-based hook for deferred counters — called by game systems for cumulative tracking.
        // Keep counters in PlayerPrefs under "cd.ach.counter.<eventKey>" for persistence.
        public void TrackEvent(string eventKey, int delta, string? context = null)
        {
            string prefsKey = $"cd.ach.counter.{eventKey}";
            int current = PlayerPrefs.GetInt(prefsKey, 0) + delta;
            PlayerPrefs.SetInt(prefsKey, current);

            if (registry == null) return;
            foreach (var def in registry.All)
            {
                if (def == null || def.predicateType != AchievementPredicateType.Counter) continue;
                if (def.eventKey != eventKey) continue;
                if (current >= def.threshold)
                    Unlock(def.id);
            }
        }

        public int GetEventCount(string eventKey) =>
            PlayerPrefs.GetInt($"cd.ach.counter.{eventKey}", 0);

        // Returns up to `count` most-recently-unlocked achievement ids (newest last in list).
        public IReadOnlyList<string> GetRecentUnlocked(int count)
        {
            int start = Math.Max(0, _unlockOrder.Count - count);
            return _unlockOrder.GetRange(start, _unlockOrder.Count - start);
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
