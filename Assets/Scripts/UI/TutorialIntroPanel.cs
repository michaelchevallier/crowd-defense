#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CrowdDefense.UI
{
    // 5-step intro panel shown once before L1 starts for first-time players.
    // Steps: Welcome → Place tower → Defend castle → Earn gold → Perks.
    // Gated on PlayerPrefs "cd.tutorial_done" == 0. Next/Skip buttons. Self-destructs.
    public class TutorialIntroPanel : MonoBehaviour
    {
        private const string PrefKey  = "cd.tutorial_done";
        private const string LevelId  = "W1-1";
        private const float  FadeSecs = 0.35f;

        private static readonly string[] Titles =
        {
            "Bienvenue, defenseur !",
            "Place tes tours",
            "Protege le chateau",
            "Gagne de l'or",
            "Perks",
        };

        private static readonly string[] Bodies =
        {
            "Ce tutoriel te guidera pour tes premiers pas.\nCliquez sur Suivant pour commencer.",
            "Clique sur une cellule vide pour placer une tour.\nLes tours attaquent automatiquement les ennemis.",
            "Les ennemis avancent vers ton chateau.\nSi le chateau tombe, la partie est perdue.",
            "Tue des ennemis et lance les vagues tot\npour gagner des pieces supplementaires.",
            "When your hero levels up, choose 1 of 3 perks to enhance your build.",
        };

        private int          _step;
        private CanvasGroup? _cg;
        private Text?        _titleText;
        private Text?        _bodyText;
        private Text?        _progressText;
        private Button?      _nextBtn;
        private Text?        _nextBtnLabel;

        // Called from LevelRunner.Start — no-ops silently if conditions not met.
        public static void TryShow(string? currentLevelId)
        {
            if (PlayerPrefs.GetInt(PrefKey, 0) == 1) return;
            if (currentLevelId != LevelId) return;

            var go = new GameObject("[TutorialIntroPanel]");
            go.AddComponent<TutorialIntroPanel>().Build();
        }

        private void Build()
        {
            var canvas          = gameObject.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler                   = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode           = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution   = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight    = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            _cg       = gameObject.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;

            // Semi-transparent dimmer behind the panel.
            var overlayGo      = new GameObject("Overlay");
            overlayGo.transform.SetParent(transform, false);
            var overlayImg     = overlayGo.AddComponent<Image>();
            overlayImg.color   = new Color(0f, 0f, 0f, 0.6f);
            var overlayRect    = overlayGo.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            // Block raycasts on overlay so clicks don't pass through to game.
            overlayGo.AddComponent<GraphicRaycaster>();

            // Center card — 480 × 280 px.
            var cardGo     = new GameObject("Card");
            cardGo.transform.SetParent(transform, false);
            var cardImg    = cardGo.AddComponent<Image>();
            cardImg.color  = new Color(0.09f, 0.10f, 0.16f, 0.97f);
            var cardRect   = cardGo.GetComponent<RectTransform>();
            cardRect.anchorMin  = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax  = new Vector2(0.5f, 0.5f);
            cardRect.pivot      = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta  = new Vector2(480f, 280f);
            cardRect.anchoredPosition = Vector2.zero;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Step indicator top-right  "1 / 4"
            _progressText = AddText(cardGo, "Progress", "", 13, font,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-80f, -12f), new Vector2(-12f, -34f),
                FontStyle.Normal, TextAnchor.UpperRight);
            _progressText.color = new Color(0.6f, 0.6f, 0.6f, 1f);

            // Title
            _titleText = AddText(cardGo, "Title", "", 22, font,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, -18f), new Vector2(-20f, -60f),
                FontStyle.Bold, TextAnchor.MiddleCenter);

            // Divider line
            var divGo  = new GameObject("Divider");
            divGo.transform.SetParent(cardGo.transform, false);
            var divImg = divGo.AddComponent<Image>();
            divImg.color = new Color(0.35f, 0.35f, 0.45f, 1f);
            var divRect = divGo.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0.05f, 1f);
            divRect.anchorMax = new Vector2(0.95f, 1f);
            divRect.sizeDelta = new Vector2(0f, 1f);
            divRect.anchoredPosition = new Vector2(0f, -62f);

            // Body text
            _bodyText = AddText(cardGo, "Body", "", 15, font,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(24f, -76f), new Vector2(-24f, -190f),
                FontStyle.Normal, TextAnchor.UpperLeft);
            _bodyText.lineSpacing = 1.3f;

            // Skip button (bottom-left)
            var skipGo   = new GameObject("BtnSkip");
            skipGo.transform.SetParent(cardGo.transform, false);
            var skipImg  = skipGo.AddComponent<Image>();
            skipImg.color = new Color(0.22f, 0.22f, 0.28f, 1f);
            var skipRect = skipGo.GetComponent<RectTransform>();
            skipRect.anchorMin  = new Vector2(0f, 0f);
            skipRect.anchorMax  = new Vector2(0f, 0f);
            skipRect.pivot      = new Vector2(0f, 0f);
            skipRect.sizeDelta  = new Vector2(120f, 36f);
            skipRect.anchoredPosition = new Vector2(20f, 20f);
            var skipBtn  = skipGo.AddComponent<Button>();
            skipBtn.onClick.AddListener(OnSkip);
            AddText(skipGo, "Label", "Passer", 13, font,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                FontStyle.Normal, TextAnchor.MiddleCenter);

            // Next button (bottom-right)
            var nextGo   = new GameObject("BtnNext");
            nextGo.transform.SetParent(cardGo.transform, false);
            var nextImg  = nextGo.AddComponent<Image>();
            nextImg.color = new Color(0.18f, 0.52f, 0.88f, 1f);
            var nextRect = nextGo.GetComponent<RectTransform>();
            nextRect.anchorMin  = new Vector2(1f, 0f);
            nextRect.anchorMax  = new Vector2(1f, 0f);
            nextRect.pivot      = new Vector2(1f, 0f);
            nextRect.sizeDelta  = new Vector2(140f, 36f);
            nextRect.anchoredPosition = new Vector2(-20f, 20f);
            _nextBtn = nextGo.AddComponent<Button>();
            _nextBtn.onClick.AddListener(OnNext);
            _nextBtnLabel = AddText(nextGo, "Label", "Suivant", 14, font,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                FontStyle.Bold, TextAnchor.MiddleCenter);

            RefreshSlide();
            StartCoroutine(FadeIn());
        }

        private static Text AddText(
            GameObject parent,
            string goName,
            string text,
            int fontSize,
            Font font,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            FontStyle style  = FontStyle.Normal,
            TextAnchor align = TextAnchor.UpperLeft)
        {
            var go    = new GameObject(goName);
            go.transform.SetParent(parent.transform, false);
            var lbl       = go.AddComponent<Text>();
            lbl.text      = text;
            lbl.font      = font;
            lbl.fontSize  = fontSize;
            lbl.fontStyle = style;
            lbl.alignment = align;
            lbl.color     = Color.white;
            var r         = go.GetComponent<RectTransform>();
            r.anchorMin   = anchorMin;
            r.anchorMax   = anchorMax;
            r.offsetMin   = offsetMin;
            r.offsetMax   = offsetMax;
            return lbl;
        }

        private void RefreshSlide()
        {
            if (_titleText    != null) _titleText.text    = Titles[_step];
            if (_bodyText     != null) _bodyText.text     = Bodies[_step];
            if (_progressText != null) _progressText.text = $"{_step + 1} / {Titles.Length}";
            if (_nextBtnLabel != null)
                _nextBtnLabel.text = _step == Titles.Length - 1 ? "Commencer !" : "Suivant";
        }

        private void OnNext()
        {
            _step++;
            if (_step >= Titles.Length)
                Dismiss();
            else
                RefreshSlide();
        }

        private void OnSkip() => Dismiss();

        private void Dismiss()
        {
            PlayerPrefs.SetInt(PrefKey, 1);
            PlayerPrefs.Save();
            StartCoroutine(FadeOutAndDestroy());
        }

        private IEnumerator FadeIn()
        {
            // Small delay so the scene finishes its own Start calls first.
            yield return new WaitForSecondsRealtime(0.5f);
            float elapsed = 0f;
            while (elapsed < FadeSecs)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_cg != null) _cg.alpha = Mathf.Clamp01(elapsed / FadeSecs);
                yield return null;
            }
            if (_cg != null) _cg.alpha = 1f;
        }

        private IEnumerator FadeOutAndDestroy()
        {
            float startAlpha = _cg?.alpha ?? 1f;
            float elapsed    = 0f;
            while (elapsed < FadeSecs)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_cg != null)
                    _cg.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / FadeSecs);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
