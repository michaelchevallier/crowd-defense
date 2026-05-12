# Active sprint

> Mis à jour par Mike en chat ou par superviseur quand sprint change.
> L'exec lit ce fichier pour savoir quel scope est sealed.

## Current

- **Sprint** : R6-02 (DELETE pass)
- **Mode** : AUTONOME (Mike GO 14h22)
- **Started** : 2026-05-12 14h22
- **Time cap** : 2h00 (deadline 16h22)
- **Status** : ACTIVE, batch 1 (worktree E systems) dispatched
- **ScheduleWakeup interval** : 1800s (charter §4)
- **Backlog** : `.claude/specs/R6-EXEC/_backlog.md` (24 tickets)
- **Coordination** : `.claude/coordination/sprint-R6-02.md`

## Scope sealed R6-02

- 24 tickets DELETE pass (entities + UI + systems + endscreen)
- LOC delta target : **-6750 LOC**
- 3 file deletions : RandomMapGenerator.cs, ReplayRecorder.cs, Difficulty.cs
- Smoke test post-sprint : WebGL build clean + console clean + W1-1 jouable
- API contracts à préserver (cf sprint-R6-02.md)

## Mode change conditions

Mike peut écrire en chat :
- "GO autonomous sprint R6-02 time cap N heures" → switch autonome + ScheduleWakeup 1800s
- "GO batch dispatch N tickets" → dispatch supervisé batch (3-4 tickets)
- "STOP R6-02" → exit sprint, no rescheduleWakeup, push notif

## R6-01 historique (DONE)

- Track A : ✅ `.claude/audit/2026-05-12-triage-table.md` 56 rows + 3 decisions Mike validées (commit `d87125c`)
- Track B : ✅ bug-fixer 3 commits (`08778de` shaders URP, `7d3b1af` NullRef MinimapController, `1f93e3c` ArgumentNull TowerInfoPanel) + gh-pages deploy `50c5420`

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
