#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class AutoSaveIndicator : MonoSingleton<AutoSaveIndicator>
    {
        private const float FadeInDuration  = 0.12f;
        private const float HoldDuration    = 0.55f;
        private const float FadeOutDuration = 0.35f;

        private Label?  _icon;
        private bool    _pulsing;

        protected override void OnAwakeSingleton()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _icon = new Label("💾");
            _icon.style.position         = Position.Absolute;
            _icon.style.right            = new StyleLength(12f);
            _icon.style.bottom           = new StyleLength(12f);
            _icon.style.fontSize         = new StyleLength(22f);
            _icon.style.unityTextAlign   = TextAnchor.MiddleCenter;
            _icon.style.opacity          = 0f;
            _icon.pickingMode            = PickingMode.Ignore;

            root.Add(_icon);
        }

        public void Pulse()
        {
            if (_icon == null || _pulsing) return;
            StartCoroutine(DoPulse());
        }

        private IEnumerator DoPulse()
        {
            _pulsing = true;

            // Fade in
            float elapsed = 0f;
            while (elapsed < FadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _icon!.style.opacity = Mathf.Clamp01(elapsed / FadeInDuration);
                yield return null;
            }
            _icon!.style.opacity = 1f;

            yield return new WaitForSecondsRealtime(HoldDuration);

            // Fade out
            elapsed = 0f;
            while (elapsed < FadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _icon!.style.opacity = 1f - Mathf.Clamp01(elapsed / FadeOutDuration);
                yield return null;
            }
            _icon!.style.opacity = 0f;

            _pulsing = false;
        }
    }
}
