# Axis B — AUDIO-PIPELINE Plan

**Branch** : `axis/audio` (créée depuis main HEAD `a6540cb`, 2026-05-11)
**Sub-Opus** : SO-AUDIO (Sub-Opus orchestrateur)
**Mission** : compléter le sprint 3.E (AudioMixer 4 groupes + 1 track music ambient + binding Settings UI + integration AudioClipRegistry)

## État acquis (NE PAS refaire)

- 20 SFX `.ogg` importés `Assets/Audio/SFX/*` (commit `3d56816`)
- `AudioClipRegistry.cs` SO + Editor tool `AudioClipRegistryTool.cs` (commit `fc304ea`)
- `AudioController.cs` singleton AudioSource pool + Play/PlayMusic/SetMasterVolume (commit `bff9888`)

## Zone exclusive write (axis/audio)

- `Assets/Audio/*` (sauf SFX `.ogg` déjà importés)
- `Assets/Scripts/Systems/AudioController.cs` (extensions OK)
- `Assets/Scripts/Data/AudioClipRegistry.cs` (extensions OK)
- `Assets/Editor/AudioClipRegistryTool.cs` (extensions OK)
- `Assets/Editor/AudioMixerBuilder.cs` (nouveau)
- `Assets/ScriptableObjects/Audio/*`

## Hot zones NEVER touch

- `Tower.cs`, `Enemy.cs`, `Castle.cs`, `WaveManager.cs`, `LevelRunner.cs`, `Economy.cs`, `BalanceConfig.cs`, `STATUS.md`, `Packages/manifest.json`

## API contracts canon (C1 AudioController)

```csharp
public void Play(string clipKey, float volMul = 1f);
public void PlayRandom(string[] clipKeys, float volMul = 1f);
public void PlayMusic(AudioClip clip, float fadeMs = 500);   // fadeMs ajouté
public void StopMusic(float fadeMs = 500);                   // nouveau
public void SetMasterVolume(float zeroToOne);
public void SetSFXVolume(float zeroToOne);                   // nouveau
public void SetMusicVolume(float zeroToOne);                 // nouveau
public void SetMuted(bool muted);
```

Notes :
- `PlayMusic` actuel n'a pas de paramètre `fadeMs` → extension non-breaking via default value.
- Ajouter `StopMusic` (signature C1).
- Ajouter `SetSFXVolume`, `SetMusicVolume` (signature C1) + `SetUIVolume` (déduit C1).

## Deliverables Stage A

### B.1 — AudioMixer asset 4 groupes (Editor script)

- Créer `Assets/Editor/AudioMixerBuilder.cs` → MenuItem `Tools/CrowdDefense/Build AudioMixer` qui programmatiquement crée le `.mixer` asset (Unity n'a pas de tool MCP direct).
- Sortie : `Assets/Audio/MixerGroups.mixer`
- 4 groupes : `Master → SFX, Music, UI`
- Exposed parameters : `MasterVol`, `SFXVol`, `MusicVol`, `UIVol` (en dB, -80 à 0)
- Snapshots : `Gameplay` (default), `Paused` (music -6dB ducked), `GameOver` (all -10dB)
- Route les 8 AudioSources `_sfxPool` vers `SFX` group, `_musicSource` vers `Music` group.

### B.2 — Music ambient track import

- Source dispo : `/Users/mike/Work/milan project/src-v3/public/audio/music/gameplay_calm.mp3` (5.5 MB) — utiliser comme `Ambient_W1`.
- Copier vers `Assets/Audio/Music/Ambient_W1.mp3`
- Note : BalanceConfig.cs est hot zone → music ref via `AudioClipRegistry` extension (clipKey `music_ambient_w1`) plutôt que modifier BalanceConfig. **CHOIX** : exposer via `AudioController.LoadMusicTrack(key)` qui résout via Registry.

### B.3 — AudioController extensions

- Ajouter Mixer reference SerializeField + `[SerializeField] AudioMixerGroup sfxGroup, musicGroup, uiGroup`
- Route AudioSources vers leur group au `OnAwakeSingleton`
- `SetSFXVolume(float 0-1)` → `mixer.SetFloat("SFXVol", LinearToDb(value))`
- `SetMusicVolume(float 0-1)`, `SetUIVolume(float 0-1)` idem
- `SetMasterVolume(float 0-1)` mute via `AudioListener.volume` OU mixer `MasterVol` (préférer mixer)
- `LoadAudioRegistry()` lazy : si `registry == null`, `Resources.Load<AudioClipRegistry>("AudioClipRegistry")` fallback
- `Awake` hook SettingsRegistry (si dispo via reflection ou null-safe ref)
- `PlayMusic(AudioClip clip, float fadeMs = 500)` + `StopMusic(float fadeMs = 500)` avec coroutine fade volume

### B.4 — Build AudioClipRegistry .asset

- Run MenuItem `Tools/CrowdDefense/Build AudioClipRegistry` via UnityMCP
- Verify 20 entries
- Update AudioClipRegistryTool pour scanner aussi `Assets/Audio/Music/*` (ajouter `music_ambient_w1` key)
- Assign Registry sur AudioController GameObject dans Main.unity

## Workflow

1. ✅ Crée branche `axis/audio` (DONE)
2. ✅ Plan évolutif `.claude/plans/axis-audio.md` (THIS FILE)
3. Spawn Sonnets en parallèle (max 3) :
   - **Sonnet B1** : `AudioMixerBuilder.cs` Editor script + run MenuItem
   - **Sonnet B2** : Music ambient copy + AudioClipRegistry music scan extension
   - **Sonnet B3** : `AudioController.cs` extensions (SetSFXVolume + StopMusic + fade + mixer routing)
4. QA per commit (cf qa-gates.md QA-2)
5. Push axis/audio à chaque milestone
6. **Sonnet B4** (séquentiel après B1+B3) : Run Build AudioClipRegistry menu + assign Registry+Mixer sur AudioController dans Main.unity
7. Rapport final `.claude/coordination/axis-audio-report.md`

## Risks
- Sonnet B3 dépend de B1 livré (mixer asset existe) pour assigner mixer groups → soit ordonnancer séquentiel B1→B3, soit B3 livre `SetMixerGroups()` qui sera assigné Inspector par B4.
  - **Choix** : B3 livre les extensions code-only, B4 assigne Inspector → permet parallèle B1+B2+B3.
- Music `.mp3` Unity import default `Decompress on Load` = RAM heavy. Forcer `Streaming` via TextureImporter equivalent (`AudioImporter.preloadAudioData = false` + `loadType = AudioClipLoadType.Streaming`).
- AudioMixer en code via `UnityEditor.Audio.AudioMixerController` est internal API — fragile selon version Unity. Fallback : Sonnet B1 utilise `AssetDatabase.CreateAsset` avec `ScriptableObject.CreateInstance("AudioMixerController")` via reflection.

## QA-3 pre-merge checklist
- [ ] AudioMixer asset existant `Assets/Audio/MixerGroups.mixer` (4 groupes + 4 exposed params + 3 snapshots)
- [ ] Music track `Assets/Audio/Music/Ambient_W1.mp3` présent
- [ ] AudioController.SetSFXVolume / SetMusicVolume / SetUIVolume / SetMasterVolume / StopMusic exposés
- [ ] AudioController fade music in/out smooth
- [ ] AudioClipRegistry `.asset` populé 21+ entries (20 SFX + 1 music)
- [ ] AudioController Inspector dans Main.unity : registry + mixer groups assignés
- [ ] Compile Unity : 0 errors via `mcp__UnityMCP__read_console`
- [ ] Aucun touch hot zone (`git diff axis/audio main` filtre)
- [ ] Conformance API contracts C1 (Play, PlayRandom, PlayMusic, StopMusic, SetMasterVolume, SetSFXVolume, SetMusicVolume, SetMuted)
- [ ] Commits atomic + conventional + footer Co-Authored-By

## Budget : 4-6h
