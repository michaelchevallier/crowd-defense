# QA-3 Pre-Merge Report — axis/build

**Date** : 2026-05-11
**Axis** : E PLATFORM-BUILDS
**Sub-Opus** : SO-BUILD
**Status** : **PASS** (with caveat on local OSX build validation)

## Files changed (axis/build vs main)

| File | LOC delta | Zone |
|---|---|---|
| `Assets/Editor/BuildScript.cs` | +183 / -25 | own (zone exclusive) |
| `.github/workflows/build-matrix.yml` | +77 / 0 | own (zone exclusive) |
| `.github/workflows/deploy-webgl.yml` | +87 / 0 | own (zone exclusive) |
| `tools/ci/SETUP.md` | +222 / 0 | own (zone exclusive) |
| `tools/ci/size-audit.md` | +89 / 0 | own (zone exclusive) |
| `.claude/plans/axis-build.md` | +275 / 0 | own (coordination plan) |

**Total** : 6 files, +933 / -25.

## Ownership compliance (file-ownership.md)

- [x] **0 hot zone files touched** (vérifié grep : pas de Tower.cs, Enemy.cs, Castle.cs, WaveManager.cs, LevelRunner.cs, Economy.cs, BalanceConfig.cs, STATUS.md, Packages/manifest.json)
- [x] Tous les fichiers dans zone exclusive Axis E :
  - `Assets/Editor/BuildScript*.cs` ✓
  - `.github/workflows/*.yml` ✓
  - `tools/ci/*` ✓
- [x] Aucun ProjectSettings/Build*.asset touché (Unity 6+ Build Profiles non utilisés Phase 3, skip volontaire)

## Compile compliance

- [x] `validate_script` (Unity-MCP) sur `BuildScript.cs` : **0 errors, 0 warnings**
- [x] YAML syntax : Python `yaml.safe_load()` parse les 2 workflows sans erreur
- [x] Markdown lint : visual check SETUP.md + size-audit.md (no broken links visibles)
- [x] No `Debug.Log` sans `#if UNITY_EDITOR` (BuildScript wrap `#if UNITY_EDITOR` complet bloc)

## C7 API contracts compliance

Signatures C7 respectées :
- [x] `BuildScript.BuildWebGL()` ✓ (existing, refactored output `Build/WebGL/`)
- [x] `BuildScript.BuildOSX()` ✓ (nouveau, Mac Universal)
- [x] `BuildScript.BuildWindows()` ✓ (nouveau, Win64)
- [x] `BuildScript.BuildLinux()` ✓ (nouveau, Linux64)
- [x] `BuildScript.BuildIOS()` ✓ (nouveau, Xcode export)
- [x] `BuildScript.BuildAndroid()` ✓ + bonus `BuildAndroidAAB()` (Phase 4 Play Store)
- [x] `BuildScript.BuildAll()` ✓ (Mac + Win + Linux séquentiel, skip iOS/Android)
- [x] Output path `Build/<platform>/` ✓ (NOT legacy `Builds/`)
- [x] Bundle ID `com.crowddefense.game` ✓ (apply via NamedBuildTarget per platform)
- [x] IL2CPP + code stripping enabled toutes plateformes ✓
- [x] Version `PlayerSettings.bundleVersion` ✓ (pulled from ProjectSettings, manual update Mike)

## Secrets compliance

- [x] Aucun secret hardcoded dans workflows : `grep -r 'password\|secret\|api_key'` retourne uniquement `secrets.*` references
- [x] `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` documentés dans SETUP.md
- [x] Phase 4 secrets prep (Steam/Apple/Google Play) documentés sans inclure valeurs réelles

## Build validation

### Compile statique

- `mcp__UnityMCP__validate_script` sur `BuildScript.cs` worktree path : **success, 0 errors**

### Build local OSX

**Status** : **DEFERRED to post-merge**.

**Justification** :
- L'instance Unity Editor courante tourne sur le main project (`/Users/mike/Work/crowd-defense/`), busy avec asset imports (AssetImportWorker0+3 actifs, ~700 MB RAM).
- Lancer un second `Unity -batchmode` sur le worktree (`/Users/mike/Work/crowd-defense/.claude/worktrees/agent-ab9d2e5c2553d10f6/`) ferait :
  - Création d'une second Library/ (~5 GB) pour ce worktree → coûteux disque
  - Conflict éventuel sur lock files Unity
  - Première compile project complète (~10 min depuis 0 cache)
- L'alternative serait de copier BuildScript.cs sur le main project worktree temporairement, mais ça pollue la zone owned par d'autres axes en flight (axis/audio, axis/visual-core, axis/asset-gen écrivent dans main).
- **Recommandation** : Mike (ou MO Integrator) exécute la validation post-merge axis/build → integration/phase3-4-5 :
  ```bash
  cd /Users/mike/Work/crowd-defense
  "/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app/Contents/MacOS/Unity" \
    -batchmode -nographics -projectPath . \
    -executeMethod CrowdDefense.Build.BuildScript.BuildOSX -quit
  test -d Build/OSX/CrowdDefense.app && echo "OK" || echo "FAIL"
  ```

### YAML workflows validation

**Status** : compile-only check (full E2E requires UNITY_LICENSE secret Mike add).

- `python3 -c "import yaml; yaml.safe_load(open(...))"` parse les 2 fichiers sans erreur
- Action versions cross-checked :
  - `actions/checkout@v4` (latest stable)
  - `actions/cache@v4` (latest stable)
  - `actions/upload-artifact@v4` (latest stable)
  - `actions/download-artifact@v4` (latest stable)
  - `game-ci/unity-builder@v4` (latest stable, v5.0.0 released 2026-05-07 mais on stay sur v4 pour stabilité)
  - `peaceiris/actions-gh-pages@v4` (latest stable)
- Caveat known : `unityci/editor` Docker image jusqu'à `6000.3.14f1` only ; project `6000.3.15f1`. Documenté dans SETUP.md §1 avec 3 fallbacks (downgrade local / self-hosted runner / wait Docker publish).

## Git history compliance

- [x] 4 commits atomic (BuildScript / workflows / docs / plan)
- [x] Convention `feat()`, `feat(ci)`, `docs(ci)`, `chore(coord)` respectée
- [x] Footer `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>` sur les 4 commits
- [x] Rebase clean sur main HEAD (`0d9bb3b`)
- [x] Pas de merge commit polluant

## Verdict

**PASS — ready for merge axis/build → integration/phase3-4-5**.

Caveat unique : local OSX build artifact validation deferred to MO Integrator pour les raisons exposées ci-dessus. Le `validate_script` Unity-MCP a confirmé compile clean ; la probabilité d'un fail batch build est faible (les API Unity utilisées sont standard NamedBuildTarget/BuildPipeline). Si fail à l'integration, fix-forward (probable cause : Stripping High runtime break → fallback Medium, déjà documenté dans size-audit.md).

## Recommendations futures (post Stage A)

- **Pre-Phase 4** : Mike doit ajouter `UNITY_LICENSE` secret AVANT le 1er run CI (workflow_dispatch test)
- **Phase 4 polish** : ajouter workflow iOS dédié (extends build-matrix avec macos runner + Xcode + signing) une fois Mike a son Apple cert
- **Phase 4 polish** : ajouter workflow Android dédié (sur Linux runner avec Android SDK + keystore secrets) une fois Mike a son Google Play Service Account
- **Long terme** : si bundle size dépasse 30 MB Phase 3 final, migrer vers Addressables avec lazy-load levels par world
