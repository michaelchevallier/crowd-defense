#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Gere le radial menu upgrade/sell/range (D1-03).
    /// Ecoute PlacementController.OnTowerSelected (event).
    /// Segments visibles par etat :
    ///   L1 : Upgrade L2 (cost) | Range | Sell
    ///   L2 signature : Upgrade L3-DPS (cost) | Upgrade L3-Utility (cost) | Range | Sell
    ///   L2 non-signature : Upgrade L3 (cost) | Range | Sell
    ///   L3 : Range | Sell uniquement
    /// Position : Camera.WorldToScreenPoint sur la tour selectionnee.
    /// Fermeture : outside-click (via OnTowerSelected null) ou ESC (PlacementController.DeselectTower).
    /// </summary>
    public class RadialMenuController : MonoBehaviour
    {
        public static RadialMenuController? Instance { get; private set; }

        private static readonly TowerId[] SignatureIds = { TowerId.Archer, TowerId.Mage, TowerId.Ballista, TowerId.Cannon };

        private VisualElement? radialMenu;
        private Label? radialTitle;
        private Button? btnUpgradeL2;
        private Label? btnUpgradeL2Cost;
        private Button? btnDps;
        private Button? btnUtility;
        private Button? btnUpgradeL3;
        private Label? btnUpgradeL3Cost;
        private Button? btnSell;
        private Button? btnRange;
        private Button? btnTarget;
        private Button? btnGuard;
        private Label? btnDpsLabel;
        private Label? btnDpsCost;
        private Label? btnDpsHint;
        private Label? btnUtilityLabel;
        private Label? btnUtilityCost;
        private Label? btnUtilityHint;
        private Label? btnSellLabel;

        private Tower? currentTower;
        private bool _rangeVisible;

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

            radialMenu        = root.Q<VisualElement>("radial-menu");
            radialTitle       = root.Q<Label>("radial-title");
            btnUpgradeL2      = root.Q<Button>("btn-upgrade-l2");
            btnUpgradeL2Cost  = root.Q<Label>("btn-upgrade-l2-cost");
            btnDps            = root.Q<Button>("btn-upgrade-dps");
            btnUtility        = root.Q<Button>("btn-upgrade-utility");
            btnUpgradeL3      = root.Q<Button>("btn-upgrade-l3");
            btnUpgradeL3Cost  = root.Q<Label>("btn-upgrade-l3-cost");
            btnSell           = root.Q<Button>("btn-sell");
            btnRange          = root.Q<Button>("btn-range");
            btnTarget         = root.Q<Button>("btn-target");
            btnGuard          = root.Q<Button>("btn-guard");
            btnDpsLabel       = root.Q<Label>("btn-dps-label");
            btnDpsCost        = root.Q<Label>("btn-dps-cost");
            btnDpsHint        = root.Q<Label>("btn-dps-hint");
            btnUtilityLabel   = root.Q<Label>("btn-utility-label");
            btnUtilityCost    = root.Q<Label>("btn-utility-cost");
            btnUtilityHint    = root.Q<Label>("btn-utility-hint");
            btnSellLabel      = root.Q<Label>("btn-sell-label");

            btnUpgradeL2?.RegisterCallback<ClickEvent>(_ => OnUpgradeL2Clicked());
            btnDps?.RegisterCallback<ClickEvent>(_ => OnUpgradeL3Clicked(TowerBranch.Dps));
            btnUtility?.RegisterCallback<ClickEvent>(_ => OnUpgradeL3Clicked(TowerBranch.Utility));
            btnUpgradeL3?.RegisterCallback<ClickEvent>(_ => OnUpgradeL3Clicked(TowerBranch.None));
            btnSell?.RegisterCallback<ClickEvent>(_ => OnSellClicked());
            btnRange?.RegisterCallback<ClickEvent>(_ => OnRangeClicked());
            btnTarget?.RegisterCallback<ClickEvent>(_ => OnTargetClicked());
            btnGuard?.RegisterCallback<ClickEvent>(_ => OnGuardClicked());

            if (PlacementController.Instance != null)
                PlacementController.Instance.OnTowerSelected += OnTowerSelected;

            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnTowerSelected -= OnTowerSelected;
        }

        private void OnTowerSelected(Tower? tower)
        {
            if (currentTower != null && currentTower != tower)
            {
                currentTower.ShowRangeRing(false);
                _rangeVisible = false;
            }
            currentTower = tower;
            if (tower == null) { Hide(); return; }
            RefreshMenu(tower);
        }

        private void LateUpdate()
        {
            // Keep menu anchored to tower world position each frame while visible
            if (currentTower == null || radialMenu == null) return;
            if (radialMenu.ClassListContains("hidden")) return;
            PositionMenuAtTower(currentTower);
        }

        private void PositionMenuAtTower(Tower tower)
        {
            if (radialMenu == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 screenPos = cam.WorldToScreenPoint(tower.transform.position + Vector3.up * 1.2f);
            if (screenPos.z < 0f) { Hide(); return; }

            // UI Toolkit uses top-left origin; screen coords are bottom-left
            var panel = radialMenu.panel;
            if (panel == null) return;
            float panelH = panel.visualTree.layout.height;
            radialMenu.style.left = screenPos.x - radialMenu.layout.width * 0.5f;
            radialMenu.style.top  = panelH - screenPos.y - radialMenu.layout.height * 0.5f;
        }

        private void RefreshMenu(Tower tower)
        {
            var cfg = tower.Config;
            if (cfg == null) { Hide(); return; }

            string displayName = string.IsNullOrEmpty(cfg.DisplayName) ? cfg.Id : cfg.DisplayName;
            bool isSignature = IsSignature(TowerIdExtensions.FromKey(cfg.Id));
            int gold = Economy.Instance?.Gold ?? 0;
            var bal = BalanceConfig.Get();

            int l2Cost = Mathf.RoundToInt(cfg.Cost * bal.UpgradeMulL2);
            int l3Cost = Mathf.RoundToInt(cfg.Cost * bal.UpgradeMulL3);
            int refund = Mathf.RoundToInt(tower.CumulativeCost * bal.SellRefundRatio);

            // Hide all upgrade buttons, reveal only those relevant to current level
            SetVisible(btnUpgradeL2, false);
            SetVisible(btnDps, false);
            SetVisible(btnUtility, false);
            SetVisible(btnUpgradeL3, false);

            switch (tower.UpgradeLevel)
            {
                case 1:
                    if (radialTitle != null) radialTitle.text = displayName;
                    PopulateL2Button(l2Cost, gold >= l2Cost);
                    SetVisible(btnUpgradeL2, true);
                    break;

                case 2 when isSignature:
                    if (radialTitle != null)
                        radialTitle.text = L.Get("hud.radial_title", displayName);
                    bool canAffordL3 = gold >= l3Cost;
                    var tid = TowerIdExtensions.FromKey(cfg.Id);
                    PopulateDpsButton(tid, l3Cost, canAffordL3);
                    PopulateUtilityButton(tid, l3Cost, canAffordL3);
                    SetVisible(btnDps, true);
                    SetVisible(btnUtility, true);
                    break;

                case 2:
                    if (radialTitle != null) radialTitle.text = displayName;
                    PopulateL3StandardButton(l3Cost, gold >= l3Cost);
                    SetVisible(btnUpgradeL3, true);
                    break;

                case 3:
                    if (radialTitle != null) radialTitle.text = displayName;
                    break;
            }

            if (btnSellLabel != null)
                btnSellLabel.text = L.Get("hud.radial_sell", refund);

            RefreshTargetButton(tower);
            RefreshGuardButton(tower);

            // Sync range button active class to current _rangeVisible state
            if (btnRange != null)
            {
                if (_rangeVisible)
                    btnRange.AddToClassList("radial-btn-range--active");
                else
                    btnRange.RemoveFromClassList("radial-btn-range--active");
            }

            Show();
            PositionMenuAtTower(tower);
        }

        private void PopulateL2Button(int cost, bool canAfford)
        {
            if (btnUpgradeL2Cost != null) btnUpgradeL2Cost.text = $"{cost}g";
            btnUpgradeL2?.SetEnabled(canAfford);
        }

        private void PopulateL3StandardButton(int cost, bool canAfford)
        {
            if (btnUpgradeL3Cost != null) btnUpgradeL3Cost.text = $"{cost}g";
            btnUpgradeL3?.SetEnabled(canAfford);
        }

        private void PopulateDpsButton(TowerId towerId, int cost, bool canAfford)
        {
            if (btnDpsCost != null) btnDpsCost.text = $"{cost}g";

            string labelKey = towerId switch
            {
                TowerId.Archer   => "hud.radial_dps.archer",
                TowerId.Mage     => "hud.radial_dps.mage",
                TowerId.Ballista => "hud.radial_dps.ballista",
                TowerId.Cannon   => "hud.radial_dps.cannon",
                _                => "hud.radial_branch_dps",
            };
            string hintKey = towerId switch
            {
                TowerId.Archer   => "hud.radial_dps_hint.archer",
                TowerId.Mage     => "hud.radial_dps_hint.mage",
                TowerId.Ballista => "hud.radial_dps_hint.ballista",
                TowerId.Cannon   => "hud.radial_dps_hint.cannon",
                _                => "hud.radial_dps_hint.default",
            };
            if (btnDpsLabel != null) btnDpsLabel.text = L.Get(labelKey);
            if (btnDpsHint != null)  btnDpsHint.text  = L.Get(hintKey);
            btnDps?.SetEnabled(canAfford);
        }

        private void PopulateUtilityButton(TowerId towerId, int cost, bool canAfford)
        {
            if (btnUtilityCost != null) btnUtilityCost.text = $"{cost}g";

            string labelKey = towerId switch
            {
                TowerId.Archer   => "hud.radial_util.archer",
                TowerId.Mage     => "hud.radial_util.mage",
                TowerId.Ballista => "hud.radial_util.ballista",
                TowerId.Cannon   => "hud.radial_util.cannon",
                _                => "hud.radial_branch_utility",
            };
            string hintKey = towerId switch
            {
                TowerId.Archer   => "hud.radial_util_hint.archer",
                TowerId.Mage     => "hud.radial_util_hint.mage",
                TowerId.Ballista => "hud.radial_util_hint.ballista",
                TowerId.Cannon   => "hud.radial_util_hint.cannon",
                _                => "hud.radial_util_hint.default",
            };
            if (btnUtilityLabel != null) btnUtilityLabel.text = L.Get(labelKey);
            if (btnUtilityHint != null)  btnUtilityHint.text  = L.Get(hintKey);
            btnUtility?.SetEnabled(canAfford);
        }

        private void OnUpgradeL2Clicked()
        {
            var tower = currentTower;
            if (tower == null) return;

            bool ok = tower.UpgradeTo(2);
            if (!ok) return;

            PlacementController.Instance?.NotifyTowerUpgraded(tower, 2);
#if UNITY_EDITOR
            Debug.Log($"[RadialMenu] Upgrade L2 sur {tower.Config?.Id} ok");
#endif
            RefreshMenu(tower);
        }

        private void OnUpgradeL3Clicked(TowerBranch branch)
        {
            var tower = currentTower;
            if (tower == null) return;

            bool ok = tower.UpgradeTo(3, branch);
            if (!ok) return;

            tower.ApplyL3Tint();
            PlacementController.Instance?.NotifyTowerUpgraded(tower, 3);
#if UNITY_EDITOR
            Debug.Log($"[RadialMenu] Upgrade L3 {branch} sur {tower.Config?.Id} ok");
#endif
            RefreshMenu(tower);
        }

        private void RefreshTargetButton(Tower tower)
        {
            if (btnTarget == null) return;
            string label = tower.CurrentTargetPriority switch
            {
                TargetPriority.First     => "Cible: 1er",
                TargetPriority.Last      => "Cible: Dernier",
                TargetPriority.Strongest => "Cible: Fort",
                TargetPriority.Weakest   => "Cible: Faible",
                TargetPriority.Closest   => "Cible: Proche",
                _                         => "Cible: 1er",
            };
            btnTarget.text = label;
        }

        private void OnTargetClicked()
        {
            if (currentTower == null) return;
            var next = (TargetPriority)(((int)currentTower.CurrentTargetPriority + 1) % 5);
            currentTower.SetTargetPriority(next);
            RefreshTargetButton(currentTower);
        }

        private void RefreshGuardButton(Tower tower)
        {
            if (btnGuard == null) return;
            btnGuard.text = tower.CurrentGuardMode switch
            {
                GuardMode.All        => "Mode: Tout",
                GuardMode.AirOnly    => "Mode: Air",
                GuardMode.GroundOnly => "Mode: Sol",
                _                    => "Mode: Tout",
            };
        }

        private void OnGuardClicked()
        {
            if (currentTower == null) return;
            var next = (GuardMode)(((int)currentTower.CurrentGuardMode + 1) % 3);
            currentTower.SetGuardMode(next);
            RefreshGuardButton(currentTower);
        }

        private void OnRangeClicked()
        {
            if (currentTower == null) return;
            _rangeVisible = !_rangeVisible;
            currentTower.ShowRangeRing(_rangeVisible);
            if (btnRange != null)
            {
                if (_rangeVisible)
                    btnRange.AddToClassList("radial-btn-range--active");
                else
                    btnRange.RemoveFromClassList("radial-btn-range--active");
            }
        }

        // Called by TowerInfoPanel sell button as well as internal btn-sell click
        public void TrySellCurrentTower() => OnSellClicked();

        private void OnSellClicked()
        {
            var tower = currentTower;
            if (tower == null) return;
            tower.ShowRangeRing(false);
            _rangeVisible = false;

            int refund = CrowdDefense.Data.BalanceConfig.Get() is { } bal
                ? UnityEngine.Mathf.RoundToInt(tower.CumulativeCost * bal.SellRefundRatio)
                : 0;

            currentTower = null;
            Hide();
            PlacementController.Instance?.DeselectTower();
            PlacementController.Instance?.NotifyTowerSold(tower, refund);
            tower.Sell();
        }

        private static bool IsSignature(TowerId id) =>
            System.Array.IndexOf(SignatureIds, id) >= 0;

        private static void SetVisible(VisualElement? el, bool visible)
        {
            if (el == null) return;
            el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Show() => radialMenu?.RemoveFromClassList("hidden");

        private void Hide()
        {
            radialMenu?.AddToClassList("hidden");
            if (_rangeVisible && currentTower != null)
                currentTower.ShowRangeRing(false);
            _rangeVisible = false;
            btnRange?.RemoveFromClassList("radial-btn-range--active");
        }
    }
}
