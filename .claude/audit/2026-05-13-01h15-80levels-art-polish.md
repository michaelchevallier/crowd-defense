# 80 Levels — Art Polish Theme-Asset Mapping Audit

**Date** 2026-05-12 (logical 2026-05-13 01h15) — N22 lecture-seule.
**Source** 90 LevelData assets (`Assets/ScriptableObjects/Levels/W{1-10}-{1-9}.asset`).
**Mapping** 1 monde = 1 thème (90 levels = 10 themes × 9 per world).

## Inventaire thèmes vs assets disponibles

| Theme | Skybox.mat | Sky tex | Ground.mat | Path.mat | Decor palette | Weather preset | LevelThemeMat config |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Plaine (W1) | OK | OK | OK | OK | OK | Clouds | OK (theme 0) |
| Foret (W2) | OK | OK | OK | OK | OK | Pollen+Mist | OK (theme 1) |
| Desert (W3) | OK | OK | OK | OK | OK (rock-only) | Dust+Wind | **MISSING** (theme 2) |
| Volcan (W4) | OK | OK | OK | OK | OK (rock-heavy) | Embers+Ash | OK (theme 3) |
| Foire (W5) | OK | OK | OK | OK | sparse (4 keys) | Confetti+Pollen | **MISSING** (theme 9) |
| Apocalypse (W6) | OK | OK | OK | OK | OK (rich, 18+ keys) | Dust+Ash+Wind | **MISSING** (theme 4) |
| Espace (W7) | OK | OK | OK | OK | OK | Stars+Snow | **MISSING** (theme 5) |
| Submarin (W8) | OK | OK | OK | OK | OK (sparse fish) | Bubbles | OK (theme 6) |
| Medieval (W9) | OK | OK | OK | OK | OK (rich, 16+ keys) | Dust+Wind | OK (theme 7) |
| Cyberpunk (W10) | OK | OK | OK | OK | OK | NeonRain+Wind | **MISSING** (theme 8) |

## Sample 10 levels (1 par monde)

| Level | Theme | World HP | Cellsize | gridVariants | Briefing | Cutscene |
| --- | --- | --- | --- | --- | --- | --- |
| W1-1 | plaine | 150 | 4 | empty | empty | world1 |
| W2-5 | foret | 150 | 4 | OK | OK | — |
| W3-9 | desert | 195 | 4 | OK | OK | — |
| W4-2 | volcan | 185 | 4 | OK | OK | — |
| W5-6 | foire | 200 | 4 | OK | OK | — |
| W6-4 | apocalypse | 320 | 4 | OK | OK | — |
| W7-8 | espace | 350 | 4 | OK | OK | — |
| W8-5 | submarin | 250 | 4 | OK | OK | — |
| W9-2 | medieval | 260 | 4 | OK | OK | — |
| W10-9 | cyberpunk | 290 | 4 | OK | OK | — |

## Anomalies détectées

### A1 — LevelThemeMaterialConfig **5/10 themes manquants** (CRITICAL)
`Assets/Resources/LevelThemeMaterialConfig.asset` n'a que 5 entries (theme 0/1/3/6/7).
Missing : Desert (2), Apocalypse (4), Espace (5), Cyberpunk (8), Foire (9).
Impact : ces 5 thèmes (45 levels = 50% du contenu) tombent en fallback `Toon_Default`/`Toon_Water`/`Toon_Lava` au lieu de variants thématiques. Ground mats existent dans `Resources/Materials/` mais ne sont jamais référencés via le SO.

### A2 — `gridVariants` vides sur 6 levels W1+W2 (LOW)
W1-1..W1-5, W2-1 n'ont pas de variants alternatifs de pathing.
Impact : Pas de replay variety, mais acceptable pour tutoriel/onboarding.

### A3 — `briefing` vide pour les 10 W*-1 (LOW)
Tous les premiers niveaux de chaque monde ont `briefing: ""` ; intentional (cutscene `worldN` joue à la place). Vérifier que CutsceneController couvre les 10 worlds.

### A4 — `waveEvents` vide partout (0/90) (MEDIUM)
La feature dynamic events R6-PARITY-012 n'est wirée sur aucun level. SandStorm / NeonOverload / etc. inactifs en pratique malgré le code WeatherController.SpawnPreset prêt.

### A5 — DecorPalette Foire et Submarin pauvres (LOW)
Foire : 4 SmallKeys, partage `nature_*` (pas thématique).
Submarin : seulement 2 BigKeys (shipwreck + pine reused).
Visuellement moins "signature" que Apocalypse (18 keys) ou Medieval (16 keys).

## Top 5 polish opportunities priorisées

| # | Priority | Effort | Action |
| --- | --- | --- | --- |
| 1 | **HIGH** | 30 min | Ajouter 5 entries manquantes dans `LevelThemeMaterialConfig.asset` (Desert/Apocalypse/Espace/Cyberpunk/Foire) — refs ground+path mats existants dans `Resources/Materials/` |
| 2 | **MEDIUM** | 1h | Wirer waveEvents R6-PARITY-012 sur ~10 levels marquants (e.g. W3-5 SandStorm, W7-7 SolarFlare, W10-8 NeonOverload) |
| 3 | **MEDIUM** | 45 min | Enrichir DecorPalette Foire (ajouter assets foire spécifiques : barbe-à-papa, manège, stand) et Submarin (corail, algues, sub) |
| 4 | LOW | 20 min | Ajouter gridVariants pour W1-1..W1-5+W2-1 (au moins 1 variant chacun pour replay variety) |
| 5 | LOW | 15 min | Vérifier les 10 cutscenes `worldN` existent dans `CutsceneRegistry.asset` et jouent au start de chaque W*-1 |

## Reco par thème (top 3)

1. **Foire (W5)** — needs : LevelThemeMatConfig entry + decor enrichissement (4 → 12+ keys signature foire) + ground variant `_v2` underutilisé.
2. **Apocalypse (W6)** — already rich décor (18 keys), juste needs LevelThemeMatConfig entry (theme 4 missing).
3. **Cyberpunk (W10)** — needs LevelThemeMatConfig entry (theme 8 missing) + waveEvent NeonOverload pour livrer fantasy Néo-Tokyo (W10-9 "Protocole Delta" actuellement plat).

## Files audités (lecture seule)

- `Assets/ScriptableObjects/Levels/*.asset` (90 files)
- `Assets/Resources/LevelThemeMaterialConfig.asset`
- `Assets/Scripts/Data/{LevelData,LevelTheme,LevelThemeMaterialConfig}.cs`
- `Assets/Scripts/Visual/{WeatherController,SceneDecor}.cs`
- `Assets/Materials/Skybox/skybox_*.mat` (10 files)
- `Assets/Textures/{Sky,Ground,Tiles}/*.png` (40+ tex)
- `Assets/Resources/Materials/{ground_*,path_*,Toon_*}.mat` (25 mats)
