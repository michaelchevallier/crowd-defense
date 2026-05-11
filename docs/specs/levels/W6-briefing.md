# W6 — Apocalypse (RUPTURE-difficulté)

**Theme** : `apocalypse`
**Levels** : W6-1 (Cendres Chaudes) → W6-7 (Ruines Sombres) → W6-8 (Boss : Confrontation Finale)
**Castle HP range** : 235-310 (post-fix gap-report)
**Mob roster** : Basic, Brute, Shielded, Flyer, Imp, **ApocalypseBoss**

## Lore-light

Le monde a brûlé. Cendres et fumée masquent le ciel. Le château se dresse seul, dernier vestige. Quelque chose remonte des décombres — il sait que le joueur est ici, et il a tout son temps.

## Narrative hook (CRITICAL — Mike rupture demandée D1-04)

W6 est **la rupture de difficulté arbitrée par Mike** (D1-04 §1.1). Sentiment cible : *"Tu peux te tromper 1-2 fois mais après tu meurs."*

Implémentation :
- **No-regen castle HP entre waves** (`waveRegen = 0` runtime, cf BalanceConfig.NoRegenWorldThreshold = 6)
- **Pression mob +34% composite** vs W5 (mobHpMul 1.40→1.65, mobCountMul 1.20→1.30, mobSpeedMul 1.05→1.10)
- **Châteaux corrigés post-gap-report** : ratio 1.06-1.10 vs spec (anciennement 1.26-1.54, surdimensionnés — rupture annulée par accident)

## Gameplay focus

- **W6-1 à W6-3** : intro rupture. Le joueur survit s'il a tout maîtrisé W1-W5. Sinon : early grave.
- **W6-4 à W6-7** : escalation rapide. Multi-portails arrivent (cf maps), bridges sur ravins de cendre.
- **W6-8 Boss ApocalypseBoss "Confrontation Finale"** : mob unique HP géant + spawns + AOE. **Cible D1-04 50-70% fail première tentative.**

## Décor

- Sol : roche brûlée, cendre grise/orange (`#3a2a25` + `#c97050`)
- Eléments : squelettes de bâtiments effondrés, brasiers résiduels (`L` reskinned), pylônes de fer tordu
- Castle : forteresse fortifiée d'urgence (barricades visibles)
- Ennemis : silhouettes brûlées, créatures de cendres, apocalypse boss titanesque

## Localization keys recommandés

- `level.w6_1.briefing` à `level.w6_8.briefing`
- `level.w6.world_intro` (texte fort : annonce la rupture difficulté)
- `enemy.apocalypse_boss.flavor`
- `hud.warning.no_regen_w6plus` (Axis F UX hint affiché 1× quand le joueur entre W6 — D1-04 §7 mitigation risque)
