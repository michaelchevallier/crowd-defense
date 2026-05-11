#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Singleton MonoBehaviour — spawns floating damage/coin/gems labels above world positions.
    /// Port of Popups.js (V5): spawnDamagePopup / spawnCoinPopup / spawnGemsPopup.
    /// Requires a UIDocument with FloatingPopup.uxml on this GameObject.
    /// Animation: translate Y -40px + opacity 1→0 over 900ms via Coroutine (unscaled time).
    /// Pool of max 50 Label instances, reused FIFO when limit is reached.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class FloatingPopupController : MonoSingleton<FloatingPopupController>
    {
        private const int MaxActive = 50;
        private const float LifetimeS = 0.9f;
        private const float RisePixels = 40f;

        private VisualElement? _overlay;
        private readonly Queue<Label> _pool = new(MaxActive);
        private readonly List<Label> _active = new(MaxActive);

        protected override void OnAwakeSingleton()
        {
            var doc = GetComponent<UIDocument>();
            _overlay = doc.rootVisualElement.Q<VisualElement>("popup-overlay");
        }

        public void SpawnDamage(float dmg, Vector3 worldPos)
            => Spawn($"-{Mathf.RoundToInt(dmg)}", "popup-damage", worldPos);

        public void SpawnCoin(int amount, Vector3 worldPos)
            => Spawn($"+{amount}", "popup-coin", worldPos);

        public void SpawnGems(int amount, Vector3 worldPos)
            => Spawn($"+{amount}", "popup-gems", worldPos);

        private void Spawn(string text, string cssClass, Vector3 worldPos)
        {
            if (_overlay == null) return;

            // World → screen (normalized → pixels)
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 vp = cam.WorldToViewportPoint(worldPos);
            // Behind camera — skip
            if (vp.z < 0f) return;

            float sx = vp.x * Screen.width;
            float sy = (1f - vp.y) * Screen.height;

            var lbl = AcquireLabel(cssClass);
            lbl.text = text;
            // Position in UIDocument panel space (matches screen pixels at scale 1)
            lbl.style.left = new StyleLength(sx);
            lbl.style.top = new StyleLength(sy);
            lbl.style.opacity = 1f;
            lbl.style.translate = new Translate(0, 0);

            _overlay.Add(lbl);
            _active.Add(lbl);

            StartCoroutine(AnimatePopup(lbl, sy));
        }

        private System.Collections.IEnumerator AnimatePopup(Label lbl, float startY)
        {
            float elapsed = 0f;
            while (elapsed < LifetimeS)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / LifetimeS);
                float dy = -RisePixels * t;
                lbl.style.translate = new Translate(0, new Length(dy, LengthUnit.Pixel));
                lbl.style.opacity = 1f - t;
                yield return null;
            }
            ReturnLabel(lbl);
        }

        private Label AcquireLabel(string cssClass)
        {
            // Evict oldest active label if pool is exhausted and cap is reached
            if (_pool.Count == 0 && _active.Count >= MaxActive)
            {
                var oldest = _active[0];
                _active.RemoveAt(0);
                ReturnLabelImmediate(oldest);
            }

            Label lbl;
            if (_pool.Count > 0)
            {
                lbl = _pool.Dequeue();
                // Reset CSS classes
                lbl.ClearClassList();
            }
            else
            {
                lbl = new Label();
            }
            lbl.AddToClassList("floating-popup");
            lbl.AddToClassList(cssClass);
            return lbl;
        }

        private void ReturnLabel(Label lbl)
        {
            _active.Remove(lbl);
            ReturnLabelImmediate(lbl);
        }

        private void ReturnLabelImmediate(Label lbl)
        {
            if (_overlay != null && lbl.parent == _overlay)
                _overlay.Remove(lbl);
            lbl.style.opacity = 0f;
            if (_pool.Count < MaxActive)
                _pool.Enqueue(lbl);
        }
    }
}
