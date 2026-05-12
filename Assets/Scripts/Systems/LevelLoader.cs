#nullable enable
using CrowdDefense.UI;
using UnityEngine.SceneManagement;

namespace CrowdDefense.Systems
{
    public static class LevelLoader
    {
        public static string? NextLevelId { get; set; }

        // Non-null when the next run is a Daily challenge (no LevelData asset needed).
        public static DailyLevelSpec?    NextDailySpec      { get; set; }

        // Non-null when the daily challenge modifier applies (banned tower, gold mul, etc.).
        public static ChallengeSpec? NextDailyChallenge { get; set; }

        public static bool HasHeroChoice()
            => !string.IsNullOrEmpty(UnityEngine.PlayerPrefs.GetString(HeroPickScreen.PrefsKey, ""));

        public static void LoadLevel(string levelId)
        {
            if (!HasHeroChoice())
            {
                HeroPickScreen.Instance?.Show(levelId, () => LoadLevel(levelId));
                return;
            }

            NextLevelId = levelId;
            NextDailySpec = null;
            Fade("Main");
        }

        public static void LoadDailyLevel()
        {
            NextDailySpec     = Daily.BuildDailyLevel();
            NextLevelId       = "daily";
            Fade("Main");
        }

        public static void GoToWorldMap()
        {
            NextLevelId = null;
            Fade("WorldMap");
        }

        public static void GoToMenu()
        {
            NextLevelId = null;
            Fade("Menu");
        }

        static void Fade(string sceneName)
        {
            SceneTransition.EnsureExists();
            SceneTransition.Instance?.LoadSceneFade(sceneName);
        }
    }
}
