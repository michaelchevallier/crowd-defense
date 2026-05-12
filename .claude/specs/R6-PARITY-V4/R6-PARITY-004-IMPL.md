# R6-PARITY-004-IMPL — Wire 9 VFX textures unmapped

**Sprint** : R6-PARITY-V4 (Batch P1-A, WT3)
**Type** : feature-dev (Sonnet, worktree)
**Priorité** : P1 #3
**Source** : R6-PARITY-004 self-report — "9 unmapped (no current spawn method)"

## Contexte

R6-PARITY-004 a wire 12/21 textures VFX V4 dans VfxPool. **9 textures restent unmapped** (imported, available, mais pas wired à un spawn method) :
- electric_cloud
- explosion_small
- glyph_dark
- heal_aura
- lightning_bolt
- poison_cloud
- shield_aura
- slow_aura
- smoke_gray

## Task

1. Read `Assets/Scripts/Visual/VfxPool.cs` + `VfxPoolBindings.cs` (ou splits post-REFACTOR si ticket #1 mergé avant) pour catalog des spawn methods existants.
2. Pour chaque texture unmapped, identifier OU créer un spawn method approprié :
   - `electric_cloud` → `SpawnElectricImpact` (tower shock attack) ou wire dans existing `SpawnSpark` variant electric
   - `explosion_small` → wire dans `SpawnImpact` variant small (small projectile hit)
   - `glyph_dark` → `SpawnGlyph` (nouveau ou variant SpawnPerkPickup theme dark)
   - `heal_aura` → `SpawnHealAura` (hero heal proc, castle repair tick)
   - `lightning_bolt` → `SpawnLightningChain` (lightning tower chain attack, existant ?)
   - `poison_cloud` → `SpawnPoisonField` (poison tower AoE)
   - `shield_aura` → `SpawnShieldAura` (shielded enemy aura, tower buff)
   - `slow_aura` → `SpawnSlowField` (slow tower AoE)
   - `smoke_gray` → wire as sub-emitter (already partially used in BuildExplosionSmokeModule, élargir)
3. Cross-check `Tower.cs`, `Castle.cs`, `Hero.cs` callsites pour identifier où ces VFX devraient déjà être triggered (V4 source : `src-v3/systems/VfxPool.js` ou similaire).
4. Add spawn methods dans VfxPool.cs si manquants (cap 500 LOC respecté), wire les callers existants pour invoquer.
5. **Exploit Unity** : ParticleSystem texture sheet animation, Module helpers (BuildXxxModule), sub-emitters pour effects composés.

## Hard rules

- Cap 500 LOC par fichier (extract si needed)
- No feature creep (juste wire ces 9 textures, pas nouveau VFX type unrelated)
- Compile gate
- Self-report 100 mots max

## Deliverable

- Commit `feat(parity-v4-004-impl): wire 9 unmapped VFX textures + spawn methods`
- Self-report : sprites wired (9/9 ?), spawn methods added/modified (list), Unity exploits used, callsites updated count, compile OK, commit hash

## Time estimate

~2-3h
