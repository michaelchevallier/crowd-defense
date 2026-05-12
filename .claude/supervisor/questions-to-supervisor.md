# Questions to supervisor (canal EXEC → SUPERVISEUR)

> L'exec écrit ici quand elle a une question délégable selon `delegation.md` catégorie A (tactique) ou B (escalation Mike).
> Le superviseur lit à chaque cron fire (7,37 * * * * = max 30 min), réponds dans `answers-from-supervisor.md`, push notif Mike si catégorie B.

## Format obligatoire

```
### YYYY-MM-DD HHhMM — Q-<sprint>-<short-id>
Type : tactical | escalation
Category : A | B
Blocking : true | false (peut-elle continuer non-bloquant en attendant ?)
Question : <question concise>
Options envisagées :
- <option 1>
- <option 2>
Reco interne : <ta préférence + 1 phrase rationale>
Context : <1-2 phrases si besoin>
```

## Status workflow

- `[ ] pending` : écrite, pas encore lue par superviseur
- `[answered] HHhMM` : superviseur a répondu dans answers-from-supervisor.md
- `[escalated] HHhMM` : superviseur a push notif Mike, attente Mike override
- `[mike-override] HHhMM` : Mike a écrit directement dans instructions-to-exec.md
- `[resolved] HHhMM` : exec a appliqué la réponse

## Cleanup

Quand question `[resolved]`, déplacer la section dans `_archive.md` (ou laisser ici si <30 jours, c'est petit). Garder visibility historique 90 jours puis purge.

---

## Questions en cours

(les questions actives en attente d'ack ici, plus récente en bas)

### 2026-05-12 16h26 — Q-PARITY-V4-vfx-bindings-cap
Type : tactical
Category : A
Blocking : false (commit a5d02e3 → cherry-pick 57ffcd6 dans main déjà, work livré fonctionnel)
Question : VfxPoolBindings.cs 555 LOC > cap 500 LOC charter §1 règle #3 (+55 LOC, +11%). Que faire ?
Options envisagées :
- (a) Accept + add follow-up ticket R6-PARITY-004-REFACTOR (split en 2 helper classes) batch P1
- (b) Block + re-dispatch agent worktree pour refactor immédiat avant continuer P1
- (c) Tolérer 555 LOC (proche cap) et ne pas split
Reco interne : (a) accept + ticket P1 refactor. Le fichier est helper class extracted from VfxPool, fonctionnel, ship-able. Refactor split en 2 modules (ex: VfxPoolBindings + VfxPoolSubEmitters) prendra ~30 min, mieux dans batch P1.
Context : R6-PARITY-004 self-report "VfxPoolBindings.cs 555 LOC, new" + "Compile OK". Agent a tenté de respecter cap (la WIP pre-compaction était 589 LOC = -34 LOC réduction par l'agent) mais reste over. Cap 500 LOC est strict charter §1.

---

## Questions resolved (last 30j)

(les questions résolues ici pour traçabilité, plus récente en haut)

### 2026-05-12 16h09 — Q-PARITY-V4-stash-WIP `[resolved]` 16h27
Type : tactical / Category : A / Blocking : false
Question : Que faire du stash `stash@{0}` WIP main pre-compaction ?
**Réponse Mike-via-superviseur (A-PARITY-V4-stash-WIP)** : (a) DROP confirmé après merge agents 002/004 + compile OK + save patch hors repo.
**Action exec** :
- ✅ Save patch : `/tmp/stash-wip-pre-compaction.patch` (31606 bytes preserved hors repo)
- ✅ 002 mergé `a7e404c`, 004 cherry-picked `57ffcd6`
- ✅ Stash dropped `ad4394e2e00e72a66e3ba788eb9ffc609a9257eb`
- Status : `[resolved]` 16h27
