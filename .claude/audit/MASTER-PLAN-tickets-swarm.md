# Master Plan : V6 → 100% User-Facing Parity (Swarm Tickets)

> Agrégation des 10 audits par couche (`v6-layer-01..10-*.md`). Mike's no-stop directive.
>
> **État actuel** : V6 code-level 96-97% FULL, user-facing **45-65%** (audit `adb68ee`). Mike feedback 23h25 : "gray textures, no animations, no menu, caméra déconne, pas de musique, pas de HUD" — confirmé par audits.

## Synthèse cross-layer

| Layer | Status | Critical findings |
|---|---|---|
| L1 Build/Config | 🟢 OK | GLTFast 6.14.1 (race condition #772), pre-release packages low risk |
| L2 Scenes | 🟠 7 wires manquants | **Castle 4 meshes**, **PlacementController.towerRegistry**, Achievements×2 |
| L3 Assets | 🟢 634 assets | Config-driven (pas Tower.prefab per type), 0 .anim clips (embedded) |
| L4 Scripts | 🟡 OK | Hero monolithique 1581 LOC, Enemy+Tower 8111 LOC (charter violation), cascade init |
| L5 UI | 🟠 RadialMenu cassé | RadialMenuController query 6 elements absents (btn-repair, btn-guard, etc.) |
| L6 Gameplay | 🔴 BuildPoint MISSING | Hero ULT R-key NOT wired, tower placement V4-walk-in regressed |
| L7 Audio | 🟠 4 stings/SFX null | victorySting+defeatSting NULL, castle_lost+boss_roar missing, AudioMixer param mismatch |
| L8 Visual | 🔴 Materials gray | **Textures non assignées aux meshes**, Water/Lava placeholder, T-pose animations |
| L9 Levels | ⏳ pending | (audit en cours) |
| L10 Integration | 🟡 OK | Loader scene orphan, no cheat console, L.cs 47k LOC hardcoded |

## Mike's complaints → Root causes

| Mike's complaint | Root cause | Layer | Ticket |
|---|---|---|---|
| "Gray textures everywhere" | Textures non assignées aux materials des meshes Castle/Enemy/Tower/Ground/Path | L8 + L2 | T-VISUAL-001 |
| "No animations (T-pose)" | AnimationController.SetupAnimator silent no-op si controller introuvable | L8 | T-VISUAL-002 |
| "No menu" | Probablement OK (Menu.unity exists, Build settings OK) — UI controllers Menu pas wired | L2 + L5 | T-UI-001 |
| "Caméra déconne" | ✅ **FIXED** par `de6eec1` (camera pos+rot V4 angles) | L8 | DONE |
| "No music" | ✅ **FIXED** par `cdfe829` (MusicManager + 4 tracks wired). Reste 7 SFX manquants | L7 | T-AUDIO-001 |
| "No HUD visible" | HUD.uxml charge bien (591 nodes post `cd67666`) mais elements visuels gray (textures) | L8 | T-VISUAL-001 |
| "No tower placement V4 walk-in" | BuildPoint mechanic REGRESSED (use hotkey+click only) | L6 | T-GAMEPLAY-001 |
| "No debug level" | Debug HUD F3 OK, mais console API `__cd.*` cheats absent | L10 | T-DEBUG-001 |

## Tickets prioritized (24 tickets P0-P3)

### P0 — Bloquants user-facing visible (4-6h total)

#### T-VISUAL-001 — Wire textures sur materials (CRITIQUE — root cause gray everything)
- **Scope** : Materials de Castle, Enemy, Tower, Ground tiles, Path tiles, Wall meshes
- **Action** : Assign Flux Textures PNG (78 disponibles dans Assets/Resources/Textures/) → m_BaseMap field des materials correspondants
- **Files** : 32 `.mat` files dans Assets/Materials/ + Assets/Resources/
- **Effort** : 2-3h
- **Verification** : Play mode + screenshot → ground/castle/enemies colorés (pas gray)

#### T-VISUAL-002 — Fix Animator controllers (T-pose)
- **Scope** : Hero `Mesh_knight` + 5 hero skins + 28 Enemy types
- **Action** : Verify Animator components present sur prefabs, RuntimeAnimatorController assigned, états Idle/Walk/Attack/Death wired
- **Files** : Prefabs Hero + Enemy + Resources/Animations/Controllers/*.controller
- **Effort** : 2-3h
- **Verification** : Enemies walk animés (pas T-pose ou sliding)

#### T-SCENE-001 — Wire PlacementController.towerRegistry
- **Scope** : Main.unity Systems → PlacementController component
- **Action** : YAML edit `towerRegistry: {fileID: ..., guid: <TowerRegistry.asset GUID>, type: 2}`
- **Effort** : 5min
- **Verification** : Tower placement workflow fonctionne

#### T-SCENE-002 — Wire Castle 4 meshes
- **Scope** : Main.unity Castle GameObject
- **Action** : Find Castle.fbx/obj meshes, assign `_meshIntact/_Cracked/_Ruined/_Critical` fields
- **Effort** : 15min (asset discovery + 4 YAML edits)
- **Verification** : Castle render comme un château (pas cube), change visuel selon HP

#### T-GAMEPLAY-001 — Restore BuildPoint walk-in mechanic
- **Scope** : Create BuildPoint.cs + BuildPoint.prefab + spawning logic in LevelRunner
- **Action** : Cf prompt agent `a74c03d0` deja designé. Hero collider enters BuildPoint trigger → tower picker UI opens → click tower icon → place at cell
- **Effort** : 1-2h (agent `a74c03d0` ready to execute)
- **Verification** : Hero WASD vers BuildPoint circle → tower-select-menu visible → click → tower placed

### P1 — User-facing degraded (3-4h total)

#### T-AUDIO-001 — Assets manquants registry (7 SFX + 2 stings)
- **Scope** : AudioClipRegistry.asset + MusicManager
- **Missing clips** : castle_lost, boss_roar, combo_up, cutscene_start, path_reveal, set_bonus, hero_ult, cancel
- **Missing stings** : victorySting, defeatSting (MusicManager Inspector fields)
- **Action** : (a) Acquérir/générer audio assets ou (b) procedural fallback dans code
- **Effort** : 1-2h
- **Verification** : Boss combat plays boss_roar, game-over plays castle_lost, victory→defeat plays sting

#### T-AUDIO-002 — AudioMixer param naming mismatch
- **Scope** : MainAudioMixer.mixer + AudioMixerController.cs
- **Action** : Renommer exposed params mixer "MasterVol" → "Master_Volume" (etc.) ou update code pour match noms actuels
- **Effort** : 15min
- **Verification** : Volume sliders affectent réellement Master/SFX/Music/UI

#### T-UI-001 — RadialMenuController query non-existent elements
- **Scope** : RadialMenuController.cs OR HUD.uxml
- **Action** : Soit ajouter `btn-repair/guard/research/cancel + upgrade-preview` à HUD.uxml radial section, soit fix RadialMenuController pour utiliser elements existants
- **Effort** : 30min-1h
- **Verification** : Radial menu fonctionne pour repair/guard/research towers

#### T-GAMEPLAY-002 — Hero ULT R-key binding
- **Scope** : KeyBindings.cs + Hero.cs ULT trigger
- **Action** : Add `R` key binding "hero_ult" → Hero.TriggerUlt()
- **Effort** : 15min
- **Verification** : Press R → ULT VFX + projectiles fan + cooldown 30s

#### T-SCENE-003 — Achievements registry wires
- **Scope** : Main.unity Achievements + HUD.AchievementToastController
- **Action** : Wire `registry` → AchievementsRegistry.asset (×2)
- **Effort** : 10min
- **Verification** : Kill 10 enemies → "First Blood" toast pops + counter tracked

#### T-VISUAL-003 — ThemeAmbientConfig SOs
- **Scope** : Create 10 ThemeAmbientConfig ScriptableObjects (1 per theme)
- **Action** : ScriptableObject assets dans Resources/Lighting/ avec sky+equator+ground colors
- **Effort** : 30min
- **Verification** : Theme-driven lighting differences visibles entre Plaine/Volcan/Espace

### P2 — Polish & QA (2-3h total)

#### T-VISUAL-004 — Water/Lava texture frames (8 PNG each)
- **Scope** : Import 8 water frames + 8 lava frames (procedural ou Flux gen)
- **Action** : Resources/Textures/Water_Frames/water_01..08.png, swap WaterLavaAnimController to use textures
- **Effort** : 1h
- **Verification** : Water/Lava animated avec frames PNG (pas placeholder colors)

#### T-AUDIO-003 — 3D audio mixer routing
- **Scope** : AudioController.Play3D
- **Action** : Replace PlayClipAtPoint avec pool AudioSource + outputAudioMixerGroup set
- **Effort** : 30min
- **Verification** : boss_roar volume controlled by SFX slider

#### T-UI-002 — MuteToggleController in scene
- **Scope** : Main.unity HUD ou autre GO
- **Action** : Add MuteToggleController component avec UIDocument wire
- **Effort** : 15min
- **Verification** : Mute button HUD top-right toggle + PlayerPrefs persist

#### T-DEBUG-001 — Cheat console __cd.* API
- **Scope** : Create DebugConsole.cs (RuntimeInitializeOnLoadMethod)
- **Action** : Console window F1 toggle, command parser : unlockAll, addGold N, goto LEVEL, killAll
- **Effort** : 1-2h
- **Verification** : F1 console + commands fonctionnent

#### T-INTEGRATION-001 — Loader scene add to Build Settings
- **Scope** : EditorBuildSettings.asset
- **Action** : Add Loader.unity to scenes list (created by BuildLoaderSceneTool)
- **Effort** : 5min
- **Verification** : Loader scene loaded in WebGL build

### P3 — Architecture & long-term (8-12h total)

#### T-ARCH-001 — Partition Hero.cs (1581 LOC) en 5 partials
#### T-ARCH-002 — Audit PlacementController cascade init
#### T-ARCH-003 — Explicit shutdown phase MonoSingletons
#### T-ARCH-004 — Scene validator missing singletons
#### T-ARCH-005 — Layer collision matrix per Tower/Enemy/UI groups
#### T-INTEGRATION-002 — Externalize L.cs strings to CSV/JSON loader
#### T-INTEGRATION-003 — Atomic SaveSystem transactions
#### T-CONFIG-001 — Update companyName + pin packages

## Plan d'exécution swarm

### Wave A (parallel, P0 - 4-6h)
- Agent A1 (bug-fixer) : T-VISUAL-001 (textures wire)
- Agent A2 (bug-fixer) : T-VISUAL-002 (animators)
- Agent A3 (bug-fixer) : T-SCENE-001 + T-SCENE-002 + T-SCENE-003 (4 scene wires)
- Agent A4 (feature-dev) : T-GAMEPLAY-001 (BuildPoint walk-in) — déjà spawné `a74c03d0`

### Wave B (parallel, P1 - 3-4h, après Wave A)
- Agent B1 : T-AUDIO-001 + T-AUDIO-002 (audio assets + mixer params)
- Agent B2 : T-UI-001 (RadialMenu fix)
- Agent B3 : T-GAMEPLAY-002 (Hero ULT R)
- Agent B4 : T-VISUAL-003 (ThemeAmbientConfig SOs)

### Wave C (parallel, P2 - 2-3h)
- Agent C1 : T-VISUAL-004 (water/lava frames)
- Agent C2 : T-AUDIO-003 + T-UI-002 (3D audio routing + mute UI)
- Agent C3 : T-DEBUG-001 (cheat console)
- Agent C4 : T-INTEGRATION-001 (Loader scene)

### Wave D (sequential, P3 - 8-12h) — sprint suivant
- T-ARCH-001..005 (refacto), T-INTEGRATION-002..003, T-CONFIG-001

## Estimation totale

- **P0 (4 tickets)** : 4-6h → V6 visible parity 45→75%
- **P1 (5 tickets)** : 3-4h → 75→90%
- **P2 (5 tickets)** : 2-3h → 90→95%
- **P3 (8 tickets)** : 8-12h → 95→100% + clean architecture

**Total parité 95%+ : 9-13h en swarm 4-parallèle = 2-3h wall-clock**

## Acceptance Criteria

À chaque wave : Mike retest Unity Editor Play mode, screenshot, compare visuellement à `/v4/` deployed.

**Definition of Done 100%** :
- ✅ 0 NRE/error console (warnings cosmétiques OK)
- ✅ Castle = château texturé (pas cube)
- ✅ Enemy walk animé (pas T-pose)
- ✅ Ground/path tiles texturés (pas gray)
- ✅ Hero WASD + walk into BuildPoint → tower picker → place tower
- ✅ Wave 1-5 cycle complete (spawn → kill → next wave)
- ✅ Music plays (4 tracks per scene/state)
- ✅ SFX fire (tower shoot, enemy hit, castle hit)
- ✅ Menu → WorldMap → Main → result → next level navigation
- ✅ Achievements toast on first kill
- ✅ Volume sliders work (Master/SFX/Music/UI)
- ✅ Mute button toggle
- ✅ Hero ULT R-key triggers
- ✅ Radial menu repair/guard/research towers
- ✅ Debug F3 HUD + F1 cheat console accessible
