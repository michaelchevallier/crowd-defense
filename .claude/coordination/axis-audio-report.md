# Axis B AUDIO-PIPELINE — Stage A Report

**Date** : 2026-05-11
**Sub-Opus** : SO-AUDIO (Opus 4.7 orchestrator)
**Branch** : `axis/audio` (pushed origin, 4 commits ahead of `main@a6540cb`)
**Status** : Stage A **DONE**, ready for QA-3 pre-merge gate + Stage B integration hooks.

---

## Deliverables livrés

### B.1 — AudioMixer asset 4 groupes + 4 exposed params + 3 snapshots ✅

Asset : `Assets/Audio/MixerGroups.mixer`
- Groupes : Master / SFX / Music / UI
- Exposed parameters : `MasterVol`, `SFXVol`, `MusicVol`, `UIVol` (dB log [-80, 0])
- Snapshots : Gameplay (default), Paused, GameOver

Editor tool : `Assets/Editor/AudioMixerBuilder.cs` — MenuItem `Tools/CrowdDefense/Build AudioMixer`. Reconstruit le mixer programmatiquement via reflection sur `UnityEditor.Audio.AudioMixerController` (Unity n'expose pas d'API publique pour créer un AudioMixer en code).

Rationale : reflection sur API interne. Documenté en commentaires. Stable Unity 6.x, à re-vérifier si upgrade Unity 7+. Cf code `BuildAudioMixer.cs:23-115`.

### B.2 — Music ambient track import ✅

Source disponible : `/Users/mike/Work/milan project/src-v3/public/audio/music/gameplay_calm.mp3` (5.5 MB).
Cible : `Assets/Audio/Music/Ambient_W1.mp3` + meta.

**Note** : la brief initiale indiquait "Phaser n'avait pas Music.js". Faux — milan project a 6 tracks `.mp3` dans `src-v3/public/audio/music/` (gameplay_calm, gameplay_intense, boss_theme, menu_theme, defeat_sting, victory_sting). J'ai porté `gameplay_calm` comme placeholder Phase 3. Phase 4 : import les 6, route via boss snapshot pour ducking.

### B.3 — AudioController extensions (contrat C1) ✅

`Assets/Scripts/Systems/AudioController.cs` étendu :

API conforme C1 :
```csharp
public void Play(string clipKey, float volMul = 1f);
public void PlayRandom(string[] keys, float volMul = 1f);
public void PlayMusic(AudioClip clip, float fadeMs = 500f);   // NEW: fade param
public void StopMusic(float fadeMs = 500f);                   // NEW
public void SetMasterVolume(float zeroToOne);                 // updated: mixer route
public void SetSFXVolume(float zeroToOne);                    // NEW
public void SetMusicVolume(float zeroToOne);                  // NEW
public void SetUIVolume(float zeroToOne);                     // NEW (extension non-breaking)
public void SetMuted(bool muted);
public void LoadAudioRegistry();                              // NEW: lazy Resources fallback
```

Détails :
- Routing : 8 AudioSources `_sfxPool` routées vers `sfxGroup` au `OnAwakeSingleton`, `_musicSource` vers `musicGroup`.
- `LinearToDb` : conversion 0-1 → dB log clampée [-80, 0].
- Music fade : coroutine `Time.unscaledDeltaTime` (pause-safe).
- Anti-replay 28ms (`MinReplayInterval`) conservé du commit `bff9888`.
- `LoadAudioRegistry()` fallback `Resources.Load<AudioClipRegistry>("AudioClipRegistry")` — pour l'instant le registry n'est pas dans Resources/, donc fallback fail mais Inspector binding fonctionne (cf B.4).

### B.4 — AudioClipRegistry populé + binding scène ✅

`Assets/ScriptableObjects/Audio/AudioClipRegistry.asset` — 21 entrées :
- 20 SFX (achievement, blue_pill, boom, boss_charge, castle_hit, coin_pickup, enemy_die_basic/medium/boss, enemy_hit, gem_gain, hero_shoot, level_up, perk_pick, skin_equip, tower_built, tower_shoot, tower_upgrade, wave_clear, wave_start)
- 1 music (`music_ambient_w1` → Ambient_W1.mp3)

`AudioClipRegistryTool.cs` étendu pour scanner `Assets/Audio/SFX` + `Assets/Audio/Music` (préfixe `music_` pour distinguer).

`Assets/Scenes/Main.unity` : nouveau GameObject `Systems/AudioController` avec composant `AudioController` binded :
- `registry` → `AudioClipRegistry.asset`
- `mixer` → `MixerGroups.mixer`
- `sfxGroup` → Master/SFX
- `musicGroup` → Master/Music
- `uiGroup` → Master/UI

---

## Commits sur axis/audio

```
5f75755 chore(audio): peupler AudioClipRegistry.asset + bind AudioController dans Main.unity
c9dec36 feat(audio): AudioController extensions C1 contract + AudioClipRegistry music scan
47f2d28 chore(audio): import Ambient_W1 music track (5.5 MB) depuis milan project
081659e feat(audio): AudioMixerBuilder + MixerGroups.mixer 4 groups + 3 snapshots
```

Pushed origin `axis/audio` ↦ ready for QA-3 + integration.

---

## QA-3 self-check (pre-merge)

| Item | Status |
|---|---|
| Aucune hot zone touchée (`Tower.cs`, `Enemy.cs`, `Castle.cs`, `WaveManager.cs`, `LevelRunner.cs`, `Economy.cs`, `BalanceConfig.cs`, `Packages/manifest.json`) | ✅ |
| Files modifiés tous dans zone axis/audio (`Assets/Audio/*`, `AudioController.cs`, `AudioClipRegistry.cs`, `AudioClipRegistryTool.cs`, `AudioMixerBuilder.cs`, `ScriptableObjects/Audio/*`, `Scenes/Main.unity`, `plans/axis-audio.md`) | ✅ |
| Compile Unity batch (audio scripts) : 0 errors | ✅ |
| AudioController API conforme C1 (Play/PlayRandom/PlayMusic/StopMusic/SetMasterVolume/SetSFXVolume/SetMusicVolume/SetMuted) | ✅ |
| Extensions documentées (SetUIVolume, LoadAudioRegistry, fadeMs sur PlayMusic) | ✅ |
| AudioClipRegistry .asset 21 entrées | ✅ |
| AudioMixer .mixer asset 4 groupes + 4 params + 3 snapshots | ✅ |
| Commits atomiques + conventional + footer Co-Authored-By | ✅ |
| Music asset présent (`Ambient_W1.mp3`) | ✅ |

**STATUS.md non touché par mes commits** (diff vs main vient du fork base `a6540cb` plus ancien — Mike a updated STATUS.md sur main HEAD `0d9bb3b` après mon fork ; Stage B 3-way merge le résoudra naturellement vers la version main).

---

## Caveats / known limitations

1. **AudioMixer via reflection internal API** : fragile aux upgrades Unity majeurs. Re-vérifier si upgrade Unity 7+. Test inverse : `BuildAudioMixer` MenuItem fonctionne ponctuellement, on peut le re-run pour rebuild.

2. **Music track placeholder** : `Ambient_W1.mp3` = `gameplay_calm.mp3` du milan project (CC0 via générateur original). Phase 4 polish : Mike peut remplacer par track Phase 4 dédiée (drop dans `Assets/Audio/Music/Ambient_W1.mp3`, re-run `Build AudioClipRegistry`).

3. **MP3 import settings non optimisés** : `Ambient_W1.mp3` importé avec settings Unity defaults (Decompress on Load, RAM heavy ~5 MB chargé). Optimisation Phase 4 : `loadType = Streaming` + `preloadAudioData = false` pour budget mémoire mobile. Pas urgent Phase 3 MVP.

4. **AudioController non dans Resources/** : si la scène Main.unity ne pré-binde pas AudioController (e.g. scène Menu chargée en premier), le fallback `Resources.Load<AudioClipRegistry>("AudioClipRegistry")` retournera null. Phase 4 fix : déplacer `AudioClipRegistry.asset` vers `Assets/Resources/`, OU rendre AudioController un singleton DontDestroyOnLoad chargé bootstrap. Pas bloquant — Main.unity binding suffit pour Phase 3.

5. **`UI_Group`** : aucun AudioSource n'est routé vers UI group pour l'instant (Axis F UX devra acquérir des `AudioSource` UI dédiés sliders/buttons via Axis F's UXML hooks).

6. **STATUS.md drift** : ma branche `axis/audio` a un STATUS.md plus ancien que `main` parce que le fork s'est fait sur `a6540cb`. Stage B merge utilisera la version main (3-way merge), pas la mienne. Confirmé : aucun de mes 4 commits ne touche STATUS.md.

---

## Coordination avec autres axes

**Axis F UX SettingsPanel** : a maintenant tout ce qu'il faut pour binder les sliders volume :
```csharp
// Pseudo-code Axis F UX
masterSlider.RegisterValueChangedCallback(evt => AudioController.Instance?.SetMasterVolume(evt.newValue));
sfxSlider.RegisterValueChangedCallback(evt => AudioController.Instance?.SetSFXVolume(evt.newValue));
musicSlider.RegisterValueChangedCallback(evt => AudioController.Instance?.SetMusicVolume(evt.newValue));
uiSlider.RegisterValueChangedCallback(evt => AudioController.Instance?.SetUIVolume(evt.newValue));
muteToggle.RegisterValueChangedCallback(evt => AudioController.Instance?.SetMuted(evt.newValue));
```

**Stage B Integrator** : peut appliquer les hooks audio canon `AudioController.Instance?.Play("tower_shoot", 0.55f)` etc dans Tower.cs/Enemy.cs/Castle.cs/etc selon `integration-spec.md` §I1-I6. Music ambient bootstrap dans LevelRunner.OnLevelStart() :
```csharp
var music = registry?.Get("music_ambient_w1");
if (music != null) AudioController.Instance?.PlayMusic(music, 800);
```

(Note : si Mike veut référencer le music asset directement via BalanceConfig — qui est hot zone — un REQUEST file est à créer pour MO d'ajouter le champ `ambientMusic` à BalanceConfig comme spécifié dans `integration-spec.md` §I7. Alternative cleaner : LevelRunner consume via registry key, pas via BalanceConfig.)

---

## REQUEST to Main Orchestrator (optional)

Pour finaliser proprement le binding Phase 3 :

**REQ-1** (low priority) : si MO préfère le pattern `BalanceConfig.AmbientMusic` (cf integration-spec §I7), modifier `BalanceConfig.cs` (hot zone) pour ajouter :
```csharp
[Header("Audio Phase 3")]
[SerializeField] private AudioClip? ambientMusic;
public AudioClip? AmbientMusic => ambientMusic;
```
+ assign `BalanceConfig.asset` Inspector → `Ambient_W1.mp3`.

Sinon, le pattern registry-key (`registry.Get("music_ambient_w1")`) marche très bien sans modifier BalanceConfig.

**REQ-2** (no-op) : Axis E BUILD a actuellement 2 erreurs compile dans `BuildScript.cs` (lignes 61 + 201) non commitées sur main. N'a pas bloqué mes deliverables (compilation incrémentale tolère), mais MO devrait coordonner Axis E pour fix avant merge axis/audio (sinon WebGL build batch CI échouera).

---

## Budget

Estimé 4-6h. Réel : ~30 min (work done directement par SO-AUDIO sans spawn Sonnet — tâches assez simples + risk de revert+race par Sonnet sur worktree non-isolé). Décision : pas de Sonnet spawned, work executed directly via Unity-MCP `execute_code` + `manage_gameobject` + `manage_components`.

---

## Fin Stage A

✅ Stage A complete pour axis/audio. Branch pushed origin. Ready for MO arbitrage QA-3 + merge into `integration/phase3-4-5`.
