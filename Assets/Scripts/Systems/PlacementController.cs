#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    public class PlacementController : MonoBehaviour
    {
        public static PlacementController? Instance { get; private set; }

        [SerializeField] private TowerType? selectedTowerType;
        [SerializeField] private GameObject? towerPrefab;
        [SerializeField] private GameObject? projectilePrefab;

        private Camera? cam;
        private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        private readonly List<Tower> placedTowers = new();

        public IReadOnlyList<Tower> PlacedTowers => placedTowers;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            cam = Camera.main;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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
            if (Economy.Instance == null || !Economy.Instance.TrySpend(cost))
            {
#if UNITY_EDITOR
                Debug.Log($"[Place] reject : not enough gold ({Economy.Instance?.Gold ?? 0} < {cost})");
#endif
                return;
            }
#if UNITY_EDITOR
            Debug.Log($"[Place] cost={cost} gold, remaining={Economy.Instance.Gold}");
#endif

            Vector3 cellWorld = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize);
            var go = Instantiate(towerPrefab, cellWorld, Quaternion.identity);
            var tower = go.GetComponent<Tower>();
            if (tower != null)
            {
                tower.Init(selectedTowerType, projectilePrefab);
                placedTowers.Add(tower);
            }
        }

        public void UnregisterTower(Tower t) => placedTowers.Remove(t);
    }
}
