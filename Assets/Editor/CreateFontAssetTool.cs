#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace CrowdDefense.EditorTools
{
    public static class CreateFontAssetTool
    {
        private const string SourceFontPath = "Assets/Fonts/Roboto-Regular.ttf";
        private const string OutputFontAssetPath = "Assets/Fonts/Roboto-Regular SDF.asset";

        [MenuItem("Crowd Defense/UX/Build Roboto SDF Font Asset")]
        public static void BuildRobotoSDF()
        {
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogError($"[CreateFontAssetTool] Source font not found: {SourceFontPath}");
                return;
            }

            const int atlasPadding = 9;
            const int atlasWidth = 1024;
            const int atlasHeight = 1024;
            const int pointSize = 90;

            var fontAsset = FontAsset.CreateFontAsset(
                sourceFont,
                pointSize,
                atlasPadding,
                GlyphRenderMode.SDFAA,
                atlasWidth,
                atlasHeight,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true
            );

            if (fontAsset == null)
            {
                Debug.LogError("[CreateFontAssetTool] CreateFontAsset returned null");
                return;
            }

            fontAsset.name = "Roboto-Regular SDF";

            var outputDir = Path.GetDirectoryName(OutputFontAssetPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            if (File.Exists(OutputFontAssetPath))
                AssetDatabase.DeleteAsset(OutputFontAssetPath);

            AssetDatabase.CreateAsset(fontAsset, OutputFontAssetPath);

            if (fontAsset.atlasTexture != null)
            {
                fontAsset.atlasTexture.name = "Roboto-Regular Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = "Roboto-Regular Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CreateFontAssetTool] Created FontAsset SDF: {OutputFontAssetPath}");
        }
    }
}
