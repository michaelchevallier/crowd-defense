#nullable enable
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.Visual
{
    public class PlacementHighlight : MonoSingleton<PlacementHighlight>
    {
        private static readonly Color ColorValid   = new Color(0f, 1f, 0f, 0.30f);
        private static readonly Color ColorInvalid = new Color(1f, 0f, 0f, 0.30f);
        private const float GroundY = 0.02f;

        private GameObject? quad;
        private MeshRenderer? quadRenderer;
        private Material? quadMat;

        private Vector3 lastMouseWorld;
        private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        private Camera? cam;

        protected override void OnAwakeSingleton()
        {
            cam = Camera.main;
            BuildQuad();
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnHoverPlacementCell += OnHoverCell;
        }

        protected override void OnDestroySingleton()
        {
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnHoverPlacementCell -= OnHoverCell;
        }

        private void BuildQuad()
        {
            quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "PlacementHighlight";
            quad.transform.SetParent(transform, false);

            var col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Rotate 90° on X so the quad lies flat on XZ ground plane
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            quadRenderer = quad.GetComponent<MeshRenderer>();

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Unlit/Transparent")
                      ?? Shader.Find("Unlit/Color");
            quadMat = new Material(shader!)
            {
                renderQueue = 2999,
            };
            quadMat.SetFloat("_Surface", 1f);
            quadMat.SetFloat("_Blend", 0f);
            quadMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            quadMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            quadMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            quadMat.SetInt("_ZWrite", 0);
            SetQuadColor(ColorValid);

            if (quadRenderer != null) quadRenderer.sharedMaterial = quadMat;

            quad.SetActive(false);
        }

        private void Update()
        {
            var pc = PlacementController.Instance;
            if (pc == null || pc.SelectedTowerType == null)
            {
                Hide();
                return;
            }

            float cellSize = PathManager.Instance?.Grid?.CellSize ?? 1f;
            if (quad != null)
                quad.transform.localScale = new Vector3(cellSize, cellSize, 1f);

            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (groundPlane.Raycast(ray, out float dist))
                    lastMouseWorld = ray.GetPoint(dist);
            }
        }

        private void OnHoverCell(Vector2Int? cell)
        {
            if (quad == null || quadMat == null) return;

            var pc = PlacementController.Instance;
            if (pc == null || pc.SelectedTowerType == null) { Hide(); return; }

            Vector3 pos;
            if (cell.HasValue)
            {
                var grid = PathManager.Instance!.Grid!;
                pos = GridCoords.CellToWorld(cell.Value.x, cell.Value.y, grid.Width, grid.Height, grid.CellSize);
                SetQuadColor(ColorValid);
            }
            else
            {
                pos = lastMouseWorld;
                SetQuadColor(ColorInvalid);
            }

            pos.y = GroundY;
            quad.transform.position = pos;
            quad.SetActive(true);
        }

        private void SetQuadColor(Color c)
        {
            if (quadMat == null) return;
            if (quadMat.HasProperty("_BaseColor"))
                quadMat.SetColor("_BaseColor", c);
            else
                quadMat.color = c;
        }

        public void Hide()
        {
            if (quad != null) quad.SetActive(false);
        }
    }
}
