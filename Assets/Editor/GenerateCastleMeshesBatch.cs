#nullable enable
using UnityEditor;
using UnityEngine;

namespace CrowdDefense.Editor
{
    /// <summary>
    /// Batch-mode entry point for generating castle meshes without opening Editor UI.
    /// Usage: Unity -projectPath . -executeMethod CrowdDefense.Editor.GenerateCastleMeshesBatch.GenerateAndQuit
    /// </summary>
    public class GenerateCastleMeshesBatch
    {
        public static void GenerateAndQuit()
        {
            BuildCastleProceduralMeshes.BuildAllCastleMeshes();
            EditorApplication.Exit(0);
        }

        public static void Generate()
        {
            BuildCastleProceduralMeshes.BuildAllCastleMeshes();
        }
    }
}
