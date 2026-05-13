#nullable enable
using System.Collections.Generic;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Visual;
using UnityEngine;

namespace CrowdDefense.Systems
{
    [DefaultExecutionOrder(50)]
    public class MapRenderer : MonoBehaviour
    {
        // Per-char cache keyed by (char, theme) so animated mats are theme-specific.
        private static readonly Dictionary<(char, LevelTheme), Material> _matCache = new();

        // Procedural textures generated once and reused across all cells.
        private static Texture2D? _texGrass;
        private static Texture2D? _texPath;
        private static Texture2D? _texDirt;

        // Theme resolved from LevelRunner at Start — default Plaine if not available.
        private LevelTheme _theme = LevelTheme.Plaine;

        // Spawned slab renderers kept for ApplyWorldTheme.
        private readonly List<MeshRenderer> _slabRenderers = new();

        private void Start()
        {
            var pm = PathManager.Instance;
            if (pm == null || pm.Grid == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[MapRenderer] PathManager not ready");
#endif
                return;
            }

            // Resolve current theme and world from active LevelRunner if available
            var lr = LevelRunner.Instance;
            int world = 1;
            if (lr?.CurrentLevel != null)
            {
                _theme = lr.CurrentLevel.LevelTheme;
                world = lr.CurrentLevel.World;
            }

            var grid = pm.Grid;

            // Bootstrap WaterLavaAnimController singleton before slab loop so
            // registration calls (RegisterWater/RegisterLava) find a live instance.
            if (WaterLavaAnimController.Instance == null)
            {
                var animGo = new GameObject("WaterLavaAnimController");
                animGo.transform.SetParent(transform, false);
                animGo.AddComponent<WaterLavaAnimController>();
            }

            for (int r = 0; r < grid.Height; r++)
            {
                for (int c = 0; c < grid.Width; c++)
                {
                    char ch = grid.At(c, r);
                    if (ch == GridCoords.VOID) continue;

                    Vector3 pos = GridCoords.CellToWorld(c, r, grid.Width, grid.Height, grid.CellSize);
                    pos.y = -0.05f;

                    var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    slab.name = $"Cell_{c}_{r}";
                    slab.transform.SetParent(transform, false);
                    slab.transform.position = pos;
                    float s = grid.CellSize * 0.95f;
                    slab.transform.localScale = new Vector3(s, 0.1f, s);

                    var col = slab.GetComponent<Collider>();
                    if (col != null) Destroy(col);

                    var mr = slab.GetComponent<MeshRenderer>();
                    // Bridges sit on water/lava — use the underlying liquid material on the
                    // slab so the animated stream is visible at the edges of the plank tile.
                    char surfaceCh = ch switch
                    {
                        GridCoords.BRIDGE_WATER => GridCoords.WATER,
                        GridCoords.BRIDGE_LAVA  => GridCoords.LAVA,
                        _                       => ch,
                    };
                    mr.sharedMaterial = GetMat(surfaceCh, _theme);
                    _slabRenderers.Add(mr);

                    // Register animated tiles with WaterLavaAnimController. Bridges register too
                    // so the stream beneath them animates in sync (their plank quad covers ~70%).
                    if (ch == GridCoords.WATER || ch == GridCoords.BRIDGE_WATER)
                        WaterLavaAnimController.Instance?.RegisterWater(mr);
                    else if (ch == GridCoords.LAVA || ch == GridCoords.BRIDGE_LAVA)
                        WaterLavaAnimController.Instance?.RegisterLava(mr);
                }
            }

            ApplyWorldTheme(world);

            // Overlay topology-aware path tiles on top of the slab floor.
            var ptc = GetComponentInChildren<PathTilesController>(includeInactive: false);
            if (ptc == null)
            {
                var ptGo = new GameObject("PathTiles");
                ptGo.transform.SetParent(transform, false);
                ptc = ptGo.AddComponent<PathTilesController>();
            }
            ptc.SpawnPathTiles(grid, _theme);

#if UNITY_EDITOR
            Debug.Log($"[MapRenderer] Spawned slabs for {grid.Width}x{grid.Height} grid, theme={_theme}, world={world}");
#endif
        }

        // Applies a world-based colour tint to all floor slabs via MaterialPropertyBlock (zero-alloc per-instance override).
        // W1-2: grass green, W3-4: sand desert, W5-6: snow ice, W7-8: volcanic rock, W9+: void dark.
        public void ApplyWorldTheme(int worldId)
        {
            var tint = WorldThemeTint(worldId);
            var mpb = new MaterialPropertyBlock();
            foreach (var mr in _slabRenderers)
            {
                mr.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", tint);
                mr.SetPropertyBlock(mpb);
            }
        }

        private static Color WorldThemeTint(int worldId) => worldId switch
        {
            1 or 2 => new Color(0.40f, 0.80f, 0.30f),  // grass green
            3 or 4 => new Color(0.90f, 0.80f, 0.50f),  // sand desert
            5 or 6 => new Color(0.85f, 0.92f, 1.00f),  // snow ice
            7 or 8 => new Color(0.70f, 0.40f, 0.20f),  // volcanic rock
            _      => new Color(0.20f, 0.15f, 0.40f),  // void dark (W9+)
        };

        private static Material GetMat(char ch, LevelTheme theme)
        {
            var key = (ch, theme);
            if (!_matCache.TryGetValue(key, out var m))
            {
                m = BuildMaterial(ch, theme);
                _matCache[key] = m;
            }
            return m;
        }

        // Returns animated toon material for special cells, plain color for rest.
        private static Material BuildMaterial(char ch, LevelTheme theme)
        {
            var config = LevelThemeMaterialConfig.Get();

            if (ch == GridCoords.WATER)
            {
                var waterMat = config?.GetWaterMat(theme);
                if (waterMat != null)
                {
                    var cloned = new Material(waterMat);
                    cloned.enableInstancing = true;
                    return cloned;
                }
            }
            else if (ch == GridCoords.LAVA)
            {
                var lavaMat = config?.GetLavaMat(theme);
                if (lavaMat != null)
                {
                    var cloned = new Material(lavaMat);
                    cloned.enableInstancing = true;
                    return cloned;
                }
            }

            // Try to load theme-specific textured material for ground cells (GRASS, GRASS_BLOCK, TREE, BUSH, DECOR, ROCK)
            if (ch == GridCoords.GRASS || ch == GridCoords.GRASS_BLOCK || ch == GridCoords.TREE || ch == GridCoords.BUSH || ch == GridCoords.DECOR || ch == GridCoords.ROCK)
            {
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
                var groundMat = Resources.Load<Material>("Materials/ground_" + themeName);
                if (groundMat != null)
                {
                    var cloned = new Material(groundMat);
                    cloned.enableInstancing = true;
                    return cloned;
                }
            }

            // Snow theme — static cells get snow material
            if (theme == LevelTheme.Espace || theme == LevelTheme.Medieval)
            {
                if (ch == GridCoords.GRASS || ch == GridCoords.GRASS_BLOCK)
                {
                    var snowMat = Resources.Load<Material>("Materials/Toon_Snow");
                    if (snowMat != null)
                    {
                        var cloned = new Material(snowMat);
                        cloned.enableInstancing = true;
                        return cloned;
                    }
                }
            }

            // Default: load pre-built Toon_Default material, apply color tint + procedural texture
            var baseMat = Resources.Load<Material>("Materials/Toon_Default");
            var mat = baseMat != null
                ? new Material(baseMat)
                : new Material(ShaderUtil.GetToonShader());

            // Apply procedural texture for ground cells so _BaseColor alone isn't flat
            var tex = GetProceduralTex(ch);
            if (tex != null)
            {
                // URP Lit uses _BaseMap; Standard shader uses _MainTex — try both.
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
                else if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", tex);
                // Keep a light color tint; white = no tint so texture shows true.
                mat.SetColor("_BaseColor", Color.white * 0.95f);
            }
            else
            {
                mat.SetColor("_BaseColor", CellColor(ch));
            }

            mat.enableInstancing = true;
            return mat;
        }

        // Returns a lazily-created 64x64 procedural texture for the given cell type.
        // Grass: green noise. Path: sandy/dirt noise. Dirt (decor/rock): grey-brown noise.
        private static Texture2D? GetProceduralTex(char ch)
        {
            if (ch == GridCoords.GRASS || ch == GridCoords.GRASS_BLOCK || ch == GridCoords.TREE || ch == GridCoords.BUSH)
            {
                _texGrass ??= GenerateNoiseTexture(64, new Color(0.22f, 0.48f, 0.18f), 0.08f, 42);
                return _texGrass;
            }
            if (ch == GridCoords.PATH || ch == GridCoords.BRIDGE_WATER || ch == GridCoords.BRIDGE_LAVA)
            {
                _texPath ??= GenerateNoiseTexture(64, new Color(0.70f, 0.58f, 0.38f), 0.10f, 7);
                return _texPath;
            }
            if (ch == GridCoords.DECOR || ch == GridCoords.ROCK)
            {
                _texDirt ??= GenerateNoiseTexture(64, new Color(0.45f, 0.40f, 0.35f), 0.07f, 13);
                return _texDirt;
            }
            return null;
        }

        // Generates a square texture with per-pixel colour jitter around baseColor.
        // jitter=0.08 means ±8% brightness variation per channel.
        private static Texture2D GenerateNoiseTexture(int size, Color baseColor, float jitter, int seed)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            var rng = new System.Random(seed);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                float n = (float)(rng.NextDouble() * 2.0 - 1.0) * jitter;
                pixels[i] = new Color(
                    Mathf.Clamp01(baseColor.r + n),
                    Mathf.Clamp01(baseColor.g + n),
                    Mathf.Clamp01(baseColor.b + n),
                    1f);
            }
            tex.SetPixels(pixels);
            tex.Apply(updateMipmaps: true);
            return tex;
        }

        private static Color CellColor(char ch) => ch switch
        {
            GridCoords.GRASS        => new Color(0.30f, 0.55f, 0.25f),
            GridCoords.GRASS_BLOCK  => new Color(0.28f, 0.50f, 0.22f),
            GridCoords.PATH         => new Color(0.75f, 0.65f, 0.45f),
            GridCoords.PORTAL       => new Color(0.90f, 0.30f, 0.30f),
            GridCoords.CASTLE       => new Color(0.30f, 0.45f, 0.90f),
            GridCoords.WATER        => new Color(0.20f, 0.40f, 0.75f),
            GridCoords.LAVA         => new Color(0.95f, 0.35f, 0.10f),
            GridCoords.BRIDGE_WATER => new Color(0.55f, 0.40f, 0.25f),
            GridCoords.BRIDGE_LAVA  => new Color(0.45f, 0.30f, 0.20f),
            GridCoords.DECOR        => new Color(0.40f, 0.40f, 0.40f),
            GridCoords.TREE         => new Color(0.20f, 0.45f, 0.15f),
            GridCoords.ROCK         => new Color(0.55f, 0.50f, 0.45f),
            GridCoords.BUSH         => new Color(0.25f, 0.50f, 0.20f),
            GridCoords.TREASURE     => new Color(1.00f, 0.80f, 0.10f),   // doré D1-01 §3.6
            _                       => new Color(0.20f, 0.20f, 0.20f),
        };
    }
}
