#nullable enable
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
        private Button? _btnQuit;
        private Button? _btnTalents;

        private VisualElement? _root;

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
            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null)
            {
                Debug.LogError("[MenuController] UIDocument component not found");
                return;
            }
            _root = uiDoc.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("[MenuController] rootVisualElement is null — Menu UXML failed to load");
                return;
            }

            _btnContinue = _root.Q<Button>("btn-continue");
            _btnNewRun   = _root.Q<Button>("btn-newrun");
            _btnSettings = _root.Q<Button>("btn-settings");
            _btnQuit     = _root.Q<Button>("btn-quit");
            _btnTalents  = _root.Q<Button>("btn-talents");

            if (_btnContinue != null) _btnContinue.clicked += OnContinue;
            if (_btnNewRun   != null) _btnNewRun.clicked   += OnNewRun;
            if (_btnSettings != null) _btnSettings.clicked += OnSettings;
            if (_btnQuit     != null) _btnQuit.clicked     += OnQuit;
            if (_btnTalents  != null) _btnTalents.clicked  += OnTalents;

            RefreshContinueButton();
        }

        private void Update()
        {
            if (_demoActive) return;

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
                    fontSize                = 20,
                    color                   = new StyleColor(Color.white),
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom            = 32,
                    unityTextAlign          = TextAnchor.MiddleCenter
                }
            };

            _demoOverlay.Add(label);
            _root?.Add(_demoOverlay);

            _demoOverlay.RegisterCallback<ClickEvent>(_ => CancelDemoMode());

            LevelLoader.NextLevelId   = DemoLevelId;
            LevelLoader.NextDailySpec = null;
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

        private static void OnContinue() => SaveSlotController.Instance?.Show();

        private static void OnNewRun() => LevelLoader.GoToWorldMap();

        private static void OnSettings() => SettingsPanelController.Instance?.Show();

        private static void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void OnTalents() => TalentPanelController.Instance?.Show();
    }
}
