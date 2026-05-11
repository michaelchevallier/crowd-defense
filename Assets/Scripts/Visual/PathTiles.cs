#nullable enable
using System.Collections.Generic;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using UnityEngine;

namespace CrowdDefense.Visual
{
    // Spawns animated water/lava stream quads + bridge overlays above MapRenderer slabs.
    // Port of V5 PathTiles.js + PathVariant.js logic.
    // Lifecycle: called by LevelVisualBridge on OnLevelStart; clears on OnLevelEnd.
    [DefaultExecutionOrder(60)]
    public class PathTiles : MonoSingleton<PathTiles>
    {
        // -----------------------------------------------------------------------
        // Tile variant enum — mirrors PathVariant.js
        // -----------------------------------------------------------------------
        private enum TileVariant { Dot, Straight, Corner, T, Cross, Cap }

        private readonly struct VariantResult
        {
            public readonly TileVariant Variant;
            public readonly float RotY;
            public VariantResult(TileVariant v, float rotY) { Variant = v; RotY = rotY; }
        }

        // -----------------------------------------------------------------------
        // Predicates (mirrors isWaterLike / isLavaLike / isPathLike)
        // -----------------------------------------------------------------------
        private static bool IsWaterLike(char ch) => ch == GridCoords.WATER || ch == GridCoords.BRIDGE_WATER;
        private static bool IsLavaLike(char ch)  => ch == GridCoords.LAVA  || ch == GridCoords.BRIDGE_LAVA;
        private static bool IsPathLike(char ch)  =>
            ch == GridCoords.PATH || ch == GridCoords.PORTAL || ch == GridCoords.CASTLE ||
            ch == GridCoords.BRIDGE_WATER || ch == GridCoords.BRIDGE_LAVA;
        private static bool IsBridge(char ch) => ch == GridCoords.BRIDGE_WATER || ch == GridCoords.BRIDGE_LAVA;

        // -----------------------------------------------------------------------
        // State
        // -----------------------------------------------------------------------
        private readonly List<GameObject> _spawnedObjects = new();
        private readonly List<Material>   _animatedMats   = new();

        // Shader property IDs — cached once (avoid string lookup per-frame)
        private static readonly int _shaderTime      = Shader.PropertyToID("_Time");
        private static readonly int _shaderBaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int _shaderEmission  = Shader.PropertyToID("_EmissionColor");

        // -----------------------------------------------------------------------
        // Public API — called by LevelVisualBridge
        // -----------------------------------------------------------------------
        public void BuildForLevel(GridData grid, LevelTheme theme)
        {
            ClearAll();

            var config = LevelThemeMaterialConfig.Get();
            BuildStreamLayer(grid, theme, config);
            BuildBridgeLayer(grid, theme, config);

#if UNITY_EDITOR
            Debug.Log($"[PathTiles] built {_spawnedObjects.Count} objects, {_animatedMats.Count} animated mats, theme={theme}");
#endif
        }

        public void ClearAll()
        {
            foreach (var go in _spawnedObjects)
                if (go != null) Destroy(go);
            _spawnedObjects.Clear();
            _animatedMats.Clear();
        }

        // -----------------------------------------------------------------------
        // Stream layer — water / lava quads with animated material
        // Batched: one combined mesh per stream type when > 50 cells (perf budget).
        // -----------------------------------------------------------------------
        private void BuildStreamLayer(GridData grid, LevelTheme theme, LevelThemeMaterialConfig? config)
        {
            var waterCells = new List<Vector2Int>();
            var lavaCells  = new List<Vector2Int>();

            for (int r = 0; r < grid.Height; r++)
                for (int c = 0; c < grid.Width; c++)
                {
                    char ch = grid.At(c, r);
                    if (IsWaterLike(ch) && !IsBridge(ch)) waterCells.Add(new Vector2Int(c, r));
                    else if (IsLavaLike(ch) && !IsBridge(ch)) lavaCells.Add(new Vector2Int(c, r));
                }

            if (waterCells.Count > 0)
            {
                var mat = config?.GetWaterMat(theme) ?? MakeFallbackWaterMat();
                SpawnStreamBatch(grid, waterCells, mat, yOffset: 0.02f, "WaterStreams");
            }

            if (lavaCells.Count > 0)
            {
                var mat = config?.GetLavaMat(theme) ?? MakeFallbackLavaMat();
                SpawnStreamBatch(grid, lavaCells, mat, yOffset: 0.02f, "LavaStreams");
            }
        }

        // If <= 50 cells: individual GameObjects (simpler, allows per-cell UV variant).
        // If > 50 cells: single combined CombinedMesh for draw-call budget.
        private void SpawnStreamBatch(GridData grid, List<Vector2Int> cells, Material mat, float yOffset, string label)
        {
            var matInstance = Object.Instantiate(mat);
            _animatedMats.Add(matInstance);

            float cs = grid.CellSize;

            if (cells.Count <= 50)
            {
                foreach (var cell in cells)
                    SpawnStreamQuad(grid, cell.x, cell.y, cs, matInstance, yOffset, label);
            }
            else
            {
                SpawnCombinedStreamMesh(grid, cells, cs, matInstance, yOffset, label);
            }
        }

        private void SpawnStreamQuad(GridData grid, int c, int r, float cs, Material mat, float yOffset, string label)
        {
            Vector3 pos = GridCoords.CellToWorld(c, r, grid.Width, grid.Height, cs);
            pos.y = yOffset;

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = $"{label}_{c}_{r}";
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(cs * 1.02f, cs * 1.02f, 1f);

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            _spawnedObjects.Add(go);
        }

        // Single mesh combining all quads — one draw call for large stream areas.
        private void SpawnCombinedStreamMesh(GridData grid, List<Vector2Int> cells, float cs, Material mat, float yOffset, string label)
        {
            var combineInstances = new CombineInstance[cells.Count];
            var quadMesh = BuildQuadMesh(cs * 1.02f);

            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                Vector3 pos = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, cs);
                pos.y = yOffset;
                var matrix = Matrix4x4.TRS(pos, Quaternion.Euler(90f, 0f, 0f), Vector3.one);
                combineInstances[i] = new CombineInstance { mesh = quadMesh, transform = matrix };
            }

            var combined = new Mesh { name = label };
            combined.CombineMeshes(combineInstances, mergeSubMeshes: true, useMatrices: true);

            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().mesh = combined;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.receiveShadows = true;

            _spawnedObjects.Add(go);
            Object.Destroy(quadMesh);
        }

        // -----------------------------------------------------------------------
        // Bridge layer — path quads over water/lava cells (cross-stream walkways)
        // Uses bitmask variant rotation — straight/corner/cross bridges orient correctly.
        // -----------------------------------------------------------------------
        private void BuildBridgeLayer(GridData grid, LevelTheme theme, LevelThemeMaterialConfig? config)
        {
            bool hasLavaBridge  = false;
            bool hasWaterBridge = false;

            for (int r = 0; r < grid.Height; r++)
                for (int c = 0; c < grid.Width; c++)
                {
                    char ch = grid.At(c, r);
                    if (ch == GridCoords.BRIDGE_WATER) hasWaterBridge = true;
                    if (ch == GridCoords.BRIDGE_LAVA)  hasLavaBridge  = true;
                }

            if (hasWaterBridge)
            {
                var mat = MakeBridgeWaterMat();
                SpawnBridgeQuads(grid, GridCoords.BRIDGE_WATER, mat, yOffset: 0.04f, "BridgeWater");
            }

            if (hasLavaBridge)
            {
                var mat = MakeBridgeLavaMat(theme);
                SpawnBridgeQuads(grid, GridCoords.BRIDGE_LAVA, mat, yOffset: 0.04f, "BridgeLava");
            }
        }

        private void SpawnBridgeQuads(GridData grid, char bridgeChar, Material mat, float yOffset, string label)
        {
            float cs = grid.CellSize;

            for (int r = 0; r < grid.Height; r++)
            {
                for (int c = 0; c < grid.Width; c++)
                {
                    if (grid.At(c, r) != bridgeChar) continue;

                    var vr = GetTileVariant(grid, c, r, IsPathLike);
                    Vector3 pos = GridCoords.CellToWorld(c, r, grid.Width, grid.Height, cs);
                    pos.y = yOffset;

                    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    go.name = $"{label}_{c}_{r}";
                    go.transform.SetParent(transform, false);
                    go.transform.position = pos;
                    // XZ plane: Euler(90,rotY,0) — align flat then rotate to match path direction
                    go.transform.rotation = Quaternion.Euler(90f, vr.RotY * Mathf.Rad2Deg, 0f);
                    go.transform.localScale = new Vector3(cs * 1.02f, cs * 1.02f, 1f);

                    var col = go.GetComponent<Collider>();
                    if (col != null) Destroy(col);

                    var matInst = Object.Instantiate(mat);
                    go.GetComponent<MeshRenderer>().sharedMaterial = matInst;

                    _spawnedObjects.Add(go);
                }
            }
        }

        // -----------------------------------------------------------------------
        // Tile variant — port of PathVariant.js getTileVariant()
        // Bitmask: N=1, E=2, S=4, W=8
        // -----------------------------------------------------------------------
        private static VariantResult GetTileVariant(GridData grid, int c, int r, System.Func<char, bool> predicate)
        {
            int n = (r > 0              && predicate(grid.At(c, r - 1))) ? 1 : 0;
            int e = (c < grid.Width - 1 && predicate(grid.At(c + 1, r))) ? 2 : 0;
            int s = (r < grid.Height - 1 && predicate(grid.At(c, r + 1))) ? 4 : 0;
            int w = (c > 0              && predicate(grid.At(c - 1, r))) ? 8 : 0;
            int mask = n | e | s | w;

            const float PI  = Mathf.PI;
            const float HPI = Mathf.PI / 2f;

            return mask switch
            {
                15 => new VariantResult(TileVariant.Cross,    0f),
                7  => new VariantResult(TileVariant.T,        0f),
                11 => new VariantResult(TileVariant.T,        HPI),
                13 => new VariantResult(TileVariant.T,        PI),
                14 => new VariantResult(TileVariant.T,        3f * HPI),
                6  => new VariantResult(TileVariant.Corner,   HPI),
                3  => new VariantResult(TileVariant.Corner,   PI),
                9  => new VariantResult(TileVariant.Corner,   3f * HPI),
                12 => new VariantResult(TileVariant.Corner,   0f),
                5  => new VariantResult(TileVariant.Straight, 0f),
                10 => new VariantResult(TileVariant.Straight, HPI),
                1  => new VariantResult(TileVariant.Cap,      0f),
                2  => new VariantResult(TileVariant.Cap,      3f * HPI),
                4  => new VariantResult(TileVariant.Cap,      PI),
                8  => new VariantResult(TileVariant.Cap,      HPI),
                _  => new VariantResult(TileVariant.Dot,      0f),
            };
        }

        // -----------------------------------------------------------------------
        // Procedural mesh helpers
        // -----------------------------------------------------------------------
        private static Mesh BuildQuadMesh(float size)
        {
            float h = size / 2f;
            var mesh = new Mesh { name = "StreamQuad" };
            mesh.vertices = new Vector3[]
            {
                new(-h, 0f,  h),
                new( h, 0f,  h),
                new( h, 0f, -h),
                new(-h, 0f, -h),
            };
            mesh.uv = new Vector2[]
            {
                new(0f, 1f), new(1f, 1f), new(1f, 0f), new(0f, 0f),
            };
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            return mesh;
        }

        // -----------------------------------------------------------------------
        // Fallback materials — procedural color mats when no SO asset is assigned.
        // Use Toon/Water or Toon/Lava shader if present, else Standard/URP Lit.
        // -----------------------------------------------------------------------
        private Material MakeFallbackWaterMat()
        {
            var mat = new Material(ShaderUtil.GetToonWaterShader()) { name = "FallbackWater" };
            mat.SetColor(_shaderBaseColor, new Color(0.18f, 0.42f, 0.80f, 0.88f));
            mat.enableInstancing = true;
            _animatedMats.Add(mat);
            return mat;
        }

        private Material MakeFallbackLavaMat()
        {
            var mat = new Material(ShaderUtil.GetToonLavaShader()) { name = "FallbackLava" };
            mat.SetColor(_shaderBaseColor, new Color(0.95f, 0.30f, 0.05f, 1f));
            if (mat.HasProperty(_shaderEmission))
                mat.SetColor(_shaderEmission, new Color(1.0f, 0.25f, 0.0f) * 1.4f);
            mat.enableInstancing = true;
            _animatedMats.Add(mat);
            return mat;
        }

        // Bridge water — warm brown planks (toon shaded)
        private static Material MakeBridgeWaterMat()
        {
            var mat = new Material(ShaderUtil.GetToonShader()) { name = "BridgeWood" };
            mat.SetColor(Shader.PropertyToID("_BaseColor"), new Color(0.62f, 0.40f, 0.22f));
            mat.enableInstancing = true;
            return mat;
        }

        // Bridge lava — dark stone with emissive crack tint, varies by theme
        private static Material MakeBridgeLavaMat(LevelTheme theme)
        {
            var mat = new Material(ShaderUtil.GetToonLavaShader()) { name = "BridgeStone" };
            var baseCol = theme == LevelTheme.Volcan
                ? new Color(0.20f, 0.18f, 0.17f)
                : new Color(0.25f, 0.22f, 0.20f);
            mat.SetColor(Shader.PropertyToID("_BaseColor"), baseCol);
            if (mat.HasProperty(_shaderEmission))
                mat.SetColor(_shaderEmission, new Color(0.80f, 0.12f, 0.01f) * 0.5f);
            mat.enableInstancing = true;
            return mat;
        }

        // -----------------------------------------------------------------------
        // Update — drive shader _Time for animated water/lava mats that don't use
        // Unity's built-in _Time (e.g. custom scrolling UV offset or emissive pulse).
        // -----------------------------------------------------------------------
        private void Update()
        {
            if (_animatedMats.Count == 0) return;

            float t = Time.time;
            for (int i = 0; i < _animatedMats.Count; i++)
            {
                var mat = _animatedMats[i];
                if (mat == null) continue;
                if (mat.HasProperty("_FlowTime"))
                    mat.SetFloat("_FlowTime", t);
                if (mat.HasProperty("_PulseTime"))
                    mat.SetFloat("_PulseTime", t);
            }
        }
    }
}
