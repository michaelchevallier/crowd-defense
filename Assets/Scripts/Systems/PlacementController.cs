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

        // Ghost preview state
        private GameObject? ghost;
        private bool ghostIsValid;
        private Material? ghostMatValid;
        private Material? ghostMatInvalid;
        private static readonly Color GhostValidColor   = new Color(0f,   0.85f, 1f,   0.45f);
        private static readonly Color GhostInvalidColor = new Color(1f,   0.15f, 0.1f, 0.45f);

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

        protected override void OnAwakeSingleton()
        {
            cam = Camera.main;
            BuildGhostMaterials();
        }

        private void BuildGhostMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return;

            ghostMatValid = new Material(shader);
            ghostMatValid.color = GhostValidColor;
            ghostMatValid.SetFloat("_Surface", 1);   // URP Transparent
            ghostMatValid.SetFloat("_Blend", 0);
            ghostMatValid.renderQueue = 3000;
            ghostMatValid.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            ghostMatValid.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            ghostMatValid.SetInt("_ZWrite", 0);
            ghostMatValid.EnableKeyword("_ALPHABLEND_ON");

            ghostMatInvalid = new Material(ghostMatValid);
            ghostMatInvalid.color = GhostInvalidColor;
        }

        private void SpawnGhost()
        {
            DestroyGhost();
            if (towerPrefab == null) return;

            ghost = Instantiate(towerPrefab);
            ghost.name = "GhostTower";

            // Disable all MonoBehaviours so ghost is inert (no shooting, no Init side-effects)
            foreach (var mb in ghost.GetComponentsInChildren<MonoBehaviour>())
                mb.enabled = false;

            // Disable all colliders so ghost doesn't interfere with raycasts
            foreach (var col in ghost.GetComponentsInChildren<Collider>())
                col.enabled = false;

            ApplyGhostMaterial(true);
        }

        private void DestroyGhost()
        {
            if (ghost != null)
            {
                Destroy(ghost);
                ghost = null;
            }
        }

        private void ApplyGhostMaterial(bool valid)
        {
            if (ghost == null) return;
            ghostIsValid = valid;
            var mat = valid ? ghostMatValid : ghostMatInvalid;
            if (mat == null) return;

            foreach (var r in ghost.GetComponentsInChildren<Renderer>())
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.materials = mats;
            }
        }

        private void UpdateGhost()
        {
            if (ghost == null || cam == null) return;

            var grid = PathManager.Instance?.Grid;
            if (grid == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!groundPlane.Raycast(ray, out float dist)) return;

            Vector3 hitPos  = ray.GetPoint(dist);
            Vector2Int cell = GridCoords.WorldToCell(hitPos, grid.Width, grid.Height, grid.CellSize);
            Vector3 snapPos = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize);

            ghost.transform.position = snapPos;

            bool cellBuildable = grid.IsBuildable(cell.x, cell.y);
            bool canAfford      = selectedTowerType == null
                || Economy.Instance == null
                || Economy.Instance.Gold >= selectedTowerType.Cost;

            bool valid = cellBuildable && canAfford;
            if (valid != ghostIsValid) ApplyGhostMaterial(valid);
        }

        private void Update()
        {
            // Right-click cancels placement mode
            if (Input.GetMouseButtonDown(1) && selectedTowerType != null)
            {
                SelectTowerForPlacement(null);
                return;
            }

            // ESC: cancel placement first, then deselect tower
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (selectedTowerType != null)
                {
                    SelectTowerForPlacement(null);
                    return;
                }
                if (selectedTower != null)
                {
                    SetSelectedTower(null);
                    return;
                }
            }

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

            // Ghost preview update (every frame while in placement mode)
            if (selectedTowerType != null) UpdateGhost();

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

            Ray placeRay = cam.ScreenPointToRay(Input.mousePosition);
            if (!groundPlane.Raycast(placeRay, out float placeDist)) return;
            Vector3 hitPos = placeRay.GetPoint(placeDist);

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
                DestroyGhost();
                AudioController.Instance?.Play("tower_built", 0.7f);
                OnTowerPlaced?.Invoke(tower);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DestroyGhost();
            if (ghostMatValid != null)   Destroy(ghostMatValid);
            if (ghostMatInvalid != null) Destroy(ghostMatInvalid);
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
            SetSelectedTower(closest);
#if UNITY_EDITOR
            Debug.Log($"[Place] selectedTower={selectedTower?.Config?.Id ?? "none"} L{selectedTower?.UpgradeLevel}");
#endif
        }

        public void DeselectTower() => SetSelectedTower(null);

        private void SetSelectedTower(Tower? tower)
        {
            if (selectedTower == tower) return;
            selectedTower = tower;
            OnTowerSelected?.Invoke(tower);
        }

        public void SelectTowerForPlacement(TowerType? type)
        {
            selectedTowerType = type;
            if (type != null)
                SpawnGhost();
            else
                DestroyGhost();
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
