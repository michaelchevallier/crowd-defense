#nullable enable
using UnityEditor;
using UnityEditor.SceneHierarchy;
using UnityEngine;
using System.IO;

namespace CrowdDefense.Editor
{
    /// <summary>
    /// Auto-generates castle procedural meshes on first import if they don't exist.
    /// Runs during Asset import phase before scenes load.
    /// </summary>
    public class CastleMeshPostProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // Check if castle meshes exist
            if (!NeedsCastleMeshes()) return;

            // Generate them on first run
            BuildCastleProceduralMeshes.BuildAllCastleMeshes();
        }

        private static bool NeedsCastleMeshes()
        {
            string castleDir = "Assets/Models/Castle";
            if (!Directory.Exists(castleDir))
                return true;

            string[] requiredMeshes = {
                "castle_intact.asset",
                "castle_cracked.asset",
                "castle_ruined.asset",
                "castle_critical.asset"
            };

            foreach (var mesh in requiredMeshes)
            {
                if (!File.Exists(Path.Combine(castleDir, mesh)))
                    return true;
            }

            return false;
        }
    }
}
