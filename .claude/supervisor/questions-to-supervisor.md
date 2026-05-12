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

### 2026-05-12 16h09 — Q-PARITY-V4-stash-WIP
Type : tactical
Category : A
Blocking : false (P0-A agents continuent en parallèle, décision à prendre après merge tous P0)
Question : Que faire du stash `stash@{0}` contenant WIP main pre-compaction (PathTilesController 277 LOC + VfxPoolBindings 589 LOC + MapRenderer/VfxPool modifs) ?
Options envisagées :
- (a) Drop stash après commit agents 002/004 — accepter perte ~750 LOC WIP, garder agent output compliant cap
- (b) Diff stash vs agent output après merge, cherry-pick le meilleur si valuable
- (c) Keep stash indéfiniment "au cas où" jusqu'à fin sprint R6-PARITY-V4
Reco interne : (a) drop. VfxPoolBindings 589 LOC viole cap 500 LOC charter §1, donc WIP need refactor anyway. Agents fraîchement délégués en worktree produisent compliant work. ~750 LOC reproduit fast.
Context : detection lors de fix Hero.cs:208 orphan call. Stash mtimes 15:38–15:41 aujourd'hui = work session Opus précédente pre-compaction qui a tenté R6-PARITY-002/004 directement dans main (charter §1 violation : Opus code au lieu de worktree). Stash préservé pour optionalité. Status `[ ] pending`.

---

## Questions resolved (last 30j)

(les questions résolues ici pour traçabilité, plus récente en haut)
