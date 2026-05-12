#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using CrowdDefense.Visual;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SettingsPanelController : MonoBehaviour
    {
        public static SettingsPanelController? Instance { get; private set; }
        public bool IsOpen => _settingsRoot != null && !_settingsRoot.ClassListContains("hidden");

        private VisualElement? _root;
        private VisualElement? _settingsRoot;
        private Button? _fullscreenBtn;

        private Slider? _masterSlider;
        private Slider? _sfxSlider;
        private Slider? _musicSlider;
        private Slider? _uiSlider;
        private SliderInt? _gameSpeedSlider;
        private Label? _masterValue;
        private Label? _sfxValue;
        private Label? _musicValue;
        private Label? _uiValue;
        private Label? _gameSpeedValue;
        private Toggle? _muteToggle;
        private Toggle? _sfxMuteToggle;
        private Toggle? _musicMuteToggle;
        private Toggle? _followHeroToggle;
        private Label?  _followHeroLabel;
        private Toggle? _joystickToggle;
        private Label?  _joystickLabel;
        private Toggle? _heroAutoAttackToggle;
        private Label?  _heroAutoAttackLabel;
        private Button? _resetCameraBtn;
        private Button? _changeNameBtn;
        private Button? _resetProgressBtn;
        private Label?  _resetProgressWarnLabel;

        private DropdownField? _qualityDropdown;
        private Toggle? _vfxToggle;
        private Toggle? _shakeToggle;
        private Toggle? _autoPauseToggle;
        private Label?  _autoPauseLabel;

        private Toggle? _musicPulseToggle;
        private Label?  _musicPulseLabel;
        private Toggle? _colorblindToggle;
        private Toggle? _reduceMotionToggle;
        private Toggle? _largeTextToggle;

        private DropdownField? _langDropdown;
        private Button? _closeBtn;
        private Button? _keybindingsBtn;

        // Tab system
        private Button? _tabAudio;
        private Button? _tabVideo;
        private Button? _tabControls;
        private Button? _tabGameplay;
        private VisualElement? _panelAudio;
        private VisualElement? _panelVideo;
        private VisualElement? _panelControls;
        private VisualElement? _panelGameplay;

        // Section title label refs for locale refresh
        private Label? _settingsTitleLabel;
        private Label? _audioSectionLabel;
        private Label? _masterLabel;
        private Label? _sfxLabel;
        private Label? _musicLabel;
        private Label? _uiLabel;
        private Label? _muteLabel;
        private Label? _sfxMuteLabel;
        private Label? _musicMuteLabel;
        private Label? _gameSpeedLabel;
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

        private static readonly string[] LangCodes = { "en", "fr", "es" };

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
            L.Get("settings.lang_es"),
        };

        private bool _suppressEvents;

        private void Awake() => Instance = this;

        private void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _settingsRoot = _root.Q<VisualElement>("settings-root");

            _masterSlider = _root.Q<Slider>("master-slider");
            _sfxSlider = _root.Q<Slider>("sfx-slider");
            _musicSlider = _root.Q<Slider>("music-slider");
            _uiSlider = _root.Q<Slider>("ui-slider");
            _gameSpeedSlider = _root.Q<SliderInt>("game-speed-slider");
            _masterValue = _root.Q<Label>("master-value");
            _sfxValue = _root.Q<Label>("sfx-value");
            _musicValue = _root.Q<Label>("music-value");
            _uiValue = _root.Q<Label>("ui-value");
            _gameSpeedValue = _root.Q<Label>("game-speed-value");
            _muteToggle = _root.Q<Toggle>("mute-toggle");
            _sfxMuteToggle = _root.Q<Toggle>("sfx-mute-toggle");
            _musicMuteToggle = _root.Q<Toggle>("music-mute-toggle");
            _followHeroToggle = _root.Q<Toggle>("follow-hero-toggle");
            _followHeroLabel  = _root.Q<Label>("follow-hero-label");
            _joystickToggle   = _root.Q<Toggle>("joystick-toggle");
            _joystickLabel    = _root.Q<Label>("joystick-label");
            _heroAutoAttackToggle = _root.Q<Toggle>("hero-auto-attack-toggle");
            _heroAutoAttackLabel  = _root.Q<Label>("hero-auto-attack-label");
            _resetCameraBtn      = _root.Q<Button>("settings-reset-camera-btn");
            _changeNameBtn       = _root.Q<Button>("settings-change-name-btn");
            _resetProgressBtn    = _root.Q<Button>("settings-reset-progress-btn");
            _resetProgressWarnLabel = _root.Q<Label>("reset-progress-warn");

            _qualityDropdown = _root.Q<DropdownField>("quality-dropdown");
            _vfxToggle = _root.Q<Toggle>("vfx-toggle");
            _shakeToggle = _root.Q<Toggle>("shake-toggle");
            _autoPauseToggle = _root.Q<Toggle>("auto-pause-toggle");
            _autoPauseLabel  = _root.Q<Label>("auto-pause-label");

            _musicPulseToggle = _root.Q<Toggle>("music-pulse-toggle");
            _musicPulseLabel  = _root.Q<Label>("music-pulse-label");
            _colorblindToggle = _root.Q<Toggle>("colorblind-toggle");
            _reduceMotionToggle = _root.Q<Toggle>("reduce-motion-toggle");
            _largeTextToggle = _root.Q<Toggle>("large-text-toggle");

            _langDropdown = _root.Q<DropdownField>("lang-dropdown");
            _closeBtn = _root.Q<Button>("settings-close-btn");
            _fullscreenBtn = _root.Q<Button>("settings-fullscreen-btn");
            _keybindingsBtn = _root.Q<Button>("settings-keybindings-btn");

            _tabAudio    = _root.Q<Button>("tab-audio");
            _tabVideo    = _root.Q<Button>("tab-video");
            _tabControls = _root.Q<Button>("tab-controls");
            _tabGameplay = _root.Q<Button>("tab-gameplay");
            _panelAudio    = _root.Q<VisualElement>("panel-audio");
            _panelVideo    = _root.Q<VisualElement>("panel-video");
            _panelControls = _root.Q<VisualElement>("panel-controls");
            _panelGameplay = _root.Q<VisualElement>("panel-gameplay");

            _settingsTitleLabel  = _root.Q<Label>("settings-title");
            _audioSectionLabel   = _root.Q<Label>("audio-section-title");
            _masterLabel         = _root.Q<Label>("master-label");
            _sfxLabel            = _root.Q<Label>("sfx-label");
            _musicLabel          = _root.Q<Label>("music-label");
            _uiLabel             = _root.Q<Label>("ui-label");
            _muteLabel           = _root.Q<Label>("mute-label");
            _sfxMuteLabel        = _root.Q<Label>("sfx-mute-label");
            _musicMuteLabel      = _root.Q<Label>("music-mute-label");
            _gameSpeedLabel      = _root.Q<Label>("game-speed-label");
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

            if (_gameSpeedSlider != null)
            {
                _gameSpeedSlider.lowValue = 1;
                _gameSpeedSlider.highValue = 3;
            }

            ApplyLocalizedTexts();
            L.OnLocaleChanged += ApplyLocalizedTexts;
            BindCallbacks();
            SyncFromRegistry();
            SyncFollowHero();
            SyncJoystick();
            SyncHeroAutoAttack();
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
            if (Instance == this) Instance = null;
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
            if (_sfxMuteLabel != null)        _sfxMuteLabel.text        = L.Get("settings.sfx_mute");
            if (_musicMuteLabel != null)      _musicMuteLabel.text      = L.Get("settings.music_mute");
            if (_gameSpeedLabel != null)      _gameSpeedLabel.text      = L.Get("settings.game_speed");
            if (_gfxSectionLabel != null)     _gfxSectionLabel.text     = L.Get("settings.gfx_section");
            if (_qualityLabel != null)        _qualityLabel.text        = L.Get("settings.quality");
            if (_vfxLabel != null)            _vfxLabel.text            = L.Get("settings.vfx");
            if (_shakeLabel != null)          _shakeLabel.text          = L.Get("settings.shake");
            if (_musicPulseLabel != null)
                _musicPulseLabel.text = L.CurrentLocale == "fr" ? "Pulse musical"
                    : L.CurrentLocale == "es" ? "Pulso musical"
                    : "Music pulse";
            if (_autoPauseLabel != null)      _autoPauseLabel.text      = L.Get("settings.auto_pause_blur");
            if (_a11ySectionLabel != null)    _a11ySectionLabel.text    = L.Get("settings.a11y_section");
            if (_colorblindLabel != null)     _colorblindLabel.text     = L.Get("settings.colorblind");
            if (_reduceMotionLabel != null)   _reduceMotionLabel.text   = L.Get("settings.reduce_motion");
            if (_largeTextLabel != null)      _largeTextLabel.text      = L.Get("settings.large_text");
            if (_langSectionLabel != null)    _langSectionLabel.text    = L.Get("settings.lang_section");
            if (_langLabel != null)           _langLabel.text           = L.Get("settings.lang_label");
            if (_closeBtn != null)            _closeBtn.text            = L.Get("settings.close");
            if (_keybindingsBtn != null)      _keybindingsBtn.text      = L.CurrentLocale == "fr" ? "Raccourcis"
                : L.CurrentLocale == "es" ? "Atajos" : "Keybindings";
            if (_resetCameraBtn != null)      _resetCameraBtn.text      = L.Get("settings.reset_camera");
            if (_resetProgressBtn != null)    _resetProgressBtn.text    = L.Get("settings.reset_progress");
            if (_resetProgressWarnLabel != null) _resetProgressWarnLabel.text = L.Get("settings.reset_progress_warn");
            if (_followHeroLabel != null)
                _followHeroLabel.text = L.CurrentLocale == "fr" ? "Caméra suit le Hero"
                    : L.CurrentLocale == "es" ? "Cámara sigue al Héroe"
                    : "Camera follows Hero";
            if (_joystickLabel != null)
                _joystickLabel.text = L.CurrentLocale == "fr" ? "Joystick virtuel"
                    : L.CurrentLocale == "es" ? "Joystick virtual"
                    : "Virtual joystick";
            if (_heroAutoAttackLabel != null)
                _heroAutoAttackLabel.text = L.CurrentLocale == "fr" ? "Attaque auto du Héro"
                    : L.CurrentLocale == "es" ? "Ataque automático del Héroe"
                    : "Hero auto-attack";

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

            _gameSpeedSlider?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.GameSpeed = evt.newValue;
                if (_gameSpeedValue != null) _gameSpeedValue.text = "x" + evt.newValue;
            });

            _muteToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.Muted = evt.newValue;
            });

            _sfxMuteToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.SFXMuted = evt.newValue;
            });

            _musicMuteToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.MusicMuted = evt.newValue;
            });

            _followHeroToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents) return;
                var cam = CameraController.Instance;
                if (cam != null) cam.FollowHero = evt.newValue;
            });

            _joystickToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents) return;
                var joystick = VirtualJoystick.Instance;
                if (joystick != null) joystick.Enabled = evt.newValue;
            });

            _heroAutoAttackToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents) return;
                var hero = Systems.LevelRunner.Instance?.Hero;
                if (hero != null) hero.AutoAttack = evt.newValue;
                else PlayerPrefs.SetInt("hero_auto_attack_v1", evt.newValue ? 1 : 0);
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

            _musicPulseToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.MusicPulseEnabled = evt.newValue;
            });

            _autoPauseToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.AutoPauseOnBlur = evt.newValue;
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
            _fullscreenBtn?.RegisterCallback<ClickEvent>(_ => ToggleFullscreen());
            _keybindingsBtn?.RegisterCallback<ClickEvent>(_ => OpenKeyBindings());
            _resetCameraBtn?.RegisterCallback<ClickEvent>(_ => ResetCamera());
            _changeNameBtn?.RegisterCallback<ClickEvent>(_ => OnChangeName());
            _resetProgressBtn?.RegisterCallback<ClickEvent>(_ => OnResetProgressClicked());

            _tabAudio?.RegisterCallback<ClickEvent>(_    => SwitchTab(_tabAudio, _panelAudio));
            _tabVideo?.RegisterCallback<ClickEvent>(_    => SwitchTab(_tabVideo, _panelVideo));
            _tabControls?.RegisterCallback<ClickEvent>(_ => SwitchTab(_tabControls, _panelControls));
            _tabGameplay?.RegisterCallback<ClickEvent>(_ => SwitchTab(_tabGameplay, _panelGameplay));
        }

        private void SwitchTab(Button? activeTab, VisualElement? activePanel)
        {
            Button?[] tabs = { _tabAudio, _tabVideo, _tabControls, _tabGameplay };
            VisualElement?[] panels = { _panelAudio, _panelVideo, _panelControls, _panelGameplay };

            for (int i = 0; i < tabs.Length; i++)
            {
                bool isActive = tabs[i] == activeTab;
                if (tabs[i] != null)
                {
                    if (isActive) tabs[i]!.AddToClassList("tab-active");
                    else tabs[i]!.RemoveFromClassList("tab-active");
                }
                if (panels[i] != null)
                {
                    if (isActive) panels[i]!.RemoveFromClassList("hidden");
                    else panels[i]!.AddToClassList("hidden");
                }
            }
        }

        public void Show()
        {
            SyncFromRegistry();
            SyncFollowHero();
            SyncJoystick();
            SyncHeroAutoAttack();
            UpdateFullscreenLabel();
            SwitchTab(_tabAudio, _panelAudio);
            _settingsRoot?.RemoveFromClassList("hidden");
        }

        public void Hide()
        {
            _settingsRoot?.AddToClassList("hidden");
        }

        private void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            UpdateFullscreenLabel();
        }

        private void UpdateFullscreenLabel()
        {
            if (_fullscreenBtn == null) return;
            _fullscreenBtn.text = Screen.fullScreen
                ? L.Get("settings.fullscreen_off")
                : L.Get("settings.fullscreen_on");
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
                if (_gameSpeedSlider != null) _gameSpeedSlider.value = reg.GameSpeed;
                if (_gameSpeedValue != null) _gameSpeedValue.text = "x" + reg.GameSpeed;
                if (_muteToggle != null) _muteToggle.value = reg.Muted;
                if (_sfxMuteToggle != null) _sfxMuteToggle.value = reg.SFXMuted;
                if (_musicMuteToggle != null) _musicMuteToggle.value = reg.MusicMuted;

                if (_qualityDropdown != null)
                {
                    int idx = Mathf.Clamp(reg.QualityLevel, 0, QualityChoices.Count - 1);
                    _qualityDropdown.SetValueWithoutNotify(QualityChoices[idx]);
                }
                if (_vfxToggle != null) _vfxToggle.value = reg.VFXEnabled;
                if (_shakeToggle != null) _shakeToggle.value = reg.ShakeEnabled;
                if (_musicPulseToggle != null) _musicPulseToggle.value = reg.MusicPulseEnabled;
                if (_autoPauseToggle != null) _autoPauseToggle.value = reg.AutoPauseOnBlur;

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

        private void SyncFollowHero()
        {
            if (_followHeroToggle == null) return;
            var cam = CameraController.Instance;
            _suppressEvents = true;
            _followHeroToggle.value = cam != null && cam.FollowHero;
            _suppressEvents = false;
        }

        private void SyncJoystick()
        {
            if (_joystickToggle == null) return;
            var joystick = VirtualJoystick.Instance;
            _suppressEvents = true;
            _joystickToggle.value = joystick != null && joystick.Enabled;
            _suppressEvents = false;
        }

        private void SyncHeroAutoAttack()
        {
            if (_heroAutoAttackToggle == null) return;
            var hero = Systems.LevelRunner.Instance?.Hero;
            bool val = hero != null ? hero.AutoAttack : PlayerPrefs.GetInt("hero_auto_attack_v1", 1) != 0;
            _suppressEvents = true;
            _heroAutoAttackToggle.value = val;
            _suppressEvents = false;
        }

        private static void ResetCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.transform.SetPositionAndRotation(Vector3.back * 10f, Quaternion.identity);
        }

        private void OnChangeName()
        {
            var popup = FindFirstObjectByType<NameInputPopup>();
            if (popup != null)
                popup.Show(() => { });
        }

        private void OnResetProgressClicked()
        {
            Confirm.Show(
                L.Get("confirm.reset_progress_title"),
                L.Get("confirm.reset_progress_msg"),
                onConfirm: () =>
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    SceneManager.LoadScene(0);
                });
        }

        private void OpenKeyBindings()
        {
            var panel = GetComponent<KeyBindingsPanel>();
            panel?.Show();
        }

        private static string FormatPct(float v) => Mathf.RoundToInt(v * 100f) + "%";
    }
}
