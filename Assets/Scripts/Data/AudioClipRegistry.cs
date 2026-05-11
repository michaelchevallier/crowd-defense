#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(menuName = "CrowdDefense/AudioClipRegistry", fileName = "AudioClipRegistry")]
    public class AudioClipRegistry : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string Key;
            public AudioClip? Clip;
        }

        [SerializeField] private Entry[] entries = System.Array.Empty<Entry>();

        private Dictionary<string, AudioClip>? _cache;

        public AudioClip? Get(string key)
        {
            if (_cache == null) BuildCache();
            _cache!.TryGetValue(key, out var clip);
            return clip;
        }

        public bool Has(string key)
        {
            if (_cache == null) BuildCache();
            return _cache!.ContainsKey(key);
        }

        private void BuildCache()
        {
            _cache = new Dictionary<string, AudioClip>(entries.Length);
            foreach (var e in entries)
            {
                if (!string.IsNullOrEmpty(e.Key) && e.Clip != null)
                    _cache[e.Key] = e.Clip;
            }
        }

        private void OnEnable() => _cache = null;
    }
}
