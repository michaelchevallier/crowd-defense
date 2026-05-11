#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Systems
{
    // Port of V5 Daily.js — per-day seeded challenge.
    // Seed = YYYYMMDD integer → mulberry32 PRNG → reproducible level + waves across all players.
    // Score persistence: PlayerPrefs key "cd.daily.YYYY-MM-DD.score"
    // Reset: UTC midnight (date key changes automatically).
    public static class Daily
    {
        private const string PrefixKey = "cd.daily.";
        private const int DailyCastleHP = 110;
        private const int DailyStartCoins = 250;
        private const int WaveCount = 5;

        // Enemy pools matching V5 source
        private static readonly string[] PoolEasy = { "basic", "basic", "runner" };
        private static readonly string[] PoolMid  = { "basic", "runner", "runner", "brute", "assassin" };
        private static readonly string[] PoolHard  = { "runner", "brute", "assassin", "flyer", "shielded" };
        private static readonly string[] PoolBoss  = { "runner", "brute", "flyer", "shielded", "midboss" };

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        public static string GetDateKey(DateTime? utcDate = null)
        {
            var d = utcDate ?? DateTime.UtcNow;
            return $"{d.Year:D4}-{d.Month:D2}-{d.Day:D2}";
        }

        public static int GetStoredScore(string? dateKey = null)
        {
            dateKey ??= GetDateKey();
            return PlayerPrefs.GetInt(PrefixKey + dateKey + ".score", 0);
        }

        public static void SetScore(int score, string? dateKey = null)
        {
            dateKey ??= GetDateKey();
            string key = PrefixKey + dateKey + ".score";
            if (score > PlayerPrefs.GetInt(key, 0))
            {
                PlayerPrefs.SetInt(key, score);
                PlayerPrefs.Save();
            }
        }

        // Returns true if the player already has a score for today.
        public static bool HasPlayedToday() => GetStoredScore() > 0;

        // Build a runtime-only LevelData-equivalent struct for today's challenge.
        // Returns a DailyLevelSpec (not a ScriptableObject — avoids Editor pollution).
        public static DailyLevelSpec BuildDailyLevel(DateTime? utcDate = null)
        {
            var d = utcDate ?? DateTime.UtcNow;
            int seed = d.Year * 10000 + d.Month * 100 + d.Day;
            var rng = new Mulberry32(seed);

            var waves = new List<DailyWaveSpec>
            {
                BuildWave(rng, PoolEasy, 12, 750, 5000),
                BuildWave(rng, PoolMid,  16, 650, 4500),
                BuildWave(rng, PoolHard, 18, 580, 4000),
                BuildWave(rng, PoolHard, 20, 520, 4000),
                BuildWave(rng, PoolBoss, 22, 450, 0),
            };

            return new DailyLevelSpec
            {
                DateKey    = GetDateKey(d),
                DisplayName = $"Defi du {GetDateKey(d)}",
                Theme      = "plaine",
                CastleHP   = DailyCastleHP,
                StartCoins = DailyStartCoins,
                Briefing   = "Defi du Jour — niveau unique, 1 essai par jour. Bonne chance !",
                Waves      = waves,
            };
        }

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        private static DailyWaveSpec BuildWave(Mulberry32 rng, string[] pool, int baseCount,
            int spawnRateMs, int breakMs)
        {
            int count = baseCount + Mathf.FloorToInt(rng.Next() * 4);
            var entries = new List<DailyEnemyEntry>();
            var tally = new Dictionary<string, int>();
            for (int i = 0; i < count; i++)
            {
                string t = pool[Mathf.FloorToInt(rng.Next() * pool.Length)];
                tally.TryGetValue(t, out int cur);
                tally[t] = cur + 1;
            }
            foreach (var kv in tally)
                entries.Add(new DailyEnemyEntry { TypeId = kv.Key, Count = kv.Value });

            return new DailyWaveSpec
            {
                Entries     = entries,
                SpawnRateMs = spawnRateMs,
                BreakMs     = breakMs,
            };
        }

        // ---------------------------------------------------------------
        // Mulberry32 — same algorithm as V5 mulberry32()
        // ---------------------------------------------------------------
        private struct Mulberry32
        {
            private uint _s;

            public Mulberry32(int seed) => _s = (uint)seed;

            public float Next()
            {
                _s += 0x6D2B79F5u;
                uint t = (_s ^ (_s >> 15)) * (1u | _s);
                t = (t + (t ^ (t >> 7)) * (61u | t)) ^ t;
                return (float)((t ^ (t >> 14)) / 4294967296.0);
            }
        }
    }

    // ---------------------------------------------------------------
    // Plain data structs (not ScriptableObjects — runtime only)
    // ---------------------------------------------------------------

    [Serializable]
    public struct DailyEnemyEntry
    {
        public string TypeId;
        public int Count;
    }

    [Serializable]
    public struct DailyWaveSpec
    {
        public List<DailyEnemyEntry> Entries;
        public int SpawnRateMs;
        public int BreakMs;
    }

    [Serializable]
    public class DailyLevelSpec
    {
        public string DateKey    = "";
        public string DisplayName = "";
        public string Theme      = "plaine";
        public int    CastleHP   = 110;
        public int    StartCoins = 250;
        public string Briefing   = "";
        public List<DailyWaveSpec> Waves = new();
    }
}
