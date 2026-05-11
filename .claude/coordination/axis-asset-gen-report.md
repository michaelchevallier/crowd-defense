# Axis ASSET-GEN — Stage A Report

**Date** : 2026-05-11
**Sub-Opus** : SO-ASSET-GEN (Opus 4.7 orchestrator)
**Branch** : `axis/asset-gen`
**Commits** : 3 (af3566c, 9f7ff5b, d622a49) on top of main `a6540cb`

## Mission status

| Deliverable | Status | Note |
|---|---|---|
| C1 Re-export 11 .gltf → .glb | DONE | 11/11 OK, round-trip verified in Blender |
| C2 Blender MCP diagnostic + setup | DONE (docs) | Cause identified, manual fix steps for Mike documented |
| C3 Mixamo auto-downloader prep | DONE (stub) | README + script stub + enemies list, Mike does Adobe login |
| C4 ComfyUI MCP verify | DONE | Server listening, MCP config present |

## C1 — Re-export 11 .gltf → .glb

### Findings

**Hypothèse du brief Mike** : "refs textures externes manquantes" comme cause des skips.

**Réalité après inspection** (`Assets/Models/Enemies/*.gltf`) :
- Tous les 11 .gltf ont leur buffer **embed en base64** dans la JSON (`"uri": "data:application/octet-stream;base64,..."`)
- **0 images externes** declarées
- **0 extensions requises**

Donc la vraie cause des skips est côté **UnityGLTF parser** (peut-être animations structure spécifique ou autre quirk Khronos Blender I/O v1.6.16). Re-export via Blender 5.1.1 régénère un .glb propre que tout parser glTF 2.0 conformant accepte.

### Round-trip verification

Test Blender re-import sur les 11 .glb output : tous parseables avec leurs meshes + armatures + animations préservées :

| File | meshes | armatures | anim clips |
|---|---|---|---|
| goblin.glb | 2 | 1 | **17** |
| knight.glb | 2 | 1 | **17** |
| knightgolden.glb | 2 | 1 | **17** |
| mob_cyberpunk_2legs.glb | 2 | 1 | 7 |
| mob_cyberpunk_character.glb | 5 | 1 | **22** |
| mob_cyberpunk_flying.glb | 3 | 1 | 6 |
| mob_cyberpunk_large.glb | 3 | 1 | 8 |
| pirate.glb | 2 | 1 | **17** |
| soldier.glb | 2 | 1 | **17** |
| wizard.glb | 2 | 1 | **17** |
| zombie.glb | 2 | 1 | **17** |

**Implication majeure pour Phase 4** : les enemies humanoid ont déjà **7-22 anim clips embedded** dans le .glb. Mixamo retargeting devient OPTIONNEL pour ces 11 mobs — Phase 4 peut se rabattre sur les anims Quaternius natives via le `BuildAnimatorControllers` Editor menu existant (cf commit `59785a6`).

### Build size impact

- **Removed** 11 .gltf : 13.9 MB total (base64 inflate JSON ~2-3× vs binary)
- **Added** 11 .glb : 6.4 MB total
- **Net change : -7.5 MB** ← below brief target of "< 5 MB added", we actually reduced size

## C2 — Blender MCP diagnostic

### Root cause (Opus diagnostic)

```bash
$ lsof -i :9876
COMMAND   PID USER   FD   TYPE   DEVICE       SIZE/OFF NODE NAME
Python  61776 mike    3u  IPv4   0xd3678b70...  0t0    TCP localhost:9876 (LISTEN)

$ ps -p 61776 -o command=
/Library/Frameworks/Python.framework/.../Python /tmp/capture_server.py
```

Port `9876` (default Blender MCP addon listener) is **occupied by `/tmp/capture_server.py`** — a screenshot capture HTTP server unrelated to Blender, probably spawned by another Claude Code session. Blender MCP addon cannot bind → `/mcp` reconnect fails.

### Manual fix steps (Mike action)

Documented in `tools/blender/SETUP_NOTES.md`. Summary:
1. `kill 61776` (or `kill $(lsof -ti :9876)`) to free the port
2. In Blender: verify BlenderMCP addon is enabled in `Edit > Preferences > Add-ons`
3. In Blender 3D viewport: press N → BlenderMCP tab → click "Connect to Claude"
4. From Claude Code: `/mcp` should show `blender ✓ Connected`

### Workaround used during this axis

Re-export 11 .gltf done **without MCP** via `blender --background --python tools/blender/reexport_gltf_to_glb.py -- <path>` direct CLI. No MCP dependency.

## C3 — Mixamo prep

Created (stub):
- `tools/mixamo/README.md` : Mike-facing setup instructions (Adobe SSO login, cookie session capture)
- `tools/mixamo/download_anims.py` : CLI stub with argparse, `--dry-run`, `--check-auth` modes; real network fetch deferred (TODO comments)
- `tools/mixamo/enemies_humanoid.txt` : 18-line list of humanoid enemy base names for batch fetch

**Blocker for full implementation** : Adobe SSO auth flow (potentially MFA + captcha). Mike must do manual login once, then provide cookie session via `~/.mixamo-session.json` for the script to function.

**Reduced urgency** : C1 round-trip verification showed humanoid .glb files already include 17 anim clips (Idle, Walk, Run, Attack, Die likely covered by Quaternius pack). Mixamo retargeting can be a Phase 4 polish item rather than a Phase 3 blocker.

## C4 — ComfyUI MCP

Verification:
- ComfyUI server **listening** on `http://127.0.0.1:8188` (PID 35226)
- MCP config **present** in `~/.claude.json` (entry `comfy` via `uvx --with "mcp<1.6" comfy-mcp-server`)
- 2 tools exposed by MCP: `mcp__comfy__generate_image(prompt)` + `mcp__comfy__generate_prompt(topic)`

Setup notes in `tools/comfy/SETUP_NOTES.md`. End-to-end test (`mcp__comfy__generate_image` invoke) **not run by Opus** because the tool requires `ToolSearch` load in the calling session AND its config relies on `/Users/mike/flux-local/workflows/flux_schnell_basic.json` whose existence I haven't verified. If the workflow file is missing → blocker, but that's a Mike-only check (the file is outside the repo).

## Files modified / added

### Added (tracked)
- `tools/blender/reexport_gltf_to_glb.py`
- `tools/blender/SETUP_NOTES.md`
- `tools/comfy/SETUP_NOTES.md`
- `tools/mixamo/README.md`
- `tools/mixamo/download_anims.py`
- `tools/mixamo/enemies_humanoid.txt`
- `Assets/Models/Enemies/goblin.glb`
- `Assets/Models/Enemies/knight.glb`
- `Assets/Models/Enemies/knightgolden.glb`
- `Assets/Models/Enemies/mob_cyberpunk_2legs.glb`
- `Assets/Models/Enemies/mob_cyberpunk_character.glb`
- `Assets/Models/Enemies/mob_cyberpunk_flying.glb`
- `Assets/Models/Enemies/mob_cyberpunk_large.glb`
- `Assets/Models/Enemies/pirate.glb`
- `Assets/Models/Enemies/soldier.glb`
- `Assets/Models/Enemies/wizard.glb`
- `Assets/Models/Enemies/zombie.glb`
- `.claude/plans/axis-asset-gen.md`
- `.claude/coordination/axis-asset-gen-report.md` (this file)

### Removed (tracked)
- 11 × `Assets/Models/Enemies/{name}.gltf`
- 11 × `Assets/Models/Enemies/{name}.gltf.meta`

### NOT yet generated (Unity Editor will create on first import post-merge)
- 11 × `Assets/Models/Enemies/{name}.glb.meta`

## Validation status

### QA-1 Pre-Spawn (self-check Sub-Opus)
- [x] Lu `.claude/coordination/file-ownership.md` — zone : `tools/blender/`, `tools/comfy/`, `tools/mixamo/`, `Assets/Models/Enemies/*.glb`
- [x] Lu `.claude/coordination/api-contracts.md` — pas d'API publique côté ASSET-GEN
- [x] Plan évolutif écrit
- [x] Branche `axis/asset-gen` créée depuis main HEAD

### QA-2 Per-Commit
- [x] Aucun file dans HOT ZONES (Tower.cs, Enemy.cs, etc.) — vérifié 3 commits clean
- [x] Tous les files touched dans la zone ownership ASSET-GEN
- [ ] Compile Unity : **NOT RUN** (Unity Editor pointe sur main, pas sur worktree) — MO doit run après merge
- [ ] API contracts : N/A (pas de nouveau public method exposé)

### QA-3 Pre-Merge ship-gate
- [ ] **MO action requise** : after merge axis/asset-gen → integration/phase3-4-5, run `mcp__UnityMCP__execute_menu_item` `Tools/CrowdDefense/Build AssetRegistry` → check console shows 60 entries (49 existing + 11 new)

## Risks & caveats

### R1 — Unity .meta files not committed
Unity standard practice is to commit .meta with assets. We did NOT commit `*.glb.meta` because Unity Editor was pointed at the main repo (not this worktree) and can't generate them here. Post-merge, **first thing MO must do** is open Unity (pointing at integration branch worktree), wait for asset import, then commit the generated `*.glb.meta` separately. Without .meta files, GUIDs are unstable and scene refs to these prefabs may break.

### R2 — Re-export sanity in Unity not verified
While Blender round-trip verifies the .glb files are glTF 2.0 spec-compliant, **UnityGLTF specifically** has not yet imported them in this session. There's a small risk UnityGLTF rejects them too (e.g., some quirk specific to that parser). If so, fallback options:
1. Try com.unity.cloud.gltfast (alternative parser, but failed our due diligence earlier — cf memory `feedback_dependency_due_diligence`)
2. Switch to FBX export from Blender (more conservative format)
3. Manual import via Unity Editor (would generate .meta)

### R3 — capture_server.py is not killed by this axis
For safety, we did NOT kill PID 61776 ourselves (the rogue `/tmp/capture_server.py`). Mike should kill it before reconnecting Blender MCP. Documented in `tools/blender/SETUP_NOTES.md` step 1.

### R4 — Mixamo not actually downloading anims
Only a stub. Real network logic needs:
- Adobe auth (SSO complexity, possibly MFA/captcha)
- Mixamo API reverse-engineering or use of `loveletter/mixamo-auto-downloader` fork (not yet vetted)

If priority remains "T3 stretch" per STATUS.md, defer to Phase 4. Otherwise spawn dedicated ticket for real implementation post-axis merge.

### R5 — ComfyUI workflow file existence not verified
`COMFY_WORKFLOW_JSON_FILE=/Users/mike/flux-local/workflows/flux_schnell_basic.json` is referenced in MCP env but I didn't read the file (outside repo scope). If missing → `mcp__comfy__generate_image` fails at runtime. Mike to verify.

## Recommended MO next steps

1. Switch Unity Editor project to `integration/phase3-4-5` worktree (or stage merge to main and reload).
2. Wait for Unity import of 11 new .glb files (~30s).
3. Commit generated `.meta` files separately: `chore(meta): Unity auto-meta for 11 Quaternius re-exports`.
4. Run `Tools/CrowdDefense/Build AssetRegistry` Editor menu.
5. Verify console: `Built registry with 60 entries.` (was 49).
6. If any of the 11 new .glb failed to load, AssetRegistry will log `Skipped (load failed) [Enemies] ...` — diagnose UnityGLTF errors via `read_console`.
7. (Optional) Re-deploy /v6/ if visual smoke test of re-exported mobs is desired Phase 3.

## Push status

Commits ready locally on `axis/asset-gen`. Push command for MO:

```bash
cd /Users/mike/Work/crowd-defense/.claude/worktrees/axis-asset-gen
git push -u origin axis/asset-gen
```

Or MO can fetch the worktree branch directly: `git fetch . axis/asset-gen:axis/asset-gen` from main worktree.

## Commits delivered

```
d622a49 chore(assets): re-export 11 Quaternius .gltf -> .glb self-contained
9f7ff5b feat(tools): mixamo downloader stub + comfy MCP SETUP_NOTES
af3566c feat(tools): blender reexport_gltf_to_glb.py + plan ASSET-GEN axis
```
