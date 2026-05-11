#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class WaveBannerController : MonoSingleton<WaveBannerController>
    {
        private const float SlideInS  = 0.3f;
        private const float HoldS     = 1.4f;
        private const float SlideOutS = 0.3f;

        // Banner height used for translate animation (pixels, matches USS min-height)
        private const float BannerHeightPx = 90f;

        private VisualElement? _banner;
        private Label?         _label;
        private Coroutine?     _anim;

        protected override void OnAwakeSingleton()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            BuildBanner(root);
        }

        private void Start()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveStart += OnWaveStart;
        }

        protected override void OnDestroySingleton()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveStart -= OnWaveStart;
        }

        private void OnWaveStart(int idx)
        {
            int total = WaveManager.Instance?.TotalWaves ?? 0;
            Show(idx + 1, total);
        }

        public void Show(int wave, int total)
        {
            if (_banner == null || _label == null) return;
            _label.text = L.Get("hud.wave_banner", wave, total);
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimateBanner());
        }

        private IEnumerator AnimateBanner()
        {
            if (_banner == null) yield break;

            // Ensure visible
            _banner.RemoveFromClassList("hidden");
            _banner.style.opacity = 1f;

            // Slide in from top: translate Y from -BannerHeightPx → 0
            float t = 0f;
            while (t < SlideInS)
            {
                t += Time.unscaledDeltaTime;
                float p  = Mathf.Clamp01(t / SlideInS);
                float dy = Mathf.Lerp(-BannerHeightPx, 0f, EaseOut(p));
                _banner.style.translate = new Translate(new Length(0), new Length(dy, LengthUnit.Pixel));
                yield return null;
            }
            _banner.style.translate = new Translate(new Length(0), new Length(0));

            // Hold
            yield return new WaitForSecondsRealtime(HoldS);

            // Slide out to top: translate Y from 0 → -BannerHeightPx
            t = 0f;
            while (t < SlideOutS)
            {
                t += Time.unscaledDeltaTime;
                float p  = Mathf.Clamp01(t / SlideOutS);
                float dy = Mathf.Lerp(0f, -BannerHeightPx, EaseIn(p));
                _banner.style.translate = new Translate(new Length(0), new Length(dy, LengthUnit.Pixel));
                yield return null;
            }

            _banner.AddToClassList("hidden");
            _anim = null;
        }

        private void BuildBanner(VisualElement root)
        {
            _banner = new VisualElement();
            _banner.AddToClassList("wave-banner");
            _banner.AddToClassList("hidden");

            _label = new Label();
            _label.AddToClassList("wave-banner-label");
            _banner.Add(_label);

            root.Add(_banner);
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseIn(float t)  => t * t;
    }
}
