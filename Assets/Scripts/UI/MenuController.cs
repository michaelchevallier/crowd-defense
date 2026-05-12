#nullable enable
using System.Collections;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MenuController : MonoBehaviour
    {
        public static MenuController? Instance { get; private set; }

        private Button? _btnContinue;
        private Button? _btnNewRun;
        private Button? _btnSettings;
        private Button? _btnCredits;
        private Button? _btnQuit;
        private Button? _btnTalents;
        private Button? _btnDaily;

        private VisualElement? _root;
        private VisualElement? _menuButtons;
        private VisualElement? _splashOverlay;
        private VisualElement? _showcaseRow;
        private bool _splashDone;

        // ── Demo mode ────────────────────────────────────────────────────────
        private const float IdleTimeoutSeconds = 60f;
        private const string DemoLevelId = "L1-1";
        private float _idleTimer;
        private bool _demoActive;
        private VisualElement? _demoOverlay;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;

            _btnContinue = _root.Q<Button>("btn-continue");
            _btnNewRun   = _root.Q<Button>("btn-newrun");
            _btnSettings = _root.Q<Button>("btn-settings");
            _btnCredits  = _root.Q<Button>("btn-credits");
            _btnQuit     = _root.Q<Button>("btn-quit");
            _btnTalents  = _root.Q<Button>("btn-talents");
            _btnDaily    = _root.Q<Button>("btn-daily");

            if (_btnContinue != null) _btnContinue.clicked += OnContinue;
            if (_btnNewRun   != null) _btnNewRun.clicked   += OnNewRun;
            if (_btnSettings != null) _btnSettings.clicked += OnSettings;
            if (_btnCredits  != null) _btnCredits.clicked  += OnCredits;
            if (_btnQuit     != null) _btnQuit.clicked     += OnQuit;
            if (_btnTalents  != null) _btnTalents.clicked  += OnTalents;
            if (_btnDaily    != null) _btnDaily.clicked    += OnDaily;

            RefreshDailyButton();
            RefreshContinueButton();

            // Collect all direct menu children to hide during splash
            _menuButtons = _root.Q<VisualElement>("menu-buttons") ?? _root;

            BuildAchievementsShowcase();

            StartCoroutine(PlayLogoSplash());
        }

        // ── Logo splash ──────────────────────────────────────────────────────

        private IEnumerator PlayLogoSplash()
        {
            // Build overlay
            _splashOverlay = new VisualElement
            {
                name = "logo-splash-overlay",
                style =
                {
                    position        = Position.Absolute,
                    left            = 0, right = 0, top = 0, bottom = 0,
                    justifyContent  = Justify.Center,
                    alignItems      = Align.Center,
                    backgroundColor = new StyleColor(new Color(0.05f, 0.05f, 0.08f, 0.95f)),
                    opacity         = 1f
                }
            };

            var logo = new Label("Crowd Defense")
            {
                style =
                {
                    fontSize        = 64,
                    color           = new StyleColor(Color.white),
                    unityFontStyleAndWeight = FontStyle.Bold,
                    scale           = new StyleScale(new Scale(Vector3.zero)),
                    transformOrigin = new StyleTransformOrigin(new TransformOrigin(Length.Percent(50), Length.Percent(50)))
                }
            };

            _splashOverlay.Add(logo);
            _root!.Add(_splashOverlay);

            // Hide menu content during splash
            if (_menuButtons != null && _menuButtons != _root)
                _menuButtons.style.display = DisplayStyle.None;

            // Register skip on click/tap
            _splashOverlay.RegisterCallback<ClickEvent>(_ => SkipSplash());

            // Scale 0 → 1 over 0.4 s
            float elapsed = 0f;
            const float scaleIn = 0.4f;
            while (elapsed < scaleIn && !_splashDone)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / scaleIn);
                float s = Mathf.SmoothStep(0f, 1f, t);
                logo.style.scale = new StyleScale(new Scale(new Vector3(s, s, 1f)));
                yield return null;
            }
            logo.style.scale = new StyleScale(new Scale(Vector3.one));

            // Hold 0.6 s
            float holdEnd = Time.time + 0.6f;
            while (Time.time < holdEnd && !_splashDone)
                yield return null;

            // Fade out 0.4 s
            elapsed = 0f;
            const float fadeOut = 0.4f;
            while (elapsed < fadeOut && !_splashDone)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOut);
                _splashOverlay.style.opacity = 1f - t;
                yield return null;
            }

            FinishSplash();
        }

        private void SkipSplash()
        {
            if (_splashDone) return;
            _splashDone = true;
            StopAllCoroutines();
            FinishSplash();
        }

        private void FinishSplash()
        {
            _splashDone = true;
            if (_splashOverlay != null)
            {
                _splashOverlay.RemoveFromHierarchy();
                _splashOverlay = null;
            }
            if (_menuButtons != null && _menuButtons != _root)
                _menuButtons.style.display = DisplayStyle.Flex;
        }

        private void Update()
        {
            if (_demoActive || !_splashDone) return;

            bool anyInput = Input.anyKeyDown
                         || Input.GetMouseButtonDown(0)
                         || Input.GetMouseButtonDown(1)
                         || Input.touchCount > 0;

            if (anyInput)
            {
                _idleTimer = 0f;
                return;
            }

            _idleTimer += Time.unscaledDeltaTime;
            if (_idleTimer >= IdleTimeoutSeconds)
                StartDemoMode();
        }

        private void StartDemoMode()
        {
            _demoActive = true;

            // Build fullscreen overlay "DEMO MODE — click to exit"
            _demoOverlay = new VisualElement
            {
                name  = "demo-mode-overlay",
                style =
                {
                    position        = Position.Absolute,
                    left            = 0, right = 0, top = 0, bottom = 0,
                    justifyContent  = Justify.FlexEnd,
                    alignItems      = Align.Center,
                    backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.45f))
                }
            };

            var label = new Label("DEMO MODE — cliquer pour quitter")
            {
                style =
                {
                    fontSize        = 20,
                    color           = new StyleColor(Color.white),
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom    = 32,
                    unityTextAlign  = TextAnchor.MiddleCenter
                }
            };

            _demoOverlay.Add(label);
            _root?.Add(_demoOverlay);

            _demoOverlay.RegisterCallback<ClickEvent>(_ => CancelDemoMode());

            // Bypass hero/avatar gates: set NextLevelId directly then fade to Main
            LevelLoader.NextLevelId    = DemoLevelId;
            LevelLoader.NextDailySpec  = null;
            LevelLoader.Fade("Main");
        }

        private void CancelDemoMode()
        {
            if (!_demoActive) return;
            _demoActive = false;
            _idleTimer  = 0f;

            if (_demoOverlay != null)
            {
                _demoOverlay.RemoveFromHierarchy();
                _demoOverlay = null;
            }

            LevelLoader.GoToMenu();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Achievement showcase ─────────────────────────────────────────────

        private void BuildAchievementsShowcase()
        {
            if (_root == null) return;

            var ach      = Achievements.Instance;
            var registry = Resources.Load<AchievementRegistry>("AchievementRegistry");

            // Need at least one unlock to display the row.
            if (ach == null || registry == null || ach.UnlockedCount == 0) return;

            var recent = ach.GetRecentUnlocked(5);
            if (recent.Count == 0) return;

            _showcaseRow = new VisualElement { name = "achievements-showcase-row" };
            _showcaseRow.style.flexDirection  = FlexDirection.Row;
            _showcaseRow.style.justifyContent = Justify.Center;
            _showcaseRow.style.alignItems     = Align.Center;
            _showcaseRow.style.marginBottom   = 12;
            _showcaseRow.style.marginTop      = 8;

            // Iterate from most-recent (end of list) to oldest.
            for (int i = recent.Count - 1; i >= 0; i--)
            {
                string achId = recent[i];
                var def = registry.Get(achId);
                if (def == null) continue;

                var icon = new Label(def.IconEmoji);
                icon.AddToClassList("ach-showcase-icon");
                icon.style.fontSize  = 28;
                icon.style.marginLeft  = 6;
                icon.style.marginRight = 6;
                icon.tooltip = string.IsNullOrEmpty(def.titleKey) ? def.id : L.Get(def.titleKey);

                // Click on any icon → open achievements panel.
                icon.RegisterCallback<ClickEvent>(_ => AchievementsPanel.Instance?.Show());
                _showcaseRow.Add(icon);
            }

            // Insert at top of _root (before all other children).
            _root.Insert(0, _showcaseRow);
        }

        private void RefreshContinueButton()
        {
            if (_btnContinue == null) return;
            bool hasSave = SaveSystem.SlotHasData(0) || SaveSystem.SlotHasData(1) || SaveSystem.SlotHasData(2);
            _btnContinue.SetEnabled(hasSave);
            if (!hasSave)
                _btnContinue.AddToClassList("menu-btn-disabled");
            else
                _btnContinue.RemoveFromClassList("menu-btn-disabled");
        }

        private static void OnContinue()
        {
            SaveSlotController.Instance?.Show();
        }

        private static void OnNewRun()
        {
            LevelLoader.GoToWorldMap();
        }

        private static void OnSettings()
        {
            SettingsPanelController.Instance?.Show();
        }

        private static void OnCredits()
        {
            CreditsScreen.Instance?.Show();
        }

        private static void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void OnTalents()
        {
            TalentPanelController.Instance?.Show();
        }

        private void RefreshDailyButton()
        {
            if (_btnDaily == null) return;
            bool done = DailyChallenge.Instance?.HasCompletedToday() ?? false;
            _btnDaily.text = done
                ? $"{L.Get("worldmap.btn_daily")} (fait)"
                : L.Get("worldmap.btn_daily");
            if (done)
                _btnDaily.AddToClassList("menu-btn-done");
            else
                _btnDaily.RemoveFromClassList("menu-btn-done");
        }

        private static void OnDaily()
        {
            DailyChallengeModal.Instance?.Show();
        }
    }
}
