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
        private VisualElement?[] _nodeTooltips = new VisualElement[TowerResearchTree.NodeCount];
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
            if (root == null) { Debug.LogError("[TowerResearchPanel] rootVisualElement is null"); enabled = false; return; }

            _panel = root.Q<VisualElement>("research-panel");
            _title = root.Q<Label>("research-title");
            _pointsLabel = root.Q<Label>("research-points");
            _closeBtn = root.Q<Button>("research-close");

            for (int i = 0; i < TowerResearchTree.NodeCount; i++)
            {
                int idx = i;
                _nodeBtns[i] = root.Q<Button>($"research-node-{i}");
                _nodeLabels[i] = root.Q<Label>($"research-node-{i}-label");

                // Build tooltip element parented to the node button
                if (_nodeBtns[i] != null)
                {
                    var tooltip = new VisualElement();
                    tooltip.AddToClassList("node-tooltip");
                    tooltip.AddToClassList("hidden");
                    _nodeBtns[i]!.Add(tooltip);
                    _nodeTooltips[i] = tooltip;

                    _nodeBtns[i]!.RegisterCallback<MouseEnterEvent>(_ => ShowTooltip(idx));
                    _nodeBtns[i]!.RegisterCallback<MouseLeaveEvent>(_ => HideTooltip(idx));
                    _nodeBtns[i]!.RegisterCallback<ClickEvent>(_ => OnNodeClicked(idx));
                }
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

                var btn = _nodeBtns[i];
                if (btn != null)
                {
                    btn.SetEnabled(!unlocked && canUnlock);

                    // Visual state: greyed-out class for disabled/unavailable nodes
                    if (unlocked)
                    {
                        btn.RemoveFromClassList("node-disabled");
                        btn.RemoveFromClassList("node-available");
                        btn.AddToClassList("node-unlocked");
                    }
                    else if (canUnlock)
                    {
                        btn.RemoveFromClassList("node-disabled");
                        btn.RemoveFromClassList("node-unlocked");
                        btn.AddToClassList("node-available");
                    }
                    else
                    {
                        btn.RemoveFromClassList("node-available");
                        btn.RemoveFromClassList("node-unlocked");
                        btn.AddToClassList("node-disabled");
                    }
                }
            }
        }

        private void ShowTooltip(int node)
        {
            var tooltip = _nodeTooltips[node];
            if (tooltip == null) return;

            bool unlocked = TowerResearchTree.IsUnlocked(_towerId, node);
            string desc = TowerResearchTree.NodeDescription(_towerId, node);
            string cost = unlocked ? "Deja debloque" : "Cout : 1 point de talent";
            tooltip.Clear();
            tooltip.Add(new Label(desc) { name = "tooltip-desc" });
            tooltip.Add(new Label(cost) { name = "tooltip-cost" });
            tooltip.RemoveFromClassList("hidden");
        }

        private void HideTooltip(int node) =>
            _nodeTooltips[node]?.AddToClassList("hidden");

        private void OnNodeClicked(int node)
        {
            bool ok = TowerResearchTree.TryUnlock(_towerId, node);
            if (!ok) return;
#if UNITY_EDITOR
            Debug.Log($"[Research] {_towerId} node {node} unlocked. Points left: {TalentSystem.AvailablePoints}");
#endif
            SpawnUnlockPopup(node);
            Refresh();
        }

        // Spawns a "+1" label that floats upward then fades over ~600 ms.
        private void SpawnUnlockPopup(int node)
        {
            var btn = _nodeBtns[node];
            if (btn == null || _panel == null) return;

            var popup = new Label("+1");
            popup.AddToClassList("unlock-popup");
            // Start position: centered above the button, animated via inline style
            popup.style.position = Position.Absolute;
            popup.style.left = btn.worldBound.center.x - _panel.worldBound.xMin - 16f;
            popup.style.top = btn.worldBound.yMin - _panel.worldBound.yMin - 10f;
            popup.style.opacity = 1f;
            _panel.Add(popup);

            // Animate: move up 30px and fade to 0 over 600 ms using IVisualElementScheduler
            const int steps = 20;
            const int intervalMs = 30;
            int step = 0;
            float startTop = popup.style.top.value.value;

            popup.schedule.Execute(() =>
            {
                step++;
                float t = step / (float)steps;
                popup.style.top = startTop - 30f * t;
                popup.style.opacity = 1f - t;
                if (step >= steps)
                    popup.RemoveFromHierarchy();
            }).Every(intervalMs).Until(() => step >= steps);
        }
    }
}
