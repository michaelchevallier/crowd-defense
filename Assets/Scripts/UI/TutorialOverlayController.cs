#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class TutorialOverlayController : MonoBehaviour
    {
        private VisualElement? _root;
        private VisualElement? _arrow;
        private Label?         _textLabel;
        private Button?        _btnNext;
        private Button?        _btnSkip;

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
                TutorialState.Instance.OnTutorialCompleted += Hide;
                TutorialState.Instance.OnTutorialSkipped   += Hide;
            }

            SyncToCurrentStep();
        }

        private void OnDestroy()
        {
            L.OnLocaleChanged -= RefreshText;

            if (TutorialState.Instance != null)
            {
                TutorialState.Instance.OnStepChanged       -= OnStepChanged;
                TutorialState.Instance.OnTutorialCompleted -= Hide;
                TutorialState.Instance.OnTutorialSkipped   -= Hide;
            }
        }

        // ── Event handlers ─────────────────────────────────────────────────────────

        private void OnStepChanged(TutorialStep _) => SyncToCurrentStep();

        private void OnNextClicked()  => TutorialState.Instance?.AdvanceStep();
        private void OnSkipClicked()  => TutorialState.Instance?.SkipTutorial();

        // ── Display ────────────────────────────────────────────────────────────────

        private void SyncToCurrentStep()
        {
            var state = TutorialState.Instance;
            if (state == null || !state.IsActive)
            {
                Hide();
                return;
            }

            Show();
            RefreshText();
            UpdateNextButtonVisibility(state.CurrentStepDef);
            PositionArrowForStep(state.CurrentStep);
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
            bool show = def == null || def.showNextButton;
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
                default:
                    _arrow.style.display = DisplayStyle.None;
                    break;
            }
        }
    }
}
