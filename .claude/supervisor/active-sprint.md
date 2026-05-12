# Active sprint

> Mis à jour par Mike en chat ou par superviseur quand sprint change.
> L'exec lit ce fichier pour savoir quel scope est sealed.

## Current

- **Sprint** : R6-01 (triage + bug-fixer runtime)
- **Mode** : supervisé Mike (pas autonome)
- **Started** : 2026-05-12
- **Time cap** : aucun (mode supervisé)
- **Status** : en cours (Track A triage table + Track B bug-fixer en parallèle)

## Scope sealed

- Track A : produire `.claude/audit/2026-05-12-triage-table.md` (40-70 lignes max)
- Track B : 1 Sonnet bug-fixer en worktree, fix 5 bugs runtime
  (3 shaders URP + NullRef + ArgumentNull)
- Aucun autre commit code feature autorisé pendant R6-01

## Backlog file

- N/A pour R6-01 (mode triage + bug-fixer 1-shot)
- R6-02+ : `.claude/specs/R6-EXEC/_backlog.md` à créer post-validation Mike

## Coordination file

- N/A pour R6-01
- R6-02+ : `.claude/coordination/sprint-R6-02.md`

## Hard prohibitions actives

- Aucun ScheduleWakeup pendant R6-01
- Aucun Sub-Opus spawn
- Aucun commit code source pendant triage table production
- Pas de feature creep "en passant"

## History sprints

- R6-01 : 2026-05-12 → in_progress
- R6-02 DELETE : pending (après Mike valide triage)
- R6-03 REFACTO god classes : pending
- R6-04 Q1-Q18 implementation : pending
- R6-05 V4 parity gap : pending
- R6-06 POLISH (si Mike priorise) : pending
