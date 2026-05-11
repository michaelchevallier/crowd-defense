# D1-01 — Économie Milan CD V3 (refonte stratégique)

**Persona** : `game-designer`
**Sprint** : D1 — Design coeur
**Date** : 2026-05-11
**Mission** : refondre l'économie pour ramener le ratio kill/spend < 0.65 en W5+ sur 80 % des runs, casser le magnet-spam, ajouter un sink (interest leak penalty) et un pull (treasure tiles), durcir la marche L3.

---

## 1. Contexte

### 1.1 Diagnostic chiffré (R2-06)

Les 80 levels actuels affichent un ratio kill/spend (= `(startCoins + totalReward) / (totalHP × 0.6)`) **moyen 1.5–2.4** sur l'ensemble du jeu. Cible "tendu mais juste" du benchmark R1-01 : **0.4–0.65** (KR/BTD6 CHIMPS/DG). Tous les worlds sont en sur-rétribution :

- W1 1.53 (acceptable onboarding) — W2 1.92 — W3 2.05 — W4 1.95
- W5 **2.06** (pic) — W6 1.66 — W7 1.69 — W8 1.67 — W9 1.58 — W10 1.69

Top 5 levels les plus sur-rétribués : world5-6 (2.39), world3-5 (2.34), world3-2 (2.24), world4-1 (2.20), world2-2 (2.18). **Aucun level < 0.7.**

Conséquence : le joueur peut spammer magnet (×2.0 coin aura R6.5, audit `Tower.js:91-101`) sans contrepartie, achever toutes les upgrades L3 dès W3, transformer le late-game en autopilot.

### 1.2 Précédents industrie (R1-01 + R2-04)

- **Ratio top-tier vs L1** : KR Vengeance ×8.86, DG ×7, BTD6 ×125 (T5), ETD2 ×85 (L4 essence-locked), GCFW ×16 (G3/G1). Milan actuel ×2.5 = **3× moins agressif que la moyenne industrie**.
- **Boss reward** : KR = 0×, BTD6 = layer-by-layer (×500-2000 cumul mais $0 par "kill"), DG = ×10-30. Pattern universel **R1-01 P-U-3** : boss ≠ "mob ×1.5", soit 0× soit massif. Milan = mob ×1.0 actuellement = zone grise.
- **Anti-snowball banking** : ETD2 = interest +2 %/15 s **disabled si leak** (smart play récompensé, sloppy play puni). Milan = aucun mécanisme banking.
- **Bonus tile non-touchée** : GemCraft "Apparition gem" (récompense le contrôle parfait). Pattern transposable en `*` tile.

### 1.3 Décisions Mike arbitrées (interview 2026-05-11)

- **Interest leak penalty** : bank +5 %/wave, leak = bank reset 0. HUD affiche bank.
- **Ratio L3/L1 = ×5 total** : L1→L2 ×1.5, L2→L3 ×2.5 supplémentaire.
- **Boss reward = 0×** (cf KR P-U-3).
- **Magnet rework hard** : range 5, coinMul ×1.3, cost 130 (à compléter avec anti-double).
- **Cible ratio** : < 0.65 en W5+ sur 80 % des runs.

---

## 2. Fichiers impactés

| Path | Lines (audit) | Modification |
|---|---|---|
| `src-v3/main.js` | 1097-1103 (`_upgradeCost`) | Refonte formule L2/L3 |
| `src-v3/main.js` | 1080-1086 (tooltip cost) | Affichage `upgradeCost` (info, pas comportement) |
| `src-v3/entities/Tower.js` | 9-119 (TOWER_TYPES) | Costs L1 ajustement 3 tours fortes |
| `src-v3/entities/Tower.js` | 91-101 (magnet) | Rework coinMul/range/cost + flag anti-double |
| `src-v3/systems/LevelRunner.js` | 586-590 (reward) | Ajout `_worldRewardMul()` + check `isBoss` = 0 |
| `src-v3/systems/LevelRunner.js` | (nouveau) | `_waveBank` accumulator + leak detection |
| `src-v3/systems/MapGrid.js` | 7-22 (CELL grammar) | Ajout `*` treasure tile (déjà planned E1) |
| `src-v3/systems/MapPathfinder.js` | 26-54 (BFS) | `*` non-walkable (déjà planned) |
| `src-v3/systems/MapValidator.js` | (nouveau, déjà planned) | Vérif distance min entre magnets |
| `index.html` | 1082-1090 (HUD top-buttons) | Ajout `#hud-bank-display` |

**Hors-scope D1-01** (à compléter par D1-02 si besoin) : `Synergies.js` magnet passive (sera modifié par implémentation, mais la spec ici donne juste les numbers).

---

## 3. Pseudo-code des fixes

### 3.1 `_upgradeCost` refonte (`main.js:1097-1103`)

```js
function _upgradeCost(tower) {
  const baseCost = tower.cfg.cost || 50;
  const lvl = tower.upgradeLevel || 1;
  // L1→L2 : 1.5 × base. L2→L3 : 2.5 × base supplémentaire. Total L3 = 5.0 × base.
  if (lvl === 1) return Math.round(baseCost * 1.5);
  if (lvl === 2) return Math.round(baseCost * 2.5);
  return 0;
}
```

**Justification ratios** :
- L1→L2 ×1.5 : aligné KR (×1.57 Archer L1→L2 ref R2-04 §4.2), au-dessus de Milan actuel ×1.0 (audit `main.js:1100`).
- L2→L3 ×2.5 (supplémentaire) : marche cumulée ×5.0 vs L1. Aligné R1-01 P-U-2 (top-tier ≥ ×4 vs L1, ici ×5 = mid-range industrie). Décision Mike arbitrée.

### 3.2 Costs L1 ajustement (`Tower.js:9-119`)

12 tours actuelles dans `TOWER_TYPES`. Hike +20 % sur les 3 tours fortes identifiées dans audit codebase (`src-v3/CLAUDE.md` section "Économie refonte") :

| Tour | Cost actuel | Cost cible | Δ | Justification |
|---|---|---|---|---|
| archer | 30 | **30** | 0 % | Tour starter, ratio dmg/cost déjà ok |
| tank | 50 | **50** | 0 % | Tour starter |
| mage | 70 | **85** | +21 % | dmg 2.76 × AOE 2.0 = dominant W1-W3, hike pour aligner ratio dmg/coût avec archer |
| ballista | 100 | **100** | 0 % | dmg 5.52 pierce 2 = OK pour cost actuel |
| mine | 60 | **80** | +33 % | cluster ×3 + AOE 2.5 dmg 5.75 = burst le plus haut du jeu, sous-coûté |
| cannon | 100 | **100** | 0 % | dmg 6.9 AOE 3.0 = OK |
| fan | 70 | **70** | 0 % | utility, pas DPS |
| frost | 60 | **60** | 0 % | utility |
| crossbow | 140 | **160** | +14 % | range 16 dmg 6.9 pierce 4 = sniper end-game, hike pour réduire spam W4+ |
| portal | 130 | **130** | 0 % | utility buff aura |
| magnet | 100 | **130** | +30 % | Cf §3.4 rework |
| skyguard | 85 | **85** | 0 % | spécialisé volants |
| acid | 110 | **110** | 0 % | armor-break utility |

**Note** : `mage 85`, `mine 80`, `crossbow 160` sont les valeurs Mike arbitrées dans `src-v3/CLAUDE.md`. Les autres tours restent inchangées (audit indique pas de problème de balance individuel).

### 3.3 Reward kill courbe par world (`LevelRunner.js:586-590`)

```js
// _worldRewardMul(world) — formule linéaire descendante
// reward_actual = base × (1 - 0.05 × (world - 1))
function _worldRewardMul(world) {
  return Math.max(0.5, 1.0 - 0.05 * (world - 1));
}

// dans tick() ligne 586 :
if (e.dead) {
  const cfg = ENEMY_TYPES[e.type];
  if (cfg?.isBoss) {
    // Boss reward = 0× (cf R1-01 P-U-3, KR pattern).
    // gems/skin/XP conservés inchangés ligne 598+.
  } else {
    const baseReward = (e.reward || 2) * 0.85;
    const worldMul = this._worldRewardMul(this.level.world || 1);
    const magnetMul = Synergies.getCoinMulAt(e.group.position);
    const reward = Math.max(1, Math.round(
      baseReward * (this.hero ? this.hero.coinGainMul : 1) * magnetMul * worldMul * (this.coinMul ?? 1)
    ));
    this.coins += reward;
  }
  // ... reste du tick (XP, lifesteal, gems, skin drop) inchangé
}
```

**Valeurs cibles** :
- W1 : ×1.00 (baseline onboarding, ratio actuel 1.53 → cible 1.30)
- W5 : ×0.80 (pic actuel 2.06 → cible 0.65)
- W10 : ×0.55 (actuel 1.69 → cible 0.55)
- Floor 0.5 (W11+ endless / défi du jour).

**Justification formule** : linéaire descendante 5 %/world. Aligné BTD6 P-D-2 (pop income dégressif) mais sans la rupture brutale ×0.5 (Milan reste un jeu enfant, transitions douces). Calculé pour atteindre cible 0.65 au W5 (audit R2-06 médiane 2.06 ÷ 0.65 ≈ 3.17, soit reward × 0.31 vs base actuel + ratio L3 ×2 = effort joueur ×3.17, atteint à W5 par cumul reward_mul 0.80 × upgrade_mul 0.4 = 0.32). Le `Math.max(0.5, …)` évite floor négatif pour endless World 20+.

### 3.4 Magnet rework + anti-double (`Tower.js:91-101`)

```js
magnet: {
  range: 5,           // 6.5 → 5 (Mike arbitré)
  fireRateMs: 0, damage: 0, projColor: 0xff66aa, projSpeed: 0,
  asset: "tower_magnet", sizeMultiplier: 0.85, label: "Aimant", aoe: 0, pierce: 0,
  fallbackColor: 0xff4488,
  behavior: "coinPull",
  coinMul: 1.3,       // 2.0 → 1.3 (Mike arbitré)
  pullSlow: 0.7,
  cost: 130,          // 100 → 130 (Mike arbitré)
  icon: "🧲", unlockWorld: 4,
  placementRule: "antiDoubleMagnet",  // nouveau flag
  synergies: [
    { type: "passive", effect: { coinMul: 1.3 }, range: 5 },  // sync avec behavior
    { type: "crossEffect", from: "tank", effect: { pullToTank: true }, range: 4 },
  ],
},
```

**Anti-double : choix recommandé = cap global par level via compteur**, PAS BFS distance.

**Justification du choix BFS-distance vs cap-global** :
- BFS distance 6 cells : un level large (W7+ 4P convergents) pourrait poser 3-4 magnets répartis légalement (R6 entre chacun), ce qui contourne l'intention "max 1-2 magnets".
- Cap global compteur `magnetCount <= magnetCap` : déterministe, lisible (joueur sait combien il en a droit), aligné R1-01 P-U-3 (BTD6 "1 Tier 5 par tour-type" = limite structurelle dure).
- **Formule** : `magnetCap = level.allowMultiMagnet ? 2 : 1`. Par défaut **1 magnet/level**, flag opt-in `allowMultiMagnet: true` dans les data/levels JSON pour W7+ multi-portail (max **2** dans ces cas-là, jamais 3+).

**Implémentation** (côté placement) :
```js
// dans le handler de placement (main.js, ou wherever placement valide) :
const magnetCount = this.runner.towers.filter(t => t.type === "magnet").length;
const cap = this.runner.level.allowMultiMagnet ? 2 : 1;
if (type === "magnet" && magnetCount >= cap) {
  this._showHudWarning(`Maximum ${cap} aimant${cap > 1 ? 's' : ''} par level`);
  return false;
}
```

**Coin multiplier effectif** :
- Avant : 1 magnet ×2.0 = 100 % bonus coin (spammable). 2 magnets stack pas (max() dans `getCoinMulAt`), mais visuel encourageait le spam.
- Après : 1 magnet ×1.3 = 30 % bonus. 1 par level (sauf W7+ = max 2). Gain économique réduit ×2.0 → ×1.3 = **−35 % de coin bonus**, et cost +30 % = ROI **−51 % au pire**. Magnet devient un choix tactique, plus un auto-pick.

### 3.5 Interest leak penalty (bank +5 %/wave, reset si leak)

**Nouveau système** : entre chaque wave, le bank capitalise +5 % du gold actuel (cap absolu pour éviter snowball). Si une wave a leaké (au moins 1 mob a atteint le château), le bank est reset à 0 au début de la wave suivante.

```js
// LevelRunner.js — nouveaux champs runtime
this._waveBank = 0;            // gold accumulé en bank (visible HUD)
this._waveLeaked = false;      // flag set à true dans onCastleHit()

// Hook dans onCastleHit() :
onCastleHit(damage, pathIdx) {
  this._waveLeaked = true;
  // ... reste de la logique castle damage existante
}

// Hook entre waves (à la fin d'une wave, avant la suivante) :
_onWaveCompleted() {
  if (this._waveLeaked) {
    this._waveBank = 0;  // RESET — punition leak
    emit("crowdef:bank-reset", { reason: "leak" });
  } else {
    const bankGain = Math.floor(this.coins * 0.05);
    this._waveBank += bankGain;
    this.coins += bankGain;
    emit("crowdef:bank-tick", { gain: bankGain, total: this._waveBank });
  }
  this._waveLeaked = false;
}
```

**Cap bank** : aucun cap absolu (la formule 5 % du gold actuel est auto-limitante : si joueur dépense, bank gain diminue). Aligné ETD2 R1-01 §3.4 ("Interest disabled si leak durant la wave en cours → punition économique du sloppy play").

**HUD bank display** (`index.html:1082-1090` ajout) :
```html
<div id="hud-bank-display" class="hud-pill">
  <span class="hud-bank-icon">🏦</span>
  <span class="hud-bank-value">0</span>
</div>
```
Visible top-right à côté du gold. Anim flash vert sur tick gain, anim flash rouge sur reset leak.

### 3.6 Treasure tiles `*` (CELL grammar + spawn rule)

**Convention** :
- `*` = treasure tile, posée par level designer dans le JSON map (pas spawn dynamique).
- Non-walkable (mob ne passe pas dessus, BFS l'ignore comme une grass).
- Non-buildable (joueur ne pose pas de tour dessus).
- Si **non touchée par un mob jusqu'à la fin du level** → octroi `+100¢` à la fin (level cleared bonus).
- Si touchée par un mob (BFS dévie pas, donc impossible normalement, MAIS multi-portail peut amener un mob proche → check distance < 1 cell) → coffre cassé, 0¢.

**Spawn rate par level (level designer guideline)** :
- W1-W3 : 1 treasure par level
- W4-W6 : 2 treasures par level
- W7-W10 : 2-3 treasures par level

**Valeur fixe +100¢** (pas +50-150¢ comme dans `src-v3/CLAUDE.md` E1-D bullet, simplifie). Justification : +100¢ = équivalent ~3-4 kills basiques W5+ après reward nerf, suffisant pour acheter +1 ballista basique, pas écrasant. Ajustable en E1 si playtests montrent < 5 % d'engagement.

**Implementation** (`MapGrid.js:7-22`) :
```js
const CELL = {
  // ... existing
  TREASURE: "*",  // nouveau
};

// Dans le parser map (LevelRunner.js setup) :
if (cell === "*") {
  this.treasures.push({ col, row, x, z, touched: false });
  // visuel : spawn un mesh chest doré à cette position
}

// À la fin du level (victoire) :
const intactTreasures = this.treasures.filter(t => !t.touched).length;
if (intactTreasures > 0) {
  const bonus = intactTreasures * 100;
  this.coins += bonus;
  emit("crowdef:treasure-bonus", { count: intactTreasures, bonus });
}
```

**Detection touch** : dans le tick mob, check distance euclidienne < 0.5 cell vs chaque treasure position. O(treasures × mobs) — négligeable (≤3 treasures × ≤30 mobs = 90 checks/frame).

---

## 4. Critères de succès

Mesurés via `__cd.metrics.lastRun` (à instrumenter en E1 TICKET-E1-J — déjà planned).

| Métrique | Cible | Source mesure |
|---|---|---|
| Ratio kill/spend W5+ (médian) | **< 0.65** | `(startCoins + totalReward) / totalSpent` par run, agrégé sur 20 runs/world W5-W10 |
| % runs avec ratio < 0.65 W5+ | **≥ 80 %** | Pourcentage runs respectant la cible |
| Castle HP final moyen W5+ | 30-70 % | `castleHpEnd / castleHpMax` — ni trop facile, ni trop dur |
| Magnet placement count moyen | **≤ 1.2** sur W4-W10 | `towers.filter(t => t.type==='magnet').length` agrégé |
| Bank reset rate (leak penalty) | 30-50 % des waves W5+ | `bankResets / wavesPlayed` — preuve que le leak penalty mord |
| Treasure bonus collected (W4+) | ≥ 70 % des treasures intact | `intactTreasures / totalTreasures` — confirme spawn rate ok |
| L3 upgrades atteints par run | 0-3 (W1-W3) / 3-6 (W5+) | `towers.filter(t => t.upgradeLevel===3).length` |

**Hard floor** : si après E1 implementation le ratio médian W5+ reste > 0.80 → ajuster `_worldRewardMul` à 7 %/world (au lieu de 5 %).

**Hard ceiling** : si ratio médian W5+ < 0.40 → ajuster startCoins +25 % pour donner respiration (déjà 50¢ uniforme, → 65¢).

---

## 5. Effort estimé

**Total : 5 commits / 6-8 h Sonnet feature-dev**

| Commit | Scope | Effort |
|---|---|---|
| `feat(v3): refonte _upgradeCost L1→L2 ×1.5, L2→L3 ×2.5` | `main.js:1097-1103` | 0.5 h |
| `feat(v3): hike costs L1 mage/mine/crossbow/magnet` | `Tower.js:9-119` (4 tours) | 0.5 h |
| `feat(v3): reward kill mul par world + boss=0×` | `LevelRunner.js:586-590` + helper | 1.5 h |
| `feat(v3): magnet rework + anti-double cap` | `Tower.js:91-101` + handler placement | 2 h |
| `feat(v3): interest bank +5%/wave + leak reset + HUD` | `LevelRunner.js` (new fields) + `index.html` + emit hooks | 2.5 h |
| `feat(v3): treasure tiles * grammar + spawn + bonus` | `MapGrid.js`, `LevelRunner.js`, parser, victory hook | 1 h (grammar+parse) — *décompté de l'effort principal car déjà E1-D planned, on co-livre ici juste le numbers spec* |

Bloqué par : aucun. D1-02 (synergies) et D1-03 (L3 hybride) consomment cette spec mais ne sont pas blockers d'implémentation.

---

## 6. Risques

1. **Ratio over-corrigé W1-W3** : si reward × 1.0 W1 reste, le ratio actuel W1 1.53 → après upgrade ×5 il faudra ~3× plus de gold pour all-L3 ; cumul peut donner ratio 0.4-0.5 en W1 = trop dur pour onboarding. **Mitigation** : floor 0.55 dans `_worldRewardMul`, et startCoins augmentable à 65¢ en W1 si playtests confirment frustration.
2. **Leak penalty trop puni** : si joueur leak 1 mob en wave 4/10, il perd tout le bank cumulé. **Mitigation** : bank +5 % seulement, donc max ~30-50¢ perdus, pas catastrophique. Si playtests jugent dur, soft-reset à 50 % au lieu de 0 %.
3. **Anti-double magnet rage** : joueur frustré de ne pas pouvoir poser 2 magnets en W3 (où il avait l'habitude). **Mitigation** : warning HUD clair + cap visible dans le HUD ("🧲 1/1"). Le coinMul ×2 → ×1.3 + cost 100 → 130 fait déjà mal, le cap est la cerise.
4. **Treasure spawn dans level pas designed pour** : si level designer oublie de poser `*`, certains levels W4+ n'auront pas de treasure → écart entre worlds. **Mitigation** : `MapValidator.js` warn (pas error) si W4+ level n'a pas de treasure. Reste corrigeable level par level en D2.
5. **Reward W10 = ×0.55 = trop sec pour endless** : endless mode (`endless.js`) utilise world=20+. `Math.max(0.5)` floor protège, mais endless ratio peut chuter < 0.4. **Mitigation** : si `level.endless`, override `_worldRewardMul` retourne `Math.max(0.7, …)`.

---

## 7. Test plan

### 7.1 Tests unitaires (auto, `npm run test:crowdef`)

À ajouter 5 nouveaux tests :

- `economy_upgrade_cost_l2_is_1_5x_base()` — pose archer cost 30, upgrade L1→L2, assert cost demandé = 45.
- `economy_upgrade_cost_l3_is_2_5x_base()` — upgrade L2→L3 sur archer 30, assert cost = 75. Total cumulé L3 = 150 = 5×base ✓.
- `economy_boss_reward_is_zero()` — spawn boss `isBoss: true`, kill, assert `runner.coins` unchanged.
- `economy_world_reward_mul_w5()` — set world=5, mob basique reward 10, assert effective coin gain = 10 × 0.85 × 0.80 = 6.8 → round 7.
- `economy_magnet_cap_1_per_level()` — pose magnet, tenter 2nd magnet sans `allowMultiMagnet`, assert placement refusé + count = 1.

Maintenir baseline 23/25 (+5 = 28/30 attendu) → 2 tests pré-existants en fail (`towers built >= 1`, `endless has 30 waves`) inchangés.

### 7.2 Tests live Chrome MCP (manuel, validation E1)

Scenario 1 — **Ratio W5 cible** :
- `__cd.unlockAll()` puis `__cd.goto("world5-1")`.
- Jouer 1 run "no-magnet, no-skip" jusqu'à victoire ou défaite.
- Vérifier `__cd.metrics.lastRun.killSpendRatio` ∈ [0.50, 0.75].

Scenario 2 — **Magnet anti-double** :
- `__cd.goto("world4-1")` (level mono-magnet attendu).
- Poser 1 magnet, tenter poser 2nd → HUD warning "Maximum 1 aimant".
- `__cd.goto("world7-8")` (level 4P, `allowMultiMagnet: true`).
- Poser 2 magnets OK, tenter 3rd → warning "Maximum 2".

Scenario 3 — **Leak penalty** :
- `__cd.goto("world3-5")`, ne pas poser de tour, laisser wave 1 leak.
- Vérifier `__cd.runner._waveBank === 0` après wave 1 ET emit `crowdef:bank-reset`.
- Wave 2 clean (poser ballista cost 100), confirmer bank +5 % du current gold après wave 2.

Scenario 4 — **Boss reward 0×** :
- `__cd.goto("world5-8")` (boss arena).
- Tuer boss, vérifier `runner.coins` ne bouge pas sur ce kill (XP/gems toujours octroyés).

Scenario 5 — **Treasure tile intacte** :
- Charger un level avec 2 `*` (à ajouter dans world4-1 ou test level).
- Compléter sans laisser passer un mob.
- Vérifier popup victory "+200¢ treasures intactes" et `runner.coins += 200`.

### 7.3 Métriques agrégées (post-E1)

Run autopilot (auto-qa-runner) sur 80 levels × 3 seeds = 240 runs. Cibles :
- Ratio kill/spend médian W5+ < 0.65 sur ≥ 80 % des runs.
- Castle HP final ∈ [30 %, 70 %] sur ≥ 60 % des runs.
- Aucun crash, aucun stuck (run termine en < 10 min).

Rapport agrégé déposé dans `.claude/qa/reports/D1-01-economy-validation.md` par auto-qa-runner.
