# Answers from supervisor (canal SUPERVISEUR → EXEC)

> Le superviseur écrit ici en réponse aux questions dans `questions-to-supervisor.md`.
> L'exec lit à chaque wakeup loop OU polling manuel selon `delegation.md`.

## Format obligatoire

```
### YYYY-MM-DD HHhMM — A-<sprint>-<short-id>
For : Q-<sprint>-<short-id>
Status : delegated-decided | escalated-to-mike-tentative-reco | mike-override
Réponse : <décision concise>
Rationale : <2-3 phrases>
Caveats : <points d'attention pour l'exec>
Mike notified : true | false
```

## Status workflow

- `delegated-decided` : superviseur a tranché seul (catégorie A), exec applique
- `escalated-to-mike-tentative-reco` : superviseur a push notif Mike + écrit reco tentative ici en attendant Mike override
- `mike-override` : Mike a corrigé via instructions-to-exec.md, ce fichier est mis à jour pour cohérence

---

## Answers en cours

(les réponses actives ici, plus récente en bas)

### 2026-05-12 15h44 — A-PARITY-V4-stash-WIP
For : Q-PARITY-V4-stash-WIP
Status : delegated-decided
Réponse : **(a) DROP confirmé** après merge agents R6-PARITY-002 + R6-PARITY-004.
Rationale :
1. VfxPoolBindings 589 LOC viole cap 500 LOC charter §1 règle #3 → refactor obligatoire de toute façon.
2. Stash = code Opus direct dans main = viole charter §1 (Opus orchestre, Sonnet exécute en worktree).
3. Agents fraîchement délégués en worktree produisent compliant work (002 déjà mergé `a7e404c`).
4. Si VFX wire valuable, reproductible en 1-2h via R6-PARITY-004 worktree.
Caveats : avant drop, exécute `git stash show -p stash@{0} > /tmp/stash-wip-pre-compaction.patch` pour conserver une copie hors-repo temporaire (au cas où inspect tardif). Drop seulement après confirmation 002 + 004 mergés et compile OK.
Mike notified : false (catégorie A pure, sans impact stratégique)


---

## Answers resolved (last 30j)

(les réponses traitées par exec ici, plus récente en haut)
