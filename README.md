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

## Build & Deploy

### Build WebGL local

Unity Editor : menu `CrowdDefense > Build WebGL` (ou CLI batch mode) :

```bash
/Applications/Unity/Hub/Editor/6000.0.74f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath /Users/mike/Work/crowd-defense \
  -executeMethod CrowdDefense.Build.BuildScript.BuildWebGL -quit \
  -logFile /tmp/unity-build.log
```

Output dans `Builds/WebGL/index.html`. Test local :

```bash
cd Builds/WebGL && python3 -m http.server 8000
# Browse http://localhost:8000
```

### Deploy /v6/ GitHub Pages

```bash
# 1. Build (cf ci-dessus).
unset GITHUB_TOKEN

# 2. Si branche gh-pages pas encore créée (première fois) :
git checkout --orphan gh-pages
git rm -rf .
git commit --allow-empty -m "init gh-pages branch"
git push origin gh-pages
git checkout main

# 3. Worktree + copy build
git worktree add /tmp/gh-pages-checkout gh-pages
rm -rf /tmp/gh-pages-checkout/v6
mkdir -p /tmp/gh-pages-checkout/v6
cp -r Builds/WebGL/* /tmp/gh-pages-checkout/v6/

# 4. Commit & push gh-pages
cd /tmp/gh-pages-checkout
git add v6
git commit -m "deploy v6 POC build $(date +%Y-%m-%d)"
unset GITHUB_TOKEN && git push origin gh-pages
cd /Users/mike/Work/crowd-defense
git worktree remove /tmp/gh-pages-checkout

# 5. URL accessible après ~30-60s propagation :
# https://michaelchevallier.github.io/crowd-defense/v6/
```

### Note GITHUB_TOKEN

Le shell parent a un `GITHUB_TOKEN` invalide (expiré). Toujours `unset GITHUB_TOKEN` avant `gh` ou `git push` (sinon HTTP 401).

### Note GitHub Pages (premier déploiement)

Si l'URL retourne 404 après le premier push : activer GitHub Pages dans repo Settings → Pages → Source = `gh-pages`, `/`.

## Repos liés

- **Archive Phaser** : https://github.com/michaelchevallier/lava_game (tag `v5.0-pre-pivot-unity` = snapshot pré-pivot)
- **Plan Opus principal historique** : `~/.claude/plans/rustling-nibbling-wirth.md` (réf historique)
