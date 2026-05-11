#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Entities
{
    /// <summary>
    /// Choix de branche L3 pour les 4 tours signature (D1-03).
    /// None = tour non-signature ou pas encore upgradée L3.
    /// </summary>
    public enum TowerBranch { None, Dps, Utility }

    public class Tower : MonoBehaviour
    {
        [SerializeField] private GameObject? projectilePrefab;

        private TowerType? cfg;
        private float cooldown;
        private Enemy? target;

        // ── Synergy output fields (reset + recompute chaque tick par Synergies.cs) ──

        // BuffAura / Portal : reçoit un buff dmg d'un Portal voisin
        public float _buffMul = 1f;
        // Portal aura ou ballista+portal
        public int _pierceBonus = 0;
        // archer+frost
        public int _multiShotBonus = 0;
        // mage+skyguard
        public float _flyerDmgBonus = 1f;
        // cannon+frost : {mul, durMs} — non-null = appliquer slow on hit
        public bool _slowOnHitActive = false;
        public float _slowOnHitMul = 1f;
        public int _slowOnHitDurMs = 0;
        // crossbow+frost : appliquer slow on hit avec ces params
        public bool _appliesSlowActive = false;
        public float _appliesSlowMul = 1f;
        public int _appliesSlowDurMs = 0;
        // crossbow+mage : propagate AoE on hit
        public bool _propagateAoEActive = false;
        public float _propagateAoERadius = 0f;
        public float _propagateAoEDmg = 0f;
        // mine+cannon
        public float _cascadeRadius = 0f;
        // crossbow+fan
        public float _knockbackOnHit = 0f;
        // magnet+tank
        public bool _pullActive = false;
        // acid+ballista
        public bool _propagateDebuff = false;
        // skyguard+frost
        public bool _freezeOnHitActive = false;
        public int _freezeDurMs = 0;
        // Visual indicator : au moins une synergie active
        public bool _synergyActive = false;

        // Cluster (Mine) : timer spawn
        private float _clusterTimer;

        // Slow : tick rapide indépendant du cooldown standard
        private float _slowTickTimer;

        // Upgrade state
        public int UpgradeLevel { get; private set; } = 1;
        // Branche L3 — None jusqu'à L3 signature
        public TowerBranch UpgradeBranch { get; private set; } = TowerBranch.None;

        // L3 runtime overrides (branch divergence — D1-03)
        // Ces valeurs surchargent cfg.X lors du calcul Fire/Update.
        public float L3DmgMul { get; private set; } = 1f;       // multiplicateur dmg final
        public float L3FireRateMul { get; private set; } = 1f;  // multiplicateur fireRate (>1 = plus lent)
        public float L3Aoe { get; private set; } = 0f;          // aoe override (0 = utilise cfg)
        public int L3Pierce { get; private set; } = 0;          // pierce override (0 = utilise cfg)
        public int L3MultiShot { get; private set; } = 0;       // extra projectiles (0 = aucun)
        public bool L3SlowOnHit { get; private set; } = false;
        public float L3SlowMul { get; private set; } = 1f;
        public int L3SlowDurMs { get; private set; } = 0;
        public bool L3BurnDot { get; private set; } = false;
        public float L3BurnDps { get; private set; } = 0f;
        public int L3BurnDurMs { get; private set; } = 0;
        public bool L3ArmorBreak { get; private set; } = false;
        public float L3ArmorBreakMul { get; private set; } = 1f;
        public int L3ArmorBreakDurMs { get; private set; } = 0;
        public bool L3Knockback { get; private set; } = false;

        // Coût cumulé pour le calcul du refund sell
        public int CumulativeCost { get; private set; }

        // Scale dégâts appliqué à ce niveau (ratio vs L1 Phaser convention)
        private float _levelDmgScale = 1f;

        public TowerType? Config => cfg;

        public void Init(TowerType type, GameObject? projPrefab)
        {
            cfg = type;
            cooldown = 0f;
            projectilePrefab = projPrefab;
            _clusterTimer = 0f;
            _slowTickTimer = 0f;
            UpgradeLevel = 1;
            UpgradeBranch = TowerBranch.None;
            CumulativeCost = type.Cost;
            // L1 damage scale : Phaser LEVEL_SCALE[0] = 0.75
            _levelDmgScale = BalanceConfig.Get().LevelScale.Length > 0
                ? BalanceConfig.Get().LevelScale[0]
                : 0.75f;

            var renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in renderers)
            {
                rend.material = new Material(ShaderUtil.GetLitShader());
                rend.material.color = type.BodyColor;
            }

            transform.localScale = Vector3.one * type.SizeMultiplier;
        }

        /// <summary>
        /// Upgrade this tower to the given level (2 or 3).
        /// branch is reserved for L2→L3 (CORE-20) : null = default.
        /// Returns false if upgrade is invalid or not enough gold.
        /// </summary>
        public bool UpgradeTo(int level, string? branch = null)
        {
            if (cfg == null || Economy.Instance == null) return false;
            if (level != UpgradeLevel + 1) return false;
            if (level < 2 || level > 3) return false;

            var bal = BalanceConfig.Get();
            float mul = level == 2 ? bal.UpgradeMulL2 : bal.UpgradeMulL3;
            int cost = Mathf.RoundToInt(cfg.Cost * mul);

            if (!Economy.Instance.TrySpend(cost)) return false;

            CumulativeCost += cost;
            UpgradeLevel = level;
            if (branch != null && System.Enum.TryParse<TowerBranch>(branch, true, out var parsed))
                UpgradeBranch = parsed;

            // Stats scaling — ratio vs L1 Phaser convention
            // L1 scale = LevelScale[0] (0.75), L2 = LevelScale[1] (1.0), L3 = LevelScale[2] (1.30)
            float[] scales = bal.LevelScale;
            int scaleIdx = Mathf.Clamp(level - 1, 0, scales.Length - 1);
            _levelDmgScale = scales.Length > scaleIdx ? scales[scaleIdx] : 1f;

#if UNITY_EDITOR
            Debug.Log($"[Tower] UpgradeTo L{level} cost={cost} cumul={CumulativeCost} dmgScale={_levelDmgScale:F2} branch={branch ?? "null"}");
#endif
            return true;
        }

        /// <summary>
        /// Sell this tower : refund 80% of cumulative cost (Q8 BalanceConfig.SellRefundRatio).
        /// Handles Economy refund, unregisters from PlacementController and destroys GameObject.
        /// </summary>
        public void Sell()
        {
            if (cfg == null) return;
            var bal = BalanceConfig.Get();
            int refund = Mathf.RoundToInt(CumulativeCost * bal.SellRefundRatio);
            Economy.Instance?.AddGold(refund);
            PlacementController.Instance?.UnregisterTower(this);
#if UNITY_EDITOR
            Debug.Log($"[Tower] Sell cumul={CumulativeCost} refund={refund} ratio={bal.SellRefundRatio:F2}");
#endif
            Destroy(gameObject);
        }

        private void Update()
        {
            if (cfg == null) return;

            // _buffMul et tous les champs synergy sont reset + recomputed par Synergies.LateUpdate.
            // Tower.Update lit les valeurs posées par le tick précédent (1 frame de delay OK).
            switch (cfg.Behavior)
            {
                case TowerBehavior.Attack:   UpdateAttack();   break;
                case TowerBehavior.Cluster:  UpdateCluster();  break;
                case TowerBehavior.Slow:     UpdateSlow();     break;
                case TowerBehavior.BuffAura: break; // délégué à Synergies.cs
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
            if (cfg == null) return;
            if (ProjectilePool.Instance == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[Tower] ProjectilePool.Instance is null — projectile not fired");
#endif
                return;
            }

            // _levelDmgScale encode le scaling Phaser : L1=0.75, L2=1.0, L3=1.30
            float dmg = cfg.Damage * BalanceConfig.Get().TowerDamageMul * _buffMul * _levelDmgScale;

            // Bonus dégâts flyer : cfg de base × synergie mage+skyguard (_flyerDmgBonus)
            if (t.IsFlyer && !t.ImmuneToFlyerBonus)
            {
                float flyMul = Mathf.Max(cfg.FlyerDmgMul, _flyerDmgBonus);
                if (flyMul > 1f) dmg *= flyMul;
            }

            var proj = ProjectilePool.Instance.Get();
            proj.transform.position = transform.position + Vector3.up * 1.0f;
            proj.transform.rotation = Quaternion.identity;
            proj.Init(t, dmg, cfg.ProjectileSpeed, cfg.ProjectileColor);

            // archer+frost : multiShotBonus — fire extra projectiles in spread
            if (_multiShotBonus > 0)
            {
                for (int i = 0; i < _multiShotBonus; i++)
                {
                    float spreadAngle = (i + 1) * 12f; // 12° spread par extra projectile
                    Vector3 dir = (t.transform.position - transform.position).normalized;
                    Vector3 spread = Quaternion.Euler(0f, spreadAngle, 0f) * dir;
                    var proj2 = ProjectilePool.Instance.Get();
                    proj2.transform.position = transform.position + Vector3.up * 1.0f;
                    proj2.transform.rotation = Quaternion.LookRotation(spread);
                    proj2.Init(t, dmg, cfg.ProjectileSpeed, cfg.ProjectileColor);
                }
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
