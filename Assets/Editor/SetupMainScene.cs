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
            EnsurePanelSettingsTheme();

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
            // Generate (or refresh) the 23 PerkDef SOs + PerkSetBonusDef SOs and wire them into
            // PerkRegistry in one idempotent pass BEFORE the generic EnsureRegistry call below
            // (which would create an empty PerkRegistry.asset if none existed).
            BuildPerkAssets.Generate();

            // Seed Cutscene / Event / Modifier assets from V5 data (idempotent).
            BuildCutsceneAssets.Generate();
            BuildEventAssets.Generate();
            BuildModifierAssets.Generate();

            // Seed Doctrine / Skin / Achievement assets (idempotent).
            BuildDoctrineAssets.Generate();
            BuildSkinAssets.Generate();
            BuildAchievementAssets.Generate();

            // Seed EnemyType SOs from V5 ENEMY_TYPES (idempotent — skips existing assets).
            EnemySeedTool.Generate();

            int n = 0;
            n += EnsureRegistry<PerkRegistry>("Assets/Resources/PerkRegistry.asset");
            n += EnsureRegistry<MetaUpgradeRegistry>("Assets/Resources/MetaUpgradeRegistry.asset");
            n += EnsureRegistry<DoctrineRegistry>("Assets/Resources/DoctrineRegistry.asset");
            n += EnsureRegistry<SkinRegistry>("Assets/Resources/SkinRegistry.asset");
            n += EnsureRegistry<TowerRegistry>("Assets/Resources/TowerRegistry.asset");
            n += EnsureRegistry<EnemyRegistry>("Assets/Resources/EnemyRegistry.asset");
            n += EnsureRegistry<CutsceneRegistry>("Assets/Resources/CutsceneRegistry.asset");
            n += EnsureRegistry<EventRegistry>("Assets/Resources/EventRegistry.asset");
            n += EnsureRegistry<ModifierRegistry>("Assets/Resources/ModifierRegistry.asset");
            n += EnsureRegistry<AchievementRegistry>("Assets/Resources/AchievementRegistry.asset");
            // BossDefRegistry is not a type — BossSystem.registry is List<BossDef> wired via Inspector

            PopulatePerkRegistry();
            PopulateMetaUpgradeRegistry();
            PopulateDoctrineRegistry();
            PopulateSkinRegistry();
            PopulateAchievementRegistry();
            PopulateCutsceneRegistry();
            PopulateEventRegistry();
            PopulateModifierRegistry();
            PopulateEnemyRegistry();

            // Wire TowerType / EnemyType / HeroType assetKey → GLTF key, then rebuild AssetRegistry entries
            BuildAssetRegistryMappings.Generate();

            AssetDatabase.SaveAssets();
            return n;
        }

        private static void EnsurePanelSettingsTheme()
        {
            const string panelPath = "Assets/UI/HUDPanelSettings.asset";
            const string tssPath = "Assets/Resources/UI/UnityDefaultRuntimeTheme.tss";

            try
            {
                AssetDatabase.ImportAsset(tssPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                var panel = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>(panelPath);
                if (panel == null) { Debug.LogError($"[SetupMainScene] PanelSettings not found at {panelPath}"); return; }

                var tss = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.ThemeStyleSheet>(tssPath);
                if (tss == null) { Debug.LogError($"[SetupMainScene] ThemeStyleSheet not found at {tssPath}"); return; }

                var so = new SerializedObject(panel);
                var prop = so.FindProperty("themeUss");
                if (prop == null) { Debug.LogError("[SetupMainScene] PanelSettings.themeUss property not found"); return; }

                if (prop.objectReferenceValue == tss) { Debug.Log("[SetupMainScene] PanelSettings.themeUss already correctly set"); return; }

                prop.objectReferenceValue = tss;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(panel);
                AssetDatabase.SaveAssets();
                Debug.Log("[SetupMainScene] HUDPanelSettings.themeUss assigned to UnityDefaultRuntimeTheme.tss (now in Resources/)");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SetupMainScene] EnsurePanelSettingsTheme failed: {ex.Message}");
            }
        }

        private static void PopulatePerkRegistry()
        {
            const string registryPath = "Assets/Resources/PerkRegistry.asset";
            var reg = AssetDatabase.LoadAssetAtPath<PerkRegistry>(registryPath);
            if (reg == null) return;

            var so = new SerializedObject(reg);
            PopulateRegistryArray(so, "standard",    "t:PerkDef",        new[] { "Assets/ScriptableObjects/Perks/Standard" });
            PopulateRegistryArray(so, "schoolPerks", "t:PerkDef",        new[] { "Assets/ScriptableObjects/Perks/School" });
            PopulateRegistryArray(so, "setBonuses",  "t:PerkSetBonusDef",new[] { "Assets/ScriptableObjects/Perks/SetBonus" });
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(reg);
        }

        private static void PopulateRegistryArray(SerializedObject so, string propName, string filter, string[] searchFolders)
        {
            var guids = AssetDatabase.FindAssets(filter, searchFolders);
            if (guids.Length == 0) return;

            var prop = so.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogWarning($"[SetupMainScene] PerkRegistry.{propName} not found");
                return;
            }

            prop.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
            {
                var path  = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = asset;
            }
        }

        private static void PopulateMetaUpgradeRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<MetaUpgradeRegistry>("Assets/Resources/MetaUpgradeRegistry.asset");
            if (registry == null) return;

            var guids = AssetDatabase.FindAssets("t:MetaUpgradeDef", new[] { "Assets/ScriptableObjects/MetaUpgrades" });
            if (guids.Length == 0)
            {
                MetaUpgradeSeedTool.SeedAll();
                guids = AssetDatabase.FindAssets("t:MetaUpgradeDef", new[] { "Assets/ScriptableObjects/MetaUpgrades" });
            }
            if (guids.Length > 0)
                PopulateList(registry, "defs", guids);
        }

        private static void PopulateDoctrineRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<DoctrineRegistry>("Assets/Resources/DoctrineRegistry.asset");
            if (registry == null) return;

            var guids = AssetDatabase.FindAssets("t:DoctrineDef", new[] { "Assets/ScriptableObjects/Doctrines" });
            if (guids.Length > 0)
                PopulateList(registry, "defs", guids);
        }

        private static void PopulateSkinRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<SkinRegistry>("Assets/Resources/SkinRegistry.asset");
            if (registry == null) return;

            var guids = AssetDatabase.FindAssets("t:SkinDef", new[] { "Assets/ScriptableObjects/Skins" });
            if (guids.Length > 0)
                PopulateList(registry, "skins", guids);
        }

        private static void PopulateAchievementRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<AchievementRegistry>("Assets/Resources/AchievementRegistry.asset");
            if (registry == null) return;

            var guids = AssetDatabase.FindAssets("t:AchievementDef", new[] { "Assets/ScriptableObjects/Achievements" });
            if (guids.Length == 0)
            {
                BuildAchievementAssets.Generate();
                guids = AssetDatabase.FindAssets("t:AchievementDef", new[] { "Assets/ScriptableObjects/Achievements" });
            }
            if (guids.Length > 0)
                PopulateList(registry, "defs", guids);
        }

        private static void PopulateCutsceneRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CutsceneRegistry>("Assets/Resources/CutsceneRegistry.asset");
            if (registry == null) return;

            var guids = AssetDatabase.FindAssets("t:CutsceneDef", new[] { "Assets/ScriptableObjects/Cutscenes" });
            if (guids.Length > 0)
                PopulateList(registry, "cutscenes", guids);
        }

        private static void PopulateEventRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<EventRegistry>("Assets/Resources/EventRegistry.asset");
            if (registry == null) return;

            var guids = AssetDatabase.FindAssets("t:EventDef", new[] { "Assets/ScriptableObjects/Events" });
            if (guids.Length > 0)
                PopulateList(registry, "events", guids);
        }

        private static void PopulateModifierRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<ModifierRegistry>("Assets/Resources/ModifierRegistry.asset");
            if (registry == null) return;

            var guids = AssetDatabase.FindAssets("t:ModifierDef", new[] { "Assets/ScriptableObjects/Modifiers" });
            if (guids.Length > 0)
                PopulateList(registry, "modifiers", guids);
        }

        private static void PopulateEnemyRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<EnemyRegistry>("Assets/Resources/EnemyRegistry.asset");
            if (registry == null) return;

            var guids = AssetDatabase.FindAssets("t:EnemyType", new[] { "Assets/ScriptableObjects/Enemies" });
            if (guids.Length > 0)
                PopulateList(registry, "enemies", guids);
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
            EnsureChild<Achievements>(systems, "Achievements", ref created, ref existing);
            EnsureChild<ComboSystem>(systems, "ComboSystem", ref created, ref existing);
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
            c += EnsureComponent<RuntimeThemeFixup>(hud);
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
            c += EnsureComponent<ToastController>(hud);
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
            AssetDatabase.SaveAssets();
            WireTowerToolbar();
            AssetDatabase.SaveAssets();
            WireBossSystem();
            AssetDatabase.SaveAssets();
            WireEnemyPool();
            AssetDatabase.SaveAssets();
        }

        private static void WireLevelRunner()
        {
            var lr = Object.FindFirstObjectByType<LevelRunner>();
            if (lr == null) { Debug.LogError("[SetupMainScene] LevelRunner not found in scene"); return; }

            var so   = new SerializedObject(lr);
            var hero = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hero.prefab");
            var ht   = AssetDatabase.LoadAssetAtPath<HeroType>("Assets/ScriptableObjects/Heroes/Knight.asset");

            var heroPrefabProp = so.FindProperty("heroPrefab");
            var heroTypeProp   = so.FindProperty("heroType");

            if (heroPrefabProp == null)
                Debug.LogError("[SetupMainScene] LevelRunner.heroPrefab field not found — check field name in LevelRunner.cs");
            else if (hero == null)
                Debug.LogError("[SetupMainScene] Assets/Prefabs/Hero.prefab not found");
            else
            {
                heroPrefabProp.objectReferenceValue = hero;
                Debug.Log("[SetupMainScene] wired LevelRunner.heroPrefab");
            }

            if (heroTypeProp == null)
                Debug.LogError("[SetupMainScene] LevelRunner.heroType field not found — check field name in LevelRunner.cs");
            else if (ht == null)
                Debug.LogError("[SetupMainScene] Assets/ScriptableObjects/Heroes/Knight.asset not found");
            else
            {
                heroTypeProp.objectReferenceValue = ht;
                Debug.Log("[SetupMainScene] wired LevelRunner.heroType");
            }

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

        private static void WireEnemyPool()
        {
            var ep = Object.FindFirstObjectByType<EnemyPool>();
            if (ep == null) { Debug.LogError("[SetupMainScene] EnemyPool not found in scene"); return; }

            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy.prefab");
            if (enemyPrefab == null)
            {
                Debug.LogError("[SetupMainScene] Assets/Prefabs/Enemies/Enemy.prefab not found");
                return;
            }

            var so = new SerializedObject(ep);
            var prop = so.FindProperty("basePrefab");
            if (prop == null)
            {
                Debug.LogError("[SetupMainScene] EnemyPool.basePrefab field not found — check field name in EnemyPool.cs");
                return;
            }

            prop.objectReferenceValue = enemyPrefab;
            so.ApplyModifiedProperties();
            Debug.Log("[SetupMainScene] wired EnemyPool.basePrefab");
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
