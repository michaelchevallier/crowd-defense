# Phase 5 — Architecture Improvements Roadmap

**Auteur** : Opus orchestrator (planner read-only)
**Date** : 2026-05-11
**Statut** : DRAFT — spec roadmap pré-Phase 5 ship
**Estimé total** : ~25-35 tickets `MIGRATE-ARCH-XX`, distribués entre Phase 5 (ship) et Phase 6 (post-launch)
**Scope** : READ-ONLY scoping document. Aucune implémentation : seulement spec + recommandations.

---

## 1. Contexte

Phase 3 swarm a révélé une **fragilité scene-bootstrap** : 7 singletons (`VfxPool`, `JuiceFX`, `EnemyPool`, `ProjectilePool`, `SlowEffectManager`, `CoinPullManager`, `SettingsRegistry`) absents de `Main.unity` après merge multi-axes, déclenchant cascade `NullReferenceException` masquée par opérateurs `?.`. Fix immédiat : `MonoSingleton<T>` avec lazy `Instance` getter + `FindFirstObjectByType` fallback + auto-create `GameObject`. Workaround `[DefaultExecutionOrder]` rejeté car coupe le lien runtime / scene-setup contract.

Phase 5 ship demande 3 piliers architecturaux non couverts Phase 1-3 :

1. **DI container vs MonoSingleton** — décision keep/migrate avant content phase
2. **Save/load robust** — versioning, validation, cloud sync (Steam/iCloud/Google Play)
3. **Telemetry/analytics** — post-launch balance tuning data + privacy GDPR

Plus 2 chantiers escalés depuis Phase 3 :
4. **Localization full** — migration `L.cs` Dictionary → Unity Localization package + ES/DE/ZH
5. **Accessibility full** — au-delà des 3 toggles SettingsRegistry actuels

**Lecture obligatoire avant exécution Phase 5** :
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Common/MonoSingleton.cs`
- `/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/SaveSystem.cs`
- `/Users/mike/Work/crowd-defense/.claude/coordination/qa-reports/unity-review-phase3-postmortem.md`
- `/Users/mike/Work/crowd-defense/.claude/coordination/qa-reports/csharp-review-phase3-postmortem.md`

**Skill Mike** : zéro Unity/C# — pattern doit être **simple, Inspector-friendly, minimum magic**. Aucune réflection custom, aucun container DI requérant attributs lourds.

---

## 2. DI container vs MonoSingleton — décision et plan

### 2.1 État actuel

Tous les singletons héritent de `MonoSingleton<T>` (depuis fix Phase 3) :

| Singleton | Source | Inspector deps |
|---|---|---|
| `AudioController` | scene Main | `mixer`, `registry` (Resources) |
| `WaveManager` | scene Main | `levelData`, `enemyPrefab` |
| `LevelRunner` | scene Main | `currentLevel`, `castlePrefab` |
| `Economy` | scene Main | aucun |
| `PlacementController` | scene Main | `selectedTowerType`, `towerPrefab` |
| `PathManager` | scene Main | `levelData` |
| `HudController` | scene Main | UI Toolkit doc |
| `VfxPool` | scene Main | `impactPrefab`, `deathPrefab` (Resources fallback) |
| `JuiceFX` | scene Main | aucun |
| `EnemyPool`, `ProjectilePool` | scene Main | `enemyPrefab`, `projectilePrefab` |
| `SlowEffectManager`, `CoinPullManager`, `Synergies` | scene Main | aucun |
| `SettingsRegistry` | scene Main | aucun (PlayerPrefs) |
| `Achievements` | scene Main | `registry` (Resources fallback) |
| `TreasureSpawner`, `MapRenderer` | scene Main | refs spawner |

Compte : **~16 MonoSingleton** + 1 `LevelLoader` static + 1 `SaveSystem` static + 1 `L` static + 1 `AssetRegistry`/`LevelRegistry`/`AudioClipRegistry` SO.

**Pattern actuel** :
```csharp
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T? _instance;
    public static T? Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = Object.FindFirstObjectByType<T>();
            if (_instance != null) return _instance;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[MonoSingleton] {typeof(T).Name} auto-created — missing in scene.");
#endif
            var go = new GameObject($"[Auto] {typeof(T).Name}");
            _instance = go.AddComponent<T>();
            return _instance;
        }
    }
    // ... Awake/OnDestroy guard
}
```

### 2.2 Comparaison DI containers Unity

| Critère | MonoSingleton (actuel) | Zenject | VContainer | Reflex |
|---|---|---|---|---|
| Maintenance | Plus maintenu officiellement (2023 archive) | Extenject fork actif | Actif (hadashiA) | Actif (gustavopsantos) |
| Taille runtime | 0 KB (built-in) | ~600 KB | ~80 KB | ~50 KB |
| Codegen IL2CPP | N/A | OK avec config | OK natif | OK natif |
| Apprentissage | Trivial | Steep (Installer, Binder, Factory, Signal, Context...) | Moyen (Builder, Lifetime) | Faible (Container.Bind/Resolve) |
| Inspector UX | GameObject + AddComponent | Inspector + Context prefabs | Code-only (LifetimeScope) | Code-only (ProjectScope) |
| Testabilité | Manuel (subclass override) | Excellente (mock binding) | Bonne | Bonne |
| Détection scene-drift | Auto-create masque le bug | Throw au resolve | Throw au resolve | Throw au resolve |
| WebGL/IL2CPP | OK | OK avec linker.xml | OK | OK |
| Skill Mike | Compatible (Awake/Start familier) | Coût élevé | Coût moyen | Coût faible |

### 2.3 Décision et recommandation

**STATU QUO POUR PHASE 5 SHIP** — garder `MonoSingleton<T>` lazy.

Raisons :
1. **Skill barrier** : Mike zéro Unity. Un container DI ajoute 3 nouveaux concepts (LifetimeScope, Registration, Resolve) sans gain critique pour un solo dev.
2. **Coût migration** : 16 singletons × ~15 min refactor = ~4h de bricolage Inspector + risque de régression `OnAwakeSingleton` hooks. Phase 5 ship n'a pas la marge.
3. **Auto-create degraded mode** : suffisant pour MVP. Les 4 singletons avec `[SerializeField]` deps (`AudioController`, `WaveManager`, `VfxPool`, `LevelRunner`) ont des fallbacks Resources/null-checks.
4. **MonoSingleton détectabilité** : ajouter validation Editor menu (cf §2.5) capture les scene-drift avant build, sans framework.

**Mais corriger 4 dettes héritées Phase 3** :

#### Ticket `MIGRATE-ARCH-DI-01` — Harden MonoSingleton (1 commit, 30 min)
- Fichier : `/Users/mike/Work/crowd-defense/Assets/Scripts/Common/MonoSingleton.cs`
- Ajouter `TryInstance` (`T?` jamais auto-create) pour callers non-critiques :
  ```csharp
  public static T? TryInstance => _instance != null ? _instance : Object.FindFirstObjectByType<T>();
  ```
- Garder `Instance` (auto-create) comme degraded path
- Ajouter attribut marker `[RequiresSceneSetup]` qui désactive l'auto-create + throw explicit en runtime build (pas seulement Editor) :
  ```csharp
  [AttributeUsage(AttributeTargets.Class)]
  public sealed class RequiresSceneSetupAttribute : Attribute { }
  ```
- Annoter `AudioController`, `WaveManager`, `VfxPool`, `LevelRunner`, `PathManager`, `PlacementController` (les 6 avec Inspector deps obligatoires)

#### Ticket `MIGRATE-ARCH-DI-02` — Build Main Scene Editor menu (1 commit, 1h)
- Fichier nouveau : `/Users/mike/Work/crowd-defense/Assets/Editor/Tools/BuildMainScene.cs`
- MenuItem `Tools/CrowdDefense/Build Main Scene`
- Pseudo-code :
  ```csharp
  [MenuItem("Tools/CrowdDefense/Build Main Scene")]
  public static void Build()
  {
      var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
      var systems = FindOrCreate("Systems");
      EnsureComponent<AudioController>(systems);
      EnsureComponent<WaveManager>(systems);
      // ... 14 autres singletons
      WireInspectorRefs();   // SerializedObject.FindProperty + AssetDatabase.LoadAssetAtPath
      EditorSceneManager.MarkSceneDirty(scene);
      EditorSceneManager.SaveScene(scene);
  }
  ```
- Idempotent : `EnsureComponent` early-out si déjà présent
- Sortie : `Debug.Log` summary `+3 added, 13 already present`

#### Ticket `MIGRATE-ARCH-DI-03` — Scene validation test (1 commit, 45 min)
- Fichier nouveau : `/Users/mike/Work/crowd-defense/Assets/Tests/PlayMode/SceneSetupTests.cs`
- PlayMode test : ouvre `Main.unity`, attend 1 frame, assert toutes les `*.Instance` non-null
- Run via `Unity -batchmode -executeMethod TestRunner.RunPlayMode`
- Catch P0-1 type bugs en CI **avant build**

#### Ticket `MIGRATE-ARCH-DI-04` — TryInstance audit (1-2 commits, 1h)
- Audit des ~85 call-sites `Instance?.` dans Phase 3 code
- Décider par site : `Instance?.X` (auto-create OK = `JuiceFX`, `VfxPool`, `Achievements`, `CoinPullManager`) ou `TryInstance?.X` (auto-create dangerous = `WaveManager`, `LevelRunner`)
- Documenter dans `CLAUDE.md` section "Pièges Unity 6 LTS / MonoSingleton"

**Total Phase 5 DI** : ~4 commits, 4h.

### 2.4 Phase 6 fallback : migration VContainer si pain réel

Si post-launch :
- Tests Phase 5 scene-validation cassent fréquemment
- Tickets contiennent > 5 régressions liées à scene-drift
- Mike monte en compétence Unity / accueillant à abstraction

Alors ticket `MIGRATE-ARCH-DI-PHASE6-MIGRATION` (~15 commits, 1-2 jours) :
- Adopter **VContainer** (le plus simple des 3, IL2CPP-native)
- Créer `RootLifetimeScope` (project-level) + `MainSceneLifetimeScope` (gameplay)
- Refactor 16 singletons → registered services
- Garder `MonoSingleton<T>` pendant 1 sprint pour rollback rapide

**Critère trigger** : > 3 bugs scene-setup-drift en Phase 5 post-ship.

### 2.5 Risques DI section

- **Risque** : Mike s'attache au pattern Singleton, refuse migration plus tard. **Mitigation** : Garder spec VContainer prête mais ne pas pousser tant que pain pas réel.
- **Risque** : Auto-create produit instances avec `[SerializeField] null` refs → crash silencieux. **Mitigation** : `[RequiresSceneSetup]` attribute + Editor menu Build Main Scene.
- **Risque** : `FindFirstObjectByType<T>` peut être lent au premier hit (scene graph traversal). **Mitigation** : Lazy = called once, puis cache. Mesuré ~0.2ms par singleton sur scène Phase 3.

---

## 3. Save/Load robust

### 3.1 État actuel

`/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/SaveSystem.cs` (91 lignes) :
- Backend unique : `PlayerPrefs.SetString("cd_progression_v1", json)` avec `JsonUtility.ToJson`
- Données : `ProgressData` (cleared levels, unlocked levels, totalKills, volumes, lang)
- Cache `_cached` mémoire
- Try/catch silencieux pour deserialization fail
- Reset all hard reset
- **Pas** de versioning explicite (le `_v1` du key est figé, pas une migration)
- **Pas** de checksum / validation intégrité
- **Pas** de backup / restore
- **Pas** de cloud sync

`/Users/mike/Work/crowd-defense/Assets/Scripts/UI/SettingsRegistry.cs` + `/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/Achievements.cs` écrivent aussi à `PlayerPrefs` directement (clés `cd.audio.*`, `cd.gfx.*`, `cd.a11y.*`, `cd.locale`, `cd.achievements.unlocked`, `cd.ach.counter.*`). Pas unifié.

### 3.2 Phase 5 needs

| Besoin | Justification | Phase 5 ? |
|---|---|---|
| Save versioning v1 → v2 | LevelData ID renames + new fields | OUI |
| Save validation (checksum) | Detect corruption avant load → fallback | OUI |
| Backup auto avant write | Revert si crash mid-write | OUI |
| Cross-platform abstraction | Steam Mac/Win/Linux + iOS + Android + WebGL | OUI |
| Cloud save (Steam Cloud) | Sync entre 3 PCs Steam users | OUI (Phase 4 Steam research) |
| Cloud save iCloud | Sync iOS devices | NON (Phase 7 mobile) |
| Cloud save Google Play | Sync Android | NON (Phase 7 mobile) |
| Save encryption | Anti-cheat single-player TD = low value | NON |
| Save compression | ProgressData < 5 KB → no gain | NON |

### 3.3 Architecture cible

```
Assets/Scripts/Systems/Save/
├── ISaveBackend.cs              # Interface : Save(string key, byte[]) + Load(string key)
├── PlayerPrefsBackend.cs        # WebGL + fallback
├── FileSystemBackend.cs         # Application.persistentDataPath + Steam standalone
├── SteamCloudBackend.cs         # Wrapper Steamworks.NET ISteamRemoteStorage
├── SaveSystem.cs                # API public, choisit backend par platform
├── SaveData.cs                  # Versioned ProgressData (v1, v2, ...)
├── SaveMigrator.cs              # v1 → v2 migration rules
├── SaveValidator.cs             # CRC32 checksum + JSON schema validate
└── SaveBackup.cs                # Auto-snapshot avant write, rollback API
```

**Interface clé** :
```csharp
public interface ISaveBackend
{
    bool TryLoad(string key, out byte[] data);
    void Save(string key, byte[] data);
    void Delete(string key);
    bool Exists(string key);
}
```

**Choix backend par platform** :
```csharp
public static ISaveBackend Resolve()
{
#if UNITY_STANDALONE && !UNITY_EDITOR
    if (SteamManager.Initialized) return new SteamCloudBackend();
    return new FileSystemBackend();
#elif UNITY_WEBGL
    return new PlayerPrefsBackend();
#elif UNITY_IOS || UNITY_ANDROID
    return new FileSystemBackend();
#else
    return new PlayerPrefsBackend();
#endif
}
```

### 3.4 Versioning v1 → v2

**Pattern** : envelope JSON avec `version` field :
```json
{
  "version": 2,
  "checksum": "a1b2c3d4",
  "data": {
    "clearedLevels": [...],
    "unlockedLevels": [...],
    "totalKills": 1234,
    "totalPlaytimeMs": 567890,    // <- new in v2
    "achievementCounters": {...}  // <- new in v2 (moved from PlayerPrefs)
  }
}
```

**Migrator** :
```csharp
public static SaveDataV2 Migrate(string rawJson)
{
    var envelope = JsonUtility.FromJson<SaveEnvelope>(rawJson);
    switch (envelope.version)
    {
        case 0:  // legacy : raw ProgressData without envelope
        case 1:
            var v1 = JsonUtility.FromJson<SaveDataV1>(envelope.data);
            return new SaveDataV2 { /* copy v1 fields, init new ones */ };
        case 2:
            return JsonUtility.FromJson<SaveDataV2>(envelope.data);
        default:
            throw new SaveVersionException(envelope.version);
    }
}
```

### 3.5 Tickets save/load

#### `MIGRATE-SAVE-01` — Refactor SaveSystem en interface + PlayerPrefs backend (1 commit, 2h)
- Extraire `ISaveBackend` + `PlayerPrefsBackend` from current `SaveSystem.cs`
- API publique inchangée (Mike + autres callers ne voient rien)
- `SaveSystem.Load()` / `Save()` délègue à `_backend`
- Tests : `Application.platform == WebGLPlayer → PlayerPrefs`

#### `MIGRATE-SAVE-02` — FileSystemBackend pour standalone (1 commit, 1h30)
- Fichier nouveau : `Assets/Scripts/Systems/Save/FileSystemBackend.cs`
- Écrit dans `Application.persistentDataPath + "/saves/" + key + ".save"`
- `Save(string, byte[])` : write atomic via `.tmp` + `File.Move`
- `TryLoad(string, out byte[])` : `File.Exists` + `File.ReadAllBytes`
- Tests : Editor playmode write/read roundtrip

#### `MIGRATE-SAVE-03` — Versioning envelope + CRC32 checksum (1 commit, 2h)
- Fichier nouveau : `Assets/Scripts/Systems/Save/SaveEnvelope.cs`
- Struct : `{ int version; string checksum; string data; }`
- CRC32 via System.IO.Hashing (Unity 6 built-in)
- Save() wraps : compute CRC32 → write envelope
- Load() verifies : recompute CRC32 → if mismatch, log + fallback to backup
- Tests : corrupt 1 byte → load returns backup

#### `MIGRATE-SAVE-04` — Backup snapshot avant write (1 commit, 1h)
- Fichier nouveau : `Assets/Scripts/Systems/Save/SaveBackup.cs`
- Avant chaque `Save()`, copier le fichier précédent vers `<key>.backup`
- Garder 1 backup unique (pas chaîne — keep simple)
- Load() : si corrupt, attempt `.backup` automatiquement
- Si backup aussi corrupt : `ResetAll()` + alert UI "save corrupted"

#### `MIGRATE-SAVE-05` — SaveMigrator v0 → v1 → v2 (1 commit, 1h30)
- Fichier nouveau : `Assets/Scripts/Systems/Save/SaveMigrator.cs`
- Migrate(rawJson) returns SaveDataV2
- v0 (current legacy `cd_progression_v1` key) : `JsonUtility.FromJson<ProgressData>` + wrap envelope
- v1 (envelope sans nouveau fields) : copy + init `totalPlaytimeMs = 0`, `achievementCounters = {}`
- v2 : direct deserialize
- Tests : v0 save sur disk → load → v2 in memory

#### `MIGRATE-SAVE-06` — SteamCloudBackend (1 commit, 3h, bloqué par Phase 4 Steam research)
- Fichier nouveau : `Assets/Scripts/Systems/Save/SteamCloudBackend.cs`
- Wrapper `SteamRemoteStorage.FileWrite/FileRead`
- Quota check : `SteamRemoteStorage.GetQuota` avant write
- Bloqué par : Phase 4 Steamworks SDK plugin intégré
- Fallback si Steam offline : `FileSystemBackend`
- Tests : manual sur 2 Steam accounts

#### `MIGRATE-SAVE-07` — Migrate Settings + Achievements writes vers SaveSystem (1 commit, 2h)
- Fichier modifié : `Assets/Scripts/UI/SettingsRegistry.cs` lignes 109-138
- Remplacer 11× `PlayerPrefs.SetX/GetX` par `SaveSystem.SaveSettings(SettingsData)` + `LoadSettings()`
- Fichier modifié : `Assets/Scripts/Systems/Achievements.cs` lignes 37-99
- Remplacer `PlayerPrefs.GetString/SetString("cd.achievements.unlocked")` par `SaveSystem.SaveAchievements(AchievementData)`
- Migrate v0 → v1 : si nouveau load échoue, fallback lit anciennes clés PlayerPrefs + écrit envelope + supprime anciennes

**Total Phase 5 Save** : ~7 commits, 12h.

### 3.6 Risques save/load section

- **Risque** : Steamworks SDK Phase 4 pas prêt avant Phase 5 ship. **Mitigation** : `SaveMigrate-06` est optionnel ; `FileSystemBackend` suffit pour Steam Mac/Win/Linux MVP.
- **Risque** : Migration v0 → v1 perd des saves si bug. **Mitigation** : Versionning préserve fichier `.v0.bak` 30 jours.
- **Risque** : Atomic write `.tmp + Move` échoue sur iOS sandbox. **Mitigation** : `File.WriteAllBytes` direct iOS, atomic write desktop only.
- **Risque** : CRC32 checksum sur dictionary ordering non-stable. **Mitigation** : `JsonUtility` Unity est deterministic field order. Tester unit.

---

## 4. Telemetry / Analytics

### 4.1 Goal

**Post-launch balance tuning** : savoir quels tours sont sous-utilisés, quels niveaux bloquent les joueurs, quelles synergies sont triggers, quel taux d'utilisation du skip wave bonus, quels achievements jamais unlockés.

**NON goal Phase 5** : pas de "session length" / "DAU" / monetization analytics. Solo dev, no IAP, no ads.

### 4.2 Privacy stance

**QUESTION OUVERTE POUR MIKE** :
- Option A : **Opt-in default OFF** (RGPD safe, GDPR strict, conservative) — perdra ~80% des data
- Option B : **Opt-in default ON** avec écran "Help improve Crowd Defense?" au premier lancement (GDPR-friendly si choix explicite affiché)
- Option C : **Anonyme only, default ON, opt-out caché dans Settings** (gray area RGPD, certains éditeurs adoptent, mais risque si audit CNIL)

**Recommandation** : **Option B**. Affichage explicite + texte clair "anonymous usage data, no PII" + Settings toggle pour revenir. Conforme RGPD si :
- Pas de IP collected
- Pas de device ID (utiliser UUID local généré + stocké local seulement)
- Lien vers privacy policy clair
- Opt-out instantané (event count freeze, pas de "rétroactif delete" car anonyme)

### 4.3 Comparaison backends

| Backend | Coût | Privacy | Unity integration | Status 2026 |
|---|---|---|---|---|
| Unity Analytics | Free | RGPD-compliant via opt-in | Built-in package | **Deprecated 2024**, end-of-life |
| Unity Cloud Analytics | Free<5K MAU | RGPD ok | Built-in | Successor mais bloated, requires Unity Cloud account |
| Firebase Analytics | Free | Need GDPR consent banner | Plugin Firebase | Requires Google account + SDK ~5 MB |
| GameAnalytics | Free<100K MAU | RGPD compliant | Plugin gameanalytics-sdk-unity | Lightweight, gaming-focused |
| Custom backend | Hosting cost | Full control | Custom HTTP | Effort dev + maintenance |
| **Postlog dev-only (Editor + log file)** | Zero | Local only | None | Pour debug Phase 5 ship pre-launch |

**Recommandation** : **2-tier strategy** :
- **Phase 5 ship MVP** : `LocalFileTelemetry` — log events to `Application.persistentDataPath/telemetry.jsonl`. Mike peut récupérer le fichier via Steam Cloud / mail si bug report.
- **Phase 6 post-launch** : si pain réel ("je veux savoir quels tours unused sur 1000 players"), intégrer **GameAnalytics** (le plus simple, le plus respectueux RGPD).

### 4.4 Architecture

```
Assets/Scripts/Systems/Telemetry/
├── ITelemetryBackend.cs         # Interface : LogEvent(string name, Dictionary<string, object> params)
├── LocalFileBackend.cs          # JSONL writer to persistent data
├── NullBackend.cs               # No-op si opt-out
├── GameAnalyticsBackend.cs      # Phase 6 only — wrap GA SDK
├── Telemetry.cs                 # Static API public
└── TelemetryConsent.cs          # First-launch consent screen logic
```

**API simple** :
```csharp
public static class Telemetry
{
    public static void LogEvent(string name, params (string key, object value)[] tags) { ... }
    public static void SetConsent(bool optIn) { ... }
    public static bool HasConsent => _backend is not NullBackend;
}
```

**Events à tracker** (Phase 5 MVP) :
| Event name | Payload | Hot zone |
|---|---|---|
| `app_launched` | `{platform, version}` | `Main.unity Start` |
| `level_started` | `{levelId, world}` | `LevelRunner.OnAwakeSingleton` |
| `level_completed` | `{levelId, durationMs, towersUsed[], castleHPRemaining}` | `LevelRunner.OnVictory` |
| `level_failed` | `{levelId, waveReached, durationMs}` | `LevelRunner.SetState(GameOver)` |
| `tower_placed` | `{towerId, world, wave}` | `PlacementController.PlaceTower` |
| `tower_upgraded` | `{towerId, fromLevel, toLevel, branch}` | `Tower.Upgrade / ApplyL3Branch` |
| `tower_sold` | `{towerId, level}` | `PlacementController.SellTower` |
| `wave_cleared` | `{waveIdx, durationMs, streakUsed}` | `WaveManager.OnWaveCleared` |
| `wave_skipped` | `{waveIdx, secondsRemaining}` | `WaveManager.SkipBreak` |
| `achievement_unlocked` | `{id, unlockTotal}` | `Achievements.Unlock` |
| `synergy_triggered` | `{synergyId, towerCount}` | `Synergies.Resolve` |
| `settings_changed` | `{field, newValue}` | `SettingsRegistry.Notify` |

**Pas tracker** :
- Player ID / device ID / IP
- Specifics tower positions (could be PII if user shares)
- Time-of-day / locale exact

### 4.5 Tickets telemetry

#### `MIGRATE-TELEMETRY-01` — Static API + NullBackend + LocalFileBackend (1 commit, 2h)
- Fichier nouveau : `Assets/Scripts/Systems/Telemetry/Telemetry.cs` + `ITelemetryBackend.cs` + `NullBackend.cs` + `LocalFileBackend.cs`
- `Telemetry.LogEvent("level_started", ("levelId", "world1-1"))` API
- `LocalFileBackend` écrit ligne JSON dans `Application.persistentDataPath/telemetry.jsonl`
- Init au boot : `Telemetry.Init()` → si consent ON, charge `LocalFileBackend`, sinon `NullBackend`
- `NullBackend.LogEvent` = no-op zero alloc

#### `MIGRATE-TELEMETRY-02` — Consent screen first launch (1 commit, 2h)
- Fichier nouveau : `Assets/Scripts/UI/TelemetryConsentScreen.cs`
- UI Toolkit overlay au premier lancement (`PlayerPrefs.GetInt("cd.telemetry.consent_asked", 0) == 0`)
- 2 boutons : "Help improve" (OK, set consent ON) / "No thanks" (set OFF)
- Texte clair : "Anonymous usage data only. No personal information. You can change this in Settings anytime."
- Lien hypertext vers `docs/privacy-policy.md` (TODO Phase 5 final)
- Save `cd.telemetry.consent_asked = 1` + `cd.telemetry.consent_value = 0/1`

#### `MIGRATE-TELEMETRY-03` — Wiring events dans gameplay (1-2 commits, 3h)
- Fichier modifié : `Assets/Scripts/Systems/LevelRunner.cs` ajoute `Telemetry.LogEvent("level_started", ...)` dans `Start`
- Fichier modifié : `Assets/Scripts/Systems/WaveManager.cs` ajoute events `wave_cleared`, `wave_skipped`
- Fichier modifié : `Assets/Scripts/Systems/PlacementController.cs` ajoute `tower_placed`, `tower_sold`
- Fichier modifié : `Assets/Scripts/Entities/Tower.cs` ajoute `tower_upgraded`
- Fichier modifié : `Assets/Scripts/Systems/Achievements.cs` Unlock ajoute event
- Fichier modifié : `Assets/Scripts/Systems/Synergies.cs` Resolve ajoute event (1× par activation, pas par tick → cache previous state)
- Fichier modifié : `Assets/Scripts/UI/SettingsRegistry.cs` Notify ajoute event

#### `MIGRATE-TELEMETRY-04` — Settings opt-in toggle (1 commit, 1h)
- Fichier modifié : `Assets/Scripts/UI/SettingsPanelController.cs`
- Ajouter Toggle "Share anonymous usage data" dans section A11y ou nouvelle section "Privacy"
- Wire to `Telemetry.SetConsent(bool)`

#### `MIGRATE-TELEMETRY-05` — Phase 6 GameAnalytics backend (deferred)
- NOT Phase 5. Spec préliminaire :
- Plugin install via Unity Package Manager
- Game key + Secret key dans `Assets/Resources/Settings/GameAnalyticsSettings.asset`
- `GameAnalyticsBackend : ITelemetryBackend`
- Replace `LocalFileBackend` at boot si consent ON et build production
- Garder `LocalFileBackend` en debug/Editor

**Total Phase 5 Telemetry** : ~4 commits, 8h.

### 4.6 Risques telemetry section

- **Risque** : RGPD audit si claim "anonymous" mais le fichier `.jsonl` local contient timestamps + locale (= peut être de-anonymisé). **Mitigation** : limite payload à 5 champs max, pas de timestamp absolu (use waveIdx + relative durationMs).
- **Risque** : Mike refuse consent screen (UX intrusive). **Mitigation** : option A (default OFF) reste valide ; spec Telemetry-02 devient "Settings toggle only, no popup".
- **Risque** : `telemetry.jsonl` grossit indéfiniment sur Steam Cloud. **Mitigation** : rotation après 1 MB → archive + nouveau fichier.

---

## 5. Localization Phase 5 full

### 5.1 État actuel

`/Users/mike/Work/crowd-defense/Assets/Scripts/UI/L.cs` (131 lignes) :
- Dictionary statique hardcoded en/fr
- 36 clés (hud.*, overlay.*, menu.*, settings.*)
- `L.Get(key, table)` lookup + fallback "en"
- `L.SetLocale(code)` + event `OnLocaleChanged`
- **Pas** de format args validation
- **Pas** de fallback key visible si missing
- **Pas** de support RTL (arabe, hebreu)
- **Pas** de CJK font support

### 5.2 Phase 5 needs

- **5 locales** : en, fr, es, de, zh-Hans (la moitié des Steam buyers worldwide)
- **CSV-driven** : Mike + traducteurs peuvent contribuer sans toucher au code
- **TextMeshPro CJK** : font asset chinois (~10 MB texture atlas, common chars subset 3500)
- **Editor preview** : changer la langue dans Inspector sans relancer
- **Missing key warning** : Editor + DEVELOPMENT_BUILD log

### 5.3 Architecture cible : Unity Localization package

**Choix** : `com.unity.localization` (officiel Unity, package manager).

Pourquoi pas garder `L.cs` Dictionary ?
- Scale : 36 keys × 5 locales = 180 entrées dans code → migration assets externalisés inévitable Phase 5+
- CSV/Excel pipeline : Unity Localization import natif via `LocalizationTableImporter`
- TextMeshPro CJK : auto-binding `LocalizeStringEvent` + font selector
- Editor UI : Window > Asset Management > Localization Tables

**Architecture** :
```
Assets/Localization/
├── String Table Collection.asset        # SO root
├── String Table - en.asset              # en locale
├── String Table - fr.asset              # fr locale
├── String Table - es.asset
├── String Table - de.asset
├── String Table - zh-Hans.asset
├── LocalesAvailable.asset               # SO list (en, fr, es, de, zh-Hans)
└── Fonts/
    ├── LatinFonts.fontasset             # Roboto subset Basic Latin + Latin-1
    └── CJKFont-zh.fontasset             # NotoSansSC subset 3500 common chars
```

### 5.4 Tickets localization

#### `MIGRATE-L10N-01` — Install Unity Localization package (1 commit, 30 min)
- Package Manager → `com.unity.localization` v1.5.5+ (Unity 6 LTS compatible)
- Vérifier `Packages/manifest.json`
- Test playmode : compile clean

#### `MIGRATE-L10N-02` — Migrate L.cs Dictionary → String Table en/fr (1 commit, 2h)
- Fichier nouveau : `Assets/Localization/String Table Collection.asset`
- Importer 36 clés × 2 locales depuis `L.cs` dict en CSV
- Tool `Tools/CrowdDefense/Migrate L.cs → Localization` (Editor script one-shot)
- Garder `L.Get(string key, ...)` API mais delegate to `LocalizationSettings.StringDatabase.GetLocalizedString`

#### `MIGRATE-L10N-03` — Add es/de/zh-Hans empty + Crowdin export pipeline (1 commit, 1h)
- 3 nouvelles String Tables vides (placeholders = "[ES] {0}", "[DE] {0}", "[ZH] {0}")
- `Tools/CrowdDefense/Export Strings to CSV` → CSV pour Crowdin/translator
- Import CSV via Unity Localization built-in CSV importer

#### `MIGRATE-L10N-04` — TextMeshPro CJK font asset (1 commit, 2h)
- Télécharger NotoSansSC Regular (~10 MB)
- Generate TMP font asset subset commons chars (3500 chars depuis CC Common SC list)
- Garder asset à `Assets/Fonts/CJKFont-zh.fontasset` (~3-5 MB)
- Hookup fallback : `LatinFonts.fallbackList = { CJKFont-zh }` pour glyphs absents

#### `MIGRATE-L10N-05` — Wire all HUD/overlay/settings UI to LocalizeStringEvent (1-2 commits, 3h)
- Replace `Label.text = L.Get("hud.gold")` calls with UI Toolkit `LocalizeStringEvent` (or static binding)
- Note : UI Toolkit Localization integration moins mature qu'uGUI ; spec utilise `L.Get` wrapper qui auto-refresh sur `LocalizationSettings.SelectedLocaleChanged`

#### `MIGRATE-L10N-06` — Editor missing-key validation (1 commit, 1h)
- Editor script : scan `Assets/Scripts` pour calls `L.Get("...")` (regex)
- Cross-check avec String Table : warn si key dans code mais pas dans table
- Run avant build : `Tools/CrowdDefense/Validate Localization`

#### `MIGRATE-L10N-07` — Decision : drop L.cs Dictionary fallback (1 commit, 30 min)
- Une fois L10N-02 stable, supprimer le `_fallback` Dictionary
- `L.Get` ne fait que delegate
- Garder `L.SetLocale` API pour back-compat HUD callers

**Total Phase 5 L10N** : ~7 commits, 10h.

### 5.5 Risques L10N section

- **Risque** : Unity Localization package conflict avec UI Toolkit (uGUI-first design). **Mitigation** : tester sur HudController seul avant migration full ; fallback wrapper `L.Get` si bug.
- **Risque** : NotoSansSC subset 3500 chars manque encore 5% des phrases marketing. **Mitigation** : dynamic atlas avec fallback rendering (TMP supporte), accepter 1 frame lag glyph generation.
- **Risque** : Traducteurs absents avant ship → "[ES]/[DE]/[ZH]" visible. **Mitigation** : ship en/fr only Phase 5, ajouter autres locales post-launch.

---

## 6. Accessibility full

### 6.1 État actuel

`SettingsRegistry.cs` expose 3 flags A11y :
- `ColorblindMode` (bool) — pas wired downstream encore
- `ReduceMotion` (bool) — pas wired (JuiceFX devrait checker)
- `LargeText` (bool) — pas wired (UI scale devrait checker)

**Pas** de :
- Screen reader hints (NVDA/VoiceOver/TalkBack)
- Keyboard nav full (HUD partial, level select non testé)
- Controller support (Steam Deck est priorité Phase 4)
- Subtitle / caption support
- Audio cue redundancy pour deaf players
- Pause-on-focus-loss (anti-anxiety)

### 6.2 Phase 5 needs

Steam Deck Verified requires :
- Native gamepad support (face buttons, D-pad, sticks)
- All UI navigable without mouse
- Text readable at 800x1280 portrait (Deck native)

GDC accessibility minimums :
- WCAG 2.1 AA contrast (4.5:1 normal text, 3:1 large)
- Subtitles when audio important (level start announcer, wave clear)
- Pause anywhere via gamepad button
- Reduce motion respected (JuiceFX shake/flash/slowmo disabled)

### 6.3 Architecture

```
Assets/Scripts/UI/Accessibility/
├── ColorblindPalette.cs       # SO : remap red→orange, green→blue for daltonien types
├── ReducedMotionGuards.cs     # static helpers : ShakeIfAllowed, FlashIfAllowed
├── KeyboardNavigation.cs      # Tab order + focus rings UI Toolkit
├── GamepadInput.cs            # Input System bindings
└── AccessibilityAudit.cs      # Editor script : check contrast, scan missing alt-text
```

### 6.4 Tickets accessibility

#### `MIGRATE-A11Y-01` — Wire ReduceMotion to JuiceFX (1 commit, 30 min)
- Fichier modifié : `Assets/Scripts/Visual/JuiceFX.cs` Shake/Flash/SlowMo
- Check `SettingsRegistry.Instance?.ReduceMotion == true` → no-op
- Test : enable toggle → no shake observed

#### `MIGRATE-A11Y-02` — Wire ColorblindMode to enemy/tower tints (1 commit, 2h)
- Fichier nouveau : `Assets/Scripts/UI/Accessibility/ColorblindPalette.cs` SO
- 4 palettes : Normal, Protanopia, Deuteranopia, Tritanopia (Mike choisit dropdown au lieu de bool)
- Refactor `SettingsRegistry.ColorblindMode` bool → `ColorblindType enum`
- Wire to `Enemy.Init` body color (use palette remap) + Tower outlines + UI buttons

#### `MIGRATE-A11Y-03` — Wire LargeText to UI Toolkit (1 commit, 1h)
- Fichier modifié : `Assets/Scripts/UI/HudController.cs` + `LevelSelectController.cs`
- Si `LargeText == true` : root `style.fontSize` += 4px, scale buttons
- Test : check no overflow

#### `MIGRATE-A11Y-04` — Keyboard nav full (1-2 commits, 4h)
- UI Toolkit : add `focusable = true` + `tabIndex` à tous les Buttons
- Custom focus ring style (2px yellow outline)
- Tester Tab/Shift+Tab order sur Main + Menu + Settings + Level Select

#### `MIGRATE-A11Y-05` — Gamepad input (Steam Deck) (2 commits, 6h)
- Install Input System package si absent
- Fichier nouveau : `Assets/Settings/Input/CrowdDefenseInputActions.inputactions`
- Bindings : Gamepad/Buttons (A=confirm, B=cancel, Start=pause, Y=upgrade tower, X=sell)
- Refactor `PlacementController.HandleInput` to use action map
- HUD shows on-screen prompts si gamepad detected (`Gamepad.current != null`)

#### `MIGRATE-A11Y-06` — Pause anywhere + focus loss (1 commit, 1h)
- Fichier modifié : `Assets/Scripts/Systems/LevelRunner.cs`
- `OnApplicationFocus(false)` → SetState(Paused)
- Add Pause state to GameState enum
- ESC key + Start button → toggle pause

#### `MIGRATE-A11Y-07` — Accessibility audit Editor tool (1 commit, 2h)
- Fichier nouveau : `Assets/Editor/Tools/AccessibilityAudit.cs`
- Scan : UI Toolkit colors → WCAG contrast ratio
- Scan : audio events without subtitle binding
- Scan : Inputs without keyboard fallback
- MenuItem `Tools/CrowdDefense/A11y Audit` → console report

**Total Phase 5 A11y** : ~8 commits, 16h.

### 6.5 Risques A11y section

- **Risque** : Screen reader (NVDA/VoiceOver) intégration Unity quasi-impossible sans plugin tiers (UAP, paid). **Mitigation** : Phase 5 ship sans screen reader, marquer "Screen reader pending" dans Settings, ne pas claim "fully accessible".
- **Risque** : Gamepad input rework casse mouse input. **Mitigation** : tester en parallèle, Input System supporte les deux nativement.
- **Risque** : Steam Deck Verified review fails sur 1 critère mineur. **Mitigation** : check liste officielle Valve **avant** submit, itérer.

---

## 7. Critères de succès Phase 5 architecture (checklist testable)

### DI / Singletons
- [ ] `MonoSingleton<T>.TryInstance` ajouté + audité par caller (`grep "Instance?\." Assets/Scripts | wc -l` < 30)
- [ ] `[RequiresSceneSetup]` attribute sur 6 singletons avec Inspector deps
- [ ] Editor menu `Tools/CrowdDefense/Build Main Scene` idempotent, run sans warnings
- [ ] PlayMode test `SceneSetupTests.cs` passe en CI batch mode
- [ ] Zéro `NullReferenceException` au boot Main.unity (Editor + DEVELOPMENT_BUILD)

### Save/Load
- [ ] `ISaveBackend` interface + 2 implems (PlayerPrefs + FileSystem)
- [ ] Save versioning v0 → v1 → v2 migrate sans data loss (unit test)
- [ ] CRC32 checksum sur write/read roundtrip valide
- [ ] Backup auto-revert si load fail (test corrupt 1 byte → load `.backup`)
- [ ] SteamCloudBackend wired si Steam SDK Phase 4 disponible (sinon backlog Phase 6)

### Telemetry
- [ ] `Telemetry.LogEvent` API + 11 events wired dans hot zones
- [ ] Consent screen au premier lancement (option B retenue) OU Settings toggle (option A)
- [ ] `LocalFileBackend` écrit `telemetry.jsonl` lisible Mike
- [ ] `NullBackend` confirmé zero-alloc (Profiler 0 GC sur LogEvent)

### Localization
- [ ] Unity Localization package installé sans build errors
- [ ] String Table en/fr migrée from L.cs (36 keys)
- [ ] String Tables es/de/zh-Hans présents avec placeholders
- [ ] CJK font asset 3500 chars chargé < 5 MB final
- [ ] Validation tool warn 0 missing key

### Accessibility
- [ ] ReduceMotion désactive Shake/Flash/SlowMo (test playmode visuel)
- [ ] ColorblindType dropdown remap 4 palettes
- [ ] LargeText scale up HUD 25%
- [ ] Tab keyboard nav couvre Main + Menu + Settings + Level Select
- [ ] Gamepad navigates HUD + pauses + selects (test Steam Deck si dispo)
- [ ] Pause-on-focus-loss fonctionne
- [ ] WCAG audit report < 5 warnings non-blocking

### Performance / regression
- [ ] FPS > 60 desktop Phase 3 baseline maintained
- [ ] GC alloc / frame < 1 KB Profiler (Telemetry NullBackend zero-alloc)
- [ ] WebGL build size < 18 MB (CJK font + Localization tables ajoutent ~3 MB)
- [ ] Boot time < 3s desktop (Localization async load OK)

---

## 8. Effort estimé total Phase 5 architecture

| Section | Commits | Heures | Priorité |
|---|---|---|---|
| DI / MonoSingleton harden | 4 | 4 | P0 (block Phase 5 content) |
| Save/Load robust (sans Steam Cloud) | 6 | 9 | P0 |
| Save/Load Steam Cloud | 1 | 3 | P1 (Phase 4 dep) |
| Telemetry MVP local | 4 | 8 | P1 |
| Localization migration | 7 | 10 | P1 (ES/DE/ZH peut slip Phase 6) |
| Accessibility | 7 | 14 | P1 (Steam Deck requirement) |
| **TOTAL** | **~29 commits** | **~48h** | — |

**Ordre recommandé** :
1. **DI harden** (urgence post-Phase 3 postmortem)
2. **Save migration v1** (avant tout content Phase 5 qui ajouterait fields)
3. **Telemetry local MVP** (instrumenter content au fur et à mesure)
4. **Accessibility ReduceMotion + ColorblindType** (low-cost, high-value)
5. **Localization migration en/fr** (avant ES/DE/ZH translators kickoff)
6. **Save Steam Cloud** (parallèle Phase 4 Steam research)
7. **Gamepad / Steam Deck** (avant Steam Deck Verified submit)
8. **Telemetry GameAnalytics** (deferred Phase 6)

---

## 9. Risques globaux Phase 5 architecture

- **Risque méta** : scope creep. Phase 5 ship = MVP, pas perfection. **Mitigation** : tag chaque ticket `must-ship` vs `nice-to-have`. Tickets `must-ship` = DI harden + Save migration v1 + Telemetry local + A11y ReduceMotion. Le reste est `nice-to-have` deferrable Phase 6.
- **Risque** : Mike refactor sans réviser Phase 3 postmortem P1 (Enemy.UpdateStealth allocs). **Mitigation** : ouvrir tickets `MIGRATE-PERF-XX` parallèles, ce spec ne les couvre pas.
- **Risque** : Unity 6 LTS minor version bump casse Localization package. **Mitigation** : pin `com.unity.localization` version dans `Packages/manifest.json` + ne pas auto-update.
- **Risque** : Mike abandonne consent screen → RGPD non-compliant Steam EU launch. **Mitigation** : ce spec doit avoir réponse "opt-in default ON ou OFF ?" avant kickoff Telemetry-01.

---

## 10. Questions ouvertes pour Mike

1. **Telemetry consent** : opt-in default OFF (option A, conservative RGPD) ou default ON avec screen explicite (option B) ?
2. **Cloud save MVP** : Steam Cloud Phase 5 ship obligatoire, ou peut-on slip Phase 6 (FileSystemBackend only Phase 5) ?
3. **Locales Phase 5 ship** : en/fr only (translators absents) ou bloquer ship jusqu'à ES/DE/ZH translations ?
4. **Steam Deck Verified** : objectif Phase 5 ship ou Phase 6 polish ?
5. **DI container migration** : trigger Phase 6 = > 3 scene-drift bugs, ou trigger autre (eg "je veux tester unitairement Synergies") ?
6. **Telemetry backend Phase 6** : GameAnalytics privilégié, ou Mike préfère Firebase / custom ?

---

**Fin spec Phase 5 architecture improvements.**

Total ~700 lignes, 5 chantiers, ~29 commits atomiques, ~48h estimé.

Référence STATUS.md + CLAUDE.md pour intégration sprint backlog.
