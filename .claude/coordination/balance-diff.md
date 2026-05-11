# Balance Diff Unity vs V5 — Audit 2026-05-12

Sources : V5 `milan project/src-v3/systems/MapBalance.js` + `entities/Hero.js` + `main.js:1097`. Unity `Assets/Resources/BalanceConfig.asset` + `Assets/ScriptableObjects/{Towers,Enemies}/*.asset`.

DPS Unity = `damage × TowerDamageMul(1.6) / (fireRateMs/1000)`.

## Towers (13 audités, V5 réf = 8)

| Tower | Cost V5/U | DPS V5/U | Range V5/U | Verdict |
|---|---|---|---|---|
| archer | 30/30 | 3.15/3.15 | 8/8 | OK |
| tank | 50/50 | 5.02/5.02 | 5/5 | OK |
| mage | 70/70 | 3.68/3.68 | 7/7 | OK |
| ballista | 100/100 | 5.89/5.89 | 14/14 | OK |
| cannon | 100/100 | 5.52/5.52 | 9/9 | OK |
| frost | 60/60 | 0/0 | 3/3 | OK |
| crossbow | 140/140 | 6.13/6.13 | 16/16 | OK |
| **skyguard** | 85/85 | 14.72/14.72 | **8/12** | **ECART +50% range** |
| frost(slow) | slowMul 0.5/0.5 dur 4s/4s | — | — | OK |
| magnet | 130/130 coinMul 1.3/1.3 | — | 5/5 | OK |
| fan, mine, portal, acid | Unity-only (post-V5) | — | — | OK (nouvelles) |

## Enemies (28 audités, V5 réf = 24 dont 11 bosses)

Mobs basiques W1-W4 (basic, runner, brute, shielded, flyer, assassin, imp, skeleton_minion, cyber_*) : **100% MATCH** (HP/speed/reward identiques).
Variants régionaux (desert_runner, forest_brute, submarin_runner, forest_bee, plaine_pigeon) : **Unity-only**, ne sont pas dans V5 ENEMY_STATS — à valider si voulus en MapBalance ou laisser hors auto-tune.

Bosses, écarts (V5 → Unity, format HP/speed/reward) :

| Boss | V5 | Unity | Drift |
|---|---|---|---|
| boss (medieval) | 60/0.4/50 | 60/**0.6**/50 | speed +50% |
| brigand_boss | 60/0.5/50 | **80**/0.6/**100** | HP +33% reward ×2 |
| warlord_boss | 120/0.45/80 | **100**/0.55/**120** | HP -17% reward +50% |
| **corsair_boss** | 200/0.5/120 | **90**/**0.9**/**110** | **HP -55%** speed +80% |
| **dragon_boss** | 300/0.6/150 | **130**/0.5/**200** | **HP -57%** reward +33% |
| apocalypse | 500/0.4/250 | 600/0.55/**500** | HP +20% reward ×2 |
| cosmic_boss | 600/0.5/300 | 700/0.5/**550** | reward +83% |
| kraken_boss | 700/0.4/400 | 800/0.45/**600** | reward +50% |
| wizard_king | 800/0.5/500 | 900/0.42/**650** | reward +30% |
| ai_hub | 1000/0.4/700 | 1000/0.38/700 | OK |

Pattern : late-game bosses Unity offrent **+30 à +100% reward** (déflation économique D1-01 ?), mais corsair/dragon ont **HP divisée par ~2** (régression cassante : ratio difficulty/économie).

## Castle / Hero / Economy

| Param | V5 | Unity (BalanceConfig.asset) | Note |
|---|---|---|---|
| Castle HP W1L1 | 120 (data/levels/world1-1) | formule 100+50·√1·1=150, floor 200 | OK spec D1-04 |
| Castle HP W10L8 | 450 (data/levels/world10-8) | 100+50·√10·1.5 = 337 | **ECART** Unity -25% en endgame |
| Castle regen | dur 6 worlds (`NoRegenWorldThreshold`) | idem | OK |
| Start coins W1L1 | 120 | non stocké (par level SO ?) | À vérifier sur LevelData |
| Start coins W10L8 | 350 | non stocké global | idem |
| Hero HP | N/A (invincible V5) | — | OK (parité) |
| Hero damage | DAMAGE=0.45, fireRate 600ms | — non en SO | à vérifier dans Hero.cs |
| Upgrade cost L1→L2 | ×1.5 base (`main.js:1097`) | UpgradeMulL2=1.5 | OK |
| Upgrade cost L2→L3 | ×2.5 base, total 5×base (V5 CLAUDE.md) | UpgradeMulL3=2.5 | OK (spec D1-01) |
| Sell refund | 80% | SellRefundRatio=0.8 | OK |
| Bank interest | non en V5 (D1-01 nouveau) | 0.05 | OK |
| Magnet coinMul | 1.3 | 1.3 | OK |
| TowerDamageMul | 1.6 (implicite, V5 raw dmg réel) | 1.6 | OK |

## Top 5 écarts à corriger (priorité décroissante)

1. **Corsair Boss HP 200→90 (-55%)** : W7 boss devient trivial vs V5. Restore HP=200 (ou ajuster reward proportionnellement).
2. **Dragon Boss HP 300→130 (-57%)** : W6 boss cassé. Restore HP=300.
3. **Skyguard range 8→12 (+50%)** : valeur ne match pas V5 (8). Soit normaliser à 8, soit accepter buff documenté.
4. **Bosses W7-W10 reward inflation (+30 à +100%)** : si voulu pour économie D1-01, documenter ; sinon réaligner sur V5 (corsair 120, dragon 150, apocalypse 250, cosmic 300, kraken 400, wizard 500).
5. **Castle HP late-game -25%** : formule sqrt produit 337 à W10L8 vs V5=450. Étudier si LevelDifficultyMul=1.5 suffit ou augmenter `CastleHPSqrtMul` à ~65.

Decision binaires pour Mike :
- Q1 : restore HP corsair/dragon V5 (200/300) ou keep Unity (90/130) ?
- Q2 : reward late-game bosses = V5 baseline ou Unity inflated (+économie D1-01) ?
- Q3 : skyguard range = 8 (V5) ou 12 (Unity buff documenté) ?
