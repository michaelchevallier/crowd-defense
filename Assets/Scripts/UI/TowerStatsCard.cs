#nullable enable
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Affiche une stats card (Damage / Range / Fire-rate / DPS) au survol d'une tour placée.
    // Suit le curseur. Fade-in opacity 0→1 en 0.2s via USS transition.
    // Piloté par TowerHoverController.HoveredTower — pas de raycasting interne.
    public class TowerStatsCard : UIControllerBase
    {
        private const float OffsetX = 16f;
        private const float OffsetY = 16f;
        private const string HiddenClass = "tower-stats-card--hidden";

        private VisualElement? _card;
        private Label? _header;
        private Label? _body;
        private Tower? _displayed;
        private readonly StringBuilder _sb = new();

        private void Start()
        {
            if (ResolveUI()) return;
            var doc = FindFirstObjectByType<UIDocument>();
            if (doc == null) return;
            var root = doc.rootVisualElement;
            _card   = root.Q<VisualElement>("tower-stats-card");
            _header = root.Q<Label>("stats-card-header");
            _body   = root.Q<Label>("stats-card-body");
            HideCard();
        }

        protected override void OnUIReady()
        {
            _card   = Root?.Q<VisualElement>("tower-stats-card");
            _header = Root?.Q<Label>("stats-card-header");
            _body   = Root?.Q<Label>("stats-card-body");
            HideCard();
        }

        private void Update()
        {
            if (_card == null) return;

            var hovered = TowerHoverController.Instance?.HoveredTower;

            if (hovered != _displayed)
            {
                _displayed = hovered;
                if (_displayed != null)
                    ShowCard(_displayed);
                else
                    HideCard();
            }

            if (_displayed != null)
                FollowCursor();
        }

        private void ShowCard(Tower tower)
        {
            if (_card == null || _header == null || _body == null) return;

            var cfg = tower.Config;
            if (cfg == null) { HideCard(); return; }

            // Header: tower name + level
            _sb.Clear();
            string name = string.IsNullOrEmpty(cfg.DisplayName) ? cfg.Id : cfg.DisplayName;
            _sb.Append(name);
            _sb.Append("  L");
            _sb.Append(tower.UpgradeLevel);
            _header.text = _sb.ToString();

            // Body: Damage / Range / Fire-rate / DPS
            _sb.Clear();
            float dmg = cfg.Damage * tower._buffMul;
            _sb.Append("Degats: ");
            _sb.Append(dmg.ToString("F1"));
            _sb.Append('\n');

            _sb.Append("Portee: ");
            _sb.Append(cfg.Range.ToString("F1"));
            _sb.Append(" m\n");

            _sb.Append("Cadence: ");
            if (cfg.FireRateMs <= 0f)
                _sb.Append("Continu");
            else
            {
                float rps = 1000f / cfg.FireRateMs;
                _sb.Append(rps.ToString("F1"));
                _sb.Append(" tirs/s");
            }
            _sb.Append('\n');

            float dps = tower.GetLiveDps();
            _sb.Append("DPS: ");
            _sb.Append(dps.ToString("F1"));
            _body.text = _sb.ToString();

            _card.RemoveFromClassList(HiddenClass);
            FollowCursor();
        }

        private void HideCard() => _card?.AddToClassList(HiddenClass);

        private void FollowCursor()
        {
            if (_card == null) return;
            var mp = Input.mousePosition;
            float px = mp.x + OffsetX;
            float py = Screen.height - mp.y + OffsetY;

            float cardW = _card.resolvedStyle.width;
            if (cardW > 0 && px + cardW > Screen.width)
                px = mp.x - cardW - OffsetX;

            _card.style.left = new Length(px, LengthUnit.Pixel);
            _card.style.top  = new Length(py, LengthUnit.Pixel);
        }
    }
}
