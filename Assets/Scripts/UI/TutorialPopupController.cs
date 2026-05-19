#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CrowdDefense.UI
{
    // First-time onboarding popup shown once when entering W1-1 for the first time.
    // Driven by PlayerPrefs "tutorial_done_v1" (0 = not seen, 1 = dismissed).
    // Self-destructs after the player confirms — no dependency on TutorialState flow.
    public class TutorialPopupController : MonoBehaviour
    {
        private const string PrefKey   = "tutorial_done_v1";
        private const string LevelId   = "world1-1";
        private const float  FadeSecs  = 0.4f;

        private CanvasGroup? _canvasGroup;

        // Called from LevelRunner.Start — no-ops silently if conditions not met.
        public static void TryShow(string? currentLevelId)
        {
            if (PlayerPrefs.GetInt(PrefKey, 0) == 1) return;
            if (currentLevelId != LevelId) return;

            var go = new GameObject("[TutorialPopup]");
            go.AddComponent<TutorialPopupController>().Build();
        }

        private void Build()
        {
            // Root canvas — renders above everything else.
            var canvas           = gameObject.AddComponent<Canvas>();
            canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder  = 999;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();

            _canvasGroup         = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha   = 0f;

            // Semi-transparent black overlay.
            var overlayGo        = new GameObject("Overlay");
            overlayGo.transform.SetParent(transform, false);
            var overlayImg       = overlayGo.AddComponent<Image>();
            overlayImg.color     = new Color(0f, 0f, 0f, 0.55f);
            var overlayRect      = overlayGo.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            // Center panel 400 × 220 px.
            var panelGo          = new GameObject("Panel");
            panelGo.transform.SetParent(transform, false);
            var panelImg         = panelGo.AddComponent<Image>();
            panelImg.color       = new Color(0.10f, 0.10f, 0.15f, 0.97f);
            var panelRect        = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin  = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax  = new Vector2(0.5f, 0.5f);
            panelRect.pivot      = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta  = new Vector2(400f, 220f);
            panelRect.anchoredPosition = Vector2.zero;

            // Title.
            AddLabel(panelGo, "Title", "Bienvenue, defenseur !", 20,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(16f, -12f), new Vector2(-16f, -44f), FontStyle.Bold);

            // Bullet 1.
            AddLabel(panelGo, "B1", "Protege ton chateau.", 14,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(24f, -56f), new Vector2(-24f, -80f));

            // Bullet 2.
            AddLabel(panelGo, "B2", "Place des tours sur les cellules vides.", 14,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(24f, -88f), new Vector2(-24f, -112f));

            // Bullet 3.
            AddLabel(panelGo, "B3", "Lance la vague avec le bouton 'Vague'.", 14,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(24f, -120f), new Vector2(-24f, -144f));

            // Confirm button.
            var btnGo            = new GameObject("BtnConfirm");
            btnGo.transform.SetParent(panelGo.transform, false);
            var btnImg           = btnGo.AddComponent<Image>();
            btnImg.color         = new Color(0.20f, 0.55f, 0.90f, 1f);
            var btnRect          = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin    = new Vector2(0.5f, 0f);
            btnRect.anchorMax    = new Vector2(0.5f, 0f);
            btnRect.pivot        = new Vector2(0.5f, 0f);
            btnRect.sizeDelta    = new Vector2(160f, 38f);
            btnRect.anchoredPosition = new Vector2(0f, 14f);

            var btnComponent     = btnGo.AddComponent<Button>();
            btnComponent.onClick.AddListener(OnConfirm);

            AddLabel(btnGo, "BtnLabel", "C'est parti !", 15,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero, FontStyle.Bold, TextAnchor.MiddleCenter);

            StartCoroutine(FadeIn());
        }

        private static void AddLabel(
            GameObject parent,
            string goName,
            string text,
            int fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            FontStyle style   = FontStyle.Normal,
            TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var go        = new GameObject(goName);
            go.transform.SetParent(parent.transform, false);
            var lbl       = go.AddComponent<Text>();
            lbl.text      = text;
            lbl.fontSize  = fontSize;
            lbl.fontStyle = style;
            lbl.alignment = anchor;
            lbl.color     = Color.white;
            var r         = go.GetComponent<RectTransform>();
            r.anchorMin   = anchorMin;
            r.anchorMax   = anchorMax;
            r.offsetMin   = offsetMin;
            r.offsetMax   = offsetMax;
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < FadeSecs)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_canvasGroup != null)
                    _canvasGroup.alpha = Mathf.Clamp01(elapsed / FadeSecs);
                yield return null;
            }
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
        }

        private void OnConfirm()
        {
            PlayerPrefs.SetInt(PrefKey, 1);
            PlayerPrefs.Save();
            StartCoroutine(FadeOutAndDestroy());
        }

        private IEnumerator FadeOutAndDestroy()
        {
            float elapsed = 0f;
            float startAlpha = _canvasGroup?.alpha ?? 1f;
            while (elapsed < FadeSecs)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_canvasGroup != null)
                    _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / FadeSecs);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
