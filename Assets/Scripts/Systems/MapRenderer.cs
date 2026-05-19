#nullable enable
using System;
using System.Collections;
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
        public static MapRenderer? Instance { get; private set; }

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

        // Path-like cell visuals (slabs + PathTilesController PT_c_r children) grouped by cell,
        // used by RevealFromSpawn to stagger tile activation outward from the portal.
        private readonly Dictionary<Vector2Int, List<GameObject>> _pathCellVisuals = new();
        private Coroutine? _revealRoutine;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

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

                    // Track path visuals for reveal animation (md-pathtiles-reveal-anim).
                    if (IsPathLike(ch))
                        TrackPathVisual(new Vector2Int(c, r), slab);

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

            CollectPathTilesControllerChildren(ptc);

#if UNITY_EDITOR
            Debug.Log($"[MapRenderer] Spawned slabs for {grid.Width}x{grid.Height} grid, theme={_theme}, world={world}");
#endif
        }

        // Applies a world-based colour tint to all floor slabs via MaterialPropertyBlock (zero-alloc per-instance override).
        // W1-2: grass green, W3-4: sand desert, W5-6: snow ice, W7-8: volcanic rock, W9+: void dark.
        // Blends tint with existing material color to preserve visibility if base material is already colored.
        public void ApplyWorldTheme(int worldId)
        {
            var tint = WorldThemeTint(worldId);
            var mpb = new MaterialPropertyBlock();
            foreach (var mr in _slabRenderers)
            {
                mr.GetPropertyBlock(mpb);
                // Only blend if material supports _BaseColor (animated tiles like Toon_Water/Toon_Lava use _Tint instead).
                var mat = mr.sharedMaterial;
                if (mat != null && mat.HasProperty("_BaseColor"))
                {
                    Color baseColor = mat.GetColor("_BaseColor");
                    Color blended = new Color(
                        baseColor.r * tint.r,
                        baseColor.g * tint.g,
                        baseColor.b * tint.b,
                        baseColor.a
                    );
                    mpb.SetColor("_BaseColor", blended);
                    mr.SetPropertyBlock(mpb);
                }
            }
        }

        // Hides every path-like cell visual (slab + PathTilesController tile) then
        // re-activates them grouped by Manhattan distance from spawn, 60ms per step,
        // producing a radial reveal pulse emanating from the portal.
        public void RevealFromSpawn(Vector2Int spawn)
        {
            if (!isActiveAndEnabled) return;
            if (_revealRoutine != null) StopCoroutine(_revealRoutine);

            foreach (var kv in _pathCellVisuals)
                foreach (var go in kv.Value)
                    if (go != null) go.SetActive(false);

            _revealRoutine = StartCoroutine(RevealRoutine(spawn));
        }

        private IEnumerator RevealRoutine(Vector2Int spawn)
        {
            const float StepDelay = 0.06f;

            var cells = new List<Vector2Int>(_pathCellVisuals.Keys);
            cells.Sort((a, b) => Manhattan(a, spawn).CompareTo(Manhattan(b, spawn)));

            int prevDist = -1;
            int i = 0;
            while (i < cells.Count)
            {
                int d = Manhattan(cells[i], spawn);
                int wait = prevDist < 0 ? d : d - prevDist;
                if (wait > 0) yield return new WaitForSeconds(StepDelay * wait);

                while (i < cells.Count && Manhattan(cells[i], spawn) == d)
                {
                    if (_pathCellVisuals.TryGetValue(cells[i], out var list))
                        foreach (var go in list)
                            if (go != null) go.SetActive(true);
                    i++;
                }
                prevDist = d;
            }

            _revealRoutine = null;
        }

        private void TrackPathVisual(Vector2Int cell, GameObject go)
        {
            if (!_pathCellVisuals.TryGetValue(cell, out var list))
            {
                list = new List<GameObject>();
                _pathCellVisuals[cell] = list;
            }
            list.Add(go);
        }

        // PathTilesController spawns children named "PT_{c}_{r}" — pair them with their cell so
        // they reveal in lockstep with the underlying slab.
        private void CollectPathTilesControllerChildren(PathTilesController ptc)
        {
            foreach (Transform child in ptc.transform)
            {
                var name = child.name;
                if (name.Length < 4 || name[0] != 'P' || name[1] != 'T' || name[2] != '_') continue;
                int underscore = name.IndexOf('_', 3);
                if (underscore <= 3) continue;
                if (!int.TryParse(name.AsSpan(3, underscore - 3), out int c)) continue;
                if (!int.TryParse(name.AsSpan(underscore + 1), out int r)) continue;
                TrackPathVisual(new Vector2Int(c, r), child.gameObject);
            }
        }

        private static bool IsPathLike(char ch) =>
            ch == GridCoords.PATH || ch == GridCoords.PORTAL || ch == GridCoords.CASTLE ||
            ch == GridCoords.BRIDGE_WATER || ch == GridCoords.BRIDGE_LAVA;

        private static int Manhattan(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

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
                // Force URP/Unlit on ALL slab materials so tiles are visible regardless of
                // lighting setup. ToonCelShading needs directional light which may be absent
                // or point the wrong way for flat floor tiles (nDotL ≈ 0 → pure shadow band).
                // Skip water/lava animated materials — they have their own shader pipeline.
                bool isAnimated = (ch == GridCoords.WATER || ch == GridCoords.LAVA);
                if (isAnimated)
                {
                    // Toon_Water/Toon_Lava shaders use _Tint (not _BaseColor). If the shader
                    // failed to compile on URP 17.3.0 (ShadowCaster pass references undeclared
                    // _BaseMap/_BaseColor), _Tint will be absent. Fall back to URP/Unlit + solid
                    // color so the tile is always visible at level load time.
                    bool shaderBroken = !m.HasProperty("_Tint");
                    if (shaderBroken)
                    {
                        m.shader = ShaderUtil.GetUnlitShader();
                        m.SetColor("_BaseColor", CellColor(ch));
                    }
                }
                else
                {
                    var unlit = ShaderUtil.GetUnlitShader();
                    // Migrate _MainTex (ToonCelShading slot) → _BaseMap (URP Unlit slot).
                    Texture? tex = null;
                    if (m.HasProperty("_MainTex"))
                        tex = m.GetTexture("_MainTex");
                    m.shader = unlit;
                    if (tex != null && m.HasProperty("_BaseMap"))
                        m.SetTexture("_BaseMap", tex);
                    // V6 W1-W: ALWAYS set _BaseColor to CellColor(ch) — the .mat default may be dark navy,
                    // strict Color.clear check missed that case. Wave-1 grass texture should multiply with this.
                    if (m.HasProperty("_BaseColor"))
                        m.SetColor("_BaseColor", CellColor(ch));
                }
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
                // No procedural texture fallback — use cell color directly (ensures visibility)
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
