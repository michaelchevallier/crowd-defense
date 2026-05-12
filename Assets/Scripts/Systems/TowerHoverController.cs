#nullable enable
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Entities;
using CrowdDefense.UI;

namespace CrowdDefense.Systems
{
    public class TowerHoverController : MonoSingleton<TowerHoverController>
    {
        private Camera? cam;
        private Tower? hoveredTower;
        private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        protected override void OnAwakeSingleton() => cam = Camera.main;

        private void Update()
        {
            if (cam == null || PlacementController.Instance == null) return;

            // Ne pas afficher le ring hover si une tour est sélectionnée (radial menu ouvert)
            if (PlacementController.Instance.SelectedTower != null)
            {
                ClearHover();
                return;
            }

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!groundPlane.Raycast(ray, out float dist))
            {
                ClearHover();
                return;
            }

            Vector3 hitPos = ray.GetPoint(dist);
            Tower? found = null;
            float bestDist = 1.5f;
            var towers = PlacementController.Instance.PlacedTowers;
            for (int i = 0; i < towers.Count; i++)
            {
                var t = towers[i];
                if (t == null) continue;
                float d = (t.transform.position - hitPos).magnitude;
                if (d < bestDist) { bestDist = d; found = t; }
            }

            if (found == hoveredTower) return;

            ClearClusterHighlights();

            if (hoveredTower != null)
            {
                hoveredTower.ShowRangeRing(false);
                TowerTooltipController.Instance?.Hide();
            }
            hoveredTower = found;
            if (hoveredTower != null)
            {
                hoveredTower.ShowRangeRing(true);
                TowerTooltipController.Instance?.Show(hoveredTower);
                ShowClusterHighlights(hoveredTower, towers);
            }
        }

        private void ShowClusterHighlights(Tower source, System.Collections.Generic.List<Tower> towers)
        {
            string? sourceId = source.Config?.Id;
            if (sourceId == null) return;
            Vector3 sourcePos = source.transform.position;
            for (int i = 0; i < towers.Count; i++)
            {
                var t = towers[i];
                if (t == null || t == source) continue;
                if (t.Config?.Id != sourceId) continue;
                if (Vector3.Distance(t.transform.position, sourcePos) > 2f) continue;
                t.ShowClusterHighlight(true);
            }
        }

        private void ClearClusterHighlights()
        {
            if (PlacementController.Instance == null) return;
            var towers = PlacementController.Instance.PlacedTowers;
            for (int i = 0; i < towers.Count; i++)
                towers[i]?.ShowClusterHighlight(false);
        }

        private void ClearHover()
        {
            if (hoveredTower == null) return;
            ClearClusterHighlights();
            hoveredTower.ShowRangeRing(false);
            hoveredTower = null;
            TowerTooltipController.Instance?.Hide();
        }
    }
}
