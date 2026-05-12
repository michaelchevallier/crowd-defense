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

### 2026-05-12 15h57 — A-PARITY-V4-vfx-bindings-cap
For : Q-PARITY-V4-vfx-bindings-cap
Status : delegated-decided
Réponse : **(a) ACCEPT** + **ticket R6-PARITY-004-REFACTOR EN TÊTE DU BATCH P1** (priority within P1, dispatch AVANT autres P1).
Rationale :
1. 555 LOC > cap 500 = violation charter §1 règle #3 réelle mais mineure (+11%, helper class fonctionnel ship-able).
2. Block + re-dispatch (b) = perfectionnisme, peut attendre P1 batch sans bloquer P0 milestone.
3. Tolérer (c) = refusé, charter strict explicite "hard cap 500 LOC".
4. Hybrid (a)+priority : ship en main maintenant, refactor split en 1er ticket P1 (priorité absolue P1) pour pas créer dette qui s'accumule.
Caveats :
- Spec ticket R6-PARITY-004-REFACTOR doit documenter ship-temp + plan split (partial class `VfxPoolBindings.cs` + `VfxPoolBindings.Modules.cs` OU extraction `VfxPoolTextures.cs` textures-only vs `VfxPoolBuilders.cs` Build*Module).
- Audit batch P0 (a81562b6) a aussi recommandé le split — preuve indépendante du besoin.
- Ne pas oublier R6-PARITY-005-IMPL (5 PARTIAL enemies ~265 LOC) + R6-PARITY-004-IMPL (9 textures unmapped) dans batch P1 backlog.
Mike notified : true (T1 sprint complete P0-A + auto-handled violation, decision gate Mike pour mode dispatch P1)


---

## Answers resolved (last 30j)

(les réponses traitées par exec ici, plus récente en haut)
