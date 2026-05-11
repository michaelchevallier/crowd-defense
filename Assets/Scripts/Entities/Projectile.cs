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
                target.TakeDamage(damage);

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
            }

            ReleaseToPool();
        }

        private void ApplyOnHitEffects(Enemy e)
        {
            if (sourceTower == null || e.IsDead) return;
            var slow = SlowEffectManager.Instance;
            if (slow == null) return;

            if (sourceTower._freezeOnHitActive)
                e.ApplyFreeze(sourceTower._freezeDurMs / 1000f);
            else if (sourceTower._slowOnHitActive)
                slow.ApplySlow(e, sourceTower._slowOnHitMul, sourceTower._slowOnHitDurMs);
            else if (sourceTower._appliesSlowActive)
                slow.ApplySlow(e, sourceTower._appliesSlowMul, sourceTower._appliesSlowDurMs);
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
