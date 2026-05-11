#nullable enable
using UnityEngine;
using UnityEngine.Pool;
using CrowdDefense.Common;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    [DefaultExecutionOrder(-50)]
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
            GameObject go;
            if (projectilePrefab != null)
            {
                go = Instantiate(projectilePrefab);
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[ProjectilePool] projectilePrefab is null — creating primitive sphere fallback");
#endif
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.localScale = Vector3.one * 0.18f;
                Object.Destroy(go.GetComponent<SphereCollider>());
                go.AddComponent<Projectile>();
            }

            var p = go.GetComponent<Projectile>();
            if (p == null)
                p = go.AddComponent<Projectile>();
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

        public int ActiveCount => pool?.CountActive ?? 0;

        public Projectile Get() => pool!.Get();

        public void Release(Projectile p) => pool!.Release(p);
    }
}
