#nullable enable
using System.Collections;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public partial class Enemy : MonoBehaviour
    {
        private void BuildHpBar()
        {
            bool isBoss = cfg != null && (cfg.IsBoss || cfg.IsApocalypseBoss);
            float barScale = isBoss ? 2f : 1f;

            // Re-use existing bar if already built (pool reuse)
            if (_hpBarRoot == null)
            {
                // Background (red)
                var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
                bg.name = "HPBarBg";
                Object.Destroy(bg.GetComponent<Collider>());
                _hpBarRoot = bg.transform;
                _hpBarRoot.SetParent(transform, false);

                var bgMR = bg.GetComponent<MeshRenderer>();
                if (bgMR != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                    mat.color = Color.red;
                    bgMR.material = mat;
                    bgMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    bgMR.receiveShadows = false;
                }

                // Foreground (green, child of bg)
                var fg = GameObject.CreatePrimitive(PrimitiveType.Quad);
                fg.name = "HPBarFg";
                Object.Destroy(fg.GetComponent<Collider>());
                _hpBarFg = fg.transform;
                _hpBarFg.SetParent(_hpBarRoot, false);
                _hpBarFg.localPosition = Vector3.zero;
                _hpBarFg.localScale    = Vector3.one;

                _hpBarFgMR = fg.GetComponent<MeshRenderer>();
                if (_hpBarFgMR != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                    mat.color = Color.green;
                    _hpBarFgMR.material = mat;
                    _hpBarFgMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _hpBarFgMR.receiveShadows = false;
                }

                _hpBarMpb = new MaterialPropertyBlock();
            }

            _hpBarRoot.localPosition = new Vector3(0f, 1.5f, 0f);
            _hpBarRoot.localScale    = new Vector3(barScale, barScale * 0.1f, 1f);
            _hpBarRoot.gameObject.SetActive(false); // hidden at full HP
        }

        private void UpdateHpBar()
        {
            if (_hpBarRoot == null || _hpBarFg == null || _hpBarFgMR == null || _hpBarMpb == null) return;

            float ratio = HpRatio;

            // Visibility
            bool visible = ratio < 1f;
            if (_hpBarRoot.gameObject.activeSelf != visible)
                _hpBarRoot.gameObject.SetActive(visible);

            if (!visible) return;

            // Width: pivot is center — shift fg so left edge stays fixed
            _hpBarFg.localScale    = new Vector3(ratio, 1f, 1f);
            _hpBarFg.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, -0.001f);

            // Color: red → green
            Color barColor = Color.Lerp(Color.red, Color.green, ratio);
            _hpBarMpb.SetColor(_baseColorId, barColor);
            _hpBarMpb.SetColor(_colorId,     barColor);
            _hpBarFgMR.SetPropertyBlock(_hpBarMpb);

            // Billboard: face camera
            var camFwd = MainCameraCache.Main;
            if (camFwd != null)
                _hpBarRoot.rotation = Quaternion.LookRotation(camFwd.transform.forward);
        }

        private void BuildShieldHalo()
        private void BuildShieldHalo()
        {
            shieldHalo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldHalo.name = "ShieldHalo";
            shieldHalo.transform.SetParent(transform, false);
            shieldHalo.transform.localScale = Vector3.one * 1.2f;
            Destroy(shieldHalo.GetComponent<Collider>());
            var haloRend = shieldHalo.GetComponent<MeshRenderer>();
            if (haloRend != null)
            {
                var mat = new Material(ShaderUtil.GetLitShader());
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.renderQueue = 3001;
                mat.color = new Color(1f, 0.85f, 0.1f, 0.35f);
                haloRend.material = mat;
            }
        }

        private void EnsureBossAura()
        {
            if (cfg == null || !cfg.IsBoss)
            {
                if (_bossAuraGO != null) _bossAuraGO.SetActive(false);
                return;
            }

            if (_bossAuraGO == null)
            {
                _bossAuraGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _bossAuraGO.name = "BossAura";
                _bossAuraGO.transform.SetParent(transform, false);
                _bossAuraGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                _bossAuraGO.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                _bossAuraGO.transform.localScale    = new Vector3(2.8f, 2.8f, 1f);
                Destroy(_bossAuraGO.GetComponent<Collider>());
                _bossAuraMR = _bossAuraGO.GetComponent<MeshRenderer>();
                if (_bossAuraMR != null)
                {
                    var mat = new Material(ShaderUtil.GetLitShader());
                    mat.SetFloat("_Surface", 1f);
                    mat.renderQueue = 2999;
                    mat.color = new Color(cfg.BossAuraColor.r, cfg.BossAuraColor.g, cfg.BossAuraColor.b, 0.5f);
                    _bossAuraMR.material = mat;
                    _bossAuraMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _bossAuraMR.receiveShadows = false;
                }
            }
            else
            {
                _bossAuraGO.SetActive(true);
                if (_bossAuraMR != null)
                {
                    var c = cfg.BossAuraColor;
                    _auraMpb ??= new MaterialPropertyBlock();
                    _auraMpb.SetColor(_baseColorId, new Color(c.r, c.g, c.b, 0.5f));
                    _bossAuraMR.SetPropertyBlock(_auraMpb);
                }
            }
        }

    }
}
