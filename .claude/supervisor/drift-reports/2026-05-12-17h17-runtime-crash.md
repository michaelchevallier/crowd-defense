# Drift Report — Runtime crash /v6/ post R6-PARITY-V4 P0+P1+fixes

**Date** : 2026-05-12 17h17
**Detection** : Mike a collé browser console output en chat (live test sur `https://michaelchevallier.github.io/crowd-defense/v6/`)
**Severity** : 🛑 CRITICAL — game crash dans la première frame, HALTING PROGRAM
**Drift criteria triggered** : D10 (build broken — runtime crash) + D11 (runtime exceptions ≥ 3) confirmed sur 1 check (charter §2 D10/D11 = 1 check suffit pour confirmed)

## Console output complet

### 1. Boot logs OK (Unity 6000.3.15f1 / WebGL 2.0 / OpenGL ES 3.0)

Engine init normal jusqu'à scene load `data.unity3d`.

### 2. 3 shaders URP not supported ❌

```
ERROR: Shader Hidden/CoreSRP/CoreCopy shader is not supported on this GPU (none of subshaders/fallbacks are suitable)
ERROR: Shader Hidden/Universal Render Pipeline/StencilDitherMaskSeed shader is not supported on this GPU
ERROR: Shader Hidden/Universal/HDRDebugView shader is not supported on this GPU
```

Cause probable :
- Shaders Unity URP non inclus dans build settings WebGL
- Project Settings > Graphics > Always Included Shaders manquant
- URP package version incompat avec Unity 6000.3.15f1 ?

### 3. NullReferenceException (1×, sans stack utile)

```
NullReferenceException: Object reference not set to an instance of an object.
  at UnityEngine.Bindings.ThrowHelper.ThrowNullReferenceException (System.Object obj)
```

Stack trace stripped (release build). Probable collateral du #4.

### 4. ArgumentNullException UIElements.Q (5×)

```
ArgumentNullException: Value cannot be null. Parameter name: e
  at UnityEngine.UIElements.UQueryExtensions.Q (VisualElement e, String name, String className)
  (répété 5 fois — variantes Q[T] inclus)
```

Cause probable :
- HUD UXML pas chargé → controller query `Q[T]` sur root null
- UI Toolkit binding cassé post R6-02 DELETE pass ou post R6-PARITY-V4 refacto
- Candidates : HelpOverlayController, TowerInfoPanel, MinimapController, BossBannerController, ou nouveaux post-P1 (CastleSkinController, DynamicEventManager UI ?)

### 5. RuntimeError table index out of bounds (CRITICAL, HALTS PROGRAM)

```
Uncaught RuntimeError: table index is out of bounds
    at 0e8c4ff6:0x1a8f0fc
    at 0e8c4ff6:0x21ca01b
    ...
Halting program.
```

WASM stack trace (Unity build, no symbol names). C'est un access out-of-bounds dans une table interne Unity ou dans le code C# JIT compilé.

Causes typiques :
- Array/List access avec index invalide
- Dictionary lookup sur key inexistante (cf `fd8f4a1` `DynamicEventManager.Dict<Tower, float>`)
- VFX pool index hors limite (cf `4b5a423` split VfxPool* + `06b8719` 9 textures wire)
- Boss behaviors static class index (cf `08d7229` + `7817aeb` split EnemyBossBehaviors)
- Path waypoint index dans PathTilesController (`a7e404c`)

## Commits suspects (chronologique)

| Commit | Risk | Reason |
|---|---|---|
| `02d44da` textures port | Low | Just assets, pas de logique |
| `a7e404c` PathTiles 280 LOC | **Medium** | Bitmask `mask = N\|E\|S\|W` + segments array — out-of-bound possible si grid topology rare |
| `0cbaae9` Skybox | Low | Material swap on load, peu de logique runtime |
| `57ffcd6` VFX 21 PNG wired | Medium | ParticleSystem renderers + texture sheet, missing texture = null ref possible |
| `4b5a423` REFACTOR VfxPool split 4 files | **Medium** | Refactor 555 LOC → 61 + 102 + 404 + 76, possible dispatch facade qui rate un call ou index dans BuildXxxModule |
| `29c50a6` 010 Weather 411 LOC | **Medium** | 11 themes presets, per-theme ParticleSystem switch — manque enum value possible |
| `a502416` 014 Boss phases | **HIGH** | Apocalypse 4 phases + warlord charge + dragon fire breath, gestion d'état multi-phase avec timers — bug potentiel élevé |
| `06b8719` 004-IMPL 9 VFX wire | Medium | 9 nouveaux spawn methods, possible index mismatch |
| `46a48db` 011 Castle/VFX skins placeholder-first | Low | Texture swap, placeholder = pas de logique |
| `f883fdd` 013 SceneDecor placeholder prims | Low | Just decor, peu de logique runtime |
| `08d7229` merge 014+005-IMPL | **HIGH** | Merge boss behaviors + 5 enemies (wizard/ai_hub/kraken) — pile critique d'intégration |
| `a49ed12` 012 Dynamic events sand_storm/lava_surge/carousel | **HIGH** | Mid-wave events affectant Tower.EventRangeMul/IsDisabled, Enemy.currentSpeedMul, Castle.TakeDamage — touch a beaucoup d'objets |
| `7817aeb` SPLIT EnemyBossBehaviors 582 → 446 + 145 | **HIGH** | Split + partial class récente, possible static class extraction qui rate un binding |
| `fd8f4a1` DynamicEventManager Dict<Tower, float> per-entity | Medium | Dict lookup possible KeyNotFoundException ou un cluster spawn bug |
| `ae7945b` VfxPoolFactions.cs.meta regen | Low | Just .meta file, no logic |

## Hypothèse principale (top 3)

1. **`7817aeb` SPLIT EnemyBossBehaviors** : extraction static class wizard/ai_hub/kraken vers `EnemyBossBehaviorsStatic.cs`. Si binding internal field access perdu (Enemy._teleportTimer/_burstSummonTimer/_tentacleSlamTimer), tick boss → KeyNotFoundException → table index out of bounds.

2. **`a49ed12` Dynamic events** : `% 5` auto-trigger touche Tower/Enemy/Castle. Index out of bound possible dans loop `foreach (var t in TowerPool.Active)` si pool reordered pendant l'event.

3. **`a502416` 014 Boss phases** + `08d7229` merge : Apocalypse 4 phases avec timers + summons. Array out-of-bound possible sur ApocalypsePhases array si phase index dépasse `phases.Length`.

## Actions immédiates prises

1. ✅ T1 push notif Mike envoyée via notify.sh (`🛑 DRIFT D10/D11 CONFIRMÉ — runtime crash /v6/`)
2. ✅ bug-fixer agent dispatched URGENT (ID `ab94607c0d28cb1fb`, background) avec full console log + commits suspects + investigation plan (4 fixes prioritisés : RuntimeError, UIElements.Q, shaders URP, NullRef)
3. ⏳ Instruction STOP-RUNTIME-CRITICAL à écrire (exec pause P2/P3 considérations)
4. ⏳ Append clean-log + commit

## Charter §3 D10/D11 action

D10 action : `STOP, build broken, run bug-fixer` ✅ done (bug-fixer dispatched)
D11 action : `STOP, runtime exceptions, investigate` ✅ done (bug-fixer investigates)

## Decision Mike post-fix

Une fois bug-fixer terminé :
- Si fixes triviaux (<30 min) : auto-deploy + Mike re-test live
- Si fixes complexes (>1h) : escalation Mike — propose plan ou revert partial commits R6-PARITY-V4 ?
