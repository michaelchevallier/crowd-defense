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
        private static readonly HashSet<string> _loggedMissingKeys = new();

        public GameObject? Get(string key)
        {
            if (_cache == null) BuildCache();
            if (_cache!.TryGetValue(key, out var prefab))
                return prefab;

            // Log missing key once per session with top-3 suggestions
            if (_loggedMissingKeys.Add(key))
            {
                var suggestions = FindTopSimilarKeys(key, 3);
                var suggestionStr = suggestions.Count > 0 ? string.Join(", ", suggestions) : "none";
                Debug.LogError($"[AssetRegistry] MISSING key={key}. Available top-3 similar: {suggestionStr}");
            }
            return null;
        }

        private List<string> FindTopSimilarKeys(string targetKey, int count)
        {
            var results = new List<(string key, int distance)>();
            foreach (var k in _cache!.Keys)
            {
                var dist = LevenshteinDistance(targetKey, k);
                results.Add((k, dist));
            }
            results.Sort((a, b) => a.distance.CompareTo(b.distance));
            var topSimilar = new List<string>();
            for (int i = 0; i < count && i < results.Count; i++)
                topSimilar.Add(results[i].key);
            return topSimilar;
        }

        private static int LevenshteinDistance(string a, string b)
        {
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;
            var d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = System.Math.Min(System.Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            return d[a.Length, b.Length];
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

#if UNITY_EDITOR
        public Entry[] GetAllEntries() => entries;
#endif
    }
}
