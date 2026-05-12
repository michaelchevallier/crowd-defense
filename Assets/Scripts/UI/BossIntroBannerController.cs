#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Banner dramatique plein-écran quand un boss spawn.
    /// Écoute BossEncounteredEvent — fond rouge sombre 80% alpha,
    /// texte doré 60pt. Slide-in depuis droite 0.4s, hold 2.0s, slide-out vers gauche 0.4s.
    /// </summary>
    public class BossIntroBannerController : MonoSingleton<BossIntroBannerController>
    {
        private const float SlideInDuration = 0.4f;
        private const float HoldDuration    = 2.0f;
        private const float SlideOutDuration = 0.4f;

        private Canvas?     _canvas;
        private RectTransform? _panel;
        private TextMeshProUGUI? _label;
        private Coroutine?  _animCo;

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

        public void Show(string bossName)
        {
            if (_animCo != null) StopCoroutine(_animCo);
            if (_label != null) _label.text = $"BOSS APPROCHE\n{bossName.ToUpper()}";
            _animCo = StartCoroutine(AnimateBanner());

            // AudioController log-warns + joue un beep si la clé manque — pas de crash
            AudioController.Instance?.Play("boss_intro_roar", 1f);
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

            // Masqué par défaut — le panel commence hors-écran (offset X = Screen.width)
            SetPanelOffsetX(Screen.width);
            go.SetActive(false);
        }

        private IEnumerator AnimateBanner()
        {
            if (_canvas == null || _panel == null) yield break;
            _canvas.gameObject.SetActive(true);

            float screenW = Screen.width;

            // Slide-in depuis droite
            yield return SlidePanel(screenW, 0f, SlideInDuration);

            // Hold
            yield return new WaitForSecondsRealtime(HoldDuration);

            // Slide-out vers gauche
            yield return SlidePanel(0f, -screenW, SlideOutDuration);

            _canvas.gameObject.SetActive(false);
            _animCo = null;
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

#if UNITY_EDITOR
        [ContextMenu("Test Boss Intro Banner")]
        private void TestBanner() => Show("TITAN INFERNAL");
#endif
    }
}
