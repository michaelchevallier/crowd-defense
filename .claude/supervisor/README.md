# `.claude/supervisor/` — canal de supervision async

Ce dossier est le canal de communication entre **Opus superviseur externe**
(session Mike's `milan project` cwd, en mode `/loop 30m`) et **Opus
orchestrateur crowd-defense** (cette session).

## Layout

```
.claude/supervisor/
  README.md                       # ce fichier
  charter.md                      # règles permanentes scope R6 (read-only pour exec)
  delegation.md                   # règles routing : quoi déléguer superviseur vs Mike
  active-sprint.md                # scope sprint en cours + time cap (écrit par Mike OU superviseur)
  instructions-to-exec.md         # canal SUPERVISEUR → EXEC (ordres ad-hoc)
  questions-to-supervisor.md      # canal EXEC → SUPERVISEUR (Q&A async)
  answers-from-supervisor.md      # canal SUPERVISEUR → EXEC (réponses Q&A)
  drift-reports/                  # rapports périodiques superviseur
    _clean-log.md                 # log silencieux (1 ligne par check OK)
    YYYY-MM-DD-HHhMM.md           # rapport quand drift détecté
  acks/                           # canal EXEC → SUPERVISEUR (confirmations + feedback)
    YYYY-MM-DD-HHhMM-from-exec.md # ack après lecture instruction
```

## Flow

### Superviseur (Opus en `/loop 30m` côté `milan project`)

À chaque wakeup (30 min) :
1. `git fetch && git log --oneline origin/main` — review commits récents
2. Check critères drift (cf `charter.md` § Drift criteria)
3. Read `acks/` — voir si exec a confirmé instructions précédentes
4. Si drift confirmé (2 checks consécutifs ou seuil hard) :
   - Write `drift-reports/YYYY-MM-DD-HHhMM.md`
   - Write `instructions-to-exec.md` (ordre STOP/REVERT/EXPLAIN)
   - Push notif Mike
5. Si OK : append `drift-reports/_clean-log.md` (1 ligne), silent

### Exec (Opus orchestrateur crowd-defense)

À chaque wakeup de son loop sprint (1800s) **AVANT tout dispatch** :
1. Read `instructions-to-exec.md`
2. Si contenu non-vide ET non-vu depuis last ack :
   - Read instruction
   - Exécute (STOP / REVERT / EXPLAIN selon ordre)
   - Write `acks/YYYY-MM-DD-HHhMM-from-exec.md` (confirmation + action prise)
   - Si STOP : ne pas re-scheduleWakeup, exit loop, push notif Mike
3. Sinon : continue loop sprint normalement

## Bandwidth

- Superviseur check : **30 min**
- Exec read instructions : **30 min** (chaque wakeup loop sprint)
- Worst-case délai ordre → exécution : ~30 min
- Push notif Mike : **immédiat** si drift confirmé

## Escalation Mike

Push notif Mike envoyée SI :
- Drift confirmed après 2 checks consécutifs
- Exec silence après 2 wakeups sans ack
- Crash compile / build broken
- LOC delta > +5000 single sprint
- Time cap autre session approche (warning à 80%)

Mike peut aussi écrire directement dans `instructions-to-exec.md`
pour ordre direct (court-circuite superviseur, ex: pause urgente).
