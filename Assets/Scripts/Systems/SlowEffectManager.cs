#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    public class SlowEffectManager : MonoSingleton<SlowEffectManager>
    {
        private struct SlowRecord
        {
            public float untilTime;
            public float mul;
        }

        private readonly Dictionary<Enemy, SlowRecord> slows = new();
        private readonly List<Enemy?> _toRemove = new();

        public void ApplySlow(Enemy enemy, float mul, int durMs)
        {
            if (enemy == null || enemy.IsDead) return;
            float until = Time.time + durMs / 1000f;
            if (!slows.TryGetValue(enemy, out var existing) || until > existing.untilTime)
                slows[enemy] = new SlowRecord { untilTime = until, mul = Mathf.Clamp01(mul) };
        }

        private void Update()
        {
            float now = Time.time;
            _toRemove.Clear();

            foreach (var kv in slows)
            {
                Enemy? enemy = kv.Key;
                // Unity may have destroyed the enemy object
                if (enemy == null || enemy.IsDead)
                {
                    _toRemove.Add(enemy);
                    continue;
                }
                if (now >= kv.Value.untilTime)
                {
                    enemy.currentSpeedMul = 1f;
                    enemy.SetSlowTint(false);
                    _toRemove.Add(enemy);
                }
                else
                {
                    enemy.currentSpeedMul = kv.Value.mul;
                    enemy.SetSlowTint(true);
                }
            }

            foreach (var e in _toRemove)
            {
                if (e != null) slows.Remove(e);
            }
        }
    }
}
