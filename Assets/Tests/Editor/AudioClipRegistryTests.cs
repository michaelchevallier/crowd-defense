#nullable enable
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Tests.Editor
{
    public class AudioClipRegistryTests
    {
        private AudioClipRegistry? _registry;
        private AudioClip? _clipA;
        private AudioClip? _clipB;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<AudioClipRegistry>();
            _clipA = AudioClip.Create("clipA", 1024, 1, 44100, false);
            _clipB = AudioClip.Create("clipB", 1024, 1, 44100, false);

            InjectEntries(_registry, new[]
            {
                new AudioClipRegistry.Entry { Key = "tower_shoot", Clip = _clipA },
                new AudioClipRegistry.Entry { Key = "enemy_hit", Clip = _clipB }
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (_registry != null) Object.DestroyImmediate(_registry);
            if (_clipA != null) Object.DestroyImmediate(_clipA);
            if (_clipB != null) Object.DestroyImmediate(_clipB);
        }

        [Test]
        public void Get_ExistingKey_ReturnsClip()
        {
            Assert.IsNotNull(_registry);
            var clip = _registry!.Get("tower_shoot");
            Assert.IsNotNull(clip);
            Assert.AreSame(_clipA, clip);
        }

        [Test]
        public void Get_MissingKey_ReturnsNull()
        {
            Assert.IsNotNull(_registry);
            Assert.IsNull(_registry!.Get("missing_key"));
        }

        [Test]
        public void Has_ExistingKey_ReturnsTrue()
        {
            Assert.IsNotNull(_registry);
            Assert.IsTrue(_registry!.Has("tower_shoot"));
            Assert.IsTrue(_registry!.Has("enemy_hit"));
        }

        [Test]
        public void Has_MissingKey_ReturnsFalse()
        {
            Assert.IsNotNull(_registry);
            Assert.IsFalse(_registry!.Has("missing"));
        }

        // === Helpers ===

        private static void InjectEntries(AudioClipRegistry registry, AudioClipRegistry.Entry[] entries)
        {
            var entriesField = typeof(AudioClipRegistry).GetField("entries",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(entriesField, "AudioClipRegistry.entries private field must exist.");
            entriesField!.SetValue(registry, entries);

            var cacheField = typeof(AudioClipRegistry).GetField("_cache",
                BindingFlags.Instance | BindingFlags.NonPublic);
            cacheField?.SetValue(registry, null);
        }
    }
}
