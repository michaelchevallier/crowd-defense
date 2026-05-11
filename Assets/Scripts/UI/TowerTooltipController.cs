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
            string name = string.IsNullOrEmpty(cfg.DisplayName) ? cfg.Id : cfg.DisplayName;
            sb.Append(name);
            sb.Append("  L");
            sb.Append(tower.UpgradeLevel);
            if (tower.UpgradeBranch != null)
            {
                sb.Append(" (");
                sb.Append(tower.UpgradeBranch);
                sb.Append(')');
            }
            sb.Append("  |  ");
            sb.Append(tower.CumulativeCost);
            sb.Append("g investi");
            if (tooltipHeader != null) tooltipHeader.text = sb.ToString();

            // ── Stats effectives ──────────────────────────────────────────────
            sb.Clear();

            // Damage avec buffMul + levelDmgScale (levelDmgScale privé → on affiche le chiffre resultant via formule)
            // On lit cfg.Damage × _buffMul — le _levelDmgScale est interne mais exposé par résultat fire()
            // Pour affichage on montre cfg.Damage × _buffMul ; note si buffMul > 1 on indique le buff
            float baseDmg = cfg.Damage;
            float effectiveDmg = baseDmg * tower._buffMul;
            sb.Append("Dmg ");
            sb.Append(effectiveDmg.ToString("F1"));
            if (tower._buffMul > 1.01f)
            {
                sb.Append(" (x");
                sb.Append(tower._buffMul.ToString("F2"));
                sb.Append(" buff)");
            }
            sb.Append('\n');

            sb.Append("Range ");
            sb.Append(cfg.Range.ToString("F1"));
            sb.Append('\n');

            float fireRateSec = cfg.FireRateMs / 1000f;
            sb.Append("Cadence ");
            sb.Append(fireRateSec.ToString("F2"));
            sb.Append("s");
            sb.Append('\n');

            int effectivePierce = cfg.Pierce + tower._pierceBonus;
            sb.Append("Pierce ");
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
                sb.Append("MultiShot +");
                sb.Append(tower._multiShotBonus);
                sb.Append('\n');
            }

            if (tooltipStats != null) tooltipStats.text = sb.ToString().TrimEnd('\n');

            // ── Synergies actives ─────────────────────────────────────────────
            sb.Clear();
            int synCount = 0;

            if (tower._buffMul > 1.01f && synCount < 5)
            {
                sb.Append("Aura dmg x");
                sb.Append(tower._buffMul.ToString("F2"));
                sb.Append('\n');
                synCount++;
            }
            if (tower._pierceBonus > 0 && synCount < 5)
            {
                sb.Append("Pierce +");
                sb.Append(tower._pierceBonus == 99 ? "inf" : tower._pierceBonus.ToString());
                sb.Append('\n');
                synCount++;
            }
            if (tower._multiShotBonus > 0 && synCount < 5)
            {
                sb.Append("MultiShot +");
                sb.Append(tower._multiShotBonus);
                sb.Append('\n');
                synCount++;
            }
            if (tower._flyerDmgBonus > 1.01f && synCount < 5)
            {
                sb.Append("Flyer dmg x");
                sb.Append(tower._flyerDmgBonus.ToString("F2"));
                sb.Append('\n');
                synCount++;
            }
            if (tower._slowOnHitActive && synCount < 5)
            {
                sb.Append("Slow on hit x");
                sb.Append(tower._slowOnHitMul.ToString("F2"));
                sb.Append(' ');
                sb.Append((tower._slowOnHitDurMs / 1000f).ToString("F1"));
                sb.Append("s");
                sb.Append('\n');
                synCount++;
            }
            if (tower._appliesSlowActive && synCount < 5)
            {
                sb.Append("Applique slow x");
                sb.Append(tower._appliesSlowMul.ToString("F2"));
                sb.Append('\n');
                synCount++;
            }
            if (tower._propagateAoEActive && synCount < 5)
            {
                sb.Append("PropAoE r");
                sb.Append(tower._propagateAoERadius.ToString("F1"));
                sb.Append(" dmg ");
                sb.Append(tower._propagateAoEDmg.ToString("F1"));
                sb.Append('\n');
                synCount++;
            }
            if (tower._cascadeRadius > 0f && synCount < 5)
            {
                sb.Append("Cascade r");
                sb.Append(tower._cascadeRadius.ToString("F1"));
                sb.Append('\n');
                synCount++;
            }
            if (tower._knockbackOnHit > 0f && synCount < 5)
            {
                sb.Append("Knockback ");
                sb.Append(tower._knockbackOnHit.ToString("F1"));
                sb.Append('\n');
                synCount++;
            }
            if (tower._freezeOnHitActive && synCount < 5)
            {
                sb.Append("Freeze ");
                sb.Append((tower._freezeDurMs / 1000f).ToString("F1"));
                sb.Append("s");
                sb.Append('\n');
                synCount++;
            }
            if (tower._propagateDebuff && synCount < 5)
            {
                sb.Append("PropagateDebuff");
                sb.Append('\n');
                synCount++;
            }
            if (tower._pullActive && synCount < 5)
            {
                sb.Append("Pull actif");
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
