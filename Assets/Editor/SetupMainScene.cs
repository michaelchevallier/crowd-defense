#if UNITY_EDITOR
#nullable enable
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.UI;

namespace CrowdDefense.Editor
{
    public static class SetupMainScene
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Tools/CrowdDefense/Setup Main Scene")]
        public static void Run()
        {
            int regs = EnsureRegistries();

            // BuildMainSceneTool opens its own scene and saves — must run FIRST,
            // before our additions, because it re-opens the scene (would discard in-memory mods).
            BuildMainSceneTool.BuildMainScene();

            // NOW re-open the freshly-saved scene to add our extras
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Purge any orphaned [Auto] GOs left over from previous play mode auto-create fallbacks
            foreach (var rootGO in scene.GetRootGameObjects())
            {
                if (rootGO.name.StartsWith("[Auto]"))
                    Object.DestroyImmediate(rootGO);
            }

            int sys  = EnsureNewSingletons();
            int ui   = EnsureHudControllers();
            WireInspectorRefs();

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SetupMainScene] OK — {sys} singletons, {ui} UI controllers, {regs} registries created/updated. SaveScene={saved}");
        }

        // ── 1. Registries SO ────────────────────────────────────────────────

        private static int EnsureRegistries()
        {
            int n = 0;
            n += EnsureRegistry<PerkRegistry>("Assets/Resources/PerkRegistry.asset");
            n += EnsureRegistry<MetaUpgradeRegistry>("Assets/Resources/MetaUpgradeRegistry.asset");
            n += EnsureRegistry<DoctrineRegistry>("Assets/Resources/DoctrineRegistry.asset");
            n += EnsureRegistry<SkinRegistry>("Assets/Resources/SkinRegistry.asset");
            n += EnsureRegistry<TowerRegistry>("Assets/Resources/TowerRegistry.asset");
            n += EnsureRegistry<CutsceneRegistry>("Assets/Resources/CutsceneRegistry.asset");
            // BossDefRegistry is not a type — BossSystem.registry is List<BossDef> wired via Inspector
            AssetDatabase.SaveAssets();
            return n;
        }

        private static int EnsureRegistry<T>(string path) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return 0;
            System.IO.Directory.CreateDirectory("Assets/Resources");
            var so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
            return 1;
        }

        // ── 2. New Singletons under Systems/ ────────────────────────────────

        private static int EnsureNewSingletons()
        {
            var systems = GameObject.Find("Systems") ?? new GameObject("Systems");
            int created = 0, existing = 0;
            EnsureChild<PerkSystem>(systems, "PerkSystem", ref created, ref existing);
            EnsureChild<BossSystem>(systems, "BossSystem", ref created, ref existing);
            EnsureChild<MetaUpgradeSystem>(systems, "MetaUpgradeSystem", ref created, ref existing);
            EnsureChild<DoctrineSystem>(systems, "DoctrineSystem", ref created, ref existing);
            EnsureChild<SkinSystem>(systems, "SkinSystem", ref created, ref existing);
            EnsureChild<RunContext>(systems, "RunContext", ref created, ref existing);
            return created;
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

        // ── 3. UI Controllers on HUD GO ──────────────────────────────────────

        private static int EnsureHudControllers()
        {
            var hud = GameObject.Find("HUD");
            if (hud == null)
            {
                Debug.LogError("[SetupMainScene] HUD GO missing — run BuildMainScene first");
                return 0;
            }
            int c = 0;
            c += EnsureComponent<TowerToolbarController>(hud);
            c += EnsureComponent<DoctrineController>(hud);
            c += EnsureComponent<ShopController>(hud);
            c += EnsureComponent<BossUI>(hud);
            c += EnsureComponent<MinimapController>(hud);
            c += EnsureComponent<DebugHudController>(hud);
            c += EnsureComponent<PerkPickerController>(hud);
            c += EnsureComponent<HudPerkBadges>(hud);
            c += EnsureComponent<SkinPickerController>(hud);
            c += EnsureComponent<CutsceneController>(hud);
            c += EnsureComponent<WorldMapController>(hud);
            c += EnsureComponent<WavePreviewController>(hud);
            c += EnsureComponent<AchievementToastController>(hud);
            c += EnsureComponent<ComboHudController>(hud);
            c += EnsureComponent<SpeedControlController>(hud);
            c += EnsureComponent<TutorialOverlayController>(hud);
            // PerkChoiceOverlay is Canvas-based — needs a separate GO, not added here
            return c;
        }

        private static int EnsureComponent<T>(GameObject go) where T : Component
        {
            if (go.GetComponent<T>() != null) return 0;
            go.AddComponent<T>();
            return 1;
        }

        // ── 4. Inspector Refs ─────────────────────────────────────────────────

        private static void WireInspectorRefs()
        {
            WireLevelRunner();
            WireTowerToolbar();
            WireBossSystem();
        }

        private static void WireLevelRunner()
        {
            var lr = Object.FindFirstObjectByType<LevelRunner>();
            if (lr == null) return;

            var so   = new SerializedObject(lr);
            var hero = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hero.prefab");
            var ht   = AssetDatabase.LoadAssetAtPath<HeroType>("Assets/ScriptableObjects/Heroes/Knight.asset");

            var heroPrefabProp = so.FindProperty("heroPrefab");
            var heroTypeProp   = so.FindProperty("heroType");

            if (heroPrefabProp == null)
                Debug.LogWarning("[SetupMainScene] LevelRunner.heroPrefab field not found — field renamed?");
            else if (hero != null)
                heroPrefabProp.objectReferenceValue = hero;
            else
                Debug.LogWarning("[SetupMainScene] Assets/Prefabs/Hero.prefab not found — skipping heroPrefab wiring");

            if (heroTypeProp == null)
                Debug.LogWarning("[SetupMainScene] LevelRunner.heroType field not found — field renamed?");
            else if (ht != null)
                heroTypeProp.objectReferenceValue = ht;

            so.ApplyModifiedProperties();
        }

        private static void WireTowerToolbar()
        {
            var tt = Object.FindFirstObjectByType<TowerToolbarController>();
            if (tt == null) return;

            var registry = AssetDatabase.LoadAssetAtPath<TowerRegistry>("Assets/Resources/TowerRegistry.asset");
            if (registry == null) return;

            // Populate TowerRegistry.towers from existing TowerType assets if empty
            var guids = AssetDatabase.FindAssets("t:TowerType", new[] { "Assets/ScriptableObjects/Towers" });
            if (guids.Length > 0)
                PopulateList(registry, "towers", guids);

            var so   = new SerializedObject(tt);
            var prop = so.FindProperty("towerRegistry");
            if (prop == null)
            {
                Debug.LogWarning("[SetupMainScene] TowerToolbarController.towerRegistry field not found");
                return;
            }
            prop.objectReferenceValue = registry;
            so.ApplyModifiedProperties();
        }

        private static void WireBossSystem()
        {
            var bs = Object.FindFirstObjectByType<BossSystem>();
            if (bs == null) return;

            var guids = AssetDatabase.FindAssets("t:BossDef", new[] { "Assets/ScriptableObjects/Bosses" });
            if (guids.Length == 0) return;

            PopulateList(bs, "registry", guids);
        }

        private static void PopulateList(Object target, string fieldName, string[] guids)
        {
            var so   = new SerializedObject(target);
            var list = so.FindProperty(fieldName);
            if (list == null)
            {
                Debug.LogWarning($"[SetupMainScene] {target.GetType().Name}.{fieldName} not found");
                return;
            }

            // Skip re-population if count already matches (avoids overwriting manual additions)
            if (list.arraySize == guids.Length) return;

            list.ClearArray();
            for (int i = 0; i < guids.Length; i++)
            {
                var path  = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                list.InsertArrayElementAtIndex(i);
                list.GetArrayElementAtIndex(i).objectReferenceValue = asset;
            }
            so.ApplyModifiedProperties();
        }
    }
}
#endif
