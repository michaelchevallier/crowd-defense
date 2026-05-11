#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    public class PlacementController : MonoSingleton<PlacementController>
    {
        [SerializeField] private TowerType? selectedTowerType;
        [SerializeField] private GameObject? towerPrefab;
        [SerializeField] private GameObject? projectilePrefab;

        private Camera? cam;
        private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        private readonly List<Tower> placedTowers = new();

        // Tour active (debug sell hotkey S, CORE-20 radial menu)
        private Tower? selectedTower;

        public IReadOnlyList<Tower> PlacedTowers => placedTowers;
        // Exposé pour radial menu CORE-20
        public Tower? SelectedTower => selectedTower;

        // Fired after a tower is successfully placed (tutorial + achievements hooks)
        public event Action<Tower>? OnTowerPlaced;

        protected override void OnAwakeSingleton()
        {
            cam = Camera.main;
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

            // Click sur tour existante : sélectionner pour radial menu (production + debug)
            // Priorité : si selectedTowerType non set, tenter sélection tour.
            // Si selectedTowerType set, placement a priorité (click place la tour).
            if (selectedTowerType == null)
            {
                TrySelectTowerAtMouse();
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Shift-click : sélectionner tour même quand selectedTowerType est set (debug override)
            if (Input.GetKey(KeyCode.LeftShift))
            {
                TrySelectTowerAtMouse();
                return;
            }
#endif

            if (towerPrefab == null) return;
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

            // Anti-double magnet (D1-01 Q3) : cap = 1 par défaut, 2 si AllowMultiMagnet
            if (selectedTowerType.Behavior == TowerBehavior.CoinPull)
            {
                var cfg = BalanceConfig.Get();
                bool allowMulti = LevelRunner.Instance?.CurrentLevel?.AllowMultiMagnet ?? false;
                int cap = allowMulti ? cfg.MagnetCapAllowMulti : cfg.MagnetCapDefault;
                int count = 0;
                foreach (var t in placedTowers)
                    if (t != null && t.Config?.Behavior == TowerBehavior.CoinPull) count++;
                if (count >= cap)
                {
#if UNITY_EDITOR
                    Debug.Log($"[Place] reject : magnet cap reached ({count}/{cap})");
#endif
                    return;
                }
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
                OnTowerPlaced?.Invoke(tower);
            }
        }

        // Selection de tour via click (production + debug) — utilisee par radial menu CORE-20
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

        public void UnregisterTower(Tower t) => placedTowers.Remove(t);

        // Called by boss AoE blast to destroy a tower directly (POC — no HP system yet).
        public void RemoveTower(Tower t)
        {
            placedTowers.Remove(t);
            Destroy(t.gameObject);
        }

        // Tower.CumulativeCost is the source of truth — no sync needed.
        // Kept for RadialMenuController call-site compatibility (no-op is safe).
        public void SyncCumulativeCost(Tower _) { }
    }
}
