# Audit batch P1 R6-PARITY-V4

Date : 2026-05-12 ~16h35
Auditor : agent superviseur (Opus, read-only)
Scope : 8 commits 4b5a423 → a49ed12 (batch P1 COMPLETE)
Charter ref : charter §1 cap 500 LOC strict TOLERANCE ZERO depuis ack P1-GO

## Conformité charter §1 cap 500 LOC

Fichiers C# **créés ou structurellement réécrits dans P1** :

| Fichier | LOC | Cap 500 | Verdict |
|---|---|---|---|
| Assets/Scripts/Visual/VfxPoolBindings.cs | 61 | OK | clean |
| Assets/Scripts/Visual/VfxPoolBuilders.cs | 404 | OK | clean |
| Assets/Scripts/Visual/VfxPoolBuilders2.cs | 242 | OK | clean |
| Assets/Scripts/Visual/VfxPoolExtra.cs | 68 | OK | clean |
| Assets/Scripts/Visual/VfxPoolTextures.cs | 102 | OK | clean |
| Assets/Scripts/Visual/SharedCurves.cs | 76 | OK | clean |
| Assets/Scripts/Visual/VfxPoolFactions.cs | 40 | OK | clean |
| Assets/Scripts/Visual/VfxPool.cs | 493 | OK | clean (proche cap) |
| Assets/Scripts/Visual/WeatherController.cs | 411 | OK | clean |
| Assets/Scripts/Visual/CastleSkinController.cs | 77 | OK | clean |
| Assets/Scripts/Systems/SceneDecorController.cs | 346 | OK | clean |
| Assets/Scripts/Systems/DynamicEventManager.cs | 218 | OK | clean |
| Assets/Scripts/Entities/EnemyBossBehaviors.cs | **582** | **FAIL** | **VIOLATION cap 500 (post-merge 08d7229)** |

Fichiers legacy touchés par P1 (déjà >500 avant le sprint, intouchés par charter pour P1 par implicite — note for follow-up) :

| Fichier | LOC | Δ P1 | Note |
|---|---|---|---|
| Entities/Enemy.cs | 2051 | -329 (014 extract partial) +18 (012 add) | net -311, mais reste à 2051 (>500) |
| Entities/Tower.cs | 2254 | +11 (012 EventRangeMul/IsDisabled) | regression dette technique (mineur) |
| Entities/Castle.cs | 762 | +1 (06b8719 SpawnHealAura) | regression dette technique (négligeable) |
| Data/EnemyType.cs | 154 | +8 (014) +32 (08d7229) | OK (<500) |

## Fidélité V4 par ticket

| Ticket | Port fidèle | Unity-amélioration documentée | Notes |
|---|---|---|---|
| 010 Weather | Partiel (7 V4 → 14 Unity) | Oui (7 nouveaux types Rain/Snow/Wind/Pollen/Mist/Fireflies/NeonRain) | Acceptable — extensions cohérentes V4 themes. |
| 011 Castle/VFX skins | Approximatif | Oui — Option A texture-swap (V4 GLTF non importable) | 8 themes castle / 4 vfx coin. Foire + Medieval **manquants** dans castle 8-skins (skip silencieux → default tint). |
| 012 Events dynamiques | **Partiel + divergence design** | Non documentée | V4 EventManager.js = 8 events data-driven via `level.events[]`. Port Unity = 3 events random `% 5` auto-trigger. 5 events V4 NON portés (void_pulse, zero_g, undertow, battle_cry, hack). Divergence trigger non flagué comme amélioration. |
| 013 SceneDecor | Partiel (placeholder prims) | Oui — placeholder Cylinder/Cube/Sphere/Capsule (V4 utilisait GLTF assets) | FNV-1a hash conservé pour seeding (fidèle). Décor xray fade et tower xray non portés (out-of-scope ticket, OK). |
| 014 Boss phases | Fidèle (étendu) | Apocalypse 4 phases + Warlord charge + Dragon fire breath. Spec respectée. | TickCharge/TickFireBreath bien dans partial Enemy.cs. |
| 004-IMPL VFX wire | Fidèle | 9 nouveaux pools/spawn + sub-emitter smoke_gray. | Files ≤ 500 LOC chacun (242/68/40/etc). |
| 004-REFACTOR | Fidèle (split) | 555→61 LOC dispatch facade + 3 extractions (Textures/Builders/SharedCurves) | API publique inchangée. Validation OK. |
| 08d7229 merge | Intégration 014 + 005-IMPL | Static class EnemyBossBehaviors injecte wizard/ai_hub/kraken | Merge correct mais pousse EnemyBossBehaviors.cs à 582 LOC → break cap. |

## REFACTOR validation (4b5a423)

| Fichier post-split | LOC | <500 ? |
|---|---|---|
| VfxPoolBindings.cs (dispatch facade) | 61 | OK |
| VfxPoolTextures.cs (texture loading) | 102 | OK |
| VfxPoolBuilders.cs (15 BuildXxxModule) | 404 | OK |
| SharedCurves.cs (gradient/curve helpers) | 76 | OK |

Verdict REFACTOR : **OUI — la violation pré-existante 555 LOC est résolue.** Split par catégorie de responsabilité (textures / builders / shared helpers). Public API identique, zero callsite modifié. Charter §1 cap 500 désormais respecté pour VfxPoolBindings et famille.

## Issues détectées

### Critiques (1)

1. **EnemyBossBehaviors.cs = 582 LOC (cap 500 dépassé +82)**
   - Origine : commit 014 a créé 446 LOC. Merge 08d7229 a ajouté +136 LOC (wizard_king + ai_hub + kraken) sans split.
   - Charter §1 TOLERANCE ZERO → c'est une régression cap **commise pendant P1**.
   - Fix proposé : extraire `static class EnemyBossBehaviors` (wizard/ai_hub/kraken = 138 LOC) vers `EnemyBossBehaviorsStatic.cs`. Ramène partial class à ~444 LOC.

### Mineures (4)

2. **Fichier .meta manquant : VfxPoolFactions.cs**
   - VfxPoolFactions.cs.meta absent du commit 46a48db. Unity générera un nouveau GUID au prochain open editor → warning console (ne bloque pas la compilation mais asset references inconsistant si étendu).
   - Fix : `unity-fixer` régénère le .meta.

3. **DynamicEventManager : _prevRangeMul / _prevSpeedMul écrasés en boucle**
   - StartSandStorm L83-100 : la variable `_prevRangeMul` est réassignée à chaque itération → seul le dernier tower a sa valeur préservée. StopSandStorm restaure la mauvaise valeur si entités hétérogènes (cluster boost ≠ 1f).
   - V4 EventManager équivalent utilise mul global runner (acceptable) mais ici per-entity write est inconsistant.
   - Mitigation actuelle : si toutes les towers ont EventRangeMul=1f au start (cas nominal), zero impact.

4. **R6-PARITY-012 trigger divergence non documentée**
   - V4 EventManager.js : `def.waveIndex === wave` data-driven via `level.events[]`.
   - Unity DynamicEventManager : `waveIdx % 5 == 0` random parmi 3.
   - 5 events V4 absents : void_pulse / zero_g / undertow / battle_cry / hack.
   - Commit message scope "3 events" mais sans flag "non-V4 trigger model".

5. **R6-PARITY-011 Foire + Medieval theme castle skin manquants**
   - ThemeSkins[] couvre 8/10 themes. Foire et Medieval → default tint silencieux.
   - Commit message annonce "8 castle theme swaps" → scope OK selon spec, mais documenter explicitement dans P2 backlog.

## Compile-readiness

Refs croisées vérifiées (toutes OK) :
- `WaveManager.OnWaveStart` event (PathManager.Paths, WaypointCountOnPath, GetWaypointOnPath)
- `EnemyPool.Instance.ActiveEnemies` IReadOnlyList<Enemy>
- `Castle.Instance.TakeDamage(int)`
- `Tower.EventRangeMul`, `Tower.IsDisabled` (déjà câblés OutOfRange L959 + AcquireTarget L966 + Update L832)
- `Enemy.ForceRecalcPath`, `Enemy.currentSpeedMul`, `Enemy.PathIdx`, `Enemy.IsDead`, `Enemy.CurrentWaypoint`
- `LevelEvents.OnLevelStart(LevelData, Bounds)` event
- `GridCoords.DECOR/TREE/BUSH/ROCK` + `GridCoords.CellToWorld`
- `GridData.At(c,r)`, `GridData.Width/Height/CellSize`
- `ShaderUtil.GetToonShader` (Common.ShaderUtil — used by SceneDecor)
- `VfxPool.SpawnHealAura/SpawnLightningChain/SpawnShieldAura/SpawnSlowField/SpawnImpact/SpawnExplosion/SpawnPortal/SpawnFireBreath`
- Partial class Enemy : fields `_teleportTimer/_burstSummonTimer/_tentacleSlamTimer` déclarés internal Enemy.cs L184-186, accessibles depuis EnemyBossBehaviors static class.

Aucune dependency cassée détectée. **Compile-readiness : OK.**

## Verdict superviseur

⚠️ **batch P1 — 1 issue critique (cap §1) + 4 mineures**

Le batch est **fonctionnellement complet** (8/8 tickets livrés, refs croisées OK, REFACTOR résout la violation pré-P1). Mais :

- **REFACTOR (4b5a423) : succès complet** — la violation 555 LOC est résolue, split propre par responsabilité, API inchangée.
- **MAIS le merge 08d7229 introduit une NOUVELLE violation cap §1** sur EnemyBossBehaviors.cs (582 LOC). C'est une régression pendant P1 lui-même, sur un fichier créé dans P1. Charter §1 TOLERANCE ZERO → doit être fixé avant ack final P1.

Action recommandée superviseur :
1. Re-split EnemyBossBehaviors.cs → static class extraite vers fichier séparé (~138 LOC), ramène partial Enemy à ~444 LOC.
2. unity-fixer regen VfxPoolFactions.cs.meta.
3. Filer Q-P2 : porter 5 events V4 manquants + design "data-driven via level.events vs % 5 auto-trigger" (12-decision).
4. P2 backlog : Castle skin Foire + Medieval.

Si Mike accepte mineurs 2-5 comme P2 follow-ups : **batch P1 = clean après fix issue 1 (split EnemyBossBehaviors)** — temps estimé split : 5 min, exec Sonnet.
