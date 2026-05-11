#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CrowdDefense.Editor
{
    // Réécrit les fichiers .meta des .glb/.gltf qui ont DefaultImporter au lieu
    // du ScriptedImporter UnityGLTF (Khronos, package org.khronos.unitygltf).
    // Conserve le guid original — pas de break references existantes.
    // Lancer via menu Tools > CrowdDefense > Fix GLTF Importers
    // ou en batch via CrowdDefense.Editor.FixGltfImporters.RunBatch.
    public static class FixGltfImporters
    {
        private const string k_ModelsRoot = "Assets/Models";
        // UnityGLTF (Khronos) ScriptedImporter script guid — voir Heroes/Stylized/*.glb.meta.
        private const string k_UnityGltfScriptGuid = "715df9372183c47e389bb6e19fbc3b52";

        [MenuItem("Tools/CrowdDefense/Fix GLTF Importers")]
        public static void Run()
        {
            RunInternal(verbose: true);
        }

        // Entry point pour batch mode CLI.
        public static void RunBatch()
        {
            try
            {
                RunInternal(verbose: true);
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FixGltfImporters] Batch failed: {ex}");
                EditorApplication.Exit(1);
            }
        }

        // Exposé pour AssetRegistryTool.BuildAssetRegistry — idempotence garantie.
        public static (int fixedCount, int skipped, int crashed) RunInternal(bool verbose)
        {
            if (!Directory.Exists(k_ModelsRoot))
            {
                Debug.LogWarning($"[FixGltfImporters] Models root not found: {k_ModelsRoot}");
                return (0, 0, 0);
            }

            var files = Directory.GetFiles(k_ModelsRoot, "*.*", SearchOption.AllDirectories)
                .Where(p =>
                {
                    var ext = Path.GetExtension(p).ToLowerInvariant();
                    return ext == ".glb" || ext == ".gltf";
                })
                .Select(p => p.Replace('\\', '/'))
                .ToList();

            int fixedCount = 0;
            int skipped = 0;
            int crashed = 0;
            var rewrittenPaths = new List<string>();

            foreach (var assetPath in files)
            {
                var metaPath = assetPath + ".meta";
                if (!File.Exists(metaPath))
                {
                    if (verbose) Debug.LogWarning($"[FixGltfImporters] Missing .meta: {metaPath}");
                    skipped++;
                    continue;
                }

                string content;
                try
                {
                    content = File.ReadAllText(metaPath);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FixGltfImporters] Read failed for {metaPath}: {ex.Message}");
                    skipped++;
                    continue;
                }

                if (content.Contains("ScriptedImporter:"))
                {
                    skipped++;
                    continue;
                }

                if (!content.Contains("DefaultImporter:"))
                {
                    if (verbose) Debug.LogWarning($"[FixGltfImporters] Unknown importer in {metaPath} — skipped.");
                    skipped++;
                    continue;
                }

                var guidMatch = Regex.Match(content, @"^guid:\s*([0-9a-fA-F]+)\s*$", RegexOptions.Multiline);
                if (!guidMatch.Success)
                {
                    Debug.LogWarning($"[FixGltfImporters] No guid found in {metaPath} — corrupted, skipped.");
                    skipped++;
                    continue;
                }

                var guid = guidMatch.Groups[1].Value;
                var newContent = BuildScriptedImporterMeta(guid);

                try
                {
                    File.WriteAllText(metaPath, newContent);
                    rewrittenPaths.Add(assetPath);
                    fixedCount++;
                    if (verbose) Debug.Log($"[FixGltfImporters] Rewrote {metaPath} (guid={guid})");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FixGltfImporters] Write failed for {metaPath}: {ex.Message}");
                    skipped++;
                }
            }

            if (rewrittenPaths.Count > 0)
            {
                // AssetDatabase.Refresh() suffit pour ré-importer les .meta réécrits.
                // ImportAsset(path, ForceUpdate) déclenche un bug GLTFast Jobs threading
                // (SortAndNormalizeBoneWeightsJob race condition) sur les rigged GLB/GLTF.
                AssetDatabase.Refresh();
            }

            Debug.Log($"[FixGltfImporters] Summary: {fixedCount} fixed, {skipped} skipped, {crashed} crashed (total scanned: {files.Count}).");
            return (fixedCount, skipped, crashed);
        }

        private static string BuildScriptedImporterMeta(string guid)
        {
            return
                "fileFormatVersion: 2\n" +
                $"guid: {guid}\n" +
                "ScriptedImporter:\n" +
                "  internalIDToNameTable: []\n" +
                "  externalObjects: {}\n" +
                "  serializedVersion: 2\n" +
                "  userData: \n" +
                "  assetBundleName: \n" +
                "  assetBundleVariant: \n" +
                $"  script: {{fileID: 11500000, guid: {k_UnityGltfScriptGuid}, type: 3}}\n" +
                "  editorImportSettings:\n" +
                "    generateSecondaryUVSet: 0\n" +
                "  importSettings:\n" +
                "    nodeNameMethod: 1\n" +
                "    animationMethod: 2\n" +
                "    generateMipMaps: 1\n" +
                "    texturesReadable: 0\n" +
                "    defaultMinFilterMode: 9729\n" +
                "    defaultMagFilterMode: 9729\n" +
                "    anisotropicFilterLevel: 1\n" +
                "  instantiationSettings:\n" +
                "    mask: -1\n" +
                "    layer: 0\n" +
                "    skinUpdateWhenOffscreen: 1\n" +
                "    lightIntensityFactor: 1\n" +
                "    sceneObjectCreation: 2\n" +
                "  assetDependencies: []\n" +
                "  reportItems: []\n";
        }
    }
}
#endif
