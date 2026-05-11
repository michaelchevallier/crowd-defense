#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(fileName = "EventRegistry", menuName = "CrowdDefense/EventRegistry")]
    public class EventRegistry : ScriptableObject
    {
        [SerializeField] private EventDef[] events = System.Array.Empty<EventDef>();

        private static EventRegistry? _instance;

        public static EventRegistry? Get()
        {
            if (_instance == null)
                _instance = Resources.Load<EventRegistry>("EventRegistry");
            return _instance;
        }

        public EventDef? PickRandom(List<string>? excluded = null)
        {
            if (events.Length == 0) return null;

            var pool = new List<EventDef>(events.Length);
            for (int i = 0; i < events.Length; i++)
            {
                var e = events[i];
                if (e == null) continue;
                if (excluded != null && excluded.Contains(e.Id)) continue;
                pool.Add(e);
            }

            if (pool.Count == 0)
            {
                // tous exclus : reset et reprendre dans le pool complet
                for (int i = 0; i < events.Length; i++)
                    if (events[i] != null) pool.Add(events[i]);
            }

            return pool.Count > 0
                ? pool[Random.Range(0, pool.Count)]
                : null;
        }
    }
}
