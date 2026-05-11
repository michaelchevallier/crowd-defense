#nullable enable
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

        [MenuItem("Tools/CrowdDefense/Build AudioClipRegistry")]
        public static void BuildAudioClipRegistry()
        {
            var registry = LoadOrCreateRegistry();

            if (!Directory.Exists(SfxRoot))
            {
                Debug.LogWarning($"[AudioClipRegistryTool] SFX directory not found: {SfxRoot}");
                return;
            }

            var files = Directory.GetFiles(SfxRoot, "*.ogg", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(SfxRoot, "*.wav", SearchOption.TopDirectoryOnly))
                .Concat(Directory.GetFiles(SfxRoot, "*.mp3", SearchOption.TopDirectoryOnly))
                .ToArray();

            var serialized = new SerializedObject(registry);
            var arrayProp = serialized.FindProperty("entries");
            arrayProp.arraySize = files.Length;

            int populated = 0;
            for (int i = 0; i < files.Length; i++)
            {
                var unityPath = files[i].Replace('\\', '/');
                var key = Path.GetFileNameWithoutExtension(unityPath).ToLowerInvariant();
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

            Debug.Log($"[AudioClipRegistryTool] Built registry with {populated}/{files.Length} entries.");
        }

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
