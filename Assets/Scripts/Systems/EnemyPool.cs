#nullable enable
using UnityEngine;
using UnityEngine.Pool;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    public class EnemyPool : MonoBehaviour
    {
        public static EnemyPool? Instance { get; private set; }

        [SerializeField] private GameObject? enemyPrefab;

        private ObjectPool<Enemy>? pool;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            pool = new ObjectPool<Enemy>(
                createFunc: CreateEnemy,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnPoolDestroy,
                collectionCheck: false,
                defaultCapacity: 50,
                maxSize: 200
            );
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private Enemy CreateEnemy()
        {
            var go = Instantiate(enemyPrefab!);
            var e = go.GetComponent<Enemy>();
            e.SetPool(this);
            return e;
        }

        private static void OnGet(Enemy e) => e.gameObject.SetActive(true);

        private static void OnRelease(Enemy e)
        {
            e.StopAllCoroutines();
            e.gameObject.SetActive(false);
        }

        private static void OnPoolDestroy(Enemy e) => Destroy(e.gameObject);

        public Enemy Get() => pool!.Get();

        public void Release(Enemy e) => pool!.Release(e);
    }
}
