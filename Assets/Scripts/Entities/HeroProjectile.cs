#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    /// <summary>
    /// Self-managed projectile spawned by Hero.
    /// Handles pierce, ricochet, fireball AoE, pierce-explode, glaciation, crits.
    /// Hero.UpdateProjectiles polls IsDone and removes from its list.
    /// </summary>
    public class HeroProjectile : MonoBehaviour
    {
        public bool IsDone { get; private set; }

        // Projectile data
        private float   _speed;
        private Vector3 _dir;
        private float   _damage;
        private float   _lifetime;
        private int     _pierceLeft;
        private int     _ricochetLeft;
        private float   _ricochetDecay;
        private float   _ricochetDmgMul = 1f;
        private bool    _aoeOnHit;
        private float   _fireballRadius;
        private float   _fireballDmgMul;
        private bool    _explodeOnConsume;
        private float   _pierceExplodeRadius;
        private float   _pierceExplodeDmgMul;
        private float   _critChance;
        private float   _critMul;
        private int     _critStaggerMs;
        private bool    _glaciation;

        private Action<Vector3>? _onEnemyKilled;

        private readonly HashSet<Enemy> _hitSet = new();

        // Trail emission throttle
        private int _trailFrame;

        public void Init(
            float speed, Vector3 dir, float damage, float lifetime,
            int pierceLeft, int ricochetLeft, float ricochetDecay,
            bool aoeOnHit, float fireballRadius, float fireballDmgMul,
            bool explodeOnConsume, float pierceExplodeRadius, float pierceExplodeDmgMul,
            float critChance, float critMul, int critStaggerMs,
            bool glaciation, Action<Vector3>? onEnemyKilled)
        {
            _speed              = speed;
            _dir                = dir.normalized;
            _damage             = damage;
            _lifetime           = lifetime;
            _pierceLeft         = pierceLeft;
            _ricochetLeft       = ricochetLeft;
            _ricochetDecay      = ricochetDecay;
            _aoeOnHit           = aoeOnHit;
            _fireballRadius     = fireballRadius;
            _fireballDmgMul     = fireballDmgMul;
            _explodeOnConsume   = explodeOnConsume;
            _pierceExplodeRadius = pierceExplodeRadius;
            _pierceExplodeDmgMul = pierceExplodeDmgMul;
            _critChance         = critChance;
            _critMul            = critMul;
            _critStaggerMs      = critStaggerMs;
            _glaciation         = glaciation;
            _onEnemyKilled      = onEnemyKilled;
        }

        private void Update()
        {
            if (IsDone) return;
            float dt = Time.deltaTime;
            _lifetime -= dt;
            transform.position += _dir * _speed * dt;

            // Sparse trail VFX (every other frame)
            _trailFrame = (_trailFrame + 1) % 2;
            if (_trailFrame == 0)
                VfxPool.Instance?.SpawnImpact(transform.position, new Color(1f, 0.957f, 0.835f, 0.5f));

            if (_lifetime <= 0f) { Expire(); return; }

            CheckHits();
        }

        private void CheckHits()
        {
            if (WaveManager.Instance == null) return;
            var active = WaveManager.Instance.ActiveEnemies;
            Enemy? hitTarget = null;

            for (int i = 0; i < active.Count; i++)
            {
                var e = active[i];
                if (e == null || e.IsDead || _hitSet.Contains(e)) continue;
                if ((e.transform.position - transform.position).sqrMagnitude < 0.36f)
                {
                    hitTarget = e;
                    break;
                }
            }

            if (hitTarget == null) return;

            bool isCrit = UnityEngine.Random.value < _critChance;
            float finalDmg = _damage * (isCrit ? _critMul : 1f) * _ricochetDmgMul;

            if (isCrit && _critStaggerMs > 0)
                SlowEffectManager.Instance?.ApplySlow(hitTarget, 0f, _critStaggerMs);

            bool consumed = false;

            if (_aoeOnHit)
            {
                AoeBlast(transform.position, _fireballRadius, finalDmg * _fireballDmgMul, active);
                consumed = true;
            }
            else
            {
                hitTarget.TakeDamage(finalDmg);
                _hitSet.Add(hitTarget);

                if (_glaciation && UnityEngine.Random.value < 0.3f)
                    SlowEffectManager.Instance?.ApplySlow(hitTarget, 0.5f, 2000);

                if (hitTarget.IsDead)
                    _onEnemyKilled?.Invoke(hitTarget.transform.position);

                if (_ricochetLeft > 0)
                {
                    var next = FindRicochetTarget(hitTarget, active, 4f);
                    if (next != null)
                    {
                        _ricochetLeft--;
                        _ricochetDmgMul *= _ricochetDecay;
                        _dir = (next.transform.position - hitTarget.transform.position).normalized;
                        _dir.y = 0f;
                        _lifetime = Mathf.Max(_lifetime, 0.7f);
                        transform.position = hitTarget.transform.position + Vector3.up * 0.9f;
                    }
                    else { consumed = true; }
                }
                else if (_pierceLeft > 0)
                {
                    _pierceLeft--;
                    if (_pierceLeft <= 0) consumed = true;
                }
                else
                {
                    consumed = true;
                }
            }

            if (consumed && _explodeOnConsume)
                AoeBlast(transform.position, _pierceExplodeRadius,
                    _damage * _pierceExplodeDmgMul, active);

            if (consumed) Expire();
        }

        private static void AoeBlast(Vector3 center, float radius, float dmg,
            IReadOnlyList<Enemy> enemies)
        {
            float r2 = radius * radius;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - center).sqrMagnitude < r2)
                    e.TakeDamage(dmg);
            }
            VfxPool.Instance?.SpawnImpact(center + Vector3.up * 0.2f, new Color(1f, 0.54f, 0.19f));
        }

        private Enemy? FindRicochetTarget(Enemy from, IReadOnlyList<Enemy> enemies, float range)
        {
            float best = range * range;
            Enemy? result = null;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead || _hitSet.Contains(e)) continue;
                float d2 = (e.transform.position - from.transform.position).sqrMagnitude;
                if (d2 < best) { best = d2; result = e; }
            }
            return result;
        }

        private void Expire()
        {
            IsDone = true;
            Destroy(gameObject);
        }
    }
}
