#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using CrowdDefense.Visual;

namespace CrowdDefense.UI
{
    public class SettingsPanelController : UIControllerBase
    {
        public static SettingsPanelController? Instance { get; private set; }
        public bool IsOpen => _settingsRoot != null && !_settingsRoot.ClassListContains("hidden");
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
        private Button? _keyResetBtn;
        private Button? _changeNameBtn;
        private Button? _resetDefaultsBtn;
        private Button? _resetProgressBtn;
        private Label?  _resetProgressWarnLabel;
        private Button? _exportSaveBtn;
        private Button? _importSaveBtn;

        private Button? _diffEasyBtn;
        private Button? _diffNormalBtn;
        private Button? _diffHardBtn;
        private Label?  _diffSectionLabel;

        private DropdownField? _qualityDropdown;
        private DropdownField? _bloomDropdown;
        private Toggle? _vfxToggle;
        private Toggle? _shakeToggle;
        private Toggle? _weatherToggle;
        private Toggle? _damageIconsToggle;
        private Toggle? _autoPauseToggle;
        private Label?  _autoPauseLabel;

        private Toggle? _musicPulseToggle;
        private Label?  _musicPulseLabel;
        private Toggle? _colorblindToggle;
        private Toggle? _reduceMotionToggle;
        private Toggle? _largeTextToggle;
        private SliderInt? _fontSizeSlider;
        private Label?     _fontSizeValue;
        private Toggle?    _highContrastToggle;

        private DropdownField? _langDropdown;
        private TextField? _playerNameField;
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
        private Label? _bloomLabel;
        private Label? _vfxLabel;
        private Label? _shakeLabel;
        private Label? _weatherLabel;
        private Label? _damageIconsLabel;
        private Label? _a11ySectionLabel;
        private Label? _colorblindLabel;
        private Label? _reduceMotionLabel;
        private Label? _largeTextLabel;
        private Label? _fontSizeLabel;
        private Label? _highContrastLabel;
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
        private List<string> BloomChoices => new()
        {
            L.CurrentLocale == "fr" ? "Bas" : L.CurrentLocale == "es" ? "Bajo" : "Low",
            L.CurrentLocale == "fr" ? "Moyen" : L.CurrentLocale == "es" ? "Medio" : "Med",
            L.CurrentLocale == "fr" ? "Haut" : L.CurrentLocale == "es" ? "Alto" : "High",
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
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            if (Root == null) return;

            _settingsRoot = Root.Q<VisualElement>("settings-root");

            _masterSlider = Root.Q<Slider>("master-slider");
            _sfxSlider = Root.Q<Slider>("sfx-slider");
            _musicSlider = Root.Q<Slider>("music-slider");
            _uiSlider = Root.Q<Slider>("ui-slider");
            _gameSpeedSlider = Root.Q<SliderInt>("game-speed-slider");
            _masterValue = Root.Q<Label>("master-value");
            _sfxValue = Root.Q<Label>("sfx-value");
            _musicValue = Root.Q<Label>("music-value");
            _uiValue = Root.Q<Label>("ui-value");
            _gameSpeedValue = Root.Q<Label>("game-speed-value");
            _muteToggle = Root.Q<Toggle>("mute-toggle");
            _sfxMuteToggle = Root.Q<Toggle>("sfx-mute-toggle");
            _musicMuteToggle = Root.Q<Toggle>("music-mute-toggle");
            _followHeroToggle = Root.Q<Toggle>("follow-hero-toggle");
            _followHeroLabel  = Root.Q<Label>("follow-hero-label");
            _joystickToggle   = Root.Q<Toggle>("joystick-toggle");
            _joystickLabel    = Root.Q<Label>("joystick-label");
            _heroAutoAttackToggle = Root.Q<Toggle>("hero-auto-attack-toggle");
            _heroAutoAttackLabel  = Root.Q<Label>("hero-auto-attack-label");
            _resetCameraBtn      = Root.Q<Button>("settings-reset-camera-btn");
            _keyResetBtn         = Root.Q<Button>("key-reset-btn");
            _changeNameBtn       = Root.Q<Button>("settings-change-name-btn");
            _resetDefaultsBtn    = Root.Q<Button>("settings-reset-defaults-btn");
            _resetProgressBtn    = Root.Q<Button>("settings-reset-progress-btn");
            _resetProgressWarnLabel = Root.Q<Label>("reset-progress-warn");
            _exportSaveBtn       = Root.Q<Button>("settings-export-save-btn");
            _importSaveBtn       = Root.Q<Button>("settings-import-save-btn");

            _diffEasyBtn    = Root.Q<Button>("difficulty-btn-easy");
            _diffNormalBtn  = Root.Q<Button>("difficulty-btn-normal");
            _diffHardBtn    = Root.Q<Button>("difficulty-btn-hard");
            _diffSectionLabel = Root.Q<Label>("difficulty-section-title");

            _qualityDropdown = Root.Q<DropdownField>("quality-dropdown");
            _bloomDropdown   = Root.Q<DropdownField>("bloom-dropdown");
            _vfxToggle = Root.Q<Toggle>("vfx-toggle");
            _shakeToggle = Root.Q<Toggle>("shake-toggle");
            _weatherToggle = Root.Q<Toggle>("weather-toggle");
            _damageIconsToggle = Root.Q<Toggle>("damage-icons-toggle");
            _autoPauseToggle = Root.Q<Toggle>("auto-pause-toggle");
            _autoPauseLabel  = Root.Q<Label>("auto-pause-label");

            _musicPulseToggle = Root.Q<Toggle>("music-pulse-toggle");
            _musicPulseLabel  = Root.Q<Label>("music-pulse-label");
            _colorblindToggle = Root.Q<Toggle>("colorblind-toggle");
            _reduceMotionToggle = Root.Q<Toggle>("reduce-motion-toggle");
            _largeTextToggle = Root.Q<Toggle>("large-text-toggle");
            _fontSizeSlider = Root.Q<SliderInt>("font-size-slider");
            _fontSizeValue = Root.Q<Label>("font-size-value");
            _highContrastToggle = Root.Q<Toggle>("high-contrast-toggle");

            if (_fontSizeSlider != null)
            {
                _fontSizeSlider.lowValue = 0;
                _fontSizeSlider.highValue = 2;
            }

            _playerNameField = Root.Q<TextField>("player-name-field");
            _langDropdown = Root.Q<DropdownField>("lang-dropdown");
            _closeBtn = Root.Q<Button>("settings-close-btn");
            _fullscreenBtn = Root.Q<Button>("settings-fullscreen-btn");
            _keybindingsBtn = Root.Q<Button>("settings-keybindings-btn");

            _tabAudio    = Root.Q<Button>("tab-audio");
            _tabVideo    = Root.Q<Button>("tab-video");
            _tabControls = Root.Q<Button>("tab-controls");
            _tabGameplay = Root.Q<Button>("tab-gameplay");
            _panelAudio    = Root.Q<VisualElement>("panel-audio");
            _panelVideo    = Root.Q<VisualElement>("panel-video");
            _panelControls = Root.Q<VisualElement>("panel-controls");
            _panelGameplay = Root.Q<VisualElement>("panel-gameplay");

            _settingsTitleLabel  = Root.Q<Label>("settings-title");
            _audioSectionLabel   = Root.Q<Label>("audio-section-title");
            _masterLabel         = Root.Q<Label>("master-label");
            _sfxLabel            = Root.Q<Label>("sfx-label");
            _musicLabel          = Root.Q<Label>("music-label");
            _uiLabel             = Root.Q<Label>("ui-label");
            _muteLabel           = Root.Q<Label>("mute-label");
            _sfxMuteLabel        = Root.Q<Label>("sfx-mute-label");
            _musicMuteLabel      = Root.Q<Label>("music-mute-label");
            _gameSpeedLabel      = Root.Q<Label>("game-speed-label");
            _gfxSectionLabel     = Root.Q<Label>("gfx-section-title");
            _qualityLabel        = Root.Q<Label>("quality-label");
            _bloomLabel          = Root.Q<Label>("bloom-label");
            _vfxLabel            = Root.Q<Label>("vfx-label");
            _shakeLabel          = Root.Q<Label>("shake-label");
            _weatherLabel        = Root.Q<Label>("weather-label");
            _damageIconsLabel    = Root.Q<Label>("damage-icons-label");
            _a11ySectionLabel    = Root.Q<Label>("a11y-section-title");
            _colorblindLabel     = Root.Q<Label>("colorblind-label");
            _reduceMotionLabel   = Root.Q<Label>("reduce-motion-label");
            _largeTextLabel      = Root.Q<Label>("large-text-label");
            _fontSizeLabel       = Root.Q<Label>("font-size-label");
            _highContrastLabel   = Root.Q<Label>("high-contrast-label");
            _langSectionLabel    = Root.Q<Label>("lang-section-title");
            _langLabel           = Root.Q<Label>("lang-label");

            if (_qualityDropdown != null)
            {
                _qualityDropdown.choices = QualityChoices;
            }
            if (_bloomDropdown != null)
            {
                _bloomDropdown.choices = BloomChoices;
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
            if (_bloomLabel != null)          _bloomLabel.text          = L.CurrentLocale == "fr" ? "Bloom"
                : L.CurrentLocale == "es" ? "Bloom" : "Bloom";
            if (_vfxLabel != null)            _vfxLabel.text            = L.Get("settings.vfx");
            if (_shakeLabel != null)          _shakeLabel.text          = L.Get("settings.shake");
            if (_weatherLabel != null)
                _weatherLabel.text = L.CurrentLocale == "fr" ? "Meteo"
                    : L.CurrentLocale == "es" ? "Clima"
                    : "Weather";
            if (_damageIconsLabel != null)
                _damageIconsLabel.text = L.CurrentLocale == "fr" ? "Icones de degats"
                    : L.CurrentLocale == "es" ? "Iconos de dano"
                    : "Damage Icons";
            if (_musicPulseLabel != null)
                _musicPulseLabel.text = L.CurrentLocale == "fr" ? "Pulse musical"
                    : L.CurrentLocale == "es" ? "Pulso musical"
                    : "Music pulse";
            if (_autoPauseLabel != null)      _autoPauseLabel.text      = L.Get("settings.auto_pause_blur");
            if (_diffSectionLabel != null)
                _diffSectionLabel.text = L.CurrentLocale == "fr" ? "Difficulte"
                    : L.CurrentLocale == "es" ? "Dificultad" : "Difficulty";
            if (_diffEasyBtn != null)
                _diffEasyBtn.text = L.CurrentLocale == "fr" ? "Facile"
                    : L.CurrentLocale == "es" ? "Facil" : "Easy";
            if (_diffNormalBtn != null)
                _diffNormalBtn.text = L.CurrentLocale == "fr" ? "Normal"
                    : L.CurrentLocale == "es" ? "Normal" : "Normal";
            if (_diffHardBtn != null)
                _diffHardBtn.text = L.CurrentLocale == "fr" ? "Difficile"
                    : L.CurrentLocale == "es" ? "Dificil" : "Hard";
            if (_a11ySectionLabel != null)    _a11ySectionLabel.text    = L.Get("settings.a11y_section");
            if (_colorblindLabel != null)     _colorblindLabel.text     = L.Get("settings.colorblind");
            if (_reduceMotionLabel != null)   _reduceMotionLabel.text   = L.Get("settings.reduce_motion");
            if (_largeTextLabel != null)      _largeTextLabel.text      = L.Get("settings.large_text");
            if (_fontSizeLabel != null)
                _fontSizeLabel.text = L.CurrentLocale == "fr" ? "Taille du texte (S/M/L)"
                    : L.CurrentLocale == "es" ? "Tamaño de texto (S/M/L)"
                    : "Font size (S/M/L)";
            if (_highContrastLabel != null)
                _highContrastLabel.text = L.CurrentLocale == "fr" ? "Contraste élevé"
                    : L.CurrentLocale == "es" ? "Alto contraste"
                    : "High contrast";
            if (_langSectionLabel != null)    _langSectionLabel.text    = L.Get("settings.lang_section");
            if (_langLabel != null)           _langLabel.text           = L.Get("settings.lang_label");
            if (_closeBtn != null)            _closeBtn.text            = L.Get("settings.close");
            if (_keybindingsBtn != null)      _keybindingsBtn.text      = L.CurrentLocale == "fr" ? "Raccourcis"
                : L.CurrentLocale == "es" ? "Atajos" : "Keybindings";
            if (_resetCameraBtn != null)      _resetCameraBtn.text      = L.Get("settings.reset_camera");
            if (_keyResetBtn != null)         _keyResetBtn.text         = L.CurrentLocale == "fr" ? "Reinitialiser les touches"
                : L.CurrentLocale == "es" ? "Restablecer teclas"
                : "Reset Keyboard Defaults";
            if (_resetDefaultsBtn != null)    _resetDefaultsBtn.text    = L.CurrentLocale == "fr" ? "Reinitialiser les parametres"
                : L.CurrentLocale == "es" ? "Restablecer ajustes"
                : "Reset to Defaults";
            if (_resetProgressBtn != null)    _resetProgressBtn.text    = L.Get("settings.reset_progress");
            if (_resetProgressWarnLabel != null) _resetProgressWarnLabel.text = L.Get("settings.reset_progress_warn");
            if (_exportSaveBtn != null)       _exportSaveBtn.text       = L.CurrentLocale == "fr" ? "Exporter la sauvegarde"
                : L.CurrentLocale == "es" ? "Exportar guardado"
                : "Export Save";
            if (_importSaveBtn != null)       _importSaveBtn.text       = L.CurrentLocale == "fr" ? "Importer une sauvegarde"
                : L.CurrentLocale == "es" ? "Importar guardado"
                : "Import Save";
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
            if (_bloomDropdown != null)   _bloomDropdown.choices   = BloomChoices;
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

            _bloomDropdown?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                int idx = BloomChoices.IndexOf(evt.newValue);
                if (idx >= 0) SettingsRegistry.Instance.BloomLevel = idx;
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

            _weatherToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.WeatherEnabled = evt.newValue;
            });

            _damageIconsToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.ShowDamageIcons = evt.newValue;
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

            _fontSizeSlider?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.FontSize = evt.newValue;
                if (_fontSizeValue != null)
                    _fontSizeValue.text = evt.newValue == 0 ? "S" : evt.newValue == 2 ? "L" : "M";
            });

            _highContrastToggle?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents || SettingsRegistry.Instance == null) return;
                SettingsRegistry.Instance.HighContrast = evt.newValue;
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

            _diffEasyBtn?.RegisterCallback<ClickEvent>(_   => OnDifficultyPicked(0));
            _diffNormalBtn?.RegisterCallback<ClickEvent>(_ => OnDifficultyPicked(1));
            _diffHardBtn?.RegisterCallback<ClickEvent>(_   => OnDifficultyPicked(2));

            _closeBtn?.RegisterCallback<ClickEvent>(_ => Hide());
            _fullscreenBtn?.RegisterCallback<ClickEvent>(_ => ToggleFullscreen());
            _keybindingsBtn?.RegisterCallback<ClickEvent>(_ => OpenKeyBindings());
            _resetCameraBtn?.RegisterCallback<ClickEvent>(_ => ResetCamera());
            _keyResetBtn?.RegisterCallback<ClickEvent>(_ => OnKeyReset());
            _changeNameBtn?.RegisterCallback<ClickEvent>(_ => OnChangeName());

            _playerNameField?.RegisterValueChangedCallback(evt =>
            {
                if (_suppressEvents) return;
                Systems.PlayerProfile.Instance?.SetName(evt.newValue);
            });
            _resetDefaultsBtn?.RegisterCallback<ClickEvent>(_ => OnResetSettingsClicked());
            _resetProgressBtn?.RegisterCallback<ClickEvent>(_ => OnResetProgressClicked());
            _exportSaveBtn?.RegisterCallback<ClickEvent>(_ => OnExportSave());
            _importSaveBtn?.RegisterCallback<ClickEvent>(_ => OnImportSave());

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
                if (_bloomDropdown != null)
                {
                    int bidx = Mathf.Clamp(reg.BloomLevel, 0, BloomChoices.Count - 1);
                    _bloomDropdown.SetValueWithoutNotify(BloomChoices[bidx]);
                }
                if (_vfxToggle != null) _vfxToggle.value = reg.VFXEnabled;
                if (_shakeToggle != null) _shakeToggle.value = reg.ShakeEnabled;
                if (_weatherToggle != null) _weatherToggle.value = reg.WeatherEnabled;
                if (_damageIconsToggle != null) _damageIconsToggle.value = reg.ShowDamageIcons;
                if (_musicPulseToggle != null) _musicPulseToggle.value = reg.MusicPulseEnabled;
                if (_autoPauseToggle != null) _autoPauseToggle.value = reg.AutoPauseOnBlur;

                if (_colorblindToggle != null) _colorblindToggle.value = reg.ColorblindMode;
                if (_reduceMotionToggle != null) _reduceMotionToggle.value = reg.ReduceMotion;
                if (_largeTextToggle != null) _largeTextToggle.value = reg.LargeText;
                if (_fontSizeSlider != null) _fontSizeSlider.value = reg.FontSize;
                if (_fontSizeValue != null) _fontSizeValue.text = reg.FontSize == 0 ? "S" : reg.FontSize == 2 ? "L" : "M";
                if (_highContrastToggle != null) _highContrastToggle.value = reg.HighContrast;

                if (_langDropdown != null)
                {
                    int li = System.Array.IndexOf(LangCodes, reg.Locale);
                    if (li < 0) li = 0;
                    _langDropdown.SetValueWithoutNotify(LangChoices[li]);
                }

                SyncDifficultyButtons(reg.Difficulty);

                if (_playerNameField != null)
                    _playerNameField.SetValueWithoutNotify(Systems.PlayerProfile.Instance?.GetName() ?? "Joueur");
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

        private static void OnKeyReset() => Systems.KeyBindings.ResetAll();

        private void OnChangeName()
        {
            var popup = FindAnyObjectByType<NameInputPopup>();
            if (popup != null)
                popup.Show(() => { });
        }

        private void OnResetSettingsClicked()
        {
            string title = L.CurrentLocale == "fr" ? "Reinitialiser les parametres"
                : L.CurrentLocale == "es" ? "Restablecer ajustes"
                : "Reset Settings";
            string msg = L.CurrentLocale == "fr" ? "Reinitialiser tous les parametres audio, graphiques et accessibilite ?"
                : L.CurrentLocale == "es" ? "Restaurar todos los ajustes de audio, graficos y accesibilidad ?"
                : "Reset all audio, graphics and accessibility settings?";
            Confirm.Show(title, msg, onConfirm: () =>
            {
                string[] keys = {
                    "cd.audio.master", "cd.audio.sfx", "cd.audio.music", "cd.audio.ui",
                    "cd.audio.muted", "cd.audio.sfx_muted", "cd.audio.music_muted",
                    "cd.gfx.quality", "cd.gfx.vfx", "cd.gfx.shake",
                    "cd.gfx.damage_icons", "cd.gfx.music_pulse_v1", "cd.gfx.weather", "cd.gfx.bloom",
                    "cd.a11y.colorblind", "cd.a11y.reduce_motion", "cd.a11y.large_text",
                    "cd.a11y.font_size", "cd.a11y.high_contrast",
                    "cd.locale", "cd.difficulty",
                };
                foreach (string key in keys)
                    PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                var reg = SettingsRegistry.Instance;
                if (reg != null) { reg.Load(); SyncFromRegistry(); }
            });
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

        private void OnDifficultyPicked(int level)
        {
            if (SettingsRegistry.Instance == null) return;
            SettingsRegistry.Instance.Difficulty = level;
            SyncDifficultyButtons(level);
        }

        private void SyncDifficultyButtons(int level)
        {
            if (_diffEasyBtn   != null) _diffEasyBtn.EnableInClassList("diff-radio-active",   level == 0);
            if (_diffNormalBtn != null) _diffNormalBtn.EnableInClassList("diff-radio-active", level == 1);
            if (_diffHardBtn   != null) _diffHardBtn.EnableInClassList("diff-radio-active",   level == 2);
        }

        private static string FormatPct(float v) => Mathf.RoundToInt(v * 100f) + "%";

        private static void OnExportSave()
        {
            string json = Systems.SaveSystem.ExportToJson();
            GUIUtility.systemCopyBuffer = json;
#if UNITY_EDITOR
            Debug.Log("[Settings] Save exported to clipboard (" + json.Length + " chars)");
#endif
        }

        private void OnImportSave()
        {
            string json = GUIUtility.systemCopyBuffer;
            bool ok = Systems.SaveSystem.ImportFromJson(json);
            string msg = ok
                ? (L.CurrentLocale == "fr" ? "Sauvegarde importee avec succes."
                    : L.CurrentLocale == "es" ? "Guardado importado correctamente."
                    : "Save imported successfully.")
                : (L.CurrentLocale == "fr" ? "Echec de l'import : JSON invalide ou version incompatible."
                    : L.CurrentLocale == "es" ? "Error al importar: JSON invalido o version incompatible."
                    : "Import failed: invalid JSON or incompatible version.");
            Confirm.Show(L.CurrentLocale == "fr" ? "Import sauvegarde"
                : L.CurrentLocale == "es" ? "Importar guardado"
                : "Import Save", msg, onConfirm: () => { });
        }
    }
}
