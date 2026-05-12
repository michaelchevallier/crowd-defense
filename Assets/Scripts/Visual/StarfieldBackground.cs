#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.Visual
{
    // Animated parallax starfield for the Main Menu.
    // Attach to the same GameObject as the UIDocument (or any active GO in Menu.unity).
    // Creates 40 absolute-positioned VisualElements injected BEHIND the existing UI root.
    // 3 depth tiers — near (fast+big), mid, far (slow+small).
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(UIDocument))]
    public class StarfieldBackground : MonoBehaviour
    {
        // --- depth tier config (driftPx/sec, size px, alpha) ---
        private static readonly (float speed, float size, float alpha)[] Tiers =
        {
            (3.0f, 3.0f, 0.85f),   // near
            (1.8f, 2.0f, 0.65f),   // mid
            (0.8f, 1.2f, 0.40f),   // far
        };

        private const int TotalStars = 40;

        private readonly struct Star
        {
            public readonly VisualElement El;
            public readonly float Speed;
            public Star(VisualElement el, float speed) { El = el; Speed = speed; }
        }

        private readonly List<Star> _stars = new(TotalStars);
        private VisualElement? _canvas;
        private float _canvasH;
        private float _canvasW;

        private void Start()
        {
            var doc = GetComponent<UIDocument>();
            var root = doc.rootVisualElement;

            // Canvas is injected at index 0 so it sits behind all existing children.
            _canvas = new VisualElement { name = "starfield-canvas" };
            _canvas.style.position      = Position.Absolute;
            _canvas.style.left          = 0; _canvas.style.top    = 0;
            _canvas.style.right         = 0; _canvas.style.bottom = 0;
            _canvas.style.overflow      = Overflow.Hidden;
            _canvas.pickingMode         = PickingMode.Ignore;
            root.Insert(0, _canvas);

            // Defer star creation one frame so the layout resolves panel dimensions.
            _canvas.RegisterCallback<GeometryChangedEvent>(OnCanvasReady);
        }

        private void OnCanvasReady(GeometryChangedEvent _)
        {
            _canvas!.UnregisterCallback<GeometryChangedEvent>(OnCanvasReady);
            _canvasW = _canvas.resolvedStyle.width;
            _canvasH = _canvas.resolvedStyle.height;

            // Fallback if layout not yet resolved.
            if (_canvasW <= 0f) _canvasW = Screen.width;
            if (_canvasH <= 0f) _canvasH = Screen.height;

            SpawnStars();
        }

        private void SpawnStars()
        {
            var rng = new System.Random(42);
            int perTier = TotalStars / Tiers.Length;

            for (int t = 0; t < Tiers.Length; t++)
            {
                var (speed, size, alpha) = Tiers[t];
                int count = (t == Tiers.Length - 1) ? TotalStars - perTier * t : perTier;

                for (int i = 0; i < count; i++)
                {
                    float x = (float)(rng.NextDouble() * _canvasW);
                    float y = (float)(rng.NextDouble() * _canvasH);

                    var el = new VisualElement();
                    el.pickingMode = PickingMode.Ignore;
                    el.style.position    = Position.Absolute;
                    el.style.width       = size;
                    el.style.height      = size;
                    el.style.borderTopLeftRadius     = size / 2f;
                    el.style.borderTopRightRadius    = size / 2f;
                    el.style.borderBottomLeftRadius  = size / 2f;
                    el.style.borderBottomRightRadius = size / 2f;
                    el.style.backgroundColor = new Color(1f, 1f, 1f, alpha);
                    el.style.left = x;
                    el.style.top  = y;

                    _canvas!.Add(el);
                    _stars.Add(new Star(el, speed));
                }
            }
        }

        private void Update()
        {
            if (_canvas == null || _stars.Count == 0) return;

            float h = _canvasH > 0f ? _canvasH : Screen.height;
            float dt = Time.deltaTime;

            foreach (var star in _stars)
            {
                float top = star.El.resolvedStyle.top + star.Speed * dt;
                if (top > h) top -= h;   // wrap: re-enter from top
                star.El.style.top = top;
            }
        }

        private void OnDestroy()
        {
            _stars.Clear();
        }
    }
}
