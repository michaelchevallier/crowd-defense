#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    public class TowerToolbarController : UIControllerBase
    {
        [SerializeField] private TowerRegistry? towerRegistry;

        private VisualElement? toolbarRoot;
        private VisualElement? tooltipEl;
        private Label? tooltipName;
        private Label? tooltipDesc;
        private VisualElement? tooltipBehaviorsRow;
        private VisualElement? synTooltipEl;

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

        // Start (not Awake) — shared UIDocument's root is populated by its OnEnable; Awake
        // races that lifecycle. ResolveUI() in Start guarantees Root is bound.
        private void Start()
        {
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            if (Root == null) return;
            toolbarRoot = Root.Q<VisualElement>("tower-toolbar");

            if (towerRegistry == null || toolbarRoot == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[TowerToolbar] TowerRegistry={towerRegistry}, toolbar-root={toolbarRoot}");
#endif
                return;
            }

            // Ensure toolbar is visible
            toolbarRoot.style.display = DisplayStyle.Flex;

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

            tooltipBehaviorsRow = new VisualElement();
            tooltipBehaviorsRow.AddToClassList("behaviors-row");
            tooltipEl.Add(tooltipBehaviorsRow);

            toolbarRoot.Add(tooltipEl);

            // Synergy tooltip — separate floating element
            synTooltipEl = new VisualElement();
            synTooltipEl.AddToClassList("tt-syn-tip");
            synTooltipEl.AddToClassList("hidden");
            toolbarRoot.Add(synTooltipEl);
        }

        private void BuildCells()
        {
            if (towerRegistry == null || toolbarRoot == null) return;

            towerOrder.Clear();
            cells.Clear();

            var currentLevel = LevelRunner.Instance?.CurrentLevel;
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

                // Apply forbidden state if tower is in current level's forbidden list
                if (currentLevel != null && currentLevel.ForbiddenTowers != null &&
                    currentLevel.ForbiddenTowers.Contains(tower))
                {
                    cell.AddToClassList("toolbar-cell--forbidden");
                }

                // Apply locked state based on world progression
                bool isLocked = IsTowerLocked(tower);
                cell.EnableInClassList("toolbar-cell--locked", isLocked);

                int capturedIdx = towerOrder.Count;
                cell.RegisterCallback<ClickEvent>(_ => OnCellClick(capturedIdx));
                cell.RegisterCallback<MouseEnterEvent>(_ => { ShowTooltip(capturedIdx, cell); ShowSynergyTooltip(capturedIdx, cell); });
                cell.RegisterCallback<MouseLeaveEvent>(_ => { HideTooltip(); HideSynergyTooltip(); });

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
            var currentLevel = LevelRunner.Instance?.CurrentLevel;

            // Block click if tower is forbidden or locked
            if (currentLevel != null && currentLevel.ForbiddenTowers != null &&
                currentLevel.ForbiddenTowers.Contains(tower))
            {
                return;
            }
            if (IsTowerLocked(tower)) return;

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

            PopulateBehaviorBadges(tower);

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

        // ── P1-UI-3 : Behavior badges ─────────────────────────────────────────
        private void PopulateBehaviorBadges(TowerType tower)
        {
            if (tooltipBehaviorsRow == null) return;
            tooltipBehaviorsRow.Clear();
            var behaviors = tower.Behaviors;
            if (behaviors == null || behaviors.Count == 0)
            {
                tooltipBehaviorsRow.style.display = DisplayStyle.None;
                return;
            }
            tooltipBehaviorsRow.style.display = DisplayStyle.Flex;
            foreach (var b in behaviors)
            {
                var chip = new Label(BehaviorEmoji(b) + " " + b);
                chip.AddToClassList("behavior-badge");
                tooltipBehaviorsRow.Add(chip);
            }
        }

        private static string BehaviorEmoji(string b) => b switch
        {
            "explosion" => "\U0001F4A5",  // 💥
            "perce"     => "\U0001F3AF",  // 🎯
            "slow"      => "\U0001F40C",  // 🐌
            "aura"      => "\U0001F52E",  // 🔮
            "freeze"    => "\U0001F9CA",  // 🧊
            "poison"    => "\U0001F9AA",  // 🧪
            "pull"      => "\U0001F9F2",  // 🧲
            _           => "\U00002699",  // ⚙
        };

        // ── P1-UI-4 : Synergy tooltip ────────────────────────────────────────
        private void ShowSynergyTooltip(int idx, VisualElement cell)
        {
            if (synTooltipEl == null) return;
            if (idx < 0 || idx >= towerOrder.Count) return;

            var tower = towerOrder[idx];
            var syns = tower.Synergies;
            if (syns == null || syns.Count == 0)
            {
                synTooltipEl.AddToClassList("hidden");
                return;
            }

            synTooltipEl.Clear();
            foreach (var s in syns)
            {
                string line = BuildSynergyLine(s);
                if (string.IsNullOrEmpty(line)) continue;
                var lbl = new Label(line);
                lbl.AddToClassList("syn-tip-line");
                synTooltipEl.Add(lbl);
            }

            if (synTooltipEl.childCount == 0)
            {
                synTooltipEl.AddToClassList("hidden");
                return;
            }

            synTooltipEl.RemoveFromClassList("hidden");
            synTooltipEl.style.position = Position.Absolute;
            synTooltipEl.style.left = new StyleLength(cell.worldBound.xMax + 4f);
            synTooltipEl.style.top  = new StyleLength(cell.worldBound.y);
        }

        private static string BuildSynergyLine(SynergyDef s)
        {
            if (!string.IsNullOrEmpty(s.from))
                return $"+ {s.from} : {DescribeSynergyEffect(s)}";
            return DescribeSynergyEffect(s);
        }

        private static string DescribeSynergyEffect(SynergyDef s)
        {
            if (s.dmgMul > 1f)          return $"+{Mathf.RoundToInt((s.dmgMul - 1f) * 100)}% DMG";
            if (s.pierceMega)           return "Pierce x99";
            if (s.pierceBonus > 0)      return $"+{s.pierceBonus} Pierce";
            if (s.multiShotBonus > 0)   return $"+{s.multiShotBonus} MultiShot";
            if (s.flyerDmgBonus > 1f)   return $"+{Mathf.RoundToInt((s.flyerDmgBonus - 1f) * 100)}% Flyer DMG";
            if (s.freezeOnHit)          return "Freeze on hit";
            if (s.slowOnHit.mul > 0f)   return $"Slow {Mathf.RoundToInt(s.slowOnHit.mul * 100)}% on hit";
            if (s.appliesSlow.mul > 0f) return $"Apply slow {Mathf.RoundToInt(s.appliesSlow.mul * 100)}%";
            if (s.cascadeRadius > 0f)   return $"Cascade AoE {s.cascadeRadius:0.#}";
            if (s.knockbackOnHit > 0f)  return $"Knockback {s.knockbackOnHit:0.#}";
            if (s.pullToTank)           return "Pull enemies to tank";
            if (s.propagateDebuff)      return "Propagate debuff";
            if (s.coinMul > 1f)         return $"Coins x{s.coinMul:0.#}";
            if (s.slowArea.mul > 0f)    return $"Slow area {Mathf.RoundToInt(s.slowArea.mul * 100)}%";
            return "";
        }

        private void HideSynergyTooltip()
        {
            synTooltipEl?.AddToClassList("hidden");
        }

        // ── P1-UI-5 : Locked state ───────────────────────────────────────────
        private static bool IsTowerLocked(TowerType tower)
        {
            if (tower.UnlockWorld <= 1) return false;
            int worldReached = SaveSystem.Load().worldReached;
            return worldReached < tower.UnlockWorld;
        }
    }
}
