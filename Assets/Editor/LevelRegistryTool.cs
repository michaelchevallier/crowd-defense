#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    public static class LevelRegistryTool
    {
        private const string LevelsFolder = "Assets/ScriptableObjects/Levels";
        private const string RegistryPath = "Assets/Resources/LevelRegistry.asset";

        [MenuItem("Tools/CrowdDefense/Build LevelRegistry")]
        public static void BuildLevelRegistry()
        {
            Directory.CreateDirectory("Assets/Resources");

            LevelRegistry? registry = AssetDatabase.LoadAssetAtPath<LevelRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<LevelRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            var so = new SerializedObject(registry);
            var levelsProp = so.FindProperty("levels");
            levelsProp.ClearArray();

            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { LevelsFolder });
            int count = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData? ld = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (ld == null) continue;
                levelsProp.InsertArrayElementAtIndex(count);
                levelsProp.GetArrayElementAtIndex(count).objectReferenceValue = ld;
                count++;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LevelRegistryTool] LevelRegistry built: {count} levels at {RegistryPath}");
        }
    }
}
