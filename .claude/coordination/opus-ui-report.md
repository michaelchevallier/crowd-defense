# Opus UI/UX — Synthese mission iso V4

**Date** : 2026-05-12
**Scope** : audit UI gap + dispatch 5-8 fixes parallèles vers iso https://michaelchevallier.github.io/lava_game/v4/

## Audit gap

Scene `Main.unity` n'instanciait que 21 controllers UI mais 26 sont câblés sur le HUD UIDocument. **6 controllers manquants** : `PauseMenuController`, `TowerTooltipController`, `RadialMenuController`, `SynergyHudController`, `FloatingPopupController`, `SettingsPanelController`. De plus, `HudController.ApplyDeviceClasses` était défini static mais jamais appelé → classes `.mobile`/`.portrait` jamais ajoutées au `hud-root`, donc tout le CSS responsive USS mort.

## Fix livré (parallèle Opus session — commit `04f3a1c`)

1. `Assets/UI/HUD.uxml` — inlining `popup-overlay` (FloatingPopup) + `pause-root` (PauseMenu) + chargement des USS `PauseMenu.uss` et `FloatingPopup.uss`. Tout sous un seul UIDocument évite scene yaml fragility.
2. `Assets/Scripts/UI/HudController.cs:Start()` — appelle `ApplyDeviceClasses(root)` après init, et `EnsureSibling<T>()` (helper idempotent ligne 442-445) auto-ajoute les 5 sibling controllers manquants (PauseMenu, TowerTooltip, Synergy, FloatingPopup, Radial). Approche self-healing : pas besoin de yaml-edit Main.unity, le HUD s'auto-câble runtime.

## Bugs initiaux (status)

- Wave button hidden : déjà résolu commit `2962ecd` (WaveManager late-init `Update()` safety net dans HudController).
- Toolbar invisible : code OK (`TowerToolbarController` wiré scène + `BuildCells()` construit 12 cells). Cause probable historique = USS responsive cassé sans `ApplyDeviceClasses` → maintenant fixe.
- Hero panel pas wired : résolu commit `04f3a1c` (`heroHpLabel`/`heroXpLabel` Q manquants ajoutés).
- Mobile USS pas activé : fix via `ApplyDeviceClasses(root)` dans `HudController.Start`.
- ESC pause non wiré : `LevelRunner.cs:182` appelle déjà `TogglePause()`, et `PauseMenuController` est maintenant auto-installé via `EnsureSibling`.
- Tooltips invisibles : `TowerTooltipController` maintenant auto-installé.

## Restant (hors scope cette mission)

- `SettingsPanelController` — pas auto-installable (UIDocument séparé `SettingsPanel.uxml`). À wire scène quand le panel Settings sera ouvert depuis le HUD.
- Live test Chrome MCP non lancé (le live URL `/v6/` doit être rebuild + redéployé pour valider visuellement). Cf `qa-build28-full.md` pour critères de validation.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
