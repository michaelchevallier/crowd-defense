#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Gere le radial menu L3 upgrade (D1-03).
    /// Ecoute PlacementController.SelectedTower chaque frame.
    /// Visible seulement si tower L2 est selectionnee.
    /// Tours signature : 2 boutons DPS/Utility.
    /// Tours non-signature : 1 bouton upgrade standard (hidden — geree par HUD existant).
    /// 1-click direct Q10 (pas de confirmation).
    /// </summary>
    public class RadialMenuController : MonoBehaviour
    {
        private static readonly string[] SignatureIds = { "archer", "mage", "ballista", "cannon" };

        private VisualElement? radialMenu;
        private Label? radialTitle;
        private Button? btnDps;
        private Button? btnUtility;
        private Button? btnSell;
        private Label? btnDpsLabel;
        private Label? btnDpsCost;
        private Label? btnDpsHint;
        private Label? btnUtilityLabel;
        private Label? btnUtilityCost;
        private Label? btnUtilityHint;
        private Label? btnSellLabel;

        private Tower? lastSelectedTower;

        private void Start()
        {
            var doc = GetComponent<UIDocument>() ?? FindFirstObjectByType<UIDocument>();
            if (doc == null) { enabled = false; return; }
            var root = doc.rootVisualElement;
            radialMenu       = root.Q<VisualElement>("radial-menu");
            radialTitle      = root.Q<Label>("radial-title");
            btnDps           = root.Q<Button>("btn-upgrade-dps");
            btnUtility       = root.Q<Button>("btn-upgrade-utility");
            btnSell          = root.Q<Button>("btn-sell");
            btnDpsLabel      = root.Q<Label>("btn-dps-label");
            btnDpsCost       = root.Q<Label>("btn-dps-cost");
            btnDpsHint       = root.Q<Label>("btn-dps-hint");
            btnUtilityLabel  = root.Q<Label>("btn-utility-label");
            btnUtilityCost   = root.Q<Label>("btn-utility-cost");
            btnUtilityHint   = root.Q<Label>("btn-utility-hint");
            btnSellLabel     = root.Q<Label>("btn-sell-label");

            btnDps?.RegisterCallback<ClickEvent>(_ => OnUpgradeClicked(TowerBranch.Dps));
            btnUtility?.RegisterCallback<ClickEvent>(_ => OnUpgradeClicked(TowerBranch.Utility));
            btnSell?.RegisterCallback<ClickEvent>(_ => OnSellClicked());

            Hide();
        }

        private void Update()
        {
            if (PlacementController.Instance == null) { Hide(); return; }
            var tower = PlacementController.Instance.SelectedTower;

            if (tower != lastSelectedTower)
            {
                lastSelectedTower = tower;
                RefreshMenu(tower);
            }
        }

        private void RefreshMenu(Tower? tower)
        {
            if (tower == null || tower.UpgradeLevel != 2)
            {
                Hide();
                return;
            }

            var cfg = tower.Config;
            if (cfg == null) { Hide(); return; }

            bool isSignature = IsSignature(cfg.Id);

            // Pour les tours non-signature en L2, le radial menu ne s'affiche pas
            // (upgrade standard geree par le HUD normal via hotkey U debug)
            if (!isSignature) { Hide(); return; }

            string displayName = string.IsNullOrEmpty(cfg.DisplayName) ? cfg.Id : cfg.DisplayName;

            if (radialTitle != null)
                radialTitle.text = L.Get("hud.radial_title", displayName);

            int l3Cost = Mathf.RoundToInt(cfg.Cost * BalanceConfig.Get().UpgradeMulL3);
            int gold = Economy.Instance?.Gold ?? 0;
            bool canAfford = gold >= l3Cost;

            PopulateDpsButton(cfg.Id, l3Cost, canAfford);
            PopulateUtilityButton(cfg.Id, l3Cost, canAfford);

            int refund = Mathf.RoundToInt(tower.CumulativeCost * BalanceConfig.Get().SellRefundRatio);
            if (btnSellLabel != null)
                btnSellLabel.text = L.Get("hud.radial_sell", refund);

            Show();
        }

        private void PopulateDpsButton(string towerId, int cost, bool canAfford)
        {
            if (btnDpsCost != null) btnDpsCost.text = $"{cost}g";

            string labelKey = towerId switch
            {
                "archer"   => "hud.radial_dps.archer",
                "mage"     => "hud.radial_dps.mage",
                "ballista" => "hud.radial_dps.ballista",
                "cannon"   => "hud.radial_dps.cannon",
                _          => "hud.radial_branch_dps",
            };
            string hintKey = towerId switch
            {
                "archer"   => "hud.radial_dps_hint.archer",
                "mage"     => "hud.radial_dps_hint.mage",
                "ballista" => "hud.radial_dps_hint.ballista",
                "cannon"   => "hud.radial_dps_hint.cannon",
                _          => "hud.radial_dps_hint.default",
            };
            if (btnDpsLabel != null) btnDpsLabel.text = L.Get(labelKey);
            if (btnDpsHint != null)  btnDpsHint.text = L.Get(hintKey);

            if (btnDps != null)
            {
                btnDps.SetEnabled(canAfford);
            }
        }

        private void PopulateUtilityButton(string towerId, int cost, bool canAfford)
        {
            if (btnUtilityCost != null) btnUtilityCost.text = $"{cost}g";

            string labelKey = towerId switch
            {
                "archer"   => "hud.radial_util.archer",
                "mage"     => "hud.radial_util.mage",
                "ballista" => "hud.radial_util.ballista",
                "cannon"   => "hud.radial_util.cannon",
                _          => "hud.radial_branch_utility",
            };
            string hintKey = towerId switch
            {
                "archer"   => "hud.radial_util_hint.archer",
                "mage"     => "hud.radial_util_hint.mage",
                "ballista" => "hud.radial_util_hint.ballista",
                "cannon"   => "hud.radial_util_hint.cannon",
                _          => "hud.radial_util_hint.default",
            };
            if (btnUtilityLabel != null) btnUtilityLabel.text = L.Get(labelKey);
            if (btnUtilityHint != null)  btnUtilityHint.text = L.Get(hintKey);

            if (btnUtility != null)
            {
                btnUtility.SetEnabled(canAfford);
            }
        }

        private void OnUpgradeClicked(TowerBranch branch)
        {
            var tower = lastSelectedTower;
            if (tower == null) return;

            bool ok = tower.UpgradeTo(3, branch);
            if (!ok) return;

            // Appliquer tint visuel L3 immediatement (commit 4)
            tower.ApplyL3Tint();

            // Sync cumulative cost dans PlacementController
            PlacementController.Instance?.SyncCumulativeCost(tower);

#if UNITY_EDITOR
            Debug.Log($"[RadialMenu] Upgrade L3 {branch} sur {tower.Config?.Id} ok");
#endif
            // Cacher le menu apres upgrade (plus de choix disponible)
            Hide();
        }

        private void OnSellClicked()
        {
            var tower = lastSelectedTower;
            if (tower == null) return;
            lastSelectedTower = null;
            Hide();
            tower.Sell();
        }

        private static bool IsSignature(string id) =>
            System.Array.IndexOf(SignatureIds, id) >= 0;

        private void Show() => radialMenu?.RemoveFromClassList("hidden");
        private void Hide() => radialMenu?.AddToClassList("hidden");
    }
}
