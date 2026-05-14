#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Displays a detailed stats panel (top-right) when a placed tower is selected.
    /// Listens to PlacementController.OnTowerSelected.
    /// Compatible with RadialMenuController: selecting a tower shows stats (radial opens separately).
    /// Hiding: deselect (click outside / ESC) → PlacementController fires null → panel hides.
    /// </summary>
    public class TowerStatsPanel : MonoBehaviour
    {
        private VisualElement? _panel;
        private Label? _nameLabel;
        private Label? _levelLabel;
        private Label? _dpsValue;
        private Label? _rangeValue;
        private Label? _fireRateValue;
        private Label? _dmgValue;
        private Label? _specialLabel;

        private void Start()
        {
            var doc = GetComponent<UIDocument>() ?? FindAnyObjectByType<UIDocument>();
            if (doc == null) { enabled = false; return; }
            var root = doc.rootVisualElement;

            _panel         = root.Q<VisualElement>("tower-stats-panel");
            _nameLabel     = root.Q<Label>("stats-tower-name");
            _levelLabel    = root.Q<Label>("stats-tower-level");
            _dpsValue      = root.Q<Label>("stats-dps-value");
            _rangeValue    = root.Q<Label>("stats-range-value");
            _fireRateValue = root.Q<Label>("stats-firerate-value");
            _dmgValue      = root.Q<Label>("stats-dmg-value");
            _specialLabel  = root.Q<Label>("stats-special");

            if (PlacementController.Instance != null)
                PlacementController.Instance.OnTowerSelected += OnTowerSelected;

            Hide();
        }

        private void OnDestroy()
        {
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnTowerSelected -= OnTowerSelected;
        }

        private void OnTowerSelected(Tower? tower)
        {
            if (tower == null) { Hide(); return; }
            Populate(tower);
            Show();
        }

        private void Populate(Tower tower)
        {
            var cfg = tower.Config;
            if (cfg == null) { Hide(); return; }

            string displayName = string.IsNullOrEmpty(cfg.DisplayName) ? cfg.Id : cfg.DisplayName;
            if (_nameLabel != null)  _nameLabel.text  = displayName;
            if (_levelLabel != null) _levelLabel.text = $"L{tower.UpgradeLevel}";

            // Compute effective damage with level scale (mirrors Tower.Fire logic)
            var bal = BalanceConfig.Get();
            float[] scales = bal.LevelScale;
            int scaleIdx = Mathf.Clamp(tower.UpgradeLevel - 1, 0, scales.Length - 1);
            float levelScale = scales.Length > scaleIdx ? scales[scaleIdx] : 1f;

            float effectiveDmg = cfg.Damage * bal.TowerDamageMul * levelScale * tower.L3DmgMul;

            // DPS = damage / (fireRateMs / 1000)
            float fireRateSec = cfg.FireRateMs * tower.L3FireRateMul / 1000f;
            float dps = fireRateSec > 0f ? effectiveDmg / fireRateSec : 0f;

            if (_dpsValue != null)      _dpsValue.text      = $"{dps:F1}";
            if (_rangeValue != null)    _rangeValue.text     = $"{cfg.Range:F1}";
            if (_fireRateValue != null) _fireRateValue.text  = fireRateSec > 0f ? $"{1f / fireRateSec:F1}/s" : "-";
            if (_dmgValue != null)      _dmgValue.text       = $"{effectiveDmg:F1}";

            if (_specialLabel != null)
                _specialLabel.text = BuildSpecialText(cfg, tower);
        }

        private static string BuildSpecialText(TowerType cfg, Tower tower)
        {
            var parts = new System.Text.StringBuilder();

            // Slow (Frost / Fan behavior or L3 slow)
            bool hasSlow = cfg.Behavior == TowerBehavior.Slow
                || (cfg.SlowMul < 1f && cfg.SlowDurationMs > 0);
            bool hasL3Slow = tower.L3SlowOnHit;
            if (hasSlow || hasL3Slow)
            {
                float mul = hasL3Slow ? tower.L3SlowMul : cfg.SlowMul;
                int durMs = hasL3Slow ? tower.L3SlowDurMs : cfg.SlowDurationMs;
                parts.Append($"Slow {(1f - mul) * 100f:F0}% / {durMs / 1000f:F1}s");
            }

            // AoE
            float aoe = tower.L3Aoe > 0f ? tower.L3Aoe : cfg.Aoe;
            if (aoe > 0f)
            {
                if (parts.Length > 0) parts.Append("  ");
                parts.Append($"AoE {aoe:F1}");
            }

            // Pierce
            int pierce = tower.L3Pierce > 0 ? tower.L3Pierce : cfg.Pierce;
            if (pierce > 0)
            {
                if (parts.Length > 0) parts.Append("  ");
                parts.Append(pierce >= 99 ? "Pierce inf" : $"Pierce {pierce}");
            }

            // Cluster (Mine)
            if (cfg.Behavior == TowerBehavior.Cluster)
            {
                if (parts.Length > 0) parts.Append("  ");
                parts.Append("Cluster");
            }

            // Buff aura (Portal)
            if (cfg.Behavior == TowerBehavior.BuffAura)
            {
                if (parts.Length > 0) parts.Append("  ");
                parts.Append("Aura");
            }

            // L3 burn DoT
            if (tower.L3BurnDot)
            {
                if (parts.Length > 0) parts.Append("  ");
                parts.Append($"Burn {tower.L3BurnDurMs / 1000f:F0}s");
            }

            // L3 branch label (only at L3)
            if (tower.UpgradeLevel >= 3 && tower.UpgradeBranch != TowerBranch.None)
            {
                string branchTag = tower.UpgradeBranch == TowerBranch.Dps ? "[DPS]" : "[Utility]";
                if (parts.Length > 0) parts.Append("  ");
                parts.Append(branchTag);
            }

            return parts.ToString();
        }

        private void Show() => _panel?.RemoveFromClassList("hidden");
        private void Hide() => _panel?.AddToClassList("hidden");
    }
}
