# Agent Watchdog — Cron Single-Shot

## 2026-05-12 03:55

### Snapshot

- Actifs (<8 min) : 9 (dont bbissrkip = Monitor build, bd4n4rpu6 = watchdog scan, a6c12a80d96576a52 = qa-tester V4 diff en flight)
- Stalled (8-30 min) : 10 (b* IDs anciennes sessions — artifacts inertes, pas d'agents Sonnet vivants à killer)
- Worktrees : 16
- Unity batch : **YES** PID 93091 (Unity.app batchmode rebuild WebGL post-shader-fix)
- Lockfile : present (normal, Unity batch tient le lock)
- Build log silence : 1s (compile actif, sain)

### Killed

0. Build Unity PID 93091 actif et productif (log mtime fresh) — laisser tourner.

### Stalled outputs (mtime 8-30 min)

b5v2ds2f9 (17m), bibtu76k4 (15m), bu7e8gd55 (17m), be1gq22ca (23m), bzw06hzvd (27m), buezxrxvi (27m), birqagk98 (27m), byy2poq6d (28m), bkavqvtya (28m), bt2pgnqq3 (29m)

### Notes

10 "b*" outputs sont vieux task-result files (cross-session), pas Sonnet sub-agents — pas d'action destructive. Build Unity en pleine compile post Library/Bee wipe, log silent 1s = healthy. Monitor `bbissrkip` armé pour notification "Build succeeded|failed".

### Next

Re-check next iter (5 min). Surveiller : Unity batch silent > 120s → bash tools/unity-watchdog.sh ; nouveaux Sonnet stalled > 15 min → tail + dispatch bug-fixer.

### Stdout

`Actifs:9 Stalled:10 Killed:0 Worktrees:16 UnityBatch:YES(PID93091,silent1s)`
