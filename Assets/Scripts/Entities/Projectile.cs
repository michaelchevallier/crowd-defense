#nullable enable
using System.Collections.Generic;
using CrowdDefense.Common;
using CrowdDefense.Systems;
using UnityEngine;

namespace CrowdDefense.Entities
{
    [RequireComponent(typeof(MeshRenderer))]
    public class Projectile : MonoBehaviour
    {
        private Enemy? target;
        private float damage;
        private float speed;
        private float lifetimeSec;
        private ProjectilePool? pool;
        private MeshRenderer? rend;

        // Source tower — used to apply synergy on-hit effects (slow / freeze)
        private Tower? sourceTower;

        // Parabolic arc (Cannon)
        private bool parabolic;
        private Vector3 startPosition;
        private float flightDuration;   // seconds from Init to impact
        private float flightElapsed;    // seconds since Init
        private float arcHeight;

        // Pierce (Ballista / Crossbow)
        private int piercesRemaining;
        private readonly HashSet<Enemy> alreadyHit = new();

        // AoE radius (Mage / Cannon / Mine)
        private float aoe;

        // Called once by ProjectilePool after Instantiate to back-link the pool
        public void SetPool(ProjectilePool p) => pool = p;

        public void Init(
            Enemy target,
            float damage,
            float speed,
            Color color,
            int pierce = 0,
            float aoeRadius = 0f,
            bool isParabolic = false,
            float flightDur = 0f,
            float arcH = 0f,
            Tower? source = null)
        {
            this.target = target;
            this.damage = damage;
            this.speed = speed;
            this.sourceTower = source;
            lifetimeSec = 5f;

            piercesRemaining = pierce;
            alreadyHit.Clear();
            aoe = aoeRadius;

            parabolic = isParabolic;
            startPosition = transform.position;
            flightDuration = flightDur > 0f ? flightDur : 1f;
            flightElapsed = 0f;
            arcHeight = arcH;

            rend = GetComponent<MeshRenderer>();
            if (rend != null)
            {
                if (rend.material == null)
                    rend.material = new Material(ShaderUtil.GetLitShader());
                rend.material.color = color;
                rend.material.SetFloat("_Smoothness", 0.9f);
            }
        }

        private void Update()
        {
            lifetimeSec -= Time.deltaTime;
            if (lifetimeSec <= 0f || target == null || target.IsDead)
            {
                ReleaseToPool();
                return;
            }

            if (parabolic)
                UpdateParabolic();
            else
                UpdateLinear();
        }

        private void UpdateParabolic()
        {
            flightElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(flightElapsed / flightDuration);

            Vector3 origin = startPosition;
            Vector3 dest = target!.transform.position;
            Vector3 mid = (origin + dest) * 0.5f + Vector3.up * arcHeight;

            transform.position = QuadraticBezier(origin, mid, dest, t);

            if (t >= 1f)
                ApplyHit();
        }

        private void UpdateLinear()
        {
            Vector3 targetPos = target!.transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            if ((transform.position - targetPos).sqrMagnitude < 0.04f)
                ApplyHit();
        }

        private void ApplyHit()
        {
            if (target == null) { ReleaseToPool(); return; }

            if (aoe > 0f)
                ApplyAoeDamage();
            else if (!alreadyHit.Contains(target))
                target.TakeDamage(damage, sourceTower);

            ApplyOnHitEffects(target);

            if (piercesRemaining > 0)
            {
                piercesRemaining--;
                alreadyHit.Add(target);
                Enemy? next = FindNextPierceTarget();
                if (next != null)
                {
                    target = next;
                    if (parabolic)
                    {
                        startPosition = transform.position;
                        float dist = (next.transform.position - startPosition).magnitude;
                        flightDuration = dist / Mathf.Max(speed, 1f);
                        flightElapsed = 0f;
                        arcHeight = dist / 3f;
                    }
                    return;
                }
                // No next target — pierce consumed. If source is L3 Crossbow, trigger final explosion.
                TryFinalExplosion();
            }

            ReleaseToPool();
        }

        // L3 Crossbow "FinalExplosion": when last pierce target consumed, AoE damage bonus burst.
        private void TryFinalExplosion()
        {
            if (sourceTower == null || !sourceTower.L3FinalExplosion) return;
            if (sourceTower.L3FinalExplosionAoe <= 0f || sourceTower.L3FinalExplosionDmg <= 0f) return;
            if (WaveManager.Instance == null) return;

            float r2 = sourceTower.L3FinalExplosionAoe * sourceTower.L3FinalExplosionAoe;
            float boom = sourceTower.L3FinalExplosionDmg;
            var enemies = WaveManager.Instance.ActiveEnemies;
            Vector3 pos = transform.position;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - pos).sqrMagnitude <= r2)
                    e.TakeDamage(boom);
            }
            Visual.VfxPool.Instance?.SpawnExplosion(pos, sourceTower.L3FinalExplosionAoe);
        }

        private void ApplyOnHitEffects(Enemy e)
        {
            if (sourceTower == null || e.IsDead) return;
            var slow = SlowEffectManager.Instance;
            if (slow != null)
            {
                if (sourceTower._freezeOnHitActive)
                    e.ApplyFreeze(sourceTower._freezeDurMs / 1000f);
                else if (sourceTower._slowOnHitActive)
                    slow.ApplySlow(e, sourceTower._slowOnHitMul, sourceTower._slowOnHitDurMs);
                else if (sourceTower._appliesSlowActive)
                    slow.ApplySlow(e, sourceTower._appliesSlowMul, sourceTower._appliesSlowDurMs);
            }

            // Armor break (Ballista L3 / synergy) — boost incoming damage temporarily
            if (sourceTower.L3ArmorBreak && sourceTower.L3ArmorBreakMul > 1f)
                e.ApplyArmorBreak(sourceTower.L3ArmorBreakMul, sourceTower.L3ArmorBreakDurMs);

            // Knockback (Tank L3 / synergy) — push enemy back along path
            float kb = sourceTower._knockbackOnHit;
            if (sourceTower.L3Knockback) kb = Mathf.Max(kb, 1f);
            if (kb > 0f) e.ApplyKnockback(kb);

            // PropagateAoE (mage→cannon cross-effect) — splash damage to nearby non-target enemies
            if (sourceTower._propagateAoEActive && sourceTower._propagateAoERadius > 0f
                && sourceTower._propagateAoEDmg > 0f && WaveManager.Instance != null)
            {
                float r2 = sourceTower._propagateAoERadius * sourceTower._propagateAoERadius;
                var enemies = WaveManager.Instance.ActiveEnemies;
                Vector3 hitPos = e.transform.position;
                for (int i = 0; i < enemies.Count; i++)
                {
                    var e2 = enemies[i];
                    if (e2 == null || e2 == e || e2.IsDead) continue;
                    if ((e2.transform.position - hitPos).sqrMagnitude <= r2)
                        e2.TakeDamage(sourceTower._propagateAoEDmg);
                }
                Visual.VfxPool.Instance?.SpawnImpact(hitPos + Vector3.up * 0.4f, new Color(0.63f, 0.31f, 1f));
            }

            // CascadeRadius (cannon→X cross-effect) — chain damage at reduced power
            if (sourceTower._cascadeRadius > 0f && WaveManager.Instance != null)
            {
                float r2 = sourceTower._cascadeRadius * sourceTower._cascadeRadius;
                var enemies = WaveManager.Instance.ActiveEnemies;
                Vector3 hitPos = e.transform.position;
                float chainDmg = damage * 0.5f * Mathf.Max(1f, sourceTower._buffMul);
                for (int i = 0; i < enemies.Count; i++)
                {
                    var e2 = enemies[i];
                    if (e2 == null || e2 == e || e2.IsDead) continue;
                    if ((e2.transform.position - hitPos).sqrMagnitude <= r2)
                        e2.TakeDamage(chainDmg);
                }
            }
        }

        private void ApplyAoeDamage()
        {
            if (WaveManager.Instance == null) return;
            float aoeSq = aoe * aoe;
            var enemies = WaveManager.Instance.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - transform.position).sqrMagnitude <= aoeSq)
                    e.TakeDamage(damage);
            }
        }

        private Enemy? FindNextPierceTarget()
        {
            if (WaveManager.Instance == null) return null;
            float searchRangeSq = 20f * 20f;
            Enemy? best = null;
            float bestDist = float.MaxValue;
            var enemies = WaveManager.Instance.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if (alreadyHit.Contains(e)) continue;
                float distSq = (e.transform.position - transform.position).sqrMagnitude;
                if (distSq > searchRangeSq) continue;
                float dist = Mathf.Sqrt(distSq);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = e;
                }
            }
            return best;
        }

        private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }

        private void ReleaseToPool()
        {
            if (pool != null)
                pool.Release(this);
            else
                Destroy(gameObject);
        }
    }
}
