# Phase 4 — Spec UX Mobile HUD (iOS + Android)

**Date** : 2026-05-11
**Auteur** : `ux-designer`
**Scope** : Redesign HUD touch-first pour iOS/Android. Cible 375×667 (iPhone SE) → 1290×2796 (iPhone 15 Pro Max). Portrait + landscape. Read-only spec — implementation Phase 4.

---

## Inventaire desktop actuel (baseline)

| Element | Position actuelle (USS) | Taille desktop | Probleme mobile |
|---|---|---|---|
| Pills Gold/Wave/HP | `top-bar`, padding 16px 24px | pill min-width 100-140px, font 24px | Trop petites sur SE, pas thumb-safe |
| Wave launch btn | `bottom: 110px; left: 50%` | 240×60px | 60px < 88px HIG minimum, centré = hors zone pouce |
| Radial menu L3 | `bottom: 110px; left: 50%` | 340px wide, boutons 148×76px | Boutons OK desktop, chevauchement en portrait |
| Sell btn radial | pleine largeur 340px, hauteur 36px | 36px | 36px < 44px minimum — trop petit |
| Settings panel | modal centré 560px wide | fixed width | Hors ecran sur SE (375px wide) |
| Overlay btns | `padding: 16px 48px` | font 24px | Probablement OK mais non validé tactile |
| Tower tooltip | position absolute, max-width 280px | min-width 200px | Depasse viewport sur SE |

Raccourcis clavier actifs (HudController.cs + main.js heritage) :
- `N` : lancer vague (debounce 300ms)
- `W/A/S/D` + fleches : deplacement hero (legacy Phaser)
- `B` : radial open/toggle
- `U` : upgrade DPS (radial ouvert)
- `I` : upgrade Utility (radial ouvert)
- `V` : sell (radial ouvert)
- `Space` : ultime hero
- `Tab` : zoom closeup
- `Escape` / `P` : pause / fermer modal
- `E` : encyclopedie
- `1-9`, `0`, `-`, `=` : selection tours

Aucun conflit detecte entre raccourcis existants. Sur mobile les raccourcis clavier ne s'appliquent pas (sauf clavier Bluetooth externe — bas priorite Phase 4).

---

## 1. HUD Layout Mobile

### 1.1 Principes directeurs

- **Thumb zone** : zone de confort pouce droit = quart inferieur droit. Actions primaires la. Zone dangereuse = centre haut.
- **Safe area** : respecter `env(safe-area-inset-*)` pour notch iPhone + punch-hole Android.
- **Touch target minimum** : 88×88px pour actions primaires (Apple HIG), 44×44px absolu minimum (Material 3).
- **Animations** : 250-300ms ease-out pour entrees, 200ms ease-in pour sorties. Jamais > 400ms (perf budget Unity main thread).
- **Opacite** : elements HUD a 0.85-0.92 opacity pour ne pas masquer la zone de jeu.

### 1.2 Pills Gold / Wave / HP

**Probleme desktop** : pills en `top-bar` avec `justify-content: space-between`. Sur SE (375px), chaque pill fait ~115px — lisible mais marge insuffisante pour les valeurs longues ("120/120").

**Solution mobile** :

Pills reformatees en barre horizontale compacte collant au bord superieur + safe-area. Font reducte, valeur proeminente.

USS mobile (a ajouter via media query `pointer: coarse` ou classe `.platform-mobile`) :

```
.top-bar {
    padding: 8px 12px;
    padding-top: calc(8px + env(safe-area-inset-top, 0px));
    flex-direction: row;
    justify-content: space-between;
}

.pill {
    min-width: 88px;
    padding: 6px 10px;
    border-radius: 10px;
}

.pill-label {
    font-size: 10px;
}

.pill-value {
    font-size: 18px;
}

.pill-hp {
    min-width: 120px;
}

.hp-bar-bg {
    height: 4px;
    margin-top: 3px;
}
```

Wireframe portrait iPhone SE (375×667) — etat "skip window, streak x2" :

```
+-----------------------------------------------+
| [safe-area top ~44px iOS]                     |
+-----------------------------------------------+
|  [Or  ]  [WAVE ]  [HP          ===]           |  <- top-bar 52px
|  120   |  2/4   |  90/120   [====    ]        |
+-----------------------------------------------+
|                                               |
|                                               |
|            ZONE JEU 3D                        |
|                                               |
|                                               |
|          [ +30c · 3.0s · +10%   ]             |  <- pill skip, centree
|                                               |
+-----------------------------------------------+
|  [safe-area bottom ~34px iOS]                 |
|       [ Lancer la vague 2/4  +x2 ]            |  <- btn 88px high, thumb-right
+-----------------------------------------------+
```

Wireframe portrait iPhone 15 Pro Max (430×932) :

```
+----------------------------------------------------+
| [safe-area top ~59px]                             |
+----------------------------------------------------+
|  [Or   ]   [WAVE  ]   [HP              ====]      |  <- 56px top-bar
|  120        2/4         90/120                    |
+----------------------------------------------------+
|                                                   |
|                                                   |
|              ZONE JEU 3D                          |
|                                                   |
|              [ +30c · 3.0s · +10%    ]            |
|                                                   |
+----------------------------------------------------+
| [safe-area bottom ~34px]                          |
|          [  Lancer la vague 2/4  [+10%] ]         |  <- 88×88+ btn
+----------------------------------------------------+
```

### 1.3 Bouton "Lancer la vague"

**Desktop actuel** : 240×60px, centree, `bottom: 110px`. Non conforme HIG (60 < 88px height).

**Mobile** :
- Taille : **320×88px** minimum portrait, **400×88px** landscape.
- Position : **bottom-center** a `bottom: calc(16px + env(safe-area-inset-bottom, 0px))`.
- Justification : centree est acceptable ici car le bouton est la seule action primaire inter-wave. Le pouce droit y arrive facilement sur tous les formats.
- Streak badge : coin superieur droit du bouton, 32×24px minimum (44px hit target inclus padding invisible).

USS mobile :

```
.wave-launch-btn {
    position: absolute;
    left: 50%;
    translate: -50% 0;
    bottom: calc(16px + env(safe-area-inset-bottom, 0px));
    width: 320px;
    height: 88px;
    border-radius: 20px;
    border-width: 3px;
}

.wave-launch-label {
    font-size: 20px;
}

.wave-launch-sub {
    font-size: 14px;
}
```

Animation entree (wave break detectee) :
- Demarre `display: none` -> `display: flex` + `opacity: 0; translate: 0 40px`
- Transition vers `opacity: 1; translate: 0 0` en 280ms ease-out
- Streak badge : scale 0 -> 1 en 200ms avec 80ms de delay (apres le bouton)

Animation sortie (wave lancee) :
- `opacity: 1 -> 0; scale: 0.92` en 180ms ease-in
- Puis `display: none`

### 1.4 Radial Menu L3 (selection tour L2)

**Desktop actuel** : 340px wide, boutons DPS/Utility 148×76px, sell 340×36px, position `bottom: 110px`.

Problemes sur mobile :
- Sell btn : 36px height < 44px minimum
- Affichage simultaneously avec wave-launch btn = collision visuelle en portrait
- Positionnement `left: 50%` = hors zone pouce

**Solution mobile** : le radial apparait lorsqu'une tour L2 est tappee. Il remplace temporairement le wave-launch btn dans la zone bas (pas de coexistence simultanee — les deux etats sont mutuellement exclusifs par game logic).

USS mobile :

```
.radial-menu {
    position: absolute;
    bottom: calc(16px + env(safe-area-inset-bottom, 0px));
    left: 50%;
    translate: -50% 0;
    width: calc(100% - 32px);
    max-width: 400px;
    padding: 14px;
    border-radius: 16px;
}

.radial-upgrade-row {
    flex-direction: row;
    justify-content: space-between;
    gap: 12px;
}

.radial-btn {
    flex: 1;
    height: 88px;
    border-radius: 14px;
    border-width: 2px;
    padding: 10px 8px;
}

.radial-btn-sell {
    width: 100%;
    height: 56px;   /* >= 44px */
    border-radius: 10px;
    margin-top: 10px;
}
```

Wireframe radial menu mobile portrait :

```
+-----------------------------------------------+
|            ZONE JEU 3D                        |
|                          [ Tour selectionnee  |
|                            range indicator ]  |
+-----------------------------------------------+
|  [safe-area bottom]                           |
|  +-------------------------------------------+|
|  | Tour de Feu L2                             ||  <- titre radial
|  |  +------------------+  +-----------------+||
|  |  |  DPS            |  | Utility         |||  <- 88px height chacun
|  |  |  +350 PdF        |  | +Slow 20%       |||
|  |  |  120c            |  | 80c             |||
|  |  +------------------+  +-----------------+||
|  |  [   Vendre (retour 80c)                 ]||  <- 56px height
|  +-------------------------------------------+|
+-----------------------------------------------+
```

Interaction touch : tap tour => radial open (animation 250ms slide-up depuis bas). Tap hors radial => fermeture (200ms slide-down). Pas de swipe-to-open — le tap direct est plus simple et sans ambiguite.

### 1.5 Tower Tooltip

**Desktop actuel** : position absolute, max-width 280px, apparait au hover.

**Mobile** : le hover n'existe pas. Pattern remplace :

- **Tap-and-hold 400ms** sur une tour placee => tooltip apparait
- Position : au-dessus de la tour selectionnee, clamped dans le viewport (ne depasse jamais les bords)
- Fermeture : relacher le doigt
- Taille minimum : 240px wide, hauteur auto

USS mobile :

```
.tower-tooltip {
    min-width: 240px;
    max-width: calc(100% - 32px);
    padding: 12px 16px;
    border-radius: 12px;
    border-width: 1px;
}

.tooltip-header {
    font-size: 15px;
}

.tooltip-stats {
    font-size: 13px;
}

.tooltip-synergies {
    font-size: 13px;
}
```

---

## 2. Touch Controls

### 2.1 Placement de tour

**Desktop** : click sur cellule verte avec type de tour selectionne dans toolbar.

**Mobile** :

| Geste | Action | Notes |
|---|---|---|
| Tap cellule verte | Placer tour du type selectionne | Si aucun type selectionne : ouvre selecteur de tour (bottom sheet) |
| Tap tour placee | Selectionner tour (ouvre radial) | |
| Tap hors grille | Deselectionner | |
| Tap-and-hold tour | Afficher tooltip stats | Relacher = fermer |

Pas de drag-to-place — trop de risques de conflits avec la camera pan. Le tap direct (BTD6 pattern) est plus previsible.

**Bottom sheet selecteur de tour** (si aucune tour pre-selectionnee) :

```
+-----------------------------------------------+
|                                               |
|  [drag indicator]                             |
|  Placer une tour                              |
|  +------+ +------+ +------+ +------+          |
|  | Feu  | | Glac | | Ecla | | .....|          |  <- 80×80px chacun + label
|  | 50c  | | 60c  | |  75c | |      |          |
|  +------+ +------+ +------+ +------+          |
|  [Annuler]                                    |
+-----------------------------------------------+
```

Sheet hauteur : 240px, handle drag. Swipe down = dismiss. Touch target icones tour : 80×80px (> 44px OK).

### 2.2 Camera pan / zoom

| Geste | Action |
|---|---|
| 1 doigt drag | Pan camera |
| 2 doigts pinch | Zoom in/out |
| 2 doigts rotation | Non implementee (hors scope Phase 4) |
| Double tap | Reset zoom niveau default |

Contrainte : distinguer 1 doigt "pan" de 1 doigt "tap tour". Resolution :

- Distance < 8px et duree < 200ms = **tap** (action)
- Distance >= 8px OU duree >= 200ms = **pan** (camera)
- Ce seuil (8px/200ms) est standard Unity `Touch.deltaPosition` / `EventSystem` threshold

### 2.3 Confirmation "Vendre tour"

**Desktop actuel** : bouton Vendre direct dans radial, pas de confirmation.

**Mobile** : le tap accidentel "Vendre" est un risque reel (bouton pleine largeur juste sous les upgrades). Solution :

- Tap "Vendre" => **modal de confirmation** (non swipe-to-confirm, trop complexe a implementer proprement en UIElements)
- Modal minimale : titre "Vendre [Nom Tour] ?" + valeur retour + bouton "Confirmer" (rouge) + bouton "Annuler" (secondaire)
- Touch targets boutons confirmation : minimum 88×44px chacun, espacement 12px entre eux

```
+--------------------------------+
|  Vendre Tour de Feu L2 ?       |
|  Retour : 80c                  |
|                                |
|  [ Annuler ]  [ Confirmer ]    |  <- Confirmer rouge, Annuler gris
+--------------------------------+
```

Animation : slide-up 250ms, backdrop 200ms fade-in. Fermeture : 180ms ease-in.

---

## 3. Settings Panel Mobile

### 3.1 Full-screen vs modal

**Desktop actuel** : modal centree 560px wide. Non compatible SE (375px < 560px).

**Mobile** : settings en **full-screen** sur mobile (< 768px wide). Pas de bordure decorative — prend toute la surface utile (safe-area incluse).

USS mobile :

```
.settings-panel {
    width: 100%;
    max-width: 100%;
    height: 100%;
    max-height: 100%;
    border-radius: 0;
    border-width: 0;
    padding: 0;
    padding-top: env(safe-area-inset-top, 0px);
    padding-bottom: env(safe-area-inset-bottom, 0px);
}

.settings-title {
    font-size: 22px;
    padding: 16px 20px 12px;
    border-bottom-color: rgba(100, 120, 160, 0.3);
    border-bottom-width: 1px;
}

.settings-scroll {
    flex-grow: 1;
    padding: 0 16px;
}
```

### 3.2 Rows touch-friendly

**Desktop actuel** : `setting-row` padding 4px 6px. Trop petit pour le tactile.

USS mobile :

```
.setting-row {
    min-height: 52px;    /* > 44px target */
    padding: 8px 12px;
    border-bottom-color: rgba(100, 120, 160, 0.1);
    border-bottom-width: 1px;
}

.setting-label {
    font-size: 16px;
    min-width: 130px;
}

.setting-value {
    font-size: 15px;
    min-width: 44px;
}

.setting-toggle {
    min-width: 52px;
    min-height: 32px;   /* Unity Toggle nativement > 32px — a verifier */
}
```

### 3.3 Sliders audio

Unity `Slider` : la thumb est la seule zone draggable. Sur mobile la thumb doit faire >= 44px. Wrapper custom requis :

```
.setting-slider {
    flex-grow: 1;
    margin: 0 10px;
    min-width: 120px;
    /* Unity USS : pas de --thumb-size natif en UIToolkit.
       Solution Phase 4 : surclasser via C# TouchManipulator ou custom VisualElement.
       Interim : laisser le slider natif, la zone tactile est acceptable sur la majorite
       des tailles d'ecran (thumb ~24px natif UIToolkit). Ticket separé recommande. */
}
```

Decision point : les sliders audio Unity UIToolkit ont une thumb natif ~24px. Sous 44px HIG. Phase 4 devra soit implementer un custom slider soit accepter cette limitation. A valider avec Mike.

### 3.4 Sections accessibilite en premier

Sur mobile, l'accessibilite est plus critique (taille d'ecran, conditions de jeu en exterieur). Recommandation : remonter la section Accessibilite en deuxieme position (juste apres Audio), avant Graphics et Language. Wireframe ordre :

1. Audio
2. Accessibilite (Large text, Reduce motion, Colorblind)
3. Graphics
4. Language

### 3.5 Bouton fermer

**Desktop actuel** : bouton "Close" en bas-droite du panel.

**Mobile** : icone X en haut-droite (standard iOS/Android), touch target 44×44px.

```
+-----------------------------------------------+
| [safe-area top]                               |
| Parametres                          [ X 44px ]|  <- header fixe
+-----------------------------------------------+
| [scroll content]                              |
|  Audio                                        |
|   Master     [======----]  80%                |
|   SFX        [========--]  100%               |
|   Musique    [=======---]  70%                |
|   Muet                           [ O ]        |
|                                               |
|  Accessibilite                                |
|   Daltonien                      [ O ]        |
|   Reduire animations             [ O ]        |
|   Grand texte                    [ O ]        |
|                                               |
|  ... scroll ...                               |
+-----------------------------------------------+
```

---

## 4. Responsive Layout

### 4.1 Portrait vs Landscape

**Portrait** (layout par defaut ci-dessus) : pills en haut, bouton vague en bas centree, jeu au milieu.

**Landscape** : l'espace vertical est reduit (~375px sur SE landscape). La zone de jeu doit maximiser l'espace vertical.

Layout landscape :

```
+-----------------------------------------------------------+
|[sf] [Or][WAVE][HP====]              [settings btn 44px] |  <- top bar 44px
+-----------------------------------------------------------+
|                                                          |
|              ZONE JEU 3D                                 |
|                                      +------------------+|
|                                      | [Lancer vague   ]||  <- 88×56px
+-----------------------------------------------------------+
|[sf bottom]                                               |
+-----------------------------------------------------------+
```

Changements landscape vs portrait :
- Wave launch btn : deplace en **bas-droite** (thumb zone), 88px wide × 56px height min (compromis vertical)
- Pills top-bar : hauteur reduite a 44px, font-size pill-value = 16px
- Radial menu : positionne a droite (`right: 16px; bottom: safe`) plutot que centree

Breakpoint : `height < 500px` = activation layout landscape. En Unity UIToolkit, detecter via `Screen.orientation` ou resolution dans `HudController`.

### 4.2 Safe Area Handling

Unity UIToolkit supporte `env(safe-area-inset-*)` via USS depuis Unity 2023+. A confirmer fonctionne en Unity 6 LTS.

Regles :

| Element | Inset a appliquer |
|---|---|
| Top-bar | `padding-top += safe-area-inset-top` |
| Wave launch btn | `bottom += safe-area-inset-bottom` |
| Radial menu | `bottom += safe-area-inset-bottom` |
| Settings panel (full-screen) | tous les quatre cotes |
| Overlay panels (game over, victory) | padding interne safe-area |

Valeurs typiques iOS :
- iPhone SE (3rd gen) : top 0px, bottom 0px (pas de notch)
- iPhone 15 : top 59px, bottom 34px
- iPhone 15 Pro Max : top 59px, bottom 34px

Fallback si `env()` non supporte : ajouter via C# `Screen.safeArea` et appliquer margin programmatiquement dans `HudController.Start()`.

### 4.3 Font Scaling

**Systeme** : 3 niveaux, toggle dans Settings > Accessibilite > Grand texte.

| Niveau | Multiplicateur | USS class |
|---|---|---|
| Normal (defaut) | 1.0x | `.font-normal` |
| Moyen | 1.15x | `.font-medium` |
| Grand | 1.35x | `.font-large` |

Implementation : classe sur `hud-root`, overrides via USS child selectors :

```
.font-large .pill-value { font-size: 22px; }     /* 18 × 1.22 */
.font-large .pill-label { font-size: 12px; }
.font-large .wave-launch-label { font-size: 24px; }
.font-large .wave-launch-sub { font-size: 16px; }
.font-large .radial-btn-label { font-size: 20px; }
.font-large .tooltip-header { font-size: 18px; }
.font-large .tooltip-stats { font-size: 15px; }
.font-large .section-title { font-size: 19px; }
.font-large .setting-label { font-size: 18px; }
```

---

## 5. Accessibilite

### 5.1 Contraste WCAG AA

| Element | Fond | Texte | Ratio actuel | Conforme AA ? |
|---|---|---|---|---|
| pill-label | rgba(0,0,0,0.65) | rgba(220,220,220,0.7) | ~5.1:1 | OK |
| pill-value (or) | rgba(0,0,0,0.65) | rgb(255,210,63) | ~8.2:1 | OK |
| pill-value (blanc) | rgba(0,0,0,0.65) | white | ~13.5:1 | OK |
| wave-launch-label | rgb(240,160,32) | rgb(26,26,26) | ~4.6:1 | OK (minimum 4.5:1) |
| wave-launch-sub | rgb(240,160,32) | rgba(26,26,26,0.75) | ~3.4:1 | ECHEC AA (< 4.5:1) |
| radial-btn-label | rgba(200,80,20,0.85) | white | ~4.9:1 | OK |
| radial-btn-hint | rgba(200,80,20,0.85) | rgba(200,200,200,0.75) | ~3.1:1 | ECHEC AA |
| tooltip-stats | rgba(10,14,20,0.92) | rgba(200,210,230,0.85) | ~9.8:1 | OK |
| settings-panel bg | rgba(20,26,38,0.97) | rgb(220,230,240) | ~12.4:1 | OK |

**Corrections requises** :
1. `wave-launch-sub` : opacite texte 0.75 -> 1.0, soit `rgb(26,26,26)` direct. Ratio passe a ~4.6:1.
2. `radial-btn-hint` : opacite 0.75 -> 1.0 ou fond plus sombre. Sur DPS (fond ~C85014) avec blanc pur : ~4.9:1 OK. Sur Utility (fond ~2828B4) avec blanc pur : ~7.1:1 OK.

### 5.2 Mode Daltonien

Couleurs problematiques rouge/vert dans le HUD :

| Element | Couleur actuelle | Alternative daltonien |
|---|---|---|
| hp-bar-fill (> 60%) | rgb(80,220,80) vert | rgb(0,114,255) bleu (Deuteranopie-safe) |
| hp-bar-fill (30-60%) | rgb(219,140,33) orange | rgb(218,165,32) — acceptable |
| hp-bar-fill (< 30%) | rgb(219,51,33) rouge | rgb(255,140,0) orange-jaune |
| radial-btn-dps | rouge-orange | garder (reinforcement par label textuel) |
| radial-btn-utility | bleu-violet | garder |
| streak badge | rgb(95,224,121) vert | rgb(0,120,255) bleu |

Toggle "Daltonien" dans settings applique classe `.colorblind` sur `hud-root`. USS overrides :

```
.colorblind .hp-bar-fill-high { background-color: rgb(0, 114, 255); }
.colorblind .hp-bar-fill-mid  { background-color: rgb(218, 165, 32); }
.colorblind .hp-bar-fill-low  { background-color: rgb(255, 140, 0); }
.colorblind .wave-launch-streak { background-color: rgb(0, 120, 255); }
```

### 5.3 Reduce Motion

Toggle "Reduire animations" desactive :
- Animations entree/sortie wave-launch btn (apparition immediate, display toggle direct)
- Animations entree/sortie radial menu
- Animation streak badge (scale pop)
- Overlay transitions

Les transitions de gameplay (deplacement ennemis, projectiles) ne sont PAS affectees — hors perimetre HUD.

### 5.4 Focus visible

Sur mobile, le focus clavier (clavier externe Bluetooth) doit etre visible. UIToolkit applique `:focus` par defaut avec outline. A verifier en Phase 4 que l'outline est visible sur les fonds sombres du HUD. Recommandation : outline `2px solid rgb(255,210,63)` (couleur or — bon contraste sur tous les fonds du HUD).

---

## 6. Animation Budget

| Animation | Duree | Easing | Notes |
|---|---|---|---|
| Wave btn entree | 280ms | ease-out | slide-up + fade |
| Wave btn sortie | 180ms | ease-in | fade + scale-down |
| Streak badge pop | 200ms (+ 80ms delay) | ease-out | scale 0->1 |
| Pill skip entree | 250ms | ease-out | fade-in |
| Pill skip sortie | 160ms | ease-in | fade-out |
| Radial menu entree | 250ms | ease-out | slide-up |
| Radial menu sortie | 200ms | ease-in | slide-down |
| Tooltip entree | 200ms | ease-out | fade-in |
| Overlay (game over/victory) | 300ms | ease-out | fade-in backdrop |
| Settings panel entree | 280ms | ease-out | slide-up full-screen |
| Settings panel sortie | 220ms | ease-in | slide-down |
| Confirmation sell entree | 250ms | ease-out | slide-up |
| Confirmation sell sortie | 180ms | ease-in | fade |

Toutes durees <= 400ms. Budget perf : animations CSS-only via Unity UIToolkit USS transitions, zero allocation C# en hot path.

---

## 7. Touch Target Audit

| Element | Taille actuelle | Taille mobile cible | HIG compliant ? |
|---|---|---|---|
| Wave launch btn | 240×60px | 320×88px | Oui |
| Radial btn DPS | 148×76px | flex×88px | Oui |
| Radial btn Utility | 148×76px | flex×88px | Oui |
| Radial btn Sell | 340×36px | 100%×56px | Oui (56 >= 44) |
| Overlay btn Recommencer | padding 16 48 | 88×52px min | Oui |
| Settings close (X) | n/a (desktop: "Close" btn) | 44×44px | Oui (minimum) |
| Settings row toggle | natif toggle | 52px row height | Oui |
| Pill Gold (info seule) | 100px | 88×52px | OK (non-interactive) |
| Streak badge (affichage) | ~32px | non-interactive | N/A |

---

## 8. Decision Points pour Mike

1. **Bottom sheet selecteur de tour** : implique refactoring `TowerToolbar` (non present dans HUD.uxml actuel). A ajouter en Phase 4 ou garder la toolbar desktop avec adaptation scroll ?

2. **Sliders audio** : thumb size Unity UIToolkit ~24px non conforme HIG 44px. Option A : custom VisualElement slider. Option B : accepter compromise (les sliders audio sont rarement utilisees in-game). Recommandation : option B pour Phase 4, ticket separe.

3. **Orientation lock** : iOS/Android permettent de locker en portrait ou autoriser les deux. Lock portrait seul simplifie le layout (un seul a designer/tester). Recommandation : portrait only pour Phase 4 ship, landscape Phase 5.

4. **`env(safe-area-inset-*)` en Unity 6** : a confirmer via test device reel. Fallback `Screen.safeArea` en C# est certain de fonctionner.

5. **Confirmation vendre** : la modal de confirmation (Section 2.3) n'existe pas dans le code actuel. Nouveau composant UIElements requis — ticket Phase 4.

6. **Wave launch btn landscape** : compromis 88×56px (non conforme HIG 88px carre). Alternative : augmenter a 88×88px et accepter qu'il masque plus de zone de jeu. A valider.

---

## Fichiers impactes (Phase 4 implementation)

| Fichier | Modification |
|---|---|
| `Assets/UI/HUD.uss` | Ajout section `/* === Mobile (pointer: coarse) === */` avec toutes les overrides |
| `Assets/UI/RadialMenu.uss` | Ajout section mobile : `.radial-btn` height 88px, `.radial-btn-sell` height 56px |
| `Assets/UI/SettingsPanel.uss` | Ajout section mobile : full-screen, rows 52px, bouton X |
| `Assets/UI/HUD.uxml` | Ajout element `sell-confirm-modal` (nouveau) |
| `Assets/Scripts/UI/HudController.cs` | Ajout `ApplyMobileLayout()`, safe-area via `Screen.safeArea`, `sell-confirm-modal` logic |
| `Assets/UI/SettingsPanel.uxml` | Ajout bouton X fermeture, reorder sections Accessibilite avant Graphics |
