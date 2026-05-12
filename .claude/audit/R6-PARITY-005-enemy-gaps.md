# R6-PARITY-005 — Enemy types audit V4 vs V6

**Date** : 2026-05-12
**Sources V4** : `/Users/mike/Work/milan project/src-v3/entities/Enemy.js` (ENEMY_TYPES constant, 28 entries)
**Sources V6** : `Assets/Scripts/Entities/Enemy.cs` + `Assets/Scripts/Data/EnemyType.cs` + `Assets/ScriptableObjects/Enemies/` (28 SO assets)

---

## Table complète 28/28

| # | Enemy V4 id | Catégorie | Status V6 | Behaviors V4 | Behaviors V6 | Gaps / Notes |
|---|-------------|-----------|-----------|-------------|--------------|--------------|
| 1 | `basic` | Basic | **PRESENT** | Walk, bodyColor, dust | Walk, dust, hit flash, freeze/burn decals | COMPLET |
| 2 | `skeleton_minion` | Basic | **PRESENT** | Walk, Walking_A anim | Walk anim | COMPLET |
| 3 | `runner` | Basic | **PRESENT** | Run, speed×2 | Run, ComputeEffectiveSpeed | COMPLET |
| 4 | `brute` | Basic | **PRESENT** | Walk, tanky | Walk, AoEAttack on castle | COMPLET |
| 5 | `shielded` | Basic | **PRESENT** | shieldHP directional block | shieldHp absorb + halo | COMPLET |
| 6 | `midboss` | Mid-boss | **PRESENT** | isMidBoss, walk | isMidBoss, enrage @50% | COMPLET |
| 7 | `boss` (generic) | Boss | **PRESENT** | isBoss, aura | isBoss, aura, cinematic | COMPLET |
| 8 | `brigand_boss` | Boss | **PRESENT** | isBrigand, charge sprint (chargeMs=1500, chargeMul=4) | UpdateCharge, BossChargeWarningEvent | COMPLET |
| 9 | `assassin` | Basic | **PRESENT** | isStealth, stealthCycleMs=2200, opacity 0.25 | UpdateStealth, StealthAlpha, ring | COMPLET |
| 10 | `warlord_boss` | Boss | **PARTIAL** | summonsMinions(runner), chargeMs=6000 in V4 | summonsMinions=1 (OK) but **chargeMs=0 in SO** | MANQUE charge sprint dans WarlordBoss.asset |
| 11 | `flyer` | Basic | **PRESENT** | isFlyer, ignorePath, flyHeight=2.5 | UpdateFlyer, ignorePath logic | COMPLET |
| 12 | `corsair_boss` | Boss | **PRESENT** | isCorsair, aoeBlastMs=8000, radius=4.5, dmg=30 | UpdateAoeBlast, EmitAoeBlast — aoeBlastMs=8000 in SO | COMPLET |
| 13 | `imp` | Basic | **PRESENT** | isFiery=true | isFiery trail, fiery particles | COMPLET |
| 14 | `dragon_boss` | Boss | **PARTIAL** | fireBreath cone (V4 uses aoeBlastMs as proxy), summonsMinions(imp), isFlyer | UpdateFireBreath (id-based detection) + summonsMinions + isFlyer | **fire breath detection par id-string fragile** — isFire flag manquant dans EnemyType |
| 15 | `apocalypse_boss` | Boss | **PRESENT** | isApocalypseBoss, 4 phases: P1 normal / P2 invul+summons / P3 speed×2 / P4 AoE pulse | TickApocalypseBossPhases — P2(invul 2s+speed×1.5), P3(4 skeletons), P4(AoE pulse 3s+damageMul×2+EnrageVFX) | COMPLET (parity étendue) |
| 16 | `cosmic_boss` | Boss | **PRESENT** | isApocalypseBoss, summonsMinions(flyer) | isApocalypseBoss + summonsMinions OK | COMPLET |
| 17 | `kraken_boss` | Boss | **PARTIAL** | isApocalypseBoss, summonsMinions(shielded), shaderOverlay=jellyfish, **tentacle slam** AoE | isApocalypseBoss + summonsMinions + jellyfish shader | **tentacle slam absent** — V4 source note "tentacle slam" in doc but NOT implemented in Enemy.js. AoE pulse du phase 4 couvre partiellement. PARTIAL par doc intent |
| 18 | `wizard_king` | Boss | **PARTIAL** | isApocalypseBoss, summonsMinions(assassin), **teleport**, **projectile rain** | isApocalypseBoss + summonsMinions OK | **teleport + projectile rain absents** — ni dans Enemy.cs ni dans EnemyType.cs ni dans BossSystem.cs |
| 19 | `ai_hub` | Boss | **PARTIAL** | isApocalypseBoss, summonsMinions(flyer), shaderOverlay=hologram, **drone swarm summons pattern** | isApocalypseBoss + summonsMinions + hologram shader | **drone swarm** = simple summonsMinions en V6 (pas de burst pattern). PARTIAL vs V4 intent |
| 20 | `desert_runner` | Variant | **PRESENT** | Run, asset=mob_cactoro | Runner variant DesertRunner.asset | COMPLET |
| 21 | `forest_brute` | Variant | **PRESENT** | Walk, asset=mob_orc | ForestBrute.asset | COMPLET |
| 22 | `submarin_runner` | Variant | **PRESENT** | Jump anim, asset=mob_frog | SubmarinRunner.asset | COMPLET |
| 23 | `forest_bee` | Variant flyer | **PRESENT** | isFlyer, Fast_Flying, asset=mob_armabee | ForestBee.asset + flyer logic | COMPLET |
| 24 | `plaine_pigeon` | Variant flyer | **PRESENT** | isFlyer, Fast_Flying, asset=mob_pigeon | PlainePigeon.asset + flyer logic | COMPLET |
| 25 | `cyber_basic` | Variant Cyberpunk | **PRESENT** | Walk, asset=mob_cyberpunk_character | CyberBasic.asset | COMPLET |
| 26 | `cyber_runner` | Variant Cyberpunk | **PRESENT** | Run, asset=mob_cyberpunk_2legs | CyberRunner.asset | COMPLET |
| 27 | `cyber_flyer` | Variant Cyberpunk | **PRESENT** | isFlyer, Flying, asset=mob_cyberpunk_flying | CyberFlyer.asset + flyer | COMPLET |
| 28 | `cyber_brute` | Variant Cyberpunk | **PARTIAL** | Walk, asset=mob_cyberpunk_large, high HP/dmg | CyberBrute.asset exists | **isFiery not set** — V4 cyber_brute sans isFiery, V6 idem. PRESENT réel. Reclass → PRESENT |

**Recalcul CyberBrute** : PRESENT (pas de gap réel).

---

## Résumé

| Status | Count |
|--------|-------|
| PRESENT | 23 |
| PARTIAL | 5 |
| MISSING | 0 |

**5 PARTIAL** :
1. `warlord_boss` — chargeMs=0 dans SO (V4: chargeCooldownMs=6000, chargeMul=4 attendus)
2. `dragon_boss` — fire breath via id-string fragile (pas de flag `isFire` dans EnemyType)
3. `kraken_boss` — tentacle slam absent (note: V4 source n'implémente pas non plus, doc intent)
4. `wizard_king` — teleport + projectile rain absents dans Engine et EnemyType
5. `ai_hub` — drone summon = summonsMinions ordinaire, pas de burst pattern différencié

---

## Top 5 gaps prioritaires (impact gameplay)

### P1. `wizard_king` — Teleport + projectile rain (MISSING behavior)
- V4 définit `isApocalypseBoss=true`, `summonsMinions=assassin`. Doc intent = boss téléporte hors portée tour + rain de projectiles
- V6 : aucun code teleport/projectile dans Enemy.cs, EnemyType.cs ou BossSystem.cs
- LOC estimé : ~120 LOC (EnemyType.cs +2 flags, Enemy.cs +TickWizardKing method, BossSystem.cs event)
- **Impact** : Boss du monde Medieval = gameplay flat vs intent

### P2. `warlord_boss` — Charge sprint (SO misconfigured)
- V4 `warlord_boss` : `chargeMs=1500, chargeCooldownMs=6000, chargeMul=4`
- V6 `WarlordBoss.asset` : `chargeMs=0` (charge logic existe dans Enemy.cs via `isBrigand` flag mais warlord n'est pas flagged brigand NOR a charge)
- Note : V4 n'a pas isBrigand sur warlord — la charge était explicite via chargeMs. V6 relie charge uniquement à isBrigand, donc warlord charge ne peut pas activer
- LOC estimé : **~5 LOC** — SO data fix uniquement (set chargeMs=1500, chargeCooldownMs=6000 dans WarlordBoss.asset)
- **Impact** : P0 boss mid-game sans behavior distinctif = trop facile

### P3. `dragon_boss` — Fire breath via id-string (fragile)
- V6 `UpdateFireBreath` : `id.IndexOf("dragon") || id.IndexOf("fire") || id.IndexOf("infernal")`
- Fragile si renommage id. V4 avait flag implicite via `aoeBlastMs` (fallback)
- LOC estimé : ~10 LOC (EnemyType.cs +1 `[SerializeField] bool isFireBreath`, Enemy.cs fix ifdirection)
- **Impact** : Maintenabilité, risque régressions

### P4. `ai_hub` — Drone swarm pattern différencié
- V4 intent : burst périodique de drones (type flyer) en pattern circulaire
- V6 : `summonsMinions=1, summonType=flyer` — identique à cosmic_boss. Pas de comportement distinct
- LOC estimé : ~40 LOC (EnemyType +flag `isDroneSummon`, Enemy.cs +UpdateDroneBurst avec burst×5 en éventail)
- **Impact** : World Cyberpunk boss sans identité visuelle/mécanique propre

### P5. `kraken_boss` — Tentacle slam
- V4 source Enemy.js ne l'implémente PAS non plus (doc intention uniquement)
- V6 parity : lacune documentée, non-régressif
- LOC estimé : ~80 LOC nouveau (EnemyType +flag `isTentacleBoss`, Enemy.cs +TickTentacleSlam + AoE stun towers)
- **Impact** : Moyen — phase 4 AoE pulse couvre partiellement le rôle

---

## Fix mineur applicable dans ce ticket

### Fix P2 — WarlordBoss.asset chargeMs=0 → 1500 (SO data, ~5 LOC équivalent)

Le gap P2 est une erreur de configuration SO, pas de code manquant. Le code `UpdateCharge` est déjà complet dans Enemy.cs.
**Problème** : `UpdateCharge` lit `cfg.IsBrigand` comme gate. WarlordBoss a `isBrigand=0`.
En V4 le charge est déclenché par `chargeMs>0` seul, sans flag `isBrigand`.

V6 `UpdateCharge` :
```csharp
if (cfg == null || !cfg.IsBrigand || cfg.ChargeCooldownMs <= 0) return;
```

Ce gate `IsBrigand` empêche warlord_boss de charger même si on set chargeMs.
Fix correct = SO + condition Enemy.cs.

**Décision** : La correction complète (SO + code) dépasse 5 LOC mais reste < 50 LOC.
Fix proposé dans ticket follow-up R6-PARITY-005-IMPL avec batch P1-P5.

---

## Recommandation

Créer ticket **R6-PARITY-005-IMPL** couvrant :
1. `warlord_boss` charge : SO data + `UpdateCharge` gate suppression isBrigand → chargeMs>0 (~15 LOC)
2. `dragon_boss` fire breath : EnemyType +flag `isFireBreath` + Enemy.cs fix (~10 LOC)
3. `wizard_king` teleport+rain : nouveau TickWizardKing + EnemyType flags (~120 LOC)
4. `ai_hub` drone burst : EnemyType flag + Enemy.cs burst pattern (~40 LOC)
5. `kraken_boss` tentacle slam : EnemyType flag + TickTentacleSlam (~80 LOC)

**Total estimé** : ~265 LOC, 1 ticket P1 batch.
