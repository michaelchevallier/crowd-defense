#nullable enable
using System.Collections;
using System.Collections.Generic;
using CrowdDefense.Common;
using CrowdDefense.Data;
using UnityEngine;
using UnityEngine.Audio;

namespace CrowdDefense.Systems
{
    public class AudioController : MonoSingleton<AudioController>
    {
        private const int PoolSize = 8;
        private const float MinReplayInterval = 0.028f;
        private const float MinDb = -80f;
        private const float MaxDb = 0f;

        [Header("Registry (auto-loaded from Resources if null)")]
        [SerializeField] private AudioClipRegistry? registry;

        [Header("Mixer routing (assign Inspector for full pipeline)")]
        [SerializeField] private AudioMixer? mixer;
        [SerializeField] private AudioMixerGroup? sfxGroup;
        [SerializeField] private AudioMixerGroup? musicGroup;
        [SerializeField] private AudioMixerGroup? uiGroup;

        private AudioSource[] _sfxPool = System.Array.Empty<AudioSource>();
        private AudioSource? _musicSource;
        private int _nextIdx;
        private readonly Dictionary<string, float> _lastPlayedAt = new();
        private Coroutine? _musicFadeCo;

        protected override void OnAwakeSingleton()
        {
            LoadAudioRegistry();

            _sfxPool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject($"SFX_Pool_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                if (sfxGroup != null) src.outputAudioMixerGroup = sfxGroup;
                _sfxPool[i] = src;
            }

            var musicGo = new GameObject("Music_Source");
            musicGo.transform.SetParent(transform);
            _musicSource = musicGo.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            if (musicGroup != null) _musicSource.outputAudioMixerGroup = musicGroup;
        }

        public void LoadAudioRegistry()
        {
            if (registry != null) return;
            registry = Resources.Load<AudioClipRegistry>("AudioClipRegistry");
#if UNITY_EDITOR
            if (registry == null)
                Debug.LogWarning("[AudioController] AudioClipRegistry not assigned and not found in Resources/.");
#endif
        }

        public void Play(string clipKey, float volMul = 1f)
        {
            if (registry == null) LoadAudioRegistry();
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

        public void PlayMusic(AudioClip clip, float fadeMs = 500f)
        {
            if (_musicSource == null) return;
            if (_musicFadeCo != null) StopCoroutine(_musicFadeCo);
            _musicFadeCo = StartCoroutine(FadeInMusicCo(clip, fadeMs / 1000f));
        }

        public void StopMusic(float fadeMs = 500f)
        {
            if (_musicSource == null) return;
            if (_musicFadeCo != null) StopCoroutine(_musicFadeCo);
            _musicFadeCo = StartCoroutine(FadeOutMusicCo(fadeMs / 1000f));
        }

        private IEnumerator FadeInMusicCo(AudioClip clip, float fadeSeconds)
        {
            if (_musicSource == null) yield break;
            if (_musicSource.isPlaying)
            {
                yield return FadeMusicVolumeCo(0f, fadeSeconds * 0.5f);
                _musicSource.Stop();
            }
            _musicSource.clip = clip;
            _musicSource.volume = 0f;
            _musicSource.Play();
            yield return FadeMusicVolumeCo(1f, fadeSeconds);
            _musicFadeCo = null;
        }

        private IEnumerator FadeOutMusicCo(float fadeSeconds)
        {
            if (_musicSource == null) yield break;
            yield return FadeMusicVolumeCo(0f, fadeSeconds);
            _musicSource.Stop();
            _musicFadeCo = null;
        }

        private IEnumerator FadeMusicVolumeCo(float target, float duration)
        {
            if (_musicSource == null) yield break;
            float start = _musicSource.volume;
            if (duration <= 0f)
            {
                _musicSource.volume = target;
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
                yield return null;
            }
            _musicSource.volume = target;
        }

        public void SetMasterVolume(float zeroToOne)
        {
            if (mixer != null && mixer.SetFloat("MasterVol", LinearToDb(zeroToOne))) return;
            AudioListener.volume = Mathf.Clamp01(zeroToOne);
        }

        public void SetSFXVolume(float zeroToOne)
        {
            if (mixer == null) return;
            mixer.SetFloat("SFXVol", LinearToDb(zeroToOne));
        }

        public void SetMusicVolume(float zeroToOne)
        {
            if (mixer == null) return;
            mixer.SetFloat("MusicVol", LinearToDb(zeroToOne));
        }

        public void SetUIVolume(float zeroToOne)
        {
            if (mixer == null) return;
            mixer.SetFloat("UIVol", LinearToDb(zeroToOne));
        }

        public void SetMuted(bool muted) =>
            AudioListener.pause = muted;

        private static float LinearToDb(float zeroToOne)
        {
            float clamped = Mathf.Clamp(zeroToOne, 0.0001f, 1f);
            float db = Mathf.Log10(clamped) * 20f;
            return Mathf.Clamp(db, MinDb, MaxDb);
        }
    }
}
