#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Entities
{
    public class Tower : MonoBehaviour
    {
        [SerializeField] private GameObject? projectilePrefab;

        private TowerType? cfg;
        private float cooldown;
        private Enemy? target;

        // BuffAura : reçoit un buff d'un Portal voisin chaque frame
        public float _buffMul = 1f;

        // Cluster (Mine) : timer spawn
        private float _clusterTimer;

        // Slow : tick rapide indépendant du cooldown standard
        private float _slowTickTimer;

        public TowerType? Config => cfg;

        public void Init(TowerType type, GameObject? projPrefab)
        {
            cfg = type;
            cooldown = 0f;
            projectilePrefab = projPrefab;
            _clusterTimer = 0f;
            _slowTickTimer = 0f;

            var renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in renderers)
            {
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rend.material.color = type.BodyColor;
            }

            transform.localScale = Vector3.one * type.SizeMultiplier;
        }

        private void Update()
        {
            if (cfg == null) return;

            // Reset buff chaque frame — Portal le re-pose dans UpdateBuffAura
            _buffMul = 1f;

            switch (cfg.Behavior)
            {
                case TowerBehavior.Attack:   UpdateAttack();   break;
                case TowerBehavior.Cluster:  UpdateCluster();  break;
                case TowerBehavior.Slow:     UpdateSlow();     break;
                case TowerBehavior.BuffAura: UpdateBuffAura(); break;
                case TowerBehavior.CoinPull: UpdateCoinPull(); break;
            }
        }

        // ── Attack ───────────────────────────────────────────────────────────
        private void UpdateAttack()
        {
            cooldown -= Time.deltaTime;

            if (target == null || target.IsDead || OutOfRange(target))
                target = AcquireTarget();

            if (target != null && cooldown <= 0f)
            {
                Fire(target);
                cooldown = cfg!.FireRateMs / 1000f;
            }
        }

        // ── Cluster (Mine) ───────────────────────────────────────────────────
        private void UpdateCluster()
        {
            if (cfg == null) return;
            _clusterTimer -= Time.deltaTime;
            if (_clusterTimer <= 0f)
            {
                SpawnMineRing();
                _clusterTimer = cfg.CooldownMs / 1000f;
                if (_clusterTimer <= 0f) _clusterTimer = 12f; // fallback si CooldownMs non set
            }
        }

        private void SpawnMineRing()
        {
            if (cfg == null) return;
            int count = cfg.ClusterCount > 0 ? cfg.ClusterCount : 3;
            float spawnRadius = cfg.Range / 2f;
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnRadius;
                Vector3 pos = transform.position + offset;

                var go = new GameObject("MineExplosive");
                go.transform.position = pos;
                var mine = go.AddComponent<MineExplosive>();
                mine.Init(cfg.Damage, cfg.Aoe > 0f ? cfg.Aoe : 2.5f);
            }
        }

        // ── Slow (Fan / Frost) ───────────────────────────────────────────────
        private void UpdateSlow()
        {
            if (cfg == null) return;
            _slowTickTimer -= Time.deltaTime;
            if (_slowTickTimer > 0f) return;
            _slowTickTimer = 0.15f; // tick toutes les 150 ms

            if (WaveManager.Instance == null || SlowEffectManager.Instance == null) return;
            float rangeSq = cfg.Range * cfg.Range;
            var enemies = WaveManager.Instance.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - transform.position).sqrMagnitude > rangeSq) continue;
                SlowEffectManager.Instance.ApplySlow(e, cfg.SlowMul, cfg.SlowDurationMs);
            }
        }

        // ── BuffAura (Portal) ────────────────────────────────────────────────
        private void UpdateBuffAura()
        {
            if (cfg == null || PlacementController.Instance == null) return;
            float rangeSq = cfg.Range * cfg.Range;
            var towers = PlacementController.Instance.PlacedTowers;
            for (int i = 0; i < towers.Count; i++)
            {
                var t = towers[i];
                if (t == null || t == this) continue;
                if ((t.transform.position - transform.position).sqrMagnitude > rangeSq) continue;
                // _buffMul a été reset à 1 en début d'Update de CETTE tour
                // On pose le buff sur les voisines (elles lisent _buffMul dans Fire())
                t._buffMul = Mathf.Max(t._buffMul, cfg.BuffMul);
            }
        }

        // ── CoinPull (Magnet) ────────────────────────────────────────────────
        private void UpdateCoinPull()
        {
            if (cfg == null || CoinPullManager.Instance == null) return;
            CoinPullManager.Instance.RegisterSource(
                transform.position,
                cfg.Range,
                cfg.CoinMul > 0f ? cfg.CoinMul : BalanceConfig.Get().MagnetCoinMul);
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private bool OutOfRange(Enemy e)
        {
            if (cfg == null || e == null) return true;
            return (e.transform.position - transform.position).sqrMagnitude > cfg.Range * cfg.Range;
        }

        private Enemy? AcquireTarget()
        {
            if (cfg == null || WaveManager.Instance == null) return null;
            float rangeSq = cfg.Range * cfg.Range;
            Enemy? best = null;
            int bestWp = -1;
            var enemies = WaveManager.Instance.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - transform.position).sqrMagnitude > rangeSq) continue;

                // FlyerOnly : ne cible que les ennemis volants (Skyguard)
                if (cfg.FlyerOnly && !e.IsFlyer) continue;
                // Sans CanHitFlyers ni FlyerOnly : skip les volants
                if (e.IsFlyer && !cfg.FlyerOnly && !cfg.CanHitFlyers) continue;
                // Stealth en phase basse opacité : untargetable
                if (e.StealthAlpha < 0.4f) continue;

                // Flyers : pas de waypoint counter → priorité par distance au castle
                if (e.IsFlyer)
                {
                    float distSq = Castle.Instance != null
                        ? (e.transform.position - Castle.Instance.transform.position).sqrMagnitude
                        : float.MaxValue;
                    // On utilise bestWp négatif comme proxy "le plus proche du castle"
                    int flyerPriority = -(int)(distSq * 10f);
                    if (best == null || flyerPriority > bestWp)
                    {
                        bestWp = flyerPriority;
                        best = e;
                    }
                    continue;
                }

                if (e.CurrentWaypoint > bestWp)
                {
                    bestWp = e.CurrentWaypoint;
                    best = e;
                }
            }
            return best;
        }

        private void Fire(Enemy t)
        {
            if (projectilePrefab == null || cfg == null) return;
            var go = Instantiate(projectilePrefab, transform.position + Vector3.up * 1.0f, Quaternion.identity);
            var proj = go.GetComponent<Projectile>();
            if (proj != null)
            {
                float dmg = cfg.Damage * BalanceConfig.Get().TowerDamageMul * _buffMul;
                // Bonus dégâts flyer (Skyguard, Mage, Ballista, Arbalète)
                if (cfg.FlyerDmgMul > 1f && t.IsFlyer && !t.ImmuneToFlyerBonus)
                    dmg *= cfg.FlyerDmgMul;
                proj.Init(t, dmg, cfg.ProjectileSpeed, cfg.ProjectileColor);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (cfg == null) return;
            Gizmos.color = new Color(0.3f, 0.6f, 0.9f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, cfg.Range);
        }
#endif
    }
}
