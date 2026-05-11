# Phase 1 POC — Code Audit

**Date** : 2026-05-11  
**Scope** : ~2200 lignes C#, 18 fichiers Scripts + 5 Editor + 2 UI assets  
**Auditeur** : Sonnet quality-maintainer  
**État branch** : `main`, HEAD propre (pas de modifications actives CORE-0x détectées au moment de la lecture — les fichiers SO Data/ sont déjà en forme finale ou en attente d'agents BG)

---

## Section 1 — Dead code & TODOs

### 1.1 Commentaire POC actif signalant une limitation future

- **`Assets/Scripts/Systems/PathManager.cs:41`**  
  ```
  // POC : first portal × first castle only.
  ```
  Limitation connue : multi-portal non supporté. C'est un vrai TODO qui restera bloquant dès que des niveaux avec 2 portails existeront. À tracker en ticket MIGRATE-CORE ou BACKLOG.

### 1.2 Imports non utilisés

- **`Assets/Scripts/Data/WaveDef.cs:2`** — `using System;` importé, mais `[Serializable]` est l'attribut utilisé. En C# Unity, `System.Serializable` = `[Serializable]` ; l'import est nécessaire. **Pas de dette.**

- **`Assets/Editor/POC05Setup.cs:5`** — `using CrowdDefense.Systems;` importé pour `PlacementController`, qui est bien dans ce namespace. **Pas de dette.**

- **`Assets/Editor/POC06Setup.cs:5`** — `using CrowdDefense.Entities;` importé. `Castle` n'est utilisé nulle part dans le fichier (seul `LevelRunner` et `Economy` sont manipulés). **Import mort.**  
  Ref : POC06Setup.cs ligne 5.

### 1.3 Champs/propriétés jamais lus en dehors de leur classe

- **`EnemyType.cs`** — `WalkAnim` (l.75), `AssetKey` (l.76), `ShaderOverlay` (l.102) : propriétés en read-only exposées, aucun appelant dans le code Phase 1. Normal pour SO — les appelants viendront en Phase 2. **Pas de dette en soi**, mais à noter : si ces champs restent sans appelants à fin Phase 2, supprimer.

- **`TowerType.cs`** — `Icon` (l.77) : idem, pas d'appelant en Phase 1. Réservé pour UI Phase 2.

- **`BalanceConfig.cs`** — `LevelScale` (l.19, array `float[]`) est déclaré `public` directement (pas `[SerializeField] private`), exposé mais jamais lu. **Smell mineur** : incohérence de convention + potentiellement inutilisé.  
  Ref : BalanceConfig.cs:19.

- **`Castle.cs`** — `OnDestroyed` (l.17) est invoqué (l.46) mais aucun subscriber dans le code Phase 1. Normal — HudController n'écoute que `OnHPChanged`. Pas de dette.

### 1.4 Méthodes jamais appelées depuis l'extérieur

- **`BalanceConfig.cs` — `DifficultyMulFor(int world)`** (l.61) : jamais appelée dans le code Phase 1. Utilisée dans `CastleHPFor()` implicitement ? Non — `CastleHPFor` prend `difficultyMul` en paramètre, `DifficultyMulFor` est un helper public orphelin. Candidat à dead code si Phase 2 n'appelle pas directement.

- **`GridData.cs` — `ReconstructPath()`** (l.82) : méthode `private static`, appelée depuis `BfsShortestPath`. OK.

- **`BuildScript.cs` — `ApplyWebGLPlayerSettings()`** (l.42) : `public static`, appelée depuis `BuildWebGL()`. Cohérent.

### 1.5 Debug.Log non wrappés `#if UNITY_EDITOR`

Les 4 fichiers suivants contiennent des `Debug.Log/LogError` **non protégés** qui iront en prod build :

| Fichier | Lignes | Type |
|---------|--------|------|
| `Systems/WaveManager.cs` | 43 | `LogError` non wrappé |
| `Systems/PathManager.cs` | 28, 37, 48 | `LogError` × 3 non wrappés |
| `Systems/MapRenderer.cs` | 17 | `LogError` non wrappé |
| `Systems/Economy.cs` | aucun | OK |

Les `Debug.Log` des lignes 72, 96, 115, 135 de `WaveManager.cs` sont **correctement wrappés** `#if UNITY_EDITOR`.  
Les `LogError` des lignes 43 (WaveManager), 28/37/48 (PathManager), 17 (MapRenderer) **ne le sont pas**. Erreurs légitimes (config manquante), mais en WebGL/mobile ils polluent la console de prod.

---

## Section 2 — Duplications

### 2.1 `Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")` — 5 occurrences

Pattern identique répété verbatim dans :

1. `Assets/Scripts/Entities/Tower.cs:28`
2. `Assets/Scripts/Entities/Enemy.cs:32`
3. `Assets/Scripts/Entities/Projectile.cs:22`
4. `Assets/Scripts/Systems/MapRenderer.cs:55`
5. `Assets/Editor/POC06Setup.cs:89`

**Recommandation** : extraire dans `ShaderUtil.GetLitShader()` (classe `static` dans `Systems` ou `Entities/Shared`) — 15 min, élimine 5 duplications, évite de typer la string URP à la main.

### 2.2 Singleton Awake pattern — 5 occurrences identiques

```csharp
if (Instance != null && Instance != this) { Destroy(gameObject); return; }
Instance = this;
```

Répété verbatim dans :

1. `LevelRunner.cs:24-25`
2. `Economy.cs:16-17`
3. `WaveManager.cs:35-36`
4. `PathManager.cs:19-20`
5. `Castle.cs:21-22`

Voir Section 4 pour analyse détaillée et recommandation `MonoSingleton<T>`.

### 2.3 `MarkSceneDirty + SaveOpenScenes` — 4 occurrences dans 3 fichiers Editor

```csharp
EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
EditorSceneManager.SaveOpenScenes();
```

Occurrences :
- `POC05Setup.cs:74-78` (avec namespace long `UnityEditor.SceneManagement.`)
- `POC06Setup.cs:63-65` (namespace long)
- `POC06Setup.cs:99-101` (namespace long, 2e fois dans le même fichier)
- `POC07Setup.cs:68-69` (namespace court via `using`)

**Inconsistance** : POC05 et POC06 utilisent le namespace qualifié complet (`UnityEditor.SceneManagement.EditorSceneManager.`), POC07 utilise le `using` en tête — même résultat, style divergent.  
**Recommandation** : extraire une méthode `EditorSceneHelper.SaveDirtyScene()` dans un fichier `EditorSceneHelper.cs` — 10 min, 4 appels consolidés, style unifié.

### 2.4 `new Material(shader)` + `rend.material = ...` — 4 occurrences liées

Tower, Enemy, Projectile, MapRenderer créent chacun une `new Material` par instance à l'init. Chaque enemy spawn = 1 alloc Material. Avec 30+ ennemis simultanés, c'est ~30 allocations GC Material/niveau.  
MapRenderer atténue avec un `_matCache` static par char — **seul MapRenderer fait bien.**  
Tower et Enemy créent une Material par instance sans cache, ce qui est correct pour coloration individuelle, mais voir Section 3.

---

## Section 3 — Anti-patterns Unity

### 3.1 `Camera.main` en Awake — OK

`PlacementController.cs:19` — `cam = Camera.main` est appelé dans `Awake()` et caché en champ. Pas de lookup en `Update`. **Correct.**

### 3.2 `GetComponent` en `Update` — Pas trouvé

Aucun `GetComponent` dans les corps de `Update()`. Les appels post-`Instantiate` sont dans les méthodes de spawn (`SpawnEnemy`, `Fire`) qui ne tournent pas chaque frame. **Correct.**

### 3.3 `Instantiate` + `Destroy` sans pool — Présent, scope POC acceptable

- `Tower.cs:80` — `Instantiate(projectilePrefab)` à chaque tir.
- `WaveManager.cs:128` — `Instantiate(enemyPrefab)` à chaque spawn.
- `Enemy.cs:51,86,98` — `Destroy(gameObject)` à mort/atteinte castle.
- `Projectile.cs:33,43` — `Destroy(gameObject)` à expire/hit.

**Magnitude** : avec 30 ennemis par vague × N tours tirant, on peut avoir 50-100 Instantiate/Destroy par seconde en late game. C'est la principale source de GC spikes sur mobile/WebGL.  
Acceptable pour POC, **critique à adresser avant Phase 2 mobile build** via ObjectPool Unity (`UnityEngine.Pool.ObjectPool<T>`).

### 3.4 `new Material` par instance sans partage — smell mineur

`Tower.cs:28-29` et `Enemy.cs:32-33` créent une `new Material` par instance pour la coloration individuelle. Correct pour des couleurs distinctes, mais 30 ennemis × 1 Material = 30 instances Material sur GPU. Pas de fuite (materials liées au GO, détruites avec lui), mais pas optimal.  
`MapRenderer.cs:10` a un `_matCache` static correct pour les tuiles partagées.

### 3.5 `new System.Random()` au lieu de `UnityEngine.Random` — smell

- **`WaveManager.cs:62`** — `var rng = new System.Random();`

`System.Random` est instancié sans seed fixe (non-déterministe, pas reproductible en tests). `UnityEngine.Random.Range()` est l'idiome Unity standard et utilisé partout ailleurs dans le projet (ex: `BalanceConfig.cs:76`). Incohérence de 1 fichier sur 18.

**Fix** : remplacer le Fisher-Yates par `UnityEngine.Random.Range(0, i+1)` — 2 lignes.

### 3.6 `BalanceConfig.Get()` appelé dans un corps de méthode de spawn

- **`WaveManager.cs:53`** — `BalanceConfig.Get().SwarmMul` dans `BeginWave()`. Appelé une fois par vague, pas par frame. **Acceptable.**
- **`Tower.cs:84`** — `BalanceConfig.Get().TowerDamageMul` dans `Fire()`. Appelé à chaque tir (potentiellement chaque frame si cooldown court). `Resources.Load` avec cache, donc 1 lookup dict — acceptable mais idéalement caché dans `Tower.Awake()`.

### 3.7 `BalanceConfig` — champs `public` au lieu de `[SerializeField] private`

`BalanceConfig.cs` expose tous ses champs en `public float TowerDamageMul = 1.6f` etc. La convention du projet (CLAUDE.md + les autres SO) est `[SerializeField] private` + propriété readonly. **Incohérence de convention.**  
Scope : 20+ champs publics dans BalanceConfig, probablement délibéré pour accès direct sans propriété (performance), mais divergent du reste.

### 3.8 `WaveDef` non-nullable fields de struct

- **`WaveDef.cs:11`** — `EnemySpawnEntry.type` est `EnemyType` (non-nullable), mais en pratique peut être `null` si Unity laisse le champ vide dans l'Inspector. `WaveManager.cs:57` teste `if (entry.type == null) continue;`, ce qui valide que la nullabilité est réelle mais non déclarée dans le type. Avec `#nullable enable`, le compilateur ne voit pas ce danger.

### 3.9 Singletons sans `Instance = null` dans `OnDestroy`

Aucun des 5 singletons (LevelRunner, Economy, WaveManager, PathManager, Castle) ne remet `Instance = null` dans `OnDestroy`. En scène unique (reload via `SceneManager.LoadScene`), les Instances stales pointent vers des GO détruits. Unity gère (`==null` sur MonoBehaviour détruit retourne true), mais en tests unitaires ou rechargement de scène sans DontDestroyOnLoad, ça peut créer des références obsolètes.

**Note** : `LevelRunner.OnDestroy()` gère le `unsubscribe` event — cohérent. Mais n'efface pas `Instance`.

---

## Section 4 — Patterns divergents

### 4.1 Singletons — 5 implémentations identiques, pas de base commune

Les 5 singletons (`LevelRunner`, `Economy`, `WaveManager`, `PathManager`, `Castle`) ont exactement le même pattern Awake à 2 lignes. Aucune variante entre eux : même garde `Instance != this`, même `Destroy`, même assignation. Pas de `DontDestroyOnLoad` sur aucun (correct pour scène unique).

**Divergence notable** :
- `LevelRunner` et `HudController` implémentent `OnDestroy` pour cleanup d'events — bon pattern.
- `Economy`, `WaveManager`, `PathManager`, `Castle` n'ont **pas** de `OnDestroy` — OK si pas d'events externes, mais `Castle` expose 2 events (`OnHPChanged`, `OnDestroyed`) sans garantie de unsubscribe côté Castle lui-même.
- `WaveManager` expose `OnWaveStart`, `OnWaveCleared`, `OnAllWavesCompleted` sans `OnDestroy` pour nettoyer.

**Recommandation** : `MonoSingleton<T>` base class avec `OnDestroy` qui reset `Instance = null`. 30 min, élimine 10 lignes dupliquées, ajoute la sécurité reset.

### 4.2 Editor scripts POC* — Pattern create-assign-save répété

Les 3 scripts POC font tous le cycle :
1. `GameObject.Find("Systems")` — ou `Find("nom")` 
2. `new GameObject("nom")` si null
3. `AddComponent` si null
4. `SerializedObject` + `FindProperty` + `ApplyModifiedProperties`
5. `MarkSceneDirty` + `SaveOpenScenes`

**Divergences** :
- POC05 et POC06 utilisent le namespace qualifié `UnityEditor.SceneManagement.EditorSceneManager.` (verbeux)
- POC07 a un `using UnityEditor.SceneManagement;` et appelle directement `EditorSceneManager.` (propre)
- POC06 utilise `Undo.RegisterCreatedObjectUndo` et `Undo.AddComponent` (correct, supporte Ctrl+Z)
- POC05 utilise `pcGO.AddComponent<>()` sans `Undo` (ne supporte pas Ctrl+Z)
- POC07 utilise `Undo.AddComponent` (correct)

**Inconsistance** : POC05.SetupScene ne wrappe pas la création avec `Undo`, alors que POC06 et POC07 le font.

### 4.3 Subscribe/Unsubscribe events — Cohérence partielle

| Fichier | Subscribe | Unsubscribe |
|---------|-----------|-------------|
| `LevelRunner.cs` | `Start()` | `OnDestroy()` |
| `HudController.cs` | `Start()` | `OnDestroy()` |
| `WaveManager.cs` | expose events, pas de subscribe propre | — |
| `Economy.cs` | expose `OnGoldChanged`, pas de subscribe | — |

Pattern cohérent pour les abonnés (Start/OnDestroy). Les émetteurs n'ont pas de cleanup propre, acceptable.

**Problème potentiel** : `HudController.OnDestroy()` unsubscribe via `if (Economy.Instance != null)`. Si `Economy` est détruit avant `HudController` (ordre de destruction non garanti), l'unsubscribe est skippé. En pratique sur rechargement de scène tous les GOs sont détruits ensemble, risque faible mais réel en tests.

---

## Section 5 — Nullability + Safety

### 5.1 Couverture `#nullable enable` — Excellente

18/18 fichiers Scripts ont `#nullable enable` en ligne 1.  
2/5 fichiers Editor ont `#nullable enable` (BuildScript, TowerSeedTool).  
POC05/06/07 n'ont **pas** `#nullable enable` — ces fichiers manipulent des refs `SerializedObject.FindProperty()` avec `!` implicite sans guard. Risk faible (Editor only), mais incohérent.

### 5.2 Convention `[SerializeField] private` — Cohérente dans Scripts, pas dans BalanceConfig

- Data SO (TowerType, EnemyType, LevelData, WaveDef) : tous `[SerializeField] private` + propriétés readonly. **Parfait.**
- `BalanceConfig.cs` : tous `public float` directement, sans `[SerializeField]`. **Divergent.** Probable choix délibéré pour accès direct (`BalanceConfig.Get().TowerDamageMul` sans propriété), mais rompt la convention.

### 5.3 Null-forgiving operator `!` usage

- `WaveManager.cs:52` — `levelData!.Waves[idx]` : garde à la ligne 41 (`levelData == null → return`) mais le compilateur ne peut pas tracker ça hors du `Start()`. Le `!` est justifié mais fragile.
- `MapRenderer.cs:56` — `new Material(shader!)` : `shader` peut être null si ni URP ni Standard sont présents (build stripped). `!` swallow l'erreur potentielle. **Smell mineur.**
- `TowerSeedTool.cs:319+` — multiples `FindProperty(prop)!` : si prop name est typo, NullReferenceException non explicite.

### 5.4 Instance `Castle.cs` — protection double-init correcte

`Castle.cs:21` : `if (Instance != null && Instance != this) { Destroy(gameObject); return; }` — correct.  
Pas de `DontDestroyOnLoad` → détruit sur reload de scène. Comportement attendu.

### 5.5 `BalanceConfig._cached` — Reset entre Play sessions

`BalanceConfig.cs:9` : `private static BalanceConfig? _cached;`. En Editor, le static survit entre Enter/Exit Play Mode si la domain reload est désactivée (Unity 6 opt-in). `_cached` peut pointer vers un SO désérialisé. **Smell mineur en Editor, pas en build.**  
Fix minimal : ajouter `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetCache() => _cached = null;`.

---

## Section 6 — Recommandations refactor Phase 2

Priorité par rapport valeur/effort, ordre décroissant :

### R1 — Extract `MonoSingleton<T>` base class [HAUTE VALEUR — 30 min]

**Cible** : `LevelRunner`, `Economy`, `WaveManager`, `PathManager`, `Castle` (5 fichiers).  
**Gain** : -10 lignes dupliquées, `Instance = null` dans `OnDestroy` gratuit, point unique de correctif si le pattern doit évoluer (ex: `DontDestroyOnLoad` pour futur menu → game).  
```csharp
// Assets/Scripts/Systems/MonoSingleton.cs
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour {
    public static T? Instance { get; private set; }
    protected virtual void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    protected virtual void OnDestroy() { if (Instance == this) Instance = null; }
}
```

### R2 — Extract `ShaderUtil.GetLitShader()` helper [HAUTE VALEUR — 15 min]

**Cible** : Tower, Enemy, Projectile, MapRenderer, POC06Setup (5 occurrences).  
**Gain** : string URP typée 1 seule fois, facilite migration vers `GraphicsSettings.defaultRenderPipeline` en Phase 3.

### R3 — ObjectPool pour Enemy et Projectile [CRITIQUE PERF — 2h]

**Cible** : `WaveManager.SpawnEnemy()`, `Tower.Fire()`, destructions dans `Enemy` et `Projectile`.  
**Gain** : zéro GC spikes en combat, obligatoire pour 60fps mobile. Utiliser `UnityEngine.Pool.ObjectPool<T>` (natif Unity 2021+).  
**Timing** : à faire **avant** les premiers tests perf mobiles, pas après.

### R4 — Remplacer `new System.Random()` par `UnityEngine.Random` [FAIBLE EFFORT — 5 min]

**Cible** : `WaveManager.cs:62-66`.  
**Gain** : cohérence API Unity, déterminisme optionnel via `Random.InitState(seed)`.

### R5 — `BalanceConfig` — convention `[SerializeField] private` + reset cache [FAIBLE EFFORT — 20 min]

**Cible** : `BalanceConfig.cs` 20+ champs `public`.  
**Gain** : cohérence convention projet, évite modif accidentelle depuis Inspector en runtime Editor.  
**Bonus** : ajouter `[RuntimeInitializeOnLoadMethod]` pour `_cached` reset (1 ligne).

### R6 — Unifier les imports `EditorSceneManager` dans POC* [COSMÉTIQUE — 10 min]

**Cible** : POC05Setup.cs, POC06Setup.cs (namespace qualifié long).  
**Gain** : lisibilité, cohérence avec POC07. Ajouter `using UnityEditor.SceneManagement;` en tête + Undo wrap dans POC05.

### R7 — `PathManager` : supprimer le commentaire `// POC : first portal × first castle only.` quand multi-portal est implémenté

**Cible** : `PathManager.cs:41`.  
**Action** : créer ticket MIGRATE-PATH-MULTI ou marquer ce commentaire comme blocker Phase 2 worlds avec > 1 portal.

### R8 — Wraper les `Debug.LogError` non protégés en `#if UNITY_EDITOR || DEVELOPMENT_BUILD` [QUALITÉ BUILD — 15 min]

**Cible** : WaveManager:43, PathManager:28/37/48, MapRenderer:17.  
**Gain** : sorties propres en WebGL/mobile release. Note : les `LogError` de config manquante sont légitimes à garder en `DEVELOPMENT_BUILD` pour debugging sur device.

### R9 — `EnemySpawnEntry.type` → nullable `EnemyType?` [SAFETY — 5 min]

**Cible** : `WaveDef.cs:11`.  
**Gain** : le `null` guard de WaveManager:57 devient explicitement documenté dans le type.

### R10 — `BalanceConfig.Get().TowerDamageMul` dans `Tower.Fire()` → cache en champ [MICRO-PERF — 5 min]

**Cible** : `Tower.cs:84`.  
**Gain** : évite le dict lookup `Resources` à chaque tir. Cache `BalanceConfig` dans `Tower.Awake()`.

---

## Résumé des smells par criticité

| Criticité | Smell | Fichier(s) |
|-----------|-------|-----------|
| CRITIQUE (perf) | Instantiate/Destroy sans pool | Tower, WaveManager, Enemy, Projectile |
| HAUTE | Shader.Find dupliqué ×5 | Tower, Enemy, Projectile, MapRenderer, POC06 |
| HAUTE | Singleton pattern dupliqué ×5 | LevelRunner, Economy, WaveManager, PathManager, Castle |
| MOYENNE | Debug.LogError non wrappés (4 appels) | WaveManager:43, PathManager:28/37/48, MapRenderer:17 |
| MOYENNE | `new System.Random()` incohérent | WaveManager:62 |
| MOYENNE | `BalanceConfig` champs `public` (convention) | BalanceConfig |
| FAIBLE | Import mort `CrowdDefense.Entities` | POC06Setup:5 |
| FAIBLE | `MarkSceneDirty`/`SaveOpenScenes` dupliqué ×4 | POC05, POC06 ×2, POC07 |
| FAIBLE | `shader!` null-forgiving sans guard | MapRenderer:56 |
| FAIBLE | `BalanceConfig._cached` non reset entre sessions Editor | BalanceConfig:9 |
| INFO | Commentaire `// POC : first portal` — future limitation | PathManager:41 |

