# ProjectSettings polish audit — 2026-05-12 23h45

**Scope** : 7 ProjectSettings/*.asset (lecture seule). Build readiness pre-Steam/Web/Mobile.

## Status table

| File | Status | Notes |
|---|---|---|
| EditorBuildSettings.asset | OK | 3 scenes enabled : Menu (0), WorldMap (1), Main (2). Order valide pour boot menu-first. |
| InputManager.asset | OK | Horizontal/Vertical/Fire1-3/Submit/Cancel/Jump présents + debug axes. `m_UsePhysicalKeys: 1` OK for cross-locale keyboards. |
| QualitySettings.asset | MISCONFIGURED | 6 levels (VeryLow/Low/Medium/High/VeryHigh/Ultra) mais **toutes ont `customRenderPipeline: {fileID: 0}`** — aucun URP asset wire par niveau. Fallback global GraphicsSettings only. Mobile (Android=2, iPhone=2) pointe Medium — pas d'URP mobile-tuned asset. |
| GraphicsSettings.asset | OK | URP wired (`1f494b8cd314d41d69f0d9c3b14b13be`). 28 Always Included Shaders dont les 3 W4-T0 URP (CoreCopy/StencilDither/HDRDebugView). LinearIntensity + ColorTemperature ON. |
| TagManager.asset | MISSING | `tags: []` vide. Layers 0-5 default (Default/TransparentFX/Ignore Raycast/Water/UI) — pas de layers Tower/Enemy/Castle/Path/Projectile. Code utilise `CompareTag` uniquement via `PerkTag` enum (data-driven), pas string tags → OK pragmatique mais limite Physics LayerMask. |
| PhysicsManager.asset | OK | Gravity -9.81, layer matrix all-1 (collide all). `m_QueriesHitTriggers: 1`. `m_AutoSyncTransforms: 0` (perf-friendly). Pas de raycast layer-filtering utilisé (1 seul Physics.Raycast → EnemyHoverController.cs:23). |
| TimeManager.asset | OK | Fixed Timestep = 2822399/141120000 = ~0.02s (50Hz physics). `m_TimeScale: 1`. Maximum Allowed Timestep 0.333s (anti-spiral OK). |

## Top 3 polish opportunities

1. **QualitySettings URP per-level missing** (Mobile perf risk)
   Reco : créer `URP_PipelineAsset_Mobile.asset` (no shadows, no MSAA, half-res RT) + wire dans Low/Medium `customRenderPipeline` field. Sinon mobile build utilise Desktop URP → frame drops Android/iOS.

2. **Tags vides + Layers minimal** (architecture future-proofing)
   Reco : ajouter tags `Tower`/`Enemy`/`Castle`/`Projectile` + layers `Enemies` (8) / `Towers` (9) / `Projectiles` (10) pour Physics LayerMask filtering (déjà partiellement utile : EnemyHoverController raycast collides UI by default).

3. **ProjectSettings metadata default** (Steam/Apple submission blocker)
   `companyName: DefaultCompany` + `productName: crowd-defense` (slug) → renommer `companyName: Crowd Defense Studio` (ou autre) + `productName: Crowd Defense` (display). Sinon Steam build affiche "DefaultCompany" dans Activity Monitor / Task Manager.

## Build readiness

- **WebGL** : READY. 28 shaders Always Included (W4-T0 fix appliqué), defaultScreenWidthWeb 960x600 OK pour itch.io embed, scenes ordered Menu→WorldMap→Main.
- **Mac standalone (Steam)** : 80% READY. Bloqué par `companyName` placeholder. URP wired global OK.
- **Mobile (iOS/Android)** : 60% READY. Bloqué par absence URP per-level mobile asset → risque 30 FPS non-tenu sur device low-end. Aussi Submit/Cancel mappés clavier (return/escape) → besoin touch UI complète (existante mais audit séparé).

Verdict global : **WebGL ship-ready, Mac/Mobile need polish avant Steam/store submission**.
