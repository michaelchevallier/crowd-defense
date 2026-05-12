# Drift report — 2026-05-12 23h54 — D11 regression confirmed

## Severity : HIGH (839 files dirty workspace, asset import pipeline)

## What

Commit `f781e5f` (fix WebGL black-screen) removed `com.unity.ai.assistant 2.7.0-pre.3` from `Packages/manifest.json` to unblock Unity batchmode (Burst/SkillsScanner crash from 23h21). Unintended consequence : `com.unity.ai.assistant` had a transitive dependency on `com.unity.cloud.gltfast 6.14.1`. Removing AI Assistant removed gltfast.

Without gltfast, Unity Editor (running locally) re-imported every `.glb` and `.gltf` asset using the fallback `org.khronos.unitygltf` importer instead :

- ScriptedImporter script GUID change : `715df9372183c47e389bb6e19fbc3b52` (GltfImporter / gltfast) → `804e1ce4c496647cfa3f1a1134187c71` (UnityGLTF).
- Per-file ImportSettings rewritten with different defaults (`_removeEmptyRootObjects`, `_scaleFactor`, `_deduplicateResources`, `_maximumLod`, etc).
- 124 new `.controller` files generated under `Assets/Resources/Animations/Controllers/` (auto-extracted by UnityGLTF importer per character).

## Scope

```
839 files changed, 55612 insertions(+), 22980 deletions(-)
```

Top categories :

- `Assets/Models/Enemies/**.gltf.meta` + `.glb.meta` (28 enemy meshes incl 14 bosses)
- `Assets/Models/Towers/**.glb.meta` + `.gltf.meta` (13 tower types × 3 levels)
- `Assets/Models/{Castle,Heroes,Carnival,Decor}/**.meta`
- `Assets/Resources/AssetRegistry.asset` (244 lines changed — runtime asset refs)
- `Assets/Resources/LevelThemeMaterialConfig.asset` (48 lines)
- `Assets/Resources/UnityGLTFSettings.asset` (gltfast settings reset)
- `Assets/ScriptableObjects/Achievements/AchievementRegistry.asset` (lost 6 hidden_* entries AGAIN)
- `ProjectSettings/GraphicsSettings.asset` (3 lines)
- `ProjectSettings/ProjectSettings.asset` (181 lines)
- 124 untracked `Assets/Resources/Animations/Controllers/*.controller` files

## Risk if shipped as-is

- All 200+ 3D models reimported by different importer → mesh root, anchor, animation curves, scale factor may differ from V4 reference established in sprint R6.
- Material/texture wires on prefabs may break (importer extracts subassets with different fileIDs).
- Sprint R6 95%+ parity claim becomes invalid (visible regressions on every enemy/tower/boss).
- Build pipeline (BatchRebuild.SetupAndBuild) likely fails to find expected assets via AssetDatabase.LoadAssetAtPath.

## Fix applied (commit `11fca57`)

1. **`git restore`** all 839 dirty tracked files (back to `f781e5f` baseline).
2. **`Packages/manifest.json`** : added `"com.unity.cloud.gltfast": "6.14.1"` as direct dependency (no longer tied to AI Assistant).
3. **`git push`** origin/main → auto-build-loop next iteration will resolve packages and pick GltfImporter back as canonical → `.meta` script GUID will match committed state, Unity stops re-importing.

## Open items

- 124 untracked `.controller` files left in working tree. They are orphans from UnityGLTF importer. Two possible resolutions :
  - **A.** Unity Editor next refresh sees gltfast restored → unused controllers stay orphan untracked. `git clean -fd Assets/Resources/Animations/Controllers/*.controller` if Mike wants clean tree.
  - **B.** Some of these might be useful if Mike opens Unity and gltfast extraction skips animation controllers. Verify by checking AssetRegistry references post-refresh.
- The `_clean-log.md` from scrute #38 noted Unity AI Assistant crash. We didn't realise it was load-bearing for gltfast.

## D11 criterion

> Regression : commit introduces breakage on previously-working pipeline / assets.

Confirmed in 1 check (no 2-consecutive needed for D11). Resolved by `11fca57` in the same scrute window.

## Mike notification

T2 info — Mike is in chat live, was diagnosing the black-screen `/v6/` symptom that led to this fix chain. Notif via conversation already given.
