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

        public int   TotalKills       => PlayerPrefs.GetInt(KeyKills, 0);
        public int   TotalGold        => PlayerPrefs.GetInt(KeyGold, 0);
        public float TotalTimePlayed  => PlayerPrefs.GetFloat(KeyTime, 0f);
        public int   LevelsWon        => PlayerPrefs.GetInt(KeyWins, 0);
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
