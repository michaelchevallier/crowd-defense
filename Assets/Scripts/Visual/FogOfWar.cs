#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Systems;
using CrowdDefense.Entities;

namespace CrowdDefense.Visual
{
    // Fog-of-war overlay: dark semi-transparent quads above every cell,
    // revealed (removed) within radius 4 cells of each placed tower.
    [DefaultExecutionOrder(60)]
    public class FogOfWar : MonoBehaviour
    {
        private const int REVEAL_RADIUS = 4;
        private static readonly Color FogColor = new(0.05f, 0.05f, 0.10f, 0.72f);

        // col,row → fog quad renderer
        private readonly Dictionary<Vector2Int, GameObject> _fogCells = new();
        private readonly HashSet<Vector2Int> _revealed = new();

        private GridData? _grid;

        private void Start()
        {
            var pm = PathManager.Instance;
            if (pm?.Grid == null) return;
            _grid = pm.Grid;

            SpawnFogLayer();

            var pc = PlacementController.Instance;
            if (pc != null)
            {
                pc.OnTowerPlaced += OnTowerPlaced;
                // Reveal around towers already present (scene reload / debug)
                foreach (var t in pc.PlacedTowers)
                    RevealAround(t.transform.position);
            }
        }

        private void OnDestroy()
        {
            var pc = PlacementController.Instance;
            if (pc != null) pc.OnTowerPlaced -= OnTowerPlaced;
        }

        private void OnTowerPlaced(Tower tower) => RevealAround(tower.transform.position);

        private void SpawnFogLayer()
        {
            if (_grid == null) return;

            var mat = BuildFogMaterial();

            for (int r = 0; r < _grid.Height; r++)
            {
                for (int c = 0; c < _grid.Width; c++)
                {
                    if (_grid.At(c, r) == GridCoords.VOID) continue;

                    Vector3 pos = GridCoords.CellToWorld(c, r, _grid.Width, _grid.Height, _grid.CellSize);
                    pos.y = 0.12f; // just above floor slabs (y=-0.05 + 0.1 height = top at 0.0, we sit at 0.12)

                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.name = $"Fog_{c}_{r}";
                    quad.transform.SetParent(transform, false);
                    quad.transform.position = pos;
                    float s = _grid.CellSize * 0.98f;
                    quad.transform.localScale = new Vector3(s, s, 1f);
                    quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // lay flat on XZ

                    var col = quad.GetComponent<Collider>();
                    if (col != null) Destroy(col);

                    var mr = quad.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = mat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;

                    _fogCells[new Vector2Int(c, r)] = quad;
                }
            }
        }

        private void RevealAround(Vector3 worldPos)
        {
            if (_grid == null) return;

            var cell = GridCoords.WorldToCell(worldPos, _grid.Width, _grid.Height, _grid.CellSize);

            for (int dr = -REVEAL_RADIUS; dr <= REVEAL_RADIUS; dr++)
            {
                for (int dc = -REVEAL_RADIUS; dc <= REVEAL_RADIUS; dc++)
                {
                    if (dc * dc + dr * dr > REVEAL_RADIUS * REVEAL_RADIUS) continue; // circle mask

                    var key = new Vector2Int(cell.x + dc, cell.y + dr);
                    if (_revealed.Contains(key)) continue;
                    if (!_fogCells.TryGetValue(key, out var go)) continue;

                    _revealed.Add(key);
                    go.SetActive(false);
                }
            }
        }

        private static Material BuildFogMaterial()
        {
            // Use URP Unlit or built-in transparent depending on pipeline.
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Unlit/Color")
                      ?? Shader.Find("Standard");

            var mat = new Material(shader != null ? shader : Shader.Find("Standard")!);

            // Transparent blending
            mat.SetFloat("_Surface", 1f);           // URP: 1 = Transparent
            mat.SetFloat("_Blend", 0f);             // Alpha blend
            mat.SetFloat("_AlphaClip", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            // Works for both URP (_BaseColor) and Standard (_Color)
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", FogColor);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     FogColor);

            mat.enableInstancing = true;
            return mat;
        }
    }
}
