#nullable enable
using System.Collections;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public partial class Enemy
    {
        // ── Boss aura pulse ────────────────────────────────────────────────────

        private void TickBossAura()
        {
            if (_bossAuraGO == null || _bossAuraMR == null || cfg == null) return;
            float t = Time.time * 3f;
            float scale = 1f + 0.15f * Mathf.Sin(t);
            _bossAuraGO.transform.localScale = new Vector3(scale * 2.8f, scale * 2.8f, 1f);
            Color c = cfg.BossAuraColor;
            float alpha = 0.38f + 0.27f * (Mathf.Sin(t) * 0.5f + 0.5f);
            _auraMpb ??= new MaterialPropertyBlock();
            _auraMpb.SetColor(_baseColorId, new Color(c.r, c.g, c.b, alpha));
            _bossAuraMR.SetPropertyBlock(_auraMpb);
        }

        private void TickEnrageLight()
        {
            if (_enrageLight == null || !_enrageLight.gameObject.activeSelf) return;
            _enrageLight.intensity = _enrageLightBaseIntensity + Random.Range(-1.2f, 1.2f);
        }

        // ── Apocalypse 4-phase state machine ──────────────────────────────────
        // P1 normal → P2 invul+imps every 2s for 6s → P3 speed×2 → P4 AoE pulse 1.5s

        private void TickApocalypseBoss()
        {
            if (cfg == null || !cfg.IsApocalypseBoss) return;

            // P2: periodic imp summons (3 imps every 2s for 6s window)
            if (_apocImpSummonEndTime > 0f && Time.time < _apocImpSummonEndTime && !_dying && !IsDead)
            {
                _apocImpSummonTimer -= Time.deltaTime;
                if (_apocImpSummonTimer <= 0f)
                {
                    _apocImpSummonTimer = 2f;
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = i * 120f * Mathf.Deg2Rad;
                        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.8f;
                        SpawnMinionAt(transform.position + offset);
                    }
                    VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 0.5f, 2f);
                }
            }

            // P3: delayed skeleton summons (one-shot horde at phase entry)
            if (_summonHordePending && Time.time >= _summonHordeTime)
            {
                _summonHordePending = false;
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * 90f * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2f;
                    SpawnMinionAt(transform.position + offset);
                }
                VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 0.8f, 3f);
            }

            // P4: AoE pulse every 1.5s
            if (_apocPhase >= 4 && !_dying && !IsDead)
            {
                _aoePulseTimer -= Time.deltaTime;
                if (_aoePulseTimer <= 0f)
                {
                    _aoePulseTimer = 1.5f;
                    TelegraphAoePulse();
                }
            }

            // Pulse aura harder during invulnerability window
            if (_invulUntilTime > 0f && Time.time < _invulUntilTime && _bossAuraMR != null && cfg != null)
            {
                float pulse = 1f + 0.5f * Mathf.Sin(Time.time * 15f);
                _bossAuraGO!.transform.localScale = new Vector3(pulse * 4.2f, pulse * 4.2f, 1f);
                Color c = cfg.BossAuraColor;
                _auraMpb ??= new MaterialPropertyBlock();
                _auraMpb.SetColor(_baseColorId, new Color(c.r, c.g, c.b, 0.7f + 0.3f * Mathf.Sin(Time.time * 15f)));
                _bossAuraMR.SetPropertyBlock(_auraMpb);
            }
        }

        private void TickApocalypseBossPhases(float ratio)
        {
            // P2 @75%: invul 2s + spawn 3 imps every 2s for 6s
            if (_apocPhase < 2 && ratio <= 0.75f)
            {
                _apocPhase = 2;
                _invulUntilTime = Time.time + 2f;
                _apocImpSummonEndTime = Time.time + 6f;
                _apocImpSummonTimer = 0f;
                VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 1f, 4f);
                JuiceFX.Instance?.Shake(0.3f, 400);
                EventManager.Instance?.Publish(new BossPhaseChangedEvent("L'Apocalypse — Phase 2 : Invulnérable !", 2));
            }
            // P3 @50%: speed ×2
            if (_apocPhase < 3 && ratio <= 0.50f)
            {
                _apocPhase = 3;
                pressureSpeedMul *= 2f;
                _summonHordePending = true;
                _summonHordeTime = Time.time + 1f;
                VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 1f, 4.5f);
                JuiceFX.Instance?.Shake(0.35f, 500);
                EventManager.Instance?.Publish(new BossPhaseChangedEvent("L'Apocalypse — Phase 3 : Vitesse ×2 !", 3));
            }
            // P4 @25%: AoE pulse 1.5s interval + enrage VFX
            if (_apocPhase < 4 && ratio <= 0.25f)
            {
                _apocPhase = 4;
                _damageMul *= 2f;
                _aoePulseTimer = 0f;
                VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 1f, 5f);
                JuiceFX.Instance?.Shake(0.5f, 700);
                EventManager.Instance?.Publish(new BossPhaseChangedEvent("L'Apocalypse — Phase 4 : ENRAGE FINAL !", 4));
                StartEnrageVFX();
            }
        }

        private void TelegraphAoePulse()
        {
            if (_dying || IsDead) return;
            StartCoroutine(AoeTelegraphCoroutine());
        }

        private IEnumerator AoeTelegraphCoroutine()
        {
            const float TelegraphDuration = 0.8f;
            const float AoePulseRadius    = 4f;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.position   = new Vector3(transform.position.x, 0.05f, transform.position.z);
            quad.transform.rotation   = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(AoePulseRadius * 2f, AoePulseRadius * 2f, 1f);
            var mr  = quad.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = new Color(1f, 0f, 0f, 0.4f);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend",   2f);
            mat.renderQueue = 3000;
            mr.material = mat;

            float elapsed = 0f;
            while (elapsed < TelegraphDuration)
            {
                if (_dying || IsDead) { Object.Destroy(quad); yield break; }
                float t     = (elapsed % 0.2f) / 0.2f;
                float alpha = Mathf.Lerp(0.4f, 0.7f, Mathf.PingPong(t * 2f, 1f));
                mat.color = new Color(1f, 0f, 0f, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
            Object.Destroy(quad);
            EmitAoePulse();
        }

        private void EmitAoePulse()
        {
            if (_dying || IsDead) return;
            const float AoePulseRadius = 4f;
            const int   AoePulseDamage = 20;
            if (Castle.Instance != null)
            {
                float radiusSq = AoePulseRadius * AoePulseRadius;
                if ((Castle.Instance.transform.position - transform.position).sqrMagnitude < radiusSq)
                    Castle.Instance.TakeDamage(AoePulseDamage);
            }
            VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 0.6f, AoePulseRadius);
        }

        // ── Warlord charge sprint ──────────────────────────────────────────────
        // EnableCharge flag (from EnemyType SO). Telegraph 1.5s wind-up, then 2× speed for ChargeMs, cooldown 8s.

        private void UpdateCharge()
        {
            if (cfg == null || !cfg.EnableCharge || cfg.ChargeCooldownMs <= 0) return;

            if (_chargeWindUpActive)
            {
                _chargeWindUpTimer -= Time.deltaTime;
                // Flash scale during wind-up using AnimationCurve-like sin ramp
                float flashScale = 1f + 0.12f * Mathf.Abs(Mathf.Sin(Time.time * 18f));
                transform.localScale = Vector3.one * (_bossBaseScale * flashScale);
                if (_chargeWindUpTimer <= 0f)
                {
                    _chargeWindUpActive = false;
                    _chargeActive       = true;
                    _chargeActiveTimer  = cfg.ChargeMs;
                    transform.localScale = Vector3.one * _bossBaseScale;
                    EventManager.Instance?.Publish(new BossChargeWarningEvent());
                }
                return;
            }

            if (_chargeActive)
            {
                VfxPool.Instance?.SpawnImpact(
                    transform.position + Vector3.up * 0.4f,
                    new Color(1f, 0.23f, 0.063f));
                _chargeActiveTimer -= Time.deltaTime * 1000f;
                if (_chargeActiveTimer <= 0f)
                {
                    _chargeActive  = false;
                    chargeTimer    = 0f;
                }
            }
            else
            {
                chargeTimer += Time.deltaTime * 1000f;
                if (chargeTimer >= cfg.ChargeCooldownMs)
                {
                    chargeTimer         = 0f;
                    _chargeWindUpActive = true;
                    _chargeWindUpTimer  = 1.5f;
                    VfxPool.Instance?.SpawnImpact(
                        transform.position + Vector3.up * 0.8f,
                        new Color(1f, 0.7f, 0f));
                }
            }
        }

        // ── Dragon fire breath cone ────────────────────────────────────────────
        // HasFireBreath flag. Telegraph 1s (orange flash) + 3s cone forward + damage 10/tick to towers.

        private void UpdateFireBreath()
        {
            if (cfg == null || !cfg.HasFireBreath) return;

            if (_fireBreathTelegraphActive) return;

            _fireBreathTimer -= Time.deltaTime;
            if (_fireBreathTimer > 0f) return;

            _fireBreathTimer = FireBreathCooldown;
            StartCoroutine(FireBreathRoutine());
        }

        private IEnumerator FireBreathRoutine()
        {
            _fireBreathTelegraphActive = true;

            // Telegraph 1s: orange pulsing emission on self
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                if (_dying || IsDead) { _fireBreathTelegraphActive = false; yield break; }
                float pulse = Mathf.Abs(Mathf.Sin(elapsed * Mathf.PI * 4f));
                if (_cachedRenderers != null && _mpb != null)
                {
                    _mpb.SetColor(_emissiveId, new Color(1f, 0.5f * pulse, 0f));
                    for (int i = 0; i < _cachedRenderers.Length; i++)
                        _cachedRenderers[i].SetPropertyBlock(_mpb);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Clear telegraph emission
            if (_cachedRenderers != null && _mpb != null)
            {
                _mpb.SetColor(_emissiveId, _bossPhaseEmission);
                for (int i = 0; i < _cachedRenderers.Length; i++)
                    _cachedRenderers[i].SetPropertyBlock(_mpb);
            }

            _fireBreathTelegraphActive = false;

            if (_dying || IsDead) yield break;

            // Breath 3s: emit cone + damage towers 10/tick every 0.5s
            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Vector3 dir = Castle.Instance != null
                ? (Castle.Instance.transform.position - transform.position).normalized
                : transform.forward;

            VfxPool.Instance?.SpawnFireBreath(origin, dir, 8f);

            float breathElapsed = 0f;
            float tickTimer     = 0f;
            const float BreathDuration  = 3f;
            const float BreathTickRate  = 0.5f;
            const float BreathRange     = 8f;
            const float ConeHalfAngle   = 25f;
            const int   BreathDamage    = 10;

            while (breathElapsed < BreathDuration)
            {
                if (_dying || IsDead) yield break;
                tickTimer     += Time.deltaTime;
                breathElapsed += Time.deltaTime;
                if (tickTimer >= BreathTickRate && PlacementController.Instance != null)
                {
                    tickTimer = 0f;
                    var towers = PlacementController.Instance.PlacedTowers;
                    for (int i = towers.Count - 1; i >= 0; i--)
                    {
                        var tower = towers[i];
                        if (tower == null) continue;
                        Vector3 toTower = tower.transform.position - transform.position;
                        float dist = toTower.magnitude;
                        if (dist > BreathRange) continue;
                        float angle = Vector3.Angle(dir, toTower.normalized);
                        if (angle < ConeHalfAngle)
                            tower.ReceiveEnemySplash(BreathDamage);
                    }
                }
                yield return null;
            }
        }

        // ── Enrage VFX ────────────────────────────────────────────────────────

        private void StartEnrageVFX()
        {
            if (_enragePS == null)
            {
                var psGO = new GameObject("EnrageAura");
                psGO.transform.SetParent(transform, false);
                psGO.transform.localPosition = Vector3.up * 0.5f;
                _enragePS = psGO.AddComponent<ParticleSystem>();
                var main = _enragePS.main;
                main.loop            = true;
                main.startLifetime   = 0.6f;
                main.startSpeed      = 1.8f;
                main.startSize       = 0.35f;
                main.startColor      = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.15f, 0f, 0.9f), new Color(1f, 0.55f, 0.05f, 0.7f));
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = _enragePS.emission;
                emission.rateOverTime = 50f;
                var shape = _enragePS.shape;
                shape.enabled   = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius    = 1.5f;
            }
            _enragePS.gameObject.SetActive(true);
            if (!_enragePS.isPlaying) _enragePS.Play();

            if (_enrageLight == null)
            {
                var lightGO = new GameObject("EnrageLight");
                lightGO.transform.SetParent(transform, false);
                lightGO.transform.localPosition = Vector3.up * 1f;
                _enrageLight           = lightGO.AddComponent<Light>();
                _enrageLight.type      = LightType.Point;
                _enrageLight.color     = new Color(1f, 0.15f, 0.05f);
                _enrageLight.range     = 5f;
                _enrageLight.intensity = _enrageLightBaseIntensity;
                _enrageLight.shadows   = LightShadows.None;
            }
            _enrageLight.gameObject.SetActive(true);

            if (_enrageAudio == null)
            {
                var audioGO = new GameObject("EnrageAudio");
                audioGO.transform.SetParent(transform, false);
                _enrageAudio              = audioGO.AddComponent<AudioSource>();
                _enrageAudio.spatialBlend = 1f;
                _enrageAudio.loop         = true;
                _enrageAudio.volume       = 0.7f;
                _enrageAudio.maxDistance  = 20f;
                _enrageAudio.rolloffMode  = AudioRolloffMode.Linear;
                var clip = AudioController.Instance?.GetClip("boss_enrage_loop");
                if (clip != null) _enrageAudio.clip = clip;
            }
            _enrageAudio.gameObject.SetActive(true);
            if (_enrageAudio.clip != null && !_enrageAudio.isPlaying) _enrageAudio.Play();
        }

        private void StopEnrageVFX()
        {
            if (_enragePS != null)    { _enragePS.Stop(true, ParticleSystemStopBehavior.StopEmitting); _enragePS.gameObject.SetActive(false); }
            if (_enrageLight != null)   _enrageLight.gameObject.SetActive(false);
            if (_enrageAudio != null) { _enrageAudio.Stop(); _enrageAudio.gameObject.SetActive(false); }
        }

        private void StartEnrageRing()
        {
            const int   Verts  = 32;
            const float Radius = 2f;

            var ringGO = new GameObject("EnrageRing");
            ringGO.transform.SetParent(transform, false);
            ringGO.transform.localPosition = Vector3.zero;

            _enrageRing               = ringGO.AddComponent<LineRenderer>();
            _enrageRing.loop          = true;
            _enrageRing.positionCount = Verts;
            _enrageRing.startWidth    = 0.12f;
            _enrageRing.endWidth      = 0.12f;
            _enrageRing.useWorldSpace = false;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")
                               ?? Shader.Find("Sprites/Default")
                               ?? Shader.Find("Standard")!) { name = "EnrageRing_Mat" };
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            _enrageRing.material = mat;

            for (int i = 0; i < Verts; i++)
            {
                float angle = i / (float)Verts * Mathf.PI * 2f;
                _enrageRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * Radius, 0.1f, Mathf.Sin(angle) * Radius));
            }
            _enrageRing.startColor = new Color(1f, 0.05f, 0.05f, 0.5f);
            _enrageRing.endColor   = new Color(1f, 0.05f, 0.05f, 0.5f);

            _enrageRingCoroutine = StartCoroutine(PulseEnrageRing());
        }

        private IEnumerator PulseEnrageRing()
        {
            const float PulseHz = 2f;
            while (_enrageRing != null)
            {
                float alpha = 0.25f + 0.25f * Mathf.Sin(Time.time * Mathf.PI * 2f * PulseHz);
                var col = new Color(1f, 0.05f, 0.05f, alpha);
                _enrageRing.startColor = col;
                _enrageRing.endColor   = col;
                yield return null;
            }
        }

        private void StopEnrageRing()
        {
            if (_enrageRingCoroutine != null) { StopCoroutine(_enrageRingCoroutine); _enrageRingCoroutine = null; }
            if (_enrageRing != null)
            {
                if (_enrageRing.gameObject != null) Object.Destroy(_enrageRing.gameObject);
                _enrageRing = null;
            }
        }
    }
}
