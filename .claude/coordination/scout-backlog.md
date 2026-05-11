# Backlog Scout - 2026-05-12 01:06

> Maintained by permanent SCOUT. Source data : `git log -60`, `STATUS.md`, `v5-gap-audit-2026-05-11.md`, `qa-build28-full.md`, `unity-review-post-merge.md`, `csharp-review-post-merge.md`, `recursion-suspects.md`, `phase3-postmortem` reports, `parallel-scout-backlog.md`, `live-audit-build25.md`.
> Iso target : V4 (https://michaelchevallier.github.io/lava_game/v4/?debug=1).
> Build state : Phase 3 swarm shipped → 35-65 % iso-V4 visible. Recent commits (e42759f → 9897c42) ont stabilisé runtime singletons + GLTF spawn + URP. Wave button race + EnemyPool null déjà patché (`2962ecd`, `8a067f5`). Boot mode actuel sans bug critique connu, mais gameplay loop end-to-end pas validé Chrome MCP post-fix.

---

## P0 - BLOCKERS RUNTIME (gameplay broken)

### Initialization & singletons
- [ ] **MonoSingleton<T> lazy auto-create masquerades scene-setup bugs en prod build** — `Assets/Scripts/Common/MonoSingleton.cs:13-27` log warn gated `UNITY_EDITOR || DEVELOPMENT_BUILD`, prod silently no-ops `[SerializeField]` deps. Risque enrollement Phase 4. Build verify : `Tools/CrowdDefense/Build Main Scene` idempotent + Unity Test Framework assert non-null after 1 frame.
- [ ] **Refactor non-MonoSingleton singletons** (EnemyPool, ProjectilePool, SlowEffectManager, CoinPullManager, Synergies) → inherit `MonoSingleton<T>` pour discipline init unifiée + safety net lazy. Files : `Assets/Scripts/Systems/EnemyPool.cs`, `ProjectilePool.cs`, `SlowEffectManager.cs`, `CoinPullManager.cs`, `Synergies.cs`. Verify : compile clean + `Tools/CrowdDefense/TestRunner.RunAll`.
- [ ] **DefaultExecutionOrder pour systèmes critiques** — `WaveManager`, `AudioController`, `VfxPool`, `JuiceFX`, `SettingsRegistry`, `LevelRunner`, `Economy` doivent Awake avant HudController + UI controllers (sinon HUD subscribe vide). Files : `Assets/Scripts/Systems/*.cs` add `[DefaultExecutionOrder(-100)]`. Verify : play mode W1-1 boot console clean + HUD complet visible.

### Validation runtime end-to-end (Chrome MCP)
- [ ] **QA smoke E2E /v6/?cb=latest** — rebuild WebGL + deploy gh-pages + Chrome MCP scenario complet : page load → hero spawn → wave button visible → click → enemies spawn → tower placement (toolbar visible) → kill 1 enemy → coin pull → perk picker → wave 2. Status actuel inconnu après dernière vague de fixes (`e42759f` → `9897c42`). Verify : `Builds/WebGL/` + `mcp__claude-in-chrome__navigate` + console clean.
- [ ] **Hero panel rendering** — `Assets/Scripts/UI/HudController.cs` Start() ne `.Q<>()` jamais les fields `heroPanel`, `heroHpLabel`, `heroLevelLabel`, `heroXpBarFill`, `heroXpLabel`, `heroXpValue`, `heroUltLabel` (lignes 42-48). UpdateHeroPanel() bail line 169. Verify : play mode → hero panel top-left visible avec HP/XP/lvl/ult ring.
- [ ] **HUD multi-controller UIDocument conflict** — TowerToolbar/SpeedControl/Minimap/HudPerkBadges partagent 1 UIDocument sur HUD GO → premier GetComponent wins, autres silent fail. Solution canonique : split 1 UIDocument par controller OU centraliser root cache. Files : `Assets/Scripts/UI/{TowerToolbarController,SpeedControlController,MinimapController,HudPerkBadges,HudController}.cs`. Verify : play mode → tous controllers visibles ensemble.

### Event wiring
- [ ] **LevelEvents.RaiseLevelStart/End jamais appelés** — `Assets/Scripts/Systems/LevelEvents.cs:20-24` zero hits. Minimap stays hidden (`MinimapController.cs:28-29` OnLevelStart subscriber), LevelVisualBridge theme jamais appliqué. Fix : `LevelRunner.Start()` après SpawnCastle()+SpawnHero() → `LevelEvents.RaiseLevelStart(currentLevel, gridBounds)`. RaiseLevelEnd dans SetState(Victory|GameOver). Verify : play mode W1-1 → minimap visible + theme appliqué.
- [ ] **OnLevelComplete zero subscribers → perk picker jamais affiché** — `Assets/Scripts/Systems/LevelRunner.cs:36` `public event Action? OnLevelComplete` jamais subscribed. `PerkPickerController.cs:12` commentaire dit "hooks ShowAndWait" mais wire absent. Fix : `PerkPickerController.Start()` add `LevelRunner.Instance?.OnLevelComplete += () => ShowAndWait(...)` + matching `-=` OnDestroy. Verify : play mode W1-1 victory → 3 cards perk picker s'affichent.

---

## P1 - HIGH IMPACT (visible iso V4 gap)

### Hot path perf (critical avant Phase 4 mobile)
- [ ] **Enemy.UpdateStealth + SetSlowTint allocs every frame** — `Assets/Scripts/Entities/Enemy.cs:258-280, 408-421` : `GetComponentsInChildren<Renderer>()` + `r.materials` allouent par frame par enemy. À 150 mobs × 60 fps = GC storm. Fix : cache Renderer[] dans Init + MaterialPropertyBlock + Shader.PropertyToID. Verify : Profiler GC.Alloc < 1 KB/frame swarm wave.
- [ ] **MaterialController.ApplyToon new Material(_toonBase) par spawn** — `Assets/Scripts/Visual/MaterialController.cs:38-39` instancie material à chaque renderer × spawn. 150 mobs × 3 renderers = 450 mats. Fix : Dictionary<Color, Material> cache keyed by tint OU MaterialPropertyBlock. Verify : memory snapshot post-wave count materials < 50.
- [ ] **VfxPool AutoReleaseRoutine alloc WaitForSeconds per spawn** — `Assets/Scripts/Visual/VfxPool.cs:155` chaque spawn alloc IEnumerator+WaitForSeconds. À 30 spawn/s en hot fight = 60 GC/s. Fix : ParticleSystem.OnParticleSystemStopped callback OU cache WaitForSeconds par pool (lifetime constant). Verify : Profiler.

### Bug logic + data parity
- [ ] **EventSystem.ApplyAction pipe-separator pas parsé** — `Assets/Scripts/Systems/EventSystem.cs:74` 4 events ont compound `"A|B"` actions (Event_haunted_shrine, Event_merchant_caravan, Event_raven_omen, Event_lava_geyser) → silent ignore. Fix : split `|` → call ApplyAction récursif chaque token. Aussi `random50:A:B` parser (Event_raven_omen). Verify : trigger Event_haunted_shrine → both effects apply.
- [ ] **EventSystem.ApplyAction recursion guard** — `Assets/Scripts/Systems/EventSystem.cs:103` `ApplyAction(mod.ApplyAction)` zero depth counter, cycle A→B→A = stack overflow. Fix : add `depth` param default 0, throw LogError si > 8. Verify : ModifierDef cyclique → 1 log error, pas de crash.
- [ ] **Outline shared material color race** — `Assets/Scripts/Visual/Outline.cs:55-76` static `_outlineMat` mutated via `SetColor("_OutlineColor")` chaque call. 2 spawns simultanés différentes couleurs → second clobbers first. Fix : MaterialPropertyBlock OU Dictionary<Color, Material>. Verify : spawn red+black outline same frame → tous deux rendus correct.
- [ ] **JuiceFX.LateUpdate overwrites camera pos always** — `Assets/Scripts/Visual/JuiceFX.cs:71-95` force snap `_cam.transform.position = _baseCamPos` chaque frame, break tout pan/zoom gameplay. Fix : track `_currentShakeOffset`, subtract prev frame avant new offset, jamais touch `_baseCamPos` hors `SetBaseCamPos`. Verify : Hero teleport (BluePill) + Shake → camera follows hero sans snap-back.

### Gameplay parity (V5 manquant ou partiel)
- [ ] **Castle.cs D1-04 regen + pressure mob W1-W10 non câblés** — `Assets/Scripts/Entities/Castle.cs:64` no-op POC, WaveManager.cs:258 commentaire "Phase 3 implémentera". Spec Q11 + D1-04 (no-regen W6+). Verify : play W7 → castleHP ne regen plus + pressure spawn pattern conforme spec.
- [ ] **Tower piercing/cascade/multishot wire-up runtime audit** — `Assets/Scripts/Entities/Tower.cs:605` flags présents mais audit gameplay réel pas fait. Test : Cannon L3 DPS branch fait bien cascade radius 3.0 ? Crossbow pierce traverse 3 cibles ? Verify : play W1-1 avec Tower L3 chaque branche + observation.
- [ ] **LevelRunner state machine `won/lost/running` + score system + run summary modal manquants** — Unity LevelRunner = 270 LOC vs V5 1333. Split WaveManager+RunContext mais flow run-end pas complet. Files : `Assets/Scripts/Systems/LevelRunner.cs` + new run summary overlay. Verify : play W1-1 victory → modal stats waves cleared/kills/gold/time.
- [ ] **Hero projectile pool intégration "stub"** — `Assets/Scripts/Entities/Hero.cs:586` commentaire explicit. HeroProjectile.cs (200 LOC) existe mais wire incomplete. Verify : Hero attack → projectile spawned via pool (pas Instantiate).
- [ ] **TutorialState phase 6 + proximity check** — V5 a 6 steps, Unity 5. Proximity auto-advance (player approche zone X → next step) absent. Files : `Assets/Scripts/Systems/TutorialState.cs:186`, `Assets/Scripts/UI/TutorialOverlayController.cs:142`. Verify : tutorial flow complet 6 steps.
- [ ] **Achievements.TrackEvent wiring (counters + predicate evaluation)** — `Assets/Scripts/Systems/Achievements.cs:80-85` TODO block. Wire 6 hot sites : Enemy.Die, Tower.OnPlaced, Economy.AddGold, WaveManager wave clear, LevelRunner world complete, Synergies activate. + CheckCounterAchievements impl. 51 SO assets existent (`Assets/ScriptableObjects/Achievements/`). Verify : play W1-1 first kill → first_blood unlock + toast.
- [ ] **WeatherController PlayAmbientAudio / StopAmbientAudio stubs** — `Assets/Scripts/Visual/WeatherController.cs:138-139` `/* TODO Phase 4 */`. Skybox makeSkyGradient aussi manquant. Verify : level W3 desert → ambient wind audio + sky gradient orange.
- [ ] **PerkPickerController fallback leaks SO instances** — `Assets/Scripts/UI/PerkPickerController.cs:86-91` `ScriptableObject.CreateInstance<PerkDef>()` × 3 chaque level-up, jamais Destroy. PerkRegistry maintenant peuplé donc fallback rare mais safety net. Fix : DestroyImmediate post-consumption OU drop fallback. Verify : Profiler memory pré/post 10 wave breaks.

### UI/V4 visible gaps
- [ ] **Game Over + Victory panel score breakdown** — V4 a stats container (waves cleared, enemies killed, gold earned, towers placed, perks, time, stars 1-3). Files : `Assets/UI/HUD.uxml`, `Assets/UI/HUD.uss`, `Assets/Scripts/UI/HudController.cs`, `L.cs`. V5 ref : `RunMode.js:272-330`. Verify : play W1-1 victory → stats pane + stars animate.
- [ ] **HUD font Roboto + i18n EN/FR sweep** — V4 polish UX gap. `Assets/Fonts/Roboto-Regular.ttf` présent (status `?? Assets/Fonts/`). Pas wired UIDocument PanelSettings. Verify : HUD labels en Roboto + L.SetLocale toggle fr/en update labels.
- [ ] **MapRenderer streams (water/lava) + bridges textures animées manquants** — V5 PathTiles.js 600 LOC. Unity MapRenderer = 127 LOC. W3 desert / W4 volcan / W8 submarin signature visuelle absente. Files : `Assets/Scripts/Systems/MapRenderer.cs`, new `Assets/Scripts/Visual/PathTilesController.cs`. Verify : load W4 → lava streams animés sur path.
- [ ] **PathTiles tile-by-tile reveal anim level start** — V5 BFS distance-from-spawn stagger 60ms. Files : `Assets/Scripts/Systems/MapRenderer.cs` + `Visual/LevelVisualBridge.cs`. Verify : load W1-1 → tiles apparaissent depuis portail.
- [ ] **Floating popups system damage/coin/gem** — partiel : FloatingPopupController.cs (126 LOC) + CoinToken.cs (169 LOC) existent. V5 Popups.js : spawnDamage/Coin/Gems + spawnFlyingCoin. Wire Enemy.TakeDamage + Economy.AddGold + TreasureTile.Collect. Verify : kill enemy → -dmg float popup ; treasure collect → +50¢ popup.

### Shaders V5 (identité visuelle)
- [ ] **8 custom shaders V5 manquants** — Shaders.js (445 LOC) : Water, Lava, Portal, Hologram (partial Assets/Resources/Hologram.mat), Kelp, Jellyfish (partial), Starfield, SmokeTrail. Seul Toon base + Outline portés. Files : new `Assets/Shaders/*.shadergraph` (URP). Bloque identité W2 forest / W4 volcan / W6 ocean / W8 submarin / W9 space / W10 apocalypse. Verify : load chaque world → biome signature shader visible.

### Singletons + scene drift
- [ ] **Tools/CrowdDefense/Build Main Scene idempotent** — `BuildMainSceneTool.cs` existe (commit 9897c42 ajoute Castle) mais ne couvre pas tous les Phase 3 singletons (VfxPool, JuiceFX, EnemyPool, ProjectilePool, SlowEffectManager, CoinPullManager, SettingsRegistry, Synergies, SkinSystem, RunContext, etc.). Files : `Assets/Editor/BuildMainSceneTool.cs`. Verify : new scene → run menu → tous singletons présents avec Inspector refs wired.

---

## P2 - POLISH (after iso V4 reached)

### Code quality
- [ ] **PerkSystem.cs:4 retire `using System.Linq;` mort** + check `using CrowdDefense.Visual;` orphan. Verify : compile clean.
- [ ] **SaveSystem.cs:264 supprime `IsStackable` dead code** (zéro caller). Verify : compile clean.
- [ ] **SaveSystem.cs:74-81 catch silencieux** → ajouter `#if UNITY_EDITOR Debug.LogWarning("[SaveSystem] Load corrompu, reset")`. Verify : corrupt PlayerPrefs → log Editor visible.
- [ ] **PerkSystem.cs:190 magic 1.5f Forteresse** → champ `PerkDef.forteresseCastleHpMul`. Verify : Inspector slider expose tweakable.
- [ ] **PerkSystem.cs:185 magic 8f aura range** → `const float DefaultTowerAuraRange = 8f` OU `BalanceConfig.DefaultTowerAuraRange`.
- [ ] **BossSystem.cs _defeatedPublished consolidate** → extract `PublishDefeatOnce()` helper, appelé depuis LateUpdate/ResetBoss/OnEnemySpawned. Verify : 2× kill boss W3 → 1 BossDefeatedEvent.
- [ ] **HudController.Start refactor 62 lignes → BindUiRefs() / WireCallbacks() / SubscribeSystems()** — cyclomatic complexity > charter limit.
- [ ] **Tower.ApplyL3Branch switch 77 lignes** → L3Stats struct + Dictionary<(id, branch), L3Stats> OU SO TowerType L3DpsStats/L3UtilityStats sub-objects (D1-03 spec implies data-driven).
- [ ] **SettingsRegistry.Save() debounce** — actuellement PlayerPrefs.Save() chaque setter (slider drag = 60 events/s thrash disk). Fix : coroutine debounce OnDisable / OnApplicationPause.
- [ ] **AudioController.SetMasterVolume mixe 2 paths via short-circuit** — `SetFloat` (dB) ou `AudioListener.volume` (0-1) selon mixer exposed param présent → semantically different units. Split en 2 methods.
- [ ] **DoctrineController lambda subscribe/unsubscribe mismatch (leak)** — `Assets/Scripts/UI/DoctrineController.cs:38, 45` lambdas distincts → unsub ne match jamais. Fix : extract `HandleDoctrineChanged(DoctrineDef? _) => RefreshCards();` method group. Verify : reload scene 2× → no NullRef cascade.

### Magic numbers → JuiceConfig SO
- [ ] **Magic numbers juice hooks** — Tower.Fire (0.55f, 0.05f, 100), Tower.UpgradeTo (0.3f, 200), Castle.TakeDamage (0.65f, 0.1f, 200, 0.4f, 150), Enemy.TakeDamage boss (0.3f, 400, 0.8f, 250), LevelRunner.OnVictory (0.4f, 500, 1.2f). Files : new `Assets/Scripts/Data/JuiceConfig.cs` SO + 1 .asset + remove magic numbers. Verify : tweak Inspector → effets juice changent runtime.

### V5 features manquants/partiels (post iso-V4)
- [ ] **RunMap roguelike map graph** (V5 RunMap.js 168 LOC) — Act 1-3, node types combat/rest/shop/mystery/boss, edges aléatoires, swarmMul. Unity WorldMapController = level select linéaire. Ossature run flow. Files : new `Assets/Scripts/Systems/RunMap.cs`, refactor `WorldMapController.cs`. Effort heavy.
- [ ] **PathVariant 3 layouts par world** — V5 PathVariant.js 124 LOC. Replay value. Files : new `Assets/Scripts/Systems/PathVariant.cs`. Effort med.
- [ ] **MapValidator grid sanity** — V5 MapValidator.js 283 LOC. Validate portals/castles/walkable connectivity. Bloque level editor tooling. Files : new `Assets/Editor/MapValidator.cs`.
- [ ] **SchoolDef SO standalone** — V5 5 schools encapsulé dans perks subset. Identité run. Files : new `Assets/Scripts/Data/SchoolDef.cs` + 5 .asset.
- [ ] **AssetVariants BOSS_TINTS + CASTLE_TINTS registries** — `Assets/Scripts/Visual/AssetVariants.cs:94` `ApplySkin + ApplyTheme` OK ; tint registries V5 non confirmés.
- [ ] **SceneDecor updateDecorFade + updateTowerXRay** — V5 fade decor LateUpdate + xray towers occlusion. Unity SceneDecor = SpawnForLevel/ClearAll seulement.
- [ ] **Enemy mesh cache miss pool reuse different AssetKey** — `Enemy.cs:163-168` recycle reuse `_meshChild` même si new EnemyType. Phase 2 hit (30 types). Fix : `if (_meshChild != null && _meshChild.name == "Mesh_" + assetKey)`.
- [ ] **MetaUpgrade/Doctrine magic strings → enum** — `Assets/Scripts/Data/MetaUpgradeDef.cs`, `DoctrineDef.cs`, systems switch on string keys. Fix : `enum MetaUpgradeEffectKey`, `enum DoctrineEffectKey`. Drift assuré entre Inspector et code.
- [ ] **RunContext / SettingsRegistry record class** — DTO C# 9 record vs class mutable, lock canonical pattern.

### Tests
- [ ] **Unity Test Framework smoke E2E tests** — instantiate Main.unity programmatically + assert 15 singletons non-null after 1 frame + 13 registries chargés. Aurait évité P0-1 scene drift. Files : new `Assets/Tests/SmokeTests.cs`. CI hook via `BuildScript.cs`.

---

## DONE (recent merges, last 30 — main)

- ✅ e42759f fix: defensive null checks + colored fallback primitives for missing GLTF assets
- ✅ 0d12b63 fix(build): GraphicsSettings via AssetDatabase + CameraController.cs.meta MonoImporter
- ✅ e7759e6 feat(render): install URP 17.3.0 + URPSetup Editor script + fix BuildScript GraphicsSettings API
- ✅ 9897c42 feat(editor): BuildMainSceneTool crée Castle GO (0,0,0) avec Castle.cs + Cube gris pierre
- ✅ a76ea57 feat(projectile): ProjectilePool fallback sphere + null guards on Get()
- ✅ 419614d feat(editor): EnsureDirectionalLight intensity 1.5 + warm color Sun
- ✅ 8cc8a4e feat(systems): EnemyPool.SpawnFromType debug logs + PathManager fallback path
- ✅ dc015b2 feat(hero): spawn at castle pos + WASD input verified + OnDrawGizmos red sphere debug
- ✅ fdf2c25 fix: ensure singletons are root GameObjects before DontDestroyOnLoad
- ✅ d8bd135 fix: include AssetRegistry GLTFs in WebGL build preloaded assets
- ✅ 90e0785 fix: ensure custom toon shaders included in WebGL builds
- ✅ b48ad16 feat(debug): Add 100×100 m debug ground plane to Main scene
- ✅ 047d431 fix: prevent cascading singleton auto-creation causing stack overflow
- ✅ dae7e48 fix(editor): disable ForceUpdate GLTF reimport on build to avoid GLTFast Jobs threading bug
- ✅ d5709e3 feat(systems): StatsTracker tracks run-scoped kills/waves/gold/towers + lifetime levels/stars
- ✅ c2122d6 feat(heroes): wire HeroType assetKeys hero_<id> + add 5 registry entries to AssetRegistry
- ✅ e83b66b feat(ui): pill badge countdown ticks each frame + localized "Lancer dans Xs pour +Y%"
- ✅ 40483e8 feat(editor): EnsureSkyboxAndLighting BuildMainSceneTool + couleurs ambientes par thème
- ✅ 9e24ac9 fix(data): TowerType assetKey tower_acid + tower_skyguard alignés registry
- ✅ 656ee28 fix(editor): AssetRegistryTool re-imports GLTF/GLB before building registry
- ✅ 4c865cb fix: add Achievements + ComboSystem singletons, seed Doctrine + Skin registries
- ✅ 4da18be feat(ui): tooltips + keyboard hints footer bar (N/ESC/Space/F3/M)
- ✅ 6afcbc0 feat(levels): W1-1 asset 5 waves + MenuItem Seed W1-1
- ✅ 9619e48 feat(editor): TestRunner MenuItem boot smoketest validates 15 singletons + 13 registries
- ✅ 83d2e5b perf: éliminer allocations .material par frame + activer GPU instancing map
- ✅ 7031eca feat(editor): GameSmoketest batch headless game loop verification
- ✅ 5bfe9c5 fix(systems): LevelRunner loads W1-1 fallback si no level specified at startup
- ✅ 13cfc4d fix: Move theme to Resources/ + add RuntimeThemeFixup for WebGL build stripping
- ✅ 2962ecd fix(ui): WaveManager race condition — lazy subscribe in Update if not ready at Start
- ✅ 8a067f5 fix(editor): EnsurePanelSettingsTheme force reimport + wire EnemyPool.basePrefab

---

## Notes scout

- **Bugs P0 historiques résolus** : wave launch button race (commit `2962ecd`), EnemyPool.enemyPrefab null (`8a067f5`), MonoSingleton cascading stack overflow (`047d431`), URP shaders stripped WebGL (`90e0785`), GLTF preload WebGL (`d8bd135`).
- **Données V5 maintenant peuplées** (v5-gap-audit était outdated 2026-05-11) : 11 Events, 8 Modifiers, 10 Cutscenes .asset présents `Assets/ScriptableObjects/Events/Modifiers/Cutscenes/`. Gap analysis prioritaire P0 datée morning → now caduque.
- **iso-V4 estimé courant** : ~50-65 % (35 % était audit pre-fix). Wave loop end-to-end pas re-validé Chrome MCP post `e42759f` → priorité absolue scout (P0 §QA smoke).
- **Phase 4 cible** : Mac/Win/Linux + iOS/Android builds (CI matrix). Pre-req perf 30 fps mobile = P1 hot path allocs Enemy + VfxPool MUST fix avant.
- **In-flight / dispatched (DO NOT DUPLICATE)** : parallel-scout-backlog mentionne wave preview UI, Synergy HUD badges, CoinPull anim polish, QA smoke live, perf audit live, texture gen ComfyUI Flux. Vérifier merge status si autres scouts en parallèle.
