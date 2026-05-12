#nullable enable
using UnityEngine;
using CrowdDefense.Entities;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Ensures castle meshes are generated on first run.
    /// Call from InitializeOnLoad or from a scene initialization.
    /// </summary>
    public static class CastleMeshInitializer
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EnsureCastleMeshes()
        {
            string castleDir = "Assets/Models/Castle";
            
            // Skip if all meshes exist
            if (File.Exists($"{castleDir}/castle_intact.asset") &&
                File.Exists($"{castleDir}/castle_cracked.asset") &&
                File.Exists($"{castleDir}/castle_ruined.asset") &&
                File.Exists($"{castleDir}/castle_critical.asset"))
            {
                return;
            }

            // Generate them automatically
            Editor.BuildCastleProceduralMeshes.BuildAllCastleMeshes();
        }
#endif
    }
}
