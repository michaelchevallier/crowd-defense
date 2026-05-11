#nullable enable
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CrowdDefense.Build
{
    public static class BuildScript
    {
        private const string OutputFolder = "Builds/WebGL";
        private const string LoaderScene = "Assets/Scenes/Loader.unity";
        private const string MainScene = "Assets/Scenes/Main.unity";

        [MenuItem("CrowdDefense/Build WebGL")]
        public static void BuildWebGL()
        {
            ApplyWebGLPlayerSettings();

            string absOutput = Path.Combine(Directory.GetCurrentDirectory(), OutputFolder);
            if (Directory.Exists(absOutput))
                Directory.Delete(absOutput, true);
            Directory.CreateDirectory(absOutput);

            var scenesList = new System.Collections.Generic.List<string>();
            if (File.Exists(LoaderScene)) scenesList.Add(LoaderScene);
            scenesList.Add(MainScene);

            var opts = new BuildPlayerOptions
            {
                scenes = scenesList.ToArray(),
                locationPathName = OutputFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            Debug.Log($"[BuildScript] Building WebGL → {absOutput}");
            var report = BuildPipeline.BuildPlayer(opts);
            var summary = report.summary;
            Debug.Log($"[BuildScript] Build {summary.result} : size={summary.totalSize / 1024 / 1024} MB, time={summary.totalTime.TotalSeconds:F1}s");

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorUtility.RevealInFinder(Path.Combine(absOutput, "index.html"));
        }

        public static void ApplyWebGLPlayerSettings()
        {
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.memorySize = 256;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
        }
    }
}
#endif
