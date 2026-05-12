# Layer 1 — Build & Project Configuration

> Audit complet 2026-05-12 par Explore agent. Read-only — pas de fix appliqué.

## 1.1 Build Settings (EditorBuildSettings.asset)

**Scenes in Build (3 total, all enabled, correct order)** :

| Build Index | Scene | Status |
|---|---|---|
| 0 | `Assets/Scenes/Menu.unity` | enabled ✅ (first scene) |
| 1 | `Assets/Scenes/WorldMap.unity` | enabled ✅ |
| 2 | `Assets/Scenes/Main.unity` | enabled ✅ |

Toutes les scenes existent sur disque. Menu correctement positionné comme première scene.

## 1.2 ProjectSettings exhaustif

### Player Settings
- `productName` : `crowd-defense` ✅
- `companyName` : **`DefaultCompany`** ⚠️ (non customisé pour stores)
- Target Orientation : 4 (landscape) ✅
- Default resolution : 1920×1080 ✅
- Active Color Space : Linear ✅
- Rendering Path : Forward ✅

### InputManager.asset
Axes définis :
- Horizontal (left/a, right/d) gravity 3, dead 0.001 ✅
- Vertical (down/s, up/w) gravity 3, dead 0.001 ✅
- Fire1, Fire2, Fire3 (keyboard + mouse)
- Jump (spacebar)
- Mouse X, Y, ScrollWheel

⚠️ Pas de Fire4/Fire5 ni axes custom (spawn tower, pause, abilities).

### Physics (DynamicsManager.asset)
- Gravity : (0, -9.81, 0) ✅
- Solver iterations : 6
- Layer collision matrix : **all-collide** ⚠️ (pas de groupes Tower/Enemy/Terrain/UI)
- World bounds : 256m × 256m ✅
- Auto Sync Transforms : disabled (peut causer surprises mais standard Unity)

### AudioManager.asset
- Default Speaker Mode : 2 (stereo) ✅
- DSP Buffer Size : 1024
- Virtual Voice Count : 512
- Real Voice Count : 32
- Output Sampling Rate : 48kHz

### QualitySettings.asset
- Current quality level : 5 (mid-tier)
- 6 tiers définis (Very Low → Excellent)
- ⚠️ Aucun override URP-spécifique per quality (shadow distance, MSAA, post-FX)

### TimeManager.asset
- Fixed Timestep : 0.0203s (~50Hz) ✅
- Max Timestep : 0.333s
- Time Scale : 1.0

### TagManager.asset
- Tags : **vides** ⚠️ (aucun tag custom)
- Layers : Default, TransparentFX, Ignore Raycast, Water (4), UI (5) — 27 slots libres
- 1 sorting layer "Default"
- Rendering Layers : Default only

### GraphicsSettings.asset
- Render Pipeline : URP (guid `1f494b8cd314d41d69f0d9c3b14b13be`)
- Rendering Path : Forward
- 30 shaders Always Included
- LightsUseLinearIntensity : 1 ✅ (corrigé scrute #22 commit `65ea06f`)
- LightsUseColorTemperature : 1 ✅
- Transparency Sort : Default

### URPProjectSettings.asset
- Last Material Version : 10
- Settings Folder : URPDefaultResources

### EditorSettings.asset
- Serialization Mode : 2 (YAML text)
- Line Endings : 1 (Unix LF) ✅
- Enter Play Mode Options : enabled, options=0 (full domain reload OK)
- Async Shader Compilation : enabled ✅
- Force Asset Unload on Scene Load : enabled

## 1.3 Packages installed

### manifest.json (42 deps)
- `com.unity.render-pipelines.universal` : 17.3.0 ✅
- `com.unity.ai.assistant` : **2.7.0-pre.3** ⚠️ (pre-release)
- `com.unity.ai.inference` : 2.6.1
- `com.unity.multiplayer.center` : 1.0.1
- `org.khronos.unitygltf` : release/2.19.2 (git, **conflict**)
- `com.coplaydev.unity-mcp` : **main branch** ⚠️ (unversioned git)

### packages-lock.json transitive
- `com.unity.cloud.gltfast` : 6.14.1 (recherche R3 disait upgrade 6.15+ pour race condition)
- `com.unity.shadergraph` : 17.3.0
- `com.unity.collections` : 2.6.5
- `com.unity.burst` : 1.8.29
- `com.unity.mathematics` : 1.3.3
- `com.unity.nuget.newtonsoft-json` : 3.2.2
- `com.unity.dt.app-ui` : 2.1.1
- `com.unity.test-framework` : 1.6.0
- `com.unity.ugui` : 2.0.0

⚠️ **UnityGLTF 2.19.2 attend ShaderGraph 10.0.0 + Collections 1.0.0** — installé 17.3.0 + 2.6.5. Risque silent shader compile failures.

## 1.4 Resources folder

```
Assets/Resources/
├── Animations/Controllers/   (39 controllers)
├── Audio/                    (Music + SFX clips)
├── Materials/
├── Schools/                  (SO Schools 5 expected)
├── Textures/VFX/             (21 VFX PNG)
└── UI/                       (UnityDefaultRuntimeTheme.tss probable)
```

## 1.5 Issues identifiés (Layer 1)

| # | Sev | Issue | Impact |
|---|---|---|---|
| L1-1 | LOW | Pre-release packages `ai.assistant pre.3` + `unity-mcp main` | Dev-time risk, pas user-facing |
| L1-2 | LOW | `companyName: DefaultCompany` | Mobile store branding mauvais |
| L1-3 | MED | GLTFast 6.14.1 race condition (issue #772) + UnityGLTF version mismatch | Possible silent import failures (déjà workaround `dae7e48` disable ForceUpdate) |
| L1-4 | LOW | Pas d'axes Input custom (Fire4/5, AbilityKey) | Hotkeys hardcoded dans KeyBindings.cs OK |
| L1-5 | LOW | Layer collision matrix all-collide | Perf raycasts ; pas bloquant à 16 enemies |
| L1-6 | LOW | Quality settings sans URP override per tier | Polish |

## 1.6 Tickets recommandés

| Ticket | Priorité | Action | Effort |
|---|---|---|---|
| CONFIG-001 | LOW | Pin pre-release packages ou doc rationale | 15min |
| CONFIG-002 | LOW | `companyName` → `Mike Chevallier` ou équivalent | 5min |
| CONFIG-003 | MED | Test GLTF imports + upgrade UnityGLTF 2.20+ ou GLTFast 6.15+ | 30-60min |
| CONFIG-004 | LOW | Optional : add Input Fire4/Fire5 axes | 15min |
| CONFIG-005 | LOW | Layer matrix groups (Tower/Enemy/UI) | 30min |
| CONFIG-006 | LOW | URP per-quality renderer features | 30min |

## 1.7 Top 3 user-facing impact

1. **L1-3 GLTFast version mismatch** — peut expliquer "models ne s'importent pas" → enemies T-pose / Mesh_knight no Animator (cross-ref Layer 8 visual).
2. **L1-1 Pre-release packages** — non bloquant user, mais risque break en build production WebGL.
3. **L1-2 companyName** — purement cosmétique, pas bloquant.

**Conclusion Layer 1** : configuration globalement saine. 1 issue medium (GLTFast) + cosmétiques. Pas la cause majeure du gap V4↔V6 user-facing.
