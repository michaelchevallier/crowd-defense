#nullable enable
using UnityEngine;
using UnityEngine.UI;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Runtime-built UGUI dialog. Shown after 2 consecutive defeats on the same level.
    // No UIDocument required — auto-creates its own Canvas in Awake.
    public sealed class SupportProposalDialog : MonoSingleton<SupportProposalDialog>
    {
        private Canvas?     _canvas;
        private GameObject? _panel;
        private Text?       _messageText;
        private Button?     _btnActivate;
        private Button?     _btnDecline;

        private string _pendingLevelId = "";

        private static readonly Color PanelColor   = new(0.08f, 0.06f, 0.14f, 0.95f);
        private static readonly Color TitleColor   = new(0.90f, 0.72f, 1.00f, 1.00f);
        private static readonly Color ButtonActive = new(0.32f, 0.20f, 0.55f, 1.00f);

        protected override void OnAwakeSingleton()
        {
            DontDestroyOnLoad(gameObject);
            BuildUI();
        }

        public void Show(string levelId)
        {
            _pendingLevelId = levelId;
            if (_messageText != null)
                _messageText.text =
                    "Deux defaites consecutives detectees.\n\n" +
                    "Activer l'Aide automatique ?\n\n" +
                    "+15% HP chateau   +20% or   -15% HP ennemis";
            if (_panel != null) _panel.SetActive(true);
            if (_canvas != null) _canvas.enabled = true;
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("SupportProposalCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 120;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();
            _canvas.enabled = false;

            // Fullscreen dimmer
            var dimGo  = new GameObject("Dim");
            dimGo.transform.SetParent(canvasGo.transform, false);
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.55f);
            StretchFull(dimGo);

            // Dialog box
            _panel = new GameObject("SupportPanel");
            _panel.transform.SetParent(canvasGo.transform, false);
            var panelImg = _panel.AddComponent<Image>();
            panelImg.color = PanelColor;
            var panelRt = _panel.GetComponent<RectTransform>();
            panelRt.anchorMin  = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax  = new Vector2(0.5f, 0.5f);
            panelRt.pivot      = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta  = new Vector2(480f, 280f);
            panelRt.anchoredPosition = Vector2.zero;
            _panel.SetActive(false);

            // Title
            var titleGo  = new GameObject("Title");
            titleGo.transform.SetParent(_panel.transform, false);
            var titleTxt = titleGo.AddComponent<Text>();
            titleTxt.text      = "Mode Aide";
            titleTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize  = 22;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color     = TitleColor;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin  = new Vector2(0f, 0.75f);
            titleRt.anchorMax  = new Vector2(1f, 1f);
            titleRt.offsetMin  = titleRt.offsetMax = Vector2.zero;

            // Message
            var msgGo = new GameObject("Message");
            msgGo.transform.SetParent(_panel.transform, false);
            _messageText = msgGo.AddComponent<Text>();
            _messageText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _messageText.fontSize  = 14;
            _messageText.color     = Color.white;
            _messageText.alignment = TextAnchor.MiddleCenter;
            var msgRt = msgGo.GetComponent<RectTransform>();
            msgRt.anchorMin  = new Vector2(0.05f, 0.25f);
            msgRt.anchorMax  = new Vector2(0.95f, 0.75f);
            msgRt.offsetMin  = msgRt.offsetMax = Vector2.zero;

            // Activate button
            _btnActivate = BuildButton(_panel.transform, "Activer", new Vector2(-0.27f, 0.12f), ButtonActive);
            _btnActivate.onClick.AddListener(OnActivate);

            // Decline button
            _btnDecline = BuildButton(_panel.transform, "Non merci", new Vector2(0.27f, 0.12f), new Color(0.25f, 0.25f, 0.25f, 1f));
            _btnDecline.onClick.AddListener(OnDecline);
        }

        private static Button BuildButton(Transform parent, string label, Vector2 anchorCenter, Color bg)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin       = new Vector2(0.5f + anchorCenter.x - 0.2f, anchorCenter.y);
            rt.anchorMax       = new Vector2(0.5f + anchorCenter.x + 0.2f, anchorCenter.y + 0.16f);
            rt.offsetMin       = rt.offsetMax = Vector2.zero;

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var txt  = txtGo.AddComponent<Text>();
            txt.text      = label;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = 15;
            txt.color     = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            StretchFull(txtGo);

            return go.AddComponent<Button>();
        }

        private void OnActivate()
        {
            SupportMode.Activate();
            Close();
        }

        private void OnDecline() => Close();

        private void Close()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_canvas != null) _canvas.enabled = false;
        }

        private static void StretchFull(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            if (_canvas == null || !_canvas.enabled) return;
            if (Input.GetKeyDown(KeyCode.Escape)) OnDecline();
        }
    }
}
