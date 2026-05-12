# R6-PARITY-019 — Schools mapping V4 ↔ V6 audit

**Date** : 2026-05-12 — **Type** : audit lecture seule (no code change)
**Sources** : `milan project/src-v3/data/schools.js` + `src-v3/data/perks.js` (SET_BONUSES) ↔ `Assets/Editor/BuildSchoolAssets.cs` + `Assets/Scripts/Data/PerkDef.cs` (enum School + PerkTag)

## V4 (src-v3) — 3 schools formelles + 6 tags

V4 distingue **schools** (formal class, starter tower, exclusive perks) et **tags** (set bonus 3-of-kind).

| School (V4) | Icon | Color | Starter tower | Set bonus tag | Exclusive perks |
|---|---|---|---|---|---|
| feu | 🔥 | `#ff5020` | mage | feu → Brasier (+20% cadence) | combustion, pyromancie |
| givre | 🧊 | `#5fbcff` | frost | vide → Néant (1/4 proj AoE) | glaciation, cristal_glace |
| maconnerie | 🪨 | `#a08060` | tank | pierre → Forteresse (+20% PV château) | forteresse_perk, murs_pierre |

**Tags V4 (6 total)** : `foudre` (Tempête +15% crit), `sang` (Carmin +1 PV/kill lifesteal), `pierre` (Forteresse), `feu` (Brasier), `vide` (Néant), `or` (Pactole +30% gold/kill). Les 3 tags non-school (`foudre`/`sang`/`or`) sont activables uniquement via perks tagués, sans starter tower dédié.

## V6 — 5 schools

| School (V6) | Theme | Unlock cost | Description |
|---|---|---|---|
| elementaire | green | 0 | Foudre, feu, glace — DPS magie naturelle |
| mecanique | brown | 100 | Ingénierie, fortification, auras |
| mystique | purple | 200 | Vide, magie noire, piercing/ricochet/AoE |
| bestiaire | red | 300 | Sang, lifesteal, vitesse, attaques en chaîne |
| strategie | gold | 400 | Économie, contrôle, gains de pièces |

**Note clé** : `PerkDef.cs` préserve `enum PerkTag { None, Foudre, Sang, Pierre, Feu, Vide, Or }` → les 6 tags V4 survivent intacts pour set bonuses, indépendamment des schools.

## Mapping V4 → V6

| V4 school/tag | V6 school | Type | Notes |
|---|---|---|---|
| feu (school) | elementaire | merged | feu absorbé dans thème éléments |
| givre (school) | elementaire | merged | glace = élément |
| maconnerie (school) | mecanique | 1:1 | tank/fortification → ingénierie |
| foudre (tag only) | elementaire | tag preserved | foudre = élément + PerkTag.Foudre |
| vide (tag only) | mystique | tag preserved | vide → mystique (piercing/AoE) + PerkTag.Vide |
| sang (tag only) | bestiaire | tag preserved | sang/lifesteal → bestiaire + PerkTag.Sang |
| or (tag only) | strategie | tag preserved | économie → strategie + PerkTag.Or |

**Zéro perte sémantique** : les 6 V4 tags → couverts par 5 V6 schools + PerkTag enum. Les 3 V4 schools "formelles" se réduisent à 2 V6 schools (elementaire absorbe feu+givre, mecanique = maconnerie). Les 3 V4 tags-sans-school deviennent 3 V6 schools de plein droit (mystique/bestiaire/strategie) → **élargissement, pas réduction**.

## Top 3 "gameplay losses" potentiels

1. **Starter tower mapping** : V4 lie school → starter (feu→mage, givre→frost, maconnerie→tank). V6 SchoolDef n'a pas de champ `starterTowerType`. **Impact** : faible — peut être reconstruit côté `Hero.cs` Init via un dict id→towerType.
2. **Exclusive perks par school** : V4 a `school.exclusivePerks: [...]`. V6 utilise `PerkDef.school` (string) pour filtrer côté perk → mapping inverse, équivalent fonctionnel. **Impact** : nul.
3. **Set bonus auto à pick de school** : V4 `applySchoolSetBonus()` auto-active le set bonus du tag lié dès qu'on choisit la school. V6 n'a pas (encore ?) cette mécanique d'auto-activation. **Impact** : moyen — UX moins gratifiante, mais système de tags 3-of-kind reste fonctionnel via perks normaux.

## Recommandation

**KEEP V6 5 SCHOOLS** (décision Mike validée pendant pivot R6-PARITY-V4, confirmée par audit).

Justification :
- Couverture sémantique V4 → V6 = **complète** (0 tag perdu via `PerkTag` enum)
- V6 élargit en fait l'espace stratégique (5 schools vs 3 schools formelles V4)
- Aucun "6e school" critique manquant : les 6 V4 tags sont déjà tous représentés (elementaire/mecanique/mystique/bestiaire/strategie + PerkTag preservation)

**Follow-ups optionnels (non-bloquants, hors scope cet audit)** :
- ajouter `starterTowerType` dans `SchoolDef.cs` si on garde le mapping school→starter au pick (R6-PARITY-020-FUTURE)
- restaurer `applySchoolSetBonus()` auto-trigger côté `PerkSystem.cs` si UX confirme valeur (R6-PARITY-021-FUTURE)

**Pas de 6e school recommandé.**
