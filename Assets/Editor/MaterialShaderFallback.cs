using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace CrowdDefense.Editor
{
    /// <summary>
    /// Fallback strategy for materials with broken shaders.
    /// If a shader is null/error, replace with URP Lit (sensible default for Toon-based game).
    /// </summary>
    public static class MaterialShaderFallback
    {
        [MenuItem("Tools/CrowdDefense/Emergency — Apply Shader Fallbacks")]
        public static void ApplyFallbacks()
        {
            var matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
            var fallback = Shader.Find("Universal Render Pipeline/Lit");
            
            if (fallback == null)
            {
                Debug.LogError("[MaterialShaderFallback] URP Lit shader not found. Aborting.");
                return;
            }

            int fixed_ = 0;
            var changed = new List<string>();

            foreach (var guid in matGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (mat == null) continue;

                bool isBroken = (mat.shader == null) || 
                                (mat.shader.name == "Hidden/InternalErrorShader") ||
                                (mat.shader.name.Contains("Error"));

                if (isBroken)
                {
                    mat.shader = fallback;
                    EditorUtility.SetDirty(mat);
                    changed.Add(path);
                    fixed_++;
                }
            }

            if (fixed_ > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[MaterialShaderFallback] Applied fallback to {fixed_} material(s):");
                foreach (var path in changed)
                {
                    Debug.Log($"  ✓ {path} → Universal Render Pipeline/Lit");
                }
            }
            else
            {
                Debug.Log("[MaterialShaderFallback] No broken shaders found. No changes applied.");
            }
        }
    }
}
