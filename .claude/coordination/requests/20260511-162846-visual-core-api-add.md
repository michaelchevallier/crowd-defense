# API Extension Request — VfxPool.ReleaseAura

**Date** : 2026-05-11 16:28 UTC
**Axis** : VISUAL-CORE
**Type** : API contract extension (additive, non-breaking)

## Context

C3 VfxPool API defines `SpawnAura(Transform parent, Color tint, bool isBoss = false) → ParticleSystem`
to return a continuous ParticleSystem ref. But no explicit "stop/release" method is defined for it.

Issue : without an explicit Release path, the Aura pool would leak as the consumer can't return the
PS to the pool. Calling `pool.Release(ps)` requires VfxPool.Instance internals.

## Proposed extension

Add to C3 :

```csharp
// Release a continuous aura ParticleSystem back to the pool.
// Stops emission, clears existing particles, re-parents to pool root.
// Required to balance SpawnAura.
public void ReleaseAura(ParticleSystem? ps);
```

## Usage from hot zones (Stage B integrator reference)

```csharp
// In Tower with BuffAura (or boss Enemy with aura):
ParticleSystem? _aura;

void OnAuraStart() {
    _aura = VfxPool.Instance?.SpawnAura(transform, cfg.AuraColor, isBoss: false);
}

void OnAuraStop() {
    VfxPool.Instance?.ReleaseAura(_aura);
    _aura = null;
}
```

## Status

Already implemented in commit `aa9779e` (axis/visual-core). Asking MO to ack +
update `.claude/coordination/api-contracts.md` C3 section to include this signature
post-merge.

No-op alternative if MO rejects : consumer can call `ps.Stop(true, StopEmittingAndClear)` directly
without re-parenting — would leak ParticleSystem instances over time but pool maxSize 200 would cap.
