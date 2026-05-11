#if UNITY_EDITOR
#nullable enable
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using CrowdDefense.Systems;
using CrowdDefense.UI;

namespace CrowdDefense.Editor
{
    public static class BuildLoaderSceneTool
    {
        private const string LoaderScenePath = "Assets/Scenes/Loader.unity";

        [MenuItem("Tools/CrowdDefense/Build Loader Scene")]
        public static void BuildLoaderScene()
        {
            // Create Loader.unity if it does not exist
            if (!System.IO.File.Exists(LoaderScenePath))
            {
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(newScene, LoaderScenePath);
                AssetDatabase.Refresh();
                Debug.Log("[BuildLoaderSceneTool] Created new Loader.unity");
            }

            var scene = EditorSceneManager.OpenScene(LoaderScenePath, OpenSceneMode.Single);

            int created = 0;
            int existing = 0;

            EnsureCamera(ref created, ref existing);
            EnsureEventManager(ref created, ref existing);
            EnsureMusicManager(ref created, ref existing);
            EnsureSettingsRegistry(ref created, ref existing);
            EnsureLoaderUI(ref created, ref existing);
            EnsureBuildSettings();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[BuildLoaderSceneTool] Loader.unity ready: {created} new + {existing} existing");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void EnsureCamera(ref int created, ref int existing)
        {
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null) { existing++; return; }

            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var c = go.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.03f, 0.047f, 0.086f, 1f);
            go.AddComponent<AudioListener>();
            created++;
        }

        private static void EnsureEventManager(ref int created, ref int existing)
        {
            if (Object.FindFirstObjectByType<EventManager>() != null) { existing++; return; }
            var go = new GameObject("EventManager");
            go.AddComponent<EventManager>();
            created++;
        }

        private static void EnsureMusicManager(ref int created, ref int existing)
        {
            if (Object.FindFirstObjectByType<MusicManager>() != null) { existing++; return; }
            var go = new GameObject("MusicManager");
            go.AddComponent<MusicManager>();
            created++;
        }

        private static void EnsureSettingsRegistry(ref int created, ref int existing)
        {
            if (Object.FindFirstObjectByType<SettingsRegistry>() != null) { existing++; return; }
            var go = new GameObject("SettingsRegistry");
            go.AddComponent<SettingsRegistry>();
            created++;
        }

        private static void EnsureLoaderUI(ref int created, ref int existing)
        {
            // Loader UIDocument
            var loaderGO = GameObject.Find("LoaderUI");
            if (loaderGO == null)
            {
                loaderGO = new GameObject("LoaderUI");
                created++;
            }
            else
            {
                existing++;
            }

            var doc = loaderGO.GetComponent<UIDocument>() ?? loaderGO.AddComponent<UIDocument>();
            var loaderTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Loader.uxml");
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/HUDPanelSettings.asset");

            var docSO = new SerializedObject(doc);
            if (panelSettings != null)
                docSO.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
            if (loaderTree != null)
                docSO.FindProperty("sourceAsset").objectReferenceValue = loaderTree;
            docSO.ApplyModifiedProperties();

            var loader = loaderGO.GetComponent<LoaderController>() ?? loaderGO.AddComponent<LoaderController>();

            // Settings panel on a separate GO
            var settingsGO = GameObject.Find("SettingsPanelUI");
            if (settingsGO == null)
            {
                settingsGO = new GameObject("SettingsPanelUI");
                created++;
            }
            else
            {
                existing++;
            }

            var settingsDoc = settingsGO.GetComponent<UIDocument>() ?? settingsGO.AddComponent<UIDocument>();
            var settingsTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/SettingsPanel.uxml");
            var settingsSO = new SerializedObject(settingsDoc);
            if (panelSettings != null)
                settingsSO.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
            if (settingsTree != null)
                settingsSO.FindProperty("sourceAsset").objectReferenceValue = settingsTree;
            settingsSO.ApplyModifiedProperties();

            var settingsCtrl = settingsGO.GetComponent<SettingsPanelController>()
                            ?? settingsGO.AddComponent<SettingsPanelController>();

            // Wire settingsPanel reference on LoaderController
            var loaderSerObj = new SerializedObject(loader);
            loaderSerObj.FindProperty("settingsPanel").objectReferenceValue = settingsCtrl;
            loaderSerObj.ApplyModifiedProperties();
        }

        private static void EnsureBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            bool loaderPresent = false;
            bool loaderIsFirst = false;

            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == LoaderScenePath)
                {
                    loaderPresent = true;
                    loaderIsFirst = (i == 0);
                    break;
                }
            }

            if (!loaderPresent)
            {
                var newList = new EditorBuildSettingsScene[scenes.Length + 1];
                newList[0] = new EditorBuildSettingsScene(LoaderScenePath, true);
                for (int i = 0; i < scenes.Length; i++)
                    newList[i + 1] = scenes[i];
                EditorBuildSettings.scenes = newList;
                Debug.Log("[BuildLoaderSceneTool] Loader.unity added as first scene in Build Settings");
            }
            else if (!loaderIsFirst)
            {
                Debug.Log("[BuildLoaderSceneTool] Loader.unity already in Build Settings (not first — move manually if needed)");
            }
        }
    }
}
#endif
