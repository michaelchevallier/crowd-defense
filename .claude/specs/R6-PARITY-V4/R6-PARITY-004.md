# R6-PARITY-004 — VFX sprites import & wire

**Sprint** : R6-PARITY-V4
**Type** : feature-dev (Sonnet, worktree)
**Priorité** : P0 (juice visuel important)
**Audit ref** : `.claude/audit/2026-05-12-v4-parity-gap.md` row "22 sprites VFX"

## Contexte

V4 a 21 sprites VFX PNG dans `/Users/mike/Work/milan project/src-v3/public/textures/vfx/` (sparkle_gold, explosion_big, blood_splat, glyph, etc.). V6 `VfxPool.cs` utilise ParticleSystem procédural Unity (sans textures sheet), donc effets visuels **inférieurs** vs V4.

## Task

1. **Source** : 21 PNG sprites dans `/Users/mike/Work/milan project/src-v3/public/textures/vfx/` :
   - sparkle_gold, explosion_big, blood_splat, glyph_runic, aura_gold, cloud_smoke, fire_burst, ice_shatter, lightning_flash, coin_drop, gem_sparkle, dust_puff, ember_trail, etc. (vérifier liste exacte ls).

2. **Si R6-PARITY-001 a importé** : skip copy, juste wire.

3. **Wire dans `Assets/Scripts/Visual/VfxPool.cs`** :
   - Pour chaque spawn method existant (`SpawnImpact`, `SpawnDeath`, `SpawnSpark`, `SpawnExplosion`, `SpawnCoinBurst`, etc.), set la texture appropriée via ParticleSystem renderer `material.mainTexture` ou `texture sheet animation` module.
   - Lookup table : `spawn type` → `texture name` (ex: SpawnExplosion → explosion_big.png).

4. **Exploit Unity** (Mike addendum) :
   - **Texture sheet animation** : si VFX texture est 4×4 grid frames, set frame range cycling
   - **Sub-emitters** : explosion → secondary smoke sub-emitter
   - **Noise module** : turbulence pour fire/smoke réaliste
   - **Collision module** : sparks bounce off ground (opt-in)

5. **Placeholder-first** : si textures absentes, garder ParticleSystem procédural V6 actuel (no-op), flag manquant.

## Hard rules

- Cap 500 LOC `VfxPool.cs` (déjà ~600 LOC peut-être, attention au cap)
- No feature creep
- Self-report 100 mots max

## Deliverable

- Commit `feat(parity-v4-004): VFX sprites — 21 PNG wired into VfxPool ParticleSystem renderers`
- Self-report :
  - Sprites wired count (X/21)
  - Spawn methods modified (list)
  - Unity exploits used (texture sheet, sub-emitters, noise) : list
  - Compile OK : y/n
  - Commit hash

## Time estimate

~1-2h (wire dans VfxPool, surtout setup par méthode).