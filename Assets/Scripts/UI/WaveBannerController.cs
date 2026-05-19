#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Data;
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
        private const float BannerHeightPx = 110f;

        private VisualElement? _banner;
        private Label?         _label;
        private Label?         _icons;
        private Coroutine?     _anim;

        protected override void OnAwakeSingleton()
        {
            var uiDoc = GetComponent<UIDocument>();

            if (uiDoc == null) return;

            var root = uiDoc.rootVisualElement;

            if (root == null) return;
            BuildBanner(root);
        }

        private VisualElement? _startBannerUxml;
        private VisualElement? _clearBannerUxml;
        private Label?         _clearBannerN;
        private Label?         _startBannerN;
        private Coroutine?     _clearAnim;
        private Coroutine?     _startAnim;

        private const float ClearHoldS = 2.5f;
        private const float StartHoldS = 1.5f;

        private void Start()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStart   += OnWaveStart;
                WaveManager.Instance.OnWaveCleared += OnWaveCleared;
            }

            // Wire the UXML-declared banners (start & clear)
            var uiDoc = GetComponent<UIDocument>();
            var root = uiDoc?.rootVisualElement;
            if (root != null)
            {
                _startBannerUxml = root.Q<VisualElement>("wave-start-banner");
                _startBannerN    = root.Q<Label>("wave-start-n");
                _clearBannerUxml = root.Q<VisualElement>("wave-clear-banner");
                _clearBannerN    = root.Q<Label>("wave-clear-n");
            }
        }

        protected override void OnDestroySingleton()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStart   -= OnWaveStart;
                WaveManager.Instance.OnWaveCleared -= OnWaveCleared;
            }
        }

        private void OnWaveStart(int idx)
        {
            int total = WaveManager.Instance?.TotalWaves ?? 0;
            var waveDef = WaveManager.Instance?.GetWaveDef(idx);
            Show(idx + 1, total, waveDef);
            AnimateStartBanner(idx + 1);
        }

        private void OnWaveCleared(int idx)
        {
            if (_clearBannerUxml == null) return;
            if (_clearBannerN != null) _clearBannerN.text = (idx + 1).ToString();
            if (_clearAnim != null) StopCoroutine(_clearAnim);
            _clearAnim = StartCoroutine(AnimateClearBanner());
        }

        private void AnimateStartBanner(int waveNum)
        {
            if (_startBannerUxml == null) return;
            if (_startBannerN != null) _startBannerN.text = waveNum.ToString();
            if (_startAnim != null) StopCoroutine(_startAnim);
            _startAnim = StartCoroutine(AnimateStartBannerCoroutine());
        }

        private IEnumerator AnimateStartBannerCoroutine()
        {
            if (_startBannerUxml == null) yield break;
            _startBannerUxml.RemoveFromClassList("wave-banner--hidden");
            yield return new WaitForSeconds(StartHoldS);
            _startBannerUxml.AddToClassList("wave-banner--hidden");
            _startAnim = null;
        }

        private IEnumerator AnimateClearBanner()
        {
            if (_clearBannerUxml == null) yield break;
            _clearBannerUxml.RemoveFromClassList("wave-banner--hidden");
            yield return new WaitForSeconds(ClearHoldS);
            _clearBannerUxml.AddToClassList("wave-banner--hidden");
            _clearAnim = null;
        }

        public void Show(int wave, int total, WaveDef? waveDef = null)
        {
            if (_banner == null || _label == null) return;
            _label.text = L.Get("hud.wave_banner", wave, total);
            if (_icons != null)
                _icons.text = BuildIconsText(waveDef);
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimateBanner());
        }

        private static string BuildIconsText(WaveDef? waveDef)
        {
            if (waveDef == null) return string.Empty;
            var entries = waveDef.Value.entries;
            if (entries == null || entries.Count == 0) return string.Empty;

            // Collect dominant variants (by total count, top 5)
            var counts = new Dictionary<string, int>();
            bool hasBoss = false;

            foreach (var entry in entries)
            {
                if (entry.type != null && entry.type.IsBoss)
                    hasBoss = true;

                string key = entry.variant switch
                {
                    EnemyVariant.Fast    => "fast",
                    EnemyVariant.Tough   => "tough",
                    EnemyVariant.Regen   => "regen",
                    EnemyVariant.Armored => "armored",
                    _                    => "normal",
                };
                counts[key] = (counts.TryGetValue(key, out int v) ? v : 0) + entry.count;
            }

            // Build icon string: dominant variant icons (max 3 distinct), then boss if present
            var ordered = counts.OrderByDescending(p => p.Value).Take(3);
            var parts = new List<string>();
            foreach (var p in ordered)
            {
                string icon = p.Key switch
                {
                    "fast"    => "\U0001f4a8",   // 💨
                    "tough"   => "\U0001f6e1",   // 🛡
                    "regen"   => "\U0001f49a",   // 💚
                    "armored" => "⚔",        // ⚔
                    _         => string.Empty,
                };
                if (!string.IsNullOrEmpty(icon))
                    parts.Add(icon);
            }
            if (hasBoss)
                parts.Add("\U0001f480"); // 💀

            return string.Join(" ", parts);
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

            _icons = new Label();
            _icons.AddToClassList("wave-banner-icons");
            _banner.Add(_icons);

            root.Add(_banner);
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseIn(float t)  => t * t;
    }
}
