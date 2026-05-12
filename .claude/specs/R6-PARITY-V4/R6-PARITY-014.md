# R6-PARITY-014 — Boss phases complete (Apocalypse 4 phases + charge sprint + fire breath cone)

**Sprint** : R6-PARITY-V4 (Batch P1-A, WT4)
**Type** : feature-dev (Sonnet, worktree)
**Priorité** : P1 #4
**Source** : audit V4 parity gap + R6-PARITY-005-enemy-gaps Top 5

## Contexte

Bosses V4 ont multi-phase behaviors complets que V6 a en partie. Compléter :
- **Apocalypse_boss** : 4 phases (P1 normal → P2 invul + summons → P3 speed×2 → P4 AoE pulse 360°)
- **Warlord/Brigand boss** : charge sprint (~2s wind-up, ~2× speed forward)
- **Dragon_boss** : fire breath cone (telegraph 1s + breath 3s damaging cone forward)

**Coordination R6-PARITY-005-IMPL** : R6-PARITY-005-IMPL crée les flags/data SO + behavior basic (ex: `EnableCharge` flag). Ce ticket utilise ces flags pour implémenter les SYSTÈMES MULTI-PHASE complexes (phase transitions, telegraphs, AoE patterns). Si conflit merge, ce ticket prend précédence pour les fichiers boss-specific.

## Task

### 1. Apocalypse_boss 4-phase state machine (~50 LOC)
- In `Enemy.cs` (ou extract `EnemyBossBehaviors.cs` si Enemy.cs déjà >2300 LOC) :
  - State enum : `ApocPhase { Normal, InvulSummons, Speed2x, AoEPulse }`
  - Transition triggers : 75% HP → P2, 50% HP → P3, 25% HP → P4
  - P1 : normal AI behavior
  - P2 : `IsInvulnerable = true`, summon 3 imp ennemis chaque 2s pendant 6s, puis transition
  - P3 : `SpeedMultiplier = 2.0`, normal damage, durée jusqu'à 25% HP
  - P4 : AoE pulse 360° autour boss tous les 1.5s, damage = 20 to castle in radius
- Cinematic transitions : `BossUI` ou notification ScreenFlash (existant ?)

### 2. Warlord/Brigand charge sprint (~15 LOC)
- Si `EnableCharge` (set par R6-PARITY-005-IMPL) et cooldown OK :
  - Telegraph : 1.5s wind-up animation (scale flash ou particle)
  - Sprint : 2× speed forward pendant 2s, damage castle si collision
  - Cooldown : 8s after sprint

### 3. Dragon fire breath cone (~15 LOC)
- Si `HasFireBreath` (set par R6-PARITY-005-IMPL) et cooldown OK :
  - Telegraph : 1s wind-up (open mouth animation ou particle preview)
  - Breath : 3s emit fire cone forward (ParticleSystem cone shape), damage = 10 per tick to towers in cone

## Exploit Unity (addendum scope)

- Animator state machines pour Apocalypse phase transitions (visual feedback)
- Cinemachine boss reveal cinematic (cf R6-PARITY-021 P2 BossUI cutscene plus tard)
- ParticleSystem cone shape pour fire breath
- AnimationCurve pour speed ramp warlord charge

## Hard rules

- Cap 500 LOC par fichier — Enemy.cs déjà ~2348 LOC, extract IMPÉRATIVE à `EnemyBossBehaviors.cs` si ajout >50 LOC
- No feature creep (juste ces 3 boss systems, pas autres bosses)
- Compile gate
- Self-report 100 mots max

## Deliverable

- Commit `feat(parity-v4-014): boss phases complete — Apocalypse 4 phases + warlord charge + dragon fire breath`
- Self-report : 3 bosses impl (3/3 ?), LOC ajoutées, extract `EnemyBossBehaviors.cs` done y/n + LOC, Unity exploits used (Animator, Cinemachine, ParticleSystem), compile OK, commit hash

## Time estimate

~5h
