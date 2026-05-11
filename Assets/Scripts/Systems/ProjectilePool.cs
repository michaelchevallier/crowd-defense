#nullable enable
using UnityEngine;
using UnityEngine.Pool;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    public class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool? Instance { get; private set; }

        [SerializeField] private GameObject? projectilePrefab;

        private ObjectPool<Projectile>? pool;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
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

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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
