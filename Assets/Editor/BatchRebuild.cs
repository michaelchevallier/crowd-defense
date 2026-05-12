#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace CrowdDefense.Editor
{
    public static class BatchRebuild
    {
        public static void SetupAndBuild()
        {
            Debug.Log("[BatchRebuild] === Setup Main Scene ===");
            SetupMainScene.Run();
            Debug.Log("[BatchRebuild] === Ensure Always-Included Shaders ===");
            EnsureAlwaysIncludedShaders.Run();
            Debug.Log("[BatchRebuild] === Build WebGL ===");
            CrowdDefense.Build.BuildScript.BuildWebGL();
            Debug.Log("[BatchRebuild] === Done ===");
        }
    }
}
#endif
