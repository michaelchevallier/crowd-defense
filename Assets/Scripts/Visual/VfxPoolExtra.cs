#nullable enable
using UnityEngine;
using UnityEngine.Pool;

namespace CrowdDefense.Visual
{
    // Partial class extension of VfxPool — holds the 9 new VFX pools from R6-PARITY-004-IMPL.
    // Keeps VfxPool.cs under 500 LOC by isolating pool fields + init for new effects.
    public partial class VfxPool
    {
        [SerializeField] private GameObject? electricCloudPrefab;
        [SerializeField] private GameObject? explosionSmallPrefab;
        [SerializeField] private GameObject? glyphDarkPrefab;
        [SerializeField] private GameObject? healAuraPrefab;
        [SerializeField] private GameObject? lightningBoltPrefab;
        [SerializeField] private GameObject? poisonCloudPrefab;
        [SerializeField] private GameObject? shieldAuraPrefab;
        [SerializeField] private GameObject? slowAuraPrefab;

        private ObjectPool<ParticleSystem>? _electricCloudPool;
        private ObjectPool<ParticleSystem>? _explosionSmallPool;
        private ObjectPool<ParticleSystem>? _glyphDarkPool;
        private ObjectPool<ParticleSystem>? _healAuraPool;
        private ObjectPool<ParticleSystem>? _lightningBoltPool;
        private ObjectPool<ParticleSystem>? _poisonCloudPool;
        private ObjectPool<ParticleSystem>? _shieldAuraPool;
        private ObjectPool<ParticleSystem>? _slowAuraPool;

        private void InitExtra()
        {
            electricCloudPrefab  ??= MakeProcPrefab("ElectricCloud",  ps => VfxPoolBindings.BuildElectricCloudModule(ps, _additiveMat));
            explosionSmallPrefab ??= MakeProcPrefab("ExplosionSmall", ps => VfxPoolBindings.BuildExplosionSmallModule(ps, _additiveMat));
            glyphDarkPrefab      ??= MakeProcPrefab("GlyphDark",      ps => VfxPoolBindings.BuildGlyphDarkModule(ps, _additiveMat));
            healAuraPrefab       ??= MakeProcPrefab("HealAura",       ps => VfxPoolBindings.BuildHealAuraModule(ps, _additiveMat));
            lightningBoltPrefab  ??= MakeProcPrefab("LightningBolt",  ps => VfxPoolBindings.BuildLightningBoltModule(ps, _additiveMat));
            poisonCloudPrefab    ??= MakeProcPrefab("PoisonCloud",    ps => VfxPoolBindings.BuildPoisonCloudModule(ps, _additiveMat));
            shieldAuraPrefab     ??= MakeProcPrefab("ShieldAura",     ps => VfxPoolBindings.BuildShieldAuraModule(ps, _additiveMat));
            slowAuraPrefab       ??= MakeProcPrefab("SlowAura",       ps => VfxPoolBindings.BuildSlowAuraModule(ps, _additiveMat));

            // smoke_gray wired as additional sub-emitter on explosion (gray puff variant)
            var smokeGrayPrefab = MakeProcPrefab("SmokeGray", ps => VfxPoolBindings.BuildSmokeGrayModule(ps, _additiveMat));
            VfxPoolBindings.AttachSubEmitter(explosionPrefab, smokeGrayPrefab);

            _electricCloudPool   = MakePool(electricCloudPrefab,   "ElectricCloud");
            _explosionSmallPool  = MakePool(explosionSmallPrefab,  "ExplosionSmall");
            _glyphDarkPool       = MakePool(glyphDarkPrefab,       "GlyphDark");
            _healAuraPool        = MakePool(healAuraPrefab,        "HealAura");
            _lightningBoltPool   = MakePool(lightningBoltPrefab,   "LightningBolt");
            _poisonCloudPool     = MakePool(poisonCloudPrefab,     "PoisonCloud");
            _shieldAuraPool      = MakePool(shieldAuraPrefab,      "ShieldAura");
            _slowAuraPool        = MakePool(slowAuraPrefab,        "SlowAura");

            PreWarmExtra();
        }

        private void PreWarmExtra()
        {
            foreach (var pool in new[] { _electricCloudPool, _explosionSmallPool, _glyphDarkPool,
                _healAuraPool, _lightningBoltPool, _poisonCloudPool, _shieldAuraPool, _slowAuraPool })
            {
                if (pool == null) continue;
                var buf = new ParticleSystem[DefaultCapacity];
                for (int i = 0; i < DefaultCapacity; i++) buf[i] = pool.Get();
                for (int i = 0; i < DefaultCapacity; i++) pool.Release(buf[i]);
            }
        }
    }
}
