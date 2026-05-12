#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class AchievementsPanel : MonoBehaviour
    {
        public static AchievementsPanel? Instance { get; private set; }

        private VisualElement? _root;
        private Label?         _scoreLabel;
        private VisualElement? _grid;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            var doc = GetComponent<UIDocument>().rootVisualElement;
            _root       = doc.Q<VisualElement>("achievements-root");
            _scoreLabel = doc.Q<Label>("achievements-score");
            _grid       = doc.Q<VisualElement>("achievements-grid");

            var btnBack = doc.Q<Button>("btn-achievements-back");
            if (btnBack != null) btnBack.clicked += Hide;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show()
        {
            if (_root == null) return;
            Rebuild();
            _root.RemoveFromClassList("hidden");
        }

        public void Hide() => _root?.AddToClassList("hidden");

        private void Rebuild()
        {
            if (_grid == null) return;
            _grid.Clear();

            var ach = Achievements.Instance;
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

            foreach (var def in registry.All)
            {
                if (def == null) continue;
                total += def.points;

                bool unlocked = ach != null && ach.IsUnlocked(def.id);
                if (unlocked) earned += def.points;

                _grid.Add(BuildCard(def, unlocked));
            }

            if (_scoreLabel != null)
                _scoreLabel.text = $"Score : {earned} / {total}";
        }

        private static VisualElement BuildCard(AchievementDef def, bool unlocked)
        {
            var card = new VisualElement();
            card.AddToClassList("ach-card");
            if (unlocked) card.AddToClassList("ach-card--unlocked");

            var icon = new Label(unlocked ? "\U0001F3C6" : "\U0001F512");
            icon.AddToClassList("ach-icon");
            card.Add(icon);

            var title = new Label(string.IsNullOrEmpty(def.titleKey) ? def.id : L.Get(def.titleKey));
            title.AddToClassList("ach-title");
            card.Add(title);

            var desc = new Label(string.IsNullOrEmpty(def.descKey) ? "" : L.Get(def.descKey));
            desc.AddToClassList("ach-desc");
            card.Add(desc);

            var pts = new Label($"{def.points} pts");
            pts.AddToClassList("ach-pts");
            card.Add(pts);

            return card;
        }
    }
}
