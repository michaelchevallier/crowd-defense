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
        private void SpawnEliteTrail()
        {
            var go = new GameObject("EliteTrail");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop              = true;
            main.startLifetime     = 0.5f;
            main.startSpeed        = 0.6f;
            main.startSize         = 0.08f;
            main.startColor        = new Color(1f, 0.85f, 0.2f, 0.85f);
            main.simulationSpace   = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime  = 18f;
            var shape = ps.shape;
            shape.enabled          = true;
            shape.shapeType        = ParticleSystemShapeType.Sphere;
            shape.radius           = 0.2f;
            var colorOverLifetime  = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0f), new GradientColorKey(new Color(1f, 0.6f, 0f), 1f) },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;
            ps.Play();
        }
        private IEnumerator LerpBossTint(Color target, Color emission, float dur)
        {
            if (_cachedRenderers == null || _mpb == null) yield break;
            Color from = _currentBossTint;
            float t    = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                Color c = Color.Lerp(from, target, t / dur);
                _mpb.SetColor(_baseColorId, c);
                _mpb.SetColor(_colorId,     c);
                if (_hitFlashTimer <= 0f)
                    _mpb.SetColor(_emissiveId, Color.Lerp(Color.black, emission, t / dur));
                for (int i = 0; i < _cachedRenderers.Length; i++)
                    _cachedRenderers[i].SetPropertyBlock(_mpb);
                yield return null;
            }
            _currentBossTint = target;
            _mpb.SetColor(_baseColorId, target);
            _mpb.SetColor(_colorId,     target);
            if (_hitFlashTimer <= 0f)
                _mpb.SetColor(_emissiveId, emission);
            for (int i = 0; i < _cachedRenderers.Length; i++)
                _cachedRenderers[i].SetPropertyBlock(_mpb);
            _bossTintLerp = null;
        }
        private System.Collections.IEnumerator BossSpawnCinematic()
        {
            const float Duration     = 1.2f;
            const int   RayCount     = 5;
            const float RayHeight    = 8f;
            const float RotSpeed     = 90f;   // deg/s

            var bossPos = transform.position;

            // ── 5 spotlight rays ─────────────────────────────────────────────
            var rays = new GameObject[RayCount];
            for (int i = 0; i < RayCount; i++)
            {
                float angle  = i * (360f / RayCount) * Mathf.Deg2Rad;
                float radius = 0.8f;
                var   offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                var go = new GameObject($"BossSpawnRay_{i}");
                go.transform.position = bossPos + offset;
                rays[i] = go;

                var lr = go.AddComponent<LineRenderer>();
                lr.positionCount  = 2;
                lr.SetPosition(0, bossPos + offset);
                lr.SetPosition(1, bossPos + offset + Vector3.up * RayHeight);
                lr.startWidth     = 0.18f;
                lr.endWidth       = 0.04f;
                lr.useWorldSpace  = true;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = Color.yellow * 2.5f;
                lr.material       = mat;
            }

            // ── Bass drone AudioSource ────────────────────────────────────────
            AudioSource? droneSource = null;
            var droneGO = new GameObject("BossSpawnDrone");
            droneSource = droneGO.AddComponent<AudioSource>();
            droneSource.loop        = false;
            droneSource.spatialBlend = 0f;
            droneSource.volume      = 1.0f;
            droneSource.pitch       = 0.6f;

            // Lowpass filter for deep rumble feel
            var lowpass = droneGO.AddComponent<AudioLowPassFilter>();
            lowpass.cutoffFrequency = 800f;
            lowpass.lowpassResonanceQ = 1.5f;

            // Try to play a boss spawn stinger; skip silently if clip absent
            AudioController.Instance?.PlayPitched("boss_spawn_drone", 1.5f, 0.6f);

            // ── Rotate rays + lerp drone pitch over duration ──────────────────
            float elapsed = 0f;
            while (elapsed < Duration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                float rot = RotSpeed * dt;
                for (int i = 0; i < RayCount; i++)
                {
                    if (rays[i] == null) continue;
                    rays[i].transform.RotateAround(bossPos, Vector3.up, rot);
                    // Update LineRenderer endpoints to match rotated position
                    var lr = rays[i].GetComponent<LineRenderer>();
                    if (lr != null)
                    {
                        var p0 = rays[i].transform.position;
                        lr.SetPosition(0, p0);
                        lr.SetPosition(1, p0 + Vector3.up * RayHeight);
                    }
                }

                // Lerp drone pitch 0.6 → 0.8
                if (droneSource != null)
                    droneSource.pitch = Mathf.Lerp(0.6f, 0.8f, elapsed / Duration);

                // Fade out rays in last 0.2 s
                if (elapsed > Duration - 0.2f)
                {
                    float alpha = (Duration - elapsed) / 0.2f;
                    for (int i = 0; i < RayCount; i++)
                    {
                        if (rays[i] == null) continue;
                        var lr = rays[i].GetComponent<LineRenderer>();
                        if (lr?.material != null)
                        {
                            var c = lr.material.color;
                            c.a = alpha;
                            lr.material.color = c;
                        }
                    }
                }

                yield return null;
            }

            // ── Cleanup ────────────────────────────────────────────────────────
            for (int i = 0; i < RayCount; i++)
                if (rays[i] != null) Object.Destroy(rays[i]);
            if (droneGO != null) Object.Destroy(droneGO);
        }

        // ── Spawn pop-in animation ────────────────────────────────────────────
        private System.Collections.IEnumerator SpawnPopIn(float targetScale, bool isBoss)
        {
            float duration  = isBoss ? 0.6f : 0.3f;
            float overshoot = isBoss ? 1.3f : 1.1f;
            float elapsed   = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / duration);
                // EaseOutBack: cubic overshoot then settle to targetScale
                float c1 = overshoot - 1f;
                float c3 = c1 + 1f;
                float s  = c3 * t * t * t - c1 * t * t;
                transform.localScale = Vector3.one * (targetScale * Mathf.Max(0f, s));
                yield return null;
            }
            transform.localScale = Vector3.one * targetScale;
            _popInCoroutine = null;
        }

        private GameObject? SpawnMeshChild(string assetKey)
        {
            if (string.IsNullOrEmpty(assetKey)) return null;

            var registry = Resources.Load<AssetRegistry>("AssetRegistry");
            if (registry == null)
            {
                Debug.LogError($"[Enemy] AssetRegistry not found — check Resources/AssetRegistry.asset exists");
                return null;
            }

            var prefab = registry.Get(assetKey);
            if (prefab == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[Enemy] GLTF prefab missing for assetKey='{assetKey}' — using colored capsule fallback");
#endif
                return CreateColoredFallback(cfg?.BodyColor ?? Color.red);
            }

            // Re-use existing GLTF child if same prefab (pool reuse: same cfg → keep mesh)
            if (_meshChild != null && _meshChild.name == "Mesh_" + assetKey)
            {
                _meshChild.SetActive(true);
                return _meshChild;
            }

            // Mismatch: destroy stale mesh from previous EnemyType
            if (_meshChild != null)
                Object.Destroy(_meshChild);
            _meshChild = null;

            var instance = Object.Instantiate(prefab, transform);
            instance.name = "Mesh_" + assetKey;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale    = Vector3.one;
            return instance;
        }

        private GameObject CreateColoredFallback(Color color)
        {
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = "FallbackCapsule";
            fallback.transform.SetParent(transform);
            fallback.transform.localPosition = Vector3.zero;
            fallback.transform.localRotation = Quaternion.identity;
            fallback.transform.localScale = new Vector3(0.5f, 1.0f, 0.5f);
            var rend = fallback.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                var mat = new Material(ShaderUtil.GetUnlitShader());
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                rend.material = mat;
            }
            Object.Destroy(fallback.GetComponent<Collider>());
            return fallback;
        }

        private GameObject? SpawnSkinMeshChild(GameObject? skinPrefab)
        {
            if (skinPrefab == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[Enemy] SpawnSkinMeshChild called with null skinPrefab — skipping GLTF spawn");
#endif
                return null;
            }
            if (_meshChild != null && _meshChild.name == "Skin_" + skinPrefab.name)
            {
                _meshChild.SetActive(true);
                return _meshChild;
            }

            // Mismatch: destroy stale mesh from previous skin
            if (_meshChild != null)
                Object.Destroy(_meshChild);
            _meshChild = null;

            var inst = Object.Instantiate(skinPrefab, transform);
            inst.name = "Skin_" + skinPrefab.name;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale    = Vector3.one;
            return inst;
        }

        // ── Visual helpers ────────────────────────────────────────────────────
        private void ApplyTint(Color tint)
        {
            if (_cachedRenderers == null || _mpb == null) return;
            _mpb.SetColor(_baseColorId, tint);
            _mpb.SetColor(_colorId, tint);
            for (int i = 0; i < _cachedRenderers.Length; i++)
                _cachedRenderers[i].SetPropertyBlock(_mpb);
        }

        // Port of _triggerHitFlash / _tickHitFeedback from Enemy.js
        private void TriggerHitFlash()
        {
            if (_cachedRenderers == null || _mpb == null) return;
            _mpb.SetColor(_emissiveId, new Color(1f, 0.125f, 0.125f));
            for (int i = 0; i < _cachedRenderers.Length; i++)
                _cachedRenderers[i].SetPropertyBlock(_mpb);
            _hitFlashTimer = HitFlashDuration;
        }

        private void ClearHitFlash()
        {
            if (_cachedRenderers == null || _mpb == null) return;
            // Restore boss phase emission (if any) instead of hard black
            _mpb.SetColor(_emissiveId, _bossPhaseEmission);
            for (int i = 0; i < _cachedRenderers.Length; i++)
                _cachedRenderers[i].SetPropertyBlock(_mpb);
        }

        private void ApplyFreezeEmissive(bool frozen)
        {
            if (_cachedRenderers == null || _mpb == null) return;
            Color emissive = frozen ? new Color(0.4f, 0.87f, 1f) : Color.black;
            _mpb.SetColor(_emissiveId, emissive);
            for (int i = 0; i < _cachedRenderers.Length; i++)
                _cachedRenderers[i].SetPropertyBlock(_mpb);
        }

        // ── Update lifecycle ──────────────────────────────────────────────────

        private void Start() { }
        private System.Collections.IEnumerator BossCinematic()
        {
            const float CinematicDuration = 1.5f;

            var pos = transform.position;

            // Triple-burst explosion — radius ×3, red-orange-gold
            VfxPool.Instance?.SpawnExplosion(pos + Vector3.up * 0.5f, 3f);
            VfxPool.Instance?.SpawnImpact(pos + Vector3.up * 0.8f, new Color(1f, 0.4f, 0f));
            VfxPool.Instance?.SpawnImpact(pos + Vector3.up * 1.2f, new Color(1f, 0.85f, 0f));

            // Rainbow confetti x2 intensity (no tint = gradient path)
            VfxPool.Instance?.SpawnConfetti(pos + Vector3.up * 1f, 2f);

            // Camera zoom on death position
            CameraController.Instance?.ZoomOnDeathPos(pos, CinematicDuration);

            // Screen shake + audio
            CameraController.Instance?.Shake(1.5f, CinematicDuration);
            AudioController.Instance?.Play("boss_death_roar", 1f);
            AudioController.Instance?.Play("boss_defeated", 1f);
            // Tower kill bonus : louder pitched-up explosion + reverb cue
            if (_lastDamageTower != null)
                AudioController.Instance?.PlayPitched("boss_kill_special", 1.4f, 1.25f);

            // "BOSS VAINCU !" popup
            CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                "BOSS VAINCU !", pos + Vector3.up * 2f, new Color(1f, 0.85f, 0f));

            // SlowMo via JuiceFX (handles ramp-down safely; unscaled wait keeps coroutine alive)
            JuiceFX.Instance?.SlowMo(0.4f, (int)(CinematicDuration * 1000f));
            yield return new WaitForSecondsRealtime(CinematicDuration);
        }

        // ── Tint helpers ──────────────────────────────────────────────────────

        // Tint cyan during slow, restore base color on expiration — preserves stealth alpha
        public void SetSlowTint(bool slowed)
        {
            float a = (cfg?.IsStealth == true) ? StealthAlpha : 1f;
            Color tint = slowed
                ? new Color(0.4f, 0.9f, 1.0f, a)
                : new Color(baseColor.r, baseColor.g, baseColor.b, a);
            ApplyTint(tint);
        }

        // ── SetStatic (decoration / preview mode) ────────────────────────────
        // Freezes enemy in world space, switches to Idle animation
        public void SetStatic(float worldX, float worldZ, float rotY = 0f)
        {
            _static     = true;
            _staticRotY = rotY;
            float y = cfg != null && cfg.IsFlyer ? cfg.FlyHeight : 0f;
            transform.position = new Vector3(worldX, y, worldZ);
            transform.rotation  = Quaternion.Euler(0f, rotY, 0f);
            AnimationController.SetWalking(_animator, false);
        }

        // Stealth ring decal — shown when enemy is stealth/cloaked
        private void EnsureStealthRing()
        {
            if (_stealthRingGO != null) return;
            _stealthRingGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _stealthRingGO.name = "StealthRing";
            _stealthRingGO.transform.SetParent(transform, false);
            _stealthRingGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _stealthRingGO.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            _stealthRingGO.transform.localScale    = Vector3.one * (StealthRingRadius * 2f);
            Destroy(_stealthRingGO.GetComponent<Collider>());
            _stealthRingMR = _stealthRingGO.GetComponent<MeshRenderer>();
            if (_stealthRingMR != null)
            {
                var mat = new Material(ShaderUtil.GetUnlitShader());
                mat.SetFloat("_Surface", 1f);
                mat.renderQueue = 3000;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 0.53f, 0.13f, 0.7f));
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(1f, 0.53f, 0.13f, 0.7f));
                _stealthRingMR.material = mat;
                _stealthRingMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _stealthRingMR.receiveShadows = false;
            }
        }

        // Boss skin phase (apocalypse visual: scale + tint + emission). phase 1=default, 2=darkred, 3=fire.
        public void ApplyBossPhase(int phase)
        {
            if (_cachedRenderers == null || _mpb == null) return;
            if (_bossPhase == phase) return;
            _bossPhase = phase;

            float scaleMul;
            Color tint;
            Color emission;

            string bossId = cfg?.Id ?? "";
            if (AssetVariants.BossTints.TryGetValue((bossId, phase), out Color registryTint))
            {
                scaleMul = phase switch { 2 => 1.15f, 3 => 1.3f, 4 => 1.4f, _ => 1f };
                tint     = registryTint;
                emission = phase >= 2 ? registryTint * 0.35f : Color.black;
            }
            else
            {
                switch (phase)
                {
                    case 2:
                        scaleMul = 1.15f;
                        tint     = new Color(0.7f, 0.3f, 0.3f);
                        emission = new Color(0.3f, 0f, 0f);
                        break;
                    case 3:
                        scaleMul = 1.3f;
                        tint     = new Color(1f, 0.4f, 0.1f);
                        emission = new Color(0.8f, 0.24f, 0f);
                        break;
                    default:
                        scaleMul = 1f;
                        tint     = baseColor;
                        emission = Color.black;
                        break;
                }
            }

            float eliteMul = _isElite ? (_bossBaseScale > 0f ? transform.localScale.x / _bossBaseScale : 1f) : 1f;
            transform.localScale = Vector3.one * (_bossBaseScale * scaleMul * eliteMul);

            _bossPhaseEmission = emission;
            if (_bossTintLerp != null) StopCoroutine(_bossTintLerp);
            _bossTintLerp = StartCoroutine(LerpBossTint(tint, emission, 0.3f));

            if (phase == 3)
                VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 0.8f, 1.8f);
        }

        // ── Castle reached ────────────────────────────────────────────────────

        private const float AttackTelegraphDuration = 0.5f;
        private const int   TelegraphSegments       = 32;

        private GameObject BuildTelegraphCircle(float radius)
        {
            var go = new GameObject("AttackTelegraph");
            go.transform.position = transform.position;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop          = true;
            lr.positionCount = TelegraphSegments;
            lr.startWidth    = 0.08f;
            lr.endWidth      = 0.08f;

            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1f, 0f, 0f, 0.75f);
            lr.material = mat;

            for (int i = 0; i < TelegraphSegments; i++)
            {
                float a = i / (float)TelegraphSegments * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0.05f, Mathf.Sin(a) * radius));
            }

            return go;
        }
    }
}
