#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace CrowdDefense.Editor
{
    // Programmatically build MixerGroups.mixer with 4 groups (Master/SFX/Music/UI),
    // 4 exposed params (MasterVol/SFXVol/MusicVol/UIVol) and 3 snapshots (Gameplay/Paused/GameOver).
    // Uses Unity's internal DoCreateAudioMixer flow + AudioMixerController reflection.
    public static class AudioMixerBuilder
    {
        private const string MixerPath = "Assets/Audio/MixerGroups.mixer";
        private const string MixerDir = "Assets/Audio";

        [MenuItem("Tools/CrowdDefense/Build AudioMixer")]
        public static void BuildAudioMixer()
        {
            if (!Directory.Exists(MixerDir))
                Directory.CreateDirectory(MixerDir);

            if (AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath) != null)
            {
                Debug.Log($"[AudioMixerBuilder] Mixer exists at {MixerPath}, rebuilding.");
                AssetDatabase.DeleteAsset(MixerPath);
            }

            // Use DoCreateAudioMixer.Action(int, string, string) — same flow as Project → Create → Audio Mixer.
            var doCreateType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.FullName == "UnityEditor.ProjectWindowCallback.DoCreateAudioMixer");
            if (doCreateType == null)
            {
                Debug.LogError("[AudioMixerBuilder] DoCreateAudioMixer type not found.");
                return;
            }
            var doCreate = ScriptableObject.CreateInstance(doCreateType);
            var actionMethod = doCreateType.GetMethod(
                "Action", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (actionMethod == null)
            {
                Debug.LogError("[AudioMixerBuilder] DoCreateAudioMixer.Action not found.");
                return;
            }
            actionMethod.Invoke(doCreate, new object[] { 0, MixerPath, null! });

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null)
            {
                Debug.LogError("[AudioMixerBuilder] Failed to load mixer after creation.");
                return;
            }

            var controller = mixer;
            var controllerType = controller.GetType();
            var masterGroup = controllerType.GetProperty(
                "masterGroup", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(controller);
            if (masterGroup == null)
            {
                Debug.LogError("[AudioMixerBuilder] masterGroup null after default creation.");
                return;
            }

            // Add child groups SFX / Music / UI under master.
            var createNewGroup = controllerType.GetMethod(
                "CreateNewGroup",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null, new[] { typeof(string), typeof(bool) }, null);
            var addChildToParent = controllerType.GetMethod(
                "AddChildToParent",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var addGroupToCurrentView = controllerType.GetMethod(
                "AddGroupToCurrentView",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var addNewSubAsset = controllerType.GetMethod(
                "AddNewSubAsset",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (createNewGroup == null || addChildToParent == null)
            {
                Debug.LogError("[AudioMixerBuilder] CreateNewGroup / AddChildToParent missing.");
                return;
            }

            foreach (var name in new[] { "SFX", "Music", "UI" })
            {
                var group = createNewGroup.Invoke(controller, new object[] { name, false });
                if (group == null) continue;
                addChildToParent.Invoke(controller, new[] { group, masterGroup });
                // CreateNewGroup already adds the group as a sub-asset of the mixer ;
                // AddGroupToCurrentView and AddNewSubAsset both throw on a freshly created
                // mixer (empty views[] and SaveAssets ordering). Skipping both is safe.
            }

            // Expose volume params per group.
            ExposeGroupVolume(controller, masterGroup, "MasterVol");
            ExposeNamedGroupVolume(controller, "SFX", "SFXVol");
            ExposeNamedGroupVolume(controller, "Music", "MusicVol");
            ExposeNamedGroupVolume(controller, "UI", "UIVol");

            // Snapshots Gameplay (rename default) + Paused + GameOver.
            BuildSnapshots(controller);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AudioMixerBuilder] Created {MixerPath} with 4 groups + 4 exposed params + 3 snapshots.");
        }

        private static void ExposeNamedGroupVolume(object controller, string groupName, string paramName)
        {
            var type = controller.GetType();
            var getAllGroupsSlow = type.GetMethod(
                "GetAllAudioGroupsSlow",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (getAllGroupsSlow == null) return;

            var allGroups = getAllGroupsSlow.Invoke(controller, null) as IEnumerable;
            if (allGroups == null) return;

            foreach (var g in allGroups)
            {
                var nameProp = g.GetType().GetProperty("name");
                if ((nameProp?.GetValue(g) as string) == groupName)
                {
                    ExposeGroupVolume(controller, g, paramName);
                    return;
                }
            }
            Debug.LogWarning($"[AudioMixerBuilder] Group '{groupName}' not found for exposure.");
        }

        private static void ExposeGroupVolume(object controller, object group, string paramName)
        {
            // AudioGroupParameterPath(AudioMixerGroupController, UnityEditor.GUID) is the
            // path Unity uses to bind an exposed param to a group's volume parameter.
            var pathType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.FullName == "UnityEditor.Audio.AudioGroupParameterPath");
            if (pathType == null)
            {
                Debug.LogWarning("[AudioMixerBuilder] AudioGroupParameterPath type not found.");
                return;
            }

            var getGuidForVolume = group.GetType().GetMethod(
                "GetGUIDForVolume",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var volumeGuid = getGuidForVolume?.Invoke(group, null);
            if (volumeGuid == null) return;

            object? pathInstance;
            try
            {
                pathInstance = Activator.CreateInstance(pathType, new[] { group, volumeGuid });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AudioMixerBuilder] Failed to construct path: {ex.InnerException?.Message ?? ex.Message}");
                return;
            }

            var controllerType = controller.GetType();
            var addExposed = controllerType.GetMethod(
                "AddExposedParameter",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            addExposed?.Invoke(controller, new[] { pathInstance });

            // Rename the freshly-added exposed parameter (last entry) from "MyExposedParam"
            // to our canonical paramName. ExposedAudioParameter is a struct so we must
            // box-modify-unbox via the array element setter.
            // Find the just-added param by matching its volumeGuid (struct field), then rename.
            var exposedParamsProp = controllerType.GetProperty(
                "exposedParameters",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var exposedArr = exposedParamsProp?.GetValue(controller) as Array;
            if (exposedArr == null || exposedArr.Length == 0) return;

            for (int i = 0; i < exposedArr.Length; i++)
            {
                object p = exposedArr.GetValue(i);
                var guidField = p.GetType().GetField(
                    "guid", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (guidField == null) continue;
                if (!guidField.GetValue(p).Equals(volumeGuid)) continue;

                var nameField = p.GetType().GetField(
                    "name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (nameField == null) return;
                nameField.SetValue(p, paramName);
                exposedArr.SetValue(p, i);
                exposedParamsProp!.SetValue(controller, exposedArr);
                return;
            }
        }

        private static void BuildSnapshots(object controller)
        {
            var type = controller.GetType();

            // Rename default "Snapshot" → "Gameplay".
            var snapshotsProp = type.GetProperty(
                "snapshots", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var snapshots = snapshotsProp?.GetValue(controller) as Array;
            if (snapshots != null && snapshots.Length > 0)
            {
                var first = snapshots.GetValue(0);
                var nameProp = first.GetType().GetProperty("name");
                nameProp?.SetValue(first, "Gameplay");
            }

            // CloneNewSnapshotFromTarget(bool storeUndoState) creates a copy of the current snapshot.
            var clone = type.GetMethod(
                "CloneNewSnapshotFromTarget", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (clone == null) return;

            clone.Invoke(controller, new object[] { false });
            RenameLastSnapshot(controller, "Paused");

            clone.Invoke(controller, new object[] { false });
            RenameLastSnapshot(controller, "GameOver");
        }

        private static void RenameLastSnapshot(object controller, string newName)
        {
            var type = controller.GetType();
            var snapshotsProp = type.GetProperty(
                "snapshots", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var snapshots = snapshotsProp?.GetValue(controller) as Array;
            if (snapshots == null || snapshots.Length == 0) return;

            var last = snapshots.GetValue(snapshots.Length - 1);
            var nameProp = last.GetType().GetProperty("name");
            nameProp?.SetValue(last, newName);
        }
    }
}
