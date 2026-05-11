# Phase 3 Visuals + Audio + Polish — Plan de migration

**Sprint** : MIGRATE Phase 3 (post-Phase 2 Core livré 2026-05-11, ~50 commits, 28 tickets MIGRATE-CORE-XX)
**Auteur** : Opus orchestrator
**Date** : 2026-05-11
**Estimé total** : 2-3 semaines, 15-25 tickets `MIGRATE-VISUAL-XX`
**Cible fin Phase 3** : POC visuel transformé en expérience proche d'un produit fini. GLTF models partout (towers/enemies/castle/decor), URP ToonMaterial cel-shading, outlines silhouette, animations Idle/Walk/Attack/Death basic, particles VFX (impact/death/aura), JuiceFX (shake/flash/slow-mo), audio sweep complet (SFX + music).

---

## 0. Préalable — état Phase 2 acquis et placeholder visuels actuels

| Élément | État Phase 2 |
|---|---|
| 12 Tower SO + 30 Enemy SO + 80 LevelData | ✅ |
| BalanceConfig SO singleton | ✅ |
| Multi-portal × multi-castle | ✅ |
| Tower behaviors (attack/cluster/slow/buffAura/coinPull) | ✅ |
| Enemy behaviors (flyer/stealth/shieldHP/boss/summons/blast) | ✅ |
| D1-01..04 (économie / pacing / L3 / castleHP) | ✅ |
| Synergies cross-tower (11 hardcoded) | ✅ |
| Save system PlayerPrefs + JSON | ✅ |
| Object pool enemies+projectiles | ✅ |
| Tower mesh visuel | ⚠️ `GameObject.CreatePrimitive(Cube)` + `Cylinder` bleu (POC) |
| Enemy mesh visuel | ⚠️ `Capsule` colorée par `bodyColor` SO field |
| Castle visuel | ⚠️ Cube gris + halo Light point |
| MapRenderer tiles | ⚠️ Cubes plats (grass green, path brown, water blue) |
| Materials | ⚠️ URP Lit standard (PBR, pas cel-shading) |
| Outline silhouette | ❌ inexistant |
| Animations | ❌ inexistant (towers rotation manuelle vers target, enemies translation lerp) |
| Particles | ❌ inexistant (Phaser avait pool 400 sprites radial) |
| Screen shake / hit flash / slow-mo | ❌ inexistant |
| Audio SFX | ❌ silencieux |
| Music | ❌ silencieux |

**Source code Phaser à porter (Phase 3)** :
- `/Users/mike/Work/milan project/src-v3/systems/AssetLoader.js` — 240+ assets GLTF mappés assetKey → path (`quaternius/`, `kaykit/`, `kenney/`, `polypizza/`). Source de vérité du mapping.
- `/Users/mike/Work/milan project/src-v3/systems/ToonMaterial.js` — 80 LOC. Wrapper `MeshToonMaterial` Three.js avec gradient map 3-step (shadow/mid/bright) + `applyToonToScene` qui re-mat tous les meshes traverse-récursif.
- `/Users/mike/Work/milan project/src-v3/systems/Audio.js` — 206 LOC. SFX pool via WebAudio API + ~20 SFX clés (`hero_shoot`, `tower_shoot`, `enemy_hit`, `enemy_die_basic/medium/boss`, `tower_built`, `tower_upgrade`, `wave_start`, `castle_hit`, `coin_pickup`, `boom`, `boss_charge`, `wave_clear`, `level_up`, `achievement`, etc.) + 2 procedural (sfxNoGold / sfxUlt via OscillatorNode).
- `/Users/mike/Work/milan project/src-v3/systems/Particles.js` — pool 400 sprites radial texture + 21 enemy particle colors mapping.
- `/Users/mike/Work/milan project/src-v3/systems/AnimationController.js` — 55 LOC. Wrapper `AnimationMixer` Three.js + `play(name, {fade, loop, timeScale})` cross-fade.
- `/Users/mike/Work/milan project/src-v3/systems/JuiceFX.js` — 52 LOC. Camera shake (Vec3 offset additif sur camera.position) + flash DOM overlay opacity animate.

**Assets disque source** : `/Users/mike/Work/milan project/public/assets/` (subdirs `quaternius/`, `kaykit/`, `kenney/`, `polypizza/`) + `/Users/mike/Work/milan project/public/audio/sfx/` (~20 `.ogg`) + `/Users/mike/Work/milan project/public/textures/vfx/` (PNG sprites VFX).

**Estimation taille** : ~50-100 MB assets bruts, ~30-50 MB après import Unity (compression textures + collider strip).

---

## Section 1 — Priorisation et ordering

5 sprints internes, ordering basé sur **dépendances pipeline asset** (import → shader → anim → VFX → audio) + minimisation des refactors croisés :

### Sprint 3.A — Assets import GLTF + AssetRegistry (3-5 j, ~5-7 tickets)

**Objectif** : copier les ~50 assets GLTF nécessaires Phase 3 depuis `milan project/public/assets/` vers `Assets/Models/` (organisé par catégorie : Towers/, Enemies/, Castles/, Decor/). Créer `AssetRegistry` SO mappant `assetKey: string → GameObject` (prefab GLTF). Refactor `Tower.Init` et `Enemy.Init` pour lire `cfg.assetKey` → spawn prefab depuis Registry au lieu de primitives.

**Pipeline import recommandé** :
1. Copier les `.glb` / `.gltf` sources depuis milan project vers `Assets/Models/<category>/`.
2. Unity importe auto à la détection (FBX/glTF Importer natif Unity 6 LTS).
3. Pour chaque asset importé : créer un Prefab variant dans `Assets/Prefabs/<category>/` (`Knight_Male.prefab` etc.), configurer pivot + scale + LOD basique si besoin.
4. `AssetRegistry.asset` SO singleton dans `Resources/AssetRegistry.asset` mappe les keys utilisés par les SO Phase 2 (`tower_archer`, `mob_skeleton`, `boss_volcan_dragon_v2`, etc.) → prefab.

**Critères de fin** :
- ~50 GLTF assets dans `Assets/Models/` (12 towers L1 + 3-4 variantes L2/L3 archer/mage/tank/ballista + ~25 enemies + 4-5 castles + 5-10 decor).
- `AssetRegistry.asset` peuplé avec tous les `assetKey` utilisés par les 12+30 SO instances.
- `Tower.Init` instancie un `Instantiate(registry.Get(cfg.AssetKey))` enfant du tower GO. Idem `Enemy.Init`.
- Fallback : si `assetKey` manquant ou null → keep primitive (régression-safe).
- W1-1 jouable avec mesh Knight/Zombie/Goblin Quaternius visibles à la place des capsules.

**Tickets** : `MIGRATE-VISUAL-01` (AssetRegistry + import 12 towers), `-02` (import 30 enemies), `-03` (import castles + decor de base), `-04` (refactor Tower/Enemy Init pour consommer Registry).

**Dépendances** : aucune (Phase 2 fournit déjà les SO).

### Sprint 3.B — Shaders + Materials URP (3-4 j, ~3-4 tickets)

**Objectif** : porter `ToonMaterial.js` (MeshToonMaterial Three.js) → URP Shader Graph `ToonShader`. Porter le pattern outline (inverted hull, scale 1.02, black material backface culling front) en `OutlineRenderer.cs` ou shader pass dédié. Créer `MaterialController.cs` qui applique au runtime les skin overlays (jellyfish/hologram pour shaderOverlay enemies type `kraken_boss`, `cosmic_boss`).

**Critères de fin** :
- `Assets/Shaders/ToonURP.shadergraph` : 3-step gradient ramp (shadow #888 / mid #ccc / bright #fff) configurable + base color + main texture slot.
- `Assets/Materials/ToonBase.mat` : material template instancié par variantes (per-tower / per-enemy via `MaterialPropertyBlock` pour éviter explosion materials).
- `Assets/Scripts/Visual/MaterialController.cs` : `ApplyToon(GameObject, Color tint)` qui swap tous les `Renderer.materials` du subtree vers ToonURP avec MaterialPropertyBlock pour couleur.
- `Assets/Scripts/Visual/OutlineRenderer.cs` : ajoute un mesh enfant scale 1.02 avec material black inverted hull (front cull = backface only visible).
- Couleur outline auto adapt fond clair/sombre (cf `cellShadingOutlineColor()` Phaser : luminance fond → noir/blanc). MVP : noir hardcoded, dynamic Phase 3.D.
- Skin overlays : 2 shaders dédiés `JellyfishShader.shadergraph` (kraken) + `HologramShader.shadergraph` (cosmic) avec scrolling UV/fresnel/noise. Applied conditionnellement quand `EnemyType.shaderOverlay != null`.

**Tickets** : `MIGRATE-VISUAL-05` (ToonURP Shader Graph + ToonBase material + apply Tower/Enemy), `-06` (OutlineRenderer + couleur adaptative), `-07` (Jellyfish + Hologram shaders pour boss spéciaux).

**Dépendances** : 3.A.04 (Tower/Enemy spawn GLTF mesh requis avant material swap).

### Sprint 3.C — Animations (2-3 j, ~3-4 tickets)

**Objectif** : porter `AnimationController.js` → `AnimationController.cs` wrapper Animator natif Unity. State machine basic Idle/Walk/Attack/Death pour enemies. Idle/Attack uniquement pour towers (towers fixées au sol, juste rotate vers target). Cross-fade entre states (équivalent `action.fadeIn(0.2)`).

**Critères de fin** :
- `Assets/Animations/Controllers/EnemyAnimator.controller` : state machine 4 states (Idle, Walk, Attack, Death) + Parameters bool/trigger (`isWalking`, `attackTrigger`, `dieTrigger`).
- `Assets/Animations/Controllers/TowerAnimator.controller` : 2 states (Idle, Attack) + trigger `fireTrigger`.
- `Assets/Scripts/Visual/AnimationController.cs` : MonoBehaviour wrapper qui :
  - lit le `Animator` enfant du mesh GLTF importé (clips auto-extracted par Unity à l'import GLTF avec animations embedded).
  - expose `Play(string state, float fade = 0.2f)` qui set bool/trigger param correspondant.
  - fallback : si Animator absent (asset GLTF sans clips) → skip silencieusement.
- `Enemy.cs` : appelle `_anim.Play("Walk")` au démarrage + `Play("Attack")` quand attack-castle + `Play("Death")` au TakeDamage fatal (avant Destroy avec délai 0.5s).
- `Tower.cs` : appelle `Play("Attack")` au Fire().
- Mapping anim names Phaser `walkAnim` field SO (ex: `"Walking_A"`, `"Run"`) → résolu via reflection sur clips du Animator (cherche clip dont name contient `walkAnim` substring).

**Tickets** : `MIGRATE-VISUAL-08` (AnimationController.cs + Enemy state machine), `-09` (Tower attack anim), `-10` (Death anim cleanup pool integration).

**Dépendances** : 3.A.04 (mesh GLTF importé avec clips Animator présent).

### Sprint 3.D — Particles + JuiceFX (2-3 j, ~3-4 tickets)

**Objectif** : porter `Particles.js` (pool 400 sprites radial) → Unity Particle System préfabriqué. 4-5 systems clés : `ImpactVFX` (hit projectile), `DeathVFX` (enemy die), `AuraVFX` (Magnet, Portal, BuffAura), `BossAuraVFX` (boss visual aura), `CoinPickupVFX`. Porter `JuiceFX.js` (camera shake + flash) → `JuiceFX.cs` qui applique offset additif sur Camera.transform.position via LateUpdate + UI overlay flash via UI Toolkit transient element.

**Critères de fin** :
- `Assets/Prefabs/VFX/Impact.prefab` (ParticleSystem ~10-15 particles, burst 1, lifetime 0.3s, color Color enemy type).
- `Assets/Prefabs/VFX/Death.prefab` (burst 20 particles, lifetime 0.5s).
- `Assets/Prefabs/VFX/Aura.prefab` (continuous emission rate 10/s, lifetime 1s, circular emitter shape autour tour).
- `Assets/Prefabs/VFX/BossAura.prefab` (variante Aura, larger scale + glow color = `bossAuraColor` SO field).
- `Assets/Scripts/Visual/VfxPool.cs` : pool préalloué 50 instances chaque type, recycle via `gameObject.SetActive(false)`. Évite Instantiate hot path.
- `Assets/Scripts/Visual/JuiceFX.cs` :
  - `Shake(intensity, durationMs)` : ajoute entry à liste shakes, applique offset random additif sur `Camera.main.transform.position` chaque LateUpdate.
  - `Flash(Color color, durationMs)` : crée transient `VisualElement` plein écran dans le HUD root, opacity 0 → 0.45 → 0 sur durée.
  - `SlowMo(timeScale, durationMs)` : set `Time.timeScale = timeScale` puis restore après délai. Pour critical kills boss.
- Tower.Fire() → spawn ImpactVFX au point d'impact + `JuiceFX.Shake(0.05, 100)` léger.
- Enemy.Die() → spawn DeathVFX position + couleur de `ENEMY_PARTICLE_COLORS` lookup.
- Boss death → `JuiceFX.SlowMo(0.3f, 800)` + `Flash(white, 250)` + DeathVFX scale ×2.

**Tickets** : `MIGRATE-VISUAL-11` (VfxPool + Impact/Death prefabs), `-12` (Aura + BossAura), `-13` (JuiceFX shake/flash/slow-mo).

**Dépendances** : 3.B (couleurs material lookup pour particle color match) + 3.C (Death anim trigger avant VFX spawn).

### Sprint 3.E — Audio sweep (2-3 j, ~3-4 tickets)

**Objectif** : importer ~20 SFX `.ogg` Phaser → `Assets/Audio/SFX/`. Porter `Audio.js` (WebAudio pool) → `AudioController.cs` (AudioSource pool + AudioMixer). Setup Audio Mixer Unity avec 4 groupes (Master, SFX, Music, UI) + Snapshots (gameplay / paused / gameOver pour ducking). Music : 1 track ambient loop Phase 3 MVP (1 fichier ~3-5 min loop seamless). Mute toggle UI + volume sliders settings.

**Critères de fin** :
- `Assets/Audio/SFX/*.ogg` : ~20 fichiers (`tower_shoot.ogg`, `enemy_hit.ogg`, `enemy_die_basic.ogg`, `enemy_die_medium.ogg`, `enemy_die_boss.ogg`, `tower_built.ogg`, `tower_upgrade.ogg`, `wave_start.ogg`, `castle_hit.ogg`, `coin_pickup.ogg`, `boom.ogg`, `boss_charge.ogg`, `wave_clear.ogg`, `level_up.ogg`, `achievement.ogg`, etc.).
- `Assets/Audio/Music/Ambient_W1.ogg` : 1 track loop pour MVP (recherche track CC0 Phase 3 si Phaser n'en a pas, ou silence music acceptable).
- `Assets/Audio/MixerGroups.mixer` : 4 groupes (Master, SFX, Music, UI) avec volumes par défaut + exposed parameters.
- `Assets/ScriptableObjects/Audio/AudioClipRegistry.asset` : SO mapping `clipKey: string → AudioClip` (équivalent SFX_FILES Phaser).
- `Assets/Scripts/Visual/AudioController.cs` : singleton MonoBehaviour
  - `Play(string clipKey, float volMul = 1f)` : récupère AudioClip via Registry, joue sur AudioSource pool (~8 sources préallouées round-robin) routed via SFX MixerGroup.
  - `PlayMusic(AudioClip)` : crossfade music via 2nd AudioSource loop.
  - `SetMasterVolume(float)`, `SetMuted(bool)` : binding settings UI.
  - Min replay interval 28ms (cf Phaser `MIN_REPLAY_INTERVAL_MS`) pour éviter audio crackle quand 20+ ennemis meurent en 100ms.
- Integration hooks : Tower.Fire → `Play("tower_shoot", 0.55)` ; Enemy.TakeDamage → `Play("enemy_hit", 0.4)` ; Enemy.Die → `Play("enemy_die_basic|medium|boss")` selon tier ; WaveManager.StartWave → `Play("wave_start")` ; Castle.TakeDamage → `Play("castle_hit")` ; Tower.OnPlaced → `Play("tower_built")` ; Tower.UpgradeTo → `Play("tower_upgrade")`.
- Procedural SFX `sfxNoGold` et `sfxUlt` (cf Audio.js:154+) : skip Phase 3 MVP, on use SFX `.ogg` simple "denied" + "boom" classiques. Si Mike veut procedural plus tard, ticket dédié.

**Tickets** : `MIGRATE-VISUAL-14` (import SFX + AudioController + AudioMixer), `-15` (integration hooks Tower/Enemy/Wave/Castle), `-16` (music loop + settings panel UI).

**Dépendances** : aucune (parallélisable 3.A-D, mais à intégrer en fin pour éviter de bricoler hooks Tower.cs N fois).

---

## Section 2 — Premiers tickets MIGRATE-VISUAL-01..06 (briefables Sonnet)

### MIGRATE-VISUAL-01 — AssetRegistry SO + import 12 tower GLTF

**Goal** : copier les GLTF tower assets Phaser → `Assets/Models/Towers/`, créer `AssetRegistry` SO singleton, peupler avec les ~12 entrées tower.

**Bloqué par** : aucun (Phase 2 fournit les SO TowerType).

**Source Phaser à porter** : `/Users/mike/Work/milan project/src-v3/systems/AssetLoader.js` lignes 16-32 (manifest entries `tower_archer`, `tower_archer_l2/l3`, `tower_mage`, `tower_mage_l2`, `tower_tank`, `tower_tank_l2/l3`, `tower_ballista`, `tower_ballista_l2/l3`, `tower_cannon`, `tower_crossbow`, `tower_fan`, `tower_mine`, `tower_magnet`, `tower_portal`, `tower_frost`) + lignes 76-83 (Batch 2 RTS towers).

**Fichiers Unity à créer/modifier** :
- `Assets/Models/Towers/` : copier ~20 fichiers `.glb` / `.gltf` depuis `/Users/mike/Work/milan project/public/assets/quaternius/Tower_*.gltf` + `/Users/mike/Work/milan project/public/assets/towers/*.glb`.
- `Assets/Prefabs/Towers/` : 12 prefab `<TowerKey>.prefab` (Tank.prefab, Archer.prefab, etc.) construits depuis chaque GLTF importé : empty parent + GLTF model child + collider stripped + scale ajustée si besoin.
- `Assets/Scripts/Data/AssetRegistry.cs` : nouveau SO singleton.
  ```csharp
  [CreateAssetMenu(menuName = "CrowdDefense/AssetRegistry")]
  public class AssetRegistry : ScriptableObject {
    [System.Serializable]
    public struct Entry { public string Key; public GameObject Prefab; }
    [SerializeField] private Entry[] entries;
    private Dictionary<string, GameObject> _cache;
    public GameObject Get(string key) { /* lazy build dict, return prefab or null */ }
  }
  ```
- `Assets/ScriptableObjects/Registry/AssetRegistry.asset` : instance peuplée des 12+ entries.
- `Assets/Scripts/Data/TowerType.cs` : champ `[SerializeField] private string assetKey` (si pas déjà ajouté Phase 2).

**Choix techniques Unity** :
- Format préféré : `.glb` (binary, single file) over `.gltf` (multi-file). Unity 6 LTS importe les deux nativement via package `com.unity.cloud.gltfast` ou `glTFast` (à vérifier installé, sinon ajouter au Packages/manifest.json).
- `AssetRegistry` placé dans `Resources/` pour `Resources.Load<AssetRegistry>("AssetRegistry")` singleton bootstrap. **OU** referenced dans `BalanceConfig.asset` champ `[SerializeField] AssetRegistry registry` (préfèré, evite Resources/ pattern).
- Import settings GLTF : `Read/Write Enabled = false`, `Optimize Mesh = true`, `Generate Colliders = false`, `Animation Compression = Optimal`.
- Pour les towers sans GLTF natif (`tower_magnet` est `magnet_polygoogle.glb` Poly Pizza, vérifier license) : copy as-is.

**Commits prévus** :
1. `chore(assets): copy 20 tower GLTF assets from milan project to Assets/Models/Towers`
2. `feat(data): AssetRegistry SO singleton avec dict assetKey → Prefab`
3. `chore(data): peupler AssetRegistry avec 12 tower entries + variantes L2/L3 où dispo`

**Verification** :
- UnityMCP `list_assets Assets/Models/Towers` → ~20 `.glb`.
- UnityMCP `list_assets Assets/Prefabs/Towers` → 12 `.prefab`.
- Inspector ouvrir `AssetRegistry.asset` → 12+ entries visibles avec Prefab refs non-null.
- Régression-safe : POC W1-1 Archer toujours fonctionnel (refactor consume Phase 3.A.04, pas ce ticket).

**Estimation** : 3 commits, 4-5 h Sonnet (essentiellement copy + Inspector population, peu de code).

---

### MIGRATE-VISUAL-02 — Import ~25-30 enemy GLTF + populate AssetRegistry

**Goal** : importer les enemies GLTF (Quaternius Knight/Zombie/Goblin/Wizard/Pirate + monsters Big/Flying/Blob + kaykit skeleton + cyberpunk Quaternius + boss assets). Étendre `AssetRegistry` avec ~30 entries enemy.

**Bloqué par** : `MIGRATE-VISUAL-01` (AssetRegistry doit exister).

**Source Phaser à porter** : `/Users/mike/Work/milan project/src-v3/systems/AssetLoader.js` :
- lignes 9-15 (humanoid : knight, zombie, goblin, soldier, knightgolden, wizard, pirate)
- lignes 84-86 (mob_skeleton kaykit)
- lignes 108-133 (Batch 4 monsters Quaternius : Dragon, MushroomKing, Ghost, Demon, Yeti, Cactoro, Orc, Ninja, Tribal, Dino, Bunny, Frog, Birb, Blobs, Armabee, Alpaking, Pigeon, Squidle)
- lignes 145-149 (cyberpunk mobs)
- lignes 166 (mob_espace_astronaut)
- Boss assets lignes 64-70 + 108-117.

**Fichiers Unity à créer/modifier** :
- `Assets/Models/Enemies/` : copy ~30 fichiers `.glb` / `.gltf`.
- `Assets/Prefabs/Enemies/` : ~30 prefabs `<EnemyKey>.prefab`.
- `Assets/ScriptableObjects/Registry/AssetRegistry.asset` : ajouter ~30 entries.
- `Assets/Scripts/Data/EnemyType.cs` : confirmer champ `assetKey` (ajouté Phase 2 MIGRATE-CORE-02).

**Choix techniques** :
- Boss assets souvent plus gros (Dragon 5 MB, MushroomKing 3 MB) → vérifier total Assets/Models budget < 50 MB pour ne pas exploser build WebGL.
- Skinned mesh : Unity importe les bones + clips Animator embedded auto. Phase 3.C consommera ces clips.
- `boss_submarin_kraken` et `boss_espace_entite` (cosmic) Phaser : rendus via shaders (jellyfish + starfield), pas de GLTF. Phase 3.B port shaders, ce ticket skip ces 2 et utilise `mob_squidle` (alt kraken) + `boss_espace_ghost` comme proxy GLTF. À l'usage, MaterialController appliquera l'override shader.

**Commits prévus** :
1. `chore(assets): copy 30 enemy GLTF assets to Assets/Models/Enemies`
2. `chore(data): peupler AssetRegistry avec 30 enemy entries`
3. `chore(assets): proxy GLTF pour kraken_boss + cosmic_boss (shaders Phase 3.B)`

**Verification** :
- `list_assets Assets/Prefabs/Enemies` → ~30 prefab.
- Inspector boss_dragon prefab : Animator component présent (auto par GLTF importer).
- Build WebGL size check : avant Phase 3 = ~6 MB, target post Phase 3.A = < 30 MB compressed Brotli.

**Estimation** : 3 commits, 4-6 h Sonnet.

---

### MIGRATE-VISUAL-03 — Refactor Tower.Init + Enemy.Init pour spawn GLTF via Registry

**Goal** : modifier `Tower.Init(cfg)` et `Enemy.Init(cfg, ...)` pour instancier le prefab GLTF depuis `AssetRegistry.Get(cfg.AssetKey)` comme child du GameObject, et désactiver/supprimer la primitive placeholder. Fallback si AssetKey null → keep primitive.

**Bloqué par** : `MIGRATE-VISUAL-01` + `MIGRATE-VISUAL-02` (Registry peuplé).

**Source Phaser** : `/Users/mike/Work/milan project/src-v3/entities/Tower.js` méthode `_buildMesh()` (charge GLTF via `AssetLoader.get(cfg.asset)`, fallback primitive) + `/Users/mike/Work/milan project/src-v3/entities/Enemy.js` méthode équivalente.

**Fichiers Unity à modifier** :
- `Assets/Scripts/Entities/Tower.cs` :
  - Dans `Init(cfg)` : après le `GetComponent<MeshRenderer>().material` actuel placeholder, check `cfg.AssetKey != null` → `Instantiate(AssetRegistry.Instance.Get(cfg.AssetKey), this.transform)` + destroy le primitive Cube/Cylinder child placeholder. Sinon keep primitive (fallback).
  - Refactor : extract le placeholder en méthode `BuildPlaceholderMesh()` ; appelé uniquement si fallback.
- `Assets/Scripts/Entities/Enemy.cs` : idem pattern.
- `Assets/Scripts/Data/AssetRegistry.cs` : ajouter static `Instance` resolved via `BalanceConfig.Instance.Registry`.

**Choix techniques** :
- Le GLTF Instantiate enfant : reset `localPosition = Vector3.zero`, `localRotation = Quaternion.identity`, `localScale = Vector3.one * cfg.Scale` (champ SO).
- Cleanup placeholder child : `Destroy(transform.Find("PlaceholderMesh").gameObject)`.
- Pool integration : Phase 2 a `EnemyPool.Get()` qui Instantiate prefab Enemy. Le GLTF child est partie du prefab Enemy mais paramétrable. Recommandation : Enemy prefab reste générique (avec script) + GLTF child instancié à `Init` au premier usage du pool, cached sur l'instance pool (pas re-instancié à chaque recycle si même cfg). Si cfg change au recycle → swap child.
- Performance : 30 prefab types × pool 200 = jusqu'à 6000 GLTF instances théoriques. En pratique, pool partagé toutes EnemyType → ~200 instances max simultanées. OK.

**Commits prévus** :
1. `refactor(entities): Tower.Init spawn GLTF mesh from AssetRegistry avec fallback primitive`
2. `refactor(entities): Enemy.Init idem + cleanup placeholder`
3. `feat(entities): pool-aware GLTF swap si EnemyType change au recycle`

**Verification** :
- Play mode W1-1 : Archer = mesh archer Knight Quaternius visible (pas le cube bleu). Basic enemy = mesh Knight, Runner = mesh Pirate, Brute = mesh Demon (per `EnemyType.AssetKey` SO field).
- Console clean (no missing prefab warnings).
- Si `AssetRegistry.Get("foo_inexistant")` → log warning Editor only, fallback primitive sans crash.

**Estimation** : 3 commits, 4-5 h Sonnet.

---

### MIGRATE-VISUAL-04 — ToonURP Shader Graph + ToonBase material + apply Tower/Enemy

**Goal** : créer un shader URP via Shader Graph reproduisant le rendu cel-shading de `ToonMaterial.js` Three.js : 3-step gradient ramp (shadow/mid/bright) + base color + main texture optional. Applied à tous les meshes Tower/Enemy via `MaterialController.cs` qui swap `Renderer.materials` du subtree GLTF importé.

**Bloqué par** : `MIGRATE-VISUAL-03` (mesh GLTF importés).

**Source Phaser** : `/Users/mike/Work/milan project/src-v3/systems/ToonMaterial.js` (80 LOC, lu pour ce ticket).
- `makeGradientTexture()` : canvas 4×1 px, 3 stops (`#888888` shadow, `#cccccc` mid, `#ffffff` bright).
- `makeToonMaterial({color, map, transparent, side})` : `THREE.MeshToonMaterial` avec gradientMap.
- `applyToonToScene(root, opts)` : traverse récursif tous Mesh/SkinnedMesh, remplace `node.material` par ToonMaterial conservant color + map originaux.

**Fichiers Unity à créer** :
- `Assets/Shaders/ToonURP.shadergraph` : Shader Graph URP avec :
  - Input : BaseColor (Color), MainTex (Texture2D 2D), ShadowColor (Color default #888), MidColor (Color default #ccc), BrightColor (Color default #fff), RampSmooth (Float 0-1).
  - Logic : dot(NormalWS, MainLightDirectionWS) → remap to 0-1 → step into 3 bands (lt 0.33 = shadow, lt 0.66 = mid, else bright). Multiply by BaseColor × MainTex.Sample.
  - Optional rim light : fresnel * BrightColor faible intensité pour lisibilité silhouette.
- `Assets/Materials/ToonBase.mat` : material instance utilisant ToonURP shader avec defaults.
- `Assets/Scripts/Visual/MaterialController.cs` :
  ```csharp
  public static class MaterialController {
    private static Material _toonBase;
    public static void ApplyToon(GameObject root, Color tint) {
      if (_toonBase == null) _toonBase = Resources.Load<Material>("ToonBase");
      foreach (var r in root.GetComponentsInChildren<Renderer>()) {
        var mats = new Material[r.sharedMaterials.Length];
        for (int i = 0; i < mats.Length; i++) {
          mats[i] = new Material(_toonBase);
          // Conserve original texture + tint
          if (r.sharedMaterials[i]?.mainTexture != null)
            mats[i].mainTexture = r.sharedMaterials[i].mainTexture;
          mats[i].SetColor("_BaseColor", tint);
        }
        r.materials = mats;
      }
    }
  }
  ```
- Hook : `Tower.Init` et `Enemy.Init` appellent `MaterialController.ApplyToon(meshChild, cfg.BodyColor)` après GLTF Instantiate.

**Choix techniques** :
- Shader Graph plutôt que HLSL custom : Mike skill Unity zéro → SG = visual debug Inspector facile. HLSL impose maintenance text-only. Coût : SG plus lourd au compile mais 1-time.
- `MaterialPropertyBlock` (optim sans new Material per renderer) considéré, mais ToonURP a plusieurs SetColor en plus de _BaseColor (shadow/mid/bright pourraient être globaux). Phase 3 MVP : new Material acceptable (12 towers + 30 enemies = 42 unique mats max, négligeable).
- Pas de toon outline ici (Phase 3.B MIGRATE-VISUAL-06 dédié).

**Commits prévus** :
1. `feat(visual): ToonURP Shader Graph + ToonBase material 3-step gradient`
2. `feat(visual): MaterialController.ApplyToon swap subtree renderers`
3. `refactor(entities): Tower/Enemy Init apply ToonMaterial après GLTF spawn`

**Verification** :
- Play mode W1-1 : rendering cel-shading visible sur enemies + towers (bandes shadow/mid/bright nettes, pas de gradient PBR doux).
- Build WebGL : shader compile OK (vérifier `read_console` éditor + browser console post-build).
- Régression : `BodyColor` SO field correctement appliqué (Basic = grey, Runner = cyan, Brute = red bandes différentes).

**Estimation** : 3 commits, 5-7 h Sonnet (Shader Graph apprentissage + tuning).

---

### MIGRATE-VISUAL-05 — OutlineRenderer (inverted hull silhouette) + couleur adaptative

**Goal** : ajouter une silhouette noire (ou blanche selon luminance fond) autour de chaque tour et enemy via la technique "inverted hull" : mesh enfant scale 1.02, material black, front face culling (seul le backface visible, qui dépasse du mesh principal d'où l'effet outline).

**Bloqué par** : `MIGRATE-VISUAL-03` (GLTF mesh spawned).

**Source Phaser** : `/Users/mike/Work/milan project/src-v3/systems/ToonMaterial.js` lignes 61-79 (`bgLuminance`, `cellShadingOutlineColor`) + outline pattern probablement dans `Tower.js` ou `Enemy.js` (cherche `outline`).

**Fichiers Unity à créer** :
- `Assets/Shaders/Outline.shadergraph` : shader simple unlit, takes Color input, FrontFaceCulling = ON (Render face = Back), vertex displacement along normal × _OutlineWidth (default 0.02).
- `Assets/Materials/OutlineBlack.mat` + `OutlineWhite.mat` : 2 instances avec couleur respective.
- `Assets/Scripts/Visual/OutlineRenderer.cs` : MonoBehaviour
  - `Setup(GameObject meshRoot, Color outlineColor)` : foreach Renderer descendant, duplique le MeshFilter+MeshRenderer en GameObject enfant, scale 1.02, material = OutlineBlack ou White.
  - Update : ajuste outline color si fond change (singleton `BackgroundLuminance.Current` lu chaque LateUpdate, switch material si luminance flip seuil 100).
- Hook : `Tower.Init` et `Enemy.Init` appellent `OutlineRenderer.Setup(meshChild, BalanceConfig.OutlineColor)` après ApplyToon.

**Choix techniques** :
- Inverted hull plus simple que post-process screen-space outline URP (Sobel / depth-normal edge). Trade-off : pas d'outline sur edges intérieurs (creases), juste silhouette extérieure. Acceptable pour ce style cel-shading.
- Outline width : 0.02 unit en world space. Sur Knight ~2 unit tall, ratio 1%, soit ~1-2 px à la caméra Phase 2 distance.
- Couleur adaptative : `BackgroundLuminance.Current` mis à jour par MapRenderer en début de level selon biome (plaine = vert clair → outline noir, volcan = sombre → outline blanc). Phase 3 MVP : hardcoded noir partout, dynamic plus tard.

**Commits prévus** :
1. `feat(visual): Outline shader inverted hull URP`
2. `feat(visual): OutlineRenderer.Setup duplicate meshes scale 1.02`
3. `refactor(entities): Tower/Enemy Init add outline post-ApplyToon`

**Verification** :
- Play mode : enemies + towers ont liseré noir 1-2 px visible.
- Perf check : doublement draw calls. Sur 200 enemies + 20 towers = 440 draw calls outline added. Acceptable < 1000 batches limite mobile.
- Si > 60 fps drop : downgrade outline à `Camera.gameObject.AddComponent<URP renderer feature>` post-process screen-space (Phase 3 polish ticket dédié).

**Estimation** : 3 commits, 4-5 h Sonnet.

---

### MIGRATE-VISUAL-06 — AnimationController + Enemy Animator state machine (Idle + Walk)

**Goal** : porter `AnimationController.js` → `AnimationController.cs` wrapper Animator Unity natif. Configure Enemy Animator Controller avec 2 states minimum (Idle + Walk) + transitions via bool `isWalking`. Tower idle anim (rotate vers target conservé) — pas d'Animator pour towers ce ticket (MIGRATE-VISUAL-09 séparé).

**Bloqué par** : `MIGRATE-VISUAL-03` (mesh GLTF avec clips Animator embedded importé).

**Source Phaser** : `/Users/mike/Work/milan project/src-v3/systems/AnimationController.js` (55 LOC). Méthodes `play(name, {fade, loop, timeScale})`, `has(name)`, `stop()`, `tick(dt)`.

**Fichiers Unity à créer/modifier** :
- `Assets/Animations/Controllers/EnemyAnimator.controller` : 2 states (Idle default, Walk). Parameter `isWalking` (Bool). Transition Idle→Walk si `isWalking == true`, Walk→Idle si `false`. Exit time 0, transition duration 0.2s (équivalent fade).
- Mapping clip names : à l'import GLTF Quaternius, les clips sont nommés `"Idle"`, `"Walking_A"`, `"Running_A"`, `"1H_Melee_Attack_Chop"`, etc. (cf Mixamo-like). L'EnemyAnimator binding aux clips se fait par drag dans le State Inspector (manuel pour 1er asset, scriptable Editor tool pour les 30 enemies).
- Editor tool `Assets/Editor/AnimatorAssignTool.cs` : pour chaque prefab enemy, lit ses clips Animator → match les noms attendus (`*Idle*`, `*Walk*` ou `*Run*`) → assign aux states correspondants. Permet d'éviter manual assign × 30.
- `Assets/Scripts/Visual/AnimationController.cs` : wrapper sur Animator
  ```csharp
  public class AnimationController : MonoBehaviour {
    private Animator _anim;
    void Awake() { _anim = GetComponentInChildren<Animator>(); }
    public void PlayWalk(bool walking) { _anim?.SetBool("isWalking", walking); }
    public void TriggerAttack() { _anim?.SetTrigger("attackTrigger"); }
    public void TriggerDeath() { _anim?.SetTrigger("dieTrigger"); }
    public bool HasState(string state) { /* lookup via _anim.HasState hash */ }
  }
  ```
- `Assets/Scripts/Entities/Enemy.cs` :
  - Field `private AnimationController _animController;` cached Awake.
  - Update : si vitesse > 0.01 → `_animController?.PlayWalk(true)` ; sinon false. (Hot path : utiliser flag dirty pour éviter SetBool chaque frame, SetBool est cheap mais évitable.)

**Choix techniques** :
- Unity Mechanim state machine plutôt que coroutine timing-based : Mechanim natif + blend tree visualisable Inspector, scale mieux pour Phase 3.C extensions (Attack, Death).
- Pas d'Animator sur towers ce ticket : towers fixées au sol, rotation manuelle vers target déjà OK Phase 2. Animator tower MIGRATE-VISUAL-09 dédié.
- Clip name matching naïf (substring `Walk` ou `Run`) : suffisant pour Quaternius Ultimate Monsters conventions cohérentes. Si fail : fallback Animator state vide → log warning Editor.

**Commits prévus** :
1. `feat(visual): AnimationController.cs wrapper Animator Unity`
2. `feat(visual): EnemyAnimator.controller (Idle/Walk states + isWalking param)`
3. `feat(editor): AnimatorAssignTool batch-assign clips depuis GLTF imported`
4. `refactor(entities): Enemy.cs hook AnimationController.PlayWalk selon vitesse`

**Verification** :
- Play mode W1-1 : enemies Knight Walk anim visible (jambes bougent), Idle anim quand arrêtés (stealth phase Assassin par ex).
- Animator window Inspector : transition Idle→Walk se déclenche au play.
- Pas de NullRef si enemy GLTF sans Animator (legacy fallback primitive cube).

**Estimation** : 4 commits, 6-8 h Sonnet.

---

## Section 3 — Questions ouvertes à Mike (5)

### 3.1 Animation system : Mechanim state machine ou simple coroutines play-by-name ?

**Option A — Mechanim full** : Animator Controller Unity natif, state machine visuelle Inspector, blend trees, transitions paramétrables (bool/trigger/float).
- Pro : pattern Unity-canonique, scale Phase 3 (Attack/Death) + Phase 4 (combos, blend run/walk speed-based). Permet à futur level-designer non-coder d'éditer transitions.
- Con : courbe d'apprentissage Mike, lourdeur initial (30 controllers × 4 states = manual setup ou Editor tool obligatoire).

**Option B — Simple wrapper coroutines** : `AnimationController.cs` lit clips bruts via `Animation.legacy` ou `Animator.Play(stateHash)` direct, sans Controller graph. Cross-fade via `Animator.CrossFade(hash, 0.2f)`.
- Pro : 1 ligne par anim, pas de manual Controller setup. Plus proche du port Phaser.
- Con : pas de blend tree, pas de constraints transitions, code-only. Plus dur à debug visuellement.

**Recommandation Opus** : **Option A Mechanim** avec Editor tool batch-assign (cf MIGRATE-VISUAL-06). Pattern canonique, paie l'investissement dès Phase 4 polish.

**Question Mike** : OK Mechanim avec batch Editor tool, ou tu préfères Option B coroutines simples ?

### 3.2 Particles : Unity Particle System natif (Shuriken) ou Visual Effect Graph (VFX Graph URP) ?

**Option A — Shuriken** (`ParticleSystem` component) : système legacy Unity, CPU-based, configurable Inspector via modules (Emission, Shape, Color over Lifetime, etc.).
- Pro : universel, marche WebGL + mobile sans setup. Doc/tutos abondants. Suffit largement pour 5 systems Phase 3.
- Con : CPU-bound, limite ~1000 particles simultanées avant drop fps mobile.

**Option B — VFX Graph** (URP package `com.unity.visualeffectgraph`) : GPU-based, node graph visuel, scale 100k+ particles.
- Pro : perf insane, effets boss apocalyptiques possibles.
- Con : pas supporté WebGL 2.0 (Compute Shaders requis) → blocker pour notre cible. Surcoût apprentissage. Overkill Phase 3.

**Recommandation Opus** : **Option A Shuriken**. WebGL = priorité shipping, VFX Graph hors-table tant qu'on n'est pas WebGPU. Si Phase 4 ajoute WebGPU build → reconsidérer pour boss VFX.

**Question Mike** : OK Shuriken confirmé ?

### 3.3 Music : tracks variées par world (8 fichiers, ~30 MB) ou loop ambient unique Phase 3 ?

Source Phaser : pas de `Music.js` (cf scan `src-v3/systems/`). Audio.js gère SFX uniquement. Donc Phaser n'avait pas de music.
- Option A — **Music skip Phase 3** : aucun fichier music, ambiance SFX only. Économise 8 fichiers × 3-5 MB = ~25 MB build size + ~6h research/sélection tracks CC0.
- Option B — **1 track ambient loop** (~3-5 MB) : présent partout, monotone mais "fill" le silence.
- Option C — **8 tracks per world** (~25 MB) : variété, mais charge build + recherche tracks CC0 (freesound, OpenGameArt) ou commission ($).

**Recommandation Opus** : **Option B Phase 3** + reroll Option C Phase 4 polish si Mike veut investir budget musique. MVP Phase 3 = 1 track instrumental medieval ambient CC0 (recherche freesound.org "medieval ambient loop" 5-10 min sélection).

**Question Mike** : OK 1 track ambient loop unique Phase 3 ? Ou tu veux skip music complet Phase 3 et déléguer Phase 4 ?

### 3.4 Audio Mixer routing complexity : 4 groupes (Master/SFX/Music/UI) ou 2 groupes simples (Master/Music) ?

**Option A — 4 groupes** : Master → SFX, Music, UI. Ducking gameplay (boss roar duck music -6dB pendant 2s). Volumes indépendants settings.
- Pro : pattern pro, settings UI clean.
- Con : 4 AudioMixerGroup + Snapshots à setup. ~2h.

**Option B — 2 groupes simples** : Master → SFX+UI mixed, Music séparé. Pas de ducking.
- Pro : 30 min setup.
- Con : settings UI moins fin (pas de slider UI volume séparé du SFX).

**Recommandation Opus** : **Option A 4 groupes** dès Phase 3. La complexité est dans l'asset Mixer one-time, le code consume route via `AudioMixer.SetFloat("SFXVol", db)`. Setup propre paie en Phase 4 packaging Steam (settings menu standard).

**Question Mike** : OK 4 groupes Mixer ?

### 3.5 Shader Graph vs HLSL custom pour ToonMaterial ?

**Option A — Shader Graph URP** : noeuds visuels Unity natif, Inspector preview live.
- Pro : Mike skill zéro, debug visuel facile. Maintenance facile (drag noeud Color → tweaking).
- Con : un peu plus lourd à compile, génère HLSL verbeux non-readable.

**Option B — HLSL custom** : fichier `.shader` texte, syntax Cg/HLSL Unity.
- Pro : leaner, contrôle total, fits version control diff propre.
- Con : impose Mike apprentissage HLSL syntax. Erreurs cryptic en compile.

**Recommandation Opus** : **Option A Shader Graph**. Aligné Mike skill zéro Unity, fits pattern Phase 3 (Mike doit pouvoir tweaker rendu visuel sans Sonnet). Si perf critical observée Phase 4 → port HLSL ticket dédié.

**Question Mike** : OK Shader Graph confirmé ?

---

## Section 4 — Risks raised

### R1 — Perf WebGL avec 100+ GLTF instances + outlines + toon shader (HIGH)

État Phase 2 : ~6 MB WebGL, 60 fps desktop stable, 30 fps mobile estimé.
Phase 3 ajoute :
- ~50 GLTF instances simultanées peak (50 enemies + 20 towers + decor) × draw call mesh + outline mesh = ~140 draw calls par frame.
- Toon shader fragment cost (3 step ramp + fresnel) ~1.5× standard Lit.
- Particles : 5 systems actifs × ~30 particles/system = 150 particles peak.

Risque drop sub-30 fps mobile sur W6+ (Phase 2 swarm 100+ enemies × all-stuff).

**Mitigation** :
- Static batching map decor (Phase 3 dedicated ticket).
- GPU instancing material toon (enable `Enable GPU Instancing` checkbox sur ToonBase.mat).
- Pool VFX (cf MIGRATE-VISUAL-11) preallocated.
- Si fail QA mobile : tickets perf-tuning Sprint 3.F (LOD groups, shader simplifications mobile preset).
- Build size watch : target < 30 MB compressed Brotli post Phase 3.A. Au-delà → IL2CPP strip + asset compression aggressive.

### R2 — Shader Graph URP cross-platform edge cases (MEDIUM)

Shader Graph compile bytecode différent par target (WebGL 2.0 vs Metal iOS vs Vulkan Android). Quelques noeuds (Compute, Tessellation) WebGL-incompatibles.
**Mitigation** : tester chaque shader sur build WebGL early. Stick aux noeuds standards (Sample Texture 2D, Multiply, Dot Product, Smoothstep). Pas de Custom Function HLSL embarquée dans SG.

### R3 — Audio latency mobile (MEDIUM)

iOS WebGL audio latency notoirement haute (200-500ms via WebAudio Safari). Pas spécifique Unity mais affecte UX SFX réactif (`tower_shoot` click feel).
**Mitigation** : preload tous AudioClips à level start (`AudioController.WarmUp()`). Compress SFX en `.ogg` short (<1s), force `Load In Background = false` + `Preload Audio Data = true`.

### R4 — Animation clip name mismatch GLTF Quaternius (MEDIUM)

Quaternius clips nommés `Walking_A`, `Running_A`, `1H_Melee_Attack_Chop`, `Death_A`. Matching naïf substring (`Walk`, `Run`, `Attack`, `Die`/`Death`) couvre 90% mais edge cases possibles (e.g. Wizard fait `Idle_C` au lieu de `Idle`).
**Mitigation** : Editor tool `AnimatorAssignTool` log warnings list assets sans match → manual fix Inspector. Pas de blocker bloquer ticket sur 30/30, accepter 80% auto + 20% manual.

### R5 — Build size WebGL explosion (HIGH)

Phase 1 = 6 MB. Phase 3 cible < 30 MB.
- ~50 GLTF × ~500 KB avg = 25 MB.
- ~20 SFX × 50 KB = 1 MB.
- 1 music 3-5 MB.
- Textures import si pas optimisées : 5-10 MB.

**Mitigation** : 
- Compressed Mesh Format au max (`Mesh Compression = High` import settings).
- Audio compression : Vorbis 96 kbps (suffisant SFX), 128 kbps music.
- Texture Compression : `ASTC 6×6` mobile, `DXT5` WebGL desktop. Format `Crunch Compression` ON.
- Strip Animator clips inutilisés via Preset Importer.
- Phase 3 fin : audit build size dédié (ticket MIGRATE-VISUAL-17 polish).

### R6 — Outline doubling draw calls + URP forward path (MEDIUM)

Inverted hull = 2× mesh rendered. Sur 50 enemies + 20 towers = 140 mesh draws. URP forward path batch poor si materials varient (chaque enemy a unique tint).
**Mitigation** : GPU instancing dans OutlineBlack.mat (constant black color, parfait instancing target). ToonBase.mat aussi instancing si Phase 3.B-04 confirm que MaterialPropertyBlock used (not new Material per instance — refactor ce point).

### R7 — VFX particle count overflow mobile (MEDIUM)

Sprint 3.D : 5 VFX systems × ~30 particles peak. Sur boss spawn + 30 enemy deaths frame ≈ 500 particles. Mobile mid-range (iPhone 8, Galaxy S8) target.
**Mitigation** : VfxPool taille cap (50 instances par type = 250 particles max). Si dépassé → recycle older. Better skip-frame than crash.

### R8 — Music loop seam audible (LOW)

Track ambient 3-5 min loop seam imperceptible difficile (compositeur skill). Source CC0 freesound rarely loop-perfect.
**Mitigation** : Audacity edit fade-in/out 200ms overlap + Unity AudioSource `Loop = true` + AudioSource `Bypass Effects = false` (mixer reverb tail smooths seam). Si fail → 2 tracks ping-pong via AudioController crossfade.

### R9 — Stealth opacity + toon shader compat (LOW)

Phase 2 Assassin `stealthOpacity` lerp `material.color.a` chaque frame. ToonURP doit avoir `_BaseColor` alpha + Transparent surface mode. Si shader opaque only → stealth invisible.
**Mitigation** : ToonURP Shader Graph configure Surface = Transparent + Alpha clip threshold 0.1. Alternative : 2 variantes shader (ToonOpaque + ToonTransparent), Enemy.Init select selon `cfg.IsStealth`.

### R10 — Apple Silicon (M1) GLTF import bug Unity 6 LTS (LOW, historique)

Unity 6 LTS sur M1 a eu bugs glTFast 2024-2025 (skinned mesh weights corrupted). Patched dans 6000.0.74f1 ? À vérifier.
**Mitigation** : early test import 1 enemy skinned mesh + play mode anim. Si fail → use FBX format (Mixamo export) au lieu de GLTF, ou downgrade glTFast plugin.

---

## Annexe — Récap timeline + budget

| Sprint | Tickets | Estimé temps Sonnet | Cumul |
|---|---|---|---|
| 3.A Assets import | -01 à -04 | 16-22 h | 16-22 h |
| 3.B Shaders + Materials | -05 à -07 | 14-18 h | 30-40 h |
| 3.C Animations | -08 à -10 | 14-20 h | 44-60 h |
| 3.D Particles + JuiceFX | -11 à -13 | 10-14 h | 54-74 h |
| 3.E Audio sweep | -14 à -16 | 10-14 h | 64-88 h |
| Polish & perf buffer | -17 à -20 | 8-15 h | 72-103 h |
| **Total Phase 3** | **~20 tickets** | **~70-100 h** | — |

À 6-8 h Sonnet productifs/jour × 2-3 parallèles : **~2-3 semaines calendrier** conforme estimé STATUS.md.

**Décisions points à valider Mike avant démarrage** : §3.1 (Mechanim vs coroutines), §3.2 (Shuriken vs VFX Graph), §3.3 (music scope), §3.4 (Mixer 4 vs 2 groupes), §3.5 (Shader Graph vs HLSL).

**Démarrage recommandé** : lancer MIGRATE-VISUAL-01 + -02 en parallèle (2 Sonnet feature-dev en worktree, zéro overlap fichiers — différent subdir `Assets/Models/Towers/` vs `/Enemies/`). Critical path : -03 (refactor Init) → ensuite -04 (toon) ∥ -05 (outline) ∥ -06 (animator) en parallèle. Sprint 3.D et 3.E en fin séquentielle (consomment hooks Tower/Enemy établis sprints précédents).

**Dépendances tooling backlog** (cf STATUS.md §Backlog tooling) : si Blender MCP T1 et ComfyUI MCP T1 setup en parallèle Phase 3 (BG Mike), les assets manquants (e.g. tower_acid pas dans manifest Phaser, ou variantes L3 manquantes) peuvent être générés en cours plutôt que blockers. Tickets séparés `MIGRATE-VISUAL-GEN-*` si besoin.

**Impact sur Phase 4** : Phase 4 (multi-platform builds Mac/Win/iOS/Android + Steam/Stores packaging) hérite du Phase 3 visual baseline. Si Phase 3 ship correct cel-shading + animations, Phase 4 est essentiellement build configs + store assets (screenshots, trailers) + i18n LocalizationPackage. Pas de refactor visuel attendu Phase 4.
