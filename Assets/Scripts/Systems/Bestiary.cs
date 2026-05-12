#nullable enable
using System;
using System.Collections.Generic;
using CrowdDefense.Common;
using UnityEngine;

namespace CrowdDefense.Systems
{
    public class Bestiary : MonoSingleton<Bestiary>
    {
        private const string PrefsKey = "cd.bestiary.kills";

        private readonly Dictionary<string, int> _kills = new();

        public static event Action<string>? OnFirstKill;

        protected override void OnAwakeSingleton() => Load();

        private void Load()
        {
            string raw = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(raw)) return;
            foreach (string entry in raw.Split(';'))
            {
                int sep = entry.IndexOf(':');
                if (sep < 1) continue;
                string id = entry[..sep];
                if (int.TryParse(entry[(sep + 1)..], out int count) && count > 0)
                    _kills[id] = count;
            }
        }

        private void Save()
        {
            var parts = new List<string>(_kills.Count);
            foreach (var kv in _kills)
                parts.Add($"{kv.Key}:{kv.Value}");
            PlayerPrefs.SetString(PrefsKey, string.Join(";", parts));
            PlayerPrefs.Save();
        }

        public void RecordKill(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId)) return;
            bool wasLocked = !IsUnlocked(enemyId);
            _kills[enemyId] = (_kills.TryGetValue(enemyId, out int c) ? c : 0) + 1;
            Save();
            if (wasLocked) OnFirstKill?.Invoke(enemyId);
        }

        public bool IsUnlocked(string id) =>
            !string.IsNullOrEmpty(id) && _kills.TryGetValue(id, out int c) && c >= 1;

        public int KillCount(string id) =>
            _kills.TryGetValue(id, out int c) ? c : 0;

        public IReadOnlyDictionary<string, int> AllKills => _kills;
    }
}
