#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Overlay "EN PAUSE" visible quand timeScale == 0 mais PauseMenu NON ouvert (ex: auto-pause focus lost).
    // Fade-in 0.2s, fade-out instantané.
    public class PauseIndicator : MonoSingleton<PauseIndicator>
    {
        private Canvas?        _canvas;
        private CanvasGroup?   _group;
        private Coroutine?     _fadeCo;

        protected override void OnAwakeSingleton()
        {
            BuildCanvas();
        }

        private void OnEnable()
        {
            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnPauseChanged += Sync;
        }

        private void OnDisable()
        {
            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnPauseChanged -= Sync;
        }

        private void Sync()
        {
            bool paused    = LevelRunner.Instance?.IsPaused ?? false;
            bool menuOpen  = PauseMenuController.Instance?.IsMenuOpen ?? false;
            bool showHint  = paused && !menuOpen;

            if (_fadeCo != null) StopCoroutine(_fadeCo);

            if (showHint)
                _fadeCo = StartCoroutine(FadeIn(0.2f));
            else
                Hide();
        }

        private IEnumerator FadeIn(float duration)
        {
            if (_canvas == null || _group == null) yield break;
            _canvas.gameObject.SetActive(true);
            float elapsed = 0f;
            float startAlpha = _group.alpha;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(startAlpha, 1f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            _group.alpha = 1f;
        }

        private void Hide()
        {
            if (_group != null)  _group.alpha = 0f;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        private void BuildCanvas()
        {
            var go = new GameObject("[PauseIndicator]");
            go.transform.SetParent(transform);

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 190; // Sous BossIntroBanner (200) mais au-dessus du HUD

            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            _group = go.AddComponent<CanvasGroup>();
            _group.alpha          = 0f;
            _group.blocksRaycasts = false; // transparent aux clics
            _group.interactable   = false;

            // Fond semi-transparent léger (noir 40%) pour contraste
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.40f);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Texte "EN PAUSE" centré, grand, blanc
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text      = "EN PAUSE";
            label.fontSize  = 72f;
            label.fontStyle = FontStyles.Bold;
            label.color     = Color.white;
            label.alignment = TextAlignmentOptions.Center;

            var labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.2f, 0.4f);
            labelRt.anchorMax = new Vector2(0.8f, 0.6f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            go.SetActive(false);
        }
    }
}
