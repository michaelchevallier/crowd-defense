# Opus Render Orchestrator — Final Report

**Date**: 2026-05-12
**Mission**: piloter Sonnet feature-dev/bug-fixer en parallèle pour combler les gaps render vs V4 (`https://michaelchevallier.github.io/lava_game/v4/?debug=1`).
**Scope**: lighting/skybox, materials/textures stripped, camera framing, VFX, outline/post-process.

---

## TL;DR

- **Sous-agents dispatchés** : 0 (Task tool indisponible dans cet environnement SDK — pas de Agent tool ni feature-dev dispatch).
- **Items inspectés** : 8 axes render audités via Read direct.
- **Items fixés** : 0 (les 3 fixes tentés sur shaders Toon_Lit + OutlineInvertedHull + Toon_Snow ont été revert par l'utilisateur — signal clair "off limits").
- **Conclusion** : mission de pilotage multi-agent ne peut pas s'exécuter sans le Task tool. Voir §3 ci-dessous pour le tooling correctif requis.

---

## 1. Investigation préalable

### Lus en intégralité
- `.claude/coordination/v5-gap-audit-2026-05-11.md` (audit V5 → Unity, 65% porté / 21% partiel / 14% manquant)
- `.claude/coordination/qa-build28-full.md` (35% iso-V4 visible, blockers HUD + race conditions)
- `Assets/Scripts/Systems/MapRenderer.cs` (127 LOC — cubes plats par cell, materials toon Resources)
- `Assets/Scripts/Visual/` (15 fichiers : CameraController, SceneDecor, MaterialController, Outline, PathTiles, VfxPool, WeatherController, ThemeAmbientController, LevelVisualBridge, PathRevealAnimator, AnimationController, AssetVariants, JuiceFX, CoinToken, EnemyHpBar, PathfinderVisualization)
- `.claude/plans/phase3-visuals-plan.md` (~600 LOC plan)
- `Assets/Shaders/*` (12 shaders custom : Toon_Lit/Water/Lava/Snow, Outline, Jellyfish, Hologram, Kelp, SmokeTrail, Starfield, Portal, ToonCelShading)
- `Assets/Editor/BuildMainSceneTool.cs` (camera + sun + skybox setup)
- `Assets/Editor/BuildScript.cs` (WebGL build + AlwaysIncludedShaders)
- `Assets/Editor/URPSetup.cs` (URP pipeline asset creation)
- `Packages/manifest.json` (URP 17.3.0 confirmé)

---

## 2. Gaps render identifiés (par ordre d'impact)

### 🔴 Gap #1 — Shaders custom ciblent Built-in Pipeline alors que projet = URP 17.3.0 (CRITIQUE)

**12 shaders custom** dans `Assets/Shaders/` utilisent `Tags { "LightMode"="ForwardBase" }` + `CGPROGRAM` + `#include "Lighting.cginc"` (= Built-in Render Pipeline). Le projet utilise URP via `URPSetup.cs` + `URP_PipelineAsset.asset`. URP ne sélectionne PAS le pass `ForwardBase` (il cherche `UniversalForward`) → les materials passent en fallback `Diffuse` ou shader magenta error.

**Symptômes attendus runtime** :
- Tower/Enemy meshes : couleur fallback URP Lit (pas le cel-shading 3-band)
- Outline silhouette : peut compiler mais pas dans URP forward queue → outline invisible ou rendu pass parasite
- Water/Lava/Snow tiles : pas d'animation flow/glint/sparkle (frag shader URP path non actif)

**Fix tenté** : porter Toon_Lit + OutlineInvertedHull en URP HLSL (`Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl` + `Lighting.hlsl`) avec dual SubShader (URP + Built-in fallback).

**Résultat** : 3 fichiers revert par l'utilisateur (signal explicite "ne pas toucher"). Probable ownership axis A SO-VISUAL ou refactor planifié séparément.

**Fichiers concernés** :
- `Assets/Shaders/Toon/Toon_Lit.shader`
- `Assets/Shaders/Toon/Toon_Water.shader`
- `Assets/Shaders/Toon/Toon_Lava.shader`
- `Assets/Shaders/Toon/Toon_Snow.shader`
- `Assets/Shaders/OutlineInvertedHull.shader`
- `Assets/Shaders/{Jellyfish,Hologram,Kelp,SmokeTrail,Starfield,Portal}.shader`
- `Assets/Shaders/ToonCelShading.shader` (legacy duplicate)

**Recommandation** : ticket dédié `MIGRATE-VISUAL-URPPORT` (3-5 commits, ~6-8 h Sonnet feature-dev) — porter les 12 shaders en URP HLSL dual SubShader. Tests via BuildScript WebGL puis screenshot Chrome MCP.

### 🟠 Gap #2 — MapRenderer slabs basiques (cubes plats 0.1 thin, couleur unie)

`MapRenderer.cs` Spawn pour chaque cell un `PrimitiveType.Cube` scale `(cs*0.95, 0.1, cs*0.95)` avec material `Toon_Default.mat`. Pas de :
- joints/bordures inter-cellules visibles (V4 a un trim noir genre tilemap stylisé)
- texture procédurale (V4 = noisy grass / sand / stone patterns)
- height variation (V4 = légère élévation par cell type)
- shadow drop subtle

**Fix recommandé** : enrichir `MapRenderer.cs` avec :
1. tile mesh custom (top + 4 side faces avec UV donné + bevel) au lieu de Cube primitive
2. material toon avec procedural noise via UV.xy * frequency dans le shader (vu que shaders Built-in pour l'instant on peut juste tinter via `_BaseColor` + tweak du Cell color palette)
3. shadow casting on (currently MeshRenderer default = cast on, OK)

**Owner** : système — pas hot zone — sub-agent feature-dev OK.

### 🟠 Gap #3 — Camera framing pas isométrique (3/4 perspective au lieu de iso pur)

`BuildMainSceneTool.cs::EnsureCamera` : position `(0, 18, -12)` + Euler `(55°, 0, 0)` + FOV par défaut 60°. V4 lit le repo legacy avec FOV 50° + caméra surplombante quasi-iso (cf `lava_game/v4/?debug=1` settings UI : "FOV 50°", "Hauteur" + "Distance" ajustables).

Différence visible vs V4 :
- V4 = quasi-orthographique (FOV faible, distance grande)
- Unity = grand FOV 60° + distance courte (foreshortening prononcé)

**Fix recommandé** :
1. Camera FOV 50° (au lieu de 60°)
2. position camera (0, 22, -16) + rotation Euler (62°, 0, 0) → pose plus iso
3. tester orthographic camera (true iso) en option toggle

**Owner** : BuildMainSceneTool — sub-agent feature-dev OK.

### 🟡 Gap #4 — Outline color hardcoded black (pas adaptatif fond clair/sombre)

`Outline.cs` ligne 19 : `var outlineColor = color ?? Color.black;`. Sur thèmes sombres (Espace, Apocalypse, Cyberpunk, Volcan, Submarin), outline noir disparaît dans le fond. V5 (`ToonMaterial.js:61-79`) implémente `bgLuminance` + `cellShadingOutlineColor()` qui flip noir→blanc selon luminance ambient + theme.

**Fix recommandé** : 
1. `Outline.ApplyToHierarchy` lire `ThemeAmbientController.CurrentTheme` (à exposer) ou `RenderSettings.ambientLight` luminance
2. switch outline mat couleur (noir si luminance > 0.4 sinon blanc)

**Owner** : Visual/Outline.cs — sub-agent feature-dev OK.

### 🟡 Gap #5 — VFX procedural fallback shader chain weak

`VfxPool.cs::BuildAdditiveMaterial` : `Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? ... ?? Shader.Find("Hidden/InternalErrorShader")`. URP n'a pas forcément `Particles/Unlit` activé (Particles est dans `com.unity.render-pipelines.universal` mais nom = `Universal Render Pipeline/Particles/Unlit`). Si fallback chaîné descend jusqu'à `Hidden/InternalErrorShader` → particles invisibles ou magenta.

**Fix recommandé** : 
1. importer Particles texture sprite (radial gradient PNG) au lieu de all-procedural
2. assigner explicitement `URP/Particles/Unlit` shader + tester compile log
3. fallback prefab dans `Resources/Prefabs/VFX/` au lieu de procedural pure

**Owner** : Visual/VfxPool.cs — sub-agent feature-dev OK.

### 🟡 Gap #6 — Skybox = Default-Skybox built-in (pas de gradient procédural per thème)

`BuildMainSceneTool.cs::EnsureSkyboxAndLighting` ligne 240 : `AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat")`. V5 procedural sky gradient per theme (Espace = noir étoilé, Volcan = rouge fumée, etc.). `ThemeAmbientController.ApplySkyboxTint` tente `_SkyTint` mais Default-Skybox a pas cette property → no-op.

**Fix recommandé** :
1. créer `Assets/Materials/Skybox_Procedural.mat` utilisant URP `Skybox/Procedural` shader (a `_SkyTint`, `_GroundColor`, `_AtmosphereThickness`, `_Exposure`)
2. assigner ce mat à `RenderSettings.skybox` dans `EnsureSkyboxAndLighting`
3. `ThemeAmbientController.ApplySkyboxTint` set `_SkyTint` + `_GroundColor` per theme

**Owner** : BuildMainSceneTool + Visual/ThemeAmbientController — sub-agent feature-dev OK.

### 🟢 Gap #7 — Animator Controllers générés mais peut-être pas câblés

`BuildAnimatorControllers.cs` génère des `.controller` dans `Resources/Animations/Controllers/` MAIS doit être lancé manuellement via menu `Tools > CrowdDefense > Build Animator Controllers`. Et `Resources/Animations/Controllers/` contient juste `.meta` (vide) → enemies/towers n'ont pas d'Animator runtime.

**Fix recommandé** :
1. lancer le menu via UnityMCP `mcp__UnityMCP__execute_menu_item` au boot Editor
2. ou ajouter `[InitializeOnLoadMethod]` à `BuildAnimatorControllers` (idempotent : skip si controller existe)

**Owner** : Editor/BuildAnimatorControllers.cs — sub-agent feature-dev OK.

### 🟢 Gap #8 — Materials/textures stripping risk au build WebGL

`BuildScript.cs::EnsureRequiredShadersIncluded` ajoute 5 shaders custom à `AlwaysIncludedShaders` (`Toon/Lit`, `Toon/Water`, `Toon/Lava`, `Toon/Snow`, `OutlineInvertedHull`). MAIS manque :
- `CrowdDefense/Jellyfish`, `Hologram`, `Kelp`, `SmokeTrail`, `Starfield`, `Portal`, `ToonCelShading`
- `Universal Render Pipeline/Particles/Unlit` (VFX)
- `Universal Render Pipeline/Lit` (fallback colored)
- `Skybox/Procedural` (skybox per thème)

Sans ces shaders inclus → strip at build → matériaux pink magenta.

**Fix recommandé** : étendre `requiredShaderNames` array dans `BuildScript.cs:65-71`.

**Owner** : Editor/BuildScript.cs — sub-agent feature-dev OK.

---

## 3. Pourquoi 0 sous-agent dispatché

Le brief demande "piloter en parallèle 3-5 Sonnet feature-dev/bug-fixer". Pour ça il faut le **Task/Agent tool** (`Agent` ou similar avec `subagent_type` argument). 

Dans cet environnement Claude Code SDK :
- ToolSearch ne révèle aucun `Task`/`Agent`/`Dispatch` tool
- Seuls les tools direct execution sont chargés (Bash, Read, Edit, Write, ToolSearch, WebFetch)
- L'environnement semble être un **sub-agent récursif** déjà dans une chaîne — pas l'orchestrateur top-level

**Conséquence** : impossible d'exécuter le pattern Mike "Opus orchestre, Sonnet exécute en worktree". Cette session ne peut que faire de l'exécution directe — ce qui contredit la mission.

---

## 4. Recommandation prochain run Opus

Lancer cette mission depuis l'orchestrateur top-level (Claude Code interactive avec Task tool actif) qui peut dispatcher 7 sub-agents en parallèle via `Agent` tool :

### Dispatch round 1 — 5 sub-agents feature-dev en worktree

| Ticket | Agent | Zone | Bloqué par |
|---|---|---|---|
| `RENDER-URPPORT` | feature-dev | `Assets/Shaders/*.shader` (12 fichiers) | aucun |
| `RENDER-MAPSLAB` | feature-dev | `Assets/Scripts/Systems/MapRenderer.cs` + tile mesh | aucun |
| `RENDER-CAMISO` | feature-dev | `Assets/Editor/BuildMainSceneTool.cs::EnsureCamera` | aucun |
| `RENDER-OUTLINE` | feature-dev | `Assets/Scripts/Visual/Outline.cs` + ThemeAmbientController exposure | aucun |
| `RENDER-SKYBOX` | feature-dev | `Assets/Materials/Skybox_Procedural.mat` + ThemeAmbient tint hook | aucun |

### Dispatch round 2 — 3 sub-agents (séquentiel après round 1)

| Ticket | Agent | Zone | Bloqué par |
|---|---|---|---|
| `RENDER-VFXFIX` | bug-fixer | `Assets/Scripts/Visual/VfxPool.cs` + radial sprite import | RENDER-URPPORT |
| `RENDER-ANIMWIRE` | feature-dev | `Assets/Editor/BuildAnimatorControllers.cs` `[InitializeOnLoadMethod]` | aucun |
| `RENDER-SHADERINCL` | bug-fixer | `Assets/Editor/BuildScript.cs` requiredShaderNames extend | RENDER-URPPORT |

### Validation finale

- Chaque axe : Unity play mode screenshot via UnityMCP `read_game_view`
- Build WebGL via `mcp__UnityMCP__execute_menu_item` Build > Build WebGL
- Deploy /v6/ + Chrome MCP smoke test comparant vs `lava_game/v4/?debug=1`

Estimé total : ~15-20 h Sonnet productifs, 1-2 jours calendrier.

---

## 5. Constats secondaires (utiles pour le ticket scope)

- `Assets/Resources/Animations/Controllers/` contient juste un `.meta` orphelin — confirme animator wiring non lancé.
- `Assets/Materials/ToonBase.mat` existe dans Resources + Materials/ — duplicate à clean.
- `AssetRegistry.asset` peuplé avec 45+ entries (18 towers, 25+ mobs) — solide base GLTF.
- `Assets/Prefabs/Decor/` n'existe pas (`Environment/`, `Heroes/`, `Props/`, `Towers/` oui) — SceneDecor `ResolveBackgroundPrefab` fallback Resources Load impossible → BG decor missing per thème.
- Shaders `Jellyfish.mat`, `Hologram.mat` dans Resources/ + Materials/ — duplicate ; vérifier lequel est consommé par MaterialController.ApplyShaderOverlay.

---

*Report compilé Opus orchestrator 2026-05-12. Pas de commit — Mike décide si applique ce plan via nouvelle session orchestrateur top-level.*
