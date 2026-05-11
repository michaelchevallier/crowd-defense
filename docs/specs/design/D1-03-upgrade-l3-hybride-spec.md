# D1-03 — Upgrade L3 hybride : paywall + choix binaire

**Persona** : `game-designer`
**Date** : 2026-05-11
**Sprint** : D1 (design coeur)
**Plan ref** : `/Users/mike/.claude/plans/rustling-nibbling-wirth.md` (sprint D1 — refonte stratégique 8 semaines)
**Recherche source** : `.claude/research/R2-04-synergies-benchmark.md` (5 jeux étudiés : BTD6, ETD2, GCFW, Kingdom Rush, Defense Grid 2)
**Décisions Mike actées** (interview 2026-05-11) :
- Ratio L3/L1 = ×5 total (×1.5 puis ×2.5 supp.).
- Différenciation 2 specs = stat-by-stat séparée (DPS pur vs utility/CC).
- Réversibilité = pelle uniquement (refund 70 % cumulé — vs 80 % actuel — voir Risques).
- Synergies passives = +50 % → +25 % impact.

---

## Contexte

L'audit Milan CD V3 (R2-06) et le benchmark industrie (R2-04) convergent : la progression actuelle est **trop linéaire** (L1→L2→L3 chacun ×1.5 du précédent, total ×2.25). Pas de paywall, pas de choix, pas de spécialisation. Le joueur upgrade tout par réflexe, le mid-game devient un blur de tours toutes maxées identiques.

**Modèle cible** copié sur Kingdom Rush (le plus directement transposable selon R2-04 §8.4) :
1. **L1 → L2 linéaire** forcé, sans choix.
2. **L2 → L3 paywall** (×2.5 supplémentaire = ×5 base) avec **choix binaire** entre 2 specs thématiquement distinctes.
3. **Différenciation stat-by-stat** : une branche pousse DPS pur (single-target killer), l'autre pousse utility/CC (AOE, slow, knockback). Le joueur ne peut pas spreadsheet-compare, doit choisir au contexte.
4. **Irréversible sauf pelle** : pas de re-spec gratuit. Vendre = -30 % du gold cumulé.

Le scope se limite à **4 tours signature** : `archer`, `mage`, `ballista`, `cannon`. Les 9 autres tours (`tank`, `mine`, `fan`, `frost`, `crossbow`, `portal`, `magnet`, `skyguard`, `acid`) gardent un L3 linéaire mono-branche. Justification : (a) coût dev contenu (8 branches vs 26 si toutes les tours), (b) ces 4 tours = les **role-fillers du joueur lambda** (anti-armor, anti-flying, mass control, AOE), donc le choix porte sur les rôles que le joueur exerce vraiment.

---

## Fichiers impactés

| Fichier | Lignes | Nature du diff |
|---|---|---|
| `src-v3/entities/Tower.js` | `9-119` | Ajouter `signature: true` + `branches: { a, b }` sur 4 tours signature ; ajouter `cost` L1 hike via D1-01 (out-of-scope ici, signalé). |
| `src-v3/entities/Tower.js` | `336-368` | Étendre `upgradeTo(level, branch = null)` → branche obligatoire si `level === 3 && cfg.signature === true`. |
| `src-v3/main.js` | `1097-1118` | Réécrire `_upgradeCost(tower)` : `lvl 1 → 1.5×base`, `lvl 2 → 2.5×base supp.`. Branche `_previewNextLevel(type, nextLvl, branch?)`. |
| `src-v3/main.js` | `1132-1185` | `refreshRadialButtons` : si L2 et tour signature → 2 boutons côte à côte (`branch=a`/`branch=b`) au lieu d'un bouton `upgrade` simple. |
| `src-v3/main.js` | `1187-1247` | Listener `click` : action `upgrade-a` / `upgrade-b` passe `branch` à `Tower.upgradeTo`. |
| `src-v3/entities/BuildPoint.js` | `126-134` | Changer `Math.round(this.totalInvested * 0.8)` → `0.7`. **Breaking change UX** — voir Risques §R5. |
| `src-v3/main.js` | `1147` | Idem (0.8 → 0.7). |
| `src-v3/systems/Synergies.js` | partout | Réduire impacts +50 % → +25 % (`dmgMul: 1.5 → 1.25`, `pierceBonus`, `multiShotBonus`, `buffMul`, etc.). Voir tableau §"Synergies réduites". |
| `index.html` | bloc `#radial-menu` | Ajouter slots HTML `[data-radial="upgrade-a"]` / `[data-radial="upgrade-b"]` avec layout flex côte à côte. |

**Hors scope D1-03** (renvoyé D1-01) : ajustement `cost` L1 sur tours fortes (crossbow 140→160, mage 70→85, mine 60→80) — signalé pour cohérence éco.

---

## Pseudo-code Tower.upgrade(level, branch)

```js
// src-v3/entities/Tower.js (extension lignes 335-368)

upgradeTo(level, branch = null) {
  if (level <= this.upgradeLevel) return;
  this.upgradeLevel = level;
  const base = this.cfg;
  const isSignature = base.signature === true;

  if (level === 2) {
    // identique actuel — pas de choix
    this.damage = base.damage * TOWER_DAMAGE_MUL * 1.5;
    this.range = base.range * 1.2;
    if (this.type === "skyguard") {
      this.pierce = 3;
      this.fireRateMs = 500;
    }
  } else if (level === 3) {
    // Branche obligatoire pour tours signature
    if (isSignature && (branch !== "a" && branch !== "b")) {
      // garde-fou : on doit toujours recevoir une branch
      console.warn(`[Tower] L3 signature ${this.type} sans branche — fallback A`);
      branch = "a";
    }
    this.upgradeBranch = isSignature ? branch : null;

    // Stats de base L3 (même pour toutes)
    this.range = base.range * 1.4;

    if (this.type === "archer") {
      if (branch === "a") {
        // SNIPER : DPS pur single-target
        this.damage = base.damage * TOWER_DAMAGE_MUL * 3.0;  // ×3 dmg (justifié : sniper = headshot one-shot fantasy)
        this.fireRateMs = Math.round(base.fireRateMs * 0.5); // ÷2 fire rate (cadence redouble — flèche puissante mais lente)
      } else {
        // PLUIE D'ARCHER : utility AOE
        this.damage = base.damage * TOWER_DAMAGE_MUL * 1.2;  // +20% dmg seulement
        this.aoe = 3.0;                                      // AOE 3 cells (souffle visuel)
        this.multiShot = 2;                                  // 2 flèches simultanées
      }
    } else if (this.type === "mage") {
      if (branch === "a") {
        // ARCANE : DPS soutenu + slow
        this.damage = base.damage * TOWER_DAMAGE_MUL * 2.5;
        this._slowOnHit = { mul: 0.7, durMs: 1500 };         // slow 30% pendant 1.5s
      } else {
        // BOULE DE FEU : AOE + burn DOT
        this.damage = base.damage * TOWER_DAMAGE_MUL * 1.8;
        this.aoe = 4.0;                                      // AOE 4 cells (plus large)
        this._burnDot = { dps: base.damage * 0.8, durMs: 3000 }; // burn 3s, 80% du dmg base par sec
      }
    } else if (this.type === "ballista") {
      if (branch === "a") {
        // PIERCE INFINI : single-line piercer
        this.damage = base.damage * TOWER_DAMAGE_MUL * 2.5;
        this.pierce = 99;                                    // perce toute la file (cap pratique 99)
        this._armorBreakOnHit = { dmgTakenMul: 1.5, durMs: 10000 }; // conservé du L3 actuel
      } else {
        // EXPLOSION : AOE knockback
        this.damage = base.damage * TOWER_DAMAGE_MUL * 2.0;
        this.aoe = 5.0;                                      // AOE 5 cells
        this._knockbackOnHit = 0.008;                        // léger knockback (justifié : reuse Synergies pattern)
      }
    } else if (this.type === "cannon") {
      if (branch === "a") {
        // MEGA SHELL : single-shot ravageur + slow
        this.damage = base.damage * TOWER_DAMAGE_MUL * 3.0;
        this.aoe = 3.5;                                      // AOE réduit (concentre l'impact)
        this._slowOnHit = { mul: 0.5, durMs: 2000 };
      } else {
        // SHOTGUN : multi-shot spread
        this.damage = base.damage * TOWER_DAMAGE_MUL * 1.5;
        this.multiShot = 5;                                  // 5 projectiles spread
        this.aoe = 2.0;                                      // AOE petit par projectile
      }
    } else {
      // Tours non-signature : L3 linéaire (logique existante préservée)
      this.damage = base.damage * TOWER_DAMAGE_MUL * 2.5;
      if (this.type === "tank") { this.pierce = 2; this._tankBlockAura = { mul: 0.75, range: 5 }; }
      else if (this.type === "skyguard") { this.aoe = 2.5; this.fireRateMs = 450; this.pierce = 0; }
      else if (this.type === "crossbow") this._finalExplosion = { aoe: 2, dmg: base.damage * TOWER_DAMAGE_MUL * 2.5 };
      else if (this.type === "mine") this._chainExplosion = true;
      else if (this.type === "fan") this._knockbackOnSlow = { force: 3.5, durMs: 250 };
      else if (this.type === "frost") this.slowDurationMs = 8000;
      else if (this.type === "portal") this._auraBigRange = 9.5;
      else if (this.type === "magnet") this._coinAura = { coinMul: 1.6, range: 9 }; // hard-nerfé D1-01 (était 3.5)
    }
  }
  // Asset key : signature L3 utilise variantes _l3a / _l3b si dispo
  let assetKey = base.asset;
  if (level === 2) assetKey = base.asset + "_l2";
  else if (level === 3) {
    if (isSignature && branch) {
      assetKey = base.asset + `_l3${branch}`; // _l3a, _l3b
    } else {
      assetKey = base.asset + "_l3";
    }
  }
  if (this.type === "mage" && level === 3 && !isSignature) assetKey = base.asset + "_l2";

  this._disposeModel();
  this._loadModel(assetKey, (base.sizeMultiplier ?? 1.0) * (LEVEL_SCALE[level] ?? 1.0));
  this._buildRangeRing();
  this._drawTierPips(level);
  if (level === 3) this._tintGold();
}
```

**Justification magic numbers** :
- `×3.0` archer Sniper, `×3.0` cannon Mega : c'est le "wow moment" — un tir = un kill sur mob standard. Calibré pour boss W7+ mais one-shot mob basique.
- `÷2 fire rate` Sniper : sustain DPS = `3.0 / 2.0 = 1.5×` vs L2 (×1.5). Compense ×3 dmg.
- `×1.2` Pluie d'archer : faible dmg ×, mais multiShot 2 + aoe 3 = DPS effectif `1.2 × 2 × ~2 cibles = ~4.8×`, comparable à Sniper.
- `aoe 4.0` Boule de feu, `aoe 5.0` Explosion ballista : `4 = 1 lane standard`, `5 = lane + frontière`. Volontairement asymétrique pour différencier les 4 tours.
- `multiShot 5` Shotgun : 5 = max raisonnable visuel (au-delà ça surcharge la scène). Spread cone ~30°.
- `slow mul 0.7 / 0.5` : 30% slow soft (mage), 50% slow hard (cannon). Cohérent avec frost (×0.5 baseline).
- `burn DOT 3s, 80% dmg base/s` : sur mage L3-B, total burn = `0.8 × 3 = 2.4× base damage` étalé. Récompense les fights longs (boss).

**Asset fallback** : si `_l3a` / `_l3b` n'existent pas, fallback sur `_l3` puis `_l2`. Pas bloquant pour E1 (assets E2).

---

## Tableau 8 branches L3 signature

| Tour | Branche A (DPS pur) | Branche B (utility/CC) |
|---|---|---|
| **Archer** | **Sniper** : ×3 dmg, fire-rate ÷2, single-target killer | **Pluie d'archer** : multiShot 2, AOE 3, +20 % dmg |
| **Mage** | **Arcane** : ×2.5 dmg, slow on hit 30 %/1.5s | **Boule de feu** : AOE 4, +80 % dmg, burn DOT 3s |
| **Ballista** | **Pierce ∞** : ×2.5 dmg, pierce 99, armor break 10s | **Explosion** : AOE 5, ×2 dmg, knockback |
| **Cannon** | **Mega shell** : ×3 dmg, AOE 3.5, slow 50 %/2s | **Shotgun** : multiShot 5, ×1.5 dmg, AOE 2 |

**Lecture grille** :
- Branche A = "j'ai un boss ou un mob tank devant moi" → focus fire, low spread, slow optionnel.
- Branche B = "j'ai une vague qui arrive, plein de mobs faibles" → AOE, multiShot, spread.
- Aucune branche n'est strictement dominante : Sniper bat un Plumber tank L8 en 1 tir, Pluie d'archer wipe 6 visiteurs faibles en 1 tir. Le joueur **doit lire la wave** avant de cliquer.

**Synergies réduites** (Synergies.js — +50 % → +25 %) :

| Synergie | Avant | Après |
|---|---|---|
| `archer + frost` (multiShot bonus) | `+1` | `+1` (déjà discret, conservé) |
| `mage + skyguard` (flyerDmgBonus) | `×1.5` | `×1.25` |
| `cannon + frost` (slowOnHit) | `×0.5 / 2000ms` | `×0.7 / 2000ms` |
| `crossbow + mage` (propagateAoE dmg) | `1.73` base | `1.40` base |
| `ballista + portal` (pierceMega) | `pierce +99` | `pierce +5` (déclasser : suffisant mais pas infini) |
| `magnet (passive coinMul)` | `×2.0` | `×1.3` (déjà hard-nerf D1-01) |
| `portal aura dmgMul` | `×1.5` | `×1.25` |
| `portal aura pierceBonus` | `+1` | `+1` (discret, conservé) |
| `tank L3 blockAura` | `mul: 0.75` | `mul: 0.85` |

---

## UI specs — radial menu

**État A (L1)** : 1 bouton `upgrade` → `↑ X¢` (inchangé).
**État B (L2 standard, tours non-signature)** : 1 bouton `upgrade` → `↑ X¢` (inchangé).
**État C (L2 signature)** : **2 boutons côte à côte** :

```
┌─────────────────────────────────────────┐
│  ╔═══════════╗   ╔═══════════╗          │
│  ║  🎯 Sniper ║   ║ 🌧️ Pluie ║          │
│  ║   X¢      ║   ║   X¢      ║          │
│  ║ +DPS pur  ║   ║ +AOE crowd║          │
│  ╚═══════════╝   ╚═══════════╝          │
│  [💰 Vendre +Y¢]                        │
└─────────────────────────────────────────┘
```

**Spécifications visuelles** :
- Bouton A (DPS) : icône + label gauche, couleur or `#ffd23f` (style Sniper/single-target).
- Bouton B (utility) : icône + label droite, couleur violet `#a060ff` (style AOE/crowd).
- Label sous le coût : 2-3 mots max (`+DPS pur`, `+AOE crowd`, `+Pierce ∞`, etc.) — pas de description longue.
- Tooltip hover sur chaque bouton (hover ≥ 500ms) : description longue + stats projetées (dmg, AOE, fire rate post-upgrade).
- **Confirmation visuelle** : clic = animation flash + sound `coins_spend.wav` + 2.5s anim upgrade (reuse `_upgradePending` existant) + warning toast 1.8s "**Choix irréversible sauf pelle**".
- Bouton désactivé (greyed) si `runner.coins < cost`. Si les 2 sont accessibles, joueur peut hover/comparer librement.
- Une fois upgradé en L3 : bouton `upgrade` disparaît, seul `sell` reste. Plus de "switch" possible.

**Mobile** : layout passe en stack vertical (2 boutons l'un sur l'autre) si `window.innerWidth < 480`.

**Accessibilité** : raccourcis clavier `Y` / `U` pour upgrade-a / upgrade-b quand radial ouvert. Aligne sur convention `T` (tower preview) déjà en place.

---

## Critères de succès

1. **Tower.upgradeTo(2)** = identique avant/après (régression zéro sur L1→L2).
2. **Tower.upgradeTo(3, "a")** sur archer → `damage = 1.38 × 1.6 × 3.0 = 6.62`, `fireRateMs = 350` (700 ÷ 2).
3. **Tower.upgradeTo(3, "b")** sur archer → `damage = 1.38 × 1.6 × 1.2 = 2.65`, `multiShot = 2`, `aoe = 3.0`.
4. **Tower.upgradeTo(3)** sans branche sur tour signature → warning console + fallback `branch = "a"`.
5. **Tower.upgradeTo(3)** sur tour non-signature (tank, mine, etc.) → pas de branche, comportement actuel préservé.
6. **`_upgradeCost(tower)`** retourne :
   - L1 (vers L2) : `round(base × 1.5)` (ex archer 30 → 45).
   - L2 (vers L3) : `round(base × 2.5)` (ex archer 30 → 75). **Total cumulé L1+L2+L3 = base + 1.5×base + 2.5×base = 5.0×base.** Validé.
7. **Radial menu L2 sur tour signature** : affiche 2 boutons distincts cliquables.
8. **Radial menu L2 sur tour non-signature** : affiche 1 bouton (inchangé).
9. **Synergies actives** post-réduction : `Synergies.resolve()` retourne `dmgMul ≤ 1.25` pour tous les buffs passifs (vérifier via console DevTools en hover hero).
10. **Vente L3 signature** : refund = `round(totalInvested × 0.7)`. Pour archer L3-A : invested = 30 + 45 + 75 = 150 → refund = 105.
11. **`UndoSell` post-vente** : restore correctement `upgradeBranch` (pas juste `upgradeLevel`) — sinon la tour ressuscitée n'a pas la bonne spec.

---

## Effort estimé

- **Tower.upgradeTo extension** : 2 commits Sonnet (1 refactor sig, 1 ajout branches).
- **main.js `_upgradeCost` + radial menu** : 2 commits Sonnet (logic + UI HTML/CSS).
- **Synergies.js nerf +25 %** : 1 commit Sonnet (tableau values).
- **BuildPoint refund 0.8 → 0.7 + main.js cohérence** : 1 commit Sonnet (1-line diff).
- **index.html radial layout** : 1 commit Sonnet (HTML + CSS).
- **Tests unitaires** : 1 commit Sonnet (test:crowdef extension, ~4 nouveaux tests).

**Total : 8 commits, ~1.5 jour Sonnet** (en // si bien découpé en 2 tickets : TOWER+UPGRADE-LOGIC vs UI+SYNERGIES).

**Bloque** : E1 TICKET-E1-G (implémentation L3 hybride). Cette spec est le brief de E1-G.
**Bloqué par** : aucun. Mike a validé toutes les décisions arbitrées.

---

## Risques

**R1. Imbalance branche A vs B** : si une branche est statistiquement supérieure à l'autre, le choix devient illusoire. Mitigation : DPS effectif calibré dans le pseudo-code (Sniper sustain DPS = ×1.5, Pluie effective DPS ~×4.8 sur crowd ≠ ×1.5 sur boss). Validation : 3 niveaux QA scenario (boss-focus W7-3, crowd-focus W8-5, mixed W9-7) post-E1. **Si déséquilibré : tuning numérique simple, pas de refactor.**

**R2. Multi-shot 5 (cannon Shotgun) perf** : 5 projectiles par tir × N cannons → potentiellement >100 projectiles simultanés en lategame. Mitigation : cannon `fireRateMs = 2000` → 5 proj × 0.5 hz = 2.5 proj/s par tour, ×10 cannons = 25 proj/s, négligeable vs particle cap 500 (V3 baseline). **À monitorer en QA si Mike spam cannons.**

**R3. Tooltip hover ≥ 500ms** : utilisateurs mobiles n'ont pas hover. Mitigation : sur tactile, tap court = preview tooltip 2s, tap long (≥800ms) = confirm upgrade. Convention déjà en place sur tower selector existant.

**R4. Asset L3-A/L3-B manquants** : on prévoit 2 visuels différents par tour signature (8 nouveaux assets). Si non livré E1, fallback sur `_l3` existant avec tint or différent (branche A = doré pur, branche B = doré-violet). Voir RAID assets E2.

**R5. Refund 0.7 vs 0.8 — breaking change UX** : actuel 80 %, Mike décide 70 %. **Régression perçue** par joueur habitué V3. Mitigation : (a) annoncer dans patch notes E2, (b) bouton sell affiche `+Y¢` (montant exact) donc le joueur voit le delta. Justification design : 70 % aligne sur Kingdom Rush + force engagement (vendre = vrai coût, pas free re-spec). **Décision Mike confirmée 2026-05-11**.

**R6. Branche choisie en panic-click** : si joueur clic accidentel sur mauvaise branche, frustration. Mitigation : **confirmation 2-clics** sur L3 signature uniquement. Premier clic = bouton se charge en couleur pleine + tooltip "Cliquer encore pour confirmer (irréversible)". Deuxième clic dans 3s = exécute upgrade. Reuse pattern `_upgradePending` 2.5s anim existant comme grace period.

**R7. Tours non-signature L3 dévalorisées** : si seules les 4 signature ont du choix, joueur perçoit tank/frost/portal/etc. comme "tours mineures". Mitigation : c'est intentionnel — leur rôle est différent (support, utility passive, niche). Communication via tooltip `info` au radial menu : "Tank L3 = bloqueur de file, pas DPS — combine avec archer Sniper derrière".

---

## Test plan

### Tests unitaires (test:crowdef extension, +4 tests)

```js
// tests/crowdef/upgrade-l3.test.js (nouveau)
test("Tower L3-A archer = Sniper stats", () => {
  const t = new Tower(scene, {x:0,y:0,z:0}, "archer");
  t.upgradeTo(2);
  t.upgradeTo(3, "a");
  expect(t.damage).toBeCloseTo(1.38 * 1.6 * 3.0, 2);
  expect(t.fireRateMs).toBe(350);
  expect(t.upgradeBranch).toBe("a");
});

test("Tower L3-B archer = Pluie stats", () => {
  const t = new Tower(scene, {x:0,y:0,z:0}, "archer");
  t.upgradeTo(2);
  t.upgradeTo(3, "b");
  expect(t.multiShot).toBe(2);
  expect(t.aoe).toBe(3.0);
  expect(t.upgradeBranch).toBe("b");
});

test("Tower L3 tank (non-signature) = pas de branche", () => {
  const t = new Tower(scene, {x:0,y:0,z:0}, "tank");
  t.upgradeTo(2);
  t.upgradeTo(3);  // pas de branch arg
  expect(t.upgradeBranch).toBeNull();
  expect(t.pierce).toBe(2);
});

test("_upgradeCost cumul L3 = 5× base", () => {
  const cfg = { cost: 30 };
  const t = { cfg, upgradeLevel: 1 };
  expect(_upgradeCost(t)).toBe(45);  // 30 × 1.5
  t.upgradeLevel = 2;
  expect(_upgradeCost(t)).toBe(75);  // 30 × 2.5
  // cumul = 30 + 45 + 75 = 150 = 30 × 5
});
```

**Baseline préservé** : 23/25 actuel + 4 nouveaux = 27/29. Pas de régression sur les 23 passants.

### Live test scenarios (Chrome MCP, post-E1-G)

1. **Scénario "Boss W7-3"** : forcer spawn boss Knight at W7-3. Build 1 archer L3-A (Sniper). Vérifier kill en ≤ 3 tirs. Loguer DPS calc via `__cd.runner.towers[0].kills / durationSec`.
2. **Scénario "Crowd W8-5"** : spawn wave 12-mobs basique. Build 1 archer L3-B (Pluie). Vérifier kill wave en ≤ 4 secondes. Loguer DPS effectif sur crowd.
3. **Scénario "Comparaison équilibrage"** : run 2 levels W6-1 identiques, run A = full archer L3-A, run B = full archer L3-B. Comparer castle HP final, kills, duration. Objectif : delta absolu ≤ 15 %.
4. **Scénario "Vente avec refund"** : build archer + L2 + L3-A, total invest 150¢. Cliquer sell, vérifier refund = 105 (70 %).
5. **Scénario "Choix irréversible"** : upgrade archer L3-A, vérifier radial menu n'expose plus L3-B. Reload page, vérifier persistence (savegame, si applicable au level current — sinon nope, restart wipe state).
6. **Scénario "Synergies +25 %"** : place archer + frost adjacents. Hover archer → tooltip affiche `Synergie active : ×1.25 dmg` (vs ×1.5 avant). Place portal + ballista → vérifier `pierce = 5` (vs 99 avant).

### Auto-QA pattern (sprint-gate D1)

```js
// .claude/qa/scenarios/sprint-D1/upgrade-l3-binary-choice.mjs
export default {
  id: "upgrade-l3-binary-choice",
  description: "Vérifie que radial L2 signature expose 2 boutons distincts",
  setup: async (page) => {
    await page.evaluate(() => window.__cd.unlockAll());
    await page.evaluate(() => window.__cd.goto("world1-1"));
    await page.evaluate(() => {
      const bp = window.__cd.runner._buildPoints[0];
      window.__cd.runner._placeTower(bp, "archer");
      bp.tower.upgradeTo(2);
    });
  },
  assertions: [
    { type: "hard", expr: "document.querySelector('[data-radial=\"upgrade-a\"]') !== null" },
    { type: "hard", expr: "document.querySelector('[data-radial=\"upgrade-b\"]') !== null" },
    { type: "hard", expr: "document.querySelector('[data-radial=\"upgrade-a\"]').textContent.includes('Sniper')" },
    { type: "soft", judge: "Les 2 boutons L3-A/B sont-ils visuellement distincts et lisibles ?" },
    { type: "soft", judge: "Le label 'Choix irréversible' est-il bien visible avant clic ?" }
  ]
};
```

**Critère sprint-gate D1** : `upgrade-l3-binary-choice` doit retourner 4 hard PASS + 2 soft GREEN. Sinon sprint D1 bloqué.

---

## Notes implémentation pour Sonnet feature-dev

- **Ne pas casser** la signature publique `Tower.upgradeTo(level)`. Le 2e arg `branch` est optionnel et ne fail que sur tour signature L3.
- **Conserver** le pattern `_upgradePending` 2.5s pour anim upgrade (reuse loop existant).
- **Index assets** : si `_l3a` non trouvé → tomber sur `_l3` (déjà géré par AssetLoader fallback).
- **Synergies.js réduction** : modifier les valeurs **dans le fichier**, pas via env variable. Magic numbers OK ici (constantes de design).
- **Commit pattern** : `feat(v3): L3 hybride choix binaire archer/mage/ballista/cannon`, footer Co-Authored-By Claude Opus 4.7.
