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

    [RequireComponent(typeof(UIDocument))]
    public class AchievementsPanel : MonoBehaviour
    {
        public static AchievementsPanel? Instance { get; private set; }

        private VisualElement? _root;
        private Label?         _scoreLabel;
        private VisualElement? _grid;
        private Button?        _btnAll;
        private Button?        _btnUnlocked;
        private Button?        _btnLocked;

        private AchievementFilter _filter = AchievementFilter.All;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            var doc = GetComponent<UIDocument>().rootVisualElement;
            _root        = doc.Q<VisualElement>("achievements-root");
            _scoreLabel  = doc.Q<Label>("achievements-score");
            _grid        = doc.Q<VisualElement>("achievements-grid");
            _btnAll      = doc.Q<Button>("btn-filter-all");
            _btnUnlocked = doc.Q<Button>("btn-filter-unlocked");
            _btnLocked   = doc.Q<Button>("btn-filter-locked");

            var btnBack = doc.Q<Button>("btn-achievements-back");
            if (btnBack != null) btnBack.clicked += Hide;

            if (_btnAll      != null) _btnAll.clicked      += () => SetFilter(AchievementFilter.All);
            if (_btnUnlocked != null) _btnUnlocked.clicked += () => SetFilter(AchievementFilter.Unlocked);
            if (_btnLocked   != null) _btnLocked.clicked   += () => SetFilter(AchievementFilter.Locked);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool IsOpen => _root != null && !_root.ClassListContains("hidden");

        public void Show()
        {
            if (_root == null) return;
            _filter = AchievementFilter.All;
            RefreshFilterButtons();
            Rebuild();
            _root.RemoveFromClassList("hidden");
        }

        public void Hide() => _root?.AddToClassList("hidden");

        private void SetFilter(AchievementFilter f)
        {
            _filter = f;
            RefreshFilterButtons();
            Rebuild();
        }

        private void RefreshFilterButtons()
        {
            SetActive(_btnAll,      _filter == AchievementFilter.All);
            SetActive(_btnUnlocked, _filter == AchievementFilter.Unlocked);
            SetActive(_btnLocked,   _filter == AchievementFilter.Locked);
        }

        private static void SetActive(Button? btn, bool active)
        {
            if (btn == null) return;
            if (active) btn.AddToClassList("ach-filter-btn--active");
            else        btn.RemoveFromClassList("ach-filter-btn--active");
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

            int earned = 0;
            int total  = 0;

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
                if (unlocked) earned += def.points;
                sorted[count++] = (def, unlocked, count);
            }

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
                bool show = _filter switch
                {
                    AchievementFilter.Unlocked => unlocked,
                    AchievementFilter.Locked   => !unlocked,
                    _                          => true,
                };
                if (!show) continue;

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

            var icon = new Label(unlocked ? def.IconEmoji : "\U0001F512");
            icon.AddToClassList("ach-icon");
            card.Add(icon);

            var title = new Label(string.IsNullOrEmpty(def.titleKey) ? def.id : L.Get(def.titleKey));
            title.AddToClassList("ach-title");
            card.Add(title);

            if (unlocked)
            {
                var desc = new Label(string.IsNullOrEmpty(def.descKey) ? "" : L.Get(def.descKey));
                desc.AddToClassList("ach-desc");
                card.Add(desc);
            }
            else if (def.predicateType == AchievementPredicateType.Counter && def.threshold > 0)
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
                // Predicate (Event) locked: show description if not hidden, else "???".
                string descText = def.hidden || string.IsNullOrEmpty(def.descKey)
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
