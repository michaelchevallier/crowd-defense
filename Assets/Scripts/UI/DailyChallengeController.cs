#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class DailyChallengeController : MonoBehaviour
    {
        private VisualElement? _root;
        private VisualElement? _dailyRoot;

        private Label? _titleLabel;
        private Label? _seedValueLabel;
        private Label? _challengeValueLabel;
        private Label? _bestValueLabel;
        private Button? _playBtn;
        private Button? _closeBtn;

        private void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _dailyRoot = _root.Q<VisualElement>("daily-root");

            _titleLabel         = _root.Q<Label>("daily-title");
            _seedValueLabel     = _root.Q<Label>("daily-seed-value");
            _challengeValueLabel = _root.Q<Label>("daily-challenge-value");
            _bestValueLabel     = _root.Q<Label>("daily-best-value");
            _playBtn            = _root.Q<Button>("daily-play-btn");
            _closeBtn           = _root.Q<Button>("daily-close-btn");

            ApplyLocalizedTexts();
            L.OnLocaleChanged += ApplyLocalizedTexts;

            _playBtn?.RegisterCallback<ClickEvent>(_ => OnPlayClicked());
            _closeBtn?.RegisterCallback<ClickEvent>(_ => Hide());
        }

        private void OnDestroy()
        {
            L.OnLocaleChanged -= ApplyLocalizedTexts;
        }

        private void ApplyLocalizedTexts()
        {
            if (_titleLabel != null)    _titleLabel.text    = L.Get("daily.title");
            if (_playBtn != null)       _playBtn.text       = L.Get("daily.play_btn");

            RefreshData();
        }

        private void RefreshData()
        {
            var spec = Daily.BuildDailyLevel();
            int seed = Daily.GetDailySeed();
            int bestScore = Daily.GetStoredScore();
            bool playedToday = Daily.HasPlayedToday();

            if (_seedValueLabel != null)
                _seedValueLabel.text = seed.ToString();

            if (_challengeValueLabel != null)
                _challengeValueLabel.text = spec.DisplayName;

            if (_bestValueLabel != null)
                _bestValueLabel.text = bestScore > 0 ? bestScore.ToString() : L.Get("daily.best_score_none");

            if (_playBtn != null)
            {
                _playBtn.SetEnabled(!playedToday);
                _playBtn.text = playedToday ? L.Get("daily.played_today") : L.Get("daily.play_btn");
            }
        }

        private void OnPlayClicked()
        {
            if (Daily.HasPlayedToday()) return;
            Hide();
            LevelLoader.LoadDaily();
        }

        public void Show()
        {
            RefreshData();
            _dailyRoot?.RemoveFromClassList("hidden");
        }

        public void Hide()
        {
            _dailyRoot?.AddToClassList("hidden");
        }
    }
}
