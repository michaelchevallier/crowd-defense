using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace CrowdDefense.Editor
{
    /// <summary>
    /// Resolves missing shader GUIDs by scanning all .meta files.
    /// Helps identify when a material points to a shader GUID that no longer exists.
    /// </summary>
    public static class ShaderGUIDResolver
    {
        [MenuItem("Tools/CrowdDefense/Debug — List All Shader GUIDs")]
        public static void ListShaderGUIDs()
        {
            var shaderGuids = new Dictionary<string, string>();
            var shaderMetaFiles = Directory.GetFiles("Assets/Shaders", "*.meta", SearchOption.AllDirectories);

            foreach (var metaFile in shaderMetaFiles)
            {
                var lines = File.ReadAllLines(metaFile);
                var guidLine = lines.FirstOrDefault(l => l.Contains("guid:"));
                
                if (guidLine != null)
                {
                    var guid = guidLine.Split(' ')[1];
                    var shaderFile = metaFile.Replace(".meta", "");
                    shaderGuids[guid] = Path.GetFileName(shaderFile);
                }
            }

            Debug.Log($"[ShaderGUIDResolver] Found {shaderGuids.Count} shader file(s):");
            foreach (var (guid, name) in shaderGuids)
            {
                Debug.Log($"  {name:40} → {guid}");
            }
        }

        [MenuItem("Tools/CrowdDefense/Debug — Validate All Material Shader References")]
        public static void ValidateShaderReferences()
        {
            var shaderGuids = new HashSet<string>();
            var shaderMetaFiles = Directory.GetFiles("Assets/Shaders", "*.meta", SearchOption.AllDirectories);

            foreach (var metaFile in shaderMetaFiles)
            {
                var lines = File.ReadAllLines(metaFile);
                var guidLine = lines.FirstOrDefault(l => l.Contains("guid:"));
                if (guidLine != null)
                {
                    var guid = guidLine.Split(' ')[1];
                    shaderGuids.Add(guid);
                }
            }

            var matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
            var orphaned = new List<(string path, string guid)>();

            foreach (var guid in matGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var lines = File.ReadAllLines(path);
                var shaderLine = lines.FirstOrDefault(l => l.Contains("m_Shader:"));
                
                if (shaderLine != null)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(shaderLine, @"guid: ([0-9a-f]+)");
                    if (match.Success)
                    {
                        var shaderGuid = match.Groups[1].Value;
                        if (!shaderGuids.Contains(shaderGuid) && shaderGuid != "0000000000000000f000000000000000")
                        {
                            orphaned.Add((Path.GetFileName(path), shaderGuid));
                        }
                    }
                }
            }

            if (orphaned.Count == 0)
            {
                Debug.Log("[ShaderGUIDResolver] ✓ All material shader references are valid.");
                return;
            }

            Debug.LogWarning($"[ShaderGUIDResolver] Found {orphaned.Count} orphaned shader reference(s):");
            foreach (var (matName, shaderGuid) in orphaned)
            {
                Debug.LogWarning($"  {matName} → missing shader GUID {shaderGuid}");
            }
        }
    }
}
