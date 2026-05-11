# Phase 4 — Performance Roadmap : audit + tickets

**Date** : 2026-05-11  
**Cible** : 60 fps desktop WebGL / 30 fps mobile iOS+Android  
**Auteur** : Claude Sonnet 4.6 audit statique  
**Scope audit** : code source Phase 3 post-commit `59785a6`, mesure statique (pas de Profiler live à ce stade)

---

## 1. Baseline estimé (mesures statiques Phase 3)

### Draw calls — estimation worst-case W6 (100 ennemis + 20 tours)

| Catégorie | Instances | Draw calls/instance | Total DCs |
|---|---|---|---|
| Ennemis mesh toon | 100 | 1–3 (multi-submesh GLTF) | 100–300 |
| Ennemis outline inverted hull | 100 | 1–3 | 100–300 |
| Tours mesh toon | 20 | 1–3 | 20–60 |
| Tours outline | 20 | 1–3 | 20–60 |
| Tiles MapRenderer (16×8 map W4-6 = 128 tiles) | ~100 tiles actives | 1 (sharedMaterial) | **~10** (batché) |
| Projectiles | ~60 max pool | 1 | 60 |
| Particles actives (4 pools × 50 max) | jusqu'à 200 PS | 1 | 200 |
| HUD UIDocument | — | ~5 | 5 |
| **Total estimé W6 peak** | | | **~515–995 DCs** |

**Cible mobile < 200 DCs** — on est 2.5×–5× au-dessus. Chemin critique.

### GPU instancing — état actuel

- `ToonBase.mat` : `m_EnableInstancingVariants: 1` — checkbox ON. Bon départ.
- `OutlineInvertedHull.shader` : pas de `#pragma instancing_options` dans le CGPROGRAM → **instancing non déclaré côté shader**. Le bit sur le mat ne suffit pas.
- `ToonCelShading.shader` : pas de `#pragma multi_compile_instancing` ni `UNITY_SETUP_INSTANCE_ID` → **instancing désactivé dans le shader built-in**.
- Conséquence : chaque ennemi avec une _BaseColor différente = draw call séparé même si même mesh. GPU instancing ne fonctionne pas actuellement pour les ennemis tintés.

### GC allocations identifiées — sources par sévérité

| Fichier | Ligne | Pattern | Fréquence | GC/frame estimé |
|---|---|---|---|---|
| `SlowEffectManager.Update` | 30 | `new List<Enemy?>()` par frame | 60/s | ~40 B/frame |
| `MaterialController.ApplyToon` | 36,39 | `new Material[]` + `new Material(_toonBase)` × N renderers | Init enemy/pool swap | 1 KB/enemy init |
| `MaterialController.UpdateTint` | 72 | `foreach (var m in r.materials)` — `.materials` property alloue un array | L3 upgrade trigger | mineur |
| `WaveManager.BeginWave` | 74 | `new List<EnemyType>()` + `new Queue<EnemyType>(list)` | 1/wave | négligeable (1/vague) |
| `MineExplosive.Explode` | 61 | `new List<Enemy>(enemies)` snapshot | Chaque explosion | ~1 KB |
| `Projectile.Init` | 71 | `rend.material = new Material(...)` + `rend.material.color` (double `.material` alloc) | Chaque tir | ~200 B/tir |
| `Tower.SpawnMineRing` | 404 | `new GameObject("MineExplosive")` × N en Update | Cluster tick | ~500 B/mine |
| `Enemy.SpawnMeshChild` | 163 | `Resources.Load<AssetRegistry>()` à chaque Init — même si pool reuse | Init enemy | sync IO hit |
| `Tower.SpawnMeshChild` | 147 | idem `Resources.Load<AssetRegistry>()` | Init tower | sync IO hit |

**GC critique** : `SlowEffectManager.Update` alloue une `List<Enemy?>` 60 fois/seconde. Avec ~30 ennemis ralentis simultanément = 60 × 40 B = ~2.4 KB/s GC continu = GC pressure sur mobile (seuil ~4 KB/s avant minor GC visible).

### Build size actuel estimé

| Asset catégorie | Taille disque brute | Compressé Brotli (×0.35 estimé) |
|---|---|---|
| Models Enemies (43 GLTF/GLB) | 58 MB | ~20 MB |
| Models Towers (18 GLTF/GLB) | 3.5 MB | ~1.2 MB |
| Shaders + Materials | ~0.1 MB | ~0.05 MB |
| Scripts (C# IL2CPP) | ~0.8 MB | ~0.3 MB |
| Audio (20 SFX + 1 music, ogg) | ~2 MB | ~1.5 MB |
| UI, SO, Prefabs | ~1 MB | ~0.35 MB |
| Unity engine WebGL runtime | ~8 MB | ~2.8 MB |
| **Total estimé** | **~73 MB** | **~26 MB** |

26 MB compressé = dans les clous de la cible Phase 3 (< 30 MB). Mais 58 MB de mesh bruts non-compressés signifient un first-load lent sur mobile (3G ~30s). Texture atlas pourrait réduire, LOD pourrait supprimer les meshes haute poly des boss.

### Hot paths Update 60fps

- `Enemy.Update` × N ennemis : `UpdateStealth` → `ApplyTint` → `SetPropertyBlock` × N renderers. Avec `_cachedRenderers` (fixé en `408bfab`), c'est O(1) par enemy par frame pour les ennemis stealth. Non-stealth = 0 coût.
- `Tower.UpdateAttack` : `AcquireTarget` itère `WaveManager.ActiveEnemies` (List<Enemy>) linéairement. 20 tours × 100 ennemis = 2000 comparaisons/frame. **Pas de spatial partitioning** (quadtree, grid cells). Coût CPU croît O(T×E).
- `Tower.UpdateSlow` : throttlé à 150ms via `_slowTickTimer`. OK.
- `Synergies.LateUpdate` : throttlé à 200ms via `_tickTimer`. OK.
- `SlowEffectManager.Update` : allocation `new List<Enemy?>()` 60/s (critique, voir GC section).
- `MineExplosive.Update` : N mines × M enemies comparaison par frame. Mine pool absent (new GameObject + Destroy).

### Particules — budget mobile

- 4 pools VfxPool × `MaxPoolSize=200` = **800 ParticleSystem instances max** preallouées.
- Chaque PS : ~15 particles par burst × 200 actives simultanées = 3000 particles peak.
- iPhone 12 / Galaxy S21 budget : ~500–1000 particles GPU. 3000 = dépassement potentiel sur mid-range (iPhone 8 = ~300 particles budget).
- `DefaultCapacity=50` par pool = 200 PS préalloués au démarrage = scène overhead ~200 GO inactifs.

### Audio — AudioController

- Pool 8 `AudioSource` — acceptable.
- `MinReplayInterval = 0.028s` → anti-spam OK.
- Pas de `Preload Audio Data = true` vérifié dans code (dépend des import settings, non lus).

### Render Pipeline — état

- Aucun package URP (`com.unity.render-pipelines.universal`) dans `manifest.json` ou `packages-lock.json`.
- Seulement `com.unity.render-pipelines.core` (dépendance transitoire de shadergraph) et `com.unity.shadergraph` (non utilisé — les shaders sont en Built-in CG, pas SG).
- **Conclusion : projet tourne Built-in Render Pipeline, pas URP.** Les shaders `CrowdDefense/ToonCelShading` et `CrowdDefense/OutlineInvertedHull` utilisent `CGPROGRAM`/`UnityCG.cginc`, cohérent.
- Implication : pas de `#pragma multi_compile_instancing` = instancing GPU inactif même avec bit mat.

### IL2CPP stripping

- `managedStrippingLevel: {}` = vide = **stripping non configuré** → niveau par défaut (Disabled ou Minimal selon target).
- Pour WebGL : Unity 6 LTS impose Minimal stripping minimum. High stripping non activé.
- Pas de `link.xml` trouvé → reflection-based types à risque de strip.

---

## 2. Bottlenecks par sévérité

### P0 — Bloquants mobile 30fps

**P0-A : Draw calls 2–5× au-dessus de la cible mobile**
Chemin : 100 ennemis × 2 DCs (toon + outline) × 2 submeshes GLTF moyens = ~400 DCs pour les ennemis seuls. Cible mobile < 200 DCs totaux.

Cause racine : pas de batching actif sur les ennemis/tours. GPU instancing déclaré sur le mat mais pas dans le shader source (CGPROGRAM sans `multi_compile_instancing`). Chaque ennemi avec tint unique = draw call séparé.

**P0-B : Outline GOs = 2× mesh count, aucun batching**
`Outline.ApplyToHierarchy` crée N GameObjects "Outline" enfants pour chaque MeshFilter du subtree GLTF. Sur un boss dragon (~15 submeshes), ça génère 15 GO outlines. 100 ennemis × 3 submeshes moyens × outline = 300 DCs outline-only.

Le shader `OutlineInvertedHull` use `sharedMaterial` (1 seule instance mat) → les outlines pourraient batcher si mesh identiques. Mais ennemis de types différents = meshes différents = pas de batching. URP renderer feature serait plus efficace (1 pass screen-space pour tous).

### P1 — Significatif, adressable Phase 4

**P1-A : `SlowEffectManager.Update` alloc `new List<Enemy?>()` 60fps**
~2.4 KB/s GC continu. Sur mobile WebGL GC non-incrémental = stutter visible toutes les ~500ms quand slow effects actifs. Fix : pré-allouer `_toRemove` comme champ privé réutilisé.

**P1-B : `AcquireTarget` O(T×E) sans spatial partitioning**
20 tours × 100 ennemis = 2000 distance checks par frame. Chaque check = sqrMagnitude (3 multiplications). À 100 ennemis, ~120 000 ops/s. Acceptable sur desktop, marginal sur mobile à W7-W8 swarm (200+ ennemis).

**P1-C : `Resources.Load<AssetRegistry>()` à chaque `Enemy.Init` et `Tower.Init`**
`Resources.Load` est une opération synchrone qui scanne l'asset database. Déclenché à chaque Init poolé. Fix : cache statique partagé, chargé une seule fois au démarrage.

**P1-D : `Projectile.Init` alloc `new Material()` + double `.material` property access**
Chaque tir crée un nouveau Material. 20 tours × 1 tir/s = 20 new Material/s = 20 × ~200 B GC = ~4 KB/s. Fix : pool de couleurs de projectiles comme MaterialPropertyBlock sur un shared mat.

**P1-E : `MineExplosive` non poolé — `new GameObject` + `Destroy` par Update tick**
`Tower.UpdateCluster` → `SpawnMineRing` → N × `new GameObject("MineExplosive")` toutes les `CooldownMs`. Sur Mine tower active = ~3–5 `new GameObject` toutes les 12s. Léger mais Destroy génère GC et peut causer stutter frame.

**P1-F : `MaterialController.ApplyToon` alloc `new Material[]` + `new Material` × N renderers à chaque Init**
Déclenché sur chaque ennemi Init (pool reuse inclus si cfg change). Sur un ennemi GLTF à 4 submeshes = 4 `new Material`. Fix : utiliser `MaterialPropertyBlock` exclusivement pour `_BaseColor` tint + garder un seul Material partagé par type (ou `sharedMaterial` + MPB).

### P2 — Optimisation Phase 4+ (important mais non-bloquant shipping)

**P2-A : Pas de LOD groups sur les modèles GLTF (boss dragon 5+ MB)**
boss_volcan_dragon_v2.gltf est dans `Assets/Models/Enemies/Bosses/` — vertex count non mesuré mais GLTF Quaternius boss = 5 000–20 000 verts typiquement. LOD0/1/2 auto-généré Unity = 30–70% réduction vertex à distance > 5 unités.

**P2-B : Pas de static batching sur les tiles MapRenderer**
`MapRenderer` utilise `sharedMaterial` par couleur de tile (correct — ~10 mats partagés). Mais les slab GO ne sont pas marqués `GameObjectFlags.Static` → pas de static batching Unity. Sur 16×8 map = 128 tiles × 1 DC/mat type = ~10 DCs possible après static batch (groupé par couleur). Actuel = probablement ~60–80 DCs tiles (mats partagés mais batching nécessite static flag).

**P2-C : IL2CPP stripping non configuré**
`managedStrippingLevel: {}` = Disabled/Minimal. WebGL High stripping = ~15–20% réduction bundle size + ~5–10% runtime init speedup. Risk : `link.xml` absent peut stripper des types utilisés par reflection (ex: `Resources.Load<T>` types). Mitigation : ajouter `link.xml` avant d'activer High.

**P2-D : Particles pool DefaultCapacity 50 × 4 = 200 PS préalloués au Start**
200 PS inactifs en scène = overhead transform update minimal mais présent. Sur mobile low-end : réduire à `DefaultCapacity = 10`, `MaxPoolSize = 50` (mobile preset).

**P2-E : ToonCelShading ShadowCaster pass actif sur tous les ennemis/tours**
Shader a un ShadowCaster pass. Sur 100 ennemis = 100 shadow caster draws + 100 outline DCs qui n'ont pas de shadow (OK, `ShadowCastingMode.Off` dans Outline.cs). Désactiver shadows sur ennemis/tours mobiles via `renderer.shadowCastingMode = ShadowCastingMode.Off` (mobile preset) = ~25% DC reduction sur shadow depth pass.

**P2-F : AudioController `MinReplayInterval = 0.028s` — Dictionary lookup par string 60fps**
`_lastPlayedAt.TryGetValue(clipKey, ...)` sur chaque `Play()` call. 20 tours × 1 tir/s = 20 dictionary lookups/s. Acceptable. Pas de hot path concern.

**P2-G : Absence de texture atlas ennemis**
60 GLTF assets × textures embarquées = N textures non-regroupées. Batching GPU instancing requiert même texture. Atlas 2048×2048 permet de regrouper les ennemis humanoides (knight/goblin/zombie/pirate/wizard/soldier = 6 textures similaires). Réduit les texture swaps GPU. Effort estimé élevé (Blender bake) — Phase 5 si besoin.

---

## 3. Tickets Phase 4 (N=10 tickets atomiques)

### PERF-01 — GPU instancing dans ToonCelShading + OutlineInvertedHull shaders

**Type** : quality-maintainer  
**Estimé** : 1 commit, 2h  
**Bloqué par** : rien  
**Brief** : ToonBase.mat a `m_EnableInstancingVariants: 1` mais le shader CGPROGRAM n'a pas les pragmas requis. Sans `#pragma multi_compile_instancing` + `UNITY_SETUP_INSTANCE_ID` + `UNITY_INSTANCED_PROP`, l'instancing reste inactif. Les ennemis identiques (même mesh, tints différents via MPB) n'utilisent pas SRP Batcher / GPU instancing = N draw calls au lieu de 1 drawcall instancié.

**Commits à livrer** :
1. `fix(shaders): ToonCelShading add multi_compile_instancing + UNITY_SETUP_INSTANCE_ID + per-instance _BaseColor`  
   - Fichier : `Assets/Shaders/ToonCelShading.shader`  
   - Ajouter `#pragma multi_compile_instancing` dans le Pass ToonForward  
   - Ajouter `UNITY_INSTANCING_BUFFER_START(Props)` + `UNITY_DEFINE_INSTANCED_PROP(fixed4, _BaseColor)` + `UNITY_INSTANCING_BUFFER_END(Props)`  
   - Dans vert : `UNITY_SETUP_INSTANCE_ID(v)` + `UNITY_TRANSFER_INSTANCE_ID(v, o)`  
   - Dans frag : `UNITY_SETUP_INSTANCE_ID(i)` + lire `_BaseColor` via `UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColor)`  
2. `fix(shaders): OutlineInvertedHull add multi_compile_instancing`  
   - Fichier : `Assets/Shaders/OutlineInvertedHull.shader`  
   - Idem pattern — `_OutlineColor` per-instance (constant black = pas besoin, mais instancing pragma requis pour que le batcher groupe les outlines)  
   - Simplification : `_OutlineColor` constant = même couleur pour tous → 1 draw call instancié pour tous les outlines de même mesh. Gain réel sur ennemis du même type (knight ×20 = 1 DC instancié).

**Verification** : Unity Editor → Frame Debugger : compter DCs avant/après. Target : 100 ennemis du même type = 1–2 DCs au lieu de 100–200.

**Impact estimé draw calls** : -40 à -70% DCs sur vagues mono-type (W1-W3). W7+ multi-type : -20 à -40%.

---

### PERF-02 — MaterialController : MPB exclusif, zéro new Material() par enemy Init

**Type** : quality-maintainer  
**Estimé** : 2 commits, 3h  
**Bloqué par** : PERF-01 (pour cohérence — MPB + instancing vont ensemble)  
**Brief** : `MaterialController.ApplyToon` crée actuellement `new Material(_toonBase)` par renderer par ennemi Init (pool reuse inclus). Sur 100 ennemis × 2 renderers = 200 allocations Material. Fix : partager `ToonBase.mat` via `sharedMaterial` + passer la couleur via `MaterialPropertyBlock._BaseColor`. MPB = zero GC, compatible GPU instancing.

**Commits à livrer** :
1. `refactor(visual): MaterialController.ApplyToon use sharedMaterial + MPB for _BaseColor tint`  
   - Fichier : `Assets/Scripts/Visual/MaterialController.cs`  
   - Supprimer `new Material(_toonBase)` loop  
   - Utiliser `r.sharedMaterial = _toonBase` (ou variante transparent si `IsStealth`)  
   - Créer `MaterialPropertyBlock` par Renderer, setter `_BaseColor` via `SetColor`  
   - Note : pour stealth (transparent surface), garder 1 `Material` shared dédié `ToonTransparent.mat` (instancié 1 fois, partagé tous stealth enemies) — MPB contrôle l'alpha  
   - Supprimer la texture-copy logic (les GLTF importés via UnityGLTF ont déjà leur texture baked dans le sharedMaterial source — pas besoin de conserver)  
2. `refactor(visual): MaterialController.UpdateTint use SetPropertyBlock not r.materials`  
   - Fichier : `Assets/Scripts/Visual/MaterialController.cs`  
   - `UpdateTint` itère actuellement `r.materials` (alloc) — remplacer par MPB cached per-renderer (ou re-utiliser le MPB ennemi `_mpb` passé en paramètre)

**Verification** : Unity Profiler Memory → `GC.Alloc` frame lors d'un spawn vague → cible < 100 B/enemy spawn (vs ~1 KB actuel).

---

### PERF-03 — SlowEffectManager : éliminer new List<Enemy?> 60fps

**Type** : bug-fixer (GC stutter)  
**Estimé** : 1 commit, 1h  
**Bloqué par** : rien  
**Brief** : `SlowEffectManager.Update` crée `new List<Enemy?>()` 60 fois/seconde. ~2.4 KB/s GC = minor GC collection visible ~500ms sur mobile WebGL (non-incrémental GC). Fix trivial : pré-allouer `_toRemove` comme champ privé, `Clear()` avant usage.

**Commits à livrer** :
1. `fix(systems): SlowEffectManager pre-alloc _toRemove list, eliminate 60fps GC alloc`  
   - Fichier : `Assets/Scripts/Systems/SlowEffectManager.cs`  
   - Ajouter `private readonly List<Enemy?> _toRemove = new();` champ  
   - Dans `Update()` : remplacer `var toRemove = new List<Enemy?>()` par `_toRemove.Clear()`

**Verification** : Unity Profiler → `GC.Alloc` dans `SlowEffectManager.Update` = 0 B/frame.

---

### PERF-04 — Projectile : MPB + sharedMaterial au lieu de new Material() par tir

**Type** : quality-maintainer  
**Estimé** : 2 commits, 3h  
**Bloqué par** : rien  
**Brief** : `Projectile.Init` crée `new Material(ShaderUtil.GetLitShader())` puis setter `.material.color` = double `.material` property access (chaque accès à `.material` en non-shared alloue si pas déjà unique). 20 tours × ~1 tir/s = ~20 new Material/s + GC. Fix : pool de couleurs (7 couleurs projectile distinctes dans les TowerType SO) → 7 Materials pré-créés, partagés. MPB pour la couleur.

**Commits à livrer** :
1. `feat(entities): ProjectileColorCache static dict Color→Material, pre-built at level start`  
   - Fichier : `Assets/Scripts/Entities/Projectile.cs`  
   - Ajouter `private static readonly Dictionary<Color, Material> _colorMats = new()`  
   - Dans `Init` : `if (!_colorMats.TryGetValue(color, out var m)) { m = new Material(ShaderUtil.GetLitShader()); m.color = color; _colorMats[color] = m; }` puis `rend.sharedMaterial = m`  
2. `fix(entities): Projectile.Init use sharedMaterial from cache, zero GC per shot`  
   - Supprimer les 2 lignes `rend.material.SetFloat` — `.sharedMaterial` cohérent  
   - Note : tous les projectiles d'une même couleur partagent le même Material → pas de tint runtime per-instance, acceptable (projectile color = per-TowerType constant)

**Verification** : Profiler `GC.Alloc` frame pendant combat = 0 B de Material allocation.

---

### PERF-05 — AssetRegistry : cache statique, zéro Resources.Load par pool Init

**Type** : quality-maintainer  
**Estimé** : 1 commit, 1.5h  
**Bloqué par** : rien  
**Brief** : `Enemy.SpawnMeshChild` et `Tower.SpawnMeshChild` appellent `Resources.Load<AssetRegistry>("AssetRegistry")` à chaque Init. `Resources.Load` est une opération synchrone avec lookup dans l'asset database. Déclenché à chaque spawn ennemi (pool). Fix : cache statique chargé une seule fois.

**Commits à livrer** :
1. `fix(data): AssetRegistry add static _instance cache, lazy-loaded once, zero per-spawn IO`  
   - Fichier : `Assets/Scripts/Data/AssetRegistry.cs`  
   - Ajouter `private static AssetRegistry? _instance;`  
   - Ajouter `public static AssetRegistry? Instance => _instance ??= Resources.Load<AssetRegistry>("AssetRegistry");`  
   - Fichier : `Assets/Scripts/Entities/Enemy.cs` — remplacer `Resources.Load<AssetRegistry>("AssetRegistry")` par `AssetRegistry.Instance`  
   - Fichier : `Assets/Scripts/Entities/Tower.cs` — idem

**Verification** : CPU Profiler `Resources.Load` count = 0 pendant gameplay (uniquement au premier Init).

---

### PERF-06 — MineExplosive pool : supprimer new GameObject + Destroy par Update tick

**Type** : quality-maintainer  
**Estimé** : 2 commits, 3h  
**Bloqué par** : rien  
**Brief** : `Tower.UpdateCluster` → `SpawnMineRing` → N × `new GameObject("MineExplosive")`. Les mines explosent et font `Destroy(gameObject)`. Pas de pool. Fix : pool Unity `ObjectPool<MineExplosive>` similaire à `EnemyPool` / `ProjectilePool`. Impact limité (Mine tower usage restreint) mais Destroy génère GC + GC spike visible.

**Commits à livrer** :
1. `feat(systems): MineExplosivePool ObjectPool wrapper, cap 30 instances`  
   - Nouveau fichier : `Assets/Scripts/Systems/MineExplosivePool.cs` — MonoSingleton, `ObjectPool<MineExplosive>`, defaultCapacity 10, maxSize 30  
2. `refactor(entities): Tower.UpdateCluster use MineExplosivePool instead of new GameObject`  
   - Fichier : `Assets/Scripts/Entities/Tower.cs` méthode `SpawnMineRing`  
   - Remplacer `new GameObject("MineExplosive")` par `MineExplosivePool.Instance.Get()`  
   - Fichier : `Assets/Scripts/Entities/MineExplosive.cs` : `Explode` appelle `pool.Release(this)` au lieu de `Destroy`  
   - `MineExplosive` ajoute `private MineExplosivePool? pool;` + `SetPool()` pattern (cf EnemyPool)

**Verification** : Profiler `GC.Alloc` frame lors d'une mine explosion = 0 B.

---

### PERF-07 — AcquireTarget + UpdateSlow : spatial grid partitioning O(1) lookup

**Type** : feature-dev  
**Estimé** : 3 commits, 6h  
**Bloqué par** : rien (indépendant des autres)  
**Brief** : `Tower.AcquireTarget` itère tous les `WaveManager.ActiveEnemies` linéairement. 20 tours × 100 ennemis = 2 000 checks/frame. À W7-W8 avec 200 ennemis = 4 000 checks/frame = ~240 000 ops/s. `Tower.UpdateSlow` idem (throttlé 150ms, moindre impact). Fix : `EnemyCellCache` singleton partitionne la carte en cellules (ex: 4×4 unités) et maintient une liste d'ennemis par cellule. Mise à jour 1 fois / frame via `LateUpdate` batché. Lookup dans un rayon = itérer seulement les cellules dans le cercle range.

**Commits à livrer** :
1. `feat(systems): EnemyCellCache grid partitioning (4u cells), update 1/frame LateUpdate`  
   - Nouveau fichier : `Assets/Scripts/Systems/EnemyCellCache.cs`  
   - Grille de cellules 4 unités × 4 unités = ~4×4 = 16 cellules pour map 16×8 (W4-6)  
   - `LateUpdate` : clear + re-insert tous ennemis actifs O(E)  
   - `GetEnemiesInRadius(Vector3 center, float radius) : IEnumerable<Enemy>` — itère les ~4–9 cellules dans le rayon  
2. `refactor(entities): Tower.AcquireTarget use EnemyCellCache instead of linear scan`  
   - Fichier : `Assets/Scripts/Entities/Tower.cs`  
   - `AcquireTarget` utilise `EnemyCellCache.Instance.GetEnemiesInRadius(position, cfg.Range)`  
3. `refactor(entities): Tower.UpdateSlow use EnemyCellCache`  
   - Fichier : `Assets/Scripts/Entities/Tower.cs`  
   - `UpdateSlow` idem (throttlé 150ms, gain plus limité mais cohérent)

**Verification** : Unity Profiler `Tower.AcquireTarget` CPU time = < 0.1ms à 100 ennemis (vs ~0.5ms linear).

---

### PERF-08 — Static batching tiles MapRenderer

**Type** : quality-maintainer  
**Estimé** : 1 commit, 1.5h  
**Bloqué par** : rien  
**Brief** : `MapRenderer` crée N slabs `GameObject.CreatePrimitive(Cube)` partageant `sharedMaterial` par couleur tile (~10 mats). Les GO ne sont pas marqués `static` → pas de static batching Unity. `StaticBatchingUtility.Combine(parent)` ou marquer `GameObjectFlags.Static` au runtime = combine meshes par mat → 10 DCs au lieu de ~60–100.

**Commits à livrer** :
1. `perf(systems): MapRenderer call StaticBatchingUtility.Combine after spawning all slabs`  
   - Fichier : `Assets/Scripts/Systems/MapRenderer.cs`  
   - À la fin de `Start()` après la double boucle : `StaticBatchingUtility.Combine(gameObject);`  
   - Note : tiles créés à Start et jamais modifiés en runtime → static batching safe. Colliders déjà détruits (ligne 43) — OK.

**Verification** : Frame Debugger → tiles grass/path/water regroupés en batches de même couleur. Compter DCs tiles avant/après.

---

### PERF-09 — Particles : mobile preset via SettingsRegistry, réduire pool size

**Type** : quality-maintainer  
**Estimé** : 2 commits, 3h  
**Bloqué par** : rien  
**Brief** : `VfxPool` a `DefaultCapacity=50`, `MaxPoolSize=200` par pool. 4 pools = 200 PS préalloués au Start + 800 max actifs. Sur mobile mid-range (iPhone 8) budget ~300 particles total. Fix : appliquer preset selon `SystemInfo.graphicsMemorySize` (ou `Application.isMobilePlatform`) au Awake : mobile = DefaultCapacity 10, MaxPoolSize 50 (total 200 PS max).

**Commits à livrer** :
1. `feat(visual): VfxPool mobile quality preset : scale DefaultCapacity + MaxPoolSize by platform`  
   - Fichier : `Assets/Scripts/Visual/VfxPool.cs`  
   - Dans `OnAwakeSingleton()` : `bool isMobile = Application.isMobilePlatform || SystemInfo.graphicsMemorySize < 2048`  
   - `int cap = isMobile ? 10 : 50; int maxSize = isMobile ? 50 : 200;`  
   - Passer `cap` et `maxSize` à `BuildPool(prefab, label, cap, maxSize)` (refactor signature)  
2. `feat(visual): VfxPool expose static ParticleBudget property for boss VFX scaling`  
   - `public static int MaxActiveBudget => isMobilePreset ? 100 : 400;`  
   - Boss death VFX scale down si `_impactPool.CountActive + _deathPool.CountActive > MaxActiveBudget * 0.8f`

**Verification** : Play mode + Unity Profiler Particle system → peak particles < 300 en mobile preset.

---

### PERF-10 — IL2CPP stripping + link.xml, WebGL build size

**Type** : quality-maintainer  
**Estimé** : 2 commits, 2h  
**Bloqué par** : rien  
**Brief** : `managedStrippingLevel: {}` = stripping non configuré. WebGL avec High stripping = ~15–20% réduction bundle + 5–10% startup speedup. Risk : reflection-based types (`Resources.Load<T>`, `JsonUtility`) stripped. Mitigation : `link.xml` préserve les types critiques.

**Commits à livrer** :
1. `chore(build): add link.xml preserve CrowdDefense assemblies + Unity.TextMeshPro`  
   - Nouveau fichier : `Assets/link.xml`  
   ```xml
   <linker>
     <assembly fullname="CrowdDefense" preserve="all"/>
     <assembly fullname="Unity.TextMeshPro" preserve="all"/>
     <assembly fullname="UnityEngine.UI" preserve="all"/>
   </linker>
   ```
2. `chore(build): enable IL2CPP ManagedStrippingLevel High for WebGL + iOS + Android in ProjectSettings`  
   - Fichier : `ProjectSettings/ProjectSettings.asset`  
   - `managedStrippingLevel: {2: 3, 3: 3, 13: 3}` (keys : 2=WebGL, 3=iOS, 13=Android ; valeur 3=High)  
   - `scriptingBackend: {2: 1, 3: 1, 13: 1}` (1=IL2CPP)  
   - Note : WebGL est déjà IL2CPP-only dans Unity 6. Confirmer iOS/Android.

**Verification** : Build WebGL size avant/après. Target : < 22 MB compressé (gain ~15%).

---

## 4. Risks + considérations mobile spécifiques

### R-MOBILE-01 : Render Pipeline Built-in vs URP — draw call budget

Le projet utilise **Built-in Render Pipeline** (pas URP). Le SRP Batcher (URP/HDRP exclusif, Unity 6) ne s'applique pas ici. Les optimisations instancing doivent passer par les pragmas CGPROGRAM classiques (cf PERF-01). Si migration URP est envisagée Phase 5 pour accéder au SRP Batcher, c'est un effort non-trivial (port shaders + URP pipeline asset setup). La recommandation Phase 4 est de rester Built-in + corriger instancing pragmas (PERF-01) plutôt que migrer URP.

### R-MOBILE-02 : WebGL GC non-incrémental — stutter spikes

WebGL Unity 6 utilise le GC Boehm non-incrémental. Tout GC.Collect est un freeze main thread. `SlowEffectManager` (PERF-03) + `Projectile.Init` (PERF-04) sont les sources les plus fréquentes. Post PERF-03+04, surveiller via `Profiler.GetMonoHeapSizeLong()` en WebGL build — si > 64 MB, déclenche GC majeur visible.

### R-MOBILE-03 : iOS Safari WebGL audio latency 200–500ms

`AudioController.Play("tower_shoot")` déclenché chaque tir. iOS Safari WebAudio latency élevée (200–500ms) = décalage son/animation. Mitigation : `AudioClip.LoadType = CompressedInMemory` + `Preload Audio Data = true` sur tous les SFX courts (< 1s). À vérifier dans les import settings des `.ogg`.

### R-MOBILE-04 : Outline GOs vs URP Renderer Feature

La technique inverted hull (PERF-01, PERF-02 après fixes) reste coûteuse en DCs sur mobile. Une alternative Phase 4+ : remplacer les GO outlines par une URP Renderer Feature post-process screen-space (1 pass pour tous les outlines). Conditionné à migration URP (R-MOBILE-01). Si projet reste Built-in : l'instancing des outlines via PERF-01 est le maximum atteignable.

### R-MOBILE-05 : Boss GLTF heavy meshes sans LOD

`boss_volcan_dragon_v2.gltf` (dans Assets/Models/Enemies/Bosses/) — pas de LOD group. Boss apparaît rare (W4+ : 1 par vague finale) mais peut être visible en même temps que 100 ennemis = overdraw + vertex shading cost. LOD auto Unity (`LODGroup` + `GenerateLOD` Editor scripting) sur les 14 boss assets serait un ticket PERF-11 Phase 4 optionnel.

### R-MOBILE-06 : Terrain de test — pas de Profiler live disponible

Cet audit est **statique** (lecture code + assets). Les chiffres de draw calls, GC allocs et frame times sont des **estimations** basées sur les patterns de code. Avant de prioriser les tickets Phase 4, la validation par Unity Profiler (play mode Editor + Profiler window) et Frame Debugger est essentielle pour confirmer les P0 réels vs estimés. Recommandation : ouvrir Unity Editor, lancer W4 play mode avec Profiler attached, noter les vraies valeurs avant de commencer les tickets.

---

## 5. Ordering recommandé Phase 4

```
Sprint 4.A (1–2j) — GC quick wins :
  PERF-03 (1h) + PERF-05 (1.5h) + PERF-08 (1.5h)
  → 0 GC SlowEffect + 0 Resources.Load/spawn + static batch tiles

Sprint 4.B (2–3j) — Rendering :
  PERF-01 (2h) + PERF-02 (3h) + PERF-04 (3h)
  → GPU instancing shaders + MPB enemies + 0 Material/tir

Sprint 4.C (1–2j) — Pools + misc :
  PERF-06 (3h) + PERF-09 (3h)
  → Mine pool + particles mobile preset

Sprint 4.D (2–3j) — Spatial + build :
  PERF-07 (6h) + PERF-10 (2h)
  → EnemyCellCache + IL2CPP stripping
```

**Estimé total** : ~12 tickets = ~31h Sonnet. 2 worktrees parallèles possibles dès Sprint 4.B (PERF-01 indépendant de PERF-02 sauf merge final).

---

## 6. Tableau récapitulatif des gains estimés

| Ticket | Métrique | Avant (estimé) | Après (estimé) |
|---|---|---|---|
| PERF-01 | Draw calls W6 100 ennemis même type | ~400 DCs | ~10 DCs (instancié) |
| PERF-02 | GC alloc / enemy Init | ~1 KB | < 50 B |
| PERF-03 | GC alloc / frame slow effect | ~40 B/frame | 0 B/frame |
| PERF-04 | GC alloc / projectile tir | ~200 B/tir | 0 B/tir |
| PERF-05 | Resources.Load / spawn | sync IO hit | 0 (cached) |
| PERF-06 | GC alloc / mine explosion | ~500 B | 0 B |
| PERF-07 | AcquireTarget CPU 100 ennemis | ~2000 checks/frame | ~20–50 checks/frame |
| PERF-08 | Draw calls tiles (128 tiles) | ~60–100 DCs | ~10 DCs (batché) |
| PERF-09 | Particles max mobile | 3000 | 300 |
| PERF-10 | WebGL bundle size | ~26 MB | ~22 MB estimé |

**Draw calls total estimé post Phase 4** (W6, 100 ennemis mixed-type) :
- Ennemis toon (instancié par mesh-type) : ~20 DCs (vs ~300)
- Ennemis outline (instancié) : ~20 DCs (vs ~300)
- Tours : ~5 DCs (instancié)
- Tours outline : ~5 DCs
- Tiles (static batch) : ~10 DCs (vs ~80)
- Projectiles : ~10 DCs (instancié)
- Particles : ~20 DCs (poolé)
- HUD : ~5 DCs
- **Total estimé post-Phase 4 : ~95 DCs** (vs ~700–995 actuel)

Cible mobile < 200 DCs = **atteignable post Phase 4**.

