# Delegation rules — quoi router à qui

> Définit les frontières entre questions **déléguées au superviseur** (canal async fichier Q&A) et questions **escaladées à Mike** (push notif chat). Mike override toujours possible via commit direct dans `instructions-to-exec.md`.

## Catégorie A — DÉLÉGUÉES au superviseur

L'exec écrit dans `questions-to-supervisor.md` et **continue son boulot non-bloquant** pendant qu'elle attend. Worst-case ack 30 min (interval cron `7,37 * * * *`).

- **Mode dispatch sprint** : supervisé batch / autonome time cap / manuel
- **Ordre tickets, priorité, premier batch** dans backlog sealed
- **Choix worktree** (A vs B) sans impact game design ni cross-references
- **Bug-fixer scope** si <50 LOC et root cause clair
- **Refacto local évident** non listé mais qui débloque (ex: extract method)
- **Conflit naming interne** (sans impact spec ni brand)
- **Stratégie merge worktrees** (rebase vs merge commit)
- **Polling interval** côté autre session si elle veut accélérer Q&A
- **Validation post-ticket compile gate** (auto via mon cron, pas besoin de demander)

## Catégorie B — ESCALADÉES à Mike (push notif)

L'exec écrit dans `questions-to-supervisor.md` avec tag `escalation:true`. Je push notif Mike immédiatement à mon prochain check + write recommandation tentative dans `answers-from-supervisor.md` (Mike override toujours possible).

- **Conflit Q1-Q18 / spec ambiguity** (Q14 castleHP 120 vs 200, etc.)
- **Changement scope** : add/remove tickets dans backlog sealed
- **Phase advancement** (R6-02 → R6-03, sprint termination)
- **Architecture cross-feature** (touche 2+ god classes ou 2+ systèmes)
- **Polish / VFX / new content** (toujours Mike — sprint R6-06 only)
- **Build target multi-platform** (Steam/iOS/Android : credentials)
- **Backwards compat / breaking change** runtime ou API
- **Time cap modification** (extend, shorten)
- **Mode change** sprint en cours (de batch à autonome ou inverse)
- **Decision Mike originale réversée** (changer d'avis sur un Q-N)
- **Dépendance tierce nouvelle** (npm/uvx/UPM package) — cf memory `feedback_dependency_due_diligence`

## Catégorie C — INTERDITES (charter §1 enforced)

L'exec NE doit PAS faire ces décisions, même avec délégation. Mike obligatoire chat direct.

- Sub-Opus spawn
- ScheduleWakeup hors loop sprint autorisé
- Feature creep (ajout feature hors backlog sealed)
- Build/deploy auto sans validation
- Refacto god class sans ticket REFACTO Mike-validé
- Commit avec message non-référencé (Q-N / triage row / R6-EXEC ticket manquant)

## Default si question non catégorisée

Tag `category:uncategorized` → escaladée Mike automatiquement.

---

# Push notification tiers Mike

## T1 — IMMÉDIATE (push notif sans batch, urgence haute)

- **Build broken** WebGL : compile fail non auto-fixable
- **Runtime exceptions** ≥ 3 nouvelles depuis dernier gate vert
- **LOC delta** > +5000 single sprint (creep alert)
- **Time cap** hit (sprint pas fini)
- **Sub-Opus spawn** détecté (charter violation §1 règle #10)
- **Sprint complete** (good news, partial OK aussi)
- **Question catégorie B** : escalation tag in `questions-to-supervisor.md`

Format T1 (concis, actionable) :
```
🎯 [T1] crowd-defense — <1-line context>

<question ou état>

Action attendue :
- A. <option 1>
- B. <option 2>

Fichier ref : .claude/supervisor/<file>
Prochain auto check : HH:MM
```

## T2 — BATCHED (peut attendre next check 30 min, agrégé si plusieurs)

- **Drift criterion D1-D9** 1ère occurrence (warning, pas confirmed)
- **Suggestion superviseur scope** (ex: "j'ajouterais ticket X")
- **Phase advancement ready** (sprint done, next attente Mike)
- **Stale ack** : exec silence > 2 wakeups consecutif

Format T2 :
```
📊 [T2] crowd-defense digest <HHhMM>

- <signal 1>
- <signal 2>

Prochain auto check : HH:MM. Pas urgent.
```

## T3 — LOG ONLY (silent, append `_clean-log.md`)

- Clean checks (rien à signaler)
- Acks routine (exec ack mes instructions standard)
- Commits avec ref Q-N / triage / R6-EXEC OK
- LOC stable ou descendant (sprint DELETE attendu)

## Anti-spam

- T1 jamais batched (chacun standalone)
- T2 agrégé si > 1 dans même check (1 push notif avec digest)
- T3 zéro push (uniquement clean-log)
- Max 4 push notif Mike par heure (rate limit). Si dépassé → switch tous T2 en T3 + 1 push final "supervisor saturated, multiple signals, check drift-reports/"
