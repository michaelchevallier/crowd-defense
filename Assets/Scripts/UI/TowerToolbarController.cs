#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class TowerToolbarController : MonoBehaviour
    {
        [SerializeField] private TowerRegistry? towerRegistry;

        private VisualElement? toolbarRoot;
        private VisualElement? tooltipEl;
        private Label? tooltipName;
        private Label? tooltipDesc;

        // Parallel arrays: one entry per tower in registry order
        private readonly List<TowerType> towerOrder = new();
        private readonly List<VisualElement> cells = new();

        // Mobile long-press tracking
        private int longPressIdx = -1;
        private float longPressTimer;
        private const float LongPressSec = 0.4f;

        private static readonly KeyCode[] HotkeyMap =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8,
            KeyCode.Alpha9, KeyCode.Alpha0,
        };

        private void Start()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[TowerToolbar] No UIDocument component found.");
#endif
                return;
            }

            var root = doc.rootVisualElement;
            if (root == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[TowerToolbar] rootVisualElement is null");
#endif
                return;
            }
            toolbarRoot = root.Q<VisualElement>("tower-toolbar");

            if (towerRegistry == null || toolbarRoot == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[TowerToolbar] TowerRegistry={towerRegistry}, toolbar-root={toolbarRoot}");
#endif
                return;
            }

            BuildTooltipEl();
            BuildCells();

            if (Economy.Instance != null)
            {
                Economy.Instance.OnGoldChanged += OnGoldChanged;
                RefreshAffordability(Economy.Instance.Gold);
            }

            if (PlacementController.Instance != null)
                PlacementController.Instance.OnTowerPlaced += OnTowerPlaced;

            L.OnLocaleChanged += RefreshTooltipText;
        }

        private void OnDestroy()
        {
            if (Economy.Instance != null)
                Economy.Instance.OnGoldChanged -= OnGoldChanged;
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnTowerPlaced -= OnTowerPlaced;
            L.OnLocaleChanged -= RefreshTooltipText;
        }

        private void OnTowerPlaced(Tower _) => RefreshSelection();

        private void BuildTooltipEl()
        {
            if (toolbarRoot == null) return;

            tooltipEl = new VisualElement();
            tooltipEl.AddToClassList("toolbar-tooltip");
            tooltipEl.AddToClassList("hidden");

            tooltipName = new Label();
            tooltipName.AddToClassList("toolbar-tooltip-name");
            tooltipEl.Add(tooltipName);

            tooltipDesc = new Label();
            tooltipDesc.AddToClassList("toolbar-tooltip-desc");
            tooltipEl.Add(tooltipDesc);

            toolbarRoot.Add(tooltipEl);
        }

        private void BuildCells()
        {
            if (towerRegistry == null || toolbarRoot == null) return;

            towerOrder.Clear();
            cells.Clear();

            var towers = towerRegistry.Towers;
            for (int i = 0; i < towers.Length; i++)
            {
                var tower = towers[i];
                if (tower == null) continue;

                string keyLabel = i < 9 ? $"{i + 1}" : i == 9 ? "0" : i == 10 ? "-" : "=";

                var cell = new VisualElement();
                cell.AddToClassList("toolbar-cell");
                cell.name = $"toolbar-cell-{tower.Id}";

                var keyLbl = new Label(keyLabel);
                keyLbl.AddToClassList("toolbar-key");
                cell.Add(keyLbl);

                var iconLbl = new Label(tower.IconEmoji);
                iconLbl.AddToClassList("toolbar-icon");
                cell.Add(iconLbl);

                var costLbl = new Label($"{tower.Cost}");
                costLbl.AddToClassList("toolbar-cost");
                cell.Add(costLbl);

                int capturedIdx = towerOrder.Count;
                cell.RegisterCallback<ClickEvent>(_ => OnCellClick(capturedIdx));
                cell.RegisterCallback<MouseEnterEvent>(_ => ShowTooltip(capturedIdx, cell));
                cell.RegisterCallback<MouseLeaveEvent>(_ => HideTooltip());

                // Mobile long-press
                cell.RegisterCallback<PointerDownEvent>(_ => StartLongPress(capturedIdx));
                cell.RegisterCallback<PointerUpEvent>(_ => CancelLongPress());
                cell.RegisterCallback<PointerLeaveEvent>(_ => CancelLongPress());

                toolbarRoot.Add(cell);
                towerOrder.Add(tower);
                cells.Add(cell);
            }

            RefreshSelection();
            if (Economy.Instance != null)
                RefreshAffordability(Economy.Instance.Gold);
        }

        private void Update()
        {
            HandleHotkeys();
            HandleLongPress();
        }

        private void HandleHotkeys()
        {
            for (int i = 0; i < HotkeyMap.Length && i < towerOrder.Count; i++)
            {
                if (Input.GetKeyDown(HotkeyMap[i]))
                {
                    OnCellClick(i);
                    return;
                }
            }
        }

        private void HandleLongPress()
        {
            if (longPressIdx < 0) return;
            longPressTimer += Time.unscaledDeltaTime;
            if (longPressTimer >= LongPressSec)
            {
                if (longPressIdx < cells.Count)
                    ShowTooltip(longPressIdx, cells[longPressIdx]);
                longPressIdx = -1;
            }
        }

        private void StartLongPress(int idx)
        {
            longPressIdx = idx;
            longPressTimer = 0f;
        }

        private void CancelLongPress()
        {
            longPressIdx = -1;
        }

        private void OnCellClick(int idx)
        {
            if (idx < 0 || idx >= towerOrder.Count) return;
            if (PlacementController.Instance == null) return;

            var tower = towerOrder[idx];
            var current = PlacementController.Instance.SelectedTowerType;
            // Toggle: clicking same tower deselects
            PlacementController.Instance.SelectTowerForPlacement(current == tower ? null : tower);
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            var selected = PlacementController.Instance?.SelectedTowerType;
            for (int i = 0; i < cells.Count; i++)
            {
                bool isSelected = selected != null && selected == towerOrder[i];
                if (isSelected) cells[i].AddToClassList("toolbar-cell--selected");
                else cells[i].RemoveFromClassList("toolbar-cell--selected");
            }
        }

        private void OnGoldChanged(int gold) => RefreshAffordability(gold);

        private void RefreshAffordability(int gold)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                bool poor = gold < towerOrder[i].Cost;
                if (poor) cells[i].AddToClassList("toolbar-cell--poor");
                else cells[i].RemoveFromClassList("toolbar-cell--poor");
            }
        }

        private void ShowTooltip(int idx, VisualElement anchor)
        {
            if (tooltipEl == null || tooltipName == null || tooltipDesc == null) return;
            if (idx < 0 || idx >= towerOrder.Count) return;

            var tower = towerOrder[idx];
            int gold = Economy.Instance?.Gold ?? 0;
            bool poor = gold < tower.Cost;

            tooltipName.text = $"{L.Get($"tower.{tower.Id}.name", "Towers")}  {tower.Cost}";
            tooltipDesc.text = poor
                ? L.Get("toolbar.not_enough_gold")
                : L.Get($"tower.{tower.Id}.desc", "Towers");

            if (poor) tooltipEl.AddToClassList("toolbar-tooltip--poor");
            else tooltipEl.RemoveFromClassList("toolbar-tooltip--poor");

            // Position: above the anchor cell, horizontally clamped
            tooltipEl.RemoveFromClassList("hidden");
        }

        private void HideTooltip()
        {
            tooltipEl?.AddToClassList("hidden");
        }

        private void RefreshTooltipText()
        {
            HideTooltip();
            // Rebuild cells to pick up new locale for cost labels (icon/cost don't change but names refresh on next hover)
        }
    }
}
