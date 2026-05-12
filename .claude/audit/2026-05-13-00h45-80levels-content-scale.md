# Audit — Content scale 80 levels (target) vs réel 90

**Date** : 2026-05-13 00h45
**Ticket** : N6 (follow-up W4-T7)
**Mode** : LECTURE SEULE

## Résumé

- **Levels trouvés** : **90** (10 worlds × 9 levels) — target ticket = 80, réalité = 90 (9 par monde, pas 8). Cf `STATUS.md` à clarifier.
- **Repartition** : 40 OK / 50 WARN / 0 FAIL
- **Castle HP** : 100% des levels ≥ 100 (min = W2-1 à 130)
- **Cutscenes gates** : 10/10 (`world1`..`world10` wired sur W1-1..W10-1 — cf W3-V2 c36b580 confirmé)
- **Topology** : 100% ont `P` (path) + `C` (castle), aucune map vide

## Critères audit

| Critère | Seuil FAIL | Seuil WARN |
|---|---|---|
| `castleHP` | < 100 | — |
| `waves[].Count` | < 3 | > 5 |
| `wave1Enemies` | — | > 100 |
| `maxWaveEnemies` | — | > 200 |
| `cutsceneIdAtStart` gate (L1) | absent | — |
| `mapRows` topology | sans path/castle | — |

## Anomalies par catégorie

### Waves > 5 (design choice ?)
W1-3..W1-9 (6 waves), W2-8/W2-9, W3-8/W3-9, W4-8/W4-9, W5-4..W5-9 (6-10), W6-9, W7-9, W8-9, W9-9, W10-9.
**Note** : tous les `W*-9` ont 10 waves (boss levels intentionnels ?).

### Wave 1 enemies > 100 (curve trop dure dès start)
- W5-1 (130), W5-2 (150), W5-4 (109), W5-6 (150)
- W6-7 (128), W6-8 (150)
- W7-5 (140), W7-7 (120), W7-8 (150)
- W8-5 (150), W8-7 (128), W8-8 (150)
- **W9 entier** : W9-1..W9-9 tous 112-150 enemies wave 1
- **W10 entier** : W10-1..W10-9 tous 90-150 enemies wave 1

### Max wave enemies > 200
- W9-9 (204), W10-9 (236)

## Top 5 fixes recommandés

1. **W9/W10 wave1 ramping** (15 levels) : 150 enemies wave 1 = pas de ramp-up, joueur submergé instantanément. Recommandation : 60-80 max wave 1, scale jusqu'à 150 en wave 4-5.
2. **W*-9 boss levels (10 levels)** : 10 waves vs 3-5 spec. Soit officialiser comme "boss endurance levels" dans le design doc, soit réduire à 5-6 waves max.
3. **W2-5 / W2-6 difficulty spike** : 112-104 enemies wave 1 en early W2 → diff curve cassée vs W1 (35-58).
4. **W4-1..W4-3 trop faciles** : total 174-187 enemies (vs W3-7 = 398). Mid-game dip détecté.
5. **CastleHP W2 baisse vs W1** : W1-9 = 160 HP, W2-1 = 130 HP (régression). Confirmer si BalanceConfig formula intentionnelle ou override manquant.

## Note sur "80 levels"

Le ticket dit "80 levels (10 worlds × 8 niveaux)" mais le repo a **9 levels par monde** (W*-1 à W*-9), soit 90. À clarifier avec Mike : retire 1 level/monde (lequel ?) ou aligne le target à 90.

## Cutscene gates (W3-V2 c36b580 verification)

| World | Gate L1 | cutsceneId |
|---|---|---|
| W1-1 | OK | `world1` |
| W2-1 | OK | `world2` |
| W3-1 | OK | `world3` |
| W4-1 | OK | `world4` |
| W5-1 | OK | `world5` |
| W6-1 | OK | `world6` |
| W7-1 | OK | `world7` |
| W8-1 | OK | `world8` |
| W9-1 | OK | `world9` |
| W10-1 | OK | `world10` |

Tous les 10 gates wired. W3-V2 OK.

## Conclusion

Pas de FAIL bloquant. 50 WARN sont des choix design potentiellement intentionnels (boss waves, late-game intensity) à valider design-side. Pas de fix urgent — playtest W5+ pour valider la difficulty curve avant rebalancing.
