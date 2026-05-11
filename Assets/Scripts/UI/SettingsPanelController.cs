#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SettingsPanelController : MonoBehaviour
    {
        private VisualElement? _root;
        private VisualElement? _settingsRoot;

        private Slider? _masterSlider;
        private Slider? _sfxSlider;
        private Slider? _musicSlider;
        private Label? _masterValue;
        private Label? _sfxValue;
        private Label? _musicValue;
        private Toggle? _muteToggle;

        private DropdownField? _qualityDropdown;
        private Toggle? _vfxToggle;
        private Toggle? _shakeToggle;

        private Toggle? _colorblindToggle;
        private Toggle? _reduceMotionToggle;
        private Toggle? _largeTextToggle;

        private DropdownField? _langDropdown;
        private Button? _closeBtn;

        private static readonly List<string> QualityChoices = new() { "Mobile", "Desktop", "High" };
        private static readonly List<string> LangChoices = new() { "English", "Français" };
        private static readonly string[] LangCodes = { "en", "fr" };

        private bool _suppressEvents;

        private void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _settingsRoot = _root.Q<VisualElement>("settings-root");

            _masterSlider = _root.Q<Slider>("master-slider");
            _sfxSlider = _root.Q<Slider>("sfx-slider");
            _musicSlider = _root.Q<Slider>("music-slider");
            _masterValue = _root.Q<Label>("master-value");
            _sfxValue = _root.Q<Label>("sfx-value");
            _musicValue = _root.Q<Label>("music-value");
            _muteToggle = _root.Q<Toggle>("mute-toggle");

            _qualityDropdown = _root.Q<DropdownField>("quality-dropdown");
            _vfxToggle = _root.Q<Toggle>("vfx-toggle");
            _shakeToggle = _root.Q<Toggle>("shake-toggle");

            _colorblindToggle = _root.Q<Toggle>("colorblind-toggle");
            _reduceMotionToggle = _root.Q<Toggle>("reduce-motion-toggle");
            _largeTextToggle = _root.Q<Toggle>("large-text-toggle");

            _langDropdown = _root.Q<DropdownField>("lang-dropdown");
            _closeBtn = _root.Q<Button>("settings-close-btn");

            if (_qualityDropdown != null)
            {
                _qualityDropdown.choices = QualityChoices;
            }
            if (_langDropdown != null)
            {
                _langDropdown.choices = LangChoices;
            }

            BindCallbacks();
            SyncFromRegistry();
        }

        private void OnEnable()
        {
            if (SettingsRegistry.Instance != null)
                SettingsRegistry.Instance.OnSettingsChanged += SyncFromRegistry;
        }

        private void OnDisable()
        {
            if (SettingsRegistry.Instance != null)
                SettingsRegistry.Instance.OnSettingsChanged -= SyncFromRegistry;
        }

        private void BindCallbacks()
        {
            _masterSlider?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.MasterVolume = evt.newValue;
                if (_masterValue != null) _masterValue.text = FormatPct(evt.newValue);
            });

            _sfxSlider?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.SFXVolume = evt.newValue;
                if (_sfxValue != null) _sfxValue.text = FormatPct(evt.newValue);
            });

            _musicSlider?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.MusicVolume = evt.newValue;
                if (_musicValue != null) _musicValue.text = FormatPct(evt.newValue);
            });

            _muteToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.Muted = evt.newValue;
            });

            _qualityDropdown?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                int idx = QualityChoices.IndexOf(evt.newValue);
                if (idx >= 0) SettingsRegistry.Instance.QualityLevel = idx;
            });

            _vfxToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.VFXEnabled = evt.newValue;
            });

            _shakeToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.ShakeEnabled = evt.newValue;
            });

            _colorblindToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.ColorblindMode = evt.newValue;
            });

            _reduceMotionToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.ReduceMotion = evt.newValue;
            });

            _largeTextToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.LargeText = evt.newValue;
            });

            _langDropdown?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                int idx = LangChoices.IndexOf(evt.newValue);
                if (idx < 0 || idx >= LangCodes.Length) return;
                string code = LangCodes[idx];
                SettingsRegistry.Instance.Locale = code;
                L.SetLocale(code);
            });

            _closeBtn?.RegisterCallback<ClickEvent>(_ => Hide());
        }

        public void Show()
        {
            SyncFromRegistry();
            _settingsRoot?.RemoveFromClassList("hidden");
        }

        public void Hide()
        {
            _settingsRoot?.AddToClassList("hidden");
        }

        private void SyncFromRegistry()
        {
            var reg = SettingsRegistry.Instance;
            if (reg == null) return;

            _suppressEvents = true;
            try
            {
                if (_masterSlider != null) _masterSlider.value = reg.MasterVolume;
                if (_sfxSlider != null) _sfxSlider.value = reg.SFXVolume;
                if (_musicSlider != null) _musicSlider.value = reg.MusicVolume;
                if (_masterValue != null) _masterValue.text = FormatPct(reg.MasterVolume);
                if (_sfxValue != null) _sfxValue.text = FormatPct(reg.SFXVolume);
                if (_musicValue != null) _musicValue.text = FormatPct(reg.MusicVolume);
                if (_muteToggle != null) _muteToggle.value = reg.Muted;

                if (_qualityDropdown != null)
                {
                    int idx = Mathf.Clamp(reg.QualityLevel, 0, QualityChoices.Count - 1);
                    _qualityDropdown.SetValueWithoutNotify(QualityChoices[idx]);
                }
                if (_vfxToggle != null) _vfxToggle.value = reg.VFXEnabled;
                if (_shakeToggle != null) _shakeToggle.value = reg.ShakeEnabled;

                if (_colorblindToggle != null) _colorblindToggle.value = reg.ColorblindMode;
                if (_reduceMotionToggle != null) _reduceMotionToggle.value = reg.ReduceMotion;
                if (_largeTextToggle != null) _largeTextToggle.value = reg.LargeText;

                if (_langDropdown != null)
                {
                    int li = System.Array.IndexOf(LangCodes, reg.Locale);
                    if (li < 0) li = 0;
                    _langDropdown.SetValueWithoutNotify(LangChoices[li]);
                }
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private static string FormatPct(float v) => Mathf.RoundToInt(v * 100f) + "%";
    }
}
