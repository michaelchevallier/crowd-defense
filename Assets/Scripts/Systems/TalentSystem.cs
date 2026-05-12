#nullable enable
using UnityEngine;

namespace CrowdDefense.Systems
{
    // Static talent tree: 3 talents, 5 levels each, +5% per level.
    // Persisted via PlayerPrefs keys cd.talent.{key}.lvl (0-5).
    // Multipliers are read at runtime — no SO mutation.
    public static class TalentSystem
    {
        public const int MaxLevel = 5;

        private const string KeyTowerDamage = "tower_damage";
        private const string KeyHeroPower   = "hero_power";
        private const string KeyGoldIncome  = "gold_income";

        private const string PrefPrefix = "cd.talent.";
        private const string PrefSuffix = ".lvl";

        // ── Level accessors ───────────────────────────────────────────────────

        public static int TowerDamageLevel
        {
            get => PlayerPrefs.GetInt(PrefPrefix + KeyTowerDamage + PrefSuffix, 0);
            private set => PlayerPrefs.SetInt(PrefPrefix + KeyTowerDamage + PrefSuffix, value);
        }

        public static int HeroPowerLevel
        {
            get => PlayerPrefs.GetInt(PrefPrefix + KeyHeroPower + PrefSuffix, 0);
            private set => PlayerPrefs.SetInt(PrefPrefix + KeyHeroPower + PrefSuffix, value);
        }

        public static int GoldIncomeLevel
        {
            get => PlayerPrefs.GetInt(PrefPrefix + KeyGoldIncome + PrefSuffix, 0);
            private set => PlayerPrefs.SetInt(PrefPrefix + KeyGoldIncome + PrefSuffix, value);
        }

        // Pending unspent talent points (global, not per-talent).
        private const string PrefPoints = "cd.talent.points";

        public static int AvailablePoints
        {
            get => PlayerPrefs.GetInt(PrefPoints, 0);
            private set => PlayerPrefs.SetInt(PrefPoints, value);
        }

        // ── Multiplier helpers ───────────────────────────────────────────────

        public static float TowerDamageMul   => 1f + 0.05f * TowerDamageLevel;
        public static float HeroPowerMul     => 1f + 0.05f * HeroPowerLevel;
        public static float GoldIncomeMul    => 1f + 0.05f * GoldIncomeLevel;

        // ── Points ───────────────────────────────────────────────────────────

        public static void EarnTalentPoint(int n = 1)
        {
            if (n <= 0) return;
            AvailablePoints += n;
            PlayerPrefs.Save();
        }

        // Spend one point — returns false if none available (used by TowerResearchTree)
        public static bool TrySpendPoint()
        {
            if (AvailablePoints <= 0) return false;
            AvailablePoints--;
            PlayerPrefs.Save();
            return true;
        }

        // ── Upgrade ──────────────────────────────────────────────────────────

        public static bool CanUpgrade(string talentKey)
        {
            if (AvailablePoints <= 0) return false;
            return talentKey switch
            {
                KeyTowerDamage => TowerDamageLevel < MaxLevel,
                KeyHeroPower   => HeroPowerLevel   < MaxLevel,
                KeyGoldIncome  => GoldIncomeLevel  < MaxLevel,
                _              => false,
            };
        }

        public static bool TryUpgrade(string talentKey)
        {
            if (!CanUpgrade(talentKey)) return false;
            switch (talentKey)
            {
                case KeyTowerDamage: TowerDamageLevel++; break;
                case KeyHeroPower:   HeroPowerLevel++;   break;
                case KeyGoldIncome:  GoldIncomeLevel++;  break;
                default: return false;
            }
            AvailablePoints--;
            PlayerPrefs.Save();
            return true;
        }

        // Convenience constants for callers
        public const string Tower  = KeyTowerDamage;
        public const string Hero   = KeyHeroPower;
        public const string Gold   = KeyGoldIncome;
    }
}
