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
        private void TickIdleAnim()
        {
            TickSynergyHalo();
            TickAffordableHighlight();
            TickHitFlash();

            bool isSelected = PlacementController.Instance?.SelectedTower == this;
            bool recentFire = (Time.time - _lastFireAt) < 0.2f;

            if (!isSelected && !recentFire)
            {
                float bobY = _basePos.y + Mathf.Sin(Time.time * 1f + _idlePhase) * 0.03f;
                transform.position = new Vector3(_basePos.x, bobY, _basePos.z);
            }

            if (_meshChild == null) return;

            if (recentFire) return;

            float t = Time.time;
            float phase = _idlePhase;
            _meshChild.transform.localPosition = new Vector3(
                0f,
                Mathf.Sin(t * 1f + phase) * 0.03f,
                0f);
            _meshChild.transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Sin(t * 0.8f + phase) * 1.7f);
        }

        public void FlashHitConfirmation() => _hitFlashElapsed = 0f;

        private void TickHitFlash()
        {
            if (_hitFlashElapsed >= HitFlashTotal) return;
            _hitFlashElapsed += Time.deltaTime;

            float intensity;
            if (_hitFlashElapsed < HitFlashPeak)
                intensity = Mathf.Lerp(0.5f, 0.5f, _hitFlashElapsed / HitFlashPeak);
            else
                intensity = Mathf.Lerp(0.5f, 0f, (_hitFlashElapsed - HitFlashPeak) / (HitFlashTotal - HitFlashPeak));

            var renderers = _cachedRenderers;
            if (renderers == null) return;
            _hitFlashMpb ??= new MaterialPropertyBlock();
            var emission = Color.white * intensity;
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(_hitFlashMpb);
                _hitFlashMpb.SetColor(_emissionColorId, emission);
                r.SetPropertyBlock(_hitFlashMpb);
            }

            if (_hitFlashElapsed >= HitFlashTotal)
            {
                foreach (var r in renderers)
                {
                    r.GetPropertyBlock(_hitFlashMpb);
                    _hitFlashMpb.SetColor(_emissionColorId, Color.black);
                    r.SetPropertyBlock(_hitFlashMpb);
                }
            }
        }

        private void TickHeadAim()
        {
            if (_meshHead == null || target == null || target.IsDead) return;
            if (cfg == null) return;

            bool isPassive = cfg.Id == "frost" || cfg.Id.Contains("ice")
                          || cfg.Id == "magnet"
                          || cfg.Id == "portal";
            if (isPassive) return;

            Vector3 dir = target.transform.position - _meshHead.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion desired = Quaternion.LookRotation(dir);

            bool switched = !ReferenceEquals(target, _prevAimTarget);
            _prevAimTarget = target;

            if (switched)
            {
                _meshHead.transform.rotation = desired;
                return;
            }

            float degsPerSec = UpgradeLevel >= 3 ? 12f : 8f;
            _meshHead.transform.rotation = Quaternion.RotateTowards(
                _meshHead.transform.rotation, desired, degsPerSec * Time.deltaTime);
        }

        private IEnumerator RecoilRoutine()
        {
            _recoiling = true;
            if (_meshHead == null) { _recoiling = false; yield break; }

            Vector3 origin = _meshHead.transform.localPosition;
            Vector3 back   = origin + _meshHead.transform.localRotation * new Vector3(0f, 0f, -0.5f);

            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(t + Time.deltaTime / 0.06f, 1f);
                _meshHead.transform.localPosition = Vector3.Lerp(origin, back, t);
                yield return null;
            }
            t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(t + Time.deltaTime / 0.12f, 1f);
                _meshHead.transform.localPosition = Vector3.Lerp(back, origin, t);
                yield return null;
            }
            _meshHead.transform.localPosition = origin;
            _recoiling = false;
        }

        private void BuildAimLine()
        {
            var go = new GameObject("AimLine");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            _aimLine = go.AddComponent<LineRenderer>();
            _aimLine.positionCount = 2;
            _aimLine.useWorldSpace = true;
            _aimLine.startWidth = 0.05f;
            _aimLine.endWidth   = 0.02f;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(1f, 1f, 1f, 0.5f);
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }
            _aimLine.material = mat;
            go.SetActive(false);
        }

        private void TickAimLine()
        {
            if (_aimLine == null) return;
            if (!ShowTargetLine || target == null || target.IsDead)
            {
                if (_aimLine.gameObject.activeSelf) _aimLine.gameObject.SetActive(false);
                return;
            }
            if (!_aimLine.gameObject.activeSelf) _aimLine.gameObject.SetActive(true);
            Vector3 start = _barrelTip != null
                ? _barrelTip.position
                : transform.position + Vector3.up * 1f;
            _aimLine.SetPosition(0, start);
            _aimLine.SetPosition(1, target.transform.position + Vector3.up * 0.5f);
            Color c = cooldown <= 0.1f ? Color.red : new Color(1f, 1f, 1f, 0.5f);
            _aimLine.startColor = c;
            _aimLine.endColor   = new Color(c.r, c.g, c.b, 0f);
        }

        public void SetSelected(bool selected)
        {
            bool wasSelected = _isSelected;
            _isSelected = selected;

            if (selected && !wasSelected)
            {
                float pitch = Random.Range(0.95f, 1.05f);
                AudioController.Instance?.Play3DPitched("tower_select_click", transform.position, 0.5f, pitch);
                var sparkPos = transform.position + Vector3.up * 0.1f;
                for (int i = 0; i < 3; i++)
                    VfxPool.Instance?.SpawnSpark(sparkPos, Color.cyan);
            }
            else if (!selected && wasSelected)
            {
                AudioController.Instance?.Play3D("tower_deselect_click", transform.position, 0.3f);
            }
        }

        public void ShowRangeRing(bool visible)
        {
            if (_rangeRing != null)
                _rangeRing.SetActive(visible);
            if (_rangeCircle != null)
                _rangeCircle.enabled = visible;
        }

        private void BuildRangeRing(float range)
        {
            if (_rangeRing != null)
                Destroy(_rangeRing);

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "RangeRing";
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float diameter = range * 2f;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);
            Object.Destroy(go.GetComponent<Collider>());

            const int texSize = 64;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[texSize * texSize];
            float half = texSize * 0.5f;
            for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.SmoothStep(1f, 0f, dist) * 0.4f;
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
                pixels[y * texSize + x] = new Color32(102, 222, 255, a);
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.mainTexture = tex;
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.material = mat;

            go.SetActive(false);
            _rangeRing = go;
        }

        private void BuildRangeCircle(float range)
        {
            if (_rangeCircle != null)
            {
                Destroy(_rangeCircle.gameObject);
                _rangeCircle = null;
            }

            var go = new GameObject("RangeCircle");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 64;
            lr.startWidth = 0.06f;
            lr.endWidth   = 0.06f;
            lr.startColor = new Color(1f, 1f, 1f, 0.4f);
            lr.endColor   = new Color(1f, 1f, 1f, 0.4f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3001;
            }
            mat.color = new Color(1f, 1f, 1f, 0.4f);
            lr.material = mat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            const int segments = 64;
            float step = 2f * Mathf.PI / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * step;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * range, 0f, Mathf.Sin(angle) * range));
            }

            lr.enabled = false;
            _rangeCircle = lr;
        }

        public void ShowClusterHighlight(bool visible)
        {
            if (_clusterHighlight != null)
                _clusterHighlight.SetActive(visible);
        }

        private void BuildClusterHighlight()
        {
            if (_clusterHighlight != null)
                Destroy(_clusterHighlight);

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "ClusterHighlight";
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
            Object.Destroy(go.GetComponent<Collider>());

            const int texSize = 64;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[texSize * texSize];
            float half = texSize * 0.5f;
            for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float ring = Mathf.SmoothStep(0f, 1f, (dist - 0.72f) / 0.12f)
                           * Mathf.SmoothStep(0f, 1f, (1f - dist) / 0.08f);
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(ring * 0.75f) * 255f);
                pixels[y * texSize + x] = new Color32(255, 220, 40, a);
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.mainTexture = tex;
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.material = mat;

            go.SetActive(false);
            _clusterHighlight = go;
        }

        public void DrawTierPips(int level)
        {
            for (int i = 0; i < _tierPips.Count; i++)
            {
                if (_tierPips[i] != null) Destroy(_tierPips[i]);
            }
            _tierPips.Clear();

            if (level <= 1) return;

            int count = level;
            float radius = 0.72f;
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(angle) * radius, 0.06f, Mathf.Sin(angle) * radius);

                var pip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pip.name = "TierPip_" + i;
                pip.transform.SetParent(transform);
                pip.transform.localPosition = pos;
                pip.transform.localScale    = Vector3.one * 0.12f;
                Object.Destroy(pip.GetComponent<Collider>());

                var rend = pip.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mat.color = level >= 3
                        ? new Color(1f, 0.82f, 0.15f)
                        : new Color(0.8f, 0.8f, 0.9f);
                    rend.material = mat;
                }

                _tierPips.Add(pip);
            }
        }

        private void BuildDamageIcon(DamageType dt)
        {
            if (_damageIconQuad != null) Destroy(_damageIconQuad);
            _damageIconQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _damageIconQuad.name = "DamageIcon";
            Destroy(_damageIconQuad.GetComponent<Collider>());
            var t = _damageIconQuad.transform;
            t.SetParent(transform, worldPositionStays: false);
            t.localPosition = new Vector3(0f, 0.55f, 0f);
            t.localRotation = Quaternion.Euler(90f, 0f, 0f);
            t.localScale    = new Vector3(0.15f, 0.15f, 0.15f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = DamageTypeColor(dt);
            _damageIconQuad.GetComponent<MeshRenderer>().material = mat;
            var settings = CrowdDefense.UI.SettingsRegistry.Instance;
            bool show = settings != null && settings.ShowDamageIcons;
            _damageIconQuad.SetActive(show);
            if (settings != null)
                settings.OnSettingsChanged += RefreshDamageIconVisibility;
        }

        private static Color DamageTypeColor(DamageType dt) => dt switch
        {
            DamageType.Physical => new Color(0.75f, 0.75f, 0.75f),
            DamageType.Magic    => new Color(0.60f, 0.20f, 0.90f),
            DamageType.Frost    => new Color(0.20f, 0.85f, 1.00f),
            DamageType.Fire     => new Color(1.00f, 0.35f, 0.05f),
            _                   => Color.white,
        };

        private void RefreshDamageIconVisibility()
        {
            if (_damageIconQuad == null) return;
            var settings = CrowdDefense.UI.SettingsRegistry.Instance;
            bool show = settings != null && settings.ShowDamageIcons;
            _damageIconQuad.SetActive(show);
        }

        private static GameObject? FindChildNamed(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == childName) return child.gameObject;
                var found = FindChildNamed(child, childName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
