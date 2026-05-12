#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using CrowdDefense.Common;

namespace CrowdDefense.Visual
{
    // World-space billboard token spawned on enemy death.
    // Flies via cubic bezier from death position toward HUD gold pill (screen-space target
    // back-projected to world), then fires CoinBurst + notifies Economy.
    // Managed exclusively by CoinPullManager's ObjectPool — never call Destroy on this.
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public sealed class CoinToken : MonoBehaviour
    {
        private static readonly int _colorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color _coinColor = new Color(1f, 0.85f, 0.15f);
        private static readonly Color _textColor = new Color(1f, 0.95f, 0.55f);

        private MeshRenderer? _rend;
        private Material? _mat;
        private Coroutine? _flyRoutine;
        private IObjectPool<CoinToken>? _pool;

        // Amount label — rendered via Debug.DrawLine substitute (world-space TextMesh)
        private TextMesh? _label;

        private void Awake()
        {
            _rend = GetComponent<MeshRenderer>();
            _mat = BuildCoinMaterial();
            if (_rend != null) _rend.sharedMaterial = _mat;
            BuildMesh();
            _label = BuildLabel();
            gameObject.SetActive(false);
        }

        internal void SetPool(IObjectPool<CoinToken> pool) => _pool = pool;

        // Called by CoinPullManager to launch the animation
        internal void Fly(Vector3 start, Vector3 worldTarget, int amount, float durationSec)
        {
            transform.position = start;
            if (_label != null) _label.text = $"+{amount}";
            if (_flyRoutine != null) StopCoroutine(_flyRoutine);
            _flyRoutine = StartCoroutine(FlyRoutine(start, worldTarget, amount, durationSec));
        }

        private IEnumerator FlyRoutine(Vector3 start, Vector3 end, int amount, float dur)
        {
            // Cubic bezier control points — arc that curves upward then sweeps to target
            float arcHeight = Mathf.Max(1.5f, Vector3.Distance(start, end) * 0.4f);
            Vector3 p1 = start + Vector3.up * arcHeight + Random.insideUnitSphere * 0.5f;
            Vector3 p2 = Vector3.Lerp(start, end, 0.7f) + Vector3.up * arcHeight * 0.4f;

            float elapsed = 0f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float ease = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic

                transform.position = CubicBezier(start, p1, p2, end, ease);

                // Billboard: face camera
                var coinCam = MainCameraCache.Main;
                if (coinCam != null)
                    transform.LookAt(transform.position + coinCam.transform.rotation * Vector3.forward,
                                     coinCam.transform.rotation * Vector3.up);

                // Pulse scale: grow then shrink on arrival
                float scaleCurve = t < 0.8f
                    ? Mathf.Lerp(0.6f, 1.1f, t / 0.8f)
                    : Mathf.Lerp(1.1f, 0.4f, (t - 0.8f) / 0.2f);
                transform.localScale = Vector3.one * scaleCurve * 0.35f;

                // Label fades out in last 30%
                if (_label != null)
                {
                    float alpha = t < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
                    _label.color = new Color(_textColor.r, _textColor.g, _textColor.b, alpha);
                }

                yield return null;
            }

            // Arrival
            transform.position = end;
            VfxPool.Instance?.SpawnCoinBurst(end);
            CrowdDefense.Systems.AudioController.Instance?.Play("coin_pickup", 0.55f);
            JuiceFX.Instance?.Flash(new Color(1f, 0.92f, 0.2f, 0.18f), 120);

            ReturnToPool();
        }

        private void ReturnToPool()
        {
            _flyRoutine = null;
            if (_label != null) _label.text = "";
            transform.localScale = Vector3.one * 0.35f;
            _pool?.Release(this);
        }

        private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            return u * u * u * p0
                 + 3f * u * u * t * p1
                 + 3f * u * t * t * p2
                 + t * t * t * p3;
        }

        private static Material BuildCoinMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Sprites/Default")
                      ?? Shader.Find("Standard")!;
            var mat = new Material(shader) { name = "CoinToken_Mat" };
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.color = _coinColor;
            if (mat.HasProperty(_colorId))
                mat.SetColor(_colorId, _coinColor);
            mat.renderQueue = 3500;
            return mat;
        }

        private void BuildMesh()
        {
            var mf = GetComponent<MeshFilter>();
            if (mf == null) return;
            // Simple diamond quad — 4 verts, 2 tris
            var mesh = new Mesh { name = "CoinDiamond" };
            mesh.vertices = new[]
            {
                new Vector3( 0f,  0.5f, 0f),
                new Vector3( 0.35f, 0f, 0f),
                new Vector3( 0f, -0.5f, 0f),
                new Vector3(-0.35f, 0f, 0f),
            };
            mesh.triangles = new[] { 0, 1, 3, 1, 2, 3 };
            mesh.uv = new[]
            {
                new Vector2(0.5f, 1f),
                new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 0.5f),
            };
            mesh.RecalculateNormals();
            mf.sharedMesh = mesh;
        }

        private TextMesh BuildLabel()
        {
            var labelGo = new GameObject("CoinLabel");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = "";
            tm.fontSize = 48;
            tm.characterSize = 0.05f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = _textColor;
            tm.fontStyle = FontStyle.Bold;
            return tm;
        }
    }
}
