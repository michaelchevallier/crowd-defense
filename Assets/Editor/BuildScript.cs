#nullable enable
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CrowdDefense.Build
{
    public static class BuildScript
    {
        private const string OutputRoot = "Build";
        private const string MainScene = "Assets/Scenes/Main.unity";
        private const string MenuScene = "Assets/Scenes/Menu.unity";
        private const string BundleId = "com.crowddefense.game";
        private const string CompanyName = "Crowd Defense";
        private const string ProductName = "Crowd Defense";

        private static readonly string[] Scenes = { MainScene, MenuScene };

        // ──────────────────────────────────────────────────────────────────────────
        // WebGL
        // ──────────────────────────────────────────────────────────────────────────

        [MenuItem("CrowdDefense/Build/WebGL")]
        public static void BuildWebGL()
        {
            ApplyCommonPlayerSettings();
            ApplyWebGLPlayerSettings();
            RunBuild(BuildTarget.WebGL, NamedBuildTarget.WebGL, "WebGL", revealEntry: "index.html");
        }

        public static void ApplyWebGLPlayerSettings()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Master);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.memorySize = 256;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.stripEngineCode = true;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // OSX (Mac Universal Apple Silicon + Intel)
        // ──────────────────────────────────────────────────────────────────────────

        [MenuItem("CrowdDefense/Build/OSX (Mac Universal)")]
        public static void BuildOSX()
        {
            ApplyCommonPlayerSettings();
            ApplyStandalonePlayerSettings();
            PlayerSettings.macOS.applicationCategoryType = "public.app-category.games";
            UserBuildSettings.architecture = OSArchitecture.x64ARM64;
            RunBuild(BuildTarget.StandaloneOSX, NamedBuildTarget.Standalone, "OSX/CrowdDefense.app");
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Windows 64
        // ──────────────────────────────────────────────────────────────────────────

        [MenuItem("CrowdDefense/Build/Windows64")]
        public static void BuildWindows()
        {
            ApplyCommonPlayerSettings();
            ApplyStandalonePlayerSettings();
            RunBuild(BuildTarget.StandaloneWindows64, NamedBuildTarget.Standalone, "Windows/CrowdDefense.exe");
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Linux 64
        // ──────────────────────────────────────────────────────────────────────────

        [MenuItem("CrowdDefense/Build/Linux64")]
        public static void BuildLinux()
        {
            ApplyCommonPlayerSettings();
            ApplyStandalonePlayerSettings();
            RunBuild(BuildTarget.StandaloneLinux64, NamedBuildTarget.Standalone, "Linux/CrowdDefense.x86_64");
        }

        // ──────────────────────────────────────────────────────────────────────────
        // iOS Xcode export (macOS host only)
        // ──────────────────────────────────────────────────────────────────────────

        [MenuItem("CrowdDefense/Build/iOS (Xcode)")]
        public static void BuildIOS()
        {
            ApplyCommonPlayerSettings();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.iOS, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.High);
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            RunBuild(BuildTarget.iOS, NamedBuildTarget.iOS, "iOS");
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Android APK (development) / AAB (Play Store)
        // ──────────────────────────────────────────────────────────────────────────

        [MenuItem("CrowdDefense/Build/Android APK")]
        public static void BuildAndroid()
        {
            ApplyCommonPlayerSettings();
            ApplyAndroidPlayerSettings(buildAppBundle: false);
            RunBuild(BuildTarget.Android, NamedBuildTarget.Android, "Android/CrowdDefense.apk");
        }

        [MenuItem("CrowdDefense/Build/Android AAB (Play Store)")]
        public static void BuildAndroidAAB()
        {
            ApplyCommonPlayerSettings();
            ApplyAndroidPlayerSettings(buildAppBundle: true);
            RunBuild(BuildTarget.Android, NamedBuildTarget.Android, "Android/CrowdDefense.aab");
        }

        // ──────────────────────────────────────────────────────────────────────────
        // BuildAll : Mac + Win + Linux séquentiel (skip iOS/Android car credentials)
        // ──────────────────────────────────────────────────────────────────────────

        [MenuItem("CrowdDefense/Build/All Desktop (Mac+Win+Linux)")]
        public static void BuildAll()
        {
            var t0 = DateTime.UtcNow;
            Debug.Log("[BuildScript] BuildAll start (Mac, Win, Linux)");
            BuildOSX();
            BuildWindows();
            BuildLinux();
            var dt = (DateTime.UtcNow - t0).TotalSeconds;
            Debug.Log($"[BuildScript] BuildAll done in {dt:F1}s");
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Shared settings
        // ──────────────────────────────────────────────────────────────────────────

        private static void ApplyCommonPlayerSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.WebGL, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
        }

        private static void ApplyStandalonePlayerSettings()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Standalone, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.High);
            PlayerSettings.stripEngineCode = true;
        }

        private static void ApplyAndroidPlayerSettings(bool buildAppBundle)
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.High);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            EditorUserBuildSettings.buildAppBundle = buildAppBundle;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Build runner
        // ──────────────────────────────────────────────────────────────────────────

        private static void RunBuild(BuildTarget target, NamedBuildTarget namedTarget, string relativeOutput, string? revealEntry = null)
        {
            string relRoot = Path.Combine(OutputRoot, relativeOutput);
            string absRoot = Path.Combine(Directory.GetCurrentDirectory(), relRoot);
            string platformDir = Path.GetDirectoryName(absRoot) ?? absRoot;

            if (Directory.Exists(platformDir))
                Directory.Delete(platformDir, true);
            Directory.CreateDirectory(platformDir);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(target), target);

            var opts = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = relRoot,
                target = target,
                options = BuildOptions.None,
            };

            Debug.Log($"[BuildScript] Building {target} → {absRoot}");
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            BuildSummary summary = report.summary;
            long mb = summary.totalSize / 1024 / 1024;
            Debug.Log($"[BuildScript] {target} {summary.result} : size={mb} MB, time={summary.totalTime.TotalSeconds:F1}s, output={absRoot}");

            if (summary.result != BuildResult.Succeeded)
                throw new Exception($"[BuildScript] {target} build failed: {summary.result}");

            if (revealEntry != null && Application.isBatchMode == false)
                EditorUtility.RevealInFinder(Path.Combine(platformDir, revealEntry));
        }
    }
}
#endif
