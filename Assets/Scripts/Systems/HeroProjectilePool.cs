#nullable enable
using UnityEngine;
using UnityEngine.Pool;
using CrowdDefense.Common;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Object pool for HeroProjectile instances.
    /// Place on a scene GameObject alongside ProjectilePool.
    /// Assign heroProjectilePrefab — a prefab with HeroProjectile + MeshRenderer (small sphere).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class HeroProjectilePool : MonoSingleton<HeroProjectilePool>
    {
        [SerializeField] private GameObject? heroProjectilePrefab;

        private ObjectPool<HeroProjectile>? _pool;

        protected override void OnAwakeSingleton()
        {
            _pool = new ObjectPool<HeroProjectile>(
                createFunc:       CreateProjectile,
                actionOnGet:      OnGet,
                actionOnRelease:  OnRelease,
                actionOnDestroy:  OnPoolDestroy,
                collectionCheck:  false,
                defaultCapacity:  30,
                maxSize:          200
            );
        }

        private HeroProjectile CreateProjectile()
        {
            GameObject go;
            if (heroProjectilePrefab != null)
            {
                go = Instantiate(heroProjectilePrefab);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.localScale = Vector3.one * 0.22f;
                Object.Destroy(go.GetComponent<Collider>());
                var _heroProjMR = go.GetComponent<MeshRenderer>();
                if (_heroProjMR != null)
                {
                    var _heroProjMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? ShaderUtil.GetUnlitShader());
                    if (_heroProjMat.HasProperty("_BaseColor")) _heroProjMat.SetColor("_BaseColor", new Color(0.9f, 0.9f, 0.2f));
                    else _heroProjMat.color = new Color(0.9f, 0.9f, 0.2f);
                    _heroProjMR.material = _heroProjMat;
                }
            }

            go.name = "HeroProj";
            var proj = go.GetComponent<HeroProjectile>() ?? go.AddComponent<HeroProjectile>();
            proj.Pool = this;
            go.SetActive(false);
            return proj;
        }

        private static void OnGet(HeroProjectile p)
        {
            p.ResetState();
            p.gameObject.SetActive(true);
        }

        private static void OnRelease(HeroProjectile p) => p.gameObject.SetActive(false);

        private static void OnPoolDestroy(HeroProjectile p) => Destroy(p.gameObject);

        /// <summary>
        /// Retrieves a pooled HeroProjectile, placed at <paramref name="position"/>.
        /// Call HeroProjectile.Init immediately after.
        /// </summary>
        public HeroProjectile Get(Vector3 position)
        {
            var proj = _pool!.Get();
            proj.transform.position = position;
            return proj;
        }

        public void Return(HeroProjectile p) => _pool!.Release(p);
    }
}
