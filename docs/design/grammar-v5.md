# Crowd Defense V3 → V5 — Instructions Claude (refonte stratégique)

> Ce fichier est créé en bootstrap S1 du plan stratégique 8 semaines (`/Users/mike/.claude/plans/rustling-nibbling-wirth.md`). Mis à jour sprint après sprint.
> À lire après `STATUS.md` au début de chaque session opus orientée code v5.

## Contexte

Codebase `src-v3/` reçoit la refonte stratégique 2026-05-11 → 2026-07-XX. Le nom du dossier reste `src-v3/` même si le déploiement cible devient `/v5/` (`/v4/` figé sur tag `v4.0-pre-refonte-strategique`).

Build :
- `npm run build:kingshot` → `dist-kingshot/` → déployé sur `/v5/` via GitHub Pages
- `npm run dev:kingshot` → http://localhost:4174/

Tests :
- `npm run test:crowdef` → 23/25 baseline (2 fails préexistants : `towers built >= 1`, `endless has 30 waves`). Tout ticket E1/E2 doit maintenir ce baseline + 4 nouveaux tests à ajouter en E1.

## Grammar maps (`MapGrid.js`)

CELL grammar actuelle (pré-extension D2-01) :

| Char | Sens                | Note                                           |
|------|---------------------|------------------------------------------------|
| `0`  | grass / buildable   | tour posable                                   |
| `1`  | path                | mob marche dessus                              |
| `P`  | portal (spawn)      | un ou plusieurs par level                      |
| `C`  | castle (objectif)   | **MONO-CHÂTEAU strict — count(C) === 1 toujours** |
| `~`  | bridge (water)      | path-like, traverse stream W                   |
| `^`  | bridge (lava)       | path-like, traverse stream L                   |
| `W`  | water stream        | bloque mob (sauf bridge ~)                     |
| `L`  | lava stream         | bloque mob (sauf bridge ^), DOT tower si touch |
| `D`  | decor (ground)      | non-functional, visual                         |
| `R`  | decor (rock)        | non-functional, blocks build                   |
| `T`  | decor (tree)        | non-functional, blocks build                   |
| `B`  | decor (bush)        | non-functional, walkable visual                |
| ` `  | void                | hors-jeu, fog                                  |

**Extensions D2-01 (à ajouter en E1 TICKET-E1-I + sprint D2-01)** :

| Char | Sens                            | Status              |
|------|----------------------------------|---------------------|
| `J`  | junction probabiliste           | **planned E1-I**    |
| `*`  | treasure tile (+50–150¢ si non touché) | **planned E1-D** |

`J` : path cell qui force le mob à tirer rand parmi les neighbors `J`/`1`. Permet fork-path sans toucher BFS. `MapValidator.js` doit vérifier J a ≥ 2 neighbors path.

`*` : bonus tile non-path, +50–150¢ si non touché par mob jusqu'à la fin du level. Spawn 1–2 par level. Inspiration GemCraft "Apparition gem".

Rétrocompat : levels existants sans J/* restent valides.

## Multi-portail policy (mono-château strict)

**Règle absolue** : tout level n'a qu'**un seul château `C`**. Multi-portail = plusieurs `P` qui convergent vers ce castle unique.

- **1P** : levels W1–W4 majoritairement, tutorial-friendly.
- **2P opposés** (N-S ou E-W) → 1 C : W3+ apparition, force répartition des défenses sur 2 fronts.
- **2P même côté** (asymétrique) → 1 C : W5+ comme variante.
- **3P en triangle** → 1 C : W7+, boss-style.
- **4P cardinaux convergents → 1 C central** : W7-8 (Espace, premier 4P du jeu) + W10-8 (boss IA hub). Le joueur doit choisir quel angle bouchonner. Échec = tout perdre.

`MapValidator.js` doit lever erreur si `count(C) > 1` (à ajouter en E2 TICKET-E2-VALIDATOR).

Refonte rétrocompat : `world9-8.js` (2P×2C historique) → refondu en 2P×1C en D2-05.

Wave spawn distribution : champ `spawnWeight: { p0: 0.4, p1: 0.4, p2: 0.1, p3: 0.1 }` par wave si on veut asymétrie spatiale par portail.

## Helpers debug (console DevTools)

Object `__cd` :
- `__cd.runner` accès au LevelRunner courant
- `__cd.scene` la THREE.Scene
- `__cd.camera` la THREE.PerspectiveCamera
- `__cd.goto(id)` charge un niveau direct
- `__cd.debugOn()` / `__cd.debugOff()` toggle menu debug
- `__cd.toggleCornerLabels()` toggle labels mask+rotY sur tiles
- `__cd.unlockAll()` débloque tout
- `__cd.runner._spawnEnemy("<type>")` spawn un mob au portail (utilisé par les zones d'interaction Q)
- `__cd.runner.staticEnemies` / `staticTowers` accès aux entités showcase

**Nouveau v5 (à ajouter en E1 TICKET-E1-J)** :
- `__cd.metrics.lastRun` retourne `{ castleHpPercent, killSpendRatio, skipsUsed, durationSec, towersPlaced, l3Reached }`.
- `__cd.metrics.session` cumule sur la session.

## Raccourcis clavier (audit `main.js:828-921`)

Existants :
- `1-9 / 0 / - / =` sélection tour
- `P` pause
- `Space` ult
- `Tab` zoom toggle
- `B` support
- `Shift` run
- `Q` interaction zone (showcase)

**Nouveau v5 (à ajouter en E1 TICKET-E1-E)** :
- `N` lancer la vague (audit fait : pas utilisé, OK)

## Bouton "Lancer la vague" (E1 TICKET-E1-E)

Desktop : bottom-center HUD, taille 220×56px, animation pulse (1.2s ease-in-out), couleur état default `#ffd23f`.
Mobile : bottom-right au-dessus du speed-control, taille 80×80px, anim pulse.
Debounce 300ms (anti double-tap).

Texte : `⚔️ Lancer la vague (N)`

Auto-start désactivé : `_waveBreakTimer` devient compteur fail-safe (timeout à `breakMsMax = 60_000` ms = 60 s) — pour éviter joueur AFK.

Skip bonus :
- `+1¢ par seconde restante du breakMsMax` (cap +30¢).
- **ET** `+5 % reward sur la wave suivante` cumulatif si skip multi (cap +25 %).
- Toast HUD : "⚔️ Wave 3 lancée — Bonus +18¢, +15 % reward" 1.8s.

## Économie refonte (D1-01, à appliquer en E1 TICKET-E1-A/B/C)

- `_upgradeCost` (`main.js:1097`) : L1→L2 = `1.5 × base`, L2→L3 = `2.5 × base supplémentaire` → total L3 = `5.0 × base`. **Ratio L3/L1 = 5.0×** (vs 2.5× actuel).
- Costs L1 augmentés +20 % sur tours fortes : crossbow `140 → 160`, mage `70 → 85`, mine `60 → 80`.
- Reward kill par world : `reward_actual = base × (1 - 0.05 × (world - 1))`. W1 = ×1.0, W10 = ×0.55.
- Magnet rework :
  - `coinMul: 2.0 → 1.3`.
  - `cost: 100 → 130`.
  - `range: 6.5 → 5`.
  - Anti-double : si un autre magnet existe à < 6 cells → placement refusé (warning HUD).

## Upgrade L3 hybride (D1-03, à appliquer en E1 TICKET-E1-G)

4 tours signature : `archer`, `mage`, `ballista`, `cannon`. Les 9 autres restent linéaires sans choix L3.

L1 → L2 : coût `1.5 × base`, stats +. Pas de choix.
L2 → L3 : coût `4.0 × base × (1 + 0.1 × (world - 1))`. W1 archer = 30×4×1.0 = 120¢. W10 archer = 30×4×1.9 = 228¢.

L3 = **choix binaire** spec :
- archer L3 = `Sniper` (×3 dmg, fire-rate ÷2) **OU** `Pluie d'archer` (AOE 3 cells, +20 % dmg).
- mage L3 = `Arcane` (×2.5 dmg, slow on hit) **OU** `Boule de feu` (AOE 4 cells, burn DOT).
- ballista L3 = `Pierce ∞` (perce tous) **OU** `Explosion` (AOE 5 cells, knockback).
- cannon L3 = `Mega shell` (×3 dmg, ralenti) **OU** `Shotgun` (multi-shot 5).

Implémentation : extension `upgrade(level, branch)` dans `Tower.js:336-368`. UI radial menu = 2 boutons côte à côte au L2→L3.

Synergies passives existantes (`Synergies.js`) restent mais impact réduit `+50% → +25%` (libère room pour les choix L3).

## Castle HP refonte (D1-04, à appliquer en E1 TICKET-E1-H)

- Courbe `castleHP = 100 + 50 × √world × difficultyMul`.
- `difficultyMul` par level dans world : 1.0 / 1.0 / 1.05 / 1.1 / 1.15 / 1.15 / 1.2 / **boss = 1.5**.
- Pas de regen entre waves W5+ (`waveRegen` du hero) — supprimer ou cap fort.

## Pattern auto-QA scenarios (S5+)

Format `.claude/qa/scenarios/sprint-<NAME>/<scenario-id>.mjs` :

```js
export default {
  id: "pacing-manual-wave",
  description: "Vérifie que le bouton wave doit être cliqué pour lancer",
  setup: async (page) => {
    await page.evaluate(() => window.__cd.unlockAll());
    await page.evaluate(() => window.__cd.goto("world1-1"));
  },
  assertions: [
    { type: "hard", expr: "document.querySelector('#wave-button-launch') !== null" },
    { type: "hard", expr: "window.__cd.runner._waveBreakTimer < window.__cd.runner._currentBreakMs" },
    { type: "soft", judge: "Le bouton est-il visible et bien centré bottom?" }
  ]
};
```

L'agent `auto-qa-runner` interprète ce format, lance via Chrome MCP, génère report dans `.claude/qa/reports/`.

## Files critiques (référence rapide)

- `src-v3/main.js:1097-1118` — `_upgradeCost`, radial menu (économie + L3 hybride)
- `src-v3/main.js:828-921` — keyboard shortcuts (raccourci `N` à ajouter)
- `src-v3/entities/Tower.js:9-119` — TOWER_TYPES (costs)
- `src-v3/entities/Tower.js:336-368` — `upgrade()` (L3 hybride)
- `src-v3/entities/Tower.js:91-101` — magnet (rework)
- `src-v3/systems/LevelRunner.js:103` — `_currentBreakMs` (auto-start fail-safe)
- `src-v3/systems/LevelRunner.js:499-513` — wave break logic (désactiver auto-start)
- `src-v3/systems/LevelRunner.js:586-590` — reward calc (`_worldRewardMul()` à ajouter)
- `src-v3/systems/LevelRunner.js:110-161` — castle HP loading (courbe refondue)
- `src-v3/systems/MapGrid.js:7-22` — CELL grammar (extension J + *)
- `src-v3/systems/MapPathfinder.js:26-54` — BFS (J extension à ajouter)
- `src-v3/systems/MapValidator.js` — count(C) === 1 strict (à ajouter)
- `src-v3/systems/Synergies.js` — magnet ×1.3 + L3 branch effects
- `index.html:1082-1090` — `#hud-top-buttons` pattern bouton (template `.speed-btn`)
- `index.html:1101` — `#wave-start-banner` (référence wave UI existante)
