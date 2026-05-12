# Agent Watchdog — Cron Single-Shot

## 2026-05-12 latest

### Snapshot

- Actifs (<30 min) : 18
- Stalled (8-30 min) : 14
- Worktrees : 16
- Unity batch : NO (no process)
- Lockfile : absent (clean)

### Killed

0. Pas de batch Unity actif, pas de lockfile orphelin. Rien à killer.

### Stalled outputs (mtime 8-30 min)

bibtu76k4 (9m), bu7e8gd55 (11m), b5v2ds2f9 (12m), be1gq22ca (18m), bzw06hzvd (22m), buezxrxvi (22m), birqagk98 (22m), byy2poq6d (23m), bkavqvtya (23m), bt2pgnqq3 (24m), b5zb6d9tj (26m), blt0ic8re (26m), bqbjgk5ks (27m), b80la2eto (29m)

### Notes

14 agents Sonnet en background silencieux 8-30 min — pas d'action destructive (peuvent encore livrer). 16 worktrees ouverts. Pas de batch Unity ni lockfile = pas de watchdog Unity nécessaire.

### Next

Re-check next iter. Si nouveau batch démarre et silent > 15 min → `bash /Users/mike/Work/crowd-defense/tools/unity-watchdog.sh`.

### Stdout

`Actifs:18 Stalled:14 Killed:0 Worktrees:16 UnityBatch:N`
