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
        private Slider? _uiSlider;
        private Label? _masterValue;
        private Label? _sfxValue;
        private Label? _musicValue;
        private Label? _uiValue;
        private Toggle? _muteToggle;
        private Button? _resetCameraBtn;

        private DropdownField? _qualityDropdown;
        private Toggle? _vfxToggle;
        private Toggle? _shakeToggle;

        private Toggle? _colorblindToggle;
        private Toggle? _reduceMotionToggle;
        private Toggle? _largeTextToggle;

        private DropdownField? _langDropdown;
        private Button? _closeBtn;

        // Section title label refs for locale refresh
        private Label? _settingsTitleLabel;
        private Label? _audioSectionLabel;
        private Label? _masterLabel;
        private Label? _sfxLabel;
        private Label? _musicLabel;
        private Label? _uiLabel;
        private Label? _muteLabel;
        private Label? _gfxSectionLabel;
        private Label? _qualityLabel;
        private Label? _vfxLabel;
        private Label? _shakeLabel;
        private Label? _a11ySectionLabel;
        private Label? _colorblindLabel;
        private Label? _reduceMotionLabel;
        private Label? _largeTextLabel;
        private Label? _langSectionLabel;
        private Label? _langLabel;

        private static readonly string[] LangCodes = { "en", "fr" };

        // Built at runtime so locale changes are reflected
        private List<string> QualityChoices => new()
        {
            L.Get("settings.quality_mobile"),
            L.Get("settings.quality_desktop"),
            L.Get("settings.quality_high"),
        };
        private List<string> LangChoices => new()
        {
            L.Get("settings.lang_en"),
            L.Get("settings.lang_fr"),
        };

        private bool _suppressEvents;

        private void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _settingsRoot = _root.Q<VisualElement>("settings-root");

            _masterSlider = _root.Q<Slider>("master-slider");
            _sfxSlider = _root.Q<Slider>("sfx-slider");
            _musicSlider = _root.Q<Slider>("music-slider");
            _uiSlider = _root.Q<Slider>("ui-slider");
            _masterValue = _root.Q<Label>("master-value");
            _sfxValue = _root.Q<Label>("sfx-value");
            _musicValue = _root.Q<Label>("music-value");
            _uiValue = _root.Q<Label>("ui-value");
            _muteToggle = _root.Q<Toggle>("mute-toggle");
            _resetCameraBtn = _root.Q<Button>("settings-reset-camera-btn");

            _qualityDropdown = _root.Q<DropdownField>("quality-dropdown");
            _vfxToggle = _root.Q<Toggle>("vfx-toggle");
            _shakeToggle = _root.Q<Toggle>("shake-toggle");

            _colorblindToggle = _root.Q<Toggle>("colorblind-toggle");
            _reduceMotionToggle = _root.Q<Toggle>("reduce-motion-toggle");
            _largeTextToggle = _root.Q<Toggle>("large-text-toggle");

            _langDropdown = _root.Q<DropdownField>("lang-dropdown");
            _closeBtn = _root.Q<Button>("settings-close-btn");

            _settingsTitleLabel  = _root.Q<Label>("settings-title");
            _audioSectionLabel   = _root.Q<Label>("audio-section-title");
            _masterLabel         = _root.Q<Label>("master-label");
            _sfxLabel            = _root.Q<Label>("sfx-label");
            _musicLabel          = _root.Q<Label>("music-label");
            _uiLabel             = _root.Q<Label>("ui-label");
            _muteLabel           = _root.Q<Label>("mute-label");
            _gfxSectionLabel     = _root.Q<Label>("gfx-section-title");
            _qualityLabel        = _root.Q<Label>("quality-label");
            _vfxLabel            = _root.Q<Label>("vfx-label");
            _shakeLabel          = _root.Q<Label>("shake-label");
            _a11ySectionLabel    = _root.Q<Label>("a11y-section-title");
            _colorblindLabel     = _root.Q<Label>("colorblind-label");
            _reduceMotionLabel   = _root.Q<Label>("reduce-motion-label");
            _largeTextLabel      = _root.Q<Label>("large-text-label");
            _langSectionLabel    = _root.Q<Label>("lang-section-title");
            _langLabel           = _root.Q<Label>("lang-label");

            if (_qualityDropdown != null)
            {
                _qualityDropdown.choices = QualityChoices;
            }
            if (_langDropdown != null)
            {
                _langDropdown.choices = LangChoices;
            }

            ApplyLocalizedTexts();
            L.OnLocaleChanged += ApplyLocalizedTexts;
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

        private void OnDestroy()
        {
            L.OnLocaleChanged -= ApplyLocalizedTexts;
        }

        private void ApplyLocalizedTexts()
        {
            if (_settingsTitleLabel != null)  _settingsTitleLabel.text  = L.Get("settings.title");
            if (_audioSectionLabel != null)   _audioSectionLabel.text   = L.Get("settings.audio_section");
            if (_masterLabel != null)         _masterLabel.text         = L.Get("settings.master");
            if (_sfxLabel != null)            _sfxLabel.text            = L.Get("settings.sfx");
            if (_musicLabel != null)          _musicLabel.text          = L.Get("settings.music");
            if (_uiLabel != null)             _uiLabel.text             = L.Get("settings.ui");
            if (_muteLabel != null)           _muteLabel.text           = L.Get("settings.mute");
            if (_gfxSectionLabel != null)     _gfxSectionLabel.text     = L.Get("settings.gfx_section");
            if (_qualityLabel != null)        _qualityLabel.text        = L.Get("settings.quality");
            if (_vfxLabel != null)            _vfxLabel.text            = L.Get("settings.vfx");
            if (_shakeLabel != null)          _shakeLabel.text          = L.Get("settings.shake");
            if (_a11ySectionLabel != null)    _a11ySectionLabel.text    = L.Get("settings.a11y_section");
            if (_colorblindLabel != null)     _colorblindLabel.text     = L.Get("settings.colorblind");
            if (_reduceMotionLabel != null)   _reduceMotionLabel.text   = L.Get("settings.reduce_motion");
            if (_largeTextLabel != null)      _largeTextLabel.text      = L.Get("settings.large_text");
            if (_langSectionLabel != null)    _langSectionLabel.text    = L.Get("settings.lang_section");
            if (_langLabel != null)           _langLabel.text           = L.Get("settings.lang_label");
            if (_closeBtn != null)            _closeBtn.text            = L.Get("settings.close");
            if (_resetCameraBtn != null)      _resetCameraBtn.text      = L.Get("settings.reset_camera");

            if (_qualityDropdown != null) _qualityDropdown.choices = QualityChoices;
            if (_langDropdown != null)    _langDropdown.choices    = LangChoices;
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

            _uiSlider?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.UIVolume = evt.newValue;
                if (_uiValue != null) _uiValue.text = FormatPct(evt.newValue);
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

            _resetCameraBtn?.RegisterCallback<ClickEvent>(_ => ResetCamera());
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
                if (_uiSlider != null) _uiSlider.value = reg.UIVolume;
                if (_masterValue != null) _masterValue.text = FormatPct(reg.MasterVolume);
                if (_sfxValue != null) _sfxValue.text = FormatPct(reg.SFXVolume);
                if (_musicValue != null) _musicValue.text = FormatPct(reg.MusicVolume);
                if (_uiValue != null) _uiValue.text = FormatPct(reg.UIVolume);
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

        private static void ResetCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.transform.SetPositionAndRotation(Vector3.back * 10f, Quaternion.identity);
        }

        private static string FormatPct(float v) => Mathf.RoundToInt(v * 100f) + "%";
    }
}
