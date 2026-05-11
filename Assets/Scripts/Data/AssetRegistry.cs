#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(menuName = "CrowdDefense/AssetRegistry", fileName = "AssetRegistry")]
    public class AssetRegistry : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string Key;
            public GameObject? Prefab;
        }

        [SerializeField] private Entry[] entries = System.Array.Empty<Entry>();

        private Dictionary<string, GameObject>? _cache;

        public GameObject? Get(string key)
        {
            if (_cache == null) BuildCache();
            _cache!.TryGetValue(key, out var prefab);
            return prefab;
        }

        public bool Has(string key)
        {
            if (_cache == null) BuildCache();
            return _cache!.ContainsKey(key);
        }

        private void BuildCache()
        {
            _cache = new Dictionary<string, GameObject>(entries.Length);
            foreach (var e in entries)
            {
                if (!string.IsNullOrEmpty(e.Key) && e.Prefab != null)
                    _cache[e.Key] = e.Prefab;
            }
        }

        // Called by Unity when the SO is loaded/reloaded in Editor
        private void OnEnable() => _cache = null;
    }
}
