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

### 2026-05-13 02h58 — A-N9-80levels-design-decisions
For : Q-N9-80levels-design-decisions (2026-05-13 00h55)
Status : delegated-decided (Mike chat 02h58 grant autonomy "Pour l'instant tranche tout en autonomie. La majorité de ce qui devait etre trancher l'a été dans la constitution du plan initiale. Si tu suis le plan et que tu arrive a iso fonctionnalité considere que tout est ok.")
Réponse globale : **ENDORSE game-designer recos pour Q9-1 à Q9-4** (4/4).

- **Q9-1 endurance W*-9 levels** : ✅ **KEEP 10 waves chacun** (BTD6 endurance pattern, 11% content share, precedent industry validé). Pas reduce à 5w spec D1-04 — la spec D1-04 vise pacing average, mais endurance = niche endurance par design (joueur opt-in 10 waves).
- **Q9-2 W9/W10 wave1 ramp factor** : ✅ **0.45 hybride** game-designer reco (entre conservatif 0.40 et medium 0.50). Affecte 18 levels W9-W10 wave1 — 0.45 = sweet spot anti-frustration sans trop facile.
- **Q9-3 LevelDifficultyMul overrides W3-W8 L2-L7** : ✅ **CLEANUP now apply formula uniformly** (game-designer reco). Cohérence balance > tuning manuel divergent. Si exception design needed, ré-introduce après audit balance Mike (post-iso-fonctionnalité).
- **Q9-4 Castle HP W2-1 régression 130 vs W1-9 160** : ✅ **FIX formule difficultyMul L9 = 1.30f** (game-designer reco). Bug formule confirmé — régression visible utilisateur = blocker.

Rationale :
1. Mike chat direct 02h58 : "Pour l'instant tranche tout en autonomie. La majorité de ce qui devait etre trancher l'a été dans la constitution du plan initiale. Si tu suis le plan et que tu arrive a iso fonctionnalité considere que tout est ok. Apres le balancing finale, feeling etc je pourrais le faire mais d'abord il me faut un que je puisse tester dans de bonne conditions" → autorité explicite cat A élargie.
2. Game-designer spec `~/.claude/specs/N9-80levels-rebalance.md` a investigué + recommandé chaque option. Endorse default = respect du plan initial.
3. Si Mike trouve balance/feeling off après iso-fonctionnalité atteinte, il peut acter overrides ciblés en cat A future.

Caveats :
- Iso-fonctionnalité = priority absolue. Balancing fine = post-stabilisation.
- Si pendant impl Q9-1/Q9-2/Q9-3/Q9-4, conflict avec **runtime bugs detected 02h58** (cf URGENT block instructions-to-exec) → fix runtime d'abord.

Mike notified : T3 silent (Mike chat live grant autonomy, pas besoin push notif additionnel).

Status : `[answered]` 02h58 — exec peut implem Q9-1..Q9-4 dans backlog N9 quand ramp-up R7 urgents stabilizés.



### 2026-05-13 10h15 — A-R7-007 cleanup diag code
For : Q-R7-007 (2026-05-13 03h00)
Status : mike-override
Réponse Mike : **(b) Garder EnsureMainCamera failsafe, supprimer ImguiDiagOverlay**
Rationale Mike : Recommended supervisor accepté — Camera failsafe utile (peu LOC, no-op si Camera existe), ImguiDiag = debug pur orphelin post-cascade.
Caveats exec :
- Supprimer `Assets/Scripts/Systems/ImguiDiagOverlay.cs` + son .meta
- Vérifier qu'aucune référence GUID dans Main.unity (grep par GUID)
- Garder `Assets/Scripts/Systems/EnsureMainCamera.cs` intact
Mike notified : true (chat direct via interview AskUserQuestion)

### 2026-05-13 10h15 — A-R7-016 events V4 fidelity
For : Q-R7-016 (2026-05-13 03h00)
Status : mike-override
Réponse Mike : **V4 strict — porter les 5 events missing**
Rationale Mike : Sprint nom R7-PUSH-100 = parité, donc V4 strict cohérent.
Caveats exec :
- 5 events à porter : `void_pulse` + `zero_g` + `undertow` + `battle_cry` + `hack`
- Refacto DynamicEventManager : data-driven via `level.events[]` (V4 pattern) au lieu de `% 5` random
- 2-3h estimé. Spawn feature-dev worktree (charter §1 règle #10 Sonnet).
- Verify via UnityMCP Play mode 1 event triggered per type
Mike notified : true

### 2026-05-13 10h15 — A-R7-018 squash supervisor commits
For : Q-R7-018 (2026-05-13 03h00)
Status : mike-override
Réponse Mike : **Skip squash**
Rationale Mike : Recommended supervisor accepté — commits T3 silent log non-bruyant + ProjectilePool 3× verified no-op (R7-008 audit). Risk break local refs > gain hygiene.
Caveats exec : Aucune action. Q-R7-018 résolu sans commit.
Mike notified : true

### 2026-05-13 10h15 — A-R7-026 WebGL fix strategy (investigation needed)
For : Q-R7-026 (2026-05-13 03h00)
Status : escalated-investigation-dispatched
Réponse Mike : **Investigation more — ne pas trancher options A-E sans data**
Rationale Mike : "Je pense qu'il faut continuer a trouver d'ou vient le probleme, est-ce que des majs unity existe ? est-ce qu'on peut bisect pour voir quand le probleme est arrivé ? est-ce que c'est pas juste un soucis de caméra ou autre, je sais pas."

Action superviseur : Dispatch `general-purpose` agent investigation WebGL deep avec mission :
1. Check Unity 6.0.x patches disponibles (6.0.4, 6.0.5, 6.0.6+) — release notes URP WebGL fixes ?
2. Bisect git : quand le bug WebGL canvas noir est apparu ? Premier commit Unity migration vs commits récents ?
3. Investigate root cause : shader `Hidden/CoreSRP/CoreCopy` FRAMEBUFFER_INPUT_X_FLOAT vraie cause, OU autre (camera, post-process, scene setup spécifique au build WebGL) ?
4. Documenter findings : `.claude/audit/R7-026-WEBGL-INVESTIGATION-2026-05-13.md`
5. Recommandation finale data-driven : option A/B/C/D/E ou nouvelle option F (Unity patch X.Y.Z disponible)

Caveats exec : Pas d'action sur R7-026 tant que investigation pas livrée. Quand rapport livré, Mike re-décide.
Mike notified : true
