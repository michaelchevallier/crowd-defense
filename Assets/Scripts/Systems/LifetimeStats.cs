#nullable enable
using System;
using System.Collections.Generic;
using CrowdDefense.Common;
using UnityEngine;

namespace CrowdDefense.Systems
{
    [DefaultExecutionOrder(-90)]
    public class LifetimeStats : MonoSingleton<LifetimeStats>
    {
        private const string KeyKills       = "total_kills_lifetime_v1";
        private const string KeyGold        = "total_gold_lifetime_v1";
        private const string KeyTime        = "total_time_played_seconds_v1";
        private const string KeyWins        = "levels_won_lifetime_v1";
        private const string KeyRuns        = "total_runs_lifetime_v1";
        private const string KeyLeaderboard = "cd.leaderboard.scores";
        private const int    MaxLeaderboard = 5;

        // Daily tracking — reset when date changes
        private const string KeyTodayDate  = "cd.today.date";
        private const string KeyTodayRuns  = "cd.today.runs";
        private const string KeyTodayKills = "cd.today.kills";
        private const string KeyTodayTime  = "cd.today.time";

        private static string TodayString() => DateTime.Today.ToString("yyyy-MM-dd");

        private static void EnsureTodayReset()
        {
            var stored = PlayerPrefs.GetString(KeyTodayDate, "");
            if (stored == TodayString()) return;
            PlayerPrefs.SetString(KeyTodayDate,  TodayString());
            PlayerPrefs.SetInt(KeyTodayRuns,  0);
            PlayerPrefs.SetInt(KeyTodayKills, 0);
            PlayerPrefs.SetFloat(KeyTodayTime, 0f);
            PlayerPrefs.Save();
        }

        public static int   TodayRuns  { get { EnsureTodayReset(); return PlayerPrefs.GetInt(KeyTodayRuns, 0); } }
        public static int   TodayKills { get { EnsureTodayReset(); return PlayerPrefs.GetInt(KeyTodayKills, 0); } }
        public static float TodayTime  { get { EnsureTodayReset(); return PlayerPrefs.GetFloat(KeyTodayTime, 0f); } }

        public static void AddTodayRun()
        {
            EnsureTodayReset();
            PlayerPrefs.SetInt(KeyTodayRuns, TodayRuns + 1);
            PlayerPrefs.Save();
        }

        public static void AddTodayKills(int n)
        {
            if (n <= 0) return;
            EnsureTodayReset();
            PlayerPrefs.SetInt(KeyTodayKills, TodayKills + n);
            PlayerPrefs.Save();
        }

        public static void AddTodayTime(float s)
        {
            if (s <= 0f) return;
            EnsureTodayReset();
            PlayerPrefs.SetFloat(KeyTodayTime, TodayTime + s);
            PlayerPrefs.Save();
        }

        [Serializable]
        public class ScoreEntry
        {
            public int    score;
            public string date  = "";
            public int    world;
        }

        [Serializable]
        private class ScoreList { public List<ScoreEntry> entries = new(); }

        public static List<ScoreEntry> GetLeaderboard()
        {
            var json = PlayerPrefs.GetString(KeyLeaderboard, "");
            if (string.IsNullOrEmpty(json)) return new List<ScoreEntry>();
            try { return JsonUtility.FromJson<ScoreList>(json)?.entries ?? new List<ScoreEntry>(); }
            catch { return new List<ScoreEntry>(); }
        }

        public static void RecordScore(int score, string runDate, int world = 0)
        {
            var list = GetLeaderboard();
            list.Add(new ScoreEntry { score = score, date = runDate, world = world });
            list.Sort((a, b) => b.score.CompareTo(a.score));
            if (list.Count > MaxLeaderboard) list.RemoveRange(MaxLeaderboard, list.Count - MaxLeaderboard);
            var wrapper = new ScoreList { entries = list };
            PlayerPrefs.SetString(KeyLeaderboard, JsonUtility.ToJson(wrapper));
            PlayerPrefs.Save();
        }

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
            PlayerPrefs.Save();
        }

        public void AddLevelWon()
        {
            PlayerPrefs.SetInt(KeyWins, LevelsWon + 1);
            PlayerPrefs.Save();
        }

        public void AddRun()
        {
            PlayerPrefs.SetInt(KeyRuns, TotalRuns + 1);
            PlayerPrefs.Save();
        }

        // ── Per-tower lifetime stats ─────────────────────────────────────────
        private static string TowerPlacedKey(string towerType) => $"cd.tower_stats.{towerType}.placed";
        private static string TowerKillsKey(string towerType)  => $"cd.tower_stats.{towerType}.kills";

        public static void AddTowerPlaced(string towerType)
        {
            if (string.IsNullOrEmpty(towerType)) return;
            var key = TowerPlacedKey(towerType);
            PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + 1);
            PlayerPrefs.Save();
        }

        public static void AddTowerKills(string towerType, int n)
        {
            if (string.IsNullOrEmpty(towerType) || n <= 0) return;
            var key = TowerKillsKey(towerType);
            PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + n);
            PlayerPrefs.Save();
        }

        public static int GetTowerPlaced(string towerType) =>
            PlayerPrefs.GetInt(TowerPlacedKey(towerType), 0);

        public static int GetTowerKills(string towerType) =>
            PlayerPrefs.GetInt(TowerKillsKey(towerType), 0);
    }
}
