#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.UI
{
    // Full-screen overlay: choose avatar archetype (Warrior / Mage / Ranger) before a level.
    // Auto-builds its own Canvas at runtime; no prefab required.
    // Call AvatarPickPanel.Instance.Show(continuation) from LevelLoader or HeroPickScreen.
    public class AvatarPickPanel : MonoSingleton<AvatarPickPanel>
    {
        public const string PrefsKey = "hero_avatar";

        private Canvas?        _canvas;
        private RectTransform? _cardContainer;
        private Action?        _continuation;

        // Floating tooltip reused across all cards.
        private GameObject?  _tooltip;
        private Text?        _tooltipText;
        private RectTransform? _tooltipRt;

        protected override void OnAwakeSingleton() => EnsureCanvas();

        // Shows the panel; continuation is invoked after the player confirms.
        public void Show(Action continuation)
        {
            _continuation = continuation;
            BuildButtons();
            if (_canvas != null) _canvas.enabled = true;
        }

        public void Hide()
        {
            if (_canvas != null) _canvas.enabled = false;
            ClearButtons();
        }

        private void BuildButtons()
        {
            ClearButtons();
            if (_cardContainer == null) return;

            foreach (HeroAvatar avatar in System.Enum.GetValues(typeof(HeroAvatar)))
                BuildCard(avatar);
        }

        private void BuildCard(HeroAvatar avatar)
        {
            if (_cardContainer == null) return;

            var go = new GameObject($"avatar_{avatar}", typeof(RectTransform), typeof(CanvasRenderer));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_cardContainer, false);
            rt.sizeDelta = new Vector2(180f, 300f);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

            // Color swatch
            var swatchGo = new GameObject("swatch", typeof(RectTransform), typeof(CanvasRenderer));
            var swatchRt = swatchGo.GetComponent<RectTransform>();
            swatchRt.SetParent(rt, false);
            swatchRt.anchorMin = new Vector2(0.15f, 0.60f);
            swatchRt.anchorMax = new Vector2(0.85f, 0.93f);
            swatchRt.offsetMin = Vector2.zero;
            swatchRt.offsetMax = Vector2.zero;
            var swatch = swatchGo.AddComponent<Image>();
            swatch.color = HeroType.AvatarColor(avatar);

            // Name
            var nameGo = new GameObject("name", typeof(RectTransform), typeof(CanvasRenderer));
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.SetParent(rt, false);
            nameRt.anchorMin = new Vector2(0.05f, 0.47f);
            nameRt.anchorMax = new Vector2(0.95f, 0.59f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            var nameTxt = nameGo.AddComponent<Text>();
            nameTxt.text      = HeroType.AvatarLabel(avatar);
            nameTxt.fontSize  = 20;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameTxt.color     = Color.white;

            // Stat lines (2 lines stacked, anchored below name)
            string[] stats = HeroType.AvatarStatLines(avatar);
            float statTop  = 0.46f;
            float lineH    = 0.09f;
            for (int i = 0; i < stats.Length; i++)
            {
                float yMax = statTop - i * lineH;
                float yMin = yMax - lineH;
                var sgo = new GameObject($"stat_{i}", typeof(RectTransform), typeof(CanvasRenderer));
                var srt = sgo.GetComponent<RectTransform>();
                srt.SetParent(rt, false);
                srt.anchorMin = new Vector2(0.05f, yMin);
                srt.anchorMax = new Vector2(0.95f, yMax);
                srt.offsetMin = Vector2.zero;
                srt.offsetMax = Vector2.zero;
                var stxt = sgo.AddComponent<Text>();
                stxt.text      = stats[i];
                stxt.fontSize  = 13;
                stxt.alignment = TextAnchor.MiddleCenter;
                stxt.color     = new Color(0.8f, 1f, 0.8f, 1f);
            }

            // Choose button
            var btnGo = new GameObject("btn", typeof(RectTransform), typeof(CanvasRenderer));
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.SetParent(rt, false);
            btnRt.anchorMin = new Vector2(0.1f, 0.04f);
            btnRt.anchorMax = new Vector2(0.9f, 0.24f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = HeroType.AvatarColor(avatar);
            var btn = btnGo.AddComponent<Button>();

            var lblGo = new GameObject("label", typeof(RectTransform), typeof(CanvasRenderer));
            var lblRt = lblGo.GetComponent<RectTransform>();
            lblRt.SetParent(btnRt, false);
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;
            var lbl = lblGo.AddComponent<Text>();
            lbl.text      = "Choisir";
            lbl.fontSize  = 14;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color     = Color.white;

            HeroAvatar captured = avatar;
            btn.onClick.AddListener(() => OnAvatarChosen(captured));

            // Hover tooltip trigger on the card background
            var trigger = go.AddComponent<EventTrigger>();
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => ShowTooltip(captured));
            trigger.triggers.Add(enterEntry);
            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => HideTooltip());
            trigger.triggers.Add(exitEntry);
        }

        private void OnAvatarChosen(HeroAvatar avatar)
        {
            PlayerPrefs.SetString(PrefsKey, avatar.ToString());
            PlayerPrefs.Save();
            HideTooltip();

            // Apply archetype stat multipliers to the current Hero if already spawned.
            if (Hero.Current != null)
                HeroType.ApplyArchetype(avatar, Hero.Current);

            Hide();
            _continuation?.Invoke();
            _continuation = null;
        }

        private void ShowTooltip(HeroAvatar avatar)
        {
            EnsureTooltip();
            if (_tooltipText != null)
                _tooltipText.text = HeroType.AvatarTooltip(avatar);
            if (_tooltip != null)
                _tooltip.SetActive(true);
        }

        private void HideTooltip()
        {
            if (_tooltip != null)
                _tooltip.SetActive(false);
        }

        private void EnsureTooltip()
        {
            if (_tooltip != null) return;
            if (_canvas == null) return;

            _tooltip = new GameObject("Tooltip", typeof(RectTransform), typeof(CanvasRenderer));
            _tooltip.transform.SetParent(transform, false);
            _tooltipRt = _tooltip.GetComponent<RectTransform>();
            _tooltipRt.anchorMin = new Vector2(0.30f, 0.04f);
            _tooltipRt.anchorMax = new Vector2(0.70f, 0.14f);
            _tooltipRt.offsetMin = Vector2.zero;
            _tooltipRt.offsetMax = Vector2.zero;

            var bg = _tooltip.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.10f, 0.93f);

            var txtGo = new GameObject("text", typeof(RectTransform), typeof(CanvasRenderer));
            txtGo.transform.SetParent(_tooltipRt, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0.02f, 0f);
            txtRt.anchorMax = new Vector2(0.98f, 1f);
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            _tooltipText = txtGo.AddComponent<Text>();
            _tooltipText.fontSize  = 13;
            _tooltipText.alignment = TextAnchor.MiddleCenter;
            _tooltipText.color     = new Color(0.9f, 0.9f, 0.9f, 1f);

            _tooltip.SetActive(false);
        }

        private void ClearButtons()
        {
            if (_cardContainer == null) return;
            foreach (Transform child in _cardContainer)
                Destroy(child.gameObject);
        }

        private void EnsureCanvas()
        {
            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();

            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 110;  // above HeroPickScreen (100)

            if (gameObject.GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // Background overlay
            var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer));
            bg.transform.SetParent(transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.82f);

            // Title
            var title = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
            title.transform.SetParent(bg.transform, false);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.1f, 0.82f);
            titleRt.anchorMax = new Vector2(0.9f, 0.94f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            var titleTxt = title.AddComponent<Text>();
            titleTxt.text      = "Choisir votre avatar";
            titleTxt.fontSize  = 34;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color     = Color.white;

            // Card container (horizontal row)
            var container = new GameObject("CardContainer", typeof(RectTransform));
            container.transform.SetParent(bg.transform, false);
            var containerRt = container.GetComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.1f, 0.18f);
            containerRt.anchorMax = new Vector2(0.9f, 0.80f);
            containerRt.offsetMin = Vector2.zero;
            containerRt.offsetMax = Vector2.zero;

            var hLayout = container.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing                = 24f;
            hLayout.childAlignment         = TextAnchor.MiddleCenter;
            hLayout.childForceExpandWidth  = false;
            hLayout.childForceExpandHeight = false;
            hLayout.childControlWidth      = false;
            hLayout.childControlHeight     = false;
            hLayout.padding                = new RectOffset(8, 8, 8, 8);

            _cardContainer = containerRt;

            _canvas.enabled = false;
        }
    }
}
