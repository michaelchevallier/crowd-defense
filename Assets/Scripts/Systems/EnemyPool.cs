#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Visual;

namespace CrowdDefense.Systems
{
    [DefaultExecutionOrder(-50)]
    public class EnemyPool : MonoSingleton<EnemyPool>
    {
        [SerializeField] private GameObject? basePrefab;

        // Per-type sub-pools keyed by EnemyType.Id
        private readonly Dictionary<string, ObjectPool<Enemy>> _pools = new();

        // Flat list of all currently active (live) enemies — used by EnemyPathingSystem
        private readonly List<Enemy> _active = new();
        public IReadOnlyList<Enemy> ActiveEnemies => _active;
        public int ActiveCount => _active.Count;

        // Spawn, position, and Init an enemy of the given type in one call.
        // Called by WaveManager instead of Get() + manual Init.
        public Enemy SpawnFromType(EnemyType type, Vector3 position, int pathIdx, float endlessMul = 1f, CrowdDefense.Data.EnemyVariant variant = CrowdDefense.Data.EnemyVariant.Normal)
        {
            // Fallback: if PathManager has no paths, inject a hardcoded straight-line path so
            // enemies don't silently self-release and the bug is visible in play mode.
            var pm = PathManager.Instance;
            if (pm != null && pm.Paths.Count == 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[EnemyPool] PathManager has no paths — injecting fallback straight path (0,0,0)→(10,0,0)");
#endif
                pm.InjectFallbackPath();
            }

            var pool = GetOrCreatePool(type.Id);
            var enemy = pool.Get();
            enemy._poolTypeId = type.Id;
            enemy.transform.position = position;
            enemy.transform.rotation = Quaternion.identity;
            VfxPool.Instance?.SpawnPortal(position);
            enemy.Init(type, pathIdx, endlessMul);
            if (variant != CrowdDefense.Data.EnemyVariant.Normal) enemy.ApplyVariant(variant);
            int waveIdx = WaveManager.Instance?.CurrentWaveIdx ?? 0;
            bool eliteEligible = waveIdx >= 4 && !(type.IsBoss || type.IsMidBoss);
            if (eliteEligible && Random.value < 0.10f) enemy.ApplyElite();

#if UNITY_EDITOR
            Vector3 target = (pm != null && pm.WaypointCountOnPath(pathIdx) > 1)
                ? pm.GetWaypointOnPath(pathIdx, 1)
                : position + Vector3.forward * 10f;
            Debug.Log($"[EnemyPool] SpawnFromType id={type.Id} pos={position} pathIdx={pathIdx} firstTarget={target}");
#endif
            return enemy;
        }

        // Route release back to the correct per-type sub-pool.
        // Called by Enemy.ReleaseToPool via the back-linked EnemyPool reference.
        public void ReleaseTyped(Enemy e)
        {
            if (_pools.TryGetValue(e._poolTypeId, out var pool))
                pool.Release(e);
            else
                Destroy(e.gameObject);
        }

        // Legacy: kept so existing code that calls Release(e) still compiles.
        public void Release(Enemy e) => ReleaseTyped(e);

        // Prewarm a per-type sub-pool before a wave starts to avoid mid-wave Instantiate spikes.
        // count is capped internally: ≤ 2 for boss-tagged types, ≤ 30 for regular mobs.
        public void PrewarmType(EnemyType type, int requestedCount)
        {
            bool isBoss = type.Id.Contains("boss", System.StringComparison.OrdinalIgnoreCase);
            int cap = isBoss ? 2 : 30;
            int count = Mathf.Min(requestedCount, cap);

            var pool = GetOrCreatePool(type.Id);
            // Allocate count instances into the pool without activating them in-scene.
            var tmp = new Enemy[count];
            for (int i = 0; i < count; i++) tmp[i] = pool.Get();
            for (int i = 0; i < count; i++) pool.Release(tmp[i]);
        }

        private ObjectPool<Enemy> GetOrCreatePool(string typeId)
        {
            if (_pools.TryGetValue(typeId, out var existing)) return existing;

            var newPool = new ObjectPool<Enemy>(
                createFunc: CreateEnemy,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnPoolDestroy,
                collectionCheck: false,
                defaultCapacity: 50,
                maxSize: 100
            );
            _pools[typeId] = newPool;
            return newPool;
        }

        private Enemy CreateEnemy()
        {
            GameObject go;
            if (basePrefab != null)
            {
                go = Instantiate(basePrefab);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "Enemy_Fallback";
                go.AddComponent<Enemy>();
            }
            var e = go.GetComponent<Enemy>();
            e.SetPool(this);
            // Attach world-space HP bar if not already present (prefab may lack it)
            if (go.GetComponent<EnemyHpBar>() == null)
                go.AddComponent<EnemyHpBar>();
            return e;
        }

        private void OnGet(Enemy e)
        {
            // Hide root placeholder renderer before Init assigns the real mesh child,
            // preventing a 1-frame magenta flash (Default-Material on URP capsule).
            var rootRend = e.GetComponent<MeshRenderer>();
            if (rootRend != null) rootRend.enabled = false;
            e.gameObject.SetActive(true);
            _active.Add(e);
        }

        private void OnRelease(Enemy e)
        {
            e.StopAllCoroutines();
            e.gameObject.SetActive(false);
            _active.Remove(e);
        }

        private static void OnPoolDestroy(Enemy e) => Destroy(e.gameObject);
    }
}
