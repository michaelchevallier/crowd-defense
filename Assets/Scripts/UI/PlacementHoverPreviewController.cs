#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    /// Shows a stats card near the cursor when hovering a valid build cell
    /// while a tower type is selected. Hides when no valid cell is hovered.
    public class PlacementHoverPreviewController : UIControllerBase
    {
        private const float LerpSpeed = 18f; // approx 0.2s settle at 60fps

        private VisualElement? _card;
        private Label? _name;
        private Label? _stats;

        // Smoothed panel position in UI-space (px)
        private Vector2 _panelPos;
        private bool    _posInitialized;

        private void Start()
        {
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            _card  = Root?.Q<VisualElement>("tower-hover-card");
            _name  = Root?.Q<Label>("hover-card-name");
            _stats = Root?.Q<Label>("hover-card-stats");

            HideCard();

            if (PlacementController.Instance != null)
                PlacementController.Instance.OnHoverPlacementCell += OnHoverCell;
        }

        private void OnDestroy()
        {
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnHoverPlacementCell -= OnHoverCell;
        }

        private void Update()
        {
            if (_card == null) return;
            if (_card.ClassListContains("hidden")) return;

            // Screen coords → UI Toolkit panel coords (origin top-left, y axis flipped)
            var mp = Input.mousePosition;
            float targetX = mp.x + 16f;
            float targetY = Screen.height - mp.y - 16f;

            // Clamp so card stays within screen bounds
            float cardW = _card.resolvedStyle.width;
            float cardH = _card.resolvedStyle.height;
            if (cardW > 0) targetX = Mathf.Min(targetX, Screen.width  - cardW - 4f);
            if (cardH > 0) targetY = Mathf.Max(targetY, 4f);

            if (!_posInitialized)
            {
                _panelPos       = new Vector2(targetX, targetY);
                _posInitialized = true;
            }
            else
            {
                float t = 1f - Mathf.Exp(-LerpSpeed * Time.unscaledDeltaTime);
                _panelPos.x = Mathf.Lerp(_panelPos.x, targetX, t);
                _panelPos.y = Mathf.Lerp(_panelPos.y, targetY, t);
            }

            _card.style.left = _panelPos.x;
            _card.style.top  = _panelPos.y;
        }

        private void OnHoverCell(Vector2Int? cell)
        {
            var type = PlacementController.Instance?.SelectedTowerType;
            if (cell == null || type == null)
            {
                HideCard();
                return;
            }
            ShowCard(type);
        }

        private void ShowCard(TowerType type)
        {
            if (_card == null || _name == null || _stats == null) return;

            int gold = Economy.Instance?.Gold ?? 0;
            bool poor = gold < type.Cost;

            _name.text = $"{type.DisplayName}  {type.Cost}g";

            float ratePerSec = 1000f / Mathf.Max(type.FireRateMs, 1);
            _stats.text = $"Atk {type.Damage:F0}  Cadence {ratePerSec:F1}/s  Portee {type.Range:F1}m";

            if (poor)
                _card.AddToClassList("tower-hover-card--poor");
            else
                _card.RemoveFromClassList("tower-hover-card--poor");

            if (_card.ClassListContains("hidden"))
            {
                _posInitialized = false; // snap to cursor on first show
                _card.RemoveFromClassList("hidden");
            }
        }

        private void HideCard() => _card?.AddToClassList("hidden");
    }
}
