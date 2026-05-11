#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class FloatingPopupController : MonoSingleton<FloatingPopupController>
    {
        private const int   MaxActive    = 30;
        private const float LifetimeS    = 0.9f;
        private const float RisePixels   = 40f;
        private const float StackWindowS = 0.1f;

        private VisualElement? _overlay;
        private readonly Queue<Label> _pool   = new(MaxActive);
        private readonly List<Label>  _active = new(MaxActive);

        private readonly Dictionary<(int, string), StackEntry> _stacks = new();

        private struct StackEntry
        {
            public Label Lbl;
            public float AccumDmg;
            public float SpawnTime;
        }

        protected override void OnAwakeSingleton()
        {
            var doc = GetComponent<UIDocument>();
            _overlay = doc.rootVisualElement.Q<VisualElement>("popup-overlay");
        }

        public void SpawnDamage(float dmg, Vector3 worldPos, int enemyId = 0)
            => SpawnStacked(Mathf.RoundToInt(dmg), "popup-damage", worldPos, enemyId, "-");

        public void SpawnCrit(float dmg, Vector3 worldPos, int enemyId = 0)
            => SpawnStacked(Mathf.RoundToInt(dmg), "popup-crit", worldPos, enemyId, "!");

        public void SpawnCoin(int amount, Vector3 worldPos)
            => Spawn($"+{amount}g", "popup-coin", worldPos);

        public void SpawnHeal(int amount, Vector3 worldPos)
            => Spawn($"+{amount}", "popup-heal", worldPos);

        public void SpawnReject(string text, Vector3 worldPos)
            => Spawn(text, "popup-reject", worldPos);

        public void SpawnGems(int amount, Vector3 worldPos)
            => Spawn($"+{amount}", "popup-gems", worldPos);

        private void SpawnStacked(int value, string cssClass, Vector3 worldPos, int enemyId, string prefix)
        {
            if (_overlay == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 vp = cam.WorldToViewportPoint(worldPos);
            if (vp.z < 0f) return;

            float sx = vp.x * Screen.width;
            float sy = (1f - vp.y) * Screen.height;

            var key = (enemyId, cssClass);
            float now = Time.unscaledTime;

            if (enemyId != 0
                && _stacks.TryGetValue(key, out var entry)
                && now - entry.SpawnTime < StackWindowS
                && _active.Contains(entry.Lbl))
            {
                entry.AccumDmg += value;
                entry.Lbl.text  = BuildText(prefix, Mathf.RoundToInt(entry.AccumDmg));
                _stacks[key]    = entry;
                return;
            }

            var lbl = AcquireLabel(cssClass);
            lbl.text = BuildText(prefix, value);
            lbl.style.left      = new StyleLength(sx);
            lbl.style.top       = new StyleLength(sy);
            lbl.style.opacity   = 1f;
            lbl.style.translate = new Translate(0, 0);

            _overlay.Add(lbl);
            _active.Add(lbl);

            if (enemyId != 0)
                _stacks[key] = new StackEntry { Lbl = lbl, AccumDmg = value, SpawnTime = now };

            StartCoroutine(AnimatePopup(lbl));
        }

        private static string BuildText(string prefix, int val)
            => prefix == "!" ? $"CRIT {val}!" : $"{prefix}{val}";

        private void Spawn(string text, string cssClass, Vector3 worldPos)
        {
            if (_overlay == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 vp = cam.WorldToViewportPoint(worldPos);
            if (vp.z < 0f) return;

            float sx = vp.x * Screen.width;
            float sy = (1f - vp.y) * Screen.height;

            var lbl = AcquireLabel(cssClass);
            lbl.text = text;
            lbl.style.left      = new StyleLength(sx);
            lbl.style.top       = new StyleLength(sy);
            lbl.style.opacity   = 1f;
            lbl.style.translate = new Translate(0, 0);

            _overlay.Add(lbl);
            _active.Add(lbl);
            StartCoroutine(AnimatePopup(lbl));
        }

        private IEnumerator AnimatePopup(Label lbl)
        {
            float elapsed = 0f;
            while (elapsed < LifetimeS)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = Mathf.Clamp01(elapsed / LifetimeS);
                float dy = -RisePixels * t;
                lbl.style.translate = new Translate(0, new Length(dy, LengthUnit.Pixel));
                lbl.style.opacity   = 1f - t;
                yield return null;
            }
            ReturnLabel(lbl);
        }

        private Label AcquireLabel(string cssClass)
        {
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
