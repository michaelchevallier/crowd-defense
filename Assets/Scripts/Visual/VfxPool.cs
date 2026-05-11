#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using CrowdDefense.Common;
using CrowdDefense.UI;

namespace CrowdDefense.Visual
{
    // Port de Particles.js (Phaser pool 400 sprites radial) → Unity ObjectPool<ParticleSystem>.
    // Pools : Impact / Death / Explosion / CoinBurst / HitFlash.
    // Prefabs optionnels via Inspector ; si non assignés, génère des ParticleSystem procéduraux.
    // API canon C3. Tint via MainModule.startColor.
    public class VfxPool : MonoSingleton<VfxPool>
    {
        private const int DefaultCapacity = 20;
        private const int MaxPoolSize = 100;

        [SerializeField] private GameObject? impactPrefab;
        [SerializeField] private GameObject? deathPrefab;
        [SerializeField] private GameObject? explosionPrefab;
        [SerializeField] private GameObject? coinBurstPrefab;
        [SerializeField] private GameObject? hitFlashPrefab;
        [SerializeField] private GameObject? levelUpPrefab;
        [SerializeField] private GameObject? perkPickupPrefab;

        private ObjectPool<ParticleSystem>? _impactPool;
        private ObjectPool<ParticleSystem>? _deathPool;
        private ObjectPool<ParticleSystem>? _explosionPool;
        private ObjectPool<ParticleSystem>? _coinBurstPool;
        private ObjectPool<ParticleSystem>? _hitFlashPool;
        private ObjectPool<ParticleSystem>? _levelUpPool;
        private ObjectPool<ParticleSystem>? _perkPickupPool;

        private Transform? _root;
        private Material? _additiveMat;

        protected override void OnAwakeSingleton()
        {
            if (transform.parent != null) transform.SetParent(null);
            _root = transform;
            _additiveMat = BuildAdditiveMaterial();

            impactPrefab    ??= BuildProceduralPrefab("Impact",    BuildImpactModule);
            deathPrefab     ??= BuildProceduralPrefab("Death",     BuildDeathModule);
            explosionPrefab ??= BuildProceduralPrefab("Explosion", BuildExplosionModule);
            coinBurstPrefab ??= BuildProceduralPrefab("CoinBurst", BuildCoinBurstModule);
            hitFlashPrefab  ??= BuildProceduralPrefab("HitFlash",  BuildHitFlashModule);
            levelUpPrefab   ??= BuildProceduralPrefab("LevelUp",   BuildLevelUpModule);
            perkPickupPrefab ??= BuildProceduralPrefab("PerkPickup", BuildPerkPickupModule);

            _impactPool    = BuildPool(impactPrefab,    "Impact",    DefaultCapacity);
            _deathPool     = BuildPool(deathPrefab,     "Death",     DefaultCapacity);
            _explosionPool = BuildPool(explosionPrefab, "Explosion", 10);
            _coinBurstPool = BuildPool(coinBurstPrefab, "CoinBurst", DefaultCapacity);
            _hitFlashPool  = BuildPool(hitFlashPrefab,  "HitFlash",  DefaultCapacity);
            _levelUpPool   = BuildPool(levelUpPrefab,   "LevelUp",   8);
            _perkPickupPool = BuildPool(perkPickupPrefab, "PerkPickup", DefaultCapacity);

            PreWarm();
        }

        // ── API canon ─────────────────────────────────────────────────────────

        public void SpawnImpact(Vector3 worldPos, Color tint)
        {
            if (!IsVfxEnabled() || _impactPool == null) return;
            var ps = _impactPool.Get();
            ps.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            ApplyTint(ps, tint);
            PlayAndAutoRelease(ps, _impactPool);
        }

        public void SpawnDeath(Vector3 worldPos, Color tint, bool isBoss = false)
        {
            if (!IsVfxEnabled() || _deathPool == null) return;
            var ps = _deathPool.Get();
            ps.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            ps.transform.localScale = isBoss ? Vector3.one * 2.5f : Vector3.one;
            ApplyTint(ps, tint);
            PlayAndAutoRelease(ps, _deathPool);
        }

        public void SpawnDeathPuff(Vector3 worldPos, int tier = 0)
        {
            Color tint = tier switch
            {
                2 => new Color(1f, 0.55f, 0.05f),
                1 => new Color(0.9f, 0.3f, 0.9f),
                _ => Color.white
            };
            SpawnDeath(worldPos, tint, isBoss: tier >= 2);
        }

        public void SpawnExplosion(Vector3 worldPos, float radius = 2f)
        {
            if (!IsVfxEnabled() || _explosionPool == null) return;
            var ps = _explosionPool.Get();
            ps.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            float scale = Mathf.Clamp(radius * 0.5f, 0.5f, 5f);
            ps.transform.localScale = Vector3.one * scale;
            ApplyTint(ps, new Color(1f, 0.65f, 0.15f));
            PlayAndAutoRelease(ps, _explosionPool);
        }

        public void SpawnCoinBurst(Vector3 worldPos)
        {
            if (!IsVfxEnabled() || _coinBurstPool == null) return;
            var ps = _coinBurstPool.Get();
            ps.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            ApplyTint(ps, new Color(1f, 0.88f, 0.15f));
            PlayAndAutoRelease(ps, _coinBurstPool);
        }

        public void SpawnCoinPickup(Vector3 worldPos) => SpawnCoinBurst(worldPos);

        public void SpawnHitFlash(Transform target)
        {
            if (!IsVfxEnabled() || _hitFlashPool == null) return;
            var ps = _hitFlashPool.Get();
            ps.transform.SetPositionAndRotation(target.position, Quaternion.identity);
            ps.transform.SetParent(target, worldPositionStays: true);
            ApplyTint(ps, Color.white);
            PlayAndAutoRelease(ps, _hitFlashPool);
        }

        public void SpawnLevelUp(Vector3 worldPos)
        {
            if (!IsVfxEnabled() || _levelUpPool == null) return;
            var ps = _levelUpPool.Get();
            ps.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            ApplyTint(ps, new Color(1f, 0.84f, 0f));
            PlayAndAutoRelease(ps, _levelUpPool);
        }

        public void SpawnPerkPickup(Vector3 worldPos, Color tint)
        {
            if (!IsVfxEnabled() || _perkPickupPool == null) return;
            var ps = _perkPickupPool.Get();
            ps.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            ApplyTint(ps, tint);
            PlayAndAutoRelease(ps, _perkPickupPool);
        }

        // ── Pool internals ────────────────────────────────────────────────────

        private ObjectPool<ParticleSystem> BuildPool(GameObject prefab, string label, int capacity)
        {
            return new ObjectPool<ParticleSystem>(
                createFunc: () => CreateInstance(prefab, label),
                actionOnGet: ps => ps.gameObject.SetActive(true),
                actionOnRelease: ps =>
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.transform.SetParent(_root, false);
                    ps.transform.localScale = Vector3.one;
                    ps.gameObject.SetActive(false);
                },
                actionOnDestroy: ps => { if (ps != null) Destroy(ps.gameObject); },
                collectionCheck: false,
                defaultCapacity: capacity,
                maxSize: MaxPoolSize
            );
        }

        private ParticleSystem CreateInstance(GameObject prefab, string label)
        {
            var go = Instantiate(prefab, _root);
            go.name = $"{label}_VFX";
            go.SetActive(false);
            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null) ps = go.AddComponent<ParticleSystem>();
            return ps;
        }

        private void PreWarm()
        {
            PreWarmPool(_impactPool,    DefaultCapacity);
            PreWarmPool(_deathPool,     DefaultCapacity);
            PreWarmPool(_explosionPool, 10);
            PreWarmPool(_coinBurstPool, DefaultCapacity);
            PreWarmPool(_hitFlashPool,  DefaultCapacity);
            PreWarmPool(_levelUpPool,   8);
            PreWarmPool(_perkPickupPool, DefaultCapacity);
        }

        private static void PreWarmPool(ObjectPool<ParticleSystem>? pool, int count)
        {
            if (pool == null) return;
            var buf = new ParticleSystem[count];
            for (int i = 0; i < count; i++) buf[i] = pool.Get();
            for (int i = 0; i < count; i++) pool.Release(buf[i]);
        }

        private static void ApplyTint(ParticleSystem ps, Color tint)
        {
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(tint);
        }

        private void PlayAndAutoRelease(ParticleSystem ps, ObjectPool<ParticleSystem> pool)
        {
            ps.Play(true);
            StartCoroutine(AutoReleaseRoutine(ps, pool, _root));
        }

        private static IEnumerator AutoReleaseRoutine(ParticleSystem ps,
                                                       ObjectPool<ParticleSystem> pool,
                                                       Transform? root)
        {
            var main = ps.main;
            float waitTime = main.startLifetime.constantMax + main.duration + 0.1f;
            yield return new WaitForSeconds(waitTime);
            if (ps == null || !ps.gameObject.activeSelf) yield break;
            if (root != null) ps.transform.SetParent(root, worldPositionStays: false);
            pool.Release(ps);
        }

        private static bool IsVfxEnabled()
            => SettingsRegistry.Instance?.VFXEnabled ?? true;

        // ── Procedural prefab builders ────────────────────────────────────────

        private GameObject BuildProceduralPrefab(string label, System.Action<ParticleSystem> configure)
        {
            var go = new GameObject($"Proc_{label}");
            go.SetActive(false);
            DontDestroyOnLoad(go);
            var ps = go.AddComponent<ParticleSystem>();

            var psr = go.GetComponent<ParticleSystemRenderer>();
            if (psr != null && _additiveMat != null)
                psr.material = _additiveMat;

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Disable;

            configure(ps);
            return go;
        }

        private static void BuildImpactModule(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime  = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startSpeed     = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
            main.startSize      = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor     = Color.white;
            main.maxParticles   = 20;
            main.duration       = 0.1f;
            main.gravityModifier = 0.6f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 12, 1, 0.01f) });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.1f;

            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
        }

        private static void BuildDeathModule(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime  = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed     = new ParticleSystem.MinMaxCurve(3.5f, 8f);
            main.startSize      = new ParticleSystem.MinMaxCurve(0.12f, 0.38f);
            main.startColor     = Color.white;
            main.maxParticles   = 35;
            main.duration       = 0.1f;
            main.gravityModifier = 1.2f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16, 28, 1, 0.01f) });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.2f;

            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
        }

        private static void BuildExplosionModule(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime  = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed     = new ParticleSystem.MinMaxCurve(5f, 12f);
            main.startSize      = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startColor     = new Color(1f, 0.65f, 0.15f);
            main.maxParticles   = 60;
            main.duration       = 0.15f;
            main.gravityModifier = 0.3f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30, 50, 1, 0.01f) });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.4f;

            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
        }

        private static void BuildCoinBurstModule(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime  = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed     = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize      = new ParticleSystem.MinMaxCurve(0.07f, 0.18f);
            main.startColor     = new Color(1f, 0.88f, 0.15f);
            main.maxParticles   = 20;
            main.duration       = 0.1f;
            main.gravityModifier = 2f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 14, 1, 0.01f) });

            var shape = ps.shape;
            shape.enabled      = true;
            shape.shapeType    = ParticleSystemShapeType.Hemisphere;
            shape.radius       = 0.1f;
            shape.rotation     = new Vector3(-90f, 0f, 0f);

            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
        }

        private static void BuildHitFlashModule(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime  = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
            main.startSpeed     = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            main.startSize      = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startColor     = Color.white;
            main.maxParticles   = 12;
            main.duration       = 0.05f;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6, 10, 1, 0.01f) });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.05f;

            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
        }

        private static void BuildLevelUpModule(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime  = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed     = new ParticleSystem.MinMaxCurve(3f, 9f);
            main.startSize      = new ParticleSystem.MinMaxCurve(0.12f, 0.42f);
            main.startColor     = new Color(1f, 0.84f, 0f);
            main.maxParticles   = 60;
            main.duration       = 0.2f;
            main.gravityModifier = -0.4f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25, 45, 1, 0.01f) });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.3f;

            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
        }

        private static void BuildPerkPickupModule(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime  = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed     = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize      = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            main.startColor     = Color.white;
            main.maxParticles   = 25;
            main.duration       = 0.1f;
            main.gravityModifier = 0.2f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 18, 1, 0.01f) });

            var shape = ps.shape;
            shape.enabled      = true;
            shape.shapeType    = ParticleSystemShapeType.Hemisphere;
            shape.radius       = 0.2f;
            shape.rotation     = new Vector3(-90f, 0f, 0f);

            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
        }

        // ── Shared curve helpers ──────────────────────────────────────────────

        private static void SetSizeOverLifetimeFade(ParticleSystem ps)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(
                    new Keyframe(0f, 1f, 0f, -1.5f),
                    new Keyframe(1f, 0.15f, -1.5f, 0f)));
        }

        private static void SetColorAlphaFade(ParticleSystem ps)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);
        }

        private static Material BuildAdditiveMaterial()
        {
            // Hidden/InternalErrorShader is always present in Unity — safe terminal fallback.
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                      ?? Shader.Find("Particles/Standard Unlit")
                      ?? Shader.Find("Sprites/Default")
                      ?? Shader.Find("Standard")
                      ?? Shader.Find("Hidden/InternalErrorShader")!;
            var mat = new Material(shader) { name = "VfxParticle_Additive" };
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 3f);
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_SrcBlend", 1);
            mat.SetInt("_DstBlend", 1);
            mat.renderQueue = 3000;
            return mat;
        }
    }
}
