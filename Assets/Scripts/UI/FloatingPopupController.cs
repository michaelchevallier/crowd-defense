#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using TMPro;
using CrowdDefense.Common;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class FloatingPopupController : MonoSingleton<FloatingPopupController>
    {
        // ── UI Toolkit (coins / heals / gems) ────────────────────────────────
        private const int   MaxActive    = 30;
        private const float LifetimeS    = 0.9f;
        private const float RisePixels   = 40f;
        private VisualElement? _overlay;
        private readonly Queue<Label> _pool   = new(MaxActive);
        private readonly List<Label>  _active = new(MaxActive);

        // ── World-space 3D popup pool ─────────────────────────────────────────
        private const int   MaxWorld      = 50;
        private const float WorldLifetime = 0.9f;
        private const float WorldRise     = 1.0f;
        private const float PunchDuration = 0.2f;
        private const float PunchScale    = 1.2f;

        private static readonly Color ColorDamage = new Color(1f, 0.28f, 0.28f);
        private static readonly Color ColorCrit   = new Color(1f, 0.90f, 0.18f);

        private readonly Queue<WorldPopup> _worldPool   = new(MaxWorld);
        private readonly List<WorldPopup>  _worldActive = new(MaxWorld);

        private sealed class WorldPopup
        {
            public GameObject Go    = null!;
            public TextMeshPro Tmp  = null!;
            public bool        Alive;
        }

        protected override void OnAwakeSingleton()
        {
            var doc = GetComponent<UIDocument>();
            _overlay = doc.rootVisualElement.Q<VisualElement>("popup-overlay");

            for (int i = 0; i < MaxWorld; i++)
                _worldPool.Enqueue(CreateWorldPopup());
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SpawnDamage(float dmg, Vector3 worldPos, int enemyId = 0)
            => SpawnWorld($"-{Mathf.RoundToInt(dmg)}", worldPos, ColorDamage);

        public void SpawnCrit(float dmg, Vector3 worldPos, int enemyId = 0)
            => SpawnWorld($"CRIT {Mathf.RoundToInt(dmg)}!", worldPos, ColorCrit);

        public void SpawnCoin(int amount, Vector3 worldPos)
            => Spawn($"+{amount}g", "popup-coin", worldPos);

        public void SpawnHeal(int amount, Vector3 worldPos)
            => Spawn($"+{amount}", "popup-heal", worldPos);

        public void SpawnGems(int amount, Vector3 worldPos)
            => Spawn($"+{amount}", "popup-gems", worldPos);

        public void SpawnReward(string text, Vector3 worldPos, Color color)
            => SpawnWorld(text, worldPos, color);

        // ── World-space 3D implementation ─────────────────────────────────────

        private void SpawnWorld(string text, Vector3 worldPos, Color color)
        {
            var wp = AcquireWorldPopup();
            wp.Alive = true;
            wp.Tmp.text  = text;
            wp.Tmp.color = color;
            wp.Go.transform.position = worldPos + Vector3.up * 0.5f;
            wp.Go.transform.localScale = Vector3.zero;
            wp.Go.SetActive(true);
            _worldActive.Add(wp);
            StartCoroutine(AnimateWorldPopup(wp));
        }

        private IEnumerator AnimateWorldPopup(WorldPopup wp)
        {
            var cam = Camera.main;
            float elapsed = 0f;

            // Punch-in : scale 0 → 1.2 → 1 sur PunchDuration
            while (elapsed < PunchDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / PunchDuration);
                float s = t < 0.5f
                    ? Mathf.Lerp(0f, PunchScale, t * 2f)
                    : Mathf.Lerp(PunchScale, 1f, (t - 0.5f) * 2f);
                wp.Go.transform.localScale = Vector3.one * s;
                BillboardToCamera(wp.Go.transform, cam);
                yield return null;
            }

            wp.Go.transform.localScale = Vector3.one;

            // Float + fade sur le reste de la durée
            Vector3 startPos = wp.Go.transform.position;
            float remaining  = WorldLifetime - PunchDuration;
            elapsed = 0f;

            while (elapsed < remaining)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / remaining);
                wp.Go.transform.position = startPos + Vector3.up * (WorldRise * t);
                var c = wp.Tmp.color;
                c.a = 1f - t;
                wp.Tmp.color = c;
                BillboardToCamera(wp.Go.transform, cam);
                yield return null;
            }

            ReturnWorldPopup(wp);
        }

        private static void BillboardToCamera(Transform t, Camera? cam)
        {
            if (cam == null) return;
            t.rotation = cam.transform.rotation;
        }

        private WorldPopup AcquireWorldPopup()
        {
            if (_worldPool.Count == 0 && _worldActive.Count >= MaxWorld)
            {
                var oldest = _worldActive[0];
                _worldActive.RemoveAt(0);
                ReturnWorldPopup(oldest);
            }

            if (_worldPool.Count > 0)
            {
                var wp = _worldPool.Dequeue();
                var c = wp.Tmp.color;
                c.a = 1f;
                wp.Tmp.color = c;
                wp.Go.transform.position = Vector3.zero;
                wp.Go.transform.localScale = Vector3.one;
                return wp;
            }

            return CreateWorldPopup();
        }

        private WorldPopup CreateWorldPopup()
        {
            var go = new GameObject("WorldPopup");
            go.hideFlags = HideFlags.HideInHierarchy;
            go.SetActive(false);

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.fontSize              = 4f;
            tmp.fontStyle             = FontStyles.Bold;
            tmp.alignment             = TextAlignmentOptions.Center;
            tmp.outlineWidth          = 0.2f;
            tmp.outlineColor          = new Color32(0, 0, 0, 255);
            tmp.enableWordWrapping    = false;
            tmp.autoSizeTextContainer = false;
            tmp.rectTransform.sizeDelta = new Vector2(4f, 1.5f);

            return new WorldPopup { Go = go, Tmp = tmp, Alive = false };
        }

        private void ReturnWorldPopup(WorldPopup wp)
        {
            wp.Alive = false;
            wp.Go.SetActive(false);
            _worldActive.Remove(wp);
            if (_worldPool.Count < MaxWorld)
                _worldPool.Enqueue(wp);
        }

        // ── UI Toolkit implementation (coins / heals / gems) ─────────────────

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
