#if UNITY_EDITOR
#nullable enable
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using CrowdDefense.Systems;
using CrowdDefense.Visual;
using CrowdDefense.UI;

namespace CrowdDefense.Editor
{
    public static class BuildMainSceneTool
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Tools/CrowdDefense/Build Main Scene")]
        public static void BuildMainScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            int created = 0;
            int existing = 0;

            var systems = EnsureRootGO("Systems", ref existing);

            EnsureChild<PathManager>(systems, "PathManager", ref created, ref existing);
            EnsureChild<LevelRunner>(systems, "LevelRunner", ref created, ref existing);
            EnsureChild<WaveManager>(systems, "WaveManager", ref created, ref existing);
            EnsureChild<Economy>(systems, "Economy", ref created, ref existing);
            EnsureChild<PlacementController>(systems, "PlacementController", ref created, ref existing);
            EnsureChild<EnemyPool>(systems, "EnemyPool", ref created, ref existing);
            EnsureChild<ProjectilePool>(systems, "ProjectilePool", ref created, ref existing);
            EnsureChild<SlowEffectManager>(systems, "SlowEffectManager", ref created, ref existing);
            EnsureChild<CoinPullManager>(systems, "CoinPullManager", ref created, ref existing);
            EnsureChild<Synergies>(systems, "Synergies", ref created, ref existing);
            EnsureChild<SettingsRegistry>(systems, "SettingsRegistry", ref created, ref existing);

            EnsureJuiceFX(systems, ref created, ref existing);
            EnsureAudioController(systems, ref created, ref existing);
            EnsureVfxPool(systems, ref created, ref existing);

            EnsureCamera(ref created, ref existing);
            EnsureDirectionalLight(ref created, ref existing);
            EnsureHUD(ref created, ref existing);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[BuildMainSceneTool] Built/Updated Main.unity with {created} new + {existing} existing untouched");
        }

        private static GameObject EnsureRootGO(string name, ref int existing)
        {
            var go = GameObject.Find(name);
            if (go != null) { existing++; return go; }
            go = new GameObject(name);
            return go;
        }

        private static T EnsureChild<T>(GameObject parent, string name, ref int created, ref int existing) where T : Component
        {
            var child = parent.transform.Find(name);
            if (child == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent.transform, false);
                child = go.transform;
                created++;
            }
            else
            {
                existing++;
            }

            var comp = child.GetComponent<T>();
            if (comp == null)
                comp = child.gameObject.AddComponent<T>();
            return comp;
        }

        private static void EnsureJuiceFX(GameObject parent, ref int created, ref int existing)
        {
            EnsureChild<JuiceFX>(parent, "JuiceFX", ref created, ref existing);
        }

        private static void EnsureAudioController(GameObject parent, ref int created, ref int existing)
        {
            var ac = EnsureChild<AudioController>(parent, "AudioController", ref created, ref existing);

            var registry = AssetDatabase.LoadAssetAtPath<CrowdDefense.Data.AudioClipRegistry>(
                "Assets/ScriptableObjects/Audio/AudioClipRegistry.asset");
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/MixerGroups.mixer");

            var so = new SerializedObject(ac);
            if (registry != null)
                so.FindProperty("registry").objectReferenceValue = registry;
            if (mixer != null)
                so.FindProperty("mixer").objectReferenceValue = mixer;
            so.ApplyModifiedProperties();
        }

        private static void EnsureVfxPool(GameObject parent, ref int created, ref int existing)
        {
            // VfxPool generates procedural ParticleSystem prefabs at runtime if Inspector
            // refs are null. Leave fields empty — runtime BuildProceduralPrefab handles it.
            EnsureChild<VfxPool>(parent, "VfxPool", ref created, ref existing);
        }

        private static void EnsureCamera(ref int created, ref int existing)
        {
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null) { existing++; return; }

            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();

            go.transform.position = new Vector3(0, 18, -12);
            go.transform.eulerAngles = new Vector3(55, 0, 0);
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = Color.black;

            created++;
        }

        private static void EnsureDirectionalLight(ref int created, ref int existing)
        {
            var allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in allLights)
            {
                if (l.type == LightType.Directional) { existing++; return; }
            }

            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            created++;
        }

        private static void EnsureHUD(ref int created, ref int existing)
        {
            var hudGO = GameObject.Find("HUD");
            if (hudGO != null) { existing++; return; }

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/HUDPanelSettings.asset");
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/HUD.uxml");

            hudGO = new GameObject("HUD");
            var doc = hudGO.AddComponent<UIDocument>();

            if (panelSettings != null || visualTree != null)
            {
                var so = new SerializedObject(doc);
                if (panelSettings != null)
                    so.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
                if (visualTree != null)
                    so.FindProperty("sourceAsset").objectReferenceValue = visualTree;
                so.ApplyModifiedProperties();
            }

            if (hudGO.GetComponent<HudController>() == null)
                hudGO.AddComponent<HudController>();

            created++;
        }
    }
}
#endif
