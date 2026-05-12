# Tower Gameplay Flags Audit — 2026-05-12

## Summary
Audit of Tower.cs pierce/cascade/multishot wire-up at runtime.

## Flags Status

| Flag | Location | Status | Evidence |
|------|----------|--------|----------|
| **Pierce** | Tower.cs:922 + Projectile.cs:60,130–146 | OK | `effectivePierce` calc → `piercesRemaining` decrement loop w/ alreadyHit tracking |
| **MultiShot** | Tower.cs:946–963 + L3StatsTable:108 | OK | Loop spawns N extra projectiles w/ angle spread (archer 15°, others 12°) |
| **Cascade** | Tower.cs:81 + Projectile.cs:216–230 | OK | ApplyOnHitEffects radius scan at 50% dmg, no init needed |
| **PropagateAoE** | Tower.cs:77–79 + Projectile.cs:200–214 | OK | Flag check + radius splash to non-target, synergy-driven |
| **L3 FinalExplosion** | Tower.cs:183–185, 561–564 + Projectile.cs:148–174 | OK | L3StatsTable → ApplyL3Stats → TryFinalExplosion after pierce consumed |
| **L3 ChainLightning** | Tower.cs:992, 1302–1332 + L3StatsTable:120 | OK | Fire() calls FireChainLightning, jump logic w/ -20% dmg decay |

## Conclusion
All 6 gameplay flags are **fully wired and operational**. No missing implementations or incomplete chains detected.

No patches required.
