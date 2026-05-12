#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class RunMapController : MonoBehaviour
    {
        // Icon glyphs per node type (plain ASCII, no special chars per CLAUDE.md).
        private static readonly Dictionary<RunMapNodeType, string> NodeIcons = new()
        {
            { RunMapNodeType.Combat,  "[C]" },
            { RunMapNodeType.Elite,   "[E]" },
            { RunMapNodeType.Mystery, "[?]" },
            { RunMapNodeType.Shop,    "[$]" },
            { RunMapNodeType.Rest,    "[R]" },
            { RunMapNodeType.Boss,    "[B]" },
        };

        private static readonly Dictionary<RunMapNodeType, string> NodeTypeNames = new()
        {
            { RunMapNodeType.Combat,  "Combat" },
            { RunMapNodeType.Elite,   "Elite" },
            { RunMapNodeType.Mystery, "Mystere" },
            { RunMapNodeType.Shop,    "Boutique" },
            { RunMapNodeType.Rest,    "Repos" },
            { RunMapNodeType.Boss,    "BOSS" },
        };

        private VisualElement? _graphContainer;
        private Label? _hintLabel;
        private RunMap? _runMap;
        private HashSet<string> _availableIds = new();

        private void Start()
        {
            _runMap = RunMap.Instance;

            if (_runMap == null || !_runMap.HasActiveMap())
            {
#if UNITY_EDITOR
                Debug.LogWarning("[RunMapController] No active RunMap — generating default act1 map for preview.");
#endif
                _runMap?.Generate(1, 42);
            }

            var uiDoc = GetComponent<UIDocument>();


            if (uiDoc == null) return;


            var root = uiDoc.rootVisualElement;


            if (root == null) return;
            _graphContainer = root.Q<VisualElement>("runmap-graph");
            _hintLabel = root.Q<Label>("runmap-node-hint");

            var actLabel = root.Q<Label>("runmap-act-label");
            if (actLabel != null && _runMap?.State != null)
                actLabel.text = $"Acte {_runMap.State.worldId}";

            Rebuild();
        }

        private void Rebuild()
        {
            if (_graphContainer == null || _runMap == null) return;
            _graphContainer.Clear();

            _availableIds = new HashSet<string>();
            var availableNodes = _runMap.GetAvailableNextNodes();
            foreach (var n in availableNodes)
                _availableIds.Add(n.id);

            // If no current node set yet (start of run), start nodes are all available.
            var currentNode = _runMap.GetCurrentNode();
            bool atStart = currentNode == null;
            if (atStart)
            {
                foreach (var n in _runMap.GetStartNodes())
                    _availableIds.Add(n.id);
            }

            var byRow = _runMap.GetNodesByRow();
            int maxRow = 6;

            // Render rows bottom-to-top (row 0 = start at bottom, row 6 = boss at top).
            // The graph container uses flex-direction: column-reverse, so we add row 0 first.
            for (int r = 0; r <= maxRow; r++)
            {
                if (!byRow.TryGetValue(r, out var rowNodes)) continue;
                var rowEl = BuildRow(rowNodes);
                _graphContainer.Add(rowEl);
            }
        }

        private VisualElement BuildRow(List<RunMapNode> nodes)
        {
            var row = new VisualElement();
            row.AddToClassList("runmap-row");

            foreach (var node in nodes)
                row.Add(BuildNodeElement(node));

            return row;
        }

        private VisualElement BuildNodeElement(RunMapNode node)
        {
            var el = new VisualElement();
            el.AddToClassList("runmap-node");
            el.AddToClassList(node.type.ToString().ToLower());

            bool isCurrent = _runMap?.IsNodeCurrent(node.id) ?? false;
            bool isVisited = _runMap?.IsNodeVisited(node.id) ?? false;
            bool isAvailable = _availableIds.Contains(node.id);

            if (isCurrent)
            {
                el.AddToClassList("current");
            }
            else if (isVisited)
            {
                el.AddToClassList("visited");
            }
            else if (isAvailable)
            {
                el.AddToClassList("available");
                string nodeId = node.id;
                el.RegisterCallback<ClickEvent>(_ => OnNodeClicked(nodeId));
                el.RegisterCallback<MouseEnterEvent>(_ => ShowHint(node));
                el.RegisterCallback<MouseLeaveEvent>(_ => ClearHint());
            }
            else
            {
                el.AddToClassList("hidden");
            }

            var icon = new Label(NodeIcons.TryGetValue(node.type, out var ico) ? ico : "?");
            icon.AddToClassList("runmap-node-icon");
            el.Add(icon);

            var label = new Label(NodeTypeNames.TryGetValue(node.type, out var name) ? name : "");
            label.AddToClassList("runmap-node-label");
            el.Add(label);

            return el;
        }

        private void OnNodeClicked(string nodeId)
        {
            if (_runMap == null) return;
            var node = FindNodeById(nodeId);
            if (node == null) return;

            _runMap.MoveTo(nodeId);

            if (node.type == RunMapNodeType.Boss)
            {
                // Boss encounter — load boss level or go back to world map
                if (!string.IsNullOrEmpty(node.bossId))
                    LevelLoader.LoadLevel(node.bossId);
                else
                    LevelLoader.GoToWorldMap();
            }
            else if ((node.type == RunMapNodeType.Combat || node.type == RunMapNodeType.Elite)
                     && !string.IsNullOrEmpty(node.combatLevelId))
            {
                LevelLoader.LoadLevel(node.combatLevelId);
            }
            else if (node.type == RunMapNodeType.Shop)
            {
                // Shop node — rebuild graph to reflect movement, shop overlay handled by other system
                Rebuild();
            }
            else
            {
                // Mystery / Rest — rebuild map to show new available nodes
                Rebuild();
            }
        }

        private void ShowHint(RunMapNode node)
        {
            if (_hintLabel == null) return;
            string type = NodeTypeNames.TryGetValue(node.type, out var t) ? t : "";
            string extra = node.type == RunMapNodeType.Elite
                ? $" (x{node.swarmMul:F1} vagues, x{node.rewardMul:F1} recompense)"
                : "";
            _hintLabel.text = $"{type}{extra}";
        }

        private void ClearHint()
        {
            if (_hintLabel != null) _hintLabel.text = "";
        }

        private RunMapNode? FindNodeById(string nodeId)
        {
            if (_runMap?.State == null) return null;
            for (int i = 0; i < _runMap.State.nodes.Count; i++)
                if (_runMap.State.nodes[i].id == nodeId) return _runMap.State.nodes[i];
            return null;
        }
    }
}
