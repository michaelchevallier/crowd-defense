#nullable enable
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CrowdDefense.Visual;

namespace CrowdDefense.Tests.Runtime
{
    public class VfxPoolTests
    {
        private GameObject? _host;
        private VfxPool? _pool;
        private GameObject? _impactPrefab;
        private GameObject? _deathPrefab;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Build minimal prefab GameObjects with ParticleSystem.
            _impactPrefab = BuildPrefab("ImpactPrefab", autoDestroy: false);
            _deathPrefab = BuildPrefab("DeathPrefab", autoDestroy: false);

            _host = new GameObject("Test_VfxPool");
            _pool = _host.AddComponent<VfxPool>();

            // Inject the private SerializeField prefabs via reflection.
            InjectField(_pool, "impactPrefab", _impactPrefab);
            InjectField(_pool, "deathPrefab", _deathPrefab);
            InjectField(_pool, "auraPrefab", _impactPrefab);
            InjectField(_pool, "coinPickupPrefab", _impactPrefab);

            // Awake runs but BuildPool already executed before injection.
            // We need to re-run OnAwakeSingleton; simplest : destroy+recreate after inject.
            Object.DestroyImmediate(_host);
            _host = new GameObject("Test_VfxPool");
            _pool = _host.AddComponent<VfxPool>();
            InjectField(_pool, "impactPrefab", _impactPrefab);
            InjectField(_pool, "deathPrefab", _deathPrefab);
            InjectField(_pool, "auraPrefab", _impactPrefab);
            InjectField(_pool, "coinPickupPrefab", _impactPrefab);

            // Manually invoke OnAwakeSingleton post-inject (since Awake already fired before inject).
            var onAwake = typeof(VfxPool).GetMethod("OnAwakeSingleton",
                BindingFlags.Instance | BindingFlags.NonPublic);
            onAwake?.Invoke(_pool, null);

            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.DestroyImmediate(_host);
            if (_impactPrefab != null) Object.DestroyImmediate(_impactPrefab);
            if (_deathPrefab != null) Object.DestroyImmediate(_deathPrefab);
        }

        [Test]
        public void Instance_IsSet_AfterAwake()
        {
            Assert.IsNotNull(VfxPool.Instance, "MonoSingleton must wire Instance during Awake.");
        }

        [Test]
        public void SpawnImpact_AtPos_DoesNotThrow_WithPrefabsAssigned()
        {
            Assert.IsNotNull(_pool);
            Assert.DoesNotThrow(() => _pool!.SpawnImpact(Vector3.zero, Color.red));
        }

        [Test]
        public void SpawnDeath_AtPos_DoesNotThrow_WithPrefabsAssigned()
        {
            Assert.IsNotNull(_pool);
            Assert.DoesNotThrow(() => _pool!.SpawnDeath(Vector3.zero, Color.green, isBoss: false));
            Assert.DoesNotThrow(() => _pool!.SpawnDeath(Vector3.zero, Color.green, isBoss: true));
        }

        [Test]
        public void SpawnCoinPickup_AtPos_DoesNotThrow()
        {
            Assert.IsNotNull(_pool);
            Assert.DoesNotThrow(() => _pool!.SpawnCoinPickup(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator SpawnImpact_ReusesPoolOnRepeatedCall()
        {
            Assert.IsNotNull(_pool);

            // Spawn 3× — pool should reuse instances (instead of unbounded Instantiate).
            for (int i = 0; i < 3; i++)
            {
                _pool!.SpawnImpact(new Vector3(i, 0, 0), Color.white);
            }
            yield return null;

            // Count child GOs under the pool root. With pool reuse + 3 spawns short-lived,
            // we expect <= 3 (could be less if first one already released).
            int children = _host!.transform.childCount;
            Assert.LessOrEqual(children, 3, "Pool should not Instantiate beyond N=3 spawns.");
        }

        [Test]
        public void SpawnAura_ReturnsParticleSystem()
        {
            Assert.IsNotNull(_pool);
            var parent = new GameObject("AuraParent");
            try
            {
                var ps = _pool!.SpawnAura(parent.transform, Color.cyan, isBoss: false);
                Assert.IsNotNull(ps, "SpawnAura must return a ParticleSystem instance.");
                _pool.ReleaseAura(ps);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        // === Helpers ===

        private static GameObject BuildPrefab(string name, bool autoDestroy)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.1f;
            main.startLifetime = 0.1f;
            main.loop = false;
            main.startSize = 0.1f;
            return go;
        }

        private static void InjectField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' must exist on {target.GetType().Name}.");
            field!.SetValue(target, value);
        }
    }
}
