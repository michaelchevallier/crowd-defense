# D1-04 — Castle HP Scaling + Mob Pressure + No-Regen Spec

**Sprint** : D1 — Design coeur (refonte stratégique W1→W10)
**Date** : 2026-05-11
**Persona** : `game-designer`
**Scope** : (a) courbe `castleHP` par level (80 cells), (b) pression mob parallèle `mobHpMul`/`mobCountMul`/`mobSpeedMul` par world, (c) suppression `waveRegen` W6+, (d) cibles fail-rate W*-8 et HP residual W6+.
**Cibles E1** : `TICKET-E1-H` (castleHP) + `TICKET-E1-K` (mob pressure) + `TICKET-E1-L` (no-regen W6+).

---

## 1. Contexte

### 1.1 Décisions Mike arbitrées (interview 2026-05-11)
- **Castle HP scaling A+B combiné** : (A) château plus solide W1→W10 ET (B) pression mob ↑ en parallèle. Quote : *"Tu peux te tromper 1-2 fois mais après tu meurs."*
- **Rupture difficulty W5→W6** (au lieu de W4→W5). *"Je veux pas que le jeu devienne intéressant que sur les derniers niveaux donc à partir de W5-W6, je veux une vraie difficulté qui impose réflexion."*
- W1-W5 = apprentissage/préparation. W6+ = rupture, "extrêmement difficile". **Boss W*-8 = 50-70% fail premier essai**.
- **No-regen castle HP entre waves W6+** (au lieu de W5+ spec initiale `src-v3/CLAUDE.md`).

### 1.2 État actuel (audit R2-06)
- 80 levels, `castleHP = 100` constant partout. Aucun scaling world.
- `LevelRunner.js:128,141` : fallback `this.level.castleHP || 100`.
- `LevelRunner.js:484-488` : `waveRegen` (hero perk) appliqué chaque wave clear, sans cap world.
- `Hero.js:99,249,280` : `waveRegen` additif, `castleHPMaxMul` boost via `forteressePerk` (×1.5).
- Audit pression mob : `LevelRunner.js:90` `SWARM_MUL = 1.4 × ...` global, pas de scaling world.

### 1.3 Pattern référence (R2-05 §6.3)
- KR1 : lives constants 10/20 (boss = 20). Pas de regen.
- BTD6 : lives capped par mode. Pas de regen.
- PvZ1 : 1 lawnmower/lane, pas regen.
- Mindustry / GCFW : regen passif (philo différente, écartée par Mike).
- **3/5 jeux pas regen. Aucun ne scale safety bar par world** → Milan innovera (A+B Mike).

---

## 2. Fichiers impactés

| Fichier | Lignes | Modification |
|---|---|---|
| `src-v3/systems/LevelRunner.js` | 110-161 | `loadCastles()` + `loadCastlesFromGrid()` : ajouter calcul `castleHP = castleHPFor(world, level)` si pas override level |
| `src-v3/systems/LevelRunner.js` | 484-488 | Cap `waveRegen` : `if (worldNum >= 6) skip` |
| `src-v3/systems/LevelRunner.js` | 83-104 | `_initWave()` : appliquer `mobHpMul` + `mobCountMul` + `mobSpeedMul` par world |
| `src-v3/entities/Enemy.js` | 222-223 | Au spawn : `hpMax = cfg.hp × mobHpMul; speed = cfg.speed × mobSpeedMul` |
| `src-v3/systems/Balance.js` (nouveau) | — | Tableaux `WORLD_PRESSURE_TABLE`, `LEVEL_DIFFICULTY_MUL` + helpers |

---

## 3. Formules

### 3.1 Castle HP par level

```js
castleHP = Math.round(100 + 50 × Math.sqrt(world) × difficultyMul);
// world ∈ [1..10], difficultyMul ∈ [1.0..1.5] selon level dans world (§3.3)
```

**Justification `√world`** :
- W1 = `100 + 50 × 1.0 × 1.0 = 150` (2 hits Tyrant si présent).
- W10 = `100 + 50 × 3.162 × 1.0 = 258` (×1.7 vs W1) — château survit ~3 hits heavy mob W10 (dmg 80-90).
- Croissance sub-linéaire `√` cohérente : KR (constant), BTD6 (capped). Évite explosion HP qui tuerait le sentiment "tendu".
- **Linéaire `100 + 25×W`** rejeté → W10 = 350 = 5 hits = trop permissif.
- **Exponentiel `100 × 1.15^W`** rejeté → W10 = 405 = encore plus permissif.

### 3.2 Pression mob parallèle par world

Cible : **W6 = +50% pression effective vs W5** (rupture demandée).

| World | `mobHpMul` | `mobCountMul` | `mobSpeedMul` | Composite |
|---|---|---|---|---|
| W1 | 1.00 | 1.00 | 1.00 | **1.000** (baseline) |
| W2 | 1.10 | 1.05 | 1.00 | 1.155 |
| W3 | 1.20 | 1.10 | 1.00 | 1.320 |
| W4 | 1.30 | 1.15 | 1.00 | 1.495 |
| W5 | 1.40 | 1.20 | 1.05 | 1.764 |
| **W6** | **1.65** | **1.30** | **1.10** | **2.360** |
| W7 | 1.85 | 1.35 | 1.10 | 2.747 |
| W8 | 2.05 | 1.40 | 1.12 | 3.214 |
| W9 | 2.25 | 1.45 | 1.15 | 3.751 |
| W10 | 2.50 | 1.50 | 1.18 | 4.425 |

**Rupture W5→W6** : pression composite +34% (1.764→2.360). Combiné avec castleHP +5% seulement (W5=212 → W6=222), **ratio HP/pression chute -22%** (W5=120 → W6=94). Avec no-regen W6+ ajouté, **rupture effective ressentie ≈ +50% conforme cible Mike**.

**Justifications multipliers** :
- `mobHpMul` croît fort → W10 = ×2.5 (vs KR ×150 sur 12 stages = ×12.5/zone, Milan plus doux sur 10 worlds).
- `mobCountMul` modéré (stack sur `SWARM_MUL=1.4` existant).
- `mobSpeedMul` prudent W5+ (joueur doit pouvoir réagir). Cap +18% W10.

### 3.3 `difficultyMul` par level dans world

| Level | `difficultyMul` | Sens |
|---|---|---|
| W*-1 | 1.00 | Intro |
| W*-2 | 1.00 | Conso |
| W*-3 | 1.05 | Variation |
| W*-4 | 1.10 | Premier pic |
| W*-5 | 1.15 | Plateau haut |
| W*-6 | 1.15 | Pré-boss-mid |
| W*-7 | 1.20 | Mid-boss / wall |
| **W*-8** | **1.50** | **Boss world (cible 50-70% fail)** |

**Justification W*-8 = ×1.5** : boss = pic dur. Castle légèrement renforcé pour donner marge anti-RNG, MAIS combiné avec mob pressure du world + comp boss multi-phase + no-regen W6+ → rupture nette. Alternative `difficultyMul=0.7` (château faible) rejetée → contradictoire avec "1-2 erreurs avant mort".

---

## 4. Tableaux finaux

### 4.1 `Balance.js` export (JS)

```js
// src-v3/systems/Balance.js (nouveau)
export const WORLD_PRESSURE_TABLE = {
  1:  { mobHpMul: 1.00, mobCountMul: 1.00, mobSpeedMul: 1.00 },
  2:  { mobHpMul: 1.10, mobCountMul: 1.05, mobSpeedMul: 1.00 },
  3:  { mobHpMul: 1.20, mobCountMul: 1.10, mobSpeedMul: 1.00 },
  4:  { mobHpMul: 1.30, mobCountMul: 1.15, mobSpeedMul: 1.00 },
  5:  { mobHpMul: 1.40, mobCountMul: 1.20, mobSpeedMul: 1.05 },
  6:  { mobHpMul: 1.65, mobCountMul: 1.30, mobSpeedMul: 1.10 },
  7:  { mobHpMul: 1.85, mobCountMul: 1.35, mobSpeedMul: 1.10 },
  8:  { mobHpMul: 2.05, mobCountMul: 1.40, mobSpeedMul: 1.12 },
  9:  { mobHpMul: 2.25, mobCountMul: 1.45, mobSpeedMul: 1.15 },
  10: { mobHpMul: 2.50, mobCountMul: 1.50, mobSpeedMul: 1.18 },
};

export const LEVEL_DIFFICULTY_MUL = {
  1: 1.00, 2: 1.00, 3: 1.05, 4: 1.10,
  5: 1.15, 6: 1.15, 7: 1.20, 8: 1.50,
};

export function castleHPFor(world, level) {
  const dMul = LEVEL_DIFFICULTY_MUL[level] || 1.0;
  return Math.round(100 + 50 * Math.sqrt(world) * dMul);
}

export function extractWorldNum(levelId) {
  const m = (levelId || "").match(/^world(\d+)-(\d+)$/);
  return m ? { world: +m[1], level: +m[2] } : { world: 1, level: 1 };
}
```

### 4.2 Tableau `castleHP` final — 80 cells (world × level)

| World | L1 | L2 | L3 | L4 | L5 | L6 | L7 | L8 (boss) |
|---|---|---|---|---|---|---|---|---|
| W1 | 150 | 150 | 153 | 155 | 158 | 158 | 160 | **175** |
| W2 | 171 | 171 | 174 | 178 | 181 | 181 | 185 | **206** |
| W3 | 187 | 187 | 191 | 195 | 200 | 200 | 204 | **230** |
| W4 | 200 | 200 | 205 | 210 | 215 | 215 | 220 | **250** |
| W5 | 212 | 212 | 217 | 223 | 229 | 229 | 234 | **268** |
| **W6** | **222** | **222** | **229** | **235** | **241** | **241** | **247** | **284** |
| W7 | 232 | 232 | 239 | 246 | 252 | 252 | 259 | **298** |
| W8 | 241 | 241 | 249 | 256 | 263 | 263 | 270 | **312** |
| W9 | 250 | 250 | 258 | 265 | 273 | 273 | 280 | **325** |
| W10 | 258 | 258 | 266 | 274 | 282 | 282 | 290 | **337** |

Range : **min W1-1 = 150 HP, max W10-8 = 337 HP. Ratio ×2.25**. Conforme Mike "plus solide W10 mais pas explosion".

### 4.3 No-regen W6+ — pseudo-code

```js
// LevelRunner.js:484-488 modifié
if (this.hero && this.hero.waveRegen > 0) {
  const { world } = extractWorldNum(this.level.id);
  if (world < 6) {
    for (const c of this.castles) {
      if (!c.isDead) c.hp = Math.min(c.hpMax, c.hp + this.hero.waveRegen);
    }
  }
  // W6+ : skip regen (silencieux)
}
```

**Skip total** préféré à cap partiel pour clarté Mike + cohérence benchmarks. Alternative B (`× 0.3`) en réserve si playtest frustré.

### 4.4 Mob pressure application — pseudo-code

```js
// LevelRunner.js:83-104 modifié
_initWave(idx) {
  const w = this.level.waves.list[idx];
  if (!w) { ... }
  const { world } = extractWorldNum(this.level.id);
  const pressure = WORLD_PRESSURE_TABLE[world] || WORLD_PRESSURE_TABLE[1];
  const SWARM_MUL = 1.4 * (this.swarmMul ?? 1) * (this._eventEnemyCountMul ?? 1) * pressure.mobCountMul;
  // ... existing build list logic ...
  this._spawnRate = Math.round((w.spawnRateMs || 600) * 0.75 / pressure.mobSpeedMul);
  this._currentMobHpMul = pressure.mobHpMul;
  this._currentMobSpeedMul = pressure.mobSpeedMul;
  this._currentBreakMs = w.breakMs ?? 4000;
}

// dans _spawnEnemy() : appliquer hpMul + speedMul à l'instance créée
enemy.hpMax = Math.round(cfg.hp * this._currentMobHpMul);
enemy.hp = enemy.hpMax;
enemy.speed = cfg.speed * this._currentMobSpeedMul;
```

### 4.5 Override par level

Code prend `level.castleHP || castleHPFor(world, levelNum)`. Recommandation : **ne pas override** sur 80 levels campagne. Showcase/debug garde override 100.

---

## 5. Critères de succès

| Métrique | Cible | Mesure |
|---|---|---|
| HP residual W6+ end of run | **70% des runs < 50% HP** | autoQA 100 runs W6-1 à W10-8 |
| Fail rate boss W*-8 first try | **50-70%** | autoQA "naive build" 20 runs/level |
| Marge erreur W1-1 | 2 hits max | 150/Tyrant.dmg=80 = 1.87 ✅ |
| Marge erreur W10-8 boss | 2 hits max | 337/200 = 1.68 ✅ |
| Rupture W5→W6 perçue | +25-50% effective | Ratio HP/pression -22% + no-regen ✅ |
| Tilt W1-3 (early) | <5% fail rate | castleHP=153, pressure=1.32, regen actif → permissif |

Qualitatif : W1-W5 = "j'apprends, je respire". W6-W10 = "1 erreur = -30% HP". Boss = "dur mais battable avec bonne comp". No-regen = "je dois éviter les hits".

---

## 6. Effort estimé

**~1.5 jour** Sonnet `feature-dev` :
- **Commit 1** : create `src-v3/systems/Balance.js` (tables + helpers).
- **Commit 2** : modify `LevelRunner.js:110-161` use `castleHPFor()`.
- **Commit 3** : modify `LevelRunner.js:83-104` apply pressure mul.
- **Commit 4** : modify spawn flow apply `hpMul`/`speedMul` to Enemy.
- **Commit 5** : modify `LevelRunner.js:484-488` no-regen W6+.
- **Commit 6** : add 4 tests `test:crowdef` (baseline 23→27).

Tuning post-implé : 1 ticket auto-qa-runner ≈ 100 runs simulation, ~4h.
Override audit 80 levels : 1h opus.

---

## 7. Risques

| Risque | Prob. | Mitigation |
|---|---|---|
| Mob `HP×Count×Speed` combine trop dur W6 (fail>70%) | Moy. | Tuning autoQA → possible dial-back `mobCountMul` W6 à 1.20 |
| `waveRegen` retiré W6+ frustre players invest perk | Moy. | UX hint "perk inactif W6+" (D1-09 UI). Alt B : cap 0.3 si frustration playtest |
| Illusion facilité W10 (HP visible +) | Faible | mob ×2.5 + count ×1.5 compensent. Ratio HP/pression W10=76 < W6=94 → en fait plus dur W10 ✅ |
| Override `castleHP` dans JSONs court-circuite courbe | Moy. | Audit pass §4.5 obligatoire avant E1 |
| Tests `test:crowdef` baseline régresse si assumait castleHP=100 | Moy. | Adapter tests en commit 6, pas masker fails légitimes |
| Formule `√world` peu lisible côté docs | Faible | Table §4.2 hardcode + commentaire `Balance.js` |

---

## 8. Test plan

### 8.1 Tests unitaires (ajout `test:crowdef`)

```js
assert.equal(castleHPFor(1, 1), 150);
assert.equal(castleHPFor(6, 8), 284);
assert.equal(WORLD_PRESSURE_TABLE[6].mobHpMul / WORLD_PRESSURE_TABLE[1].mobHpMul, 1.65);

const runner = new LevelRunner(mockLevel("world6-3"), { hero: { waveRegen: 10 } });
const beforeHP = runner.castleHP;
runner._onWaveCleared();
assert.equal(runner.castleHP, beforeHP); // no regen W6+
```

Baseline cible : **27/27** post-implé.

### 8.2 Scénarios autoQA (Chrome MCP, `.claude/qa/scenarios/sprint-e1-difficulty/`)

1. **`castle-hp-progression.mjs`** : W1-1 → `castleHPMax === 150`. W6-8 → `284`. W10-8 → `337`.
2. **`no-regen-w6plus.mjs`** : hero `waveRegen: 20` ; W5-3 wave clear → HP +20. W6-3 wave clear → HP unchanged.
3. **`mob-pressure-scaling.mjs`** : W1-1 basic mob `hpMax === 5`. W6-1 basic mob `hpMax === 8` (round 5×1.65).
4. **`boss-w8-fail-rate.mjs`** : 20 runs naive build sur W6-8 → fail rate ∈ [50%, 70%].

### 8.3 Tuning post-implé
100 runs autoQA "average build" sur W6-8 à W10-8. Si fail<50% : +10% `mobHpMul` world. Si >70% : -10%.

---

## 9. Décisions ouvertes (Mike arbitrage)

1. **`waveRegen` skip total vs cap 0.3 W6+** — défaut skip total.
2. **Garder/supprimer `castleHP: 100` explicite dans 80 JSONs** — recommandation : supprimer.
3. **UI HUD castle HP avec couleur progressive vert/jaune/rouge** — spec D1-09.
4. **W1-1 floor 150 vs 200** — garder 150 sauf retour playtest "early frustration".

---

## 10. Sources

- `R2-05-difficulty-curve-benchmark.md` §6.3 (castle/lives policy) + §7 (walls position).
- `R2-06-milan-current-audit.md` data table 80 levels.
- `src-v3/CLAUDE.md` §"Castle HP refonte (D1-04...)" formule de base.
- `src-v3/systems/LevelRunner.js:110-161, 484-488` code actuel.
- `src-v3/entities/Hero.js:99,249,280` perk `waveRegen`.
- `src-v3/entities/Enemy.js:55-200` base HP cfg `ENEMY_TYPE`.
- Interview Mike 2026-05-11 — décisions arbitrées.
