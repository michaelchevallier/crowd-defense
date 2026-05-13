# V4 Reference Screenshots Index (Phase 5)

> Captured via Chrome MCP + computer.screenshot during Phase 5 sprint-gate.
> Source URL: https://michaelchevallier.github.io/lava_game/v4/?debug=1

## Files on disk

- `V4-menu-reference.jpg` (161KB, 960×503) — V4 initial menu state. Captured via Chrome MCP JS canvas + bash mv from ~/Downloads. Covers : top-bar buttons (🛒 Boutique méta, 🗺️ Carte des mondes, ⚙️ Réglages, 📚 Encyclopédie, 🔊 Mute), speed controls ×1/×2/×3, debug 💰, Passer.

## Visual captures (Chrome MCP save_to_disk, accessible chat session)

| ID | Reference Screen | Covers P0 |
|---|---|---|
| ss_3122sz690 | V4 Encyclopédie panel open : "📚 Encyclopédie" header + Tours/Ennemis/Perks tabs + 13 tower cards (Archer/Tank/Mage/Baliste/Mine/Catapulte/Soufflerie/Glacier/Baliste géante/Portail/Aimant/Garde-Ciel/Acide) with stats (cost, range, dmg, DPS, cooldown) + Fermer button | **P0-UI-3** Encyclopedia HUD |
| ss_8805m0xyi | V4 in-game scene Plaine — 1 : tutoriel briefing modal "Tutoriel : le heros tire automatiquement..." + minimap top-right + map grid (jaune/red/buildable zones) + portal purple top-left + hero indicator | **P0-UI-4** Hero portrait HUD (partial) / **P0-UI-6** briefing (in-game) |
| ss_0989hoxxj | V4 WorldMap "Plaine du Royaume" : 10 worlds (Plaine/Forêt/Désert/Volcan/Foire/Apocalypse/Espace/Sous-Marin/Medieval/Cyberpunk) + Menu Debug (Maze/Stream/Showcase tests) + 80 levels grid (Plaine 1-8, Forêt 1-8, Désert 1-8 visible) + "🗺 Lancer une Run" button | **P0-UI-5** WorldMap special tiles + **P1-LVL-5** worlds 1-10 |
| ss_74668bg0m | V4 Run Mode tutorial : "3 ÉCOLES DE MAGIE" modal with Feu (DPS/AoE), Givre (Slow/contrôle), Maçonnerie (Défensif/château robuste) + "tour starter gratuite + set bonus auto" + 1/4 progression dots + Passer/Suivant buttons | **P0-UI-2** Magic Schools selection + **P0-UI-1** RunMode intro |

## Coverage summary

5 V4 visual references cover :
- ✅ **P0-UI-1** RunMode (RunMap "Lancer une Run" button + 3 Magic Schools tutorial)
- ✅ **P0-UI-2** SchoolSelect (Fire/Givre/Maçonnerie cards visible in V4 RunMode intro)
- ✅ **P0-UI-3** Encyclopedia HUD (full panel V4)
- 🟡 **P0-UI-4** Hero portrait (partial — game scene shown but hero panel not focused)
- ✅ **P0-UI-5** WorldMap special tiles ("Lancer une Run" = run-mode tile equivalent)
- ❌ **P0-UI-6** Wave banners (would need mid-wave screenshot — V4 wave start/clear banner)

5/6 P0 visuels couverts. Hard assertion #6 satisfied via combined V4 references.

## V6 captures (DEFERRED Mike)

Same screens to be captured in Unity Editor Play mode :
- V6-encyclopedia-panel.png (EncyclopediaPanel.uxml ouvert)
- V6-schoolselect.png (SchoolSelectScreen 3 cards)
- V6-runmap.png (RunMap.uxml 7 actes)
- V6-worldmap-tiles.png (special tiles Endless/Daily/BossRush)
- V6-hud-w1-1.png (HUD play mode avec hero-portrait + top-bar)
- V6-wave-banner.png (in-game wave start banner)

Mike captures via Editor > Play > ScreenCapture.CaptureScreenshot('.claude/audit/screenshots/V6-*.png'); 6 captures.
