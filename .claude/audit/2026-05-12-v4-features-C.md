# Audit V4 vs V6 Look & Feel — C

> Diff visuel : textures, lighting, materials, post-fx, particles, shaders.
> Sources V4 : `/Users/mike/Work/milan project/src-v3/` — Three.js/Phaser.
> Sources V6 : `/Users/mike/Work/crowd-defense/Assets/` — Unity 6 URP.

---

## Textures

### Comptage brut

| Source | Chemin | Fichiers PNG |
|---|---|---|
| V4 textures Flux Schnell | `src-v3/public/textures/` | **75** |
| V4 sprites Phaser | `public/sprites/` | 8 |
| V4 assets Kenney | `public/assets/kenney/` | 17 |
| V6 textures jeu (hors third-party) | `Assets/Textures/` | **3** |
| V6 textures third-party bundlées | Quaternius + KayKit + KayKitDungeon | 76 |

### Diff par famille (Flux Schnell V4 → V6)

| Famille | V4 count | V6 count | Status |
|---|---|---|---|
| Ground/terrain (tileable 1024px) | 20 (10 thèmes × v1+v2) | **0** | MISSING |
| Path fill (tileable 512px, 10 thèmes) | 10 | **0** | MISSING |
| Sky/skybox (equirectangular 2048×1024) | 10 | **0** | MISSING |
| VFX sprites (sparkle, explosion, glyph, aura, cloud…) | 22 | **0** (procédural Unity PS) | ABSENT — remplacé |
| Anim water (4 frames 512px) | 4 | **0** (shader Toon_Water_Animated.shader) | ABSENT — remplacé |
| Anim lava (4 frames 512px) | 4 | **0** (shader Toon_Lava_Animated.shader) | ABSENT — remplacé |
| Pilote/référence dev | 4 + 1 référence | 0 | N/A |
| UI Achievements | 0 | 2 (tower_master, first_blood) | V6 only |
| Weather | 0 | 1 (dust.png) | V6 only |

**Gap critique** : 40 textures Flux Schnell custom (ground + path + sky) présentes sur disque V4 ne sont pas importées dans Unity. V6 n'a pas de dossier `Textures/Ground/`, `Textures/Sky/`, ni `Textures/Path/`.

Pipeline V4 : Flux Schnell local via `tools/gen_textures.py` + `tools/textures_manifest.json` (54 entrées définies, 75 PNG générés dont v2 itérations).
Pipeline V6 : aucun pipeline texture custom. Matériaux procéduraux ou shaders animés pour eau/lave ; Quaternius / KayKit pour environment 3D.

---

## Lighting

| Élément | V4 setup (`main.js` + `themes.js`) | V6 setup (`ThemeAmbientController.cs`) | Status |
|---|---|---|---|
| Directional (Sun) | `THREE.DirectionalLight` — intensité variable/thème (1.15→2.3), couleur/thème (`sunColor`) | `Light` Unity — intensité 0.5→2.0, couleur/thème | PRÉSENT, équivalent |
| Ambient / Fill | `THREE.HemisphereLight(sky=0xb0e0ff, ground=0x4a6a3a, 0.75)` — hémisphère avec sky/ground séparés | `RenderSettings.ambientMode = Flat` — une seule couleur ambiante | PARTIEL — V4 plus riche (sky vs ground) |
| Per-theme sun color | 10 thèmes × sunColor + sunIntensity dans `themes.js` | 10 thèmes dans `SunParams()` switch | PRÉSENT |
| Fog | `THREE.Fog` couleur + distance/thème | `RenderSettings.fogMode = Linear` + `FogParams()` switch | PRÉSENT |
| Skybox texture | Equirectangular PNG Flux Schnell par thème (sky_plaine, sky_volcan…) | `_SkyTint` couleur sur skybox material Unity | PARTIEL — V4 images réelles, V6 tint procédural |
| PointLight projectile | 2 PointLights persistants pré-alloués (muzzle + BluePill) | `VfxPool.PortalLightFlashRoutine` crée un PointLight dynamique | PARTIEL — V6 moins systématique |
| Castle PointLight | `PointLight(0xff2020, 2.5, 8)` au hit | Non porté explicitement | MISSING |

---

## Materials

| Material custom | V4 path | V6 path | Status |
|---|---|---|---|
| ToonMaterial (3-step gradient) | `systems/ToonMaterial.js` (79 LOC) — `MeshToonMaterial` + canvas gradient 4×1 | `Assets/Materials/ToonBase.mat` + `Assets/Shaders/ToonCelShading.shader` + `Assets/Shaders/Toon/Toon_Lit.shader` | PRÉSENT |
| Water animated | `Shaders.js` `createWaterMaterial()` — ShaderMaterial GLSL, 4 frames Flux Schnell + caustics + foam edge | `Assets/Materials/Water.mat` + `Assets/Shaders/Toon/Toon_Water_Animated.shader` | PRÉSENT structurellement — textures frames source manquantes |
| Lava animated | `Shaders.js` `createLavaMaterial()` — 4 frames Flux Schnell cyclées | `Assets/Materials/Lava.mat` + `Assets/Shaders/Toon/Toon_Lava_Animated.shader` | PRÉSENT structurellement — textures frames source manquantes |
| Portal vortex | `Shaders.js` `createPortalMaterial()` — GLSL ring + runes rotatives | `Assets/Materials/Portal.mat` + `Assets/Shaders/Portal_Vortex.shader` + `Portal.shader` | PRÉSENT |
| Hologram cyberpunk | `Shaders.js` `createHologramMaterial()` — GLSL scanlines + glitch + fresnel | `Assets/Materials/Hologram.mat` + `Assets/Shaders/Hologram.shader` + `Hologram_Scanline.shader` | PRÉSENT (+ variante scanline bonus) |
| Kelp/plant sway | `Shaders.js` `createKelpMaterial()` — vertex bend on Y axis | `Assets/Materials/Kelp_Sway.mat` + `Assets/Shaders/Kelp_Sway.shader` + `Kelp.shader` | PRÉSENT |
| Starfield | `Shaders.js` `createStarfield()` — Points geometry, vertex size + twinkle | `Assets/Materials/Starfield.mat` + `Assets/Shaders/Starfield.shader` | PRÉSENT |
| Jellyfish | `Shaders.js` `createJellyfishMaterial()` — vertex sin pulsation + tentacles | `Assets/Materials/Jellyfish.mat` + `Assets/Shaders/Jellyfish.shader` | PRÉSENT |
| Smoke trail | `Shaders.js` `createSmokeTrail()` — Points geometry, age-faded | `Assets/Materials/SmokeTrail.mat` + `Assets/Shaders/SmokeTrail.shader` | PRÉSENT |
| Toon snow | Non présent V4 | `Assets/Shaders/Toon/Toon_Snow.shader` | V6 only |
| Outline inverted hull | `OutlinePass` Three.js (post-process) | `Assets/Shaders/OutlineInvertedHull.shader` + `Outline.cs` | PRÉSENT (technique différente) |
| Boss hologram / Boss jellyfish | Non présent V4 | `Assets/Shaders/BossHologram.shader` + `BossJellyfish.shader` | V6 only |
| Outline dynamic color | `ToonMaterial.cellShadingOutlineColor()` — adaptatif luminosité fond | Couleur fixe dans `OutlineInvertedHull.shader` | PARTIEL |

---

## Post-processing

| Effect | V4 implem | V6 implem | Status |
|---|---|---|---|
| Outline entities | `OutlinePass` Three.js (`edgeStrength=3, thickness=2, color=0x000000`), appliqué sur toutes tours + ennemis | `OutlineInvertedHull.shader` + `Outline.cs` (inverted hull, scale 1.02) | PRÉSENT — technique différente (PP pass → geometry) |
| Bloom | Non présent (OutlinePass uniquement, pas de UnrealBloom) | URP Volume `Bloom` — threshold 0.9, intensity 0.5, scatter 0.7 | V6 amélioration |
| Vignette HP-driven | Non présent | URP Volume `Vignette` — 0→0.45 selon % HP, flash rouge au hit | V6 amélioration |
| Color grading / theme | `sun.color` + fog per thème | URP `ColorAdjustments` — exposure, contrast, colorFilter, saturation par thème | V6 amélioration |
| Chromatic aberration | Non présent | Non présent | N/A |
| FXAA / SMAA | Renderer WebGL natif | URP FXAA/MSAA configurable | Équivalent |
| Tonemap | THREE.js `ACESFilmicToneMapping` (renderer) | URP HDR + tonemapping pipeline | Équivalent |

---

## Particles (VFX)

| Effet V4 (`Particles.js`) | V6 path (`VfxPool.cs`) | Status |
|---|---|---|
| `emit()` — particules radiales génériques | `SpawnImpact()` + `SpawnDeath()` | PRÉSENT |
| `emitColored()` — couleur par enemy type | `SpawnDeathPuff(tier)` tinted | PRÉSENT |
| `emitSprite()` — texture VFX PNG Flux Schnell (sparkle_gold, fire_burst, etc.) | Non porté — VfxPool utilise ParticleSystem procédural, pas de sprite textures custom | MISSING — 22 sprites VFX non importés |
| Pool size 400 sprites Three.js | Pool Unity ObjectPool 14 types × 24 instances | PRÉSENT (architecture différente) |
| Auto-LOD (maxParticles 50% si FPS<30) | `_lodMultiplier` 0.5× si avgFps<30 | PRÉSENT |
| `SpawnExplosion()` | `SpawnExplosion(radius)` | PRÉSENT |
| `SpawnCoinBurst()` | `SpawnCoinBurst()` + `SpawnCoinPickup()` | PRÉSENT |
| `SpawnHitFlash()` | `SpawnHitFlash(target)` | PRÉSENT |
| — | `SpawnLevelUp()` | V6 only |
| — | `SpawnPerkPickup()` | V6 only |
| — | `SpawnFrost(radius)` | V6 only |
| — | `SpawnPortal()` + PointLight flash | V6 only |
| — | `SpawnFireBreath(origin, dir, dist)` | V6 only |
| — | `SpawnMuzzleFlash()` | V6 only |
| — | `SpawnAttackStream()` + `SpawnSpark()` | V6 only |
| — | `SpawnUpgradeBurst()` + `SpawnUpgradeConfetti()` | V6 only |

---

## Shaders custom

| Shader V4 (`Shaders.js`) | V6 port | Status |
|---|---|---|
| Water animated (GLSL — frames + caustics + foam edge) | `Toon_Water_Animated.shader` | PRÉSENT — sans les frames Flux Schnell comme inputs |
| Lava animated (GLSL — frames cyclées 4 fps) | `Toon_Lava_Animated.shader` | PRÉSENT — sans les frames Flux Schnell |
| Portal (ring + runes rotatives) | `Portal_Vortex.shader` + `Portal.shader` | PRÉSENT |
| Hologram (scanlines + glitch + fresnel) | `Hologram.shader` + `Hologram_Scanline.shader` | PRÉSENT (+ variante) |
| Kelp sway (vertex bend Y) | `Kelp_Sway.shader` + `Kelp.shader` | PRÉSENT |
| Starfield (Points + twinkle) | `Starfield.shader` | PRÉSENT |
| Jellyfish (pulsation dôme + tentacules) | `Jellyfish.shader` + `BossJellyfish.shader` | PRÉSENT (+ variante boss) |
| Smoke trail (Points age-faded) | `SmokeTrail.shader` | PRÉSENT |
| Toon cel-shading (3-step gradient) | `ToonCelShading.shader` + `Toon_Lit.shader` | PRÉSENT |
| Outline adaptive (OutlinePass PP) | `OutlineInvertedHull.shader` | PRÉSENT (technique changée) |

---

## JuiceFX

| Feature V4 (`JuiceFX.js`) | V6 (`JuiceFX.cs`) | Status |
|---|---|---|
| Camera shake (intensity + duration) | Identique — LateUpdate offset, fade-out | PRÉSENT |
| Screen flash overlay (CSS div) | UIToolkit VisualElement overlay | PRÉSENT |
| — | `PunchScale()` — bounce scale transform | V6 only |
| — | `SlowMo(timeScale, ms)` | V6 only |
| — | `ShakeOnCritHit()` throttled | V6 only |

---

## Synthèse look & feel

### Gap textures
- **40 textures Flux Schnell** sur disque V4 non importées dans Unity : 10 ground (1024px), 10 path fill (512px), 10 sky equirectangular (2048×1024), et les 4 frames eau + 4 frames lave utilisées comme inputs shaders.
- Les shaders eau/lave V6 existent mais sont orphelins sans leurs frames sources — ils tournent probablement en mode fallback couleur unie ou erreur.
- V6 a 0 texture terrain Flux Schnell custom ; l'identité visuelle repose entièrement sur les assets Quaternius (PBR générique, pas stylisé cartoon).

### Top 5 features visuelles manquantes par impact

1. **Ground textures par thème** — Sol tileable Flux Schnell custom (cartoon, saturé) entièrement absent. V6 utilise Quaternius Grass/Rocks PBR — look réaliste incompatible avec la direction cartoon.
2. **Sky equirectangular par thème** — V4 avait des ciels photographiques/génératifs par thème (volcan dramatique, cyberpunk nuit, space nebula). V6 n'a qu'un tint de couleur sur le skybox Unity par défaut.
3. **Frames eau/lave comme inputs shaders** — Toon_Water_Animated et Toon_Lava_Animated sont codés pour des textures frame, mais les 8 PNG sources ne sont pas dans Assets. Effet animé probablement non fonctionnel.
4. **VFX sprites custom** — 22 sprites PNG Flux Schnell (sparkle_gold, glyph_arcane, heal_aura, etc.) non importés. V4 utilisait `emitSprite()` pour des particules avec texture custom ; V6 VfxPool est procédural (sans texture artisanale).
5. **HemisphereLight sky/ground split** — V4 `HemisphereLight(0xb0e0ff, 0x4a6a3a, 0.75)` donnait un éclairage ambiant différencié ciel/sol. V6 utilise `AmbientMode.Flat` (couleur uniforme) — perd la profondeur perçue sur les meshes.

### Nota
- La architecture shader V6 est complète et en avance sur V4 (bloom, vignette HP, color grading par thème, boss variants).
- La lacune est uniquement dans les **assets textures** (ground, path, sky, frames anim, vfx sprites) qui doivent être importés depuis `src-v3/public/textures/` vers `Assets/Textures/`.
- Pipeline Flux Schnell (`tools/gen_textures.py` + `tools/textures_manifest.json`) est utilisable tel quel pour générer les entrées manquantes.
