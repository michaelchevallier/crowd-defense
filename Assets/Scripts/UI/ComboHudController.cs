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
    // Milestone banner: big gold x2!/x4!/x8! slides in from top-right on crossing kill levels 2/4/8.
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

        private const float BannerSlideInSec = 0.4f;
        private const float BannerHoldSec = 1.2f;
        private const float BannerPulsePeak = 1.25f;

        private static readonly int[] MilestoneKills = { 2, 4, 8 };
        private static readonly string[] MilestoneLabels = { "x2!", "x4!", "x8!" };

        private VisualElement? _comboDisplay;
        private Label? _comboLabel;
        private Label? _comboBanner;
        private float _hideTimer;
        private bool _visible;
        private Coroutine? _animCo;
        private Coroutine? _bannerCo;
        private int _lastMilestoneLevel;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _comboDisplay = root.Q<VisualElement>("combo-display");
            _comboLabel = root.Q<Label>("combo-label");
            _comboBanner = root.Q<Label>("combo-banner");

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

            // Fire milestone banner when crossing 2/4/8 kill thresholds
            TryFireMilestoneBanner(evt.Level);

            // Restart grow+shake animation on each new combo level
            if (_animCo != null) StopCoroutine(_animCo);
            _animCo = StartCoroutine(AnimateCo());
        }

        private void OnComboReset(ComboResetEvent _)
        {
            if (_animCo != null) { StopCoroutine(_animCo); _animCo = null; }
            if (_bannerCo != null) { StopCoroutine(_bannerCo); _bannerCo = null; }
            _lastMilestoneLevel = 0;
            ResetTransform();
            HideBanner();
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

        private void TryFireMilestoneBanner(int level)
        {
            if (_comboBanner == null) return;
            for (int i = MilestoneKills.Length - 1; i >= 0; i--)
            {
                if (level >= MilestoneKills[i] && _lastMilestoneLevel < MilestoneKills[i])
                {
                    _lastMilestoneLevel = MilestoneKills[i];
                    _comboBanner.text = MilestoneLabels[i];
                    if (_bannerCo != null) StopCoroutine(_bannerCo);
                    _bannerCo = StartCoroutine(BannerCo());
                    break;
                }
            }
        }

        private IEnumerator BannerCo()
        {
            if (_comboBanner == null) yield break;

            // Slide in: remove hidden, remove exit class
            _comboBanner.RemoveFromClassList("hidden");
            _comboBanner.RemoveFromClassList("combo-banner-exit");
            _comboBanner.transform.scale = Vector3.one;

            yield return new WaitForSecondsRealtime(BannerSlideInSec);

            // Pulse: scale up then back
            float t = 0f;
            float pulseDur = 0.15f;
            while (t < pulseDur)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1f, BannerPulsePeak, t / pulseDur);
                _comboBanner.transform.scale = new Vector3(s, s, 1f);
                yield return null;
            }
            t = 0f;
            while (t < pulseDur)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(BannerPulsePeak, 1f, t / pulseDur);
                _comboBanner.transform.scale = new Vector3(s, s, 1f);
                yield return null;
            }
            _comboBanner.transform.scale = Vector3.one;

            yield return new WaitForSecondsRealtime(BannerHoldSec);

            // Slide out: apply exit class (CSS transition handles opacity + translate)
            _comboBanner.AddToClassList("combo-banner-exit");
            yield return new WaitForSecondsRealtime(0.4f);

            HideBanner();
            _bannerCo = null;
        }

        private void HideBanner()
        {
            if (_comboBanner == null) return;
            _comboBanner.RemoveFromClassList("combo-banner-exit");
            _comboBanner.transform.scale = Vector3.one;
            _comboBanner.AddToClassList("hidden");
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
