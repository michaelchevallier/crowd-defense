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
            public GameObject Go        = null!;
            public TextMeshPro Tmp      = null!;
            public bool        Alive;
            public float       BaseScale = 1f;
        }

        protected override void OnAwakeSingleton()
        {
            ResolveUIWithFallback();

            for (int i = 0; i < MaxWorld; i++)
                _worldPool.Enqueue(CreateWorldPopup());
        }

        private void ResolveUIWithFallback()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null)
            {
                Debug.LogError("[FloatingPopupController] UIDocument not found on this GameObject — trying HUD parent");
                var hudObj = FindAnyObjectByType<HudController>();
                if (hudObj != null)
                    doc = hudObj.GetComponent<UIDocument>();
                if (doc == null)
                {
                    Debug.LogError("[FloatingPopupController] Could not find HUD UIDocument — popups disabled");
                    return;
                }
            }
            var root = doc.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[FloatingPopupController] rootVisualElement is null — UXML failed to load");
                return;
            }
            _overlay = root.Q<VisualElement>("popup-overlay");
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SpawnDamage(float dmg, Vector3 worldPos, int enemyId = 0)
            => SpawnWorld($"-{Mathf.RoundToInt(dmg)}", worldPos, ColorDamage);

        public void SpawnCrit(float dmg, Vector3 worldPos, int enemyId = 0)
            => SpawnWorld($"CRIT {Mathf.RoundToInt(dmg)}!", worldPos, ColorCrit);

        public void SpawnCoin(int amount, Vector3 worldPos)
            => Spawn($"+{amount}g", "popup-coin", worldPos);

        public void SpawnBankInterest(int amount, Vector3 worldPos)
            => Spawn($"+{amount} interet banque", "popup-bank", worldPos);

        public void SpawnHeal(int amount, Vector3 worldPos)
            => Spawn($"+{amount}", "popup-heal", worldPos);

        public void SpawnGems(int amount, Vector3 worldPos)
            => Spawn($"+{amount}", "popup-gems", worldPos);

        public void SpawnReward(string text, Vector3 worldPos, Color color)
            => SpawnWorld(text, worldPos, color, 1f);

        public void SpawnReward(string text, Vector3 worldPos, Color color, float scale)
            => SpawnWorld(text, worldPos, color, scale);

        // Tiered gold reward popup: < 10 → white 0.6, 10–30 → yellow 0.8, > 30 → gold 1.0 + sparkle
        public void SpawnGoldReward(int amount, Vector3 worldPos)
        {
            if (Time.timeScale <= 0f) return;
            Color  color;
            float  scale;
            bool   sparkle;
            if (amount < 10)
            {
                color   = Color.white;
                scale   = 0.6f;
                sparkle = false;
            }
            else if (amount <= 30)
            {
                color   = new Color(1f, 0.92f, 0.15f);
                scale   = 0.8f;
                sparkle = false;
            }
            else
            {
                color   = new Color(1f, 0.78f, 0f);
                scale   = 1.0f;
                sparkle = true;
            }
            SpawnWorld($"+{amount}", worldPos + Vector3.up * 1.0f, color, scale, sparkle);
        }

        // Screen-space popup — sx/sy in pixels (top-left origin). Used for HUD skip bonus.
        public void SpawnAtScreenPos(string text, string cssClass, float sx, float sy)
        {
            if (_overlay == null) return;
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

        // ── World-space 3D implementation ─────────────────────────────────────

        private void SpawnWorld(string text, Vector3 worldPos, Color color, float scale = 1f, bool sparkle = false)
        {
            var wp = AcquireWorldPopup();
            wp.Alive       = true;
            wp.Tmp.text    = text;
            wp.Tmp.color   = color;
            wp.BaseScale   = scale;
            wp.Go.transform.position   = worldPos + Vector3.up * 0.5f;
            wp.Go.transform.localScale = Vector3.zero;
            wp.Go.SetActive(true);
            _worldActive.Add(wp);
            if (sparkle) SpawnSparkle(worldPos);
            StartCoroutine(AnimateWorldPopup(wp));
        }

        private void SpawnSparkle(Vector3 worldPos)
        {
            for (int i = 0; i < 6; i++)
            {
                var offset = new Vector3(
                    UnityEngine.Random.Range(-0.4f, 0.4f),
                    UnityEngine.Random.Range(0.8f, 1.8f),
                    UnityEngine.Random.Range(-0.4f, 0.4f));
                CrowdDefense.Visual.VfxPool.Instance?.SpawnSpark(worldPos + offset, new Color(1f, 0.85f, 0.1f, 0.9f));
            }
        }

        private IEnumerator AnimateWorldPopup(WorldPopup wp)
        {
            var cam = Camera.main;
            float elapsed = 0f;
            float bs = wp.BaseScale;

            // Punch-in : scale 0 → PunchScale*bs → bs sur PunchDuration
            while (elapsed < PunchDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / PunchDuration);
                float s = t < 0.5f
                    ? Mathf.Lerp(0f, PunchScale * bs, t * 2f)
                    : Mathf.Lerp(PunchScale * bs, bs, (t - 0.5f) * 2f);
                wp.Go.transform.localScale = Vector3.one * s;
                BillboardToCamera(wp.Go.transform, cam);
                yield return null;
            }

            wp.Go.transform.localScale = Vector3.one * bs;

            // Float + fade sur le reste de la durée (0.8s total rise matching brief)
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
                wp.Go.transform.position   = Vector3.zero;
                wp.Go.transform.localScale = Vector3.one;
                wp.BaseScale               = 1f;
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
            if (tmp != null)
            {
                tmp.fontSize              = 4f;
                tmp.fontStyle             = FontStyles.Bold;
                tmp.alignment             = TextAlignmentOptions.Center;
                tmp.textWrappingMode      = TMPro.TextWrappingModes.NoWrap;
                tmp.autoSizeTextContainer = false;
                tmp.rectTransform.sizeDelta = new Vector2(4f, 1.5f);
                // Safe outline setup — skip if shader doesn't support it
                try { tmp.outlineWidth = 0.2f; } catch { }
                try { tmp.outlineColor = new Color32(0, 0, 0, 255); } catch { }
            }

            return new WorldPopup { Go = go, Tmp = tmp!, Alive = false };
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
