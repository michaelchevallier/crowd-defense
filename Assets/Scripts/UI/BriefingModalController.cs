#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    public class BriefingModalController : UIControllerBase
    {
        public static BriefingModalController? Instance { get; private set; }

        private VisualElement? _overlay;
        private Label?         _titleLabel;
        private Label?         _textLabel;
        private Label?         _countdownLabel;

        private void Awake()
        {
            Instance = this;
            ResolveUI();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        protected override void OnUIReady()
        {
            if (Root == null) return;
            _overlay        = Root.Q<VisualElement>("briefing-modal-overlay");
            _titleLabel     = Root.Q<Label>("briefing-title");
            _textLabel      = Root.Q<Label>("briefing-text");
            _countdownLabel = Root.Q<Label>("briefing-countdown");
        }

        public IEnumerator ShowAndCountdown(string title, string briefing)
        {
            if (_overlay == null) yield break;

            if (_titleLabel     != null) _titleLabel.text     = title;
            if (_textLabel      != null) _textLabel.text      = briefing;
            if (_countdownLabel != null) _countdownLabel.text = "";

            _overlay.RemoveFromClassList("hidden");

            // brief read window before countdown starts
            yield return new WaitForSecondsRealtime(1.5f);

            for (int i = 3; i > 0; i--)
            {
                if (_countdownLabel != null) _countdownLabel.text = i.ToString();
                yield return new WaitForSecondsRealtime(1f);
            }

            if (_countdownLabel != null) _countdownLabel.text = "GO!";
            yield return new WaitForSecondsRealtime(0.5f);

            _overlay.AddToClassList("hidden");
        }
    }
}
