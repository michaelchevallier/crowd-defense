# Opus Gameplay Loop — Synthèse 2026-05-12

> Sub-Opus orchestrator gameplay loop. Mission : auditer la boucle wave button → spawn → towers fire → kill → coin → upgrade vs V4 cible, identifier blockers, scope sous-tâches Sonnet pour fix.

## TL;DR

La boucle gameplay est **architecturalement complète** côté code C# (LevelRunner / WaveManager / Enemy / Tower / Projectile / Economy / PlacementController / HudController tous wires). Les fixes critiques des sessions précédentes (`e42759f` null guards, `047d431` MonoSingleton overflow, `2962ecd` HudController race, `6fcff10` EnemyPool basePrefab) sont **présents dans le code** et fonctionnellement correct sur audit statique.

**Le principal blocker runtime restant est visuel** : Hero spawn fallback green cube car AssetRegistry.asset n'a aucune entrée `hero_*` malgré commits c2122d6 / d8bd135. Les Tower/Enemy GLTFs sont OK (registry contient tower_* et mob_* / boss_*).

**Pas pu spawn Sonnet en parallèle** : sub-Opus orchestrator en session sub-agent n'a pas accès au Task tool. Les sous-tâches sont scopées ci-dessous, à dispatcher par Mike top-level via worktrees.

## Architecture loop auditée

```
WaveManager.StartNextWave()              (HudController N key / btn click)
  → BeginWave(idx) → BalanceConfig swarmMul × pressure.mobCountMul
  → SpawnEnemy(type, pathIdx) every wave.spawnRateMs
    → EnemyPool.SpawnFromType() → Enemy.Init() — GLTF via AssetRegistry.Get("mob_*")
    → enemy walks PathManager waypoints → Castle ou tué
  → Tower.UpdateAttack() AcquireTarget + Fire()
    → ProjectilePool.Get() → Projectile.Init(target, dmg, ...)
    → projectile.Update() → ApplyHit() → Enemy.TakeDamage()
  → Enemy.HandleDeath() if hp<=0
    → publish EnemyKilledEvent → ComboSystem mul update
    → Economy.AddGoldFromKill(reward, worldPos) + FloatingPopup
  → WaveManager.Update() detects pendingSpawns=0 & activeEnemies=0
    → HandleWaveCleared() → Economy.ProcessInterestBank() → OnWaveCleared
  → LevelRunner.HandleWaveCleared() → TransitionTo(WaveBreak)
  → HudController re-shows wave-launch-btn via OnBreakStateChanged
```

Tout est branché. La boucle DOIT fonctionner. Les bugs probables sont visuels/timing/scene-config.

## Bugs identifiés (audit statique)

### B1 (CRITIQUE) — Hero spawn = green cube fallback

**Symptôme V4 cible : hero modèle 3D ; actuel : cube vert.**

- `AssetRegistry.asset` (113 lignes YAML) contient 17 tower entries + 26 mob/boss entries. **Aucune `hero_*` entry.**
- `HeroType.assetKey` correctement set (`hero_barbarian/knight/mage/ranger/rogue` confirmés dans SO YAML).
- `Hero.cs:163-179` → `AssetRegistry.Get(assetKey)` retourne null → fallback green cube.
- Root cause : `AssetRegistryTool.BuildAssetRegistry()` ne scan que `Models/Towers` + `Models/Enemies`. **`Models/Heroes` jamais scanné.**
- GLB sources OK : `Assets/Models/Heroes/KayKit/Characters/{Barbarian,Knight,Mage,Ranger,Rogue_Hooded}.glb`.
- `BuildAssetRegistryMappings.HeroIdToKey` ne contient que `{knight, knight}` — buggy mapping, devrait être `{knight, hero_knight}` pour matcher.

**Fix Sonnet bug-fixer (worktree axe gameplay-hero-mesh)** :
1. Etend `AssetRegistryTool.ScanDirectory` pour inclure `$"{ModelsRoot}/Heroes"` (3e appel après Towers/Enemies)
2. Add `AddHeroAliases(entries)` helper qui map `hero_<id>` → premier prefab scanné dont basename matche `<id>` (case-insensitive) ; KayKit name `Rogue_Hooded` → fallback liste `["rogue_hooded","rogue"]`
3. Run via `Tools/CrowdDefense/Build AssetRegistry Mappings` menu OU via batch `BatchRebuild.SetupAndBuild` (déjà appelé par `SetupMainScene.Run`)
4. Verify `AssetRegistry.asset` YAML post-build inclut `hero_barbarian/knight/mage/ranger/rogue` entries

### B2 (haute) — Default URP Renderer missing

**Build log** : `Default Renderer is missing, make sure there is a Renderer assigned as the default on the current Universal RP asset:URP_PipelineAsset`

→ Build WebGL passe mais erreurs shader stripping en cascade. Cause probable d'artefacts visuels au runtime (matériaux fallback magenta).

**Fix Sonnet bug-fixer (axe rendering-urp)** :
1. Inspecter `Assets/Settings/URP_PipelineAsset.asset` — vérifier `m_RendererDataList` non-vide + `m_DefaultRendererIndex` valide
2. Si vide : générer un `UniversalRendererData` et l'assigner via SerializedObject API
3. URPSetup.cs Editor script existe — peut nécessiter run unique

### B3 (haute) — Enemy GLTF reuse logic potentiellement buggy

`Enemy.cs:311-316` (`SpawnMeshChild`) : Si `_meshChild != null` (réutilisé du pool), il fait `_meshChild.SetActive(true)` et return — **sans vérifier que c'est bien le bon AssetKey**. Si le même pool slot a hébergé deux types différents (rare mais possible avec `_pools` indexé par typeId), bug visuel : enemy A skin sur enemy B mesh.

**Fix Sonnet** (low priority, pool est typed donc rare) : tracker `_lastAssetKey` sur Enemy ; if mismatch, destroy + respawn.

### B4 (moyenne) — `LevelRunner.cs` modifié par autre Opus (uncommitted)

`git status` shows `M Assets/Scripts/Systems/LevelRunner.cs` + `M Assets/Scripts/Systems/MetaUpgradeSystem.cs`. **Autre Opus en cours sur ces fichiers** — ne pas y toucher avant merge.

### B5 (basse) — `WaveManager.cs` no-fallback levelData

`WaveManager.Start():52` fallback `LevelRunner.Instance.CurrentLevel` mais si `LevelRunner` pas encore Awake'd → `Instance` est null. **Probablement OK** car LevelRunner crée Instance via MonoSingleton lazy getter. À monitor.

## Sous-tâches dispatch Sonnet (5 axes parallèles non-conflictuels)

### G1 — Hero GLTF wiring (bug-fixer)
- **Files** : `Assets/Editor/AssetRegistryTool.cs`, `Assets/Editor/BuildAssetRegistryMappings.cs`
- **Brief** : Fix B1. Ajouter scan `Models/Heroes` + alias map `hero_<id>` → prefab basename match. Run menu tool, commit YAML diff.
- **Vérif** : Unity Editor → `Tools/CrowdDefense/Build AssetRegistry Mappings` → AssetRegistry.asset contient 5 nouvelles entries `hero_*`. Run play mode → Hero affiche modèle KayKit, pas green cube.
- **Estimé** : 1 commit, 20 min.

### G2 — URP default renderer fix (bug-fixer)
- **Files** : `Assets/Settings/URP_PipelineAsset.asset`, `Assets/Editor/URPSetup.cs`
- **Brief** : Fix B2. Vérifier RendererDataList + DefaultRendererIndex. Si manquant, créer Forward Renderer asset + assigner.
- **Vérif** : Build log sans "Default Renderer is missing" + scene play mode sans materiaux magenta.
- **Estimé** : 1 commit, 30 min.

### G3 — Live smoke test E2E (qa-tester via Chrome MCP)
- **Files** : N/A (read-only inspection)
- **Brief** : Build /v7/ déployé (en cours via `auto-build-loop.sh` + Unity batch). Chrome MCP → load page → screenshot game_view → click wave button → screenshot → confirm enemies spawn + towers fire + gold counter incremente. Reporter avec screenshots dans `.claude/coordination/qa-build38-smoke.md`.
- **Vérif** : Screenshots avant/après wave 1 launch. Console errors via `read_console_messages`. Network requests via `read_network_requests` pour vérifier WebGL assets load OK.
- **Estimé** : 1 commit report, 20 min.

### G4 — Projectile visibility audit (feature-dev, optionnel)
- **Files** : `Assets/Prefabs/Projectile.prefab`, `Assets/Scripts/Entities/Projectile.cs`
- **Brief** : Vérifier que le Projectile prefab a un MeshRenderer + sphere mesh assigné + URP/Lit material. ProjectilePool fallback OK mais prefab assigné peut être broken visuellement.
- **Vérif** : Play mode, tower fire → projectile sprite visible vol vers enemy.
- **Estimé** : 1 commit si broken, sinon no-op + report.

### G5 — Tower upgrade radial menu visible (feature-dev, optionnel V4 parity)
- **Files** : `Assets/Scripts/UI/RadialMenuController.cs`, `Assets/UI/RadialMenu.uxml`
- **Brief** : V4 cible click tower → radial menu apparaît (3 upgrades L2/L3 + sell). Vérifier que click sur tower placée (PlacementController.SelectedTower != null) trigger menu visible. État actuel : code C# correct, UXML peut être broken.
- **Vérif** : Place tour, click dessus, menu apparaît centré, 3 options visibles.
- **Estimé** : 1-2 commits si UI broken.

## Hors-scope explicite (autres Opus owns)

- ❌ MapRenderer (camera/lighting/decor) → autre sub-Opus
- ❌ Visual polish toon/outline → axe SO-VISUAL existant
- ❌ Audio mix → axe SO-AUDIO existant
- ❌ `LevelRunner.cs` + `MetaUpgradeSystem.cs` (uncommitted M = autre Opus en cours)

## Recommandation dispatch Mike

Lancer G1 + G2 + G3 en parallèle (worktrees indépendants, 0 file overlap). G4/G5 en série après ou skip si V4 parity acceptable. Total estimé : 1h30 de Sonnet wall-clock + 30 min Mike merge/verify.

**Quick win** : G1 résout 80% du gap visuel ressenti par Mike (Hero green cube → Hero animé). G2 résout les warnings shader stripping qui font flicker matériaux runtime.
