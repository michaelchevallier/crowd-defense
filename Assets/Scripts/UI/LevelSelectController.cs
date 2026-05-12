#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class LevelSelectController : MonoBehaviour
    {
        private const int WorldCount     = 8;
        private const int LevelsPerWorld = 10;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            var titleLabel = root.Q<Label>("menu-title-label");
            if (titleLabel != null) titleLabel.text = L.Get("menu.game_title");

            RefreshGreeting(root);

            var btnBestiary = root.Q<Button>("btn-open-bestiary");
            if (btnBestiary != null)
                btnBestiary.clicked += () => BestiaryPanel.Instance?.Show();

            var btnAchievements = root.Q<Button>("btn-open-achievements");
            if (btnAchievements != null)
                btnAchievements.clicked += () => AchievementsPanel.Instance?.Show();

            var btnCredits = root.Q<Button>("btn-open-credits");
            if (btnCredits != null)
                btnCredits.clicked += () => CreditsScreen.Instance?.Show();

            var btnStats = root.Q<Button>("btn-open-stats");
            if (btnStats != null)
                btnStats.clicked += () => StatsLifetimePanel.Instance?.Show();

            var versionFooter = root.Q<Label>("menu-version-footer");
            if (versionFooter != null)
            {
                string buildHash = string.IsNullOrEmpty(Application.version) ? "dev" : Application.version;
                versionFooter.text = $"Crowd Defense v6.0 — Unity {Application.unityVersion} — {buildHash}";
            }

            var grid = root.Q<VisualElement>("level-grid");
            if (grid == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[LevelSelectController] level-grid VisualElement not found in UXML.");
#endif
                return;
            }

            AddContinueRow(grid);
            AddEndlessRow(grid);
            AddDailyRow(grid);

            var registry = LevelRegistry.Get();

            for (int w = 1; w <= WorldCount; w++)
            {
                var row = new VisualElement();
                row.AddToClassList("world-row");

                var worldLabel = new Label(L.Get("menu.world_label", w));
                worldLabel.AddToClassList("world-label");
                row.Add(worldLabel);

                for (int l = 1; l <= LevelsPerWorld; l++)
                {
                    string levelId = $"world{w}-{l}";
                    bool unlocked = SaveSystem.IsLevelUnlocked(levelId);
                    bool cleared  = SaveSystem.IsLevelCleared(levelId);

                    var btn = new Button();
                    btn.text = l.ToString();
                    btn.AddToClassList("level-btn");

                    if (cleared)        btn.AddToClassList("cleared");
                    else if (!unlocked) btn.AddToClassList("locked");

                    if (unlocked)
                    {
                        string id = levelId;
                        LevelData? levelData = registry?.FindById(id);
                        if (levelData != null)
                            btn.RegisterCallback<ClickEvent>(_ => OnLevelClicked(id));
                        else
                            btn.SetEnabled(false);
                    }
                    else
                    {
                        btn.SetEnabled(false);
                    }

                    row.Add(btn);
                }

                grid.Add(row);
            }
        }

        private static void RefreshGreeting(VisualElement root)
        {
            var greetingLabel = root.Q<Label>("menu-greeting-label");
            if (greetingLabel == null) return;
            string name = PlayerProfile.Instance?.GetName() ?? "Joueur";
            greetingLabel.text = $"Bonjour, {name}";
        }

        private static void AddContinueRow(VisualElement grid)
        {
            if (!SaveSystem.HasRunState()) return;

            var mid = SaveSystem.LoadRunState();
            if (mid == null || string.IsNullOrEmpty(mid.levelId)) return;

            string display = $"{mid.levelId} V{mid.waveIdx}";
            var row = new VisualElement();
            row.AddToClassList("world-row");

            var btn = new Button();
            btn.text = $"Continuer ({display})";
            btn.AddToClassList("level-btn");
            btn.AddToClassList("continue-btn");

            string savedLevelId = mid.levelId;
            btn.RegisterCallback<ClickEvent>(_ => LevelLoader.LoadLevel(savedLevelId));
            row.Add(btn);

            grid.Add(row);
        }

        private static void OnLevelClicked(string levelId)
        {
            if (DifficultySelector.Instance != null)
                DifficultySelector.Instance.Show(() => LevelLoader.LoadLevel(levelId));
            else
                LevelLoader.LoadLevel(levelId);
        }

        private static void AddEndlessRow(VisualElement grid)
        {
            var row = new VisualElement();
            row.AddToClassList("world-row");

            var label = new Label("Sans Fin");
            label.AddToClassList("world-label");
            row.Add(label);

            var btn = new Button();
            int best = EndlessMode.Instance?.BestWave ?? 0;
            btn.text = best > 0 ? $"Inf. Sans Fin  (Record : vague {best})" : "Inf. Sans Fin";
            btn.AddToClassList("level-btn");
            btn.AddToClassList("endless-btn");
            btn.RegisterCallback<ClickEvent>(_ => EndlessMode.Instance?.StartEndless());
            row.Add(btn);

            grid.Add(row);
        }

        private static void AddDailyRow(VisualElement grid)
        {
            var row = new VisualElement();
            row.AddToClassList("world-row");

            var label = new Label(L.Get("daily.title"));
            label.AddToClassList("world-label");
            row.Add(label);

            var btn = new Button();
            bool playedToday  = Daily.HasPlayedToday();
            bool doneChallenge = DailyChallenge.Instance?.HasCompletedToday() ?? false;
            int  bestScore    = Daily.GetStoredScore();
            string sub = (playedToday || doneChallenge)
                ? $"{L.Get("daily.played_today")} ({L.Get("daily.best_score_label")}: {bestScore})"
                : "Defi Quotidien";
            btn.text = sub;
            btn.AddToClassList("level-btn");
            if (playedToday || doneChallenge) btn.AddToClassList("cleared");

            btn.RegisterCallback<ClickEvent>(_ => DailyChallengeModal.Instance?.Show());
            row.Add(btn);

            grid.Add(row);
        }
    }
}
