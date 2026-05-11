#nullable enable

using System;
using System.Collections.Generic;
using CrowdDefense.Common;
using UnityEngine;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Centralized strongly-typed event bus.
    /// Subscribe<T> / Unsubscribe<T> / Publish<T> with handler isolation
    /// (one bad handler never breaks propagation to the rest).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class EventManager : MonoSingleton<EventManager>
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public void Subscribe<T>(Action<T> handler)
        {
            var key = typeof(T);
            if (!_handlers.TryGetValue(key, out var list))
            {
                list = new List<Delegate>();
                _handlers[key] = list;
            }
            if (!list.Contains(handler))
                list.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        public void Publish<T>(T evt)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;

            // Snapshot to allow safe unsubscribe during iteration
            var snapshot = list.ToArray();
            foreach (var d in snapshot)
            {
                try
                {
                    ((Action<T>)d)(evt);
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError($"[EventManager] Handler error for {typeof(T).Name}: {ex}");
#endif
                }
            }
        }

        protected override void OnDestroySingleton()
        {
            _handlers.Clear();
        }
    }
}
