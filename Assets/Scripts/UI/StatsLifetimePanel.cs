#nullable enable
using CrowdDefense.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    // Standalone lifetime-stats modal for the Menu scene.
    // Shows kills / gold / time / wins / achievements unlocked sourced from LifetimeStats.
    // Attach to a GameObject in the Menu scene alongside a UIDocument.
    public class StatsLifetimePanel : MonoBehaviour
    {
        public static StatsLifetimePanel? Instance { get; private set; }

        private VisualElement? _root;
        private Label?         _killsValue;
        private Label?         _goldValue;
        private Label?         _timeValue;
        private Label?         _winsValue;
        private Label?         _achValue;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            var doc = GetComponent<UIDocument>();
            if (doc == null) return;

            var ve = doc.rootVisualElement;
            _root = ve.Q<VisualElement>("stats-lifetime-root");

            _killsValue = ve.Q<Label>("lt-kills-value");
            _goldValue  = ve.Q<Label>("lt-gold-value");
            _timeValue  = ve.Q<Label>("lt-time-value");
            _winsValue  = ve.Q<Label>("lt-wins-value");
            _achValue   = ve.Q<Label>("lt-ach-value");

            var closeBtn = ve.Q<Button>("lt-close-btn");
            if (closeBtn != null) closeBtn.clicked += Hide;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool IsOpen => _root != null && !_root.ClassListContains("hidden");

        public void Show()
        {
            if (_root == null) return;
            Refresh();
            _root.RemoveFromClassList("hidden");
        }

        public void Hide() => _root?.AddToClassList("hidden");

        private void Refresh()
        {
            var ls = LifetimeStats.Instance;
            if (ls == null) return;

            if (_killsValue != null) _killsValue.text = ls.TotalKills.ToString("N0");
            if (_goldValue  != null) _goldValue.text  = ls.TotalGold.ToString("N0");
            if (_timeValue  != null) _timeValue.text  = FormatTime(ls.TotalTimePlayed);
            if (_winsValue  != null) _winsValue.text  = ls.LevelsWon.ToString("N0");
            if (_achValue   != null) _achValue.text   = ls.AchievementsUnlocked.ToString("N0");
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.RoundToInt(seconds);
            int h     = total / 3600;
            int m     = (total % 3600) / 60;
            return $"{h}h {m:D2}m";
        }
    }
}
