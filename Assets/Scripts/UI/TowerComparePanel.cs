#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Modal panel centered 600x300 : compare two towers side-by-side.
    // Opened via Shift+click on a placed tower in PlacementController.
    // First Shift+click selects slot A, second Shift+click populates both columns and shows the panel.
    // Closing (button or ESC) resets both slots.
    public class TowerComparePanel : MonoSingleton<TowerComparePanel>
    {
        private VisualElement? _overlay;

        private VisualElement? _portrait1;
        private Label? _name1;
        private Label? _level1;
        private Label? _dps1;
        private Label? _dmg1;
        private Label? _range1;
        private Label? _rate1;
        private Label? _cost1;
        private Label? _special1;

        private VisualElement? _portrait2;
        private Label? _name2;
        private Label? _level2;
        private Label? _dps2;
        private Label? _dmg2;
        private Label? _range2;
        private Label? _rate2;
        private Label? _cost2;
        private Label? _special2;

        private Label? _hint;
        private Button? _closeBtn;

        private Tower? _slotA;

        public bool IsOpen => _overlay != null && !_overlay.ClassListContains("hidden");
        public Tower? PendingSlotA => _slotA;

        protected override void OnAwakeSingleton() { }

        private void Start()
        {
            var doc = GetComponent<UIDocument>() ?? FindFirstObjectByType<UIDocument>();
            if (doc == null) { enabled = false; return; }
            var root = doc.rootVisualElement;

            _overlay  = root.Q<VisualElement>("compare-overlay");

            _portrait1 = root.Q<VisualElement>("col1-portrait");
            _name1     = root.Q<Label>("col1-name");
            _level1    = root.Q<Label>("col1-level");
            _dps1      = root.Q<Label>("col1-dps");
            _dmg1      = root.Q<Label>("col1-dmg");
            _range1    = root.Q<Label>("col1-range");
            _rate1     = root.Q<Label>("col1-rate");
            _cost1     = root.Q<Label>("col1-cost");
            _special1  = root.Q<Label>("col1-special");

            _portrait2 = root.Q<VisualElement>("col2-portrait");
            _name2     = root.Q<Label>("col2-name");
            _level2    = root.Q<Label>("col2-level");
            _dps2      = root.Q<Label>("col2-dps");
            _dmg2      = root.Q<Label>("col2-dmg");
            _range2    = root.Q<Label>("col2-range");
            _rate2     = root.Q<Label>("col2-rate");
            _cost2     = root.Q<Label>("col2-cost");
            _special2  = root.Q<Label>("col2-special");

            _hint      = root.Q<Label>("compare-hint");
            _closeBtn  = root.Q<Button>("compare-close-btn");

            if (_closeBtn != null)
                _closeBtn.RegisterCallback<ClickEvent>(_ => Close());

            Hide();
        }

        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        // Called by PlacementController on Shift+click over a tower.
        public void RegisterShiftClick(Tower tower)
        {
            if (_slotA == null)
            {
                _slotA = tower;
                UpdateHint();
                return;
            }

            // Both slots filled — open panel.
            var slotB = tower;
            if (_slotA == slotB)
            {
                // Same tower clicked twice — reset.
                _slotA = null;
                UpdateHint();
                return;
            }

            PopulateAndShow(_slotA, slotB);
            _slotA = null;
        }

        private void PopulateAndShow(Tower a, Tower b)
        {
            var cfgA = a.Config;
            var cfgB = b.Config;
            if (cfgA == null || cfgB == null) return;

            var bal = BalanceConfig.Get();
            float dpsA = ComputeDps(a, cfgA, bal);
            float dpsB = ComputeDps(b, cfgB, bal);
            float dmgA = ComputeEffDmg(a, cfgA, bal);
            float dmgB = ComputeEffDmg(b, cfgB, bal);
            float rateA = cfgA.FireRateMs * a.L3FireRateMul > 0f ? 1000f / (cfgA.FireRateMs * a.L3FireRateMul) : 0f;
            float rateB = cfgB.FireRateMs * b.L3FireRateMul > 0f ? 1000f / (cfgB.FireRateMs * b.L3FireRateMul) : 0f;

            PopulateColumn(
                portrait: _portrait1, name: _name1, level: _level1,
                dpsLbl: _dps1, dmgLbl: _dmg1, rangeLbl: _range1, rateLbl: _rate1, costLbl: _cost1, specialLbl: _special1,
                tower: a, cfg: cfgA,
                dps: dpsA, dmg: dmgA, range: cfgA.Range, rate: rateA, cost: cfgA.Cost,
                otherDps: dpsB, otherDmg: dmgB, otherRange: cfgB.Range, otherRate: rateB);

            PopulateColumn(
                portrait: _portrait2, name: _name2, level: _level2,
                dpsLbl: _dps2, dmgLbl: _dmg2, rangeLbl: _range2, rateLbl: _rate2, costLbl: _cost2, specialLbl: _special2,
                tower: b, cfg: cfgB,
                dps: dpsB, dmg: dmgB, range: cfgB.Range, rate: rateB, cost: cfgB.Cost,
                otherDps: dpsA, otherDmg: dmgA, otherRange: cfgA.Range, otherRate: rateA);

            if (_hint != null)
                _hint.style.display = DisplayStyle.None;

            Show();
        }

        private static void PopulateColumn(
            VisualElement? portrait, Label? name, Label? level,
            Label? dpsLbl, Label? dmgLbl, Label? rangeLbl, Label? rateLbl, Label? costLbl, Label? specialLbl,
            Tower tower, TowerType cfg,
            float dps, float dmg, float range, float rate, int cost,
            float otherDps, float otherDmg, float otherRange, float otherRate)
        {
            if (portrait != null)
                portrait.style.backgroundColor = new StyleColor(cfg.ProjectileColor);

            if (name != null)
                name.text = string.IsNullOrEmpty(cfg.DisplayName) ? cfg.Id : cfg.DisplayName;

            if (level != null)
                level.text = $"L{tower.UpgradeLevel}";

            SetValueWithHighlight(dpsLbl,   $"{dps:F1}",          dps,   otherDps,   higherIsBetter: true);
            SetValueWithHighlight(dmgLbl,   $"{dmg:F1}",          dmg,   otherDmg,   higherIsBetter: true);
            SetValueWithHighlight(rangeLbl, $"{range:F1}m",        range, otherRange, higherIsBetter: true);
            SetValueWithHighlight(rateLbl,  $"{rate:F2}/s",        rate,  otherRate,  higherIsBetter: true);

            if (costLbl != null)
            {
                costLbl.text = $"{cost}";
                // No highlight for cost — neutral info.
                costLbl.RemoveFromClassList("compare-stat-better");
                costLbl.RemoveFromClassList("compare-stat-worse");
            }

            if (specialLbl != null)
                specialLbl.text = BuildSpecialText(cfg, tower);
        }

        private static void SetValueWithHighlight(Label? lbl, string text, float val, float other, bool higherIsBetter)
        {
            if (lbl == null) return;
            lbl.text = text;
            lbl.RemoveFromClassList("compare-stat-better");
            lbl.RemoveFromClassList("compare-stat-worse");
            const float epsilon = 0.01f;
            if (Mathf.Abs(val - other) < epsilon) return;
            bool isBetter = higherIsBetter ? val > other : val < other;
            lbl.AddToClassList(isBetter ? "compare-stat-better" : "compare-stat-worse");
        }

        private static float ComputeEffDmg(Tower tower, TowerType cfg, BalanceConfig bal)
        {
            float[] scales = bal.LevelScale;
            int idx = Mathf.Clamp(tower.UpgradeLevel - 1, 0, scales.Length - 1);
            float scale = scales.Length > idx ? scales[idx] : 1f;
            return cfg.Damage * bal.TowerDamageMul * scale * tower.L3DmgMul;
        }

        private static float ComputeDps(Tower tower, TowerType cfg, BalanceConfig bal)
        {
            float effDmg = ComputeEffDmg(tower, cfg, bal);
            float rateMs = cfg.FireRateMs * tower.L3FireRateMul;
            return rateMs > 0f ? effDmg / (rateMs / 1000f) : 0f;
        }

        private static string BuildSpecialText(TowerType cfg, Tower tower)
        {
            var sb = new System.Text.StringBuilder();

            bool hasSlow = cfg.Behavior == TowerBehavior.Slow || (cfg.SlowMul < 1f && cfg.SlowDurationMs > 0);
            if (hasSlow || tower.L3SlowOnHit)
            {
                float mul = tower.L3SlowOnHit ? tower.L3SlowMul : cfg.SlowMul;
                int durMs = tower.L3SlowOnHit ? tower.L3SlowDurMs : cfg.SlowDurationMs;
                sb.Append($"Slow {(1f - mul) * 100f:F0}%/{durMs / 1000f:F1}s");
            }

            float aoe = tower.L3Aoe > 0f ? tower.L3Aoe : cfg.Aoe;
            if (aoe > 0f) { if (sb.Length > 0) sb.Append("  "); sb.Append($"AoE {aoe:F1}"); }

            int pierce = tower.L3Pierce > 0 ? tower.L3Pierce : cfg.Pierce;
            if (pierce > 0) { if (sb.Length > 0) sb.Append("  "); sb.Append(pierce >= 99 ? "Perc.inf" : $"Perc.{pierce}"); }

            if (cfg.Behavior == TowerBehavior.BuffAura) { if (sb.Length > 0) sb.Append("  "); sb.Append("Aura"); }
            if (tower.L3BurnDot) { if (sb.Length > 0) sb.Append("  "); sb.Append($"Burn{tower.L3BurnDurMs / 1000f:F0}s"); }

            return sb.ToString();
        }

        private void UpdateHint()
        {
            if (_hint == null) return;
            _hint.style.display = DisplayStyle.Flex;
            _hint.text = _slotA != null
                ? $"Tour 1 : {_slotA.Config?.DisplayName ?? _slotA.Config?.Id ?? "?"}  —  Shift+clic sur une 2e tour"
                : "Shift+clic sur une tour pour comparer";
        }

        private void Show()  => _overlay?.RemoveFromClassList("hidden");
        private void Hide()  => _overlay?.AddToClassList("hidden");

        public void Close()
        {
            Hide();
            _slotA = null;
        }
    }
}
