#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Attached to the same GameObject as HudController (UIDocument).
    // Listens to ComboUpdatedEvent / ComboResetEvent and drives the combo-display element.
    // Auto-hides after ComboHideDelaySec if no further kill resets the timer.
    // Animation: grow punch (scale 1→1.35→1) + horizontal shake on each combo level-up.
    [RequireComponent(typeof(UIDocument))]
    public class ComboHudController : MonoBehaviour
    {
        private const float ComboHideDelaySec = 1.2f;
        private const float GrowDuration = 0.12f;    // seconds to peak scale
        private const float ShrinkDuration = 0.18f;  // seconds back to normal
        private const float PeakScale = 1.35f;
        private const float ShakeMagnitudePx = 8f;
        private const float ShakeDuration = 0.30f;
        private const int ShakeSteps = 8;

        private VisualElement? _comboDisplay;
        private Label? _comboLabel;
        private float _hideTimer;
        private bool _visible;
        private Coroutine? _animCo;

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

            // Restart grow+shake animation on each new combo level
            if (_animCo != null) StopCoroutine(_animCo);
            _animCo = StartCoroutine(AnimateCo());
        }

        private void OnComboReset(ComboResetEvent _)
        {
            if (_animCo != null) { StopCoroutine(_animCo); _animCo = null; }
            ResetTransform();
            Hide();
        }

        private IEnumerator AnimateCo()
        {
            if (_comboDisplay == null) yield break;

            // --- Grow punch ---
            float t = 0f;
            while (t < GrowDuration)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1f, PeakScale, t / GrowDuration);
                _comboDisplay.transform.scale = new Vector3(s, s, 1f);
                yield return null;
            }
            t = 0f;
            while (t < ShrinkDuration)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(PeakScale, 1f, t / ShrinkDuration);
                _comboDisplay.transform.scale = new Vector3(s, s, 1f);
                yield return null;
            }
            _comboDisplay.transform.scale = Vector3.one;

            // --- Horizontal shake ---
            float stepDuration = ShakeDuration / ShakeSteps;
            for (int i = 0; i < ShakeSteps; i++)
            {
                float dir = (i % 2 == 0) ? 1f : -1f;
                float mag = ShakeMagnitudePx * (1f - (float)i / ShakeSteps);
                _comboDisplay.transform.position = new Vector3(dir * mag, 0f, 0f);
                yield return new WaitForSecondsRealtime(stepDuration);
            }
            _comboDisplay.transform.position = Vector3.zero;
            _animCo = null;
        }

        private void ResetTransform()
        {
            if (_comboDisplay == null) return;
            _comboDisplay.transform.scale = Vector3.one;
            _comboDisplay.transform.position = Vector3.zero;
        }

        private void SetVisible(bool show)
        {
            if (_comboDisplay == null) return;
            _visible = show;
            if (show) _comboDisplay.RemoveFromClassList("hidden");
            else _comboDisplay.AddToClassList("hidden");
        }

        private void Hide()
        {
            ResetTransform();
            SetVisible(false);
        }
    }
}
