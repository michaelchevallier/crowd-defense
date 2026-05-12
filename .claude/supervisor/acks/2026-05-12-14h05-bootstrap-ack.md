# Ack BOOTSTRAP supervisor channel

**From** : Opus exec orchestrateur (cwd `/Users/mike/Work/crowd-defense`)
**To** : Opus superviseur (cwd `milan project`, `/loop 30m`)
**Timestamp instruction lue** : 2026-05-12 14h00
**Timestamp ack** : 2026-05-12 14h05
**Status** : DONE

## Confirmation lecture

- ✅ Read `.claude/supervisor/README.md` — mécanisme global compris :
  - Layout dossier supervisor/ (charter, active-sprint, instructions-to-exec, acks/, drift-reports/)
  - Bandwidth 30 min worst-case, push notif Mike immédiat si drift confirmé
- ✅ Read `.claude/supervisor/charter.md` — règles permanentes R6 intégrées :
  - 10 guard rails (trigger=Mike chat, gate avant dispatch, cap 500 LOC, no-feature-creep, compile+console gate, no polish hors R6-06, coordination centrale, self-report agents, max 4 worktrees, no Sub-Opus)
  - 12 drift criteria (D1-D12) avec seuils hard et actions superviseur correspondantes
- ✅ Read `.claude/supervisor/active-sprint.md` — scope R6-01 noté :
  - Track A triage table 40-70 lignes
  - Track B 1 Sonnet bug-fixer 5 bugs runtime
  - Hard prohibitions : no ScheduleWakeup, no Sub-Opus, no commit code feature pendant triage
- ✅ Read `.claude/supervisor/instructions-to-exec.md` — instruction BOOTSTRAP 14h00 lue

## Workflow engagé

À chaque wakeup loop sprint (R6-02+) ET avant chaque nouveau dispatch agent en mode supervisé R6-01 :

1. Read `.claude/supervisor/instructions-to-exec.md`
2. Si nouvelle instruction (timestamp > dernier ack `acks/`) :
   - Read full instruction
   - Exécute selon directive (STOP / REVERT / EXPLAIN / WARN / etc.)
   - Write `.claude/supervisor/acks/YYYY-MM-DD-HHhMM-from-exec.md` avec timestamp lu, action prise, commits affectés, status (DONE/IN_PROGRESS/BLOCKED)
   - Si STOP : exit loop, no rescheduleWakeup, push notif Mike
3. Sinon : continue loop normal

## Status sprint R6-01 actuel

- **Track A** ✅ DONE : `.claude/audit/2026-05-12-triage-table.md` (56 rows)
- **Track B** ✅ DONE : bug-fixer commits `08778de` (3 shaders URP fix) + `7d3b1af` (NullRef Minimap) + `1f93e3c` (ArgumentNull TowerInfoPanel) + deploy gh-pages `50c5420`

🛑 Attente validation Mike triage table + 3 questions critiques (Q14 castleHP, Tower research, Roguelike/EndScreen FREEZE vs DELETE) avant R6-02 GO.

## Commit + push

Commit ce fichier sur main, push origin/main pour visibilité superviseur.
