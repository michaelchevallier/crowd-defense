#nullable enable
using System.Collections.Generic;
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
        private bool _warnedNullPrefab;

        protected override void OnAwakeSingleton()
        {
            pool = new ObjectPool<Projectile>(
                createFunc: CreateProjectile,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnPoolDestroy,
                collectionCheck: false,
                defaultCapacity: 40,
                maxSize: 100
            );
            Prewarm(40);
        }

        private void Prewarm(int count)
        {
            var temp = new List<Projectile>(count);
            for (int i = 0; i < count; i++) temp.Add(Get());
            foreach (var p in temp) Release(p);
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
                if (!_warnedNullPrefab)
                {
                    Debug.LogWarning("[ProjectilePool] projectilePrefab is null — creating primitive sphere fallback (suppressing future occurrences)");
                    _warnedNullPrefab = true;
                }
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
