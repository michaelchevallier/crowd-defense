#nullable enable
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    // Picks a random active enemy every 2-5 s and plays a positional grunt/screech.
    // A global cooldown of 1.5 s prevents overlap when many enemies are active.
    [DefaultExecutionOrder(10)]
    public class EnemyAmbientChatter : MonoSingleton<EnemyAmbientChatter>
    {
        private static readonly string[] ChatterKeys =
        {
            "enemy_grunt",
            "enemy_chatter",
            "enemy_screech",
        };

        private const float GlobalCooldown  = 1.5f;
        private const float IntervalMin     = 5f;
        private const float IntervalMax     = 5f;
        private const float RandomOffsetSec = 2f;

        private float _nextChatterTime;
        private float _globalCooldownUntil;

        protected override void OnAwakeSingleton()
        {
            _nextChatterTime = Time.time + Random.Range(IntervalMin, IntervalMax);
        }

        private void Update()
        {
            if (Time.time < _nextChatterTime) return;

            // Schedule next tick regardless of whether we actually play.
            _nextChatterTime = Time.time + IntervalMin + Random.Range(-RandomOffsetSec, RandomOffsetSec);

            if (Time.time < _globalCooldownUntil) return;

            var pool = EnemyPool.Instance;
            if (pool == null || pool.ActiveCount == 0) return;

            var enemies = pool.ActiveEnemies;
            int tries = 0;
            Enemy? picked = null;
            while (tries < 4)
            {
                var candidate = enemies[Random.Range(0, enemies.Count)];
                if (!candidate.IsDead)
                {
                    picked = candidate;
                    break;
                }
                tries++;
            }

            if (picked == null) return;

            string key = ChatterKeys[Random.Range(0, ChatterKeys.Length)];
            Vector3 pos = picked.transform.position;

            AudioController.Instance?.Play3D(key, pos, 0.55f);

            _globalCooldownUntil = Time.time + GlobalCooldown;
        }
    }
}
