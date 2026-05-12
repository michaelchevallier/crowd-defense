#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Attach to same UIDocument GameObject as HudController (or a dedicated one).
    // Toggle with L key or the bottom-right HUD button named "btn-history-log".
    [RequireComponent(typeof(UIDocument))]
    public class HistoryLogPanel : MonoBehaviour
    {
        private VisualElement? _panel;
        private ScrollView?   _scroll;
        private Button?       _toggleBtn;
        private bool          _visible;
        private float         _refreshTimer;
        private const float   RefreshInterval = 1f;

        private void Start()
        {
            var doc  = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            _panel     = root.Q<VisualElement>("history-log-panel");
            _scroll    = root.Q<ScrollView>("history-log-scroll");
            _toggleBtn = root.Q<Button>("btn-history-log");

            if (_panel == null)
            {
                _panel  = BuildPanel();
                _scroll = _panel.Q<ScrollView>("history-log-scroll");
                root.Add(_panel);
            }

            _toggleBtn?.RegisterCallback<ClickEvent>(_ => TogglePanel());
            SetVisible(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
                TogglePanel();

            if (!_visible) return;
            _refreshTimer += Time.unscaledDeltaTime;
            if (_refreshTimer >= RefreshInterval)
            {
                _refreshTimer = 0f;
                Refresh();
            }
        }

        private void TogglePanel()
        {
            SetVisible(!_visible);
            if (_visible) Refresh();
        }

        private void SetVisible(bool show)
        {
            _visible = show;
            if (_panel != null)
                _panel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Refresh()
        {
            if (_scroll == null) return;
            _scroll.Clear();

            var log = WaveHistoryLog.Instance;
            if (log == null) return;

            foreach (var ev in log.Events)
            {
                int minutes = (int)(ev.Time / 60f);
                int seconds = (int)(ev.Time % 60f);

                var row = new Label($"[{minutes:00}:{seconds:00}] {ev.Detail}")
                {
                    style =
                    {
                        fontSize           = 12,
                        color              = new StyleColor(Color.white),
                        paddingBottom      = 2,
                        paddingTop         = 2,
                        whiteSpace         = WhiteSpace.Normal,
                        unityTextAlign     = TextAnchor.MiddleLeft,
                    }
                };

                row.AddToClassList("history-row");
                row.AddToClassList($"history-{ev.Category}");
                _scroll.Add(row);
            }

            // Scroll to bottom (most recent)
            _scroll.ScrollTo(_scroll.contentContainer.ElementAt(
                Mathf.Max(0, _scroll.contentContainer.childCount - 1)));
        }

        // Procedural panel if UXML doesn't declare it.
        private VisualElement BuildPanel()
        {
            var panel = new VisualElement { name = "history-log-panel" };
            panel.style.position          = Position.Absolute;
            panel.style.right             = 8;
            panel.style.bottom            = 48;
            panel.style.width             = 320;
            panel.style.height            = 260;
            panel.style.backgroundColor   = new StyleColor(new Color(0f, 0f, 0f, 0.75f));
            panel.style.borderTopLeftRadius    = 6;
            panel.style.borderTopRightRadius   = 6;
            panel.style.borderBottomLeftRadius = 6;
            panel.style.borderBottomRightRadius= 6;
            panel.style.paddingTop    = 6;
            panel.style.paddingBottom = 6;
            panel.style.paddingLeft   = 8;
            panel.style.paddingRight  = 8;

            var title = new Label("Historique")
            {
                style =
                {
                    fontSize       = 13,
                    color          = new StyleColor(new Color(1f, 0.85f, 0.3f)),
                    marginBottom   = 4,
                    unityFontStyleAndWeight = FontStyle.Bold,
                }
            };
            panel.Add(title);

            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "history-log-scroll" };
            scroll.style.flexGrow = 1;
            panel.Add(scroll);

            return panel;
        }
    }
}
