# HUD V4 — Analyse profonde codebase + runtime

> Lecture directe de `/Users/mike/Work/milan project/src-v3/` (Phaser/Three.js legacy).
> Pas d'accès Chrome MCP pour le live — section B basée sur le code seul.

---

## Section A : V4 Codebase findings

### A1. Architecture HUD (index.html + main.js)

Le HUD V4 est 100 % DOM HTML/CSS positionné en `position: fixed` par-dessus un `<canvas id="app">` Three.js. Pas de système Unity Canvas — tout est du DOM natif.

#### Structure DOM principale

```
<canvas id="app" />                  ← Three.js renderer (position:fixed, inset:0)
<div id="fps-meter" />               ← fixed left:10px top:10px, z:30
<div id="build-version" />           ← fixed right:6px bottom:4px, z:25
<div id="hint-shift" />              ← fixed right:18px bottom:220px, z:21
<div id="hint-bluepill" />           ← fixed right:18px bottom:178px, z:21
<div id="ui">                        ← position:fixed inset:0, padding:14px 18px
  <div class="row top-bar">          ← grid 3-colonnes: 1fr auto 1fr
    <div class="left">               ← title + wave + hero-bar
    <div id="castle-hp" />           ← centré (colonne auto)
    <div class="right">              ← buttons + coins + gems
</div>
<div id="tower-toolbar" />           ← fixed bottom:16px centré, grid 6 cols × 76px
<div id="speed-control" />           ← fixed left:18px bottom:18px
<div id="combo-tracker" />           ← fixed right:18px bottom:96px
<div id="minimap" />                 ← canvas fixed top:62px right:8px, 240×140px
<div id="radial-menu" />             ← fixed, positionné par JS sur tour sélectionnée
<div id="hero-ult-cooldown" />       ← fixed right:18px bottom:262px
<div id="boss-banner" />             ← fixed top:70px centré
<div id="wave-countdown" />          ← fixed top:72px centré
<div id="wave-start-banner" />       ← fixed top:72px centré
<div id="wave-clear-banner" />       ← fixed top:72px centré
```

#### Top-bar layout exact (desktop)

```
┌─────────────────────────────────────────────────────────┐
│ [title] [Vague N] [hero-bar]  [castle-hp-bar]  [🛒🗺️⚙️📚🔊] [🪙N] [💎N] │
└─────────────────────────────────────────────────────────┘
```

- Grid CSS `grid-template-columns: 1fr auto 1fr`
- `castle-hp` est l'élément central (colonne `auto`)
- Gauche : title (`display:none` sur mobile) + wave counter + hero XP bar
- Droite : icon buttons + coins + gems

### A2. Tower Toolbar (Toolbar.js + index.html)

```css
#tower-toolbar {
  position: fixed;
  left: 50%; bottom: 16px;
  transform: translateX(-50%);
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  grid-auto-rows: 76px;
  gap: 8px;
  padding: 10px;
  background: rgba(14, 20, 26, 0.88);
  border: 2px solid #3a4a5a;
  border-radius: 16px;
  max-width: 620px;
}

.tt-cell {
  width: 92px; height: 76px;
  background: rgba(28, 40, 52, 0.9);
  border: 2px solid #4a5a6a;
  border-radius: 12px;
  font-size: 30px;   /* emoji icône tour */
}
```

Chaque cellule contient :
- `tt-key` : hotkey (1-9, 0, -, =) — top-left, 12px, fond `rgba(0,0,0,0.45)`
- `tt-icon` : emoji tour (30px)
- `tt-cost` : coût en centimes — bottom-right, 13px, couleur `#ffd23f`

États CSS :
- `.selected` : border `#ffd23f`, bg `rgba(60,50,20,0.95)`, glow `rgba(255,210,63,0.6)`
- `.locked` : opacity 0.45, grayscale 0.8, cursor not-allowed + overlay label `🔒 WN`
- `.poor` : opacity 0.55
- `.tt-cell--forbidden` : opacity 0.35, grayscale 0.8, pointer-events none

Animation sélection : `tt-pulse`, 0.22s ease-out, scale 1 → 1.09 → 1.

Mobile (<640px) : cellules 50×44px, font 18px. Scroll horizontal snap (100vw - 12px).

### A3. Minimap (Minimap.js)

Canvas 2D, taille fixe 240×140px. Positionné `top: calc(62px + env(safe-area-inset-top))`, `right: 8px`.

Border : `2px solid #ffd23f`. Background : `rgba(20,28,36,0.85)`. z-index: 12.

Mobile (< 600px) : réduit à 96×56px. Masqué si < 400px.

Rendu canvas :
- Path : ligne `#ffd23f`, width 2px
- Entry : cercle blanc r=6
- Build points occupés : `#ffd23f` 1.5×1.5px, libres : `rgba(120,220,255,0.7)`
- Ennemis normaux : `#ff4040` r=3, boss : `#ffd23f` r=5
- Château (fin chemin) : emoji 🏰, 12px
- Héros : cercle `#5fe079` r=4 + flèche direction

### A4. Palette couleurs V4

```
Fond HUD panels   : rgba(14-20, 20-28, 26-36, 0.85-0.92)
Border principale : #3a4a5a  (bleu-gris)
Accent or         : #ffd23f  (sélection, cost, minimap path)
Texte primaire    : #ffffff
Texte secondaire  : #cfd8e6  (hotkeys toolbar)
Coins             : #ffd23f préfixé 🪙, bg rgba(20,28,36,0.85)
Gems              : #d8a8ff préfixé 💎, bg rgba(40,18,52,0.85), border #6a3aa0
Hero XP bar fill  : linear-gradient #66ccff → #aae5ff
Castle HP bar     : vert #5fe079/#8fee9b → orange #f0c83d → rouge #e15454
Boss HP bar       : #c63a10 → #ff7a55
FPS good          : #5fe079, warn : #f0c83d, bad : #ff7a55
```

Typographie :
- `Fredoka` (Google Fonts) — tout le HUD, weights 400/500/600
- `Bangers` (Google Fonts) — titres boss, level result, splash
- Fallback : `sans-serif`

### A5. Keyboard mapping complet (main.js:828-921)

| Touche | Action |
|--------|--------|
| W / ArrowUp | Déplacer héros avant |
| S / ArrowDown | Déplacer héros arrière |
| A / ArrowLeft | Déplacer héros gauche |
| D / ArrowRight | Déplacer héros droite |
| Shift | Courir (hold) |
| B | BluePill toggle (téléport château) |
| U | Upgrade tour (radial menu ouvert) |
| I | Info tour (radial menu ouvert) |
| V | Vendre tour (radial menu ouvert) |
| 1-9 | Sélectionner tour 1-9 (toggle) |
| 0 | Tour 10 |
| - / NumpadSubtract | Tour 11 |
| = / NumpadAdd | Tour 12 |
| Space | Hero ult (salve) |
| Tab | Toggle zoom closeup |
| Escape | Déselect tour → fermer modal → pause |
| P | Pause toggle |
| E | Encyclopédie (si pas de zone enemy) |
| F | Toggle FPS meter |

Note : pas de touche N/Space pour "Lancer la vague" en V4. Le lancement de vague est automatique (timer `_waveBreakTimer`), affiché via `#wave-countdown`.

### A6. Castle (Castle.js)

**Pas de mini-camera separate.** La barre HP du château est un élément DOM dans `#castle-hp` (top bar). Le modèle 3D du château dans la scène Three.js a :
- `hpBarBg` : THREE.PlaneGeometry 3.2×0.32, MeshBasicMaterial noir, positionné y=5.0, incliné -PI/4
- `hpBar` : idem en vert `0x44dd44` → orange `0xffaa22` → rouge `0xff3322`
- `hpText` : THREE.Sprite avec CanvasTexture, "N / N" en bold 38px sans-serif, blanc/ombre noire, positionné y=5.6

La HP bar in-world (3D) est visible sur le mesh dans la scène. La HP bar DOM (top-center) reflète la même donnée via `runner.castleHP`.

GLTF par thème :
```js
plaine    → castle_plaine    scale 2.1
foret     → castle_medieval  scale 2.1
desert    → castle_volcan    scale 2.1
volcan    → castle_volcan    scale 2.1
foire     → castle_plaine    scale 2.1
apocalypse→ castle_apocalypse scale 2.1
espace    → castle_espace    scale 2.3
submarin  → decor_submarin_shipwreck scale 1.8
medieval  → castle_medieval  scale 2.1
cyberpunk → decor_cyberpunk_computer scale 1.6
```

Effets HP :
- < 50% HP : smoke particle loop (`setInterval` 400ms)
- < 20% HP : `PointLight(0xff2020, 2.5, r=8)` pulsant sin via `tick(dt)`
- 0 HP : grayscale complet, `CustomEvent("crowdef:castle-destroyed")`

### A7. Speed control

```html
<div id="speed-control" style="position:fixed; left:18px; bottom:18px; z-index:19;">
  <button class="speed-btn" data-speed="1">×1</button>
  <button class="speed-btn" data-speed="2">×2</button>
  <button class="speed-btn" data-speed="3">×3</button>
</div>
```

Boutons `min-width: 44px; min-height: 44px` (touch-compliant). `runner.setSpeed(s)` avec interne `gameSpeed = s * 1.5`.

Sur mobile (`pointer: coarse`) : `#speed-control` caché, remplacé par `#mobile-speed-control` en fixed left:8px bottom:90px.

### A8. Radial menu tour

Déclenché sur clic/tap d'une tour placée. 3 boutons circulaires (60px, r=50%) :
- Upgrade : `left:0; top:-70px` (au-dessus)
- Info : `left:-65px; top:35px` (bas-gauche)
- Sell : `left:65px; top:35px` (bas-droite)

Animation : `scale(0) → scale(1)`, `0.18s cubic-bezier(0.34, 1.56, 0.64, 1)`.

Sur mobile (< 600px) : Info passe à `left:-44px`, Sell à `left:44px` (plus proche).
Touch targets élargis à 72×72px sur `pointer: coarse`.

---

## Section B : Live runtime observations

> Pas d'accès Chrome MCP live — observations déduites du code.

**Rendu global estimé (desktop) :**

```
┌──────────────────────────────────────────────────────────────────┐
│ [Milan CD] [Vague 1] [Lv1 ──██──]   [🏰 100/100 ████████]   [🛒🗺️⚙️📚🔊] [🪙100] [💎0] │
├──────────────────────────────────────────────────────────────────┤
│                      Three.js scene                               │
│                                                         [minimap] │
│                [wave-countdown: Vague 2 dans 4s]                  │
│              [ult cooldown: Espace ⚡ ring]                       │
│                                              [hint-shift: Shift]  │
│                                              [hint-bluepill: B]   │
│                                              [combo-tracker pills]│
│   [×1][×2][×3]                                                    │
│                 [🗼1][🏹2][🧊3][🔮4][⚡5][🏯6]                   │
│                 [🏰7][💣8][🛡️9][🌀0][🌌-][👻=]                   │
└──────────────────────────────────────────────────────────────────┘
```

Couleurs observées via CSS :
- Background body : `#0d1418` (quasi-noir)
- HUD panels : `rgba(20,28,36,0.85)` = bleu-marine très sombre semi-transparent
- Theme-color meta : `#1a0a30` (violet foncé)
- Splash bg : `radial-gradient(#2a1810, #0a0604)` = brun-noir avec halo rougeâtre
- Splash title : `#ff8030` (orange lava)

---

## Section C : Castle preview reality

**Verdict : double affichage — DOM top-bar + 3D in-world. Pas de mini-camera.**

1. **DOM `#castle-hp` (top-bar centre)** : barre de progression 2D uniquement, hauteur 18px, pas de visuel du château. Labels "🏰 N/N" + barre verte/orange/rouge.

2. **3D in-world** : modèle GLTF (ou primitives Three.js fallback) positionné à la fin du chemin. Barre HP Three.js PlaneGeometry flottant au-dessus (y=5.0), incliné vers la caméra (-PI/4). Sprite CanvasTexture "N/N" (y=5.6).

3. **Minimap** : simple emoji 🏰 dessiné en `font-size: 12px` via `ctx.fillText` — pas de rendu 3D.

**Il n'y a PAS de "castle preview" au sens d'une RenderTexture ou mini-camera.** Le château est uniquement visible dans la scène principale Three.js avec sa barre HP 3D.

---

## Section D : Top 10 differences V4 vs V6

Ordonnées par impact visuel (1 = plus visible).

| # | V4 (legacy Three.js/DOM) | V6 (Unity actuel) | Impact |
|---|--------------------------|-------------------|--------|
| 1 | Fonts Google (Bangers + Fredoka) — chargées via `<link>` | Unity utilise Roboto-Regular.ttf (vu dans Assets/Fonts/) — pas de Bangers/Fredoka | Très haut — toute la personnalité typographique du HUD |
| 2 | Castle HP : barre DOM centré top + 3D in-world billboardé | À implémenter — actuellement unknonw | Haut — élément central de lisibilité |
| 3 | Tower toolbar : grid 6 cols × 76px, bottom-center, fond dark semi-transparent, émojis tours | À mapper en Unity Canvas | Haut — principale surface interactive |
| 4 | Minimap canvas 2D 240×140px, top-right, border gold `#ffd23f` | À implémenter | Haut — navigation spatiale |
| 5 | Speed control ×1/×2/×3 en bas-gauche (44×44px buttons) | Non visible dans V6 actuel | Moyen — pacing gameplay |
| 6 | Radial menu 3 boutons circulaires (Upgrade/Info/Sell) déclenché sur clic tour | À implémenter | Moyen — interaction tour |
| 7 | Combo tracker pills (synergies actives) en bas-droite | Non spécifié dans V6 | Moyen — feedback synergie |
| 8 | Hero ult cooldown ring conic-gradient bottom-right | À implémenter | Moyen — lisibilité cooldown |
| 9 | Wave-start/clear banners top-center (transition 0.2s ease) | Non spécifié | Bas-moyen — feedback pacing |
| 10 | Danger vignette (box-shadow inset rouge pulsant) sur castle danger | Non visible dans V6 | Bas — feedback urgence |

---

## Section E : Implementation plan révisé

### Priorités fondées sur le code V4

**E1. Typographie (P0 — bloquant visuel)**

V4 utilise Fredoka (corps HUD) + Bangers (titres/boss). Unity V6 a `Assets/Fonts/Roboto-Regular.ttf`. Action requise :
- Importer `Fredoka-Regular.ttf` + `Fredoka-SemiBold.ttf` via Google Fonts → `Assets/Fonts/`
- Importer `Bangers-Regular.ttf` → `Assets/Fonts/`
- Configurer 2 `TMP_FontAsset` dans `Assets/UI/Fonts/`
- Mapper `HudController.cs` pour utiliser `fredokaFont` (corps) et `bangersFont` (titres)

**E2. Top-bar layout — castle-hp centré (P0)**

Structure V4 exacte : `grid-template-columns: 1fr auto 1fr` avec castle-hp en colonne centrale.
En Unity Canvas : `HorizontalLayoutGroup` avec `LayoutElement.FlexibleWidth = 1` pour les colonnes gauche/droite, `LayoutElement.PreferredWidth = 320` pour castle-hp.

Castle-hp dimensions : `min-width: 240px; max-width: 320px; height: 18px` barre interne. Couleurs : vert `#5fe079` → orange `#f0c83d` → rouge `#e15454` (seuils 0.6 et 0.3).

**E3. Tower toolbar (P0)**

6 colonnes × 76px (desktop), 92×76px par cellule. Responsive mobile : 60×60px scroll horizontal.
Chaque cellule : hotkey badge (top-left, fond `rgba(0,0,0,0.45)`, couleur `#cfd8e6`), icône (30px), coût (bottom-right, `#ffd23f`).
Background toolbar : `rgba(14,20,26,0.88)`, border `#3a4a5a`, radius 16px.

**E4. Minimap (P1)**

Canvas 240×140px, positionné top-right sous la safe area. `Minimap.js` utilise le path 3D sampelé (`path.getSpacedPoints(80)`). En Unity : `RenderTexture` camera orthographique top-down OU redessiner via `Texture2D` en script C# en read de `runner.enemies` + `runner.buildPoints`.

**E5. Radial menu (P1)**

3 boutons circulaires 60px positionnés relativement à la tour cliquée. En Unity : `RectTransform` dynamiquement repositionné en WorldToScreenPoint. Animation : scale 0→1, 0.18s `cubic-bezier(0.34, 1.56, 0.64, 1)` = `easeOutBack` en Unity.

**E6. Castle 3D HP bar (P1)**

Double billboarded in-world. En Unity : `Canvas (World Space)` attaché au `Castle` GameObject, `Camera.main.transform` tracked via `LateUpdate`. OU utiliser le DOM-équivalent Unity : `TextMeshPro` in world space + `Image` pour la barre.

**E7. Speed control (P1)**

3 boutons ×1/×2/×3, `min 44×44px`, bottom-left. Simple `HorizontalLayoutGroup` avec `Button[]`. `LevelRunner.SetSpeed(int s)` → `gameSpeed = s * 1.5f`.

**E8. Wave feedback banners (P2)**

Top-center, slide-in 0.2s ease. Position : `top: 72px` = juste sous la top-bar. En Unity : `Canvas` Z=21, `VerticalLayoutGroup` centré horizontal, animation `DOTween` ou `Coroutine` translate Y -20px → 0.

**E9. Danger vignette (P2)**

`Image` full-screen avec `Color(r=0.77, g=0.22, b=0.06, a=0)`, animé en boucle via `Mathf.Sin`. S'active quand `castleHP / castleHPMax < 0.3`.

**E10. Combo tracker pills (P2)**

`VerticalLayoutGroup` bottom-right, `z-index: 19` (= entre toolbar et top-bar). Chaque synergy active = une pill `RoundedImage` border `#ffd23f`, text synergy.

---

### Note sur les wave triggers

**V4 n'a PAS de bouton "Lancer la vague (N)"** — le spec D1-02 l'ajoute. V4 lance les vagues automatiquement via `_waveBreakTimer` avec un comptdown affiché dans `#wave-countdown`. L'intégration D1-02 (bouton avec skip bonus 5s/30¢) est un ajout pur au-dessus du système existant.

### Couleurs finales à documenter pour Unity

```
Color panelBg      = new Color(0.078f, 0.110f, 0.141f, 0.85f);  // rgba(20,28,36,0.85)
Color borderDefault = new Color(0.227f, 0.290f, 0.353f, 1f);     // #3a4a5a
Color accentGold   = new Color(1f, 0.824f, 0.247f, 1f);          // #ffd23f
Color hpGreen      = new Color(0.373f, 0.878f, 0.475f, 1f);      // #5fe079
Color hpOrange     = new Color(0.941f, 0.784f, 0.239f, 1f);      // #f0c83d
Color hpRed        = new Color(0.882f, 0.333f, 0.333f, 1f);      // #e15454
Color gemsPurple   = new Color(0.847f, 0.659f, 1f, 1f);          // #d8a8ff
Color heroBlue     = new Color(0.4f, 0.8f, 1f, 1f);              // #66ccff
```
