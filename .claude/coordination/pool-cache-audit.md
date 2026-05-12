# Pool Cache Audit — 2026-05-12

## EnemyPool.cs — OK

- Per-type sub-pools keyed by `EnemyType.Id`: type mismatch structurally impossible.
- `_cachedRenderers` rebuilt each `Init()` via `GetComponentsInChildren` — correct since GLTF mesh child may differ per type.
- `_mpb` allocated once, fully overwritten before every `SetPropertyBlock` call — no stale state.
- `OnRelease`: `StopAllCoroutines` + `SetActive(false)` — clean.
- No TrailRenderer. No assetKey check needed (sub-pools handle it).

**Verdict: OK**

## ProjectilePool.cs — PATCHED

- Single-type pool, no type mismatch risk.
- **Miss**: `Projectile.Init` was calling `rend.material.color = color` — accessing `.material` on a `MeshRenderer` creates a new `Material` instance per call (Unity behaviour), causing a heap alloc + renderer cache invalidation on every pool Get.
- **Fix**: `_mpb` (`MaterialPropertyBlock`) allocated once per instance, reused on every `Init`. `_BaseColor` + `_Smoothness` set via `SetPropertyBlock` — zero alloc, no renderer state fork.
- No TrailRenderer present.

**Verdict: PATCHED** (`Assets/Scripts/Entities/Projectile.cs`)

## HeroProjectilePool.cs — OK

- `ResetState()` called on every `OnGet` — clears all fields, `_hitSet.Clear()`, `_trailFrame = 0`.
- No TrailRenderer (trail simulated via `VfxPool.SpawnImpact` every 2 frames, no persistent component).
- No MPB / material tint applied — visual is constant mesh, no color state to reset.
- No assetKey: single prefab type.

**Verdict: OK**

## VfxPool.cs — OK

- `actionOnRelease`: `ps.Stop(true, StopEmittingAndClear)` + `localScale = Vector3.one` + reparent to `_root` + `SetActive(false)` — full reset.
- `ApplyTint` overwrites `main.startColor` on every `Get` before `Play` — no stale color.
- `ApplyLod` overwrites `maxParticles` conditionally — correct.
- `SpawnFrost` overrides `shape.radius` before play — correct.
- `SpawnFireBreath` overrides `shape.length` before play — correct.
- No ParticleSystem `main` module fields that persist across pool cycles without reset.

**Verdict: OK**

## Summary

| File | Status | Issue |
|------|--------|-------|
| EnemyPool.cs | OK | — |
| ProjectilePool.cs (via Projectile.cs) | PATCHED | `.material` alloc on Get → MPB |
| HeroProjectilePool.cs | OK | — |
| VfxPool.cs | OK | — |

1 file patched, 3 OK.
