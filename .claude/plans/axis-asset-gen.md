# Axis ASSET-GEN — Stage A Plan

**Date** : 2026-05-11
**Sub-Opus** : SO-ASSET-GEN (Opus 4.7)
**Branch** : `axis/asset-gen`
**Worktree** : `/Users/mike/Work/crowd-defense/.claude/worktrees/axis-asset-gen`

---

## Mission

Combler trous d'assets Phase 3 :
1. **C1** Re-export 11 .gltf Quaternius skipped par UnityGLTF en .glb self-contained Blender
2. **C2** Fix Blender MCP server cassé
3. **C3** Setup Mixamo auto-downloader (prep, pas de login Adobe)
4. **C4** Vérifier ComfyUI MCP fonctionnel

## QA-1 Pre-Spawn Self-Check

- [x] Lu `.claude/coordination/file-ownership.md` — zone : `tools/blender/`, `tools/comfy/`, `tools/mixamo/`, `Assets/Models/Enemies/*.glb`, `Assets/Animations/*`
- [x] Lu `.claude/coordination/api-contracts.md` — aucune API publique exposée par ASSET-GEN
- [x] Plan évolutif écrit (ce fichier)
- [x] Branche `axis/asset-gen` créée depuis main `a6540cb`

## Diagnostic préliminaire C1/C2 (Opus, avant spawn)

### C1 — Cause des 11 skips dans AssetRegistry

**Hypothèse Mike brief** : "refs textures externes manquantes" (Quaternius packs splittés).

**Réalité après inspection** (`Assets/Models/Enemies/goblin.gltf` etc.) :
- Tous les .gltf ont **bufferView embed en base64** dans la JSON (`"uri": "data:application/octet-stream;base64,..."`)
- **0 images externes** (`"images": []`)
- **0 extensions requises**
- File sizes : 0.31 MB (cyberpunk_flying) → 1.98 MB (wizard), total **15.5 MB pour 11 fichiers**

Donc cause réelle des skips ≠ refs textures. Probablement UnityGLTF échoue parser ces .gltf pour raison interne (animations, accessors, KHR extensions Blender-generated). Re-export Blender→.glb devrait quand même résoudre (Blender re-encode propre).

**Impact build size** : 15.5 MB ajouté → au-dessus du target 5 MB du brief (cf R5 risk Phase 3 plan). À noter dans rapport final.

### C2 — Cause Blender MCP fail

**Diagnostic ports** :
- Blender process running (PID 239)
- Port 9876 (default Blender MCP addon) **occupé par `/tmp/capture_server.py` (PID 61776)** — un service indépendant tournant en background, pas le Blender MCP addon
- Blender MCP addon ne peut donc PAS bind sur 9876 → MCP fail
- Probable cause secondaire : addon Blender MCP désactivé dans Preferences (à vérifier manuellement par Mike)

**Action** :
1. Tuer `/tmp/capture_server.py` (PID 61776) pour libérer 9876
2. Activer addon Blender MCP dans Blender Preferences (manuel Mike)
3. Connect "to Claude" via panel sidebar (manuel Mike)

Mais pour la mission de re-export, **on n'a PAS besoin du MCP** : on peut invoquer `blender --background --python script.py` directement via Bash. Donc C1 (re-export) peut tourner **sans attendre C2 (fix MCP)**.

## Sonnets à spawner

### Sonnet C1+C2 (combiné) — feature-dev

**Type** : feature-dev
**Estimé** : 1-2 commits, ~30 min
**Brief** :
- Créer `tools/blender/reexport_gltf_to_glb.py` : script Blender headless qui parse argv[-1] = path .gltf, import, export → .glb même path.
- Créer `tools/blender/SETUP_NOTES.md` : findings diagnostic Blender MCP + steps manuels Mike (tuer capture_server, activer addon, etc.)
- Lancer le script pour les 11 .gltf via `blender --background --python script.py -- <path>`
- Verify output : 11 fichiers .glb dans `Assets/Models/Enemies/`
- Suppr les 11 .gltf originaux + .meta correspondants (Unity régénère le .meta du .glb au prochain refresh)
- **NE PAS** committer les .meta files manuellement — laisser Unity générer
- Commit 1 : `feat(tools): blender script reexport_gltf_to_glb.py + SETUP_NOTES.md`
- Commit 2 : `chore(assets): re-export 11 Quaternius .gltf → .glb self-contained`

### Sonnet C3 — feature-dev

**Type** : feature-dev
**Estimé** : 1 commit, ~15 min
**Brief** :
- Créer `tools/mixamo/README.md` : instructions Mike pour login Adobe SSO + setup
- Créer `tools/mixamo/download_anims.py` script stub : prend `--anims walk run attack die --target Assets/Animations/Mixamo/`, sketch logique (auth via cookie session, GET char list, POST download)
- **NE PAS** tenter login Adobe (Mike fera manuellement)
- Document limitations : Adobe SSO probable, fetch via Selenium/Playwright si pas d'API officielle
- Commit : `feat(tools): mixamo auto-downloader prep — README + stub script`

### Sonnet C4 — quality-maintainer (test invoke)

**Type** : quality-maintainer
**Estimé** : 1 commit, ~10 min
**Brief** :
- Verify ComfyUI is listening (port 8188) → already confirmed by Opus
- Inspecter workflow `/Users/mike/flux-local/workflows/flux_schnell_basic.json` si existe
- Document state dans `tools/comfy/SETUP_NOTES.md` : config MCP présente, ComfyUI listening, comment invoquer (`mcp__comfy__generate_image` tool)
- Si workflow file manquant → noter dans SETUP_NOTES comme blocker
- Commit : `docs(tools): comfy SETUP_NOTES — verify ComfyUI listening + MCP config`

## Verification finale (Opus avant push)

1. Rebuild AssetRegistry via `mcp__UnityMCP__execute_menu_item Tools/CrowdDefense/Build AssetRegistry`
2. `mcp__UnityMCP__read_console` → check 60 entries (49 + 11 nouveaux .glb)
3. `git log axis/asset-gen --oneline` → 3-4 commits atomiques
4. QA-3 ship-gate : Sonnet QA validate
5. Push `axis/asset-gen` upstream
6. Rapport `.claude/coordination/axis-asset-gen-report.md`

## Constraints respectées

- **Pas d'ajout package Unity** (Blender script externe, pas de UPM dep)
- **Blender --background headless** (pas d'interaction)
- **Build size +15.5 MB** : au-dessus du target 5 MB du brief — documenté comme caveat dans rapport
- **Max 3 Sonnets parallèles** : 3 spawnés (C1+C2 combiné, C3, C4)
- **Budget 4-6h** : on devrait finir en 1h grand max (rebuild AssetRegistry inclus)

## Status

- [x] QA-1 pre-spawn done
- [x] Plan écrit
- [ ] Sonnet C1+C2 spawned
- [ ] Sonnet C3 spawned
- [ ] Sonnet C4 spawned
- [ ] Rebuild AssetRegistry verified 60 entries
- [ ] QA-3 ship-gate pass
- [ ] Push axis/asset-gen
- [ ] Rapport final
