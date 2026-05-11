#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(menuName = "CrowdDefense/MetaUpgradeRegistry", fileName = "MetaUpgradeRegistry")]
    public class MetaUpgradeRegistry : ScriptableObject
    {
        private static MetaUpgradeRegistry? _cached;

        public static MetaUpgradeRegistry? Get()
        {
            if (_cached == null) _cached = Resources.Load<MetaUpgradeRegistry>("MetaUpgradeRegistry");
            return _cached;
        }

        [SerializeField] private MetaUpgradeDef[] defs = System.Array.Empty<MetaUpgradeDef>();

        // Tier unlock thresholds: key = tier (1/2/3), value = worlds cleared required
        [SerializeField] public int tier2UnlockWorldsCleared = 1;
        [SerializeField] public int tier3UnlockWorldsCleared = 2;

        [SerializeField] public int resetCostGems = 10;

        private Dictionary<string, MetaUpgradeDef>? _cache;

        public MetaUpgradeDef[] All => defs;

        public MetaUpgradeDef? Get(string id)
        {
            if (_cache == null) BuildCache();
            _cache!.TryGetValue(id, out var def);
            return def;
        }

        public bool IsTierUnlocked(int tier, int worldsCleared) => tier switch
        {
            1 => true,
            2 => worldsCleared >= tier2UnlockWorldsCleared,
            3 => worldsCleared >= tier3UnlockWorldsCleared,
            _ => false,
        };

        private void BuildCache()
        {
            _cache = new Dictionary<string, MetaUpgradeDef>(defs.Length);
            foreach (var d in defs)
                if (d != null && !string.IsNullOrEmpty(d.id))
                    _cache[d.id] = d;
        }

        private void OnEnable() => _cache = null;
    }
}
