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

### 2026-05-13 03h00 — Q-R7-007 cleanup diag code [answered] 10h15
Type : escalation
Category : B
Blocking : false (autres tickets parallèle continue)
Question : Supprimer `Assets/Scripts/Systems/EnsureMainCamera.cs` + `Assets/Scripts/Systems/ImguiDiagOverlay.cs` (diag iter#9 + #12 cascade post-WebGL épuisée) ?
Risque : Si Menu.unity n'a aucune Camera explicite, suppression EnsureMainCamera → écran noir Editor Play mode.
Options envisagées :
- (a) Supprimer les deux (cleanup intégral)
- (b) Garder EnsureMainCamera (failsafe utile), supprimer ImguiDiagOverlay (debug-only orphelin)
- (c) Garder les deux (no-op pas urgent)
Reco interne (et superviseur spec) : (b) — failsafe utile, ImguiDiagOverlay = debug pur post-cascade
Context : R7-007 P1 ticket open-ended

### 2026-05-13 03h00 — Q-R7-016 events V4 fidelity [answered] 10h15
Type : escalation (design Mike)
Category : B
Blocking : false (autres tickets pioche continue)
Question : Refacto DynamicEventManager → V4 strict (8 events data-driven via `level.events[]`) OU keep Unity simplification (3 events random `% 5`) + documenter divergence intentionnelle ?
- V4 strict : void_pulse + zero_g + undertow + battle_cry + hack (5 missing) + sand_storm + lava_surge + carousel_spin (3 OK) = 8 events full parity. Effort 2-3h.
- Unity keep : 3 random events fonctionnel mais moins variety. Effort 0h.
Reco interne : V4 strict si parity priority (sprint nom "R7-PUSH-100") sinon keep
Context : R7-016 P3 ticket

### 2026-05-13 03h00 — Q-R7-018 squash supervisor commits [answered] 10h15
Type : escalation
Category : B
Blocking : false
Question : Squash 5 supervisor commits + 3 ProjectilePool redondants via `git rebase -i` + force-push ?
Risque : Rewrites public history origin/main. Si Mike a worktree local pointant commits squashés, sync break.
Options :
- (a) Squash + force-push (hygiene clean, risk break local refs)
- (b) Skip squash (history hygiene < workflow risk)
Reco interne (et superviseur spec) : (b) SKIP — commits supervisor T3 silent log non bruyant, ProjectilePool 3× = no-op asset state (verified R7-008)

### 2026-05-13 03h00 — Q-R7-026 WebGL fix strategy [escalated-investigation] 10h15
Type : escalation (Mike chat live directive long-terme)
Category : B
Blocking : false (test Editor mode actif suffit dev/test full pour le moment)
Question : Quelle option fix WebGL distribution (V4 marchait nickel, V6 cassé Unity 6 + URP 17.3 + WebGL2 subpass input shader) ?
Options envisagées :
- (a) Wait Unity 6.1 / URP 17.4+ patch official (0h effort, ETA inconnue semaines-mois)
- (b) Downgrade Unity 2022.3 LTS + URP 14.x stable WebGL2 (4-8h migration + retest 60 levels, perte features Unity 6 input v2 etc.)
- (c) Switch BiRP Built-in Render Pipeline (6-12h migration shaders/materials, perte URP custom passes)
- (d) Strip URP CoreCopy custom + force fallback shader path (2-4h R&D pragmatique)
- (e) Switch WebGPU experimental target (2-3h test, browser compat Chrome only stable)
Reco interne (et superviseur spec) : (a) si patience OK (Editor mode permet dev/test full), sinon (d) pour pragmatisme rapide.
Context : R7-026 P1 long-terme. Mike chat 02h55 : "WebGL medium distribution #1, V4 marchait nickel, V6 doit re-marcher web une fois"

### 2026-05-13 00h55 — Q-N9-80levels-design-decisions
Type : escalation (design Mike)
Category : B
Blocking : false (impl N12 attend, autres tickets pioche continue)
Source : N9 game-designer spec `~/.claude/specs/N9-80levels-rebalance.md` (hors repo)
Questions design Mike :
- **Q9-1** : W*-9 endurance levels (10 levels 10 waves chacun) — keep 10 waves (BTD6 endurance pattern, 11% content share) OU reduce à 5 waves spec D1-04 ? Reco game-designer : keep (precedent industry).
- **Q9-2** : W9/W10 wave1 ramp factor — 0.40 (conservatif) vs 0.50 (medium) ? Affecte 18 levels. Reco : 0.45 hybride.
- **Q9-3** : LevelDifficultyMul overrides W3-W8 L2-L7 — cleanup now (apply formula uniformly) OR keep manual tuning ? Reco game-designer : cleanup pour cohérence.
- **Q9-4** : Castle HP W2-1 régression 130 vs W1-9 160 — bug formule ? Reco fix difficultyMul L9 = 1.30f.
Context : 90 levels content scale audit (50 WARN, 0 FAIL). Mike décide direction balance. Status `[ ] pending`.

---

### 2026-05-12 18h18 — Q-P3-enemy-refacto-cap `[resolved]` 18h25 exec-auto-action
Type : tactical / Category : A / Blocking : false
Question : P3.1 R6-REFACTO-ENEMY core reste 850 LOC > cap 500. Que faire ?
**Action exec autonome (cohérence A-vfx-bindings-cap)** : dispatched R6-REFACTO-ENEMY-CORE-V2 bug-fixer immédiat ~2.5 min.
**Résultat** :
- ✅ Enemy.cs 850 → 491 LOC (under cap)
- ✅ Enemy.Init.cs 217 LOC nouveau
- ✅ Enemy.Update.cs 161 LOC nouveau
- ✅ Enemy.Lifecycle.cs 44 LOC nouveau
- ✅ ALL partials Enemy ecosystem (11 fichiers) under cap 500 LOC strict
- ✅ Zero behavior change
- ✅ Commit `d79b853` push origin main
- Status : `[resolved]` 18h25

---

### 2026-05-12 17h05 — Q-PARITY-V4-P1-cap-enemy `[resolved]` 18h53 (supervisor auto-detect)
**Auto-resolved par supervisor BACKLOG-WAVE-3 commit `6dac3cd`** : `EnemyBossBehaviors.cs` re-mesuré 582 → 446 LOC (sous cap 500). `Enemy.cs` resolved via P3.1 + R6-REFACTO-ENEMY-CORE-V2 → 491 LOC (sous cap).
Original Q below for traceability.

### 2026-05-12 17h05 — Q-PARITY-V4-P1-cap-enemy (original)
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
