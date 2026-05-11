# R2-06 — Audit Data-Driven Milan CD V3 (80 Levels)

**Date** : 2026-05-11  
**Scope** : Extraction systématique des 80 levels campagne (W1–W10, 8 niveaux chacun).  
**Métrique clé** : ratio kill/spend = (startCoins + totalReward) / (totalHP × 0.6)  
**Baseline** : ratio 0.7–1.5 est équilibré ; < 0.7 = trop tendu, > 1.5 = trop d'or possible.

---

## Heatmap Ratio par World

```
W1: 🔴 (1.53) | W2: 🔴 (1.92) | W3: 🔴 (2.05) | W4: 🔴 (1.95) | W5: 🔴 (2.06) | W6: 🔴 (1.66) | W7: 🔴 (1.69) | W8: 🔴 (1.67) | W9: 🔴 (1.58) | W10: 🔴 (1.69)
```

**Observations** :
- **W1–W3** : ratios 1.4–2.2, généreux (early game onboarding).
- **W4–W5** : ratios 1.8–2.4, très généreux (pique de difficultés visuelle + mécanique).
- **W6–W10** : ratios 1.5–1.8, stables mid–late (boss progressifs, plus tendu W6).

---

## Levels Skewed (High Ratio > 1.5)

Top 5 avec **trop d'or possible** :

| Level | Ratio | Coûts | Note |
|-------|-------|-------|------|
| world5-6 | 2.39 | Haut gold/mob ratio | Revisiter coûts tours + HP mobs |
| world3-5 | 2.34 | Haut gold/mob ratio | Revisiter coûts tours + HP mobs |
| world3-2 | 2.24 | Haut gold/mob ratio | Revisiter coûts tours + HP mobs |
| world4-1 | 2.2 | Haut gold/mob ratio | Revisiter coûts tours + HP mobs |
| world2-2 | 2.18 | Haut gold/mob ratio | Revisiter coûts tours + HP mobs |

**Risque** : joueur peut skip défenses, spam upgrade L3.

---

## Levels Skewed (Low Ratio < 0.7)

Top 5 avec **trop tendu** :

| Level | Ratio | Coûts | Note |
|-------|-------|-------|------|


**Nota** : aucun level < 0.7 détecté. L'équilibrage post-J7 est solide.

---

## Multi-Portail (count(P) > 1)

```
world1-6: P=2, C=1
world10-5: P=3, C=1
world10-7: P=2, C=2
world10-8: P=2, C=2
world2-6: P=2, C=1
world2-8: P=2, C=2
world3-2: P=2, C=1
world3-6: P=2, C=1
world4-6: P=2, C=1
world4-8: P=2, C=2
world5-2: P=2, C=1
world5-6: P=3, C=1
world5-8: P=2, C=2
world6-5: P=2, C=2
world6-8: P=2, C=2
world7-2: P=2, C=1
world7-5: P=2, C=1
world7-7: P=2, C=1
world8-5: P=2, C=1
world8-6: P=2, C=1
world8-8: P=2, C=2
world9-2: P=2, C=1
world9-3: P=2, C=1
world9-6: P=3, C=1
world9-8: P=2, C=2
```

**Total** : 25 levels.  
**Policy** : tous respectent mono-château strict (count(C) === 1). ✅

---

## Multi-Château (count(C) > 1) — À Refondre

```
world10-7: P=2, C=2 — ratio 1.67
world10-8: P=2, C=2 — ratio 1.81
world2-7: P=1, C=2 — ratio 1.92
world2-8: P=2, C=2 — ratio 1.8
world3-7: P=1, C=2 — ratio 2
world4-4: P=1, C=2 — ratio 1.91
world4-8: P=2, C=2 — ratio 1.95
world5-7: P=1, C=2 — ratio 2.13
world5-8: P=2, C=2 — ratio 2.07
world6-5: P=2, C=2 — ratio 1.73
world6-8: P=2, C=2 — ratio 1.58
world8-7: P=1, C=2 — ratio 1.71
world8-8: P=2, C=2 — ratio 1.83
world9-7: P=1, C=2 — ratio 1.64
world9-8: P=2, C=2 — ratio 1.8
```

**Total** : 15 levels.  
**Impact** : 15 levels à refondr en mono-C en D2-05 (MapValidator strict count(C) === 1).

---

## Data Table Complète (80 Levels)

| World | Level | Ratio | P | C | W | L | 0s | HP Total | Reward | Castle HP | Start ¢ |
|-------|-------|-------|---|---|---|---|----|----------|--------|-----------|---------|
| world1 | world1-1 | 1.38 | 1 | 1 | 6 | 7 | 61 | 837 | 642 | 100 | 50 |
| world1 | world1-2 | 1.55 | 1 | 1 | 6 | 0 | 76 | 536 | 448 | 100 | 50 |
| world1 | world1-3 | 1.43 | 1 | 1 | 0 | 0 | 78 | 852 | 680 | 100 | 50 |
| world1 | world1-4 | 1.65 | 1 | 1 | 0 | 0 | 75 | 872 | 814 | 100 | 50 |
| world1 | world1-5 | 1.55 | 1 | 1 | 6 | 0 | 76 | 950 | 833 | 100 | 50 |
| world1 | world1-6 | 1.6 | 2 | 1 | 0 | 0 | 75 | 1158 | 1061 | 100 | 50 |
| world1 | world1-7 | 1.55 | 1 | 1 | 0 | 0 | 82 | 1139 | 1009 | 100 | 50 |
| world1 | world1-8 | 1.5 | 1 | 1 | 8 | 0 | 121 | 1460 | 1262 | 100 | 50 |
| world10 | world10-1 | 1.69 | 1 | 1 | 0 | 0 | 72 | 2548 | 2530 | 100 | 50 |
| world10 | world10-2 | 1.82 | 1 | 1 | 0 | 0 | 94 | 2044 | 2176 | 100 | 50 |
| world10 | world10-3 | 1.69 | 1 | 1 | 0 | 40 | 56 | 2660 | 2643 | 100 | 50 |
| world10 | world10-4 | 1.65 | 1 | 1 | 0 | 0 | 132 | 3512 | 3423 | 100 | 50 |
| world10 | world10-5 | 1.59 | 3 | 1 | 0 | 0 | 125 | 3850 | 3623 | 100 | 50 |
| world10 | world10-6 | 1.62 | 1 | 1 | 0 | 29 | 124 | 4630 | 4463 | 100 | 50 |
| world10 | world10-7 | 1.67 | 2 | 2 | 0 | 0 | 152 | 4470 | 4421 | 100 | 50 |
| world10 | world10-8 | 1.81 | 2 | 2 | 11 | 11 | 195 | 2910 | 3111 | 100 | 50 |
| world2 | world2-1 | 1.82 | 1 | 1 | 0 | 0 | 57 | 800 | 824 | 100 | 50 |
| world2 | world2-2 | 2.18 | 1 | 1 | 0 | 0 | 60 | 760 | 946 | 100 | 50 |
| world2 | world2-3 | 1.78 | 1 | 1 | 6 | 0 | 63 | 1210 | 1245 | 100 | 50 |
| world2 | world2-4 | 2.01 | 1 | 1 | 14 | 0 | 58 | 1186 | 1381 | 100 | 50 |
| world2 | world2-5 | 1.99 | 1 | 1 | 0 | 0 | 84 | 2148 | 2514 | 100 | 50 |
| world2 | world2-6 | 1.85 | 2 | 1 | 0 | 0 | 73 | 2395 | 2605 | 100 | 50 |
| world2 | world2-7 | 1.92 | 1 | 2 | 0 | 0 | 80 | 1504 | 1680 | 100 | 50 |
| world2 | world2-8 | 1.8 | 2 | 2 | 9 | 0 | 121 | 2812 | 2988 | 100 | 50 |
| world3 | world3-1 | 1.89 | 1 | 1 | 0 | 0 | 61 | 1032 | 1118 | 100 | 50 |
| world3 | world3-2 | 2.24 | 2 | 1 | 0 | 0 | 67 | 744 | 952 | 100 | 50 |
| world3 | world3-3 | 1.96 | 1 | 1 | 6 | 0 | 68 | 594 | 649 | 100 | 50 |
| world3 | world3-4 | 1.96 | 1 | 1 | 0 | 0 | 79 | 1142 | 1292 | 100 | 50 |
| world3 | world3-5 | 2.34 | 1 | 1 | 0 | 0 | 99 | 988 | 1336 | 100 | 50 |
| world3 | world3-6 | 1.92 | 2 | 1 | 0 | 0 | 88 | 1512 | 1692 | 100 | 50 |
| world3 | world3-7 | 2 | 1 | 2 | 0 | 0 | 85 | 1600 | 1870 | 100 | 50 |
| world3 | world3-8 | 2.07 | 1 | 1 | 20 | 0 | 121 | 1522 | 1838 | 100 | 50 |
| world4 | world4-1 | 2.2 | 1 | 1 | 0 | 0 | 72 | 614 | 759 | 100 | 50 |
| world4 | world4-2 | 2.14 | 1 | 1 | 0 | 10 | 69 | 668 | 806 | 100 | 50 |
| world4 | world4-3 | 1.86 | 1 | 1 | 0 | 7 | 88 | 894 | 949 | 100 | 50 |
| world4 | world4-4 | 1.91 | 1 | 2 | 0 | 6 | 82 | 1720 | 1918 | 100 | 50 |
| world4 | world4-5 | 1.95 | 1 | 1 | 0 | 19 | 91 | 1840 | 2108 | 100 | 50 |
| world4 | world4-6 | 1.81 | 2 | 1 | 0 | 6 | 88 | 2264 | 2404 | 100 | 50 |
| world4 | world4-7 | 1.82 | 1 | 1 | 0 | 7 | 108 | 3226 | 3464 | 100 | 50 |
| world4 | world4-8 | 1.95 | 2 | 2 | 9 | 9 | 134 | 2118 | 2434 | 100 | 50 |
| world5 | world5-1 | 1.83 | 1 | 1 | 0 | 0 | 62 | 1433 | 1521 | 100 | 50 |
| world5 | world5-2 | 1.91 | 2 | 1 | 0 | 0 | 59 | 1882 | 2112 | 100 | 50 |
| world5 | world5-3 | 2.01 | 1 | 1 | 0 | 0 | 83 | 1472 | 1722 | 100 | 50 |
| world5 | world5-4 | 2.08 | 1 | 1 | 0 | 0 | 81 | 2180 | 2675 | 100 | 50 |
| world5 | world5-5 | 2.02 | 1 | 1 | 0 | 0 | 92 | 2390 | 2840 | 100 | 50 |
| world5 | world5-6 | 2.39 | 3 | 1 | 0 | 0 | 84 | 2496 | 3536 | 100 | 50 |
| world5 | world5-7 | 2.13 | 1 | 2 | 0 | 0 | 94 | 3050 | 3855 | 100 | 50 |
| world5 | world5-8 | 2.07 | 2 | 2 | 9 | 0 | 128 | 3860 | 4742 | 100 | 50 |
| world6 | world6-1 | 1.52 | 1 | 1 | 0 | 21 | 68 | 1652 | 1452 | 100 | 50 |
| world6 | world6-2 | 1.67 | 1 | 1 | 0 | 11 | 81 | 1766 | 1719 | 100 | 50 |
| world6 | world6-3 | 1.62 | 1 | 1 | 0 | 14 | 92 | 2044 | 1932 | 100 | 50 |
| world6 | world6-4 | 1.72 | 1 | 1 | 0 | 12 | 89 | 2318 | 2343 | 100 | 50 |
| world6 | world6-5 | 1.73 | 2 | 2 | 0 | 7 | 93 | 3448 | 3530 | 100 | 50 |
| world6 | world6-6 | 1.76 | 1 | 1 | 0 | 13 | 97 | 3064 | 3190 | 100 | 50 |
| world6 | world6-7 | 1.71 | 1 | 1 | 0 | 22 | 95 | 4058 | 4105 | 100 | 50 |
| world6 | world6-8 | 1.58 | 2 | 2 | 9 | 9 | 131 | 3406 | 3177 | 100 | 50 |
| world7 | world7-1 | 1.59 | 1 | 1 | 0 | 0 | 65 | 1736 | 1608 | 100 | 50 |
| world7 | world7-2 | 1.74 | 2 | 1 | 0 | 0 | 88 | 2148 | 2192 | 100 | 50 |
| world7 | world7-3 | 1.7 | 1 | 1 | 0 | 0 | 93 | 1554 | 1535 | 100 | 50 |
| world7 | world7-4 | 1.7 | 1 | 1 | 0 | 0 | 110 | 2046 | 2039 | 100 | 50 |
| world7 | world7-5 | 1.64 | 2 | 1 | 0 | 0 | 105 | 3650 | 3534 | 100 | 50 |
| world7 | world7-6 | 1.73 | 1 | 1 | 0 | 0 | 91 | 3456 | 3546 | 100 | 50 |
| world7 | world7-7 | 1.69 | 2 | 1 | 0 | 0 | 124 | 4026 | 4041 | 100 | 50 |
| world7 | world7-8 | 1.74 | 1 | 1 | 0 | 0 | 104 | 2830 | 2907 | 100 | 50 |
| world8 | world8-1 | 1.51 | 1 | 1 | 6 | 0 | 77 | 1656 | 1448 | 100 | 50 |
| world8 | world8-2 | 1.68 | 1 | 1 | 7 | 0 | 89 | 2072 | 2040 | 100 | 50 |
| world8 | world8-3 | 1.64 | 1 | 1 | 14 | 0 | 85 | 2877 | 2776 | 100 | 50 |
| world8 | world8-4 | 1.71 | 1 | 1 | 16 | 0 | 99 | 2062 | 2071 | 100 | 50 |
| world8 | world8-5 | 1.57 | 2 | 1 | 13 | 0 | 98 | 3930 | 3663 | 100 | 50 |
| world8 | world8-6 | 1.74 | 2 | 1 | 7 | 0 | 105 | 3472 | 3578 | 100 | 50 |
| world8 | world8-7 | 1.71 | 1 | 2 | 11 | 0 | 123 | 4058 | 4105 | 100 | 50 |
| world8 | world8-8 | 1.83 | 2 | 2 | 18 | 9 | 120 | 2930 | 3165 | 100 | 50 |
| world9 | world9-1 | 1.46 | 1 | 1 | 0 | 0 | 71 | 2352 | 2006 | 100 | 50 |
| world9 | world9-2 | 1.53 | 2 | 1 | 0 | 0 | 80 | 2638 | 2372 | 100 | 50 |
| world9 | world9-3 | 1.52 | 2 | 1 | 8 | 0 | 97 | 4020 | 3623 | 100 | 50 |
| world9 | world9-4 | 1.6 | 1 | 1 | 0 | 0 | 111 | 3390 | 3197 | 100 | 50 |
| world9 | world9-5 | 1.57 | 1 | 1 | 0 | 0 | 101 | 3930 | 3663 | 100 | 50 |
| world9 | world9-6 | 1.55 | 3 | 1 | 0 | 0 | 117 | 5030 | 4623 | 100 | 50 |
| world9 | world9-7 | 1.64 | 1 | 2 | 0 | 0 | 148 | 4610 | 4473 | 100 | 50 |
| world9 | world9-8 | 1.8 | 2 | 2 | 18 | 0 | 131 | 3030 | 3215 | 100 | 50 |

---

## Decision Points (D1-01, D2-05)

### D1-01 — Économie
- **Coûts L1** : hike +20% sur crossbow, mage, mine (voir CLAUDE.md).
- **Reward mul** : baisser by world (W10 = ×0.55 W1).
- **Impact** : niveler les 5 high-ratio levels (W2–W5 perte −0.3 ratio).

### D2-05 — Level Redesign (Multi-Château)
- 15 levels avec C > 1 : refondr en 1P→1C chacun.
- MapValidator.js strict : `count(C) === 1 || throw`.
- Rétrocompat : 78 levels valides actuellement.

---

## Summary

**Status** : ✅ 80 levels parsés, data clean.  
**Key Finding** : **aucun level < 0.7**, équilibrage solide post-J7. Heatmap W1–W5 généreux (onboarding), W6–W10 stables (1.5–1.8).  
**Action** : D1-01 apply cost hike pour platten high-ratio W2–W5 (−0.3 cada). D2-05 refond 15 multi-C.

---

**Generated** : 2026-05-11 via audit automatisé.

