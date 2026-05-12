#nullable enable
using System.Collections;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    // Static helpers for boss-specific behaviors: wizard_king teleport+rain, ai_hub drone burst, kraken tentacles.
    // Called from Enemy.Update. Each method is a no-op if the relevant EnemyType flag is false.
    internal static class EnemyBossBehaviorsStatic
    {
        // ── Constants ─────────────────────────────────────────────────────────
        private const float WizardTeleportProjectileArc = 30f;
        private const float WizardProjectileCooldown    = 0.15f;
        private const float AiHubBurstCooldown          = 5f;
        private const float KrakenSlamCooldown          = 6f;
        private const float KrakenSlamWarningDuration   = 0.6f;

        // ── 1. WizardKing — Teleport + Projectile Rain ────────────────────────
        internal static void TickWizardKing(Enemy e)
        {
            var cfg = e.Config;
            if (cfg == null || !cfg.CanTeleport) return;

            e._teleportTimer += Time.deltaTime;
            if (e._teleportTimer < cfg.TeleportCooldown) return;
            e._teleportTimer = 0f;

            var pm = PathManager.Instance;
            if (pm == null) return;
            int wpCount = pm.WaypointCountOnPath(e.PathIdx);
            if (wpCount <= 0) return;

            int minWp = Mathf.Min(e.CurrentWaypoint + 1, wpCount - 1);
            int targetWp = Random.Range(minWp, wpCount);
            Vector3 dest = pm.GetWaypointOnPath(e.PathIdx, targetWp) + Vector3.up * 0.5f;

            VfxPool.Instance?.SpawnPortal(e.transform.position);
            e.transform.position = dest;
            VfxPool.Instance?.SpawnPortal(dest);

            if (cfg.ProjectileRainCount > 0)
                e.StartCoroutine(FireProjectileRain(e, cfg.ProjectileRainCount));
        }

        private static IEnumerator FireProjectileRain(Enemy e, int count)
        {
            var castle = Castle.Instance;
            if (castle == null) yield break;

            Vector3 castleDir = (castle.transform.position - e.transform.position).normalized;
            float halfArc = (count - 1) * WizardTeleportProjectileArc * 0.5f;

            for (int i = 0; i < count; i++)
            {
                if (e == null || e.IsDead) yield break;
                float angle = -halfArc + i * WizardTeleportProjectileArc;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * castleDir;
                VfxPool.Instance?.SpawnFireBreath(
                    e.transform.position + Vector3.up * 1.8f,
                    dir,
                    6f);
                yield return new WaitForSeconds(WizardProjectileCooldown);
            }
        }

        // ── 2. WarlordBoss — Charge handled in partial Enemy.UpdateCharge ─────
        // Gap P2 fix: UpdateCharge reads cfg.EnableCharge. WarlordBoss.asset: enableCharge=1.
        internal static void TickWarlordCharge(Enemy e) { }

        // ── 3. AI Hub — Drone Burst Pattern ──────────────────────────────────
        internal static void TickAiHubBurst(Enemy e)
        {
            var cfg = e.Config;
            if (cfg == null || !cfg.IsBurstSummoner) return;

            e._burstSummonTimer += Time.deltaTime;
            if (e._burstSummonTimer < AiHubBurstCooldown) return;
            e._burstSummonTimer = 0f;

            int count = cfg.BurstCount;
            float step = cfg.BurstAngleStep;

            for (int i = 0; i < count; i++)
            {
                float angle = i * step * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.8f;
                Vector3 spawnPos = e.transform.position + offset;
                VfxPool.Instance?.SpawnPortal(spawnPos);
                e.SpawnMinionAt(spawnPos);
            }

#if UNITY_EDITOR
            Debug.Log($"[EnemyBossBehaviorsStatic] ai_hub burst — {count} drones spawned");
#endif
        }

        // ── 4. Kraken Boss — Tentacle Slam AoE ───────────────────────────────
        internal static void TickKrakenTentacles(Enemy e)
        {
            var cfg = e.Config;
            if (cfg == null || !cfg.HasTentacleSlam) return;

            e._tentacleSlamTimer += Time.deltaTime;
            if (e._tentacleSlamTimer < KrakenSlamCooldown) return;
            e._tentacleSlamTimer = 0f;

            e.StartCoroutine(TentacleSlamCoroutine(e, cfg.TentacleCount, cfg.TentacleDamage, cfg.TentacleRadius));
        }

        private static IEnumerator TentacleSlamCoroutine(
            Enemy e, int count, int damage, float radius)
        {
            float stepAngle = 360f / Mathf.Max(count, 1);
            for (int i = 0; i < count; i++)
            {
                float angle = i * stepAngle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (radius * 0.7f);
                VfxPool.Instance?.SpawnImpact(
                    e.transform.position + offset + Vector3.up * 0.1f,
                    new Color(0f, 0.8f, 0.8f));
            }

            yield return new WaitForSeconds(KrakenSlamWarningDuration);

            if (e == null || e.IsDead) yield break;

            var castle = Castle.Instance;
            if (castle != null)
            {
                float distSq = (castle.transform.position - e.transform.position).sqrMagnitude;
                if (distSq <= radius * radius)
                    castle.TakeDamage(damage);
            }

            VfxPool.Instance?.SpawnExplosion(e.transform.position + Vector3.up * 0.3f, radius * 0.5f);

#if UNITY_EDITOR
            Debug.Log($"[EnemyBossBehaviorsStatic] kraken_boss tentacle slam — damage={damage} radius={radius}");
#endif
        }
    }
}
