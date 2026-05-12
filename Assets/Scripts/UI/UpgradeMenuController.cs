#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Affiche le modal de choix de branche L3 pour les tours signature (D1-03).
    /// Invoqué par RadialMenuController.ShowL3Choice quand la tour est à L2 signature.
    /// Player clique DPS ou Utility → applique Tower.UpgradeTo(3, branch) + ferme modal.
    /// </summary>
    public class UpgradeMenuController : MonoBehaviour
    {
        public static UpgradeMenuController? Instance { get; private set; }

        private VisualElement? _panel;
        private Label? _titleLabel;
        private Label? _subLabel;
        private Button? _dpsCard;
        private Label? _dpsNameLabel;
        private Label? _dpsStatsLabel;
        private Label? _dpsCostLabel;
        private Button? _utilityCard;
        private Label? _utilityNameLabel;
        private Label? _utilityStatsLabel;
        private Label? _utilityCostLabel;
        private Button? _cancelBtn;

        private Tower? _pendingTower;

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

            _panel          = root.Q<VisualElement>("l3-choice-panel");
            _titleLabel     = root.Q<Label>("l3-choice-title");
            _subLabel       = root.Q<Label>("l3-choice-sub");
            _dpsCard        = root.Q<Button>("l3-dps-card");
            _dpsNameLabel   = root.Q<Label>("l3-dps-label");
            _dpsStatsLabel  = root.Q<Label>("l3-dps-stats");
            _dpsCostLabel   = root.Q<Label>("l3-dps-cost");
            _utilityCard    = root.Q<Button>("l3-utility-card");
            _utilityNameLabel  = root.Q<Label>("l3-utility-label");
            _utilityStatsLabel = root.Q<Label>("l3-utility-stats");
            _utilityCostLabel  = root.Q<Label>("l3-utility-cost");
            _cancelBtn      = root.Q<Button>("l3-cancel-btn");

            _dpsCard?.RegisterCallback<ClickEvent>(_ => OnBranchChosen(TowerBranch.Dps));
            _utilityCard?.RegisterCallback<ClickEvent>(_ => OnBranchChosen(TowerBranch.Utility));
            _cancelBtn?.RegisterCallback<ClickEvent>(_ => Hide());

            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Affiche le modal pour la tour signature t (doit être L2).
        /// </summary>
        public void ShowL3Choice(Tower t)
        {
            if (t.Config == null) return;
            _pendingTower = t;

            var cfg = t.Config;
            var bal = BalanceConfig.Get();
            int l3Cost = Mathf.RoundToInt(cfg.Cost * bal.UpgradeMulL3);
            int gold = Economy.Instance?.Gold ?? 0;
            bool canAfford = gold >= l3Cost;

            string displayName = string.IsNullOrEmpty(cfg.DisplayName) ? cfg.Id : cfg.DisplayName;
            var tid = TowerIdExtensions.FromKey(cfg.Id);

            if (_titleLabel != null) _titleLabel.text = L.Get("hud.l3_choice_title");
            if (_subLabel != null)   _subLabel.text   = L.Get("hud.l3_choice_sub", displayName);

            PopulateCard(_dpsNameLabel, _dpsStatsLabel, _dpsCostLabel, _dpsCard,
                         DpsLabelKey(tid), DpsHintKey(tid), l3Cost, canAfford);
            PopulateCard(_utilityNameLabel, _utilityStatsLabel, _utilityCostLabel, _utilityCard,
                         UtilityLabelKey(tid), UtilityHintKey(tid), l3Cost, canAfford);

            if (_cancelBtn != null) _cancelBtn.text = L.Get("hud.l3_choice_cancel");

            _panel?.RemoveFromClassList("hidden");
        }

        public void Hide()
        {
            _panel?.AddToClassList("hidden");
            _pendingTower = null;
        }

        private void OnBranchChosen(TowerBranch branch)
        {
            var tower = _pendingTower;
            Hide();
            if (tower == null) return;

            bool ok = tower.UpgradeTo(3, branch);
            if (!ok) return;

            tower.ApplyL3Tint();
            PlacementController.Instance?.NotifyTowerUpgraded(tower, 3);
            RadialMenuController.Instance?.RefreshCurrentTower();
        }

        private static void PopulateCard(
            Label? nameLabel, Label? statsLabel, Label? costLabel, Button? card,
            string nameKey, string hintKey, int cost, bool canAfford)
        {
            if (nameLabel != null)  nameLabel.text  = L.Get(nameKey);
            if (statsLabel != null) statsLabel.text = L.Get(hintKey);
            if (costLabel != null)  costLabel.text  = $"{cost}g";
            card?.SetEnabled(canAfford);
        }

        private static string DpsLabelKey(TowerId id) => id switch
        {
            TowerId.Archer   => "hud.radial_dps.archer",
            TowerId.Mage     => "hud.radial_dps.mage",
            TowerId.Ballista => "hud.radial_dps.ballista",
            TowerId.Cannon   => "hud.radial_dps.cannon",
            _                => "hud.radial_branch_dps",
        };

        private static string DpsHintKey(TowerId id) => id switch
        {
            TowerId.Archer   => "hud.radial_dps_hint.archer",
            TowerId.Mage     => "hud.radial_dps_hint.mage",
            TowerId.Ballista => "hud.radial_dps_hint.ballista",
            TowerId.Cannon   => "hud.radial_dps_hint.cannon",
            _                => "hud.radial_dps_hint.default",
        };

        private static string UtilityLabelKey(TowerId id) => id switch
        {
            TowerId.Archer   => "hud.radial_util.archer",
            TowerId.Mage     => "hud.radial_util.mage",
            TowerId.Ballista => "hud.radial_util.ballista",
            TowerId.Cannon   => "hud.radial_util.cannon",
            _                => "hud.radial_branch_utility",
        };

        private static string UtilityHintKey(TowerId id) => id switch
        {
            TowerId.Archer   => "hud.radial_util_hint.archer",
            TowerId.Mage     => "hud.radial_util_hint.mage",
            TowerId.Ballista => "hud.radial_util_hint.ballista",
            TowerId.Cannon   => "hud.radial_util_hint.cannon",
            _                => "hud.radial_util_hint.default",
        };
    }
}
