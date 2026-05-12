#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // ── Contextual hint keys (shown once per save, non-blocking) ──────────────
    internal static class HintKey
    {
        internal const string RadialMenu   = "hint_radial_menu";
        internal const string SprintFire   = "hint_sprint_fire";
        internal const string SpeedControl = "hint_speed_control";
        internal const string Shop         = "hint_shop";
    }

    [RequireComponent(typeof(UIDocument))]
    public class TutorialOverlayController : MonoBehaviour
    {
        private VisualElement? _root;
        private VisualElement? _arrow;
        private Label?         _textLabel;
        private Button?        _btnNext;
        private Button?        _btnSkip;

        // Hint state
        private Coroutine? _hintAutoDismiss;
        private bool       _hintVisible;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Start()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            _root      = doc.Q<VisualElement>("tutorial-root");
            _arrow     = doc.Q<VisualElement>("tutorial-arrow");
            _textLabel = doc.Q<Label>("tutorial-text");
            _btnNext   = doc.Q<Button>("tutorial-btn-next");
            _btnSkip   = doc.Q<Button>("tutorial-btn-skip");

            _btnNext?.RegisterCallback<ClickEvent>(_ => OnNextClicked());
            _btnSkip?.RegisterCallback<ClickEvent>(_ => OnSkipClicked());

            L.OnLocaleChanged += RefreshText;

            if (TutorialState.Instance != null)
            {
                TutorialState.Instance.OnStepChanged       += OnStepChanged;
                TutorialState.Instance.OnTutorialCompleted += OnTutorialCompleted;
                TutorialState.Instance.OnTutorialSkipped   += Hide;
            }

            // Wire hint triggers
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnTowerPlaced += OnTowerPlacedHint;
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveStart += OnWaveStartHint;

            SyncToCurrentStep();
        }

        private void OnDestroy()
        {
            L.OnLocaleChanged -= RefreshText;

            if (TutorialState.Instance != null)
            {
                TutorialState.Instance.OnStepChanged       -= OnStepChanged;
                TutorialState.Instance.OnTutorialCompleted -= OnTutorialCompleted;
                TutorialState.Instance.OnTutorialSkipped   -= Hide;
            }

            if (PlacementController.Instance != null)
                PlacementController.Instance.OnTowerPlaced -= OnTowerPlacedHint;
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveStart -= OnWaveStartHint;
        }

        // ── Tutorial step events ───────────────────────────────────────────────

        private void OnStepChanged(TutorialStep _) => SyncToCurrentStep();

        private void OnTutorialCompleted()
        {
            StartCoroutine(ShowDoneAndHide());
        }

        private IEnumerator ShowDoneAndHide()
        {
            if (_textLabel != null)
                _textLabel.text = L.Get("tutorial.done.text");
            if (_btnNext != null)
                _btnNext.style.display = DisplayStyle.None;
            if (_btnSkip != null)
                _btnSkip.style.display = DisplayStyle.None;
            if (_arrow != null)
                _arrow.style.display = DisplayStyle.None;
            Show();
            yield return new WaitForSeconds(3f);
            Hide();
        }

        private void OnNextClicked()  => TutorialState.Instance?.AdvanceStep();
        private void OnSkipClicked()  => TutorialState.Instance?.SkipTutorial();

        // ── Hint triggers (wired regardless of tutorial active state) ──────────

        private void OnTowerPlacedHint(Tower _)
        {
            if (SaveSystem.IsHintSeen(HintKey.RadialMenu)) return;
            StartCoroutine(ShowHintDelayed(HintKey.RadialMenu, 5f));
        }

        private void OnWaveStartHint(int wave)
        {
            if (wave == 2) TryShowHint(HintKey.SprintFire);
            if (wave == 3) TryShowHint(HintKey.SpeedControl);
        }

        // Called by external code (e.g. LevelCompletePanel) when level is won
        public void NotifyLevelWon() => TryShowHint(HintKey.Shop);

        private void TryShowHint(string key)
        {
            if (SaveSystem.IsHintSeen(key)) return;
            ShowHint(key);
        }

        private IEnumerator ShowHintDelayed(string key, float delay)
        {
            yield return new WaitForSeconds(delay);
            TryShowHint(key);
        }

        private void ShowHint(string key)
        {
            if (_hintVisible) return;
            SaveSystem.MarkHintSeen(key);

            if (_textLabel != null)
                _textLabel.text = L.Get(HintLocKey(key));
            if (_btnNext != null)
                _btnNext.style.display = DisplayStyle.None;
            if (_btnSkip != null)
                _btnSkip.style.display = DisplayStyle.None;
            if (_arrow != null)
                _arrow.style.display = DisplayStyle.None;

            Show();
            _hintVisible = true;

            if (_hintAutoDismiss != null) StopCoroutine(_hintAutoDismiss);
            _hintAutoDismiss = StartCoroutine(HintAutoDismiss());
        }

        private IEnumerator HintAutoDismiss()
        {
            yield return new WaitForSeconds(7f);
            DismissHint();
        }

        private void DismissHint()
        {
            if (!_hintVisible) return;
            _hintVisible = false;
            if (_hintAutoDismiss != null) { StopCoroutine(_hintAutoDismiss); _hintAutoDismiss = null; }
            // If tutorial is active restore the step display; otherwise hide
            if (TutorialState.Instance != null && TutorialState.Instance.IsActive)
                SyncToCurrentStep();
            else
                Hide();
        }

        // ── Display ────────────────────────────────────────────────────────────

        private void SyncToCurrentStep()
        {
            var state = TutorialState.Instance;
            if (state == null || !state.IsActive)
            {
                if (!_hintVisible) Hide();
                return;
            }

            Show();
            RefreshText();
            UpdateNextButtonVisibility(state.CurrentStepDef);
            PositionArrowForStep(state.CurrentStep);

            if (_btnSkip != null)
                _btnSkip.style.display = DisplayStyle.Flex;
        }

        private void RefreshText()
        {
            var state = TutorialState.Instance;
            if (state == null) return;

            var def = state.CurrentStepDef;
            if (_textLabel != null)
                _textLabel.text = def != null ? L.Get(def.textKey) : "";

            if (_btnNext != null)
                _btnNext.text = L.Get("tutorial.btn_next");

            if (_btnSkip != null)
                _btnSkip.text = L.Get("tutorial.btn_skip");
        }

        private void UpdateNextButtonVisibility(TutorialStepDef? def)
        {
            if (_btnNext == null) return;
            bool show = def != null && def.showNextButton;
            _btnNext.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Show() => _root?.RemoveFromClassList("hidden");
        private void Hide() => _root?.AddToClassList("hidden");

        // Arrow positions are static offsets relative to HUD layout
        private void PositionArrowForStep(TutorialStep step)
        {
            if (_arrow == null) return;

            switch (step)
            {
                case TutorialStep.Step1_PlaceTower:
                    _arrow.style.display = DisplayStyle.Flex;
                    _arrow.style.left    = new Length(50f, LengthUnit.Percent);
                    _arrow.style.top     = new Length(60f, LengthUnit.Percent);
                    break;
                case TutorialStep.Step2_StartWave:
                    _arrow.style.display = DisplayStyle.Flex;
                    _arrow.style.left    = new Length(50f, LengthUnit.Percent);
                    _arrow.style.top     = new Length(75f, LengthUnit.Percent);
                    break;
                case TutorialStep.Step3_KillEnemy:
                    _arrow.style.display = DisplayStyle.Flex;
                    _arrow.style.left    = new Length(55f, LengthUnit.Percent);
                    _arrow.style.top     = new Length(50f, LengthUnit.Percent);
                    break;
                case TutorialStep.Step4_PlaceHero:
                    _arrow.style.display = DisplayStyle.Flex;
                    _arrow.style.left    = new Length(40f, LengthUnit.Percent);
                    _arrow.style.top     = new Length(55f, LengthUnit.Percent);
                    break;
                case TutorialStep.Step5_CollectCoins:
                    _arrow.style.display = DisplayStyle.Flex;
                    _arrow.style.left    = new Length(5f,  LengthUnit.Percent);
                    _arrow.style.top     = new Length(10f, LengthUnit.Percent);
                    break;
                case TutorialStep.Step6_ChoosePerk:
                    _arrow.style.display = DisplayStyle.Flex;
                    _arrow.style.left    = new Length(50f, LengthUnit.Percent);
                    _arrow.style.top     = new Length(40f, LengthUnit.Percent);
                    break;
                default:
                    _arrow.style.display = DisplayStyle.None;
                    break;
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static string HintLocKey(string key) => key switch
        {
            HintKey.RadialMenu   => "tutorial.hint.radial_menu",
            HintKey.SprintFire   => "tutorial.hint.sprint_fire",
            HintKey.SpeedControl => "tutorial.hint.speed_control",
            HintKey.Shop         => "tutorial.hint.shop",
            _                    => "",
        };
    }
}
