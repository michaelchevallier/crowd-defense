#nullable enable
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrowdDefense.Build
{
    public static class BuildScript
    {
        private const string OutputFolder = "Builds/WebGL";
        private const string LoaderScene = "Assets/Scenes/Loader.unity";
        private const string MenuScene = "Assets/Scenes/Menu.unity";
        private const string WorldMapScene = "Assets/Scenes/WorldMap.unity";
        private const string MainScene = "Assets/Scenes/Main.unity";

        [MenuItem("CrowdDefense/Build WebGL")]
        public static void BuildWebGL()
        {
            ApplyWebGLPlayerSettings();
            EnsureAssetRegistryInclusion();

            string absOutput = Path.Combine(Directory.GetCurrentDirectory(), OutputFolder);
            if (Directory.Exists(absOutput))
                Directory.Delete(absOutput, true);
            Directory.CreateDirectory(absOutput);

            var scenesList = new System.Collections.Generic.List<string>();
            if (File.Exists(LoaderScene)) scenesList.Add(LoaderScene);
            if (File.Exists(MenuScene)) scenesList.Add(MenuScene);
            if (File.Exists(WorldMapScene)) scenesList.Add(WorldMapScene);
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
            EnsureRequiredShadersIncluded();
        }

        private static void EnsureRequiredShadersIncluded()
        {
            var requiredShaderNames = new[]
            {
                "CrowdDefense/Toon/Lit",
                "CrowdDefense/Toon/Water",
                "CrowdDefense/Toon/Lava",
                "CrowdDefense/Toon/Snow",
                "CrowdDefense/OutlineInvertedHull",
            };

            var graphicsSettingsAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphicsSettingsAsset == null || graphicsSettingsAsset.Length == 0)
            {
                Debug.LogWarning("[BuildScript] Could not load GraphicsSettings.asset");
                return;
            }
            var so = new SerializedObject(graphicsSettingsAsset[0]);
            var shadersProp = so.FindProperty("m_AlwaysIncludedShaders");
            if (shadersProp == null)
            {
                Debug.LogWarning("[BuildScript] m_AlwaysIncludedShaders not found");
                return;
            }

            foreach (var shaderName in requiredShaderNames)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    Debug.LogWarning($"[BuildScript] Required shader not found: {shaderName}");
                    continue;
                }

                bool alreadyIncluded = false;
                for (int i = 0; i < shadersProp.arraySize; i++)
                {
                    var elem = shadersProp.GetArrayElementAtIndex(i);
                    if (elem.objectReferenceValue == shader)
                    {
                        alreadyIncluded = true;
                        break;
                    }
                }

                if (!alreadyIncluded)
                {
                    shadersProp.InsertArrayElementAtIndex(shadersProp.arraySize);
                    shadersProp.GetArrayElementAtIndex(shadersProp.arraySize - 1).objectReferenceValue = shader;
                    Debug.Log($"[BuildScript] Added shader to Always Included: {shaderName}");
                }
            }

            so.ApplyModifiedProperties();
        }

        // CI hook: launches the UnityTestFramework PlayMode tests (filter: Smoke fixture)
        // via the TestRunnerApi and exits 0 / 1 based on result. Invoke from CLI:
        //   Unity -batchmode -projectPath . -executeMethod CrowdDefense.Build.BuildScript.RunSmokeTests -quit
        public static void RunSmokeTests()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter
            {
                testMode = TestMode.PlayMode,
                groupNames = new[] { "CrowdDefense\\.Tests\\.Runtime\\.Scenarios\\.SmokeTests" },
            };
            api.RegisterCallbacks(new SmokeTestCallbacks());
            api.Execute(new ExecutionSettings(filter));
        }

        private sealed class SmokeTestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
                => Debug.Log($"[BuildScript] Smoke tests started: {testsToRun.TestCaseCount} case(s)");

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.HasChildren)
                    Debug.Log($"[BuildScript] {result.Test.FullName} → {result.TestStatus}");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Debug.Log($"[BuildScript] Smoke RESULT: pass={result.PassCount} fail={result.FailCount} skip={result.SkipCount}");
                EditorApplication.Exit(result.FailCount == 0 ? 0 : 1);
            }
        }

        private static void EnsureAssetRegistryInclusion()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CrowdDefense.Data.AssetRegistry>("Assets/Resources/AssetRegistry.asset");
            if (registry == null)
            {
                Debug.LogWarning("[BuildScript] AssetRegistry not found");
                return;
            }

            var preloaded = new System.Collections.Generic.List<UnityEngine.Object>(PlayerSettings.GetPreloadedAssets());
            int added = 0;

            foreach (var entry in registry.GetAllEntries())
            {
                if (entry.Prefab != null && !preloaded.Contains(entry.Prefab))
                {
                    preloaded.Add(entry.Prefab);
                    added++;
                }
            }

            if (added > 0)
            {
                PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
                Debug.Log($"[BuildScript] Added {added} assets to preloaded list (total: {preloaded.Count})");
            }
        }
    }
}
#endif
