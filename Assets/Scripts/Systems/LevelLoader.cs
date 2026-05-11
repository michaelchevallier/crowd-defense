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

        public static void GoToMenu()
        {
            NextLevelId = null;
            SceneManager.LoadScene("Menu");
        }
    }
}
