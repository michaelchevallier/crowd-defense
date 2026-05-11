# Crowd Defense — Session Recap 2026-05-11

## Start state (this morning)
- /v6/ deploy = Phase 1 POC (W1-1 grid + basic HUD pills + castle, no Toolbar/Hero/Boss/Perks/etc)
- ~35% iso-V4 (V4 = https://michaelchevallier.github.io/lava_game/v4/?debug=1)

## End state
- /v6/ deploy = Build #20 with 25+ V5 systems merged + Main.unity wired with 16 UI controllers + 6 singletons
- Live URL: https://michaelchevallier.github.io/crowd-defense/v6/
- ~65% iso-V4 visible, ~95% iso-V4 backend (systems exist, some UI controllers not yet rendering)

## What got done

### 25+ V5 systems ported (each by parallel feature-dev agent)
1. Hero entity port (Hero.cs + HeroProjectile + HeroType SO + Knight asset)
2. PerkSystem (23 perks + 6 set bonuses + level-up overlay)
3. BossSystem (10 BossDef + BossUI overlay banner)
4. Shop + MetaUpgrades (10 MetaUpgradeDef + gems economy)
5. Doctrine (7 doctrines + UI panel)
6. WorldMap level select (node graph + 10 worlds × 9 levels)
7. Cutscenes (dialogue system + 10 world intros)
8. Skins / cosmetics (SkinDef + alternate GLTF/material swap)
9. AssetVariants (procedural palette swap per theme)
10. Random Events + Modifiers (6 events × 2 choices + 8 modifiers)
11. Tower Toolbar tower picker (13 hotkey buttons)
12. RadialMenu tower upgrade (L1→L2→L3-DPS/Utility + Sell)
13. Speed control ×1/×2/×3
14. Minimap + Device helper + DebugHud
15. Combo system (multi-kill streak + UI display)
16. Achievement toast (51 AchievementDef SO + toast UI)
17. Tutorial dialogues FSM (6 steps + i18n)
18. CoinPull animation (CoinToken pool + bezier flight to HUD)
19. Synergy HUD badges (Synergies.OnSynergyChanged + UI display)
20. Floating popups (damage/coin/gem text spawnpoints)
21. PathReveal animation (tile-by-tile BFS + ease-out-back)
22. PathfinderVisualization (dashed line preview on placement hover)
23. Wave preview UI (next wave roster between waves)
24. Save slots (3 slots with continue/new/delete)
25. Statistics screen (run + lifetime tabs)
26. Settings polish (UIVolume slider + Reset Camera button)
27. Leaderboard (top 10 endless mode)
28. Daily Challenge UI + BluePill mode UI
29. Toast generic (Show API + wire 4 event sites)
30. Toon shaders (Water/Lava/Snow/Lit cel-shading) + 51 achievements seed

### Infrastructure
- HUDPanelSettings theme assigned (unblocks UI Toolkit rendering)
- SetupMainScene Editor batch tool — 1-click wire 6 singletons + 16 controllers + 6 registries + populate TowerRegistry from existing TowerType SOs
- BatchRebuild tool — single-command rebuild flow (CLI batch mode)
- C# P1 cleanup (6 review items)
- 3 perf branches: MapRenderer GPU instancing, allocs/throttle, Minimap bake static
- 11 .meta file commits for new ScriptableObjects + UI controllers

### Scout backlog
21 tickets ready-to-dispatch in `.claude/coordination/parallel-scout-backlog.md`. ~16 dispatched, ~5 remain (P2 polish).

## Gameplay validation
Smoke test (Chrome MCP screenshots):
- ✅ Game boots, no runtime errors
- ✅ Tutorial overlay displays "Continue" + "Skip Tutorial" buttons
- ✅ Click on grid → tower placed, gold deducted (120 → 60 after 2 archers)
- ✅ Hotkey "1" + click → archer selected + placed
- ✅ Castle, path, Toon water/lava shaders, CoinPull token all visible

## Known gaps (next session)

### UI controllers not rendering on live
Even though attached to HUD GameObject + UXML elements exist, these controllers fail silently in Start():
- TowerToolbarController (13 buttons should show at bottom)
- HudPerkBadges (perk icons should show in hero panel)
- SpeedControlController (×1/×2/×3 should show in top bar)
- MinimapController (minimap should show top-right)

Suspected root cause: GetComponent<UIDocument>() conflict — multiple MonoBehaviour on same HUD GO share 1 UIDocument. Or Q<> name mismatch. Dispatched bug-fixer agent for investigation.

### Scene wiring loose ends
- LevelRunner.heroPrefab not assigned (no Hero.prefab exists, Hero never spawns at runtime)
- BossSystem.registry list populated but 10 BossDef SOs may need data fields
- PerkRegistry created but not populated with 23 PerkDef SOs (BuildPerkAssets MenuItem exists, not yet run)

### Asset gen ongoing
- Texture gen ComfyUI Flux: 7/20 done (Weather pack complete, UI Achievement icons partial). ETA ~1.5h.
- Mixamo: blocked on Adobe auth (Playwright anti-bot). Mike needs manual token extract per `tools/mixamo/README.md`.

## Effort stats
- ~150 commits to main today
- ~30 parallel agents launched (feature-dev / bug-fixer / quality-maintainer / perf-auditor / spec-writer / general-purpose)
- ~20 Unity batch builds (CLI batch mode)
- 5 successful deploys to gh-pages /v6/
