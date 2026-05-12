# HUD V4 Realign — Spec de réalignement visuel

**Date** : 2026-05-12
**Objectif** : ramener V6 visuellement au niveau de V4 (référence Phaser). Zéro changement fonctionnel, uniquement CSS/layout UXML.

---

## 1. Wireframe ASCII — Cible V4

```
+------------------------------------------------------------------+
| [Crowd Defense]  [Vague 1]  [Lv 1  0/50 |||||     ]            |
|                                                                  |
|   [HP] Chateau  [||||||||||||||||  120/120  ]                   |
|                                                                  |
|                                         [livre][gear][bat][ach] |
|                                         120 (or)  9030 (gems)   |
|                                         +--minimap 150x100--+   |
|                                         |  (dark + overlay) |   |
|                                         +-------------------+   |
|                                                                  |
|                   (gameplay 3D)                                  |
|                                                                  |
|                                                                  |
|                  [ Lancer la vague (N) ]                        |
|                    Vague 1 / 4                                   |
|                                                                  |
| [castle-preview]              [tour1][tour2][tour3]...[tourN]   |
| HP 120/120                    (actives en clair, locked grisés) |
| [x1][x2][x3]                                                    |
| Espace ult                                                       |
| Shift courir                                                     |
| B retour chateau                                                 |
+------------------------------------------------------------------+
```

---

## 2. Mapping V4 -> V6 (Etat actuel)

### Elements V4 MANQUANTS dans V6

| Element V4 | Selector UXML cible | Note |
|---|---|---|
| Titre "Crowd Defense" (top-left, gold small) | `.top-bar` — ajouter `Label` `game-title` | Absent du UXML actuel |
| Badge "Vague 1" rounded dark (top-left) | `#pill-wave` existe mais pas en top-left | Déplacer |
| Badge XP "Lv 1 0/50" + progress bar (top-left) | `.hero-panel` existe mais bottom-left | Déplacer en top-left |
| HP château centré PROMINENT (top-center) | `#pill-hp` existe mais dans top-bar linear | Extraire et centrer seul, hauteur agrandie |
| 6 icon buttons en ligne (top-right) | Seulement `btn-settings`, `btn-mute`, `btn-doctrine` | Manque : livre/bestiary, achievement, building |
| Compteur or badge arrondi dark (top-right) | `#pill-gold` existe mais dans top-bar distribué | Déplacer top-right |
| Compteur gems badge (top-right) | Absent | Nouveau element `.gem-counter` |
| Minimap ~150x100px sous les boutons (top-right) | `#minimap-container` existe mais `200x200` | Resize + repositionner top-right sous les icons |
| Castle 3D preview (bottom-left) | Absent | Nouveau `.castle-preview` (Image ou VisualElement) |
| HP castle sous preview (bottom-left) | Partiellement dans hero-panel | Déplacer |
| Control hints colonne "Espace ult / Shift courir / B retour" (bottom-left) | `#keyboard-hints` existe mais bottom-center, 1 ligne | Convertir en colonne, déplacer bottom-left |
| Tower roster 10 slots (bottom-center) | `#tower-toolbar` existe | Garder, styler icones + grisage locked |
| Speed x1/x2/x3 (bottom-left) | `#speed-control` existe mais dans top-bar | Déplacer bottom-left sous castle preview |

### Elements V6 A SUPPRIMER (trop chargés, absents V4)

| Element | Selector | Raison |
|---|---|---|
| Footer `#keyboard-hints` plein texte | `.keyboard-hints` | Remplacé par colonne hints bottom-left V4 |
| `#synergy-hud-panel` top-right permanent | `.synergy-hud-panel` | Pollue coin top-right; masquer par défaut, afficher seulement via toast |
| `#combo-multiplier-label` top-right | `.combo-multiplier` | Occupe l'espace minimap V4; déplacer bottom-center si nécessaire |
| `#combo-banner` top-right 60px | `.combo-banner` | Même zone que minimap; déplacer top-center |
| `#perk-badges-row` + `#perk-set-progress-row` bottom-center permanents | `.perk-badges-row`, `.perk-set-progress-row` | Surchargent la bottom-bar; masquer entre vagues (toast suffisant) |
| `#wave-preview` large cards 80x90px | `.wave-preview-chip` | Cards trop grosses; réduire à chips compactes 40x48px ou masquer |

### Elements V6 A DEPLACER

| Element | Position V6 | Position V4 cible |
|---|---|---|
| `#pill-gold` | top-bar row distribué | top-right, sous icon buttons |
| `#pill-wave` | top-bar row distribué | top-left, badge pill compact |
| `#speed-control` | top-bar row distribué | bottom-left, sous castle preview |
| `#hero-panel` (XP/level) | bottom-left absolute | top-left, inline badge |
| `#minimap-container` | top-right 200x200 | top-right 150x100, sous icon row |
| `#wave-launch-btn` | bottom-center 240x60 | bottom-center — garder, mais remonter légèrement (bottom: 90px) |

### Elements V6 A GARDER tels quels (fonctionnels, compatibles)

- `#radial-menu` — modal, non visible en jeu normal
- `#boss-banner` + `#boss-cutscene` — événements ponctuels
- `#danger-vignette`, `.castle-danger-vignette` — effets corrects
- `#tower-tooltip`, `#enemy-tooltip` — follow-cursor, invisibles par défaut
- `#tower-info-panel` — bottom-right, corresponds à V4
- `#l3-choice-panel`, `#run-summary-panel`, `#pause-root`, `#settings-root`, `#help-root` — modals, hors layout de base
- `#panel-game-over`, `#panel-victory` — overlays full-screen corrects
- `#wave-launch-btn` — fonctionnel et positionné correctement, juste à styler

### Elements V6 A MODIFIER (style uniquement)

| Element | Probleme actuel | Fix V4 |
|---|---|---|
| `.pill` gold/wave/hp | `flex-direction: column`, distribués en ligne, pas assez gold | Restructurer top-bar en 3 zones (`top-left`, `top-center`, `top-right`); pills arrondies dark bg + border gold |
| `#pill-hp` / `.hp-bar-fill` | Trop petit (6px), perdu dans la ligne | HP bar prominente 12px, label "Chateau" visible, centered block en top-center |
| `.top-bar` | `justify-content: space-between` linear → tout sur une ligne | Passer en 3-column layout : left / center / right flex |
| `.tower-toolbar` | Style plat, pas de grisage locked | Slots locked → `opacity: 0.4` + label "W2"/"W3" visible (classe `.tower-slot-locked`) |
| `#minimap-container` | 200x200, gold border correct | `width: 150px; height: 100px` |
| `.wave-launch-btn` | Correct fonctionnellement | Ajouter `box-shadow` doré pour prominence V4 |

---

## 3. Selectors CSS a creer / modifier

```
/* Layout principal — 3 zones */
.top-bar            -> justify-content: flex-start; gap: 8px  (remplacé par 3 enfants)
.top-bar-left       -> nouveau : titre + wave-badge + xp-badge
.top-bar-center     -> nouveau : castle-hp block centré absolu
.top-bar-right      -> nouveau : icon-row + gold-counter + gem-counter + minimap

/* Badges top-left */
.game-title         -> font-size:13px; color:rgb(255,210,63); font-weight:bold
.wave-badge         -> dark bg, border-radius:20px, gold border 1px, padding:4px 12px
.hero-xp-badge      -> dark bg, border-radius:20px, xp bar interne 8px height

/* HP chateau centré */
.castle-hp-block    -> position:absolute; left:50%; translate:-50% 0; top:12px
.castle-hp-bar      -> height:12px; width:200px; gold border 2px; border-radius:6px
.castle-hp-label    -> "Chateau" en gold, font-size:13px, centré au-dessus de la bar

/* Icon row top-right */
.icon-button-row    -> flex-direction:row; gap:6px
.hud-icon-btn       -> 36x36px; dark bg; border-radius:8px; gold border 1px (hover)
                       (min 44x44 en .hud-root.mobile)

/* Counters top-right */
.gold-counter       -> dark bg; border-radius:14px; gold text; padding:4px 12px
.gem-counter        -> dark bg; border-radius:14px; gem-purple text (#c084fc)

/* Minimap */
.minimap-container  -> width:150px; height:100px (desktop); gold border 2px

/* Bottom-left bloc */
.castle-preview     -> width:120px; height:80px; dark bg; border-radius:8px; gold border
.castle-preview-hp  -> text sous preview, gold, font-size:12px
.control-hints-col  -> flex-direction:column; font-size:11px; color:rgba(220,220,220,0.6)
                       (chaque hint = 1 Label, pas 1 longue ligne)

/* Speed buttons — bottom-left */
.speed-control      -> déplacer bottom-left; flex-direction:row; compact (padding:3px 8px)

/* Tower toolbar */
.tower-slot-locked  -> opacity:0.4; cursor:default
.tower-slot-locked-label -> font-size:9px; color:rgba(255,210,63,0.7); "W2"/"W3"

/* Wave launch */
.wave-launch-btn    -> ajouter box-shadow: 0 0 12px rgba(255,185,40,0.5)
```

---

## 4. Features V6 a SUPPRIMER du layout visible permanent

- `.keyboard-hints` footer bar (1 ligne texte opaque bas) — supprimer, remplacé par `.control-hints-col`
- `.synergy-hud-panel` toujours visible top-right — masquer par défaut (`display:none`), activer seulement si synergy active ET hover
- `.perk-badges-row` et `.perk-set-progress-row` permanents — masquer (`display:none` pendant gameplay), afficher seulement via toast
- `.combo-multiplier` top-right permanent — déplacer au-dessus de `.castle-hp-block` center (évite la zone minimap)

---

## 5. Contraintes mobile (preserve D5-01)

- `.hud-icon-btn` : `min-width:44px; min-height:44px` en `.hud-root.mobile`
- `.gold-counter`, `.gem-counter` : `min-height:44px; padding:10px 16px` en mobile
- `.castle-preview` : masquer en portrait (`display:none`) pour économiser de l'espace
- `.top-bar-left` en portrait : masquer `.game-title`, réduire badges à 1 colonne
- `.control-hints-col` : masquer en mobile (`.hud-root.mobile .control-hints-col { display:none; }`)
- Joystick zone `#joystick-zone` : inchangée (portrait seulement, déjà correct)

---

## 6. Decisions a valider par Mike avant implementation

1. **Castle preview** : image 3D render vs sprite flat icon ? (V4 montrait un modèle 3D, V6 n'a rien)
2. **Gems** : existe-t-il déjà une valeur gems dans `Economy` ou LevelRunner ? Selector `#gem-value` a créer ?
3. **Icon buttons** row : quels 6 icons exactement ? V4 avait livre/options/building/achievement — confirmer mapping fonctionnel V6 (bestiary, settings, shop, achievements, ?)
4. **XP badge top-left** : `hero-panel` actuel gère le hero XP — le déplacer top-left casse-t-il l'ancrage absolu du bluepill-btn (`bottom:200px, left:16px`) ?
5. **Tower slots locked** : comment HudController sait-il quels slots sont locked W2/W3 ? `TowerType.UnlockWave` field ? Confirm avant d'ajouter la classe CSS.
