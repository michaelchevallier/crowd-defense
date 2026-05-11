# Spec — `SetupMainScene` Editor Tool (post-12-agent merge wiring)

## 1. Contexte

Le merge de la vague de 12 agents a introduit 7 nouveaux systèmes singletons et 11
nouveaux contrôleurs UI qui doivent vivre dans `Main.unity`. Aucun de ces composants
n'est aujourd'hui présent dans la scène : `MonoSingleton<T>.Instance` retombe sur
le fallback "auto-create GameObject" (cf. `Assets/Scripts/Common/MonoSingleton.cs:14-26`)
qui logue un warning et crée un GO orphelin sans configuration Inspector.

De plus, certaines références Inspector clés sont vides après merge :
`LevelRunner.heroPrefab/heroType`, `BossSystem.registry`, `TowerToolbarController.towerRegistry`,
et plusieurs registries SO `Resources/*Registry.asset` n'existent pas encore sur disque
(`Assets/Resources/` ne contient que `BalanceConfig`, `LevelRegistry`, `AssetRegistry`,
`LevelThemeMaterialConfig`, `UnityGLTFSettings`).

`BuildMainSceneTool.cs` existant gère déjà la moitié basse (PathManager / Economy / HUD)
mais ignore les nouveaux modules. Ce ticket étend ce pattern via un fichier **séparé**
pour limiter le diff et garder l'historique git lisible.

## 2. Fichiers impactés

| Chemin | Rôle | Lignes |
|---|---|---|
| `/Users/mike/Work/crowd-defense/Assets/Editor/SetupMainScene.cs` | **nouveau** — tool batch wiring complet | 1-~240 |
| `/Users/mike/Work/crowd-defense/Assets/Editor/BuildMainSceneTool.cs` | référence pattern (lecture seule) | 1-176 |
| `/Users/mike/Work/crowd-defense/Assets/Scripts/Common/MonoSingleton.cs` | comportement Instance auto-create | 13-27 |
| `/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/LevelRunner.cs` | slots heroPrefab/heroType à wirer | 17-23 |
| `/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/BossSystem.cs` | slot `registry: List<BossDef>` Inspector | 12 |
| `/Users/mike/Work/crowd-defense/Assets/Scripts/UI/TowerToolbarController.cs` | slot `towerRegistry` | 14 |
| `/Users/mike/Work/crowd-defense/Assets/Scripts/Data/*Registry.cs` | API `.Get()`/`.Load()` (Resources/*) | divers |
| `/Users/mike/Work/crowd-defense/Assets/Resources/` | cible des registries à créer si manquants | — |

## 3. Comportement attendu

L'utilisateur ouvre Unity Editor, clique **Tools > CrowdDefense > Setup Main Scene**,
et la scène `Main.unity` est wirée complètement en < 3 sec :

1. Les registries SO manquants dans `Assets/Resources/` (`PerkRegistry`, `MetaUpgradeRegistry`,
   `DoctrineRegistry`, `SkinRegistry`, `TowerRegistry`, `BossDefRegistry`, `CutsceneRegistry`)
   sont créés vides via `ScriptableObject.CreateInstance` + `AssetDatabase.CreateAsset`.
   Si l'asset existe déjà, on le réutilise (idempotence).
2. Sous `Systems/`, 7 GameObjects enfants sont créés et reçoivent leur composant singleton :
   `PerkSystem`, `BossSystem`, `MetaUpgradeSystem`, `DoctrineSystem`, `SkinSystem`,
   `RunContext`, `CutsceneRegistryLoader` (helper qui force `CutsceneRegistry.Get()` au boot).
3. Le GO `HUD` (créé par `BuildMainSceneTool.EnsureHUD`) reçoit les 11 contrôleurs UI en
   composants additionnels sur le **même** GO (ils partagent `UIDocument` via `RequireComponent`).
4. Toutes les références Inspector sont wirées : `LevelRunner.heroPrefab`, `heroType`,
   `TowerToolbarController.towerRegistry`, `DoctrineController.registry` (si non-static),
   `BossSystem.registry` (list peuplée depuis `Assets/ScriptableObjects/Bosses/*.asset`).
5. Re-cliquer **Setup Main Scene** ne duplique aucun composant ni asset (idempotent).
6. La console log final : `[SetupMainScene] OK — X singletons, Y UI controllers, Z registries created/updated`.
7. La scène est marquée dirty et sauvée. Aucun fichier .meta n'est généré en doublon.
8. Si un asset prérequis manque (`Knight.asset`, `HUD.uxml`), log Warning mais continue.

## 4. Pseudo-code des fixes

### 4.1 Entry point + ordre

```csharp
[MenuItem("Tools/CrowdDefense/Setup Main Scene")]
public static void Run()
{
    var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
    int regs = EnsureRegistries();          // 1. Registries SO d'abord (UI les charge à Start)
    BuildMainSceneTool.BuildMainScene();    // 2. Délègue l'ossature legacy (Systems/PathManager etc.)
    int sys  = EnsureNewSingletons();       // 3. Singletons post-merge
    int ui   = EnsureHudControllers();      // 4. UI controllers sur GO HUD
    WireInspectorRefs();                    // 5. Wire heroPrefab, towerRegistry, BossSystem.registry
    EditorSceneManager.MarkSceneDirty(scene);
    EditorSceneManager.SaveScene(scene);
    Debug.Log($"[SetupMainScene] OK — {sys} singletons, {ui} UI controllers, {regs} registries");
}
```

### 4.2 EnsureRegistries (idempotent SO create)

```csharp
private static int EnsureRegistries()
{
    int n = 0;
    n += EnsureRegistry<PerkRegistry>("Assets/Resources/PerkRegistry.asset");
    n += EnsureRegistry<MetaUpgradeRegistry>("Assets/Resources/MetaUpgradeRegistry.asset");
    n += EnsureRegistry<DoctrineRegistry>("Assets/Resources/DoctrineRegistry.asset");
    n += EnsureRegistry<SkinRegistry>("Assets/Resources/SkinRegistry.asset");
    n += EnsureRegistry<TowerRegistry>("Assets/Resources/TowerRegistry.asset");
    n += EnsureRegistry<CutsceneRegistry>("Assets/Resources/CutsceneRegistry.asset");
    // BossDefRegistry n'existe pas comme type — BossSystem.registry est List<BossDef> Inspector
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
```

### 4.3 EnsureNewSingletons (réutilise `EnsureChild<T>` du legacy)

```csharp
private static int EnsureNewSingletons()
{
    var systems = GameObject.Find("Systems") ?? new GameObject("Systems");
    int c = 0, e = 0;
    EnsureChild<PerkSystem>(systems, "PerkSystem", ref c, ref e);
    EnsureChild<BossSystem>(systems, "BossSystem", ref c, ref e);
    EnsureChild<MetaUpgradeSystem>(systems, "MetaUpgradeSystem", ref c, ref e);
    EnsureChild<DoctrineSystem>(systems, "DoctrineSystem", ref c, ref e);
    EnsureChild<SkinSystem>(systems, "SkinSystem", ref c, ref e);
    EnsureChild<RunContext>(systems, "RunContext", ref c, ref e);
    return c;
}
```

Note : `EnsureChild<T>` est privé dans `BuildMainSceneTool` — soit le rendre `internal`,
soit dupliquer le helper (max 20 lignes, OK avec DRY pour Editor tooling).

### 4.4 EnsureHudControllers (tous sur le GO `HUD`)

```csharp
private static int EnsureHudControllers()
{
    var hud = GameObject.Find("HUD");
    if (hud == null) { Debug.LogError("[SetupMainScene] HUD GO missing — run BuildMainScene first"); return 0; }
    int c = 0;
    c += EnsureComponent<TowerToolbarController>(hud);
    c += EnsureComponent<DoctrineController>(hud);
    c += EnsureComponent<ShopController>(hud);
    c += EnsureComponent<BossUI>(hud);
    c += EnsureComponent<MinimapController>(hud);
    c += EnsureComponent<DebugHudController>(hud);
    c += EnsureComponent<PerkPickerController>(hud);
    c += EnsureComponent<HudPerkBadges>(hud);              // MonoBehaviour, pas UIDocument-required
    c += EnsureComponent<SkinPickerController>(hud);
    c += EnsureComponent<CutsceneController>(hud);
    // PerkChoiceOverlay → Canvas-based, GO séparé (cf. §7 risques)
    return c;
}
private static int EnsureComponent<T>(GameObject go) where T : Component
    => go.GetComponent<T>() != null ? 0 : (go.AddComponent<T>(), 1).Item2;
```

### 4.5 WireInspectorRefs (SerializedObject pattern)

```csharp
private static void WireInspectorRefs()
{
    // LevelRunner.heroPrefab + heroType
    var lr = Object.FindFirstObjectByType<LevelRunner>();
    if (lr != null)
    {
        var so = new SerializedObject(lr);
        var hero  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hero.prefab");
        var ht    = AssetDatabase.LoadAssetAtPath<HeroType>("Assets/ScriptableObjects/Heroes/Knight.asset");
        if (hero != null) so.FindProperty("heroPrefab").objectReferenceValue = hero;
        if (ht   != null) so.FindProperty("heroType").objectReferenceValue   = ht;
        so.ApplyModifiedProperties();
    }
    // TowerToolbarController.towerRegistry
    var tt = Object.FindFirstObjectByType<TowerToolbarController>();
    if (tt != null) AssignField(tt, "towerRegistry",
        AssetDatabase.LoadAssetAtPath<TowerRegistry>("Assets/Resources/TowerRegistry.asset"));
    // BossSystem.registry (List<BossDef>) — peuple depuis ScriptableObjects/Bosses/*.asset
    var bs = Object.FindFirstObjectByType<BossSystem>();
    if (bs != null) PopulateList(bs, "registry",
        AssetDatabase.FindAssets("t:BossDef", new[]{"Assets/ScriptableObjects/Bosses"}));
}
```

### 4.6 PopulateList helper (sérialisation List<T>)

```csharp
private static void PopulateList(Object target, string fieldName, string[] guids)
{
    var so   = new SerializedObject(target);
    var list = so.FindProperty(fieldName);
    list.ClearArray();
    for (int i = 0; i < guids.Length; i++)
    {
        var path = AssetDatabase.GUIDToAssetPath(guids[i]);
        var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        list.InsertArrayElementAtIndex(i);
        list.GetArrayElementAtIndex(i).objectReferenceValue = asset;
    }
    so.ApplyModifiedProperties();
}
```

## 5. Critères de succès

- [ ] Menu `Tools > CrowdDefense > Setup Main Scene` apparaît et clique en < 3 sec.
- [ ] Après run sur `Main.unity` vide : 7 GO enfants sous `Systems/` post-merge, 11 composants UI sur `HUD`, 6 assets dans `Assets/Resources/*Registry.asset`.
- [ ] Re-run : 0 nouveau composant ajouté, 0 nouvel asset, log "0 created" pour chaque catégorie.
- [ ] `LevelRunner` Inspector montre `heroPrefab=Hero.prefab` et `heroType=Knight.asset` non-vides (si présents sur disque).
- [ ] `TowerToolbarController.towerRegistry` non-vide (référence `Resources/TowerRegistry.asset`).
- [ ] `BossSystem.registry` contient 10 entrées (10 fichiers `Boss_W*.asset` détectés au glob).
- [ ] Play mode : aucun warning `[MonoSingleton] X auto-created` pour les 7 nouveaux singletons.
- [ ] Console build batch (`-executeMethod CrowdDefense.Editor.SetupMainScene.Run`) : exit code 0.
- [ ] FPS > 60 en Editor play mode après wiring (pas de régression vs avant).
- [ ] Diff git : 1 fichier nouveau (`SetupMainScene.cs`), 1 modif `Main.unity`, N nouveaux `.asset` dans `Resources/`.

## 6. Effort estimé

**4 commits atomiques**, ordre recommandé :

1. `feat(editor): SetupMainScene.cs squelette + EnsureRegistries (6 SO Resources/)`
   — Crée le fichier, le MenuItem, et la création idempotente des 6 registries vides.
   Vérif : menu apparaît + 6 assets Resources/ créés au premier run.
2. `feat(editor): SetupMainScene.EnsureNewSingletons (7 GO sous Systems/)`
   — Ajoute PerkSystem, BossSystem, MetaUpgradeSystem, DoctrineSystem, SkinSystem,
   RunContext + helper `EnsureChild<T>` (dupliqué ou rendu `internal` depuis BuildMainSceneTool).
3. `feat(editor): SetupMainScene.EnsureHudControllers (11 composants sur HUD GO)`
   — Tous les contrôleurs UI ajoutés sur le même GO que `UIDocument`/`HudController`.
4. `feat(editor): SetupMainScene.WireInspectorRefs (heroPrefab/heroType/registries)`
   — Wire des champs `[SerializeField]` via `SerializedObject` + `PopulateList` pour
   `BossSystem.registry`. Log final consolidé.

**Temps estimé** : 2h30 (skeleton 30min + singletons 30min + UI 30min + wiring 45min + tests Editor 15min).

## 7. Risques & mitigations

| Risque | Mitigation |
|---|---|
| `PerkChoiceOverlay` + `HudPerkBadges` utilisent UI Toolkit + UnityEngine.UI (Canvas) — pas compatible avec un GO UIDocument-only. | Vérifier `[RequireComponent]` : `HudPerkBadges` n'a pas `[RequireComponent(UIDocument)]` mais utilise `RectTransform`/`Text` legacy → garder sur HUD si compatibilité testée, sinon créer un sous-GO `HUDLegacyCanvas` avec `Canvas` + `CanvasScaler`. **Voir §3.4** : `PerkChoiceOverlay` exclu du HUD (Canvas-based, GO séparé). |
| `BossDefRegistry` n'existe pas comme type SO — `BossSystem` a un `List<BossDef> registry` Inspector. | Pas de `.asset` registry à créer ; on peuple la List Inspector via `PopulateList()` (cf. §4.6). |
| Modifier `Main.unity` via batch peut casser le fichier YAML si Unity n'est pas idle. | Toujours `OpenScene(Single)` au début et `SaveScene` à la fin — éviter `Additive`. |
| `MonoSingleton<T>.Awake()` détruit le second instance — si un GO `[Auto]` existe déjà depuis un play mode précédent, il sera duppe. | Nettoyer en début de run : `foreach (var go in scene.GetRootGameObjects()) if (go.name.StartsWith("[Auto]")) Object.DestroyImmediate(go);`. |
| `SerializedObject.FindProperty("heroPrefab")` retourne null si le champ a été renommé. | Wrap dans `if (prop == null) { Debug.LogWarning(...); continue; }`. |
| `Assets/Prefabs/Hero.prefab` n'existe pas (cf. glob result : aucun Hero*.prefab). | Log warning, skip wiring, ne pas crasher. Le ticket parallèle `feat(entities): Hero.prefab` doit shipping avant pour wirer ce champ. |
| Re-run après ajout manuel d'un BossDef dans la List ne doit pas écraser. | `PopulateList` doit checker `list.arraySize == guids.Length` et skip si match → simple diff-then-write. Alternative : ne pas clear, append uniquement les manquants. |
| Helper `EnsureChild<T>` privé dans `BuildMainSceneTool` — duplication ou refacto ? | Pragmatique : dupliquer dans `SetupMainScene.cs` (Editor only, ~15 lignes). Refacto plus tard si 3e tool. |
| `DoctrineController`/`ShopController`/`BossUI` n'ont pas de slot `[SerializeField]` registry — chargement via `XxxRegistry.Get()` static. | Pas de wiring nécessaire, juste s'assurer que l'asset Resources existe (§4.2 le fait). |
| `LevelThemeMaterialConfig` mentionné dans le brief mais déjà présent sur disque (`Assets/Resources/LevelThemeMaterialConfig.asset`). | Skip silencieusement via `EnsureRegistry<T>` qui retourne 0 si l'asset existe. |
