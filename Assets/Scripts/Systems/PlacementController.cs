#nullable enable
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    public class PlacementController : MonoBehaviour
    {
        [SerializeField] private TowerType? selectedTowerType;
        [SerializeField] private GameObject? towerPrefab;
        [SerializeField] private GameObject? projectilePrefab;

        private Camera? cam;
        private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        private void Awake()
        {
            cam = Camera.main;
        }

        private void Update()
        {
            if (cam == null || !Input.GetMouseButtonDown(0)) return;
            if (selectedTowerType == null || towerPrefab == null) return;
            if (PathManager.Instance == null || PathManager.Instance.Grid == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!groundPlane.Raycast(ray, out float dist)) return;
            Vector3 hitPos = ray.GetPoint(dist);

            var grid = PathManager.Instance.Grid;
            Vector2Int cell = GridCoords.WorldToCell(hitPos, grid.Width, grid.Height, grid.CellSize);
            if (!grid.IsBuildable(cell.x, cell.y))
            {
#if UNITY_EDITOR
                Debug.Log($"[Place] reject cell ({cell.x},{cell.y}) char='{grid.At(cell.x, cell.y)}' (not buildable)");
#endif
                return;
            }

            int cost = selectedTowerType.Cost;
            // TODO POC-06 : if (!Economy.Instance.TrySpend(cost)) { Debug.Log("[Place] not enough gold"); return; }
#if UNITY_EDITOR
            Debug.Log($"[Place] cost={cost} gold (stub, free) at cell ({cell.x},{cell.y})");
#endif

            Vector3 cellWorld = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize);
            var go = Instantiate(towerPrefab, cellWorld, Quaternion.identity);
            var tower = go.GetComponent<Tower>();
            if (tower != null)
                tower.Init(selectedTowerType, projectilePrefab);
        }
    }
}
