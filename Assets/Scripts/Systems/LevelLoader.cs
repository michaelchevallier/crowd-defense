#nullable enable
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
            SceneManager.LoadScene("Main");
        }

        public static void LoadDailyLevel()
        {
            NextDailySpec = Daily.BuildDailyLevel();
            NextLevelId   = "daily";
            SceneManager.LoadScene("Main");
        }

        public static void GoToWorldMap()
        {
            NextLevelId = null;
            SceneManager.LoadScene("WorldMap");
        }

        public static void GoToMenu()
        {
            NextLevelId = null;
            SceneManager.LoadScene("Menu");
        }
    }
}
