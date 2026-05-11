#nullable enable
using UnityEngine;

namespace CrowdDefense.Common
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        public static T? Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = (T)this;
            OnAwakeSingleton();
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
            OnDestroySingleton();
        }

        protected virtual void OnAwakeSingleton() { }
        protected virtual void OnDestroySingleton() { }
    }
}
