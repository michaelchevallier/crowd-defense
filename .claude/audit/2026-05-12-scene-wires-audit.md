# Scene Audit: Unassigned Prefab/Asset References in Main.unity

**Date**: 2026-05-12
**Status**: Partial — projectilePrefab fixed, others identified for code-side handling

## Issues Found

### ✅ FIXED - ProjectilePool (Line 1227)
- **Component**: CrowdDefense.Systems.ProjectilePool
- **Field**: `projectilePrefab`
- **Fix**: Changed from `{fileID: 1234567890}` (placeholder) → `{fileID: 2000000000000001}` (correct)
- **Asset**: `Assets/Prefabs/Projectile.prefab`
- **GUID**: `d1e2f3a4b5c6789012345678def00202`

### ⚠️ SKIP - VfxPool (Line 934)
- **Component**: CrowdDefense.Visual.VfxPool
- **Unassigned fields** (20 total):
  - explosionPrefab, coinBurstPrefab, hitFlashPrefab, levelUpPrefab, perkPickupPrefab
  - frostPrefab, portalPrefab, fireBreathPrefab, muzzleFlashPrefab, upgradeBurstPrefab
  - sparkPrefab, upgradeConfettiPrefab, electricCloudPrefab, explosionSmallPrefab
  - glyphDarkPrefab, healAuraPrefab, lightningBoltPrefab, poisonCloudPrefab
  - shieldAuraPrefab, slowAuraPrefab
- **Reason**: Prefabs don't exist in Assets. Code likely has Resource.Load fallbacks.

### ⚠️ SKIP - LevelRunner (Line 1405)
- **Component**: CrowdDefense.Systems.LevelRunner
- **Field**: `castlePrefab`
- **Reason**: No Castle.prefab exists. Likely spawned/configured in code.

### ⚠️ SKIP - HeroProjectilePool (Line 1585)
- **Component**: CrowdDefense.Systems.HeroProjectilePool
- **Field**: `heroProjectilePrefab`
- **Reason**: No HeroProjectile.prefab exists. May share Projectile or spawn via code.

## Available Prefabs in Assets

```
Assets/Prefabs/Enemies/Enemy.prefab
Assets/Prefabs/Hero.prefab
Assets/Prefabs/Projectile.prefab
Assets/Prefabs/Towers/Tower.prefab
Assets/Prefabs/VFX/Aura.prefab
Assets/Prefabs/VFX/CoinPickup.prefab
Assets/Prefabs/VFX/Death.prefab
Assets/Prefabs/VFX/Impact.prefab
```

## Next Steps

Other warnings should be handled by parallel C# agent:
- Add Resources.Load fallbacks for VfxPool missing prefabs
- Configure LevelRunner.castlePrefab reference or spawning logic
- Configure HeroProjectilePool.heroProjectilePrefab reference or spawning logic
