# W2 — Forêt (Vert-densification)

**Theme** : `foret`
**Levels** : W2-1 (Lisière) → W2-7 (Cœur sauvage) → W2-8 (Boss : Sorcier)
**Castle HP range** : 160-180
**Mob roster** : Basic, Runner, Brute, **ForestBee** (volant), **ForestBrute** (variante terrestre)

## Lore-light

Au-delà de la plaine, une forêt ancestrale serre ses arbres autour des chemins. Des créatures s'éveillent : abeilles géantes qui survolent les défenses, brutes des bois plus robustes que leurs cousines de plaine. Un sorcier solitaire arpente les sentiers — il sait ce que le joueur défend.

## Narrative hook

La forêt **introduit les volants** (ForestBee). Première leçon : toutes les tours ne ciblent pas le ciel. Le joueur doit choisir Skyguard ou utility (Mage, Fan).

## Gameplay focus

- **W2-1 à W2-3** : densification — plus de mobs par wave (286-313 mobs total vs 250 W1).
- **W2-4 à W2-5** : ForestBee introduit. Skyguard becomes meta (cf TowerType Skyguard `unlockWorld`).
- **W2-6 à W2-7** : ForestBrute renforce les mobs ground. Apprentissage de combinaison terrestre + aérien.
- **W2-8 Boss Sorcier** : Midboss (mob unique HP très élevé + abilités). Premier vrai test du joueur.

## Décor

- Sol : herbe + sentier terre battue (`#3c5a28` + `#6a5232`)
- Eléments : arbres densément placés (`T`), bridge possible (`~`) sur cours d'eau (`W`)
- Castle : medieval avec moulin/grange (variante rurale)
- Ennemis : silhouettes humanoïdes verdâtres + insectes XL volants

## Localization keys recommandés

- `level.w2_1.briefing` à `level.w2_8.briefing`
- `level.w2.world_intro`
- `enemy.forest_bee.flavor`, `enemy.forest_brute.flavor`, `enemy.midboss.flavor`
