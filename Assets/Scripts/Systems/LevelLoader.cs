#nullable enable
using CrowdDefense.UI;
using UnityEngine.SceneManagement;

namespace CrowdDefense.Systems
{
    public static class LevelLoader
    {
        public static string? NextLevelId { get; set; }

        // Non-null when the next run is a Daily challenge (no LevelData asset needed).
        public static DailyLevelSpec? NextDailySpec { get; set; }

        public static void LoadLevel(string levelId)
        {
            NextLevelId = levelId;
            NextDailySpec = null;
            Fade("Main");
        }

        public static void LoadDailyLevel()
        {
            NextDailySpec = Daily.BuildDailyLevel();
            NextLevelId   = "daily";
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
