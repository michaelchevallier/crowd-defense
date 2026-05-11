#nullable enable
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Tests.Editor
{
    public class AssetRegistryTests
    {
        private AssetRegistry? _registry;
        private GameObject? _prefab1;
        private GameObject? _prefab2;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<AssetRegistry>();
            _prefab1 = new GameObject("TestPrefab_1");
            _prefab2 = new GameObject("TestPrefab_2");

            InjectEntries(_registry, new[]
            {
                new AssetRegistry.Entry { Key = "tower_archer", Prefab = _prefab1 },
                new AssetRegistry.Entry { Key = "enemy_crawler", Prefab = _prefab2 }
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (_registry != null) Object.DestroyImmediate(_registry);
            if (_prefab1 != null) Object.DestroyImmediate(_prefab1);
            if (_prefab2 != null) Object.DestroyImmediate(_prefab2);
        }

        [Test]
        public void Get_ExistingKey_ReturnsPrefab()
        {
            Assert.IsNotNull(_registry);
            var prefab = _registry!.Get("tower_archer");
            Assert.IsNotNull(prefab, "Get('tower_archer') must return non-null.");
            Assert.AreSame(_prefab1, prefab);
        }

        [Test]
        public void Get_MissingKey_ReturnsNull()
        {
            Assert.IsNotNull(_registry);
            var prefab = _registry!.Get("nonexistent_key");
            Assert.IsNull(prefab, "Get('nonexistent_key') must return null.");
        }

        [Test]
        public void Get_EmptyKey_ReturnsNull()
        {
            Assert.IsNotNull(_registry);
            Assert.IsNull(_registry!.Get(""));
        }

        [Test]
        public void Has_ExistingKey_ReturnsTrue()
        {
            Assert.IsNotNull(_registry);
            Assert.IsTrue(_registry!.Has("tower_archer"));
            Assert.IsTrue(_registry!.Has("enemy_crawler"));
        }

        [Test]
        public void Has_MissingKey_ReturnsFalse()
        {
            Assert.IsNotNull(_registry);
            Assert.IsFalse(_registry!.Has("nonexistent"));
        }

        [Test]
        public void Get_AfterOnEnableRebuild_StillResolves()
        {
            Assert.IsNotNull(_registry);
            // Get triggers cache build.
            Assert.IsNotNull(_registry!.Get("tower_archer"));

            // Force cache invalidation by clearing _cache via reflection (simulates OnEnable replay).
            var cacheField = typeof(AssetRegistry).GetField("_cache",
                BindingFlags.Instance | BindingFlags.NonPublic);
            cacheField?.SetValue(_registry, null);

            // Second Get should rebuild cache and still resolve.
            Assert.IsNotNull(_registry!.Get("tower_archer"));
        }

        // === Helpers ===

        private static void InjectEntries(AssetRegistry registry, AssetRegistry.Entry[] entries)
        {
            var entriesField = typeof(AssetRegistry).GetField("entries",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(entriesField, "AssetRegistry.entries private field must exist.");
            entriesField!.SetValue(registry, entries);

            var cacheField = typeof(AssetRegistry).GetField("_cache",
                BindingFlags.Instance | BindingFlags.NonPublic);
            cacheField?.SetValue(registry, null);
        }
    }
}
