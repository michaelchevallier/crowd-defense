#nullable enable
using System;
using CrowdDefense.Common;
using CrowdDefense.Data;
using UnityEngine;

namespace CrowdDefense.Systems
{
    public enum ChallengeModifier
    {
        None        = 0,
        NoFrost     = 1,
        NoMage      = 2,
        NoArcher    = 3,
        HalfGold    = 4,
        NoPerks     = 5,
    }

    public sealed class ChallengeSpec
    {
        public int               Level;
        public ChallengeModifier Modifier;
        public float             EnemyHpMul;
        public int               RewardCoins;
        public string            DateKey = "";

        public string Description()
        {
            string modText = Modifier switch
            {
                ChallengeModifier.NoFrost   => "sans tour Frost",
                ChallengeModifier.NoMage    => "sans tour Mage",
                ChallengeModifier.NoArcher  => "sans tour Archer",
                ChallengeModifier.HalfGold  => "or reduit x0.5",
                ChallengeModifier.NoPerks   => "sans perks",
                _                           => "sans restriction",
            };
            string hpText = EnemyHpMul > 1f
                ? $" + ennemis x{EnemyHpMul:F1} HP"
                : "";
            return $"Defi du Jour : Niveau {Level} {modText}{hpText}. Recompense {RewardCoins} pieces.";
        }

        // TowerId banned by this challenge, or TowerId.Unknown if none.
        public TowerId BannedTowerId() => Modifier switch
        {
            ChallengeModifier.NoFrost  => TowerId.Frost,
            ChallengeModifier.NoMage   => TowerId.Mage,
            ChallengeModifier.NoArcher => TowerId.Archer,
            _                          => TowerId.Unknown,
        };

        public bool PerksDisabled() => Modifier == ChallengeModifier.NoPerks;
        public float GoldMul()      => Modifier == ChallengeModifier.HalfGold ? 0.5f : 1f;
    }

    // MonoSingleton so it can be queried from UI and LevelRunner alike.
    public class DailyChallenge : MonoSingleton<DailyChallenge>
    {
        private const string CompletedKeyPrefix = "daily_completed_";
        private const string CompletedKeySuffix = "_v1";

        private ChallengeSpec? _cached;
        private string         _cachedDate = "";

        public ChallengeSpec GetTodayChallenge()
        {
            string today = TodayKey();
            if (_cached != null && _cachedDate == today) return _cached;

            int seed = DateTime.UtcNow.DayOfYear;
            var rng  = new Mulberry32Simple(seed);

            int level      = 1 + Mathf.FloorToInt(rng.Next() * 10);  // 1..10
            int modIndex   = Mathf.FloorToInt(rng.Next() * 6);        // 0..5
            float hpMul    = 1f + Mathf.Round(rng.Next() * 4) * 0.25f; // 1.0..2.0 step 0.25

            _cached = new ChallengeSpec
            {
                Level       = level,
                Modifier    = (ChallengeModifier)modIndex,
                EnemyHpMul  = hpMul,
                RewardCoins = 500,
                DateKey     = today,
            };
            _cachedDate = today;
            return _cached;
        }

        public bool HasCompletedToday()
            => PlayerPrefs.GetInt(CompletedKeyPrefix + TodayKey() + CompletedKeySuffix, 0) == 1;

        public void MarkCompleted()
        {
            PlayerPrefs.SetInt(CompletedKeyPrefix + TodayKey() + CompletedKeySuffix, 1);
            PlayerPrefs.Save();
        }

        private static string TodayKey()
        {
            var d = DateTime.UtcNow;
            return $"{d.Year:D4}-{d.Month:D2}-{d.Day:D2}";
        }

        // Minimal seeded PRNG (same Mulberry32 algo as Daily.cs but as struct value type).
        private struct Mulberry32Simple
        {
            private uint _s;
            public Mulberry32Simple(int seed) => _s = (uint)seed;
            public float Next()
            {
                _s += 0x6D2B79F5u;
                uint t = (_s ^ (_s >> 15)) * (1u | _s);
                t = (t + (t ^ (t >> 7)) * (61u | t)) ^ t;
                return (float)((t ^ (t >> 14)) / 4294967296.0);
            }
        }
    }
}
