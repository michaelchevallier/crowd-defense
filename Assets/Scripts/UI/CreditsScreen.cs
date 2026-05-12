#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    public class CreditsScreen : UIControllerBase
    {
        public static CreditsScreen? Instance { get; private set; }

        private const string CreditsBody =
            "Designed by Michael Chevallier\n\n" +
            "Built with Claude Code Opus 4.7 — Anthropic\n\n" +
            "Unity 6 LTS + URP + UI Toolkit\n\n" +
            "KayKit prototype assets (CC0)\n\n" +
            "© 2026";

        private const float ScrollSpeed    = 28f;   // px per second
        private const float ScrollPauseSec = 2.5f;  // pause at top before restarting

        private VisualElement? _root;
        private ScrollView?    _scroll;
        private Button?        _btnBack;
        private Coroutine?     _autoScroll;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            ResolveUI();
        }

        protected override void OnUIReady()
        {
            if (Root == null) return;

            _root    = Root.Q<VisualElement>("credits-root");
            _scroll  = Root.Q<ScrollView>("credits-scroll");
            _btnBack = Root.Q<Button>("btn-credits-back");

            var textLabel = Root.Q<Label>("credits-text");
            if (textLabel != null) textLabel.text = CreditsBody;

            if (_btnBack != null) _btnBack.clicked += Hide;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show()
        {
            if (_root == null) return;
            _root.RemoveFromClassList("hidden");

            // Reset scroll to top then start auto-scroll
            if (_scroll != null) _scroll.scrollOffset = Vector2.zero;
            if (_autoScroll != null) StopCoroutine(_autoScroll);
            _autoScroll = StartCoroutine(AutoScroll());
        }

        public void Hide()
        {
            if (_autoScroll != null) { StopCoroutine(_autoScroll); _autoScroll = null; }
            _root?.AddToClassList("hidden");
        }

        private IEnumerator AutoScroll()
        {
            if (_scroll == null) yield break;

            yield return new WaitForSeconds(ScrollPauseSec);

            while (true)
            {
                float maxScroll = _scroll.contentContainer.layout.height
                                - _scroll.contentViewport.layout.height;

                if (maxScroll <= 0f)
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                while (_scroll.scrollOffset.y < maxScroll)
                {
                    float delta = ScrollSpeed * Time.unscaledDeltaTime;
                    _scroll.scrollOffset = new Vector2(0f, _scroll.scrollOffset.y + delta);
                    yield return null;
                }

                // Pause at bottom, then loop back to top
                yield return new WaitForSeconds(ScrollPauseSec);
                _scroll.scrollOffset = Vector2.zero;
                yield return new WaitForSeconds(ScrollPauseSec);
            }
        }
    }
}
