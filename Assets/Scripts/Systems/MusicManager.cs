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
        private const float BossFallbackPitchShift = 0.2f;
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

        private const float IntensityVolMax   = 1.15f;
        private const float IntensityPitchMax = 1.05f;

        private string? _currentTrack;
        private string? _activeSting;
        private float _duckUntilTime;
        private Coroutine? _duckCo;
        private bool _muted;
        private float _waveIntensity = 0f;   // 0..1, drives volume/pitch swell

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
        /// Select calm track pitch-shifted per world tier.
        /// W1-2=1.0, W3-4=0.95, W5-6=0.90, W7-8=0.85, W9-10=0.80.
        /// Also applies EQ tuning (reverb/lowpass/distortion/chorus) per world biome.
        /// No-op when boss track is active.
        /// </summary>
        public void PlayWorldTheme(int worldId)
        {
            if (_currentTrack == "boss") return;
            float pitch = worldId switch
            {
                <= 2 => 1.00f,
                <= 4 => 0.95f,
                <= 6 => 0.90f,
                <= 8 => 0.85f,
                _    => 0.80f,
            };
            if (_sources.TryGetValue("calm", out var calmSrc))
            {
                calmSrc.pitch = pitch;
                ApplyWorldEQ(calmSrc, worldId);
            }
            if (_sources.TryGetValue("intense", out var intenseSrc))
            {
                intenseSrc.pitch = pitch;
                ApplyWorldEQ(intenseSrc, worldId);
            }
            Play("calm");
        }

        // World EQ presets per biome
        // W1-2 forest  : reverb 0.3 (open canopy)
        // W3-4 desert  : lowpass 0.7 (muffled heat)
        // W5-6 snow    : reverb 0.5 + chorus 0.2 (echoey expanse)
        // W7-8 lava    : distortion 0.3 (heavy, oppressive)
        // W9-10 void   : reverb 0.8 (cathedral emptiness)
        private static void ApplyWorldEQ(AudioSource src, int worldId)
        {
            // Remove any previously added filters before applying new preset
            foreach (var f in src.GetComponents<AudioReverbFilter>())    UnityEngine.Object.Destroy(f);
            foreach (var f in src.GetComponents<AudioLowPassFilter>())   UnityEngine.Object.Destroy(f);
            foreach (var f in src.GetComponents<AudioChorusFilter>())    UnityEngine.Object.Destroy(f);
            foreach (var f in src.GetComponents<AudioDistortionFilter>()) UnityEngine.Object.Destroy(f);

            if (worldId <= 2)
            {
                // Forest: light reverb, open feel
                var rev = src.gameObject.AddComponent<AudioReverbFilter>();
                rev.reverbPreset = AudioReverbPreset.Forest;
                rev.reverbLevel = -1000 + (int)(0.3f * 2000f); // approx 0.3 blend
            }
            else if (worldId <= 4)
            {
                // Desert: lowpass cutoff 0.7 (22000 Hz * 0.7 ≈ 15400 Hz, slightly muffled)
                var lp = src.gameObject.AddComponent<AudioLowPassFilter>();
                lp.cutoffFrequency = 22000f * 0.7f;
                lp.lowpassResonanceQ = 1f;
            }
            else if (worldId <= 6)
            {
                // Snow: reverb + light chorus
                var rev = src.gameObject.AddComponent<AudioReverbFilter>();
                rev.reverbPreset = AudioReverbPreset.StoneCorridor;
                rev.reverbLevel = -1000 + (int)(0.5f * 2000f);
                var cho = src.gameObject.AddComponent<AudioChorusFilter>();
                cho.depth = 0.2f;
                cho.rate = 0.4f;
                cho.dryMix = 0.9f;
            }
            else if (worldId <= 8)
            {
                // Lava: distortion 0.3
                var dist = src.gameObject.AddComponent<AudioDistortionFilter>();
                dist.distortionLevel = 0.3f;
            }
            else
            {
                // Void: heavy reverb, cathedral
                var rev = src.gameObject.AddComponent<AudioReverbFilter>();
                rev.reverbPreset = AudioReverbPreset.Auditorium;
                rev.reverbLevel = -1000 + (int)(0.8f * 2000f);
            }
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

        /// <summary>
        /// Drive music urgency from wave progress (0 = wave start, 1 = wave cleared).
        /// Volume ramps 1.0 → 1.15 and pitch 1.0 → 1.05 on the active track source.
        /// Call each frame while a wave is active, or pass 0 to reset.
        /// </summary>
        public void UpdateWaveIntensity(float progress)
        {
            _waveIntensity = Mathf.Clamp01(progress);
            if (_currentTrack == null || !_sources.TryGetValue(_currentTrack, out var src)) return;
            if (_muted) return;

            float baseVol  = Mathf.Min(1f, musicVolume * (_trackVolMul.TryGetValue(_currentTrack, out var m) ? m : 1f));
            float duck     = Time.unscaledTime < _duckUntilTime ? DuckMultiplier : 1f;
            src.volume = baseVol * duck * Mathf.Lerp(1f, IntensityVolMax,   _waveIntensity);
            src.pitch  =                             Mathf.Lerp(1f, IntensityPitchMax, _waveIntensity);
        }

        private void Update()
        {
            var wm = WaveManager.Instance;
            if (wm == null || !wm.IsWaveActive) return;
            int total = wm.WaveTotalSpawned;
            if (total <= 0) return;
            float progress = (float)wm.WaveKillCount / total;
            UpdateWaveIntensity(progress);
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
                var clip = _tracks.TryGetValue(trackName, out var c) ? c : null;
                if (clip == null) return;
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
        private void OnWaveCleared(int _)
        {
            SetCombatLayer(false);
            UpdateWaveIntensity(0f);
        }

        private void OnLevelStart(CrowdDefense.Data.LevelData level, Bounds __) => PlayWorldTheme(level.World);

        private void OnLevelThemeChanged(LevelThemeChangedEvent evt) => PlayLevel(evt.ThemeName);

        private void OnBossEncountered(BossEncounteredEvent _)
        {
            bool hasBossClip = _tracks.TryGetValue("boss", out var clip) && clip != null;
            if (hasBossClip)
            {
                // Swell: boss source starts at 0 and FadeCo ramps to TargetVol over BossCrossfadeDuration (2s)
                PlayWithCrossfade("boss", BossCrossfadeDuration);
            }
            else
            {
                // Fallback: pitch up +0.2 + reverb on current source for orchestral swell impact
                string fallback = _currentTrack ?? "intense";
                if (_sources.TryGetValue(fallback, out var fallbackSrc) && fallbackSrc != null && fallbackSrc.gameObject.activeInHierarchy)
                {
                    fallbackSrc.pitch += BossFallbackPitchShift;
                    foreach (var f in fallbackSrc.GetComponents<AudioReverbFilter>()) UnityEngine.Object.Destroy(f);
                    var rev = fallbackSrc.gameObject.AddComponent<AudioReverbFilter>();
                    if (rev != null)
                    {
                        rev.reverbPreset = AudioReverbPreset.Auditorium;
                        StartCoroutine(RemoveFallbackFxCo(fallbackSrc, rev, BossCrossfadeDuration + 4f));
                    }
                }
                float prev = _trackVolMul.TryGetValue("intense", out var m) ? m : 1f;
                _trackVolMul["intense"] = prev * BossFallbackVolBoost;
                PlayWithCrossfade("intense", BossCrossfadeDuration);
            }
        }

        private IEnumerator RemoveFallbackFxCo(AudioSource src, AudioReverbFilter rev, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (src != null)
            {
                src.pitch = Mathf.Max(1f, src.pitch - BossFallbackPitchShift);
            }
            if (rev != null) UnityEngine.Object.Destroy(rev);
        }

        private void OnBossDefeated(BossDefeatedEvent _) => PlayWithCrossfade("calm", BossCrossfadeDuration);
    }
}
