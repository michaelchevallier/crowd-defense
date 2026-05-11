# Hero Integration Spec

Ported from V5 Hero.js (tag `v5.0-pre-pivot-unity`). Status: Hero.cs + HeroType.cs done, LevelRunner/WaveManager hooks TBD Phase 4.

## Spawn (LevelRunner)

```csharp
// In LevelRunner.Start() or SpawnHero():
var heroGo = Instantiate(heroPrefab);
var hero = heroGo.GetComponent<Hero>() ?? heroGo.AddComponent<Hero>();
hero.Init(heroTypeSO, spawnPos, maxX: gridBbox.halfX, maxZ: gridBbox.halfZ);
hero.ApplyMetaBonuses(
    heroDamageMul: metaBonuses.heroDamageMul,
    heroRangeMul:  metaBonuses.heroRangeMul,
    heroFireRateMul: metaBonuses.heroFireRateMul,
    coinGainMul:   metaBonuses.coinGainMul,
    xpMul:         metaBonuses.xpMul);
hero.ApplyRunContext(savedPerkIds, savedLevel, savedXp);
// Expose on LevelRunner:
public Hero? Hero { get; private set; }
```

## Input forwarding (HeroInputAdapter or HudController)

```csharp
// Each frame (called from Update or Input system):
hero.SetMove(inputDir.x, inputDir.y);   // world-space, normalized by Hero
if (ultButton.WasPressedThisFrame()) hero.TryUlt();
```

## WaveManager hook

```csharp
// In WaveManager.OnWaveEnd callback:
LevelRunner.Instance?.Hero?.OnWaveEnd();  // triggers Castle.Regen(waveRegen)
```

## Economy — CoinGainMul

```csharp
// In Economy.AwardKill(Enemy e):
var hero = LevelRunner.Instance?.Hero;
float mul = hero?.CoinGainMul ?? 1f;
AddGold(Mathf.RoundToInt(e.Config!.Reward * mul));
hero?.GainXp(e.Config.Reward);
```

## Castle HP — CastleHPMaxMul

```csharp
// In LevelRunner.ResolveCastleHP():
var hero = LevelRunner.Instance?.Hero;
float heroMul = hero?.CastleHPMaxMul ?? 1f;
return Mathf.RoundToInt(baseHp * heroMul);
```

## Synergies — Tower aura (TBD Phase 4)

```csharp
// In Synergies.LateUpdate():
var hero = LevelRunner.Instance?.Hero;
if (hero == null) return;
var (dmgMul, rateMul, auraRange) = hero.GetTowerAuraBuffs();
// Apply to towers within auraRange of hero.transform.position
```

## HeroType SO — create asset

`Assets > Create > CrowdDefense > HeroType` → fills defaults (knight, 0.45 dmg, 12 range, 600 ms fire rate, 30s ult).

## Perk system (deferred Phase 4)

`Hero.ApplyRunContext` stores perk IDs in `Hero.Perks`. Full stat application via `PerkRegistry` SO is Phase 4. For testing, caller can set multipliers directly via `ApplyMetaBonuses`.
