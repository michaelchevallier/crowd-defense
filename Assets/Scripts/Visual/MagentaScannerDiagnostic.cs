#nullable enable
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Visual
{
    // Editor-only: scan all renderers on level start and log any with magenta-rendering materials
    [DefaultExecutionOrder(200)]
    public class MagentaScannerDiagnostic : MonoBehaviour
    {
        private void OnEnable() => LevelEvents.OnLevelStart += HandleLevelStart;
        private void OnDisable() => LevelEvents.OnLevelStart -= HandleLevelStart;

        private void HandleLevelStart(LevelData level, Bounds _)
        {
            Invoke(nameof(ScanRenderers), 0.5f);
        }

        private void ScanRenderers()
        {
            int magentaCount = 0;
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                if (r == null || !r.enabled) continue;
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null)
                    {
                        Debug.Log($"[Magenta?] Null material on '{r.name}' (parent: {r.transform.parent?.name})");
                        continue;
                    }
                    string shaderName = mat.shader?.name ?? "null";
                    if (shaderName.Contains("InternalError") || shaderName == "Standard" || shaderName.Contains("Hidden/"))
                    {
                        magentaCount++;
                        var path = GetGameObjectPath(r.transform);
                        Debug.LogWarning($"[MagentaScanner] Magenta-risk renderer: '{path}' shader='{shaderName}' mat='{mat.name}'");
                    }
                    else if (shaderName.Contains("Universal Render Pipeline/Unlit"))
                    {
                        if (!mat.HasProperty("_BaseColor")) continue;
                        var c = mat.GetColor("_BaseColor");
                        if (c == Color.magenta || (c.r == 1f && c.g == 0f && c.b == 1f))
                        {
                            magentaCount++;
                            var path = GetGameObjectPath(r.transform);
                            Debug.LogWarning($"[MagentaScanner] URP/Unlit BaseColor=magenta: '{path}' mat='{mat.name}'");
                        }
                    }
                }
            }
            Debug.Log($"[MagentaScanner] Scan complete: {magentaCount} magenta-risk renderers found of {renderers.Length} total");
        }

        private static string GetGameObjectPath(Transform t)
        {
            var path = t.name;
            while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }
    }
}
