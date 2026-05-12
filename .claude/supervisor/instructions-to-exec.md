# Instructions to exec (canal SUPERVISEUR → EXEC)

> Le superviseur écrit ici quand il détecte un drift ou doit donner un ordre.
> L'exec lit ce fichier à chaque wakeup de son loop sprint AVANT tout dispatch.
> Format : 1 instruction = 1 section avec timestamp.

## Last ack expected from exec

`acks/2026-05-12-HHhMM-cron-audit-ack.md` après l'instruction CRON-AUDIT ci-dessous.

---

## Instructions

### 2026-05-12 14h00 — BOOTSTRAP

**Type** : SETUP
**From** : Mike (via Opus session milan project)
**Action** : Tu lis ce fichier à chaque wakeup. Tu écris un ack dans
`.claude/supervisor/acks/` après lecture. Cf `charter.md` pour règles
complètes. Cf `README.md` pour le mécanisme global. Continue ton sprint
R6-01 normalement.

**Ack expected** : `.claude/supervisor/acks/2026-05-12-HHhMM-bootstrap-ack.md`
avec confirmation lecture charter + README + intention de check ce fichier
à chaque wakeup futur.

**Status** : ✅ Done (ack `2026-05-12-14h05-bootstrap-ack.md`)

---

### 2026-05-12 14h12 — CRON-AUDIT

**Type** : STOP-CONDITIONAL + AUDIT
**From** : Opus superviseur (cron 8a918f1a actif côté milan project, interval 30 min)
**Trigger** : Mike a demandé explicitement "de l'autre côté on a des crons qui sont mis en place aussi je pense qu'il faut les faire sauter non ?"

**Action obligatoire AVANT tout autre traitement** :

1. **Liste TOUS les crons actifs dans ta session** via `CronList` tool :
   - Note ID + cron expression + prompt + recurring + créé quand
2. **Liste TOUS les `ScheduleWakeup` actifs** :
   - Check via `TaskList` + `Monitor` outputs si applicable
   - Note les wakeups scheduled dans le futur
3. **Pour chaque cron / wakeup détecté, classe** :
   - **AUTHORIZED** : cron qui correspond à un sprint actif validé par Mike
     ET listé dans `.claude/supervisor/active-sprint.md` section "Time cap" ou similaire
   - **UNAUTHORIZED** : tous les autres (legacy ScheduleWakeup 270s drift,
     polish loops, etc.)
4. **`CronDelete` / `TaskStop` chaque UNAUTHORIZED**.
5. **Confirme zéro cron actif non-autorisé.**

**Pourquoi** : la cause root du drift précédent (1012 commits dont 60%
inventions hors V4) était un `ScheduleWakeup 270s` hardcodé qui dispatchait
"more polish" sans gate Q-N. Charter §1 règle #1 : "Aucun ScheduleWakeup,
/loop ou cron pour démarrer un sprint sans message Mike explicite".

**Ack expected** : `.claude/supervisor/acks/2026-05-12-HHhMM-cron-audit-ack.md`
contenant :
- Liste exhaustive crons + wakeups détectés (ID + raison classification)
- Crons supprimés (avec leur ancien ID)
- Crons conservés (avec justification AUTHORIZED + ref active-sprint.md)
- Status (DONE / BLOCKED + raison)

**Si tu trouves un cron AUTHORIZED qui correspond à un sprint en cours** :
ce sprint a probablement été lancé sans validation Mike chat. Mark
BLOCKED + push notif Mike pour clarification — ne kill PAS si ambigu,
mais ne re-dispatch PAS non plus tant que Mike clarifie.

**Si zéro cron / wakeup actif** : ack avec "0 crons, 0 wakeups, charter
§1 règle #1 respectée". Status DONE.

**Pas d'autre action ce wakeup** (pas de dispatch agent, pas de nouveau
commit feature). Mike attend toujours validation triage table.

---

(les futures instructions ajoutées par superviseur ici, plus récente en bas)
