#nullable enable
using System;
using System.Collections.Generic;
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
        private VisualElement? _leaderboardContainer;

        // Career Totals section (injected dynamically)
        private Label? _careerRunsValue;
        private Label? _careerWinsValue;
        private Label? _careerKillsValue;
        private Label? _careerGoldValue;
        private Label? _careerTimeValue;

        // Tower stats section
        private VisualElement? _towerStatsContainer;
        private VisualElement? _endlessContainer;

        private static readonly string[] AllTowerTypes =
        {
            "archer", "tank", "mage", "ballista", "cannon",
            "frost", "crossbow", "skyguard", "mine", "acid",
            "fan", "portal", "magnet"
        };

        // Today section
        private Label? _todayRunsValue;
        private Label? _todayKillsValue;
        private Label? _todayTimeValue;

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

            BuildTodaySection(ve);
            BuildCareerTotalsSection(ve);
            BuildTowerStatsSection(ve);
            BuildLeaderboardSection(ve);
            BuildEndlessTop10Section(ve);
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

            RefreshToday();
            RefreshCareerTotals(ls);
            PopulateRows();
            PopulateTowerStats();
            PopulateLeaderboard();
            PopulateEndlessTop10();
        }

        private void RefreshToday()
        {
            if (_todayRunsValue  != null) _todayRunsValue.text  = LifetimeStats.TodayRuns.ToString("N0");
            if (_todayKillsValue != null) _todayKillsValue.text = LifetimeStats.TodayKills.ToString("N0");
            if (_todayTimeValue  != null) _todayTimeValue.text  = FormatTime(LifetimeStats.TodayTime);
        }

        private void RefreshCareerTotals(LifetimeStats ls)
        {
            int runs = ls.TotalRuns;
            int wins = ls.LevelsWon;
            string winPct = runs > 0 ? $"{wins} ({wins * 100 / runs}%)" : $"{wins} (0%)";

            if (_careerRunsValue  != null) _careerRunsValue.text  = runs.ToString("N0");
            if (_careerWinsValue  != null) _careerWinsValue.text  = winPct;
            if (_careerKillsValue != null) _careerKillsValue.text = ls.TotalKills.ToString("N0");
            if (_careerGoldValue  != null) _careerGoldValue.text  = ls.TotalGold.ToString("N0");
            if (_careerTimeValue  != null) _careerTimeValue.text  = FormatTimeFull(ls.TotalTimePlayed);
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

        private void PopulateLeaderboard()
        {
            if (_leaderboardContainer == null) return;
            _leaderboardContainer.Clear();

            var scores = LifetimeStats.GetLeaderboard();
            if (scores.Count == 0)
            {
                var empty = new Label("No runs yet");
                empty.AddToClassList("lt-lb-empty");
                _leaderboardContainer.Add(empty);
                return;
            }

            for (int i = 0; i < scores.Count; i++)
            {
                var e   = scores[i];
                var lbl = new Label($"{i + 1}. {e.score:N0} pts — W{e.world} — {e.date}");
                lbl.AddToClassList("lt-lb-entry");
                _leaderboardContainer.Add(lbl);
            }
        }

        private void BuildTowerStatsSection(VisualElement root)
        {
            var anchor = root.Q<VisualElement>("lt-tower-stats") ?? _root ?? root;

            var section = new VisualElement();
            section.name = "tower-stats-section";
            section.AddToClassList("lt-section");

            var header = new Label("Tours utilisees (top 5)");
            header.AddToClassList("lt-section-header");
            section.Add(header);

            _towerStatsContainer = new VisualElement();
            _towerStatsContainer.name = "lt-tower-rows";
            section.Add(_towerStatsContainer);

            anchor.Add(section);
        }

        private void PopulateTowerStats()
        {
            if (_towerStatsContainer == null) return;
            _towerStatsContainer.Clear();

            var entries = new List<(string type, int placed, int kills)>();
            foreach (var t in AllTowerTypes)
            {
                int placed = LifetimeStats.GetTowerPlaced(t);
                if (placed == 0) continue;
                int kills = LifetimeStats.GetTowerKills(t);
                entries.Add((t, placed, kills));
            }

            if (entries.Count == 0)
            {
                var empty = new Label("Aucune tour placee");
                empty.AddToClassList("lt-lb-empty");
                _towerStatsContainer.Add(empty);
                return;
            }

            entries.Sort((a, b) => b.placed.CompareTo(a.placed));
            int count = Math.Min(5, entries.Count);
            for (int i = 0; i < count; i++)
            {
                var (type, placed, kills) = entries[i];
                float avgKills = placed > 0 ? (float)kills / placed : 0f;
                var row = new VisualElement();
                row.AddToClassList("lt-stat-row");

                var nameLabel = new Label($"{i + 1}. {type}");
                nameLabel.AddToClassList("lt-stat-label");

                var valLabel = new Label($"{placed} poses  |  {kills} kills  |  moy {avgKills:F1}");
                valLabel.AddToClassList("lt-stat-value");

                row.Add(nameLabel);
                row.Add(valLabel);
                _towerStatsContainer.Add(row);
            }
        }

        private void BuildEndlessTop10Section(VisualElement root)
        {
            var anchor = root.Q<VisualElement>("lt-endless-top10") ?? _root ?? root;

            var section = new VisualElement();
            section.name = "endless-top10-section";
            section.AddToClassList("lt-section");

            var header = new Label("Endless Top 10");
            header.AddToClassList("lt-section-header");
            section.Add(header);

            _endlessContainer = new VisualElement();
            _endlessContainer.name = "lt-endless-rows";
            section.Add(_endlessContainer);

            anchor.Add(section);
        }

        private void PopulateEndlessTop10()
        {
            if (_endlessContainer == null) return;
            _endlessContainer.Clear();

            var em = EndlessMode.Instance;
            if (em == null)
            {
                var empty = new Label("Mode endless non chargé");
                empty.AddToClassList("lt-lb-empty");
                _endlessContainer.Add(empty);
                return;
            }

            var records = em.GetTopRecords();
            if (records.Count == 0)
            {
                var empty = new Label("Aucune partie endless");
                empty.AddToClassList("lt-lb-empty");
                _endlessContainer.Add(empty);
                return;
            }

            for (int i = 0; i < records.Count; i++)
            {
                var r   = records[i];
                var lbl = new Label($"{i + 1}. Vague {r.wave}  —  {r.date}");
                lbl.AddToClassList("lt-lb-entry");
                _endlessContainer.Add(lbl);
            }
        }

        private void BuildLeaderboardSection(VisualElement root)
        {
            var anchor = root.Q<VisualElement>("lt-leaderboard") ?? _root ?? root;

            var section = new VisualElement();
            section.name = "leaderboard-section";
            section.AddToClassList("lt-section");

            var header = new Label("Top 5 Best Scores");
            header.AddToClassList("lt-section-header");
            section.Add(header);

            _leaderboardContainer = new VisualElement();
            _leaderboardContainer.name = "lt-lb-rows";
            section.Add(_leaderboardContainer);

            anchor.Add(section);
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.RoundToInt(seconds);
            int h     = total / 3600;
            int m     = (total % 3600) / 60;
            return $"{h}h {m:D2}m";
        }

        private static string FormatTimeFull(float seconds)
        {
            int total = Mathf.RoundToInt(seconds);
            int h     = total / 3600;
            int m     = (total % 3600) / 60;
            int s     = total % 60;
            return $"{h:D2}:{m:D2}:{s:D2}";
        }

        private void BuildTodaySection(VisualElement root)
        {
            var anchor = root.Q<VisualElement>("lt-today") ?? _root ?? root;

            var section = new VisualElement();
            section.name = "today-section";
            section.AddToClassList("lt-section");

            var header = new Label("Aujourd'hui");
            header.AddToClassList("lt-section-header");
            section.Add(header);

            _todayRunsValue  = AddStatRow(section, "Parties jouees", "lt-today-runs");
            _todayKillsValue = AddStatRow(section, "Kills",          "lt-today-kills");
            _todayTimeValue  = AddStatRow(section, "Temps de jeu",   "lt-today-time");

            anchor.Add(section);
        }

        private void BuildCareerTotalsSection(VisualElement root)
        {
            // Prefer a named anchor already in the UXML; if absent, append to root.
            var anchor = root.Q<VisualElement>("lt-career-totals") ?? _root ?? root;

            var section = new VisualElement();
            section.name = "career-totals-section";
            section.AddToClassList("lt-section");

            var header = new Label("Career Totals");
            header.AddToClassList("lt-section-header");
            section.Add(header);

            _careerRunsValue  = AddStatRow(section, "Runs played",    "lt-career-runs");
            _careerWinsValue  = AddStatRow(section, "Wins",           "lt-career-wins");
            _careerKillsValue = AddStatRow(section, "Total kills",    "lt-career-kills");
            _careerGoldValue  = AddStatRow(section, "Lifetime gold",  "lt-career-gold");
            _careerTimeValue  = AddStatRow(section, "Total playtime", "lt-career-time");

            anchor.Add(section);
        }

        private static Label AddStatRow(VisualElement parent, string labelText, string valueName)
        {
            var row = new VisualElement();
            row.AddToClassList("lt-stat-row");

            var lbl = new Label(labelText);
            lbl.AddToClassList("lt-stat-label");

            var val = new Label("--");
            val.name = valueName;
            val.AddToClassList("lt-stat-value");

            row.Add(lbl);
            row.Add(val);
            parent.Add(row);
            return val;
        }
    }
}
