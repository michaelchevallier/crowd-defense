#nullable enable
using CrowdDefense.UI;
using UnityEngine;
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

        // Non-null when the next run is an Endless procedural level.
        public static Data.LevelData? NextEndlessSpec { get; set; }

        public static bool HasHeroChoice()
            => !string.IsNullOrEmpty(UnityEngine.PlayerPrefs.GetString(HeroPickScreen.PrefsKey, ""));

        static bool HasAvatarChoice()
            => !string.IsNullOrEmpty(UnityEngine.PlayerPrefs.GetString(UI.AvatarPickPanel.PrefsKey, ""));

        public static void LoadLevel(string levelId)
        {
            if (!HasAvatarChoice())
            {
                UI.AvatarPickPanel.Instance?.Show(() => LoadLevel(levelId));
                return;
            }

            if (!HasHeroChoice())
            {
                HeroPickScreen.Instance?.Show(levelId, () => LoadLevel(levelId));
                return;
            }

            SaveSystem.IsHardcoreRun = false;
            NextLevelId = levelId;
            NextDailySpec = null;
            Fade("Main");
        }

        // Skip avatar/hero pick screens — instant reload for Quick Retry.
        public static void LoadLevelFast(string levelId)
        {
            SaveSystem.IsHardcoreRun = false;
            NextLevelId = levelId;
            NextDailySpec = null;
            Fade("Main", default, 0f);
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

        public static void LoadHardcoreRun()
        {
            SaveSystem.IsHardcoreRun = true;
            GoToWorldMap();
        }

        public static void Fade(string sceneName, Color fadeColor = default, float fadeDur = 0.5f)
        {
            SceneTransition.EnsureExists();
            if (SceneTransition.Instance != null)
            {
                Debug.Log($"[LevelLoader.Fade] Fading to '{sceneName}', Instance ready");
                SceneTransition.Instance.LoadSceneFade(sceneName, fadeColor, fadeDur);
            }
            else
            {
                Debug.LogError($"[LevelLoader.Fade] SceneTransition.Instance is null after EnsureExists() — LoadSceneFade skipped for '{sceneName}'");
            }
        }

        public static void FadeVictory(string sceneName)
            => Fade(sceneName, new Color(1f, 0.84f, 0f), 0.8f);

        public static void FadeDefeat(string sceneName)
            => Fade(sceneName, Color.red, 1.2f);
    }
}
