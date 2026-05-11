#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CrowdDefense.Editor
{
    // Combine les PNGs KayKit/Quaternius en 4 atlas 2048×2048 pour réduire les drawcalls.
    // Output : Assets/Textures/Atlas/{category}_Atlas.png  + matching .asset rects JSON.
    // Lancer via menu Tools > CrowdDefense > Build Texture Atlases
    public static class TextureAtlasBuilder
    {
        private const int AtlasSize = 2048;
        private const int Padding = 4;
        private const string OutputDir = "Assets/Textures/Atlas";

        // (category, root search path, recursive)
        private static readonly (string Cat, string Root, bool Recurse)[] Groups =
        {
            ("Heroes",    "Assets/Models/Heroes/KayKit/Textures",                       false),
            ("Props",     "Assets/Models/Props",                                         true),
            ("NaturePack","Assets/Models/Environment/Quaternius/StylizedNatureMegaKit",  false),
            ("MedievalPack","Assets/Models/Environment/Quaternius",                      true),
        };

        [MenuItem("Tools/CrowdDefense/Build Texture Atlases")]
        public static void BuildAll()
        {
            if (!Directory.Exists(OutputDir))
                Directory.CreateDirectory(OutputDir);

            // MedievalPack scan includes FantasyPropsMegaKit too — intentional (both env packs).
            // Heroes scan is non-recursive to avoid picking up Characters/ and Equipment/ dupes.
            foreach (var (cat, root, recurse) in Groups)
                BuildAtlas(cat, root, recurse);

            AssetDatabase.Refresh();
            Debug.Log("[TextureAtlasBuilder] Done — 4 atlases written to " + OutputDir);
        }

        private static void BuildAtlas(string category, string searchRoot, bool recurse)
        {
            var option = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            if (!Directory.Exists(searchRoot))
            {
                Debug.LogWarning($"[TextureAtlasBuilder] Skipping {category}: directory not found ({searchRoot})");
                return;
            }

            string[] diskPaths = Directory.GetFiles(searchRoot, "*.png", option)
                .Where(p => !p.EndsWith("contents.png", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToArray();

            if (diskPaths.Length == 0)
            {
                Debug.LogWarning($"[TextureAtlasBuilder] Skipping {category}: no PNGs found");
                return;
            }

            // Dedupe by filename — prefer canonical Textures/ subdir if duplicates exist.
            var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string p in diskPaths)
            {
                string name = Path.GetFileName(p);
                if (!byName.ContainsKey(name) || p.Contains("/Textures/"))
                    byName[name] = p;
            }

            string[] assetPaths = byName.Values
                .Select(p => p.Replace('\\', '/'))
                .Where(p => p.StartsWith("Assets/"))
                .ToArray();

            // Ensure textures are readable in Editor.
            foreach (string ap in assetPaths)
                EnsureReadable(ap);

            Texture2D[] textures = assetPaths
                .Select(ap => AssetDatabase.LoadAssetAtPath<Texture2D>(ap))
                .Where(t => t != null)
                .ToArray()!;

            if (textures.Length == 0)
            {
                Debug.LogWarning($"[TextureAtlasBuilder] Skipping {category}: could not load textures");
                return;
            }

            var atlas = new Texture2D(AtlasSize, AtlasSize, TextureFormat.RGBA32, true);
            Rect[] rects = atlas.PackTextures(textures, Padding, AtlasSize, false);

            string outPath = $"{OutputDir}/{category}_Atlas.png";
            File.WriteAllBytes(outPath, atlas.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(atlas);

            // Write a companion .txt with name→rect mapping for runtime lookup.
            var lines = textures.Select((t, i) =>
                $"{t.name}={rects[i].x:F4},{rects[i].y:F4},{rects[i].width:F4},{rects[i].height:F4}");
            File.WriteAllText($"{OutputDir}/{category}_Atlas_Rects.txt", string.Join("\n", lines));

            Debug.Log($"[TextureAtlasBuilder] {category}: packed {textures.Length} textures → {outPath}");
        }

        private static void EnsureReadable(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null || importer.isReadable) return;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }
}
#endif
