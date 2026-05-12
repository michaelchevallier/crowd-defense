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
        private Button? _btnResume;
        private Button? _btnRestart;
        private Button? _btnMenu;

        private void Awake()
        {
            Instance = this;
            var doc = GetComponent<UIDocument>();
            _root = doc.rootVisualElement.Q("pause-root");
            _btnResume  = _root?.Q<Button>("btn-resume");
            _btnRestart = _root?.Q<Button>("btn-restart");
            _btnMenu    = _root?.Q<Button>("btn-menu");

            if (_btnResume  != null) _btnResume.clicked  += OnResumeClicked;
            if (_btnRestart != null) _btnRestart.clicked += OnRestartClicked;
            if (_btnMenu    != null) _btnMenu.clicked    += OnMenuClicked;
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
