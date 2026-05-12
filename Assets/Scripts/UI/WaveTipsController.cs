#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class WaveTipsController : MonoSingleton<WaveTipsController>
    {
        private const int TipCount = 15;
        private const float DisplaySeconds = 8f;
        private const float FadeSeconds = 0.4f;

        private VisualElement? _panel;
        private Label? _label;
        private Coroutine? _routine;
        private int _lastTipIdx = -1;

        protected override void OnAwakeSingleton()
        {
            var uiDoc = GetComponent<UIDocument>();

            if (uiDoc == null) return;

            var root = uiDoc.rootVisualElement;

            if (root == null) return;
            BuildPanel(root);
        }

        private void Start()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveCleared += OnWaveCleared;
                WaveManager.Instance.OnWaveStart   += OnWaveStart;
            }
        }

        protected override void OnDestroySingleton()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveCleared -= OnWaveCleared;
                WaveManager.Instance.OnWaveStart   -= OnWaveStart;
            }
        }

        private void OnWaveCleared(int _) => ShowRandomTip();

        private void OnWaveStart(int _) => Hide();

        public void ShowRandomTip()
        {
            int idx = PickTipIdx();
            _lastTipIdx = idx;
            if (_label != null)
                _label.text = "💡 " + L.Get($"tip.{idx}");
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShowRoutine());
        }

        private int PickTipIdx()
        {
            // Avoid repeating the same tip twice in a row
            int idx;
            do { idx = Random.Range(0, TipCount); }
            while (idx == _lastTipIdx && TipCount > 1);
            return idx;
        }

        private IEnumerator ShowRoutine()
        {
            if (_panel == null) yield break;

            _panel.RemoveFromClassList("wave-tip-hidden");
            _panel.style.display = DisplayStyle.Flex;

            // Fade in
            float t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                _panel.style.opacity = Mathf.Clamp01(t / FadeSeconds);
                yield return null;
            }
            _panel.style.opacity = 1f;

            // Hold
            yield return new WaitForSecondsRealtime(DisplaySeconds);

            // Fade out
            t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                _panel.style.opacity = 1f - Mathf.Clamp01(t / FadeSeconds);
                yield return null;
            }

            _panel.style.display = DisplayStyle.None;
            _routine = null;
        }

        private void Hide()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            if (_panel != null)
                _panel.style.display = DisplayStyle.None;
        }

        private void BuildPanel(VisualElement root)
        {
            _panel = new VisualElement();
            _panel.AddToClassList("wave-tip-panel");
            _panel.style.display = DisplayStyle.None;

            // Layout: bottom-center, 600×80 px
            _panel.style.position        = Position.Absolute;
            _panel.style.bottom          = new Length(28, LengthUnit.Pixel);
            _panel.style.left            = new Length(50, LengthUnit.Percent);
            _panel.style.translate       = new Translate(new Length(-50, LengthUnit.Percent), new Length(0));
            _panel.style.width           = new Length(600, LengthUnit.Pixel);
            _panel.style.minHeight       = new Length(80, LengthUnit.Pixel);
            _panel.style.paddingLeft     = new Length(20, LengthUnit.Pixel);
            _panel.style.paddingRight    = new Length(20, LengthUnit.Pixel);
            _panel.style.paddingTop      = new Length(12, LengthUnit.Pixel);
            _panel.style.paddingBottom   = new Length(12, LengthUnit.Pixel);
            _panel.style.backgroundColor = new Color(0f, 0f, 0f, 0.7f);
            _panel.style.borderTopLeftRadius     = new Length(8, LengthUnit.Pixel);
            _panel.style.borderTopRightRadius    = new Length(8, LengthUnit.Pixel);
            _panel.style.borderBottomLeftRadius  = new Length(8, LengthUnit.Pixel);
            _panel.style.borderBottomRightRadius = new Length(8, LengthUnit.Pixel);
            _panel.style.justifyContent  = Justify.Center;
            _panel.style.alignItems      = Align.Center;
            _panel.style.opacity         = 0f;
            _panel.pickingMode           = PickingMode.Ignore;

            _label = new Label();
            _label.AddToClassList("wave-tip-label");
            _label.style.color          = Color.white;
            _label.style.fontSize       = new Length(16, LengthUnit.Pixel);
            _label.style.unityFontStyleAndWeight = FontStyle.Italic;
            _label.style.whiteSpace     = WhiteSpace.Normal;
            _label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _label.pickingMode          = PickingMode.Ignore;
            _panel.Add(_label);

            root.Add(_panel);
        }
    }
}
