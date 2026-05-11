#nullable enable
using UnityEngine;
using UnityEngine.Pool;
using CrowdDefense.Common;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    public class ProjectilePool : MonoSingleton<ProjectilePool>
    {
        [SerializeField] private GameObject? projectilePrefab;

        private ObjectPool<Projectile>? pool;

        protected override void OnAwakeSingleton()
        {
            pool = new ObjectPool<Projectile>(
                createFunc: CreateProjectile,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnPoolDestroy,
                collectionCheck: false,
                defaultCapacity: 30,
                maxSize: 100
            );
        }

        private Projectile CreateProjectile()
        {
            var go = Instantiate(projectilePrefab!);
            var p = go.GetComponent<Projectile>();
            p.SetPool(this);
            return p;
        }

        private static void OnGet(Projectile p) => p.gameObject.SetActive(true);

        private static void OnRelease(Projectile p)
        {
            p.StopAllCoroutines();
            p.gameObject.SetActive(false);
        }

        private static void OnPoolDestroy(Projectile p) => Destroy(p.gameObject);

        public Projectile Get() => pool!.Get();

        public void Release(Projectile p) => pool!.Release(p);
    }
}
