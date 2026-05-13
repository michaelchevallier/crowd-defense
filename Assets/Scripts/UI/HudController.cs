#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.UI
{
    public class HudController : UIControllerBase
    {
        private Label? goldLabel;
        private Label? goldValue;
        private Label? waveLabel;
        private Label? waveValue;
        private Label? hpLabel;
        private Label? hpValue;
        private VisualElement? hpBarFill;
        private VisualElement? panelGameOver;
        private Label? panelGameOverTitle;
        private Label? panelGameOverSubtitle;
        private VisualElement? panelVictory;
        private Label? panelVictoryTitle;
        private Label? panelVictorySubtitle;
        private Button? btnRestartGo;
        private Button? btnRestartVictory;
        private Button? btnMenuGo;
        private Button? btnMenuVictory;
        private VisualElement? _confirmRestartPanel;
        private Button? _confirmRestartYes;
        private Button? _confirmRestartNo;

        // D1-02 wave launch UI refs
        private VisualElement? waveLaunchBtn;
        private VisualElement? waveLaunchPill;
        private Label? waveLaunchLabel;
        private Label? waveLaunchSub;
        private VisualElement? waveLaunchStreak;
        private Label? waveLaunchStreakText;
        private Label? waveLaunchPillText;

        // Hero panel refs
        private VisualElement? heroPanel;
        private VisualElement? _heroPortraitVE;
        private Label? heroHpLabel;
        private Label? heroLevelLabel;
        private VisualElement? heroXpBarFill;
        private Label? heroXpLabel;
        private Label? heroXpValue;
        private Label? heroUltLabel;
        private VisualElement? ultBtn;
        private VisualElement? ultRingLeft;
        private VisualElement? ultRingRight;

        // BluePill button ref
        private Button? bluePillBtn;

        // Castle HP regen icon — flashes 200 ms on +HP
        private Label? _castleRegenIcon;
        private int    _lastKnownCastleHP = -1;
        private Coroutine? _regenIconCoroutine;

        // Bank pill (D1-01 §3.5)
        private Label? _bankLabel;
        private VisualElement? _bankTooltip;

        // Gems pill (top-bar)
        private Label? _gemsValue;
        private float _gemsRefreshTimer;
        private const float GemsRefreshInterval = 0.5f;

        // Combo multiplier badge (top-right, persistent while combo active)
        private Label? _comboMultiplierLabel;

        // Wave progress dots (top-center)
        private VisualElement? _waveDotsRow;
        private int _wavesCompleted = 0;

        // Wave preview panel (between waves — enemy roster chips)
        private VisualElement? _wavePreviewPanel;
        private VisualElement? _wavePreviewRoster;

        // Boss intro banner (bottom-center, 4s then fade)
        private VisualElement? _bossIntroBanner;
        private Label?         _bossIntroQuote;
        private Coroutine?     _bossIntroCoroutine;

        private static readonly System.Collections.Generic.Dictionary<string, string[]> BossQuotes =
            new System.Collections.Generic.Dictionary<string, string[]>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["apocalypse"] = new[]
            {
                "Contemplez la fin de toutes choses...",
                "Vos défenses ne sont que poussière face à moi.",
                "J'ai détruit mille mondes.",
                "Même les étoiles tremblent à mon approche.",
                "Agenouillez-vous... ou soyez anéantis.",
            },
            ["titan"] = new[]
            {
                "Vos tours sont des jouets.",
                "J'existe depuis avant votre espèce.",
                "La taille, c'est le pouvoir. Vous n'avez ni l'un ni l'autre.",
                "Chaque mur finit par tomber. Toujours.",
                "On ne peut arrêter ce que l'on ne comprend pas.",
            },
            ["phantom"] = new[]
            {
                "On ne peut voir ce que l'on ne craint pas.",
                "Je glisse entre vos projectiles comme de la fumée.",
                "La mort a bien des visages. Voici le mien.",
                "Vos yeux vous trompent... comme toujours.",
                "Les ombres ne saignent pas.",
            },
        };

        private static readonly string[] DefaultBossQuotes =
        {
            "Voici... le destructeur de mondes !",
            "Votre résistance s'arrête ici.",
            "Je suis venu réclamer ce qui m'est dû.",
            "La peur est votre dernière arme.",
            "Chaque château finit par tomber.",
        };

        // Boss healthbar (top-center, shown while a boss is alive)
        private VisualElement? _bossHpRoot;
        private VisualElement? _bossHpFill;
        private Label?         _bossNameLabel;
        private Label?         _bossHpPctLabel;
        private Enemy?         _trackedBoss;

        // Keyboard hints footer label
        private Label? keyboardHintsLabel;

        // Wave kill counter label
        private Label? waveKillCounter;

        // Wave enemy count remaining label (top-left, "Enemies: 12 / 50")
        private Label? _enemyCountLabel;

        // Wave elapsed time label
        private Label? waveTimeLabel;
        private float _waveStartTime = -1f;
        private float _lastWaveTickTime = -1f;

        // Skip-window countdown pulse state
        private float _prevSkipRemaining = -1f;
        private static readonly Color _skipTimerDefaultColor = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color _skipTimerWarningColor = Color.red;

        // Responsive breakpoints
        private int _lastWidth = -1;

        // Debounce 300ms shared between click and N key (unscaled time — immune to timeScale)
        private float lastLaunchInputTime = -1f;

        // Race condition: track if WaveManager was subscribed after delayed init
        private bool _waveManagerSubscribed = false;

        // Doctrine panel controller — sibling component on same GameObject
        private DoctrineController? _doctrineCtrl;

        // Settings panel controller — sibling component on same GameObject
        private SettingsPanelController? _settingsCtrl;

        private void Start()
        {
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            if (Root == null) return;

            _doctrineCtrl = GetComponent<DoctrineController>();

            // Auto-add UI sibling controllers that share the HUD UIDocument (each Qs its own
            // elements out of HUD.uxml). Idempotent: only added when not already present.
            EnsureSibling<SettingsPanelController>();
            _settingsCtrl = GetComponent<SettingsPanelController>();
            EnsureSibling<PauseMenuController>();
            EnsureSibling<TowerToolbarController>();
            EnsureSibling<TowerTooltipController>();
            EnsureSibling<SynergyHudController>();
            EnsureSibling<FloatingPopupController>();
            EnsureSibling<RadialMenuController>();
            EnsureSibling<TowerSelectMenuController>();
            EnsureSibling<MuteToggleController>();
            EnsureSibling<HeroSkillBarController>();
            EnsureSibling<MinimapController>();
            EnsureSibling<SpeedControlController>();
            EnsureSibling<HudPerkBadges>();
            EnsureSibling<HelpOverlayController>();
            EnsureSibling<QuickSaveHotkey>();
            EnsureSibling<KeyBindingsPanel>();
            EnsureSibling<RuntimeProfilePanel>();
            EnsureSibling<AchievementToastController>();
            EnsureSibling<EncyclopediaController>();

            ApplyDeviceClasses(Root);
            goldLabel = Root.Q<Label>("gold-label");
            goldValue = Root.Q<Label>("gold-value");
            _gemsValue = Root.Q<Label>("gems-value");
            RefreshGems();
            waveLabel = Root.Q<Label>("wave-label");
            waveValue = Root.Q<Label>("wave-value");
            hpLabel = Root.Q<Label>("hp-label");
            hpValue = Root.Q<Label>("hp-value");
            hpBarFill = Root.Q<VisualElement>("hp-bar-fill");
            _castleRegenIcon = Root.Q<Label>("castle-regen-icon");
            if (_castleRegenIcon == null)
            {
                // Fallback: create the icon element dynamically if not present in UXML
                _castleRegenIcon = new Label { name = "castle-regen-icon", text = "+" };
                _castleRegenIcon.AddToClassList("castle-regen-icon");
                _castleRegenIcon.AddToClassList("hidden");
                Root.Q<VisualElement>("hp-bar-fill")?.parent?.Add(_castleRegenIcon);
            }
            else
            {
                _castleRegenIcon.AddToClassList("hidden");
            }
            panelGameOver = Root.Q<VisualElement>("panel-game-over");
            panelGameOverTitle = Root.Q<Label>("panel-game-over-title");
            panelGameOverSubtitle = Root.Q<Label>("panel-game-over-subtitle");
            panelVictory = Root.Q<VisualElement>("panel-victory");
            panelVictoryTitle = Root.Q<Label>("panel-victory-title");
            panelVictorySubtitle = Root.Q<Label>("panel-victory-subtitle");
            btnRestartGo = Root.Q<Button>("btn-restart-go");
            btnRestartVictory = Root.Q<Button>("btn-restart-victory");
            btnMenuGo = Root.Q<Button>("btn-menu-go");
            btnMenuVictory = Root.Q<Button>("btn-menu-victory");
            _confirmRestartPanel = Root.Q<VisualElement>("confirm-restart-panel");
            _confirmRestartYes = Root.Q<Button>("btn-confirm-restart-yes");
            _confirmRestartNo = Root.Q<Button>("btn-confirm-restart-no");

            waveLaunchBtn = Root.Q<VisualElement>("wave-launch-btn");
            waveLaunchPill = Root.Q<VisualElement>("wave-launch-pill");
            waveLaunchLabel = Root.Q<Label>("wave-launch-label");
            waveLaunchSub = Root.Q<Label>("wave-launch-sub");
            waveLaunchStreak = Root.Q<VisualElement>("wave-launch-streak");
            waveLaunchStreakText = Root.Q<Label>("wave-launch-streak-text");
            waveLaunchPillText = Root.Q<Label>("wave-launch-pill-text");

            heroPanel = Root.Q<VisualElement>("hero-panel");
            _heroPortraitVE = Root.Q<VisualElement>("hero-portrait");
            BindHeroPortraitColor();
            heroHpLabel = Root.Q<Label>("hero-hp-label");
            heroLevelLabel = Root.Q<Label>("hero-level");
            heroXpLabel = Root.Q<Label>("hero-xp-label");
            heroXpBarFill = Root.Q<VisualElement>("hero-xp-bar-fill");
            heroXpValue = Root.Q<Label>("hero-xp-value");
            heroUltLabel = Root.Q<Label>("hero-ult-label");
            ultBtn = Root.Q<VisualElement>("ult-btn");
            ultRingLeft = Root.Q<VisualElement>("ult-ring-left");
            ultRingRight = Root.Q<VisualElement>("ult-ring-right");

            keyboardHintsLabel = Root.Q<Label>("keyboard-hints-label");
            waveKillCounter = Root.Q<Label>("wave-kill-counter");
            waveTimeLabel = Root.Q<Label>("wave-time");
            BuildEnemyCountLabel(Root);
            bluePillBtn = Root.Q<Button>("bluepill-btn");
            _comboMultiplierLabel = Root.Q<Label>("combo-multiplier-label");
            _bankLabel = Root.Q<Label>("bank-label");
            _bankTooltip = Root.Q<VisualElement>("bank-tooltip");

            BuildBossHpBar(Root);
            BuildBossIntroBanner(Root);
            BuildWaveProgressDots(Root);
            BindWavePreview(Root);
            // Force initial values so top-bar is never blank at runtime
            if (goldValue != null) goldValue.text = "$0";
            if (waveValue != null) waveValue.text = "—";
            if (hpValue != null) hpValue.text = "—";

            WireCallbacks();
            SubscribeSystems();
        }

        // Sets hero-portrait background-color from the avatar selection (mirrors HeroPortraitController logic).
        private void BindHeroPortraitColor()
        {
            if (_heroPortraitVE == null) return;
            var avatarKey = PlayerPrefs.GetString("hero_avatar", "");
            Color color;
            if (!string.IsNullOrEmpty(avatarKey) && System.Enum.TryParse<HeroAvatar>(avatarKey, out var avatar))
                color = HeroType.AvatarColor(avatar);
            else
            {
                var heroDef = LevelRunner.Instance?.HeroTypeDef;
                color = heroDef != null ? heroDef.BodyColor : new Color(0.3f, 0.5f, 1f);
            }
            _heroPortraitVE.style.backgroundColor = new StyleColor(color);
        }

        private void WireCallbacks()
        {
            ultBtn?.RegisterCallback<ClickEvent>(_ => TryCastUlt());
            bluePillBtn?.RegisterCallback<ClickEvent>(_ => TryStartBluePill());
            btnRestartGo?.RegisterCallback<ClickEvent>(_ => ShowRestartConfirm());
            _confirmRestartYes?.RegisterCallback<ClickEvent>(_ => { HideRestartConfirm(); Restart(); });
            _confirmRestartNo?.RegisterCallback<ClickEvent>(_ => HideRestartConfirm());
            btnRestartVictory?.RegisterCallback<ClickEvent>(_ => Restart());
            btnMenuGo?.RegisterCallback<ClickEvent>(_ => GoToMenu());
            btnMenuVictory?.RegisterCallback<ClickEvent>(_ => GoToMenu());
            waveLaunchBtn?.RegisterCallback<ClickEvent>(_ => TryLaunchWave());
            Root?.Q<Button>("btn-doctrine")?.RegisterCallback<ClickEvent>(_ => _doctrineCtrl?.Show());
            Root?.Q<Button>("btn-shop")?.RegisterCallback<ClickEvent>(_ => Systems.LevelLoader.GoToShop());
            Root?.Q<Button>("btn-map")?.RegisterCallback<ClickEvent>(_ => Systems.LevelLoader.GoToWorldMap());
            Root?.Q<Button>("btn-settings")?.RegisterCallback<ClickEvent>(_ => _settingsCtrl?.Show());
            Root?.Q<Button>("btn-encyclopedia")?.RegisterCallback<ClickEvent>(_ => EncyclopediaController.Instance?.Show());

            _bankLabel?.RegisterCallback<MouseEnterEvent>(_ => ShowBankTooltip());
            _bankLabel?.RegisterCallback<MouseLeaveEvent>(_ => HideBankTooltip());
        }

        private void SubscribeSystems()
        {
            ApplyLocalizedTexts();
            L.OnLocaleChanged += ApplyLocalizedTexts;

            if (Economy.Instance != null)
            {
                Economy.Instance.OnGoldChanged += OnGoldChanged;
                Economy.Instance.OnBankTick += HandleBankTick;
                OnGoldChanged(Economy.Instance.Gold);
            }

            if (LevelRunner.Instance != null)
            {
                LevelRunner.Instance.OnTotalHPChanged += OnHPChanged;
                LevelRunner.Instance.OnStateChanged += OnStateChanged;
                OnHPChanged(LevelRunner.Instance.TotalCastleHP, LevelRunner.Instance.TotalCastleHPMax);
                OnStateChanged(LevelRunner.Instance.State);
            }

            // Subscribe Castle regen detection (HP gain triggers icon flash)
            if (Castle.Instance != null)
            {
                _lastKnownCastleHP = Castle.Instance.HP;
                Castle.Instance.OnHPChanged += OnCastleHPChanged;
            }

            if (WaveManager.Instance != null)
            {
                _waveManagerSubscribed = true;
                WaveManager.Instance.OnWaveStart += OnWaveStart;
                WaveManager.Instance.OnWaveCleared += OnWaveCleared;
                WaveManager.Instance.OnBreakStateChanged += OnBreakStateChanged;
                WaveManager.Instance.OnKillCountChanged += OnKillCountChanged;
                OnWaveStart(WaveManager.Instance.CurrentWaveIdx);
                // Sync initial break state (W1 waits for player)
                OnBreakStateChanged();
            }

            EventManager.Instance?.Subscribe<ComboUpdatedEvent>(HandleComboUpdated);
            EventManager.Instance?.Subscribe<ComboResetEvent>(HandleComboReset);
            EventManager.Instance?.Subscribe<EnemySpawnedEvent>(HandleEnemySpawned);
            Enemy.OnDeathStatic += HandleEnemyDeath;
        }

        private void OnDestroy()
        {
            L.OnLocaleChanged -= ApplyLocalizedTexts;
            if (Economy.Instance != null)
            {
                Economy.Instance.OnGoldChanged -= OnGoldChanged;
                Economy.Instance.OnBankTick -= HandleBankTick;
            }
            if (LevelRunner.Instance != null)
            {
                LevelRunner.Instance.OnTotalHPChanged -= OnHPChanged;
                LevelRunner.Instance.OnStateChanged -= OnStateChanged;
            }
            if (Castle.Instance != null)
                Castle.Instance.OnHPChanged -= OnCastleHPChanged;
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStart -= OnWaveStart;
                WaveManager.Instance.OnWaveCleared -= OnWaveCleared;
                WaveManager.Instance.OnBreakStateChanged -= OnBreakStateChanged;
                WaveManager.Instance.OnKillCountChanged -= OnKillCountChanged;
            }
            EventManager.Instance?.Unsubscribe<ComboUpdatedEvent>(HandleComboUpdated);
            EventManager.Instance?.Unsubscribe<ComboResetEvent>(HandleComboReset);
            EventManager.Instance?.Unsubscribe<EnemySpawnedEvent>(HandleEnemySpawned);
            Enemy.OnDeathStatic  -= HandleEnemyDeath;
        }

        private void HandleComboUpdated(ComboUpdatedEvent evt)
        {
            if (_comboMultiplierLabel == null) return;
            if (evt.Multiplier <= 1.01f)
            {
                _comboMultiplierLabel.AddToClassList("hidden");
                return;
            }
            _comboMultiplierLabel.text = $"x{evt.Multiplier:F1}";
            _comboMultiplierLabel.RemoveFromClassList("hidden");
        }

        private void HandleComboReset(ComboResetEvent _)
        {
            _comboMultiplierLabel?.AddToClassList("hidden");
        }

        private void ApplyLocalizedTexts()
        {
            if (goldLabel != null) goldLabel.text = L.Get("hud.gold_label");
            if (waveLabel != null) waveLabel.text = L.Get("hud.wave_label");
            if (hpLabel != null) hpLabel.text = L.Get("hud.hp_label");
            if (panelGameOverTitle != null) panelGameOverTitle.text = L.Get("overlay.game_over_title");
            if (panelGameOverSubtitle != null) panelGameOverSubtitle.text = L.Get("overlay.game_over_subtitle");
            if (panelVictoryTitle != null) panelVictoryTitle.text = L.Get("overlay.victory_title");
            if (panelVictorySubtitle != null) panelVictorySubtitle.text = L.Get("overlay.victory_subtitle");
            if (btnRestartGo != null) btnRestartGo.text = L.Get("overlay.btn_restart");
            if (btnRestartVictory != null) btnRestartVictory.text = L.Get("overlay.btn_retry");
            if (btnMenuGo != null) btnMenuGo.text = L.Get("overlay.btn_menu");
            if (btnMenuVictory != null) btnMenuVictory.text = L.Get("overlay.btn_menu");
            if (heroHpLabel != null) heroHpLabel.text = L.Get("hud.hero_hp_label");
            if (heroXpLabel != null) heroXpLabel.text = L.Get("hud.hero_xp_label");
            if (keyboardHintsLabel != null) keyboardHintsLabel.text = L.Get("hud.keyboard_hints");
            OnBreakStateChanged();
        }

        private void Update()
        {
            if (Screen.width != _lastWidth)
            {
                _lastWidth = Screen.width;
                ApplyResponsiveClass();
            }

            // Resolve WaveManager race condition: if not subscribed in Start(), try again here
            if (!_waveManagerSubscribed && WaveManager.Instance != null)
            {
                _waveManagerSubscribed = true;
                WaveManager.Instance.OnWaveStart += OnWaveStart;
                WaveManager.Instance.OnWaveCleared += OnWaveCleared;
                WaveManager.Instance.OnBreakStateChanged += OnBreakStateChanged;
                WaveManager.Instance.OnKillCountChanged += OnKillCountChanged;
                OnWaveStart(WaveManager.Instance.CurrentWaveIdx);
                OnBreakStateChanged();
            }

            // I hotkey — toggle encyclopedia
            if (Input.GetKeyDown(KeyCode.I))
            {
                var enc = EncyclopediaController.Instance;
                if (enc != null)
                {
                    if (enc.IsOpen) enc.Hide();
                    else enc.Show();
                }
            }

            // N hotkey — debounced, shared with click (Q7)
            if (Input.GetKeyDown(KeyCode.N))
                TryLaunchWave();

            // Space — hero ultimate cast
            if (Input.GetKeyDown(KeyCode.Space))
                TryCastUlt();

            _gemsRefreshTimer -= Time.unscaledDeltaTime;
            if (_gemsRefreshTimer <= 0f)
            {
                _gemsRefreshTimer = GemsRefreshInterval;
                RefreshGems();
            }

            TickBreakPill();
            TickWaveTime();
            UpdateHeroPanel();
            TickBossHpBar();
        }

        // Per-frame smooth countdown on the pill badge and main label during the skip bonus window
        private void TickBreakPill()
        {
            var wm = WaveManager.Instance;
            if (wm == null || !wm.IsWaitingForPlayerStart)
            {
                ResetSkipTimerWarning();
                _prevSkipRemaining = -1f;
                return;
            }
            float secondsLeft = wm.SkipWindowSecondsRemaining;
            if (secondsLeft <= 0f)
            {
                ResetSkipTimerWarning();
                _prevSkipRemaining = -1f;
                return;
            }

            if (waveLaunchPill != null && waveLaunchPillText != null)
                waveLaunchPillText.text = L.Get("hud.pill_skip_text", secondsLeft, Mathf.RoundToInt(wm.StreakCount * 5));

            if (waveLaunchLabel != null)
                waveLaunchLabel.text = L.Get("hud.wave_launch_countdown", wm.NextWaveDisplayNumber, secondsLeft);

            _prevSkipRemaining = secondsLeft;
        }

        private void ResetSkipTimerWarning()
        {
            if (waveLaunchPillText != null)
            {
                waveLaunchPillText.style.color = new StyleColor(_skipTimerDefaultColor);
                waveLaunchPillText.style.scale = new StyleScale(new Vector3(1f, 1f, 1f));
            }
            if (waveLaunchLabel != null)
            {
                waveLaunchLabel.style.color = new StyleColor(_skipTimerDefaultColor);
                waveLaunchLabel.style.scale = new StyleScale(new Vector3(1f, 1f, 1f));
            }
        }

        private void TickWaveTime()
        {
            if (waveTimeLabel == null || _waveStartTime < 0f) return;
            float now = Time.unscaledTime;
            if (now - _lastWaveTickTime < 1f) return;
            _lastWaveTickTime = now;
            float elapsed = now - _waveStartTime;
            waveTimeLabel.text = $"⏱ {TimeFormatter.FormatMMSS(elapsed)}";
        }

        private void UpdateHeroPanel()
        {
            var hero = LevelRunner.Instance?.Hero;
            if (heroPanel == null) return;

            if (hero == null)
            {
                SetVisible(heroPanel, false);
                if (bluePillBtn != null) SetVisible(bluePillBtn, false);
                return;
            }

            SetVisible(heroPanel, true);
            // Show BluePill button only when hero is alive and play is active.
            bool playActive = LevelRunner.Instance?.State is GameState.WaveActive or GameState.WaveBreak or GameState.Lobby;
            if (bluePillBtn != null) SetVisible(bluePillBtn, playActive);

            if (heroLevelLabel != null)
                heroLevelLabel.text = L.Get("hud.hero_level", hero.Level);

            if (heroXpBarFill != null)
            {
                float xpRatio = hero.XpToNext > 0 ? Mathf.Clamp01((float)hero.Xp / hero.XpToNext) : 1f;
                heroXpBarFill.style.width = new Length(xpRatio * 100f, LengthUnit.Percent);
            }

            if (heroXpValue != null)
                heroXpValue.text = $"{hero.Xp}/{hero.XpToNext}";

            if (heroUltLabel != null)
            {
                bool ultReady = hero.UltCooldownRemaining <= 0f;
                if (ultReady)
                {
                    heroUltLabel.text = L.Get("hud.hero_ult_ready");
                    heroUltLabel.RemoveFromClassList("hero-ult-cooldown");
                    heroUltLabel.AddToClassList("hero-ult-ready");
                }
                else
                {
                    heroUltLabel.text = L.Get("hud.hero_ult_cd", hero.UltCooldownRemaining);
                    heroUltLabel.RemoveFromClassList("hero-ult-ready");
                    heroUltLabel.AddToClassList("hero-ult-cooldown");
                }

                if (ultBtn != null)
                {
                    ultReady = hero.UltCooldownRemaining <= 0f;
                    if (ultReady)
                    {
                        ultBtn.AddToClassList("ult-btn-ready");
                        ultBtn.RemoveFromClassList("ult-btn-cooldown");
                    }
                    else
                    {
                        ultBtn.RemoveFromClassList("ult-btn-ready");
                        ultBtn.AddToClassList("ult-btn-cooldown");
                    }
                    UpdateUltRing(1f - hero.UltCooldownFraction);
                }
            }
        }

        // Hero ultimate cast (Space key + ult button click)
        private void TryCastUlt()
        {
            var hero = LevelRunner.Instance?.Hero;
            hero?.TryUlt();
        }

        // Drive two-half circular arc: progress 0..1 = empty..full
        private void UpdateUltRing(float progress)
        {
            if (ultRingLeft == null || ultRingRight == null) return;
            progress = Mathf.Clamp01(progress);

            // Right half covers 0..0.5 (0..180 deg), left half covers 0.5..1 (180..360 deg)
            float rightDeg = Mathf.Clamp01(progress * 2f) * 180f;
            float leftDeg  = Mathf.Clamp01((progress - 0.5f) * 2f) * 180f;

            ultRingRight.style.rotate = new Rotate(new Angle(rightDeg - 180f, AngleUnit.Degree));
            ultRingLeft.style.rotate  = new Rotate(new Angle(leftDeg  - 180f, AngleUnit.Degree));
        }

        // Shared entry point for click + B key
        private void TryStartBluePill()
        {
            var hero = LevelRunner.Instance?.Hero;
            if (hero == null) return;
            Systems.BluePill.Instance?.StartChannel(hero.transform);
        }

        // Shared debounced launch entry point for click + N key
        private void TryLaunchWave()
        {
            var wm = WaveManager.Instance;
            if (wm == null || !wm.IsWaitingForPlayerStart) return;
            float now = Time.unscaledTime;
            float debounceSec = BalanceConfig.Get().InputDebounceMs / 1000f;
            if (now - lastLaunchInputTime < debounceSec) return;
            lastLaunchInputTime = now;

            bool wasInWindow = wm.SkipWindowSecondsRemaining > 0f;
            int streakBefore = wm.StreakCount;

            // Hide launch controls and start wave
            if (waveLaunchBtn != null) SetVisible(waveLaunchBtn, false);
            if (waveLaunchPill != null) SetVisible(waveLaunchPill, false);
            wm.StartNextWave();

            if (wasInWindow)
            {
                if (waveLaunchBtn != null) StartCoroutine(FlashButtonGreen(waveLaunchBtn, 0.35f));

                var cfg = BalanceConfig.Get();

                // Floating popup near gold counter
                if (FloatingPopupController.Instance != null && goldValue != null)
                {
                    var gvPanel = goldValue.worldBound;
                    float sx = gvPanel.center.x;
                    float sy = gvPanel.center.y - 20f;
                    FloatingPopupController.Instance.SpawnAtScreenPos(
                        $"+{cfg.SkipBonusGold}g Skip Bonus!", "popup-coin", sx, sy);
                }
                AudioController.Instance?.Play("gold_earn", 0.9f);

                Toast.Show(
                    L.Get("hud.skip_toast_title"),
                    L.Get("hud.skip_toast_body"),
                    1800,
                    null,
                    ToastType.Perk
                );

                int newStreak = streakBefore + 1;
                if (newStreak > 0)
                {
                    int pctBonus = Mathf.RoundToInt(Mathf.Min(newStreak, cfg.StreakCap) * cfg.StreakBonusPerWave * 100f);
                    Toast.Show(
                        L.Get("hud.streak_toast_title", newStreak),
                        L.Get("hud.streak_toast_body", pctBonus),
                        2000,
                        null,
                        ToastType.Combo
                    );
                }
            }
        }

        // ── Wave progress dots ────────────────────────────────────────────────

        private void BuildWaveProgressDots(VisualElement root)
        {
            _waveDotsRow = new VisualElement { name = "wave-progress-dots" };
            _waveDotsRow.style.position      = Position.Absolute;
            _waveDotsRow.style.top           = new Length(6f, LengthUnit.Pixel);
            _waveDotsRow.style.left          = new Length(50f, LengthUnit.Percent);
            _waveDotsRow.style.translate     = new Translate(new Length(-50f, LengthUnit.Percent), Length.Auto());
            _waveDotsRow.style.flexDirection = FlexDirection.Row;
            _waveDotsRow.style.alignItems    = Align.Center;
            _waveDotsRow.style.flexWrap      = Wrap.Wrap;
            _waveDotsRow.style.justifyContent = Justify.Center;
            _waveDotsRow.style.maxWidth      = new Length(70f, LengthUnit.Percent);
            root.Add(_waveDotsRow);
            RefreshWaveDots();
        }

        private void RefreshWaveDots()
        {
            if (_waveDotsRow == null) return;
            var wm = WaveManager.Instance;
            int total = wm?.TotalWaves ?? 0;
            int current = wm?.CurrentWaveIdx ?? 0;
            int completed = _wavesCompleted;

            _waveDotsRow.Clear();
            if (total <= 0) return;

            bool endless = LevelRunner.Instance?.IsEndlessRun == true;
            if (endless) return; // no fixed total in endless mode

            for (int i = 0; i < total; i++)
            {
                var dot = new VisualElement();
                dot.style.width        = new Length(10f, LengthUnit.Pixel);
                dot.style.height       = new Length(10f, LengthUnit.Pixel);
                dot.style.marginLeft   = new Length(3f, LengthUnit.Pixel);
                dot.style.marginRight  = new Length(3f, LengthUnit.Pixel);
                dot.style.borderTopLeftRadius     = new Length(5f, LengthUnit.Pixel);
                dot.style.borderTopRightRadius    = new Length(5f, LengthUnit.Pixel);
                dot.style.borderBottomLeftRadius  = new Length(5f, LengthUnit.Pixel);
                dot.style.borderBottomRightRadius = new Length(5f, LengthUnit.Pixel);
                dot.style.borderTopWidth    = dot.style.borderBottomWidth =
                dot.style.borderLeftWidth   = dot.style.borderRightWidth = 1f;

                if (i < completed)
                {
                    // Completed — solid green
                    dot.style.backgroundColor  = new StyleColor(new Color(0.18f, 0.78f, 0.22f));
                    dot.style.borderTopColor   = dot.style.borderBottomColor =
                    dot.style.borderLeftColor  = dot.style.borderRightColor = new StyleColor(new Color(0.1f, 0.55f, 0.15f));
                    dot.style.opacity = 1f;
                }
                else if (i == current)
                {
                    // Current wave — pulsing yellow via USS animation class
                    dot.style.backgroundColor  = new StyleColor(new Color(1f, 0.84f, 0f));
                    dot.style.borderTopColor   = dot.style.borderBottomColor =
                    dot.style.borderLeftColor  = dot.style.borderRightColor = new StyleColor(new Color(0.9f, 0.6f, 0f));
                    dot.AddToClassList("wave-dot-pulse");
                }
                else
                {
                    // Future — grey
                    dot.style.backgroundColor  = new StyleColor(new Color(0.35f, 0.35f, 0.35f, 0.75f));
                    dot.style.borderTopColor   = dot.style.borderBottomColor =
                    dot.style.borderLeftColor  = dot.style.borderRightColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f, 0.75f));
                    dot.style.opacity = 0.7f;
                }
                _waveDotsRow.Add(dot);
            }
        }

        private void OnWaveCleared(int idx)
        {
            _wavesCompleted = idx + 1;
            RefreshWaveDots();
        }

        private System.Collections.IEnumerator FlashButtonGreen(VisualElement? btn, float duration)
        {
            if (btn == null) yield break;
            btn.AddToClassList("skip-bonus-flash");
            yield return new WaitForSecondsRealtime(duration);
            btn.RemoveFromClassList("skip-bonus-flash");
        }

        private void OnGoldChanged(int gold)
        {
            if (goldValue != null) goldValue.text = $"${gold}";
        }

        private void RefreshGems()
        {
            if (_gemsValue != null) _gemsValue.text = Systems.SaveSystem.GetGems().ToString();
        }

        // gain=0 means bank reset (castle damaged); gain>0 means interest ticked
        private void HandleBankTick(int gain, int totalAccumulated)
        {
            if (_bankLabel == null) return;
            if (gain <= 0)
            {
                _bankLabel.AddToClassList("hidden");
                HideBankTooltip();
                return;
            }
            _bankLabel.RemoveFromClassList("hidden");
            _bankLabel.text = $"\U0001F3E6 {totalAccumulated}¢ (+5%)";
            // Update dynamic title with live amount
            var titleLabel = _bankTooltip?.Q<Label>("bank-tooltip-title");
            if (titleLabel != null) titleLabel.text = $"Banque : {totalAccumulated}¢";
        }

        private void ShowBankTooltip() => _bankTooltip?.RemoveFromClassList("hidden");
        private void HideBankTooltip() => _bankTooltip?.AddToClassList("hidden");

        private void OnHPChanged(int hp, int hpMax)
        {
            if (hpValue != null) hpValue.text = $"{hp}/{hpMax}";
            if (hpBarFill != null)
            {
                float ratio = hpMax > 0 ? (float)hp / hpMax : 0f;
                hpBarFill.style.width = new Length(ratio * 100f, LengthUnit.Percent);
                hpBarFill.style.backgroundColor = ratio > 0.6f
                    ? new Color(0.31f, 0.86f, 0.31f)
                    : ratio > 0.3f
                        ? new Color(0.86f, 0.55f, 0.13f)
                        : new Color(0.86f, 0.20f, 0.13f);
            }
        }

        private void OnWaveStart(int idx)
        {
            RefreshWaveDots();

            if (waveValue == null || WaveManager.Instance == null) return;
            bool endless = LevelRunner.Instance?.IsEndlessRun == true;
            waveValue.text = endless
                ? $"Inf. {idx + 1}"
                : $"{idx + 1}/{WaveManager.Instance.TotalWaves}";
            // Hide launch button while wave is in progress
            if (waveLaunchBtn != null) SetVisible(waveLaunchBtn, false);
            if (waveLaunchPill != null) SetVisible(waveLaunchPill, false);
            // Reset kill counter display at wave start
            if (waveKillCounter != null)
            {
                waveKillCounter.text = "\U0001F480 0 / 0";
                SetVisible(waveKillCounter, true);
            }
            // Show enemy count remaining label
            if (_enemyCountLabel != null)
            {
                _enemyCountLabel.text = "Ennemis: ... / ...";
                SetVisible(_enemyCountLabel, true);
            }
            // Reset and show wave timer
            _waveStartTime = Time.unscaledTime;
            _lastWaveTickTime = -1f;
            if (waveTimeLabel != null)
            {
                waveTimeLabel.text = "⏱ 0:00";
                SetVisible(waveTimeLabel, true);
            }
        }

        private void OnKillCountChanged(int kills, int total)
        {
            if (waveKillCounter == null) return;
            waveKillCounter.text = $"\U0001F480 {kills} / {total}";
            RefreshEnemyCount(kills, total);
        }

        private void RefreshEnemyCount(int kills, int total)
        {
            if (_enemyCountLabel == null) return;
            int remaining = Mathf.Max(0, total - kills);
            _enemyCountLabel.text = $"Ennemis: {remaining} / {total}";
        }

        private void OnBreakStateChanged()
        {
            if (WaveManager.Instance == null) return;
            var wm = WaveManager.Instance;
            bool waiting = wm.IsWaitingForPlayerStart;
            float secondsLeft = wm.SkipWindowSecondsRemaining;
            int streak = wm.StreakCount;
            bool inWindow = secondsLeft > 0f;

            // Stop and hide wave timer when break starts
            if (waiting && waveTimeLabel != null)
            {
                _waveStartTime = -1f;
                SetVisible(waveTimeLabel, false);
            }
            // Hide enemy count label during break
            if (waiting && _enemyCountLabel != null)
                SetVisible(_enemyCountLabel, false);

            // Show/hide launch button
            if (waveLaunchBtn != null)
            {
                SetVisible(waveLaunchBtn, waiting);

                // Update label text — show +30¢ hint during skip window
                if (waveLaunchLabel != null)
                    waveLaunchLabel.text = inWindow ? L.Get("hud.wave_launch_bonus") : L.Get("hud.wave_launch");

                if (waveLaunchSub != null)
                {
                    bool endlessRun = LevelRunner.Instance?.IsEndlessRun == true;
                    waveLaunchSub.text = endlessRun
                        ? $"Inf. Vague {wm.NextWaveDisplayNumber}"
                        : L.Get("hud.wave_progress", wm.NextWaveDisplayNumber, wm.TotalWaves);
                }

                // Skip window ring class
                if (inWindow) waveLaunchBtn.AddToClassList("skip-window");
                else waveLaunchBtn.RemoveFromClassList("skip-window");
            }

            // Show/hide streak badge
            if (waveLaunchStreak != null)
            {
                bool showStreak = waiting && streak > 0;
                SetVisible(waveLaunchStreak, showStreak);
                if (showStreak && waveLaunchStreakText != null)
                    waveLaunchStreakText.text = L.Get("hud.streak_text", streak * 5);
            }

            // Show/hide pill timer
            if (waveLaunchPill != null)
            {
                SetVisible(waveLaunchPill, waiting && inWindow);
                if (inWindow && waveLaunchPillText != null)
                    waveLaunchPillText.text = L.Get("hud.pill_skip_text", secondsLeft, Mathf.RoundToInt(streak * 5));
            }

            RefreshWavePreviewRoster();
        }

        private void OnStateChanged(GameState state)
        {
            if (panelGameOver != null) SetVisible(panelGameOver, state == GameState.Lost);
            if (panelVictory != null)  SetVisible(panelVictory,  state == GameState.LevelComplete || state == GameState.Summary);
            // Hide wave launch controls whenever play is over
            bool playActive = state == GameState.WaveActive || state == GameState.WaveBreak || state == GameState.Lobby;
            if (!playActive)
            {
                if (waveLaunchBtn != null)  SetVisible(waveLaunchBtn,  false);
                if (waveLaunchPill != null) SetVisible(waveLaunchPill, false);
                if (_wavePreviewPanel != null) _wavePreviewPanel.AddToClassList("hidden");
            }
        }

        // Castle regen icon — fires when HP increases (Regen / GrantBonusHP)
        private void OnCastleHPChanged(int hp, int hpMax)
        {
            if (_lastKnownCastleHP >= 0 && hp > _lastKnownCastleHP && _castleRegenIcon != null)
            {
                if (_regenIconCoroutine != null) StopCoroutine(_regenIconCoroutine);
                _regenIconCoroutine = StartCoroutine(FlashRegenIcon());
            }
            _lastKnownCastleHP = hp;
        }

        private System.Collections.IEnumerator FlashRegenIcon()
        {
            if (_castleRegenIcon == null) yield break;
            _castleRegenIcon.RemoveFromClassList("hidden");
            _castleRegenIcon.AddToClassList("castle-regen-pulse");
            yield return new WaitForSecondsRealtime(0.2f);
            _castleRegenIcon.AddToClassList("hidden");
            _castleRegenIcon.RemoveFromClassList("castle-regen-pulse");
            _regenIconCoroutine = null;
        }

        private void ApplyResponsiveClass()
        {
            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null) return;
            var root = uiDoc.rootVisualElement;
            if (root == null) return;
            root.RemoveFromClassList("hud-mobile");
            root.RemoveFromClassList("hud-tablet");
            root.RemoveFromClassList("hud-desktop");
            if (Screen.width < 720) root.AddToClassList("hud-mobile");
            else if (Screen.width < 1280) root.AddToClassList("hud-tablet");
            else root.AddToClassList("hud-desktop");
        }

        private static void ApplyDeviceClasses(VisualElement root)
        {
            var hudRoot = root.Q<VisualElement>("hud-root");
            if (hudRoot == null) return;
            if (Device.IsMobile || Device.IsTouch)
                hudRoot.AddToClassList("mobile");
            else
                hudRoot.AddToClassList("desktop");
            if (Device.IsPortrait)
                hudRoot.AddToClassList("portrait");
        }

        private static void SetVisible(VisualElement el, bool visible)
        {
            if (visible) el.RemoveFromClassList("hidden");
            else el.AddToClassList("hidden");
        }

        // ── Boss HP bar (minimal: name + fill, no phase markers) ──────────────

        private void BuildBossHpBar(VisualElement root)
        {
            // Outer container — top-center, column layout
            _bossHpRoot = new VisualElement { name = "boss-hp-root" };
            _bossHpRoot.style.position         = Position.Absolute;
            _bossHpRoot.style.top              = new Length(80f, LengthUnit.Pixel);
            _bossHpRoot.style.left             = new Length(0f,  LengthUnit.Pixel);
            _bossHpRoot.style.right            = new Length(0f,  LengthUnit.Pixel);
            _bossHpRoot.style.flexDirection    = FlexDirection.Column;
            _bossHpRoot.style.alignItems       = Align.Center;
            _bossHpRoot.style.width            = new Length(100f, LengthUnit.Percent);
            _bossHpRoot.AddToClassList("hidden");

            // Boss name — gold, 20 px, bold
            _bossNameLabel = new Label { name = "boss-name-label", text = "" };
            _bossNameLabel.style.color         = new StyleColor(new Color(1f, 0.84f, 0f));
            _bossNameLabel.style.fontSize      = new Length(20f, LengthUnit.Pixel);
            _bossNameLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            _bossNameLabel.style.unityTextAlign = TextAnchor.UpperCenter;
            _bossNameLabel.style.marginBottom  = new Length(4f, LengthUnit.Pixel);
            _bossHpRoot.Add(_bossNameLabel);

            // Track wrapper — 400 px × 30 px
            var trackWrapper = new VisualElement { name = "boss-hp-track-wrapper" };
            trackWrapper.style.width  = new Length(400f, LengthUnit.Pixel);
            trackWrapper.style.height = new Length(30f,  LengthUnit.Pixel);
            _bossHpRoot.Add(trackWrapper);

            var track = new VisualElement { name = "boss-hp-track" };
            track.style.position        = Position.Absolute;
            track.style.left            = 0f;
            track.style.top             = 0f;
            track.style.right           = 0f;
            track.style.bottom          = 0f;
            track.style.backgroundColor = new StyleColor(new Color(0.05f, 0.05f, 0.05f, 0.92f));
            track.style.borderTopWidth  = track.style.borderBottomWidth =
            track.style.borderLeftWidth = track.style.borderRightWidth = 2f;
            track.style.borderTopColor  = track.style.borderBottomColor =
            track.style.borderLeftColor = track.style.borderRightColor = new StyleColor(new Color(0.85f, 0.65f, 0.1f));
            track.style.borderTopLeftRadius    = track.style.borderTopRightRadius    =
            track.style.borderBottomLeftRadius = track.style.borderBottomRightRadius = new Length(4f, LengthUnit.Pixel);
            track.style.overflow        = Overflow.Hidden;
            trackWrapper.Add(track);

            // Red fill
            _bossHpFill = new VisualElement { name = "boss-hp-fill" };
            _bossHpFill.style.position        = Position.Absolute;
            _bossHpFill.style.left            = 0f;
            _bossHpFill.style.top             = 0f;
            _bossHpFill.style.bottom          = 0f;
            _bossHpFill.style.width           = new Length(100f, LengthUnit.Percent);
            _bossHpFill.style.backgroundColor = new StyleColor(new Color(0.78f, 0.08f, 0.06f));
            track.Add(_bossHpFill);

            // HP percentage label — centered on track
            _bossHpPctLabel = new Label { name = "boss-hp-pct", text = "100%" };
            _bossHpPctLabel.style.position          = Position.Absolute;
            _bossHpPctLabel.style.left              = new Length(50f, LengthUnit.Percent);
            _bossHpPctLabel.style.top               = new Length(50f, LengthUnit.Percent);
            _bossHpPctLabel.style.translate         = new Translate(new Length(-50f, LengthUnit.Percent), new Length(-50f, LengthUnit.Percent));
            _bossHpPctLabel.style.color             = new StyleColor(Color.white);
            _bossHpPctLabel.style.fontSize          = new Length(13f, LengthUnit.Pixel);
            _bossHpPctLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            track.Add(_bossHpPctLabel);

            root.Add(_bossHpRoot);
        }

        private void HandleEnemySpawned(EnemySpawnedEvent evt)
        {
            if (evt.Enemy == null) return;
            if (evt.Enemy.Config == null || !evt.Enemy.Config.IsBoss) return;
            _trackedBoss = evt.Enemy;
            if (_bossNameLabel != null)
                _bossNameLabel.text = (evt.Enemy.Config.DisplayName ?? evt.Enemy.Config.Id ?? "BOSS").ToUpper();
            if (_bossHpRoot != null)
                SetVisible(_bossHpRoot, true);
            ShowBossIntroBanner(evt.Enemy.Config.Id);
        }

        private void BuildBossIntroBanner(VisualElement root)
        {
            _bossIntroBanner = new VisualElement { name = "boss-intro-banner" };
            _bossIntroBanner.style.position       = Position.Absolute;
            _bossIntroBanner.style.bottom         = new Length(15f, LengthUnit.Percent);
            _bossIntroBanner.style.left           = new Length(50f, LengthUnit.Percent);
            _bossIntroBanner.style.translate      = new Translate(new Length(-50f, LengthUnit.Percent), Length.Auto());
            _bossIntroBanner.style.paddingTop     = new Length(14f, LengthUnit.Pixel);
            _bossIntroBanner.style.paddingBottom  = new Length(14f, LengthUnit.Pixel);
            _bossIntroBanner.style.paddingLeft    = new Length(32f, LengthUnit.Pixel);
            _bossIntroBanner.style.paddingRight   = new Length(32f, LengthUnit.Pixel);
            _bossIntroBanner.style.backgroundColor = new StyleColor(new Color(0.04f, 0.02f, 0.02f, 0.88f));
            _bossIntroBanner.style.borderTopWidth  = _bossIntroBanner.style.borderBottomWidth =
            _bossIntroBanner.style.borderLeftWidth = _bossIntroBanner.style.borderRightWidth  = 2f;
            _bossIntroBanner.style.borderTopColor  = _bossIntroBanner.style.borderBottomColor =
            _bossIntroBanner.style.borderLeftColor = _bossIntroBanner.style.borderRightColor  = new StyleColor(new Color(0.85f, 0.1f, 0.1f));
            _bossIntroBanner.style.borderTopLeftRadius   = _bossIntroBanner.style.borderTopRightRadius =
            _bossIntroBanner.style.borderBottomLeftRadius = _bossIntroBanner.style.borderBottomRightRadius = new Length(6f, LengthUnit.Pixel);
            _bossIntroBanner.style.display        = DisplayStyle.None;
            _bossIntroBanner.style.alignItems     = Align.Center;

            _bossIntroQuote = new Label { name = "boss-intro-quote", text = "" };
            _bossIntroQuote.style.color                     = new StyleColor(new Color(1f, 0.84f, 0f));
            _bossIntroQuote.style.fontSize                  = new Length(22f, LengthUnit.Pixel);
            _bossIntroQuote.style.unityFontStyleAndWeight   = UnityEngine.FontStyle.BoldAndItalic;
            _bossIntroQuote.style.unityTextAlign            = TextAnchor.MiddleCenter;
            _bossIntroQuote.style.textShadow                = new TextShadow
            {
                color      = new Color(0.6f, 0f, 0f, 0.9f),
                offset     = new Vector2(2f, 2f),
                blurRadius = 6f,
            };
            _bossIntroBanner.Add(_bossIntroQuote);
            root.Add(_bossIntroBanner);
        }

        private void ShowBossIntroBanner(string? bossId)
        {
            if (_bossIntroBanner == null || _bossIntroQuote == null) return;

            string[] pool = DefaultBossQuotes;
            if (bossId != null)
            {
                foreach (var kv in BossQuotes)
                {
                    if (bossId.Contains(kv.Key))
                    {
                        pool = kv.Value;
                        break;
                    }
                }
            }
            _bossIntroQuote.text = pool[Random.Range(0, pool.Length)];

            _bossIntroBanner.style.display = DisplayStyle.Flex;
            _bossIntroBanner.style.opacity = 1f;

            if (_bossIntroCoroutine != null) StopCoroutine(_bossIntroCoroutine);
            _bossIntroCoroutine = StartCoroutine(FadeBossIntroBanner());
        }

        private System.Collections.IEnumerator FadeBossIntroBanner()
        {
            yield return new WaitForSecondsRealtime(3f);
            if (_bossIntroBanner == null) yield break;
            // 1s fade-out
            _bossIntroBanner.style.transitionProperty = new StyleList<StylePropertyName>(
                new System.Collections.Generic.List<StylePropertyName> { new StylePropertyName("opacity") });
            _bossIntroBanner.style.transitionDuration = new StyleList<TimeValue>(
                new System.Collections.Generic.List<TimeValue> { new TimeValue(1f, TimeUnit.Second) });
            _bossIntroBanner.style.transitionTimingFunction = new StyleList<EasingFunction>(
                new System.Collections.Generic.List<EasingFunction> { new EasingFunction(EasingMode.EaseIn) });
            _bossIntroBanner.style.opacity = 0f;
            yield return new WaitForSecondsRealtime(1f);
            if (_bossIntroBanner != null) _bossIntroBanner.style.display = DisplayStyle.None;
            _bossIntroCoroutine = null;
        }

        private void HandleEnemyDeath(Enemy enemy, bool isBoss)
        {
            if (!isBoss || enemy != _trackedBoss) return;
            _trackedBoss = null;
            if (_bossHpRoot != null) SetVisible(_bossHpRoot, false);
        }

        private void TickBossHpBar()
        {
            if (_trackedBoss == null || _bossHpFill == null || _bossHpPctLabel == null) return;
            if (_trackedBoss.IsDead)
            {
                _trackedBoss = null;
                if (_bossHpRoot != null) SetVisible(_bossHpRoot, false);
                return;
            }

            float ratio = _trackedBoss.HpRatio;
            _bossHpFill.style.width = new Length(ratio * 100f, LengthUnit.Percent);
            _bossHpPctLabel.text = $"{Mathf.RoundToInt(ratio * 100f)}%";
        }

        private void BuildEnemyCountLabel(VisualElement root)
        {
            _enemyCountLabel = new Label { name = "wave-enemy-count", text = "" };
            _enemyCountLabel.style.position    = Position.Absolute;
            _enemyCountLabel.style.top         = new Length(8f,  LengthUnit.Pixel);
            _enemyCountLabel.style.left        = new Length(8f,  LengthUnit.Pixel);
            _enemyCountLabel.style.fontSize    = new Length(13f, LengthUnit.Pixel);
            _enemyCountLabel.style.color       = new StyleColor(new Color(0.95f, 0.95f, 0.95f));
            _enemyCountLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            _enemyCountLabel.style.textShadow  = new TextShadow
            {
                color      = new Color(0f, 0f, 0f, 0.75f),
                offset     = new Vector2(1f, 1f),
                blurRadius = 3f,
            };
            _enemyCountLabel.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.45f));
            _enemyCountLabel.style.paddingTop    = new Length(3f, LengthUnit.Pixel);
            _enemyCountLabel.style.paddingBottom = new Length(3f, LengthUnit.Pixel);
            _enemyCountLabel.style.paddingLeft   = new Length(7f, LengthUnit.Pixel);
            _enemyCountLabel.style.paddingRight  = new Length(7f, LengthUnit.Pixel);
            _enemyCountLabel.style.borderTopLeftRadius    = new Length(4f, LengthUnit.Pixel);
            _enemyCountLabel.style.borderTopRightRadius   = new Length(4f, LengthUnit.Pixel);
            _enemyCountLabel.style.borderBottomLeftRadius = new Length(4f, LengthUnit.Pixel);
            _enemyCountLabel.style.borderBottomRightRadius = new Length(4f, LengthUnit.Pixel);
            _enemyCountLabel.AddToClassList("hidden");
            root.Add(_enemyCountLabel);
        }

        private void EnsureSibling<T>() where T : Component
        {
            if (gameObject.GetComponent<T>() == null)
                gameObject.AddComponent<T>();
        }

        private void ShowRestartConfirm()
        {
            _confirmRestartPanel?.RemoveFromClassList("hidden");
        }

        private void HideRestartConfirm()
        {
            _confirmRestartPanel?.AddToClassList("hidden");
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static void GoToMenu()
        {
            Time.timeScale = 1f;
            Systems.LevelLoader.GoToMenu();
        }

        // ── Wave preview (enemy roster during break) ────────────────────────

        private void BindWavePreview(VisualElement root)
        {
            _wavePreviewPanel  = root.Q<VisualElement>("wave-preview");
            _wavePreviewRoster = root.Q<VisualElement>("wave-preview-roster");
            var title = root.Q<Label>("wave-preview-title");
            if (title != null) title.text = "Prochaine vague";
        }

        private void RefreshWavePreviewRoster()
        {
            if (_wavePreviewPanel == null || _wavePreviewRoster == null) return;

            var wm = WaveManager.Instance;
            if (wm == null || !wm.IsWaitingForPlayerStart)
            {
                _wavePreviewPanel.AddToClassList("hidden");
                return;
            }

            var waveDef = wm.GetNextWaveDef();
            if (waveDef == null || waveDef.Value.entries == null || waveDef.Value.entries.Count == 0)
            {
                _wavePreviewPanel.AddToClassList("hidden");
                return;
            }

            _wavePreviewRoster.Clear();

            foreach (var entry in waveDef.Value.entries)
            {
                if (entry.type == null) continue;
                var chip = BuildChip(entry.type, entry.count);
                _wavePreviewRoster.Add(chip);
            }

            _wavePreviewPanel.RemoveFromClassList("hidden");
        }

        private VisualElement BuildChip(EnemyType et, int count)
        {
            var card = new VisualElement();
            card.AddToClassList("wave-preview-chip");

            var icon = new Label { text = et.IconEmoji };
            icon.AddToClassList("wave-preview-chip-icon");

            var nameLabel = new Label { text = et.DisplayName };
            nameLabel.AddToClassList("wave-preview-chip-name");

            var countLabel = new Label { text = $"x{count}" };
            countLabel.AddToClassList("wave-preview-chip-count");

            card.Add(icon);
            card.Add(nameLabel);
            card.Add(countLabel);

            return card;
        }
    }
}
