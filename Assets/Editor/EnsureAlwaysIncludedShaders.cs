#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CrowdDefense.Editor
{
    // Adds critical CrowdDefense + URP shaders to GraphicsSettings.AlwaysIncludedShaders.
    // Prevents URP shader variant stripping from removing shaders only referenced via
    // Resources.Load / runtime material assignment (which the build pipeline cannot detect).
    //
    // Symptom fixed : runtime "ERROR: Shader" + ArgumentNullException (shader == null)
    // at boot in WebGL deployed build.
    //
    // Run via menu CrowdDefense > Build > Ensure Always-Included Shaders
    // Or batch : -executeMethod CrowdDefense.Editor.EnsureAlwaysIncludedShaders.Run
    public static class EnsureAlwaysIncludedShaders
    {
        private static readonly string[] RequiredShaderNames = new[]
        {
            // Custom CrowdDefense shaders (Resources/runtime-loaded materials)
            "CrowdDefense/Toon/Lit",
            "CrowdDefense/Toon/Water",
            "CrowdDefense/Toon/Lava",
            "CrowdDefense/Toon/Snow",
            "CrowdDefense/Toon/Water_Animated",
            "CrowdDefense/Toon/Lava_Animated",
            "CrowdDefense/OutlineInvertedHull",
            "CrowdDefense/BossJellyfish",
            "CrowdDefense/BossHologram",
            "CrowdDefense/Hologram",
            "CrowdDefense/Jellyfish",
            "CrowdDefense/Kelp",
            "CrowdDefense/Portal",
            "CrowdDefense/SmokeTrail",
            "CrowdDefense/Starfield",
            "CrowdDefense/ToonCelShading",
            "CrowdDefense/Portal_Vortex",
            "CrowdDefense/Hologram_Scanline",
            "CrowdDefense/Kelp_Sway",
            // URP core shaders
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Simple Lit",
            "Universal Render Pipeline/Particles/Unlit",
            // URP internal shaders (used by render pipeline but not directly referenced)
            // These can be null on some GPU/platform combos if not explicitly included.
            "Hidden/Universal Render Pipeline/StencilDitherMaskSeed",
            "Hidden/CoreSRP/CoreCopy",
            // HDRDebugView intentionally omitted : HDR is off (m_SupportsHDR: 0) so this
            // debug shader is not needed and its subshaders fail GPU compat check on WebGL2.
            // Engine fallbacks (Sprites/Default + UI/Default sont safe).
            // NE PAS ajouter Hidden/InternalErrorShader : HideFlags.DontSave => "An asset
            // is marked with HideFlags.DontSave but is included in the build" => Build Failed.
            "Sprites/Default",
            "UI/Default",
        };

        [MenuItem("CrowdDefense/Build/Ensure Always-Included Shaders")]
        public static void RunMenu() => Run();

        public static void Run()
        {
            var graphicsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphicsAssets == null || graphicsAssets.Length == 0)
            {
                Debug.LogError("[EnsureShaders] Could not load ProjectSettings/GraphicsSettings.asset");
                return;
            }

            var so = new SerializedObject(graphicsAssets[0]);
            var shadersProp = so.FindProperty("m_AlwaysIncludedShaders");
            if (shadersProp == null || !shadersProp.isArray)
            {
                Debug.LogError("[EnsureShaders] m_AlwaysIncludedShaders property not found.");
                return;
            }

            var existing = new HashSet<Object>();
            for (int i = 0; i < shadersProp.arraySize; i++)
            {
                var elem = shadersProp.GetArrayElementAtIndex(i).objectReferenceValue;
                if (elem != null) existing.Add(elem);
            }

            int added = 0;
            int missing = 0;
            foreach (var name in RequiredShaderNames)
            {
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning($"[EnsureShaders] Shader.Find returned null for: {name}");
                    missing++;
                    continue;
                }

                if (existing.Contains(shader))
                    continue;

                int idx = shadersProp.arraySize;
                shadersProp.InsertArrayElementAtIndex(idx);
                shadersProp.GetArrayElementAtIndex(idx).objectReferenceValue = shader;
                existing.Add(shader);
                added++;
                Debug.Log($"[EnsureShaders] Added: {name}");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            Debug.Log($"[EnsureShaders] Done. Added {added}, missing {missing}, total {shadersProp.arraySize}.");
        }
    }
}
#endif
#nullable restore
