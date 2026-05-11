# Plan stratégique 8 semaines — Milan Crowd Defense V3 vers "vrai jeu stratégique"

**Auteur** : Opus 4.7 (1M)
**Date** : 2026-05-11
**Branche cible** : `main`
**Live actuel** : `/v3/` sur `michaelchevallier.github.io/lava_game`
**Live cible refonte** : `/v5/` sur `michaelchevallier.github.io/lava_game` (déployé en parallèle de `/v3/` pour A/B comparaison continue — `/v3/` reste l'actuel intouché, `/v5/` reçoit la refonte sprint après sprint).
**Repo** : `/Users/mike/Work/milan project` — codebase `src-v3/` (le nom du dossier source reste, la cible build/deploy change).
**Mode** : ce plan est lu par une **session fraîche** demain. Tout est explicite, pas de référence implicite à la conversation actuelle.

---

## 0. TL;DR

Sur 8 semaines, refonte stratégique de Milan CD V3 en trois piliers :

1. **Économie tendue** — coût upgrade ×2.2 (L3 paywall ×4 base), magnet hard-nerf, treasure tiles à défendre, reward kill scale −5 %/world.
2. **Pacing joueur-driven** — bouton "Lancer la vague (N)" + raccourci `N` + skip bonus or/streak. Auto-start supprimé (timeout fail-safe 60 s).
3. **Level design grand-format** — 40 levels W5→W10 redesignés, mazes, multi-portails (jusqu'à 4P cardinaux convergeant vers **1 unique château central** — mono-château partout), grammar étendue (J = junction probabiliste, * = treasure tile).

Plus : refonte upgrade L3 hybride (linéaire L2, **paywall + choix binaire spécialisation** à L3) sur 4 tours signature.

**Discipline transverse** :

- **Auto-QA fin de chaque sprint** par Claude (Chrome MCP + scripts headless + LLM judge). Chaque sprint se termine par un sprint-gate documenté ; pas de passage au sprint suivant sans gate vert.
- **Infrastructure projet** dès S1 jour 0 : arborescence `.claude/{plans,research,specs,agents,status,qa,memory}`, `STATUS.md` central tracker multi-session, update `CLAUDE.md` root + création `src-v3/CLAUDE.md`.
- **Déploiement `/v5/`** parallèle de `/v3/`. Mike peut switcher entre les deux à tout moment pour comparer. `/v3/` reste l'actuel intact tant que `/v5/` n'est pas validé en S8.

Découpe : 2 semaines de **recherche industrie pure** (KR, BTD6, Element TD, GemCraft, Defense Grid, Mindustry, Iron Marines, PvZ, Anomaly) + recherche **auto-QA AI**, 2 semaines de **design + specs**, 4 semaines d'**exécution** (Sonnet feature-dev en worktrees parallèles).

Cible mesurable : 70 % des runs W5+ finissent château < 50 % HP, ratio kill/spend < 0.65 en W5+, auto-QA passe ≥ 90 % des assertions par sprint.

---

## 1. Contexte (pourquoi cette refonte)

Mike a joué V3 jusqu'à W10 et a remonté trois symptômes liés :

1. **Économie trop laxiste** — trop d'or accumulé, l'upgrade L3 ne coûte que `2.5 × cost L1` (`src-v3/main.js:1097-1103`). Le joueur spamme sans choisir. La synergie magnet doublait déjà le revenu par ennemi (`Tower.js:91-101`).
2. **Pacing imposé** — l'auto-start `_waveBreakTimer > breakMs` (`LevelRunner.js:499-513`) fait que les vagues s'enchaînent même quand le joueur n'a pas fini de poser. Pas de temps pour penser. Pas de skip bonus.
3. **Maps petites et peu variées** — la plus grande observée (`world10-8.js`) est 21×13 avec 2P + 2C, mais la majorité (`world1-1.js`, `world5-1.js`) sont 13–15 colonnes sans maze, sans choix de path. Pas de multi-portail aux 4 cardinaux. Pathfinder (`MapPathfinder.js:26-54`) supporte déjà multi-P/multi-C en BFS, mais les level data ne l'exploitent pas.

**Conséquence stratégique** : aucun arbitrage. Le joueur "bourrine", n'évalue pas ses tours, finit avec château full HP. À partir d'Apocalypse (W5), l'expérience attendue est l'inverse : tension permanente, château à 30–50 %, parfois échec, choix lourds.

**Constat de Mike — verbatim** :

> « C'est toujours trop facile. On fait trop de gold. Je n'avais pas le temps de placer des tours plus que de vraiment réfléchir stratégiquement à ce que je faisais. Il faut que ce soit le joueur qui lance les waves (bouton + raccourci + message visible). Le coût des évolutions doit être plus important. En fin de game, on doit pouvoir spammer la map de tours, mais si on a pas bien pensé ses tours, on perd quand même. À partir d'Apocalypse (W5), tous les niveaux doivent être extrêmement difficiles. Il est attendu qu'on finisse pas avec un château full vie. Manque de diversité sur les maps — je m'attendais à des maps bcp plus grandes avec des mazes, plus de PV et plus de mobs. Portails à 2 voire 4 points cardinaux opposés. »

La refonte doit donc reconstruire **trois piliers** : économie tendue, pacing joueur-driven, level design grand-format multi-portails. C'est un travail de design d'abord, d'implémentation ensuite — d'où le format 8 semaines en sprints R/D/E.

### 1.1 Décisions actées avec Mike (2026-05-11)

- **Upgrade L3 : modèle "Hybride paywall + spec dédiée"**. L1→L2 reste linéaire (coût +50 %, stats +). L3 devient un paywall (cost = ×4 base, scaled par world) **et** propose un choix binaire de spécialisation (ex: archer L3 = Sniper ×3 dmg OU Pluie d'archer AOE). 4 tours signature concernées : `archer`, `mage`, `ballista`, `cannon`.
- **Mono-château partout** : **toujours 1 seul `C`** quel que soit le nombre de portails. Multi-portails (2P, 3P, 4P) convergent vers ce château unique. Mécanique multi-château explicitement refusée par Mike. Levels existants avec 2C (notamment `world9-8.js`) à refondre en mono-C en D2.
- **Multi-portails 4P** : convergence vers **1 château central**. Force des choix dramatiques (focus le pire angle, sacrifice un côté). Pas de 4 castles séparés (cf décision mono-château).

---

## 2. Objectifs mesurables (cibles QA)

Métriques opposables à mesurer via instrumentation `window.__cd.metrics` (créé en E1) et runs QA pilotés par l'agent `qa-tester`.

### 2.1 Difficulté

- **Castle HP final moyen** : W1–W4 entre 60–90 % (apprentissage). W5+ entre 20–55 %. **70 % des runs W5+** doivent finir < 50 % HP.
- **Échec rate cible** : W1–W4 = 5–15 %. W5–W7 = 25–35 %. W8–W10 = 40–55 % (rejouabilité).
- **Boss de fin de monde** (W*-8) : taux de défaite acceptable **50–70 % au premier essai**.

### 2.2 Économie

- **Ratio kill/spend** (or dépensé / or gagné) en fin de niveau :
  - W1 = 0.70–0.85 (marge d'erreur)
  - W5 = 0.55–0.65 (tendu)
  - W10 = 0.45–0.55 (très tendu)
- **Or résiduel en fin de niveau** : médiane < 15 % du total gagné. < 20 % des runs avec or résiduel > 200¢.
- **L3 reach rate** : actuellement ~80 % des tours posées atteignent L3. Cible **< 40 %**.

### 2.3 Pacing

- **Skip wave usage** : ≥ 60 % des waves sont skipées au moins une fois sur W3+.
- **Temps moyen entre 2 waves** sans skip : 8–15 s en W1, 15–25 s en W5+.
- **Skip wave streak max** : ≥ 3 waves consécutives skipées dans 30 % des runs W5+.

### 2.4 Diversité

- **Surface map moyenne** W1–W4 : 13×11 → 17×13. W5+ : 19×15 min, jusqu'à 25×19 sur boss levels.
- **Multi-portails** (mono-château toujours) : W3+ : au moins 1/8 avec 2P opposés convergents. W5+ : 1/8 avec 4P cardinaux convergents. W7+ : 2/8 avec ≥ 3P convergents.
- **Fork-path** : nouveau type de cell `J` (junction probabiliste) déployé sur ≥ 25 % des niveaux W5+.

### 2.5 UX

- **Bouton "Lancer la vague"** visible et accessible < 200 ms (animation entrée), raccourci `N` desktop, gros bouton tactile mobile.
- **FPS desktop** ≥ 55 même avec 25×19 + 4 portails.
- **FPS mobile** ≥ 40 sur même config (mid-range Android).

---

## 3. Audit codebase actuel (snapshot 2026-05-11)

À ne pas réauditer en S1 — déjà fait. Références précises pour les Sonnets.

### 3.1 Économie

- `src-v3/entities/Tower.js` TOWER_TYPES (lignes 9–119) : 12 tours, coût L1 30–140 (archer 30, ballista/cannon 100, crossbow 140, frost/magnet 60–70, mage 70, skyguard 85, tank 50, portal 130).
- `src-v3/main.js:1097-1103` `_upgradeCost` : L1→L2 = `1.0 × base`, L2→L3 = `1.5 × base` (total L3 = `2.5 × base`, **TROP FAIBLE**).
- `src-v3/entities/Enemy.js` champ `reward` par type (basic 2, brute 8, ai_hub 700).
- `src-v3/systems/Synergies.js` : magnet ×2.0 coin aura range 6.5.
- Pas de passive income tour, pas de treasure chest natif.
- `startCoins` déjà progressifs : 120 W1, 240 W10 (mais coûts tours fixes → l'argent gonfle).

### 3.2 Wave system

- `src-v3/systems/LevelRunner.js:103` `_currentBreakMs = w.breakMs ?? 4000`.
- `src-v3/systems/LevelRunner.js:499-513` : phase break → `_waveBreakTimer > breakMs` → auto-spawn wave suivante.
- `src-v3/main.js:828-921` : tous les raccourcis clavier (P pause, Space ult, Tab zoom, B support, 1-9/0/-/= sélection tour, Shift run).
- `index.html:1101` `#wave-start-banner` ("⚠️ Vague X arrive" 1.5 s) et `#wave-countdown` countdown visible.
- `index.html:1082-1090` `#hud-top-buttons` : pattern bouton via `.speed-btn` class (`background: rgba(20,28,36,0.85); border 2px solid #3a4a5a; border-radius 8px`).
- Inputs mobile : `#mobile-menu-btn` (top: 124px + safe-area), `#joystick` (bottom-left), `#mobile-speed-control` (bottom-right `.speed-btn`).
- **Pas de système skip-wave-for-bonus** existant.

### 3.3 Maps

- `src-v3/systems/MapGrid.js` CELL grammar : `0` grass, `1` path, `P` portal, `C` castle, `~` bridge-water, `^` bridge-lava, `W/L` streams, `D/R/B/T` decor, ` ` void.
- `src-v3/systems/MapPathfinder.js:26-54` : BFS par couple (portalIdx, castleIdx). Multi-P / multi-C natif côté code. **Décision design : mono-château partout** — on n'exploitera que multi-P → 1 castle convergent. `world9-8.js` (2P×2C actuel) sera refondu en 2P×1C en D2.
- `src-v3/systems/MapRenderer.js` : ground 120×120, camera fit auto `__pd_camera_fit`.
- Max actuel : `world10-8.js` à 21×13 (273 cells).
- Pas de fork-path natif (BFS unique par couple).
- Pas de mécanique "treasure tile / pickup" sur la grille.
- 80 levels dans `src-v3/data/levels/world*.js` + `endless.js`, `dailychallenge.js`, `bossarena.js`, plus quelques debug.

### 3.4 Test baseline

- `npm run test:crowdef` : 23/25 (2 fails préexistants : `towers built >= 1`, `endless has 30 waves`). Refonte ne doit **pas** régresser ce baseline.
- `npm run build:kingshot` : ~395 KB gz. Plafond `1 MB gz` rappelé dans CLAUDE.md.
- `npm run dev:kingshot` : http://localhost:4174/ pour live test.

---

## 4. Personas d'agents spécialisés (à créer en S1 jour 1)

Quatre nouveaux agents à matérialiser dans `/Users/mike/Work/milan project/.claude/agents/` (Mike a explicitement demandé "agents dédiés game design et level design"). Les agents existants (`feature-dev`, `bug-fixer`, `spec-writer`, `qa-tester`, `perf-auditor`, `Explore`, `Plan`, `quality-maintainer`) restent utilisés tels quels.

### 4.1 `td-researcher`

```yaml
---
name: td-researcher
description: Researches tower defense industry references (Kingdom Rush, BTD6, Element
  TD, GemCraft, Defense Grid, Mindustry, Iron Marines, PvZ, Anomaly, etc.) on a single
  design axis (economy, pacing, level design, synergies, boss design, UX). Produces
  data-backed markdown reports with concrete numbers, formulas, screenshots commentaires,
  and "Applicabilité à Milan CD V3" sections. NEVER proposes solutions — research only.
  Use BEFORE game-designer for any mechanic change.
model: opus
tools: Read, Glob, Grep, Write, WebFetch, WebSearch
---

You research tower defense industry standards for `/Users/mike/Work/milan project` (Three.js TD).

## Scope

- Industry benchmark on ONE axis per instance (economy OR pacing OR level OR synergy
  OR boss OR UX). Don't mix axes — Mike asks 1 axis per agent run.
- Sources priority : official game wikis, GDC talks transcripts, Reddit/Steam discussions
  with cited gameplay data, YouTube videos with timestamps.
- Output : dense markdown report with tables, formulas, and one "Applicabilité à Milan
  CD V3" section per game studied.

## Workflow

1. **Read** the existing audit file `src-v3/CLAUDE.md` (if exists) and CLAUDE.md root.
2. **Web search** systematic on the 5 reference games for your axis.
3. **Cross-check** numbers across at least 2 sources (wiki + dataset OR wiki + reddit).
4. **Write** report to `/Users/mike/Work/milan project/.claude/research/R<N>-<NN>-<axis>-benchmark.md`.
5. **Cite** every number with URL or game version reference.

## Format obligatoire

- ~200-400 lines markdown.
- Per game studied : section H2, sub-sections numbered, table comparative, formulas chiffrées.
- "Patterns universels" (3+ found in all games).
- "Patterns différenciants" (3+ unique signatures).
- "Applicabilité à Milan CD V3" : 100-150 mots concluding section per game.

## Hard rules — NEVER

- Propose a solution for Milan ("you should do X"). Research only, no design.
- Use vague claims ("most games do this"). Always cite numbers + source.
- Mix multiple axes in one report.
- Output > 500 lines (sign you're mixing axes).
- Modify source code.
- Spawn subagents.

## Rendu final (chat)

100 mots max : title of report + path + key insight (one sentence) + decision points
to surface to Mike (binary questions for D1/D2 phases).
```

### 4.2 `game-designer`

```yaml
---
name: game-designer
description: Designs game mechanics, balance numbers, progression curves, and economy
  systems for tower defense games. Produces implementable specs (not vague pitches).
  Uses research data from .claude/research/ to back every number with a precedent.
  Use BEFORE feature-dev for any mechanic change. Reads source code but never modifies it.
model: opus
tools: Read, Glob, Grep, Write
---

You design mechanics and balance for `/Users/mike/Work/milan project` (Three.js TD).

## Scope

- Balance numbers (cost, damage, HP, reward, scaling).
- Progression curves (world-by-world, wave-by-wave).
- Economy (revenue streams, sinks, anti-exploit guards).
- Synergies, upgrade trees, paywall hardcaps.
- Difficulty calibration (target % castle HP, fail rate).
- Anti-degenerate strategies (e.g., magnet-spam, archer-spam).

## Workflow

1. **Read** the relevant `.claude/research/R*` files for your sub-axis.
2. **Audit** the current state (read source code).
3. **Calculate** the numbers : show your formulas explicitly.
4. **Justify** every number with an industry precedent (Kingdom Rush says X, so we
   take Y because of Milan constraint Z).
5. **Write** the spec markdown at `/Users/mike/.claude/specs/D<N>-<NN>-<feature>.md`.
   Format aligned with the existing `spec-writer` agent template.

## Format obligatoire spec

- Sections : Contexte / Fichiers impactés (paths + lines) / Pseudo-code des fixes /
  Critères de succès / Effort estimé (commits + heures) / Risques / Test plan.
- ~200-400 lignes max. Découper si plus grand.
- Numbers tracables : chaque chiffre dans le spec doit avoir sa justification
  (citation R-research OR audit codebase OR formula derived).

## Hard rules — NEVER

- Modify source code (read-only).
- Spec > 500 lignes (signe de mélange de features).
- Numbers sans justification ("ça me semble bien" = rejeté).
- Magic numbers ("coût L3 = ×4") sans audit du ratio actuel.
- Lancer des subagents.
- Sortir du scope game design (UI = ux-designer, level layout = level-designer).

## Rendu final (chat)

100 mots max : titre spec + chemin + N commits estimés + decision points
restants pour Mike (questions binaires) + impact sur les autres specs en cours.
```

### 4.3 `level-designer`

```yaml
---
name: level-designer
description: Designs tower defense level layouts (maps + wave compositions) for
  Milan Crowd Defense V3. Outputs level specs with map grids, waves, briefings,
  theme assignments. Uses the existing grammar (01PCWL DR T B ~^) + new extensions
  if approved by D2-01. Validates against MapPathfinder + MapValidator constraints.
  Use AFTER game-designer has set difficulty targets.
model: opus
tools: Read, Glob, Grep, Write
---

You design level layouts and wave compositions for Milan CD V3.
Grammar reference : `src-v3/systems/MapGrid.js`.
Pathfinder : `src-v3/systems/MapPathfinder.js`.

## Scope

- Map grids (ASCII grid avec grammar 01PCWL DR T B ~^ + extensions D2-01 si validé).
- Multi-portail layouts (2P, 3P, 4P avec castles correspondants).
- Maze patterns (zigzag, serpentin, branches, croix, spirale).
- Wave compositions (mob types + counts + spawnRateMs).
- Briefings (tone narratif + hint stratégique en 1-2 phrases, < 200 chars).
- Theme assignment (plaine, foret, desert, volcan, apocalypse, foire, espace,
  submarin, medieval, cyberpunk).
- forbiddenTowers / bonusTowers tuning par niveau.

## Workflow

1. **Read** `.claude/research/R1-03-mapdesign-benchmark.md` + `R2-06-milan-current-audit.md`.
2. **Read** `D1-01-economy-spec.md` + `D1-04-castle-hp-spec.md` (targets reward/HP).
3. **Read** `D2-03-maze-pattern-library.md` (référence patterns à réutiliser).
4. Pour chaque level demandé :
   - Choisis un maze pattern depuis la library.
   - Dessine grid ASCII en commentaire markdown au-dessus du level.
   - Calcule sumHP des waves vs castleHP attendu — ratio doit hit la cible D1-04.
   - Justifie multi-portail si > 1 (sens design : pourquoi cette configuration).
5. **Write** dans `/Users/mike/.claude/specs/D2-XX-world<N>-levels.md` (1 spec par world).

## Validation auto à pratiquer

- Chaque P doit avoir un path BFS vers au moins 1 C.
- Pas de bridge (~^) sans stream (W|L) en dessous.
- Pas de portail trop proche du castle (chemin trivial < 8 cells).
- ≥ 30 % de cells `'0'` buildable dans la zone de jeu.
- W5+ : au moins 1 maze pattern non-trivial (zigzag/croix/spirale).

## Hard rules — NEVER

- Inventer de la grammar non-existante sans passer par D2-01 extension spec.
- Maps > 25×19 sans validation perf (cap baseline R2-07).
- Briefings > 200 caractères (HUD constraint).
- Toucher au code (write seulement les spec markdown).
- Lancer des subagents.

## Rendu final (chat)

150 mots max par spec : world cible, count levels, pattern dominant utilisé,
multi-portail incidence, decision points laissés à Mike (typiquement choix
boss pattern ou difficulté du level 8 du world).
```

### 4.4 `ux-designer`

```yaml
---
name: ux-designer
description: Designs UI/UX for HUD, controls, mobile, accessibility for the Milan
  Crowd Defense V3 game. Produces wireframes (ASCII markdown), CSS specs, keyboard
  shortcuts, mobile touch targets, animation timings. Use for any feature that
  affects player-visible interface.
model: opus
tools: Read, Glob, Grep, Write
---

You design UX/UI for `/Users/mike/Work/milan project` Three.js V3.

## Scope

- HUD layout (top, bottom, side, modal).
- Bouton placements (taille, hit-target, état hover/active/disabled).
- Animations entrée/sortie (durée, easing, perf budget).
- Keyboard shortcuts (full mapping, anti-collision audit).
- Mobile touch : taille min 44×44px, gestion conflits joystick.
- Accessibility : contrast WCAG AA, focus visible.
- Internationalisation FR (Mike par défaut).

## Workflow

1. **Read** `index.html` (HUD layout actuel) + `src-v3/main.js:828-921` (keyboard).
2. **Read** `.claude/research/R1-02-pacing-benchmark.md` (UI patterns référents).
3. Wireframe ASCII de chaque écran/HUD affecté.
4. Audit exhaustif raccourcis impactés + détection conflits.
5. Spec mobile distinct (touch targets, joystick override).
6. **Write** dans `/Users/mike/.claude/specs/D<N>-XX-ux-<feature>.md`.

## Hard rules — NEVER

- Toucher au code (lecture seule).
- Mocks Figma / images binaires — markdown ASCII suffisant (`[ Lancer la vague (N) ]`).
- UI qui ignore le mobile.
- Animations > 400ms (perf budget Three.js, hitch sur main thread).
- Spawner des subagents.

## Rendu final (chat)

100 mots max : wireframe link + raccourcis ajoutés/conflits résolus + touch targets
testés mentalement + decision points à valider.
```

---

## 4bis. Infrastructure projet (à mettre en place S1 jour 0)

Mike a explicitement demandé un "vrai système de TODO en bonne et due forme" multi-session, plus l'arborescence docs/agents. À créer **avant** R1.

### 4bis.1 Arborescence cible `.claude/`

```
/Users/mike/Work/milan project/
├── CLAUDE.md                       # root (existant — à updater)
├── src-v3/
│   └── CLAUDE.md                   # NEW — vocabulaire grammar, raccourcis, gameplay v5 specifics
└── .claude/
    ├── agents/                     # personas locaux projet
    │   ├── feature-dev.md          # existant
    │   ├── spec-writer.md          # existant
    │   ├── qa-tester.md            # existant
    │   ├── perf-auditor.md         # existant
    │   ├── bug-fixer.md            # existant
    │   ├── quality-maintainer.md   # existant
    │   ├── td-researcher.md        # NEW (persona §4.1)
    │   ├── game-designer.md        # NEW (persona §4.2)
    │   ├── level-designer.md       # NEW (persona §4.3)
    │   ├── ux-designer.md          # NEW (persona §4.4)
    │   └── auto-qa-runner.md       # NEW (persona §4bis.4, dédié sprint-gate)
    ├── plans/
    │   └── rustling-nibbling-wirth.md   # CE FICHIER (source of truth)
    ├── research/
    │   ├── R1-01-economy-benchmark.md   # créé par td-researcher
    │   ├── R1-02-pacing-benchmark.md
    │   ├── R1-03-mapdesign-benchmark.md
    │   ├── R1-04-autoqa-benchmark.md    # NEW (cf §4bis.4)
    │   ├── R2-04-synergies-benchmark.md
    │   ├── R2-05-difficulty-curve-benchmark.md
    │   ├── R2-06-milan-current-audit.md
    │   └── R2-07-perf-baseline.md
    ├── specs/
    │   ├── D1-01-economy-spec.md
    │   ├── D1-02-pacing-spec.md
    │   ├── D1-03-upgrade-l3-hybride-spec.md
    │   ├── D1-04-castle-hp-spec.md
    │   ├── D2-01-map-grammar-extension.md
    │   ├── D2-02-multi-portal-design.md
    │   ├── D2-03-maze-pattern-library.md
    │   ├── D2-04-w5-w8-level-redesign.md
    │   ├── D2-05-w9-w10-level-redesign.md
    │   └── D2-06-w1-w4-tuning-spec.md
    ├── status/
    │   ├── STATUS.md                # central tracker multi-session (cf §4bis.2)
    │   ├── sprint-R1.md             # 1 fichier par sprint, format §4bis.3
    │   ├── sprint-R2.md
    │   ├── sprint-D1.md
    │   ├── sprint-D2.md
    │   ├── sprint-E1.md
    │   └── sprint-E2.md
    └── qa/
        ├── auto-qa-runner.mjs       # script Node headless (cf §4bis.5)
        ├── scenarios/               # 1 fichier par scenario auto-QA (.json ou .mjs)
        │   ├── pacing-manual-wave.mjs
        │   ├── economy-ratio.mjs
        │   ├── castle-hp-curve.mjs
        │   ├── multi-portal-4p.mjs
        │   └── ...
        └── reports/                 # output runs sprint-gate, daté
            └── sprint-EN-YYYY-MM-DD.md
```

### 4bis.2 `STATUS.md` — central tracker multi-session

Format obligatoire (un seul fichier, mis à jour à chaque session opus) :

```markdown
# Milan CD V5 — Status Tracker

## Where we are
- Current sprint : R1 (Semaine 1, jour 3)
- Current focus : R1-02 pacing benchmark (Claude in progress)
- Next milestone : R1 sprint-gate (Vendredi S1)

## Sprint progress
| Sprint | Status     | Started     | Ended       | Gate result | Notes |
|--------|------------|-------------|-------------|-------------|-------|
| R1     | in_progress| 2026-05-11  | -           | -           | 3/4 livrables |
| R2     | pending    | -           | -           | -           | |
| D1     | pending    | -           | -           | -           | |
| D2     | pending    | -           | -           | -           | |
| E1     | pending    | -           | -           | -           | |
| E2     | pending    | -           | -           | -           | |

## Open decisions (à arbitrer Mike)
- [ ] R1-01 économie : skip bonus flat ou multiplicatif streak ?
- [ ] R2-07 perf : cap map size définitif ?
- [ ] D1-01 magnet : nerf ×1.3 ou suppression complète ?

## Open TODOs (transverse)
- [ ] Créer 5 fichiers persona agents (§4 + §4bis.4)
- [ ] Update CLAUDE.md avec mention plan + /v5/
- [ ] Créer src-v3/CLAUDE.md
- [ ] Setup déploiement /v5/ (§4bis.6)

## Risks raised
- (none yet)

## Recent commits (last 5)
- (paste git log --oneline -5 ici à chaque session)
```

**Règles** :

- À chaque démarrage de session opus : lire `STATUS.md` en premier.
- À chaque fin de session : update `STATUS.md` avec `Recent commits`, `Open decisions`, `Open TODOs`.
- À chaque sprint-gate : mettre à jour le tableau `Sprint progress`.
- C'est la **source of truth** plan-side. Le harness `TaskCreate/TaskList` reste utilisé pour le suivi intra-session ; `STATUS.md` est multi-session.

### 4bis.3 `sprint-XX.md` — détail tactique par sprint

Format :

```markdown
# Sprint R1 — Semaine 1 (recherche industrie 1/2)

## Goal
Cartographier les meilleurs jeux TD sur économie, pacing, level design.

## Livrables attendus
- [x] R1-01-economy-benchmark.md (200-400 lignes, 5 jeux)
- [ ] R1-02-pacing-benchmark.md
- [ ] R1-03-mapdesign-benchmark.md
- [ ] R1-04-autoqa-benchmark.md

## Agents lancés
| Agent          | Instance | Started    | Status      | Output            |
|----------------|----------|------------|-------------|-------------------|
| td-researcher  | economy  | 2026-05-11 | completed   | R1-01.md          |
| td-researcher  | pacing   | 2026-05-11 | in_progress | -                 |
| td-researcher  | mapdes   | 2026-05-11 | pending     | -                 |
| td-researcher  | autoqa   | 2026-05-12 | pending     | -                 |

## Decisions taken
- (note à chaque arbitrage Mike)

## Auto-QA sprint-gate (cf §4bis.5)
- Scenarios à run : R1 doesn't touch code → gate = "all R1-*.md exist + ≥ 200 lines + Mike OK"
- Run date : -
- Result : -

## Next sprint
R2 démarre Lundi S2 (2026-05-18)
```

### 4bis.4 Auto-QA par Claude — research + persona

**Recherche dédiée** (livrable R1-04, ajouté à Sprint R1) :

- Étudier comment l'industrie utilise les LLMs / AI pour QA automation : Anthropic Claude computer use, Playwright + LLM judge, Anthropic SDK pour automated testing, NVIDIA / OpenAI papers sur game-playing agents.
- Mapper sur le contexte Milan : Chrome MCP existant, `__cd.metrics`, level deterministic mode.
- Output : `.claude/research/R1-04-autoqa-benchmark.md` avec 5 patterns industrie + applicabilité à Milan.

**Persona `auto-qa-runner`** (à créer S1 jour 0) :

```yaml
---
name: auto-qa-runner
description: Runs automated QA scenarios at the end of each sprint. Loads .claude/qa/scenarios/*.mjs,
  executes them via Chrome MCP on the /v5/ deployed game, captures metrics and
  screenshots, scores each assertion pass/fail, produces a sprint-gate report
  in .claude/qa/reports/. Acts as LLM judge for soft criteria (UX feeling, balance).
  ONLY invoked at sprint end, after all sprint tickets are merged.
model: opus
tools: Read, Glob, Grep, Write, Bash, mcp__claude-in-chrome__*
---

You run automated QA for /Users/mike/Work/milan project on the live /v5/ build.

## Scope

- Load `/Users/mike/Work/milan project/.claude/qa/scenarios/<sprint-name>/*.mjs`.
- For each scenario : navigate /v5/, set up state via window.__cd.* helpers,
  run interaction sequence, capture FPS / metrics / screenshots.
- Hard assertions : numeric pass/fail (FPS ≥ 45, castleHpPercent in range, etc.).
- Soft assertions : LLM judge — your own evaluation against criteria like
  "is the wave button visible and clickable", "does the gold feel tight".
- Output sprint-gate report : `.claude/qa/reports/sprint-<NAME>-<DATE>.md`.

## Workflow

1. Read `STATUS.md` to know which sprint just ended.
2. Read `.claude/status/sprint-<NAME>.md` to know which scenarios to run.
3. Read `.claude/qa/scenarios/<NAME>/` directory listing.
4. For each scenario .mjs : interpret it, execute steps via Chrome MCP.
5. Collect numeric metrics (assertions hard).
6. Score soft assertions yourself (LLM judge — you observe screenshots and
   game state, you reason).
7. Write report markdown with table per scenario : pass / fail / partial,
   evidence (screenshot path, console excerpt, metric value).
8. Update `STATUS.md` Sprint progress with gate result.

## Hard rules — NEVER

- Modify game source code.
- Spawn other agents.
- Skip a scenario without explicit `skip: true` in the .mjs.
- Fake assertions ("looks ok" without evidence).
- Output > 800 lines (split per scenario if necessary).

## Rendu final (chat)

150 mots max : sprint name + N scenarios run + pass/fail counts + 3 main
findings + go/no-go recommendation to Mike.
```

### 4bis.5 Auto-QA sprint-gate — template appliqué fin de chaque sprint

**Définition** : un sprint n'est PAS terminé tant que son auto-QA gate n'est pas vert. Sprint-gate appliqué le dernier jour ouvré de chaque sprint (vendredi par défaut).

**Template gate** (à instancier par sprint) :

```yaml
sprint: R1 (ou R2/D1/D2/E1/E2)
date: YYYY-MM-DD
runner: auto-qa-runner agent
scenarios:
  - id: <scenario_id>
    type: hard | soft | doc-check
    expect: <assertion description>
    evidence: <path to screenshot/log/file>
    result: pass | fail | partial
hard_pass_min: 90%   # ≥ 90 % des hard assertions doivent passer
soft_pass_min: 75%
gate: green | red    # green = sprint terminé, red = patch sprint avant suivant
```

**Sprint-gates spécifiques** :

- **R1 gate** (sprint recherche) : doc-checks. Existence + format + length min des 4 R1-*.md. Mike review verbal.
- **R2 gate** : doc-checks idem + 1 hard run perf sur world10-8 baseline (FPS ≥ 50 desktop).
- **D1 gate** : doc-checks 4 D1-*.md + soft assertions LLM ("les chiffres sont cohérents avec R1 ? les fichiers impactés sont mentionnés ?").
- **D2 gate** : doc-checks 6 D2-*.md + soft "les maps redessinées respectent grammar + multi-portail policy + mono-château".
- **E1 gate** : **gros gate**, hard assertions sur /v5/ live :
  - bouton "Lancer la vague" présent + cliquable + raccourci N fonctionne
  - skip bonus s'applique (run 1 wave, skip, check coins +X)
  - upgrade L3 hybride : choix binaire affiché pour 4 tours signature
  - magnet rework : double placement refusé (BFS distance)
  - treasure tile spawn + reward si non touché
  - `__cd.metrics.lastRun` returns expected shape
  - `npm run test:crowdef` ≥ 23/25
- **E2 gate** : hard assertions sur 50 levels via Chrome MCP scripté :
  - 80 levels chargent sans crash console (smoke run 5s each)
  - 6 levels lourds (W7-8, W10-8, etc.) tiennent FPS ≥ 45 desktop
  - 10 levels représentatifs : assertions castleHpPercent + killSpendRatio in range
  - mobile responsive (viewport 390x844) bouton wave tactile
  - mono-château validé (count(C) = 1) sur 80 levels via validator

**Si gate rouge** : opus identifie quel ticket a foiré, lance bug-fixer ou feature-dev en patch, re-run gate. Pas de passage sprint suivant tant que green.

### 4bis.6 Déploiement `/v5/` parallèle de `/v3/`

Mike veut comparer A/B continu — `/v3/` reste l'actuel intouché tant que `/v5/` n'est pas validé S8 final.

**Actions concrètes** (à TICKET-E1-K en S5 jour 1) :

- Lire `.github/workflows/deploy.yml` et `vite.config.*` actuels.
- Ajouter un script `npm run build:v5` (alias de `build:kingshot` pour l'instant, mais nommé "v5" pour distinguer).
- Modifier le workflow GH Pages :
  - L'étape build actuelle produit `dist-kingshot/` qui est copié vers `/v3/`.
  - Ajouter étape parallèle : build avec `BASE_URL=/v5/` → copier vers `/v5/`.
  - Garder `/v3/` figé sur le tag `v3.0-park-defense-stable` ou simplement le snapshot avant refonte.
- Solution simple : geler `/v3/` en pinant le workflow sur un commit SHA précédent + alias supplémentaire `/v5/` qui suit main.
- Solution propre (recommandée) : 2 jobs dans le même workflow, l'un build depuis tag `v3-snapshot`, l'autre depuis HEAD. Output dirs distincts.

**Tag à créer en S1 jour 1** : `v3.1-pre-refonte-strategique` au commit `ad7fa39` (état actuel). Sert de reference figée pour `/v3/`. Tout commit refonte vit sur `main` HEAD → `/v5/`.

**Effort** : 1 commit en E1, 1.5 h. Listed as TICKET-E1-K.

**Verification** : après merge E1-K, ouvrir `/v3/` (joue ancien) et `/v5/` (joue nouveau) côte à côte. Différence sensible immédiate.

### 4bis.7 Update CLAUDE.md root + création src-v3/CLAUDE.md

**`CLAUDE.md` root** (existant, à updater en S1 jour 0) :

- Section "État du jeu" : ajouter mention du plan en cours + lien vers `.claude/plans/rustling-nibbling-wirth.md`.
- Section "Live actuel" : mentionner `/v3/` + `/v5/` parallèle.
- Section "Workflow session" : ajouter étape "lire `.claude/status/STATUS.md` en premier".
- Section "Backlog actuel" : remplacer roadmap actuel par "Refonte stratégique 8 semaines en cours, cf plan principal".

**`src-v3/CLAUDE.md`** (à créer S1 jour 0) :

- Vocabulaire grammar étendu (J, *).
- Helpers debug nouveaux (`__cd.metrics`).
- Raccourcis nouveaux (`N` lancer vague).
- Mono-château règle stricte.
- Multi-portail policy (1P / 2P opposés / 3P / 4P central).
- Pattern auto-QA scenarios.

### 4bis.8 Setup checklist S1 jour 0 (avant R1)

À exécuter par opus en début de S1 (commande Sonnet ou direct) :

1. `mkdir -p .claude/{plans,research,specs,agents,status,qa/scenarios,qa/reports}` (créer arborescence).
2. Créer 5 fichiers persona dans `.claude/agents/` : `td-researcher.md`, `game-designer.md`, `level-designer.md`, `ux-designer.md`, `auto-qa-runner.md` (contenu YAML §4 + §4bis.4).
3. Créer `.claude/status/STATUS.md` (template §4bis.2 vide).
4. Créer 6 stubs `.claude/status/sprint-{R1,R2,D1,D2,E1,E2}.md` (template §4bis.3).
5. Update `CLAUDE.md` root (§4bis.7).
6. Créer `src-v3/CLAUDE.md` (§4bis.7).
7. Tag git : `v3.1-pre-refonte-strategique` au commit `ad7fa39`.
8. Setup déploiement `/v5/` (§4bis.6 — peut être retardé jusqu'à E1 si bloqué, mais ideal S1).
9. Commit "chore: bootstrap plan strategique 8 semaines (R1-E2)".

Effort total setup : 1 demi-journée. À faire en early S1 lundi.

---

## 5. Sprints (8 semaines, 6 sprints)

Structure : **R1, R2** (recherche pure, agents `td-researcher`), **D1, D2** (design + spec, agents `game-designer` + `level-designer` + `ux-designer`), **E1, E2** (exécution, agents `feature-dev` orchestrés par Opus).

Une semaine = 5 jours ouvrés. Chaque sprint produit des livrables markdown atomiques. R + D ne touchent **aucun code**. E livre atomiquement, commit par commit, en worktree.

### Récap visuel

```
S1   S2   S3   S4   S5   S6   S7   S8
[R1][R2][D1][D2][      E1       ][      E2       ]
```

---

### Sprint R1 — Semaine 1 : recherche industrie (1/2)

**Goal** : cartographier les meilleurs jeux du genre sur les 3 piliers (économie, pacing, level design). Comprendre comment ils résolvent ce que Mike veut résoudre.

**Livrables** (markdown, dans `/Users/mike/Work/milan project/.claude/research/`) :

- `R1-01-economy-benchmark.md` — étude 5 jeux sur courbes coût, revenu, scaling boss/wave.
- `R1-02-pacing-benchmark.md` — étude 5 jeux sur waves manuelles, skip bonus, multi-skip streaks, telegraphing.
- `R1-03-mapdesign-benchmark.md` — étude 5 jeux sur tailles map, multi-portails, mazing, decision points.
- `R1-04-autoqa-benchmark.md` — étude des pratiques industrie pour auto-QA par AI/LLM sur jeux (Anthropic computer-use, Playwright + LLM judge, scripted scenarios, deterministic seeds). 5 patterns identifiés + applicabilité Milan.

**Agents lancés** (en parallèle, 1 message avec 4 appels Agent) :

- `td-researcher` instance "économie".
- `td-researcher` instance "pacing".
- `td-researcher` instance "level design".
- `td-researcher` instance "auto-QA AI" (nouvelle).

**Prompt skeleton R1 économie** (à coller verbatim, adapté) :

```
Tu es td-researcher. Étudie l'économie de :
1. Kingdom Rush (1 + Frontiers + Origins)
2. Bloons TD 6
3. Element TD 2
4. GemCraft Frostborn Wrath
5. Defense Grid: The Awakening

Pour chaque jeu, produis :
1. Tableau coût L1/L2/L3 des 3 tours signature (gold, ratio L3/L1, ratio L2/L1).
2. Courbe revenu par wave (W1, W5, W10) — gold gagné si tu kill tout.
3. Ratio kill/spend cible décodé du game design.
4. Mécaniques anti-spam : interest, treasure room, passive income, paywall L3.
5. Boss reward vs mob standard (multiplicateur).

Fais ressortir 3 patterns universels et 3 patterns différenciants.
Termine par "Applicabilité à Milan CD V3 (Three.js, 13 towers, économie kill-only)"
en 100-150 mots par jeu.

Output : `/Users/mike/Work/milan project/.claude/research/R1-01-economy-benchmark.md`.
NE PROPOSE PAS DE SOLUTION. Recherche pure.
```

**Prompt skeleton R1 pacing** :

```
Tu es td-researcher. Étudie le pacing wave-by-wave de :
1. Kingdom Rush Origins
2. Bloons TD 6
3. Plants vs Zombies (1 + 2)
4. Iron Marines
5. Anomaly Warzone Earth

Pour chaque jeu :
1. Comment le joueur lance-t-il une wave ? (auto, bouton, deux modes)
2. Bonus pour skip / start anticipé ? (formule chiffrée si possible)
3. Délai max entre 2 waves (le jeu force-t-il un auto-start ?)
4. UI/UX du bouton lancer-wave : taille, placement, feedback.
5. Multi-skip streaks : encouragé ? capé ?
6. Telegraphing : comment le jeu signale la composition à venir.

Trouve 3 designs qui FORCENT le joueur à penser, 3 qui le laissent flow.
Termine par "Applicabilité à Milan CD V3" 100-150 mots par jeu.

Output : `.claude/research/R1-02-pacing-benchmark.md`.
NE PROPOSE PAS DE SOLUTION.
```

**Prompt skeleton R1 level design** :

```
Tu es td-researcher. Étudie le level design de :
1. Kingdom Rush Vengeance
2. Defense Grid 2
3. GemCraft Frostborn Wrath
4. Mindustry (Serpulo campaign)
5. Element TD 2

Pour chaque jeu :
1. Range de tailles de map (cells/tuiles) — min/médiane/max.
2. Multi-portails : combien, cardinal/opposé/random, gameplay impact.
3. Fork-paths : statiques, dynamiques (joueur peut bloquer), branchings.
4. Mazes : entièrement joueur-construit (BTD), pré-tracé (KR), hybride.
5. Density tours buildables : ratio buildable/total.
6. Decoration vs gameplay : combien de tiles non-fonctionnelles.
7. Difficulty curve telegraphing : map plus grande = plus dure ? Multi-portail = boss ?

Trouve 3 patterns "petite map dense + dur" vs "grande map + multi-path".
Termine par "Applicabilité à Milan CD V3 (grammar 01PCWL DR T B ~^, BFS pathfinder)"
100-150 mots par jeu.

Output : `.claude/research/R1-03-mapdesign-benchmark.md`.
NE PROPOSE PAS DE SOLUTION.
```

**Décisions clés à prendre à la fin de R1** (à porter à Mike via AskUserQuestion en début S2 ou lors du review) :

1. Quel système d'économie inspire le plus pour V3 ? (linéaire KR, exponentiel BTD6, palier paywall ?)
2. Skip bonus : flat (+1¢/s restant) ou multiplicatif (× streak) ?
3. Map size sweet-spot pour Three.js sur mid-range mobile ?

**Critères "done" R1** :

- 4 fichiers markdown 200–400 lignes chacun (économie, pacing, mapdesign, autoqa).
- ≥ 5 jeux par fichier (sauf autoqa : 5 patterns industrie), données chiffrées.
- Section "Applicabilité à Milan CD V3" par jeu / pattern.
- Mike review les 4 fichiers en début S2, valide ou demande révisions ciblées.

**Sprint-gate R1** (vendredi S1, avant passage R2) :

- Run `auto-qa-runner` agent en mode "doc-check" sur les 4 R1-*.md.
- Assertions hard : chaque fichier existe + lignes ≥ 200 + sections "Applicabilité" présentes + ≥ 5 jeux étudiés.
- Soft : LLM judge évalue la "densité utile" de chaque rapport.
- Output : `.claude/qa/reports/sprint-R1-2026-05-15.md`.
- Update `STATUS.md` ligne R1 = green.

**Anti-patterns R1** :

- "Best of TD" généraliste — focus strict sur les axes Mike.
- Proposition de solution Milan — researcher ≠ designer.
- Chiffres non-sourcés.

---

### Sprint R2 — Semaine 2 : recherche industrie (2/2) + audit Milan profond

**Goal** : compléter R1 avec 2 études manquantes (synergies + difficulty curve), audit data-driven du Milan actuel, perf baseline.

**Livrables** :

- `R2-04-synergies-benchmark.md` — Element TD combos, GemCraft fusion, BTD6 tower paths, KR upgrade specials.
- `R2-05-difficulty-curve-benchmark.md` — pacing world-by-world dans KR/BTD6/Mindustry. Quand le jeu "casse" le joueur. Comment il prépare la rupture.
- `R2-06-milan-current-audit.md` — état chiffré des 80 levels Milan, ratio kill/spend, heatmap difficulté.
- `R2-07-perf-baseline.md` — benchmarks Chrome MCP sur world10-8 (250 mobs + 20 towers L3). Limite Three.js avant écroulement. Test 25×19 simulé.

**Agents lancés** (parallèle) :

- `td-researcher` × 2 (synergies + difficulty curve).
- `qa-tester` mode audit (script Node ou eval Chrome MCP itérant sur 80 levels).
- `perf-auditor` baseline.

**Prompt skeleton R2 qa-tester audit Milan** :

```
Mission : dump data-driven des 80 levels Milan CD V3.

1. Lis `src-v3/data/levels/world*.js` (80 fichiers).
2. Pour chaque level, extrais :
   - id, name, world, theme
   - map dimensions (cols × rows)
   - count(P), count(C), count(W), count(L)
   - count('0') (buildable cells)
   - sum HP de toutes les waves (resolve via ENEMY_TYPES de src-v3/entities/Enemy.js)
   - sum reward de toutes les waves
   - castleHP, startCoins
   - ratio attendu kill/spend = (startCoins + sumReward) / (sumHP × 0.6)
     (0.6 = estimateTowerEfficiency proxy moyen)
3. Sort par world, génère un tableau markdown.
4. Identifie les 10 levels "skewed" :
   - 5 levels avec ratio > 1.5 (trop d'or possible)
   - 5 levels avec ratio < 0.7 (trop tendu)
5. Ajoute heatmap visuelle (emoji 🟢🟡🟠🔴) par world × wave.

Output : `.claude/research/R2-06-milan-current-audit.md`.
NE MODIFIE AUCUN FICHIER source — read-only.
Format final ≤ 600 lignes.
```

**Prompt skeleton R2 perf-auditor** :

```
Mission : établir le perf budget Three.js pour la refonte 8 semaines.

1. Live test https://michaelchevallier.github.io/lava_game/v3/?debug=1.
2. Goto world10-8 (`__cd.loadLevel("world10-8")`).
3. Spawn 250 enemies via `__cd.runner._spawnEnemy("brute")` × 250 (loop async).
4. Place 20 towers manuelles L3 sur les buildPoints.
5. Mesure FPS sur 5s (technique standard agent).
6. Répète sur :
   - mode desktop natif (viewport > 1200px)
   - mode mobile simulé (viewport 390x844, throttle 4× CPU)
7. Identifie le bottleneck #1 (collision broad-phase, draw calls, particles).
8. Simule un map 25×19 en agrandissant runtime `runner.level.map`. Mesure à quelle
   taille on tombe sous 45 FPS desktop / 30 FPS mobile.

Output : `.claude/research/R2-07-perf-baseline.md`.
FPS table + bottleneck top-3 + recommandation max grid size.
```

**Décisions clés à prendre à la fin de R2** :

1. Modèle de synergie cible : aura passive (actuel) ou combo nommé (Element TD) ou upgrade path divergent (BTD6) ?
2. Pacing inter-monde : difficulty step W4→W5 — quelle "rupture" déclencher ?
3. Perf budget : cap quelle taille de map sur le hardware cible ?

**Critères "done" R2** : 4 fichiers markdown, R2-06 scripté (pas écrit main), Mike review.

**Sprint-gate R2** (vendredi S2) :

- Run `auto-qa-runner` doc-check sur les 4 R2-*.md.
- Hard run perf live sur `/v3/` (avant refonte) : FPS ≥ 50 desktop sur world10-8 baseline (sanity, on doit pouvoir reproduire).
- Output : `.claude/qa/reports/sprint-R2-2026-05-22.md`.
- Update `STATUS.md` ligne R2 = green.

---

### Sprint D1 — Semaine 3 : design coeur (économie + pacing + synergies + castle HP)

**Goal** : transformer R1/R2 en specs implémentables. Fixer les nombres.

**Livrables** (`/Users/mike/.claude/specs/D1-*.md`) :

- `D1-01-economy-spec.md` — refonte coût tours, coût upgrade, reward kill, magnet rework, treasure tiles.
- `D1-02-pacing-spec.md` — bouton "Lancer la vague", skip bonus + streak, raccourci `N`, UX message visible, mobile.
- `D1-03-upgrade-l3-hybride-spec.md` — L1/L2 linéaire, L3 paywall ×4 + choix binaire spécialisation pour 4 tours signature.
- `D1-04-castle-hp-spec.md` — courbe castleHP par world, expected loss target, no-regen W5+.

**Agents lancés** (parallèle) :

- `game-designer` × 2 (économie + L3 hybride).
- `game-designer` × 1 (castleHP).
- `ux-designer` × 1 (pacing UX + bouton wave + mobile).

#### Détail spec D1-01 (Économie)

- `_upgradeCost` (`main.js:1097`) : L1→L2 = `1.5 × base`, L2→L3 = `2.5 × base supplémentaire` → total L3 = `5.0 × base`. **Ratio L3/L1 = 5.0×** (vs 2.5× actuel).
- Costs L1 augmentés +20 % sur tours fortes : crossbow `140 → 160`, mage `70 → 85`, mine `60 → 80`.
- Reward kill par world : `reward_actual = base × (1 - 0.05 × (world - 1))`. W1 = ×1.0, W10 = ×0.55.
- Magnet rework :
  - `coinMul: 2.0 → 1.3`.
  - `cost: 100 → 130`.
  - `range: 6.5 → 5`.
  - Anti-double : si un autre magnet existe à < 6 cells → placement refusé (warning HUD).
- Treasure tiles (cell `*`) : 1–2 par level, persistent jusqu'à ce qu'un mob les touche → +50–150¢ si non touché. Inspiration GemCraft "Apparition gem".
- Pas de passive income (refusé : risque trop riche cf BTD6 interest bank exploit).

**Cible** : ratio kill/spend < 0.65 en W5+ sur 80 % des runs.

#### Détail spec D1-02 (Pacing)

- Désactiver auto-start : `_waveBreakTimer` devient compteur fail-safe (timeout à `breakMsMax = 60_000` ms = 60 s) — pour éviter joueur AFK.
- Bouton **"⚔️ Lancer la vague (N)"** :
  - Desktop : bottom-center HUD, taille 220×56px, animation pulse (1.2s ease-in-out), couleur état default `#ffd23f`.
  - Mobile : bottom-right au-dessus du speed-control, taille 80×80px, anim pulse.
  - Debounce 300ms (anti double-tap).
- Raccourci `N` (audit fait : pas utilisé dans `main.js:828-921`, OK).
- Skip bonus :
  - `+1¢ par seconde restante du breakMsMax` (cap +30¢).
  - **ET** `+5 % reward sur la wave suivante` cumulatif si skip multi (cap +25 %).
  - Toast HUD : "⚔️ Wave 3 lancée — Bonus +18¢, +15 % reward" 1.8s.
- Preview composition wave suivante : tooltip permanent au-dessus du bouton "Prochaine : 88 basic, 14 runner, 4 brute".
- Mobile : bouton lancer-wave dédié, distinct du mobile-menu-btn (audit conflits).

#### Détail spec D1-03 (Upgrade L3 hybride)

- 4 tours signature : `archer`, `mage`, `ballista`, `cannon`. Les 9 autres restent linéaires sans choix L3.
- L1 → L2 : coût `1.5 × base`, stats +. Pas de choix.
- L2 → L3 : coût `4.0 × base × (1 + 0.1 × (world - 1))`. W1 archer = 30×4×1.0 = 120¢. W10 archer = 30×4×1.9 = 228¢.
- L3 = **choix binaire** spec :
  - archer L3 = `Sniper` (×3 dmg, fire-rate ÷2) **OU** `Pluie d'archer` (AOE 3 cells, +20 % dmg).
  - mage L3 = `Arcane` (×2.5 dmg, slow on hit) **OU** `Boule de feu` (AOE 4 cells, burn DOT).
  - ballista L3 = `Pierce ∞` (perce tous) **OU** `Explosion` (AOE 5 cells, knockback).
  - cannon L3 = `Mega shell` (×3 dmg, ralenti) **OU** `Shotgun` (multi-shot 5).
- Implémentation : extension `upgrade(level, branch)` dans `Tower.js:336-368`. UI radial menu = 2 boutons côte à côte au L2→L3.
- Synergies passives existantes (`Synergies.js`) restent mais impact réduit `+50% → +25%` (libère room pour les choix L3).

#### Détail spec D1-04 (Castle HP)

- Courbe `castleHP = 100 + 50 × √world × difficultyMul`.
- `difficultyMul` par level dans world : 1.0 / 1.0 / 1.05 / 1.1 / 1.15 / 1.15 / 1.2 / **boss = 1.5**.
- Pas de regen entre waves W5+ (`waveRegen` du hero) — supprimer ou cap fort.

**Décisions clés D1 à valider Mike** :

1. Coût L3 final : ×4 base × world-scale (proposé) ou ×3, ×5, ×8 ?
2. Skip bonus : 1¢/s + 5%/wave streak (proposé) ou variante ?
3. Magnet : ×1.3 (proposé) ou suppression complète ?

**Critères "done" D1** :

- 4 specs 200–400 lignes chacune au format `spec-writer`.
- Mike valide chaque spec individuellement.
- Specs n'éparpillent pas la modification : ≤ 5 fichiers par spec.

**Sprint-gate D1** (vendredi S3) :

- Run `auto-qa-runner` doc-check sur les 4 D1-*.md.
- Hard : chaque spec liste les fichiers cibles avec lignes précises, chaque chiffre a une justification (citation R1/R2 OR audit).
- Soft (LLM judge) : "les chiffres D1-01 sont-ils cohérents avec R1-01 ?", "les fichiers cités existent-ils vraiment ?", "le risk register est-il rempli ?".
- Output : `.claude/qa/reports/sprint-D1-2026-05-29.md`.
- Update `STATUS.md` D1 = green.

---

### Sprint D2 — Semaine 4 : design level (maps + mazes + multi-portails)

**Goal** : produire les specs de niveau grand-format. Définir le nouveau vocabulaire grammar. Refondre 40 levels W5–W10.

**Livrables** :

- `D2-01-map-grammar-extension.md` — extension grammar pour fork-paths (J), treasure tiles (*), maze blocking. Migration path.
- `D2-02-multi-portal-design.md` — guide design 1P/2P/4P. **4P = 1 castle central convergent** (décision Mike).
- `D2-03-maze-pattern-library.md` — 8–12 patterns "blocs" réutilisables (serpentin, double-S, croix, spirale, X, étoile, donjon, glitch).
- `D2-04-w5-w8-level-redesign.md` — refonte 32 levels W5–W8.
- `D2-05-w9-w10-level-redesign.md` — refonte 16 levels W9–W10 (avec 4P central convergent W7-8 + W10-8).
- `D2-06-w1-w4-tuning-spec.md` — tuning léger W1–W4 (élargir, décor +, waves rebalancées). PAS refonte map.

**Agents lancés** (parallèle) :

- `level-designer` × 2 (W5–W8 + W9–W10).
- `level-designer` × 1 transverse (grammar + multi-portal guide + maze library).
- `game-designer` revient pour waves W5–W10 cohérent D1-01.

#### Détail spec D2-01 (Grammar extension)

Nouvelles cells :

- `J` (junction probabiliste) : path cell qui force le mob à tirer rand parmi les neighbors J/1. Permet fork-path sans toucher BFS.
- `*` (treasure tile) : bonus +50–150¢ si non touché par mob jusqu'à la fin du level. Spawn 1–2 par level.
- (Possible `S` (spawner secondary) repoussé en post-MVP — pas critique pour W5–W10.)

Implémentation :

- `MapGrid.js` CELL enum étendu.
- `MapPathfinder.js` : J = neighbor random choice (seed deterministe par mob id).
- `MapValidator.js` : J doit avoir ≥ 2 neighbors path, * doit être sur cell non-path.
- `MapRenderer.js` : J = visual subtle (sol différent), * = treasure 3D model (réutiliser asset coin existant ?).

Rétrocompat : levels existants sans J/* restent valides.

#### Détail spec D2-02 (Multi-portail, mono-château)

**Règle absolue** : tout level n'a qu'**un seul château `C`**. Multi-portail = plusieurs `P` qui convergent vers ce castle unique. Décision actée (cf §1.1).

- 1P : levels W1–W4 majoritairement, tutorial-friendly.
- 2P opposés (N-S ou E-W) → 1 C : W3+ apparition, force répartition des défenses sur 2 fronts.
- 2P même côté (asymétrique) → 1 C : W5+ comme variante.
- 3P en triangle → 1 C : W7+, boss-style.
- **4P cardinaux convergents → 1 C central** : W7-8 (Espace, premier 4P du jeu) + W10-8 (boss IA hub). Le joueur doit choisir quel angle bouchonner. Échec = tout perdre.

**Refonte rétro** : `world9-8.js` (2P×2C actuel) → refondre en 2P×1C en D2-05 (la 2e castle ré-attribuée comme decoration ou supprimée, paths re-router vers le castle conservé).

Wave spawn distribution : champ `spawnWeight: { p0: 0.4, p1: 0.4, p2: 0.1, p3: 0.1 }` par wave si on veut asymétrie spatiale.

**Validation** : `MapValidator.js` doit lever une erreur si `count(C) > 1` (à ajouter en E2 TICKET-E2-VALIDATOR).

#### Détail spec D2-03 (Maze pattern library)

8 patterns standards (ASCII drawings dans le spec) :

1. **Serpentin S** — classic zigzag, lecture facile, applique tower placement linéaire.
2. **Double-S** — 2 serpentins parallèles, force split défense.
3. **Croix** — 2 paths perpendiculaires, multi-portail 2P E-W avec 1 C centre.
4. **Spirale** — 1P en spirale vers C central, défense circulaire.
5. **Étoile / X** — 4P cardinaux + spirale interne, idéal W7-8.
6. **Donjon** — couloirs étroits avec stanzas (zones plus larges), tower placement contraint.
7. **Glitch** — cyberpunk asymétrique, paths qui semblent se couper visuellement.
8. **Branches J** — utilise les junctions probabilistes, le joueur ne sait pas par où le mob va passer.

Chaque pattern doc : 3 variantes de taille (S/M/L), use case typique.

#### Détail spec D2-04 (W5–W8 redesign)

- **W5 Apocalypse** : rupture max. 8 levels, 2 multi-portail (W5-5 = 2P N/S, W5-8 = 3P triangle). Pattern : serpentin double + croix. CastleHP +40 % vs actuel. Briefing : "Le monde brûle, t'es seul.".
- **W6 Foire Magique** : chaos visuel + mazes simples avec **diversion paths** (chemins courts vs long avec build slot). 8 levels, 1 multi-portail (W6-7). Pattern : couloirs longs + entonnoir.
- **W7 Espace** : grandes maps (23×17 max). 4 multi-portail dont **W7-8 = 4P cardinaux convergents** (premier vrai 4P, castle au centre). Pattern : étoile / X / rotational symmetry.
- **W8 Sous-Marin** : streams water massifs, bridges ~ surchargent paths. 8 levels, 1 multi-portail (W8-5). Pattern : bulles + courants. Tower frost interdite (thématique).

#### Détail spec D2-05 (W9–W10 redesign)

- **W9 Médiéval** : retour racines TD, 4 levels avec moats. W9-8 existant 2P/2C **refondu en 2P/1C** (mono-château, cf §1.1) avec maze donjon. Pattern : donjon + cours.
- **W10 Cyberpunk** : finale. **Tous les levels** ont au moins 2P. **W10-8 = 4P cardinaux convergents** + IA boss multi-phase. Pattern : circuits / glitched / asymmetric.

#### Détail spec D2-06 (W1–W4 tuning)

- Ne pas révolutionner — le tuto fonctionne.
- Élargir maps 13×11 → 17×13.
- Ajouter décor (T/R/B/D) pour casser la linéarité visuelle.
- Waves rebalancées légèrement pour matcher D1-01 (reward × 1.0 mais moins de mobs si trop d'or).
- W3+ : introduit 1 niveau avec 2P opposés (W3-7 par ex).

**Décisions clés D2 à valider Mike** :

1. Grammar extension : J + * (proposé) ou juste * (treasure) ou rien et garder grammar actuelle ?
2. Cap taille map : 25×19 (proposé) ou plus si perf-baseline R2-07 le permet ?
3. Boss W*-8 multi-phase obligatoire ou variations ?

**Critères "done" D2** :

- 6 specs, 80 levels documentés (anciens + nouveaux).
- Grammar extension a migration path (rétrocompat).
- Mike valide par batch (10 levels/jour).

**Sprint-gate D2** (vendredi S4) :

- Run `auto-qa-runner` doc-check sur les 6 D2-*.md.
- Hard : chaque level redessiné a son grid ASCII en commentaire, son rationale (pattern + multi-portail si > 1), `count(C) === 1` strict (mono-château), ≥ 30 % buildable.
- Soft (LLM judge) : "les maps W5+ sont-elles assez différentes des actuelles ?", "le wave count matche-t-il la cible D1-04 ?", "les patterns library sont-ils bien réutilisés ?".
- Output : `.claude/qa/reports/sprint-D2-2026-06-05.md`.
- Update `STATUS.md` D2 = green.

---

### Sprint E1 — Semaines 5–6 : exécution coeur (économie + pacing + grammar)

**Goal** : implémenter specs D1 + D2-01 dans `src-v3/`. Pas encore les nouveaux levels (E2).

**Méthode** : tickets atomiques, chaque ticket = 1 Sonnet `feature-dev` en worktree `isolation: "worktree"`, `run_in_background: true`. Tickets indépendants lancés en **parallèle dans un seul message**.

#### Sprint E1.1 — Semaine 5 (5 tickets parallèles)

- **TICKET-E1-A** : refonte `_upgradeCost` + `cost` par tour (D1-01).
  - Fichiers : `src-v3/main.js:1097-1103`, `src-v3/entities/Tower.js:9-119`.
  - Estimé : 1 commit, 2 h.
- **TICKET-E1-B** : courbe reward kill par world (D1-01).
  - Fichiers : `src-v3/systems/LevelRunner.js:586-590`, nouveau helper `_worldRewardMul()`.
  - Estimé : 1 commit, 1 h.
- **TICKET-E1-C** : magnet rework + anti-double (D1-01).
  - Fichiers : `src-v3/entities/Tower.js:91-101`, `src-v3/systems/Synergies.js`. Test BFS distance min.
  - Estimé : 1 commit, 2 h.
- **TICKET-E1-D** : treasure tiles `*` (D1-01 + D2-01).
  - Fichiers : `src-v3/systems/MapGrid.js`, `src-v3/systems/MapRenderer.js`, `src-v3/systems/LevelRunner.js` (gain logic).
  - Estimé : 2 commits, 4 h.
- **TICKET-E1-E** : bouton "Lancer la vague" + raccourci `N` + désactiver auto-start (D1-02).
  - Fichiers : `index.html` (bouton DOM + CSS), `src-v3/main.js:828-921` (keyboard `N`), `src-v3/systems/LevelRunner.js:498-513` (logic).
  - Estimé : 2 commits, 3 h. **Le plus visible UX**, prioritaire.

**Conflits prévisibles E1.1** :

- A + C touchent `Tower.js` → merger A puis rebase C.
- D + E touchent `LevelRunner.js` → séquentiel.

#### Sprint E1.2 — Semaine 6 (5 tickets parallèles)

- **TICKET-E1-F** : skip bonus + streak system (D1-02).
  - Fichiers : `src-v3/systems/LevelRunner.js`, `src-v3/main.js` (toast), `index.html` (bouton state).
  - Estimé : 2 commits, 4 h.
- **TICKET-E1-G** : upgrade L3 hybride (choix binaire pour 4 tours signature) (D1-03).
  - Fichiers : `src-v3/entities/Tower.js:336-368` (`upgrade(level, branch)`), `src-v3/main.js:1097-1118` (UI radial menu choix), CSS.
  - Estimé : 3 commits, 6 h. **Le plus délicat** (touche le radial menu existant).
- **TICKET-E1-H** : castle HP curve + no-regen W5+ (D1-04).
  - Fichiers : `src-v3/systems/LevelRunner.js:110-161`.
  - Estimé : 1 commit, 1.5 h.
- **TICKET-E1-I** : grammar extension `J` (junction probabiliste) (D2-01).
  - Fichiers : `src-v3/systems/MapGrid.js`, `src-v3/systems/MapPathfinder.js`, `src-v3/systems/MapValidator.js`, `src-v3/systems/MapRenderer.js`.
  - Estimé : 3 commits, 5 h. Étendre `npm run test:crowdef`.
- **TICKET-E1-J** : instrumentation `window.__cd.metrics` pour QA auto (Objectifs §2).
  - Fichiers : `src-v3/systems/LevelRunner.js` (capture events), nouveau `src-v3/systems/Metrics.js`.
  - Estimé : 1 commit, 2 h.

**Total E1** : ~13 commits (incluant TICKET-E1-K déploiement `/v5/`), ~32 h Sonnet (parallélisable wall-clock 2 semaines).

**TICKET-E1-K** : déploiement parallèle `/v5/` (§4bis.6).
- Fichiers : `.github/workflows/deploy.yml`, `vite.config.*`, `package.json` (script `build:v5`).
- Estimé : 1 commit, 1.5 h.
- Anchor S5 jour 1 — débloque tout E1.2 auto-QA sur `/v5/`.

**Critères "done" E1** :

- `npm run build:kingshot` passe.
- `npm run build:v5` passe (nouveau alias).
- `/v5/` est live et accessible publiquement.
- `npm run test:crowdef` ≥ 23/25 (baseline) + 4 tests nouveaux.
- `__cd.metrics.lastRun` retourne `{ castleHpPercent, killSpendRatio, skipsUsed, durationSec }`.

**Sprint-gate E1** (vendredi S6) — gros gate, hard assertions sur `/v5/` live :

- Bouton "Lancer la vague" présent + cliquable desktop + raccourci `N` fonctionne.
- Bouton wave présent mobile (viewport 390x844 simulé), tactile OK.
- Skip bonus s'applique : run 1 wave, skip après 5s, check `runner.coins` +X.
- Upgrade L3 hybride : choix binaire affiché pour `archer`, `mage`, `ballista`, `cannon` (4 tours signature).
- Magnet rework : double placement (< 6 cells) refusé avec warning HUD.
- Treasure tile spawn dans 2/3 runs sur world1-3 (validate `*` cell).
- `__cd.metrics.lastRun` shape correct.
- `npm run test:crowdef` ≥ 23/25.
- `/v5/` distinct visuellement de `/v3/` (Mike compare side-by-side).
- Auto-QA scenarios listés dans `.claude/qa/scenarios/sprint-E1/` exécutés à 90%+.
- Output : `.claude/qa/reports/sprint-E1-2026-06-19.md`.
- Update `STATUS.md` E1 = green.

#### Mid-project demo S6 vendredi

Mike rejoue 3 levels (W1.3, W5.5, W10.4) avec un build E1 frais. Décide si on continue E2 ou si on patch la conception (1 semaine de réparation).

---

### Sprint E2 — Semaines 7–8 : exécution levels + polish + QA

**Goal** : implémenter 40 nouveaux levels W5–W10 (D2-04+05) + tuning W1–W4 (D2-06) + polish + QA.

#### Sprint E2.1 — Semaine 7 (10 tickets parallèles)

- **TICKET-E2-W5** : 8 levels W5 (`world5-1.js` à `world5-8.js`). 1 commit, 1.5 h.
- **TICKET-E2-W6** : 8 levels W6. 1 commit, 1.5 h.
- **TICKET-E2-W7** : 8 levels W7 (inclut **4P W7-8 castle central** — vérifier perf live). 1 commit, 2 h.
- **TICKET-E2-W8** : 8 levels W8. 1 commit, 1.5 h.
- **TICKET-E2-W9** : 8 levels W9. 1 commit, 1.5 h.
- **TICKET-E2-W10** : 8 levels W10 (inclut **4P W10-8 castle central IA boss**). 1 commit, 2 h.
- **TICKET-E2-W1-4-TUNE** : tuning 32 levels W1–W4. 1 commit, 2 h.
- **TICKET-E2-MAZE-LIB** : étendre `world1-mazetest.js` avec les 8 patterns library + debug helper. 1 commit, 1 h.
- **TICKET-E2-VALIDATOR** : étendre `MapValidator.js` (30 % buildable, no trivial path, J min 2 neighbors, **count(C) === 1 strict** — mono-château). 1 commit, 1.5 h.
- **TICKET-E2-CAMERA** : auto-fit camera optimisé 25×19 + 4P (regarde `__pd_camera_fit`). 1 commit, 2 h.

**Aucun conflit prévu** (tickets isolés par world).

#### Sprint E2.2 — Semaine 8 (polish + QA)

- **TICKET-E2-PERF** : `perf-auditor` sur les 6 levels les plus lourds (W7-8 4P, W10-8, W10-7, etc.). Cible 55 FPS desktop / 40 mobile.
- **TICKET-E2-MOBILE** : `qa-tester` exhaustif mobile sur 80 levels (smoke test). Bouton wave tactile, joystick non-conflit, no scroll issues.
- **TICKET-E2-METRICS-VALIDATION** : run automatisé QA scripté sur 50 levels via Chrome MCP. Dump `__cd.metrics.lastRun` × 50, stats sur castleHpPercent / killSpendRatio. Comparer Objectifs §2.
- **TICKET-E2-BUGS** : pool de bug fixes émergents. Sonnet `bug-fixer` reactif (multi-commits, à la demande).
- **TICKET-E2-BRIEFINGS-POLISH** : polish briefings W5–W10 (Mike rewrites finaux ou `level-designer` 2e pass).
- **TICKET-E2-SAVE-MIGRATION** : si refonte casse saves (nouveaux level IDs, level data shape change), agent `feature-dev` migrate `SaveSystem.js` avec `saveVersion: 2`.
- **TICKET-E2-DOC** : `quality-maintainer` update `CLAUDE.md` + create `src-v3/CLAUDE.md` avec nouveau vocabulaire grammar + nouveaux raccourcis + objectifs §2 archivés.

**Critères "done" E2** :

- 80 levels jouables sans crash console.
- Métriques agrégées QA respectent Objectifs §2 (≥ 70 % conformité).
- FPS budget tenu (TICKET-E2-PERF).
- `git log --oneline | grep "feat(v3)\|fix(v3)" | wc -l` ≥ 30 commits sur les 8 semaines.

**Sprint-gate E2** (jeudi S8) — final ship-gate. Hard assertions massive auto-QA sur `/v5/` :

- Smoke run 5s sur 80 levels : pas de crash console, mob spawn OK, castle existe (mono-C strict).
- 6 levels lourds (W7-8 4P, W10-8 IA, W10-7, W9-7, W8-7, W5-8) : FPS ≥ 45 desktop, ≥ 30 mobile simulé.
- 10 levels représentatifs (1 par world + 4 bossfights) run complet automated : capture `__cd.metrics.lastRun`, assertion castleHpPercent et killSpendRatio dans range Objectifs §2.
- Mobile viewport 390x844 : bouton wave tactile, joystick non-conflict, no scroll issue sur tous menus.
- Validator `count(C) === 1` strict passe sur 80 levels.
- Mono-château assertion : `runner.castles.length === 1` runtime sur 5 levels random.
- Comparaison `/v3/` vs `/v5/` : screenshots côte-à-côte sur 5 levels phares (W1-3, W5-5, W7-8, W10-1, W10-8) — judge "visuellement plus dense, jouablement plus tendu".
- Output : `.claude/qa/reports/sprint-E2-2026-07-03.md` + dashboard summary `.claude/qa/reports/ship-readiness.md`.
- Mike valide ship final → tag `v5.0-strategic-overhaul` + bascule `/v5/` comme défaut éventuel (ou `/v5/` reste alias et `/v3/` reste primary tant que Mike pas convaincu).
- Update `STATUS.md` E2 = green + Project = shipped.

---

## 6. Risques

### 6.1 Techniques

- **Perf Three.js collapse sur 25×19 + 4P (mono-C)** :
  - Mitigation : R2-07 baseline force la décision dès S2. Si non viable → fallback "max 21×15 + 3P" et redesign D2.
  - Plan B : LOD aggressive sur decor, InstancedMesh pour mobs.
- **Camera auto-fit casse sur 4P cardinaux** :
  - Mitigation : TICKET-E2-CAMERA dédié, tests live W7-8 + W10-8.
- **Save migration corrompt progressions** :
  - Mitigation : TICKET-E2-SAVE-MIGRATION explicite, `saveVersion: 2`, migration auto au boot.
- **Pathfinder BFS sur J (junction probabiliste)** :
  - Mitigation : MapValidator détecte junctions sans issue, test:crowdef coverage.
- **test:crowdef baseline 23/25 régresse** :
  - Mitigation : chaque ticket Sonnet run `npm run test:crowdef` après commit, red = fix immédiat.

### 6.2 Design

- **Jeu trop dur, joueurs casuals abandonnent** :
  - Mitigation : W1–W4 reste accessible (D2-06 cap). Cibler "70 % finissent W4" garde-fou. Mode "Casual" (×0.7 mobs HP) optionnel à débattre D1 si Mike veut.
- **Trop d'or perdu détruit "spam endgame"** :
  - Mitigation : ratio kill/spend recover en W9–W10 (0.55 ≠ 0.30). Spam reste possible si joueur bien joué.
- **4P castle central trop facile à break (focus 1 angle suffit)** :
  - Mitigation : level design ajoute paths qui se croisent, junctions J pour forcer split défense. Boss multi-phase avec summon par angle.

### 6.3 UX

- **Bouton wave mobile masque joystick** :
  - Mitigation : `ux-designer` spec D1-02 zone safe. Wireframe ASCII validé avant E1.
- **Raccourci `N` collision navigateur** :
  - `N` simple sans modifier = safe (vérif `ux-designer` audit clavier).
- **Mobile double-tap accidentel** :
  - Mitigation : debounce 300 ms.
- **Mike ne valide pas pacing après E1** :
  - Pivot critique. Mid-sprint demo S6 vendredi = check.

### 6.4 Scope

- **D2 lève questions qui requièrent retour R1/R2** :
  - Mitigation : budget 10 % du temps D1/D2 pour recherche étoffée.
- **40 levels W5–W10 en 1 semaine = ambitieux** :
  - 8 levels/world/day = 30 min/level. Tendu mais faisable si patterns réutilisés.
  - Mitigation : si retard, slip 8 levels W6 (Foire) à sprint E2.3 post-projet.

---

## 7. Standards industrie à étudier (mapping feature ↔ référent)

Lecture obligatoire pour `td-researcher`. Réutiliser ce mapping comme légende dans R1/R2.

### 7.1 Pacing : waves manuelles + skip bonus

- **Kingdom Rush** (1, Frontiers, Origins, Vengeance) : référence absolue. Bouton "Send wave now" bas droite, `+restant × 1¢/s`. Mantra "stratégique = patient" baked in.
- **Bloons TD 6** : auto-start mais "Fast Forward" boost x3. Time-pressure différent.
- **Iron Marines** : RTS hybride, ressources scarcity. Pacing événementiel (waves triggered par story).
- **Anomaly Warzone Earth** : pacing inversé (joueur attaque), notion préparation temps très bien designée.
- **Plants vs Zombies** : waves définies par level, "vague énorme" bossfight signalée.

### 7.2 Économie : revenu, sinks, anti-spam

- **Bloons TD 6** : monkey knowledge + insta-monkey + special agents = sinks multiples. Interest bank mid-late.
- **GemCraft Frostborn Wrath** : mana = currency, gem fusion = sink puissant. Wave skip = mana bonus important. Multi-skip streaks récompensés.
- **Element TD 2** : économie ultra-réactive aux choix (combos rares + chers = paywalls).
- **Defense Grid 2** : tours chères absolu, peu de spam, mazing pur fait le boulot.
- **Kingdom Rush** : économie linéaire stable, simple à lire, gold balance par level.

### 7.3 Level design : maps + multi-portails + mazes

- **GemCraft** : portails 1–4 cardinaux, joueur peut ouvrir des portails secondaires. **Référence parfaite pour Milan**.
- **Mindustry (Serpulo)** : grands mazes, joueur construit le path (route logistique). Lessons sur dense gameplay.
- **Defense Grid 2** : multi-paths organic, branches qui re-merge, choix de path par mob via cost-shortest.
- **Plants vs Zombies** : layouts hyper compacts, lanes égales — opposé Milan mais lessons sur "diversité dans la contrainte".
- **Kingdom Rush Vengeance** : remix multi-portails (boss levels) avec script wave-by-wave.

### 7.4 Synergies + upgrade trees

- **Bloons TD 6** : 3 upgrade paths par tower, max 2 paths simultanés (xx5-2-0 rule). **Référence excellence**.
- **Element TD 2** : combos d'éléments (jusqu'à 6 éléments → arc-en-ciel rare). Modèle pour "long tail" de combinaisons.
- **GemCraft** : gem fusion = create new tier. Pas directement applicable Milan mais intéressant psychologie joueur.
- **Kingdom Rush** : 4 upgrades + 2 specials par tower. Simple, lisible. Modèle pour Milan (proche actuel).

### 7.5 Boss design (W*-8 levels)

- **Kingdom Rush** : boss avec phases visibles (HP threshold = phase shift), summon adds. **Référence pour W9-8, W10-8**.
- **Bloons TD 6** : BAD (Big Airborne Destruction) boss-mob unique tank épique.
- **GemCraft** : Apparitions = boss-wave entière, plusieurs boss simultanés.
- **Iron Marines** : boss + RTS controls = phase reset stratégique forcé.

### 7.6 UX bouton wave + raccourcis

- **Kingdom Rush** : bouton bas-droite, pulsing, gros tactile.
- **Bloons TD 6** : fast-forward + play/pause sticky bar tout en bas.
- **Defense Grid 2** : raccourci `Space` skip, `T` upgrade — patterns à valider.

---

## 8. Synthèse 2 mois → MVP livré

### 8.1 Output final attendu (semaine 8)

- **Coeur jeu refondu** : économie tendue (ratio kill/spend atteint), pacing joueur-driven, upgrade L3 hybride (paywall + choix binaire) sur 4 tours signature.
- **80 levels rejouables** : 40 redesignés W5–W10, 32 tunés W1–W4, 8 untouched (debug/showcase/foire/maze test).
- **Grammar étendue** : J + *, validée par BFS + validator. Compat saves anciennes.
- **UX moderne** : bouton wave HUD bas-centre, raccourci `N`, mobile responsive (bouton 80×80px).
- **Multi-portails déployés (mono-château)** : 6+ levels avec 2P→1C, 3+ avec 3P→1C, 2 levels phare 4P→1C central (W7-8, W10-8).
- **Metrics instrumentés** : `window.__cd.metrics` capture par run.
- **Perf budget tenu** : 55 FPS desktop / 40 mobile sur pire level.

### 8.2 Timeline visuelle (par jour)

```
S1 lun jour 0 : Bootstrap infra §4bis.8 (agents files, STATUS.md, /v5/ setup, tag v3.1)
S1 lun-jeu    : R1 (researcher × 4 parallèle : econ, pacing, mapdes, autoqa)
S1 ven        : Sprint-gate R1 (auto-qa-runner doc-check) + Mike review
S2 lun-jeu    : R2 (researcher × 2 + qa-tester audit + perf-auditor)
S2 ven        : Sprint-gate R2 + Mike review
S3 lun-mer    : D1 (game-designer × 3 + ux-designer parallèle)
S3 jeu        : Mike review specs D1
S3 ven        : Sprint-gate D1 (doc-check + soft judge)
S4 lun-mer    : D2 (level-designer × 4 parallèle)
S4 jeu        : Mike review specs D2 (10/jour)
S4 ven        : Sprint-gate D2 (doc-check + soft judge)
S5 lun        : TICKET-E1-K déploiement /v5/ live (priorité, débloque auto-QA suivante)
S5 lun-ven    : E1.1 (5 tickets Sonnet parallèles en worktree)
S6 lun-jeu    : E1.2 (5 tickets Sonnet parallèles)
S6 ven        : Sprint-gate E1 (hard assertions /v5/ live) + Mid-project demo Mike
S7 lun-jeu    : E2.1 (10 tickets level deployment, parallèle)
S7 ven        : E2.2 begin (perf-auditor + qa-tester)
S8 lun-jeu    : E2.2 continue (mobile, bugs, save migration, docs)
S8 ven        : Sprint-gate E2 final (ship-readiness auto-QA massive) + tag v5.0
```

### 8.3 Estimation effort (jours-agent)

- R1 : 3 j × 3 agents = 9 j-agent.
- R2 : 4 j × 4 agents = 16 j-agent.
- D1 : 5 j × 4 agents = 20 j-agent.
- D2 : 5 j × 4 agents = 20 j-agent.
- E1 : 10 j × 5 Sonnets parallèles = 50 j-agent (wall-clock 2 semaines).
- E2 : 10 j × 10 Sonnets parallèles = 100 j-agent (wall-clock 2 semaines).

Total ~215 j-agent compressés en 40 jours wall-clock (parallélisation worktree).

### 8.4 Out of scope (à débattre extension post-MVP)

- Nouvelles tours (les 13 actuelles suffisent — refonte cost/upgrade pas ajout type).
- Nouveaux mobs (les 30 types suffisent — refonte reward/HP scaling pas ajout type).
- Story branchée cross-world (déjà fait).
- Endless rework, Daily rework (touche pas).
- Localisation EN (FR reste la langue).
- Audio refonte (touche pas).
- Mode Casual / Hard global (à débattre si Mike veut).

### 8.5 Définition de "MVP livré"

Mike rejoue W1.1 → W10.8 et **sent** que :

1. Chaque niveau lui demande de penser.
2. Le château finit blessé (parfois mort), pas full HP comme avant.
3. Skip wave devient un choix risk/reward conscient.
4. Les maps W5+ ont du caractère (grandes, multi-portails, mazes).
5. L3 est un investissement, pas un automatisme. Le choix binaire L3 crée des builds différents.
6. L'or est précieux, le magnet n'est plus un cheat.

S'il valide → ship `v4.0-strategic-overhaul` + tag. S'il rejette un pilier → 1 semaine de patch sprint sur le pilier KO.

---

## 9. Setup de la prochaine session "contexte frais"

Quand Mike ouvre la prochaine session pour exécuter le plan, **exécuter la checklist §4bis.8 d'abord** :

1. **Lire** ce fichier (`/Users/mike/.claude/plans/rustling-nibbling-wirth.md`).
2. **Lire** `/Users/mike/Work/milan project/CLAUDE.md`.
3. **Lire** `.claude/status/STATUS.md` (créé en S1 jour 0 par cette session ou par la suivante).
4. **Setup infrastructure §4bis.8** si pas déjà fait :
   - Créer arborescence `.claude/{plans,research,specs,agents,status,qa/scenarios,qa/reports}`.
   - Créer 5 personas dans `.claude/agents/` : `td-researcher.md`, `game-designer.md`, `level-designer.md`, `ux-designer.md`, `auto-qa-runner.md`.
   - Créer `STATUS.md` + 6 stubs `sprint-XX.md`.
   - Update `CLAUDE.md` root + créer `src-v3/CLAUDE.md`.
   - Tag git `v3.1-pre-refonte-strategique` au commit `ad7fa39` (état actuel) — référence figée pour `/v3/`.
   - Setup déploiement `/v5/` (cf §4bis.6) — peut être retardé à E1 TICKET-E1-K mais idéal S1.
   - Commit "chore: bootstrap plan strategique 8 semaines".
5. **Vérifier** : `git status` clean après commit bootstrap, `git branch --show-current` = `main`, `npm run test:crowdef` = 23/25 baseline.
6. **Lancer R1** : 4 agents `td-researcher` en parallèle via 1 message avec 4 appels Agent (économie, pacing, level design, auto-QA — prompts skeleton §5 Sprint R1 + § R1-04 dans §4bis.4).
7. **À la fin de R1** vendredi S1 : lancer `auto-qa-runner` pour sprint-gate R1 + update `STATUS.md`.

Le plan est self-contained : pas besoin de re-explorer le codebase ni re-comprendre l'audit. Toutes les références (`Tower.js:9-119`, `main.js:1097-1103`, `LevelRunner.js:499-513`) sont précises et stables au commit `ad7fa39`.

---

## 10. Validation Mike (à signer avant S1)

Pour démarrer S1 lundi matin, Mike doit valider :

- [ ] Découpage 8 semaines R1/R2/D1/D2/E1/E2 OK.
- [ ] 5 nouveaux agents (`td-researcher`, `game-designer`, `level-designer`, `ux-designer`, `auto-qa-runner`) seront créés S1 jour 0.
- [ ] Objectifs mesurables §2 sont les bons.
- [ ] 5 jeux référents par axe §7 sont les bons (ou swap).
- [ ] Grammar extension `J` + `*` OK (ou juste `*`, ou rien).
- [ ] Mid-project demo S6 ven est dans son calendrier.
- [ ] Save migration risque accepté (TICKET-E2-SAVE-MIGRATION mitigation).
- [ ] Décisions D1 actées : L3 hybride paywall + choix binaire (acté), mono-château partout (acté), 4P central convergent vers 1 castle (acté).
- [ ] Infrastructure §4bis acceptée : `.claude/{plans,research,specs,agents,status,qa}/` + `STATUS.md` tracker + 6 sprint-files + `src-v3/CLAUDE.md`.
- [ ] Discipline auto-QA fin de chaque sprint (sprint-gate) acceptée : pas de passage sprint suivant sans gate vert.
- [ ] Déploiement `/v5/` parallèle de `/v3/` accepté (tag `v3.1-pre-refonte-strategique` figé pour `/v3/`, `main` HEAD pour `/v5/`).

Si tout validé → S1 démarre par §4bis.8 checklist puis R1.

---

## 11. Verification end-to-end

Comment vérifier la sortie 8 semaines :

```bash
# Build OK
cd "/Users/mike/Work/milan project"
npm run build:kingshot   # build legacy /v3/ (figé sur tag v3.1-pre-refonte-strategique)
npm run build:v5         # build refonte /v5/

# Tests
npm run test:crowdef    # ≥ 23/25 + 4 nouveaux

# Local
npm run dev:v5          # http://localhost:4174/v5/?debug=1
# Dans la console :
window.__cd.unlockAll()
window.__cd.loadLevel("world1-1")    # run E2E, check pacing manual
window.__cd.loadLevel("world5-5")    # check 2P opposed → 1 castle
window.__cd.loadLevel("world7-8")    # check 4P central convergent → 1 castle
window.__cd.loadLevel("world10-8")   # check final boss 4P + IA hub → 1 castle
window.__cd.metrics.lastRun          # check instrumentation
window.__cd.runner.castles.length    # === 1 strict

# Mobile (responsive)
# DevTools → Toolbar mobile → iPhone 14 Pro Max
# Check bouton wave bottom-right + joystick non-conflict

# Perf
# DevTools → Performance tab → record 30s sur world10-8
# Target : ≥ 55 FPS desktop / ≥ 40 FPS mobile simulé

# Comparaison /v3/ vs /v5/
# Ouvre 2 onglets côte à côte :
# https://michaelchevallier.github.io/lava_game/v3/
# https://michaelchevallier.github.io/lava_game/v5/
# Joue world5-5 sur les deux, observe ressenti (économie, pacing, map size)

# Auto-QA sprint-gate (à la fin de chaque sprint)
node .claude/qa/auto-qa-runner.mjs --sprint=E1   # exécute scenarios sprint E1
# OR invoque agent auto-qa-runner via Agent tool dans Claude
```

---

## Critical Files for Implementation

Fichiers les plus critiques à modifier en E1/E2 :

- `/Users/mike/Work/milan project/src-v3/main.js` — économie upgrade + raccourcis clavier + radial menu (lignes 1097–1118, 828–921).
- `/Users/mike/Work/milan project/src-v3/entities/Tower.js` — TOWER_TYPES costs + LEVEL_SCALE + upgrade method (lignes 9–119, 336–368).
- `/Users/mike/Work/milan project/src-v3/systems/LevelRunner.js` — wave timer + reward calc + castle HP loading (lignes 90–161, 480–513, 586–600).
- `/Users/mike/Work/milan project/src-v3/systems/MapGrid.js` — grammar extension cells J/* (lignes 7–22).
- `/Users/mike/Work/milan project/src-v3/systems/MapPathfinder.js` — BFS already multi-P/multi-C compatible. J extension à ajouter.
- `/Users/mike/Work/milan project/src-v3/systems/Synergies.js` — magnet rework + L3 branch effects.
- `/Users/mike/Work/milan project/src-v3/data/levels/world{5,6,7,8,9,10}-*.js` — 48 levels à redesigner (8 par world).
- `/Users/mike/Work/milan project/src-v3/data/levels/world{1,2,3,4}-*.js` — 32 levels à tuner légèrement.
- `/Users/mike/Work/milan project/index.html` — HUD bouton "Lancer la vague".

Fichiers de référence à consulter sans modifier :

- `/Users/mike/Work/milan project/CLAUDE.md` — workflow Opus orchestre / Sonnet exécute.
- `/Users/mike/Work/milan project/.claude/agents/feature-dev.md`, `spec-writer.md`, `qa-tester.md`, `perf-auditor.md` — templates existants.
- `/Users/mike/Work/milan project/src-v3/systems/MapValidator.js` — règles validation maps actuelles.
- `/Users/mike/Work/milan project/src-v3/systems/Enemy.js` — ENEMY_TYPES + reward + HP.
