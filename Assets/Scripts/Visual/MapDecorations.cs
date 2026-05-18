#nullable enable
using System.Collections.Generic;
using CrowdDefense.Common;
using CrowdDefense.Systems;
using UnityEngine;

namespace CrowdDefense.Visual
{
    // Spawns 5-10 small decoration primitives around map edges based on world theme.
    // W1-2: green cylinders (trees), W3-4: grey cubes (rocks),
    // W5-6: white narrow spikes (ice), W7-8: red spheres (lava pools).
    [DefaultExecutionOrder(55)]
    public class MapDecorations : MonoBehaviour
    {
        private readonly List<GameObject> _props = new();

        private void Start()
        {
            var pm = PathManager.Instance;
            if (pm?.Grid == null) return;

            var lr = LevelRunner.Instance;
            int world = lr?.CurrentLevel != null ? lr.CurrentLevel.World : 1;

            SpawnProps(pm.Grid, world);

#if UNITY_EDITOR
            Debug.Log($"[MapDecorations] Spawned {_props.Count} props for world={world}");
#endif
        }

        private void SpawnProps(GridData grid, int world)
        {
            var rng = new System.Random(world * 137 + 42);
            int count = rng.Next(5, 11); // 5..10 inclusive

            float halfW = (grid.Width  - 1) / 2f * grid.CellSize;
            float halfH = (grid.Height - 1) / 2f * grid.CellSize;
            float margin = grid.CellSize * 0.6f;
            float outerPad = grid.CellSize * 1.8f;

            for (int i = 0; i < count; i++)
            {
                // Distribute around the 4 edges in edge-local coordinates
                int edge = i % 4;
                float t = (float)(rng.NextDouble() * 0.8 + 0.1); // 0.1..0.9

                Vector3 pos = edge switch
                {
                    0 => new Vector3(Mathf.Lerp(-halfW, halfW, t), 0f,  halfH + margin),
                    1 => new Vector3(Mathf.Lerp(-halfW, halfW, t), 0f, -halfH - margin),
                    2 => new Vector3(-halfW - margin, 0f, Mathf.Lerp(-halfH, halfH, t)),
                    _ => new Vector3( halfW + margin, 0f, Mathf.Lerp(-halfH, halfH, t)),
                };

                // Small random offset so they don't sit on a perfect line
                pos.x += (float)(rng.NextDouble() - 0.5) * outerPad;
                pos.z += (float)(rng.NextDouble() - 0.5) * outerPad;

                var go = CreateProp(world, rng);
                if (go == null) continue;
                go.transform.SetParent(transform, false);
                go.transform.position = pos;
                go.name = $"Decor_W{world}_{i}";

                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);

                _props.Add(go);
            }
        }

        private static GameObject? CreateProp(int world, System.Random rng)
        {
            float scale = (float)(rng.NextDouble() * 0.4 + 0.3); // 0.3..0.7

            switch (world)
            {
                case 1: case 2:
                {
                    // Tree: tall green cylinder
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.transform.localScale = new Vector3(scale * 0.4f, scale, scale * 0.4f);
                    ApplyColor(go, new Color(0.18f, 0.55f, 0.12f));
                    return go;
                }
                case 3: case 4:
                {
                    // Rock: grey cube, slightly squashed
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.localScale = new Vector3(scale, scale * 0.7f, scale);
                    ApplyColor(go, new Color(0.55f, 0.52f, 0.48f));
                    return go;
                }
                case 5: case 6:
                {
                    // Ice spike: tall thin cylinder, white-blue
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.transform.localScale = new Vector3(scale * 0.25f, scale * 1.4f, scale * 0.25f);
                    ApplyColor(go, new Color(0.82f, 0.93f, 1.00f));
                    return go;
                }
                case 7: case 8:
                {
                    // Lava pool: flat red sphere
                    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.localScale = new Vector3(scale, scale * 0.3f, scale);
                    ApplyColor(go, new Color(0.95f, 0.20f, 0.05f));
                    return go;
                }
                default:
                    return null;
            }
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            var mat = new Material(ShaderUtil.GetToonShader());
            // N29: HasProperty guard — if shader fallback chain ends at a custom toon
            // shader without `_BaseColor` (Toon/Water, Toon/Lava), avoid the log warning.
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            mat.enableInstancing = true;
            mr.sharedMaterial = mat;
        }
    }
}
