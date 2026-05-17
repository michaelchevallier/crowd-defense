# Drift Report D11 SÉVÈRE — 2026-05-17 12h25

**Trigger** : Mike paste Editor Play mode console + screenshot dans chat session milan project superviseur.
**Drift criteria** : **D11** (runtime exceptions ≥3 nouvelles) confirmé sur 1 check (charter §2 : D10-D11 requires 1 check seulement).

## Symptômes observés

### Console runtime (Play mode 2026-05-17)

| # | Type | Message | Source |
|---|---|---|---|
| 1 | Warning ×25 | `[MonoSingleton] <T> auto-created — missing in scene. Fix scene setup for deterministic init.` | 25 singletons distincts (SettingsRegistry, AudioController, AudioMixerController, PostProcessController, LevelRunner, MusicManager, KeyBindings, WaveManager, Economy, Castle, EventManager, PathManager, HeroPortraitController, Synergies, PlacementController, TowerHoverController, GhostPreviewController, PlacementHighlight, PerkSystem, MetaUpgradeSystem, EnemyPathingSystem, EnemyPool, SchoolSelectController, RunModeController, RunMap) |
| 2 | Error ×4 | `Exposed name does not exist: Master_Volume / SFX_Volume / Music_Volume / UI_Volume` | AudioController.SetMixerMasterVolume + SetSFXVolume + SetMusicVolume + SetUIVolume |
| 3 | Error ×2 | `[WaveManager] No LevelData or no waves` | WaveManager.Start ligne 93 |
| 4 | Error | `[AudioController] AudioClipRegistry not assigned and not found in Resources/` | AudioController.LoadAudioRegistry ligne 66 |
| 5 | Error | `[PathManager] No LevelData assigned` (1× sur 2 init paths) | PathManager.Build ligne 58 |
| 6 | Warning ×2 | `Setting the parent of a transform which resides in a Prefab Asset is disabled to prevent data corruption` | VfxPoolBindings.AttachSubEmitter (audit dit fonction safe ligne 22 — mais 2 occurences runtime à investiguer plus tard) |
| 7 | Error | `[LevelSelectController] level-grid VisualElement not found in UXML.` | LevelSelectController.OnUIReady ligne 69 |
| 8 | Warning | `[TowerToolbar] toolbar-root world rect: width:NaN, height:NaN` | TowerToolbarController.OnUIReady ligne 55 |
| 9 | Warning ×6 | `Runtime cursors other than the default cursor need to be defined using a texture.` | UIElementsRuntimeUtilityNative — Cursor non-default sans texture |
| 10 | Log accidentel | `[WorldMapController] Start() called` + `Root name=HUD-container` | WorldMapController s'attache au mauvais UIDocument quand Main scène se charge |
| 11 | Misc | `The referenced script (Unknown) on this Behaviour is missing!` | 1 GameObject avec script reference cassée dans Main.unity |

### Symptômes visuels (Mike chat)

- "Le jeu marche pas du tout ressemble a rien"
- "Du rose partout" → matériaux URP fallback (shaders Toon cassés)
- "Pas de mob qui apparaisse" → WaveManager n'a pas de LevelData
- "La camera est mal placée"
- "Quand je me déplace ça déplace le bonhomme mais ca déplace aussi la caméra bcp plus vite" → CameraFollow sensitivity ×N
- "L'interface est illisible" → HUD overlay debug + pollution WorldMap content sur HUD-container

## Cause racine (audit Explore Main.unity)

**Main.unity contient 39 GameObjects d'état pré-Phase 5**. `Assets/Editor/BuildMainSceneTool.cs` (70+ `EnsureSingleton` calls, ligne 48-106) **n'a JAMAIS été run post-Phase 5**. De plus, le tool ne couvre pas les 6 contrôleurs UI Phase 5 ajoutés (HeroPortraitController, TowerHoverController, GhostPreviewController, PlacementHighlight, SchoolSelectController, RunModeController).

Singletons manquants en scène → `MonoSingleton<T>.get_Instance()` fallback `FindAnyObjectByType` → `AddComponent` lazy → init order non-déterministe → WaveManager init avant que LevelRunner ait wire son CurrentLevel → "No LevelData or no waves".

Secondaire :
- AudioController.cs appelle `SetFloat("Master_Volume")` mais mixer expose `MasterVol` (mismatch nommage 4 params).
- WorldMapController lazy-loaded se rattache au HUD UIDocument quand Main scène se charge (pas de scene guard).
- Matériaux Toon URP shader pointers cassés post-migration URP 17.
- Main Camera CameraFollow sensitivity non-1.0.
- LevelSelectController cherche `level-grid` renommé/supprimé dans UXML.

## Décision superviseur

1. **T1 push Mike chat** (session milan project) : "Recovery plan posé, à exécuter dans nouvelle session crowd-defense via /goal".
2. **Plan Recovery écrit** : `.claude/plans/phase5-recovery-2026-05-17.md` (6 tickets P0 parallèles + étape Mike post-merge).
3. **Drift report** : ce fichier.
4. **Instruction-to-exec** : pas écrite car Mike va lancer le /goal lui-même dans une session crowd-defense dédiée (workflow Phase 5 PARITY-V4 reproduit).

## Wave R — 6 tickets P0 parallèles

| ID | Titre | Files | Effort |
|---|---|---|---|
| R0-1 | BuildMainSceneTool ajouter 6 UI controllers | Assets/Editor/BuildMainSceneTool.cs | 1 commit, 30 min |
| R0-2 | AudioMixer ExposedName fix (MasterVol etc.) | Assets/Scripts/Systems/AudioController.cs | 1 commit, 15 min |
| R0-3 | WorldMapController guard scene scope | Assets/Scripts/UI/WorldMapController.cs | 1 commit, 30 min |
| R0-4 | Toon materials URP rose retarget + reimport menu | Assets/Materials/Toon/*.mat, Editor menu | 2-3 commits, 1-2h |
| R0-5 | CameraFollow sensitivity 1:1 | Assets/Scripts/Systems/CameraFollow.cs | 1 commit, 30 min |
| R0-6 | LevelSelect level-grid UXML query | Assets/Scripts/UI/LevelSelectController.cs + UXML | 1 commit, 20 min |

**Total** : ~3-5h wall-clock, 6 worktrees parallèles, zones fichiers disjointes.

## Étape Mike post-Wave R

1. `cd /Users/mike/Work/crowd-defense && git pull --rebase`
2. Unity Editor : `Tools > CrowdDefense > Build Main Scene` (R0-1 updated tool)
3. Save Main.unity + commit + push
4. Play mode W1-1 → verify checklist 9 items (cf plan §"Checklist verification Mike")

## Status

- **Drift D11** : confirmé sévère.
- **Notification Mike** : T1 sent (paste chat live).
- **Plan** : posé dans `.claude/plans/phase5-recovery-2026-05-17.md`.
- **Next action** : Mike colle le /goal Recovery dans nouvelle session crowd-defense.
