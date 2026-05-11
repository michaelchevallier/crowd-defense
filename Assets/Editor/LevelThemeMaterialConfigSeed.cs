#nullable enable
// Seeds LevelThemeMaterialConfig.asset with 5 theme entries.
// Menu: Tools > CrowdDefense > Seed LevelThemeMaterialConfig
// Batch: -executeMethod CrowdDefense.Editor.LevelThemeMaterialConfigSeed.Run
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    public static class LevelThemeMaterialConfigSeed
    {
        private const string AssetPath = "Assets/Resources/LevelThemeMaterialConfig.asset";
        private const string MatBase   = "Assets/Resources/Materials/";

        [MenuItem("Tools/CrowdDefense/Seed LevelThemeMaterialConfig")]
        public static void RunMenu() => Run();

        // Entry-point for -executeMethod (must be static void, no args)
        public static void Run()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<LevelThemeMaterialConfig>(AssetPath);
            if (cfg == null)
            {
                Debug.LogError("[LevelThemeMaterialConfigSeed] Asset not found at " + AssetPath);
                return;
            }

            var matDefault = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "Toon_Default.mat");
            var matWater   = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "Toon_Water.mat");
            var matLava    = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "Toon_Lava.mat");
            var matSnow    = AssetDatabase.LoadAssetAtPath<Material>(MatBase + "Toon_Snow.mat");

            if (matDefault == null || matWater == null || matLava == null || matSnow == null)
            {
                Debug.LogError("[LevelThemeMaterialConfigSeed] One or more Toon_*.mat missing in " + MatBase);
                return;
            }

            // Per-theme entries — foret uses a dedicated instance with a deeper green tint.
            var matForet = new Material(matDefault) { name = "Toon_Foret" };
            matForet.SetColor("_BaseColor", new Color(0.18f, 0.45f, 0.18f, 1f));
            AssetDatabase.CreateAsset(matForet, MatBase + "Toon_Foret.mat");

            var so = new SerializedObject(cfg);

            var entries = so.FindProperty("entries");
            entries.arraySize = 0;
            entries.arraySize = 5;

            void SetEntry(int i, LevelTheme theme,
                          Material? surf, Material? water, Material? lava)
            {
                var el = entries.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("theme").enumValueIndex     = (int)theme;
                el.FindPropertyRelative("surfaceMat").objectReferenceValue = surf;
                el.FindPropertyRelative("waterMat").objectReferenceValue   = water;
                el.FindPropertyRelative("lavaMat").objectReferenceValue    = lava;
            }

            SetEntry(0, LevelTheme.Plaine,   matDefault, matWater, null);
            SetEntry(1, LevelTheme.Foret,    matForet,   matWater, null);
            SetEntry(2, LevelTheme.Volcan,   matLava,    null,     matLava);
            SetEntry(3, LevelTheme.Medieval, matSnow,    matWater, null); // neige → Medieval fallback
            SetEntry(4, LevelTheme.Submarin, matWater,   matWater, null);

            so.FindProperty("defaultSurfaceMat").objectReferenceValue = matDefault;
            so.FindProperty("defaultWaterMat").objectReferenceValue   = matWater;
            so.FindProperty("defaultLavaMat").objectReferenceValue    = matLava;

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[LevelThemeMaterialConfigSeed] Done — 5 theme entries seeded.");
        }
    }
}
#endif
