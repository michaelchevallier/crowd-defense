#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

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

        // Animator configuré par AnimationController.SetupAnimator au Init.
        private Animator? _animator;

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

        // Hero aura buff — applied by Synergies.cs or directly by Hero tick
        private float _heroBuffDmgMul = 1f;

        // Range ring + synergy halo GameObjects
        private GameObject? _rangeRing;
        private Renderer? _synergyHaloRenderer;
        private MaterialPropertyBlock? _haloMpb;
        private static readonly int _haloColorId = Shader.PropertyToID("_BaseColor");

        // Tier pip GameObjects (L2 = 2 pips, L3 = 3 pips)
        private readonly List<GameObject> _tierPips = new();

        // Idle animation phase (random offset per tower)
        private float _idlePhase;
        private float _lastFireAt;

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
        public float L3DmgMul { get; private set; } = 1f;
        public float L3FireRateMul { get; private set; } = 1f;  // >1 = plus lent (ex sniper ×2)
        public float L3Aoe { get; private set; } = 0f;          // 0 = utilise cfg.Aoe
        public int L3Pierce { get; private set; } = 0;          // 0 = utilise cfg.Pierce
        public int L3MultiShot { get; private set; } = 0;       // extra projectiles
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

        // L3 Tank "Bloc": DoT aura that ticks 0.6 dmg/sec to enemies within range (V5 _tankBlockAura)
        public bool  L3TankBlockAura { get; private set; } = false;
        public float L3TankBlockAuraRange { get; private set; } = 5f;
        public float L3TankBlockAuraDps { get; private set; } = 0.6f;

        // L3 Crossbow "FinalExplosion": when last pierce consumed, explode with AoE bonus damage
        public bool  L3FinalExplosion { get; private set; } = false;
        public float L3FinalExplosionAoe { get; private set; } = 0f;
        public float L3FinalExplosionDmg { get; private set; } = 0f;

        // Tint appliqué au L3 signature (rouge=DPS, cyan=Utility)
        private bool _l3TintApplied = false;

        // Coût cumulé pour le calcul du refund sell
        public int CumulativeCost { get; private set; }

        // Scale dégâts appliqué à ce niveau (ratio vs L1 Phaser convention)
        private float _levelDmgScale = 1f;

        public TowerType? Config => cfg;

        // Child GO holding the spawned GLTF mesh (null = using placeholder primitives)
        private GameObject? _meshChild;

        // First child transform of _meshChild used as the rotating "head" (LookAt enemy)
        private Transform? _headTransform;

        public void Init(TowerType type, GameObject? projPrefab)
        {
            cfg = type;
            cooldown = 0f;
            projectilePrefab = projPrefab;
            _clusterTimer = 0f;
            _slowTickTimer = 0f;
            _heroBuffDmgMul = 1f;
            _idlePhase = Random.value * Mathf.PI * 2f;
            _lastFireAt = 0f;
            UpgradeLevel = 1;
            UpgradeBranch = TowerBranch.None;
            CumulativeCost = type.Cost;
            _l3TintApplied = false;
            // L1 damage scale : Phaser LEVEL_SCALE[0] = 0.75
            _levelDmgScale = BalanceConfig.Get().LevelScale.Length > 0
                ? BalanceConfig.Get().LevelScale[0]
                : 0.75f;

            transform.localScale = Vector3.one * type.SizeMultiplier;

            // Check for active skin before spawning mesh — skin may override GLTF or material
            string assetKey = type.AssetKey;
            Color bodyColor = type.BodyColor;
            Material? skinMat = null;

            var activeSkin = SkinSystem.Instance?.GetActiveSkin(SkinTargetType.Tower, type.Id);
            if (activeSkin != null)
            {
                if (activeSkin.AlternateGLTF != null)
                    assetKey = activeSkin.Id;
                if (activeSkin.AlternateMaterial != null)
                    skinMat = activeSkin.AlternateMaterial;
                if (activeSkin.UseBodyColorOverride)
                    bodyColor = activeSkin.BodyColorOverride;
            }

            _meshChild = activeSkin?.AlternateGLTF != null
                ? SpawnSkinMeshChild(activeSkin.AlternateGLTF)
                : SpawnMeshChild(assetKey);

            // Cel-shading toon material on mesh subtree (or whole GO if no GLTF)
            var toonRoot = _meshChild != null ? _meshChild : gameObject;
            if (skinMat != null)
                MaterialController.ApplyOverrideMaterial(toonRoot, skinMat);
            else
                MaterialController.ApplyToon(toonRoot, bodyColor);

            // Outline silhouette — applied after toon so outline mat is not overwritten
            Outline.ApplyToHierarchy(toonRoot.transform);

            // AssetVariants palette swap post-toon
            if (activeSkin != null && activeSkin.ThemeIndex >= 0)
                AssetVariants.ApplyThemeIndex(toonRoot, activeSkin.ThemeIndex);
            else if (activeSkin != null && activeSkin.UseBodyColorOverride)
                AssetVariants.ApplySkin(toonRoot, activeSkin);

            // Animations Mechanim : Idle uniquement pour les tours (pas de Walk, rotation vers cible = code).
            _animator = AnimationController.SetupAnimator(toonRoot, "Idle", null);

            // Head = premier enfant du mesh child (canon/tourelle qui pivote vers la cible)
            _headTransform = _meshChild != null && _meshChild.transform.childCount > 0
                ? _meshChild.transform.GetChild(0)
                : null;

            BuildRangeRing(type.Range);
            BuildSynergyHalo();
        }

        /// <summary>
        /// Instancie le prefab GLTF depuis AssetRegistry si disponible.
        /// Désactive les primitives placeholder Base/Top quand GLTF spawné.
        /// Retourne le GO enfant spawné, ou null si fallback primitives.
        /// </summary>
        private GameObject? SpawnMeshChild(string assetKey)
        {
            if (string.IsNullOrEmpty(assetKey)) return null;

            var registry = Resources.Load<AssetRegistry>("AssetRegistry");
            if (registry == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[Tower] AssetRegistry not found — fallback primitive");
#endif
                return null;
            }

            var prefab = registry.Get(assetKey);
            if (prefab == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[Tower] GLTF prefab missing for assetKey='{assetKey}' — using red cube fallback");
#endif
                return CreateColoredFallback(new Color(1f, 0f, 0f)); // Red cube
            }

            // Disable placeholder primitives (Base + Top children)
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == "Base" || child.name == "Top")
                    child.gameObject.SetActive(false);
            }

            var instance = Object.Instantiate(prefab, transform);
            instance.name = "Mesh_" + assetKey;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private GameObject CreateColoredFallback(Color color)
        {
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fallback.name = "FallbackCube";
            fallback.transform.SetParent(transform);
            fallback.transform.localPosition = Vector3.zero;
            fallback.transform.localRotation = Quaternion.identity;
            fallback.transform.localScale = Vector3.one;
            var rend = fallback.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                    { color = color };
            }
            Object.Destroy(fallback.GetComponent<Collider>());
            return fallback;
        }

        private GameObject? SpawnSkinMeshChild(GameObject skinPrefab)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == "Base" || child.name == "Top")
                    child.gameObject.SetActive(false);
            }
            var inst = Object.Instantiate(skinPrefab, transform);
            inst.name = "Skin_" + skinPrefab.name;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            return inst;
        }

        /// <summary>
        /// Upgrade this tower to the given level (2 or 3).
        /// branch = TowerBranch.Dps or .Utility pour L3 signature (D1-03).
        /// branch ignoré pour tours non-signature et L1->L2.
        /// Returns false si upgrade invalide ou pas assez d'or.
        /// </summary>
        public bool UpgradeTo(int level, TowerBranch branch = TowerBranch.None)
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

            // Stats scaling — ratio vs L1 Phaser convention
            float[] scales = bal.LevelScale;
            int scaleIdx = Mathf.Clamp(level - 1, 0, scales.Length - 1);
            _levelDmgScale = scales.Length > scaleIdx ? scales[scaleIdx] : 1f;

            if (level == 3)
                ApplyL3Branch(branch);

            PostUpgradeVisuals(level);

            // Stage B integration hooks (audio + juice + vfx)
            AudioController.Instance?.Play("tower_upgrade", 0.8f);
            JuiceFX.Instance?.Flash(new Color(1f, 0.9f, 0.4f, 0.3f), 200);
            VfxPool.Instance?.SpawnCoinPickup(transform.position);

#if UNITY_EDITOR
            Debug.Log($"[Tower] UpgradeTo L{level} cost={cost} cumul={CumulativeCost} dmgScale={_levelDmgScale:F2} branch={branch}");
#endif
            return true;
        }

        /// <summary>
        /// Applique les stats divergentes L3 selon la branche et le type de tour (D1-03).
        /// Tours signature : archer, mage, ballista, cannon.
        /// </summary>
        private void ApplyL3Branch(TowerBranch branch)
        {
            if (cfg == null) return;

            bool isSignature = cfg.Id is "archer" or "mage" or "ballista" or "cannon";

            if (isSignature && branch == TowerBranch.None)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[Tower] L3 signature {cfg.Id} sans branche — fallback Dps");
#endif
                branch = TowerBranch.Dps;
            }

            UpgradeBranch = isSignature ? branch : TowerBranch.None;

            // Tours non-signature : effets L3 spécifiques sans choix de branche (port V5 Tower.js L350-355)
            if (!isSignature)
            {
                switch (cfg.Id)
                {
                    case "tank":
                        L3TankBlockAura      = true;
                        L3TankBlockAuraRange = 5f;
                        L3TankBlockAuraDps   = 0.6f;
                        break;
                    case "crossbow":
                        L3FinalExplosion     = true;
                        L3FinalExplosionAoe  = 2f;
                        L3FinalExplosionDmg  = cfg.Damage * BalanceConfig.Get().TowerDamageMul * 2.5f;
                        break;
                }
                return;
            }

            switch (cfg.Id)
            {
                case "archer":
                    if (branch == TowerBranch.Dps)
                    {
                        // Sniper : x3 dmg, fire-rate /2
                        L3DmgMul = 3.0f;
                        L3FireRateMul = 2.0f;
                    }
                    else
                    {
                        // Pluie d'archer : multiShot 2, AOE 3, +20% dmg
                        L3DmgMul = 1.2f;
                        L3Aoe = 3.0f;
                        L3MultiShot = 2;
                    }
                    break;

                case "mage":
                    if (branch == TowerBranch.Dps)
                    {
                        // Arcane : x2.5 dmg, slow on hit 30%/1.5s
                        L3DmgMul = 2.5f;
                        L3SlowOnHit = true;
                        L3SlowMul = 0.7f;
                        L3SlowDurMs = 1500;
                    }
                    else
                    {
                        // Boule de feu : AOE 4, x1.8 dmg, burn DOT 3s
                        L3DmgMul = 1.8f;
                        L3Aoe = 4.0f;
                        L3BurnDot = true;
                        L3BurnDps = cfg.Damage * 0.8f;
                        L3BurnDurMs = 3000;
                    }
                    break;

                case "ballista":
                    if (branch == TowerBranch.Dps)
                    {
                        // Pierce infini : x2.5 dmg, pierce 99, armor break 10s
                        L3DmgMul = 2.5f;
                        L3Pierce = 99;
                        L3ArmorBreak = true;
                        L3ArmorBreakMul = 1.5f;
                        L3ArmorBreakDurMs = 10000;
                    }
                    else
                    {
                        // Explosion : AOE 5, x2 dmg, knockback
                        L3DmgMul = 2.0f;
                        L3Aoe = 5.0f;
                        L3Knockback = true;
                    }
                    break;

                case "cannon":
                    if (branch == TowerBranch.Dps)
                    {
                        // Mega shell : x3 dmg, AOE 3.5, slow 50%/2s
                        L3DmgMul = 3.0f;
                        L3Aoe = 3.5f;
                        L3SlowOnHit = true;
                        L3SlowMul = 0.5f;
                        L3SlowDurMs = 2000;
                    }
                    else
                    {
                        // Shotgun : multiShot 5, x1.5 dmg, AOE 2
                        L3DmgMul = 1.5f;
                        L3MultiShot = 5;
                        L3Aoe = 2.0f;
                    }
                    break;
            }
        }

        /// <summary>
        /// Applique le tint visuel L3 (DPS=rouge intense, Utility=cyan).
        /// Appelé par RadialMenuController apres upgrade L3.
        /// </summary>
        public void ApplyL3Tint()
        {
            if (_l3TintApplied || UpgradeLevel < 3) return;
            Color tint = UpgradeBranch == TowerBranch.Dps
                ? new Color(0.9f, 0.15f, 0.10f)   // rouge intense DPS
                : new Color(0.10f, 0.80f, 0.85f);  // cyan Utility
            MaterialController.UpdateTint(gameObject, tint);
            _l3TintApplied = true;
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

            TickIdleAnim();

            // L3 Tank DoT aura — continuous damage to enemies in radius (V5 _tankBlockAura)
            if (L3TankBlockAura) TickTankBlockAura();

            // _buffMul et tous les champs synergy sont reset + recomputed par Synergies.LateUpdate.
            switch (cfg.Behavior)
            {
                case TowerBehavior.Attack:   UpdateAttack();   break;
                case TowerBehavior.Cluster:  UpdateCluster();  break;
                case TowerBehavior.Slow:     UpdateSlow();     break;
                case TowerBehavior.BuffAura: break; // deleguee a Synergies.cs
                case TowerBehavior.CoinPull: UpdateCoinPull(); break;
            }
        }

        // ── Attack ───────────────────────────────────────────────────────────
        private void UpdateAttack()
        {
            cooldown -= Time.deltaTime;

            if (target == null || target.IsDead || OutOfRange(target))
                target = AcquireTarget();

            if (target != null && _headTransform != null)
            {
                Vector3 dir = (target.transform.position - _headTransform.position).normalized;
                if (dir != Vector3.zero)
                {
                    Quaternion desired = Quaternion.LookRotation(dir);
                    _headTransform.rotation = Quaternion.RotateTowards(
                        _headTransform.rotation, desired, 8f * Time.deltaTime);
                }
            }

            if (target != null && cooldown <= 0f)
            {
                Fire(target);
                // L3FireRateMul >1 ralentit la cadence (sniper L3-DPS archer = x2)
                float rateMs = cfg!.FireRateMs * L3FireRateMul;
                cooldown = rateMs / 1000f;
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

                if (cfg.FlyerOnly && !e.IsFlyer) continue;
                if (e.IsFlyer && !cfg.FlyerOnly && !cfg.CanHitFlyers) continue;
                if (e.StealthAlpha < 0.4f) continue;

                if (e.IsFlyer)
                {
                    float distSq = Castle.Instance != null
                        ? (e.transform.position - Castle.Instance.transform.position).sqrMagnitude
                        : float.MaxValue;
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

            // Stage B integration hooks (audio + juice + vfx + anim)
            AudioController.Instance?.Play("tower_shoot", 0.55f);
            JuiceFX.Instance?.Shake(0.05f, 100);
            VfxPool.Instance?.SpawnImpact(transform.position + Vector3.up * 0.5f, cfg.ProjectileColor);
            if (_animator != null) _animator.SetTrigger("attackTrigger");
            _lastFireAt = Time.time;

            // _levelDmgScale encode le scaling Phaser : L1=0.75, L2=1.0, L3=1.30
            // L3DmgMul applique la divergence de branche (D1-03)
            // _heroBuffDmgMul: aura du Hero (ApplyHeroBuff / ClearHeroBuff)
            float dmg = cfg.Damage * BalanceConfig.Get().TowerDamageMul * _buffMul * _heroBuffDmgMul * _levelDmgScale * L3DmgMul;

            if (t.IsFlyer && !t.ImmuneToFlyerBonus)
            {
                float flyMul = Mathf.Max(cfg.FlyerDmgMul, _flyerDmgBonus);
                if (flyMul > 1f) dmg *= flyMul;
            }

            // Pierce : L3Pierce overrides cfg.Pierce ; add _pierceBonus from synergy
            int effectivePierce = L3Pierce > 0 ? L3Pierce : cfg.Pierce + _pierceBonus;
            // AoE : L3Aoe > 0 overrides cfg.Aoe
            float effectiveAoe = L3Aoe > 0f ? L3Aoe : cfg.Aoe;

            // Parabolic arc parameters (Cannon)
            float dist = (t.transform.position - (transform.position + Vector3.up * 1.0f)).magnitude;
            float flightDur = cfg.Parabolic ? dist / Mathf.Max(cfg.ProjectileSpeed, 1f) : 0f;
            float arcH = cfg.Parabolic ? dist / 3f : 0f;

            var proj = ProjectilePool.Instance.Get();
            if (proj == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[Tower] ProjectilePool.Get() returned null — skipping fire");
#endif
                return;
            }
            proj.transform.position = transform.position + Vector3.up * 1.0f;
            proj.transform.rotation = Quaternion.identity;
            proj.Init(t, dmg, cfg.ProjectileSpeed, cfg.ProjectileColor,
                effectivePierce, effectiveAoe, cfg.Parabolic, flightDur, arcH, this);

            // Extra projectiles : synergy _multiShotBonus + L3MultiShot (cumulatifs)
            int extraShots = _multiShotBonus + L3MultiShot;
            if (extraShots > 0)
            {
                for (int i = 0; i < extraShots; i++)
                {
                    float spreadAngle = (i + 1) * 12f;
                    Vector3 dir = (t.transform.position - transform.position).normalized;
                    Vector3 spread = Quaternion.Euler(0f, spreadAngle, 0f) * dir;
                    var proj2 = ProjectilePool.Instance.Get();
                    if (proj2 == null) continue;
                    proj2.transform.position = transform.position + Vector3.up * 1.0f;
                    proj2.transform.rotation = Quaternion.LookRotation(spread);
                    proj2.Init(t, dmg, cfg.ProjectileSpeed, cfg.ProjectileColor,
                        effectivePierce, effectiveAoe, cfg.Parabolic, flightDur, arcH, this);
                }
            }

            // L3 slow on hit (mage Arcane / cannon Mega shell)
            if (L3SlowOnHit && SlowEffectManager.Instance != null)
                SlowEffectManager.Instance.ApplySlow(t, L3SlowMul, L3SlowDurMs);
        }

        // ── Hero Buff Aura ────────────────────────────────────────────────────

        /// <summary>
        /// Applied by Synergies.cs each tick when Hero is within aura range.
        /// dmgMul multiplies final projectile damage independently of tower synergies.
        /// </summary>
        public void ApplyHeroBuff(float dmgMul)
        {
            _heroBuffDmgMul = Mathf.Max(1f, dmgMul);
        }

        /// <summary>
        /// Resets hero aura buff — called by Synergies.cs when hero moves out of range.
        /// </summary>
        public void ClearHeroBuff()
        {
            _heroBuffDmgMul = 1f;
        }

        // ── Range Ring ────────────────────────────────────────────────────────

        /// <summary>
        /// Shows or hides the range ring circle (useful during placement).
        /// </summary>
        public void ShowRangeRing(bool visible)
        {
            if (_rangeRing != null)
                _rangeRing.SetActive(visible);
        }

        private void BuildRangeRing(float range)
        {
            if (_rangeRing != null)
                Destroy(_rangeRing);

            var go = new GameObject("RangeRing");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.widthMultiplier = 0.08f;
            lr.positionCount = 64;

            var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(0.4f, 0.87f, 1f, 0.38f);
            lr.material = mat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            for (int i = 0; i < 64; i++)
            {
                float a = i / 64f * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * range, 0f, Mathf.Sin(a) * range));
            }

            go.SetActive(false);
            _rangeRing = go;
        }

        // ── Synergy Halo ──────────────────────────────────────────────────────

        private void BuildSynergyHalo()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "SynergyHalo";
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            go.transform.localScale    = new Vector3(1.95f * 2f, 0.01f, 1.95f * 2f);
            Object.Destroy(go.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(1f, 0.82f, 0.15f, 0f);
            SetHaloTransparent(mat);
            _synergyHaloRenderer = go.GetComponent<Renderer>();
            if (_synergyHaloRenderer != null) _synergyHaloRenderer.material = mat;
        }

        private static void SetHaloTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }

        // ── Tier Pips ─────────────────────────────────────────────────────────

        /// <summary>
        /// Draws N small pip spheres around the tower base indicating upgrade level.
        /// Called by UpgradeTo after each level change.
        /// </summary>
        public void DrawTierPips(int level)
        {
            for (int i = 0; i < _tierPips.Count; i++)
            {
                if (_tierPips[i] != null) Destroy(_tierPips[i]);
            }
            _tierPips.Clear();

            if (level <= 1) return;

            int count = level;
            float radius = 0.72f;
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(angle) * radius, 0.06f, Mathf.Sin(angle) * radius);

                var pip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pip.name = "TierPip_" + i;
                pip.transform.SetParent(transform);
                pip.transform.localPosition = pos;
                pip.transform.localScale    = Vector3.one * 0.12f;
                Object.Destroy(pip.GetComponent<Collider>());

                var rend = pip.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mat.color = level >= 3
                        ? new Color(1f, 0.82f, 0.15f)    // gold L3
                        : new Color(0.8f, 0.8f, 0.9f);   // silver L2
                    rend.material = mat;
                }

                _tierPips.Add(pip);
            }
        }

        // ── L3 Tank Block Aura DoT ────────────────────────────────────────────
        // Continuous 0.6 dmg/sec to enemies within 5 m radius (V5 Tower.js L614-625)
        private void TickTankBlockAura()
        {
            if (WaveManager.Instance == null) return;
            float r2 = L3TankBlockAuraRange * L3TankBlockAuraRange;
            float dmg = L3TankBlockAuraDps * Time.deltaTime;
            var enemies = WaveManager.Instance.ActiveEnemies;
            Vector3 myPos = transform.position;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - myPos).sqrMagnitude < r2)
                    e.TakeDamage(dmg);
            }
        }

        // ── Idle Animation ────────────────────────────────────────────────────

        private void TickIdleAnim()
        {
            TickSynergyHalo();

            if (_meshChild == null) return;

            bool recentFire = (Time.time - _lastFireAt) < 0.2f;
            if (recentFire) return;

            float t = Time.time;
            float phase = _idlePhase;
            _meshChild.transform.localPosition = new Vector3(
                0f,
                Mathf.Sin(t * 1.5f + phase) * 0.04f,
                0f);
            _meshChild.transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Sin(t * 0.8f + phase) * 1.7f);
        }

        private void TickSynergyHalo()
        {
            if (_synergyHaloRenderer == null) return;
            _haloMpb ??= new MaterialPropertyBlock();
            _synergyHaloRenderer.GetPropertyBlock(_haloMpb);
            float prevAlpha = _haloMpb.GetColor(_haloColorId).a;
            float targetAlpha = _synergyActive
                ? 0.30f + 0.20f * Mathf.Sin(Time.time * 5f)
                : 0f;
            float nextAlpha = Mathf.Lerp(prevAlpha, targetAlpha, 0.15f);
            var baseColor = new Color(1f, 0.82f, 0.15f, nextAlpha);
            _haloMpb.SetColor(_haloColorId, baseColor);
            _synergyHaloRenderer.SetPropertyBlock(_haloMpb);
        }

        // ── Fire Angled ───────────────────────────────────────────────────────

        /// <summary>
        /// Fires an extra projectile at angleDeg offset from main target direction.
        /// Used by cannon L3-Utility barrage (multi-shot extra round).
        /// </summary>
        public void FireAngled(Enemy t, float angleDeg)
        {
            if (cfg == null) return;
            if (ProjectilePool.Instance == null) return;

            float dmg = cfg.Damage * BalanceConfig.Get().TowerDamageMul * _buffMul * _heroBuffDmgMul * _levelDmgScale * L3DmgMul;

            Vector3 baseDir = (t.transform.position - transform.position).normalized;
            Vector3 angledDir = Quaternion.Euler(0f, angleDeg, 0f) * baseDir;

            float dist = (t.transform.position - transform.position).magnitude;
            float flightDur = cfg.Parabolic ? dist / Mathf.Max(cfg.ProjectileSpeed, 1f) : 0f;
            float arcH = cfg.Parabolic ? dist / 3f : 0f;

            int effectivePierce = L3Pierce > 0 ? L3Pierce : cfg.Pierce + _pierceBonus;
            float effectiveAoe = L3Aoe > 0f ? L3Aoe : cfg.Aoe;

            var proj = ProjectilePool.Instance.Get();
            if (proj == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[Tower] ProjectilePool.Get() returned null in FireAngled");
#endif
                return;
            }
            proj.transform.position = transform.position + Vector3.up * 1.0f;
            proj.transform.rotation = Quaternion.LookRotation(angledDir);
            proj.Init(t, dmg, cfg.ProjectileSpeed, cfg.ProjectileColor,
                effectivePierce, effectiveAoe, cfg.Parabolic, flightDur, arcH, this);
        }

        // ── UpgradeTo — hook pips after level change ──────────────────────────
        // (called after ApplyL3Branch inside UpgradeTo)
        private void PostUpgradeVisuals(int level)
        {
            DrawTierPips(level);
            if (cfg != null) BuildRangeRing(cfg.Range);
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
