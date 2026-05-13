#nullable enable
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    public static class LevelRegistryBuilder
    {
        [MenuItem("Tools/CrowdDefense/Rebuild LevelRegistry")]
        public static void Rebuild()
        {
            var registryPath = "Assets/Resources/LevelRegistry.asset";
            var registry = AssetDatabase.LoadAssetAtPath<LevelRegistry>(registryPath);
            if (registry == null)
            {
                Debug.LogError($"[LevelRegistryBuilder] LevelRegistry not found at {registryPath}");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/ScriptableObjects/Levels" });
            var levels = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(l => l != null)
                .OrderBy(l => l.name)
                .ToList();

            var so = new SerializedObject(registry);
            var arr = so.FindProperty("levels");
            arr!.ClearArray();
            for (int i = 0; i < levels.Count; i++)
            {
                arr.InsertArrayElementAtIndex(i);
                arr.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LevelRegistryBuilder] Registry rebuilt: {levels.Count} levels synchronized.");
        }
    }
}
