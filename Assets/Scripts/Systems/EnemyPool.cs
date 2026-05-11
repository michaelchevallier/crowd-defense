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
    public class EnemyPool : MonoSingleton<EnemyPool>
    {
        [SerializeField] private GameObject? basePrefab;

        // Per-type sub-pools keyed by EnemyType.Id
        private readonly Dictionary<string, ObjectPool<Enemy>> _pools = new();

        // Spawn, position, and Init an enemy of the given type in one call.
        // Called by WaveManager instead of Get() + manual Init.
        public Enemy SpawnFromType(EnemyType type, Vector3 position, int pathIdx)
        {
            var pool = GetOrCreatePool(type.Id);
            var enemy = pool.Get();
            enemy._poolTypeId = type.Id;
            enemy.transform.position = position;
            enemy.transform.rotation = Quaternion.identity;
            enemy.Init(type, pathIdx);
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

        private ObjectPool<Enemy> GetOrCreatePool(string typeId)
        {
            if (_pools.TryGetValue(typeId, out var existing)) return existing;

            var newPool = new ObjectPool<Enemy>(
                createFunc: CreateEnemy,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnPoolDestroy,
                collectionCheck: false,
                defaultCapacity: 20,
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

        private static void OnGet(Enemy e) => e.gameObject.SetActive(true);

        private static void OnRelease(Enemy e)
        {
            e.StopAllCoroutines();
            e.gameObject.SetActive(false);
        }

        private static void OnPoolDestroy(Enemy e) => Destroy(e.gameObject);
    }
}
