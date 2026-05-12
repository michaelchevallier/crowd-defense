#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController? Instance { get; private set; }
        public bool IsMenuOpen { get; private set; }

        private VisualElement? _root;
        private Label?         _logoTitle;
        private Button?        _btnResume;
        private Button?        _btnRestart;
        private Button?        _btnMenu;

        // pre-cached to avoid per-frame struct allocation
        private static readonly Color GoldBase = new Color(1f, 0.85f, 0.2f, 1f);

        private void Awake()
        {
            Instance = this;
            var doc = GetComponent<UIDocument>();
            _root       = doc.rootVisualElement.Q("pause-root");
            _logoTitle  = _root?.Q<Label>("logo-title");
            _btnResume  = _root?.Q<Button>("btn-resume");
            _btnRestart = _root?.Q<Button>("btn-restart");
            _btnMenu    = _root?.Q<Button>("btn-menu");

            if (_btnResume  != null) _btnResume.clicked  += OnResumeClicked;
            if (_btnRestart != null) _btnRestart.clicked += OnRestartClicked;
            if (_btnMenu    != null) _btnMenu.clicked    += OnMenuClicked;
        }

        private void Update()
        {
            if (!IsMenuOpen || _logoTitle == null) return;

            float t = Time.unscaledTime;

            // subtle sway ±2° at 1.5 Hz
            float sway = Mathf.Sin(t * 1.5f) * 2f;
            _logoTitle.style.rotate = new Rotate(new Angle(sway, AngleUnit.Degree));

            // scale breathing 1.0 ± 0.02 at 1.2 Hz
            float sc = 1f + Mathf.Sin(t * 1.2f) * 0.02f;
            _logoTitle.style.scale = new StyleScale(new Vector3(sc, sc, 1f));

            // glow pulse: lerp gold → white by [0..0.2] at 2 Hz
            float pulse = (Mathf.Sin(t * 2f) * 0.5f + 0.5f) * 0.2f;
            Color col = Color.Lerp(GoldBase, Color.white, pulse);
            _logoTitle.style.color = new StyleColor(col);
        }

        private void OnEnable()
        {
            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnPauseChanged += Sync;
        }

        private void OnDisable()
        {
            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnPauseChanged -= Sync;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Sync()
        {
            bool paused = LevelRunner.Instance?.IsPaused ?? false;
            if (_root == null) return;
            if (paused) _root.RemoveFromClassList("hidden");
            else        _root.AddToClassList("hidden");
            IsMenuOpen = paused;
        }

        private void OnResumeClicked()  => LevelRunner.Instance?.Resume();
        private void OnRestartClicked() => LevelRunner.Instance?.RestartLevel();
        private void OnMenuClicked()
        {
            // Restore timeScale before scene switch so it doesn't carry over.
            Time.timeScale = 1f;
            LevelLoader.GoToMenu();
        }
    }
}
