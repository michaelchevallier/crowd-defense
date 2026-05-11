#nullable enable
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Tests.Runtime
{
    public class AudioControllerTests
    {
        private GameObject? _host;
        private AudioController? _controller;
        private AudioClipRegistry? _registry;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _host = new GameObject("Test_AudioController");
            _controller = _host.AddComponent<AudioController>();
            _registry = ScriptableObject.CreateInstance<AudioClipRegistry>();

            // Inject the private 'registry' field via reflection (no UnityEditor dep).
            var registryField = typeof(AudioController)
                .GetField("registry", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(registryField, "AudioController.registry private field must exist.");
            registryField!.SetValue(_controller, _registry);

            // Wait one frame so Awake/OnAwakeSingleton run + pool sources created.
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.DestroyImmediate(_host);
            if (_registry != null) Object.DestroyImmediate(_registry);
            AudioListener.volume = 1f;
            AudioListener.pause = false;
        }

        [Test]
        public void Play_MissingClipKey_IsNoOp()
        {
            Assert.IsNotNull(_controller);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                ".*\\[AudioController\\] Clip not found in registry: 'nonexistent_key'.*"));
            Assert.DoesNotThrow(() => _controller!.Play("nonexistent_key"));
        }

        [Test]
        public void Play_EmptyKey_IsNoOpNoThrow()
        {
            Assert.IsNotNull(_controller);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                ".*\\[AudioController\\] Clip not found.*"));
            Assert.DoesNotThrow(() => _controller!.Play(""));
        }

        [UnityTest]
        public IEnumerator Play_SameClipWithin28ms_DoesNotThrow()
        {
            Assert.IsNotNull(_controller);
            Assert.IsNotNull(_registry);

            // Inject a registry entry via reflection (avoids UnityEditor dep).
            var clip = AudioClip.Create("test_clip", 44100, 1, 44100, false);
            var entriesField = typeof(AudioClipRegistry)
                .GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(entriesField, "AudioClipRegistry.entries private field must exist.");
            var entry = new AudioClipRegistry.Entry { Key = "throttle_clip", Clip = clip };
            entriesField!.SetValue(_registry, new[] { entry });

            // Reset internal cache so reflection-injected entries are visible.
            var cacheField = typeof(AudioClipRegistry)
                .GetField("_cache", BindingFlags.Instance | BindingFlags.NonPublic);
            cacheField?.SetValue(_registry, null);

            Assert.IsTrue(_registry!.Has("throttle_clip"), "Registry must expose injected key.");

            // Anti-replay invariant : two consecutive Play() within 28ms must not throw and must skip 2nd.
            Assert.DoesNotThrow(() => _controller!.Play("throttle_clip", 0.5f));
            Assert.DoesNotThrow(() => _controller!.Play("throttle_clip", 0.5f));

            yield return null;
        }

        [Test]
        public void SetMasterVolume_OutOfRange_IsClamped()
        {
            Assert.IsNotNull(_controller);
            _controller!.SetMasterVolume(2f);
            Assert.AreEqual(1f, AudioListener.volume, 0.001f, "Above 1f must clamp to 1f.");

            _controller.SetMasterVolume(-0.5f);
            Assert.AreEqual(0f, AudioListener.volume, 0.001f, "Below 0f must clamp to 0f.");

            _controller.SetMasterVolume(0.42f);
            Assert.AreEqual(0.42f, AudioListener.volume, 0.001f, "In-range value must pass through.");
        }

        [Test]
        public void SetMuted_TogglesAudioListenerPause()
        {
            Assert.IsNotNull(_controller);
            _controller!.SetMuted(true);
            Assert.IsTrue(AudioListener.pause);

            _controller.SetMuted(false);
            Assert.IsFalse(AudioListener.pause);
        }

        [Test]
        public void Instance_IsSet_AfterAwake()
        {
            Assert.IsNotNull(AudioController.Instance, "MonoSingleton must wire Instance during Awake.");
        }
    }
}
