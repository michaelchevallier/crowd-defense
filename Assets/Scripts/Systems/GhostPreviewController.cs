#nullable enable
using UnityEngine;
using CrowdDefense.Common;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Affiche un cube semi-transparent (ghost) qui suit la souris pendant le mode placement.
    /// Vert = cellule buildable, rouge = cellule non-buildable ou hors-grille.
    /// S'abonne à PlacementController.OnHoverPlacementCell et surveille SelectedTowerType.
    /// </summary>
    public class GhostPreviewController : MonoSingleton<GhostPreviewController>
    {
        private static readonly Color ColorValid   = new Color(0.20f, 0.85f, 0.20f, 0.45f);
        private static readonly Color ColorInvalid = new Color(0.85f, 0.20f, 0.20f, 0.45f);

        private Camera?   cam;
        private GameObject? ghost;
        private MeshRenderer? ghostRenderer;
        private Material?   ghostMat;

        // Cache for raw mouse-tracked world position (used when no valid cell)
        private Vector3 lastMouseWorld;
        private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        protected override void OnAwakeSingleton()
        {
            cam = Camera.main;
            BuildGhost();
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnHoverPlacementCell += OnHoverCell;
        }

        protected override void OnDestroySingleton()
        {
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnHoverPlacementCell -= OnHoverCell;
        }

        private void BuildGhost()
        {
            ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.name = "TowerGhost";
            ghost.transform.SetParent(transform, false);

            // Remove collider so ghost never intercepts raycasts
            var col = ghost.GetComponent<Collider>();
            if (col != null) Destroy(col);

            ghostRenderer = ghost.GetComponent<MeshRenderer>();

            // Semi-transparent material using URP/Lit with Alpha Blending
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Standard");
            ghostMat = new Material(shader)
            {
                renderQueue = 3000,
            };
            // URP transparent surface
            ghostMat.SetFloat("_Surface", 1f);   // 0=Opaque, 1=Transparent
            ghostMat.SetFloat("_Blend", 0f);      // Alpha blend
            ghostMat.SetFloat("_AlphaClip", 0f);
            ghostMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            ghostMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            ghostMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            ghostMat.SetInt("_ZWrite", 0);

            if (ghostRenderer != null) ghostRenderer.sharedMaterial = ghostMat;

            ghost.SetActive(false);
        }

        private void Update()
        {
            var pc = PlacementController.Instance;
            if (pc == null || ghost == null) { HideGhost(); return; }

            bool inPlacementMode = pc.SelectedTowerType != null;
            if (!inPlacementMode) { HideGhost(); return; }

            // Keep ghost scale in sync with selected tower SizeMultiplier
            float sizeMul = pc.SelectedTowerType?.SizeMultiplier ?? 1f;
            float cellSize = PathManager.Instance?.Grid?.CellSize ?? 1f;
            float s = cellSize * 0.85f * sizeMul;
            ghost.transform.localScale = new Vector3(s, s * 0.6f, s);

            // Track raw mouse world for fallback positioning (when hovering non-buildable)
            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (groundPlane.Raycast(ray, out float dist))
                    lastMouseWorld = ray.GetPoint(dist);
            }
        }

        private void OnHoverCell(Vector2Int? cell)
        {
            if (ghost == null || ghostMat == null) return;

            var pc = PlacementController.Instance;
            if (pc == null || pc.SelectedTowerType == null) { HideGhost(); return; }

            if (cell.HasValue)
            {
                // Snap to cell center
                var grid = PathManager.Instance!.Grid!;
                Vector3 pos = GridCoords.CellToWorld(cell.Value.x, cell.Value.y, grid.Width, grid.Height, grid.CellSize);
                pos.y = 0.3f;
                ghost.transform.position = pos;
                ghostMat.color = ColorValid;
            }
            else
            {
                // Not on a buildable cell — follow raw mouse position, red tint
                Vector3 pos = lastMouseWorld;
                pos.y = 0.3f;
                ghost.transform.position = pos;
                ghostMat.color = ColorInvalid;
            }

            ghost.SetActive(true);
        }

        private void HideGhost()
        {
            if (ghost != null) ghost.SetActive(false);
        }
    }
}
