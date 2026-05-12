#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Listens to LevelRunner.OnSummaryReady, shows win/loss stats, wires Retry / Next / Menu buttons.
    // Attach to the same GameObject as the UIDocument for LevelSummary.uxml.
    [RequireComponent(typeof(UIDocument))]
    public class LevelSummaryController : UIControllerBase
    {
        private Label?         _titleLabel;
        private Label?         _starsLabel;
        private Label?         _statsLabel;
        private Label?         _gemsLabel;
        private Button?        _btnRetry;
        private Button?        _btnNext;
        private Button?        _btnMenu;

        // High-score name prompt
        private VisualElement? _namePrompt;
        private Label?         _nameLabel;
        private TextField?     _nameField;
        private Button?        _nameConfirm;

        private LevelResult?   _pendingResult;

        private void Awake()
        {
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            var root = Root;
            if (root == null) return;

            _titleLabel = root.Q<Label>("summary-title");
            _starsLabel = root.Q<Label>("summary-stars");
            _statsLabel = root.Q<Label>("summary-stats");
            _gemsLabel  = root.Q<Label>("summary-gems");
            _btnRetry   = root.Q<Button>("summary-btn-retry");
            _btnNext    = root.Q<Button>("summary-btn-next");
            _btnMenu    = root.Q<Button>("summary-btn-menu");

            _namePrompt  = root.Q<VisualElement>("summary-name-prompt");
            _nameLabel   = root.Q<Label>("summary-name-label");
            _nameField   = root.Q<TextField>("summary-name-field");
            _nameConfirm = root.Q<Button>("summary-name-confirm");

            if (Root != null)        Root.AddToClassList("hidden");
            if (_namePrompt != null) _namePrompt.AddToClassList("hidden");

            if (_btnRetry   != null) _btnRetry.clicked   += OnRetry;
            if (_btnNext    != null) _btnNext.clicked    += OnNext;
            if (_btnMenu    != null) _btnMenu.clicked    += OnMenu;
            if (_nameConfirm != null) _nameConfirm.clicked += OnNameConfirm;

            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnSummaryReady += Show;
        }

        private void OnDestroy()
        {
            if (_btnRetry   != null) _btnRetry.clicked   -= OnRetry;
            if (_btnNext    != null) _btnNext.clicked    -= OnNext;
            if (_btnMenu    != null) _btnMenu.clicked    -= OnMenu;
            if (_nameConfirm != null) _nameConfirm.clicked -= OnNameConfirm;

            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnSummaryReady -= Show;
        }

        private void Show(LevelResult r)
        {
            if (Root == null) return;
            Root.RemoveFromClassList("hidden");
            _pendingResult = r;

            if (_titleLabel != null)
                _titleLabel.text = r.IsVictory ? L.Get("summary.victory") : L.Get("summary.game_over");

            if (_starsLabel != null)
                _starsLabel.text = r.IsVictory ? new string('★', r.StarsEarned) + new string('☆', 3 - r.StarsEarned) : "";

            if (_statsLabel != null)
                _statsLabel.text =
                    $"{L.Get("summary.stat_score")} : {r.Score}\n" +
                    $"{L.Get("summary.stat_kills")} : {r.Kills}\n" +
                    $"{L.Get("summary.stat_towers")} : {r.TowersPlaced}\n" +
                    $"{L.Get("summary.stat_perks")} : {r.PerksAcquired}\n" +
                    $"{L.Get("summary.stat_gold")} : {r.GoldEarned}\n" +
                    $"{L.Get("summary.stat_wave")} : {r.WaveReached}\n" +
                    $"{L.Get("summary.stat_time")} : {FormatTime(r.PlaytimeSeconds)}\n" +
                    $"{L.Get("summary.stat_hp")} : {r.CastleHPRemaining}/{r.CastleHPMax}";

            if (_gemsLabel != null)
                _gemsLabel.text = r.IsVictory && r.GemsRewarded > 0
                    ? $"+{r.GemsRewarded} {L.Get("summary.gems_label")}"
                    : "";

            bool hasNext = !string.IsNullOrEmpty(RunContext.Instance?.NextLevelId);
            if (_btnNext  != null) _btnNext.SetEnabled(r.IsVictory && hasNext);
            if (_btnRetry != null) _btnRetry.SetEnabled(!r.IsVictory);

            // High-score prompt: show when score qualifies for top-10
            int score = ComputeScore(r);
            bool isHighScore = SaveSystem.IsHighScore(score);
            if (_namePrompt != null)
            {
                if (isHighScore)
                {
                    _namePrompt.RemoveFromClassList("hidden");
                    if (_nameLabel != null)  _nameLabel.text  = L.Get("summary.highscore_prompt");
                    if (_nameField != null)  _nameField.value = "";
                    if (_nameConfirm != null) _nameConfirm.text = L.Get("summary.highscore_confirm");
                    // Disable nav buttons until name is confirmed
                    SetNavEnabled(false);
                }
                else
                {
                    _namePrompt.AddToClassList("hidden");
                    SetNavEnabled(true);
                }
            }
        }

        private void OnNameConfirm()
        {
            if (_pendingResult == null) return;
            string name = (_nameField?.value ?? "").Trim();
            if (string.IsNullOrEmpty(name)) name = L.Get("summary.anon_name");
            int score = ComputeScore(_pendingResult);
            SaveSystem.AddLeaderboardEntry(_pendingResult.WaveReached, score, name);
            _namePrompt?.AddToClassList("hidden");
            SetNavEnabled(true);
        }

        private void SetNavEnabled(bool enabled)
        {
            _btnRetry?.SetEnabled(enabled && (_pendingResult?.IsVictory == false));
            _btnMenu?.SetEnabled(enabled);
            if (_btnNext != null)
            {
                bool hasNext = !string.IsNullOrEmpty(RunContext.Instance?.NextLevelId);
                _btnNext.SetEnabled(enabled && (_pendingResult?.IsVictory == true) && hasNext);
            }
        }

        private static int ComputeScore(LevelResult r) => r.Score;

        private void OnRetry()
        {
            var id = LevelRunner.Instance?.CurrentLevel?.Id;
            if (!string.IsNullOrEmpty(id))
                LevelLoader.LoadLevel(id!);
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnNext()
        {
            var nextId = RunContext.Instance?.NextLevelId;
            if (!string.IsNullOrEmpty(nextId))
            {
                RunContext.Instance!.AdvanceLevel(nextId!);
                LevelLoader.LoadLevel(nextId!);
            }
            else
            {
                LevelLoader.GoToWorldMap();
            }
        }

        private void OnMenu() => LevelLoader.GoToWorldMap();

        private static string FormatTime(float seconds)
        {
            int m = (int)(seconds / 60);
            int s = (int)(seconds % 60);
            return $"{m:D2}:{s:D2}";
        }
    }
}
