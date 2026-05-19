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

        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId     = Shader.PropertyToID("_Color");

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

        // ── 3. AI Hub — Drone Swarm Formation ────────────────────────────────
        // V4 parity: spawns drones in cycling formations (square-5, line-4, triangle-3).
        // Formation marker GOs are parented to boss so they follow with fixed offsets.
        private static readonly Vector3[] FormationSquare5 =
        {
            new Vector3( 0f,   0f,  2f),   // front-center
            new Vector3(-1.6f, 0f,  1f),   // front-left
            new Vector3( 1.6f, 0f,  1f),   // front-right
            new Vector3(-1.6f, 0f, -1f),   // rear-left
            new Vector3( 1.6f, 0f, -1f),   // rear-right
        };
        private static readonly Vector3[] FormationLine4 =
        {
            new Vector3(-3f, 0f, 0f),
            new Vector3(-1f, 0f, 0f),
            new Vector3( 1f, 0f, 0f),
            new Vector3( 3f, 0f, 0f),
        };
        private static readonly Vector3[] FormationTriangle3 =
        {
            new Vector3( 0f,   0f,  2f),
            new Vector3(-1.6f, 0f, -1f),
            new Vector3( 1.6f, 0f, -1f),
        };

        internal static void TickAiHubBurst(Enemy e)
        {
            var cfg = e.Config;
            if (cfg == null || !cfg.IsBurstSummoner) return;

            e._burstSummonTimer += Time.deltaTime;

            // Each frame: pulse-animate formation marker GOs parented to boss
            UpdateDroneFormationPositions(e);

            if (e._burstSummonTimer < AiHubBurstCooldown) return;
            e._burstSummonTimer = 0f;

            // Cycle formation shape per burst: square-5 → line-4 → triangle-3 → square-5…
            Vector3[] offsets = e._droneFormationCount switch
            {
                5 => FormationLine4,
                4 => FormationTriangle3,
                _ => FormationSquare5,
            };
            int count = offsets.Length;

            // Destroy previous formation marker GOs
            for (int i = 0; i < e._droneFormation.Length; i++)
            {
                if (e._droneFormation[i] != null)
                {
                    Object.Destroy(e._droneFormation[i]);
                    e._droneFormation[i] = null;
                }
            }
            e._droneFormationCount = count;

            // Spawn real minion enemies + create parented marker GOs for formation visuals
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = offsets[i];
                Vector3 spawnPos = e.transform.position + offset;
                VfxPool.Instance?.SpawnPortal(spawnPos);
                e.SpawnMinionAt(spawnPos);

                // Marker sphere follows boss at fixed offset (parented transform)
                var markerGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                markerGO.name = $"DroneMarker{i}";
                Object.Destroy(markerGO.GetComponent<Collider>());
                markerGO.transform.SetParent(e.transform, false);
                markerGO.transform.localPosition = offset + Vector3.up * 0.8f;
                markerGO.transform.localScale = Vector3.one * 0.35f;
                var mr = markerGO.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                    mat.color = new Color(0.1f, 0.6f, 1f, 0.85f);
                    mr.material = mat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }
                e._droneFormation[i] = markerGO;
            }

#if UNITY_EDITOR
            Debug.Log($"[EnemyBossBehaviorsStatic] ai_hub swarm — {count} drones, formation shape={count}");
#endif
        }

        private static void UpdateDroneFormationPositions(Enemy e)
        {
            if (e._droneFormationCount == 0) return;
            // Markers are parented to boss — position follows automatically.
            // Pulse scale each frame to signal active drone presence.
            float pulse = 0.35f + 0.05f * Mathf.Sin(Time.time * 6f);
            for (int i = 0; i < e._droneFormationCount && i < e._droneFormation.Length; i++)
            {
                var go = e._droneFormation[i];
                if (go == null) continue;
                go.transform.localScale = Vector3.one * pulse;
            }
        }

        // ── 4. Kraken Boss — Tentacle Slam (V4 parity) ───────────────────────
        // Pattern: 1s yellow telegraph flash on body → directional cone slam forward
        // → damage towers + castle inside cone. Differentiates from basic ring AoE.
        private const float KrakenConeHalfAngle   = 55f;  // ±55° forward cone
        private const float KrakenConeTowerDmgMul = 0.6f; // fraction of tentacleDamage applied to towers

        internal static void TickKrakenTentacles(Enemy e)
        {
            var cfg = e.Config;
            if (cfg == null || !cfg.HasTentacleSlam) return;
            if (e._krakenSlamTelegraphActive) return;

            e._tentacleSlamTimer += Time.deltaTime;
            if (e._tentacleSlamTimer < KrakenSlamCooldown) return;
            e._tentacleSlamTimer = 0f;

            e.StartCoroutine(TentacleSlamCoroutine(e, cfg.TentacleCount, cfg.TentacleDamage, cfg.TentacleRadius));
        }

        private static IEnumerator TentacleSlamCoroutine(
            Enemy e, int count, int damage, float radius)
        {
            e._krakenSlamTelegraphActive = true;

            // ── Phase 1: 1s yellow flash telegraph on kraken body ─────────────
            var mpb = new MaterialPropertyBlock();
            var rends = e.GetComponentsInChildren<Renderer>();
            float elapsed = 0f;
            const float TelegraphDuration = 1f;
            while (elapsed < TelegraphDuration)
            {
                if (e == null) yield break;
                if (e.IsDead) { e._krakenSlamTelegraphActive = false; yield break; }
                float pulse = Mathf.Abs(Mathf.Sin(elapsed * Mathf.PI * 5f));
                var yellow = new Color(1f, pulse, 0f, 1f);
                mpb.SetColor(_baseColorId, yellow);
                mpb.SetColor(_colorId,     yellow);
                for (int r = 0; r < rends.Length; r++)
                    rends[r].SetPropertyBlock(mpb);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Restore default tint
            mpb.Clear();
            for (int r = 0; r < rends.Length; r++)
                rends[r].SetPropertyBlock(mpb);

            e._krakenSlamTelegraphActive = false;
            if (e == null || e.IsDead) yield break;

            // ── Phase 2: tentacle ring impact VFX (warning) ───────────────────
            float stepAngle = 360f / Mathf.Max(count, 1);
            for (int i = 0; i < count; i++)
            {
                float angle = i * stepAngle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (radius * 0.8f);
                VfxPool.Instance?.SpawnImpact(
                    e.transform.position + offset + Vector3.up * 0.1f,
                    new Color(1f, 0.85f, 0f));  // yellow slam markers
            }

            yield return new WaitForSeconds(KrakenSlamWarningDuration);

            if (e == null || e.IsDead) yield break;

            // ── Phase 3: directional cone damage toward castle ────────────────
            Vector3 slamDir = Castle.Instance != null
                ? (Castle.Instance.transform.position - e.transform.position).normalized
                : e.transform.forward;

            float radiusSq = radius * radius;

            // Towers in cone take reduced damage
            if (PlacementController.Instance != null)
            {
                var towers = PlacementController.Instance.PlacedTowers;
                int towerDmg = Mathf.Max(1, Mathf.RoundToInt(damage * KrakenConeTowerDmgMul));
                for (int i = towers.Count - 1; i >= 0; i--)
                {
                    var tower = towers[i];
                    if (tower == null) continue;
                    Vector3 toTower = tower.transform.position - e.transform.position;
                    if (toTower.sqrMagnitude > radiusSq) continue;
                    float coneAngle = Vector3.Angle(slamDir, new Vector3(toTower.x, 0f, toTower.z));
                    if (coneAngle <= KrakenConeHalfAngle)
                        tower.ReceiveEnemySplash(towerDmg);
                }
            }

            // Castle takes full damage if inside cone radius
            if (Castle.Instance != null)
            {
                float distSq = (Castle.Instance.transform.position - e.transform.position).sqrMagnitude;
                if (distSq <= radiusSq)
                    Castle.Instance.TakeDamage(damage);
            }

            VfxPool.Instance?.SpawnExplosion(
                e.transform.position + slamDir * (radius * 0.5f) + Vector3.up * 0.3f,
                radius * 0.6f);

#if UNITY_EDITOR
            Debug.Log($"[EnemyBossBehaviorsStatic] kraken tentacle slam cone — dmg={damage} radius={radius} cone=±{KrakenConeHalfAngle}°");
#endif
        }
    }
}
