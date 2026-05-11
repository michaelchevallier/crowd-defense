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
    public class LevelSummaryController : MonoBehaviour
    {
        private VisualElement? _root;
        private Label?         _titleLabel;
        private Label?         _starsLabel;
        private Label?         _statsLabel;
        private Label?         _gemsLabel;
        private Button?        _btnRetry;
        private Button?        _btnNext;
        private Button?        _btnMenu;

        private void Start()
        {
            var doc  = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            _root       = root.Q<VisualElement>("summary-root");
            _titleLabel = root.Q<Label>("summary-title");
            _starsLabel = root.Q<Label>("summary-stars");
            _statsLabel = root.Q<Label>("summary-stats");
            _gemsLabel  = root.Q<Label>("summary-gems");
            _btnRetry   = root.Q<Button>("summary-btn-retry");
            _btnNext    = root.Q<Button>("summary-btn-next");
            _btnMenu    = root.Q<Button>("summary-btn-menu");

            if (_root != null)
                _root.AddToClassList("hidden");

            if (_btnRetry != null) _btnRetry.clicked += OnRetry;
            if (_btnNext  != null) _btnNext.clicked  += OnNext;
            if (_btnMenu  != null) _btnMenu.clicked  += OnMenu;

            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnSummaryReady += Show;
        }

        private void OnDestroy()
        {
            if (_btnRetry != null) _btnRetry.clicked -= OnRetry;
            if (_btnNext  != null) _btnNext.clicked  -= OnNext;
            if (_btnMenu  != null) _btnMenu.clicked  -= OnMenu;

            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnSummaryReady -= Show;
        }

        private void Show(LevelResult r)
        {
            if (_root == null) return;
            _root.RemoveFromClassList("hidden");

            if (_titleLabel != null)
                _titleLabel.text = r.IsVictory ? "VICTOIRE" : "GAME OVER";

            if (_starsLabel != null)
                _starsLabel.text = r.IsVictory ? new string('★', r.StarsEarned) + new string('☆', 3 - r.StarsEarned) : "";

            if (_statsLabel != null)
                _statsLabel.text =
                    $"Kills : {r.Kills}\n" +
                    $"Tours placees : {r.TowersPlaced}\n" +
                    $"Perks : {r.PerksAcquired}\n" +
                    $"Or gagne : {r.GoldEarned}\n" +
                    $"Vague : {r.WaveReached}\n" +
                    $"Temps : {FormatTime(r.PlaytimeSeconds)}\n" +
                    $"Castle HP : {r.CastleHPRemaining}/{r.CastleHPMax}";

            if (_gemsLabel != null)
                _gemsLabel.text = r.IsVictory && r.GemsRewarded > 0
                    ? $"+{r.GemsRewarded} gemmes"
                    : "";

            bool hasNext = !string.IsNullOrEmpty(RunContext.Instance?.NextLevelId);
            if (_btnNext != null) _btnNext.SetEnabled(r.IsVictory && hasNext);
            if (_btnRetry != null) _btnRetry.SetEnabled(!r.IsVictory);
        }

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
