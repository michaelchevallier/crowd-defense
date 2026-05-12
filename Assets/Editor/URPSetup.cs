#if UNITY_EDITOR
#nullable enable
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
        private const string RendererPath = "Assets/Settings/UniversalRenderer.asset";

        [InitializeOnLoadMethod]
        static void AutoSetup() => Setup(silent: true);

        [MenuItem("CrowdDefense/Build/Setup URP Pipeline")]
        public static void SetupMenu() => Setup(silent: false);

        public static void Setup(bool silent = false)
        {
            EnsureSettingsFolder();

            var pipeline = LoadOrCreatePipelineAsset(silent);
            if (pipeline == null) return;

            var renderer = LoadOrCreateRendererData(silent);
            if (renderer != null)
                AssignRendererToPipeline(pipeline, renderer, silent);

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

        private static UniversalRendererData? LoadOrCreateRendererData(bool silent)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (existing != null)
            {
                if (!silent)
                    Debug.Log($"[URPSetup] Renderer data already exists at {RendererPath}");
                return existing;
            }

            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            if (renderer == null)
            {
                if (!silent)
                    Debug.LogError("[URPSetup] Failed to create UniversalRendererData.");
                return null;
            }

            AssetDatabase.CreateAsset(renderer, RendererPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!silent)
                Debug.Log($"[URPSetup] Created UniversalRendererData at {RendererPath}");
            return renderer;
        }

        private static void AssignRendererToPipeline(UniversalRenderPipelineAsset pipeline, UniversalRendererData renderer, bool silent)
        {
            var so = new SerializedObject(pipeline);
            var rendererListProp = so.FindProperty("m_RendererDataList");

            if (rendererListProp == null || !rendererListProp.isArray)
            {
                if (!silent)
                    Debug.LogError("[URPSetup] Cannot find m_RendererDataList in pipeline asset.");
                return;
            }

            // Ensure list has at least 1 element
            if (rendererListProp.arraySize == 0)
                rendererListProp.InsertArrayElementAtIndex(0);

            var rendererElement = rendererListProp.GetArrayElementAtIndex(0);
            rendererElement.objectReferenceValue = renderer;

            so.ApplyModifiedPropertiesWithoutUndo();

            if (!silent)
                Debug.Log($"[URPSetup] Assigned UniversalRendererData to pipeline m_RendererDataList[0]");
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
#nullable restore
#endif
