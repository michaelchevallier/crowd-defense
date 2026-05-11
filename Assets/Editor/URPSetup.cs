#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

namespace CrowdDefense.Editor
{
    // Creates Assets/Settings/URP_PipelineAsset.asset and wires it into GraphicsSettings.
    // Run via menu CrowdDefense > Build > Setup URP Pipeline
    // Also runs automatically on domain reload (idempotent).
    public static class URPSetup
    {
        private const string SettingsDir  = "Assets/Settings";
        private const string AssetPath    = "Assets/Settings/URP_PipelineAsset.asset";

        [InitializeOnLoadMethod]
        static void AutoSetup() => Setup(silent: true);

        [MenuItem("CrowdDefense/Build/Setup URP Pipeline")]
        public static void SetupMenu() => Setup(silent: false);

        public static void Setup(bool silent = false)
        {
            EnsureSettingsFolder();

            var pipeline = LoadOrCreatePipelineAsset(silent);
            if (pipeline == null) return;

            SetGraphicsSettings(pipeline, silent);
        }

        private static void EnsureSettingsFolder()
        {
            if (!AssetDatabase.IsValidFolder(SettingsDir))
                AssetDatabase.CreateFolder("Assets", "Settings");
        }

        private static UniversalRenderPipelineAsset? LoadOrCreatePipelineAsset(bool silent)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath);
            if (existing != null)
            {
                if (!silent)
                    Debug.Log($"[URPSetup] Pipeline asset already exists at {AssetPath}");
                return existing;
            }

            // UniversalRenderPipelineAsset.Create() generates asset + renderer data in one call.
            var asset = UniversalRenderPipelineAsset.Create();
            if (asset == null)
            {
                if (!silent)
                    Debug.LogError("[URPSetup] Failed to create UniversalRenderPipelineAsset.");
                return null;
            }

            // Reasonable defaults for a tower-defense game (mobile + WebGL friendly)
            asset.renderScale        = 1.0f;
            asset.msaaSampleCount    = 2;
            asset.supportsHDR        = false;
            asset.shadowDistance     = 50f;

            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!silent)
                Debug.Log($"[URPSetup] Created URP pipeline asset at {AssetPath}");
            return asset;
        }

        private static void SetGraphicsSettings(UniversalRenderPipelineAsset pipeline, bool silent)
        {
            if (GraphicsSettings.defaultRenderPipeline == pipeline)
            {
                if (!silent)
                    Debug.Log("[URPSetup] GraphicsSettings already pointing to URP pipeline asset.");
                return;
            }

            var so = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var prop = so.FindProperty("m_CustomRenderPipeline");
            if (prop == null)
            {
                if (!silent)
                    Debug.LogError("[URPSetup] Cannot find m_CustomRenderPipeline in GraphicsSettings.");
                return;
            }

            prop.objectReferenceValue = pipeline;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!silent)
                Debug.Log("[URPSetup] GraphicsSettings.renderPipelineAsset set to URP_PipelineAsset.");
        }
    }
}
#endif
