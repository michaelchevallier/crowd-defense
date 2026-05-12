# Pathfinder Perf Audit — 2026-05-12

## Verdict

PathManager/GridData architecture is sound. BFS runs **once at startup** (portal × castle cross-product), results stored as pre-computed `IReadOnlyList<Vector3>` paths. No per-frame BFS. No per-enemy path query alloc. `EnemyPathingSystem.Tick()` is O(count) list-index lookups — correct.

## Issues found

| # | Location | Issue | Severity |
|---|----------|-------|----------|
| 1 | `PathManager.PathsForCastle()` | `new List<int>()` allocated every call — cache miss pattern when called from castle HP or wave logic | Medium (GC pressure, currently unused at runtime but API trap) |
| 2 | `GridData.BfsShortestPath()` | `new Dictionary` + `new Queue` without capacity hint → bucket rehashing during startup BFS | Low (startup only, not 60fps) |

## No issues found

- Per-enemy path query: `GetWaypointOnPath(piIdx, wpIdx)` = O(1) list index, 0 alloc
- BFS at runtime: never called post-startup (no tower placement re-pathfinding yet)
- EnemyPathingSystem arrays: grown on demand, never shrunk — correct amortised alloc

## Patches applied

**Patch 1** — `PathManager.cs`: `RebuildCastlePathCache()` builds `Dictionary<int, IReadOnlyList<int>>` once after `Build()` and `InjectFallbackPath()`. `PathsForCastle()` now returns cached result, signature changed to `IReadOnlyList<int>` (no breaking callers found). Static `_emptyIntList` for missing keys.

**Patch 2** — `GridData.cs`: `BfsShortestPath()` pre-sizes `Dictionary` and `Queue` to `Width * Height` to avoid rehash during startup traversal.

## Commit

`f3fed19` — perf(pathfinder): cache PathsForCastle lookup + pre-size BFS collections

## Not patched (out of scope)

- `PlacementController` will eventually need BFS re-validation on tower placement (path blocking check). When added, `PathManager.Build()` should expose an `InvalidateCache()` / re-`Build()` hook. Already clean to add.
