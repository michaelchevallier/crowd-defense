#nullable enable
using UnityEditor;
using UnityEngine;

namespace CrowdDefense.Editor
{
    public static class SeedLevelsTool
    {
        [MenuItem("Tools/CrowdDefense/Seed Levels")]
        public static void SeedAll()
        {
            LevelImporter.ImportAll();
            LevelRegistryTool.BuildLevelRegistry();
            Debug.Log("[SeedLevelsTool] Done — levels imported + LevelRegistry rebuilt.");
        }
    }
}
