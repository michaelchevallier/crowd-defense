#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Attached to the same GameObject as the HUD UIDocument.
    // Subscribes to LevelRunner.OnSummaryReady and populates the run-summary-panel
    // embedded in HUD.uxml with per-run stats + star rating.
    [RequireComponent(typeof(UIDocument))]
    public sealed class RunSummaryController : MonoBehaviour
    {
        private VisualElement? _panel;
        private Label?         _title;
        private Label?         _stars;
        private Label?         _score;
        private Label?         _waves;
        private Label?         _kills;
        private Label?         _gold;
        private Label?         _towers;
        private Label?         _perks;
        private Label?         _time;
        private Button?        _btnMenu;
        private Button?        _btnReplay;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _panel    = root.Q<VisualElement>("run-summary-panel");
            _title    = root.Q<Label>("rs-title");
            _stars    = root.Q<Label>("rs-stars");
            _score    = root.Q<Label>("rs-score");
            _waves    = root.Q<Label>("rs-waves");
            _kills    = root.Q<Label>("rs-kills");
            _gold     = root.Q<Label>("rs-gold");
            _towers   = root.Q<Label>("rs-towers");
            _perks    = root.Q<Label>("rs-perks");
            _time     = root.Q<Label>("rs-time");
            _btnMenu   = root.Q<Button>("rs-btn-menu");
            _btnReplay = root.Q<Button>("rs-btn-replay");

            if (_btnMenu   != null) _btnMenu.clicked   += OnContinue;
            if (_btnReplay != null) _btnReplay.clicked += OnReplay;

            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnSummaryReady += Show;
        }

        private void OnDestroy()
        {
            if (_btnMenu   != null) _btnMenu.clicked   -= OnContinue;
            if (_btnReplay != null) _btnReplay.clicked -= OnReplay;

            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnSummaryReady -= Show;
        }

        public void Show(LevelResult r)
        {
            if (_panel == null) return;
            _panel.RemoveFromClassList("hidden");

            if (_title != null)
                _title.text = r.IsVictory ? "VICTOIRE !" : "DEFAITE";

            if (_stars != null)
                _stars.text = r.IsVictory
                    ? new string('★', r.StarsEarned) + new string('☆', 3 - r.StarsEarned)
                    : "";

            int totalWaves = WaveManager.Instance?.TotalWaves > 0
                ? WaveManager.Instance.TotalWaves
                : r.WaveReached;

            if (_score  != null) _score.text  = r.Score.ToString();
            if (_waves  != null) _waves.text  = $"{r.WaveReached} / {totalWaves}";
            if (_kills  != null) _kills.text  = r.Kills.ToString();
            if (_gold   != null) _gold.text   = $"{r.GoldEarned}c";
            if (_towers != null) _towers.text = r.TowersPlaced.ToString();
            if (_perks  != null) _perks.text  = r.PerksAcquired.ToString();
            if (_time   != null) _time.text   = FormatTime(r.PlaytimeSeconds);
        }

        private void OnContinue() => LevelLoader.GoToWorldMap();

        private void OnReplay()
        {
            var id = LevelRunner.Instance?.CurrentLevel?.Id;
            if (!string.IsNullOrEmpty(id))
                LevelLoader.LoadLevel(id!);
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private static string FormatTime(float seconds)
        {
            int m = (int)(seconds / 60f);
            int s = (int)(seconds % 60f);
            return $"{m:D2}:{s:D2}";
        }
    }
}
