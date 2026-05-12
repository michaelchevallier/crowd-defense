#nullable enable
using CrowdDefense.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    // Standalone lifetime-stats modal for the Menu scene.
    // Shows global totals + per-world score/stars table sourced from LifetimeStats.
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
        private VisualElement? _worldContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            var doc = GetComponent<UIDocument>();
            if (doc == null) return;

            var ve = doc.rootVisualElement;
            _root = ve.Q<VisualElement>("stats-lifetime-root") ?? ve.Q<VisualElement>("stats-root");

            _killsValue      = ve.Q<Label>("lt-kills-value");
            _goldValue       = ve.Q<Label>("lt-gold-value");
            _timeValue       = ve.Q<Label>("lt-time-value");
            _winsValue       = ve.Q<Label>("lt-wins-value");
            _achValue        = ve.Q<Label>("lt-ach-value");
            _worldContainer  = ve.Q<VisualElement>("lt-world-rows");

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

            PopulateRows();
        }

        private void PopulateRows()
        {
            if (_worldContainer == null) return;
            _worldContainer.Clear();

            for (int w = 1; w <= 10; w++)
            {
                int stars = LifetimeStats.GetWorldStars(w);
                int score = LifetimeStats.GetWorldHighScore(w);
                if (stars == 0 && score == 0) continue; // skip worlds never played

                var row = CreateRow(w, stars, score);
                _worldContainer.Add(row);
            }
        }

        private static VisualElement CreateRow(int world, int stars, int score)
        {
            var row = new VisualElement();
            row.AddToClassList("lt-world-row");

            var nameLabel = new Label($"World {world}");
            nameLabel.name = "world-name";
            nameLabel.AddToClassList("lt-world-name");

            var starsLabel = new Label(StarsText(stars));
            starsLabel.name = "stars";
            starsLabel.AddToClassList("lt-world-stars");

            var scoreLabel = new Label(score.ToString("N0"));
            scoreLabel.name = "score";
            scoreLabel.AddToClassList("lt-world-score");

            row.Add(nameLabel);
            row.Add(starsLabel);
            row.Add(scoreLabel);
            return row;
        }

        private static string StarsText(int stars) => stars switch
        {
            1 => "* ",
            2 => "** ",
            3 => "***",
            _ => "---"
        };

        private static string FormatTime(float seconds)
        {
            int total = Mathf.RoundToInt(seconds);
            int h     = total / 3600;
            int m     = (total % 3600) / 60;
            return $"{h}h {m:D2}m";
        }
    }
}
