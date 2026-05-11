#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrowdDefense.Data;
using UnityEditor;
using UnityEngine;

namespace CrowdDefense.Editor
{
    public static class AudioClipRegistryTool
    {
        private const string RegistryPath = "Assets/ScriptableObjects/Audio/AudioClipRegistry.asset";
        private const string SfxRoot = "Assets/Audio/SFX";
        private const string MusicRoot = "Assets/Audio/Music";

        [MenuItem("Tools/CrowdDefense/Build AudioClipRegistry")]
        public static void BuildAudioClipRegistry()
        {
            var registry = LoadOrCreateRegistry();

            var files = new List<string>();

            if (Directory.Exists(SfxRoot))
                files.AddRange(EnumerateAudioFiles(SfxRoot));
            else
                Debug.LogWarning($"[AudioClipRegistryTool] SFX directory not found: {SfxRoot}");

            if (Directory.Exists(MusicRoot))
                files.AddRange(EnumerateAudioFiles(MusicRoot));

            var serialized = new SerializedObject(registry);
            var arrayProp = serialized.FindProperty("entries");
            arrayProp.arraySize = files.Count;

            int populated = 0;
            for (int i = 0; i < files.Count; i++)
            {
                var unityPath = files[i].Replace('\\', '/');
                var baseName = Path.GetFileNameWithoutExtension(unityPath).ToLowerInvariant();
                var key = unityPath.StartsWith(MusicRoot) ? $"music_{baseName}" : baseName;
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(unityPath);

                var elem = arrayProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("Key").stringValue = key;
                elem.FindPropertyRelative("Clip").objectReferenceValue = clip;

                if (clip != null)
                    populated++;
                else
                    Debug.LogWarning($"[AudioClipRegistryTool] Clip load failed: {unityPath}");
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AudioClipRegistryTool] Built registry with {populated}/{files.Count} entries (SFX + Music).");
        }

        private static IEnumerable<string> EnumerateAudioFiles(string root) =>
            Directory.GetFiles(root, "*.ogg", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(root, "*.wav", SearchOption.TopDirectoryOnly))
                .Concat(Directory.GetFiles(root, "*.mp3", SearchOption.TopDirectoryOnly));

        private static AudioClipRegistry LoadOrCreateRegistry()
        {
            var dir = Path.GetDirectoryName(RegistryPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var existing = AssetDatabase.LoadAssetAtPath<AudioClipRegistry>(RegistryPath);
            if (existing != null) return existing;

            var registry = ScriptableObject.CreateInstance<AudioClipRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AudioClipRegistryTool] Created new AudioClipRegistry at {RegistryPath}");
            return registry;
        }
    }
}
