#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using CrowdDefense.Common;
using CrowdDefense.UI;

namespace CrowdDefense.Visual
{
    // Port de Particles.js → Unity ObjectPool<ParticleSystem>.
    // Procedural builders + texture wiring in VfxPoolBindings.cs.
    public partial class VfxPool : MonoSingleton<VfxPool>
    {
        private static readonly Dictionary<float, WaitForSeconds> _waitCache = new();
        private const int DefaultCapacity = 24;
        private const int MaxPoolSize = 100;

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
        [SerializeField] private GameObject? upgradeBurstPrefab;
        [SerializeField] private GameObject? sparkPrefab;
        [SerializeField] private GameObject? upgradeConfettiPrefab;

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
        private ObjectPool<ParticleSystem>? _upgradeBurstPool;
        private ObjectPool<ParticleSystem>? _sparkPool;
        private ObjectPool<ParticleSystem>? _upgradeConfettiPool;

        private Transform? _root;
        private Material? _additiveMat;

        protected override void OnAwakeSingleton()
        {
            if (transform.parent != null) transform.SetParent(null);
            _root = transform;
            _additiveMat = VfxPoolBindings.BuildAdditiveMaterial();
            VfxPoolBindings.LoadAllTextures();

            // Unity-aware null check : `??=` skips fake-null missing refs ; `==` op handles both.
            if (impactPrefab          == null) impactPrefab          = MakeProcPrefab("Impact",          ps => VfxPoolBindings.BuildImpactModule(ps, _additiveMat));
            if (deathPrefab           == null) deathPrefab           = MakeProcPrefab("Death",           ps => VfxPoolBindings.BuildDeathModule(ps, _additiveMat));
            if (explosionPrefab       == null) explosionPrefab       = MakeProcPrefab("Explosion",       ps => VfxPoolBindings.BuildExplosionModule(ps, _additiveMat));
            if (coinBurstPrefab       == null) coinBurstPrefab       = MakeProcPrefab("CoinBurst",       ps => VfxPoolBindings.BuildCoinBurstModule(ps, _additiveMat));
            if (hitFlashPrefab        == null) hitFlashPrefab        = MakeProcPrefab("HitFlash",        ps => VfxPoolBindings.BuildHitFlashModule(ps, _additiveMat));
            if (levelUpPrefab         == null) levelUpPrefab         = MakeProcPrefab("LevelUp",         ps => VfxPoolBindings.BuildLevelUpModule(ps, _additiveMat));
            if (perkPickupPrefab      == null) perkPickupPrefab      = MakeProcPrefab("PerkPickup",      ps => VfxPoolBindings.BuildPerkPickupModule(ps, _additiveMat));
            if (frostPrefab           == null) frostPrefab           = MakeProcPrefab("Frost",           ps => VfxPoolBindings.BuildFrostModule(ps, _additiveMat));
            if (portalPrefab          == null) portalPrefab          = MakeProcPrefab("Portal",          ps => VfxPoolBindings.BuildPortalModule(ps, _additiveMat));
            if (fireBreathPrefab      == null) fireBreathPrefab      = MakeProcPrefab("FireBreath",      ps => VfxPoolBindings.BuildFireBreathModule(ps, _additiveMat));
            if (muzzleFlashPrefab     == null) muzzleFlashPrefab     = MakeProcPrefab("MuzzleFlash",     ps => VfxPoolBindings.BuildMuzzleFlashModule(ps, _additiveMat));
            if (upgradeBurstPrefab    == null) upgradeBurstPrefab    = MakeProcPrefab("UpgradeBurst",    ps => VfxPoolBindings.BuildUpgradeBurstModule(ps, _additiveMat));
            if (sparkPrefab           == null) sparkPrefab           = MakeProcPrefab("Spark",           ps => VfxPoolBindings.BuildSparkModule(ps, _additiveMat));
            if (upgradeConfettiPrefab == null) upgradeConfettiPrefab = MakeProcPrefab("UpgradeConfetti", ps => VfxPoolBindings.BuildUpgradeConfettiModule(ps, _additiveMat));

            // Sub-emitter smoke on explosion (fires on particle death)
            var smokePrefab = MakeProcPrefab("ExplosionSmoke", ps => VfxPoolBindings.BuildExplosionSmokeModule(ps, _additiveMat));
            VfxPoolBindings.AttachSubEmitter(explosionPrefab, smokePrefab);

            _impactPool         = MakePool(impactPrefab,         "Impact");
            _deathPool          = MakePool(deathPrefab,          "Death");
            _explosionPool      = MakePool(explosionPrefab,      "Explosion");
            _coinBurstPool      = MakePool(coinBurstPrefab,      "CoinBurst");
            _hitFlashPool       = MakePool(hitFlashPrefab,       "HitFlash");
            _levelUpPool        = MakePool(levelUpPrefab,        "LevelUp");
            _perkPickupPool     = MakePool(perkPickupPrefab,     "PerkPickup");
            _frostPool          = MakePool(frostPrefab,          "Frost");
            _portalPool         = MakePool(portalPrefab,         "Portal");
            _fireBreathPool     = MakePool(fireBreathPrefab,     "FireBreath");
            _muzzleFlashPool    = MakePool(muzzleFlashPrefab,    "MuzzleFlash");
            _upgradeBurstPool   = MakePool(upgradeBurstPrefab,   "UpgradeBurst");
            _sparkPool          = MakePool(sparkPrefab,          "Spark");
            _upgradeConfettiPool = MakePool(upgradeConfettiPrefab, "UpgradeConfetti");

            InitExtra();
            PreWarm();
        }

        private void Update()
        {
            _fpsAccum  += Time.deltaTime;
            _fpsFrames += 1;
            _lodRecheckTimer += Time.deltaTime;
            if (_lodRecheckTimer < 2f) return;
            _lodRecheckTimer = 0f;
            if (_fpsFrames > 0)
            {
                float avg = _fpsFrames / _fpsAccum;
                _lodMultiplier = avg < 30f ? 0.5f : (avg > 50f ? 1f : _lodMultiplier);
            }
            _fpsAccum = 0f;
            _fpsFrames = 0;
        }

        // ── API canon ─────────────────────────────────────────────────────────

        public void SpawnImpact(Vector3 pos, Color tint) => SpawnFrom(_impactPool, pos, tint);

        public void SpawnDeath(Vector3 pos, Color tint, float intensityMul = 1f)
        {
            if (!IsVfxEnabled() || _deathPool == null) return;
            var ps = _deathPool.Get();
            ps.transform.SetPositionAndRotation(pos, Quaternion.identity);
            ps.transform.localScale = Vector3.one * intensityMul;
            var main = ps.main;
            main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(280 * intensityMul));
            var deathEmit = ps.emission;
            deathEmit.SetBursts(new[] { new ParticleSystem.Burst(0f,
                (short)Mathf.RoundToInt(16 * intensityMul), (short)Mathf.RoundToInt(28 * intensityMul), 1, 0.01f) });
            ApplyTint(ps, tint);
            PlayRelease(ps, _deathPool);
        }

        public void SpawnDeathPuff(Vector3 pos, int tier = 0)
        {
            Color tint = tier switch { 2 => new Color(1f, 0.55f, 0.05f), 1 => new Color(0.9f, 0.3f, 0.9f), _ => Color.white };
            float intensity = tier switch { 2 => 5f, 1 => 2f, _ => 1f };
            SpawnDeath(pos, tint, intensity);
        }

        public void SpawnExplosion(Vector3 pos, float radius = 2f)
        {
            if (!IsVfxEnabled() || _explosionPool == null) return;
            var ps = _explosionPool.Get();
            ps.transform.SetPositionAndRotation(pos, Quaternion.identity);
            ps.transform.localScale = Vector3.one * Mathf.Clamp(radius * 0.5f, 0.5f, 5f);
            ApplyTint(ps, new Color(1f, 0.65f, 0.15f));
            PlayRelease(ps, _explosionPool);
        }

        public void SpawnCoinBurst(Vector3 pos)  => SpawnFrom(_coinBurstPool, pos, new Color(1f, 0.88f, 0.15f));
        public void SpawnCoinPickup(Vector3 pos) => SpawnCoinBurst(pos);

        public void SpawnConfetti(Vector3 pos, float mul = 1f)
        {
            if (!IsVfxEnabled() || _coinBurstPool == null) return;
            var ps = _coinBurstPool.Get();
            ps.transform.SetPositionAndRotation(pos, Quaternion.identity);
            var main = ps.main;
            main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(50 * mul * _lodMultiplier));
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f,
                (short)Mathf.RoundToInt(10 * mul), (short)Mathf.RoundToInt(30 * mul), 1, 0.05f) });
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color   = new ParticleSystem.MinMaxGradient(VfxPoolBindings.BuildConfettiGradient());
            PlayRelease(ps, _coinBurstPool);
        }

        public void SpawnConfetti(Vector3 pos, float mul, Color tint)
        {
            if (!IsVfxEnabled() || _coinBurstPool == null) return;
            var ps = _coinBurstPool.Get();
            ps.transform.SetPositionAndRotation(pos, Quaternion.identity);
            var main = ps.main;
            main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(50 * mul * _lodMultiplier));
            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f,
                (short)Mathf.RoundToInt(10 * mul), (short)Mathf.RoundToInt(30 * mul), 1, 0.05f) });
            var col = ps.colorOverLifetime;
            col.enabled = false;
            ApplyTint(ps, tint);
            PlayRelease(ps, _coinBurstPool);
        }

        public void SpawnHitFlash(Transform target)
        {
            if (!IsVfxEnabled() || _hitFlashPool == null) return;
            var ps = _hitFlashPool.Get();
            ps.transform.SetPositionAndRotation(target.position, Quaternion.identity);
            ps.transform.SetParent(target, worldPositionStays: true);
            ApplyTint(ps, Color.white);
            PlayRelease(ps, _hitFlashPool);
        }

        public void SpawnLevelUp(Vector3 pos)                => SpawnFrom(_levelUpPool,    pos, new Color(1f, 0.84f, 0f));
        public void SpawnPerkPickup(Vector3 pos, Color tint)  => SpawnFrom(_perkPickupPool, pos, tint);
        public void SpawnMuzzleFlash(Vector3 pos, Color tint) => SpawnFrom(_muzzleFlashPool, pos, tint);
        public void SpawnSpark(Vector3 pos, Color tint)       => SpawnFrom(_sparkPool, pos, tint);

        public void SpawnAttackStream(Vector3 from, Vector3 to, Color tint)
        {
            if (!IsVfxEnabled() || _sparkPool == null) return;
            for (int i = 1; i <= 4; i++)
            {
                var ps = _sparkPool.Get();
                ps.transform.SetPositionAndRotation(Vector3.Lerp(from, to, i / 5f), Quaternion.identity);
                ps.transform.localScale = Vector3.one * 0.35f;
                var psMain = ps.main;
                psMain.startLifetime   = new ParticleSystem.MinMaxCurve(0.10f, 0.15f);
                psMain.startSize       = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
                var psEmit = ps.emission;
                psEmit.SetBursts(new[] { new ParticleSystem.Burst(0f, 2, 2, 1, 0.01f) });
                ApplyTint(ps, tint);
                PlayRelease(ps, _sparkPool);
            }
        }

        public void SpawnUpgradeBurst(Vector3 pos, int level)
        {
            if (!IsVfxEnabled() || _upgradeBurstPool == null) return;
            var ps = _upgradeBurstPool.Get();
            ps.transform.SetPositionAndRotation(pos, Quaternion.identity);
            if (level >= 3)
            {
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color   = new ParticleSystem.MinMaxGradient(VfxPoolBindings.BuildConfettiGradient());
            }
            else
            {
                ApplyTint(ps, level == 2 ? new Color(0.2f, 0.9f, 1f) : Color.white);
                var col2 = ps.colorOverLifetime;
                col2.enabled = false;
            }
            PlayRelease(ps, _upgradeBurstPool);
        }

        private static readonly Color[] _confettiPalette =
        {
            new(1f, 0.85f, 0.2f), new(0.3f, 0.85f, 1f),
            new(1f, 0.4f,  0.7f), new(0.3f, 0.95f, 0.5f),
        };

        public void SpawnUpgradeConfetti(Vector3 pos, int level)
        {
            if (!IsVfxEnabled() || _upgradeConfettiPool == null) return;
            var ps = _upgradeConfettiPool.Get();
            ps.transform.SetPositionAndRotation(pos, Quaternion.identity);
            int count = Mathf.Max(1, Mathf.RoundToInt((level >= 3 ? 50 : 30) * _lodMultiplier));
            var confMain = ps.main;
            confMain.maxParticles = count;
            var confEmit = ps.emission;
            confEmit.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count, (short)count, 1, 0.01f) });
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color   = new ParticleSystem.MinMaxGradient(
                level >= 3 ? VfxPoolBindings.BuildRainbowGradient() : BuildPaletteGradient());
            PlayRelease(ps, _upgradeConfettiPool);
        }

        // ── New VFX spawn methods (R6-PARITY-004-IMPL) ───────────────────────────

        public void SpawnElectricImpact(Vector3 pos)
            => SpawnFrom(_electricCloudPool, pos, new Color(0.5f, 0.85f, 1f));

        public void SpawnImpactSmall(Vector3 pos, Color tint)
            => SpawnFrom(_explosionSmallPool, pos, tint);

        public void SpawnGlyph(Vector3 pos, Color tint)
            => SpawnFrom(_glyphDarkPool, pos, tint);

        public void SpawnHealAura(Vector3 pos, float radius = 1f)
            => SpawnFromRadius(_healAuraPool, pos, new Color(0.2f, 1f, 0.4f), radius);

        public void SpawnLightningChain(Vector3 pos)
            => SpawnFrom(_lightningBoltPool, pos, new Color(0.7f, 0.9f, 1f));

        public void SpawnPoisonField(Vector3 pos, float radius = 1f)
            => SpawnFromRadius(_poisonCloudPool, pos, new Color(0.3f, 0.85f, 0.2f), radius);

        public void SpawnShieldAura(Vector3 pos)
            => SpawnFrom(_shieldAuraPool, pos, new Color(1f, 0.85f, 0.15f));

        public void SpawnSlowField(Vector3 pos, float radius = 1f)
            => SpawnFromRadius(_slowAuraPool, pos, new Color(0.4f, 0.6f, 1f), radius);

        public void SpawnFrost(Vector3 pos, float radius)
        {
            if (!IsVfxEnabled() || _frostPool == null) return;
            var ps = _frostPool.Get();
            ps.transform.SetPositionAndRotation(pos, Quaternion.identity);
            var frostShape = ps.shape;
            frostShape.radius = Mathf.Max(0.3f, radius);
            ApplyTint(ps, new Color(0.5f, 0.9f, 1f));
            PlayRelease(ps, _frostPool);
        }

        public void SpawnPortal(Vector3 pos)
        {
            if (!IsVfxEnabled() || _portalPool == null) return;
            var ps = _portalPool.Get();
            ps.transform.SetPositionAndRotation(pos, Quaternion.identity);
            ApplyTint(ps, new Color(0.4f, 0.2f, 0.6f));
            PlayRelease(ps, _portalPool);
            StartCoroutine(PortalLightRoutine(pos));
        }

        public void SpawnFireBreath(Vector3 origin, Vector3 dir, float dist)
        {
            if (!IsVfxEnabled() || _fireBreathPool == null || dir.sqrMagnitude < 0.0001f) return;
            var ps = _fireBreathPool.Get();
            var pivot = new GameObject("FireBreath_Pivot");
            pivot.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(dir));
            ps.transform.SetParent(pivot.transform, false);
            ps.transform.localPosition = Vector3.zero;
            ps.transform.localRotation = Quaternion.identity;
            var fbShape = ps.shape;
            fbShape.length = dist;
            ps.gameObject.SetActive(true);
            ps.Play(true);
            StartCoroutine(FireBreathRelease(ps, pivot, _fireBreathPool, _root));
        }

        // ── Coroutines ────────────────────────────────────────────────────────

        private IEnumerator FireBreathRelease(ParticleSystem ps, GameObject pivot,
                                              ObjectPool<ParticleSystem> pool, Transform? root)
        {
            yield return GetWait(0.7f);
            ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            yield return GetWait(1.0f);
            if (ps != null && ps.gameObject != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.transform.SetParent(root, false);
                ps.transform.localScale = Vector3.one;
                ps.gameObject.SetActive(false);
                pool.Release(ps);
            }
            if (pivot != null) Destroy(pivot);
        }

        private IEnumerator PortalLightRoutine(Vector3 pos)
        {
            var go = new GameObject("PortalLight_VFX");
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.4f, 0.2f, 0.6f);
            l.range = 4f;
            l.intensity = 5f;
            float t = 0f;
            while (t < 0.3f) { t += Time.deltaTime; l.intensity = Mathf.Lerp(5f, 0f, t / 0.3f); yield return null; }
            Destroy(go);
        }

        // ── Pool internals ────────────────────────────────────────────────────

        private ObjectPool<ParticleSystem> MakePool(GameObject? prefab, string label)
            => new ObjectPool<ParticleSystem>(
                createFunc:       () => {
                    if (prefab == null)
                    {
                        Debug.LogWarning($"[VfxPool] MakePool({label}) prefab is null — creating fallback");
                        var go = new GameObject($"{label}_VFX_Fallback");
                        go.transform.SetParent(_root, false);
                        return go.AddComponent<ParticleSystem>();
                    }
                    var go2 = Instantiate(prefab, _root);
                    go2.name = $"{label}_VFX";
                    go2.SetActive(false);
                    return go2.GetComponent<ParticleSystem>() ?? go2.AddComponent<ParticleSystem>();
                },
                actionOnGet:      ps => ps.gameObject.SetActive(true),
                actionOnRelease:  ps => { ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                                          ps.transform.SetParent(_root, false); ps.transform.localScale = Vector3.one;
                                          ps.gameObject.SetActive(false); },
                actionOnDestroy:  ps => { if (ps) Destroy(ps.gameObject); },
                collectionCheck:  false,
                defaultCapacity:  DefaultCapacity,
                maxSize:          MaxPoolSize);

        private void PreWarm()
        {
            foreach (var pool in new[] { _impactPool, _deathPool, _explosionPool, _coinBurstPool,
                _hitFlashPool, _levelUpPool, _perkPickupPool, _frostPool, _portalPool,
                _fireBreathPool, _muzzleFlashPool, _upgradeBurstPool, _sparkPool, _upgradeConfettiPool })
            {
                if (pool == null) continue;
                var buf = new ParticleSystem[DefaultCapacity];
                for (int i = 0; i < DefaultCapacity; i++) buf[i] = pool.Get();
                for (int i = 0; i < DefaultCapacity; i++) pool.Release(buf[i]);
            }
        }

        private void SpawnFrom(ObjectPool<ParticleSystem>? pool, Vector3 pos, Color tint)
        {
            if (!IsVfxEnabled() || pool == null) return;
            var ps = pool.Get();
            ps.transform.SetPositionAndRotation(pos, Quaternion.identity);
            ApplyTint(ps, tint);
            PlayRelease(ps, pool);
        }

        private void SpawnFromRadius(ObjectPool<ParticleSystem>? pool, Vector3 pos, Color tint, float radius)
        {
            if (!IsVfxEnabled() || pool == null) return;
            var ps = pool.Get();
            if (ps == null) return;
            ps.transform.SetPositionAndRotation(pos, Quaternion.identity);
            var sh = ps.shape;
            sh.radius = Mathf.Max(0.3f, radius);
            ApplyTint(ps, tint);
            PlayRelease(ps, pool);
        }

        private static void ApplyTint(ParticleSystem ps, Color tint)
        {
            var m = ps.main;
            m.startColor = new ParticleSystem.MinMaxGradient(tint);
        }

        private static void ApplyLod(ParticleSystem ps)
        {
            if (_lodMultiplier >= 1f) return;
            var m = ps.main;
            m.maxParticles = Mathf.Max(1, Mathf.RoundToInt(m.maxParticles * _lodMultiplier));
        }

        private void PlayRelease(ParticleSystem ps, ObjectPool<ParticleSystem> pool)
        {
            ApplyLod(ps);
            var stopMain = ps.main;
            stopMain.stopAction = ParticleSystemStopAction.Callback;
            var h = ps.gameObject.GetComponent<VfxPoolReleaseOnStop>() ?? ps.gameObject.AddComponent<VfxPoolReleaseOnStop>();
            h.Setup(ps, pool, _root);
            ps.Play(true);
        }

        private static WaitForSeconds GetWait(float s)
        {
            float k = Mathf.Round(s * 20f) / 20f;
            if (!_waitCache.TryGetValue(k, out var w)) { w = new WaitForSeconds(k); _waitCache[k] = w; }
            return w;
        }

        private static bool IsVfxEnabled() => SettingsRegistry.Instance?.VFXEnabled ?? true;

        private GameObject MakeProcPrefab(string label, System.Action<ParticleSystem> configure)
        {
            var go = new GameObject($"Proc_{label}");
            go.SetActive(false);
            DontDestroyOnLoad(go);
            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            if (psr != null && _additiveMat != null) psr.material = new Material(_additiveMat);
            var main = ps.main;
            main.loop            = false;
            main.playOnAwake     = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction      = ParticleSystemStopAction.Disable;
            configure(ps);
            return go;
        }

        private static Gradient BuildPaletteGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[] {
                    new GradientColorKey(_confettiPalette[0], 0f),
                    new GradientColorKey(_confettiPalette[1], 0.33f),
                    new GradientColorKey(_confettiPalette[2], 0.66f),
                    new GradientColorKey(_confettiPalette[3], 1f),
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.85f, 0.5f),
                    new GradientAlphaKey(0f, 1f),
                });
            return g;
        }
    }

    public class VfxPoolReleaseOnStop : MonoBehaviour
    {
        private ParticleSystem? _ps;
        private ObjectPool<ParticleSystem>? _pool;
        private Transform? _root;

        public void Setup(ParticleSystem ps, ObjectPool<ParticleSystem> pool, Transform? root)
            => (_ps, _pool, _root) = (ps, pool, root);

        private void OnParticleSystemStopped()
        {
            if (_ps == null || _pool == null) return;
            if (_root != null) _ps.transform.SetParent(_root, false);
            _pool.Release(_ps);
        }
    }
}
