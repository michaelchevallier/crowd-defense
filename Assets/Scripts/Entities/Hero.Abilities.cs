#nullable enable
using UnityEngine;
using CrowdDefense.Systems;
using CrowdDefense.UI;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public partial class Hero : MonoBehaviour
    {
        public float GetCooldownRatio(int slotIndex) => slotIndex switch
        {
            0 => cfg != null && cfg.FireRateMs > 0 ? _cooldown / (cfg.FireRateMs / 1000f) : 0f,
            2 => UltCooldownFraction,
            3 => UltimateCooldownFraction,
            _ => 0f,
        };

        public float GetCooldownRemaining(int slotIndex) => slotIndex switch
        {
            0 => _cooldown,
            2 => _ultCooldown,
            3 => _ultimateCooldown,
            _ => 0f,
        };

        public void Cast(int slotIndex)
        {
            if (slotIndex == 0 && !_autoAttack) TryManualFire();
            if (slotIndex == 2) TryUlt();
            if (slotIndex == 3) TryUltimate();

            // V6 T24-M: play cast sound for Q/W (slots 2/3 already play via TryUlt/TryUltimate)
            if (slotIndex == 0) AudioController.Instance?.Play("skill_q_cast", 0.7f);
            if (slotIndex == 1) AudioController.Instance?.Play("skill_w_cast", 0.7f);

            StartCoroutine(CastSweepRoutine(slotIndex));
        }

        private System.Collections.IEnumerator CastSweepRoutine(int slotIndex)
        {
            Color sweepColor = slotIndex switch
            {
                1 => new Color(0.2f, 0.4f, 1f),
                2 => new Color(0.15f, 0.9f, 0.25f),
                _ => new Color(1f, 0.18f, 0.18f),
            };

            const int   Verts    = 64;
            const float Duration = 0.4f;
            const float RadiusStart = 0.5f;
            const float RadiusEnd   = 1.5f;
            const float HeightY     = 0.15f;

            var go = new GameObject("CastSweep_VFX");
            go.transform.SetParent(null);
            go.transform.position = transform.position;

            var lr = go.AddComponent<LineRenderer>();
            lr.loop           = true;
            lr.positionCount  = Verts;
            lr.useWorldSpace  = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                               ?? Shader.Find("Sprites/Default");
            var mat = new Material(particleShader != null ? particleShader : CrowdDefense.Common.ShaderUtil.GetUnlitShader());
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", sweepColor);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", sweepColor);
            mat.color = sweepColor;
            mat.SetFloat("_Surface", 1f);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            lr.material = mat;

            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                float t      = Mathf.Clamp01(elapsed / Duration);
                float radius = Mathf.Lerp(RadiusStart, RadiusEnd, t);
                float alpha  = Mathf.Lerp(1f, 0f, t);
                float width  = Mathf.Lerp(0.06f, 0.02f, t);

                lr.startWidth = width;
                lr.endWidth   = width;

                var c = sweepColor;
                c.a = alpha;
                lr.startColor = c;
                lr.endColor   = c;

                var origin = transform.position;
                for (int i = 0; i < Verts; i++)
                {
                    float angle = i * (2f * Mathf.PI / Verts);
                    lr.SetPosition(i, new Vector3(
                        origin.x + Mathf.Cos(angle) * radius,
                        origin.y + HeightY,
                        origin.z + Mathf.Sin(angle) * radius));
                }
                yield return null;
            }

            Destroy(go);
        }

        private void TryManualFire()
        {
            if (cfg == null || _running || ChannelingPill || _cooldown > 0f) return;

            float range2 = cfg.Range * RangeMul;
            range2 *= range2;
            var myPos = transform.position;

            Enemy? target = null;
            float bestDist2 = range2 + 1f;

            if (WaveManager.Instance != null)
            {
                var active = WaveManager.Instance.ActiveEnemies;
                for (int i = 0; i < active.Count; i++)
                {
                    var e = active[i];
                    if (e == null || e.IsDead) continue;
                    float d2 = (e.transform.position - myPos).sqrMagnitude;
                    if (d2 < range2 && d2 < bestDist2) { bestDist2 = d2; target = e; }
                }
            }

            if (target == null) return;

            Vector3 toTarget = target.transform.position - myPos;
            toTarget.y = 0f;
            if (toTarget != Vector3.zero) transform.rotation = Quaternion.LookRotation(toTarget);

            float rateMs = cfg.FireRateMs * FireRateMul;
            _cooldown = rateMs / 1000f;
            Fire(target);
            if (Lightning) LightningStrike(target);
        }

        public bool TryConsumeFirstTowerFree()
        {
            if (!FirstTowerFree || FirstTowerFreeUsed) return false;
            FirstTowerFreeUsed = true;
            return true;
        }

        public bool TryUlt()
        {
            if (_ultCooldown > 0f) return false;
            if (cfg == null) return false;

            _ultCooldown    = cfg.UltCooldownMs / 1000f;
            FireUltFan();
            FireUltAoe();
            TriggerUltVfx();

            AudioController.Instance?.Play("hero_ult", 1f);
            JuiceFX.Instance?.Flash(new Color(1f, 0.88f, 0.5f, 0.38f), 380);
            JuiceFX.Instance?.Shake(0.4f, 450);
            OnUltFired?.Invoke();
            return true;
        }

        public float UltCooldownRemaining => _ultCooldown;

        public float UltCooldownFraction =>
            cfg != null && cfg.UltCooldownMs > 0
                ? _ultCooldown / (cfg.UltCooldownMs / 1000f)
                : 0f;

        private void FireUltFan()
        {
            if (cfg == null) return;
            float baseAngle = transform.eulerAngles.y * Mathf.Deg2Rad;
            int shots = cfg.UltFanShotCount;
            float spreadRad = 52f * Mathf.Deg2Rad;
            for (int i = 0; i < shots; i++)
            {
                float t = shots > 1 ? (float)i / (shots - 1) : 0.5f;
                float angle = baseAngle - spreadRad + t * spreadRad * 2f;
                SpawnProjectileAt(angle, pieceBonus: cfg.UltFanPierceBonus,
                    damageMul: cfg.UltFanDamageMul, lifetime: 2.0f);
            }
        }

        private void FireUltAoe()
        {
            if (cfg == null || WaveManager.Instance == null) return;
            float r2 = cfg.UltAoeRadius * cfg.UltAoeRadius;
            var pos = transform.position;
            var active = WaveManager.Instance.ActiveEnemies;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var e = active[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - pos).sqrMagnitude < r2)
                    e.TakeDamage(cfg.UltAoeDamage);
            }
        }

        private void TriggerUltVfx()
        {
            if (VfxPool.Instance == null) return;
            VfxPool.Instance.SpawnImpact(transform.position + Vector3.up, new Color(1f, 0.82f, 0.22f));
        }

        public bool IsUltimateUnlocked => Level >= UltimateUnlockLevel;

        public float UltimateCooldownRemaining  => _ultimateCooldown;
        public float UltimateCooldownFraction   =>
            _ultimateCooldown / UltimateCooldown;

        public bool TryUltimate()
        {
            if (!IsUltimateUnlocked) return false;
            if (_ultimateCooldown > 0f) return false;
            if (WaveManager.Instance == null) return false;

            _ultimateCooldown = UltimateCooldown;

            var pos = transform.position;
            float baseDmg = cfg != null ? cfg.UltAoeDamage : 15f;
            float dmg = baseDmg * UltimateDmgMul * DamageMul;
            float r2  = UltimateAoeRadius * UltimateAoeRadius;
            var active = WaveManager.Instance.ActiveEnemies;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var e = active[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - pos).sqrMagnitude < r2)
                    e.TakeDamage(dmg);
            }

            VfxPool.Instance?.SpawnDeath(pos + Vector3.up, new Color(0.9f, 0.3f, 1f), intensityMul: 3.0f);
            VfxPool.Instance?.SpawnExplosion(pos, UltimateAoeRadius);
            AudioController.Instance?.Play("hero_ult", 1.2f);
            JuiceFX.Instance?.Flash(new Color(0.8f, 0.2f, 1f, 0.45f), 450);
            JuiceFX.Instance?.Shake(0.3f, 400);
            FloatingPopupController.Instance?.SpawnReward("ULTIMATE!", pos + Vector3.up * 2.5f, new Color(0.9f, 0.3f, 1f));
            OnUltFired?.Invoke();
            return true;
        }

        private void HandleUltimateInput()
        {
            if (KeyBindings.Instance == null) return;
            if (Input.GetKeyDown(KeyBindings.GetKey("skill_r")))
                TryUltimate();
        }
    }
}
