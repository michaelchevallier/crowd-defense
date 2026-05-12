#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    public enum AchievementFilter { All, Unlocked, Locked }

    public class AchievementsPanel : UIControllerBase
    {
        public static AchievementsPanel? Instance { get; private set; }

        private Label?         _scoreLabel;
        private VisualElement? _grid;
        private Button?        _btnAll;
        private Button?        _btnUnlocked;
        private Button?        _btnLocked;

        // Top progress bar
        private Label?         _progressLabel;
        private VisualElement? _progressFill;
        private Label?         _pointsLabel;

        private Button?        _tabAll;
        private Button?        _tabCombat;
        private Button?        _tabEconomy;
        private Button?        _tabProgression;
        private Button?        _tabMisc;

        private AchievementFilter   _filter   = AchievementFilter.All;
        private AchievementCategory? _category = null; // null = toutes

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            if (Root == null) return;
            var _root = Root.Q<VisualElement>("achievements-root");
            _scoreLabel  = Root.Q<Label>("achievements-score");
            _grid        = Root.Q<VisualElement>("achievements-grid");

            BuildTopProgressBar(_root);
            _btnAll      = Root.Q<Button>("btn-filter-all");
            _btnUnlocked = Root.Q<Button>("btn-filter-unlocked");
            _btnLocked   = Root.Q<Button>("btn-filter-locked");

            _tabAll         = Root.Q<Button>("tab-all");
            _tabCombat      = Root.Q<Button>("tab-combat");
            _tabEconomy     = Root.Q<Button>("tab-economy");
            _tabProgression = Root.Q<Button>("tab-progression");
            _tabMisc        = Root.Q<Button>("tab-misc");

            var btnBack = Root.Q<Button>("btn-achievements-back");
            if (btnBack != null) btnBack.clicked += Hide;

            if (_btnAll      != null) _btnAll.clicked      += () => SetFilter(AchievementFilter.All);
            if (_btnUnlocked != null) _btnUnlocked.clicked += () => SetFilter(AchievementFilter.Unlocked);
            if (_btnLocked   != null) _btnLocked.clicked   += () => SetFilter(AchievementFilter.Locked);

            if (_tabAll         != null) _tabAll.clicked         += () => SetCategory(null);
            if (_tabCombat      != null) _tabCombat.clicked      += () => SetCategory(AchievementCategory.Combat);
            if (_tabEconomy     != null) _tabEconomy.clicked     += () => SetCategory(AchievementCategory.Economy);
            if (_tabProgression != null) _tabProgression.clicked += () => SetCategory(AchievementCategory.Progression);
            if (_tabMisc        != null) _tabMisc.clicked        += () => SetCategory(AchievementCategory.Misc);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool IsOpen => Root != null && !Root.ClassListContains("hidden");

        public void Show()
        {
            if (Root == null) return;
            _filter   = AchievementFilter.All;
            _category = null;
            RefreshFilterButtons();
            RefreshTabs();
            Rebuild();
            Root.RemoveFromClassList("hidden");
        }

        public void Hide() => Root?.AddToClassList("hidden");

        private void SetFilter(AchievementFilter f)
        {
            _filter = f;
            RefreshFilterButtons();
            Rebuild();
        }

        private void SetCategory(AchievementCategory? cat)
        {
            _category = cat;
            RefreshTabs();
            Rebuild();
        }

        private void RefreshFilterButtons()
        {
            SetActiveClass(_btnAll,      "ach-filter-btn--active", _filter == AchievementFilter.All);
            SetActiveClass(_btnUnlocked, "ach-filter-btn--active", _filter == AchievementFilter.Unlocked);
            SetActiveClass(_btnLocked,   "ach-filter-btn--active", _filter == AchievementFilter.Locked);
        }

        private void RefreshTabs()
        {
            SetActiveClass(_tabAll,         "ach-tab--active", _category == null);
            SetActiveClass(_tabCombat,      "ach-tab--active", _category == AchievementCategory.Combat);
            SetActiveClass(_tabEconomy,     "ach-tab--active", _category == AchievementCategory.Economy);
            SetActiveClass(_tabProgression, "ach-tab--active", _category == AchievementCategory.Progression);
            SetActiveClass(_tabMisc,        "ach-tab--active", _category == AchievementCategory.Misc);
        }

        private static void SetActiveClass(Button? btn, string cssClass, bool active)
        {
            if (btn == null) return;
            if (active) btn.AddToClassList(cssClass);
            else        btn.RemoveFromClassList(cssClass);
        }

        private void BuildTopProgressBar(VisualElement? root)
        {
            if (root == null) return;

            var container = new VisualElement();
            container.AddToClassList("ach-progress-header");

            var row = new VisualElement();
            row.AddToClassList("ach-progress-header-row");

            _progressLabel = new Label("Unlocked: 0 / 0");
            _progressLabel.AddToClassList("ach-progress-label");
            row.Add(_progressLabel);

            _pointsLabel = new Label("0 / 0 pts");
            _pointsLabel.AddToClassList("ach-points-label");
            row.Add(_pointsLabel);

            container.Add(row);

            var barBg = new VisualElement();
            barBg.AddToClassList("ach-global-bar-bg");
            _progressFill = new VisualElement();
            _progressFill.AddToClassList("ach-global-bar-fill");
            _progressFill.style.width = Length.Percent(0f);
            barBg.Add(_progressFill);
            container.Add(barBg);

            // Insert at top of root, before other children.
            root.Insert(0, container);
        }

        private void RefreshTopProgressBar(int unlockedCount, int totalCount, int earnedPts, int totalPts)
        {
            if (_progressLabel != null)
                _progressLabel.text = $"Unlocked: {unlockedCount} / {totalCount}";

            if (_pointsLabel != null)
                _pointsLabel.text = $"{earnedPts} / {totalPts} pts";

            if (_progressFill != null)
            {
                float ratio = totalCount > 0 ? (float)unlockedCount / totalCount : 0f;
                _progressFill.style.width = Length.Percent(ratio * 100f);
            }
        }

        private void Rebuild()
        {
            if (_grid == null) return;
            _grid.Clear();

            var ach      = Achievements.Instance;
            var registry = Resources.Load<AchievementRegistry>("AchievementRegistry");

            if (registry == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[AchievementsPanel] AchievementRegistry not found.");
#endif
                return;
            }

            int earned        = 0;
            int total         = 0;
            int unlockedCount = 0;

            // Collect and sort: unlocked first, then locked, stable by index.
            var defs = registry.All;
            int n    = defs.Length;
            var sorted = new (AchievementDef def, bool unlocked, int idx)[n];
            int count = 0;

            foreach (var def in defs)
            {
                if (def == null) continue;
                bool unlocked = ach != null && ach.IsUnlocked(def.id);
                total  += def.points;
                if (unlocked) { earned += def.points; unlockedCount++; }
                sorted[count++] = (def, unlocked, count);
            }

            RefreshTopProgressBar(unlockedCount, count, earned, total);

            // Sort: unlocked first (false > true inverted), then original order.
            Array.Sort(sorted, 0, count, Comparer<(AchievementDef, bool unlocked, int idx)>.Create(
                (a, b) =>
                {
                    int cmp = b.unlocked.CompareTo(a.unlocked); // unlocked first
                    return cmp != 0 ? cmp : a.idx.CompareTo(b.idx);
                }));

            for (int i = 0; i < count; i++)
            {
                var (def, unlocked, _) = sorted[i];
                bool passStatus = _filter switch
                {
                    AchievementFilter.Unlocked => unlocked,
                    AchievementFilter.Locked   => !unlocked,
                    _                          => true,
                };
                bool passCategory = _category == null || def.category == _category;
                if (!passStatus || !passCategory) continue;

                int progress  = ach != null && def.predicateType == AchievementPredicateType.Counter
                    ? ach.GetEventCount(def.eventKey)
                    : 0;
                _grid.Add(BuildCard(def, unlocked, progress));
            }

            if (_scoreLabel != null)
                _scoreLabel.text = $"Score : {earned} / {total}";
        }

        private static VisualElement BuildCard(AchievementDef def, bool unlocked, int counterProgress)
        {
            var card = new VisualElement();
            card.AddToClassList("ach-card");
            if (unlocked) card.AddToClassList("ach-card--unlocked");
            else          card.AddToClassList("ach-card--locked");

            bool hiddenLocked = def.hidden && !unlocked;
            var icon = new Label(unlocked ? def.IconEmoji : "\U0001F512");
            icon.AddToClassList("ach-icon");
            card.Add(icon);

            var title = new Label(hiddenLocked ? "???" : (string.IsNullOrEmpty(def.titleKey) ? def.id : L.Get(def.titleKey)));
            title.AddToClassList("ach-title");
            card.Add(title);

            if (unlocked)
            {
                var desc = new Label(string.IsNullOrEmpty(def.descKey) ? "" : L.Get(def.descKey));
                desc.AddToClassList("ach-desc");
                card.Add(desc);
            }
            else if (!hiddenLocked && def.predicateType == AchievementPredicateType.Counter && def.threshold > 0)
            {
                // Show progress count instead of description.
                int clamped = Math.Min(counterProgress, def.threshold);
                var progressLabel = new Label($"{clamped} / {def.threshold}");
                progressLabel.AddToClassList("ach-desc");
                card.Add(progressLabel);

                // Progress bar.
                float ratio = def.threshold > 0 ? (float)clamped / def.threshold : 0f;
                var bg  = new VisualElement();
                bg.AddToClassList("ach-progress-bar-bg");
                var fill = new VisualElement();
                fill.AddToClassList("ach-progress-bar-fill");
                fill.style.width = Length.Percent(ratio * 100f);
                bg.Add(fill);
                card.Add(bg);
            }
            else
            {
                // Predicate locked or hidden: show description if not hidden, else "???".
                string descText = hiddenLocked || string.IsNullOrEmpty(def.descKey)
                    ? "???"
                    : L.Get(def.descKey);
                var desc = new Label(descText);
                desc.AddToClassList("ach-desc");
                card.Add(desc);
            }

            var pts = new Label($"{def.points} pts");
            pts.AddToClassList("ach-pts");
            card.Add(pts);

            return card;
        }
    }
}
