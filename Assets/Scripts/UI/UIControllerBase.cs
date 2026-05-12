#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Base class for UI controllers that depend on UIDocument + rootVisualElement.
    /// Provides defensive pattern: ResolveUI() validates null references with error logging,
    /// then calls OnUIReady() to let child initialize.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public abstract class UIControllerBase : MonoBehaviour
    {
        protected UIDocument? UIDoc { get; private set; }
        protected VisualElement? Root { get; private set; }

        /// <summary>
        /// Call in Awake() or Start() to resolve UIDoc and Root.
        /// Returns true if valid, false if either is null (logs error).
        /// </summary>
        protected bool ResolveUI()
        {
            UIDoc = GetComponent<UIDocument>();
            if (UIDoc == null)
            {
                Debug.LogError($"[{GetType().Name}] UIDocument component not found");
                return false;
            }

            Root = UIDoc.rootVisualElement;
            if (Root == null)
            {
                Debug.LogError($"[{GetType().Name}] rootVisualElement is null");
                return false;
            }

            OnUIReady();
            return true;
        }

        /// <summary>
        /// Override in child class to initialize UI elements after ResolveUI succeeds.
        /// Called automatically by ResolveUI() if both UIDoc and Root are valid.
        /// </summary>
        protected virtual void OnUIReady() { }
    }
}
