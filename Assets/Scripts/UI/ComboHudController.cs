#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Attached to the same GameObject as HudController (UIDocument).
    // Listens to ComboUpdatedEvent / ComboResetEvent and drives the combo-display element.
    // Auto-hides after ComboHideDelaySec if no further kill resets the timer.
    [RequireComponent(typeof(UIDocument))]
    public class ComboHudController : MonoBehaviour
    {
        private const float ComboHideDelaySec = 1.2f;

        private VisualElement? _comboDisplay;
        private Label? _comboLabel;
        private float _hideTimer;
        private bool _visible;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _comboDisplay = root.Q<VisualElement>("combo-display");
            _comboLabel = root.Q<Label>("combo-label");

            var em = EventManager.Instance;
            em?.Subscribe<ComboUpdatedEvent>(OnComboUpdated);
            em?.Subscribe<ComboResetEvent>(OnComboReset);
        }

        private void OnDestroy()
        {
            var em = EventManager.Instance;
            em?.Unsubscribe<ComboUpdatedEvent>(OnComboUpdated);
            em?.Unsubscribe<ComboResetEvent>(OnComboReset);
        }

        private void Update()
        {
            if (!_visible) return;
            _hideTimer -= Time.unscaledDeltaTime;
            if (_hideTimer <= 0f)
                Hide();
        }

        private void OnComboUpdated(ComboUpdatedEvent evt)
        {
            if (_comboDisplay == null || _comboLabel == null) return;

            string mulStr = evt.Multiplier.ToString("0.##");
            _comboLabel.text = $"x{mulStr} COMBO!";

            // Swap color-escalation class
            _comboDisplay.RemoveFromClassList("combo-level-2");
            _comboDisplay.RemoveFromClassList("combo-level-3");
            _comboDisplay.RemoveFromClassList("combo-level-4");
            if (evt.Level >= 4) _comboDisplay.AddToClassList("combo-level-4");
            else if (evt.Level == 3) _comboDisplay.AddToClassList("combo-level-3");
            else _comboDisplay.AddToClassList("combo-level-2");

            SetVisible(true);
            _hideTimer = ComboHideDelaySec;
        }

        private void OnComboReset(ComboResetEvent _)
        {
            Hide();
        }

        private void SetVisible(bool show)
        {
            if (_comboDisplay == null) return;
            _visible = show;
            if (show) _comboDisplay.RemoveFromClassList("hidden");
            else _comboDisplay.AddToClassList("hidden");
        }

        private void Hide() => SetVisible(false);
    }
}
