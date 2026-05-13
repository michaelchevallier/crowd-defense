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
        // Quad meshes shared across all path tiles
        // -------------------------------------------------------------------------

        private static Mesh? _quadMesh;
        private static Mesh? _bridgePlankMesh;
        private static Mesh? _streamMesh;

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

        // Narrower plank quad for bridges: 70% width perpendicular to the bridge
        // direction (X axis in local space; segment Y-rotation aligns Z with path).
        // This exposes the animated water/lava slab beneath at the bridge sides.
        private static Mesh GetBridgePlankMesh(float cellSize)
        {
            if (_bridgePlankMesh != null) return _bridgePlankMesh;
            float hx = cellSize * 0.70f * 0.5f;   // narrow across path (sides exposed)
            float hz = cellSize * 0.98f * 0.5f;   // full along path so adjacent bridge tiles touch
            _bridgePlankMesh = new Mesh { name = "PathTile_Bridge" };
            _bridgePlankMesh.vertices = new[]
            {
                new Vector3(-hx, 0.06f, -hz),
                new Vector3( hx, 0.06f, -hz),
                new Vector3( hx, 0.06f,  hz),
                new Vector3(-hx, 0.06f,  hz),
            };
            _bridgePlankMesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            _bridgePlankMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _bridgePlankMesh.RecalculateNormals();
            _bridgePlankMesh.RecalculateBounds();
            return _bridgePlankMesh;
        }

        // Stream overlay quad — slightly above path surface, full cell width,
        // UV tiled (2x) for scrolling animation in Update.
        private static Mesh GetStreamMesh(float cellSize)
        {
            if (_streamMesh != null) return _streamMesh;
            float h = cellSize * 0.98f * 0.5f;
            _streamMesh = new Mesh { name = "PathTile_Stream" };
            _streamMesh.vertices = new[]
            {
                new Vector3(-h, 0.04f, -h),
                new Vector3( h, 0.04f, -h),
                new Vector3( h, 0.04f,  h),
                new Vector3(-h, 0.04f,  h),
            };
            _streamMesh.uv = new[] { new Vector2(0, 0), new Vector2(2, 0), new Vector2(2, 2), new Vector2(0, 2) };
            _streamMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _streamMesh.RecalculateNormals();
            _streamMesh.RecalculateBounds();
            return _streamMesh;
        }

        // -------------------------------------------------------------------------
        // Public API — called from MapRenderer.Start()
        // -------------------------------------------------------------------------

        // Spawns one GameObject per path/bridge cell, parented under this transform.
        // For themes with a signature flowing surface (Volcan, Submarin, Desert), an
        // extra animated stream quad is spawned above each path tile. Returns the number
        // of tiles spawned (path + bridge cells, excluding stream overlays).
        public int SpawnPathTiles(GridData grid, LevelTheme theme)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            _streamRenderers.Clear();

            var mesh = GetQuadMesh(grid.CellSize);
            var bridgeMesh = GetBridgePlankMesh(grid.CellSize);
            var streamMesh = GetStreamMesh(grid.CellSize);
            var streamKind = ResolveStreamKind(theme);
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
                    Mesh tileMesh = mesh;

                    if (isBridgeWater)
                    {
                        mat = GetBridgeWoodMat();
                        seg = GetBridgeSegment(grid, c, r);
                        tileMesh = bridgeMesh;
                    }
                    else if (isBridgeLava)
                    {
                        mat = GetBridgeLavaMat();
                        seg = GetBridgeSegment(grid, c, r);
                        tileMesh = bridgeMesh;
                    }
                    else
                    {
                        int mask = WalkMask(grid, c, r);
                        seg = Classify(mask);
                        mat = GetPathMat(seg.Topo, theme);
                    }

                    Vector3 pos = GridCoords.CellToWorld(c, r, grid.Width, grid.Height, grid.CellSize);
                    SpawnTile(tileMesh, mat, pos, seg.RotY, $"PT_{c}_{r}");
                    count++;

                    if (isPath && streamKind != StreamKind.None)
                        SpawnStreamOverlay(streamMesh, streamKind, pos, seg.RotY, $"PT_{c}_{r}_stream");
                }
            }

#if UNITY_EDITOR
            Debug.Log($"[PathTilesController] Spawned {count} path tiles, theme={theme}, streamKind={streamKind}, streams={_streamRenderers.Count}");
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

        // -------------------------------------------------------------------------
        // Theme stream overlays — animated UV scroll + (volcan only) emissive pulse.
        // W3 Desert: sand drift wisp ; W4 Volcan: lava glow ; W8 Submarin: water flow.
        // Other themes: no overlay (None).
        // -------------------------------------------------------------------------

        private enum StreamKind { None, Water, Lava, Sand }

        private struct StreamEntry
        {
            public MeshRenderer Renderer;
            public StreamKind Kind;
            public Vector2 ScrollDir;   // local-space UV scroll direction (path-aligned)
        }

        private readonly List<StreamEntry> _streamRenderers = new();
        private MaterialPropertyBlock? _streamMpb;
        private Material? _streamWaterMat;
        private Material? _streamLavaMat;
        private Material? _streamSandMat;

        private static readonly int BaseMapStId      = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int StreamEmissionId = Shader.PropertyToID("_EmissionColor");

        private static StreamKind ResolveStreamKind(LevelTheme theme) => theme switch
        {
            LevelTheme.Submarin => StreamKind.Water,
            LevelTheme.Volcan   => StreamKind.Lava,
            LevelTheme.Desert   => StreamKind.Sand,
            _                   => StreamKind.None,
        };

        private void SpawnStreamOverlay(Mesh mesh, StreamKind kind, Vector3 worldPos, float rotY, string goName)
        {
            var mat = GetStreamMat(kind);
            if (mat == null) return;

            var go = new GameObject(goName);
            go.transform.SetParent(transform, false);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0f, rotY * Mathf.Rad2Deg, 0f);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.receiveShadows = false;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _streamRenderers.Add(new StreamEntry
            {
                Renderer  = mr,
                Kind      = kind,
                ScrollDir = new Vector2(0f, 1f),   // along local +Z (path forward axis)
            });
        }

        private Material? GetStreamMat(StreamKind kind)
        {
            switch (kind)
            {
                case StreamKind.Water:
                    if (_streamWaterMat == null) _streamWaterMat = BuildStreamMat(StreamKind.Water);
                    return _streamWaterMat;
                case StreamKind.Lava:
                    if (_streamLavaMat == null) _streamLavaMat = BuildStreamMat(StreamKind.Lava);
                    return _streamLavaMat;
                case StreamKind.Sand:
                    if (_streamSandMat == null) _streamSandMat = BuildStreamMat(StreamKind.Sand);
                    return _streamSandMat;
                default: return null;
            }
        }

        private static Material BuildStreamMat(StreamKind kind)
        {
            // Prefer a transparent-capable base; fall back to Toon_Default.
            var baseMat = Resources.Load<Material>("Materials/Toon_Stream")
                       ?? Resources.Load<Material>("Materials/Toon_Default");
            var mat = baseMat != null ? new Material(baseMat) : new Material(ShaderUtil.GetToonShader());
            mat.name = $"Stream_{kind}";
            mat.enableInstancing = true;

            // Tint + (lava) emissive setup. Alpha kept low so stream reads as overlay.
            Color tint = kind switch
            {
                StreamKind.Water => new Color(0.30f, 0.55f, 0.85f, 0.55f),
                StreamKind.Lava  => new Color(1.00f, 0.45f, 0.10f, 0.70f),
                StreamKind.Sand  => new Color(0.95f, 0.82f, 0.55f, 0.30f),
                _                => Color.white,
            };
            SetBaseColor(mat, tint);

            if (kind == StreamKind.Lava && mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", new Color(1.0f, 0.35f, 0.10f) * 0.6f);
                mat.EnableKeyword("_EMISSION");
            }

            // URP transparency: best-effort property/keyword toggle. Materials shipped with the
            // project should pre-author the Surface=Transparent flag in the .mat asset; this is
            // a safety net for the procedural fallback.
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);   // 1=Transparent (URP)
            if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend",   0f);    // 0=Alpha
            mat.renderQueue = 3000;

            return mat;
        }

        private void Update()
        {
            if (_streamRenderers.Count == 0) return;
            _streamMpb ??= new MaterialPropertyBlock();

            float t = Time.time;
            // Lava emissive pulse — sine 0.5 → 1.5 over 1.4 s
            float lavaPulse = 0.5f + 0.5f * (Mathf.Sin(t * (Mathf.PI * 2f / 1.4f)) + 1f);
            Color lavaEmissive = new Color(1.0f, 0.35f, 0.10f) * (0.4f + 0.6f * lavaPulse);

            for (int i = 0; i < _streamRenderers.Count; i++)
            {
                var e = _streamRenderers[i];
                if (e.Renderer == null) continue;

                float speed = e.Kind switch
                {
                    StreamKind.Water => 0.20f,
                    StreamKind.Lava  => 0.10f,
                    StreamKind.Sand  => 0.08f,
                    _                => 0f,
                };
                Vector2 offset = e.ScrollDir * (t * speed);

                e.Renderer.GetPropertyBlock(_streamMpb);
                _streamMpb.SetVector(BaseMapStId, new Vector4(1f, 1f, offset.x, offset.y));
                if (e.Kind == StreamKind.Lava)
                    _streamMpb.SetColor(StreamEmissionId, lavaEmissive);
                e.Renderer.SetPropertyBlock(_streamMpb);
            }
        }
    }
}
