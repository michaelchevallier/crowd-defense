#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(menuName = "CrowdDefense/PerkRegistry", fileName = "PerkRegistry")]
    public class PerkRegistry : ScriptableObject
    {
        [SerializeField] private PerkDef[] standard = System.Array.Empty<PerkDef>();
        [SerializeField] private PerkDef[] schoolPerks = System.Array.Empty<PerkDef>();
        [SerializeField] private PerkSetBonusDef[] setBonuses = System.Array.Empty<PerkSetBonusDef>();

        private Dictionary<string, PerkDef>? _byId;
        private Dictionary<PerkTag, PerkSetBonusDef>? _bonusByTag;

        public PerkDef[] Standard   => standard;
        public PerkDef[] AllSchool  => schoolPerks;
        public PerkSetBonusDef[] AllSetBonuses => setBonuses;

        public PerkDef? Get(string id)
        {
            if (_byId == null) BuildCache();
            _byId!.TryGetValue(id, out var def);
            return def;
        }

        public PerkSetBonusDef? GetBonus(PerkTag t)
        {
            if (_bonusByTag == null) BuildCache();
            _bonusByTag!.TryGetValue(t, out var b);
            return b;
        }

        public IEnumerable<PerkDef> GetSchoolPerks(string schoolId) =>
            schoolPerks.Where(p => p != null && p.school == schoolId);

        private void BuildCache()
        {
            _byId = new Dictionary<string, PerkDef>();
            foreach (var p in standard)
                if (p != null && !string.IsNullOrEmpty(p.id)) _byId[p.id] = p;
            foreach (var p in schoolPerks)
                if (p != null && !string.IsNullOrEmpty(p.id)) _byId[p.id] = p;
            _bonusByTag = setBonuses
                .Where(b => b != null)
                .ToDictionary(b => b.tag);
        }

        private void OnEnable() { _byId = null; _bonusByTag = null; }

        public static PerkRegistry? Load() => Resources.Load<PerkRegistry>("PerkRegistry");

        // Singleton-style accessor for PerkPickerController (Speed/RunMode agent expected this API)
        private static PerkRegistry? _cached;
        public static PerkRegistry? Get()
        {
            if (_cached == null) _cached = Load();
            return _cached;
        }

        public List<PerkDef> GetRandom(int count)
        {
            var pool = new List<PerkDef>();
            for (int i = 0; i < standard.Length; i++)
                if (standard[i] != null) pool.Add(standard[i]);
            var result = new List<PerkDef>();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
