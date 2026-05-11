#nullable enable
using System.Collections.Generic;
using CrowdDefense.Common;
using CrowdDefense.Data;
using UnityEngine;

namespace CrowdDefense.Systems
{
    public class AudioController : MonoSingleton<AudioController>
    {
        private const int PoolSize = 8;
        private const float MinReplayInterval = 0.028f;

        [SerializeField] private AudioClipRegistry? registry;

        private AudioSource[] _sfxPool = System.Array.Empty<AudioSource>();
        private AudioSource? _musicSource;
        private int _nextIdx;
        private readonly Dictionary<string, float> _lastPlayedAt = new();

        protected override void OnAwakeSingleton()
        {
            _sfxPool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject($"SFX_Pool_{i}");
                go.transform.SetParent(transform);
                _sfxPool[i] = go.AddComponent<AudioSource>();
                _sfxPool[i].playOnAwake = false;
            }

            var musicGo = new GameObject("Music_Source");
            musicGo.transform.SetParent(transform);
            _musicSource = musicGo.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
        }

        public void Play(string clipKey, float volMul = 1f)
        {
            var clip = registry?.Get(clipKey);
            if (clip == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[AudioController] Clip not found in registry: '{clipKey}'");
#endif
                return;
            }

            if (_lastPlayedAt.TryGetValue(clipKey, out float last) &&
                last + MinReplayInterval > Time.unscaledTime)
                return;

            var source = _sfxPool[_nextIdx++ % PoolSize];
            source.clip = clip;
            source.volume = Mathf.Clamp01(volMul);
            source.Play();

            _lastPlayedAt[clipKey] = Time.unscaledTime;
        }

        public void PlayRandom(string[] keys, float volMul = 1f)
        {
            if (keys == null || keys.Length == 0) return;
            Play(keys[Random.Range(0, keys.Length)], volMul);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (_musicSource == null) return;
            _musicSource.clip = clip;
            _musicSource.Play();
        }

        public void SetMasterVolume(float value) =>
            AudioListener.volume = Mathf.Clamp01(value);

        public void SetMuted(bool muted) =>
            AudioListener.pause = muted;
    }
}
