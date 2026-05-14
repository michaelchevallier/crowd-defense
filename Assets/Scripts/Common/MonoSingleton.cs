#nullable enable
using UnityEngine;

namespace CrowdDefense.Common
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T? _instance;
        private static int _creationDepth = 0;
        private const int MaxCreationDepth = 5;
        private static bool _destroying = false;

        // Lazy getter : if scene didn't include this singleton, FindFirstObjectByType fallback,
        // then auto-create GameObject as last resort. Prevents silent NullRef when scene setup
        // is incomplete (e.g., post multi-axis swarm merge missing GameObject in Main.unity).
        public static T? Instance
        {
            get
            {
                if (_instance != null) return _instance;
                if (_destroying) return null;

                // Don't auto-create during scene unload — avoid OnDestroy cascade
#if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return null;
#endif
                if (!Application.isPlaying) return null;

                _instance = Object.FindAnyObjectByType<T>();
                if (_instance != null) return _instance;

                // Guard against cascading auto-creation — prevents infinite recursion
                if (_creationDepth >= MaxCreationDepth) return null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[MonoSingleton] {typeof(T).Name} auto-created — missing in scene. Fix scene setup for deterministic init.");
#endif
                _creationDepth++;
                var go = new GameObject($"[Auto] {typeof(T).Name}");
                _instance = go.AddComponent<T>();
                _creationDepth--;
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            _destroying = false;
            if (_instance != null && _instance != this)
            {
                // Destroy only the duplicate component, NOT the GameObject — the GameObject
                // may host many other components (e.g. HUD GO with 22 UI controllers).
                Destroy(this);
                return;
            }
            _instance = (T)this;
            OnAwakeSingleton();
        }

        protected virtual void OnDestroy()
        {
            _destroying = true;
            if (_instance == this) _instance = null;
            OnDestroySingleton();
        }

        protected virtual void OnAwakeSingleton() { }
        protected virtual void OnDestroySingleton() { }
    }
}
