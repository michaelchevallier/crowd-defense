# Opus Entities V5 Parity — Sub Report

**Date**: 2026-05-12
**Mode**: single-pass implementation (no sub-Sonnet dispatch — gaps were narrow and bounded)
**V5 ref**: `/Users/mike/Work/milan project/src-v3/entities/{Tower,Enemy,Hero,Castle}.js`

## Diff vs V5

After audit `v5-gap-audit-2026-05-11.md` and inspection of current Unity entities (post 6717d09), Hero.cs was already comprehensive (PerkSystem delegate, ricochet/fireball/lightning/pierceExplode all wired in HeroProjectile). Castle.cs had the bulk of V5 visuals (HP bar, tint, smoke, danger light, grayscale, regen). Real residual gaps were concentrated in **Tower → Projectile damage path consumption** (Synergies set flags but no consumer) and a couple of Enemy/Castle V5 features.

## Gaps closed

| Gap | File(s) | Effect |
|---|---|---|
| Synergy `_propagateAoEActive/Radius/Dmg` consumed at hit | `Projectile.cs` | mage→cannon cross-effect splash damage applied |
| Synergy `_cascadeRadius` consumed at hit | `Projectile.cs` | cannon→X cross-effect chain damage applied |
| `L3ArmorBreak`/`L3Knockback`/`_knockbackOnHit` consumed | `Projectile.cs` + `Enemy.cs` | Ballista L3 amplifies subsequent damage; knockback rewinds waypoint |
| Enemy `_dmgTakenMul/Until` field + ApplyArmorBreak/ApplyKnockback | `Enemy.cs` | infrastructure for armor break + knockback |
| Boss self-trigger enrage @ 50% HP (V5 fallback) | `Enemy.cs` | bosses without registered `BossDef` now enrage anyway |
| L3 Tank DoT aura 0.6 dps × 5 m | `Tower.cs` | Tank L3 ticks damage to enemies in radius |
| L3 Crossbow FinalExplosion on last-pierce | `Tower.cs` + `Projectile.cs` | crossbow L3 explodes after pierce consumed |
| Castle HP number text above bar (V5 `_hpText` sprite) | `Castle.cs` | TextMesh "<hp> / <hpMax>" world-space above HP bar |

## Files modified

- `Assets/Scripts/Entities/Tower.cs` (+ L3TankBlock/FinalExplosion fields + ApplyL3Branch non-signature switch + TickTankBlockAura)
- `Assets/Scripts/Entities/Enemy.cs` (+ DmgTakenMul state + ApplyArmorBreak/ApplyKnockback + TakeDamage uses mul + self-enrage fallback)
- `Assets/Scripts/Entities/Projectile.cs` (ApplyOnHitEffects now consumes propagateAoE/cascade/armorBreak/knockback; TryFinalExplosion on pierce end)
- `Assets/Scripts/Entities/Castle.cs` (+ `_hpText` TextMesh, refreshed in RefreshHpBar)

## Verification

Unity Editor (already running, prevented batchmode) reimported all 4 modified files. `grep error CS Editor.log` shows only pre-existing `BuildScript.cs(72,43)` warning unrelated to entities. No new compile errors.

Runtime play-mode test not possible (Unity Editor lock).

## Residual smaller gaps (left for later)

- Castle GLTF theme variants (CASTLE_ASSET_BY_THEME + CASTLE_ADDONS antenna/radar). Visual polish, deferred.
- Tower `_recoil` model offset on fire (cosmetic).
- Hero idle / cape animation polish (cosmetic).
