#nullable enable
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Panel sticky bottom-right : stats détaillées d'une tour sélectionnée.
    // Affiché sur PlacementController.OnTowerSelected(tower), caché sur null.
    // Distinct du tooltip hover (TowerTooltipController) et du mini-panel top-right (TowerStatsPanel).
    public class TowerInfoPanel : MonoBehaviour
    {
        public static TowerInfoPanel? Instance { get; private set; }

        private VisualElement? _panel;
        private VisualElement? _portrait;
        private Label? _nameLabel;
        private Label? _levelLabel;
        private Label? _branchLabel;
        private Label? _dpsLiveLabel;
        private Label? _statsLabel;
        private Label? _totalDmgLabel;
        private Label? _killsLabel;
        private Button? _sellBtn;

        private Tower? _current;
        private float  _refreshTimer;
        private const float RefreshInterval = 0.2f;
        private readonly StringBuilder _sb = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnTowerSelected -= OnTowerSelected;
        }

        private void Start()
        {
            var doc = GetComponent<UIDocument>() ?? FindFirstObjectByType<UIDocument>();
            if (doc == null) { enabled = false; return; }
            var root = doc.rootVisualElement;

            _panel       = root.Q<VisualElement>("tower-info-panel");
            _portrait    = root.Q<VisualElement>("info-portrait");
            _nameLabel   = root.Q<Label>("info-name");
            _levelLabel  = root.Q<Label>("info-level");
            _branchLabel = root.Q<Label>("info-branch");
            _dpsLiveLabel= root.Q<Label>("info-dps-live");
            _statsLabel  = root.Q<Label>("info-stats");
            _totalDmgLabel = root.Q<Label>("info-total-dmg");
            _killsLabel  = root.Q<Label>("info-kills");
            _sellBtn     = root.Q<Button>("info-sell-btn");

            if (_sellBtn != null)
                _sellBtn.RegisterCallback<ClickEvent>(_ => OnSellClick());

            if (PlacementController.Instance != null)
                PlacementController.Instance.OnTowerSelected += OnTowerSelected;

            Hide();
        }

        private void Update()
        {
            if (_current == null || _panel == null) return;
            _refreshTimer -= Time.deltaTime;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = RefreshInterval;
                PopulateLive(_current);
            }
        }

        private void OnTowerSelected(Tower? tower)
        {
            if (tower == null) { Hide(); return; }
            _current = tower;
            _refreshTimer = 0f;
            PopulateAll(tower);
            Show();
        }

        private void PopulateAll(Tower tower)
        {
            var cfg = tower.Config;
            if (cfg == null) { Hide(); return; }

            string displayName = string.IsNullOrEmpty(cfg.DisplayName) ? cfg.Id : cfg.DisplayName;
            if (_nameLabel != null)  _nameLabel.text  = displayName;
            if (_levelLabel != null) _levelLabel.text = $"L{tower.UpgradeLevel}";

            // Portrait teinté selon Config.ProjectileColor
            if (_portrait != null)
                _portrait.style.backgroundColor = new StyleColor(cfg.ProjectileColor);

            // Branche L3
            if (_branchLabel != null)
            {
                if (tower.UpgradeLevel >= 3 && tower.UpgradeBranch != TowerBranch.None)
                    _branchLabel.text = tower.UpgradeBranch == TowerBranch.Dps ? "Branche DPS" : "Branche Utility";
                else
                    _branchLabel.text = string.Empty;
            }

            // Stats fixes (dégâts, portée, cadence, cible)
            if (_statsLabel != null)
            {
                var bal = BalanceConfig.Get();
                float[] scales = bal.LevelScale;
                int idx = Mathf.Clamp(tower.UpgradeLevel - 1, 0, scales.Length - 1);
                float scale = scales.Length > idx ? scales[idx] : 1f;
                float effDmg = cfg.Damage * bal.TowerDamageMul * scale * tower.L3DmgMul;

                _sb.Clear();
                _sb.Append("Degats : ");
                _sb.Append(effDmg.ToString("F1"));
                if (tower._buffMul > 1.01f)
                {
                    _sb.Append(" (x");
                    _sb.Append(tower._buffMul.ToString("F2"));
                    _sb.Append(')');
                }
                _sb.Append('\n');
                _sb.Append("Portee : ");
                _sb.Append(cfg.Range.ToString("F1"));
                _sb.Append("m\n");
                float rateMs = cfg.FireRateMs * tower.L3FireRateMul;
                float rps = rateMs > 0f ? 1000f / rateMs : 0f;
                _sb.Append("Cadence : ");
                _sb.Append(rps.ToString("F2"));
                _sb.Append("/s\n");
                _sb.Append("Cible : ");
                _sb.Append(tower.CurrentTargetPriority.ToString());
                _statsLabel.text = _sb.ToString();
            }

            // Sell button label
            if (_sellBtn != null)
            {
                var b2 = BalanceConfig.Get();
                int refund = Mathf.RoundToInt(tower.CumulativeCost * b2.SellRefundRatio);
                _sellBtn.text = $"Vendre (+{refund}¢)";
            }

            PopulateLive(tower);
        }

        private void PopulateLive(Tower tower)
        {
            if (_dpsLiveLabel != null)
                _dpsLiveLabel.text = $"DPS Live : {tower.GetLiveDps():F1}";

            if (_totalDmgLabel != null)
                _totalDmgLabel.text = $"Total dmg : {tower.TotalDamageDealt:F0}";

            if (_killsLabel != null)
                _killsLabel.text = $"Kills : {tower.TotalKills}";
        }

        private void OnSellClick()
        {
            if (_current == null) return;
            // Delegate to RadialMenuController which owns the sell flow (refund + notify + deselect)
            RadialMenuController.Instance?.TrySellCurrentTower();
        }

        public void Show() => _panel?.RemoveFromClassList("hidden");
        public void Hide()
        {
            _panel?.AddToClassList("hidden");
            _current = null;
        }
    }
}
