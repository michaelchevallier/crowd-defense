#nullable enable
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    // Per-tower research tree: 3 nodes per TowerType, 1 talent point each.
    // Persisted via PlayerPrefs "cd.research.{towerId}.{node}" (0 or 1).
    // Multipliers are read at tower placement / refresh — no SO mutation.
    //
    // Node definitions per tower (applied cumulatively if multiple unlocked):
    //   node0 = +15% damage
    //   node1 = +10% range
    //   node2 = -10% fire rate interval (faster)
    public static class TowerResearchTree
    {
        public const int NodeCount = 3;

        private const string PrefPrefix = "cd.research.";

        // ── Persistence ──────────────────────────────────────────────────────

        private static string Key(string towerId, int node) =>
            $"{PrefPrefix}{towerId}.{node}";

        public static bool IsUnlocked(string towerId, int node) =>
            PlayerPrefs.GetInt(Key(towerId, node), 0) == 1;

        public static int UnlockedCount(string towerId)
        {
            int count = 0;
            for (int i = 0; i < NodeCount; i++)
                if (IsUnlocked(towerId, i)) count++;
            return count;
        }

        // ── Unlock ───────────────────────────────────────────────────────────

        public static bool CanUnlock(string towerId, int node)
        {
            if (node < 0 || node >= NodeCount) return false;
            if (IsUnlocked(towerId, node)) return false;
            return TalentSystem.AvailablePoints > 0;
        }

        public static bool TryUnlock(string towerId, int node)
        {
            if (!CanUnlock(towerId, node)) return false;
            // Spend one talent point from TalentSystem
            if (!TalentSystem.TrySpendPoint()) return false;
            PlayerPrefs.SetInt(Key(towerId, node), 1);
            PlayerPrefs.Save();
            return true;
        }

        // ── Multipliers applied at tower init / refresh ──────────────────────

        public static float DamageMul(string towerId) =>
            IsUnlocked(towerId, 0) ? 1.15f : 1f;

        public static float RangeMul(string towerId) =>
            IsUnlocked(towerId, 1) ? 1.10f : 1f;

        // Returns fire-rate interval multiplier: < 1 = faster
        public static float FireRateIntervalMul(string towerId) =>
            IsUnlocked(towerId, 2) ? 0.90f : 1f;

        // Reserved for future research node — returns 0 until wired
        public static float CritChanceBonus(string towerId) => 0f;

        // ── Node display helpers ──────────────────────────────────────────────

        public static string NodeLabel(int node) => node switch
        {
            0 => "+15% DGT",
            1 => "+10% Portee",
            2 => "-10% Delai",
            _ => "?",
        };

        public static string NodeDescription(string towerId, int node) => node switch
        {
            0 => $"Degats x1.15 pour {towerId}",
            1 => $"Portee x1.10 pour {towerId}",
            2 => $"Intervalle tir x0.90 pour {towerId}",
            _ => "",
        };
    }
}
