#nullable enable
using System.Collections;
using System.Collections.Generic;
using CrowdDefense.Common;
using CrowdDefense.Data;
using UnityEngine;
using UnityEngine.Audio;

namespace CrowdDefense.Systems
{
    [DefaultExecutionOrder(-50)]
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
        private readonly HashSet<string> _warned = new();
        private Coroutine? _musicFadeCo;
        private readonly Dictionary<string, AudioSource> _loopChannels = new();

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

            if (_lastPlayedAt.TryGetValue(clipKey, out float last) &&
                last + MinReplayInterval > Time.unscaledTime)
                return;
            _lastPlayedAt[clipKey] = Time.unscaledTime;

            if (clip != null)
            {
                var source = _sfxPool[_nextIdx++ % PoolSize];
                source.clip = clip;
                source.volume = Mathf.Clamp01(volMul);
                source.Play();
                if (clipKey == "boss_roar" || clipKey == "victory")
                    MusicManager.Instance?.DuckMusic(1.5f);
                return;
            }

            // Fallback: procedural beep for missing SFX clips
            StartCoroutine(PlayProceduralBeepCo(clipKey, volMul));
        }

        private static float FreqForKey(string key)
        {
            // 220–1760 Hz spread across 3 octaves, deterministic per name
            uint h = 2166136261u;
            foreach (char c in key) h = (h ^ c) * 16777619u;
            return 220f * Mathf.Pow(2f, (h % 64u) / 21f);
        }

        private IEnumerator PlayProceduralBeepCo(string clipKey, float volMul, float pitch = 1f)
        {
            if (this == null) yield break;
            const int sampleRate = 44100;
            float freq = FreqForKey(clipKey) * pitch;
            const float duration = 0.07f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - t / duration;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }
            var beep = AudioClip.Create("_beep", samples, 1, sampleRate, false);
            beep.SetData(data, 0);
            var source = _sfxPool[_nextIdx++ % PoolSize];
            source.clip = beep;
            source.volume = Mathf.Clamp01(volMul * 0.25f);
            source.Play();
            yield return new WaitForSeconds(duration + 0.05f);
            if (this != null) Destroy(beep);
        }

        private IEnumerator PlayProceduralBeep3DCo(string clipKey, Vector3 worldPos, float volMul)
        {
            if (this == null) yield break;
            const int sampleRate = 44100;
            float freq = FreqForKey(clipKey);
            const float duration = 0.07f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - t / duration;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }
            var beep = AudioClip.Create("_beep_3d", samples, 1, sampleRate, false);
            beep.SetData(data, 0);
            var src = _sfxPool[_nextIdx++ % PoolSize];
            src.transform.position = worldPos;
            src.clip = beep;
            src.volume = Mathf.Clamp01(volMul * 0.25f);
            src.Play();
            yield return new WaitForSeconds(duration + 0.05f);
            if (this != null) Destroy(beep);
        }

        // 2-D pitched play — falls back to procedural beep at shifted freq if clip missing
        public void PlayPitched(string clipKey, float volMul = 1f, float pitch = 1f)
        {
            if (registry == null) LoadAudioRegistry();
            var clip = registry?.Get(clipKey);

            if (_lastPlayedAt.TryGetValue(clipKey, out float last) &&
                last + MinReplayInterval > Time.unscaledTime)
                return;
            _lastPlayedAt[clipKey] = Time.unscaledTime;

            if (clip != null)
            {
                var source = _sfxPool[_nextIdx++ % PoolSize];
                source.clip   = clip;
                source.pitch  = pitch;
                source.volume = Mathf.Clamp01(volMul);
                source.Play();
                return;
            }

            // Fallback: procedural beep, pitch applied as freq multiplier
            StartCoroutine(PlayProceduralBeepCo(clipKey, volMul, pitch));
        }

        public void Play3D(string clipKey, Vector3 worldPos, float volMul = 1f)
        {
            if (registry == null) LoadAudioRegistry();
            var clip = registry?.Get(clipKey);
            if (clip == null)
            {
                // Fallback: procedural beep at world position
                StartCoroutine(PlayProceduralBeep3DCo(clipKey, worldPos, volMul));
                return;
            }
            AudioSource.PlayClipAtPoint(clip, worldPos, Mathf.Clamp01(volMul));
        }

        // Plays a 3D clip with pitch shift. Falls back silently if clip missing.
        public void Play3DPitched(string clipKey, Vector3 worldPos, float volMul = 1f, float pitch = 1f)
        {
            if (registry == null) LoadAudioRegistry();
            var clip = registry?.Get(clipKey);
            if (clip == null)
            {
                if (!_warned.Contains(clipKey))
                {
                    _warned.Add(clipKey);
                    Debug.LogWarning($"[AudioController] Missing clip: {clipKey}");
                }
                return;
            }
            var src = _sfxPool[_nextIdx++ % PoolSize];
            src.transform.position = worldPos;
            src.clip  = clip;
            src.pitch  = pitch;
            src.volume = Mathf.Clamp01(volMul);
            src.Play();
        }

        public AudioClip? GetClip(string clipKey)
        {
            if (registry == null) LoadAudioRegistry();
            return registry?.Get(clipKey);
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
            if (this == null || _musicSource == null) yield break;
            if (_musicSource.isPlaying)
            {
                yield return FadeMusicVolumeCo(0f, fadeSeconds * 0.5f);
                if (this == null) yield break;
                _musicSource.Stop();
            }
            _musicSource.clip = clip;
            _musicSource.volume = 0f;
            _musicSource.Play();
            yield return FadeMusicVolumeCo(1f, fadeSeconds);
            if (this != null) _musicFadeCo = null;
        }

        private IEnumerator FadeOutMusicCo(float fadeSeconds)
        {
            if (this == null || _musicSource == null) yield break;
            yield return FadeMusicVolumeCo(0f, fadeSeconds);
            if (this == null) yield break;
            _musicSource.Stop();
            _musicFadeCo = null;
        }

        private IEnumerator FadeMusicVolumeCo(float target, float duration)
        {
            if (this == null || _musicSource == null) yield break;
            float start = _musicSource.volume;
            if (duration <= 0f)
            {
                _musicSource.volume = target;
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                if (this == null) yield break;
                t += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
                yield return null;
            }
            if (this != null) _musicSource.volume = target;
        }

        public void SetVolume(string bus, float zeroToOne)
        {
            switch (bus)
            {
                case "master": SetMasterVolume(zeroToOne); break;
                case "sfx":    SetSFXVolume(zeroToOne);    break;
                case "music":  SetMusicVolume(zeroToOne);  break;
                case "ui":     SetUIVolume(zeroToOne);     break;
            }
        }

        public void SetMasterVolume(float zeroToOne)
        {
            SetMixerMasterVolume(zeroToOne);
            SetGlobalListenerVolume(zeroToOne);
        }

        public void SetMixerMasterVolume(float zeroToOne)
        {
            if (mixer != null) mixer.SetFloat("Master_Volume", LinearToDb(zeroToOne));
        }

        public void SetGlobalListenerVolume(float zeroToOne)
        {
            AudioListener.volume = Mathf.Clamp01(zeroToOne);
        }

        public void SetSFXVolume(float zeroToOne)
        {
            if (mixer != null && mixer.SetFloat("SFX_Volume", LinearToDb(zeroToOne))) return;
            foreach (var src in _sfxPool) src.volume = Mathf.Clamp01(zeroToOne);
        }

        public void SetMusicVolume(float zeroToOne)
        {
            if (mixer != null && mixer.SetFloat("Music_Volume", LinearToDb(zeroToOne))) return;
            if (_musicSource != null) _musicSource.volume = Mathf.Clamp01(zeroToOne);
            MusicManager.Instance?.SetMusicVolume(zeroToOne);
        }

        public void SetUIVolume(float zeroToOne)
        {
            if (mixer != null && mixer.SetFloat("UI_Volume", LinearToDb(zeroToOne))) return;
        }

        public void PlayLoop(AudioClip clip, string channel, float volume = 1f)
        {
            if (!_loopChannels.TryGetValue(channel, out var src) || src == null)
            {
                var go = new GameObject($"Loop_{channel}");
                go.transform.SetParent(transform);
                src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = true;
                _loopChannels[channel] = src;
            }
            if (src.clip == clip && src.isPlaying) return;
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume);
            src.Play();
        }

        public void StopChannel(string channel)
        {
            if (_loopChannels.TryGetValue(channel, out var src) && src != null)
                src.Stop();
        }

        public void StopAllSfx()
        {
            foreach (var src in _sfxPool)
            {
                if (src != null && src.isPlaying) src.Stop();
            }
            foreach (var kvp in _loopChannels)
            {
                kvp.Value?.Stop();
            }
            _loopChannels.Clear();
        }

        public void SetMuted(bool muted) =>
            AudioListener.pause = muted;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F4)) StopAllSfx();
        }
#endif

        private static float LinearToDb(float zeroToOne)
        {
            float clamped = Mathf.Clamp(zeroToOne, 0.0001f, 1f);
            float db = Mathf.Log10(clamped) * 20f;
            return Mathf.Clamp(db, MinDb, MaxDb);
        }
    }
}
