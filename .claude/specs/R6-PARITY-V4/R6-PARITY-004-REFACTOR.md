# R6-PARITY-004-REFACTOR — Split VfxPoolBindings.cs 555 LOC → < cap 500

**Sprint** : R6-PARITY-V4 (Batch P1-A, **PRIORITÉ ABSOLUE WT1**)
**Type** : bug-fixer (Sonnet, worktree)
**Priorité** : P1 #1 (résout violation charter §1 règle #3)
**Source** : `A-PARITY-V4-vfx-bindings-cap` (réponse superviseur 15h57) + audit batch P0

## Contexte

Le commit `57ffcd6` R6-PARITY-004 a livré `Assets/Scripts/Visual/VfxPoolBindings.cs` à **555 LOC**, violant le cap charter §1 règle #3 (hard cap 500 LOC par fichier C#). Le superviseur a validé ship-temp + refactor en tête P1 batch.

## Task

1. Read `Assets/Scripts/Visual/VfxPoolBindings.cs` (555 LOC actuelles).
2. Split selon UNE des 2 stratégies (au choix de l'agent selon ce qui produit la meilleure cohésion) :
   - **(A) Partial class** : `VfxPoolBindings.cs` (header + textures map + dispatch) + `VfxPoolBindings.Modules.cs` (BuildXxxModule helpers)
   - **(B) Extraction** : `VfxPoolTextures.cs` (textures-only lookup + Material/TextureSheet setup) + `VfxPoolBuilders.cs` (BuildExplosionSmokeModule, BuildFireBreathNoise, BuildSparkCollision, etc.)
3. Chaque fichier résultant **< 500 LOC strict** (cap charter §1 règle #3, TOLERANCE ZERO).
4. Compile gate : Unity batchmode build doit passer (HEAD post-Hero.cs fix `78a9f15` est compile-clean).
5. Commit `refactor(parity-v4-004): split VfxPoolBindings 555 LOC → 2 files <500 LOC each (cap charter §1)`

## Hard rules

- Aucun changement comportement (refacto strict, méthodes pub identiques)
- Cap 500 LOC strict (chaque fichier résultant)
- No feature creep (juste split, pas d'optim)
- Self-report 100 mots max

## Deliverable

- 2 fichiers ≤500 LOC chacun
- Commit hash
- Self-report : stratégie choisie (A/B), LOC fichier 1 + fichier 2, compile OK y/n, commit hash, push origin main OK

## Time estimate

~30 min
