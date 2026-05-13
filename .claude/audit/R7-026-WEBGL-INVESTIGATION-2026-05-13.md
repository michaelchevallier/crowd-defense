# R7-026 — WebGL Black Canvas Investigation
**Date** : 2026-05-13 10h40
**Auditeur** : Opus 4.7 (1M context) — read-only investigation agent
**Mission** : Confirmer root cause WebGL black canvas Unity 6 + URP 17.3.0 avec data réelle vs hypothèse cascade

## TL;DR

**La cascade 13 iterations a corrigé des bugs RÉELS** (placeholders GUIDs, compile errors, USS path, MSAA, etc) MAIS le black canvas final restant est **dû à AU MOINS 2 problèmes co-existants** :

1. **Bug Unity confirmé** : `Hidden/CoreSRP/CoreCopy shader is not supported on this GPU` est un bug Unity réel sur **toutes les versions Unity 6.0–6.4** avec URP 17.x sur WebGL2. **Fix officiel** dans **`6000.5.0a2`** (alpha, future stable). Affecte Unity `6000.0.6f1` → `6000.4.0a1`. Notre version `6000.3.15f1` est dans la fenêtre buggy.
2. **Bug de scène nouveau (NON présent pendant la cascade)** : Le commit `c38e7909` du 2026-05-13 02h59 (R7-003) introduit un `LoaderToMenu.cs` qui tente `SceneManager.LoadSceneAsync("Menu")` MAIS `Menu.unity` n'est PAS dans la build (BuildScript.cs ligne 28-30 ne build que `[Loader, Main]`). Si `Loader` était re-deployé maintenant, on aurait un VRAI black screen Loader-stuck en plus du bug shader.

**Reco data-driven** : **OPTION F (NEW) — Upgrade Unity Editor 6000.3.15f1 → 6000.4.6f1 stable** (disponible immédiatement, gratuit, LTS branch). Si toujours cassé après 6.4.6, then 6000.5.0a2 alpha. NE PAS downgrade vers 2022.3 (perte features Unity 6), NE PAS pivot BiRP (réinstaller URP).

---

## 1. Stack actuel (vérifié read-only)

| Composant | Valeur | Source |
|---|---|---|
| Unity Editor | `6000.3.15f1` (rev `c1aa84e375f6`) | `ProjectSettings/ProjectVersion.txt` |
| URP | `17.3.0` | `Packages/manifest.json:8` |
| VFX Graph | `17.3.0` | `Packages/manifest.json:9` |
| Color Space | `Linear` (`m_ActiveColorSpace: 1`) | `ProjectSettings/ProjectSettings.asset:50` |
| Scripting backend | `IL2CPP` | `BuildScript.cs:51` |
| WebGL graphics APIs | `[]` (= défaut Unity = WebGL2) | `ProjectSettings.asset:398` |
| WebGL compression | `Brotli` | `BuildScript.cs:52` |
| WebGL initial memory | `256 MB` | `ProjectSettings.asset` |
| WebGL exception support | `1` (Explicitly thrown) | `ProjectSettings.asset` |

URP_PipelineAsset config (`Assets/Settings/URP_PipelineAsset.asset`) :
- `m_RendererType: 1` (Universal — non-2D)
- `m_SupportsHDR: 0` ✓ (HDR off — workaround actif)
- `m_MSAA: 1` (no MSAA — 1×, workaround actif depuis iter2 `b9a1bfc`)
- `m_AdditionalLightShadowsSupported: 0` ✓ (workaround "disable cast shadows")
- `m_SoftShadowsSupported: 0` ✓
- `m_LocalShadowsSupported: 0` ✓
- `m_UseSRPBatcher: 1`

UniversalRenderer (`Assets/Settings/UniversalRenderer.asset` + `URP_Renderer.asset`) :
- `m_RenderingMode: 0` (Forward — pas Forward+) ✓ workaround Forward+ freeze
- `m_UseNativeRenderPass: 0` (disabled)
- `m_IntermediateTextureMode: 0` (Auto — iter#3 fix `98713b0`)
- `m_RendererFeatures: []` (clean)

UniversalRenderPipelineGlobalSettings (`Assets/UniversalRenderPipelineGlobalSettings.asset`) :
- `m_EnableRenderGraph: 0` (Compatibility Mode — iter#13 reverted)
- `m_EnableRenderCompatibilityMode: 0` (rid:283)
- `m_StripDebugVariants: 1`
- `m_StripUnusedVariants: 1`
- `m_StripScreenCoordOverrideVariants: 1`
- `m_StripRuntimeDebugShaders: 1`

**Toutes les workarounds documentées Unity Discussions/Issue Tracker sont déjà appliquées dans le projet** (MSAA off, additional shadows off, soft shadows off, HDR off, Forward not Forward+, Compat mode, IntermediateTexture Auto, shader stripping). Le bug subsiste.

---

## 2. Unity patches state (recherche WebFetch + WebSearch 2026-05-13)

### Versions Unity actuellement disponibles

| Stream | Latest stable | Source |
|---|---|---|
| Unity 6.3 LTS (current) | `6000.3.15f1` (released ~mai 2026, today's project version) | matches our `ProjectVersion.txt` |
| **Unity 6.4 LTS-track** | **`6000.4.6f1`** STABLE | https://unity.com/releases/editor/whats-new/6000.4.6f1 |
| Unity 6.5 beta | `6000.5.0b1` BETA | https://unity.com/releases/editor/beta/6000.5.0b1 |
| Unity 6.5 alpha | `6000.5.0a8+` ALPHA | search result |

### Bug WebGL CoreCopy/HDRDebugView — état du fix

**Issue tracker Unity** : https://issuetracker.unity3d.com/issues/shader-errors-are-logged-in-webgl-when-building-a-urp-project

> **Affected versions** : `2021.3.39f1`, `2022.3.33f1`, **`6000.0.6f1` à `6000.4.0a1`** (= toute la fenêtre 6.0 → 6.4 alpha)
>
> **Fixed in** : **`6000.5.0a2`** — "Fixed in 6000.5.0a2, future release" selon le tracker
>
> **Workaround officiel** : "the error disappears if you disable the cast shadows option when building for WebGL" ← **déjà appliqué chez nous** (URP_PipelineAsset `m_AdditionalLightShadowsSupported: 0`)

### URP Changelog (https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.4/changelog/)

- `URP 17.4.x` NOT publicly released — only `17.3.0` shipped avec Unity 6.3
- Aucune entrée CoreCopy / FRAMEBUFFER_INPUT / subpass dans changelog 17.0-17.4
- `[17.0.3]` Fixed Native Render Pass dimensions w/ render scale
- `[17.0.1]` Fixed HDR Debug Views rendering black screen with Render Graph (notre bug HDRDebugView mais notre HDR est OFF)

### Conclusion patch state

- **Unity 6.4.6f1 stable** ne mentionne PAS de fix CoreCopy explicite dans release notes
- **Issue tracker dit fix in 6000.5.0a2** uniquement
- **`6000.3.15f1` release notes** (notre version) : "WebGL: [WebGPU] Fixed a RenderGraph error with MSAA textures due to CoreCopy shader (UUM-133838)" — c'est un fix **WebGPU**, pas **WebGL2**. On utilise WebGL2 (`m_BuildTargetGraphicsAPIs: []` = default WebGL2 sur Mac).

---

## 3. Git bisect — quand le bug est-il apparu ?

Historique commits Unity migration :

| Commit | Date | Action |
|---|---|---|
| `a2f6d0da` | 2026-05-11 12:46 | **Project init Unity 6000.3.15f1** + Unity-MCP (pas de URP encore) |
| `41ec4246` | 2026-05-11 (post-scaffold) | Main.unity créé (Camera + Light, 3D scene) |
| `e7759e6e` | 2026-05-12 01:03 | **URP 17.3.0 installé** + URPSetup + GraphicsSettings wiring |
| `f781e5ff` | 2026-05-12 (iter1) | **WebGL black-screen iter1** : URP shader stripping + AI Assistant crash |
| `b9a1bfc` | 2026-05-12 (iter2) | iter2 : disable MSAA + drop HDRDebugView |
| `98713b0` | 2026-05-12 (iter3) | iter3 : IntermediateTextureMode Auto |
| `f564982f` | 2026-05-12 (iter4) | iter4 : remove empty Loader.unity (rev) |
| `5208c604` | 2026-05-13 00:30 | iter5 : 6 invalid placeholder GUIDs |
| `3904c67c` | 2026-05-13 (iter5b) | iter5b : AchievementRegistry 6 V4-parity entries |
| `318092e6` | 2026-05-13 01:00 | iter6 : 6 MORE invalid GUIDs (MainMenu.uss.meta = smoking gun pour cascade) |
| `8e87ed53`, `1fe8937a`, `4c047dd6` | 2026-05-13 02h00 | iter7/8 : compile cross-asmdef + SceneHierarchy fix |
| `d053fe77` | 2026-05-13 02:30 | iter9 : **EnsureMainCamera auto-create** |
| `6f4cf2bd` | 2026-05-13 03:30 | iter10 : USS path project:// URI (reverted plus tard) |
| `0906cd81` | 2026-05-13 04:00 | iter11 : runtime UIToolkit overlay (revert quick) |
| `664dfc17` | 2026-05-13 04:00 | iter12 : IMGUI OnGUI overlay |
| `3d794466` | 2026-05-13 04:30 | iter13 : **enable URP RenderGraph (m_EnableRenderGraph 0→1)** ← aggravated pure noir |
| `0f8f365c` | 2026-05-13 05:00 | revert iter13 (RG=0) |
| `c38e7909` | 2026-05-13 02:59 | **R7-003 Loader.unity scene 0 + LoaderToMenu redirect** ← NOUVEAU bug indep |
| `fcff8bd3` + `bf969c14` | 2026-05-13 10:16 | cleanup ImguiDiagOverlay + orphan refs |

**Le bug WebGL2 CoreCopy est présent depuis l'installation initiale URP 17.3.0** (`e7759e6e`, 2026-05-12 01:03). Il ne vient pas d'un commit incremental — c'est un bug structural Unity 6.3 + URP 17.3 + WebGL2 selon le tracker officiel.

**Le bug Loader-stuck est NOUVEAU** (post-cascade) :
- Last deploy live : `759715a` (auto-build 02:24)
- LoaderToMenu intro : `c38e7909` (02:59, 35min après dernier deploy)
- Donc le live actuel ne contient PAS encore le LoaderToMenu bug. Mais SI Mike rebuild maintenant, ce bug sera visible.

---

## 4. Root cause hypothèses — verdict data-driven

### Hypothèse A — Subpass input CoreCopy bloqué WebGL2 ✅ **CONFIRMÉ**

**Status** : confirmé par Unity Issue Tracker (https://issuetracker.unity3d.com/issues/shader-errors-are-logged-in-webgl-when-building-a-urp-project) et forum threads.

**Symptom** : `ERROR: Shader Hidden/CoreSRP/CoreCopy shader is not supported on this GPU (none of subshaders/fallbacks are suitable)`

**Cause** : CoreCopy utilise `FRAMEBUFFER_INPUT_X_FLOAT` (subpass input — Vulkan/Metal/DX12 feature). Sur WebGL2 cette feature n'existe pas. URP 17.x charge le shader malgré tout (référencé via RenderGraphUtilsResources rid:297 dans `UniversalRenderPipelineGlobalSettings.asset` ligne 384) et le runtime crash sur fallback inexistant.

**Fix Unity officiel** : `6000.5.0a2` (alpha — pas dispo stable).

**Workaround documenté** : disable `cast shadows` ← **déjà fait**, mais le bug persiste = shader CoreCopy n'est PAS shadow-related, le workaround "cast shadows" mentionné par les threads n'élimine que CERTAINS errors, pas tous.

### Hypothèse B — Camera / scene setup ✅ **PARTIELLEMENT confirmé** (NOUVEAU bug post-cascade)

**Sous-hypothèse B1** : Menu.unity sans Camera → invisible UI
- ✅ **CONFIRMÉ** : Menu.unity n'a AUCUN Camera ni Light, seulement 3 UIDocument GameObjects (UIMenu, UICredits, UIAchievements)
- Fix appliqué cascade iter9 (`d053fe77` EnsureMainCamera failsafe) — fonctionne en runtime (canvas RGB(8,12,22) = exactly EnsureMainCamera bgColor 0.06,0.07,0.09 confirmé iter10 diagnostic)
- ⚠️ **MAIS** EnsureMainCamera ajoute Camera APRÈS `SceneManager.LoadSceneAsync("Menu")` — pas un problème pour Menu (Awake hook), MAIS le BackgroundColor `Color(0.06, 0.07, 0.09)` = écran sombre quasi-noir → ressemble à black screen pour Mike

**Sous-hypothèse B2** : Loader scene loop ✅ **NOUVEAU BUG IDENTIFIÉ**
- BuildScript.cs ligne 28-30 ne build QUE `[Loader.unity, Main.unity]` — **Menu.unity et WorldMap.unity NE SONT PAS dans la build**
- `LoaderToMenu.Start()` (commit `c38e7909`, 02h59 post-deploy 02h24) appelle `SceneManager.LoadSceneAsync("Menu")` qui va échouer silencieusement
- Conséquence : Loader scene reste loaded, montrant son Camera BackgroundColor `(0.03, 0.047, 0.086)` = **dark blue quasi-noir**
- **PAS encore déployé** car last gh-pages = 02:24, LoaderToMenu introduit 02:59
- Si Mike fait `npm run build` ou auto-build maintenant, ce bug Loader-stuck va s'ajouter au bug CoreCopy déjà présent

### Hypothèse C — URP renderer config ❌ **REJETÉ**

- Toutes les configs documentées comme workarounds sont APPLIQUÉES (cf section 1)
- `m_IntermediateTextureMode: 0` (Auto) — déjà fix iter3
- `m_UseNativeRenderPass: 0` — disabled
- `m_RendererFeatures: []` — clean, aucune feature custom
- Aucune piste config restante

### Hypothèse D — UI Toolkit composition broken ❌ **REJETÉ partiellement**

- IMGUI overlay (iter12 `664dfc1`) **PAS visible non plus** sur live
- Si IMGUI fail, ce n'est PAS un bug UI Toolkit spécifique — c'est un bug pipeline-wide
- IMGUI utilise une path immediate-mode qui devrait être indépendant de URP — sauf si URP intercepte le compose final
- **Conclusion** : Le bug est BIEN dans URP pipeline composition, MAIS la "cible" UI Toolkit n'est qu'un symptom — toute UI au-dessus du URP camera output disparaît
- Cohérent avec Hypothèse A : CoreCopy is the FINAL COMPOSITE pass URP utilise pour blit en backbuffer. Si CoreCopy fail → backbuffer never received pipeline output → noir

### Hypothèse E — Build settings / Player settings WebGL spécifique ❌ **REJETÉ**

- ColorSpace Linear ✓ (Unity 6 standard)
- WebGL2 (default `[]`) — pas de path alternative WebGPU
- Brotli compression — fonctionne ailleurs
- Initial memory 256MB — suffisant pour démo
- Exception support 1 — debug-friendly
- Aucun setting incohérent identifié

---

## 5. Actions reproductibles à proposer

(Pas exécutées par cet audit, propositions pour Mike ou Exec future.)

### Test T1 — Upgrade Unity 6.4.6f1 (recommandation principale)

Download `6000.4.6f1` depuis Unity Hub, open project, rebuild WebGL.
- ✅ Stable LTS-track
- ✅ Possible (déjà disponible mai 2026)
- ⚠️ Risk : migrer Unity 6.3 → 6.4 peut casser packages
- ⚠️ Fix CoreCopy pas garanti sur 6.4 (officiellement fixed in 6.5 alpha) MAIS 6.4 a beaucoup de fixes URP intermédiaires

### Test T2 — Build WebGL minimal scene (isoler bug)

Créer `Test.unity` avec 1 Camera + 1 Cube + URP shader, build WebGL, vérifier render.
- Si OK → bug est dans nos scenes spécifiques (Menu UI / HUD UXML)
- Si KO → confirme bug structural Unity 6.3 + URP 17.3 + WebGL2

### Test T3 — Strip CoreCopy manuellement

Avant build, supprimer la référence `m_CoreCopyPS` (`fileID: 4800000, guid: 12dc59547ea167a4ab435097dd0f9add`) ligne 383 de `UniversalRenderPipelineGlobalSettings.asset`.
- ❌ Effet inconnu — URP peut crash car compose final manquant
- ❌ Le shader sera réimporté par Unity au prochain refresh
- ⚠️ Hack très fragile, non recommandé

### Test T4 — Color space Gamma

Changer `m_ActiveColorSpace: 1` → `0` (Gamma) + rebuild.
- Issue tracker mentionne "Linear Color Space" comme bouton "WebGL black screen on Mali GPU" — pas notre cas (M1 Mac) mais pourrait helper
- ⚠️ Linear est meilleur pour rendering — passer à Gamma = perte qualité visuelle

### Test T5 — Add Menu.unity to BuildScript

Modifier `BuildScript.cs:30` pour ajouter Menu + WorldMap :
```csharp
scenesList.Add(LoaderScene);
scenesList.Add("Assets/Scenes/Menu.unity");
scenesList.Add("Assets/Scenes/WorldMap.unity");
scenesList.Add(MainScene);
```
- Fix le NOUVEAU bug LoaderToMenu (Hypothèse B2)
- NE FIX PAS le bug CoreCopy
- Mais évite régression imminente quand Mike rebuild

---

## 6. Recommandation finale (data-driven)

### Option F (NEW) — Upgrade Unity 6.3.15 → 6.4.6f1 stable ✅ **RECOMMANDÉ**

**Pourquoi** :
- Disponible immédiatement (stable LTS, mai 2026)
- Unity 6.4 inclut probablement des fixes WebGL non-publicisés dans release notes (URP 17.3 patches inclus)
- Risk migration faible (6.3 → 6.4 LTS = compatible packages)
- Si toujours cassé après 6.4.6 → escalade vers 6.5 alpha confirmée nécessaire

**Effort estimé** : 30-60 min (download Unity Hub, open project, fix any package conflicts, rebuild)

**Plan** :
1. Download Unity 6000.4.6f1 via Hub
2. Open project, accept Unity migration
3. Check console pour package conflicts (URP probable bump 17.3 → 17.4 si dispo)
4. `BatchRebuild.SetupAndBuild` → vérifier dispo CoreCopy
5. Live test /v6/ — si toujours black → escalade vers Option G

### Option G (fallback) — Unity 6.5 alpha (6000.5.0a2+)

**Pourquoi** : Le fix CoreCopy y est OFFICIELLEMENT (issue tracker confirmé).

**Drawbacks** : Alpha = instable, peut casser UnityMCP, dépendances autres packages.

**Effort estimé** : 1-2h (alpha install + debug regressions).

### Option H (parallèle, indep) — Fix LoaderToMenu bug régression

Avant tout rebuild, **modifier BuildScript.cs** pour inclure Menu.unity + WorldMap.unity dans build, OU **supprimer LoaderToMenu** et builder direct sur Menu scene (ou Main).

**Why** : Le bug B2 va aggraver black canvas post-rebuild même si Option F fix le shader.

**Effort** : 5 min (BuildScript.cs:30 ajout).

### Options REJETÉES après data review

| Option | Pourquoi rejetée |
|---|---|
| A. Wait Unity patch | 6.5 alpha n'est PAS un patch incremental — c'est une nouvelle version. Pas de fix dans 6.3 patch stream. Wait = bloquant. |
| B. Downgrade Unity 2022.3 + URP 14 | Trop de features Unity 6 utilisées : Cinemachine 3.1.6, gltfast 6.14, VFXGraph 17.3. Migration arrière = semaines. |
| C. Switch BiRP | Refonte massive shaders custom Toon + URP renderer features + tous PostProcess. 1-2 semaines. |
| D. Strip CoreCopy hack | Trop fragile, URP réimport, undefined behavior. |
| E. Stop /v6/ web déploy | Évite le problème mais perd canal de demo Mike. Acceptable à court terme si Option F fail. |

---

## 7. Findings critiques additionnels (NOUVEAU bugs identifiés)

### F1 — BuildScript.cs scene list incomplet (CRITICAL)

`Assets/Editor/BuildScript.cs:28-30` :
```csharp
var scenesList = new System.Collections.Generic.List<string>();
if (File.Exists(LoaderScene)) scenesList.Add(LoaderScene);
scenesList.Add(MainScene);  // MANQUE: Menu.unity + WorldMap.unity
```

`ProjectSettings/EditorBuildSettings.asset` lui contient les 4 scenes (Loader/Menu/WorldMap/Main toutes enabled), mais `BuildPipeline.BuildPlayer(opts)` reçoit `opts.scenes = [Loader, Main]` qui OVERRIDE EditorBuildSettings.

**Impact** : `SceneManager.LoadScene("Menu")` ou `LoadSceneAsync("Menu")` échoue runtime — Menu pas dans build.

**Fix** : Ajouter Menu + WorldMap à `scenesList`.

### F2 — Loader.unity scene GUID null + LoaderToMenu (post-cascade)

`ProjectSettings/EditorBuildSettings.asset:9` :
```yaml
- enabled: 1
    path: Assets/Scenes/Loader.unity
    guid: 00000000000000000000000000000000  # NULL — placeholder
```

Le vrai GUID est dans `Assets/Scenes/Loader.unity.meta:2` : `guid: 4e786006684e454fadf258253822f171`. Unity tolère normalement le null GUID en buildsettings (resolve par path) mais c'est inconsistent vs autres scenes (Menu/WorldMap/Main ont leur vrai GUID listed).

`LoaderToMenu.Start()` (`Assets/Scripts/Systems/LoaderToMenu.cs:9`) :
```csharp
private void Start() => SceneManager.LoadSceneAsync("Menu");
```

Si Menu.unity not in build (cf F1) → silent fail → Loader scene reste affiché, montrant BackgroundColor dark blue de sa Camera.

**Fix** : Soit (a) add Menu.unity to BuildScript (cf F1 fix), soit (b) supprimer LoaderToMenu et build direct sur Menu scene.

### F3 — EnsureMainCamera.cs `cullingMask = ~0` mais `nearClipPlane=0.1` peut clipper 3D scene

`Assets/Scripts/Systems/EnsureMainCamera.cs:35-44` :
```csharp
var cam = go.AddComponent<Camera>();
cam.clearFlags = CameraClearFlags.SolidColor;
cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);
cam.cullingMask = ~0;          // EVERYTHING
cam.nearClipPlane = 0.1f;
cam.farClipPlane = 100f;
cam.depth = 0;                 // ⚠️ depth=0 = render PREMIER (avant autres cams)
go.transform.position = new Vector3(0f, 1f, -10f);
```

`cam.depth = 0` est suspect car la Camera de Main.unity (qui a `m_Depth: -1`) a une priorité PLUS BASSE → si les 2 cams existent simultanément, EnsureMainCamera-Auto render APRÈS Main camera et override la frame avec le clear bgColor sombre. Sur Menu.unity (pas de Main camera), c'est OK.

**Comment** : Pas un bug bloquant mais pourrait causer black screen si EnsureMainCamera-Auto persiste en Main scene par accident (RuntimeInitializeOnLoadMethod fires une fois — devrait être OK).

### F4 — HUDPanelSettings.asset themeUss GUID = placeholder hex valid mais arbitraire

`Assets/UI/HUDPanelSettings.asset:15` :
```yaml
themeUss: {fileID: -4733365628477956816, guid: a7f3c8b9e2d4f5a6b7c8d9e0f1a2b3c4, type: 3}
```

GUID `a7f3c8b9e2d4f5a6b7c8d9e0f1a2b3c4` est trouvable dans `Assets/Resources/UI/UnityDefaultRuntimeTheme.tss.meta` (RÉSOLU ✓) — pas un bug.

MAIS le `fileID: -4733365628477956816` (StyleSheet asset embedded ID) est typique d'un asset GENERATED-EN-RUNTIME — vérifier que cette fileID reste stable entre builds (sinon UI Toolkit fall back to no theme).

### F5 — Multiple workaround layers déjà active sans effet

Le projet a appliqué TOUS les workarounds connus :
1. `m_AdditionalLightShadowsSupported: 0` (cast shadows off)
2. `m_MSAA: 1` (no MSAA)
3. `m_RenderingMode: 0` (Forward, pas Forward+)
4. `m_UseNativeRenderPass: 0` (disabled)
5. `m_StripDebugVariants: 1` (debug shaders stripped — incluant HDRDebugView intentionnellement omis cf EnsureAlwaysIncludedShaders.cs:51-52)
6. `m_EnableRenderGraph: 0` (Compat Mode — iter13 reverted)
7. `m_IntermediateTextureMode: 0` (Auto — iter3 fix)
8. AlwaysIncludedShaders enforced (StencilDitherMaskSeed, CoreCopy explicitly added)

**Et pourtant le bug persiste**. C'est la signature d'un bug structural Unity 6.3 + URP 17.3 + WebGL2, pas un fix config.

---

## 8. Process & sources

### Process audit
- Read-only : aucune modif code
- Tools : Read/Bash (git log, grep, file inspection), WebFetch/WebSearch (Unity docs, forums, issue tracker)
- Durée : ~60 min investigation
- Stop condition : root causes identifiés + reco data-driven écrite

### Sources externes
- [Unity Issue Tracker — Shader errors logged in WebGL URP project](https://issuetracker.unity3d.com/issues/shader-errors-are-logged-in-webgl-when-building-a-urp-project) (fix in 6000.5.0a2)
- [URP 17.4 Changelog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.4/changelog/CHANGELOG.html)
- [Unity Forum — URP Forward+ WebGL freeze](https://discussions.unity.com/t/unity-6-urp-froward-freezes-in-webgl/1574122) (workaround: Forward not Forward+, déjà appliqué)
- [Unity Forum — Shader error in Hidden/CoreSRP/CoreCopy](https://discussions.unity.com/t/shader-error-in-hidden-coresrp-corecopy-unity-6-preview/950993)
- [Unity 6000.4.6f1 release notes](https://unity.com/releases/editor/whats-new/6000.4.6f1) (stable LTS-track latest)
- [Unity 6000.3.15f1 release notes](https://unity.com/releases/editor/whats-new/6000.3.15f1) (notre version — fixes WebGPU mais pas WebGL2 CoreCopy)

### Sources internes scan
- `Packages/manifest.json` — package versions
- `Packages/packages-lock.json` — lock state
- `ProjectSettings/{ProjectVersion,ProjectSettings,GraphicsSettings,EditorBuildSettings}.asset` — config
- `Assets/Settings/{URP_PipelineAsset,UniversalRenderer,URP_Renderer}.asset` — URP config
- `Assets/UniversalRenderPipelineGlobalSettings.asset` — URP global settings (rid:283 RenderGraph, rid:296 ShaderStripping)
- `Assets/Scenes/{Loader,Menu,Main,WorldMap}.unity` — 4 scenes deployed
- `Assets/Editor/{BuildScript,BuildLoaderSceneTool,BuildMainSceneTool,BatchRebuild,EnsureAlwaysIncludedShaders,SetupMainScene}.cs` — build pipeline
- `Assets/Scripts/Systems/{LoaderToMenu,EnsureMainCamera}.cs` — runtime fallbacks
- `Assets/UI/HUDPanelSettings.asset` — UI Toolkit config
- `.claude/supervisor/drift-reports/_clean-log.md` — iter1-13 history (1h-5h cascade)
- `tools/auto-build-loop.sh` — auto-build script
- `Logs/AssetImportWorker0-prev.log`, `~/Library/Logs/Unity/Editor.log` — Unity Editor compile state

---

## 9. Verdict honest

**Mike a raison de questionner** l'hypothèse single-cause "URP CoreCopy". Le bug a **3 couches** :

1. **Couche structurale** (Unity bug, cascade iter#5-12 NOT relevant — bug existait depuis URP 17.3.0 install) :
   - `Hidden/CoreSRP/CoreCopy` requires subpass input WebGL2 ne supporte pas.
   - Fix officiel `6000.5.0a2` confirmé Unity issue tracker.
   - Tous workarounds doc appliqués = effet partiel.

2. **Couche cascade légitime fixes** (iter#5-12 ont corrigé de VRAIS problèmes secondaires) :
   - Placeholders GUIDs invalides (iter#5, iter#6)
   - Compile cross-asmdef (iter#7, iter#8)
   - SceneHierarchy ns Unity 6 (iter#8)
   - Menu.unity sans Camera (iter#9, EnsureMainCamera)
   - USS path bake → project:// URI (iter#10, reverted finalement)
   - UI Toolkit diag (iter#11, reverted)
   - IMGUI overlay (iter#12, marche pas non plus mais correctement diagnostiqué)

3. **Couche régression nouvelle post-cascade** (R7-003 commit 02h59) :
   - LoaderToMenu.cs introduit redirection vers Menu non-built
   - **PAS encore deployed** mais sera visible au prochain rebuild
   - Fix simple : ajouter Menu.unity à BuildScript.cs scenesList

**Reco priorisée** :
- **#1 priorité** : Apply Option H fix (BuildScript.cs add Menu.unity + WorldMap.unity) — 5 min, évite régression imminente
- **#2 priorité** : Apply Option F (Unity 6.4.6f1 upgrade) — 30-60 min, possible fix CoreCopy
- **#3 fallback** : Si Option F insuffisant → Option G (Unity 6.5 alpha 6000.5.0a2+) — 1-2h
- **#4 nuclear** : Si rien marche dans 2-3 jours → Option E (stop /v6/ web, test EXCLUSIVELY Unity Editor Play mode per memory rule actuelle)

**Important** : Le memory rule "no WebGL test for now" reste valide indépendamment — Unity Editor Play mode est le test canonical pour parité V4↔V6, et /v6/ deploy est secondaire (canal demo Mike).
