#nullable enable
using System.Collections.Generic;
using CrowdDefense.Common;
using CrowdDefense.Data;
using UnityEngine;

namespace CrowdDefense.Systems
{
    // Renders path cells as topology-aware flat quads (straight / corner / T / cross).
    // Each cell's shape is determined by its 4 walkable neighbors (N S E W bitmask).
    // Bridges (~ ^ cells) use distinct wood / lava materials that exploit URP emissive.
    // Placeholder-first: all materials are procedural solid-color; swap to textures via
    // LevelThemeMaterialConfig once R6-PARITY-001 lands.
    [DefaultExecutionOrder(55)]   // after MapRenderer (50) so it overlays correctly
    public class PathTilesController : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Topology helpers
        // -------------------------------------------------------------------------

        // Bitmask: N=1 E=2 S=4 W=8
        private enum Topo { Dot = 0, Cap, Straight, Corner, T, Cross }

        private readonly struct SegmentInfo
        {
            public readonly Topo Topo;
            public readonly float RotY;
            public SegmentInfo(Topo topo, float rotY) { Topo = topo; RotY = rotY; }
        }

        private static SegmentInfo Classify(int mask)
        {
            int bits = PopCount(mask);
            return bits switch
            {
                0 => new SegmentInfo(Topo.Dot, 0f),
                1 => mask switch
                {
                    1  => new SegmentInfo(Topo.Cap, 0f),                   // N open
                    2  => new SegmentInfo(Topo.Cap, Mathf.PI * 0.5f),      // E
                    4  => new SegmentInfo(Topo.Cap, Mathf.PI),             // S
                    8  => new SegmentInfo(Topo.Cap, Mathf.PI * 1.5f),      // W
                    _  => new SegmentInfo(Topo.Dot, 0f),
                },
                2 => mask switch
                {
                    1 | 4  => new SegmentInfo(Topo.Straight, 0f),              // NS
                    2 | 8  => new SegmentInfo(Topo.Straight, Mathf.PI * 0.5f), // EW
                    1 | 2  => new SegmentInfo(Topo.Corner, 0f),                // NE
                    1 | 8  => new SegmentInfo(Topo.Corner, Mathf.PI * 1.5f),   // NW
                    4 | 2  => new SegmentInfo(Topo.Corner, Mathf.PI * 0.5f),   // SE
                    4 | 8  => new SegmentInfo(Topo.Corner, Mathf.PI),          // SW
                    _      => new SegmentInfo(Topo.Straight, 0f),
                },
                3 => mask switch
                {
                    1 | 2 | 4  => new SegmentInfo(Topo.T, Mathf.PI * 0.5f),   // T open-W  (N E S)
                    1 | 4 | 8  => new SegmentInfo(Topo.T, Mathf.PI * 1.5f),   // T open-E  (N S W)
                    2 | 4 | 8  => new SegmentInfo(Topo.T, Mathf.PI),          // T open-N  (E S W)
                    1 | 2 | 8  => new SegmentInfo(Topo.T, 0f),                // T open-S  (N E W)
                    _          => new SegmentInfo(Topo.T, 0f),
                },
                _ => new SegmentInfo(Topo.Cross, 0f),
            };
        }

        private static int PopCount(int v)
        {
            int c = 0;
            while (v != 0) { c += v & 1; v >>= 1; }
            return c;
        }

        // -------------------------------------------------------------------------
        // Theme path colors (placeholder — swap to texture once R6-PARITY-001 lands)
        // -------------------------------------------------------------------------

        private static readonly Dictionary<LevelTheme, Color> _themePathColor = new()
        {
            { LevelTheme.Plaine,     new Color(0.78f, 0.65f, 0.44f) },
            { LevelTheme.Foret,      new Color(0.55f, 0.72f, 0.38f) },
            { LevelTheme.Desert,     new Color(0.92f, 0.80f, 0.48f) },
            { LevelTheme.Volcan,     new Color(0.55f, 0.30f, 0.20f) },
            { LevelTheme.Apocalypse, new Color(0.50f, 0.45f, 0.40f) },
            { LevelTheme.Espace,     new Color(0.25f, 0.22f, 0.42f) },
            { LevelTheme.Submarin,   new Color(0.22f, 0.50f, 0.65f) },
            { LevelTheme.Medieval,   new Color(0.62f, 0.58f, 0.48f) },
            { LevelTheme.Cyberpunk,  new Color(0.20f, 0.60f, 0.55f) },
            { LevelTheme.Foire,      new Color(0.88f, 0.55f, 0.68f) },
        };

        // -------------------------------------------------------------------------
        // Material cache — one per (Topo, LevelTheme)
        // -------------------------------------------------------------------------

        private readonly Dictionary<(Topo, LevelTheme), Material> _matCache = new();
        private Material? _bridgeWoodMat;
        private Material? _bridgeLavaMat;

        private Material GetPathMat(Topo topo, LevelTheme theme)
        {
            var key = (topo, theme);
            if (_matCache.TryGetValue(key, out var m)) return m;
            m = BuildPathMat(theme);
            _matCache[key] = m;
            return m;
        }

        private static Material BuildPathMat(LevelTheme theme)
        {
            // Try to load theme-specific textured path material first
            string themeName = theme switch
            {
                LevelTheme.Plaine => "plaine",
                LevelTheme.Foret => "foret",
                LevelTheme.Desert => "desert",
                LevelTheme.Volcan => "volcan",
                LevelTheme.Apocalypse => "apocalypse",
                LevelTheme.Espace => "espace",
                LevelTheme.Submarin => "submarin",
                LevelTheme.Medieval => "medieval",
                LevelTheme.Cyberpunk => "cyberpunk",
                LevelTheme.Foire => "foire",
                _ => "plaine",
            };
            var pathMat = Resources.Load<Material>("Materials/path_" + themeName);
            if (pathMat != null)
            {
                var cloned = new Material(pathMat);
                cloned.enableInstancing = true;
                return cloned;
            }

            // Fallback to color-based material
            var baseMat = Resources.Load<Material>("Materials/Toon_Default");
            var mat = baseMat != null ? new Material(baseMat) : new Material(ShaderUtil.GetToonShader());
            var color = _themePathColor.TryGetValue(theme, out var c) ? c : new Color(0.75f, 0.65f, 0.45f);
            SetBaseColor(mat, color);
            mat.enableInstancing = true;
            return mat;
        }

        private Material GetBridgeWoodMat()
        {
            if (_bridgeWoodMat != null) return _bridgeWoodMat;
            var baseMat = Resources.Load<Material>("Materials/Toon_Default");
            _bridgeWoodMat = baseMat != null ? new Material(baseMat) : new Material(ShaderUtil.GetToonShader());
            SetBaseColor(_bridgeWoodMat, new Color(0.55f, 0.38f, 0.22f));
            _bridgeWoodMat.enableInstancing = true;
            return _bridgeWoodMat;
        }

        private Material GetBridgeLavaMat()
        {
            if (_bridgeLavaMat != null) return _bridgeLavaMat;
            var baseMat = Resources.Load<Material>("Materials/Toon_Lava");
            _bridgeLavaMat = baseMat != null ? new Material(baseMat) : new Material(ShaderUtil.GetToonShader());
            SetBaseColor(_bridgeLavaMat, new Color(0.60f, 0.20f, 0.08f));
            // URP emissive glow for lava bridges
            if (_bridgeLavaMat.HasProperty("_EmissionColor"))
            {
                _bridgeLavaMat.SetColor("_EmissionColor", new Color(1.0f, 0.25f, 0.05f) * 0.8f);
                _bridgeLavaMat.EnableKeyword("_EMISSION");
            }
            _bridgeLavaMat.enableInstancing = true;
            return _bridgeLavaMat;
        }

        private static void SetBaseColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
        }

        // -------------------------------------------------------------------------
        // Quad mesh shared across all path tiles
        // -------------------------------------------------------------------------

        private static Mesh? _quadMesh;

        private static Mesh GetQuadMesh(float cellSize)
        {
            if (_quadMesh != null) return _quadMesh;
            // Flat XZ-plane quad, 1.02 overlap to hide seams (same as V4)
            float h = cellSize * 1.02f * 0.5f;
            _quadMesh = new Mesh { name = "PathTile_Quad" };
            _quadMesh.vertices = new[]
            {
                new Vector3(-h, 0.02f, -h),
                new Vector3( h, 0.02f, -h),
                new Vector3( h, 0.02f,  h),
                new Vector3(-h, 0.02f,  h),
            };
            _quadMesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            _quadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _quadMesh.RecalculateNormals();
            _quadMesh.RecalculateBounds();
            return _quadMesh;
        }

        // -------------------------------------------------------------------------
        // Public API — called from MapRenderer.Start()
        // -------------------------------------------------------------------------

        // Spawns one GameObject per path/bridge cell, parented under this transform.
        // Returns the number of tiles spawned.
        public int SpawnPathTiles(GridData grid, LevelTheme theme)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            var mesh = GetQuadMesh(grid.CellSize);
            int count = 0;

            for (int r = 0; r < grid.Height; r++)
            {
                for (int c = 0; c < grid.Width; c++)
                {
                    char ch = grid.At(c, r);
                    bool isBridgeWater = ch == GridCoords.BRIDGE_WATER;
                    bool isBridgeLava  = ch == GridCoords.BRIDGE_LAVA;
                    bool isPath = ch == GridCoords.PATH
                               || ch == GridCoords.PORTAL
                               || ch == GridCoords.CASTLE;

                    if (!isPath && !isBridgeWater && !isBridgeLava) continue;

                    Material mat;
                    SegmentInfo seg;

                    if (isBridgeWater)
                    {
                        mat = GetBridgeWoodMat();
                        seg = GetBridgeSegment(grid, c, r);
                    }
                    else if (isBridgeLava)
                    {
                        mat = GetBridgeLavaMat();
                        seg = GetBridgeSegment(grid, c, r);
                    }
                    else
                    {
                        int mask = WalkMask(grid, c, r);
                        seg = Classify(mask);
                        mat = GetPathMat(seg.Topo, theme);
                    }

                    Vector3 pos = GridCoords.CellToWorld(c, r, grid.Width, grid.Height, grid.CellSize);
                    SpawnTile(mesh, mat, pos, seg.RotY, $"PT_{c}_{r}");
                    count++;
                }
            }

#if UNITY_EDITOR
            Debug.Log($"[PathTilesController] Spawned {count} path tiles, theme={theme}");
#endif
            return count;
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        // Walkable-neighbour bitmask: N=1 E=2 S=4 W=8
        private static int WalkMask(GridData grid, int c, int r)
        {
            int mask = 0;
            if (r > 0               && GridCoords.Walkable.Contains(grid.At(c,     r - 1))) mask |= 1; // N
            if (c < grid.Width - 1  && GridCoords.Walkable.Contains(grid.At(c + 1, r    ))) mask |= 2; // E
            if (r < grid.Height - 1 && GridCoords.Walkable.Contains(grid.At(c,     r + 1))) mask |= 4; // S
            if (c > 0               && GridCoords.Walkable.Contains(grid.At(c - 1, r    ))) mask |= 8; // W
            return mask;
        }

        // Bridges use straight topology oriented along the bridge axis
        private static SegmentInfo GetBridgeSegment(GridData grid, int c, int r)
        {
            int mask = WalkMask(grid, c, r);
            bool ns = (mask & 1) != 0 || (mask & 4) != 0;
            bool ew = (mask & 2) != 0 || (mask & 8) != 0;
            if (ns && !ew) return new SegmentInfo(Topo.Straight, 0f);
            if (ew && !ns) return new SegmentInfo(Topo.Straight, Mathf.PI * 0.5f);
            return Classify(mask);
        }

        private void SpawnTile(Mesh mesh, Material mat, Vector3 worldPos, float rotY, string goName)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(transform, false);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0f, rotY * Mathf.Rad2Deg, 0f);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.receiveShadows = true;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
