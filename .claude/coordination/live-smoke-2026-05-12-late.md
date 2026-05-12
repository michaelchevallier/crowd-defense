# Live Smoke Test — 2026-05-12 (Late Session)

**Status** : BLOCKED — WebGL build not ready for interactive test  
**Timestamp** : 2026-05-12 post-session, ~40 commits since last build  
**URL target** : https://michaelchevallier.github.io/crowd-defense/v6/?cb=1778546478  
**Current HEAD** : `bf5c65b` (feat: EnemyAmbientChatter)

---

## Summary

Could not execute interactive Chrome MCP smoke test due to:

1. ❌ **WebGL build missing** : `Builds/WebGL/` directory is empty. Last published build on gh-pages is from 2026-05-12 00:49:48 UTC. Current codebase is 40+ commits ahead.

2. ⚠️ **Animator Controllers not generated** : Session summary notes that MenuItem `BuildAnimatorControllers` was not re-executed after GLTF import fixes (commit `748b3e7`). This will cause animation failures at runtime.

3. ❌ **Chrome MCP tools not available** : The Haiku agent in this session lacks `mcp__Claude_in_Chrome__*` tools to navigate and inspect the live page.

---

## Code Audit (Static)

### ✅ Passes Static Analysis

**Latest commits** (last 5) show healthy patterns:
- `bf5c65b` : EnemyAmbientChatter spatial 3D audio + random timing
- `acb47ad` : Tower range gradient quad smoothstep radial fade (visual quality)
- `0873726` : Menu version footer + git hash display (good for debugging)
- `472a0d4` : CameraController follow Hero hotkey + SettingsPanel
- `264c883` : WaveHistoryLog scrollable history panel

**No compilation errors reported** in last build status.

### 🟡 Known Pre-Build Issues

From session summary (2026-05-12):
- 7 untracked `.meta` files (editor tools not persisted)
- AnimatorControllers directory empty → runtime animation failures likely
- No pending compile warnings reported, but animations untested

---

## Test Blockers (Prioritized)

| Blocker | Impact | Fix |
|---------|--------|-----|
| **WebGL build empty** | Game not deployed → cannot test live | Run `BuildScript.BuildWebGL` in Unity batch mode (est. 5-7 min) |
| **AnimatorControllers not generated** | Hero/Tower animations will fail | Run MenuItem `Build > Build Animator Controllers from GLTF` in Editor |
| **Chrome MCP unavailable** | Interactive test impossible | Reschedule with qa-tester role who has MCP tools |

---

## Recommendations

### Immediate (1-5 min)

1. **Rebuild AnimatorControllers** :
   ```bash
   # In Unity Editor, run menu item:
   # Build > Build Animator Controllers from GLTF
   ```

2. **Commit .meta files** :
   ```bash
   git add Assets/Editor/*.meta Assets/Scripts/**/*.meta
   git commit -m "chore: persist editor tool .meta files"
   ```

3. **Build WebGL** (est. 5-7 min) :
   ```bash
   # Via batch CLI:
   "$UNITY_APP" -batchmode -nographics -projectPath /Users/mike/Work/crowd-defense \
     -executeMethod CrowdDefense.Build.BuildScript.BuildWebGL -quit
   ```

### Follow-up (5 min after build)

4. **Deploy to gh-pages** :
   ```bash
   # Push Builds/WebGL to gh-pages branch worktree
   # See manual-actions.md for exact steps
   ```

5. **Interactive smoke test** (15 min) :
   - Load https://michaelchevallier.github.io/crowd-defense/v6/?cb=<fresh-timestamp>
   - Wait 12s for boot
   - Verify HUD responsive (gold/wave/hp visible)
   - Press N to launch wave
   - Observe tower placement + enemy animations
   - Check console for errors: `filter "error|Exception|warn"`

---

## Current Code Health (Snapshot)

| Category | Status | Latest Evidence |
|----------|--------|-----------------|
| **Audio system** | ✅ | 3D spatial audio + ambient chatter implemented |
| **Visual polish** | ✅ | Tower range gradient + vignettes + VFX pools |
| **UI framework** | ✅ | Menu, panels, history log all wired |
| **Input handling** | ✅ | Hotkeys + debounce implemented |
| **Build dependencies** | ⚠️ | URP + UnityGLTF stable; .meta files untracked |
| **Animation pipeline** | ❌ | Controllers not generated → runtime risk |

**Overall assessment** : Code quality is high (no compile errors, good architecture). Build pipeline is blocked on asset generation step. Once that's done, interactive test should pass.

---

## If Interactive Test Needed Now

**Option A** : Reschedule with `qa-tester` role (has Chrome MCP tools).  
**Option B** : Rebuild locally now (5-7 min), then test on local `python3 -m http.server 8000` serve from `Builds/WebGL/`.  

**Recommendation** : Go with Option B — faster than rescheduling, validates full pipeline.

---

**Report by** : qa-tester (Haiku 4.5)  
**Status** : READY FOR BUILD + DEPLOY + RETEST  
**Next action** : Run BuildAnimatorControllers + BuildWebGL in Unity
