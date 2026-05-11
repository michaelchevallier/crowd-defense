# D1-02 — Spec UX Pacing joueur-driven (Bouton "Lancer la vague")

**Sprint** : D1 — Design cœur
**Date** : 2026-05-11
**Auteur** : `ux-designer`
**Scope** : Wireframe + specs implémentables du bouton manuel de lancement de vague + skip bonus + désactivation auto-start.
**Hard rule** : aucune écriture de code (lecture seule), ASCII mocks, animations ≤ 400 ms, touch target ≥ 44 px.

---

## Contexte

Implémente les arbitrages Mike du 2026-05-11 (R1-02 benchmark) :

- **Auto-start** : OFF strict. Pas de timeout fail-safe. Liberté infinie entre waves (pattern BTD6).
- **Skip bonus** : fenêtre 5 s dès la fin de la wave précédente (event `crowdef:wave-clear`). Clic dans la fenêtre → **+30¢ flat** (toast HUD). Clic hors fenêtre → 0¢.
- **Streak** : N waves consécutives skippées dans la fenêtre → bonus cumulatif `+5% × N` sur le reward kill de la wave qui démarre (cap +25% / 5 skips). Reset si une fenêtre 5 s expire sans clic.
- **Cap temporel max** : aucun. Le joueur peut attendre infiniment.

---

## Fichiers impactés (lecture seule, référence pour le ticket E1-E)

| Fichier | Lignes | Rôle |
|---------|--------|------|
| `src-v3/index.html` | 65-67, 1083-1089 | `#hud-top-buttons` (template boutons HUD) |
| `src-v3/index.html` | 503-522, 1101-1103 | wave banner / countdown référence styles |
| `src-v3/index.html` | 535-550 | `.toast` template existant |
| `src-v3/index.html` | 692-720, 742-750 | media `pointer: coarse`, `#mobile-speed-control` (bottom-left) |
| `src-v3/index.html` | 1113-1122 | `#speed-control` + `.speed-btn` |
| `src-v3/index.html` | 1148 | `#joystick` 96×96 px à 18px bottom-left mobile |
| `src-v3/main.js` | 828-921 | `keydown` listener (point d'ajout `N`) |
| `src-v3/main.js` | ~810-826 | `_isAnyOverlayOpen()` (à réutiliser pour bloquer `N`) |
| `src-v3/systems/LevelRunner.js` | 103, 499-513 | wave-break auto-advance (à remplacer par trigger manuel) |
| `src-v3/systems/LevelRunner.js` | 586-590 | reward calc (`_rewardMulSkip` à ajouter) |

Nouveaux IDs/classes :
- `#wave-launch-btn`, `#wave-launch-pill`, `#wave-launch-preview`, `#wave-launch-streak-badge`, `.toast-skip-bonus`.

---

## Wireframe ASCII

### Desktop (≥ 901 px) — état "skip window active, 3s restantes, streak +10%"

```
+-----------------------------------------------------------------------+
| coins:100 gems:3                          [shop][map][set][enc][mute] |
| [Heros XP-----] [Chateau===========]                                  |
|                                                                       |
|                                                                       |
|                        ZONE JEU 3D                                    |
|                                                                       |
|                                                                       |
|                ┌──────────────────────────────────┐                   |
|                │  +30¢ skip  ·  3.0s  ·  +10%     │  pill timer       |
|                └──────────────────────────────────┘                   |
|                                                                       |
|                ┌────────────────────────────────┐ ┌──┐                |
|                │  ⚔️  Lancer la vague (N)        │ │×2│ streak badge  |
|                │       Vague 4 / 12              │ └──┘ (green)       |
|                └────────────────────────────────┘  220×56 + pulse     |
|                                                                       |
|  ×1 ×2 ×3                                                             |
|  [archer][mage][balli][cannon][magnet][...]  toolbar bottom-center    |
+-----------------------------------------------------------------------+
```

Hover desktop (tooltip preview composition, fade-in 180 ms après délai 300 ms) :

```
                ┌──────────────────────────────────┐
                │  📜 PROCHAINE VAGUE — Wave 4      │
                │  ────────────────────────────     │
                │  🧟  Grunt        ×8              │
                │  🛡️  Shield       ×3              │
                │  💀  Banshee      ×1              │
                │  ────────────────────────────     │
                │  Reward base : 120¢               │
                │  Skip bonus  : +30¢ (3s)          │
                │  Streak      : +10% reward        │
                └──────────────┬───────────────────┘
                               ▼
                ┌────────────────────────────────┐
                │  ⚔️  Lancer la vague (N)        │
                └────────────────────────────────┘
```

États additionnels :

```
disabled (wave en cours, masqué OU grisé selon stratégie) :
  ┌────────────────────────────────┐
  │  ⏳ Vague en cours...           │  opacity 0.5  cursor:not-allowed
  │       Wave 3 / 12               │  pas de pulse  filter:grayscale
  └────────────────────────────────┘

active (clic feedback 200 ms) :
  ┌────────────────────────────────┐  scale 0.96
  │  ⚔️  Lancer la vague (N)        │  shadow ↓ (translate y+2)
  └────────────────────────────────┘
```

### Mobile (`pointer: coarse` OU ≤ 900 px) — bouton circulaire 80×80 bottom-right

```
+--------------------------------+
| ☰Menu       coins:100  gems:3  |
| [Heros--] [Chateau========]    |
|                                |
|                                |
|        ZONE JEU 3D             |
|                                |
|                                |
|              ┌────────────────┐|
|              │+30¢·3.0s·+10%  │| pill au-dessus
|              └────────────────┘|
|  ┌─────╗                   ┌──┐|
|  │ ╲│╱ ║                   │⚔️ │| 80×80 circle
|  │  +  ║                   │N4 │| bottom: 96px
|  │ ╱│╲ ║                   └──┘| right: 18px
|  └─────╝                       |
|  joystick                      |
|  96×96                         |
|  ×1                            |
|  ×2  ←mobile-speed-control     |
|  ×3                            |
|  [archer][mage][...]           |  scroll horizontal
+--------------------------------+
```

Anti-conflit spatial mobile (iPhone SE 320 px → 4K) :
- `#joystick` 96×96 left:18 bottom:18 ↔ `#wave-launch-btn` 80×80 right:18 bottom:96 → séparation horizontale ≥ 200 px sur tout viewport ≥ 320 px.
- `#mobile-speed-control` 8px left, bottom: ~90px → toujours à gauche, jamais sous le bouton wave.
- `#tower-toolbar` mobile : bottom 8px, scroll horizontal, hauteur 60px → plafond ~70px, libère la zone bouton wave (≥ 96px du sol).

---

## Specs CSS (extraits implémentables)

### Desktop — `#wave-launch-btn` base

```css
#wave-launch-btn {
  position: fixed; left: 50%; bottom: 110px; transform: translateX(-50%);
  width: 220px; height: 56px;
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  background: linear-gradient(180deg, #ffd23f, #f0a020);
  border: 3px solid #c88a00; border-radius: 14px;
  color: #1a1a1a; font-family: "Bangers","Fredoka",sans-serif;
  font-size: 18px; letter-spacing: 1px;
  cursor: pointer; user-select: none; pointer-events: auto; z-index: 23;
  box-shadow: 0 6px 0 rgba(0,0,0,0.45), 0 0 18px rgba(255,210,63,0.45);
  transition: transform 0.1s ease, box-shadow 0.15s ease, opacity 0.2s ease;
  animation: wave-launch-pulse 1.6s ease-in-out infinite;
}
#wave-launch-btn .sub { font-size: 12px; opacity: 0.85; margin-top: 2px; }
#wave-launch-btn:hover  { transform: translateX(-50%) translateY(-2px); box-shadow: 0 8px 0 #000a, 0 0 24px #ffd23fA0; }
#wave-launch-btn:active { transform: translateX(-50%) translateY(2px) scale(0.96); box-shadow: 0 2px 0 #000a; }
#wave-launch-btn.disabled { opacity: 0.5; cursor: not-allowed; animation: none; filter: grayscale(0.6); }
#wave-launch-btn.skip-window { border-color: #ffe273; box-shadow: 0 6px 0 #000a, 0 0 28px #ffe273E0; }
#wave-launch-btn.skip-window::after {
  content: ""; position: absolute; inset: -4px; border-radius: 18px;
  border: 2px solid #ffe273; animation: skip-window-ring 1.5s ease-in-out infinite; pointer-events: none;
}
@keyframes wave-launch-pulse { 0%,100% { transform: translateX(-50%) scale(1); } 50% { transform: translateX(-50%) scale(1.04); } }
@keyframes skip-window-ring  { 0% { opacity: 0.9; transform: scale(1); } 100% { opacity: 0; transform: scale(1.18); } }
```

### Streak badge + pill timer + preview popup

```css
#wave-launch-streak-badge {
  position: absolute; top: -10px; right: -10px;
  background: #5fe079; color: #0a1a0a; font-family: "Bangers",sans-serif;
  font-size: 12px; padding: 2px 8px; border-radius: 10px; border: 2px solid #0a1a0a;
  display: none;
}
#wave-launch-streak-badge.show { display: block; }
#wave-launch-pill {
  position: fixed; left: 50%; bottom: 176px; transform: translateX(-50%);
  background: rgba(20,28,36,0.92); border: 2px solid #ffd23f; border-radius: 14px;
  padding: 4px 14px; font-size: 13px; font-weight: 700; color: #ffd23f;
  pointer-events: none; z-index: 22; display: none;
  box-shadow: 0 4px 0 rgba(0,0,0,0.4);
}
#wave-launch-pill.show { display: block; }
#wave-launch-preview {
  position: fixed; bottom: 178px; left: 50%; transform: translateX(-50%) translateY(8px);
  min-width: 240px; max-width: 300px; padding: 10px 14px;
  background: rgba(14,20,26,0.95); border: 2px solid #ffd23f; border-radius: 12px;
  color: #fff; font-size: 13px; line-height: 1.45; text-align: left;
  pointer-events: none; z-index: 24; opacity: 0;
  transition: opacity 0.18s ease, transform 0.18s ease;
}
#wave-launch-preview.show { opacity: 1; transform: translateX(-50%) translateY(0); }
```

### Mobile override

```css
@media (pointer: coarse), (max-width: 900px) {
  #wave-launch-btn {
    left: auto; right: 18px;
    bottom: calc(96px + env(safe-area-inset-bottom, 0px));
    width: 80px; height: 80px; border-radius: 50%;
    font-size: 30px; padding: 0; transform: none;
  }
  #wave-launch-btn .sub { font-size: 11px; margin-top: -2px; }
  #wave-launch-btn:hover, #wave-launch-btn:active { transform: scale(0.95); }
  @keyframes wave-launch-pulse { 0%,100% { transform: scale(1); } 50% { transform: scale(1.08); } }
  #wave-launch-pill    { left: auto; right: 18px; bottom: 184px; transform: none; font-size: 12px; padding: 3px 10px; }
  #wave-launch-preview { left: auto; right: 18px; bottom: 188px; transform: translateY(8px); max-width: min(260px,calc(100vw - 36px)); }
  #wave-launch-preview.show { transform: translateY(0); }
}
```

### Toast skip bonus (réutilise `.toast` existant)

```css
.toast.toast-skip-bonus { border-color: #ffd23f; background: linear-gradient(180deg, rgba(60,44,8,0.95), rgba(36,28,4,0.95)); }
.toast.toast-skip-bonus .toast-title { color: #ffe273; }
```

Durée 1.8 s (vs 3.2 s standard) — feedback rapide adapté à enchaînements skip.

---

## Specs JS (pseudo-code pour le ticket d'exécution)

### LevelRunner — état + tick

```js
this._waveBreakActive  = false;
this._skipWindowMs     = 5000;
this._skipWindowTimer  = 0;
this._streakCount      = 0;
this._rewardMulSkip    = 1;

// À la fin d'une wave (remplace ligne 499-513) :
onWaveCleared() {
  emit("crowdef:wave-clear", { wave: this.wave });
  this._waveBreakActive = true;
  this._skipWindowTimer = this._skipWindowMs;
  ui.showWaveLaunchButton();
}

// Tick :
if (!this._waveActive && this._waveBreakActive) {
  if (this._skipWindowTimer > 0) {
    this._skipWindowTimer = Math.max(0, this._skipWindowTimer - dtMs);
    ui.updateSkipPill(this._skipWindowTimer, this._streakCount);
  } else if (this._streakCount > 0) {
    this._streakCount = 0;
    ui.updateStreakBadge(0);
  }
}
// NB: aucun branch auto-start. _currentBreakMs n'est PAS consulté ici.
```

### launchWaveManual (clic + N)

```js
launchWaveManual() {
  if (!this._waveBreakActive) return;
  if (!this._hasMoreWaves()) return this._winLevel();
  const inWindow = this._skipWindowTimer > 0;
  if (inWindow) {
    this._streakCount = Math.min(5, this._streakCount + 1);
    this._coins += 30;
    ui.spawnSkipBonusToast({ wave: this.wave + 1, coins: 30, streakPct: this._streakCount * 5 });
  } else {
    this._streakCount = 0;
    ui.updateStreakBadge(0);
  }
  this._rewardMulSkip = 1 + (this._streakCount * 0.05);   // cap natural via Math.min(5,...)
  this._waveBreakActive = false;
  this._skipWindowTimer = 0;
  this.wave++;
  this._initWave(this.wave - 1);
  this._spawnTimer = 0;
  this._waveActive = true;
  Audio.sfxWaveStart();
  ui.hideWaveLaunchButton();
  emit("crowdef:wave-start", { wave: this.wave });
  this.eventManager.onWaveStart(this.wave);
}

// Debounce 300 ms partagé (clic + touche N) :
let _lastLaunchTs = 0;
function onLaunchInput() {
  const now = performance.now();
  if (now - _lastLaunchTs < 300) return;
  _lastLaunchTs = now;
  __cd.runner.launchWaveManual();
}
```

### Reward kill — patch `_worldRewardMul()` existant

```js
_killReward(enemy) {
  const base     = enemy.baseReward;
  const worldMul = this._worldRewardMul();           // D1-01
  const skipMul  = this._rewardMulSkip ?? 1;         // D1-02
  return Math.round(base * worldMul * skipMul);
}
```

### Raccourci `N` (audit `src-v3/main.js:828-921` + grep src-v3 = aucun conflit)

Touches actuellement utilisées : 1-9/0/-/=/W/A/S/D + flèches/Shift/Space/Tab/Escape/P/B/Q/U/I/V/E. **N est libre.**

Ajout après `KeyE` (ligne ~905) :

```js
if (e.code === "KeyN" && !e.repeat) {
  if (_isAnyOverlayOpen() || runner.paused) return;
  onLaunchInput();
  return;
}
```

Justification : `N` = initiale Next/Nouvelle. Touche présente identique sur QWERTY/AZERTY/QWERTZ.

---

## Critères de succès

1. Bouton visible 200 ms après `crowdef:wave-clear`, masqué pendant `_waveActive`.
2. **Auto-start neutralisé strict** : aucune wave ne démarre seule, peu importe le temps écoulé (`_currentBreakMs` n'est plus consulté).
3. Pill timer décompte `5.0s → 0.0s` en temps réel (refresh 100 ms suffit).
4. Streak : +1 par skip dans la fenêtre, cap 5, reset si fenêtre 5 s expire sans clic, reset si clic hors fenêtre.
5. Toast skip bonus s'affiche 1.8 s à chaque clic dans la fenêtre.
6. Touche `N` lance la wave (debounce 300 ms partagé avec le bouton).
7. Mobile : bouton 80×80 (≥ 44 px WCAG), bottom-right, jamais en conflit avec joystick/speed/toolbar.
8. Preview : 3+ enemy types triés par count + reward base + état skip + état streak.
9. Animations cycles ≤ 400 ms (pulse boucle 1.6 s mais transitions internes 200 ms / ring 1.5 s linéaire).
10. Test stress 5 min sans clic : zéro consommation CPU > baseline, état stable.

---

## Effort estimé

| Sous-tâche | Estimé |
|-----------|--------|
| HTML + CSS desktop + mobile + preview | 2-3 h |
| LevelRunner refactor (état skip window + streak + désactivation auto-start) | 3-4 h |
| Binding `N` + debounce 300 ms | 30 min |
| Toast + pill timer + streak badge | 1-2 h |
| Preview composition (hover desktop, long-press mobile) | 2 h |
| Tests Chrome MCP (14 scénarios) + 4 auto-QA `.mjs` | 1-2 h |
| **Total** | **~9-13 h (1.5 j Sonnet feature-dev)** |

---

## Risques

1. **Coop split-keyboard** : P2 utilise flèches, pas N → safe.
2. **Joueur AFK** : pas de cap → wave non lancée arbitrairement longtemps. **Assumé Mike**. Pause auto-onglet (CLAUDE.md root) couvre tab inactif.
3. **Confusion débutants** : aucun tutoriel = joueur ne comprend pas comment lancer. **Mitigation** : pulse + glow + badge `(N)` + hint dans `Tutorial.js` au niveau 1.1.
4. **Streak exploit +25%** : compense le risque accru du placement plus serré (D1-01 économie). Évalué OK.
5. **Frame drop pill timer** : MAJ à 100 ms (1 décimale visible suffit), pas 16 ms.
6. **Conflit mobile spatial** : si toolbar mobile grossit, bouton wave doit garder ≥ 24 px de marge — vérifier sur écran 320 px portrait.

---

## Test plan

### Scénarios Chrome MCP manuels (14)

1. Bouton apparaît post-wave-clear sous 500 ms.
2. Skip dans fenêtre → +30¢ + toast "+30¢ skip · Streak +5% (×1)".
3. Skip hors fenêtre (>5s) → 0¢ + toast absent + streak reset.
4. Streak cap : 6 skips consécutifs → `_streakCount === 5`, `_rewardMulSkip === 1.25`.
5. Streak reset par expiration : 3 skips + laisser 4ème expirer → streak revient à 0.
6. Auto-start neutralisé : attendre 60 s sans clic → `wave` inchangée, bouton toujours visible.
7. Raccourci `N` : gameplay → trigger. Pause → no-op. Modal ouverte → no-op.
8. Debounce 300 ms : double-clic rapide → 1 seule wave lancée.
9. Hover desktop 300 ms → preview affiche enemy types triés + base reward.
10. Long-press mobile 500 ms → preview popup, re-tap close, auto-close 4 s.
11. Anti-conflit joystick mobile (2 doigts simultanés) : drag joystick + tap wave button → les deux fonctionnent.
12. Touch target mobile : DevTools mobile → mesure 80×80 px (≥ 44 conforme).
13. Reward mul : streak ×3, kill mob base 10¢ → coin gain = `round(10 × worldMul × 1.15)`.
14. Stress test 5 min AFK : CPU stable, état runner inchangé, bouton visible.

### Auto-QA scenarios (`.claude/qa/scenarios/sprint-D1/`)

```js
// d1-pacing-manual-launch.mjs
export default {
  id: "d1-pacing-manual-launch",
  description: "Bouton wave-launch + skip bonus + streak + N hotkey",
  setup: async (page) => {
    await page.evaluate(() => window.__cd.unlockAll());
    await page.evaluate(() => window.__cd.goto("world1-1"));
    await page.waitForTimeout(2000);
  },
  assertions: [
    { type: "hard", expr: "document.querySelector('#wave-launch-btn') !== null" },
    { type: "hard", expr: "typeof window.__cd.runner.launchWaveManual === 'function'" },
    { type: "soft", judge: "Le bouton est-il visible, lisible, bottom-center desktop ou bottom-right mobile?" },
    { type: "soft", judge: "L'animation pulse est-elle fluide, non-distractive?" }
  ]
};
```

3 autres `.mjs` à livrer :
- `d1-pacing-skip-window.mjs` : clic < 5 s → `runner._coins` +30 + `_streakCount === 1`.
- `d1-pacing-streak-cap.mjs` : 6 skips d'affilée → `_streakCount === 5`, `_rewardMulSkip === 1.25`.
- `d1-pacing-no-auto-start.mjs` : attendre 30 s post-clear → `_waveActive === false`, `wave` inchangée.

### Sprint-gate D1 — critères pass

- 4 scénarios auto-QA : 4/4 hard pass.
- 14 scénarios manuels Chrome MCP : 14/14 OK.
- `npm run build:kingshot` : delta bundle < +5 KB gz.
- `npm run test:crowdef` : 23/25 baseline maintenu.

---

## Decision points (récap arbitrages Mike)

| Choix | Décision | Source |
|-------|----------|--------|
| Auto-start | OFF strict | Mike 2026-05-11 |
| Timeout fail-safe | NON (liberté infinie B) | Mike 2026-05-11 |
| Skip bonus formule | +30¢ flat, fenêtre 5 s | Mike 2026-05-11 |
| Streak | +5%/wave cumulatif, cap +25% | Mike 2026-05-11 |
| Streak reset | si fenêtre expire OU clic hors fenêtre | spec D1-02 |
| Raccourci | `N` (audit clean) | spec D1-02 |
| Debounce | 300 ms | spec D1-02 |
| Position desktop | bottom-center, 220×56 px | spec D1-02 |
| Position mobile | bottom-right, 80×80 px circulaire | spec D1-02 |
| Preview wave | hover 300 ms / long-press 500 ms | spec D1-02 |
| Animation pulse | 1.6 s ease-in-out infinite | spec D1-02 |
| Toast durée | 1.8 s (vs 3.2 standard) | spec D1-02 |
