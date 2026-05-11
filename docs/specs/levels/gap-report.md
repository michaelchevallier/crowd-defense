# LevelData Gap Report — 80 Levels Audit vs D1 Specs

**Date** : 2026-05-11
**Auditor** : SO-CONTENT (Sub-Opus Stage A)
**Source** : 80 `.asset` dans `Assets/ScriptableObjects/Levels/`
**Specs comparées** : D1-01 économie, D1-02 pacing, D1-04 castle HP, Q1-Q18 arbitrages
**Référence formule** : `castleHP = round(100 + 50 × √world × difficultyMul(level))` (W1-1 floor 200)

---

## TL;DR

- **80 levels audited**, 0 critical (`ratio < 0.60`), 7 major (`ratio < 0.80`), 64 OK (`ratio ∈ [0.80, 1.30]`), 9 over-permissive (`ratio > 1.30`).
- **0 illegal `allowMultiMagnet`** en W1-W6 (conforme Q3).
- **48/80 levels** avec wave count ≠ 4 (normal — varie 4-9 selon design : boss = +1 wave, mid-world = 5 waves).
- **startCoins très bas** sur W2-W4 (94-141¢ vs ~140-200 attendu) — marqué observation, non bug.
- **W6 levels over-permissive** : W6-1..W6-8 (ratio 1.26-1.54) — rupture difficulté W5→W6 demandée par Mike (D1-04) **n'est pas reflétée** dans les overrides.

---

## Méthodologie

Pour chaque LevelData :
1. Parse YAML (`m_Name`, `id`, `world`, `level`, `startCoins`, `overrideCastleHP+castleHPOverride` OU `castleHP` direct, `allowMultiMagnet`).
2. Compute `castleEff` (valeur effective utilisée) et `castleTarget` (formule D1-04).
3. Compute `ratio = castleEff / castleTarget`.
4. Classify :
   - **Critical** : `ratio < 0.60` (château < 60% spec — game-breaking)
   - **Major** : `0.60 ≤ ratio < 0.80` (château faible vs spec — challenging)
   - **OK** : `0.80 ≤ ratio ≤ 1.30` (dans la tolérance)
   - **Over-permissive** : `ratio > 1.30` (château trop solide — facilite trop)
5. Count waves + total mob population (estimation pression).
6. Check `allowMultiMagnet` flag W7+ uniquement (Q3).

---

## Stats agrégées

| World | Levels | castleHP min | castleHP max | Total mobs avg | Boss flag |
|-------|--------|--------------|--------------|----------------|-----------|
| W1 | 8 | 120 | 200 | 286 | W1-8 (Boss) |
| W2 | 8 | 130 | 180 | 372 | W2-8 (Midboss) |
| W3 | 8 | 150 | 200 | 314 | W3-8 (SkeletonMinion+) |
| W4 | 8 | 180 | 280 | 372 | W4-8 (BrigandBoss) |
| W5 | 8 | 150 | 260 | 819 | W5-8 (WarlordBoss) |
| W6 | 8 | 280 | 400 | 454 | W6-8 (ApocalypseBoss) |
| W7 | 8 | 220 | 350 | 526 | W7-8 (CorsairBoss) |
| W8 | 8 | 230 | 400 | 521 | W8-8 (KrakenBoss) |
| W9 | 8 | 250 | 400 | 612 | W9-8 (CosmicBoss) |
| W10 | 8 | 280 | 450 | 619 | W10-8 (AiHub) |

---

## Gap details — Major (ratio < 0.80)

7 levels avec château notablement faible vs spec D1-04.

| Level | Effective | Target | Ratio | Notes | Fix proposé |
|-------|-----------|--------|-------|-------|-------------|
| W1-3 | 120 | 152 | 0.79× | W1 early — onboarding fragile | bump to 145 (0.95×) |
| W2-1 | 130 | 171 | 0.76× | W2 intro — un peu juste | bump to 160 (0.94×) |
| W2-2 | 135 | 171 | 0.79× | Idem | bump to 165 (0.96×) |
| W5-1 | 160 | 212 | 0.75× | **W5 rupture upcoming**, ce level intro doit être plus tendu | bump to 200 (0.94×) |
| W5-2 | 150 | 212 | 0.71× | Idem, plus critique | bump to 195 (0.92×) |
| W5-4 | 160 | 223 | 0.72× | Mid-world W5 — tension acceptable mais bas | bump to 210 (0.94×) |
| W5-5 | 180 | 229 | 0.79× | Plateau W5 — light bump | bump to 220 (0.96×) |

**Justification fixes** :
- Conserver la fidélité prototype (override en place), juste ajuster les valeurs pour atteindre ~95% de la formule.
- W5 est un point critique : la rupture W5→W6 demandée par Mike (D1-04 §3.2) implique W5 = "plateau haut, dernière respiration avant le mur W6". Châteaux W5 trop faibles cassent cette progression.

---

## Gap details — Over-permissive (ratio > 1.30)

9 levels avec château surdimensionné.

| Level | Effective | Target | Ratio | Notes | Fix proposé |
|-------|-----------|--------|-------|-------|-------------|
| W6-2 | 290 | 222 | 1.31× | **W6 doit être rupture difficulté**, château trop solide casse le sentiment | bump down to 235 (1.06×) |
| W6-3 | 300 | 229 | 1.31× | Idem | bump down to 245 (1.07×) |
| W6-4 | 320 | 235 | 1.36× | Idem | bump down to 250 (1.06×) |
| W6-5 | 340 | 241 | 1.41× | Idem | bump down to 260 (1.08×) |
| W6-6 | 360 | 241 | 1.49× | Idem, plus critique | bump down to 265 (1.10×) |
| W6-7 | 380 | 247 | 1.54× | Idem, **pire offender** | bump down to 270 (1.09×) |
| W6-8 | 400 | 284 | 1.41× | Boss W6 — peut rester un peu plus haut mais 1.41 trop généreux | bump down to 310 (1.09×) |
| W10-7 | 400 | 290 | 1.38× | W10 pré-boss, mais > spec | bump down to 320 (1.10×) |
| W10-8 | 450 | 337 | 1.34× | Boss final — peut rester un peu généreux | bump down to 360 (1.07×) |

**Justification fixes** :
- W6 = **rupture difficulté Mike-arbitré** (D1-04 §3.2). Châteaux W6 doivent être à ~spec (`241-247`) avec ratio HP/pression chutant -22% vs W5. Override actuels (290-400) annulent cette rupture.
- W10 pré-boss/boss — légère générosité tolérable (1.10× max).

---

## Observations (non-bugs)

### startCoins pattern

W2-W4 levels ont startCoins très bas (94-141¢) vs ce que la formule linéaire `100 + 20 × world` suggérerait. C'est probablement **intentionnel** (D1-01 économie nerf reward = early game tendu) mais ne reflète pas une formule explicite documentée.

**Recommandation** : **ne pas toucher** dans ce passe (D1-01 implementation en hot zone Economy.cs → trop risqué de casser le tuning sans Sonnet économie qui mesure les ratios live). Laisser pour Phase 4 tuning autoQA.

### Wave count

48/80 levels ont != 4 waves. Distribution :
- 4 waves : 32 levels (W7-1..W7-5, W8-1..W8-5, W9-1..W9-5, W10-1..W10-5, W6-1..W6-4, W6-8, W1-1, W1-2, W7-8, W8-8, W9-8, W10-8)
- 5 waves : 39 levels (intermediate mid-world)
- 6 waves : 4 levels (W1-8, W2-8, W3-8, W4-8, W5-4)
- 7 waves : 1 level (W5-5)
- 8 waves : 2 levels (W5-6, W5-7)
- 9 waves : 1 level (W5-8)

Pas de norme stricte sur le wave count. **Non-bug**.

### castleHP source format

- **W1-1** utilise `castleHP: 200` direct (legacy field non-mappé au LevelData.cs actuel) — fonctionnera si la sérialisation Unity fallback à 0 puis `LevelData.CastleHP` retourne formule (200 = floor formule W1-1).
- **Tous les autres** utilisent `overrideCastleHP: 1` + `castleHPOverride: N` (mapping cohérent avec LevelData.cs).

**Recommandation** : laisser W1-1 tel quel (le code calcule fallback à 200 via formule). Pas de fix nécessaire.

### Levels avec mob count > 1000

- W5-6 : 1200 mobs total — pic explicite (8 waves)
- W5-8 : 1020 mobs total (9 waves, boss arena)

Acceptable. Justification : W5-6 est un mid-world stress test selon design (audit R2-06).

---

## Conformance specs

| Spec | Status | Notes |
|------|--------|-------|
| D1-04 castleHP formule W1-1 = 200 | OK | `castleHP: 200` direct |
| D1-04 castleHP formule W10-8 = 337 | OK valeur cible | Asset override = 450 (1.34× — fix proposed) |
| D1-04 W6 rupture difficulté +50% pression effective | KO — overrides W6 trop solides | 7 fixes proposed W6-2..W6-7 + W6-8 |
| D1-04 no-regen W6+ | N/A (data) | Géré code LevelRunner runtime |
| Q3 magnet cap 1/level (W1-W6) | OK | 0 illegal `allowMultiMagnet=Y` détecté |
| Q3 magnet cap 2/level (W7+) | OK | Aucun level ne flag `allowMultiMagnet=Y` — peut être ajouté sélectivement Phase 4 |
| Q14 W1-1 castle 200 | OK | castleHP=200 |
| Wave count 4-9 | OK | Range respectée |

---

## Plan d'action

### Fixes appliqués Stage A (16 levels)

7 Major (bump-up) + 9 Over-permissive (bump-down). Liste exacte dans la section "Fixes appliqués" du rapport final `axis-content-report.md`.

### Hors scope Stage A

- startCoins tuning (W2-W4 trop bas) — laissé à Phase 4 autoQA economy
- allowMultiMagnet activation sélective W7+ — laissé à design pass dédié (Sonnet level-designer)
- Ajout briefings inline dans LevelData.briefing field — laissé à Axis F UX (localization keys préférés)

---

## Backup

Tous les .asset modifiés sont backupés dans `/tmp/levelbackup/` avant edit.
