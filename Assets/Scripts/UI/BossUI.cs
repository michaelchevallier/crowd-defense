#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Attach this to the same GameObject as UIDocument (HUD).
    // Reacts to BossSystem events and drives boss-banner, boss-cutscene, danger-vignette elements.
    [RequireComponent(typeof(UIDocument))]
    public class BossUI : MonoBehaviour
    {
        private VisualElement? _banner;
        private Label? _bannerName;
        private VisualElement? _bannerFill;
        private VisualElement? _cutscene;
        private Label? _cutsceneText;
        private VisualElement? _vignette;

        private Coroutine? _cutsceneCo;
        private Coroutine? _chargeCo;

        private void Start()
        {
            var uiDoc = GetComponent<UIDocument>();

            if (uiDoc == null) return;

            var root = uiDoc.rootVisualElement;

            if (root == null) return;
            _banner = root.Q<VisualElement>("boss-banner");
            _bannerName = root.Q<Label>("boss-banner-name");
            _bannerFill = root.Q<VisualElement>("boss-banner-fill");
            _cutscene = root.Q<VisualElement>("boss-cutscene");
            _cutsceneText = root.Q<Label>("boss-cutscene-text");
            _vignette = root.Q<VisualElement>("danger-vignette");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_banner == null) Debug.LogError("[BossUI] 'boss-banner' not found in UXML — check HUD.uxml");
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

            if (_cutsceneText != null) _cutsceneText.text = $"! {e.DisplayName.ToUpper()} !";
            if (_cutscene != null) SetVisible(_cutscene, true);

            // Pause game for cutscene — use timeScale=0 + WaitForSecondsRealtime (R3)
            Time.timeScale = 0f;

            if (_cutsceneCo != null) StopCoroutine(_cutsceneCo);
            _cutsceneCo = StartCoroutine(EndCutsceneAfter(2.0f));
        }

        private IEnumerator EndCutsceneAfter(float realSeconds)
        {
            // R3: WaitForSecondsRealtime is unaffected by timeScale=0
            yield return new WaitForSecondsRealtime(realSeconds);
            if (_cutscene != null) SetVisible(_cutscene, false);
            Time.timeScale = 1f;
            _cutsceneCo = null;
        }

        private void OnHpChanged(BossHpChangedEvent e)
        {
            if (_bannerFill == null) return;
            _bannerFill.style.width = new Length(e.Ratio * 100f, LengthUnit.Percent);
        }

        private void OnPhaseChanged(BossPhaseChangedEvent e)
        {
            if (e.PhaseIdx >= 1)
            {
                // Enraged / desperate: flash danger vignette
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
            // Stop any running coroutines
            if (_cutsceneCo != null) { StopCoroutine(_cutsceneCo); _cutsceneCo = null; }
            if (_chargeCo != null) { StopCoroutine(_chargeCo); _chargeCo = null; }

            // Restore timeScale in case boss died during cutscene
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
    }
}
