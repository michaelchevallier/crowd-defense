#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class HudController : MonoBehaviour
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

        // Perk badges — sibling component wired after UIDocument root is ready
        private HudPerkBadges? _perkBadges;

        // Sidebar showing all active perks of the current run
        private CurrentRunPerksPanel? _perksPanel;

        // Combo multiplier badge (top-right, persistent while combo active)
        private Label? _comboMultiplierLabel;

        // Keyboard hints footer label
        private Label? keyboardHintsLabel;

        // Wave kill counter label
        private Label? waveKillCounter;

        // Wave elapsed time label
        private Label? waveTimeLabel;
        private float _waveStartTime = -1f;
        private float _lastWaveTickTime = -1f;

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
            BindUiRefs();
            WireCallbacks();
            SubscribeSystems();
        }

        private void BindUiRefs()
        {
            _doctrineCtrl = GetComponent<DoctrineController>();
            _perkBadges = GetComponent<HudPerkBadges>();

            // Auto-add UI sibling controllers that share the HUD UIDocument (each Qs its own
            // elements out of HUD.uxml). Idempotent: only added when not already present.
            EnsureSibling<SettingsPanelController>();
            _settingsCtrl = GetComponent<SettingsPanelController>();
            EnsureSibling<PauseMenuController>();
            EnsureSibling<TowerToolbarController>();
            EnsureSibling<TowerTooltipController>();
            EnsureSibling<SynergyHudController>();
            EnsureSibling<SynergyHudPanel>();
            EnsureSibling<FloatingPopupController>();
            EnsureSibling<RadialMenuController>();
            EnsureSibling<TowerSelectMenuController>();
            EnsureSibling<MuteToggleController>();
            EnsureSibling<HeroSkillBarController>();
            EnsureSibling<MinimapController>();
            EnsureSibling<HelpOverlayController>();
            EnsureSibling<QuickSaveHotkey>();
            EnsureSibling<KeyBindingsPanel>();
            EnsureSibling<CurrentRunPerksPanel>();
            _perksPanel = GetComponent<CurrentRunPerksPanel>();

            var root = GetComponent<UIDocument>().rootVisualElement;
            ApplyDeviceClasses(root);
            goldLabel = root.Q<Label>("gold-label");
            goldValue = root.Q<Label>("gold-value");
            waveLabel = root.Q<Label>("wave-label");
            waveValue = root.Q<Label>("wave-value");
            hpLabel = root.Q<Label>("hp-label");
            hpValue = root.Q<Label>("hp-value");
            hpBarFill = root.Q<VisualElement>("hp-bar-fill");
            panelGameOver = root.Q<VisualElement>("panel-game-over");
            panelGameOverTitle = root.Q<Label>("panel-game-over-title");
            panelGameOverSubtitle = root.Q<Label>("panel-game-over-subtitle");
            panelVictory = root.Q<VisualElement>("panel-victory");
            panelVictoryTitle = root.Q<Label>("panel-victory-title");
            panelVictorySubtitle = root.Q<Label>("panel-victory-subtitle");
            btnRestartGo = root.Q<Button>("btn-restart-go");
            btnRestartVictory = root.Q<Button>("btn-restart-victory");
            btnMenuGo = root.Q<Button>("btn-menu-go");
            btnMenuVictory = root.Q<Button>("btn-menu-victory");

            waveLaunchBtn = root.Q<VisualElement>("wave-launch-btn");
            waveLaunchPill = root.Q<VisualElement>("wave-launch-pill");
            waveLaunchLabel = root.Q<Label>("wave-launch-label");
            waveLaunchSub = root.Q<Label>("wave-launch-sub");
            waveLaunchStreak = root.Q<VisualElement>("wave-launch-streak");
            waveLaunchStreakText = root.Q<Label>("wave-launch-streak-text");
            waveLaunchPillText = root.Q<Label>("wave-launch-pill-text");

            heroPanel = root.Q<VisualElement>("hero-panel");
            heroHpLabel = root.Q<Label>("hero-hp-label");
            heroLevelLabel = root.Q<Label>("hero-level");
            heroXpLabel = root.Q<Label>("hero-xp-label");
            heroXpBarFill = root.Q<VisualElement>("hero-xp-bar-fill");
            heroXpValue = root.Q<Label>("hero-xp-value");
            heroUltLabel = root.Q<Label>("hero-ult-label");
            ultBtn = root.Q<VisualElement>("ult-btn");
            ultRingLeft = root.Q<VisualElement>("ult-ring-left");
            ultRingRight = root.Q<VisualElement>("ult-ring-right");

            keyboardHintsLabel = root.Q<Label>("keyboard-hints-label");
            waveKillCounter = root.Q<Label>("wave-kill-counter");
            waveTimeLabel = root.Q<Label>("wave-time");
            bluePillBtn = root.Q<Button>("bluepill-btn");
            _comboMultiplierLabel = root.Q<Label>("combo-multiplier-label");

            // Force initial values so top-bar is never blank at runtime
            if (goldValue != null) goldValue.text = "0";
            if (waveValue != null) waveValue.text = "—";
            if (hpValue != null) hpValue.text = "—";
        }

        private void WireCallbacks()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            ultBtn?.RegisterCallback<ClickEvent>(_ => TryCastUlt());
            bluePillBtn?.RegisterCallback<ClickEvent>(_ => TryStartBluePill());
            btnRestartGo?.RegisterCallback<ClickEvent>(_ => Restart());
            btnRestartVictory?.RegisterCallback<ClickEvent>(_ => Restart());
            btnMenuGo?.RegisterCallback<ClickEvent>(_ => GoToMenu());
            btnMenuVictory?.RegisterCallback<ClickEvent>(_ => GoToMenu());
            waveLaunchBtn?.RegisterCallback<ClickEvent>(_ => TryLaunchWave());
            root.Q<Button>("btn-doctrine")?.RegisterCallback<ClickEvent>(_ => _doctrineCtrl?.Show());
            root.Q<Button>("btn-settings")?.RegisterCallback<ClickEvent>(_ => _settingsCtrl?.Show());
        }

        private void SubscribeSystems()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            ApplyLocalizedTexts();
            L.OnLocaleChanged += ApplyLocalizedTexts;

            if (Economy.Instance != null)
            {
                Economy.Instance.OnGoldChanged += OnGoldChanged;
                OnGoldChanged(Economy.Instance.Gold);
            }

            if (LevelRunner.Instance != null)
            {
                LevelRunner.Instance.OnTotalHPChanged += OnHPChanged;
                LevelRunner.Instance.OnStateChanged += OnStateChanged;
                OnHPChanged(LevelRunner.Instance.TotalCastleHP, LevelRunner.Instance.TotalCastleHPMax);
                OnStateChanged(LevelRunner.Instance.State);
            }

            if (WaveManager.Instance != null)
            {
                _waveManagerSubscribed = true;
                WaveManager.Instance.OnWaveStart += OnWaveStart;
                WaveManager.Instance.OnBreakStateChanged += OnBreakStateChanged;
                WaveManager.Instance.OnKillCountChanged += OnKillCountChanged;
                OnWaveStart(WaveManager.Instance.CurrentWaveIdx);
                // Sync initial break state (W1 waits for player)
                OnBreakStateChanged();
            }

            EventManager.Instance?.Subscribe<ComboUpdatedEvent>(HandleComboUpdated);
            EventManager.Instance?.Subscribe<ComboResetEvent>(HandleComboReset);

            // Wire perk badges + sidebar once hero is known
            var hero = LevelRunner.Instance?.Hero;
            if (_perkBadges != null && hero != null)
                _perkBadges.Init(root, hero);
            if (_perksPanel != null && hero != null)
                _perksPanel.Init(root, hero);
        }

        private void OnDestroy()
        {
            L.OnLocaleChanged -= ApplyLocalizedTexts;
            if (Economy.Instance != null) Economy.Instance.OnGoldChanged -= OnGoldChanged;
            if (LevelRunner.Instance != null)
            {
                LevelRunner.Instance.OnTotalHPChanged -= OnHPChanged;
                LevelRunner.Instance.OnStateChanged -= OnStateChanged;
            }
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStart -= OnWaveStart;
                WaveManager.Instance.OnBreakStateChanged -= OnBreakStateChanged;
                WaveManager.Instance.OnKillCountChanged -= OnKillCountChanged;
            }
            EventManager.Instance?.Unsubscribe<ComboUpdatedEvent>(HandleComboUpdated);
            EventManager.Instance?.Unsubscribe<ComboResetEvent>(HandleComboReset);
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
            // Resolve WaveManager race condition: if not subscribed in Start(), try again here
            if (!_waveManagerSubscribed && WaveManager.Instance != null)
            {
                _waveManagerSubscribed = true;
                WaveManager.Instance.OnWaveStart += OnWaveStart;
                WaveManager.Instance.OnBreakStateChanged += OnBreakStateChanged;
                WaveManager.Instance.OnKillCountChanged += OnKillCountChanged;
                OnWaveStart(WaveManager.Instance.CurrentWaveIdx);
                OnBreakStateChanged();
            }

            // N hotkey — debounced, shared with click (Q7)
            if (Input.GetKeyDown(KeyCode.N))
                TryLaunchWave();

            // Space — hero ultimate cast
            if (Input.GetKeyDown(KeyCode.Space))
                TryCastUlt();

            TickBreakPill();
            TickWaveTime();
            UpdateHeroPanel();
        }

        // Per-frame smooth countdown on the pill badge and main label during the skip bonus window
        private void TickBreakPill()
        {
            var wm = WaveManager.Instance;
            if (wm == null || !wm.IsWaitingForPlayerStart) return;
            float secondsLeft = wm.SkipWindowSecondsRemaining;
            if (secondsLeft <= 0f) return;

            if (waveLaunchPill != null && waveLaunchPillText != null)
                waveLaunchPillText.text = L.Get("hud.pill_skip_text", secondsLeft, Mathf.RoundToInt(wm.StreakCount * 5));

            if (waveLaunchLabel != null)
                waveLaunchLabel.text = L.Get("hud.wave_launch_countdown", wm.NextWaveDisplayNumber, secondsLeft);
        }

        private void TickWaveTime()
        {
            if (waveTimeLabel == null || _waveStartTime < 0f) return;
            float now = Time.unscaledTime;
            if (now - _lastWaveTickTime < 1f) return;
            _lastWaveTickTime = now;
            int elapsed = Mathf.FloorToInt(now - _waveStartTime);
            int minutes = elapsed / 60;
            int seconds = elapsed % 60;
            waveTimeLabel.text = $"⏱ {minutes}:{seconds:00}";
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
            wm.StartNextWave();

            if (wasInWindow)
            {
                // Flash the wave launch button green
                StartCoroutine(FlashButtonGreen(waveLaunchBtn, 0.35f));

                // Skip bonus popup — use Toast (HUD-space, no 3D position needed)
                var cfg = BalanceConfig.Get();
                Toast.Show(
                    L.Get("hud.skip_toast_title"),
                    L.Get("hud.skip_toast_body"),
                    1800,
                    null,
                    ToastType.Perk
                );

                // Streak toast when streak just incremented
                int newStreak = streakBefore + 1; // WaveManager caps at StreakCap but we show intent
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

        private System.Collections.IEnumerator FlashButtonGreen(VisualElement? btn, float duration)
        {
            if (btn == null) yield break;
            btn.AddToClassList("skip-bonus-flash");
            yield return new WaitForSecondsRealtime(duration);
            btn.RemoveFromClassList("skip-bonus-flash");
        }

        private void OnGoldChanged(int gold)
        {
            if (goldValue != null) goldValue.text = gold.ToString();
        }

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
            }
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

        private void EnsureSibling<T>() where T : Component
        {
            if (gameObject.GetComponent<T>() == null)
                gameObject.AddComponent<T>();
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
    }
}
