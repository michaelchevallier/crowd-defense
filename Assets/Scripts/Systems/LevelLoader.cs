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
            // V8E FIX: AvatarPickPanel + HeroPickScreen UGUI Canvas auto-built at NaN-derived
            // positions (Canvas.ScaleMode mismatch with PanelSettings) so cards land off-screen
            // and user can't click them. Until those panels are wired to scene prefabs with
            // proper layouts, auto-assign defaults on first run so player isn't soft-locked at
            // WorldMap. Defaults: Warrior avatar + first hero in HeroRegistry.
            if (!HasAvatarChoice())
            {
                Debug.Log("[LevelLoader] No avatar choice — auto-assigning Warrior default (V8E bypass)");
                PlayerPrefs.SetString(UI.AvatarPickPanel.PrefsKey, "Warrior");
                PlayerPrefs.Save();
            }

            if (!HasHeroChoice())
            {
                Debug.Log("[LevelLoader] No hero choice — auto-assigning default hero (V8E bypass)");
                PlayerPrefs.SetString(HeroPickScreen.PrefsKey, "BluePill");
                PlayerPrefs.Save();
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

        // Navigate to the RunMap scene (roguelike map between encounters).
        public static void GoToRunMap()
        {
            NextLevelId = null;
            Fade("WorldMap");
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
