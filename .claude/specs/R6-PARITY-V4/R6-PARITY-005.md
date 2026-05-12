# R6-PARITY-005 — Enemy types audit complet V4 → V6

**Sprint** : R6-PARITY-V4 (Batch P0-B, parallèle ou queued)
**Type** : feature-dev (Sonnet, worktree)
**Priorité** : P0 (gameplay critical, boss roster complet attendu)
**Audit ref** : `.claude/audit/2026-05-12-v4-parity-gap.md` row "28 enemy types"

## Contexte

V4 a 28 enemy types avec behaviors spécifiques. V6 a 28 enemy types également (Audit B confirm) mais certains behaviors complexes (boss phases, special attacks) sont **incomplets ou manquants** post-R6-02 partial DELETE.

## Task

1. **Audit V4 source** : Read `/Users/mike/Work/milan project/src-v3/entities/Enemy.js` + `data/enemies.js` ou similaire pour la liste exhaustive des 28 enemy types et leurs behaviors spécifiques.

2. **Audit V6 source** : Read `/Users/mike/Work/crowd-defense/Assets/Scripts/Entities/Enemy.cs` (~2348 LOC post-R6-02) + `Data/EnemyType.cs` + `EnemyRegistry` SO assets.

3. **Vérifier chaque enemy V4** :
   - **Basic** : grunt, runner, brute, shielded, flyer, imp, assassin, brigand, corsair, fiery variant
   - **Mid-boss** : warlord_boss (charge sprint), corsair_boss (AoE blast)
   - **Boss** : dragon_boss (fire breath cone), apocalypse_boss (4 phases : P1 normal → P2 invul+summons → P3 speed×2 → P4 AoE pulse 360°), cosmic_boss, kraken_boss (tentacle slam), wizard_king (teleport+projectile rain), ai_hub (drone summons)
   - **Variants thématiques** : version reteinte per thème (cf V4 `AssetVariants.js`)

4. **Pour chaque enemy MISSING ou behavior PARTIAL en V6** :
   - Si missing : noter dans `.claude/audit/R6-PARITY-005-enemy-gaps.md`
   - Si partial : noter behaviors manquants
   - Ce ticket ne fait PAS l'implémentation des gaps — c'est l'AUDIT.

5. **Output** : créer `.claude/audit/R6-PARITY-005-enemy-gaps.md` listant :
   - Table : Enemy V4 → V6 status (PRESENT/PARTIAL/MISSING) + behaviors specifiques manquants
   - Top 5 gaps prioritaires (high gameplay impact)
   - LOC estimé port pour combler

6. **Si gaps mineurs détectés** (<50 LOC fix), proposer fix dans CE ticket. Sinon, propose ticket follow-up R6-PARITY-005-IMPL.

## Hard rules

- LECTURE SEULE pour l'audit. Modifications code uniquement si gap mineur <50 LOC.
- No feature creep
- Self-report 100 mots max

## Deliverable

- 1 fichier `.claude/audit/R6-PARITY-005-enemy-gaps.md` (~50-80 rows max)
- Si fix mineur appliqué : commit `feat(parity-v4-005): enemy types audit + minor fix {detail}`
- Sinon : pas de commit, juste fichier audit (committable)
- Self-report :
  - 28/28 enemies cataloged
  - Gaps detected count (PRESENT/PARTIAL/MISSING)
  - Top 5 prioritaires
  - Fix mineur appliqué : y/n (LOC)
  - Commit hash si applicable

## Time estimate

~1-2h (audit principalement).