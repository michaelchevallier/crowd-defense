#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.Visual
{
    // Dashed line preview of enemy traversal path while player hovers a placement cell.
    // Subscribes to PlacementController.OnHoverPlacementCell.
    // Uses a single LineRenderer with texture-scroll UV to simulate dashes without
    // requiring a dedicated material asset — create the material procedurally at runtime.
    public class PathfinderVisualization : MonoSingleton<PathfinderVisualization>
    {
        [SerializeField] private Color lineColor = new Color(1f, 0.9f, 0.2f, 0.85f);
        [SerializeField] private float lineWidth = 0.18f;
        // Texture tiles per world unit — controls dash density
        [SerializeField] private float tilesPerUnit = 0.8f;
        // UV scroll speed (world units per second)
        [SerializeField] private float scrollSpeed = 2f;

        private LineRenderer? _lr;
        private Material? _mat;
        private float _totalLength;
        private float _scrollOffset;

        protected override void OnAwakeSingleton()
        {
            BuildLineRenderer();
        }

        private void OnEnable()
        {
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnHoverPlacementCell += OnHover;
        }

        private void OnDisable()
        {
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnHoverPlacementCell -= OnHover;
            HideLine();
        }

        private void Update()
        {
            if (_lr == null || !_lr.enabled || _mat == null || _totalLength <= 0f) return;
            _scrollOffset += scrollSpeed * Time.deltaTime;
            _mat.mainTextureOffset = new Vector2(_scrollOffset, 0f);
        }

        private void OnHover(Vector2Int? cell)
        {
            if (cell == null)
            {
                HideLine();
                return;
            }

            var pm = PathManager.Instance;
            if (pm == null || pm.Paths.Count == 0)
            {
                HideLine();
                return;
            }

            // Use path 0 (first portal → first castle) as canonical preview path
            var waypoints = pm.Paths[0];
            if (waypoints.Count < 2)
            {
                HideLine();
                return;
            }

            ShowPath(waypoints);
        }

        private void ShowPath(IReadOnlyList<Vector3> waypoints)
        {
            if (_lr == null) return;

            _lr.positionCount = waypoints.Count;
            _totalLength = 0f;
            for (int i = 0; i < waypoints.Count; i++)
            {
                // Lift slightly above ground to avoid z-fighting with path tiles
                _lr.SetPosition(i, waypoints[i] + Vector3.up * 0.08f);
                if (i > 0) _totalLength += (waypoints[i] - waypoints[i - 1]).magnitude;
            }

            // Scale texture so dash count stays consistent regardless of path length
            if (_mat != null)
                _mat.mainTextureScale = new Vector2(_totalLength * tilesPerUnit, 1f);

            _lr.enabled = true;
        }

        private void HideLine()
        {
            if (_lr != null) _lr.enabled = false;
            _totalLength = 0f;
        }

        private void BuildLineRenderer()
        {
            var go = new GameObject("PathfinderLine");
            go.transform.SetParent(transform, false);

            _lr = go.AddComponent<LineRenderer>();
            _lr.useWorldSpace = true;
            _lr.startWidth = lineWidth;
            _lr.endWidth = lineWidth;
            _lr.numCornerVertices = 0;
            _lr.numCapVertices = 2;
            _lr.textureMode = LineTextureMode.Tile;

            _mat = BuildDashedMaterial();
            _lr.material = _mat;
            _lr.enabled = false;
        }

        private Material BuildDashedMaterial()
        {
            // Build a 1×4 pixel dash texture: 2 opaque + 2 transparent pixels
            var tex = new Texture2D(4, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };
            tex.SetPixel(0, 0, lineColor);
            tex.SetPixel(1, 0, lineColor);
            tex.SetPixel(2, 0, Color.clear);
            tex.SetPixel(3, 0, Color.clear);
            tex.Apply();

            // Use Sprites/Default so it renders correctly in both URP and Built-in
            var mat = new Material(Shader.Find("Sprites/Default"))
            {
                mainTexture = tex
            };
            mat.SetFloat("_Mode", 2f); // Fade blend mode hint (URP ignores, Built-in respects)
            mat.color = Color.white;   // tint via texture pixels
            return mat;
        }
    }
}
