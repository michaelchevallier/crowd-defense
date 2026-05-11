#nullable enable
using UnityEngine.SceneManagement;

namespace CrowdDefense.Systems
{
    public static class LevelLoader
    {
        public static string? NextLevelId { get; set; }

        public static void LoadLevel(string levelId)
        {
            NextLevelId = levelId;
            SceneManager.LoadScene("Main");
        }

        // Reserved id — LevelRunner detects "__daily" and loads Daily.BuildDailyLevel() spec.
        public static void LoadDaily()
        {
            NextLevelId = "__daily";
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
