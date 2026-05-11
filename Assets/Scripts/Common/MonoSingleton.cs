#nullable enable
using UnityEngine;

namespace CrowdDefense.Common
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T? _instance;

        // Lazy getter : if scene didn't include this singleton, FindFirstObjectByType fallback,
        // then auto-create GameObject as last resort. Prevents silent NullRef when scene setup
        // is incomplete (e.g., post multi-axis swarm merge missing GameObject in Main.unity).
        public static T? Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = Object.FindFirstObjectByType<T>();
                if (_instance != null) return _instance;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[MonoSingleton] {typeof(T).Name} auto-created — missing in scene. Fix scene setup for deterministic init.");
#endif
                var go = new GameObject($"[Auto] {typeof(T).Name}");
                _instance = go.AddComponent<T>();
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = (T)this;
            OnAwakeSingleton();
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
            OnDestroySingleton();
        }

        protected virtual void OnAwakeSingleton() { }
        protected virtual void OnDestroySingleton() { }
    }
}
