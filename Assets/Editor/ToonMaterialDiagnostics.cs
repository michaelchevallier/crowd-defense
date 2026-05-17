using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace CrowdDefense.Editor
{
    /// <summary>
    /// Diagnostics et réparation pour matériaux Toon cassés (pointeurs shader manquants).
    /// Fixe les matériaux orphelins en réexportant les assets et en restaurant les liens.
    /// </summary>
    public class ToonMaterialDiagnostics : EditorWindow
    {
        private Vector2 scrollPos;
        private List<(string path, string issue)> issues = new();

        [MenuItem("Tools/CrowdDefense/Toon Materials Diagnostics")]
        public static void Open()
        {
            var window = GetWindow<ToonMaterialDiagnostics>("Toon Materials");
            window.minSize = new Vector2(600, 400);
        }

        private void OnGUI()
        {
            GUILayout.Label("Toon Material Shader Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Scan Materials", GUILayout.Height(30)))
            {
                ScanMaterials();
            }

            if (GUILayout.Button("Fix All Broken Shaders", GUILayout.Height(30)))
            {
                FixBrokenShaders();
            }

            EditorGUILayout.Space();
            GUILayout.Label($"Issues Found: {issues.Count}", EditorStyles.helpBox);

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (var (path, issue) in issues)
            {
                GUILayout.Label($"• {Path.GetFileName(path)}", EditorStyles.label);
                GUILayout.Label($"  {issue}", EditorStyles.miniLabel);
            }
            GUILayout.EndScrollView();
        }

        private void ScanMaterials()
        {
            issues.Clear();
            var matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });

            foreach (var guid in matGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (mat == null)
                {
                    issues.Add((path, "Material load failed (null reference)"));
                    continue;
                }

                if (mat.shader == null)
                {
                    issues.Add((path, "Shader is NULL"));
                    continue;
                }

                if (mat.shader.name == "Hidden/InternalErrorShader" || mat.shader.name.Contains("Error"))
                {
                    issues.Add((path, $"Shader error: {mat.shader.name}"));
                    continue;
                }

                // Shader is OK
            }

            Debug.Log($"[ToonMaterialDiagnostics] Scan complete: {issues.Count} issue(s) in {matGuids.Length} material(s)");
            Repaint();
        }

        private void FixBrokenShaders()
        {
            int fixed_ = 0;
            var matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });

            foreach (var guid in matGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (mat?.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                {
                    // Force reimport to reload asset database
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    fixed_++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[ToonMaterialDiagnostics] Fixed {fixed_} material shader reference(s). Re-scanning...");
            ScanMaterials();
        }
    }
}
