#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(menuName = "CrowdDefense/BalanceConfig", fileName = "BalanceConfig")]
    public class BalanceConfig : ScriptableObject
    {
        private static BalanceConfig? _cached;

        public static BalanceConfig Get()
        {
            if (_cached == null) _cached = Resources.Load<BalanceConfig>("BalanceConfig");
            return _cached!;
        }

        [Header("Tower scaling")]
        public float TowerDamageMul = 1.6f;
        public float[] LevelScale = { 0.75f, 1.0f, 1.30f };

        [Header("Wave economy")]
        public float SwarmMul = 1.4f;
        public float FloorRewardCampaign = 0.5f;
        public float FloorRewardEndless = 0.7f;
        public float WorldRewardDecay = 0.05f;

        [Header("Castle HP (D1-04)")]
        public float CastleHPBase = 100f;
        public float CastleHPSqrtMul = 50f;
        public int FloorCastleHPW1 = 200;
        public int NoRegenWorldThreshold = 6;

        [Header("Magnet (D1-01 Q3)")]
        public int MagnetCapDefault = 1;
        public int MagnetCapAllowMulti = 2;
        public float MagnetRange = 5f;
        public float MagnetCoinMul = 1.3f;
        public int MagnetCost = 130;

        [Header("Pacing (D1-02)")]
        public int SkipBonusGold = 30;
        public float SkipWindowSeconds = 5f;
        public float StreakBonusPerWave = 0.05f;
        public int StreakCap = 5;
        public int InputDebounceMs = 300;

        [Header("Upgrade (D1-03)")]
        public float UpgradeMulL2 = 1.5f;
        public float UpgradeMulL3 = 2.5f;
        public float SellRefundRatio = 0.8f;

        [Header("Treasure (D1-01 §3.6)")]
        public int TreasureValueMin = 50;
        public int TreasureValueMax = 150;

        [Header("Interest bank (D1-01 §3.5)")]
        public float BankInterestRate = 0.05f;

        // --- Helpers ---

        public float DifficultyMulFor(int world)
        {
            if (world <= 10) return 1f;
            return Mathf.Pow(1.1f, world - 10);
        }

        public int CastleHPFor(int world, int level, float difficultyMul = 1f)
        {
            float formula = CastleHPBase + CastleHPSqrtMul * Mathf.Sqrt(world) * difficultyMul;
            int rounded = Mathf.RoundToInt(formula);
            if (world == 1) return Mathf.Max(rounded, FloorCastleHPW1);
            return rounded;
        }

        public int RollTreasureValue() =>
            Random.Range(TreasureValueMin, TreasureValueMax + 1);

        [System.Serializable]
        public struct WorldPressure
        {
            public int world;
            public float mobHpMul;
            public float mobSpeedMul;
            public float mobCountMul;
        }

        [Header("World pressure table (D1-04)")]
        public WorldPressure[] WorldPressureTable = System.Array.Empty<WorldPressure>();

        public WorldPressure GetPressure(int world)
        {
            foreach (var p in WorldPressureTable)
                if (p.world == world) return p;
            return new WorldPressure { world = world, mobHpMul = 1f, mobSpeedMul = 1f, mobCountMul = 1f };
        }
    }
}
