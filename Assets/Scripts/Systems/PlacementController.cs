#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.UI;
using CrowdDefense.Visual;

namespace CrowdDefense.Systems
{
    public class PlacementController : MonoSingleton<PlacementController>
    {
        [SerializeField] private TowerType? selectedTowerType;
        [SerializeField] private TowerRegistry? towerRegistry;
        [SerializeField] private GameObject? towerPrefab;
        [SerializeField] private GameObject? projectilePrefab;

        private Camera? cam;
        private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        private readonly List<Tower> placedTowers = new();

        // Tour active (debug sell hotkey S, CORE-20 radial menu)
        private Tower? selectedTower;

        // Double-tap detection
        private float _lastTapTime = -1f;
        private const float DoubleTapMaxInterval = 0.3f;

        public IReadOnlyList<Tower> PlacedTowers => placedTowers;
        // Exposé pour radial menu CORE-20
        public Tower? SelectedTower => selectedTower;

        // Fired after a tower is successfully placed (tutorial + achievements hooks)
        public event Action<Tower>? OnTowerPlaced;
        // Fired after a tower is upgraded (called by RadialMenuController)
        public event Action<Tower, int>? OnTowerUpgraded;
        // Fired after a tower is sold (called by RadialMenuController); int = refund amount
        public event Action<Tower, int>? OnTowerSold;
        // Fired when selected tower changes (null = deselected) — consumed by RadialMenuController
        public event Action<Tower?>? OnTowerSelected;
        // Fired each frame the mouse hovers a placement-mode cell (null = not hovering buildable cell)
        public event Action<Vector2Int?>? OnHoverPlacementCell;
        // Fired when clicking an empty buildable cell (no tower nearby, no selectedTowerType) — opens TowerSelectMenu
        public event Action<Vector3>? OnEmptyBuildableTileClick;

        protected override void OnAwakeSingleton()
        {
            cam = Camera.main;
            _ = TowerHoverController.Instance;    // ensure hover controller exists
            _ = GhostPreviewController.Instance;  // ensure ghost preview exists
            _ = PlacementHighlight.Instance;      // ensure placement highlight exists
        }

        private void Update()
        {
            // ESC deselects active tower (closes radial menu)
            if (Input.GetKeyDown(KeyCode.Escape) && selectedTower != null)
            {
                SetSelectedTower(null);
                return;
            }

            // Hotkeys 1/2/3/4 — quick select tower type for placement
            if (Input.GetKeyDown(KeyCode.Alpha1)) { SelectTowerType("archer"); return; }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { SelectTowerType("mage");   return; }
            if (Input.GetKeyDown(KeyCode.Alpha3)) { SelectTowerType("cannon"); return; }
            if (Input.GetKeyDown(KeyCode.Alpha4)) { SelectTowerType("frost");  return; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Hotkey S : sell la tour sélectionnée (debug, UI radial menu = CORE-20)
            if (Input.GetKeyDown(KeyCode.S) && selectedTower != null)
            {
                selectedTower.Sell();
                SetSelectedTower(null);
                return;
            }
            // Hotkey U : upgrade la tour sélectionnée au niveau suivant (debug)
            if (Input.GetKeyDown(KeyCode.U) && selectedTower != null)
            {
                if (selectedTower.UpgradeTo(selectedTower.UpgradeLevel + 1))
                    SyncCumulativeCost(selectedTower);
            }
#endif
            if (cam == null) return;

            // Touch input — tap-to-select + double-tap upgrade
            if (Input.touchSupported && Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    HandleTouchTap(touch.position);
                    return;
                }
            }

            // Hover tracking for PathfinderVisualization (runs every frame in placement mode)
            if (OnHoverPlacementCell != null)
            {
                if (selectedTowerType != null)
                {
                    Ray hoverRay = cam.ScreenPointToRay(Input.mousePosition);
                    if (groundPlane.Raycast(hoverRay, out float hoverDist))
                    {
                        var hoverGrid = PathManager.Instance?.Grid;
                        if (hoverGrid != null)
                        {
                            Vector3 hoverPos = hoverRay.GetPoint(hoverDist);
                            Vector2Int hoverCell = GridCoords.WorldToCell(hoverPos, hoverGrid.Width, hoverGrid.Height, hoverGrid.CellSize);
                            OnHoverPlacementCell.Invoke(hoverGrid.IsBuildable(hoverCell.x, hoverCell.y) ? hoverCell : (Vector2Int?)null);
                        }
                        else
                        {
                            OnHoverPlacementCell.Invoke(null);
                        }
                    }
                    else
                    {
                        OnHoverPlacementCell.Invoke(null);
                    }
                }
                else
                {
                    OnHoverPlacementCell.Invoke(null);
                }
            }

            if (!Input.GetMouseButtonDown(0)) return;

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

            var hero = LevelRunner.Instance?.Hero;
            int cost = ComputeTowerCost(selectedTowerType.Cost, hero);
            if (cost > 0 && (Economy.Instance == null || !Economy.Instance.TrySpend(cost)))
            {
#if UNITY_EDITOR
                Debug.Log($"[Place] reject : not enough gold ({Economy.Instance?.Gold ?? 0} < {cost})");
#endif
                return;
            }
            if (cost == 0 && hero != null && hero.FirstTowerFree)
                hero.FirstTowerFreeUsed = true;
#if UNITY_EDITOR
            Debug.Log($"[Place] cost={cost} (base={selectedTowerType.Cost}) gold remaining={Economy.Instance?.Gold ?? 0}");
#endif

            Vector3 cellWorld = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize);
            var go = Instantiate(towerPrefab, cellWorld, Quaternion.identity);
            var tower = go.GetComponent<Tower>();
            if (tower != null)
            {
                tower.Init(selectedTowerType, projectilePrefab);
                placedTowers.Add(tower);
                AudioController.Instance?.Play("tower_built", 0.7f);
                VfxPool.Instance?.SpawnImpact(cellWorld + Vector3.up * 0.3f, new Color(1f, 0.84f, 0f));
                AudioController.Instance?.Play3D("tower_placed", cellWorld);
                JuiceFX.Instance?.PunchScale(tower.transform, 1.15f, 0.3f);
                CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward("Tour placee !", cellWorld + Vector3.up * 1.5f, Color.green);
                OnTowerPlaced?.Invoke(tower);
            }
        }

        // Touch tap handler — single tap = select, double tap = upgrade selected tower
        private void HandleTouchTap(Vector2 screenPos)
        {
            bool isDoubleTap = (Time.unscaledTime - _lastTapTime) < DoubleTapMaxInterval;
            _lastTapTime = Time.unscaledTime;

            if (isDoubleTap && selectedTower != null)
            {
                if (selectedTower.UpgradeTo(selectedTower.UpgradeLevel + 1))
                    SyncCumulativeCost(selectedTower);
                return;
            }

            if (selectedTowerType == null)
            {
                TrySelectTowerAt(screenPos);
                return;
            }

            if (towerPrefab == null) return;
            if (PathManager.Instance == null || PathManager.Instance.Grid == null) return;

            Ray ray = cam!.ScreenPointToRay(screenPos);
            if (!groundPlane.Raycast(ray, out float dist)) return;
            Vector3 hitPos = ray.GetPoint(dist);

            var grid = PathManager.Instance.Grid;
            Vector2Int cell = GridCoords.WorldToCell(hitPos, grid.Width, grid.Height, grid.CellSize);
            if (!grid.IsBuildable(cell.x, cell.y)) return;

            if (selectedTowerType.Behavior == TowerBehavior.CoinPull)
            {
                var cfg = BalanceConfig.Get();
                bool allowMulti = LevelRunner.Instance?.CurrentLevel?.AllowMultiMagnet ?? false;
                int cap = allowMulti ? cfg.MagnetCapAllowMulti : cfg.MagnetCapDefault;
                int count = 0;
                foreach (var t in placedTowers)
                    if (t != null && t.Config?.Behavior == TowerBehavior.CoinPull) count++;
                if (count >= cap) return;
            }

            var hero = LevelRunner.Instance?.Hero;
            int cost = ComputeTowerCost(selectedTowerType.Cost, hero);
            if (cost > 0 && (Economy.Instance == null || !Economy.Instance.TrySpend(cost))) return;
            if (cost == 0 && hero != null && hero.FirstTowerFree)
                hero.FirstTowerFreeUsed = true;

            Vector3 cellWorld = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize);
            var go = Instantiate(towerPrefab, cellWorld, Quaternion.identity);
            var tower = go.GetComponent<Tower>();
            if (tower != null)
            {
                tower.Init(selectedTowerType, projectilePrefab);
                placedTowers.Add(tower);
                AudioController.Instance?.Play("tower_built", 0.7f);
                VfxPool.Instance?.SpawnImpact(cellWorld + Vector3.up * 0.3f, new Color(1f, 0.84f, 0f));
                AudioController.Instance?.Play3D("tower_placed", cellWorld);
                JuiceFX.Instance?.PunchScale(tower.transform, 1.15f, 0.3f);
                CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward("Tour placee !", cellWorld + Vector3.up * 1.5f, Color.green);
                OnTowerPlaced?.Invoke(tower);
            }
        }

        // Selection de tour via click (production + debug) — utilisee par radial menu CORE-20
        private void TrySelectTowerAtMouse()
        {
            if (cam == null) return;
            TrySelectTowerAt(Input.mousePosition);
        }

        private void TrySelectTowerAt(Vector2 screenPos)
        {
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(screenPos);
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
            SetSelectedTower(closest);
#if UNITY_EDITOR
            Debug.Log($"[Place] selectedTower={selectedTower?.Config?.Id ?? "none"} L{selectedTower?.UpgradeLevel}");
#endif

            // No tower found near tap/click — check if cell is buildable and open select menu
            if (closest == null && OnEmptyBuildableTileClick != null)
            {
                var grid = PathManager.Instance?.Grid;
                if (grid != null)
                {
                    Vector2Int cell = GridCoords.WorldToCell(hitPos, grid.Width, grid.Height, grid.CellSize);
                    if (grid.IsBuildable(cell.x, cell.y))
                    {
                        Vector3 cellWorld = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize);
                        OnEmptyBuildableTileClick.Invoke(cellWorld);
                    }
                }
            }
        }

        public void DeselectTower() => SetSelectedTower(null);

        private void SetSelectedTower(Tower? tower)
        {
            if (selectedTower == tower) return;
            selectedTower = tower;
            OnTowerSelected?.Invoke(tower);
        }

        private void SelectTowerType(string id)
        {
            TowerType? type = null;
            if (towerRegistry != null)
                foreach (var t in towerRegistry.Towers)
                    if (t != null && t.Id == id) { type = t; break; }

            selectedTowerType = type;

            string label = id switch
            {
                "archer" => "Archer",
                "mage"   => "Mage",
                "cannon" => "Canon",
                "frost"  => "Givre",
                _        => id
            };
            Toast.Show("Tour selectionnee", label, durationMs: 1500);
        }

        public void SelectTowerForPlacement(TowerType? type)
        {
            selectedTowerType = type;
        }

        public TowerType? SelectedTowerType => selectedTowerType;

        private static int ComputeTowerCost(int baseCost, Hero? hero)
        {
            if (hero == null) return baseCost;
            if (hero.FirstTowerFree && !hero.FirstTowerFreeUsed) return 0;
            return Mathf.Max(0, Mathf.RoundToInt(baseCost * hero.TowerCostMul));
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

        // Called by RadialMenuController after a successful UpgradeTo
        public void NotifyTowerUpgraded(Tower tower, int newLevel) =>
            OnTowerUpgraded?.Invoke(tower, newLevel);

        // Called by RadialMenuController before tower.Sell(); refund computed by caller
        public void NotifyTowerSold(Tower tower, int refund) =>
            OnTowerSold?.Invoke(tower, refund);
    }
}
