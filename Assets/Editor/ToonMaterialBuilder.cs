// Editor-only: creates/refreshes the Toon materials under Assets/Resources/Materials/
// Run via menu CrowdDefense > Build > Create Toon Materials
// Called automatically on domain reload via [InitializeOnLoadMethod]
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace CrowdDefense.Editor
{
    public static class ToonMaterialBuilder
    {
        private const string MaterialsDir = "Assets/Resources/Materials";

        [InitializeOnLoadMethod]
        static void AutoCreate() => CreateAll(silent: true);

        [MenuItem("CrowdDefense/Build/Create Toon Materials")]
        public static void CreateAllMenu() => CreateAll(silent: false);

        public static void CreateAll(bool silent = false)
        {
            if (!AssetDatabase.IsValidFolder(MaterialsDir))
                AssetDatabase.CreateFolder("Assets/Resources", "Materials");

            CreateMat("Toon_Default", "CrowdDefense/Toon/Lit",   null,               silent);
            CreateMat("Toon_Water",   "CrowdDefense/Toon/Water",  ApplyWaterDefaults,  silent);
            CreateMat("Toon_Lava",    "CrowdDefense/Toon/Lava",   ApplyLavaDefaults,   silent);
            CreateMat("Toon_Snow",    "CrowdDefense/Toon/Snow",   ApplySnowDefaults,   silent);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!silent)
                Debug.Log("[ToonMaterialBuilder] Toon materials created/refreshed in " + MaterialsDir);
        }

        private static void CreateMat(string name, string shaderName,
            System.Action<Material>? configure, bool silent)
        {
            string path = $"{MaterialsDir}/{name}.mat";

            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                if (!silent)
                    Debug.LogWarning($"[ToonMaterialBuilder] Shader '{shaderName}' not found — skip {name}.");
                return;
            }

            Material? mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.name = name;
                configure?.Invoke(mat);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                // Update shader ref if it changed (avoids breakage after rename)
                if (mat.shader != shader)
                    mat.shader = shader;
                configure?.Invoke(mat);
                EditorUtility.SetDirty(mat);
            }
        }

        private static void ApplyWaterDefaults(Material m)
        {
            m.SetColor("_Tint",      new Color(0.373f, 0.659f, 0.816f, 1f));
            m.SetFloat("_ScrollSpeedX",    0.12f);
            m.SetFloat("_ScrollSpeedY",    0.06f);
            m.SetFloat("_WaveAmpX",        0.012f);
            m.SetFloat("_WaveAmpY",        0.010f);
            m.SetFloat("_WaveFreqX",       8.0f);
            m.SetFloat("_WaveFreqY",       6.0f);
            m.SetFloat("_WaveSpeedX",      0.8f);
            m.SetFloat("_WaveSpeedY",      0.6f);
            m.SetFloat("_CausticScale",    22.0f);
            m.SetFloat("_CausticStrength", 0.10f);
            m.SetFloat("_FoamWidth",       0.025f);
            m.SetColor("_FoamColor",       new Color(0.95f, 0.98f, 1.0f, 1f));
            m.SetFloat("_FoamStrength",    0.18f);
            m.SetFloat("_VertWaveAmp",     0.05f);
            m.SetFloat("_VertWaveFreq",    2.0f);
            m.SetFloat("_VertWaveSpeed",   1.5f);
        }

        private static void ApplyLavaDefaults(Material m)
        {
            m.SetColor("_Tint",             new Color(1f, 0.627f, 0.314f, 1f));
            m.SetFloat("_ScrollSpeedX",     0.08f);
            m.SetFloat("_ScrollSpeedY",     0.04f);
            m.SetColor("_GlowColor",        new Color(1f, 0.4f, 0f, 1f));
            m.SetFloat("_GlowPulseFreq",    1.2f);
            m.SetFloat("_GlowPulseAmp",     0.6f);
            m.SetFloat("_GlowBase",         0.8f);
            m.SetFloat("_CrackScale",       8.0f);
            m.SetFloat("_CrackContrast",    1.4f);
            m.SetFloat("_EmissionStrength", 1.5f);
        }

        private static void ApplySnowDefaults(Material m)
        {
            m.SetColor("_Tint",              new Color(0.88f, 0.93f, 1.0f, 1f));
            m.SetFloat("_NormalStrength",    0.6f);
            m.SetFloat("_SparkleScale",      30.0f);
            m.SetFloat("_SparkleSpeed",      4.0f);
            m.SetFloat("_SparkleThreshold",  0.82f);
            m.SetColor("_SparkleColor",      Color.white);
            m.SetFloat("_SparkleStrength",   0.9f);
            m.SetFloat("_GlintFreq",         3.0f);
            m.SetFloat("_GlintAmp",          0.25f);
            m.SetFloat("_FresnelPower",      3.0f);
            m.SetColor("_FresnelColor",      new Color(0.7f, 0.85f, 1.0f, 1f));
        }
    }
}
#endif
