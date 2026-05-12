#nullable enable
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Affiche le mesh de la TowerType sélectionnée en semi-transparent (ghost) qui suit la souris.
    /// Vert = cellule buildable, rouge = cellule non-buildable ou hors-grille.
    /// Alpha 0.4 via MaterialPropertyBlock sur tous les renderers (aucun collider).
    /// S'abonne à PlacementController.OnHoverPlacementCell et surveille SelectedTowerType.
    /// </summary>
    public class GhostPreviewController : MonoSingleton<GhostPreviewController>
    {
        private static readonly Color ColorValid   = new Color(0.20f, 0.85f, 0.20f, 0.40f);
        private static readonly Color ColorInvalid = new Color(0.85f, 0.20f, 0.20f, 0.40f);

        private static readonly Color LabelAfford  = new Color(0.20f, 0.90f, 0.20f, 1.00f);
        private static readonly Color LabelTooExp  = new Color(0.95f, 0.20f, 0.20f, 1.00f);

        private static readonly Color DotColor = new Color(1f, 0.85f, 0f, 0.90f);

        private static readonly Color AimLineColor = new Color(1f, 0.25f, 0.25f, 0.90f);

        private const int RingSegments = 32;
        private static readonly Color RingColorValid   = new Color(0.20f, 0.90f, 1.00f, 0.85f);
        private static readonly Color RingColorInvalid = new Color(1.00f, 0.22f, 0.22f, 0.85f);

        private Camera?      cam;
        private GameObject?  ghost;
        private GameObject?  rangeRing;
        private LineRenderer? rangeRingLine;
        private Material?    rangeRingLineMat;
        private float        lastBuiltRange  = -1f;
        private TowerType?   lastTowerType;
        private TextMeshPro? costLabel;
        private Material?    ghostMatTransparent;
        private Material?    dotMaterial;
        private LineRenderer? aimLine;

        // Path indicator dots — shown during placement mode
        private readonly List<GameObject> _pathDots = new();

        // Reusable property block — avoids per-frame GC
        // Lazy-init: MaterialPropertyBlock native ctor fails during Unity serialization
        private MaterialPropertyBlock? _mpb;

        // Cache for raw mouse-tracked world position (used when no valid cell)
        private Vector3 lastMouseWorld;
        private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        protected override void OnAwakeSingleton()
        {
            cam = Camera.main;
            ghostMatTransparent = BuildTransparentMaterial();
            dotMaterial         = BuildDotMaterial();
            BuildAimLine();
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnHoverPlacementCell += OnHoverCell;
        }

        protected override void OnDestroySingleton()
        {
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnHoverPlacementCell -= OnHoverCell;
        }

        private static Material BuildTransparentMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Standard");
            var mat = new Material(shader) { renderQueue = 3000 };
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend",   0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",   0);
            return mat;
        }

        private void Update()
        {
            var pc = PlacementController.Instance;
            if (pc == null) { HideGhost(); return; }

            bool inPlacementMode = pc.SelectedTowerType != null;
            if (!inPlacementMode) { HideGhost(); return; }

            // Rebuild ghost when TowerType changes
            if (pc.SelectedTowerType != lastTowerType)
            {
                BuildGhostFor(pc.SelectedTowerType!);
                lastTowerType = pc.SelectedTowerType;
            }

            if (ghost == null) return;

            // Keep ghost scale in sync with selected tower SizeMultiplier
            float sizeMul  = pc.SelectedTowerType?.SizeMultiplier ?? 1f;
            float cellSize = PathManager.Instance?.Grid?.CellSize ?? 1f;
            float s = cellSize * 0.85f * sizeMul;
            ghost.transform.localScale = new Vector3(s, s, s);

            // Rebuild range ring when selected tower changes
            float range = pc.SelectedTowerType?.Range ?? 0f;
            if (!Mathf.Approximately(range, lastBuiltRange))
                BuildRangeRing(range);

            // Track raw mouse world for fallback positioning
            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (groundPlane.Raycast(ray, out float dist))
                    lastMouseWorld = ray.GetPoint(dist);
            }

            // Update cost label each frame
            if (costLabel != null)
            {
                int cost      = pc.SelectedTowerType?.Cost ?? 0;
                int gold      = Economy.Instance?.Gold ?? 0;
                bool canAfford = gold >= cost;
                costLabel.text  = $"Cout: {cost}c";
                costLabel.color = canAfford ? LabelAfford : LabelTooExp;
            }

            UpdateRangeRingLinePositions();
            UpdateAimLine();
        }

        private void LateUpdate()
        {
            if (ghost == null || !ghost.activeSelf || costLabel == null || cam == null) return;
            costLabel.transform.rotation = cam.transform.rotation;
        }

        private void BuildGhostFor(TowerType towerType)
        {
            // Destroy previous ghost
            if (ghost != null)
            {
                Object.Destroy(ghost);
                ghost          = null;
                rangeRing      = null;
                rangeRingLine  = null;
                costLabel      = null;
                lastBuiltRange = -1f;
            }

            // Try to load GLTF prefab from AssetRegistry
            GameObject? sourcePrefab = null;
            if (!string.IsNullOrEmpty(towerType.AssetKey))
            {
                var registry = Resources.Load<AssetRegistry>("AssetRegistry");
                if (registry != null)
                    sourcePrefab = registry.Get(towerType.AssetKey);
            }

            if (sourcePrefab != null)
            {
                ghost = Object.Instantiate(sourcePrefab, transform);
            }
            else
            {
                // Fallback to cube when no GLTF available
                ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ghost.transform.SetParent(transform, false);
            }

            ghost.name = "TowerGhost";

            // Remove all colliders so ghost never intercepts raycasts
            foreach (var col in ghost.GetComponentsInChildren<Collider>(true))
                Object.Destroy(col);

            // Replace all materials with transparent variant + apply tint via MPB
            if (ghostMatTransparent != null)
            {
                foreach (var rend in ghost.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = new Material[rend.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++)
                        mats[i] = ghostMatTransparent;
                    rend.sharedMaterials = mats;
                }
            }

            ApplyTintToGhost(ColorValid);

            // Cost label — world-space TMP billboarded in LateUpdate
            var labelGo = new GameObject("GhostCostLabel");
            labelGo.transform.SetParent(ghost.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            costLabel = labelGo.AddComponent<TextMeshPro>();
            costLabel.fontSize              = 3.5f;
            costLabel.fontStyle             = FontStyles.Bold;
            costLabel.alignment             = TextAlignmentOptions.Center;
            costLabel.outlineWidth          = 0.25f;
            costLabel.outlineColor          = new Color32(0, 0, 0, 220);
            costLabel.textWrappingMode      = TMPro.TextWrappingModes.NoWrap;
            costLabel.autoSizeTextContainer = false;
            costLabel.rectTransform.sizeDelta = new Vector2(4f, 1f);

            ghost.SetActive(false);
        }

        private void ApplyTintToGhost(Color tint)
        {
            if (ghost == null) return;
            _mpb ??= new MaterialPropertyBlock();
            _mpb.SetColor("_BaseColor", tint);
            foreach (var rend in ghost.GetComponentsInChildren<Renderer>(true))
                rend.SetPropertyBlock(_mpb);
        }

        private void OnHoverCell(Vector2Int? cell)
        {
            if (ghost == null) return;

            var pc = PlacementController.Instance;
            if (pc == null || pc.SelectedTowerType == null) { HideGhost(); return; }

            bool isValid = cell.HasValue;
            if (isValid)
            {
                var grid = PathManager.Instance!.Grid!;
                Vector3 pos = GridCoords.CellToWorld(cell!.Value.x, cell.Value.y, grid.Width, grid.Height, grid.CellSize);
                pos.y = 0.3f;
                ghost.transform.position = pos;
                ApplyTintToGhost(ColorValid);
            }
            else
            {
                Vector3 pos = lastMouseWorld;
                pos.y = 0.3f;
                ghost.transform.position = pos;
                ApplyTintToGhost(ColorInvalid);
            }

            SetRingLineColor(isValid ? RingColorValid : RingColorInvalid);

            ghost.SetActive(true);
            if (rangeRing     != null) rangeRing.SetActive(true);
            if (rangeRingLine != null) rangeRingLine.enabled = true;
            ShowPathDots();
        }

        private void SetRingLineColor(Color c)
        {
            if (rangeRingLineMat == null) return;
            if (rangeRingLineMat.HasProperty("_BaseColor")) rangeRingLineMat.SetColor("_BaseColor", c);
            else if (rangeRingLineMat.HasProperty("_Color")) rangeRingLineMat.SetColor("_Color", c);
        }

        private void HideGhost()
        {
            if (ghost          != null) ghost.SetActive(false);
            if (rangeRing      != null) rangeRing.SetActive(false);
            if (rangeRingLine  != null) rangeRingLine.enabled = false;
            if (aimLine        != null) aimLine.enabled = false;
            HidePathDots();
        }

        private void ShowPathDots()
        {
            var pm = PathManager.Instance;
            if (pm == null || pm.Paths.Count == 0) return;

            // Collect all unique waypoint positions across all paths
            var seen = new HashSet<Vector3>();
            foreach (var path in pm.Paths)
                foreach (var wp in path)
                    seen.Add(wp);

            // Expand pool if needed, reuse existing dots
            int idx = 0;
            foreach (var wp in seen)
            {
                if (idx >= _pathDots.Count)
                {
                    var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    dot.name = "PathIndicatorDot";
                    Object.Destroy(dot.GetComponent<Collider>());
                    var rend = dot.GetComponent<Renderer>();
                    if (rend != null) rend.sharedMaterial = dotMaterial;
                    dot.transform.SetParent(transform, false);
                    _pathDots.Add(dot);
                }
                var d = _pathDots[idx];
                d.transform.position   = new Vector3(wp.x, 0.01f, wp.z);
                d.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                d.SetActive(true);
                idx++;
            }

            // Hide excess dots from previous larger path
            for (int i = idx; i < _pathDots.Count; i++)
                _pathDots[i].SetActive(false);
        }

        private void HidePathDots()
        {
            foreach (var d in _pathDots) d.SetActive(false);
        }

        private static Material BuildDotMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", DotColor);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", DotColor);
            return mat;
        }

        private void BuildAimLine()
        {
            var go = new GameObject("GhostAimLine");
            go.transform.SetParent(transform, false);
            aimLine = go.AddComponent<LineRenderer>();
            aimLine.positionCount   = 2;
            aimLine.widthMultiplier = 0.06f;
            aimLine.useWorldSpace   = true;
            aimLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            aimLine.receiveShadows  = false;

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", AimLineColor);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color",    AimLineColor);
            aimLine.sharedMaterial = mat;
            aimLine.enabled = false;
        }

        private void UpdateAimLine()
        {
            if (aimLine == null || ghost == null || !ghost.activeSelf) { if (aimLine != null) aimLine.enabled = false; return; }

            float range = PlacementController.Instance?.SelectedTowerType?.Range ?? 0f;
            if (range <= 0f) { aimLine.enabled = false; return; }

            Vector3 origin = ghost.transform.position;
            Enemy?  nearest   = null;
            float   nearestSq = range * range;

            var activeEnemies = WaveManager.Instance?.ActiveEnemies;
            if (activeEnemies == null) { aimLine.enabled = false; return; }

            foreach (var enemy in activeEnemies)
            {
                if (enemy == null) continue;
                float sq = (enemy.transform.position - origin).sqrMagnitude;
                if (sq <= nearestSq) { nearestSq = sq; nearest = enemy; }
            }

            if (nearest == null) { aimLine.enabled = false; return; }

            Vector3 from = new Vector3(origin.x,   0.5f, origin.z);
            Vector3 to   = new Vector3(nearest.transform.position.x, 0.5f, nearest.transform.position.z);
            aimLine.SetPosition(0, from);
            aimLine.SetPosition(1, to);
            aimLine.enabled = true;
        }

        private void BuildRangeRing(float range)
        {
            if (rangeRing     != null) Object.Destroy(rangeRing);
            if (rangeRingLine != null) Object.Destroy(rangeRingLine.gameObject);
            rangeRingLine = null;
            lastBuiltRange = range;
            if (range <= 0f || ghost == null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "GhostRangeRing";
            go.transform.SetParent(ghost.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float parentScale = ghost.transform.localScale.x;
            float diameter    = parentScale > 0f ? (range * 2f) / parentScale : range * 2f;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);
            Object.Destroy(go.GetComponent<Collider>());

            const int texSize = 64;
            var tex    = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[texSize * texSize];
            float half = texSize * 0.5f;
            for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
            {
                float dx   = (x - half) / half;
                float dy   = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.SmoothStep(1f, 0f, dist) * 0.4f;
                byte  a    = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
                pixels[y * texSize + x] = new Color32(102, 222, 255, a);
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.mainTexture = tex;
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite",  0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.material = mat;

            go.SetActive(false);
            rangeRing = go;

            BuildRangeRingLine(range);
        }

        private void BuildRangeRingLine(float range)
        {
            if (range <= 0f) return;

            var lineGo = new GameObject("GhostRangeRingLine");
            lineGo.transform.SetParent(transform, false); // child of controller, not ghost

            rangeRingLine = lineGo.AddComponent<LineRenderer>();
            rangeRingLine.positionCount   = RingSegments + 1; // +1 to close the loop
            rangeRingLine.loop            = false;
            rangeRingLine.useWorldSpace   = true;
            rangeRingLine.widthMultiplier = 0.07f;
            rangeRingLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rangeRingLine.receiveShadows  = false;

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            rangeRingLineMat = new Material(shader);
            if (rangeRingLineMat.HasProperty("_Surface"))
            {
                rangeRingLineMat.SetFloat("_Surface", 1f);
                rangeRingLineMat.SetFloat("_ZWrite", 0f);
                rangeRingLineMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                rangeRingLineMat.renderQueue = 3001;
            }
            rangeRingLine.sharedMaterial = rangeRingLineMat;
            SetRingLineColor(RingColorValid);
            rangeRingLine.enabled = false;
        }

        private void UpdateRangeRingLinePositions()
        {
            if (rangeRingLine == null || ghost == null) return;
            float range  = PlacementController.Instance?.SelectedTowerType?.Range ?? 0f;
            Vector3 center = new Vector3(ghost.transform.position.x, 0.06f, ghost.transform.position.z);
            for (int i = 0; i <= RingSegments; i++)
            {
                float angle = (i / (float)RingSegments) * Mathf.PI * 2f;
                rangeRingLine.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * range, 0f, Mathf.Sin(angle) * range));
            }
        }
    }
}
