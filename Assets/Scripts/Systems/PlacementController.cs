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
        // Mapping tower → cumulative cost pour calcul sell (redondant avec Tower.CumulativeCost, garde sync)
        private readonly Dictionary<Tower, int> towerCumulativeCost = new();

        // Tour active (debug sell hotkey S, CORE-20 radial menu)
        private Tower? selectedTower;

        public IReadOnlyList<Tower> PlacedTowers => placedTowers;
        // Exposé pour radial menu CORE-20
        public Tower? SelectedTower => selectedTower;

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Hotkey S : sell la tour sélectionnée (debug, UI radial menu = CORE-20)
            if (Input.GetKeyDown(KeyCode.S) && selectedTower != null)
            {
                selectedTower.Sell();
                selectedTower = null;
                return;
            }
            // Hotkey U : upgrade la tour sélectionnée au niveau suivant (debug)
            if (Input.GetKeyDown(KeyCode.U) && selectedTower != null)
            {
                if (selectedTower.UpgradeTo(selectedTower.UpgradeLevel + 1))
                    SyncCumulativeCost(selectedTower);
            }
#endif
            if (cam == null || !Input.GetMouseButtonDown(0)) return;

            // Right-click ou shift-click : sélectionner tour existante (debug)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKey(KeyCode.LeftShift))
            {
                TrySelectTowerAtMouse();
                return;
            }
#endif

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
                towerCumulativeCost[tower] = tower.CumulativeCost;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void TrySelectTowerAtMouse()
        {
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!groundPlane.Raycast(ray, out float dist)) return;
            Vector3 hitPos = ray.GetPoint(dist);

            Tower? closest = null;
            float bestDist = 1.5f; // snap radius en world units
            foreach (var t in placedTowers)
            {
                if (t == null) continue;
                float d = (t.transform.position - hitPos).magnitude;
                if (d < bestDist) { bestDist = d; closest = t; }
            }
            selectedTower = closest;
#if UNITY_EDITOR
            Debug.Log($"[Place] selectedTower={selectedTower?.Config?.Id ?? "none"} L{selectedTower?.UpgradeLevel}");
#endif
        }
#endif

        public void UnregisterTower(Tower t)
        {
            placedTowers.Remove(t);
            towerCumulativeCost.Remove(t);
        }

        /// <summary>
        /// Sync le dict cumulativeCost après un upgrade (appelé par CORE-20 radial menu ou hotkey U debug).
        /// Tower.CumulativeCost est déjà mis à jour dans Tower.UpgradeTo — ce sync garde le dict cohérent.
        /// </summary>
        public void SyncCumulativeCost(Tower t)
        {
            if (placedTowers.Contains(t))
                towerCumulativeCost[t] = t.CumulativeCost;
        }

        /// <summary>
        /// Returns cumulative cost of a placed tower (for sell refund accounting).
        /// Reads Tower.CumulativeCost directly — dict is kept in sync by SyncCumulativeCost.
        /// </summary>
        public int GetCumulativeCost(Tower t) => t.CumulativeCost;
    }
}
