# QA-3 Pre-Merge Report — Axis VISUAL-CORE

**Branch** : `axis/visual-core`
**Tip commit** : `c2ea8d5`
**Date** : 2026-05-11
**Verdict** : PASS

## Commits livrés (4)

```
c2ea8d5 feat(visual): Hologram.shader scanline+glitch cosmic boss overlay
e231a7d feat(visual): Jellyfish.shader bioluminescent kraken boss overlay
1462e29 feat(vfx): 4 prefabs ParticleSystem Impact/Death/Aura/CoinPickup
aa9779e feat(visual): VfxPool.cs singleton 4-pool ObjectPool ParticleSystem (C3)
```

## Files touched (vs main a6540cb)

```
.claude/coordination/requests/20260511-162846-visual-core-api-add.md
.claude/plans/axis-visual-core.md
Assets/Materials/Hologram.mat (+ .meta)
Assets/Materials/Jellyfish.mat (+ .meta)
Assets/Prefabs/VFX.meta
Assets/Prefabs/VFX/Aura.prefab (+ .meta)
Assets/Prefabs/VFX/CoinPickup.prefab (+ .meta)
Assets/Prefabs/VFX/Death.prefab (+ .meta)
Assets/Prefabs/VFX/Impact.prefab (+ .meta)
Assets/Scripts/Visual/VfxPool.cs (+ .meta)
Assets/Shaders/Hologram.shader (+ .meta)
Assets/Shaders/Jellyfish.shader (+ .meta)
```

## Ownership check (file-ownership.md Axis A VISUAL-CORE)

- [x] `Assets/Scripts/Visual/VfxPool.cs` → zone Visual exclusive write OK
- [x] `Assets/Prefabs/VFX/*` → zone (nouveau dossier autorisé Axis A) OK
- [x] `Assets/Shaders/*` → zone exclusive write OK
- [x] `Assets/Materials/*` → zone exclusive write OK
- [x] Hot zones NOT touched : Tower.cs, Enemy.cs, Castle.cs, WaveManager.cs, LevelRunner.cs, Economy.cs, BalanceConfig.cs (zero diff)

## API contracts (api-contracts.md C3)

C3 VfxPool signature canon : **MATCH**

```csharp
public void SpawnImpact(Vector3 worldPos, Color tint);                          // ✓
public void SpawnDeath(Vector3 worldPos, Color tint, bool isBoss = false);       // ✓
public ParticleSystem? SpawnAura(Transform parent, Color tint, bool isBoss);     // ✓ (returns nullable)
public void SpawnCoinPickup(Vector3 worldPos);                                   // ✓
```

**Extension proposed** : `ReleaseAura(ParticleSystem? ps)` (cf
`.claude/coordination/requests/20260511-162846-visual-core-api-add.md`).
Non-breaking additive. MO ack pending.

## Compile status

`mcp__UnityMCP__refresh_unity` + `read_console errors` filtered to my scope :
- VfxPool.cs : 0 errors
- Jellyfish.shader : 0 errors
- Hologram.shader : 0 errors

**Pre-existing errors not from this axis** (BuildScript.cs Axis E PLATFORM-BUILDS) :
- `UserBuildSettings` not found (Editor/BuildScript.cs:61)
- `ulong → long` cast missing (Editor/BuildScript.cs:201)

These don't block VISUAL-CORE scope. Will be resolved by Axis E.

## Settings/preferences (deferred)

- `SettingsRegistry.Instance?.VFXEnabled` check : stubbed `IsVfxEnabled() → true`
  pending Axis F UX livery of `Assets/Scripts/UI/SettingsRegistry.cs`. TODO comment
  in VfxPool.cs:158 marks the swap point.

## Stage A deliverables

| Deliverable | Priority | Status |
|---|---|---|
| A.1 VfxPool.cs | 1 | DONE |
| A.2 4 VFX prefabs | 1 | DONE |
| A.3 Jellyfish + Hologram shaders | 2 | DONE |
| A.4 ToonShader HLSL → Shader Graph | 3 | SKIPPED (time budget) |

## A.4 skip rationale

ToonCelShading.shader actuel (HLSL legacy) compile clean + fonctionne. Migration vers Shader Graph
URP est principalement esthétique (visual node editing) et impose une upgrade pipeline (URP package
config + Shader Graph asset format). Le coût (3-5h) ne justifie pas vs A.1+A.2+A.3 livré qui débloque
le Stage B Integrator. Migration A.4 ré-éligible Phase 4 polish ticket dédié si Mike souhaite éditer
le shader sans HLSL.

## Verdict

**PASS — Ready for MO merge into integration/phase3-4-5 branch.**

Recommended next : MO ack ReleaseAura extension + merge axis/visual-core →
integration. Stage B Integrator can consume :
- `VfxPool.Instance?.SpawnImpact/Death/Aura/CoinPickup(...)`
- `JuiceFX.Instance?.Shake/Flash/SlowMo(...)`  (already live, pre-existing on main)
- `AnimationController.SetWalking/TriggerAttack/TriggerDeath(anim)` (static, pre-existing)
