# File Ownership Matrix — Multi-Opus Swarm Coordination

**Date** : 2026-05-11
**Purpose** : éviter collisions Git quand 7 Sub-Opus orchestrateurs travaillent en parallèle avec leur swarm de Sonnets.

**Discipline** : chaque axe écrit **uniquement** dans sa zone. Les **hot zones** sont touchées par un seul Integrator (Main Orchestrator) en fin de cycle.

---

## Hot zones (INTEGRATOR ONLY — Main Orchestrator)

Ne JAMAIS être modifiées par un Sub-Opus ou un de ses Sonnets.

| Fichier | Raison |
|---|---|
| `Assets/Scripts/Entities/Tower.cs` | hooks audio + juice + VFX + anim |
| `Assets/Scripts/Entities/Enemy.cs` | hooks audio + juice + VFX + anim + slow trail |
| `Assets/Scripts/Entities/Castle.cs` | hooks audio + juice (castle_hit) |
| `Assets/Scripts/Systems/WaveManager.cs` | hooks audio (wave_start/clear) |
| `Assets/Scripts/Systems/LevelRunner.cs` | hooks audio (level_up) + music start/stop |
| `Assets/Scripts/Systems/Economy.cs` | hooks audio (coin_pickup, no_gold) |
| `Assets/Scripts/Data/BalanceConfig.cs` | refs vers audio/visual registries — modifs séquentielles MO uniquement |
| `STATUS.md` | source of truth multi-session, MO uniquement |
| `Packages/manifest.json` | dependencies, MO uniquement après due diligence |

---

## Axis ownership (Stage A — parallèle)

### Axis A : VISUAL-CORE
**Branch** : `axis/visual-core`
**Sub-Opus** : SO-VISUAL
**Zone exclusive write** :
- `Assets/Scripts/Visual/*` (sauf `JuiceFX.cs` déjà fait ✅, `AnimationController.cs` déjà fait ✅, `MaterialController.cs` déjà fait ✅, `Outline.cs` déjà fait ✅)
- `Assets/Shaders/*`
- `Assets/Materials/*`
- `Assets/Prefabs/VFX/*` (nouveau dossier)
**Read only** : `Assets/Scripts/Data/*`, `Assets/Models/*`, source Phaser `milan project/src-v3/systems/{Particles,JuiceFX,ToonMaterial}.js`

### Axis B : AUDIO-PIPELINE
**Branch** : `axis/audio`
**Sub-Opus** : SO-AUDIO
**Zone exclusive write** :
- `Assets/Audio/*` (sauf SFX `.ogg` déjà importés ✅)
- `Assets/Scripts/Systems/AudioController.cs` ✅ done — extensions OK
- `Assets/Scripts/Data/AudioClipRegistry.cs` ✅ done — extensions OK
- `Assets/Editor/AudioClipRegistryTool.cs` ✅ done — extensions OK
- `Assets/ScriptableObjects/Audio/*`
- `Assets/UI/SettingsPanel/*` (Audio volume sliders Settings panel — coord avec Axis F UX)
**Read only** : source Phaser `milan project/src-v3/systems/Audio.js`

### Axis C : ASSET-GEN
**Branch** : `axis/asset-gen`
**Sub-Opus** : SO-ASSET-GEN
**Zone exclusive write** :
- `tools/blender/*` (nouveau)
- `tools/comfy/*` (nouveau)
- `tools/mixamo/*` (nouveau)
- `Assets/Models/Enemies/*.glb` (re-exports des 11 Quaternius .gltf skipped)
- `Assets/Animations/*` (anim clips séparés Mixamo)
**Read only** : `Packages/manifest.json` (peut-être Blender package via UPM si besoin), AssetRegistry source

### Axis D : CONTENT-LEVELS
**Branch** : `axis/content`
**Sub-Opus** : SO-CONTENT
**Zone exclusive write** :
- `Assets/ScriptableObjects/Levels/*.asset` (80 LevelData)
- `Assets/Resources/LevelRegistry.asset` (re-build si nécessaire)
- `docs/specs/levels/*` (briefings + design notes)
**Read only** : `Assets/Scripts/Data/LevelData.cs`, `Assets/Scripts/Data/EnemyType.cs`, D1 specs `docs/specs/design/D1-*.md`

### Axis E : PLATFORM-BUILDS
**Branch** : `axis/build`
**Sub-Opus** : SO-BUILD
**Zone exclusive write** :
- `Assets/Editor/BuildScript*.cs`
- `Assets/Editor/CIBuilder.cs` (nouveau)
- `tools/ci/*` (nouveau)
- `.github/workflows/*.yml` (nouveau)
- `ProjectSettings/Build*.asset` (build profiles)
- `Build/` (output, gitignored)
**Read only** : tout le projet (config-only writes)

### Axis F : UX-POLISH
**Branch** : `axis/ux`
**Sub-Opus** : SO-UX
**Zone exclusive write** :
- `Assets/UI/*` (UXML/USS files)
- `Assets/Scripts/UI/*` (sauf hot zone : pas de touche aux scripts gameplay)
- `Assets/Resources/Localization/*` (i18n strings tables Unity Localization)
- `Assets/Fonts/*` (Roboto TMP Font Asset fix)
- `Assets/Scripts/UI/SettingsPanel.cs` (nouveau)
**Read only** : `Assets/Scripts/Systems/AudioController.cs` (pour bind sliders volume)

### Axis G : QA-AUTOMATION
**Branch** : `axis/qa`
**Sub-Opus** : SO-QA
**Zone exclusive write** :
- `.claude/qa/*` (nouveau)
- `Assets/Tests/*` (nouveau)
- `Assets/Editor/TestRunner.cs` (nouveau)
- `Assets/Scripts/Tests/*` (nouveau, sous-dossier de tests)
**Read only** : tout le projet (read pour comprendre, write seulement tests)

---

## Stage B : Integration (séquentielle, Main Orchestrator)

Après que **chaque axe ait livré son Stage A**, MO fait :
1. Merge `axis/*` branches dans `integration/phase3-4-5` branch
2. Sonnet **Integrator** unique consolide les hot zones :
   - Add `AudioController.Instance.Play("tower_shoot")` dans `Tower.cs` Fire()
   - Add `JuiceFX.Instance.Shake(0.05f, 100)` dans `Tower.cs` Fire()
   - Add `VfxPool.SpawnImpact(pos, color)` dans `Tower.cs` Fire()
   - Add `AnimationController._anim.SetTrigger("attackTrigger")` dans `Tower.cs` Fire()
   - Idem `Enemy.cs` (TakeDamage, Die), `Castle.cs` (TakeDamage), `WaveManager.cs` (StartWave), `LevelRunner.cs` (LevelComplete)
3. Merge `integration/phase3-4-5` dans `main`
4. Rebuild WebGL + redeploy /v6/

**Ordre integration hooks** :
```
Tower.Fire() {
  // 1. audio
  AudioController.Instance?.Play("tower_shoot", 0.55f);
  // 2. juice (avant VFX pour que shake commence dès le tir)
  JuiceFX.Instance?.Shake(0.05f, 100);
  // 3. VFX particle
  VfxPool.Instance?.SpawnImpact(target.position, cfg.color);
  // 4. anim
  _anim?.TriggerAttack();
  // 5. logic existant (projectile spawn, damage)
}
```

---

## Branch merge order (MO)

```
main
 ├── axis/visual-core   (merge 1)
 ├── axis/audio         (merge 2)
 ├── axis/asset-gen     (merge 3, peut introduire .glb supplémentaires)
 ├── axis/content       (merge 4)
 ├── axis/build         (merge 5)
 ├── axis/ux            (merge 6, après audio pour Settings panel)
 └── axis/qa            (merge 7)
       │
       ▼
integration/phase3-4-5  (intégrateur applique hooks)
       │
       ▼
main (final merge + deploy)
```

---

## Conflict resolution protocol

Si un Sub-Opus détecte qu'il a besoin de modifier un fichier hors de sa zone :
1. **STOP** la modif
2. Écrire dans `.claude/coordination/requests/{timestamp}-{axis}.md` avec :
   - Fichier nécessaire
   - Raison
   - Modif demandée
   - Alternative envisagée si refus
3. Continuer sur d'autres tickets de l'axe
4. MO arbitre + applique la modif lui-même OU délègue à l'axe propriétaire

**Pas de "je sais mieux"** : un Sub-Opus qui écrit hors zone = work à reverter, perte temps.

---

## Notes sur tools partagés

**Unity-MCP** (`mcp__UnityMCP__*`) : single Unity instance. Si plusieurs Sub-Opus utilisent en concurrence : queue naturelle (Unity-MCP traite séquentiellement). OK mais latence ajoutée. Sub-Opus doit be patient.

**Bash / Edit / Read / Write** : pas de conflit, chaque worktree a son fs.

**Agent (spawn Sonnets)** : pas de limite documentée, mais throttle à 5 simultanés par Sub-Opus pour rester gérable.
