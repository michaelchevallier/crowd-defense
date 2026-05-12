#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    // Loaded from Resources/Schools/ at runtime. Provides Get(id) lookup.
    public static class SchoolRegistry
    {
        private static Dictionary<string, SchoolDef>? _byId;

        public static SchoolDef? Get(string id)
        {
            if (_byId == null) BuildCache();
            _byId!.TryGetValue(id, out var def);
            return def;
        }

        public static SchoolDef[] All()
        {
            if (_byId == null) BuildCache();
            var result = new SchoolDef[_byId!.Count];
            _byId.Values.CopyTo(result, 0);
            return result;
        }

        private static void BuildCache()
        {
            _byId = new Dictionary<string, SchoolDef>();
            var assets = Resources.LoadAll<SchoolDef>("Schools");
            foreach (var s in assets)
                if (s != null && !string.IsNullOrEmpty(s.id))
                    _byId[s.id] = s;
        }
    }
}
