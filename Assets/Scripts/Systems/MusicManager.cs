#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using CrowdDefense.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Multi-track music with crossfade + ducking, ported from V5 MusicManager.js.
    /// Delegates all AudioSource routing to AudioController.MusicMixerGroup.
    /// Responds to LevelThemeChangedEvent and BossEncounteredEvent via EventManager.
    /// </summary>
    public class MusicManager : MonoSingleton<MusicManager>
    {
        private const float CrossfadeDuration = 2f;
        private const float BossCrossfadeDuration = 2f;
        private const float CombatFadeIn = 1.5f;
        private const float CombatFadeOut = 2f;
        private const float BossFallbackVolBoost = 1.2f;
        private const float DuckMultiplier = 0.35f;
        private const float StingDuckDuration = 4.5f;

        [Header("Music clips — assign in Inspector or via code")]
        [SerializeField] private AudioClip? menuTheme;
        [SerializeField] private AudioClip? gameplayCalmTheme;
        [SerializeField] private AudioClip? gameplayIntenseTheme;
        [SerializeField] private AudioClip? bossTheme;
        [SerializeField] private AudioClip? victorySting;
        [SerializeField] private AudioClip? defeatSting;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;

        [Header("Mixer group (optional — inherit from AudioController if null)")]
        [SerializeField] private UnityEngine.Audio.AudioMixerGroup? musicMixerGroup;

        private readonly Dictionary<string, AudioClip?> _tracks = new();
        private readonly Dictionary<string, float> _trackVolMul = new();
        private readonly Dictionary<string, AudioSource> _sources = new();
        private readonly Dictionary<string, AudioClip?> _stings = new();
        private readonly Dictionary<string, float> _stingVolMul = new();
        private readonly Dictionary<string, float> _stingDuckMs = new();
        private readonly Dictionary<string, AudioSource> _stingSources = new();

        private string? _currentTrack;
        private string? _activeSting;
        private float _duckUntilTime;
        private Coroutine? _duckCo;
        private bool _muted;

        protected override void OnAwakeSingleton()
        {
            // Define tracks matching V5 TRACKS table
            RegisterTrack("menu",    () => menuTheme,           0.85f);
            RegisterTrack("calm",    () => gameplayCalmTheme,   0.75f);
            RegisterTrack("intense", () => gameplayIntenseTheme, 1.0f);
            RegisterTrack("boss",    () => bossTheme,           1.1f);

            RegisterSting("victory", () => victorySting, 0.9f, StingDuckDuration);
            RegisterSting("defeat",  () => defeatSting,  0.9f, StingDuckDuration);

            BuildSources();
        }

        private void Start()
        {
            SceneManager.activeSceneChanged += HandleSceneChange;
            HandleSceneChange(default, SceneManager.GetActiveScene());

            LevelEvents.OnLevelStart += OnLevelStart;

            var wm = WaveManager.Instance;
            if (wm != null)
            {
                wm.OnWaveStart  += OnWaveStarted;
                wm.OnWaveCleared += OnWaveCleared;
            }

            var em = EventManager.Instance;
            if (em == null) return;
            em.Subscribe<LevelThemeChangedEvent>(OnLevelThemeChanged);
            em.Subscribe<BossEncounteredEvent>(OnBossEncountered);
            em.Subscribe<BossDefeatedEvent>(OnBossDefeated);
        }

        protected override void OnDestroySingleton()
        {
            SceneManager.activeSceneChanged -= HandleSceneChange;

            LevelEvents.OnLevelStart -= OnLevelStart;

            var wm = WaveManager.Instance;
            if (wm != null)
            {
                wm.OnWaveStart  -= OnWaveStarted;
                wm.OnWaveCleared -= OnWaveCleared;
            }

            var em = EventManager.Instance;
            if (em == null) return;
            em.Unsubscribe<LevelThemeChangedEvent>(OnLevelThemeChanged);
            em.Unsubscribe<BossEncounteredEvent>(OnBossEncountered);
            em.Unsubscribe<BossDefeatedEvent>(OnBossDefeated);
        }

        // ── Public API ──────────────────────────────────────────────────────

        /// <summary>
        /// Crossfade to a clip loaded from Resources/Audio/Music/. Maps well-known
        /// names: "menu_theme" → menu, "wave_combat" → calm, "boss_fight" → boss.
        /// Falls back to the named track key if no mapping exists.
        /// </summary>
        public void CrossfadeTo(string clipName, float fadeDur = 2f)
        {
            string track = clipName switch
            {
                "menu_theme"  => "menu",
                "wave_combat" => "calm",
                "boss_fight"  => "boss",
                _             => clipName,
            };
            PlayWithCrossfade(track, fadeDur);
        }

        /// <summary>
        /// Select and crossfade to a named track ("menu", "calm", "intense", "boss").
        /// </summary>
        public void Play(string trackName)
        {
            if (!_tracks.ContainsKey(trackName)) return;
            StopSting();

            if (_currentTrack == trackName)
            {
                EnsurePlaying(trackName);
                return;
            }

            string? prev = _currentTrack;
            _currentTrack = trackName;

            if (prev != null && _sources.TryGetValue(prev, out var prevSrc))
                StartCoroutine(FadeCo(prevSrc, 0f, CrossfadeDuration, stopAfter: true));

            if (_sources.TryGetValue(trackName, out var nextSrc))
            {
                var clip = _tracks[trackName];
                if (clip != null && nextSrc.clip != clip)
                {
                    nextSrc.clip = clip;
                    nextSrc.loop = true;
                }
                if (!_muted && clip != null)
                {
                    if (!nextSrc.isPlaying)
                    {
                        nextSrc.volume = 0f;
                        nextSrc.Play();
                    }
                    float target = TargetVol(trackName);
                    StartCoroutine(FadeCo(nextSrc, target, CrossfadeDuration));
                }
            }
        }

        /// <summary>
        /// Play a one-shot sting (victory / defeat) and duck BGM while it plays.
        /// </summary>
        public void PlaySting(string stingName)
        {
            if (_muted || !_stings.ContainsKey(stingName)) return;
            if (_activeSting != null && _activeSting != stingName) StopSting();

            _activeSting = stingName;
            _duckUntilTime = Time.unscaledTime + _stingDuckMs[stingName];
            ApplyAllVolumes(0.25f);

            if (_stingSources.TryGetValue(stingName, out var src))
            {
                var clip = _stings[stingName];
                if (clip != null)
                {
                    src.clip = clip;
                    src.volume = Mathf.Min(1f, musicVolume * _stingVolMul[stingName]);
                    src.Play();
                }
            }

            if (_duckCo != null) StopCoroutine(_duckCo);
            _duckCo = StartCoroutine(DuckEndCo(_stingDuckMs[stingName]));
        }

        /// <summary>
        /// Temporarily duck music (e.g. boss roar). Duration in seconds.
        /// </summary>
        public void Duck(float duration)
        {
            _duckUntilTime = Time.unscaledTime + duration;
            ApplyAllVolumes(0.25f);
            if (_duckCo != null) StopCoroutine(_duckCo);
            _duckCo = StartCoroutine(DuckEndCo(duration));
        }

        /// <summary>
        /// Duck music by <paramref name="ratio"/> for <paramref name="depth"/> seconds,
        /// then recover linearly over <paramref name="recover"/> seconds.
        /// Called by AudioController on critical SFX (boss_roar, victory).
        /// </summary>
        public void DuckMusic(float depth, float ratio = 0.3f, float recover = 0.5f)
        {
            if (_duckCo != null) StopCoroutine(_duckCo);
            _duckCo = StartCoroutine(DuckRoutine(depth, ratio, recover));
        }

        private IEnumerator DuckRoutine(float depth, float ratio, float recover)
        {
            if (_currentTrack == null || !_sources.TryGetValue(_currentTrack, out var src)) yield break;
            float orig = src.volume;
            src.volume = orig * ratio;
            yield return new WaitForSecondsRealtime(depth);
            float t = 0f;
            while (t < recover)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(orig * ratio, orig, t / recover);
                yield return null;
            }
            src.volume = orig;
            _duckCo = null;
        }

        /// <summary>
        /// Select track based on level theme string from LevelData.
        /// Mapping: "boss" → boss track, "intense" → intense, else calm.
        /// </summary>
        public void PlayLevel(string? levelTheme)
        {
            string track = levelTheme switch
            {
                "boss"    => "boss",
                "intense" => "intense",
                _         => "calm",
            };
            Play(track);
        }

        public void Stop()
        {
            StopSting();
            if (_currentTrack == null) return;
            if (_sources.TryGetValue(_currentTrack, out var src))
                StartCoroutine(FadeCo(src, 0f, 0.5f, stopAfter: true));
            _currentTrack = null;
        }

        public void SetMusicVolume(float zeroToOne)
        {
            musicVolume = Mathf.Clamp01(zeroToOne);
            ApplyAllVolumes(0.2f);
        }

        public float GetMusicVolume() => musicVolume;

        public void SetMuted(bool muted)
        {
            _muted = muted;
            if (muted)
            {
                foreach (var s in _sources.Values) { s.volume = 0f; if (s.isPlaying) s.Pause(); }
                foreach (var s in _stingSources.Values) { s.volume = 0f; if (s.isPlaying) s.Pause(); }
            }
            else
            {
                ApplyAllVolumes(0.3f);
            }
        }

        public string? GetCurrentTrack() => _currentTrack;

        public AudioSource? ActiveAudioSource =>
            _currentTrack != null && _sources.TryGetValue(_currentTrack, out var src) && src.isPlaying ? src : null;

        /// <summary>
        /// Crossfade to combat layer (intense) or back to ambient (calm).
        /// active=true : 1.5 s fade-in; active=false : 2 s fade-out.
        /// No-op when a boss track is already playing.
        /// </summary>
        public void SetCombatLayer(bool active)
        {
            if (_currentTrack == "boss") return;
            if (active)
                PlayWithCrossfade("intense", CombatFadeIn);
            else
                PlayWithCrossfade("calm", CombatFadeOut);
        }

        /// <summary>
        /// Adaptive layer intensity: 0 = calm (base), 1 = intense (drums), 2 = boss (full ensemble).
        /// Called each wave start with currentWave/10 clamped to [0,2].
        /// </summary>
        public void SetIntensity(int level)
        {
            string track = level switch
            {
                0 => "calm",
                1 => "intense",
                _ => "boss",
            };
            Play(track);
        }

        // ── Private helpers ─────────────────────────────────────────────────

        private void RegisterTrack(string name, Func<AudioClip?> clipGetter, float volMul)
        {
            _tracks[name] = clipGetter();
            _trackVolMul[name] = volMul;
        }

        private void RegisterSting(string name, Func<AudioClip?> clipGetter, float volMul, float duckSeconds)
        {
            _stings[name] = clipGetter();
            _stingVolMul[name] = volMul;
            _stingDuckMs[name] = duckSeconds;
        }

        private void BuildSources()
        {
            var group = musicMixerGroup;

            // Fallback: inherit from AudioController if available
            if (group == null)
            {
                // AudioController exposes musicGroup via property — leave null, Unity mixer routes automatically
            }

            foreach (var key in _tracks.Keys)
            {
                var go = new GameObject($"Music_{key}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = true;
                src.volume = 0f;
                if (group != null) src.outputAudioMixerGroup = group;
                _sources[key] = src;
            }

            foreach (var key in _stings.Keys)
            {
                var go = new GameObject($"Sting_{key}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.volume = 0f;
                if (group != null) src.outputAudioMixerGroup = group;
                _stingSources[key] = src;
            }
        }

        private float TargetVol(string trackName)
        {
            if (_muted) return 0f;
            float duck = Time.unscaledTime < _duckUntilTime ? DuckMultiplier : 1f;
            float mul = _trackVolMul.TryGetValue(trackName, out var m) ? m : 1f;
            return Mathf.Min(1f, musicVolume * mul * duck);
        }

        private void EnsurePlaying(string trackName)
        {
            if (_muted) return;
            if (_sources.TryGetValue(trackName, out var src) && !src.isPlaying)
            {
                src.volume = 0f;
                src.Play();
                StartCoroutine(FadeCo(src, TargetVol(trackName), CrossfadeDuration));
            }
        }

        private void ApplyAllVolumes(float fadeDuration)
        {
            foreach (var (name, src) in _sources)
            {
                float target = name == _currentTrack ? TargetVol(name) : 0f;
                if (name == _currentTrack && !_muted && !src.isPlaying && _tracks.TryGetValue(name, out var clip) && clip != null)
                    src.Play();
                StartCoroutine(FadeCo(src, target, fadeDuration));
            }
        }

        private void PlayWithCrossfade(string trackName, float duration)
        {
            if (!_tracks.ContainsKey(trackName)) return;
            StopSting();

            if (_currentTrack == trackName) { EnsurePlaying(trackName); return; }

            string? prev = _currentTrack;
            _currentTrack = trackName;

            if (prev != null && _sources.TryGetValue(prev, out var prevSrc))
                StartCoroutine(FadeCo(prevSrc, 0f, duration, stopAfter: true));

            if (_sources.TryGetValue(trackName, out var nextSrc))
            {
                var clip = _tracks[trackName];
                if (clip != null && nextSrc.clip != clip) { nextSrc.clip = clip; nextSrc.loop = true; }
                if (!_muted && clip != null)
                {
                    if (!nextSrc.isPlaying) { nextSrc.volume = 0f; nextSrc.Play(); }
                    StartCoroutine(FadeCo(nextSrc, TargetVol(trackName), duration));
                }
            }
        }

        private void StopSting()
        {
            if (_duckCo != null) { StopCoroutine(_duckCo); _duckCo = null; }
            foreach (var src in _stingSources.Values)
            {
                if (src.isPlaying) { src.Stop(); src.volume = 0f; }
            }
            _activeSting = null;
            _duckUntilTime = 0f;
        }

        private IEnumerator FadeCo(AudioSource src, float target, float duration, bool stopAfter = false)
        {
            float start = src.volume;
            if (duration <= 0f)
            {
                src.volume = target;
                if (stopAfter && target <= 0f) src.Stop();
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
                yield return null;
            }
            src.volume = target;
            if (stopAfter && target <= 0f) src.Stop();
        }

        private IEnumerator DuckEndCo(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            _activeSting = null;
            _duckUntilTime = 0f;
            _duckCo = null;
            ApplyAllVolumes(0.6f);
        }

        // ── Scene routing ────────────────────────────────────────────────────

        private void HandleSceneChange(Scene oldScene, Scene newScene)
        {
            string name = newScene.name;
            if (name == "Menu" || name == "WorldMap")
            {
                musicVolume = 0.4f;
                Play("menu");
            }
            else if (name == "Main")
            {
                musicVolume = 0.35f;
                Play("calm");
            }
        }

        // ── EventManager subscriptions ───────────────────────────────────────

        private void OnWaveStarted(int _)  => SetCombatLayer(true);
        private void OnWaveCleared(int _)   => SetCombatLayer(false);

        private void OnLevelStart(CrowdDefense.Data.LevelData _, Bounds __)  => CrossfadeTo("wave_combat");

        private void OnLevelThemeChanged(LevelThemeChangedEvent evt) => PlayLevel(evt.ThemeName);

        private void OnBossEncountered(BossEncounteredEvent _)
        {
            bool hasBossClip = _tracks.TryGetValue("boss", out var clip) && clip != null;
            if (hasBossClip)
            {
                PlayWithCrossfade("boss", BossCrossfadeDuration);
            }
            else
            {
                // Fallback: switch to "intense" with +20% volume boost
                float prev = _trackVolMul.TryGetValue("intense", out var m) ? m : 1f;
                _trackVolMul["intense"] = prev * BossFallbackVolBoost;
                PlayWithCrossfade("intense", BossCrossfadeDuration);
            }
        }

        private void OnBossDefeated(BossDefeatedEvent _) => PlayWithCrossfade("calm", BossCrossfadeDuration);
    }
}
