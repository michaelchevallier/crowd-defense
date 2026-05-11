#nullable enable
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

        // Parabolic arc (Cannon)
        private bool parabolic;
        private Vector3 startPosition;
        private float flightDuration;   // seconds from Init to impact
        private float flightElapsed;    // seconds since Init
        private float arcHeight;

        // Called once by ProjectilePool after Instantiate to back-link the pool
        public void SetPool(ProjectilePool p) => pool = p;

        /// <param name="isParabolic">True = quadratic Bezier arc (Cannon).</param>
        /// <param name="flightDur">Total flight time in seconds (parabolic only).</param>
        /// <param name="arcH">Arc peak height offset above midpoint (parabolic only).</param>
        public void Init(
            Enemy target,
            float damage,
            float speed,
            Color color,
            bool isParabolic = false,
            float flightDur = 0f,
            float arcH = 0f)
        {
            this.target = target;
            this.damage = damage;
            this.speed = speed;
            lifetimeSec = 5f;

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
            target.TakeDamage(damage);
            ReleaseToPool();
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
                Destroy(gameObject); // fallback si pas de pool
        }
    }
}
