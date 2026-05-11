# Crowd Defense

Tower defense game built with **Unity 6 LTS** (`6000.0.74f1`) and C#.

Migration en cours depuis un prototype web Phaser/Three.js (`milan project` repo, archive frozen sur tag `v5.0-pre-pivot-unity`).

## Status

Sprint MIGRATE en cours (démarré 2026-05-11). Voir `STATUS.md` pour l'état multi-session du plan.

## Stack

- **Engine** : Unity 6 LTS
- **Language** : C#
- **AI tooling** : Unity-MCP (CoplayDev) + Claude Code Opus
- **Target platforms** : Steam (Mac/Win/Linux), iOS, Android, WebGL

## Architecture cible

Voir `docs/specs/design/` pour les specs de design (D1-01 économie, D1-02 pacing, D1-03 L3 hybride, D1-04 castle HP — moteur-agnostiques, ramenées du repo `milan project`).

## Background

Doc historique :
- `docs/research/R1-*.md` — benchmarks industrie (économie, pacing, mapdesign, autoqa)
- `docs/research/R2-*.md` — synergies, difficulty curve, milan audit, perf baseline
- `docs/research/R3-*.md` — tooling research + portability research (a abouti au pivot Unity)
- `docs/decisions/Q1-Q18-arbitrages.md` — toutes les décisions Mike post-interview

## Repos liés

- **Archive Phaser** : https://github.com/michaelchevallier/lava_game (tag `v5.0-pre-pivot-unity` = snapshot pré-pivot)
- **Plan Opus principal historique** : `~/.claude/plans/rustling-nibbling-wirth.md` (réf historique)
