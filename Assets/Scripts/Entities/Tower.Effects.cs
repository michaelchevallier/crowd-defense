#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;
using CrowdDefense.UI;

namespace CrowdDefense.Entities
{
    public partial class Tower
    {
        private void TickBulwarkAura()
        {
            if (PlacementController.Instance == null) return;
            float rangeSq = L3BulwarkAuraRange * L3BulwarkAuraRange;
            Vector3 myPos = transform.position;
            foreach (var t in PlacementController.Instance.PlacedTowers)
            {
                if (t == null || t == this) continue;
                t._bulwarkProtected = (t.transform.position - myPos).sqrMagnitude < rangeSq;
            }
        }

        private void TickTankBlockAura()
        {
            if (WaveManager.Instance == null) return;
            float r2 = L3TankBlockAuraRange * L3TankBlockAuraRange;
            float dmg = L3TankBlockAuraDps * Time.deltaTime;
            var enemies = WaveManager.Instance.ActiveEnemies;
            Vector3 myPos = transform.position;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - myPos).sqrMagnitude < r2)
                    e.TakeDamage(dmg);
            }
        }

        private void BuildSynergyHalo()
        {
            var go = new GameObject("SynergyHalo");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.1f, 0f);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(go.transform);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            Object.Destroy(quad.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(0.4f, 0.8f, 1f, 0.4f);
            SetHaloTransparent(mat);

            var rend = quad.GetComponent<Renderer>();
            if (rend != null) rend.material = mat;

            _synergyHaloRenderer = rend;
            go.SetActive(true);
        }

        private static void SetHaloTransparent(Material mat)
        {
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 2999;
            }
        }

        private void TickSynergyHalo()
        {
            if (_synergyHaloRenderer == null) return;

            // Skip tick if tower is disabled/grayed out
            if (IsDisabled || TempDisabledUntilTime > Time.time)
            {
                _haloMpb ??= new MaterialPropertyBlock();
                _synergyHaloRenderer.GetPropertyBlock(_haloMpb);
                _haloMpb.SetColor(_haloColorId, new Color(0.4f, 0.8f, 1f, 0f));
                _synergyHaloRenderer.SetPropertyBlock(_haloMpb);
                return;
            }

            _haloMpb ??= new MaterialPropertyBlock();
            _synergyHaloRenderer.GetPropertyBlock(_haloMpb);
            float prevAlpha = _haloMpb.GetColor(_haloColorId).a;
            float targetAlpha = _synergyActive
                ? 0.5f + 0.3f * Mathf.Sin(Time.time * 2f * Mathf.PI)
                : 0f;
            float nextAlpha = Mathf.Lerp(prevAlpha, targetAlpha, 0.15f);
            var baseColor = new Color(0.4f, 0.8f, 1f, nextAlpha);
            _haloMpb.SetColor(_haloColorId, baseColor);
            _synergyHaloRenderer.SetPropertyBlock(_haloMpb);
        }

        private void BuildMagnetAuraCircle(float radius)
        {
            var go = new GameObject("MagnetAuraCircle");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 48;
            lr.startWidth = 0.08f;
            lr.endWidth   = 0.08f;
            lr.startColor = new Color(1f, 0.7f, 0.1f, 0.6f);
            lr.endColor   = new Color(1f, 0.7f, 0.1f, 0.6f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3001;
            }
            mat.color = new Color(1f, 0.7f, 0.1f, 0.6f);
            lr.material = mat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            const int segments = 48;
            float step = 2f * Mathf.PI / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * step;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            lr.enabled = false;
            _magnetAuraCircle = lr;
        }

        private void BuildAffordableHighlight(float range)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "AffordableHighlight";
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float size = range * 0.3f;
            go.transform.localScale = new Vector3(size, size, 1f);
            Object.Destroy(go.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(1f, 0.85f, 0.3f, 0f);
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }
            _affordHighlightRenderer = go.GetComponent<Renderer>();
            if (_affordHighlightRenderer != null) _affordHighlightRenderer.material = mat;
            _affordMpb = new MaterialPropertyBlock();
            go.SetActive(false);
            _affordableHighlight = go;
        }

        private void TickAffordableHighlight()
        {
            if (_affordableHighlight == null || cfg == null || UpgradeLevel >= 3) return;
            _affordCheckTimer -= Time.deltaTime;
            if (_affordCheckTimer > 0f)
            {
                if (_affordableHighlight.activeSelf && _affordHighlightRenderer != null && _affordMpb != null)
                {
                    float alpha = 0.2f + 0.3f * (0.5f + 0.5f * Mathf.Sin(Time.time * 3.5f));
                    _affordMpb.SetColor("_BaseColor", new Color(1f, 0.85f, 0.3f, alpha));
                    _affordHighlightRenderer.SetPropertyBlock(_affordMpb);
                }
                return;
            }
            _affordCheckTimer = 0.5f;

            var bal = BalanceConfig.Get();
            float mul = UpgradeLevel == 1 ? bal.UpgradeMulL2 : bal.UpgradeMulL3;
            int cost = Mathf.RoundToInt(cfg.Cost * mul);
            bool canAfford = Economy.Instance != null && Economy.Instance.Gold >= cost;
            _affordableHighlight.SetActive(canAfford);
        }

        private void SpawnGlowRing()
        {
            if (_glowRing != null) return;

            const int verts = 24;
            const float radius = 0.75f;

            _glowRing = new GameObject("GlowRing_L3");
            _glowRing.transform.SetParent(transform, false);
            _glowRing.transform.localPosition = new Vector3(0f, 0.1f, 0f);

            var lr = _glowRing.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = verts;
            lr.startWidth = 0.08f;
            lr.endWidth = 0.08f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            for (int i = 0; i < verts; i++)
            {
                float angle = i / (float)verts * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            lr.material = mat;

            _glowPulseRoutine = StartCoroutine(GlowPulseRoutine(lr, mat));
        }

        private IEnumerator GlowPulseRoutine(LineRenderer lr, Material mat)
        {
            Color baseColor = mat.color;
            while (true)
            {
                float t = Mathf.Sin(Time.time * Mathf.PI * 2f) * 0.5f + 0.5f;
                float alpha = Mathf.Lerp(0.4f, 0.95f, t);
                float width = Mathf.Lerp(0.05f, 0.12f, t);
                mat.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                lr.startWidth = width;
                lr.endWidth = width;
                yield return null;
            }
        }

        private void OnResearchUnlocked(string towerId, int node)
        {
            if (cfg == null || cfg.Id != towerId) return;
            var pos = transform.position + Vector3.up * 1.5f;
            AudioController.Instance?.Play3D("research_ding", pos, 1f);
            VfxPool.Instance?.SpawnConfetti(pos, 2f, Color.yellow);
            JuiceFX.Instance?.PunchScale(transform, 1.1f, 0.25f);
        }
    }
}
