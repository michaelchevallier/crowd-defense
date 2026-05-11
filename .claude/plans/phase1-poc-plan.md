# Phase 1 POC — Plan d'implémentation

> Plan formel généré par agent Plan le 2026-05-11. Pré-validation Mike requise sur les 5 questions ouvertes (cf fin de doc) avant de briefer Sonnet feature-dev sur MIGRATE-POC-01.

Cible : W1-1 jouable Unity 6.3 LTS WebGL — 1 tower (archer), 3 enemies (basic/runner/brute), 3 waves, castle HP, économie minimal, HUD.

---

## Choix techniques transverses

### Q-A — UI : **uGUI (Canvas + RectTransform)**
Justification : Mike = zéro Unity, HUD POC tient en 3 textes + 1 bouton restart. uGUI = battle-tested, doc partout, debug Inspector trivial. UI Toolkit moderne mais double charge cognitive (USS + UXML + bindings runtime) qui plombe la vitesse Sonnet en Phase 1.
Impact Phase 2 : refacto isolée derrière interface `HudController` stable.

### Q-B — Grid : **C# 2D array custom + `Vector2Int` helpers**
Justification : Source Phaser parse une `string[]` en grid typée maison (`MapGrid.js`). Unity Tilemap = overkill pour grille 7×15 statique. Custom = porté quasi 1:1.
Impact Phase 2 : zéro.

### Q-C — Path : **Waypoints sérialisés sur LevelData SO** (calculés au load via BFS C#)
Justification : Port direct `MapPathfinder.bfsShortestPerCastle`. Linear segments suffisent POC (pas de Catmull-Rom smoothing).
Impact Phase 2 : ajout smoothing additif, pas breaking.

### Q-D — Object pool : **Pas dès POC — `Instantiate`/`Destroy` direct**
Justification : W1-1 = ~150 enemies sur 3 waves échelonnées (~5-15 simultanés). En dessous du seuil critique (50+ mobs). Pool ajoute complexité + bug surface.
Impact Phase 2 : ajouter `EnemyPool` + `ProjectilePool` via Unity `ObjectPool<T>` natif (depuis 2021) avant W7+. Refacto local sur `WaveManager.SpawnEnemy()` et `Tower.Fire()`. Dette technique notée dans MIGRATE-POC-04.

### Q-E — Render : **2D top-down (SpriteRenderer + orthographic camera)**
Justification : Pas d'assets Unity dispo, objectif POC = valider boucle gameplay + workflow Unity-MCP. Sprites colorés placeholder OK.
Impact Phase 2 : refacto camera + remplacement SpriteRenderer → MeshRenderer si Mike veut 3D. World coords (Vector3) restent valides.

### Q-F — Coordinate system : **Cells = `Vector2Int (col, row)` au design ; World = `Vector3` au render**
Helper statique `GridCoords.CellToWorld()` / `WorldToCell()`. Gameplay logic en cells, render en world.
Convention 2D : plan XY (Z=0), row=0 en haut visuel (Y inversé).

### Q-G — Singleton pattern : **MonoBehaviour scene-bound + `static Instance` simple**
`LevelRunner`, `Economy`, `PathManager`, `WaveManager` sont scene-scoped. Pure singleton `Awake → Instance = this`.
Pas de `Find*()` en `Update`. Cache via `Instance` ou ref `[SerializeField]`.

Pattern standard :
```csharp
public static LevelRunner Instance { get; private set; }
private void Awake() {
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
}
```

---

## Pièges Unity transverses (rappels par ticket)

- **Pas de `Find` / `GetComponent` en `Update`** → cache en `Awake()`/`Start()`.
- **`#if UNITY_EDITOR`** wrap toute écriture runtime sur `ScriptableObject`.
- **`[SerializeField] private`** au lieu de `public` pour fields Inspector.
- **Pas de `Debug.Log` en prod** → `#if UNITY_EDITOR` wrap.
- **Pas de LINQ en hot path** → `for` manuel.
- **`Time.deltaTime`** scaling temps ; `Time.fixedDeltaTime` physics.

---

## Tickets

### MIGRATE-POC-01 — Scaffold project structure + scenes

- **Goal** : Arborescence dossiers + scene `Main.unity` vide + asmdef.
- **Deps** : aucun.
- **Source Phaser** : aucun.
- **Fichiers Unity** :
  - `Assets/Scripts/{Data,Entities,Systems,UI}/.gitkeep`
  - `Assets/Prefabs/{Towers,Enemies,UI}/.gitkeep`
  - `Assets/ScriptableObjects/{Towers,Enemies,Levels}/.gitkeep`
  - `Assets/Scenes/Main.unity` (vide + Main Camera ortho size=8 pos=(0,0,-10) + Directional Light + GameObject `Systems` parent)
  - `Assets/Scripts/CrowdDefense.asmdef` (namespace `CrowdDefense`)
- **Commits** :
  - `chore: scaffold Unity folder structure (Scripts/Prefabs/ScriptableObjects/Scenes)`
  - `chore: add Main.unity empty scene with ortho camera + systems root`
- **Verification** :
  - `manage_scene get_active` → `Main`
  - `read_console` → aucune erreur compile
  - `find_gameobjects` → `Main Camera`, `Directional Light`, `Systems`
- **Estimation** : 2 commits, ~15 min.

---

### MIGRATE-POC-02 — ScriptableObject data definitions

- **Goal** : 3 SO classes (`TowerType`, `EnemyType`, `LevelData`) + 1 instance de chaque.
- **Deps** : MIGRATE-POC-01.
- **Source Phaser** :
  - `/Users/mike/Work/milan project/src-v3/entities/Tower.js` L9-119 (TOWER_TYPES, focus archer/crossbow)
  - `/Users/mike/Work/milan project/src-v3/entities/Enemy.js` L53-120 (ENEMY_TYPES, focus basic/runner/brute)
  - `/Users/mike/Work/milan project/src-v3/data/levels/world1-1.js` (intégral)
- **Fichiers Unity** :
  - `Assets/Scripts/Data/TowerType.cs` — `[CreateAssetMenu] ScriptableObject` fields : id, displayName, cost, damage, range, fireRateMs, projectileSpeed, aoe, pierce, unlockWorld, iconColor.
  - `Assets/Scripts/Data/EnemyType.cs` — id, displayName, hp, speed, damage, reward, scale, bodyColor.
  - `Assets/Scripts/Data/WaveDef.cs` — `[Serializable] struct WaveDef { List<EnemySpawnEntry> entries; int spawnRateMs; int breakMs; }` ; `EnemySpawnEntry { EnemyType type; int count; }`.
  - `Assets/Scripts/Data/LevelData.cs` — id, displayName, mapRows (string[]), cellSize (default 1.0), startCoins, castleHP, waves (List<WaveDef>).
  - **SO instances** (via `manage_scriptable_object`) :
    - `Assets/ScriptableObjects/Towers/Archer.asset` (cost 30, dmg 1.38, range 8, fireRateMs 700, projSpeed 22).
    - `Assets/ScriptableObjects/Enemies/Basic.asset` (hp 3, speed 1.2, dmg 5, reward 2, rouge).
    - `Assets/ScriptableObjects/Enemies/Runner.asset` (hp 1, speed 2.4, dmg 4, reward 2, cyan).
    - `Assets/ScriptableObjects/Enemies/Brute.asset` (hp 12, speed 0.8, dmg 12, reward 8, marron).
    - `Assets/ScriptableObjects/Levels/W1-1.asset` (3 waves POC : 8/16/16 enemies, startCoins 120, castleHP 200, mapRows 7×13 single path).
- **Gotchas** : `[CreateAssetMenu]` requis pour création MCP. Wrap mutation runtime SO `#if UNITY_EDITOR` (anticiper Phase 2).
- **Commits** :
  - `feat(data): add TowerType/EnemyType/LevelData ScriptableObject definitions`
  - `feat(data): create W1-1 ScriptableObject instances (archer, basic/runner/brute, W1-1 level)`
- **Verification** : `read_console` no errors. 5 .asset files créés. Inspector populé via `manage_scriptable_object get`.
- **Estimation** : 2 commits, ~40 min.

---

### MIGRATE-POC-03 — Grid + path system (PathManager)

- **Goal** : Parser `mapRows` → `GridData`, BFS portal→castle, expose polyline waypoints.
- **Deps** : MIGRATE-POC-02.
- **Source Phaser** :
  - `/Users/mike/Work/milan project/src-v3/systems/MapGrid.js` (~150 lignes, focus `CELL`, `cellToWorld`, `neighbors`, `parseMap`)
  - `/Users/mike/Work/milan project/src-v3/systems/MapPathfinder.js` L1-70 (`bfsShortestPerCastle`, `reconstructPath`)
- **Fichiers Unity** :
  - `Assets/Scripts/Systems/GridCoords.cs` — `static class` :
    - Consts `GRASS='0'`, `PATH='1'`, `PORTAL='P'`, `CASTLE='C'`. `Walkable = { '1','P','C' }`.
    - `CellToWorld(int col, int row, int gridW, int gridH, float cellSize)` → Vector3 plan XY origine centrée, Y inversé : `x = (col - (gridW-1)/2f) * cellSize ; y = -((row - (gridH-1)/2f) * cellSize) ; z = 0;`
    - `WorldToCell(Vector3, int gridW, int gridH, float cellSize)` → Vector2Int (inverse).
  - `Assets/Scripts/Systems/GridData.cs` — POCO :
    - Fields : `int width, height; float cellSize; char[,] cells; List<Vector2Int> portals; List<Vector2Int> castles;`
    - `static GridData Parse(LevelData)` — itère `mapRows[]`, popule cells/portals/castles.
  - `Assets/Scripts/Systems/PathManager.cs` — MonoBehaviour singleton :
    - `public List<Vector3> Waypoints { get; private set; }` ; `public GridData Grid { get; private set; }`.
    - `Awake()` : récupère `LevelRunner.Instance.CurrentLevel`, parse grid via `GridData.Parse`, run BFS, populate `Waypoints` (centre cells path en world).
    - `List<Vector2Int> Bfs(Vector2Int from, Vector2Int to)` — port `MapPathfinder.bfsShortestPerCastle`. `Queue<Vector2Int>` + `Dictionary<Vector2Int, Vector2Int> parent`.
    - `OnDrawGizmos()` : draw cells walkable + waypoint polyline (debug visuel critique).
  - **Test data** : `W1-1.asset` mapRows = 7×13 single path :
    ```
    "0000000000000",
    "P11111111111C",
    "0000000000000",
    "0000000000000",
    "0000000000000",
    "0000000000000",
    "0000000000000",
    ```
- **Gotchas** : Convention `mapRows[row][col]`. Convention Y-flip pour row=0 en haut.
- **Commits** :
  - `feat(systems): add GridCoords helper + GridData parser (port from MapGrid.js)`
  - `feat(systems): add PathManager with BFS portal→castle (port from MapPathfinder.js)`
- **Verification** : `run_play_mode` 5s. `read_console` log `[PathManager] grid 13×7, 13 waypoints from (0,1) to (12,1)`. Scene view + Gizmos = polyline visible.
- **Estimation** : 2 commits, ~50 min.

---

### MIGRATE-POC-04 — Enemy spawn + movement (WaveManager + Enemy)

- **Goal** : Spawn enemies par waves, suivre waypoints jusqu'au castle.
- **Deps** : MIGRATE-POC-03.
- **Source Phaser** :
  - `/Users/mike/Work/milan project/src-v3/systems/LevelRunner.js` L83-104 (`_initWave`), L480-590 (tick/spawn loop).
  - `/Users/mike/Work/milan project/src-v3/entities/Enemy.js` L53-120 (ENEMY_TYPES shape, ignorer visual/anim Three.js).
- **Fichiers Unity** :
  - `Assets/Scripts/Entities/Enemy.cs` — MonoBehaviour :
    - Fields : `EnemyType cfg; float hp; int currentWaypoint;`
    - `Init(EnemyType)` : set hp, cfg, bodyColor sur SpriteRenderer, scale.
    - `Update()` : move vers `PathManager.Instance.GetWaypoint(currentWaypoint)` à `cfg.speed * Time.deltaTime`. Si distance < 0.05 → `currentWaypoint++`. Si dernier atteint → `Castle.Instance.TakeDamage(cfg.damage); Destroy(gameObject);` (POC-06).
    - `TakeDamage(float)` : `hp -= dmg; if (hp <= 0) { Economy.Instance.AddGold(cfg.reward); Destroy(gameObject); }`.
    - `[RequireComponent(typeof(SpriteRenderer))]` + `[RequireComponent(typeof(CircleCollider2D))]`.
  - `Assets/Scripts/Systems/WaveManager.cs` — MonoBehaviour singleton :
    - Fields : `[SerializeField] GameObject enemyPrefab; int currentWaveIdx; float spawnTimer; Queue<EnemyType> pendingSpawns; List<Enemy> activeEnemies;`
    - `Start()` : `BeginWave(0)`.
    - `BeginWave(int)` : build queue depuis `LevelData.waves[idx].entries` (Fisher-Yates shuffle `System.Random`).
    - `Update()` : `spawnTimer -= Time.deltaTime; if (spawnTimer <= 0 && pendingSpawns.Count > 0) SpawnEnemy(pendingSpawns.Dequeue());`. Si queue vide + tous dead → `OnWaveCompleted()` → next wave après `breakMs`.
    - `SpawnEnemy(EnemyType)` : `Instantiate(prefab, PathManager.Instance.GetWaypoint(0), Q.identity).GetComponent<Enemy>().Init(type);` ; tracker dans `activeEnemies`.
    - Events : `OnWaveStart(int)`, `OnAllWavesCompleted`.
  - **Prefab** : `Assets/Prefabs/Enemies/Enemy.prefab` — SpriteRenderer (placeholder sprite) + CircleCollider2D trigger r=0.4 + Enemy.cs.
- **Choix** : 1 prefab polymorphique config via SO. Pas de pool POC. `activeEnemies` exposé `IReadOnlyList` pour Tower targeting POC-05.
- **Gotchas** : `PathManager.Instance` cached `Awake()` côté Enemy. `activeEnemies.RemoveAll(e => e == null)` 1×/frame ou event `OnEnemyDied`. Speed sanity : `cellSize=1.0` → `speed=1.2` → 1.2 units/sec.
- **Commits** :
  - `feat(entities): add Enemy MonoBehaviour with waypoint-following movement`
  - `feat(systems): add WaveManager spawning enemies from LevelData.waves`
  - `chore: create Enemy.prefab with SpriteRenderer + CircleCollider2D`
- **Verification** : `run_play_mode` 30s. Enemies sprites colorés traversent du portal au castle, 3 waves successives. `read_console` log spawn count.
- **Estimation** : 3 commits, ~75 min.

---

### MIGRATE-POC-05 — Tower placement + targeting + shooting

- **Goal** : Click-to-place archer sur cell GRASS, acquire target la plus avancée, tirer projectile homing → damage.
- **Deps** : MIGRATE-POC-04.
- **Source Phaser** :
  - `/Users/mike/Work/milan project/src-v3/entities/Tower.js` L127-300 (`_acquireTarget`, `tick`, `fire`).
  - `/Users/mike/Work/milan project/src-v3/entities/BuildPoint.js` (placement validation, brièvement).
- **Fichiers Unity** :
  - `Assets/Scripts/Entities/Tower.cs` — MonoBehaviour :
    - Fields : `TowerType cfg; float cooldown; Enemy target;`
    - `Init(TowerType)` : assign cfg, set range collider, sprite color.
    - `Update()` :
      1. `cooldown -= Time.deltaTime;`
      2. Si `target == null || dead || sqrDist > range²` → `target = AcquireTarget();`
      3. Si `target != null && cooldown <= 0` → `Fire(target); cooldown = cfg.fireRateMs / 1000f;`
    - `AcquireTarget()` : iterate `WaveManager.Instance.activeEnemies`, pick `enemy.currentWaypoint` max dans range. Pattern "first" (KR/BTD6).
    - `Fire(Enemy)` : `Instantiate(projectilePrefab, transform.position, Q.identity).GetComponent<Projectile>().Init(target, cfg.damage, cfg.projectileSpeed);`
  - `Assets/Scripts/Entities/Projectile.cs` — MonoBehaviour homing :
    - Fields : `Enemy target; float damage; float speed;`
    - `Update()` : si `target == null` → Destroy. Else move vers `target.transform.position` à `speed`. Si distance < 0.1 → `target.TakeDamage(damage); Destroy(gameObject);`.
  - `Assets/Scripts/Systems/PlacementController.cs` — MonoBehaviour sur `Systems` :
    - `Input.GetMouseButtonDown(0)` → `Camera.main.ScreenToWorldPoint(Input.mousePosition)` → `GridCoords.WorldToCell()` → si GRASS + gold suffisant → `Economy.TrySpend(cost); Instantiate(towerPrefab).Init(archerSO);`.
    - POC : 1 seul tower type via `[SerializeField] TowerType selectedTowerType;` Inspector. Pas de menu pick.
  - **Prefabs** :
    - `Assets/Prefabs/Towers/Tower.prefab` (SpriteRenderer + CircleCollider2D + Tower.cs).
    - `Assets/Prefabs/Projectile.prefab` (SpriteRenderer petit + Projectile.cs).
- **Choix** : Targeting "first" (avancement max). Homing simple (suit target runtime). Pas de quadtree/spatial index.
- **Gotchas** : `target` typé `Enemy` direct (pas GetComponent en Update). `sqrMagnitude vs range²` (skip sqrt hot path). `Camera.main` cached dans Awake (pas chaque frame). EventSystem filtre clicks UI (OK en POC).
- **Commits** :
  - `feat(entities): add Tower MonoBehaviour with first-target acquisition + firing`
  - `feat(entities): add Projectile MonoBehaviour with homing movement + damage on hit`
  - `feat(systems): add PlacementController for click-to-place tower on grass cells`
  - `chore: create Tower.prefab + Projectile.prefab`
- **Verification** : `run_play_mode` 60s. Click grass → tower spawn → enemies in range → projectiles tirés → kills → gold up. Logs fire count, kill count.
- **Estimation** : 3 commits, ~90 min.

---

### MIGRATE-POC-06 — Economy + castle HP + game over

- **Goal** : Track gold + castle HP, game over si HP=0, victory si toutes waves clear.
- **Deps** : MIGRATE-POC-05.
- **Source Phaser** :
  - `/Users/mike/Work/milan project/src-v3/systems/LevelRunner.js` L27-160 (init, `loadCastles`, `castleHP` getter).
  - `/Users/mike/Work/milan project/src-v3/entities/Castle.js` (intégral, court).
- **Fichiers Unity** :
  - `Assets/Scripts/Systems/Economy.cs` — MonoBehaviour singleton :
    - `int Gold { get; private set; }` ; event `Action<int> OnGoldChanged;`
    - `Start()` : `Gold = LevelRunner.Instance.CurrentLevel.startCoins; OnGoldChanged?.Invoke(Gold);`
    - `bool TrySpend(int)` ; `void AddGold(int)`.
  - `Assets/Scripts/Entities/Castle.cs` — MonoBehaviour singleton :
    - `int HP { get; private set; } ; int HPMax { get; private set; }` ; events `OnHPChanged(int,int)`, `OnDestroyed`.
    - `Start()` : HP = HPMax = `LevelRunner.Instance.CurrentLevel.castleHP`.
    - `TakeDamage(int)` : `HP -= dmg`. Si HP ≤ 0 → `OnDestroyed` → `LevelRunner.SetState(GameOver);`.
  - `Assets/Scripts/Systems/LevelRunner.cs` — MonoBehaviour singleton ORCHESTRATEUR :
    - `enum GameState { Play, GameOver, Victory }` ; `GameState State { get; private set; }`.
    - `[SerializeField] LevelData currentLevel;` ; `LevelData CurrentLevel => currentLevel;`
    - `Awake()` : singleton ; `Time.timeScale = 1f;`
    - `SetState(GameState)` : update + `Time.timeScale = (s == Play) ? 1f : 0f;` + event `OnStateChanged`.
    - `Start()` : `WaveManager.Instance.OnAllWavesCompleted += () => SetState(Victory);`
  - **Modif Enemy.cs** : dernier waypoint → `Castle.Instance.TakeDamage(cfg.damage);` au lieu du placeholder.
- **Gotchas** : Script Execution Order — `LevelRunner: -100` (Edit > Project Settings) pour qu'il Awake en premier. Defensive `?.Instance?.CurrentLevel`.
- **Commits** :
  - `feat(systems): add Economy singleton with Gold + TrySpend/AddGold + OnGoldChanged event`
  - `feat(entities): add Castle singleton with HP + TakeDamage + OnHPChanged/OnDestroyed events`
  - `feat(systems): add LevelRunner orchestrator with GameState + Time.timeScale pause`
- **Verification** : `run_play_mode` complet (3 waves). Scenario 1 : pose archer → win → Victory. Scenario 2 : pas de tower → castle 0 HP → GameOver. Logs gold/HP delta.
- **Estimation** : 3 commits, ~45 min.

---

### MIGRATE-POC-07 — HUD minimal (uGUI)

- **Goal** : Overlay Gold/Wave/HP + panels GameOver/Victory + bouton Restart.
- **Deps** : MIGRATE-POC-06.
- **Source Phaser** : `/Users/mike/Work/milan project/index.html` (référence layout pills top-left/right, pas port 1:1).
- **Fichiers Unity** :
  - `Assets/Scripts/UI/HudController.cs` — MonoBehaviour :
    - Fields : `[SerializeField] TextMeshProUGUI goldLabel, waveLabel, hpLabel; [SerializeField] GameObject gameOverPanel, victoryPanel; [SerializeField] Button restartButton;`
    - `Start()` : subscribe events Economy/Castle/WaveManager/LevelRunner + lire valeur courante après subscribe (anti race init). Bind `restartButton.onClick → SceneManager.LoadScene(0)`.
    - Méthodes `UpdateGold/UpdateHP/UpdateWave/UpdateState`.
    - `OnDestroy()` : unsubscribe (anti leak).
  - **Scene setup** (via `manage_gameobject`) : Canvas Screen Space Overlay + 3 TMP labels (top-L/C/R) + 2 panels désactivés défaut.
  - **TMP Essentials** : `execute_menu_item Window/TextMeshPro/Import TMP Essential Resources` (Sonnet une fois).
- **Choix** : uGUI Canvas (cf Q-A). TextMeshProUGUI (pas Text legacy). `SceneManager.LoadScene` pour restart.
- **Gotchas** : Order init UI vs systems — lire valeur après subscribe. TMP Essentials requis. EventSystem auto-créé.
- **Commits** :
  - `feat(ui): add HudController with Gold/Wave/HP labels + game over/victory panels`
  - `chore: setup Main.unity Canvas with TMP labels + restart button bindings`
- **Verification** : `run_play_mode` 60s. Labels update temps réel. GameOver panel rouge → Restart → reload. Victory panel vert.
- **Estimation** : 2 commits, ~50 min.

---

### MIGRATE-POC-08 — WebGL build + deploy

- **Goal** : Build WebGL fonctionnel + script Editor + (option) deploy `/v6/`.
- **Deps** : MIGRATE-POC-07.
- **Source Phaser** : aucun.
- **Fichiers Unity** :
  - `Assets/Editor/BuildScript.cs` (folder `Editor` convention pour code Editor-only) :
    ```csharp
    public static class BuildScript {
        [MenuItem("CrowdDefense/Build WebGL")]
        public static void BuildWebGL() {
            var opts = new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = "Builds/WebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            BuildPipeline.BuildPlayer(opts);
        }
    }
    ```
  - `.gitignore` audit — confirmer `/Builds/` ignoré.
- **WebGL Player Settings** (via Inspector + `manage_editor`) :
  - Compression Format : Brotli (~3-5 MB vs Gzip 5-8 MB).
  - Decompression Fallback : true (http:// local sans nginx headers).
  - WebGL Memory Size : 256 MB.
  - Color Space : Linear.
- **Choix** : Build manuel via menu Editor. Pas de CI/CD POC (Phase 3 décidera). Deploy `/v6/` optionnel POC.
- **Gotchas** : WebGL build = 5-15 min première fois (IL2CPP + Brotli) → `run_in_background` + `Monitor`. CORS local : `python3 -m http.server 8000` dans `Builds/WebGL/` (pas `file://`). `GITHUB_TOKEN` invalid (STATUS.md L7) → `unset GITHUB_TOKEN && gh ...`.
- **Commits** :
  - `feat(build): add Editor BuildScript with BuildWebGL method`
  - `chore: configure WebGL Player Settings (Brotli, Linear color space, 256MB memory)`
  - `docs: add build instructions to README` (optionnel)
- **Verification** : `execute_menu_item CrowdDefense/Build WebGL`. Vérifier `Builds/WebGL/index.html` + `Build/` dir. Browse `http://localhost:8000` (manuel Mike).
- **Estimation** : 2-3 commits, ~40 min (10 min build background).

---

## Récap effort total

| Ticket | Commits | Temps Sonnet |
|---|---|---|
| POC-01 scaffold | 2 | 15 min |
| POC-02 SO defs | 2 | 40 min |
| POC-03 grid+path | 2 | 50 min |
| POC-04 enemy+wave | 3 | 75 min |
| POC-05 tower+proj+place | 4 | 90 min |
| POC-06 economy+castle+runner | 3 | 45 min |
| POC-07 HUD | 2 | 50 min |
| POC-08 WebGL build | 2-3 | 40 min |
| **TOTAL** | **20-21** | **~6.75 h** |

---

## Décisions finales validées Mike (2026-05-11)

Mike a choisi le scope **full fidélité** sur les 4 questions structurantes. Overrides finaux :

1. **Q-A UI → UI Toolkit direct** (pas uGUI). USS + UXML + bindings runtime. POC-07 : 50 → 70 min. Mike apprend UI Toolkit dès POC.

2. **Q-E Render → 2.5D top-down 3D primitives** (pas 2D sprites). MeshRenderer + Cube/Sphere/Cylinder built-in Unity + material colors. Camera perspective tilted (~60° X, pos `(0, 12, -6)`). Directional Light pertinent. POC-01 : 15 → 45 min. POC-02/04/05 : SpriteRenderer remplacé par MeshRenderer.

3. **Map W1-1 → 7×15 fidèle Phaser** (pas 7×13 single path). Mike accepte cell types spéciaux du source `world1-1.js` : `~` water, `^` lava, `B` bridge si présents. Multi-portail si stream. POC-02 +15 min, POC-03 +30 min (multi-BFS + walkable rules).

4. **POC-08 → Inclure deploy `/v6/`**. GitHub Pages branch `gh-pages` setup + push manuel avec `unset GITHUB_TOKEN`. Index.html relative paths. POC-08 : 40 → 70 min.

5. **Wave sizing → 35/76/87 fidèle Phaser** + dev cheat `Time.timeScale = 10f` (touche Tab) dans `LevelRunner.Update()` pour playtest accéléré. Pas de wave réduction. Détail tranché autonome (Mike : "tu dois faire en autonomie").

### Impact coord system 2.5D

**Q-F update** : plan **XZ** (Unity 3D standard, Y=up), pas XY (qui était la convention 2D précédente).

`GridCoords.CellToWorld(col, row, gridW, gridH, cellSize)` :
```csharp
x = (col - (gridW-1)/2f) * cellSize
z = -(row - (gridH-1)/2f) * cellSize  // row inversé pour row=0 en haut visuel
y = 0  // tous gameplay objects au sol
return new Vector3(x, y, z)
```

Tower / Enemy / Castle : `transform.position` Y légèrement positif (Y=0.5 pour Cube de taille 1) pour ne pas clipper le sol.

### Nouveau total effort

| Ticket | Commits | Temps Sonnet révisé |
|---|---|---|
| POC-01 scaffold 3D | 2 | 45 min |
| POC-02 SO defs + mesh materials | 2 | 55 min |
| POC-03 grid+multi-path BFS | 2 | 80 min |
| POC-04 enemy 3D + wave | 3 | 80 min |
| POC-05 tower+proj 3D + raycast place | 4 | 95 min |
| POC-06 economy+castle+runner + Time.timeScale cheat | 3 | 50 min |
| POC-07 HUD UI Toolkit USS/UXML | 2 | 70 min |
| POC-08 WebGL build + deploy /v6/ | 3 | 70 min |
| **TOTAL** | **21-22** | **~9 h** |

---

## Critical Files for Implementation

Les 5 fichiers structurants :
- `Assets/Scripts/Data/LevelData.cs` (data root)
- `Assets/Scripts/Systems/LevelRunner.cs` (orchestrateur)
- `Assets/Scripts/Systems/PathManager.cs` (BFS + waypoints)
- `Assets/Scripts/Entities/Enemy.cs` (movement + damage + reward)
- `Assets/Scripts/Entities/Tower.cs` (targeting + firing)
