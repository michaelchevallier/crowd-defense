#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Lightweight research panel: shows 3 nodes for a given tower type.
    // Show() is called statically — the panel is self-contained via Instance.
    // Each node costs 1 talent point (TalentSystem.AvailablePoints).
    // Unlocks persisted in PlayerPrefs via TowerResearchTree.
    public class TowerResearchPanel : MonoBehaviour
    {
        public static TowerResearchPanel? Instance { get; private set; }

        private VisualElement? _panel;
        private Label? _title;
        private Button?[] _nodeBtns = new Button[TowerResearchTree.NodeCount];
        private Label?[] _nodeLabels = new Label[TowerResearchTree.NodeCount];
        private Label? _pointsLabel;
        private Button? _closeBtn;

        private string _towerId = "";
        private Action? _onClose;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            var doc = GetComponent<UIDocument>() ?? FindFirstObjectByType<UIDocument>();
            if (doc == null) { enabled = false; return; }
            var root = doc.rootVisualElement;

            _panel = root.Q<VisualElement>("research-panel");
            _title = root.Q<Label>("research-title");
            _pointsLabel = root.Q<Label>("research-points");
            _closeBtn = root.Q<Button>("research-close");

            for (int i = 0; i < TowerResearchTree.NodeCount; i++)
            {
                int idx = i;
                _nodeBtns[i] = root.Q<Button>($"research-node-{i}");
                _nodeLabels[i] = root.Q<Label>($"research-node-{i}-label");
                _nodeBtns[i]?.RegisterCallback<ClickEvent>(_ => OnNodeClicked(idx));
            }

            _closeBtn?.RegisterCallback<ClickEvent>(_ => Close());
            Hide();
        }

        public static void Show(string towerId, Action? onClose = null)
        {
            if (Instance == null) return;
            Instance.Open(towerId, onClose);
        }

        private void Open(string towerId, Action? onClose)
        {
            _towerId = towerId;
            _onClose = onClose;
            Refresh();
            _panel?.RemoveFromClassList("hidden");
        }

        private void Close()
        {
            Hide();
            _onClose?.Invoke();
            _onClose = null;
        }

        private void Hide() => _panel?.AddToClassList("hidden");

        private void Refresh()
        {
            if (_title != null)
                _title.text = $"Recherche — {_towerId}";

            if (_pointsLabel != null)
                _pointsLabel.text = $"Points disponibles : {TalentSystem.AvailablePoints}";

            for (int i = 0; i < TowerResearchTree.NodeCount; i++)
            {
                bool unlocked = TowerResearchTree.IsUnlocked(_towerId, i);
                bool canUnlock = TowerResearchTree.CanUnlock(_towerId, i);
                string state = unlocked ? "[OK]" : (canUnlock ? "1pt" : "---");
                string label = $"{TowerResearchTree.NodeLabel(i)}  {state}";

                if (_nodeLabels[i] != null)
                    _nodeLabels[i]!.text = label;

                _nodeBtns[i]?.SetEnabled(!unlocked && canUnlock);
            }
        }

        private void OnNodeClicked(int node)
        {
            bool ok = TowerResearchTree.TryUnlock(_towerId, node);
            if (!ok) return;
#if UNITY_EDITOR
            Debug.Log($"[Research] {_towerId} node {node} unlocked. Points left: {TalentSystem.AvailablePoints}");
#endif
            Refresh();
        }
    }
}
