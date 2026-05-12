#nullable enable
using System.Collections;
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
    public sealed class RunSummaryController : UIControllerBase
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
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            _panel    = Root?.Q<VisualElement>("run-summary-panel");
            _title    = Root?.Q<Label>("rs-title");
            _stars    = Root?.Q<Label>("rs-stars");
            _score    = Root?.Q<Label>("rs-score");
            _waves    = Root?.Q<Label>("rs-waves");
            _kills    = Root?.Q<Label>("rs-kills");
            _gold     = Root?.Q<Label>("rs-gold");
            _towers   = Root?.Q<Label>("rs-towers");
            _perks    = Root?.Q<Label>("rs-perks");
            _time     = Root?.Q<Label>("rs-time");
            _btnMenu   = Root?.Q<Button>("rs-btn-menu");
            _btnReplay = Root?.Q<Button>("rs-btn-replay");

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

            // Stars and score start blank — coroutines populate them with animation
            if (_stars != null) _stars.text = "";
            if (_score != null) _score.text = "0";

            int totalWaves = WaveManager.Instance?.TotalWaves > 0
                ? WaveManager.Instance.TotalWaves
                : r.WaveReached;

            if (_waves  != null) _waves.text  = $"{r.WaveReached} / {totalWaves}";
            if (_kills  != null) _kills.text  = r.Kills.ToString();
            if (_gold   != null) _gold.text   = $"{r.GoldEarned}c";
            if (_towers != null) _towers.text = r.TowersPlaced.ToString();
            if (_perks  != null) _perks.text  = r.PerksAcquired.ToString();
            if (_time   != null) _time.text   = FormatTime(r.PlaytimeSeconds);

            StartCoroutine(CountUpScore(r.Score, 1.5f));
            if (r.IsVictory)
                StartCoroutine(AnimateStars(r.StarsEarned));
        }

        private IEnumerator CountUpScore(int target, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 3f); // ease-out cubic
                if (_score != null)
                    _score.text = Mathf.RoundToInt(target * k).ToString();
                yield return null;
            }
            if (_score != null)
                _score.text = target.ToString();
        }

        private IEnumerator AnimateStars(int earned)
        {
            if (_stars == null) yield break;
            int revealed = 0;
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSecondsRealtime(0.3f);
                revealed++;
                int shown = Mathf.Min(revealed, earned);
                _stars.text = new string('★', shown) + new string('☆', 3 - shown);

                if (revealed <= earned)
                    AudioController.Instance?.Play("star");
            }
            // Ensure final state is correct
            _stars.text = new string('★', earned) + new string('☆', 3 - earned);
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
