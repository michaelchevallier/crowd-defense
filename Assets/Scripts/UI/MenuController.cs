#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
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
        private Button? _btnHardcore;

        private VisualElement? _root;
        private VisualElement? _menuButtons;
        private VisualElement? _splashOverlay;
        private VisualElement? _showcaseRow;
        private bool _splashDone;

        // ── Play button hover particles ──────────────────────────────────────
        private const int ParticlePoolSize = 6;
        private static readonly Color ParticleColor = new Color(1f, 0.85f, 0.2f, 0.8f);
        private readonly VisualElement[] _particlePool = new VisualElement[ParticlePoolSize];
        private readonly bool[] _particleInUse = new bool[ParticlePoolSize];
        private VisualElement? _particleContainer;
        private Coroutine? _hoverParticlesCo;

        // ── Background gradient ──────────────────────────────────────────────
        private static readonly Color[] GradientColors =
        {
            new Color(0.15f, 0.20f, 0.35f), // deep blue night
            new Color(0.30f, 0.15f, 0.30f), // purple twilight
            new Color(0.35f, 0.18f, 0.10f), // warm sunset
            new Color(0.10f, 0.25f, 0.20f), // forest dawn
        };
        private Color _seasonalTint;

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
            _btnHardcore = _root.Q<Button>("btn-hardcore");

            if (_btnContinue != null) _btnContinue.clicked += OnContinue;
            if (_btnNewRun   != null) _btnNewRun.clicked   += OnNewRun;
            if (_btnSettings != null) _btnSettings.clicked += OnSettings;
            if (_btnCredits  != null) _btnCredits.clicked  += OnCredits;
            if (_btnQuit     != null) _btnQuit.clicked     += OnQuit;
            if (_btnTalents  != null) _btnTalents.clicked  += OnTalents;
            if (_btnDaily    != null) _btnDaily.clicked    += OnDaily;
            if (_btnHardcore != null) _btnHardcore.clicked += OnHardcore;

            InitHoverParticles();
            if (_btnNewRun != null)
            {
                _btnNewRun.RegisterCallback<MouseEnterEvent>(OnPlayButtonHoverEnter);
                _btnNewRun.RegisterCallback<MouseLeaveEvent>(OnPlayButtonHoverExit);
                _btnNewRun.style.transformOrigin = new StyleTransformOrigin(
                    new TransformOrigin(Length.Percent(50), Length.Percent(50)));
                _btnNewRun.style.transitionProperty = new StyleList<StylePropertyName>(
                    new List<StylePropertyName> { new StylePropertyName("scale") });
                _btnNewRun.style.transitionDuration = new StyleList<TimeValue>(
                    new List<TimeValue> { new TimeValue(150, TimeUnit.Millisecond) });
                _btnNewRun.style.transitionTimingFunction = new StyleList<EasingFunction>(
                    new List<EasingFunction> { new EasingFunction(EasingMode.EaseOut) });
            }

            ApplySeasonalTint();
            StartCoroutine(AnimateBackgroundGradient());

            RefreshHardcoreButton();

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
            FinishSplash(restartGradient: true);
        }

        private void FinishSplash(bool restartGradient = false)
        {
            _splashDone = true;
            if (_splashOverlay != null)
            {
                _splashOverlay.RemoveFromHierarchy();
                _splashOverlay = null;
            }
            if (_menuButtons != null && _menuButtons != _root)
                _menuButtons.style.display = DisplayStyle.Flex;
            // Only restart if StopAllCoroutines was called (skip path)
            if (restartGradient)
                StartCoroutine(AnimateBackgroundGradient());
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

        // ── Background gradient ──────────────────────────────────────────────

        private IEnumerator AnimateBackgroundGradient()
        {
            const float stepDuration = 7.5f; // 4 steps × 7.5s = 30s cycle
            int index = 0;
            float elapsed = 0f;

            while (gameObject.activeInHierarchy)
            {
                Color from = GradientColors[index % GradientColors.Length];
                Color to   = GradientColors[(index + 1) % GradientColors.Length];

                elapsed += Time.unscaledDeltaTime;
                float tRaw = Mathf.Clamp01(elapsed / stepDuration);
                float t    = tRaw * tRaw * (3f - 2f * tRaw); // smoothstep

                Color animated = Color.Lerp(from, to, t);
                Color final    = animated * 0.6f + new Color(
                    _seasonalTint.r, _seasonalTint.g, _seasonalTint.b, 1f) * 0.4f;
                final.a = 1f;

                if (_root != null)
                    _root.style.backgroundColor = new StyleColor(final);

                if (elapsed >= stepDuration)
                {
                    elapsed = 0f;
                    index   = (index + 1) % GradientColors.Length;
                }

                yield return null;
            }
        }

        // ── Seasonal tint ────────────────────────────────────────────────────

        private void ApplySeasonalTint()
        {
            if (_root == null) return;

            int month = DateTime.Now.Month;
            Color tint = month switch
            {
                12 or 1 or 2 => new Color(0.55f, 0.72f, 0.90f, 0.18f), // winter — cool blue
                3 or 4 or 5  => new Color(0.45f, 0.78f, 0.40f, 0.15f), // spring — soft green
                6 or 7 or 8  => new Color(0.95f, 0.70f, 0.22f, 0.15f), // summer — warm orange/yellow
                _            => new Color(0.80f, 0.48f, 0.15f, 0.16f)  // autumn  — orange/brown
            };
            _seasonalTint = tint;

            var overlay = new VisualElement
            {
                name = "seasonal-tint-overlay",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position        = Position.Absolute,
                    left            = 0, right = 0, top = 0, bottom = 0,
                    backgroundColor = new StyleColor(tint)
                }
            };

            _root.Insert(0, overlay);
        }

        // ── Play button hover particles ──────────────────────────────────────

        private void InitHoverParticles()
        {
            if (_root == null) return;

            _particleContainer = new VisualElement
            {
                name        = "hover-particle-container",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = 0, right = 0, top = 0, bottom = 0,
                    overflow = Overflow.Hidden
                }
            };
            _root.Add(_particleContainer);

            for (int i = 0; i < ParticlePoolSize; i++)
            {
                var p = new VisualElement
                {
                    name        = $"hover-particle-{i}",
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        position        = Position.Absolute,
                        width           = 8,
                        height          = 8,
                        borderTopLeftRadius     = 4,
                        borderTopRightRadius    = 4,
                        borderBottomLeftRadius  = 4,
                        borderBottomRightRadius = 4,
                        backgroundColor = new StyleColor(ParticleColor),
                        display         = DisplayStyle.None
                    }
                };
                _particleContainer.Add(p);
                _particlePool[i]   = p;
                _particleInUse[i]  = false;
            }
        }

        private void OnPlayButtonHoverEnter(MouseEnterEvent evt)
        {
            if (_btnNewRun != null)
                _btnNewRun.style.scale = new StyleScale(new Scale(new Vector3(1.05f, 1.05f, 1f)));

            AudioController.Instance?.PlayPitched("menu_hover_chime", 0.3f, 1.1f);

            if (_hoverParticlesCo != null) StopCoroutine(_hoverParticlesCo);
            _hoverParticlesCo = StartCoroutine(EmitHoverParticles());
        }

        private void OnPlayButtonHoverExit(MouseLeaveEvent evt)
        {
            if (_btnNewRun != null)
                _btnNewRun.style.scale = new StyleScale(new Scale(Vector3.one));

            if (_hoverParticlesCo != null)
            {
                StopCoroutine(_hoverParticlesCo);
                _hoverParticlesCo = null;
            }
            ReturnAllParticles();
        }

        private IEnumerator EmitHoverParticles()
        {
            while (true)
            {
                SpawnOneParticle();
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }

        private void SpawnOneParticle()
        {
            if (_btnNewRun == null || _particleContainer == null) return;

            int slot = -1;
            for (int i = 0; i < ParticlePoolSize; i++)
            {
                if (!_particleInUse[i]) { slot = i; break; }
            }
            if (slot < 0) return;

            _particleInUse[slot] = true;
            var p = _particlePool[slot];

            // Position at bottom of button, random X within button width
            var btnBounds = _btnNewRun.worldBound;
            var containerBounds = _particleContainer.worldBound;
            float relLeft   = btnBounds.xMin - containerBounds.xMin;
            float relBottom = btnBounds.yMax  - containerBounds.yMin;
            float randX     = relLeft + UnityEngine.Random.Range(0f, btnBounds.width) - 4f;

            p.style.left    = randX;
            p.style.top     = relBottom - 8f;
            p.style.opacity = 0.8f;
            p.style.display = DisplayStyle.Flex;

            StartCoroutine(AnimateParticleCo(slot, relBottom - 8f));
        }

        private IEnumerator AnimateParticleCo(int slot, float startTop)
        {
            var p        = _particlePool[slot];
            const float duration = 0.6f;
            const float rise     = 40f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (p == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                p.style.top     = startTop - t * rise;
                p.style.opacity = Mathf.Lerp(0.8f, 0f, t);
                yield return null;
            }

            ReturnParticle(slot);
        }

        private void ReturnParticle(int slot)
        {
            _particlePool[slot].style.display = DisplayStyle.None;
            _particleInUse[slot] = false;
        }

        private void ReturnAllParticles()
        {
            for (int i = 0; i < ParticlePoolSize; i++)
                ReturnParticle(i);
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
                icon.style.transformOrigin = new StyleTransformOrigin(
                    new TransformOrigin(Length.Percent(50), Length.Percent(50)));
                icon.style.transitionProperty  = new StyleList<StylePropertyName>(
                    new System.Collections.Generic.List<StylePropertyName>
                    { new StylePropertyName("scale"), new StylePropertyName("rotate") });
                icon.style.transitionDuration  = new StyleList<TimeValue>(
                    new System.Collections.Generic.List<TimeValue>
                    { new TimeValue(150, TimeUnit.Millisecond), new TimeValue(150, TimeUnit.Millisecond) });
                icon.style.transitionTimingFunction = new StyleList<EasingFunction>(
                    new System.Collections.Generic.List<EasingFunction>
                    { new EasingFunction(EasingMode.EaseOut), new EasingFunction(EasingMode.EaseOut) });
                icon.tooltip = string.IsNullOrEmpty(def.titleKey) ? def.id : L.Get(def.titleKey);

                icon.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    icon.style.scale  = new StyleScale(new Scale(new Vector3(1.2f, 1.2f, 1f)));
                    icon.style.rotate = new StyleRotate(new Rotate(new Angle(15f, AngleUnit.Degree)));
                });
                icon.RegisterCallback<PointerLeaveEvent>(_ =>
                {
                    icon.style.scale  = new StyleScale(new Scale(Vector3.one));
                    icon.style.rotate = new StyleRotate(new Rotate(new Angle(0f, AngleUnit.Degree)));
                });

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

        private void RefreshHardcoreButton()
        {
            if (_btnHardcore == null) return;
            bool unlocked = SaveSystem.IsHardcoreUnlocked();
            _btnHardcore.style.display = unlocked ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void OnHardcore()
        {
            LevelLoader.LoadHardcoreRun();
        }
    }
}
