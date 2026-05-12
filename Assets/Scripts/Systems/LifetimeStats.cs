#nullable enable
using CrowdDefense.Common;
using UnityEngine;

namespace CrowdDefense.Systems
{
    [DefaultExecutionOrder(-90)]
    public class LifetimeStats : MonoSingleton<LifetimeStats>
    {
        private const string KeyKills   = "total_kills_lifetime_v1";
        private const string KeyGold    = "total_gold_lifetime_v1";
        private const string KeyTime    = "total_time_played_seconds_v1";
        private const string KeyWins    = "levels_won_lifetime_v1";
        private const string KeyRuns    = "total_runs_lifetime_v1";

        // Per-world storage — world ids 1..10
        private static string StarKey(int world)  => $"world_{world}_best_stars_v1";
        private static string ScoreKey(int world) => $"world_{world}_best_score_v1";

        public static int GetWorldStars(int world)     => PlayerPrefs.GetInt(StarKey(world), 0);
        public static int GetWorldHighScore(int world) => PlayerPrefs.GetInt(ScoreKey(world), 0);

        public static void SetWorldResult(int world, int stars, int score)
        {
            if (stars > GetWorldStars(world))
                PlayerPrefs.SetInt(StarKey(world), Mathf.Clamp(stars, 0, 3));
            if (score > GetWorldHighScore(world))
                PlayerPrefs.SetInt(ScoreKey(world), score);
            PlayerPrefs.Save();
        }

        public int   TotalKills       => PlayerPrefs.GetInt(KeyKills, 0);
        public int   TotalGold        => PlayerPrefs.GetInt(KeyGold, 0);
        public float TotalTimePlayed  => PlayerPrefs.GetFloat(KeyTime, 0f);
        public int   LevelsWon        => PlayerPrefs.GetInt(KeyWins, 0);
        public int   TotalRuns        => PlayerPrefs.GetInt(KeyRuns, 0);
        public int   AchievementsUnlocked => Achievements.Instance?.UnlockedCount ?? 0;

        public void AddKill(int n)
        {
            if (n <= 0) return;
            PlayerPrefs.SetInt(KeyKills, TotalKills + n);
            PlayerPrefs.Save();
        }

        public void AddGold(int n)
        {
            if (n <= 0) return;
            PlayerPrefs.SetInt(KeyGold, TotalGold + n);
            PlayerPrefs.Save();
        }

        public void AddTime(float s)
        {
            if (s <= 0f) return;
            PlayerPrefs.SetFloat(KeyTime, TotalTimePlayed + s);
        }

        public void AddLevelWon()
        {
            PlayerPrefs.SetInt(KeyWins, LevelsWon + 1);
            PlayerPrefs.Save();
        }
    }
}
