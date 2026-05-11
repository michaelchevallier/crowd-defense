#nullable enable
using System.Collections.Generic;
using CrowdDefense.Common;
using CrowdDefense.Data;
using UnityEngine;

namespace CrowdDefense.Systems
{
    [DefaultExecutionOrder(50)]
    public class MapRenderer : MonoBehaviour
    {
        // Per-char cache keyed by (char, theme) so animated mats are theme-specific.
        private static readonly Dictionary<(char, LevelTheme), Material> _matCache = new();

        // Theme resolved from LevelRunner at Start — default Plaine if not available.
        private LevelTheme _theme = LevelTheme.Plaine;

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

            // Resolve current theme from active LevelRunner if available
            var lr = LevelRunner.Instance;
            if (lr?.CurrentLevel != null)
                _theme = lr.CurrentLevel.LevelTheme;

            var grid = pm.Grid;
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

                    slab.GetComponent<MeshRenderer>().sharedMaterial = GetMat(ch, _theme);
                }
            }

#if UNITY_EDITOR
            Debug.Log($"[MapRenderer] Spawned slabs for {grid.Width}x{grid.Height} grid, theme={_theme}");
#endif
        }

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
                var mat = config?.GetWaterMat(theme);
                if (mat != null) return mat;
            }
            else if (ch == GridCoords.LAVA)
            {
                var mat = config?.GetLavaMat(theme);
                if (mat != null) return mat;
            }

            // Snow theme — static cells get snow material
            if (theme == LevelTheme.Espace || theme == LevelTheme.Medieval)
            {
                if (ch == GridCoords.GRASS || ch == GridCoords.GRASS_BLOCK)
                {
                    var snowMat = Resources.Load<Material>("Materials/Toon_Snow");
                    if (snowMat != null) return snowMat;
                }
            }

            // Default: plain-color material using Toon_Lit shader
            var fallback = new Material(ShaderUtil.GetToonShader());
            fallback.SetColor("_BaseColor", CellColor(ch));
            return fallback;
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
