#nullable enable
using TMPro;
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

        private static readonly Color LabelAfford  = new Color(0.20f, 0.90f, 0.20f, 1.00f);
        private static readonly Color LabelTooExp  = new Color(0.95f, 0.20f, 0.20f, 1.00f);

        private Camera?   cam;
        private GameObject? ghost;
        private MeshRenderer? ghostRenderer;
        private Material?   ghostMat;
        private GameObject? rangeRing;
        private float       lastBuiltRange = -1f;
        private TextMeshPro? costLabel;

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

            // Cost label — world-space TMP, billboarded toward camera in LateUpdate
            var labelGo = new GameObject("GhostCostLabel");
            labelGo.transform.SetParent(ghost.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            costLabel = labelGo.AddComponent<TextMeshPro>();
            costLabel.fontSize              = 3.5f;
            costLabel.fontStyle             = FontStyles.Bold;
            costLabel.alignment             = TextAlignmentOptions.Center;
            costLabel.outlineWidth          = 0.25f;
            costLabel.outlineColor          = new Color32(0, 0, 0, 220);
            costLabel.enableWordWrapping    = false;
            costLabel.autoSizeTextContainer = false;
            costLabel.rectTransform.sizeDelta = new Vector2(4f, 1f);

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

            // Rebuild range ring when selected tower changes (after scale is current)
            float range = pc.SelectedTowerType?.Range ?? 0f;
            if (!Mathf.Approximately(range, lastBuiltRange))
                BuildRangeRing(range);

            // Track raw mouse world for fallback positioning (when hovering non-buildable)
            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (groundPlane.Raycast(ray, out float dist))
                    lastMouseWorld = ray.GetPoint(dist);
            }

            // Update cost label color and text each frame
            if (costLabel != null)
            {
                int cost  = pc.SelectedTowerType?.Cost ?? 0;
                int gold  = Economy.Instance?.Gold ?? 0;
                bool canAfford = gold >= cost;
                costLabel.text  = $"Cout: {cost}c";
                costLabel.color = canAfford ? LabelAfford : LabelTooExp;
            }
        }

        private void LateUpdate()
        {
            if (ghost == null || !ghost.activeSelf || costLabel == null || cam == null) return;
            // Billboard: make the label face the camera
            costLabel.transform.rotation = cam.transform.rotation;
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
            if (rangeRing != null) rangeRing.SetActive(true);
        }

        private void HideGhost()
        {
            if (ghost != null)     ghost.SetActive(false);
            if (rangeRing != null) rangeRing.SetActive(false);
        }

        private void BuildRangeRing(float range)
        {
            if (rangeRing != null) Object.Destroy(rangeRing);
            lastBuiltRange = range;
            if (range <= 0f) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "GhostRangeRing";
            go.transform.SetParent(ghost!.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            // Compensate parent scale so world diameter = range*2
            float parentScale = ghost!.transform.localScale.x;
            float diameter = parentScale > 0f ? (range * 2f) / parentScale : range * 2f;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);
            Object.Destroy(go.GetComponent<Collider>());

            const int texSize = 64;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[texSize * texSize];
            float half = texSize * 0.5f;
            for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.SmoothStep(1f, 0f, dist) * 0.4f;
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
                pixels[y * texSize + x] = new Color32(102, 222, 255, a);
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.mainTexture = tex;
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.material = mat;

            go.SetActive(false);
            rangeRing = go;
        }
    }
}
