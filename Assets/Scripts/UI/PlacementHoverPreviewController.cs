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
    public class PlacementHoverPreviewController : MonoBehaviour
    {
        private VisualElement? _card;
        private Label? _name;
        private Label? _stats;

        private void Start()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null) return;

            var root = doc.rootVisualElement;
            _card  = root.Q<VisualElement>("tower-hover-card");
            _name  = root.Q<Label>("hover-card-name");
            _stats = root.Q<Label>("hover-card-stats");

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
            float px = mp.x + 14f;
            float py = Screen.height - mp.y - 14f;

            // Clamp so card stays within screen bounds
            float cardW = _card.resolvedStyle.width;
            float cardH = _card.resolvedStyle.height;
            if (cardW > 0) px = Mathf.Min(px, Screen.width  - cardW - 4f);
            if (cardH > 0) py = Mathf.Max(py, 4f);

            _card.style.left = px;
            _card.style.top  = py;
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

            float dps = type.Damage / Mathf.Max(type.FireRateMs / 1000f, 0.001f);
            _stats.text = $"DPS {dps:F0}  Range {type.Range:F1}";

            if (poor)
            {
                _card.AddToClassList("tower-hover-card--poor");
            }
            else
            {
                _card.RemoveFromClassList("tower-hover-card--poor");
            }

            _card.RemoveFromClassList("hidden");
        }

        private void HideCard() => _card?.AddToClassList("hidden");
    }
}
