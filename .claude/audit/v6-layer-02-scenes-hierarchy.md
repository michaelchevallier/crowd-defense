# Layer 2 — Scenes & GameObject Hierarchy

> Audit complet 2026-05-12 par Explore agent. Read-only — pas de fix appliqué.

## 2.1 Inventaire scenes

| Scene | Build Idx | Roots | Status |
|---|---|---|---|
| Menu.unity | 0 | 3 | enabled ✅ first scene |
| WorldMap.unity | 1 | 2 | enabled ✅ |
| Main.unity | 2 | 39 (dont ~25 non-pool) | enabled ✅ |

## 2.2 Menu.unity

3 root GameObjects :

| GameObject | Active | Components | Wires |
|---|---|---|---|
| UIMenu | 1 | Transform, LevelSelectController, UIDocument, MenuController | Mostly unwired |
| UICredits | 1 | Transform, CreditsScreen, UIDocument | Unwired |
| UIAchievements | 1 | Transform, AchievementsPanel, UIDocument | Unwired |

UIDocument sourceAsset wired. Aucune main Camera dans la scene (UI-only via UIDocument).

## 2.3 WorldMap.unity

2 roots :

| GameObject | Active | Components | Wires |
|---|---|---|---|
| UIWorldMap | 1 | Transform, WorldMapController, UIDocument | sourceAsset wired |
| Main Camera | 1 | Transform, Camera | Wired (0, 1, -10), FOV 60° |

## 2.4 Main.unity

**39 roots** dont ~25 non-pool. Key GameObjects :

| GameObject | Active | Components | Notes |
|---|---|---|---|
| Systems | 1 | Transform, 30 children | Core systems container |
| MapRoot | 1 | Transform, MapRenderer | Tiles dynamic spawn |
| Castle | 1 | Transform, Castle, MeshRenderer, BoxCollider, MeshFilter | **Mesh NOT wired** ⚠️ |
| Main Camera | 1 | Transform, Camera, AudioListener, CameraShake | OK |
| HUD | 1 | Transform, 22 MonoBehaviours, UIDocument | UIDocument sourceAsset wired ✅ |
| Directional Light | 1 | Transform, Light | OK |
| EventSystem | 1 | EventSystem, StandaloneInputModule | OK |
| SkyboxController | 1 | Transform, SkyboxController | OK |
| CastleSkinController | 1 | Transform, CastleSkinController | OK |
| SceneDecor | 1 | Transform, SceneDecor | OK |

### Systems GameObject children (30 enfants)

PathManager, EnemyPool, CoinPullManager, BossSystem, FloatingPopupController, JuiceFX, ComboSystem, WaveManager, ProjectilePool, SlowEffectManager, Synergies, PerkSystem, DoctrineSystem, VfxPool, HeroProjectilePool, TutorialState, StatsTracker (+ 13 autres unnamed).

### HUD GameObject — 22 UI controllers

HudController, UIDocument, TutorialOverlayController, SpeedControlController, ComboHudController, AchievementToastController, WavePreviewController, WorldMapController, CutsceneController, SkinPickerController, HudPerkBadges, PerkPickerController, DebugHudController, MinimapController, BossUI, ShopController, DoctrineController, TowerToolbarController, ToastController, TowerStatsPanel, RuntimeThemeFixup, RadialMenuController.

## 2.5 SerializeField non-wired (fileID:0) — exhaustif

### Main.unity SerializeField gaps

| GameObject | Script | Field | Expected Type | Status |
|---|---|---|---|---|
| Achievements | CrowdDefense.Systems.Achievements | `registry` | AchievementsRegistry | ❌ NOT WIRED |
| HUD | CrowdDefense.UI.AchievementToastController | `registry` | AchievementsRegistry | ❌ NOT WIRED |
| Systems/PlacementController | CrowdDefense.Systems.PlacementController | `towerRegistry` | TowerRegistry | ❌ NOT WIRED (critique) |
| Castle | CrowdDefense.Entities.Castle | `_meshIntact` | Mesh | ❌ NOT WIRED |
| Castle | CrowdDefense.Entities.Castle | `_meshCracked` | Mesh | ❌ NOT WIRED |
| Castle | CrowdDefense.Entities.Castle | `_meshRuined` | Mesh | ❌ NOT WIRED |
| Castle | CrowdDefense.Entities.Castle | `_meshCritical` | Mesh | ❌ NOT WIRED |

### Menu.unity SerializeField gaps

Aucun détecté (UI-centric, peu de registry references).

### WorldMap.unity SerializeField gaps

Aucun détecté.

## 2.6 Issues critiques

### CRITICAL gaps

1. **PlacementController.towerRegistry NOT wired** (Systems GameObject)
   - Sans TowerRegistry, le menu placement ne peut pas afficher les options de tours
   - Impact : tower placement workflow brisé
   - Solution : wire vers TowerRegistry.asset

2. **Castle 4 meshes NOT wired** (Castle GameObject)
   - `_meshIntact/_Cracked/_Ruined/_Critical` tous à fileID:0
   - Castle render par défaut = cube primitive (visuel mauvais Mike's "gray castle" complaint)
   - Solution : wire vers 4 mesh assets (probablement dans Assets/Models/Castle/*.fbx ou .obj)

3. **Achievements registry x2 NOT wired**
   - Achievements GameObject + AchievementToastController.registry
   - Impact : achievement system non-fonctionnel, toasts ne peuvent pas afficher data
   - Solution : wire vers AchievementsRegistry.asset

## 2.7 First scene au boot

`Menu.unity` (Build index 0) — proper entry point ✅

## 2.8 Tickets recommandés

| Ticket | Priorité | Action | Effort |
|---|---|---|---|
| SCENE-001 | P0 | Wire PlacementController.towerRegistry → TowerRegistry.asset | 5min |
| SCENE-002 | P0 | Wire Castle 4 meshes → mesh assets per HP state | 15min |
| SCENE-003 | P1 | Wire Achievements.registry + AchievementToastController.registry → AchievementsRegistry.asset | 10min |
| SCENE-004 | P2 | Audit autres SerializeField non-listed (Systems children) | 30min |

## 2.9 Top 5 GameObjects à fixer

1. **Castle** (4 meshes manquants) — directement responsable visuel Castle pourri
2. **PlacementController** (towerRegistry) — bloque tower placement
3. **Achievements** + **HUD.AchievementToastController** (2 wires registries) — achievement system mort
4. **Systems children** — potentiel autres wires manquants non listed
