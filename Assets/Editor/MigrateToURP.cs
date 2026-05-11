// Editor-only: scans all .mat assets and rewires Built-in Standard shaders to URP Lit.
// Run via menu Tools/CrowdDefense/Migrate Materials to URP
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CrowdDefense.Editor
{
    public static class MigrateToURP
    {
        // Built-in shader names that map 1-to-1 with URP/Lit
        static readonly string[] BuiltInNames =
        {
            "Standard",
            "Standard (Specular setup)",
            "Autodesk Interactive",
            "GLTF/PbrMetallicRoughness",
            "GLTF/PbrSpecularGlossiness",
        };

        const string UrpLitName = "Universal Render Pipeline/Lit";

        [MenuItem("Tools/CrowdDefense/Migrate Materials to URP")]
        public static void MigrateAll()
        {
            var urpLit = Shader.Find(UrpLitName);
            if (urpLit == null)
            {
                Debug.LogError($"[MigrateToURP] Shader '{UrpLitName}' not found — is URP installed?");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Material");
            int migrated = 0;
            int skipped  = 0;
            var report   = new List<string>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                string shaderName = mat.shader != null ? mat.shader.name : "(null)";

                if (!IsBuiltIn(shaderName))
                {
                    skipped++;
                    continue;
                }

                mat.shader = urpLit;
                // Preserve albedo color if present
                if (mat.HasProperty("_Color") && !mat.HasProperty("_BaseColor"))
                {
                    Color albedo = mat.GetColor("_Color");
                    mat.SetColor("_BaseColor", albedo);
                }

                EditorUtility.SetDirty(mat);
                report.Add($"  {path}  [{shaderName} → {UrpLitName}]");
                migrated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (migrated == 0)
                Debug.Log($"[MigrateToURP] No Built-in Standard materials found. {skipped} materials already on custom/URP shaders — nothing to do.");
            else
            {
                Debug.Log($"[MigrateToURP] Migrated {migrated} material(s) to '{UrpLitName}':\n" +
                          string.Join("\n", report));
            }
        }

        static bool IsBuiltIn(string name)
        {
            foreach (string n in BuiltInNames)
                if (name == n) return true;
            return false;
        }
    }
}
#endif
