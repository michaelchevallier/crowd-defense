using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

namespace CrowdDefense.Editor
{
    /// <summary>
    /// Scans all scenes for GameObjects using materials with broken/missing shaders.
    /// Logs magenta-rendered objects and broken references.
    /// </summary>
    public static class SceneShaderAudit
    {
        [MenuItem("Tools/CrowdDefense/Audit Scenes — Find Magenta Objects")]
        public static void AuditScenes()
        {
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            int totalIssues = 0;

            foreach (var guid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                
                foreach (var renderer in renderers)
                {
                    foreach (var mat in renderer.sharedMaterials)
                    {
                        if (mat == null) continue;
                        
                        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                        {
                            Debug.LogWarning(
                                $"[SceneShaderAudit] {scenePath} → " +
                                $"GameObject '{renderer.gameObject.name}' has material '{mat.name}' " +
                                $"with broken shader '{mat.shader?.name ?? "NULL"}'",
                                renderer.gameObject
                            );
                            totalIssues++;
                        }
                    }
                }
            }

            Debug.Log($"[SceneShaderAudit] Scan complete: found {totalIssues} broken shader(s) in scenes.");
        }
    }
}
