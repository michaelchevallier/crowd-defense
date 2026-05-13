# Drift report — D8 worktrees > cap 4 CONFIRMED

**Date** : 2026-05-13 03h17
**Drift** : D8 (worktrees actifs > 4)
**Status** : CONFIRMED (charter §2 "D1-D9 sur 2 checks consécutifs")

## Détection

- **Scrute #53 @ 03h04** : 6 worktrees (1ère occurrence — warning)
- **Scrute #55 @ 03h17** : 6 worktrees (2e occurrence — CONFIRMED)

## Détail worktrees

| # | Path | Branch | HEAD | Lock | Notes |
|---|---|---|---|---|---|
| 1 | `/Users/mike/Work/crowd-defense` | `main` | `b47689e` | no | working tree principal |
| 2 | `/private/tmp/crowd-defense-v6` | `gh-pages` | `759715a` | no | déploiement site web `/v6/` |
| 3 | `.claude/worktrees/agent-a203f9d46f1056019` | `worktree-agent-...` | `02d44da` | locked (pid 84576) | **T-VISUAL-002 done c32ac3c merged main** → prunable |
| 4 | `.claude/worktrees/agent-a333d4adea155a1bb` | `worktree-agent-...` | `9750fca` | locked (pid 84576) | HEAD = R7-025 STATUS update already merged |
| 5 | `.claude/worktrees/agent-a501e05382d61a77f` | `worktree-agent-...` | `a48ccea` | locked (pid 84576) | actif ? Check pid |
| 6 | `.claude/worktrees/agent-aa3effd9e17794dcb` | `worktree-agent-...` | `d42b014` | locked (pid 84576) | actif ? Check pid |

## Cause probable

Agents feature-dev/bug-fixer finished work + commits merged main, mais worktrees pas pruned (charter §1 règle #9 cap atteint).

Charter §1 règle #9 ambigu sur "Max 4 worktrees simultanés" — inclut main + gh-pages ou seulement agent worktrees ? Audit `R7-BATCH-12-AUDIT` interprète strict `> 4` total → CONFIRMED selon cette lecture. Si interprétation = "4 agent worktrees only", currently 4 = at cap exact (not over). Either way, **cleanup post-merge est due-diligence**.

## Action charter §3 D8

> "write instruction THROTTLE worktrees to max 4 + push notif Mike"

## Instruction publiée

`.claude/supervisor/instructions-to-exec.md` § 2026-05-13 03h17 THROTTLE-WORKTREES (cf bloc dédié).

## Notif Mike

- T2 batched via `.claude/supervisor/tools/notify.sh T2` (Mike chat live, T1 panic pas requis)
- PushNotification tool : skipped (suppressed terminal focus + Mike chat live)

## Recommandation pour exec

```bash
cd /Users/mike/Work/crowd-defense
# Verify which worktree pids still alive
for w in $(git worktree list --porcelain | grep -A2 "agent-" | grep "worktree" | awk '{print $2}'); do
  pid=$(grep "pid" $w/.git/locked.lock 2>/dev/null | grep -oE '[0-9]+' || echo "none")
  if [[ "$pid" != "none" ]] && ! ps -p $pid > /dev/null 2>&1; then
    echo "STALE: $w (pid $pid dead)"
  fi
done
# Then for each stale :
git worktree remove --force <path>
# Or if locked alive but commits all merged main:
git worktree unlock <path> && git worktree remove <path>
# Finally :
git worktree prune
```

Cible : ≤ 4 total worktrees (main + gh-pages + ≤2 agent worktrees actifs).
