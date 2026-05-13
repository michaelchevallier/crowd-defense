#if UNITY_EDITOR
#nullable enable
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
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

            // Phase 3 singletons — idempotent via EnsureSingleton
            EnsureSingleton<HeroProjectilePool>(systems, "HeroProjectilePool", ref created, ref existing);
            EnsureSingleton<SkinSystem>        (systems, "SkinSystem",         ref created, ref existing);
            EnsureSingleton<RunContext>         (systems, "RunContext",          ref created, ref existing);
            // LevelEvents is a static event hub — no MonoBehaviour, skip EnsureSingleton.
            EnsureSingleton<BossSystem>         (systems, "BossSystem",          ref created, ref existing);
            EnsureSingleton<CrowdDefense.Systems.EventSystem>(systems, "EventSystem", ref created, ref existing);
            EnsureSingleton<Achievements>       (systems, "Achievements",        ref created, ref existing);
            EnsureSingleton<ComboSystem>        (systems, "ComboSystem",         ref created, ref existing);
            EnsureSingleton<FloatingPopupController>(systems, "FloatingPopupController", ref created, ref existing);
            EnsureSingleton<RunMap>             (systems, "RunMap",              ref created, ref existing);
            EnsureSingleton<MetaUpgradeSystem>  (systems, "MetaUpgradeSystem",   ref created, ref existing);
            EnsureSingleton<DoctrineSystem>     (systems, "DoctrineSystem",      ref created, ref existing);
            // SchoolRegistry is a static registry — no MonoBehaviour, skip EnsureSingleton.
            EnsureSingleton<TutorialState>      (systems, "TutorialState",       ref created, ref existing);
            EnsureSingleton<StatsTracker>       (systems, "StatsTracker",        ref created, ref existing);

            // Audio stack
            EnsureSingleton<MusicManager>       (systems, "MusicManager",        ref created, ref existing);
            EnsureSingleton<AudioMixerController>(systems, "AudioMixerController", ref created, ref existing);
            EnsureSingleton<AudioSourcePool>    (systems, "AudioSourcePool",     ref created, ref existing);

            // Event hubs
            EnsureSingleton<EventManager>       (systems, "EventManager",        ref created, ref existing);
            EnsureSingleton<DynamicEventManager>(systems, "DynamicEventManager", ref created, ref existing);
            EnsureSingleton<EventBridge>        (systems, "EventBridge",         ref created, ref existing);

            // Wave / progression
            EnsureSingleton<WaveHistoryLog>     (systems, "WaveHistoryLog",      ref created, ref existing);
            EnsureSingleton<WaveRewardSpawner>  (systems, "WaveRewardSpawner",   ref created, ref existing);
            EnsureSingleton<KillsPerWaveTracker>(systems, "KillsPerWaveTracker", ref created, ref existing);

            // Meta progression & profile
            EnsureSingleton<PerkSystem>         (systems, "PerkSystem",          ref created, ref existing);
            EnsureSingleton<PlayerProfile>      (systems, "PlayerProfile",       ref created, ref existing);
            EnsureSingleton<HighScores>         (systems, "HighScores",          ref created, ref existing);
            EnsureSingleton<LifetimeStats>      (systems, "LifetimeStats",       ref created, ref existing);
            EnsureSingleton<Bestiary>           (systems, "Bestiary",            ref created, ref existing);
            EnsureSingleton<DailyChallenge>     (systems, "DailyChallenge",      ref created, ref existing);
            EnsureSingleton<KeyBindings>        (systems, "KeyBindings",         ref created, ref existing);

            // Gameplay subsystems
            EnsureSingleton<LootSpawner>        (systems, "LootSpawner",         ref created, ref existing);
            EnsureSingleton<TreasureSpawner>    (systems, "TreasureSpawner",     ref created, ref existing);
            EnsureSingleton<BluePill>           (systems, "BluePill",            ref created, ref existing);
            EnsureSingleton<EnemyPathingSystem> (systems, "EnemyPathingSystem",  ref created, ref existing);
            EnsureSingleton<HiddenAchievementTracker>(systems, "HiddenAchievementTracker", ref created, ref existing);

            // Game modes
            EnsureSingleton<EndlessMode>        (systems, "EndlessMode",         ref created, ref existing);
            EnsureSingleton<BossRushMode>       (systems, "BossRushMode",        ref created, ref existing);

            // Visual / map / weather
            EnsureSingleton<MapRenderer>        (systems, "MapRenderer",         ref created, ref existing);
            EnsureSingleton<LevelVisualBridge>  (systems, "LevelVisualBridge",   ref created, ref existing);
            EnsureSingleton<FogOfWar>           (systems, "FogOfWar",            ref created, ref existing);
            EnsureSingleton<WeatherController>  (systems, "WeatherController",   ref created, ref existing);
            EnsureSingleton<SkyboxController>   (systems, "SkyboxController",    ref created, ref existing);
            EnsureSingleton<PostProcessController>(systems, "PostProcessController", ref created, ref existing);
            // TODO: MapPathfinder (needs nav mesh config), SceneTransition (needs animator refs)

            EnsureCastle(ref created, ref existing);
            EnsureCamera(ref created, ref existing);
            EnsureDirectionalLight(ref created, ref existing);
            EnsureChild<ThemeAmbientController>(systems, "ThemeAmbientController", ref created, ref existing);
            EnsureDebugGround(ref created, ref existing);
            EnsureHUD(ref created, ref existing);
            EnsureSkyboxAndLighting();

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

        // Returns existing component or creates new child GO + component under parent.
        // Idempotent: if GO already in hierarchy it is reused.
        private static T EnsureSingleton<T>(GameObject parent, string goName, ref int created, ref int existing) where T : Component
        {
            var existing_ = Object.FindFirstObjectByType<T>();
            if (existing_ != null) { existing++; return existing_; }
            var go = new GameObject(goName);
            go.transform.SetParent(parent.transform, false);
            created++;
            return go.AddComponent<T>();
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

        private static void EnsureCastle(ref int created, ref int existing)
        {
            var go = GameObject.Find("Castle");
            if (go != null)
            {
                if (go.GetComponent<Castle>() == null)
                    go.AddComponent<Castle>();
                existing++;
                return;
            }

            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Castle";
            go.transform.position = Vector3.zero;

            // Destroy collider generated by CreatePrimitive — Castle uses HP bar, not physics
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());

            var rend = go.GetComponent<MeshRenderer>();
            var mat  = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.55f, 0.55f, 0.65f); // stone grey
            rend.sharedMaterial = mat;

            go.AddComponent<Castle>();
            created++;
        }

        private static void EnsureCamera(ref int created, ref int existing)
        {
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                // Ensure CameraController is present even on pre-existing camera
                if (cam.GetComponent<CrowdDefense.Visual.CameraController>() == null)
                    cam.gameObject.AddComponent<CrowdDefense.Visual.CameraController>();
                existing++;
                return;
            }

            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            go.AddComponent<CrowdDefense.Visual.CameraController>();

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

            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.color = new Color(1f, 0.95f, 0.9f);
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            created++;
        }

        private static void EnsureDebugGround(ref int created, ref int existing)
        {
            // Skip DebugGround entirely — MapRenderer builds per-cell slabs at runtime.
            // A leftover "DebugGround" object would cover the entire map with a flat green plane.
            var ground = GameObject.Find("DebugGround");
            if (ground != null)
            {
                // Destroy any stale DebugGround that was created before this fix.
                Object.DestroyImmediate(ground);
#if UNITY_EDITOR
                Debug.Log("[BuildMainSceneTool] Removed stale DebugGround (MapRenderer handles ground)");
#endif
            }
            existing++; // count as existing/handled — no new object created
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

        // ── Skybox & Ambient Lighting ────────────────────────────────────────

        public static void EnsureSkyboxAndLighting()
        {
            // Unity built-in procedural skybox (always present, no import required)
            var skyboxMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            if (skyboxMat != null)
                RenderSettings.skybox = skyboxMat;

            RenderSettings.ambientMode      = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.2f;

            // Default (Plaine) — neutral daylight tint
            RenderSettings.ambientLight = ThemeAmbientColor(LevelTheme.Plaine);

            DynamicGI.UpdateEnvironment();
        }

        // Applies per-theme ambient tint to RenderSettings.ambientLight.
        // Call at runtime when a level is loaded to tint the scene to match the theme.
        public static void ApplyThemeAmbient(LevelTheme theme)
        {
            RenderSettings.ambientLight = ThemeAmbientColor(theme);
            DynamicGI.UpdateEnvironment();
        }

        private static Color ThemeAmbientColor(LevelTheme theme) => theme switch
        {
            LevelTheme.Foret      => new Color(0.45f, 0.60f, 0.38f),  // vert forêt
            LevelTheme.Desert     => new Color(0.80f, 0.68f, 0.42f),  // sable chaud
            LevelTheme.Volcan     => new Color(0.70f, 0.28f, 0.10f),  // rouge lave
            LevelTheme.Apocalypse => new Color(0.35f, 0.28f, 0.25f),  // gris cendres
            LevelTheme.Espace     => new Color(0.10f, 0.10f, 0.25f),  // bleu nuit profond
            LevelTheme.Submarin   => new Color(0.10f, 0.35f, 0.55f),  // bleu-vert sous-marin
            LevelTheme.Medieval   => new Color(0.52f, 0.48f, 0.38f),  // ocre pierres
            LevelTheme.Cyberpunk  => new Color(0.15f, 0.08f, 0.35f),  // violet néon sombre
            LevelTheme.Foire      => new Color(0.72f, 0.55f, 0.20f),  // jaune fête foraine
            _                     => new Color(0.55f, 0.62f, 0.70f),  // Plaine — bleu ciel
        };
    }
}
#endif
