#nullable enable
using UnityEngine;
using UnityEngine.Pool;
using CrowdDefense.Common;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    public class EnemyPool : MonoSingleton<EnemyPool>
    {
        [SerializeField] private GameObject? enemyPrefab;

        private ObjectPool<Enemy>? pool;

        protected override void OnAwakeSingleton()
        {
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
