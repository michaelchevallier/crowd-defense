#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(menuName = "CrowdDefense/AchievementRegistry", fileName = "AchievementRegistry")]
    public class AchievementRegistry : ScriptableObject
    {
        [SerializeField] private AchievementDef[] defs = System.Array.Empty<AchievementDef>();

        private Dictionary<string, AchievementDef>? _cache;

        public AchievementDef[] All => defs;

        public AchievementDef? Get(string id)
        {
            if (_cache == null) BuildCache();
            _cache!.TryGetValue(id, out var def);
            return def;
        }

        public bool Has(string id)
        {
            if (_cache == null) BuildCache();
            return _cache!.ContainsKey(id);
        }

        private void BuildCache()
        {
            _cache = new Dictionary<string, AchievementDef>(defs.Length);
            foreach (var d in defs)
            {
                if (d != null && !string.IsNullOrEmpty(d.id))
                    _cache[d.id] = d;
            }
        }

        private void OnEnable() => _cache = null;
    }
}
