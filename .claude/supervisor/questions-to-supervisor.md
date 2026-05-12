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

### 2026-05-12 17h05 — Q-PARITY-V4-P1-cap-enemy
Type : tactical
Category : A
Blocking : false (P1 batch 8/8 shipped, fonctionnel, ship-able)
Question : Post-merge R6-PARITY-005-IMPL + R6-PARITY-014 (commit 08d7229), 2 fichiers violent cap 500 LOC charter §1 règle #3 :
- `Enemy.cs` 2051 LOC (legacy géant, hérité R6-02 partial, +1551 over cap)
- `EnemyBossBehaviors.cs` 582 LOC (extract 014 + 005-IMPL, +82 over cap, +16%)
Options envisagées :
- (a) Accept Enemy.cs legacy (hors-scope refacto immédiat) + ticket R6-PARITY-005-IMPL-REFACTOR P2 pour split EnemyBossBehaviors.cs 582 → 2 fichiers
- (b) Block + re-dispatch refacto immédiat pour les 2 fichiers
- (c) Précédent A-vfx-bindings-cap : accept + ticket en tête P2 batch
Reco interne : (a)+(c) hybrid — Accept legacy Enemy.cs (refacto-massif = sprint dédié hors batch P1), + ticket `R6-PARITY-005-IMPL-REFACTOR` en tête P2 pour split EnemyBossBehaviors.cs 582 → 2 fichiers cohésifs (ex : `EnemyBossBehaviors.Apocalypse.cs` 014-part + `EnemyBossBehaviors.Variants.cs` 005-IMPL part). Enemy.cs 2051 LOC est blocker stratégique massif — peut justifier sprint R6-04 dédié refacto god class.
Context : agent 005-IMPL self-report mentionne extract done EnemyBossBehaviors.cs 582 LOC, +16% cap. Précédent VfxPoolBindings 555 LOC → Mike a tranché accept + refacto P1 (A-vfx-bindings-cap). Cohérence : appliquer même décision.

---

### 2026-05-12 16h26 — Q-PARITY-V4-vfx-bindings-cap `[resolved]` 16h09 wakeup
Type : tactical / Category : A / Blocking : false
Question : VfxPoolBindings.cs 555 LOC > cap 500 LOC charter §1 règle #3 (+55 LOC, +11%). Que faire ?
**Réponse Mike-via-superviseur (A-PARITY-V4-vfx-bindings-cap 15h57)** : (a) ACCEPT + ticket R6-PARITY-004-REFACTOR **en tête P1 batch** (priorité absolue P1, dispatch AVANT autres P1).
**Action exec** :
- ✅ Ship-temp accepté (commit 57ffcd6 dans main)
- ✅ Note backlog P1 : R6-PARITY-004-REFACTOR ticket #1 P1
  - Plan split documenté par superviseur : partial class `VfxPoolBindings.cs` + `VfxPoolBindings.Modules.cs` OU extraction `VfxPoolTextures.cs` textures-only vs `VfxPoolBuilders.cs` Build*Module
- ⏸ Dispatch P1 attend instruction Mike (mode "P1 GO" via instructions-to-exec.md)
- Status : `[resolved]` 16h09

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
