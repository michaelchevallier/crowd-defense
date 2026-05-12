# GPU Instancing Audit — MapRenderer + PathTiles + SceneDecor

Date: 2026-05-12

## MapRenderer.cs

| Code path | enableInstancing | Notes |
|---|---|---|
| Water clone (L91) | YES | |
| Lava clone (L100) | YES | |
| Snow/Medieval clone (L115) | YES | |
| Default mat fallback (L144) | YES | |

Verdict: **clean**. All 4 branches set `enableInstancing = true`. Materials are cached in `_matCache` keyed by `(char, LevelTheme)` so identical cell types share one material instance — GPU instancing effective for all ground slabs.

Estimated draw calls: 1 per distinct (char, theme) combination (typically 5-8). Without instancing this would be N×M (hundreds on large grids).

## PathTiles.cs

| Code path | enableInstancing | Notes |
|---|---|---|
| MakePathRevealMat (L128) | YES | |
| MakeFallbackWaterMat (L368) | YES | |
| MakeFallbackLavaMat (L379) | YES | |
| MakeBridgeWaterMat (L389) | YES | |
| MakeBridgeLavaMat (L404) | YES | |
| SpawnStreamBatch (L174) | inherited | `Object.Instantiate(mat)` once per stream type — inherits flag, OK |
| SpawnBridgeQuads (pre-fix) | broken | `Object.Instantiate(mat)` **per cell** = N material instances, instancing impossible |

**Bug fixed**: `SpawnBridgeQuads` was calling `Object.Instantiate(mat)` inside the per-cell loop, creating one separate material object per bridge quad. GPU instancing requires renderers to share the same material instance. Fix: removed per-cell instantiate, pass `mat` directly as `sharedMaterial`. Now all bridge quads of the same type (water or lava) share one instance → batchable.

Commit: see git log.

## SceneDecor.cs

Uses `MaterialPropertyBlock` exclusively for per-prop overrides (alpha, xray tint). No `new Material()` calls at runtime. `sharedMaterial` read only, never written. MPB path is fully instancing-compatible by design.

Verdict: **clean**, no changes needed.

## Summary

| File | Issues found | Fixed |
|---|---|---|
| MapRenderer.cs | 0 | — |
| PathTiles.cs | 1 (per-cell mat instantiate in SpawnBridgeQuads) | YES |
| SceneDecor.cs | 0 | — |

