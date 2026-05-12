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
        private VisualElement? _pauseOverlay;
        private Label?         _logoTitle;
        private Button?        _btnResume;
        private Button?        _btnRestart;
        private Button?        _btnMenu;

        // Overlay fade constants
        private const float OverlayTargetAlpha = 0.6f;
        private const float FadeInDuration     = 0.20f;
        private const float FadeOutDuration    = 0.15f;

        // pre-cached to avoid per-frame struct allocation
        private static readonly Color GoldBase = new Color(1f, 0.85f, 0.2f, 1f);

        // Tracks ongoing overlay animation to allow early cancellation
        private IVisualElementScheduledItem? _overlayAnim;
        private float _animStartAlpha;
        private float _animTargetAlpha;
        private float _animStartTime;
        private float _animDuration;

        private void Awake()
        {
            Instance = this;
            var doc = GetComponent<UIDocument>();
            if (doc == null)
            {
                Debug.LogError("[PauseMenuController] UIDocument component not found");
                return;
            }
            var rootElem = doc.rootVisualElement;
            if (rootElem == null)
            {
                Debug.LogError("[PauseMenuController] rootVisualElement is null");
                return;
            }
            _root         = rootElem.Q("pause-root");
            _pauseOverlay = _root?.Q("pause-overlay");
            _logoTitle    = _root?.Q<Label>("logo-title");
            _btnResume    = _root?.Q<Button>("btn-resume");
            _btnRestart   = _root?.Q<Button>("btn-restart");
            _btnMenu      = _root?.Q<Button>("btn-menu");

            if (_pauseOverlay != null)
                _pauseOverlay.pickingMode = PickingMode.Position;

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
            if (paused)
            {
                _root.RemoveFromClassList("hidden");
                AudioController.Instance?.PlayPitched("menu_open_woosh", volMul: 0.4f, pitch: 1.0f);
                FadeOverlay(OverlayTargetAlpha, FadeInDuration);
            }
            else
            {
                FadeOverlay(0f, FadeOutDuration, onComplete: () => _root.AddToClassList("hidden"));
            }
            IsMenuOpen = paused;
        }

        private void FadeOverlay(float targetAlpha, float duration, System.Action? onComplete = null)
        {
            if (_pauseOverlay == null) return;

            _overlayAnim?.Pause();

            // resolvedStyle may not be ready before first layout; fallback to inline style value
            float startAlpha = float.IsNaN(_pauseOverlay.resolvedStyle.opacity)
                ? _pauseOverlay.style.opacity.value
                : _pauseOverlay.resolvedStyle.opacity;
            _animStartAlpha  = startAlpha;
            _animTargetAlpha = targetAlpha;
            _animStartTime   = Time.unscaledTime;
            _animDuration    = duration;

            _overlayAnim = _pauseOverlay.schedule
                .Execute(() => StepOverlayFade(onComplete))
                .Every(0)
                .Until(() => OverlayFadeDone(onComplete));
        }

        private bool OverlayFadeDone(System.Action? onComplete)
        {
            if (_pauseOverlay == null) return true;
            float elapsed = Time.unscaledTime - _animStartTime;
            if (elapsed < _animDuration) return false;
            _pauseOverlay.style.opacity = _animTargetAlpha;
            onComplete?.Invoke();
            _overlayAnim = null;
            return true;
        }

        private void StepOverlayFade(System.Action? onComplete)
        {
            if (_pauseOverlay == null) return;
            float elapsed = Time.unscaledTime - _animStartTime;
            float t       = _animDuration > 0f ? Mathf.Clamp01(elapsed / _animDuration) : 1f;
            // ease-out quad for both directions
            float tEased  = 1f - (1f - t) * (1f - t);
            _pauseOverlay.style.opacity = Mathf.Lerp(_animStartAlpha, _animTargetAlpha, tEased);
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
