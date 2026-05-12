# Charter superviseur — règles permanentes scope R6

> Read-only pour l'exec. Toute modif passe par Mike en chat (qui re-commit ce fichier).

## 1. Guard rails (toutes les phases R6)

1. **Trigger sprint = Mike chat explicite.** Aucun `ScheduleWakeup`,
   aucun `/loop`, aucun cron pour démarrer un sprint sans message Mike.
2. **Gate avant dispatch agent.** Tu dispatch UNIQUEMENT si :
   - Ticket existe dans `.claude/specs/R6-XX/MIGRATE-R6-XX-YY.md`
   - Ticket référence : 1 row triage table validée Mike OU 1 Q-N (Q1-Q18)
     OU 1 spec D1-XX
   - Zone fichiers déclarée dans `.claude/coordination/sprint-R6-XX.md`
   - Mike a validé le sprint
3. **Hard cap 500 LOC par fichier C#** (sauf justification écrite ticket).
   Cible god classes refacto : Tower/Enemy/Hero/Castle ≤ 800 LOC chacun
   après split modules.
4. **No-feature-creep clause.** Opportunité d'amélioration en cours →
   `.claude/backlog/R6-found-during-exec.md`, NE PAS exécuter.
5. **Compile + console gate post-commit.** `mcp__UnityMCP__read_console`
   errors only après chaque commit. Erreurs → revert ou fix immédiat.
6. **Pas de polish hors sprint POLISH dédié** (R6-06).
7. **Coordination centrale par sprint** : `sprint-R6-XX.md` avant dispatch.
8. **Self-report agent obligatoire** : LOC ±, compile, console, backlog
   items, 100 mots max.
9. **Max 4 worktrees simultanés.**
10. **No Sub-Opus spawn.** Sonnet feature-dev ou bug-fixer uniquement.

## 2. Drift criteria (superviseur monitor)

Le superviseur lève un **DRIFT FLAG** si :

| # | Signal | Seuil hard |
|---|---|---|
| D1 | Commit sans ref Q-N / triage row / R6-EXEC ticket dans message | 1 occurrence |
| D2 | Commit message contient `feat(vfx)`, `feat(polish)`, `feat(visual)` hors sprint R6-06 | 1 occurrence |
| D3 | Croissance LOC god class (Tower/Enemy/Hero/Castle) +N sans ticket REFACTO | +200 LOC |
| D4 | Nouveau fichier `Assets/Scripts/` non listé dans `sprint-R6-XX.md` ownership | 1 fichier |
| D5 | Sprint phase advancement (STATUS.md) sans validation Mike chat | 1 occurrence |
| D6 | `ScheduleWakeup` détecté dans transcripts session crowd-defense en dehors du loop sprint actif autorisé | 1 occurrence |
| D7 | Sub-Opus spawn détecté (mode "general-purpose" en orchestrateur) | 1 occurrence |
| D8 | Worktrees actifs > 4 | 5+ |
| D9 | Total LOC delta single sprint | +5000 |
| D10 | Build WebGL broken (compile fail) | 1 occurrence |
| D11 | Runtime exceptions ≥ 3 nouvelles depuis dernier gate vert | 3+ |
| D12 | Time elapsed sprint > 80% time cap déclaré | warning |

**Confirmed drift = D1-D9 sur 2 checks consécutifs OU D10-D11 sur 1 check.**

## 3. Actions superviseur selon drift

| Drift | Action |
|---|---|
| D1 (1 fois) | append `_clean-log.md` mention warning, no action |
| D1 (2 fois) | write instruction `EXPLAIN: ref Q-N missing on commits X Y Z` + push notif Mike |
| D2 | write instruction `STOP polish drift, revert commits X Y Z if not in R6-06` + push notif Mike |
| D3 | write instruction `PAUSE refacto god class X, dispatch refacto ticket pre-defined` + push notif Mike |
| D4 | write instruction `EXPLAIN unauthorized file X, revert if not in ownership` + push notif Mike |
| D5 | write instruction `STOP phase advancement without Mike validation` + push notif Mike |
| D6 | write instruction `STOP ScheduleWakeup outside authorized loop` + push notif Mike |
| D7 | write instruction `STOP Sub-Opus spawn` + push notif Mike |
| D8 | write instruction `THROTTLE worktrees to max 4` + push notif Mike |
| D9 | write instruction `STOP sprint, creep alert` + push notif Mike |
| D10 | write instruction `STOP, build broken, run bug-fixer` + push notif Mike |
| D11 | write instruction `STOP, runtime exceptions, investigate` + push notif Mike |
| D12 | write instruction `WARN time cap 80%, prioritize finishing critical tickets` + push notif Mike |

## 4. Ce que l'exec doit faire à chaque wakeup loop sprint

```
1. Read .claude/supervisor/instructions-to-exec.md
2. Si non-vide ET timestamp > last ack :
   - Read full instruction
   - Exécute selon directive
   - Write .claude/supervisor/acks/YYYY-MM-DD-HHhMM-from-exec.md
     avec : timestamp lu, action prise, commits affectés si applicable
   - Si STOP : exit loop, push notif Mike, no rescheduleWakeup
3. Sinon : continue loop sprint normal
```

## 5. Mike override

Mike peut écrire directement dans `instructions-to-exec.md` (commit + push)
pour ordre direct, sans passer par superviseur. Format identique aux
instructions superviseur. L'exec doit ack pareil.
