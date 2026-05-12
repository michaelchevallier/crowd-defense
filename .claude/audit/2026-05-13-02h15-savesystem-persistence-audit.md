# SaveSystem Persistence Audit — V6 (post R6-PARITY-V4)

Date : 2026-05-13 02h15
Scope : `Assets/Scripts/Systems/SaveSystem.cs` + cross-ref all systems needing persistence.

## API SaveSystem (résumé)

JSON slot-based (3 slots A/B/C), `ProgressData v3` + `RunState` + `RunMapState` + `MidLevelStateData`. Migration v1→v2→v3 avec backup. Keys : `cd_progression_v2_slot{0..2}`, etc. Cache mémoire par slot. `ExportToJson` / `ImportFromJson` pour player backup.

## Table per système

| Système | SaveSystem API | PlayerPrefs direct | Status |
|---|---|---|---|
| LevelProgression (cleared/unlocked/stars) | y (Load.clearedLevels, levelStars) | n | OK |
| MetaUpgradeController | y (GetMetaUpgradeLevel/Set) | n | OK |
| SkinSystem (owned + equipped) | y (IsSkinOwned/UnlockSkin/SetEquippedSkin) | n | OK |
| Gems (currency v3) | y (GetGems/AddGems/SpendGems) | n | OK |
| Tutorial completion | y (IsTutorialCompleted) | n (mais `tutorial_done_v1` aussi via TutorialState) | DUP/drift |
| RunState (hero perks/XP) | y (GetRunState/SetRunState) | n | OK |
| MidLevelState (resume) | y (SaveRunState/LoadRunState) | n | OK |
| RunMap (graph roguelike) | y (Get/Set/ClearRunMapState) | n | OK |
| Leaderboard endless (v2) | y (AddLeaderboardEntry) | n | OK |
| Hardcore unlock | y (UnlockHardcore/IsHardcoreUnlocked) | n | OK |
| Hardcore session flag | n | y (`cd.gamemode.hardcore`) | volontaire (session) |
| Lifetime stats (kills/gold/playtime) | y (AddKills/AddGoldEarned…) dans ProgressData | y (`total_kills_lifetime_v1`, …) via LifetimeStats | **DUP/drift** |
| Settings (volume/locale/quality) | partiel (musicVolume/sfxVolume/lang dans ProgressData) | y (38 keys via SettingsRegistry) | **DUP/drift** |
| Achievements (57 unlocked) | n | y (`cd.achievements.unlocked`, `cd.achievements.order`, `cd.ach.counter.*`) | **GAP : hors slots** |
| HiddenAchievementTracker | n | via Achievements PlayerPrefs | hors slots |
| PerkSystem (run perks) | y (via RunState.heroPerks) | n | OK (in-run) |
| PlayerProfile (name + first run) | n | y (`player_name_v1`, `player_first_run_v1`) | **GAP : hors slots** |
| TalentSystem (5 talents + points) | n | y (`cd.talent.*.lvl`) | **GAP : hors slots** |
| DoctrineSystem (gems + active doctrine) | n | y (`cd.doctrine.*`, `cd.gems.*`) | **GAP** + duplique Gems SaveSystem |
| TowerResearchTree | n | y (`cd.research.{tower}.{node}`) | **GAP : hors slots** |
| Daily challenge (date+score) | n | y (`cd.daily.YYYY-MM-DD.score`, completed flags) | hors slots (volontaire daily) |
| Bestiary (enemies seen) | n | y (`bestiary_v1`) | **GAP : hors slots** |
| HighScores (autre leaderboard) | n | y (`cd.highscores`) | **GAP** + duplique leaderboard SaveSystem |
| KeyBindings | n | y (`keybind_{action}_v1`) | hors slots (settings) |
| TutorialState (vs SaveSystem.tutorial) | n | y (`tutorial_done_v1`) | DUP/drift |
| Hint flags | y (IsHintSeen/MarkHintSeen) | direct PlayerPrefs `cd_hint_*` | hybride (global) |
| LifetimeStats Today (runs/kills/time) | n | y (`cd.today.*`) | OK (daily reset, hors slots) |
| Per-tower stats (placed/kills) | n | y (`cd.tower_stats.*`) | **GAP : hors slots** |
| WorldMap bookmarks | n | y (UI/WorldMapController BookmarkKey) | hors slots (UI helper) |
| BossIntroBanner seen | n | y (per boss key) | hors slots (UI helper) |
| Tutorial popup seen | n | y | hors slots (UI helper) |
| Splash skip | n | y (`SplashScreen.SkipKey`) | hors slots (UI helper) |
| QuickSave (UI/QuickSaveHotkey) | n | y (`QUICKSAVE_KEY`) | parallèle MidLevel — **DUP** |

## Top 3 gaps

1. **Achievements (57) hors SaveSystem** — `cd.achievements.unlocked` est PlayerPrefs CSV global, jamais slot-aware. Le slot switch n'isole pas les achievements ; suppression slot ne reset pas ; pas de migration ; data loss si user clear PlayerPrefs partiel.
2. **Settings + LifetimeStats double-écrits** — volumes/playtime/kills stockés dans `ProgressData` (musicVolume/sfxVolume/playtime/totalKills) **et** PlayerPrefs `total_kills_lifetime_v1` + `SettingsRegistry.K*`. Sources of truth divergent silencieusement (le code n'écrit jamais ProgressData.musicVolume — il reste à 1f). Code utilise SettingsRegistry à 100% → champs ProgressData sont dead weight et trompeurs.
3. **TalentSystem + TowerResearchTree + DoctrineSystem + Bestiary hors slots** — progression "méta" V6 non isolée par slot. Switcher slot A→B donne accès aux talents/research/doctrine du slot A. Aucune migration handling, drift de keys hard-coded en 4 endroits.

## Reco fix priorité

- **P0** : migrer Achievements unlocked + order + counters dans `ProgressData` (List<string> achievements + List<HiddenCounter>). Casse compat — prévoir migrate v3→v4 lecture des prefs legacy.
- **P0** : décider source of truth Settings — soit purger volume/playtime/kills/lang/tutorialCompleted de `ProgressData` (et migrer v3→v4), soit unifier SettingsRegistry sur SaveSystem. **Préférer purger ProgressData** car SettingsRegistry est global (cross-slot) par design.
- **P1** : déplacer TalentSystem + TowerResearchTree + DoctrineSystem + Bestiary + per-tower stats dans `ProgressData` ou créer slot-aware wrapper. Sinon documenter explicitement "ces systèmes sont cross-slot".
- **P1** : supprimer doublons gems (DoctrineSystem.GEMS_KEY vs SaveSystem.Load().gems) + leaderboard (HighScores vs ProgressData.endlessLeaderboard) — un seul writer.
- **P2** : QuickSaveHotkey `QUICKSAVE_KEY` parallèle à `MidLevelKey` — unifier via `SaveSystem.SaveRunState(MidLevelStateData)`.
- **P2** : ajouter `[Header]` "WARNING dead fields" sur champs ProgressData non-écrits, ou les retirer en v4.
