#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;
using CrowdDefense.Common;
using CrowdDefense.Data;

namespace CrowdDefense.UI
{
    // Full-screen overlay shown before a level loads when no hero is selected.
    // Attach to a Canvas GameObject in the WorldMap scene (or any persistent scene).
    // heroTypes array must be populated in Inspector (or auto-loaded from Resources/Heroes/).
    public class HeroPickScreen : MonoSingleton<HeroPickScreen>
    {
        public const string PrefsKey = "selected_hero_v1";

        // 5 tint presets shown under each hero card.
        private static readonly (string hex, Color color)[] TintPresets =
        {
            ("#FFFFFF", Color.white),
            ("#E05252", new Color(0.878f, 0.322f, 0.322f)),
            ("#5285E0", new Color(0.322f, 0.522f, 0.878f)),
            ("#52E069", new Color(0.322f, 0.878f, 0.412f)),
            ("#9B52E0", new Color(0.608f, 0.322f, 0.878f)),
        };

        public static string TintPrefsKey(string heroId) => $"hero_tint_{heroId}_v1";

        [SerializeField] private HeroType[]   heroTypes   = Array.Empty<HeroType>();
        [SerializeField] private Canvas?      rootCanvas;
        [SerializeField] private RectTransform? cardContainer;
        [SerializeField] private GameObject?  cardPrefab;

        private string? _pendingLevelId;
        private Action? _pendingContinuation;

        protected override void OnAwakeSingleton()
        {
            if (heroTypes.Length == 0)
                heroTypes = Resources.LoadAll<HeroType>("Heroes");

            EnsureCanvasSetup();
            Hide();
        }

        // Called by LevelLoader when no hero is chosen yet.
        public void Show(string levelId, Action continuation)
        {
            _pendingLevelId    = levelId;
            _pendingContinuation = continuation;
            BuildCards();
            if (rootCanvas != null) rootCanvas.enabled = true;
        }

        public void Hide()
        {
            if (rootCanvas != null) rootCanvas.enabled = false;
            ClearCards();
        }

        private void BuildCards()
        {
            ClearCards();
            if (cardContainer == null) return;

            foreach (var hero in heroTypes)
            {
                if (cardPrefab != null)
                {
                    var go   = Instantiate(cardPrefab, cardContainer);
                    var view = go.GetComponent<HeroCardView>();
                    if (view != null) view.Bind(hero, OnHeroChosen);
                }
                else
                {
                    BuildCardDynamic(hero);
                }
            }
        }

        private void BuildCardDynamic(HeroType hero)
        {
            if (cardContainer == null) return;

            var cardGo = new GameObject($"card_{hero.Id}", typeof(RectTransform), typeof(CanvasRenderer));
            var cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.SetParent(cardContainer, false);
            cardRt.sizeDelta = new Vector2(200f, 300f);

            var bg = cardGo.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            // Portrait box (BodyColor swatch)
            var portraitGo = new GameObject("portrait", typeof(RectTransform), typeof(CanvasRenderer));
            var portraitRt = portraitGo.GetComponent<RectTransform>();
            portraitRt.SetParent(cardRt, false);
            portraitRt.anchorMin = new Vector2(0.1f, 0.55f);
            portraitRt.anchorMax = new Vector2(0.9f, 0.95f);
            portraitRt.offsetMin = Vector2.zero;
            portraitRt.offsetMax = Vector2.zero;
            var portraitImg = portraitGo.AddComponent<Image>();
            portraitImg.color = hero.BodyColor;

            // Name label
            var nameGo = new GameObject("name", typeof(RectTransform), typeof(CanvasRenderer));
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.SetParent(cardRt, false);
            nameRt.anchorMin = new Vector2(0.05f, 0.40f);
            nameRt.anchorMax = new Vector2(0.95f, 0.55f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            var nameTxt = nameGo.AddComponent<Text>();
            nameTxt.text      = hero.DisplayName;
            nameTxt.fontSize  = 18;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameTxt.color     = Color.white;

            // Description label
            var descGo = new GameObject("desc", typeof(RectTransform), typeof(CanvasRenderer));
            var descRt = descGo.GetComponent<RectTransform>();
            descRt.SetParent(cardRt, false);
            descRt.anchorMin = new Vector2(0.05f, 0.20f);
            descRt.anchorMax = new Vector2(0.95f, 0.40f);
            descRt.offsetMin = Vector2.zero;
            descRt.offsetMax = Vector2.zero;
            var descTxt = descGo.AddComponent<Text>();
            descTxt.text      = string.IsNullOrEmpty(hero.Description) ? $"DMG {hero.Damage:F2}  SPD {hero.MoveSpeed:F0}" : hero.Description;
            descTxt.fontSize  = 12;
            descTxt.alignment = TextAnchor.UpperCenter;
            descTxt.color     = new Color(0.85f, 0.85f, 0.85f);

            // Tint row (5 color swatches)
            BuildTintRow(cardRt, hero, portraitImg);

            // Choose button (moved down to make room: anchorMin.y was 0.04, now shifted to row below tints)
            var btnGo = new GameObject("btn", typeof(RectTransform), typeof(CanvasRenderer));
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.SetParent(cardRt, false);
            btnRt.anchorMin = new Vector2(0.1f, 0.04f);
            btnRt.anchorMax = new Vector2(0.9f, 0.19f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.6f, 1f, 1f);
            var btn = btnGo.AddComponent<Button>();

            var btnLblGo = new GameObject("label", typeof(RectTransform), typeof(CanvasRenderer));
            var btnLblRt = btnLblGo.GetComponent<RectTransform>();
            btnLblRt.SetParent(btnRt, false);
            btnLblRt.anchorMin = Vector2.zero;
            btnLblRt.anchorMax = Vector2.one;
            btnLblRt.offsetMin = Vector2.zero;
            btnLblRt.offsetMax = Vector2.zero;
            var btnLbl = btnLblGo.AddComponent<Text>();
            btnLbl.text      = "Choisir";
            btnLbl.fontSize  = 14;
            btnLbl.fontStyle = FontStyle.Bold;
            btnLbl.alignment = TextAnchor.MiddleCenter;
            btnLbl.color     = Color.white;

            HeroType captured = hero;
            btn.onClick.AddListener(() => OnHeroChosen(captured));
        }

        // Builds a row of 5 color-swatch buttons (19% height band above the choose-btn).
        // Clicking a swatch: saves hex to PlayerPrefs + updates portrait image color immediately.
        private void BuildTintRow(RectTransform cardRt, HeroType hero, Image portraitImg)
        {
            string savedHex = PlayerPrefs.GetString(TintPrefsKey(hero.Id), "");
            if (!string.IsNullOrEmpty(savedHex))
            {
                if (ColorUtility.TryParseHtmlString(savedHex, out var saved))
                    portraitImg.color = saved;
            }

            float swatchWidth = 1f / TintPresets.Length;
            for (int i = 0; i < TintPresets.Length; i++)
            {
                var (hex, col) = TintPresets[i];
                var swatchGo = new GameObject($"tint_{i}", typeof(RectTransform), typeof(CanvasRenderer));
                var swatchRt = swatchGo.GetComponent<RectTransform>();
                swatchRt.SetParent(cardRt, false);
                float xMin = i * swatchWidth;
                float xMax = xMin + swatchWidth;
                swatchRt.anchorMin = new Vector2(xMin, 0.19f);
                swatchRt.anchorMax = new Vector2(xMax, 0.34f);
                swatchRt.offsetMin = new Vector2(2f, 2f);
                swatchRt.offsetMax = new Vector2(-2f, -2f);

                var swatchImg = swatchGo.AddComponent<Image>();
                swatchImg.color = col;
                var swatchBtn = swatchGo.AddComponent<Button>();

                string capturedHex   = hex;
                Color  capturedColor = col;
                string heroId        = hero.Id;
                Image  portrait      = portraitImg;
                swatchBtn.onClick.AddListener(() =>
                {
                    PlayerPrefs.SetString(TintPrefsKey(heroId), capturedHex);
                    PlayerPrefs.Save();
                    portrait.color = capturedColor;
                });
            }
        }

        private void ClearCards()
        {
            if (cardContainer == null) return;
            foreach (Transform child in cardContainer)
                Destroy(child.gameObject);
        }

        private void OnHeroChosen(HeroType hero)
        {
            PlayerPrefs.SetString(PrefsKey, hero.AssetKey);
            PlayerPrefs.Save();
            Hide();
            _pendingContinuation?.Invoke();
            _pendingContinuation = null;
            _pendingLevelId      = null;
        }

        private void EnsureCanvasSetup()
        {
            if (rootCanvas != null) return;

            rootCanvas = gameObject.GetComponent<Canvas>();
            if (rootCanvas == null) rootCanvas = gameObject.AddComponent<Canvas>();

            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 100;

            if (gameObject.GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            if (cardContainer == null)
            {
                var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer));
                bg.transform.SetParent(transform, false);
                var bgRt = bg.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
                var bgImg = bg.AddComponent<Image>();
                bgImg.color = new Color(0f, 0f, 0f, 0.80f);

                var title = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
                title.transform.SetParent(bg.transform, false);
                var titleRt = title.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0.1f, 0.85f);
                titleRt.anchorMax = new Vector2(0.9f, 0.95f);
                titleRt.offsetMin = Vector2.zero;
                titleRt.offsetMax = Vector2.zero;
                var titleTxt = title.AddComponent<Text>();
                titleTxt.text      = "Choisir un Heros";
                titleTxt.fontSize  = 32;
                titleTxt.fontStyle = FontStyle.Bold;
                titleTxt.alignment = TextAnchor.MiddleCenter;
                titleTxt.color     = Color.white;

                var container = new GameObject("CardContainer", typeof(RectTransform));
                container.transform.SetParent(bg.transform, false);
                var containerRt = container.GetComponent<RectTransform>();
                containerRt.anchorMin = new Vector2(0.05f, 0.15f);
                containerRt.anchorMax = new Vector2(0.95f, 0.83f);
                containerRt.offsetMin = Vector2.zero;
                containerRt.offsetMax = Vector2.zero;

                var hLayout = container.AddComponent<HorizontalLayoutGroup>();
                hLayout.spacing            = 16f;
                hLayout.childAlignment     = TextAnchor.MiddleCenter;
                hLayout.childForceExpandWidth  = false;
                hLayout.childForceExpandHeight = false;
                hLayout.childControlWidth  = false;
                hLayout.childControlHeight = false;
                hLayout.padding            = new RectOffset(8, 8, 8, 8);

                cardContainer = containerRt;
            }
        }
    }
}
