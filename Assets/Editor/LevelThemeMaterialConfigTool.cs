// Editor-only: creates LevelThemeMaterialConfig.asset in Assets/Resources/
// Run via menu CrowdDefense > Build > Create LevelThemeMaterialConfig
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    public static class LevelThemeMaterialConfigTool
    {
        private const string AssetPath = "Assets/Resources/LevelThemeMaterialConfig.asset";

        [InitializeOnLoadMethod]
        static void AutoCreate() => EnsureExists(silent: true);

        [MenuItem("CrowdDefense/Build/Create LevelThemeMaterialConfig")]
        public static void CreateMenu() => EnsureExists(silent: false);

        private static void EnsureExists(bool silent)
        {
            var existing = AssetDatabase.LoadAssetAtPath<LevelThemeMaterialConfig>(AssetPath);
            if (existing != null) return;

            var so = ScriptableObject.CreateInstance<LevelThemeMaterialConfig>();
            AssetDatabase.CreateAsset(so, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!silent)
                Debug.Log("[LevelThemeMaterialConfigTool] Created " + AssetPath);
        }
    }
}
#endif
