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
        public float CritChance = 0.05f;
        public float CritDmgMul = 2.0f;

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
        public float MagnetSlowRadius = 3f;

        [Header("Pacing (D1-02)")]
        public int SkipBonusGold = 30;
        public float SkipWindowSeconds = 5f;
        public float StreakBonusPerWave = 0.05f;
        public int StreakCap = 5;
        public int InputDebounceMs = 300;

        [Header("Upgrade (D1-03)")]
        public float UpgradeMulL2 = 1.5f;
        public float UpgradeMulL3 = 2.25f;   // baseCost × 1.5^2 (D1-01)
        public float SellRefundRatio = 0.8f;

        [Header("Treasure (D1-01 §3.6)")]
        public int TreasureValueMin = 50;
        public int TreasureValueMax = 150;
        [Tooltip("Probability (0-1) that a treasure spawns on the path during a wave break.")]
        public float BreakTreasureChance = 0.20f;

        [Header("Interest bank (D1-01 §3.5)")]
        public float BankInterestRate = 0.05f;
        [Tooltip("Max gold awarded as interest in a single wave clear (D1-01 V4 parity).")]
        public int BankInterestGainCap = 500;

        [Header("Wave Variance")]
        public float WaveSpawnVariance = 0.15f;
        public float WaveCountVariance = 0.1f;

        [Header("Combo system (multi-kill streak)")]
        public float ComboWindowSeconds = 2f;
        public int ComboMinKills = 2;
        // Index 0 = no combo, index 1 = x2 kills, index 2 = x3 kills, …
        public float[] ComboMultipliers = { 1f, 1.5f, 2f, 2.5f, 3f };

        [Header("Perk system")]
        public float ForteresseCastleHpMul = 1.5f;
        public float DefaultTowerAuraRange = 8f;

        public float DifficultyMulFor(int world)
        {
            if (world <= 10) return 1f;
            return Mathf.Pow(1.1f, world - 10);
        }

        // D1-04 §3.3 : difficultyMul par level dans le world (W*-8 boss = 1.5)
        public static float LevelDifficultyMul(int level) => level switch
        {
            1 => 1.00f,
            2 => 1.00f,
            3 => 1.05f,
            4 => 1.10f,
            5 => 1.15f,
            6 => 1.15f,
            7 => 1.20f,
            8 => 1.50f,
            _ => 1.00f,
        };

        public int CastleHPFor(int world, int level)
        {
            float dMul = LevelDifficultyMul(level);
            float formula = CastleHPBase + CastleHPSqrtMul * Mathf.Sqrt(world) * dMul;
            int rounded = Mathf.RoundToInt(formula);
            if (world == 1 && level == 1) return Mathf.Max(rounded, FloorCastleHPW1);
            return rounded;
        }

        // D1-04: static helper used as fallback when no LevelData is loaded.
        public static int GetCastleMaxHp(int worldIndex, int levelIndex = 1)
        {
            var cfg = Get();
            float formula = cfg.CastleHPBase + cfg.CastleHPSqrtMul * Mathf.Sqrt(Mathf.Max(worldIndex, 1));
            int rounded = Mathf.RoundToInt(formula);
            if (worldIndex == 1 && levelIndex == 1) return Mathf.Max(rounded, cfg.FloorCastleHPW1);
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

        private void OnEnable() => EnsurePressureTable();

        // Popule la table si vide (lazy init — valeurs D1-04 §3.2 spec canonique)
        public void EnsurePressureTable()
        {
            if (WorldPressureTable != null && WorldPressureTable.Length >= 10) return;
            WorldPressureTable = new WorldPressure[]
            {
                new() { world = 1,  mobHpMul = 1.00f, mobCountMul = 1.00f, mobSpeedMul = 1.00f },
                new() { world = 2,  mobHpMul = 1.10f, mobCountMul = 1.05f, mobSpeedMul = 1.00f },
                new() { world = 3,  mobHpMul = 1.20f, mobCountMul = 1.10f, mobSpeedMul = 1.00f },
                new() { world = 4,  mobHpMul = 1.30f, mobCountMul = 1.15f, mobSpeedMul = 1.00f },
                new() { world = 5,  mobHpMul = 1.40f, mobCountMul = 1.20f, mobSpeedMul = 1.05f },
                new() { world = 6,  mobHpMul = 1.65f, mobCountMul = 1.30f, mobSpeedMul = 1.10f },
                new() { world = 7,  mobHpMul = 1.85f, mobCountMul = 1.35f, mobSpeedMul = 1.10f },
                new() { world = 8,  mobHpMul = 2.05f, mobCountMul = 1.40f, mobSpeedMul = 1.12f },
                new() { world = 9,  mobHpMul = 2.25f, mobCountMul = 1.45f, mobSpeedMul = 1.15f },
                new() { world = 10, mobHpMul = 2.50f, mobCountMul = 1.50f, mobSpeedMul = 1.18f },
            };
        }

        public WorldPressure GetPressure(int world)
        {
            EnsurePressureTable();
            foreach (var p in WorldPressureTable)
                if (p.world == world) return p;
            return new WorldPressure { world = world, mobHpMul = 1f, mobSpeedMul = 1f, mobCountMul = 1f };
        }
    }
}
