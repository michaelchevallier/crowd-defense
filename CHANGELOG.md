# Changelog

All notable changes are documented here.
Format inspired by [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## v6.0 — 2026-05-12

### Added

- **L3 tower branching** — each of the 12 towers gains a DPS branch and a Utility branch at level 3, doubling late-game build diversity
- **Apocalypse boss phases 2-4** — multi-phase final boss with distinct AI, attack patterns, and visual states per phase
- **Hero skin variants** — hero character supports swappable skin assets (textures + tint); 3 unlockable variants at launch
- **Daily challenge** — seeded run generated server-side each day; score posted to leaderboard
- **Endless mode** — post-campaign infinite wave scaling with exponential difficulty curve
- **Achievements** — 40+ achievements tracked client-side with unlock toasts; persisted in save slot
- **Bestiary** — enemy lore panel populated on first encounter; 30 entries total
- **Lifetime stats** — session aggregates (towers built, enemies killed, gold earned, etc.) written to persistent storage
- **Animated water shader** — URP ShaderGraph procedural water (normal-map scroll + foam edge + depth tint)
- **Animated lava shader** — URP ShaderGraph procedural lava (voronoi distortion + emissive glow + heat shimmer)
- **Weather system** — per-theme dynamic weather layer: rain (forest/swamp), embers (volcano), snow (arctic), sand (desert), fog (ruins)
- **Multi-slot save** — 3 independent save slots; slot selector on title screen
- **Continue mid-level** — save state captured on pause/quit; resume restores wave, economy, and placed towers exactly
- **i18n FR / EN / ES** — all UI strings externalized; locale auto-detected from system, overridable in settings
- **Responsive UI** — UI Toolkit layouts adapt to portrait mobile, landscape mobile, and desktop aspect ratios
- **Outline system** — inverted-hull outline shader applied to towers and enemies on spawn (highlight on hover/select)
- **AssetRegistry** — centralized GLTF model registry for towers and enemies with editor batch-generation tool for AnimatorControllers

### Changed

- Render pipeline upgraded to URP with ShaderGraph replacing legacy surface shaders
- UI migrated from uGUI to UI Toolkit (runtime + Editor tooling)
- Enemy and tower spawn system refactored to use `AssetRegistry` (GLTF lookup + capsule fallback)
- Economy rebalanced per D1-01 spec: `_upgradeCost x5`, interest bank, magnet rework, treasure tiles, boss 0x gold

### Infrastructure

- Unity 6 LTS (`6000.0.74f1`) as locked engine version
- CI/CD via GitHub Actions: batch-mode build + WebGL deploy to GitHub Pages
- Dependabot enabled for GitHub Actions dependency updates
- `SECURITY.md` vulnerability disclosure policy added

---

## v5.0-pre-pivot — 2026-05-11

Last Phaser/Three.js prototype before Unity pivot. Tagged `v5.0-pre-pivot-unity` in legacy repo.
