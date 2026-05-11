# W10 — Néo-Tokyo (Cyber-finale)

**Theme** : `cyberpunk`
**Levels** : W10-1 (Ruelle) → W10-7 (Tour CEO) → W10-8 (Boss : Hub IA)
**Castle HP range** : 258-360 (post-fix gap-report)
**Mob roster** : **CyberBasic**, **CyberRunner**, **CyberBrute**, **CyberFlyer**, **AiHub** (boss final)
**Wave count** : 4-5 typique, W10-8 = 4 waves dont 1 = boss seul

## Lore-light

Néo-Tokyo, dernière ligne. Le château est devenu un nœud de réseau, la défense du dernier serveur libre. Les drones cyber-augmentés convergent depuis toutes les directions. Au centre de la toile : l'IA. Si elle prend le serveur, le jeu est terminé.

## Narrative hook

W10 = **fin de campagne**. Tous les mécanismes maîtrisés, toutes les tours débloquées, toutes les synergies disponibles. Le joueur affronte une déclinaison futuriste de chaque mob de base, en version "Cyber" upgradée.

## Gameplay focus

- **W10-1 à W10-3** : intro cyberpunk avec mobs Cyber-{Basic,Runner,Brute,Flyer}. Décor pluie + néons.
- **W10-4 à W10-5** : densification. 600+ mobs par level. Stress test build complet.
- **W10-6 à W10-7** : multi-portails 4P, ratio HP/pression au max. Châteaux corrigés (W10-7 320 vs old 400) post-gap-report.
- **W10-8 Boss AiHub "Hub IA"** : mob unique boss final + 3 waves d'escortes massives. Châteaux corrigé 360 (vs old 450). Cible D1-04 50-70% fail première tentative.

## Décor

- Sol : asphalte sombre + flaques + néons (`#0a0a1a` + `#ff00ff` accents cyan/magenta)
- Eléments : grands buildings verticaux, pylônes électriques, panneaux holographiques, drones flottants
- Castle : tour serveur cubique avec LEDs et antennes paraboliques
- Ennemis : silhouettes cyber-augmentées (yeux LED, prothèses métalliques), AiHub mass-conscient flottant

## Localization keys recommandés

- `level.w10_1.briefing` à `level.w10_8.briefing`
- `level.w10.world_intro` (titre fort : "Le dernier serveur")
- `enemy.cyber_basic.flavor`, `enemy.cyber_runner.flavor`, `enemy.cyber_brute.flavor`, `enemy.cyber_flyer.flavor`
- `enemy.ai_hub.flavor` (boss final, texte épique)
- `hud.campaign_end.text` (post W10-8 cleared, écran de victoire + crédits courts)
