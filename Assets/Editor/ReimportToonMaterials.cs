using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace CrowdDefense.Editor
{
    public static class ReimportToonMaterials
    {
        [MenuItem("Tools/CrowdDefense/Audit Materials — Find Broken Shaders")]
        public static void AuditMaterials()
        {
            var matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
            var broken = new List<(string path, string shaderName)>();

            foreach (var guid in matGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                
                if (mat == null || mat.shader == null)
                {
                    broken.Add((path, mat?.shader?.name ?? "NULL"));
                    continue;
                }

                // Check for InternalErrorShader (magenta indicator in URP)
                if (mat.shader.name == "Hidden/InternalErrorShader" || mat.shader.name.Contains("Error"))
                {
                    broken.Add((path, mat.shader.name));
                }
            }

            if (broken.Count == 0)
            {
                Debug.Log("[CrowdDefense.Materials] ✓ All materials have valid shaders.");
                return;
            }

            Debug.LogWarning($"[CrowdDefense.Materials] Found {broken.Count} broken material(s):");
            foreach (var (path, shaderName) in broken)
            {
                Debug.LogWarning($"  • {path} → shader '{shaderName}'");
            }
        }

        [MenuItem("Tools/CrowdDefense/Fix Materials — Reimport All")]
        public static void ReimportMaterials()
        {
            var matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
            int fixed_ = 0;
            var brokenBefore = new List<string>();

            foreach (var guid in matGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (mat == null) continue;

                // Log broken shaders before fix
                if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                {
                    brokenBefore.Add(path);
                }

                // Force reimport
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                fixed_++;
            }

            AssetDatabase.SaveAssets();

            if (brokenBefore.Count > 0)
            {
                Debug.Log($"[CrowdDefense.Materials] Reimported {fixed_} materials. Fixed {brokenBefore.Count} broken shader references:");
                foreach (var path in brokenBefore)
                {
                    Debug.Log($"  ✓ {path}");
                }
            }
            else
            {
                Debug.Log($"[CrowdDefense.Materials] Reimported {fixed_} materials. No broken shaders detected.");
            }
        }
    }
}
