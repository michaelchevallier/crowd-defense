# MIGRATE-POC-04 — Enemy 3D + WaveManager spawning

> Ticket 4/8 du Phase 1 POC. Spawn enemies par waves, follow waypoints, kill/damage.

## Type & Effort

- **Type** : feature-dev
- **Estimé** : 3 commits, ~80 min
- **Bloqué par** : POC-01, 02, 03 ✅
- **Branch** : `main` direct
- **Working dir** : `/Users/mike/Work/crowd-defense/`

## Objectif

3 composants gameplay :
1. `Enemy` MonoBehaviour 3D (MeshRenderer Capsule + material color depuis EnemyType, suit waypoints, HP, mort = reward gold)
2. `WaveManager` MonoBehaviour singleton (queue spawns par wave, transitions inter-waves avec breakMs, events public)
3. `Enemy.prefab` (créé via MCP)

**Note** : `Economy.AddGold` et `Castle.TakeDamage` n'existent pas encore (POC-06). En attendant, l'Enemy log `[Enemy] reached castle: dmg=X` au lieu d'appeler Castle, et `[Enemy] killed: reward=Y` au lieu d'AddGold. POC-06 remplacera les stubs.

## Source canonique

Lire AVANT :
1. `/Users/mike/Work/milan project/src-v3/systems/LevelRunner.js` lignes 83-104 (`_initWave` : shuffle Fisher-Yates, `_pendingSpawns`, `_spawnRate`, `_currentBreakMs`).
2. `/Users/mike/Work/milan project/src-v3/systems/LevelRunner.js` lignes 465-515 (`tick` : spawn timer + wave transition).
3. `/Users/mike/Work/milan project/src-v3/entities/Enemy.js` lignes 53-75 (juste shape ENEMY_TYPES, déjà connue).

## Décisions techniques

- **Enemy mesh** : `CapsuleCollider` + `CapsuleMesh` 3D primitive (hauteur ~1, rayon ~0.3), scale × EnemyType.scale. Y=0.5 pour reposer sur le sol Y=0.
- **Movement** : `Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime)`. Threshold 0.1 pour passer au waypoint suivant.
- **WaveManager.activeEnemies** : `List<Enemy>` public read pour Tower targeting POC-05.
- **Pas de pool POC** : `Instantiate`/`Destroy` direct.
- **Fisher-Yates shuffle** : `System.Random` (PAS `UnityEngine.Random` qui n'est pas thread-safe et change avec scene reload).
- **WaveManager singleton pattern** : même pattern PathManager (`static Instance` + Awake).

---

## Commit 1 — `feat(entities): add Enemy MonoBehaviour with 3D capsule + waypoint movement + HP`

### Fichier : `Assets/Scripts/Entities/Enemy.cs`

```csharp
#nullable enable
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Entities
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class Enemy : MonoBehaviour
    {
        private EnemyType? cfg;
        private float hp;
        private int currentWaypoint;
        private PathManager? path;

        public EnemyType? Config => cfg;
        public int CurrentWaypoint => currentWaypoint;
        public bool IsDead { get; private set; }

        public void Init(EnemyType type)
        {
            cfg = type;
            hp = type.Hp;
            currentWaypoint = 1; // 0 = spawn point, start moving toward 1
            transform.localScale = Vector3.one * type.Scale;

            var rend = GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rend.material.color = type.BodyColor;
            }

            var col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = 0.3f;
                col.height = 1f;
            }
        }

        private void Start()
        {
            path = PathManager.Instance;
            if (path == null || path.WaypointCount < 2)
            {
                Debug.LogError("[Enemy] No PathManager or path too short");
                Destroy(gameObject);
                return;
            }
            transform.position = path.GetWaypoint(0) + Vector3.up * 0.5f;
        }

        private void Update()
        {
            if (cfg == null || path == null || IsDead) return;
            if (currentWaypoint >= path.WaypointCount)
            {
                OnReachedCastle();
                return;
            }

            Vector3 target = path.GetWaypoint(currentWaypoint) + Vector3.up * 0.5f;
            transform.position = Vector3.MoveTowards(transform.position, target, cfg.Speed * Time.deltaTime);

            if ((transform.position - target).sqrMagnitude < 0.01f)
                currentWaypoint++;
        }

        public void TakeDamage(float dmg)
        {
            if (IsDead) return;
            hp -= dmg;
            if (hp <= 0f)
            {
                IsDead = true;
                int reward = cfg?.Reward ?? 0;
#if UNITY_EDITOR
                Debug.Log($"[Enemy] killed type={cfg?.Id} reward={reward}");
#endif
                // TODO POC-06 : Economy.Instance.AddGold(reward);
                WaveManager.Instance?.NotifyEnemyDied(this);
                Destroy(gameObject);
            }
        }

        private void OnReachedCastle()
        {
            int dmg = cfg?.Damage ?? 0;
#if UNITY_EDITOR
            Debug.Log($"[Enemy] reached castle type={cfg?.Id} dmg={dmg}");
#endif
            // TODO POC-06 : Castle.Instance.TakeDamage(dmg);
            WaveManager.Instance?.NotifyEnemyDied(this);
            Destroy(gameObject);
        }
    }
}
```

### Process commit 1

1. Write fichier.
2. `mcp__UnityMCP__refresh_unity` + `read_console` no errors.
3. Note : `WaveManager.NotifyEnemyDied` n'existe pas encore — fichier ne compile pas tant que POC-04 commit 2 pas fait. **Skipper le commit 1 isolated** ou **combiner avec commit 2** (recommandé : 1 seul commit `feat(entities+systems): add Enemy + WaveManager with spawn loop`).

→ **Refactor** : merge commits 1+2 en un seul commit.

---

## Commit 1 (révisé) — `feat(gameplay): add Enemy + WaveManager with 3D spawn + wave transition loop`

### Fichier additionnel : `Assets/Scripts/Systems/WaveManager.cs`

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager? Instance { get; private set; }

        [SerializeField] private LevelData? levelData;
        [SerializeField] private GameObject? enemyPrefab;

        private int currentWaveIdx = 0;
        private float spawnTimerMs = 0f;
        private float breakTimerMs = 0f;
        private bool waveActive = false;
        private Queue<EnemyType> pendingSpawns = new();
        private List<Enemy> activeEnemies = new();

        public IReadOnlyList<Enemy> ActiveEnemies => activeEnemies;
        public int CurrentWaveIdx => currentWaveIdx;
        public int WaveDisplayNumber => currentWaveIdx + 1;
        public int TotalWaves => levelData?.Waves.Count ?? 0;

        public event Action<int>? OnWaveStart;
        public event Action<int>? OnWaveCleared;
        public event Action? OnAllWavesCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (levelData == null || levelData.Waves.Count == 0)
            {
                Debug.LogError("[WaveManager] No LevelData or no waves");
                return;
            }
            BeginWave(0);
        }

        private void BeginWave(int idx)
        {
            currentWaveIdx = idx;
            var wave = levelData!.Waves[idx];
            var list = new List<EnemyType>();
            foreach (var entry in wave.entries)
            {
                if (entry.type == null) continue;
                for (int i = 0; i < entry.count; i++) list.Add(entry.type);
            }
            // Fisher-Yates
            var rng = new System.Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            pendingSpawns = new Queue<EnemyType>(list);
            spawnTimerMs = 0f;
            waveActive = true;
#if UNITY_EDITOR
            Debug.Log($"[WaveManager] Wave {idx + 1}/{TotalWaves} start : {list.Count} enemies, spawnRate {wave.spawnRateMs}ms");
#endif
            OnWaveStart?.Invoke(idx);
        }

        private void Update()
        {
            if (levelData == null) return;
            float dtMs = Time.deltaTime * 1000f;

            if (waveActive)
            {
                spawnTimerMs += dtMs;
                var wave = levelData.Waves[currentWaveIdx];
                if (spawnTimerMs >= wave.spawnRateMs && pendingSpawns.Count > 0)
                {
                    spawnTimerMs = 0f;
                    SpawnEnemy(pendingSpawns.Dequeue());
                }
                if (pendingSpawns.Count == 0 && activeEnemies.Count == 0)
                {
                    waveActive = false;
                    breakTimerMs = 0f;
#if UNITY_EDITOR
                    Debug.Log($"[WaveManager] Wave {currentWaveIdx + 1} cleared");
#endif
                    OnWaveCleared?.Invoke(currentWaveIdx);
                }
            }
            else
            {
                breakTimerMs += dtMs;
                int breakMs = levelData.Waves[currentWaveIdx].breakMs;
                if (breakMs <= 0) breakMs = 4000;
                if (breakTimerMs >= breakMs)
                {
                    if (currentWaveIdx + 1 < levelData.Waves.Count)
                    {
                        BeginWave(currentWaveIdx + 1);
                    }
                    else
                    {
#if UNITY_EDITOR
                        Debug.Log("[WaveManager] All waves completed — victory");
#endif
                        OnAllWavesCompleted?.Invoke();
                        enabled = false;
                    }
                }
            }
        }

        private void SpawnEnemy(EnemyType type)
        {
            if (enemyPrefab == null || PathManager.Instance == null) return;
            Vector3 spawnPos = PathManager.Instance.GetWaypoint(0) + Vector3.up * 0.5f;
            var go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            var enemy = go.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Init(type);
                activeEnemies.Add(enemy);
            }
        }

        public void NotifyEnemyDied(Enemy e) => activeEnemies.Remove(e);
    }
}
```

### Prefab à créer : `Assets/Prefabs/Enemies/Enemy.prefab`

Composition :
- GameObject racine "Enemy"
- MeshFilter avec mesh `Capsule` (Unity primitive built-in)
- MeshRenderer avec material par défaut (sera override par Enemy.Init)
- CapsuleCollider (isTrigger=true, radius=0.3, height=1)
- Enemy.cs component

**Création via MCP** :
1. `mcp__UnityMCP__manage_gameobject action=create name="Enemy" primitive_type=Capsule` (créer GO temporaire dans scene)
2. Configure components (CapsuleCollider trigger, ajouter Enemy.cs script)
3. `mcp__UnityMCP__manage_prefabs action=create source_gameobject="Enemy" prefab_path="Assets/Prefabs/Enemies/Enemy.prefab"` (ou tool équivalent — vérifie la doc).
4. Si pas de tool prefab dédié → fallback `execute_code` :
   ```csharp
   var go = GameObject.Find("Enemy");
   var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Enemies/Enemy.prefab");
   GameObject.DestroyImmediate(go);
   ```

### Scene setup additionnel

- Créer GameObject "WaveManager" sous Systems.
- Ajouter component WaveManager.
- Assign `levelData` = W1-1.asset, `enemyPrefab` = Enemy.prefab (via execute_code SerializedObject si MCP direct ne marche pas).
- Save scene.

### Process commit unique POC-04

1. Write `Enemy.cs` + `WaveManager.cs`.
2. `refresh_unity` + verify compile.
3. Créer `Enemy.prefab` via MCP.
4. Setup `WaveManager` GameObject sous Systems + refs.
5. Save scene.
6. Test play mode 30s : `mcp__UnityMCP__manage_editor action=play` → wait → `read_console` → vérifier logs spawn et wave transitions → stop.
7. `git add Assets/Scripts/Entities/ Assets/Scripts/Systems/WaveManager.cs Assets/Prefabs/Enemies/ Assets/Scenes/` + commit.
8. Push.

**Note Play mode test** : avec 35 enemies wave 1 + spawnRate 900ms, le spawn complet prend 35×0.9s = ~32s. Si tu veux test plus rapide en dev, tu peux soit baisser temporairement le spawnRate dans `W1-1.asset` (revert avant commit) soit attendre 30s.

---

## Verification finale

```bash
find Assets/Scripts/Entities -name "*.cs" | wc -l   # 1 (Enemy)
find Assets/Scripts/Systems -name "*.cs" | wc -l    # 4 (GridCoords, GridData, PathManager, WaveManager)
find Assets/Prefabs/Enemies -name "*.prefab" | wc -l # 1
```

Via MCP play mode :
- `read_console` log `[WaveManager] Wave 1/4 start : 35 enemies, spawnRate 900ms`
- Capsules colorées (rouge brun Basic) apparaissent au portal, suivent le path L-shape vers castle.
- Log `[Enemy] reached castle type=basic dmg=5` quand un enemy atteint la fin.
- Après 35 enemies clear → `[WaveManager] Wave 1 cleared` + transition wave 2 après 4500ms.

**Critères succès** :
- 2 .cs files + 1 prefab + scene updated
- Enemies suivent path linéaire correctement (visuel)
- Logs spawn/wave transition corrects
- 1 commit pushed (regroupant Enemy + WaveManager pour éviter état non-compile)

## Pièges anticipés

1. **URP vs Standard Shader** : Unity 6 LTS utilise URP par défaut. Le code essaie `Universal Render Pipeline/Lit` puis fallback `Standard`. Si les materials apparaissent rose magenta → shader manquant, vérifier que URP est dispo.
2. **CapsuleCollider isTrigger** : sans trigger, les enemies se bloqueraient mutuellement. Set isTrigger=true.
3. **Refs assignment WaveManager** : `levelData` + `enemyPrefab` via fallback execute_code SerializedObject si MCP direct échoue.
4. **PathManager.Instance null** : si Enemy.Start trouve null, c'est que WaveManager spawn avant PathManager.Awake. Script Execution Order si problème (PathManager: -200, WaveManager: -100).
5. **`new Material()`** chaque enemy = leak. POC = OK (288 enemies max, Destroy nettoie). Phase 2 : material partagé MaterialPropertyBlock.

## Quand fini

1 commit pushed sur main, termine ton tour.
