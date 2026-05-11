#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrowdDefense.Data;
using UnityEditor;
using UnityEngine;

namespace CrowdDefense.Editor
{
    public static class AssetRegistryTool
    {
        private const string RegistryPath = "Assets/Resources/AssetRegistry.asset";
        private const string ModelsRoot = "Assets/Models";

        [MenuItem("Tools/CrowdDefense/Build AssetRegistry")]
        public static void BuildAssetRegistry()
        {
            var registry = LoadOrCreateRegistry();

            var entries = new List<AssetRegistry.Entry>();

            // Scan Models/Towers
            ScanDirectory($"{ModelsRoot}/Towers", "Towers", entries);

            // Scan Models/Enemies (populated by VISUAL-02)
            ScanDirectory($"{ModelsRoot}/Enemies", "Enemies", entries);

            var serialized = new SerializedObject(registry);
            var arrayProp = serialized.FindProperty("entries");
            arrayProp.arraySize = entries.Count;

            for (int i = 0; i < entries.Count; i++)
            {
                var elem = arrayProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("Key").stringValue = entries[i].Key;
                var prefabProp = elem.FindPropertyRelative("Prefab");
                prefabProp.objectReferenceValue = entries[i].Prefab;
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AssetRegistryTool] Built registry with {entries.Count} entries.");
        }

        private static void ScanDirectory(string dir, string category, List<AssetRegistry.Entry> entries)
        {
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning($"[AssetRegistryTool] Directory not found: {dir} — skipping.");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:GameObject", new[] { dir });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".glb" && ext != ".gltf" && ext != ".fbx") continue;

                var assetKey = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                entries.Add(new AssetRegistry.Entry { Key = assetKey, Prefab = prefab });
                Debug.Log($"[AssetRegistryTool] Registered [{category}] {assetKey} → {path}");
            }
        }

        private static AssetRegistry LoadOrCreateRegistry()
        {
            // Ensure Resources folder exists
            if (!Directory.Exists("Assets/Resources"))
                Directory.CreateDirectory("Assets/Resources");

            var existing = AssetDatabase.LoadAssetAtPath<AssetRegistry>(RegistryPath);
            if (existing != null) return existing;

            var registry = ScriptableObject.CreateInstance<AssetRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AssetRegistryTool] Created new AssetRegistry at {RegistryPath}");
            return registry;
        }
    }
}
