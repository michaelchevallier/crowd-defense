#nullable enable
using System.Collections.Generic;
using CrowdDefense.Common;
using CrowdDefense.Data;
using UnityEngine;

namespace CrowdDefense.Systems
{
    // Ports V4 SceneDecor.js placeNatureProp logic to Unity.
    // Subscribes LevelEvents.OnLevelStart, loops grid cells D/T/B/R and places
    // theme-appropriate placeholder props with seeded PRNG for replay determinism.
    // GPU instancing: MaterialPropertyBlock per prop for tint variation without
    // extra Material instances.
    [DefaultExecutionOrder(60)]
    public class SceneDecorController : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Theme palette — mirrors V4 THEME_PALETTE (placeholder prims, no GLTF)
        // -------------------------------------------------------------------------

        private enum PropShape { Cylinder, Cube, Sphere, Capsule, Tree }

        private readonly struct PropDef
        {
            public readonly PropShape Shape;
            public readonly Color     BaseColor;
            public readonly Vector3   Scale;     // (rx, ry, rz) relative to cell size
            public PropDef(PropShape s, Color c, Vector3 sc) { Shape = s; BaseColor = c; Scale = sc; }
        }

        // Per theme: [big, medium, small] prop pools for D cells; [tiny] pool for T/B/R cells
        private readonly struct ThemePalette
        {
            public readonly PropDef[] Big;     // D cell: 1 big
            public readonly PropDef[] Medium;  // D cell: occasional medium mix
            public readonly PropDef[] Small;   // T/B/R cell: 1-2 small
            public ThemePalette(PropDef[] big, PropDef[] med, PropDef[] sm)
            { Big = big; Medium = med; Small = sm; }
        }

        // Helper color shorthand
        private static Color C(float r, float g, float b) => new(r, g, b);

        // All 10 LevelTheme palettes — positional constructor args (big, med, sm)
        private static readonly Dictionary<LevelTheme, ThemePalette> _palettes = new()
        {
            [LevelTheme.Plaine] = new ThemePalette(
                new[] { new PropDef(PropShape.Tree,     C(0.25f,0.55f,0.12f), new Vector3(1.00f,1.00f,1.00f)),
                        new PropDef(PropShape.Tree,     C(0.20f,0.60f,0.15f), new Vector3(1.00f,1.00f,1.00f)),
                        new PropDef(PropShape.Tree,     C(0.28f,0.50f,0.10f), new Vector3(1.00f,1.00f,1.00f)) },
                new[] { new PropDef(PropShape.Sphere,   C(0.40f,0.72f,0.20f), new Vector3(0.45f,0.30f,0.45f)),
                        new PropDef(PropShape.Sphere,   C(0.55f,0.78f,0.25f), new Vector3(0.40f,0.25f,0.40f)) },
                new[] { new PropDef(PropShape.Sphere,   C(0.80f,0.30f,0.55f), new Vector3(0.18f,0.18f,0.18f)),
                        new PropDef(PropShape.Sphere,   C(0.90f,0.70f,0.20f), new Vector3(0.15f,0.15f,0.15f)) }),

            [LevelTheme.Foret] = new ThemePalette(
                new[] { new PropDef(PropShape.Tree,     C(0.20f,0.50f,0.20f), new Vector3(1.00f,1.00f,1.00f)),
                        new PropDef(PropShape.Tree,     C(0.15f,0.45f,0.15f), new Vector3(1.00f,1.00f,1.00f)),
                        new PropDef(PropShape.Tree,     C(0.22f,0.48f,0.18f), new Vector3(1.00f,1.00f,1.00f)) },
                new[] { new PropDef(PropShape.Sphere,   C(0.18f,0.48f,0.10f), new Vector3(0.40f,0.35f,0.40f)),
                        new PropDef(PropShape.Capsule,  C(0.20f,0.45f,0.12f), new Vector3(0.20f,0.55f,0.20f)) },
                new[] { new PropDef(PropShape.Sphere,   C(0.75f,0.25f,0.05f), new Vector3(0.14f,0.14f,0.14f)),
                        new PropDef(PropShape.Capsule,  C(0.25f,0.55f,0.08f), new Vector3(0.12f,0.30f,0.12f)) }),

            [LevelTheme.Desert] = new ThemePalette(
                new[] { new PropDef(PropShape.Cube,     C(0.75f,0.65f,0.45f), new Vector3(0.55f,0.80f,0.55f)),
                        new PropDef(PropShape.Cube,     C(0.70f,0.58f,0.38f), new Vector3(0.65f,0.65f,0.65f)),
                        new PropDef(PropShape.Sphere,   C(0.80f,0.60f,0.30f), new Vector3(0.60f,0.50f,0.60f)) },
                new[] { new PropDef(PropShape.Cube,     C(0.72f,0.62f,0.42f), new Vector3(0.40f,0.55f,0.40f)),
                        new PropDef(PropShape.Sphere,   C(0.82f,0.72f,0.52f), new Vector3(0.35f,0.28f,0.35f)) },
                new[] { new PropDef(PropShape.Sphere,   C(0.88f,0.78f,0.58f), new Vector3(0.20f,0.16f,0.20f)),
                        new PropDef(PropShape.Cube,     C(0.78f,0.68f,0.48f), new Vector3(0.18f,0.14f,0.18f)) }),

            [LevelTheme.Volcan] = new ThemePalette(
                new[] { new PropDef(PropShape.Cube,     C(0.35f,0.20f,0.15f), new Vector3(0.55f,0.90f,0.55f)),
                        new PropDef(PropShape.Cube,     C(0.40f,0.22f,0.18f), new Vector3(0.65f,0.70f,0.65f)),
                        new PropDef(PropShape.Capsule,  C(0.30f,0.18f,0.12f), new Vector3(0.30f,1.10f,0.30f)) },
                new[] { new PropDef(PropShape.Sphere,   C(0.80f,0.22f,0.05f), new Vector3(0.38f,0.28f,0.38f)),
                        new PropDef(PropShape.Cube,     C(0.45f,0.25f,0.20f), new Vector3(0.32f,0.45f,0.32f)) },
                new[] { new PropDef(PropShape.Sphere,   C(1.00f,0.35f,0.05f), new Vector3(0.20f,0.15f,0.20f)),
                        new PropDef(PropShape.Sphere,   C(0.95f,0.18f,0.02f), new Vector3(0.16f,0.12f,0.16f)) }),

            [LevelTheme.Apocalypse] = new ThemePalette(
                new[] { new PropDef(PropShape.Cylinder, C(0.28f,0.24f,0.20f), new Vector3(0.18f,1.40f,0.18f)),
                        new PropDef(PropShape.Cube,     C(0.35f,0.30f,0.25f), new Vector3(0.60f,0.85f,0.25f)),
                        new PropDef(PropShape.Capsule,  C(0.22f,0.20f,0.18f), new Vector3(0.22f,1.20f,0.22f)) },
                new[] { new PropDef(PropShape.Cube,     C(0.50f,0.42f,0.30f), new Vector3(0.35f,0.55f,0.35f)),
                        new PropDef(PropShape.Sphere,   C(0.55f,0.35f,0.15f), new Vector3(0.30f,0.30f,0.30f)) },
                new[] { new PropDef(PropShape.Cube,     C(0.40f,0.35f,0.28f), new Vector3(0.18f,0.22f,0.18f)),
                        new PropDef(PropShape.Sphere,   C(0.30f,0.25f,0.20f), new Vector3(0.16f,0.16f,0.16f)) }),

            [LevelTheme.Espace] = new ThemePalette(
                new[] { new PropDef(PropShape.Sphere,   C(0.55f,0.20f,0.80f), new Vector3(0.70f,0.70f,0.70f)),
                        new PropDef(PropShape.Sphere,   C(0.20f,0.45f,0.80f), new Vector3(0.60f,0.60f,0.60f)),
                        new PropDef(PropShape.Cylinder, C(0.40f,0.15f,0.70f), new Vector3(0.15f,1.50f,0.15f)) },
                new[] { new PropDef(PropShape.Capsule,  C(0.25f,0.80f,0.70f), new Vector3(0.20f,0.70f,0.20f)),
                        new PropDef(PropShape.Cube,     C(0.35f,0.35f,0.65f), new Vector3(0.30f,0.45f,0.30f)) },
                new[] { new PropDef(PropShape.Sphere,   C(0.70f,0.55f,0.95f), new Vector3(0.22f,0.18f,0.22f)),
                        new PropDef(PropShape.Cube,     C(0.30f,0.70f,0.80f), new Vector3(0.18f,0.20f,0.18f)) }),

            [LevelTheme.Submarin] = new ThemePalette(
                new[] { new PropDef(PropShape.Capsule,  C(0.15f,0.55f,0.72f), new Vector3(0.35f,1.20f,0.35f)),
                        new PropDef(PropShape.Sphere,   C(0.10f,0.45f,0.65f), new Vector3(0.65f,0.55f,0.65f)),
                        new PropDef(PropShape.Cylinder, C(0.20f,0.60f,0.70f), new Vector3(0.25f,1.10f,0.25f)),
                        new PropDef(PropShape.Sphere,   C(0.08f,0.35f,0.55f), new Vector3(0.50f,0.95f,0.50f)),
                        new PropDef(PropShape.Capsule,  C(0.12f,0.48f,0.68f), new Vector3(0.28f,1.35f,0.28f)) },
                new[] { new PropDef(PropShape.Sphere,   C(0.55f,0.25f,0.55f), new Vector3(0.35f,0.28f,0.35f)),
                        new PropDef(PropShape.Capsule,  C(0.18f,0.70f,0.60f), new Vector3(0.18f,0.60f,0.18f)),
                        new PropDef(PropShape.Cube,     C(0.25f,0.65f,0.75f), new Vector3(0.32f,0.40f,0.32f)),
                        new PropDef(PropShape.Cylinder, C(0.10f,0.62f,0.72f), new Vector3(0.15f,0.70f,0.15f)) },
                new[] { new PropDef(PropShape.Sphere,   C(0.90f,0.60f,0.20f), new Vector3(0.20f,0.16f,0.20f)),
                        new PropDef(PropShape.Sphere,   C(0.20f,0.80f,0.70f), new Vector3(0.16f,0.14f,0.16f)),
                        new PropDef(PropShape.Cube,     C(0.18f,0.55f,0.68f), new Vector3(0.14f,0.18f,0.14f)),
                        new PropDef(PropShape.Sphere,   C(0.22f,0.68f,0.78f), new Vector3(0.12f,0.12f,0.12f)) }),

            [LevelTheme.Medieval] = new ThemePalette(
                new[] { new PropDef(PropShape.Cube,     C(0.60f,0.55f,0.45f), new Vector3(0.40f,1.20f,0.40f)),
                        new PropDef(PropShape.Cylinder, C(0.55f,0.50f,0.40f), new Vector3(0.35f,1.40f,0.35f)),
                        new PropDef(PropShape.Cube,     C(0.65f,0.58f,0.48f), new Vector3(0.55f,0.90f,0.55f)) },
                new[] { new PropDef(PropShape.Cube,     C(0.70f,0.60f,0.40f), new Vector3(0.30f,0.50f,0.30f)),
                        new PropDef(PropShape.Cylinder, C(0.50f,0.45f,0.35f), new Vector3(0.22f,0.80f,0.22f)) },
                new[] { new PropDef(PropShape.Cube,     C(0.80f,0.70f,0.50f), new Vector3(0.20f,0.28f,0.20f)),
                        new PropDef(PropShape.Sphere,   C(0.65f,0.20f,0.20f), new Vector3(0.14f,0.18f,0.14f)) }),

            [LevelTheme.Cyberpunk] = new ThemePalette(
                new[] { new PropDef(PropShape.Cube,     C(0.10f,0.10f,0.15f), new Vector3(0.40f,1.60f,0.40f)),
                        new PropDef(PropShape.Capsule,  C(0.08f,0.55f,0.72f), new Vector3(0.18f,1.80f,0.18f)),
                        new PropDef(PropShape.Cube,     C(0.12f,0.08f,0.18f), new Vector3(0.55f,1.20f,0.55f)) },
                new[] { new PropDef(PropShape.Cube,     C(0.55f,0.05f,0.80f), new Vector3(0.28f,0.60f,0.28f)),
                        new PropDef(PropShape.Sphere,   C(0.05f,0.80f,0.70f), new Vector3(0.30f,0.24f,0.30f)) },
                new[] { new PropDef(PropShape.Cube,     C(0.80f,0.05f,0.50f), new Vector3(0.16f,0.30f,0.16f)),
                        new PropDef(PropShape.Sphere,   C(0.05f,0.70f,0.90f), new Vector3(0.14f,0.14f,0.14f)) }),

            [LevelTheme.Foire] = new ThemePalette(
                new[] { new PropDef(PropShape.Cylinder, C(0.90f,0.20f,0.20f), new Vector3(0.28f,1.00f,0.28f)),
                        new PropDef(PropShape.Sphere,   C(0.20f,0.60f,0.90f), new Vector3(0.55f,0.55f,0.55f)),
                        new PropDef(PropShape.Cylinder, C(0.20f,0.80f,0.25f), new Vector3(0.25f,0.90f,0.25f)),
                        new PropDef(PropShape.Cube,     C(0.95f,0.15f,0.60f), new Vector3(0.50f,0.75f,0.50f)),
                        new PropDef(PropShape.Sphere,   C(0.85f,0.55f,0.15f), new Vector3(0.60f,0.60f,0.60f)) },
                new[] { new PropDef(PropShape.Sphere,   C(0.95f,0.75f,0.15f), new Vector3(0.35f,0.28f,0.35f)),
                        new PropDef(PropShape.Capsule,  C(0.90f,0.30f,0.70f), new Vector3(0.18f,0.65f,0.18f)),
                        new PropDef(PropShape.Cube,     C(0.92f,0.20f,0.40f), new Vector3(0.40f,0.50f,0.40f)),
                        new PropDef(PropShape.Cylinder, C(0.95f,0.65f,0.15f), new Vector3(0.20f,0.55f,0.20f)) },
                new[] { new PropDef(PropShape.Sphere,   C(0.95f,0.40f,0.15f), new Vector3(0.18f,0.18f,0.18f)),
                        new PropDef(PropShape.Sphere,   C(0.80f,0.15f,0.80f), new Vector3(0.14f,0.14f,0.14f)),
                        new PropDef(PropShape.Cube,     C(0.88f,0.25f,0.55f), new Vector3(0.16f,0.20f,0.16f)),
                        new PropDef(PropShape.Sphere,   C(0.90f,0.70f,0.10f), new Vector3(0.12f,0.12f,0.12f)) }),
        };

        // -------------------------------------------------------------------------
        // Runtime state
        // -------------------------------------------------------------------------

        private readonly List<GameObject> _props = new();
        private Material? _instancedMat;

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        private void OnEnable()
        {
            LevelEvents.OnLevelStart += HandleLevelStart;
        }

        private void OnDisable()
        {
            LevelEvents.OnLevelStart -= HandleLevelStart;
        }

        private void OnDestroy()
        {
            ClearProps();
            if (_instancedMat != null)
                Destroy(_instancedMat);
        }

        // -------------------------------------------------------------------------
        // Event handler
        // -------------------------------------------------------------------------

        private void HandleLevelStart(Data.LevelData level, Bounds gridBounds)
        {
            ClearProps();
            var grid = PathManager.Instance?.Grid;
            if (grid == null) return;

            if (_instancedMat == null) _instancedMat = BuildBaseMaterial();
            PlaceAllProps(grid, level);

#if UNITY_EDITOR
            Debug.Log($"[SceneDecorController] {_props.Count} props placed, theme={level.LevelTheme}");
#endif
        }

        // -------------------------------------------------------------------------
        // Placement
        // -------------------------------------------------------------------------

        private void PlaceAllProps(GridData grid, Data.LevelData level)
        {
            var theme   = level.LevelTheme;
            var palette = _palettes.TryGetValue(theme, out var p) ? p : _palettes[LevelTheme.Plaine];

            // FNV-1a hash of level id for seeding, same algorithm as V4 hashLevelId
            uint seed = FnvHash(level.Id);

            for (int r = 0; r < grid.Height; r++)
            {
                for (int c = 0; c < grid.Width; c++)
                {
                    char ch = grid.At(c, r);
                    if (ch != GridCoords.DECOR &&
                        ch != GridCoords.TREE  &&
                        ch != GridCoords.BUSH  &&
                        ch != GridCoords.ROCK) continue;

                    // Seeded per-cell PRNG — deterministic replay
                    uint cellSeed = seed ^ (uint)(c * 73856093 ^ r * 19349663);
                    var  rng      = new System.Random((int)cellSeed);

                    Vector3 worldPos = GridCoords.CellToWorld(c, r, grid.Width, grid.Height, grid.CellSize);

                    if (ch == GridCoords.DECOR)
                    {
                        PlaceBigProp(palette, rng, worldPos, grid.CellSize, c, r);
                    }
                    else
                    {
                        // TREE / BUSH / ROCK → 1-2 small props
                        int count = 1 + (rng.Next(0, 3) == 0 ? 1 : 0); // 33% chance of 2nd
                        for (int i = 0; i < count; i++)
                            PlaceSmallProp(palette, rng, worldPos, grid.CellSize, i);
                    }
                }
            }
        }

        private void PlaceBigProp(ThemePalette pal, System.Random rng,
                                   Vector3 center, float cellSize, int c, int r)
        {
            // 70% chance big, 30% medium — matches V4 bigCount vs mediumCount ratio
            bool useMedium = rng.Next(0, 10) < 3 && pal.Medium.Length > 0;
            var  pool      = useMedium ? pal.Medium : pal.Big;
            var  def       = pool[rng.Next(0, pool.Length)];

            Vector3 scale  = new(
                def.Scale.x * cellSize,
                def.Scale.y * cellSize,
                def.Scale.z * cellSize
            );

            // Small jitter within cell bounds
            float jx = (float)(rng.NextDouble() - 0.5) * cellSize * 0.4f;
            float jz = (float)(rng.NextDouble() - 0.5) * cellSize * 0.4f;
            Vector3 pos = center + new Vector3(jx, 0f, jz);

            float rotY = (float)(rng.NextDouble() * 360.0);

            SpawnProp(def, pos, scale, rotY, $"Decor_D_{c}_{r}");
        }

        private void PlaceSmallProp(ThemePalette pal, System.Random rng,
                                     Vector3 center, float cellSize, int idx)
        {
            if (pal.Small.Length == 0) return;
            var def = pal.Small[rng.Next(0, pal.Small.Length)];

            Vector3 scale = new(
                def.Scale.x * cellSize,
                def.Scale.y * cellSize,
                def.Scale.z * cellSize
            );

            float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
            float r     = (float)(rng.NextDouble() * cellSize * 0.30f);
            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

            float rotY = (float)(rng.NextDouble() * 360.0);
            SpawnProp(def, pos, scale, rotY, $"Decor_S_{idx}");
        }

        // -------------------------------------------------------------------------
        // GameObject factory
        // -------------------------------------------------------------------------

        // Spawns a two-primitive tree: brown cylinder trunk + colored sphere foliage.
        private void SpawnTree(PropDef def, Vector3 pos, float rotY, string goName)
        {
            // Derive a per-tree scale variation from rotY to avoid extra rng parameter
            float rs = 0.8f + Mathf.Abs(Mathf.Sin(rotY * 0.1f)) * 0.4f;

            var root = new GameObject(goName);
            root.transform.SetParent(transform, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = goName + "_trunk";
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localScale = new Vector3(0.15f * rs, 0.5f * rs, 0.15f * rs);
            trunk.transform.localPosition = Vector3.zero;
            ApplyTreePart(trunk, new Color(0.45f, 0.30f, 0.15f));
            var tc = trunk.GetComponent<Collider>();
            if (tc != null) Destroy(tc);

            var foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.name = goName + "_foliage";
            foliage.transform.SetParent(root.transform, false);
            foliage.transform.localScale = new Vector3(0.55f * rs, 0.55f * rs, 0.55f * rs);
            foliage.transform.localPosition = new Vector3(0f, 0.7f * rs, 0f);
            ApplyTreePart(foliage, def.BaseColor);
            var fc = foliage.GetComponent<Collider>();
            if (fc != null) Destroy(fc);

            _props.Add(root);
        }

        private void ApplyTreePart(GameObject go, Color color)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null || _instancedMat == null) return;
            mr.sharedMaterial = _instancedMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mpb = new MaterialPropertyBlock();
            mr.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", color);
            mr.SetPropertyBlock(mpb);
        }

        private void SpawnProp(PropDef def, Vector3 pos, Vector3 scale, float rotY, string goName)
        {
            if (def.Shape == PropShape.Tree)
            {
                SpawnTree(def, pos, rotY, goName);
                return;
            }

            var go = def.Shape switch
            {
                PropShape.Cube    => GameObject.CreatePrimitive(PrimitiveType.Cube),
                PropShape.Sphere  => GameObject.CreatePrimitive(PrimitiveType.Sphere),
                PropShape.Capsule => GameObject.CreatePrimitive(PrimitiveType.Capsule),
                _                 => GameObject.CreatePrimitive(PrimitiveType.Cylinder),
            };

            go.name = goName;
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            // Remove collider — decor is non-interactive
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // GPU instancing: single shared Material + MaterialPropertyBlock per-instance for tint
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && _instancedMat != null)
            {
                mr.sharedMaterial = _instancedMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                var mpb = new MaterialPropertyBlock();
                mr.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", def.BaseColor);
                mr.SetPropertyBlock(mpb);
            }

            _props.Add(go);
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private void ClearProps()
        {
            for (int i = _props.Count - 1; i >= 0; i--)
            {
                if (_props[i] != null) Destroy(_props[i]);
            }
            _props.Clear();
        }

        private static Material BuildBaseMaterial()
        {
            var shader = ShaderUtil.GetToonShader();
            var mat    = new Material(shader) { name = "SceneDecor_Instanced" };
            mat.enableInstancing = true;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            return mat;
        }

        // FNV-1a 32-bit hash — matches V4 hashLevelId algorithm
        private static uint FnvHash(string s)
        {
            uint h = 2166136261u;
            foreach (char ch in s)
            {
                h ^= (byte)ch;
                h *= 16777619u;
            }
            return h;
        }
    }
}
