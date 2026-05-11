#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Drives tutorial panel visibility and content based on TutorialState phases.
    /// Panel is embedded in HUD.uxml — this component is added to the same HUD GameObject.
    /// Event-only bindings: OnTowerPlaced, OnWaveStart, OnWaveCleared.
    /// Phase 2 (upgrade L2) uses Update polling since Tower has no OnUpgraded event.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TutorialOverlayController : MonoBehaviour
    {
        private VisualElement? tutorialRoot;
        private VisualElement? arrow;
        private Label? textLabel;
        private Button? btnNext;
        private Button? btnSkip;

        // Phase 2: upgrade-L2 detection needs polling once phase is reached
        private bool watchingForUpgrade;

        private void Start()
        {
            // Queries into HUD UIDocument (tutorial panel is embedded in HUD.uxml)
            var root = GetComponent<UIDocument>().rootVisualElement;
            tutorialRoot = root.Q<VisualElement>("tutorial-root");
            arrow = root.Q<VisualElement>("tutorial-arrow");
            textLabel = root.Q<Label>("tutorial-text");
            btnNext = root.Q<Button>("tutorial-btn-next");
            btnSkip = root.Q<Button>("tutorial-btn-skip");

            btnNext?.RegisterCallback<ClickEvent>(_ => OnNextClicked());
            btnSkip?.RegisterCallback<ClickEvent>(_ => OnSkipClicked());

            L.OnLocaleChanged += RefreshText;

            // Subscribe to game events for auto-advance
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnTowerPlaced += OnTowerPlaced;

            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStart += OnWaveStart;
                WaveManager.Instance.OnWaveCleared += OnWaveCleared;
            }

            if (TutorialState.Instance != null)
            {
                TutorialState.Instance.OnPhaseChanged += OnPhaseChanged;
                TutorialState.Instance.OnTutorialCompleted += Hide;
                TutorialState.Instance.OnTutorialSkipped += Hide;
            }

            // Show initial state
            SyncToCurrentPhase();
        }

        private void OnDestroy()
        {
            L.OnLocaleChanged -= RefreshText;

            if (PlacementController.Instance != null)
                PlacementController.Instance.OnTowerPlaced -= OnTowerPlaced;

            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStart -= OnWaveStart;
                WaveManager.Instance.OnWaveCleared -= OnWaveCleared;
            }

            if (TutorialState.Instance != null)
            {
                TutorialState.Instance.OnPhaseChanged -= OnPhaseChanged;
                TutorialState.Instance.OnTutorialCompleted -= Hide;
                TutorialState.Instance.OnTutorialSkipped -= Hide;
            }
        }

        private void Update()
        {
            // Phase 2: poll for any placed tower upgraded to L2 (no OnUpgraded event on Tower)
            if (!watchingForUpgrade) return;
            if (TutorialState.Instance == null || !TutorialState.Instance.IsTutorialActive) return;
            if (TutorialState.Instance.CurrentPhase != 2) { watchingForUpgrade = false; return; }

            if (PlacementController.Instance == null) return;
            foreach (var tower in PlacementController.Instance.PlacedTowers)
            {
                if (tower != null && tower.UpgradeLevel >= 2)
                {
                    watchingForUpgrade = false;
                    TutorialState.Instance.AdvancePhase(); // 2 → 3
                    return;
                }
            }
        }

        // ── Event handlers ─────────────────────────────────────────────────────────

        private void OnTowerPlaced(Tower _)
        {
            if (TutorialState.Instance == null || !TutorialState.Instance.IsTutorialActive) return;
            if (TutorialState.Instance.CurrentPhase == 0)
                TutorialState.Instance.AdvancePhase(); // 0 → 1
        }

        private void OnWaveStart(int _)
        {
            if (TutorialState.Instance == null || !TutorialState.Instance.IsTutorialActive) return;
            if (TutorialState.Instance.CurrentPhase == 1)
                TutorialState.Instance.AdvancePhase(); // 1 → 2 (wave launched)
        }

        private void OnWaveCleared(int _)
        {
            if (TutorialState.Instance == null || !TutorialState.Instance.IsTutorialActive) return;
            // Phase 2: start watching for upgrade after first wave cleared
            if (TutorialState.Instance.CurrentPhase == 2)
                watchingForUpgrade = true;

            // Phase 3 → 4: second wave cleared = congrats
            if (TutorialState.Instance.CurrentPhase == 3)
                TutorialState.Instance.AdvancePhase(); // 3 → 4
        }

        private void OnPhaseChanged(int phase)
        {
            SyncToCurrentPhase();
        }

        private void OnNextClicked()
        {
            TutorialState.Instance?.AdvancePhase();
        }

        private void OnSkipClicked()
        {
            TutorialState.Instance?.SkipTutorial();
        }

        // ── Display ────────────────────────────────────────────────────────────────

        private void SyncToCurrentPhase()
        {
            if (TutorialState.Instance == null || !TutorialState.Instance.IsTutorialActive)
            {
                Hide();
                return;
            }

            Show();
            RefreshText();
            PositionArrowForPhase(TutorialState.Instance.CurrentPhase);
        }

        private void RefreshText()
        {
            if (TutorialState.Instance == null) return;
            int phase = TutorialState.Instance.CurrentPhase;

            if (textLabel != null)
                textLabel.text = L.Get($"tutorial.phase{phase}.text");

            if (btnNext != null)
                btnNext.text = L.Get("tutorial.btn_next");

            if (btnSkip != null)
                btnSkip.text = L.Get("tutorial.btn_skip");
        }

        private void Show()
        {
            if (tutorialRoot == null) return;
            tutorialRoot.RemoveFromClassList("hidden");
        }

        private void Hide()
        {
            if (tutorialRoot == null) return;
            tutorialRoot.AddToClassList("hidden");
        }

        /// <summary>
        /// Positions the arrow indicator relative to the HUD element relevant to each phase.
        /// Uses static offsets aligned to the HUD layout — no runtime element querying needed.
        /// </summary>
        private void PositionArrowForPhase(int phase)
        {
            if (arrow == null) return;

            switch (phase)
            {
                case 0:
                    // Phase 0: place tower — point toward game grid center-bottom
                    arrow.style.display = DisplayStyle.Flex;
                    arrow.style.left = new Length(50f, LengthUnit.Percent);
                    arrow.style.top = new Length(60f, LengthUnit.Percent);
                    break;
                case 1:
                    // Phase 1: launch wave button — bottom-center
                    arrow.style.display = DisplayStyle.Flex;
                    arrow.style.left = new Length(50f, LengthUnit.Percent);
                    arrow.style.top = new Length(75f, LengthUnit.Percent);
                    break;
                case 2:
                    // Phase 2: upgrade tower — point toward a placed tower area
                    arrow.style.display = DisplayStyle.Flex;
                    arrow.style.left = new Length(40f, LengthUnit.Percent);
                    arrow.style.top = new Length(55f, LengthUnit.Percent);
                    break;
                case 3:
                    // Phase 3: synergies — point toward adjacent tower zone
                    arrow.style.display = DisplayStyle.Flex;
                    arrow.style.left = new Length(45f, LengthUnit.Percent);
                    arrow.style.top = new Length(55f, LengthUnit.Percent);
                    break;
                default:
                    // Phase 4+: congrats — no arrow
                    arrow.style.display = DisplayStyle.None;
                    break;
            }
        }
    }
}
