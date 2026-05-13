# R7-ARCH-001 — Hero.cs partition close ✅

**Ticket** : R7-010 / T-ARCH-001
**Sprint** : R7-PUSH-100
**Date** : 2026-05-13
**Status** : ✅ DONE de facto (verified pre-existing sprint R6-PARITY-V4 partials)

## Mission

Documenter closure de la partition Hero.cs (6 partials, tous sub-500 LOC charter §1 règle #3).

## Verification charter §1 règle #3 — hard cap 500 LOC

| Fichier | LOC | Cap respecté |
|---|---|---|
| `Assets/Scripts/Entities/Hero.cs` | 199 | ✅ |
| `Assets/Scripts/Entities/Hero.Movement.cs` | 100 | ✅ |
| `Assets/Scripts/Entities/Hero.Abilities.cs` | 250 | ✅ |
| `Assets/Scripts/Entities/Hero.Perks.cs` | 276 | ✅ |
| `Assets/Scripts/Entities/Hero.Anim.cs` | 351 | ✅ |
| `Assets/Scripts/Entities/Hero.Combat.cs` | 408 | ✅ |
| **Total partials** | **1584** | **6 fichiers ✅** |
| `Assets/Scripts/Entities/HeroProjectile.cs` (companion, non-partial) | 237 | ✅ |

Tous les fichiers sont strictement sous le cap 500 LOC. Largest = Hero.Combat.cs à 408 LOC (92 LOC marge).

## Partials responsibilities (résumé)

- **Hero.cs** (199) : core, singleton Current, Init lifecycle, Update dispatcher, FireTrail struct
- **Hero.Movement.cs** (100) : input forward SetMove, bounds clamping, pathing
- **Hero.Abilities.cs** (250) : ULT casting, ChannelingPill, ultimate cooldown logic
- **Hero.Perks.cs** (276) : perk accumulation, XP gain, level-up event, skin tier
- **Hero.Anim.cs** (351) : animator setup, mesh child spawn, aura decals, perk icons billboard
- **Hero.Combat.cs** (408) : HP/damage/death, projectile fire, target acquisition, invul timing, respawn coroutine

## Historique

Partition initiale livrée pendant sprint R6-PARITY-V4 (cf retrospective sprint R6 + git log) via R6-02 partials extraction. R7-010 = pure closure documentaire (aucun code change requis).

## Conclusion

Ticket R7-010 / T-ARCH-001 **CLOSED — de facto done**. Charter §1 règle #3 respecté sur l'écosystème Hero complet. Aucune intervention code nécessaire.

Si refacto futur (Hero.Combat.cs proche limite 408/500 = 82%) cible : extraire fire-trail logic ou projectile pool wrapper dans Hero.Combat.Projectiles.cs partial supplémentaire.
