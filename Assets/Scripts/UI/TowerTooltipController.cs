#nullable enable
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Affiche un tooltip compact (hover tower) dans le panneau tower-tooltip du HUD.
    // Piloté par TowerHoverController via Show/Hide — pas de detection hover interne.
    // Partage le UIDocument de HudController (sibling component, même GameObject).
    public class TowerTooltipController : MonoBehaviour
    {
        public static TowerTooltipController? Instance { get; private set; }

        private const float OffsetX = 14f;
        private const float OffsetY = 14f;

        private VisualElement? _tooltipRoot;
        private Label? _tooltipHeader;
        private Label? _tooltipStats;
        private Label? _tooltipSynergies;

        private bool _visible;
        private Tower? _currentTower;
        private float _refreshTimer;
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
        }

        private void Start()
        {
            var doc = GetComponent<UIDocument>() ?? FindAnyObjectByType<UIDocument>();
            if (doc == null) return;
            var root = doc.rootVisualElement;
            _tooltipRoot     = root.Q<VisualElement>("tower-tooltip");
            _tooltipHeader   = root.Q<Label>("tooltip-header");
            _tooltipStats    = root.Q<Label>("tooltip-stats");
            _tooltipSynergies = root.Q<Label>("tooltip-synergies");
            Hide();
        }

        private void Update()
        {
            if (!_visible || _tooltipRoot == null) return;

            var mp = Input.mousePosition;
            float uiX = mp.x + OffsetX;
            float uiY = Screen.height - mp.y + OffsetY;

            // Clamp pour rester visible (largeur max tooltip = 280 px)
            if (uiX + 280f > Screen.width) uiX = mp.x - 280f - OffsetX;

            _tooltipRoot.style.left = new Length(uiX, LengthUnit.Pixel);
            _tooltipRoot.style.top  = new Length(uiY, LengthUnit.Pixel);

            // Refresh live DPS every 0.2s
            if (_currentTower != null)
            {
                _refreshTimer -= Time.deltaTime;
                if (_refreshTimer <= 0f)
                {
                    _refreshTimer = RefreshInterval;
                    PopulateTooltip(_currentTower);
                }
            }
        }

        public void Show(Tower tower)
        {
            if (_tooltipRoot == null) return;
            _currentTower = tower;
            _refreshTimer = 0f;
            PopulateTooltip(tower);
            _tooltipRoot.RemoveFromClassList("hidden");
            _visible = true;
        }

        public void Hide()
        {
            _tooltipRoot?.AddToClassList("hidden");
            _currentTower = null;
            _visible = false;
        }

        private void PopulateTooltip(Tower tower)
        {
            var cfg = tower.Config;
            if (cfg == null) return;

            // ── Header ───────────────────────────────────────────────────────────
            _sb.Clear();
            string name = string.IsNullOrEmpty(cfg.DisplayName) ? cfg.Id : cfg.DisplayName;
            _sb.Append(name);
            _sb.Append("  L");
            _sb.Append(tower.UpgradeLevel);
            if (tower.UpgradeBranch != TowerBranch.None)
            {
                _sb.Append(" (");
                _sb.Append(tower.UpgradeBranch == TowerBranch.Dps ? "DPS" : "Utility");
                _sb.Append(')');
            }
            _sb.Append("   ");
            _sb.Append(tower.CumulativeCost);
            _sb.Append("c investi");
            if (_tooltipHeader != null) _tooltipHeader.text = _sb.ToString();

            // ── Stats ────────────────────────────────────────────────────────────
            _sb.Clear();

            float effectiveDmg = cfg.Damage * tower._buffMul;
            _sb.Append("Degats: ");
            _sb.Append(effectiveDmg.ToString("F1"));
            if (tower._buffMul > 1.01f)
            {
                _sb.Append(" (x");
                _sb.Append(tower._buffMul.ToString("F2"));
                _sb.Append(')');
            }
            _sb.Append('\n');

            _sb.Append("Portee: ");
            _sb.Append(cfg.Range.ToString("F1"));
            _sb.Append("m\n");

            _sb.Append("Cadence: ");
            if (cfg.FireRateMs <= 0f)
                _sb.Append("Continu");
            else
            {
                float ratePerSec = 1000f / cfg.FireRateMs;
                _sb.Append(ratePerSec.ToString("F1"));
                _sb.Append(" tirs/s");
            }
            _sb.Append('\n');

            float liveDps = tower.GetLiveDps();
            _sb.Append("DPS Live: ");
            _sb.Append(liveDps.ToString("F1"));

            if (tower.L3CritChance > 0f)
            {
                _sb.Append("\nCrit: ");
                _sb.Append((tower.L3CritChance * 100f).ToString("F0"));
                _sb.Append('%');
            }

            _sb.Append("\nTues: ");
            _sb.Append(tower.TotalKills);

            var bal = BalanceConfig.Get();
            int refund = Mathf.RoundToInt(tower.CumulativeCost * bal.SellRefundRatio);
            _sb.Append("\nVendre: ");
            _sb.Append(refund);
            _sb.Append("c (80%)");

            if (_tooltipStats != null) _tooltipStats.text = _sb.ToString();

            // ── Synergies ────────────────────────────────────────────────────────
            _sb.Clear();
            int n = 0;

            if (tower._buffMul > 1.01f && n++ < 5)
            {
                _sb.Append("Aura DMG x");
                _sb.Append(tower._buffMul.ToString("F2"));
                _sb.Append('\n');
            }
            if (tower._pierceBonus > 0 && n++ < 5)
            {
                _sb.Append("Pierce +");
                _sb.Append(tower._pierceBonus == 99 ? "INF" : tower._pierceBonus.ToString());
                _sb.Append('\n');
            }
            if (tower._multiShotBonus > 0 && n++ < 5)
            {
                _sb.Append("MultiShot +");
                _sb.Append(tower._multiShotBonus);
                _sb.Append('\n');
            }
            if (tower._slowOnHitActive && n++ < 5)
            {
                _sb.Append("Ralentit on-hit\n");
            }
            if (tower._freezeOnHitActive && n++ < 5)
            {
                _sb.Append("Gele on-hit\n");
            }
            if (tower._pullActive && n++ < 5)
            {
                _sb.Append("Attraction\n");
            }

            if (_tooltipSynergies != null)
                _tooltipSynergies.text = _sb.ToString().TrimEnd('\n');
        }
    }
}
