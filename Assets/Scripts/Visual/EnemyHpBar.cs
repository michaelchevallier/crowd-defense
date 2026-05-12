#nullable enable
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Entities;

namespace CrowdDefense.Visual
{
    // World-space HP bar billboard, attached to Enemy GO at pool creation.
    // Polls Enemy.HpRatio each LateUpdate — zero per-frame alloc.
    // Hidden when HP is full; color transitions green → orange → red.
    [RequireComponent(typeof(Enemy))]
    public class EnemyHpBar : MonoBehaviour
    {
        // ── Layout constants ────────────────────────────────────────────────────
        private const float BarW    = 0.8f;
        private const float BarH    = 0.08f;
        private const float BarY    = 1.4f;   // local Y above model base
        private const float BarAlpha = 0.5f;

        // ── State ───────────────────────────────────────────────────────────────
        private Enemy?       _enemy;
        private Transform?   _bgT;
        private Transform?   _fillT;
        private MeshRenderer? _fillRend;
        private MaterialPropertyBlock? _mpb;
        private float        _fillBaseScaleX;
        private float        _lastRatio = 1f;
        private static readonly int _colorId = Shader.PropertyToID("_BaseColor");

        // ── Build ────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _mpb   = new MaterialPropertyBlock();
            BuildQuads();
            SetVisible(false);
        }

        private void BuildQuads()
        {
            // Background (dark, slightly wider)
            var bgGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgGo.name = "EnemyHpBar_BG";
            Object.Destroy(bgGo.GetComponent<MeshCollider>());
            bgGo.transform.SetParent(transform, false);
            bgGo.transform.localPosition = new Vector3(0f, BarY, 0f);
            bgGo.transform.localScale    = new Vector3(BarW, BarH, 1f);
            var bgRend = bgGo.GetComponent<MeshRenderer>();
            bgRend.material     = BuildUnlit(new Color(0f, 0f, 0f, BarAlpha * 0.85f), transparent: true);
            bgRend.sortingOrder = 3;
            bgRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bgRend.receiveShadows    = false;
            _bgT = bgGo.transform;

            // Fill (green, alpha 0.5)
            var fillGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fillGo.name = "EnemyHpBar_Fill";
            Object.Destroy(fillGo.GetComponent<MeshCollider>());
            fillGo.transform.SetParent(transform, false);
            _fillBaseScaleX = BarW * 0.92f;
            fillGo.transform.localPosition = new Vector3(0f, BarY, -0.01f);
            fillGo.transform.localScale    = new Vector3(_fillBaseScaleX, BarH * 0.65f, 1f);
            _fillRend = fillGo.GetComponent<MeshRenderer>();
            _fillRend.material     = BuildUnlit(new Color(0.27f, 0.87f, 0.27f, BarAlpha), transparent: true);
            _fillRend.sortingOrder = 4;
            _fillRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _fillRend.receiveShadows    = false;
            _fillT = fillGo.transform;
        }

        // ── LateUpdate — face camera + refresh ──────────────────────────────────

        private void LateUpdate()
        {
            if (_enemy == null) return;

            float ratio = _enemy.HpRatio;

            // Hide when full HP (or freshly spawned / dead)
            if (ratio >= 0.9999f || _enemy.IsDead)
            {
                if (_lastRatio < 0.9999f) SetVisible(false);
                _lastRatio = ratio;
                return;
            }

            if (_lastRatio >= 0.9999f) SetVisible(true);
            _lastRatio = ratio;

            RefreshFill(ratio);
            FaceCamera();
        }

        private void RefreshFill(float ratio)
        {
            if (_fillT == null || _fillRend == null || _mpb == null) return;

            // Scale fill and shift pivot so it shrinks from the right
            float newScaleX = _fillBaseScaleX * ratio;
            var fScale = _fillT.localScale;
            _fillT.localScale = new Vector3(newScaleX, fScale.y, fScale.z);

            var fPos = _fillT.localPosition;
            _fillT.localPosition = new Vector3(
                (_fillBaseScaleX * (ratio - 1f)) / 2f,
                fPos.y,
                fPos.z);

            // Color: green > 60%, orange > 30%, red otherwise (at opacity BarAlpha)
            Color fill = ratio > 0.6f
                ? new Color(0.27f, 0.87f, 0.27f, BarAlpha)
                : ratio > 0.3f
                    ? new Color(1f, 0.67f, 0.13f, BarAlpha)
                    : new Color(1f, 0.2f, 0.13f, BarAlpha);

            _mpb.SetColor(_colorId, fill);
            _fillRend.SetPropertyBlock(_mpb);
        }

        private void FaceCamera()
        {
            var cam = MainCameraCache.Main;
            if (cam == null) return;
            // Rotate both quads to face camera — they share the same parent transform local rotation
            var dir = cam.transform.position - transform.position;
            if (dir.sqrMagnitude < 0.001f) return;

            // Billboard: only rotate the bar children (BG + Fill), not the enemy model
            Quaternion look = Quaternion.LookRotation(-dir.normalized);
            if (_bgT   != null) _bgT.rotation   = look;
            if (_fillT != null) _fillT.rotation = look;
        }

        // ── Visibility helpers ──────────────────────────────────────────────────

        private void SetVisible(bool visible)
        {
            if (_bgT != null)   _bgT.gameObject.SetActive(visible);
            if (_fillT != null) _fillT.gameObject.SetActive(visible);
        }

        // ── Material builder (mirrors Castle.BuildUnlitMaterial) ────────────────

        private static Material BuildUnlit(Color color, bool transparent)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader ?? Shader.Find("Standard")!);
            mat.color = color;
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
            }
            return mat;
        }
    }
}
