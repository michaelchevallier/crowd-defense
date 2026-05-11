#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Systems;

namespace CrowdDefense.Visual
{
    // Persistent LineRenderer overlay showing all enemy paths at runtime.
    // Visible by default; toggle with key P.
    // Wired from LevelRunner.Start via AddComponent<PathPreviewRenderer>().
    public class PathPreviewRenderer : MonoBehaviour
    {
        private static readonly Color LineColor = new Color(1f, 0.9f, 0.3f, 0.4f);
        private const float LineWidth   = 0.14f;
        private const float YOffset     = 0.12f;

        private readonly List<LineRenderer> _lines = new();
        private bool _visible = true;

        private void Start()
        {
            var pm = PathManager.Instance;
            if (pm == null || pm.Paths.Count == 0) return;

            var mat = BuildMaterial();
            for (int pi = 0; pi < pm.Paths.Count; pi++)
                BuildLine(pm.Paths[pi], mat, pi);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
                SetVisible(!_visible);
        }

        private void SetVisible(bool on)
        {
            _visible = on;
            foreach (var lr in _lines)
                if (lr != null) lr.enabled = on;
        }

        private void BuildLine(IReadOnlyList<Vector3> waypoints, Material mat, int pathIdx)
        {
            if (waypoints.Count < 2) return;

            var go = new GameObject($"PathLine_{pathIdx}");
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace   = true;
            lr.startWidth      = LineWidth;
            lr.endWidth        = LineWidth;
            lr.numCornerVertices = 2;
            lr.numCapVertices    = 2;
            lr.material        = mat;
            lr.positionCount   = waypoints.Count;

            for (int i = 0; i < waypoints.Count; i++)
                lr.SetPosition(i, waypoints[i] + Vector3.up * YOffset);

            _lines.Add(lr);
        }

        private static Material BuildMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Unlit/Color")
                      ?? Shader.Find("Sprites/Default");

            var mat = new Material(shader);
            // URP Unlit surface type transparent
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.renderQueue = 3000;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.color = LineColor;
            return mat;
        }
    }
}
