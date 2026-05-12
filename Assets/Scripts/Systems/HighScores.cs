#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;

namespace CrowdDefense.Systems
{
    [Serializable]
    public class HighScore
    {
        public float bestTimeSec;
        public int   maxWaveReached;
        public int   totalKills;
        public string dateLastPlay = "";
    }

    [Serializable]
    internal class HighScoreEntry
    {
        public string    levelId = "";
        public HighScore score   = new();
    }

    [Serializable]
    internal class HighScoreStore
    {
        public List<HighScoreEntry> entries = new();
    }

    public class HighScores : MonoSingleton<HighScores>
    {
        private const string PREFS_KEY = "cd_highscores_v1";

        private readonly Dictionary<string, HighScore> _scores = new();

        protected override void OnAwakeSingleton() => Load();

        public void Record(string levelId, float timeSec, int wave, int kills)
        {
            if (string.IsNullOrEmpty(levelId)) return;

            if (!_scores.TryGetValue(levelId, out var existing))
            {
                _scores[levelId] = new HighScore
                {
                    bestTimeSec    = timeSec,
                    maxWaveReached = wave,
                    totalKills     = kills,
                    dateLastPlay   = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                };
                Save();
                return;
            }

            bool improved = false;
            if (timeSec < existing.bestTimeSec || existing.bestTimeSec <= 0f)
            {
                existing.bestTimeSec = timeSec;
                improved = true;
            }
            if (wave > existing.maxWaveReached)
            {
                existing.maxWaveReached = wave;
                improved = true;
            }
            if (kills > existing.totalKills)
            {
                existing.totalKills = kills;
                improved = true;
            }
            existing.dateLastPlay = DateTime.UtcNow.ToString("yyyy-MM-dd");

            if (improved) Save();
        }

        public HighScore? GetHighScore(string levelId) =>
            _scores.TryGetValue(levelId, out var hs) ? hs : null;

        public static string FormatTime(float seconds)
        {
            int s = Mathf.RoundToInt(seconds);
            int m = s / 60;
            return m > 0 ? $"{m}m {s % 60:D2}s" : $"{s}s";
        }

        private void Load()
        {
            string json = PlayerPrefs.GetString(PREFS_KEY, "");
            if (string.IsNullOrEmpty(json)) return;

            var store = JsonUtility.FromJson<HighScoreStore>(json);
            if (store?.entries == null) return;

            _scores.Clear();
            foreach (var e in store.entries)
                if (!string.IsNullOrEmpty(e.levelId))
                    _scores[e.levelId] = e.score;
        }

        private void Save()
        {
            var store = new HighScoreStore();
            foreach (var kv in _scores)
                store.entries.Add(new HighScoreEntry { levelId = kv.Key, score = kv.Value });

            PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(store));
            PlayerPrefs.Save();
        }
    }
}
