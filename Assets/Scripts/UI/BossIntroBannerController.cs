#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CrowdDefense.Common;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Banner dramatique plein-écran quand un boss spawn.
    /// Écoute BossEncounteredEvent — fond rouge sombre 80% alpha,
    /// texte doré 60pt. Slide-in depuis droite 0.4s, hold 2.0s, slide-out vers gauche 0.4s.
    /// </summary>
    public class BossIntroBannerController : MonoSingleton<BossIntroBannerController>
    {
        private const float SlideInDuration  = 0.4f;
        private const float HoldDuration     = 2.0f;
        private const float HoldDurationSeen = 0.5f;
        private const float SlideOutDuration = 0.4f;

        private Canvas?     _canvas;
        private RectTransform? _panel;
        private TextMeshProUGUI? _label;
        private Button?     _skipBtn;
        private Coroutine?  _animCo;
        private string      _prefsKey = string.Empty;

        protected override void OnAwakeSingleton()
        {
            BuildCanvas();
            var em = EventManager.Instance;
            if (em != null) em.Subscribe<BossEncounteredEvent>(OnBossEncountered);
        }

        protected override void OnDestroySingleton()
        {
            var em = EventManager.Instance;
            if (em != null) em.Unsubscribe<BossEncounteredEvent>(OnBossEncountered);
        }

        private void OnBossEncountered(BossEncounteredEvent e) => Show(e.DisplayName);

        public void Show(string bossName, string subtitle = "")
        {
            if (_animCo != null) StopCoroutine(_animCo);
            string body = string.IsNullOrEmpty(subtitle)
                ? $"BOSS APPROCHE\n{bossName.ToUpper()}"
                : $"BOSS APPROCHE\n{bossName.ToUpper()}\n{subtitle}";
            if (_label != null) _label.text = body;
            _prefsKey = $"boss_intro_seen_{bossName.ToLower().Replace(" ", "_")}_v1";
            bool seen = PlayerPrefs.GetInt(_prefsKey, 0) == 1;
            if (_skipBtn != null) _skipBtn.gameObject.SetActive(true);
            _animCo = StartCoroutine(AnimateBanner(seen));

            JuiceFX.Instance?.Flash(new Color(0f, 0f, 0f, 0.3f), 200);
            JuiceFX.Instance?.SlowMo(0.5f, 1000);
            StartCoroutine(CameraDramaticZoom());
            AudioController.Instance?.Play("boss_roar", 1.2f);
        }

        private void SkipBanner()
        {
            if (_animCo != null) StopCoroutine(_animCo);
            CloseBanner();
        }

        private void CloseBanner()
        {
            if (!string.IsNullOrEmpty(_prefsKey)) PlayerPrefs.SetInt(_prefsKey, 1);
            if (_skipBtn != null) _skipBtn.gameObject.SetActive(false);
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            _animCo = null;
        }

        // Canvas UGUI créé à la main — pas de Prefab requis dans la scène
        private void BuildCanvas()
        {
            var go = new GameObject("[BossIntroBanner]");
            go.transform.SetParent(transform);

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;

            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            // Fond plein-écran
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(go.transform, false);
            var img = panelGo.AddComponent<Image>();
            img.color = new Color(0.25f, 0f, 0f, 0.8f); // rouge sombre 80%

            _panel = panelGo.GetComponent<RectTransform>();
            _panel.anchorMin = Vector2.zero;
            _panel.anchorMax = Vector2.one;
            _panel.offsetMin = Vector2.zero;
            _panel.offsetMax = Vector2.zero;

            // Texte centré TMP 60pt bold doré
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(panelGo.transform, false);
            _label = labelGo.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize  = 60f;
            _label.fontStyle = FontStyles.Bold;
            _label.color     = new Color(1f, 0.85f, 0.1f); // doré

            var labelRt = _label.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            // Click anywhere = skip
            var panelBtn = panelGo.AddComponent<Button>();
            panelBtn.onClick.AddListener(SkipBanner);

            // Bouton Skip explicite en bas-droite
            var skipGo = new GameObject("SkipBtn");
            skipGo.transform.SetParent(panelGo.transform, false);
            var skipImg = skipGo.AddComponent<Image>();
            skipImg.color = new Color(1f, 1f, 1f, 0.15f);
            _skipBtn = skipGo.AddComponent<Button>();
            _skipBtn.onClick.AddListener(SkipBanner);
            var skipRt = skipGo.GetComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(1f, 0f);
            skipRt.anchorMax = new Vector2(1f, 0f);
            skipRt.pivot     = new Vector2(1f, 0f);
            skipRt.anchoredPosition = new Vector2(-20f, 20f);
            skipRt.sizeDelta = new Vector2(120f, 40f);
            var skipLabel = new GameObject("Text").AddComponent<TextMeshProUGUI>();
            skipLabel.transform.SetParent(skipGo.transform, false);
            skipLabel.text = "Skip ▶";
            skipLabel.fontSize = 22f;
            skipLabel.alignment = TextAlignmentOptions.Center;
            skipLabel.color = Color.white;
            var skipLabelRt = skipLabel.GetComponent<RectTransform>();
            skipLabelRt.anchorMin = Vector2.zero;
            skipLabelRt.anchorMax = Vector2.one;
            skipLabelRt.offsetMin = Vector2.zero;
            skipLabelRt.offsetMax = Vector2.zero;
            _skipBtn.gameObject.SetActive(false);

            // Masqué par défaut — le panel commence hors-écran (offset X = Screen.width)
            SetPanelOffsetX(Screen.width);
            go.SetActive(false);
        }

        private IEnumerator AnimateBanner(bool alreadySeen)
        {
            if (_canvas == null || _panel == null) yield break;
            _canvas.gameObject.SetActive(true);

            float screenW = Screen.width;
            float hold = alreadySeen ? HoldDurationSeen : HoldDuration;

            // Slide-in depuis droite
            yield return SlidePanel(screenW, 0f, SlideInDuration);

            // Hold
            yield return new WaitForSecondsRealtime(hold);

            // Slide-out vers gauche
            yield return SlidePanel(0f, -screenW, SlideOutDuration);

            CloseBanner();
        }

        private IEnumerator SlidePanel(float fromX, float toX, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t); // ease-out quad
                SetPanelOffsetX(Mathf.Lerp(fromX, toX, eased));
                yield return null;
            }
            SetPanelOffsetX(toX);
        }

        private void SetPanelOffsetX(float x)
        {
            if (_panel == null) return;
            _panel.anchoredPosition = new Vector2(x, 0f);
        }

        private IEnumerator CameraDramaticZoom()
        {
            Camera cam = MainCameraCache.Main;
            if (cam == null) yield break;
            float origFOV = cam.fieldOfView;
            float targetFOV = origFOV * 0.7f;
            float t = 0f;
            while (t < 0.7f)
            {
                t += Time.unscaledDeltaTime;
                cam.fieldOfView = Mathf.Lerp(origFOV, targetFOV, Mathf.Clamp01(t / 0.7f));
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.3f);
            t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                cam.fieldOfView = Mathf.Lerp(targetFOV, origFOV, Mathf.Clamp01(t / 0.5f));
                yield return null;
            }
            cam.fieldOfView = origFOV;
        }

#if UNITY_EDITOR
        [ContextMenu("Test Boss Intro Banner")]
        private void TestBanner() => Show("TITAN INFERNAL");
#endif
    }
}
