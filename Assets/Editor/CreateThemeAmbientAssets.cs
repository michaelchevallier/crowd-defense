#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Visual;

namespace CrowdDefense.Editor
{
    public static class CreateThemeAmbientAssets
    {
        [MenuItem("CrowdDefense/Create Theme Ambient Assets")]
        public static void Create()
        {
            const string dir = "Assets/Resources/Lighting";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/Resources", "Lighting");

            foreach (var cfg in Configs())
            {
                string path = $"{dir}/ThemeAmbient_{cfg.theme}.asset";
                if (AssetDatabase.LoadAssetAtPath<ThemeAmbientConfig>(path) != null) continue;
                AssetDatabase.CreateAsset(cfg, path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static ThemeAmbientConfig[] Configs()
        {
            var list = new ThemeAmbientConfig[10];

            list[0] = Make(LevelTheme.Plaine,
                sky:     new Color(0.53f, 0.75f, 0.95f),
                equator: new Color(0.70f, 0.82f, 0.70f),
                ground:  new Color(0.25f, 0.45f, 0.15f),
                1.0f);

            list[1] = Make(LevelTheme.Foret,
                sky:     new Color(0.20f, 0.40f, 0.20f),
                equator: new Color(0.30f, 0.50f, 0.28f),
                ground:  new Color(0.25f, 0.18f, 0.10f),
                0.7f);

            list[2] = Make(LevelTheme.Desert,
                sky:     new Color(0.95f, 0.60f, 0.20f),
                equator: new Color(0.95f, 0.80f, 0.50f),
                ground:  new Color(0.80f, 0.65f, 0.30f),
                1.2f);

            list[3] = Make(LevelTheme.Volcan,
                sky:     new Color(0.45f, 0.08f, 0.05f),
                equator: new Color(0.70f, 0.28f, 0.10f),
                ground:  new Color(0.28f, 0.22f, 0.18f),
                0.9f);

            list[4] = Make(LevelTheme.Apocalypse,
                sky:     new Color(0.35f, 0.28f, 0.20f),
                equator: new Color(0.45f, 0.35f, 0.25f),
                ground:  new Color(0.18f, 0.14f, 0.10f),
                0.6f);

            list[5] = Make(LevelTheme.Espace,
                sky:     new Color(0.00f, 0.00f, 0.05f),
                equator: new Color(0.05f, 0.05f, 0.15f),
                ground:  new Color(0.10f, 0.05f, 0.20f),
                0.4f);

            list[6] = Make(LevelTheme.Submarin,
                sky:     new Color(0.10f, 0.35f, 0.70f),
                equator: new Color(0.05f, 0.25f, 0.55f),
                ground:  new Color(0.02f, 0.10f, 0.30f),
                0.5f);

            list[7] = Make(LevelTheme.Medieval,
                sky:     new Color(0.55f, 0.70f, 0.90f),
                equator: new Color(0.60f, 0.65f, 0.60f),
                ground:  new Color(0.35f, 0.32f, 0.28f),
                1.0f);

            list[8] = Make(LevelTheme.Cyberpunk,
                sky:     new Color(0.35f, 0.05f, 0.55f),
                equator: new Color(0.15f, 0.08f, 0.35f),
                ground:  new Color(0.02f, 0.02f, 0.10f),
                0.8f);

            list[9] = Make(LevelTheme.Foire,
                sky:     new Color(1.00f, 0.60f, 0.80f),
                equator: new Color(0.95f, 0.85f, 0.50f),
                ground:  new Color(0.70f, 0.55f, 0.75f),
                1.3f);

            return list;
        }

        private static ThemeAmbientConfig Make(LevelTheme theme, Color sky, Color equator, Color ground, float intensity)
        {
            var so = ScriptableObject.CreateInstance<ThemeAmbientConfig>();
            so.theme        = theme;
            so.skyColor     = sky;
            so.equatorColor = equator;
            so.groundColor  = ground;
            so.intensity    = intensity;
            return so;
        }
    }
}
#endif
