# Axis VISUAL-CORE — Final Report

**Status** : COMPLETE
**Branch** : `axis/visual-core`
**Tip** : `c2ea8d5`
**Date** : 2026-05-11
**Work time** : ~2h Opus direct (Agent/Task tool unavailable, executed inline)

## Summary

VISUAL-CORE Stage A delivered : VfxPool.cs singleton + 4 ParticleSystem prefabs + 2 boss overlay
shaders (Jellyfish kraken, Hologram cosmic) + material wrappers. ToonShader HLSL→Shader Graph
migration (A.4 priority 3) skipped to stay within time budget — rationale documented in QA report.

## Commits SHA

```
c2ea8d5 feat(visual): Hologram.shader scanline+glitch cosmic boss overlay
e231a7d feat(visual): Jellyfish.shader bioluminescent kraken boss overlay
1462e29 feat(vfx): 4 prefabs ParticleSystem Impact/Death/Aura/CoinPickup
aa9779e feat(visual): VfxPool.cs singleton 4-pool ObjectPool ParticleSystem (C3)
```

All commits atomic, conventional commit messages with Co-Authored-By footer.

## Stage A scoreboard

| Deliverable | Status | Notes |
|---|---|---|
| **A.1 VfxPool.cs** | DONE | C3 canon match + 1 additive ReleaseAura extension (request filed) |
| **A.2 4 VFX prefabs** | DONE | Impact / Death / Aura / CoinPickup ParticleSystem Shuriken WebGL-compat |
| **A.3 Boss shaders** | DONE | Jellyfish.shader + Hologram.shader + Jellyfish.mat + Hologram.mat |
| **A.4 Toon SG migration** | SKIPPED | Priority 3, time-budgeted out, ToonCelShading HLSL still working |

## QA reports

- `.claude/coordination/qa-reports/visual-core-pre-merge.md` : PASS

## API extension requests

- `.claude/coordination/requests/20260511-162846-visual-core-api-add.md` : `ReleaseAura(PS)`
  non-breaking additive method to balance `SpawnAura` continuous emission. MO ack pending.

## What MO can integrate next (Stage B)

The Stage B Integrator (per `integration-spec.md`) can now apply these calls inside hot zones :

```csharp
// Tower.Fire() (I1.1)
VfxPool.Instance?.SpawnImpact(transform.position + Vector3.up * 0.5f, cfg.Color);

// Tower.OnPlaced() / Tower.UpgradeTo() (I1.2, I1.3)
VfxPool.Instance?.SpawnCoinPickup(transform.position);

// Enemy.TakeDamage() non-fatal (I2.1)
VfxPool.Instance?.SpawnImpact(transform.position + Vector3.up * 0.3f, cfg.BodyColor);

// Enemy.Die() (I2.2)
VfxPool.Instance?.SpawnDeath(transform.position, cfg.BodyColor, cfg.Tier == EnemyTier.Boss);

// Economy.AddGold() player pickup (I6.1)
VfxPool.Instance?.SpawnCoinPickup(pickupPosition);
```

For continuous Aura (Tower with BuffAura, Boss with aura) :

```csharp
// In Init / start:
_aura = VfxPool.Instance?.SpawnAura(transform, cfg.AuraColor, isBoss: false);

// In Stop / OnDestroy:
VfxPool.Instance?.ReleaseAura(_aura);
_aura = null;
```

For boss overlay shaders (only kraken + cosmic bosses) :

```csharp
// In MaterialController.ApplyOverlay (to be implemented by integrator if needed):
// Apply Jellyfish.mat or Hologram.mat as a SECOND material slot on the GLTF mesh renderer,
// using transparent render queue so it overlays the toon-shaded base mesh.
```

## VfxPool scene setup (required for runtime)

A `VfxPool` MonoBehaviour singleton GameObject must be added to `Main.unity` scene with the
4 prefab refs assigned in Inspector. **This is a scene-level setup that must happen during
integration or in a separate scene-config commit.** If MO wants to avoid scene edits during
this axis stage, the VfxPool can be added by the Stage B Integrator as part of hot-zone hooks.

Quick setup (Unity Editor) :
1. Open `Assets/Scenes/Main.unity`
2. Create empty GameObject named `VfxPool`
3. Add Component `VfxPool` (CrowdDefense.Visual)
4. Assign 4 prefab refs : Impact / Death / Aura / CoinPickup
5. Save scene

## Risks / TODOs forwarded

- **SettingsRegistry pending Axis F UX** : VfxPool stub returns `IsVfxEnabled() = true`.
  After UX livers `Assets/Scripts/UI/SettingsRegistry.cs`, swap stub for
  `SettingsRegistry.Instance?.VFXEnabled ?? true` (1-line edit, VfxPool.cs:158 TODO marker).
- **Pre-existing BuildScript.cs errors** : Axis E PLATFORM-BUILDS has 2 compile errors in
  `Assets/Editor/BuildScript.cs` on main HEAD. Out of scope. Axis E will resolve.
- **Scene wiring** : VfxPool MonoBehaviour requires Main.unity scene reference (cf Setup
  notes above).

## Performance notes

- 4 pools preallocated 50 instances each, MaxSize 200 → up to 800 ParticleSystem instances
  in worst case. CPU-bound Shuriken (not VFX Graph) for WebGL compat. Per Phase 3 plan R7
  mitigation : `VfxPool` already throttles via pool MaxSize cap.
- Boss shaders (Jellyfish, Hologram) use cheap value-noise + sin pulse, no texture lookups,
  ~30 ALU ops fragment. WebGL-safe.
- Vertex glitch in Hologram.shader displaces verts per band — could cause subtle
  bbox/culling artifacts. Mitigation : worldPos uses post-glitch vertex so lighting accurate.

## Push status

Branch `axis/visual-core` pushed locally (in worktree). MO can `git fetch && git merge` from
the main repo to integrate. If origin push needed, run from worktree :

```bash
git push -u origin axis/visual-core
```

(Not pushed automatically pending MO arbitration on `ReleaseAura` API extension.)

## Files critiques

```
/Users/mike/Work/crowd-defense/.claude/worktrees/agent-ac66eb553ebaa57db/
├── .claude/
│   ├── plans/axis-visual-core.md
│   ├── coordination/
│   │   ├── qa-reports/visual-core-pre-merge.md
│   │   ├── requests/20260511-162846-visual-core-api-add.md
│   │   └── axis-visual-core-report.md       ← this file
├── Assets/
│   ├── Scripts/Visual/VfxPool.cs
│   ├── Prefabs/VFX/{Impact,Death,Aura,CoinPickup}.prefab
│   ├── Shaders/{Jellyfish,Hologram}.shader
│   └── Materials/{Jellyfish,Hologram}.mat
```

## End of axis VISUAL-CORE Stage A.
