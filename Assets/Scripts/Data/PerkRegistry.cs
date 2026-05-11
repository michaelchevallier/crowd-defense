#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    // ScriptableObject listing all perks. PerkPickerController calls GetRandom(n).
    // If no SO asset exists yet, GetRandom falls back to hardcoded placeholder ids.
    [CreateAssetMenu(menuName = "CrowdDefense/PerkRegistry", fileName = "PerkRegistry")]
    public class PerkRegistry : ScriptableObject
    {
        [SerializeField] private List<PerkDef> perks = new();

        private static PerkRegistry? _instance;

        public static PerkRegistry? Get()
        {
            if (_instance != null) return _instance;
            _instance = Resources.Load<PerkRegistry>("PerkRegistry");
            return _instance;
        }

        public List<PerkDef> GetRandom(int count)
        {
            if (perks.Count == 0) return new List<PerkDef>();
            var pool = new List<PerkDef>(perks);
            var result = new List<PerkDef>();
            int take = Mathf.Min(count, pool.Count);
            for (int i = 0; i < take; i++)
            {
                int idx = Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
