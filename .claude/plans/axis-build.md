# Axis BUILD — Plan évolutif (Sub-Opus PLATFORM-BUILDS)

**Branche** : `axis/build` (depuis main @ `a6540cb`)
**Worktree** : `/Users/mike/Work/crowd-defense/.claude/worktrees/agent-ab9d2e5c2553d10f6`
**Sub-Opus** : SO-BUILD
**Démarré** : 2026-05-11
**Goal Stage A** : prep multi-platform builds infra (Mac/Win/Linux/WebGL + CI matrix + size optim + docs Phase 4 credentials)

---

## QA-1 Pre-Spawn checklist (auto-vérif Sub-Opus)

- [x] Lu `file-ownership.md` → zone exclusive : `Assets/Editor/BuildScript*.cs`, `Assets/Editor/CIBuilder.cs`, `tools/ci/`, `.github/workflows/`, `ProjectSettings/Build*.asset`, `Build/` output
- [x] Lu `api-contracts.md` C7 → BuildScript signatures fixées
- [x] Lu `qa-gates.md` → QA-2 per-commit + QA-3 pre-merge requis
- [x] Plan évolutif écrit (ce fichier)
- [x] Branche `axis/build` créée (renaming depuis worktree branch, HEAD = main a6540cb)

## État initial (audit before-shot)

**BuildScript actuel** : `Assets/Editor/BuildScript.cs` (54 lignes, namespace `CrowdDefense.Build`) — seulement `BuildWebGL()` + `ApplyWebGLPlayerSettings()`. Output legacy `Builds/WebGL/`. C7 contract dit output `Build/<platform>/`. Bundle ID actuel vide (`applicationIdentifier: {}`). productName `crowd-defense`. companyName `DefaultCompany`.

**Build WebGL actuel** : `Builds/WebGL/` = 6.5 MB total local. Compression Brotli, IL2CPP, memorySize 256MB.

**Pas de CI** : `.github/` n'existe pas, `tools/ci/` n'existe pas.

**Pas de ProjectSettings BuildProfiles** (Unity 6+ feature).

**Manifest packages relevant pour build** :
- `com.coplaydev.unity-mcp` (Editor only, doit pas casser builds standalone)
- `org.khronos.unitygltf 2.19.2` (runtime + editor)
- Unity AI assistant + inference (Editor-only, vérifier qu'ils ne polluent pas player builds)

---

## Sonnet briefs

### BUILD-E1 — BuildScript multi-target refactor + test local OSX

**Type** : feature-dev
**Estimé** : 1 commit, 30-45min
**Bloqué par** : —
**Files critiques** :
- `Assets/Editor/BuildScript.cs` (refactor de 54 → ~250 lignes)
- `ProjectSettings/ProjectSettings.asset` (set `applicationIdentifier` = `com.crowddefense.game` per platform group)

**Brief** :
Refactor `Assets/Editor/BuildScript.cs` pour respecter le contract C7 :
- `BuildWebGL()` — garder existing, mais changer output path `Builds/WebGL/` → `Build/WebGL/`
- `BuildOSX()` — Mac standalone Universal (Apple Silicon ARM64 + Intel x86_64). Output `Build/OSX/CrowdDefense.app`.
- `BuildWindows()` — Win64 x86_64. Output `Build/Windows/CrowdDefense.exe` + DataFolder.
- `BuildLinux()` — Linux64 x86_64. Output `Build/Linux/CrowdDefense.x86_64`.
- `BuildIOS()` — iOS Xcode export. Output `Build/iOS/` (Xcode project). Note : exec only sur macOS host.
- `BuildAndroid()` — Android APK (development), et un méthode séparée `BuildAndroidAAB()` pour Play Store AAB. Output `Build/Android/CrowdDefense.apk` ou `.aab`.
- `BuildAll()` — appelle séquentiellement BuildOSX, BuildWindows, BuildLinux (skip iOS car Xcode pas dispo en CI Mac runner direct, skip Android car keystore credentials). Log summary.

**Settings shared (apply once dans helper privé `ApplyCommonPlayerSettings()`)** :
- `PlayerSettings.applicationIdentifier` (NamedBuildTarget per-platform) = `com.crowddefense.game`
- `PlayerSettings.companyName` = `Crowd Defense` (override DefaultCompany)
- `PlayerSettings.productName` = `Crowd Defense`
- Bundle version pulled from `PlayerSettings.bundleVersion` (Mike updates manually pre-release)
- IL2CPP scripting backend pour TOUS les targets
- `PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.High)`
- Color space Linear (cohérent avec WebGL existant)

**WebGL spécifique** : garder ce qui existe (compression Brotli + decompression fallback + memorySize 256). AJOUTER :
- `PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None` (production)
- `PlayerSettings.WebGL.dataCaching = true`

**OSX spécifique** :
- `PlayerSettings.macOS.applicationCategoryType = "public.app-category.games"`
- Build target `BuildTarget.StandaloneOSX`
- `PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, 2)` = Universal Apple Silicon + Intel (2 = "x64ARM64" si valid, sinon use UserBuildSettings)
- Vérif via `UserBuildSettings.architecture` ou `PlayerSettings.macOS.buildNumber`

**Windows spécifique** : `BuildTarget.StandaloneWindows64`, `PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Standalone, Il2CppCompilerConfiguration.Release)`.

**Linux spécifique** : `BuildTarget.StandaloneLinux64`. Same IL2CPP + stripping.

**MenuItem entries** :
- `CrowdDefense/Build/WebGL` (existing renamed)
- `CrowdDefense/Build/OSX (Mac Universal)`
- `CrowdDefense/Build/Windows64`
- `CrowdDefense/Build/Linux64`
- `CrowdDefense/Build/iOS (Xcode)`
- `CrowdDefense/Build/Android APK`
- `CrowdDefense/Build/All Desktop (Mac+Win+Linux)`

**Commit** : `feat(build): BuildScript multi-target refactor + Mac/Win/Linux/iOS/Android methods per C7 contract`

**Verification** :
- Compile clean : pas d'erreurs Editor console (via `mcp__UnityMCP__read_console`)
- Test local Mac : exécuter `Unity -batchmode -nographics -projectPath /Users/mike/Work/crowd-defense/.claude/worktrees/agent-ab9d2e5c2553d10f6 -executeMethod CrowdDefense.Build.BuildScript.BuildOSX -quit` → produit `Build/OSX/CrowdDefense.app` artifact
- Liste `find Build/OSX -name '*.app' -maxdepth 2` doit retourner le .app

---

### BUILD-E2 — GitHub Actions workflows + Unity license docs

**Type** : feature-dev
**Estimé** : 2 commits, 45-60min
**Bloqué par** : BUILD-E1 (BuildScript methods doivent exister pour qu'Actions les appellent)
**Files critiques** :
- `.github/workflows/build-matrix.yml`
- `.github/workflows/deploy-webgl.yml`

**Brief** :
Setup GitHub Actions pour automated builds + auto-deploy WebGL via `game-ci/unity-builder@v3`. Mike doit ajouter UNITY_LICENSE secret (documenté en BUILD-E4).

**Workflow 1 : `.github/workflows/build-matrix.yml`** :
- Triggers :
  - `push: tags: ['v*.*.*']` (release tags)
  - `workflow_dispatch:` (manual trigger from GitHub UI)
- Jobs :
  - `build` (matrix.os = `[macos-14, ubuntu-latest]`, `matrix.target = [StandaloneOSX, StandaloneWindows64, StandaloneLinux64]`)
    - macos-14 host : builds Mac (StandaloneOSX) ET iOS (skip iOS for now si pas de cert)
    - ubuntu-latest host : builds Windows64 + Linux64 + WebGL via cross-compile Unity
    - Steps:
      1. `actions/checkout@v4` avec LFS true
      2. `actions/cache@v4` cache Library/ (key Library-${{ matrix.target }}-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }})
      3. `game-ci/unity-builder@v3` (with UNITY_LICENSE secret + buildMethod = `CrowdDefense.Build.BuildScript.Build<target>` mapped via case)
      4. `actions/upload-artifact@v4` upload `Build/<target>/` (retain 30 days)

**Workflow 2 : `.github/workflows/deploy-webgl.yml`** :
- Trigger : `push: branches: [main]` (auto-deploy à chaque merge sur main)
- Job :
  1. Checkout
  2. Cache Library/
  3. `game-ci/unity-builder@v3` buildMethod = `CrowdDefense.Build.BuildScript.BuildWebGL` targetPlatform = WebGL
  4. Deploy : `actions/upload-pages-artifact@v3` OR commit to `gh-pages` branch sous `/v6/` via `peaceiris/actions-gh-pages@v3`
  5. Notify : commit status check

**Secrets requis (Mike doit ajouter dans repo Settings → Secrets)** :
- `UNITY_LICENSE` : Personal Activation File ULF (cf game-ci docs : https://game.ci/docs/github/activation)
- `UNITY_EMAIL`, `UNITY_PASSWORD` (alternative Personal license activate)
- (Optionnel) `UNITY_SERIAL` pour Pro license

**Commit chain** :
1. `feat(ci): GitHub Actions build-matrix workflow Mac/Win/Linux via game-ci/unity-builder`
2. `feat(ci): GitHub Actions deploy-webgl auto-push /v6/ branch gh-pages on main push`

**Verification** :
- YAML lint clean : `yamllint .github/workflows/*.yml` OK (si yamllint dispo, sinon visual check)
- Workflows syntax valide : `gh workflow list` après push doit lister les 2 workflows
- Pas de credential leak (regex check : `grep -ri 'secret\|password\|key' .github/workflows/` → uniquement `${{ secrets.* }}` patterns)
- (Note : on ne PEUT pas tester le run sans UNITY_LICENSE — Mike doit l'ajouter, c'est un test E2E manuel)

---

### BUILD-E3 — Build size optimization audit + ProjectSettings tweaks

**Type** : quality-maintainer
**Estimé** : 1 commit, 30-45min
**Bloqué par** : BUILD-E1 (besoin du nouveau BuildWebGL output `Build/WebGL/`)
**Files critiques** :
- `ProjectSettings/ProjectSettings.asset` (tweaks managedStrippingLevel + texture compression defaults)
- `ProjectSettings/QualitySettings.asset` (vérifier global texture quality + master)
- `tools/ci/size-audit.md` (rapport avant/après)

**Brief** :
Audit le build WebGL actuel `Builds/WebGL/` (legacy path 6.5 MB) et appliquer optims pour viser <30 MB compressé Brotli. Mesurer impact via build re-run après tweaks.

**Audit steps** :
1. Mesurer baseline (avant) :
   - `du -sh Builds/WebGL/` (compressed bundle total)
   - `ls -lh Builds/WebGL/Build/*.data` (data payload)
   - `ls -lh Builds/WebGL/Build/*.wasm` (code payload)
   - `ls -lh Builds/WebGL/Build/*.framework.js` (loader)
2. Apply tweaks :
   - `PlayerSettings.stripEngineCode = true` (toutes platforms standalone)
   - `PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High)` (déjà spec C7)
   - `PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Master)` (production, slower compile mais smaller)
   - Texture compression : `TextureImporter.crunchedCompression = true` par defaut nouvelles textures (via `EditorBuildSettings` ou doc à Mike pour batch via menu)
   - Mesh Compression High : Default settings dans QualitySettings + override globalement les mesh imports
   - Audio compression : Vorbis Q60 (déjà via OGG natif probablement)
3. Re-build WebGL via `BuildScript.BuildWebGL()` (méthode refactorée E1, output `Build/WebGL/`)
4. Mesurer post-tweaks size + delta vs baseline
5. Document dans `tools/ci/size-audit.md` : table avant/après par asset type + total + recommendations futures (sprite atlas, addressables, etc.)

**Constraints** :
- Pas de modif gameplay code (uniquement build/import settings)
- Si Stripping High break à compile/runtime (réflexion utilisée) : fallback Medium + document
- Ne pas casser le build existant — re-test après chaque tweak

**Commit** : `perf(build): WebGL size optim stripping High + IL2CPP Master + crunched textures (X MB → Y MB)`

**Verification** :
- Build WebGL post-tweaks : success
- Compare size : ratio devrait être ≤ baseline
- Console clean (pas de stripping warnings critical)
- Rapport `tools/ci/size-audit.md` lisible

---

### BUILD-E4 — Setup docs Phase 4 credentials prep

**Type** : quality-maintainer (docs only)
**Estimé** : 1 commit, 20-30min
**Bloqué par** : BUILD-E2 (workflows existent, on documente les secrets requis)
**Files critiques** :
- `tools/ci/SETUP.md`

**Brief** :
Document pour Mike comment configurer les secrets/credentials Phase 4 sans avoir à fouiller chaque vendor doc. Pas d'install — juste instructions step-by-step.

**Sections à couvrir** :
1. **Unity License pour GitHub Actions** :
   - Comment générer activation file via `game-ci/unity-request-activation-file@v2`
   - Comment uploader sur https://license.unity3d.com/manual
   - Comment ajouter ULF/secret dans GitHub repo Settings → Secrets → Actions → `UNITY_LICENSE`
   - Lien officiel : https://game.ci/docs/github/activation
2. **Steamworks SDK setup (Phase 4)** :
   - Compte Steamworks à $100 USD (Steam Direct fee, one-time)
   - Download Steamworks SDK depuis https://partner.steamgames.com/
   - Install `Steamworks.NET` package via OpenUPM ou git URL (vérifier compat Unity 6 avant)
   - Setup `steam_appid.txt` dans `Build/OSX/`, `Build/Windows/`, `Build/Linux/`
   - Note : NE PAS commit `steam_api64.dll` ou autres binaires Steamworks (gitignore + Mike installe locally)
3. **Apple Developer cert + provisioning profile (iOS Phase 4)** :
   - Apple Developer Program $99/an
   - Generate cert + provisioning profile dans developer.apple.com
   - Export `.p12` cert + `.mobileprovision`
   - Ajouter secrets `APPLE_CERT_P12_BASE64`, `APPLE_CERT_PASSWORD`, `APPLE_PROVISIONING_PROFILE_BASE64` dans GitHub
   - Workflow iOS séparé (extension build-matrix.yml ou nouveau file)
4. **Google Play upload key + service account (Android Phase 4)** :
   - Create signing keystore : `keytool -genkeypair -alias crowddefense -keyalg RSA -keysize 2048 -validity 10000 -keystore crowddefense.keystore`
   - NE PAS commit le keystore (gitignore)
   - Ajouter base64 du keystore + password en secrets GitHub : `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`
   - Google Play Console : créer Service Account avec API access pour auto-upload (optionnel Phase 4)
5. **Workflow trigger conventions** :
   - Tags `v*.*.*` → builds all platforms via matrix
   - Push main → auto-deploy WebGL /v6/
   - Manual `workflow_dispatch` → ad-hoc rebuild
   - Rollback : redéployer un tag previous via workflow_dispatch

**Commit** : `docs(ci): SETUP.md instructions Phase 4 credentials Steam/Apple/Google Play prep`

**Verification** :
- Markdown valide (visual check)
- Tous les liens externes valides (curl HEAD sur les URLs critiques)
- Aucun secret réel dans le doc (greps `[A-Z0-9]{40}` regex génériques)

---

## Workflow d'exécution

1. **Sonnets parallèles** : E1 d'abord (BuildScript), puis E2 + E3 + E4 en parallèle (toutes branchent sur E1)
2. **QA-2 après chaque commit** : Sub-Opus check compile + ownership (auto, pas de Sonnet spawn pour rester rapide ; spawn formel QA seulement si suspicion)
3. **Test local validation** : après E1 → exécuter Unity batch mode BuildOSX, vérifier `.app` produit
4. **QA-3 pre-merge** : avant push branch axis/build, vérifier :
   - Compile 0 errors
   - 1 build OSX local success
   - WebGL build size mesuré + documenté
   - Commits atomic + Co-Authored-By footer
5. **Push axis/build** + rapport final `.claude/coordination/axis-build-report.md`

## Risques + mitigations

| Risque | Mitigation |
|---|---|
| `BuildIOS()` ne build pas en headless (manque Xcode/cert local) | Méthode existe mais ne s'exécute pas en CI sans secrets ; documenter dans SETUP.md |
| `Stripping High` break runtime (réflexion AnimationController via Animator) | Fallback Medium + log warning, re-test après |
| GitHub Actions Mac runner cher en minutes (10 min CI / build) | `actions/cache` Library/ aggressive caching = build incremental ~2 min après premier run |
| Bundle ID conflit App Store si déjà utilisé | `com.crowddefense.game` libre fait vérif Mike via App Store Connect Phase 4 |
| Unity 6 BuildProfiles (ProjectSettings/Build*.asset) pas encore configurés | Skip Phase 3 ; revisit Phase 4 si game-ci/unity-builder requires |
| `game-ci/unity-builder@v3` doesn't support Unity 6.3.15f1 yet | Vérifier compat avant : version matrix supportée. Fallback : self-hosted runner Mac Mike. |

## Critères de fin Stage A

- [x] BuildScript.cs refactored avec methods Mac/Win/Linux/WebGL/iOS/Android/All
- [x] GitHub Actions workflows commités (build-matrix.yml + deploy-webgl.yml)
- [x] Build WebGL size optim documenté (avant/après)
- [x] SETUP.md docs Phase 4 credentials
- [x] 1 build BuildOSX validation local artifact `Build/OSX/CrowdDefense.app`
- [x] QA-3 pre-merge : pass
- [x] Push axis/build + rapport final `.claude/coordination/axis-build-report.md`
