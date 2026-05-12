# R6-PARITY-003 — Skybox per-theme images

**Sprint** : R6-PARITY-V4
**Type** : feature-dev (Sonnet, worktree)
**Priorité** : P0 (look & feel critical)
**Audit ref** : `.claude/audit/2026-05-12-v4-parity-gap.md` row "Skybox/skybox"

## Contexte

V4 a 10 skybox PNG equirectangular 2048×1024 Flux Schnell (sky_{theme}.png × 10 thèmes). V6 actuel : `_SkyTint` couleur procédurale uniquement, **aucune image skybox**.

## Task

1. **Source** : 10 skybox PNG dans `/Users/mike/Work/milan project/src-v3/public/textures/sky/` :
   - sky_plaine.png, sky_foret.png, sky_desert.png, sky_marais.png, sky_glacier.png, sky_volcan.png, sky_foire.png, sky_espace.png, sky_submarin.png, sky_medieval.png, sky_cyberpunk.png

2. **Si R6-PARITY-001 a déjà importé** les textures dans `Assets/Textures/Sky/`, parfait. Sinon import here.

3. **Créer 10 Unity Skybox materials** dans `Assets/Materials/Skybox/skybox_{theme}.mat` :
   - Shader `Skybox/Panoramic` (Unity built-in equirectangular)
   - Property `_MainTex` → texture sky correspondante
   - Property `_Rotation` 0 default
   - Property `_Exposure` 1 default

4. **`SkyboxController.cs`** (~80 LOC, `Assets/Scripts/Visual/SkyboxController.cs`) :
   - Subscribe `LevelEvents.OnLevelStart`
   - Sur level start, lookup `LevelTheme` enum → match material
   - `RenderSettings.skybox = skyboxMaterial`
   - `DynamicGI.UpdateEnvironment()` pour refresh ambient lighting

5. **Exploit Unity** (Mike addendum) :
   - Skybox cubemap convolution si possible (Unity peut convoluer equirectangular → cubemap auto)
   - Ambient mode `Skybox` (vs Flat) → ambient lighting baked from skybox
   - Reflection probe auto-update

6. **Placeholder-first** : si textures pas dispo, fallback couleur unie par thème via `_Tint` color material.

## Hard rules

- Cap 500 LOC `SkyboxController.cs` (largement sous, ~80 LOC attendu)
- No feature creep
- Self-report 100 mots max

## Deliverable

- Commit `feat(parity-v4-003): Skybox per-theme — 10 equirectangular materials + SkyboxController auto-switch`
- Self-report :
  - 10 materials créés
  - SkyboxController LOC
  - Ambient mode changed to Skybox : y/n
  - Reflection probe wire : y/n
  - Compile OK : y/n
  - Commit hash

## Time estimate

~1-2h (matériaux + controller simple, placeholder OK si textures absentes).