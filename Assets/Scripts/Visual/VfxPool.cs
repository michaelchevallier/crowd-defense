#nullable enable
using System.Collections;
using System.Collections.Generic;
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
        private static readonly Dictionary<float, WaitForSeconds> _waitCache = new();
        private const int DefaultCapacity = 24;
        private const int MaxPoolSize = 100;

        // Auto-LOD : réduit maxParticles à 50% si FPS rolling avg < 30.
        private static float _lodMultiplier = 1f;
        private float _fpsAccum;
        private int   _fpsFrames;
        private float _lodRecheckTimer;

        [SerializeField] private GameObject? impactPrefab;
        [SerializeField] private GameObject? deathPrefab;
        [SerializeField] private GameObject? explosionPrefab;
        [SerializeField] private GameObject? coinBurstPrefab;
        [SerializeField] private GameObject? hitFlashPrefab;
        [SerializeField] private GameObject? levelUpPrefab;
        [SerializeField] private GameObject? perkPickupPrefab;
        [SerializeField] private GameObject? frostPrefab;
        [SerializeField] private GameObject? portalPrefab;
        [SerializeField] private GameObject? fireBreathPrefab;
        [SerializeField] private GameObject? muzzleFlashPrefab;

        private ObjectPool<ParticleSystem>? _impactPool;
        private ObjectPool<ParticleSystem>? _deathPool;
        private ObjectPool<ParticleSystem>? _explosionPool;
        private ObjectPool<ParticleSystem>? _coinBurstPool;
        private ObjectPool<ParticleSystem>? _hitFlashPool;
        private ObjectPool<ParticleSystem>? _levelUpPool;
        private ObjectPool<ParticleSystem>? _perkPickupPool;
        private ObjectPool<ParticleSystem>? _frostPool;
        private ObjectPool<ParticleSystem>? _portalPool;
        private ObjectPool<ParticleSystem>? _fireBreathPool;
        private ObjectPool<ParticleSystem>? _muzzleFlashPool;

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
            frostPrefab     ??= BuildProceduralPrefab("Frost",     BuildFrostModule);
            portalPrefab    ??= BuildProceduralPrefab("Portal",    BuildPortalModule);
            fireBreathPrefab ??= BuildProceduralPrefab("FireBreath", BuildFireBreathModule);
            muzzleFlashPrefab ??= BuildProceduralPrefab("MuzzleFlash", BuildMuzzleFlashModule);

            _impactPool    = BuildPool(impactPrefab,    "Impact",    DefaultCapacity);
            _deathPool     = BuildPool(deathPrefab,     "Death",     DefaultCapacity);
            _explosionPool = BuildPool(explosionPrefab, "Explosion", DefaultCapacity);
            _coinBurstPool = BuildPool(coinBurstPrefab, "CoinBurst", DefaultCapacity);
            _hitFlashPool  = BuildPool(hitFlashPrefab,  "HitFlash",  DefaultCapacity);
            _levelUpPool   = BuildPool(levelUpPrefab,   "LevelUp",   DefaultCapacity);
            _perkPickupPool = BuildPool(perkPickupPrefab, "PerkPickup", DefaultCapacity);
            _frostPool     = BuildPool(frostPrefab,     "Frost",     DefaultCapacity);
            _portalPool    = BuildPool(portalPrefab,    "Portal",    DefaultCapacity);
            _fireBreathPool  = BuildPool(fireBreathPrefab,  "FireBreath",  DefaultCapacity);
            _muzzleFlashPool = BuildPool(muzzleFlashPrefab, "MuzzleFlash", DefaultCapacity);

            PreWarm();
        }

        private void Update()
        {
            _fpsAccum  += Time.deltaTime;
            _fpsFrames += 1;
            _lodRecheckTimer += Time.deltaTime;

            if (_lodRecheckTimer >= 2f)
            {
                _lodRecheckTimer = 0f;
                if (_fpsFrames > 0)
                {
                    float avgFps = _fpsFrames / _fpsAccum;
                    _lodMultiplier = avgFps < 30f ? 0.5f : (avgFps > 50f ? 1f : _lodMultiplier);
                }
                _fpsAccum  = 0f;
                _fpsFrames = 0;
            }
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

        public void SpawnDeath(Vector3 worldPos, Color tint, float intensityMul = 1f)
        {
            if (!IsVfxEnabled() || _deathPool == null) return;
            var ps = _deathPool.Get();
            ps.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            ps.transform.localScale = Vector3.one * intensityMul;

            var main = ps.main;
            main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(280 * intensityMul));
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f,
                Mathf.RoundToInt(16 * intensityMul),
                Mathf.RoundToInt(28 * intensityMul), 1, 0.01f) });

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
            float intensity = tier switch
            {
                2 => 5f,
                1 => 2f,
                _ => 1f
            };
            SpawnDeath(worldPos, tint, intensity);
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

        // Trail de coins qui volent depuis fromWorld vers le gold counter HUD (top-left).
        // Chaque coin : Sphere 0.15 yellow emissive, Lerp sur 0.5s, scale fade → 0.
        public void SpawnCoinTrail(Vector3 fromWorld, int count = 5)
        {
            if (!IsVfxEnabled()) return;
            count = Mathf.Clamp(count, 1, 12);
            for (int i = 0; i < count; i++)
                StartCoroutine(FlyCoin(fromWorld, i * 0.06f));
        }

        private IEnumerator FlyCoin(Vector3 origin, float delay)
        {
            if (delay > 0f) yield return GetWait(delay);

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "CoinTrail_VFX";
            Object.Destroy(go.GetComponent<Collider>());

            // Yellow emissive material
            var shader  = Shader.Find("Universal Render Pipeline/Lit")
                       ?? Shader.Find("Standard")
                       ?? Shader.Find("Hidden/InternalErrorShader")!;
            var mat     = new Material(shader) { name = "CoinTrailMat" };
            var yellow  = new Color(1f, 0.86f, 0.1f);
            mat.color   = yellow;
            mat.SetFloat("_Surface", 0f); // Opaque
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", yellow * 1.8f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Slight random offset so coins don't all overlap at origin
            var scatter  = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.4f), Random.Range(-0.3f, 0.3f));
            var startPos = origin + scatter;
            go.transform.position   = startPos;
            go.transform.localScale = Vector3.one * 0.15f;

            // Target = approximate world position of gold counter (top-left HUD)
            var cam = MainCameraCache.Main;
            Vector3 target = cam != null
                ? cam.ScreenToWorldPoint(new Vector3(100f, Screen.height - 50f, 10f))
                : startPos + new Vector3(-5f, 3f, 0f);

            const float Duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / Duration);
                // Ease-in quad so coins accelerate toward HUD
                float te = t * t;
                go.transform.position   = Vector3.LerpUnclamped(startPos, target, te);
                // Scale fade from 0.15 → 0
                float scale = Mathf.Lerp(0.15f, 0f, t);
                go.transform.localScale = Vector3.one * scale;
                yield return null;
            }

            Object.Destroy(go);
        }

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

        public void SpawnMuzzleFlash(Vector3 worldPos, Color tint)
        {
            if (!IsVfxEnabled() || _muzzleFlashPool == null) return;
            var ps = _muzzleFlashPool.Get();
            ps.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            ApplyTint(ps, tint);
            PlayAndAutoRelease(ps, _muzzleFlashPool);
        }

        public void SpawnFrost(Vector3 worldPos, float radius)
        {
            if (!IsVfxEnabled() || _frostPool == null) return;
            var ps = _frostPool.Get();
            ps.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            // Scale the circle shape radius at runtime before play.
            var shape = ps.shape;
            shape.radius = Mathf.Max(0.3f, radius);
            ApplyTint(ps, new Color(0.5f, 0.9f, 1f));
            PlayAndAutoRelease(ps, _frostPool);
        }

        public void SpawnPortal(Vector3 worldPos)
        {
            if (!IsVfxEnabled() || _portalPool == null) return;
            var ps = _portalPool.Get();
            ps.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            ApplyTint(ps, new Color(0.4f, 0.2f, 0.6f));
            PlayAndAutoRelease(ps, _portalPool);
            StartCoroutine(PortalLightFlashRoutine(worldPos));
        }

        // Cone de feu boss dragon/fire — orient via LookRotation, durée 0.7s, rateOverTime 200/sec.
        // Un Transform pivot temporaire est créé pour aligner la shape Cone dans la direction voulue.
        public void SpawnFireBreath(Vector3 origin, Vector3 direction, float distance)
        {
            if (!IsVfxEnabled() || _fireBreathPool == null) return;
            if (direction.sqrMagnitude < 0.0001f) return;

            var ps = _fireBreathPool.Get();

            // Pivot temporaire aligné sur la direction — parente le PS pour orienter le cone
            var pivot = new GameObject("FireBreath_Pivot");
            pivot.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
            ps.transform.SetParent(pivot.transform, worldPositionStays: false);
            ps.transform.localPosition = Vector3.zero;
            ps.transform.localRotation = Quaternion.identity;

            // Ajuster la longueur du cone à distance
            var shape = ps.shape;
            shape.length = distance;

            ps.gameObject.SetActive(true);
            ps.Play(true);
            StartCoroutine(FireBreathReleaseRoutine(ps, pivot, _fireBreathPool, _root));
        }

        private IEnumerator FireBreathReleaseRoutine(ParticleSystem ps, GameObject pivot,
                                                      ObjectPool<ParticleSystem> pool, Transform? root)
        {
            const float EmitDuration = 0.7f;
            const float TailDuration = 1.0f; // lifetime max des particules
            yield return GetWait(EmitDuration);

            // Arrêter l'émission, laisser les particules existantes se consumer
            ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);

            yield return GetWait(TailDuration);

            if (ps != null && ps.gameObject != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.transform.SetParent(root, worldPositionStays: false);
                ps.transform.localScale = Vector3.one;
                ps.gameObject.SetActive(false);
                pool.Release(ps);
            }
            if (pivot != null)
                Destroy(pivot);
        }

        private IEnumerator PortalLightFlashRoutine(Vector3 worldPos)
        {
            var lightGo = new GameObject("PortalLight_VFX");
            lightGo.transform.position = worldPos;
            var light = lightGo.AddComponent<Light>();
            light.type      = LightType.Point;
            light.color     = new Color(0.4f, 0.2f, 0.6f);
            light.range     = 4f;
            light.intensity = 5f;

            float elapsed = 0f;
            const float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                light.intensity = Mathf.Lerp(5f, 0f, elapsed / duration);
                yield return null;
            }
            Destroy(lightGo);
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
            PreWarmPool(_impactPool,     DefaultCapacity);
            PreWarmPool(_deathPool,      DefaultCapacity);
            PreWarmPool(_explosionPool,  DefaultCapacity);
            PreWarmPool(_coinBurstPool,  DefaultCapacity);
            PreWarmPool(_hitFlashPool,   DefaultCapacity);
            PreWarmPool(_levelUpPool,    DefaultCapacity);
            PreWarmPool(_perkPickupPool, DefaultCapacity);
            PreWarmPool(_frostPool,      DefaultCapacity);
            PreWarmPool(_portalPool,     DefaultCapacity);
            PreWarmPool(_fireBreathPool,  DefaultCapacity);
            PreWarmPool(_muzzleFlashPool, DefaultCapacity);
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

        private static void ApplyLod(ParticleSystem ps)
        {
            if (_lodMultiplier >= 1f) return;
            var main = ps.main;
            main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(main.maxParticles * _lodMultiplier));
        }

        private void PlayAndAutoRelease(ParticleSystem ps, ObjectPool<ParticleSystem> pool)
        {
            ApplyLod(ps);
            ps.Play(true);
            StartCoroutine(AutoReleaseRoutine(ps, pool, _root));
        }

        private static WaitForSeconds GetWait(float seconds)
        {
            float key = Mathf.Round(seconds * 20f) / 20f;
            if (!_waitCache.TryGetValue(key, out var w))
            {
                w = new WaitForSeconds(key);
                _waitCache[key] = w;
            }
            return w;
        }

        private static IEnumerator AutoReleaseRoutine(ParticleSystem ps,
                                                       ObjectPool<ParticleSystem> pool,
                                                       Transform? root)
        {
            var main = ps.main;
            float waitTime = main.startLifetime.constantMax + main.duration + 0.1f;
            yield return GetWait(waitTime);
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
            main.maxParticles   = 160;
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
            main.maxParticles   = 280;
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
            main.maxParticles   = 400;
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
            main.maxParticles   = 160;
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
            main.maxParticles   = 100;
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
            main.maxParticles   = 400;
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
            main.maxParticles   = 200;
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

        private static void BuildFrostModule(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime  = new ParticleSystem.MinMaxCurve(1.0f, 1.2f);
            main.startSpeed     = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize      = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startColor     = new Color(0.5f, 0.9f, 1f);
            main.maxParticles   = 400;
            main.duration       = 0.8f;
            main.gravityModifier = 0.05f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25, 30, 1, 0.01f) });

            var shape = ps.shape;
            shape.enabled      = true;
            shape.shapeType    = ParticleSystemShapeType.Circle;
            shape.radius       = 1f; // overridden at runtime by SpawnFrost

            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
        }

        private static void BuildPortalModule(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime  = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed     = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize      = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor     = new Color(0.4f, 0.2f, 0.6f);
            main.maxParticles   = 320;
            main.duration       = 0.5f;
            main.gravityModifier = -0.3f;
            // Simulate in world space so particles stay put after spawn.
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25, 25, 1, 0.01f) });

            // Donut shape: circle with donutRadius for the hollow center effect.
            var shape = ps.shape;
            shape.enabled      = true;
            shape.shapeType    = ParticleSystemShapeType.Donut;
            shape.radius       = 0.7f;
            shape.donutRadius  = 0.15f;
            shape.rotation     = new Vector3(90f, 0f, 0f);

            // Vortex via VelocityOverLifetime orbital.
            var vol = ps.velocityOverLifetime;
            vol.enabled        = true;
            vol.space          = ParticleSystemSimulationSpace.Local;
            // orbitalY in Unity = radians/s; 3.14 ≈ π = 180°/s swirl speed.
            vol.orbitalY       = new ParticleSystem.MinMaxCurve(Mathf.PI);
            vol.orbitalOffsetY = new ParticleSystem.MinMaxCurve(0f);

            SetSizeOverLifetimeFade(ps);
            SetColorAlphaFade(ps);
        }

        // Cone de particules feu boss dragon — rateOverTime 200/sec, shape Cone angle 20 deg.
        // La couleur suit la ramp orange → rouge → fumée noire via colorOverLifetime gradient.
        private static void BuildFireBreathModule(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime  = new ParticleSystem.MinMaxCurve(0.7f, 1.0f);
            main.startSpeed     = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSize      = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startColor     = new Color(1f, 0.5f, 0.1f);
            main.maxParticles   = 400;
            main.duration       = 0.7f;
            main.gravityModifier = 0.05f;

            var emission = ps.emission;
            emission.rateOverTime = 200f;
            emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());

            // Cone aligné sur Z+ (le pivot parent sera orienté via LookRotation)
            var shape = ps.shape;
            shape.enabled         = true;
            shape.shapeType       = ParticleSystemShapeType.Cone;
            shape.angle           = 20f;
            shape.length          = 8f;
            shape.radius          = 0.15f;
            shape.radiusThickness = 1f;

            // Ramp couleur : orange → rouge → fumée sombre
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.5f, 0.1f),    0f),
                    new GradientColorKey(new Color(0.9f, 0.2f, 0.05f), 0.55f),
                    new GradientColorKey(new Color(0.2f, 0.1f, 0.05f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.7f, 0.5f),
                    new GradientAlphaKey(0f,   1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            SetSizeOverLifetimeFade(ps);
        }

        private static void BuildMuzzleFlashModule(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime  = new ParticleSystem.MinMaxCurve(0.10f, 0.15f);
            main.startSpeed     = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize      = new ParticleSystem.MinMaxCurve(0.10f, 0.20f);
            main.startColor     = new Color(1f, 0.55f, 0.05f);
            main.maxParticles   = 128;
            main.duration       = 0.05f;
            main.gravityModifier = 0.2f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 8, 1, 0.01f) });

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle     = 15f;
            shape.radius    = 0.05f;

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
