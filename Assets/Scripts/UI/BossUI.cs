#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.UI
{
    // Attach this to the same GameObject as UIDocument (HUD).
    // Reacts to BossSystem events and drives boss-banner, boss-cutscene, danger-vignette elements.
    public class BossUI : UIControllerBase
    {
        private const float LineFadeIn    = 0.5f;
        private const float LineDelay     = 0.6f;
        private const float AutoCloseTime = 5f;
        private const float FadeOutTime   = 0.35f;
        private const float BloomPeak     = 3.5f;
        private const float BloomDecay    = 1.8f;

        private VisualElement? _banner;
        private Label? _bannerName;
        private VisualElement? _bannerFill;
        private VisualElement? _cutscene;
        private VisualElement? _cutsceneDim;
        private readonly Label?[] _lines = new Label?[4];
        private Button? _skipBtn;
        private VisualElement? _vignette;

        private Coroutine? _cutsceneCo;
        private Coroutine? _chargeCo;

        private void Start()
        {
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            if (Root == null) return;

            _banner     = Root.Q<VisualElement>("boss-banner");
            _bannerName = Root.Q<Label>("boss-banner-name");
            _bannerFill = Root.Q<VisualElement>("boss-banner-fill");
            _cutscene   = Root.Q<VisualElement>("boss-cutscene");
            _cutsceneDim = Root.Q<VisualElement>("boss-cutscene-dim");
            for (int i = 0; i < 4; i++)
                _lines[i] = Root.Q<Label>($"boss-cutscene-line{i}");
            _skipBtn    = Root.Q<Button>("boss-cutscene-skip");
            _vignette   = Root.Q<VisualElement>("danger-vignette");

            if (_skipBtn != null) _skipBtn.clicked += SkipCutscene;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_banner == null) Debug.LogError("[BossUI] 'boss-banner' not found in UXML — check HUD.uxml");
            if (_cutscene == null) Debug.LogError("[BossUI] 'boss-cutscene' not found in UXML");
#endif

            var em = EventManager.Instance;
            if (em == null) return;
            em.Subscribe<BossEncounteredEvent>(OnEncountered);
            em.Subscribe<BossHpChangedEvent>(OnHpChanged);
            em.Subscribe<BossPhaseChangedEvent>(OnPhaseChanged);
            em.Subscribe<BossChargeWarningEvent>(OnChargeWarn);
            em.Subscribe<BossDefeatedEvent>(OnDefeated);
        }

        private void OnDestroy()
        {
            if (_skipBtn != null) _skipBtn.clicked -= SkipCutscene;
            var em = EventManager.Instance;
            if (em == null) return;
            em.Unsubscribe<BossEncounteredEvent>(OnEncountered);
            em.Unsubscribe<BossHpChangedEvent>(OnHpChanged);
            em.Unsubscribe<BossPhaseChangedEvent>(OnPhaseChanged);
            em.Unsubscribe<BossChargeWarningEvent>(OnChargeWarn);
            em.Unsubscribe<BossDefeatedEvent>(OnDefeated);
        }

        private void OnEncountered(BossEncounteredEvent e)
        {
            if (_banner == null) return;

            if (_bannerName != null) _bannerName.text = e.DisplayName.ToUpper();
            if (_bannerFill != null)
            {
                _bannerFill.style.width = new Length(100f, LengthUnit.Percent);
                _bannerFill.style.backgroundColor = e.AuraColor;
            }
            SetVisible(_banner, true);
            _banner.AddToClassList("show");

            if (_cutsceneCo != null) StopCoroutine(_cutsceneCo);
            _cutsceneCo = StartCoroutine(RunCutscene(e.DisplayName, e.CutsceneLines));
        }

        // ── Cutscene coroutine ────────────────────────────────────────────────────

        private IEnumerator RunCutscene(string displayName, string[] customLines)
        {
            if (_cutscene == null) yield break;

            string[] lines = BuildLines(displayName, customLines);

            // Reset line visibility
            for (int i = 0; i < 4; i++)
                ResetLine(i);

            SetVisible(_cutscene, true);
            if (_skipBtn != null) SetVisible((VisualElement)_skipBtn, true);

            Time.timeScale = 0f;

            // Bloom burst on boss spawn
            PostProcessController.Instance?.SetBloomIntensity(BloomPeak);
            StartCoroutine(DecayBloom());

            // Fade in lines sequentially
            for (int i = 0; i < lines.Length; i++)
            {
                if (_lines[i] != null)
                {
                    _lines[i]!.text = lines[i];
                    SetVisible(_lines[i]!, true);
                    yield return StartCoroutine(FadeLine(_lines[i]!, 0f, 1f, LineFadeIn, unscaled: true));
                }
                yield return new WaitForSecondsRealtime(LineDelay - LineFadeIn);
            }

            // Hold remaining time up to AutoCloseTime (counted from cutscene start)
            float elapsed = lines.Length * LineDelay;
            float remaining = AutoCloseTime - elapsed;
            if (remaining > 0f) yield return new WaitForSecondsRealtime(remaining);

            yield return StartCoroutine(FadeOutCutscene());
            _cutsceneCo = null;
        }

        private void SkipCutscene()
        {
            if (_cutsceneCo != null) { StopCoroutine(_cutsceneCo); _cutsceneCo = null; }
            StartCoroutine(FadeOutCutscene());
        }

        private IEnumerator FadeOutCutscene()
        {
            if (_cutsceneDim != null)
                yield return StartCoroutine(FadeElement(_cutsceneDim, 1f, 0f, FadeOutTime, unscaled: true));
            CloseCutscene();
        }

        private void CloseCutscene()
        {
            if (_cutscene != null) SetVisible(_cutscene, false);
            if (_skipBtn != null) SetVisible((VisualElement)_skipBtn, false);
            if (_cutsceneDim != null) _cutsceneDim.style.opacity = 1f;
            Time.timeScale = 1f;
        }

        // ── Bloom helpers ─────────────────────────────────────────────────────────

        private IEnumerator DecayBloom()
        {
            float t = 0f;
            while (t < BloomDecay)
            {
                t += Time.unscaledDeltaTime;
                float v = Mathf.Lerp(BloomPeak, 0.5f, t / BloomDecay);
                PostProcessController.Instance?.SetBloomIntensity(v);
                yield return null;
            }
            PostProcessController.Instance?.SetBloomIntensity(0.5f);
        }

        // ── Fade helpers ──────────────────────────────────────────────────────────

        private static IEnumerator FadeLine(VisualElement el, float from, float to, float dur, bool unscaled)
        {
            el.style.opacity = from;
            float t = 0f;
            while (t < dur)
            {
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                el.style.opacity = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
                yield return null;
            }
            el.style.opacity = to;
        }

        private static IEnumerator FadeElement(VisualElement el, float from, float to, float dur, bool unscaled)
        {
            el.style.opacity = from;
            float t = 0f;
            while (t < dur)
            {
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                el.style.opacity = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
                yield return null;
            }
            el.style.opacity = to;
        }

        private void ResetLine(int i)
        {
            if (_lines[i] == null) return;
            _lines[i]!.style.opacity = 0f;
            _lines[i]!.text = "";
            SetVisible(_lines[i]!, false);
        }

        // ── Line content ──────────────────────────────────────────────────────────

        private static string[] BuildLines(string displayName, string[] custom)
        {
            string name = displayName.ToUpper();
            if (custom is { Length: > 0 })
            {
                // Pad to 4 entries
                var result = new string[4];
                for (int i = 0; i < 4; i++)
                    result[i] = i < custom.Length ? custom[i] : "";
                return result;
            }
            return new[] { "BOSS", name, "Preparez-vous !", "" };
        }

        // ── Banner / HP / Phase / Charge / Defeated ───────────────────────────────

        private void OnHpChanged(BossHpChangedEvent e)
        {
            if (_bannerFill == null) return;
            _bannerFill.style.width = new Length(e.Ratio * 100f, LengthUnit.Percent);
        }

        private void OnPhaseChanged(BossPhaseChangedEvent e)
        {
            if (e.PhaseIdx >= 1)
            {
                if (_chargeCo != null) StopCoroutine(_chargeCo);
                _chargeCo = StartCoroutine(VignetteFlash(0.8f));
            }
        }

        private void OnChargeWarn(BossChargeWarningEvent _)
        {
            if (_banner != null) _banner.AddToClassList("charging");
            if (_chargeCo != null) StopCoroutine(_chargeCo);
            _chargeCo = StartCoroutine(ChargeFlashCo(1.5f));
        }

        private IEnumerator ChargeFlashCo(float duration)
        {
            if (_vignette != null) { SetVisible(_vignette, true); _vignette.AddToClassList("active"); }
            yield return new WaitForSeconds(duration);
            if (_vignette != null) { _vignette.RemoveFromClassList("active"); SetVisible(_vignette, false); }
            if (_banner != null) _banner.RemoveFromClassList("charging");
            _chargeCo = null;
        }

        private IEnumerator VignetteFlash(float duration)
        {
            if (_vignette != null) { SetVisible(_vignette, true); _vignette.AddToClassList("active"); }
            yield return new WaitForSeconds(duration);
            if (_vignette != null) { _vignette.RemoveFromClassList("active"); SetVisible(_vignette, false); }
            _chargeCo = null;
        }

        private void OnDefeated(BossDefeatedEvent _)
        {
            if (_cutsceneCo != null) { StopCoroutine(_cutsceneCo); _cutsceneCo = null; }
            if (_chargeCo != null) { StopCoroutine(_chargeCo); _chargeCo = null; }
            if (Time.timeScale == 0f) Time.timeScale = 1f;

            if (_banner != null)
            {
                SetVisible(_banner, false);
                _banner.RemoveFromClassList("show");
                _banner.RemoveFromClassList("charging");
            }
            if (_cutscene != null) SetVisible(_cutscene, false);
            if (_vignette != null)
            {
                _vignette.RemoveFromClassList("active");
                SetVisible(_vignette, false);
            }
        }

        private static void SetVisible(VisualElement el, bool visible)
        {
            if (visible) el.RemoveFromClassList("hidden");
            else el.AddToClassList("hidden");
        }

#if UNITY_EDITOR
        [ContextMenu("Test Boss Cutscene")]
        private void TestCutscene()
        {
            if (_cutsceneCo != null) StopCoroutine(_cutsceneCo);
            _cutsceneCo = StartCoroutine(RunCutscene("TITAN INFERNAL", System.Array.Empty<string>()));
        }
#endif
    }
}
