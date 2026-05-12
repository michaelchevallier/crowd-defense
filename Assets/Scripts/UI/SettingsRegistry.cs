#nullable enable
using System;
using CrowdDefense.Common;
using UnityEngine;

namespace CrowdDefense.UI
{
    public class SettingsRegistry : MonoSingleton<SettingsRegistry>
    {
        private const string KMaster = "cd.audio.master";
        private const string KSFX = "cd.audio.sfx";
        private const string KMusic = "cd.audio.music";
        private const string KUI = "cd.audio.ui";
        private const string KMuted = "cd.audio.muted";
        private const string KSFXMuted = "cd.audio.sfx_muted";
        private const string KMusicMuted = "cd.audio.music_muted";
        private const string KQuality = "cd.gfx.quality";
        private const string KVFX = "cd.gfx.vfx";
        private const string KShake = "cd.gfx.shake";
        private const string KColorblind = "cd.a11y.colorblind";
        private const string KReduceMotion = "cd.a11y.reduce_motion";
        private const string KLargeText = "cd.a11y.large_text";
        private const string KLocale = "cd.locale";
        private const string KGameSpeed = "cd.gameplay.speed";

        public event Action? OnSettingsChanged;

        private float _masterVolume = 1f;
        private float _sfxVolume = 1f;
        private float _musicVolume = 0.7f;
        private float _uiVolume = 1f;
        private bool _muted;
        private bool _sfxMuted;
        private bool _musicMuted;
        private int _qualityLevel = 1;
        private bool _vfxEnabled = true;
        private bool _shakeEnabled = true;
        private bool _colorblindMode;
        private bool _reduceMotion;
        private bool _largeText;
        private string _locale = "en";
        private int _gameSpeed = 1;

        public float MasterVolume
        {
            get => _masterVolume;
            set { if (Mathf.Approximately(_masterVolume, value)) return; _masterVolume = Mathf.Clamp01(value); ApplyAudio(); Save(); Notify(); }
        }

        public float SFXVolume
        {
            get => _sfxVolume;
            set { if (Mathf.Approximately(_sfxVolume, value)) return; _sfxVolume = Mathf.Clamp01(value); ApplyAudioSFX(); Save(); Notify(); }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set { if (Mathf.Approximately(_musicVolume, value)) return; _musicVolume = Mathf.Clamp01(value); ApplyAudioMusic(); Save(); Notify(); }
        }

        public float UIVolume
        {
            get => _uiVolume;
            set { if (Mathf.Approximately(_uiVolume, value)) return; _uiVolume = Mathf.Clamp01(value); ApplyAudioUI(); Save(); Notify(); }
        }

        public bool Muted
        {
            get => _muted;
            set { if (_muted == value) return; _muted = value; ApplyAudio(); Save(); Notify(); }
        }

        public bool SFXMuted
        {
            get => _sfxMuted;
            set { if (_sfxMuted == value) return; _sfxMuted = value; ApplyAudioSFX(); Save(); Notify(); }
        }

        public bool MusicMuted
        {
            get => _musicMuted;
            set { if (_musicMuted == value) return; _musicMuted = value; ApplyAudioMusic(); Save(); Notify(); }
        }

        public int QualityLevel
        {
            get => _qualityLevel;
            set { value = Mathf.Clamp(value, 0, 2); if (_qualityLevel == value) return; _qualityLevel = value; ApplyQuality(); Save(); Notify(); }
        }

        public bool VFXEnabled
        {
            get => _vfxEnabled;
            set { if (_vfxEnabled == value) return; _vfxEnabled = value; Save(); Notify(); }
        }

        public bool ShakeEnabled
        {
            get => _shakeEnabled;
            set { if (_shakeEnabled == value) return; _shakeEnabled = value; Save(); Notify(); }
        }

        public bool ColorblindMode
        {
            get => _colorblindMode;
            set { if (_colorblindMode == value) return; _colorblindMode = value; Save(); Notify(); }
        }

        public bool ReduceMotion
        {
            get => _reduceMotion;
            set { if (_reduceMotion == value) return; _reduceMotion = value; Save(); Notify(); }
        }

        public bool LargeText
        {
            get => _largeText;
            set { if (_largeText == value) return; _largeText = value; Save(); Notify(); }
        }

        public string Locale
        {
            get => _locale;
            set { if (_locale == value || string.IsNullOrEmpty(value)) return; _locale = value; Save(); Notify(); }
        }

        public int GameSpeed
        {
            get => _gameSpeed;
            set { value = value < 0 ? 0 : value > 3 ? 3 : value; if (_gameSpeed == value) return; _gameSpeed = value; Save(); Notify(); }
        }

        protected override void OnAwakeSingleton()
        {
            Load();
            ApplyAudio();
            ApplyQuality();
        }

        public void Save()
        {
            PlayerPrefs.SetFloat(KMaster, _masterVolume);
            PlayerPrefs.SetFloat(KSFX, _sfxVolume);
            PlayerPrefs.SetFloat(KMusic, _musicVolume);
            PlayerPrefs.SetFloat(KUI, _uiVolume);
            PlayerPrefs.SetInt(KMuted, _muted ? 1 : 0);
            PlayerPrefs.SetInt(KSFXMuted, _sfxMuted ? 1 : 0);
            PlayerPrefs.SetInt(KMusicMuted, _musicMuted ? 1 : 0);
            PlayerPrefs.SetInt(KQuality, _qualityLevel);
            PlayerPrefs.SetInt(KVFX, _vfxEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KShake, _shakeEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KColorblind, _colorblindMode ? 1 : 0);
            PlayerPrefs.SetInt(KReduceMotion, _reduceMotion ? 1 : 0);
            PlayerPrefs.SetInt(KLargeText, _largeText ? 1 : 0);
            PlayerPrefs.SetString(KLocale, _locale);
            PlayerPrefs.SetInt(KGameSpeed, _gameSpeed);
            PlayerPrefs.Save();
        }

        public void Load()
        {
            _masterVolume = PlayerPrefs.GetFloat(KMaster, 1f);
            _sfxVolume = PlayerPrefs.GetFloat(KSFX, 1f);
            _musicVolume = PlayerPrefs.GetFloat(KMusic, 0.7f);
            _uiVolume = PlayerPrefs.GetFloat(KUI, 1f);
            _muted = PlayerPrefs.GetInt(KMuted, 0) == 1;
            _sfxMuted = PlayerPrefs.GetInt(KSFXMuted, 0) == 1;
            _musicMuted = PlayerPrefs.GetInt(KMusicMuted, 0) == 1;
            _qualityLevel = PlayerPrefs.GetInt(KQuality, 1);
            _vfxEnabled = PlayerPrefs.GetInt(KVFX, 1) == 1;
            _shakeEnabled = PlayerPrefs.GetInt(KShake, 1) == 1;
            _colorblindMode = PlayerPrefs.GetInt(KColorblind, 0) == 1;
            _reduceMotion = PlayerPrefs.GetInt(KReduceMotion, 0) == 1;
            _largeText = PlayerPrefs.GetInt(KLargeText, 0) == 1;
            _locale = PlayerPrefs.GetString(KLocale, "en");
            _gameSpeed = PlayerPrefs.GetInt(KGameSpeed, 1);
        }

        private void ApplyAudio()
        {
            var audio = Systems.AudioController.Instance;
            if (audio == null) return;
            audio.SetMasterVolume(_muted ? 0f : _masterVolume);
            audio.SetSFXVolume(_sfxMuted ? 0f : _sfxVolume);
            audio.SetMusicVolume(_musicMuted ? 0f : _musicVolume);
            audio.SetUIVolume(_uiVolume);
            audio.SetMuted(_muted);
        }

        private void ApplyAudioSFX() =>
            Systems.AudioController.Instance?.SetSFXVolume(_sfxMuted ? 0f : _sfxVolume);

        private void ApplyAudioMusic() =>
            Systems.AudioController.Instance?.SetMusicVolume(_musicMuted ? 0f : _musicVolume);

        private void ApplyAudioUI() =>
            Systems.AudioController.Instance?.SetUIVolume(_uiVolume);

        private void ApplyQuality()
        {
            int idx = Mathf.Clamp(_qualityLevel, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(idx, applyExpensiveChanges: false);
        }

        private void Notify() => OnSettingsChanged?.Invoke();
    }
}
