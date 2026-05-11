#nullable enable
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Affiche un tooltip UI Toolkit au hover sur une Tower placée.
    /// Parcourt PlacedTowers chaque frame, détecte la plus proche du cursor en screen space.
    /// Populate : nom + level + cost cumulatif, stats effectives avec buffs synergy, synergies actives.
    /// Peut être placé sur le meme GameObject que HudController (partage le UIDocument).
    /// </summary>
    public class TowerTooltipController : MonoBehaviour
    {
        // Distance screen pixels en dessous de laquelle une tower est considérée "hovered"
        private const float HoverRadiusPx = 60f;
        // Décalage du tooltip par rapport au cursor
        private const float OffsetX = 14f;
        private const float OffsetY = 14f;

        private VisualElement? tooltipRoot;
        private Label? tooltipHeader;
        private Label? tooltipStats;
        private Label? tooltipSynergies;

        private Camera? cam;
        private Tower? currentHovered;
        private readonly StringBuilder sb = new();

        private void Start()
        {
            var doc = GetComponent<UIDocument>() ?? FindFirstObjectByType<UIDocument>();
            if (doc == null) return;
            var root = doc.rootVisualElement;
            tooltipRoot = root.Q<VisualElement>("tower-tooltip");
            tooltipHeader = root.Q<Label>("tooltip-header");
            tooltipStats = root.Q<Label>("tooltip-stats");
            tooltipSynergies = root.Q<Label>("tooltip-synergies");

            cam = Camera.main;
            Hide();
        }

        private void Update()
        {
            if (PlacementController.Instance == null || cam == null)
            {
                Hide();
                return;
            }

            Vector2 mousePos = Input.mousePosition;
            // Y est inversé : Unity screen = bas-gauche, UI Toolkit = haut-gauche
            float uiMouseY = Screen.height - mousePos.y;

            Tower? hovered = FindHoveredTower(mousePos);

            if (hovered == null)
            {
                if (currentHovered != null) Hide();
                currentHovered = null;
                return;
            }

            currentHovered = hovered;
            PopulateTooltip(hovered);
            PositionTooltip(mousePos.x, uiMouseY);
            Show();
        }

        private Tower? FindHoveredTower(Vector2 mousePos)
        {
            var towers = PlacementController.Instance!.PlacedTowers;
            Tower? best = null;
            float bestDist = HoverRadiusPx;

            for (int i = 0; i < towers.Count; i++)
            {
                var t = towers[i];
                if (t == null) continue;
                Vector3 screenPos = cam!.WorldToScreenPoint(t.transform.position);
                if (screenPos.z < 0f) continue; // derrière camera
                float dist = Vector2.Distance(mousePos, new Vector2(screenPos.x, screenPos.y));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = t;
                }
            }

            return best;
        }

        private void PopulateTooltip(Tower tower)
        {
            var cfg = tower.Config;
            if (cfg == null) return;

            // ── Header : Nom · L{level} · coût cumulatif ──────────────────────
            sb.Clear();
            string towerNameKey = $"tower.{cfg.Id}.name";
            string localizedName = L.Get(towerNameKey, "Towers");
            string displayName2 = localizedName != towerNameKey
                ? localizedName
                : (string.IsNullOrEmpty(cfg.DisplayName) ? cfg.Id : cfg.DisplayName);
            sb.Append(displayName2);
            sb.Append("  L");
            sb.Append(tower.UpgradeLevel);
            if (tower.UpgradeBranch != TowerBranch.None)
            {
                sb.Append(" (");
                sb.Append(tower.UpgradeBranch == TowerBranch.Dps
                    ? L.Get("tooltip.branch_dps")
                    : L.Get("tooltip.branch_utility"));
                sb.Append(')');
            }
            sb.Append("  |  ");
            sb.Append(L.Get("tooltip.invested", tower.CumulativeCost));
            if (tooltipHeader != null) tooltipHeader.text = sb.ToString();

            // ── Stats effectives ──────────────────────────────────────────────
            sb.Clear();

            float baseDmg = cfg.Damage;
            float effectiveDmg = baseDmg * tower._buffMul;
            sb.Append(L.Get("tooltip.stat_dmg"));
            sb.Append(' ');
            sb.Append(effectiveDmg.ToString("F1"));
            if (tower._buffMul > 1.01f)
                sb.Append(L.Get("tooltip.stat_dmg_buff", tower._buffMul.ToString("F2")));
            sb.Append('\n');

            sb.Append(L.Get("tooltip.stat_range"));
            sb.Append(' ');
            sb.Append(cfg.Range.ToString("F1"));
            sb.Append('\n');

            float fireRateSec = cfg.FireRateMs / 1000f;
            sb.Append(L.Get("tooltip.stat_rate"));
            sb.Append(' ');
            sb.Append(fireRateSec.ToString("F2"));
            sb.Append("s");
            sb.Append('\n');

            int effectivePierce = cfg.Pierce + tower._pierceBonus;
            sb.Append(L.Get("tooltip.stat_pierce"));
            sb.Append(' ');
            sb.Append(effectivePierce);
            if (tower._pierceBonus > 0)
            {
                sb.Append(" (+");
                sb.Append(tower._pierceBonus);
                sb.Append(')');
            }
            sb.Append('\n');

            if (tower._multiShotBonus > 0)
            {
                sb.Append(L.Get("tooltip.stat_multishot", tower._multiShotBonus));
                sb.Append('\n');
            }

            if (tooltipStats != null) tooltipStats.text = sb.ToString().TrimEnd('\n');

            // ── Synergies actives ─────────────────────────────────────────────
            sb.Clear();
            int synCount = 0;

            if (tower._buffMul > 1.01f && synCount < 5)
            {
                sb.Append(L.Get("tooltip.syn_aura_dmg", tower._buffMul.ToString("F2")));
                sb.Append('\n');
                synCount++;
            }
            if (tower._pierceBonus > 0 && synCount < 5)
            {
                string pierceVal = tower._pierceBonus == 99
                    ? L.Get("tooltip.pierce_inf")
                    : tower._pierceBonus.ToString();
                sb.Append(L.Get("tooltip.syn_pierce", pierceVal));
                sb.Append('\n');
                synCount++;
            }
            if (tower._multiShotBonus > 0 && synCount < 5)
            {
                sb.Append(L.Get("tooltip.syn_multishot", tower._multiShotBonus));
                sb.Append('\n');
                synCount++;
            }
            if (tower._flyerDmgBonus > 1.01f && synCount < 5)
            {
                sb.Append(L.Get("tooltip.syn_flyer_dmg", tower._flyerDmgBonus.ToString("F2")));
                sb.Append('\n');
                synCount++;
            }
            if (tower._slowOnHitActive && synCount < 5)
            {
                sb.Append(L.Get("tooltip.syn_slow_on_hit",
                    tower._slowOnHitMul.ToString("F2"),
                    (tower._slowOnHitDurMs / 1000f).ToString("F1")));
                sb.Append('\n');
                synCount++;
            }
            if (tower._appliesSlowActive && synCount < 5)
            {
                sb.Append(L.Get("tooltip.syn_applies_slow", tower._appliesSlowMul.ToString("F2")));
                sb.Append('\n');
                synCount++;
            }
            if (tower._propagateAoEActive && synCount < 5)
            {
                sb.Append(L.Get("tooltip.syn_prop_aoe",
                    tower._propagateAoERadius.ToString("F1"),
                    tower._propagateAoEDmg.ToString("F1")));
                sb.Append('\n');
                synCount++;
            }
            if (tower._cascadeRadius > 0f && synCount < 5)
            {
                sb.Append(L.Get("tooltip.syn_cascade", tower._cascadeRadius.ToString("F1")));
                sb.Append('\n');
                synCount++;
            }
            if (tower._knockbackOnHit > 0f && synCount < 5)
            {
                sb.Append(L.Get("tooltip.syn_knockback", tower._knockbackOnHit.ToString("F1")));
                sb.Append('\n');
                synCount++;
            }
            if (tower._freezeOnHitActive && synCount < 5)
            {
                sb.Append(L.Get("tooltip.syn_freeze", (tower._freezeDurMs / 1000f).ToString("F1")));
                sb.Append('\n');
                synCount++;
            }
            if (tower._propagateDebuff && synCount < 5)
            {
                sb.Append(L.Get("tooltip.syn_propagate_debuff"));
                sb.Append('\n');
                synCount++;
            }
            if (tower._pullActive && synCount < 5)
            {
                sb.Append(L.Get("tooltip.syn_pull"));
                sb.Append('\n');
                synCount++;
            }

            string synText = sb.ToString().TrimEnd('\n');
            if (tooltipSynergies != null)
                tooltipSynergies.text = synText;
        }

        private void PositionTooltip(float screenX, float uiY)
        {
            if (tooltipRoot == null) return;
            float x = screenX + OffsetX;
            float y = uiY + OffsetY;
            // Clamp pour rester dans l'écran (largeur max 280 px)
            if (x + 280f > Screen.width) x = screenX - 280f - OffsetX;
            tooltipRoot.style.left = new Length(x, LengthUnit.Pixel);
            tooltipRoot.style.top = new Length(y, LengthUnit.Pixel);
        }

        private void Show()
        {
            tooltipRoot?.RemoveFromClassList("hidden");
        }

        private void Hide()
        {
            tooltipRoot?.AddToClassList("hidden");
        }
    }
}
