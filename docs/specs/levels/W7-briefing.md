# W7 — Espace (Vide-mobilité)

**Theme** : `espace`
**Levels** : W7-1 (Orbite basse) → W7-7 (Anneau extérieur) → W7-8 (Boss : Galactique)
**Castle HP range** : 220-350
**Mob roster** : Flyer, Basic adapté, **CorsairBoss**, multi-portails plus communs

## Lore-light

Stations spatiales abandonnées dérivent dans le vide. Le château est devenu un module orbital — fragile, exposé, sans atmosphère. Les corsaires interstellaires y voient une cible facile.

## Narrative hook

W7 = première opportunité **`allowMultiMagnet: true`** (Q3 décision Mike). Multi-portails fréquents dans les maps = besoin de plus de magnet pour économie. Le joueur découvre la stratégie multi-magnet sur W7-3 et W7-7 (recommandé level-designer pass).

## Gameplay focus

- **W7-1 à W7-3** : décor open, mob vitesse moyenne. Maps avec plusieurs portails.
- **W7-4 à W7-5** : Flyer densité augmentée — domaine Skyguard.
- **W7-6 à W7-7** : multi-portails 3-4P, **opt-in `allowMultiMagnet`** sur ces 2 levels (recommandation).
- **W7-8 Boss CorsairBoss "Galactique"** : mob unique vol + canon laser longue portée — apprends la défense multi-cible.

## Décor

- Sol : panneaux métalliques industriels, néons bleus (`#1a2a4a` + `#4ac9ff`)
- Eléments : antennes, satellites cassés (skybox spatial étoilé), sas étanches
- Castle : module orbital cubique avec turbines
- Ennemis : corsaires en armure spatiale, drones, vaisseaux légers

## Localization keys recommandés

- `level.w7_1.briefing` à `level.w7_8.briefing`
- `level.w7.world_intro`
- `enemy.corsair_boss.flavor`
- `hint.allow_multi_magnet.w7` (Axis F UX hint au premier level multi-magnet pour expliquer cap 2)
