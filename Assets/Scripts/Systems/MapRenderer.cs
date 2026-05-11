#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Systems
{
    [DefaultExecutionOrder(50)]
    public class MapRenderer : MonoBehaviour
    {
        private static readonly Dictionary<char, Material> _matCache = new();

        private void Start()
        {
            var pm = PathManager.Instance;
            if (pm == null || pm.Grid == null)
            {
                Debug.LogError("[MapRenderer] PathManager not ready");
                return;
            }

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

                    slab.GetComponent<MeshRenderer>().sharedMaterial = GetMat(ch);
                }
            }

#if UNITY_EDITOR
            Debug.Log($"[MapRenderer] Spawned slabs for {grid.Width}x{grid.Height} grid");
#endif
        }

        private static Material GetMat(char ch)
        {
            if (!_matCache.TryGetValue(ch, out var m))
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(shader!);
                m.color = CellColor(ch);
                _matCache[ch] = m;
            }
            return m;
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
            _                       => new Color(0.20f, 0.20f, 0.20f),
        };
    }
}
