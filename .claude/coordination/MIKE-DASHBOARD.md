# Dashboard Mike — Iso V4 Final

**Date** : 2026-05-12 | **Branch** : `main` (300 commits / 5h session)

## C'est fait (✅)

| Catégorie | Progress | Notes |
|---|---|---|
| Bug fixes critiques | 100% (46) | CS errors, null guards, asmdef, AssetRegistry, URP, GLTF, theme strip |
| Visual (shaders/VFX/weather) | 95% | Toon URP, weather/theme, VfxPool, popups 3D, fog, boss jelly/holo |
| Audio | 100% (29) | Procedural fallback, MusicManager adaptive, 3D spatial, mute split |
| Gameplay | 100% (140) | L3 4×2 DPS/Utility, boss phases 2-4, treasure, perks, 56 achievements |
| UI / HUD / menus | 100% (77) | Top-bar, panels (Settings/Help/Stats/Credits/SaveSlot), hotkeys, radial |
| i18n FR/EN/ES | 100% (5) | ~25 strings localisés + audit a90f58fa |
| Systèmes core | 100% (37) | Pathing Parallel.For, SaveSystem multi-slot A/B/C + v1→v2, EndlessMode |
| Persistance | 100% | Achievements + Bestiary + LifetimeStats + HighScores per level |
| Difficulté + Hero pick | 100% | Easy/Normal/Hard + perks pick screen + hero portraits |
| Mobile UX | 100% | Touch + virtual joystick + responsive 1920x1080 |

## En cours (🚧)

- **WebGL gh-pages deploy** : main est 654 commits AVANCE sur gh-pages → rebuild + push à relancer
- **Working tree non-clean** : 5 fichiers modifs (Hero.cs, HeroPortraitController.cs, 2 .mat, agent-watchdog.md) + 3 .meta untracked

## À faire (⚠️)

- **Boss flow end-to-end** : intro banner + 4 phases + enrage VFX + death cinematic (validation play mode)
- **Scenes load chain** : Loader → Menu → WorldMap → Main → end (test crash-free complet)
- **Performance baseline** : profiler 60 FPS desktop / 30 FPS mobile (VfxPool LOD + GPU instancing à mesurer)
- **5 tickets visuels Phase 3** (v4-fixes-queue) : T-VISUAL-01 Animated Terrain, 02 Weather, 03 Boss Shaders, 04 Damage Popups, 05 VFX Pool — ~20h Sonnet (statut Ready, jamais dispatch)

## Pour Mike manuellement (📌)

- **iOS build** : Xcode local + Apple Developer account + cert
- **Steam build** : Steamworks SDK plugin + appid setup
- **Redeploy gh-pages** : `BuildScript.BuildWebGL` + push worktree `gh-pages` (ou laisser Opus)
- **Cleanup working tree** : décider commit ou revert des 5 fichiers + 3 .meta avant /clear
