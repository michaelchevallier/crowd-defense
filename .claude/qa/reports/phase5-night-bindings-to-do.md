# Phase 5 Night Bindings TODO (Mike) — Consolidated

This file consolidates all Inspector / Scene edits required across Night #1, #2, #3.
**Owner**: Mike (cannot be done headless).

## Status as of Night #3 final (2026-05-18 07:35 CEST)

### P0 — Resolved by N17

**BossDef registry on BossSystem GameObject** ✅
- N17 wired all 11 BossDef assets in `Main.unity` (commit `52da04be`).
- BossSystem.OnEnemySpawned now finds BossDef for boss/midboss/etc.
- MusicManager.OnBossEncountered receives BossEncounteredEvent.

### P1 — Resolved by N27 + N33 + N38 (code-only, no Inspector edit needed)

**MissingReferenceException 'Tower has been destroyed'** ✅
- N27 : PlacementController.PrunePlacedTowers in LateUpdate
- N33 : `?.` → `if (x != null) x.method()` for Tower refs
- N38 : Tower.RegisterKill bail on `this == null` or `_destroyStarted`
- 3-layer defense against the race between PlayDestroyAnim coroutine + projectile-in-flight.

### P2 — Mike runs Tools menu utilities

#### Animator Controllers (4 missing)

**Source** : `[AnimationController] Controller 'tower_archer/ballista/cannon/mage' missing — fallback BaseCharacter applied`

**Fix** :
```
Tools → CrowdDefense → Build Animator Controllers
```
This generates per-type controllers (tower_archer, tower_ballista, etc.) so towers
don't fall back to BaseCharacter (which renders correctly but lacks tower-specific
attack animations).

**Owner** : Mike — one-time Tools menu invocation.

#### Audio missing clips

Three keys referenced but no asset exists :
- `tower_fire` (default fire SFX — used as fallback if no tier-specific clip)
- `tower_upgrade_celebration` (upgrade L3 celebration)
- `upgrade_ring_chime` (upgrade ring spawn)

**Fix** : Create AudioClip assets at `Assets/Resources/Audio/sfx_tower_fire.wav` etc.
+ add to AudioRegistry (Resources/AudioRegistry.asset). Optionally use
existing similar clips by renaming/referencing.

**Owner** : Mike — asset creation or remapping.

### Cosmetic / Low priority

#### Particle Velocity curves warning (~15969 occurrences)

**Source** : `Particle Velocity curves must all be in the same mode`

**Investigation** :
- All 5 VFX prefabs with VelocityModule (Aura/CoinPickup/Death/Explosion/Impact)
  have `enabled: 0` and uniform x/y/z minMaxState across modules.
- Origin remains unidentified — possibly Unity 6 bug or asset-import edge case.

**Fix** : Likely a Unity Editor reimport will fix it (the prefab YAML may have a
subtle inconsistency Unity reads but my python scanner missed). Try :
```
Tools → Generic Reimport → All Prefabs
```
OR right-click Assets/Prefabs/VFX → Reimport.

**Owner** : Mike — Editor reimport.

#### WorldMapController misplaced warning

**Source** : `[WorldMapController] Misplaced controller in scene 'Main' — disabling component`

**Status** : N28 demoted to Debug.Log (expected behavior per R2-recovery comment).
No action needed.

## Summary

After Mike :
1. Runs `Tools/CrowdDefense/QA/V3Loop/Auto/Run-Now`
2. (Optional) Runs `Tools/CrowdDefense/Build Animator Controllers`
3. (Optional) Reimports VFX prefabs

The V3 11-step loop should pass cleanly with all night-1+2+3 fixes in place.

## See also

- `.claude/qa/reports/phase5-night2-bindings-to-do.md` — original P0/P1/P2 list
- `.claude/qa/reports/phase5-night3-final-2026-05-18.md` — night 3 full report
- `.claude/qa/reports/phase5-night3-blocker-mcp-down.md` — Phase 2 blocker explanation
