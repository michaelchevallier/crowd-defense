#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;
using CrowdDefense.UI;

namespace CrowdDefense.Entities
{
    /// <summary>
    /// Choix de branche L3 pour les 4 tours signature (D1-03).
    /// None = tour non-signature ou pas encore upgradée L3.
    /// </summary>
    public enum TowerBranch { None, Dps, Utility }

    /// <summary>
    /// L3 stats pour une branche donnée d'une tour signature.
    /// Rend le switch 77-lignes de ApplyL3Branch data-driven.
    /// </summary>
    internal struct L3Stats
    {
        public int Pierce;
        public int MultiShot;
        public bool FinalExplosion;
        public float FinalExplosionAoe;
        public float CritChance;
        public float CritMul;
        public int ChainLightningJumps;
        public float ChainLightningRange;
        public bool FreezeOnHit;
        public int FreezeDurMs;
        public bool BerserkerActive;
        public float BerserkerDmgMul;
        public float BerserkerHpThreshold;
        public bool BulwarkAura;
        public float BulwarkAuraRange;
        public float BulwarkDmgReduction;
    }

    public enum TargetPriority { First, Last, Strongest, Weakest, Closest }

    public enum GuardMode { All, AirOnly, GroundOnly }

    public class Tower : MonoBehaviour
    {
        [SerializeField] private GameObject? projectilePrefab;

        private TowerType? cfg;
        private float cooldown;
        private Enemy? target;
        private Enemy? _prevAimTarget; // snap detection : target switch

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

        // Selection ring — cyan LineRenderer circle at base, shown when tower is selected
        private LineRenderer? _selectionRing;
        private bool _isSelected;

        // Range ring + synergy halo GameObjects
        private GameObject? _rangeRing;
        private LineRenderer? _rangeCircle;
        private LineRenderer? _magnetAuraCircle;
        private GameObject? _clusterHighlight;
        private Renderer? _synergyHaloRenderer;
        private MaterialPropertyBlock? _haloMpb;
        private static readonly int _haloColorId = Shader.PropertyToID("_BaseColor");

        // L3 stats lookup table : (towerId, branch) → stats (D1-03)
        private static readonly Dictionary<(string, TowerBranch), L3Stats> _l3StatsTable = new()
        {
            // Archer
            [("archer", TowerBranch.Dps)] = new L3Stats { MultiShot = 2 },
            [("archer", TowerBranch.Utility)] = new L3Stats { CritChance = 0.25f, CritMul = 3f },

            // Crossbow
            [("crossbow", TowerBranch.Dps)] = new L3Stats { FinalExplosion = true, FinalExplosionAoe = 2.5f },
            [("crossbow", TowerBranch.Utility)] = new L3Stats { Pierce = 3 },

            // Tank
            [("tank", TowerBranch.Dps)] = new L3Stats { BerserkerActive = true, BerserkerDmgMul = 2f, BerserkerHpThreshold = 0.5f },
            [("tank", TowerBranch.Utility)] = new L3Stats { BulwarkAura = true, BulwarkAuraRange = 4f, BulwarkDmgReduction = 0.20f },

            // Mage
            [("mage", TowerBranch.Dps)] = new L3Stats { ChainLightningJumps = 3, ChainLightningRange = 5f },
            [("mage", TowerBranch.Utility)] = new L3Stats { FreezeOnHit = true, FreezeDurMs = 500 },
        };

        // Affordable upgrade highlight ring (gold pulsing quad when player can afford next level)
        private GameObject? _affordableHighlight;
        private Renderer? _affordHighlightRenderer;
        private MaterialPropertyBlock? _affordMpb;
        private float _affordCheckTimer;

        // Upgrade arrow — gold arrow floating/bobbing above tower head when player can afford upgrade
        private GameObject? _upgradeArrow;
        private Renderer? _upgradeArrowRenderer;
        private MaterialPropertyBlock? _upgradeArrowMpb;
        private float _upgradeArrowCheckTimer;
        private Vector3 _upgradeArrowBaseLocalPos;

        // Tier pip GameObjects (L2 = 2 pips, L3 = 3 pips)
        private readonly List<GameObject> _tierPips = new();

        // Idle animation phase (random offset per tower) + base world Y for root bob
        private float _idlePhase;
        private float _breathOffset;
        private Vector3 _basePos;
        private float _lastFireAt;

        // Kill streak combo — consecutive kills < 0.5 s apart give +5% dmg per stack (max 10)
        private int   _streakCount   = 0;
        private float _lastKillTime  = -999f;
        private const float StreakWindow  = 0.5f;
        private const int   StreakMax     = 10;
        // Set in Fire() when a crit lands; consumed once in RegisterKill() for floating +1 CRIT! text.
        private bool _lastKillWasCrit = false;

        // XP system — +5% dmg per 10 kills, max 50% (10 levels)
        private int   _xpKills    = 0;
        private float _xpDmgMul  = 1f;
        private const int   XpKillsPerLevel = 10;
        private const float XpDmgPerLevel   = 0.05f;
        private const float XpDmgMax        = 1.50f;

        // DOT feedback popup throttle — skip if popup emitted within last 0.5s
        private float _lastDotPopupAt = -1f;

        // DPS rolling window — entries are (timestamp, damage) pairs stored flat
        private readonly List<float> _damageLogTimes  = new();
        private readonly List<float> _damageLogValues = new();

        // Chain-lightning hit buffer — reused each shot to avoid per-shot HashSet alloc
        private readonly HashSet<Enemy> _chainHitBuffer = new();

        // Cluster (Mine) : timer spawn
        private float _clusterTimer;

        // Slow : tick rapide indépendant du cooldown standard
        private float _slowTickTimer;

        // Upgrade state
        public int UpgradeLevel { get; private set; } = 1;
        // Branche L3 — None jusqu'à L3 signature
        public TowerBranch UpgradeBranch { get; private set; } = TowerBranch.None;

        // Elite L4 : reached when L3 + all research nodes unlocked for this tower type.
        public bool IsEliteL4 => cfg != null
            && UpgradeLevel >= 3
            && TowerResearchTree.UnlockedCount(cfg.Id) >= TowerResearchTree.NodeCount;

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

        // L3 Archer "Ranger Marksman" (Utility): critical hit chance + multiplier (D1-03)
        public float L3CritChance { get; private set; } = 0f;   // 0–1 probability
        public float L3CritMul { get; private set; } = 1f;      // damage multiplier on crit

        // L3 Mage "Archmage" (DPS): chain lightning jumps on hit (D1-03)
        public int   L3ChainLightningJumps { get; private set; } = 0;
        public float L3ChainLightningRange { get; private set; } = 5f;

        // L3 Mage "Frostmage" (Utility): freeze on hit duration (D1-03)
        public bool L3FreezeOnHit { get; private set; } = false;
        public int  L3FreezeDurMs { get; private set; } = 0;

        // L3 Tank "Berserker" (DPS): damage ×2 when castle HP < 50% (D1-03)
        public bool  L3BerserkerActive { get; private set; } = false;
        public float L3BerserkerDmgMul { get; private set; } = 2f;
        public float L3BerserkerHpThreshold { get; private set; } = 0.5f;

        // L3 Tank "Bulwark" (Utility): -20% incoming damage to adjacent towers (D1-03)
        public bool  L3BulwarkAura { get; private set; } = false;
        public float L3BulwarkAuraRange { get; private set; } = 4f;
        public float L3BulwarkDmgReduction { get; private set; } = 0.20f;

        // Bulwark protection flag — set each frame by a nearby Bulwark tower.
        public bool _bulwarkProtected = false;

        // Tint appliqué au L3 signature (rouge=DPS, cyan=Utility)
        private bool _l3TintApplied = false;

        // L3 glow halo ring (LineRenderer 24-vert circle + 1Hz pulse coroutine)
        private GameObject? _glowRing;
        private Coroutine? _glowPulseRoutine;

        // Star row — 3 quads above tower indicating upgrade level (L1=silver, L2=gold, L3=rainbow)
        private GameObject? _starRow;
        private readonly Renderer?[] _starRenderers = new Renderer?[3];
        private readonly Material?[] _starMaterials = new Material?[3];
        private static readonly Color StarSilver  = new(0.8f, 0.8f, 0.85f, 1f);
        private static readonly Color StarGold    = new(1f, 0.85f, 0.2f, 1f);
        private const float StarDimAlpha = 0.15f;

        // L4 elite tier (UpgradeLevel >= 3 + all research nodes unlocked)
        private bool _l4EliteApplied = false;
        private Coroutine? _sparkleRoutine;

        // Windup animation coroutine — cancelled on sell/destroy mid-windup.
        private Coroutine? _windupRoutine;

        // Idle shimmer — subtle white tint pulse every 3 s over 0.4 s
        private Coroutine? _shimmerRoutine;
        private MaterialPropertyBlock? _shimmerMpb;
        private static readonly int _shimmerColorId   = Shader.PropertyToID("_BaseColor");
        private static readonly int _emissionColorId  = Shader.PropertyToID("_EmissionColor");

        // Hit-confirmation flash — white emission burst 0.08 s peak, 0.12 s decay (total 0.2 s)
        private MaterialPropertyBlock? _hitFlashMpb;
        private float _hitFlashElapsed = 1f; // >= 0.2 means inactive
        private const float HitFlashPeak   = 0.08f;
        private const float HitFlashTotal  = 0.20f;

        [SerializeField] private TargetPriority _targetPriority = TargetPriority.First;
        public TargetPriority CurrentTargetPriority => _targetPriority;
        public Enemy? CurrentTarget => target;

        [SerializeField] private GuardMode _guardMode = GuardMode.All;
        public GuardMode CurrentGuardMode => _guardMode;

        // Coût cumulé pour le calcul du refund sell
        public int CumulativeCost { get; private set; }

        // Statistiques cumulées (pour TowerInfoPanel)
        public float TotalDamageDealt { get; private set; }
        public int   TotalKills       { get; private set; }
        public void RegisterKill()
        {
            TotalKills++;
            TakeDamage(1);
            if (Time.time - _lastKillTime < StreakWindow)
                _streakCount = Mathf.Min(_streakCount + 1, StreakMax);
            else
                _streakCount = 1;
            _lastKillTime = Time.time;
            if (_streakCount > 3)
                CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                    $"x{_streakCount}", transform.position + Vector3.up * 2f, new Color(1f, 0.85f, 0.1f));

            SpawnKillFloatText();

            // XP level-up every XpKillsPerLevel kills
            _xpKills++;
            if (_xpKills % XpKillsPerLevel == 0 && _xpDmgMul < XpDmgMax)
            {
                _xpDmgMul = Mathf.Min(_xpDmgMul + XpDmgPerLevel, XpDmgMax);
                CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                    "+LVL", transform.position + Vector3.up * 2.5f, new Color(0.4f, 1f, 0.4f));
            }
        }

        private void SpawnKillFloatText()
        {
            if (!gameObject.activeInHierarchy) return;
            var popup = CrowdDefense.UI.FloatingPopupController.Instance;
            if (popup == null) return;

            Vector3 pos = transform.position + Vector3.up * 1.5f;
            bool isCrit = _lastKillWasCrit;
            _lastKillWasCrit = false;

            if (_streakCount >= 5)
            {
                popup.SpawnReward($"+1 STREAK x{_streakCount}!", pos, new Color(1f, 0.75f, 0f));
            }
            else if (isCrit)
            {
                popup.SpawnReward("+1 CRIT!", pos, new Color(1f, 0.9f, 0.1f));
            }
            else
            {
                popup.SpawnReward("+1", pos, Color.white);
            }
        }

        // HP system : 30 max, -1 per kill, repair = 10¢ per 5 HP missing (rounded up)
        private int _hp    = 30;
        private int _maxHp = 30;
        private bool _destroyStarted;
        public int Hp    => _hp;
        public int HpMax => _maxHp;
        public int RepairCost => Mathf.CeilToInt((_maxHp - _hp) / 5f) * 10;

        // Scale dégâts appliqué à ce niveau (ratio vs L1 Phaser convention)
        private float _levelDmgScale = 1f;

        public TowerType? Config => cfg;

        // Research-augmented stat helpers — read-only, computed each call (not cached).
        public float ResearchDamageMul  => cfg != null ? TowerResearchTree.DamageMul(cfg.Id)              : 1f;
        public float ResearchRangeMul   => cfg != null ? TowerResearchTree.RangeMul(cfg.Id)               : 1f;
        public float ResearchFireRateMul => cfg != null ? TowerResearchTree.FireRateIntervalMul(cfg.Id)   : 1f;

        /// <summary>
        /// Returns the upgrade cost for the next level, applying a -20% cluster discount
        /// if 3 or more towers of the same type are placed within radius meters (P1 synergie cluster).
        /// </summary>
        public int GetUpgradeCost()
        {
            if (cfg == null) return 0;
            if (UpgradeLevel >= 3) return 0;
            var bal = BalanceConfig.Get();
            float mul = UpgradeLevel == 1 ? bal.UpgradeMulL2 : bal.UpgradeMulL3;
            float baseCost = cfg.Cost * mul;
            var clusterCount = CountClusterTowers(cfg.Id, 2f);
            float discount = clusterCount >= 3 ? 0.8f : 1f;
            return Mathf.RoundToInt(baseCost * discount);
        }

        /// <summary>
        /// Counts towers of the given typeId (including this one) within radius meters.
        /// Uses PlacementController.PlacedTowers — no physics allocation.
        /// </summary>
        private int CountClusterTowers(string typeId, float radius)
        {
            if (PlacementController.Instance == null) return 0;
            float radiusSq = radius * radius;
            Vector3 myPos = transform.position;
            int count = 0;
            foreach (var t in PlacementController.Instance.PlacedTowers)
            {
                if (t == null) continue;
                if (t.Config?.Id != typeId) continue;
                if ((t.transform.position - myPos).sqrMagnitude <= radiusSq) count++;
            }
            return count;
        }

        /// <summary>
        /// Returns the average DPS over the last 5 seconds based on actual damage events.
        /// </summary>
        public float GetLiveDps()
        {
            float cutoff = Time.time - 5f;
            // Purge stale entries
            while (_damageLogTimes.Count > 0 && _damageLogTimes[0] < cutoff)
            {
                _damageLogTimes.RemoveAt(0);
                _damageLogValues.RemoveAt(0);
            }
            float total = 0f;
            for (int i = 0; i < _damageLogValues.Count; i++)
                total += _damageLogValues[i];
            return total / 5f;
        }

        // Child GO holding the spawned GLTF mesh (null = using placeholder primitives)
        private GameObject? _meshChild;

        // Child GO for the turret head — rotated toward target in Update.
        // Inspector-assignable; falls back to auto-discovery of a child named "Head" or "Turret".
        [SerializeField] private GameObject? _meshHead;

        // Barrel tip transform for muzzle flash position — child named "BarrelTip" if present.
        private Transform? _barrelTip;

        // Recoil state — prevents TickIdleAnim from overriding during recoil.
        private bool _recoiling;

        // Per-tower Perlin seed — desync wobble between instances.
        private float _wobbleSeed;

        // Global throttle — skip cam shake if another tower shook within 50 ms (multi-cannon spam guard).
        private static float _lastCamShakeAt = -1f;

        // Per-tower muzzle flash throttle — skip if same tower fired within 50 ms.
        private float _lastMuzzleFlashAt = -1f;

        // AimLine : thin red laser tower → target (togglable via PlayerPrefs "show_aim_lines_v1")
        private LineRenderer? _aimLine;
        // Prediction line visibility — true when tower is selected or hovered.
        public bool ShowTargetLine = false;

        // Damage type icon — small coloured quad above tower base.
        private GameObject? _damageIconQuad;

        public void Init(TowerType type, GameObject? projPrefab)
        {
            cfg = type;
            cooldown = 0f;
            projectilePrefab = projPrefab;
            _clusterTimer = 0f;
            _slowTickTimer = 0f;
            _heroBuffDmgMul = 1f;
            _idlePhase = Random.value * Mathf.PI * 2f;
            _breathOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            _wobbleSeed   = UnityEngine.Random.Range(0f, 1000f);
            _basePos = transform.position;
            _lastFireAt = 0f;
            UpgradeLevel = 1;
            UpgradeBranch = TowerBranch.None;
            CumulativeCost = type.Cost;
            _l3TintApplied = false;
            _l4EliteApplied = false;
            TowerResearchTree.OnResearchUnlocked -= OnResearchUnlocked;
            TowerResearchTree.OnResearchUnlocked += OnResearchUnlocked;
            Achievements.Instance?.TrackEvent("tower_placed", 1);
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

            // Elemental emission tint — set once, no Update overhead
            ApplyElementalTint(toonRoot);

            // AssetVariants palette swap post-toon
            if (activeSkin != null && activeSkin.ThemeIndex >= 0)
                AssetVariants.ApplyThemeIndex(toonRoot, activeSkin.ThemeIndex);
            else if (activeSkin != null && activeSkin.UseBodyColorOverride)
                AssetVariants.ApplySkin(toonRoot, activeSkin);

            // Colorblind Deuteranopia palette swap (no-op when mode is off)
            Visual.ColorblindPalette.ApplyToGameObject(toonRoot);

            // Animations Mechanim : Idle uniquement pour les tours (pas de Walk, rotation vers cible = code).
            _animator = AnimationController.SetupAnimator(toonRoot, "Idle", null);

            // Auto-discover turret head + barrel tip from GLTF hierarchy.
            // Inspector assignment of _meshHead takes priority.
            if (_meshHead == null && _meshChild != null)
            {
                _meshHead = FindChildNamed(_meshChild.transform, "Head")
                         ?? FindChildNamed(_meshChild.transform, "Turret");
            }
            _barrelTip = _meshHead != null
                ? FindChildNamed(_meshHead.transform, "BarrelTip")?.transform
                : (_meshChild != null ? FindChildNamed(_meshChild.transform, "BarrelTip")?.transform : null);

            BuildRangeRing(type.Range);
            BuildRangeCircle(type.Range);
            BuildSynergyHalo();
            BuildAimLine();
            BuildAffordableHighlight(type.Range);
            BuildUpgradeArrow();
            BuildClusterHighlight();
            BuildDamageIcon(type.DamageType);
            BuildSelectionRing();
            BuildStarRow();
            if (type.Behavior == TowerBehavior.CoinPull)
                BuildMagnetAuraCircle(BalanceConfig.Get().MagnetSlowRadius);

            if (_shimmerRoutine != null) StopCoroutine(_shimmerRoutine);
            _shimmerRoutine = StartCoroutine(ShimmerRoutine());
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
            {
                ApplyL3Branch(branch);
                Achievements.Instance?.Unlock("max_upgrade_tower");
            }

            PostUpgradeVisuals(level);

            // Upgrade VFX : radial burst teinté par niveau (L2=cyan, L3=rainbow) + audio + punch scale + popup
            VfxPool.Instance?.SpawnUpgradeBurst(transform.position + Vector3.up * 1.5f, level);
            AudioController.Instance?.Play3D("tower_upgrade", transform.position);
            AudioController.Instance?.Play3D("powerup", transform.position);
            JuiceFX.Instance?.PunchScale(transform, 1.25f, 0.4f);
            CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                $"L{level}!", transform.position + Vector3.up * 2f, Color.cyan);

#if UNITY_EDITOR
            Debug.Log($"[Tower] UpgradeTo L{level} cost={cost} cumul={CumulativeCost} dmgScale={_levelDmgScale:F2} branch={branch}");
#endif
            Synergies.Instance?.MarkDirty();
            return true;
        }

        private void ApplyElementalTint(GameObject root)
        {
            if (cfg == null) return;
            var renderer = root.GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            Color tint = cfg.Id switch
            {
                "frost"    => new Color(0.4f, 0.7f, 1f),
                "fire"     => new Color(1f, 0.4f, 0.1f),
                "lava"     => new Color(1f, 0.4f, 0.1f),
                "lightning"=> new Color(1f, 0.95f, 0.3f),
                "skyguard" => new Color(1f, 0.95f, 0.3f),
                "poison"   => new Color(0.4f, 1f, 0.4f),
                "acid"     => new Color(0.4f, 1f, 0.4f),
                "mage"     => new Color(0.9f, 0.4f, 1f),
                "portal"   => new Color(0.9f, 0.4f, 1f),
                _          => Color.black,
            };

            if (tint == Color.black) return;

            var mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            mpb.SetColor(_emissionColorId, tint * 0.4f);
            renderer.SetPropertyBlock(mpb);
        }

        /// <summary>
        /// Applique les stats divergentes L3 selon la branche et le type de tour (D1-03).
        /// Tours signature : archer, crossbow, tank, mage.
        /// </summary>
        private void ApplyL3Branch(TowerBranch branch)
        {
            if (cfg == null) return;

            bool isSignature = cfg.Id is "archer" or "crossbow" or "tank" or "mage";

            if (isSignature && branch == TowerBranch.None)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[Tower] L3 signature {cfg.Id} sans branche — fallback Dps");
#endif
                branch = TowerBranch.Dps;
            }

            UpgradeBranch = isSignature ? branch : TowerBranch.None;

            if (!isSignature) return;

            if (_l3StatsTable.TryGetValue((cfg.Id, branch), out var stats))
            {
                ApplyL3Stats(stats);
            }
        }

        /// <summary>
        /// Applique les stats L3 provenant du lookup table vers les propriétés de la tour.
        /// </summary>
        private void ApplyL3Stats(L3Stats stats)
        {
            if (stats.MultiShot != 0) L3MultiShot = stats.MultiShot;
            if (stats.FinalExplosion) L3FinalExplosion = true;
            if (stats.FinalExplosionAoe != 0) L3FinalExplosionAoe = stats.FinalExplosionAoe;
            if (stats.FinalExplosion && cfg != null)
            {
                L3FinalExplosionDmg = cfg.Damage * BalanceConfig.Get().TowerDamageMul * TalentSystem.TowerDamageMul * 2.5f;
            }
            if (stats.Pierce != 0 && cfg != null) L3Pierce = cfg.Pierce + stats.Pierce;
            if (stats.CritChance != 0) L3CritChance = stats.CritChance;
            if (stats.CritMul != 0) L3CritMul = stats.CritMul;
            if (stats.ChainLightningJumps != 0) L3ChainLightningJumps = stats.ChainLightningJumps;
            if (stats.ChainLightningRange != 0) L3ChainLightningRange = stats.ChainLightningRange;
            if (stats.FreezeOnHit) L3FreezeOnHit = true;
            if (stats.FreezeDurMs != 0) L3FreezeDurMs = stats.FreezeDurMs;
            if (stats.BerserkerActive) L3BerserkerActive = true;
            if (stats.BerserkerDmgMul != 0) L3BerserkerDmgMul = stats.BerserkerDmgMul;
            if (stats.BerserkerHpThreshold != 0) L3BerserkerHpThreshold = stats.BerserkerHpThreshold;
            if (stats.BulwarkAura) L3BulwarkAura = true;
            if (stats.BulwarkAuraRange != 0) L3BulwarkAuraRange = stats.BulwarkAuraRange;
            if (stats.BulwarkDmgReduction != 0) L3BulwarkDmgReduction = stats.BulwarkDmgReduction;
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
        /// L3 towers prompt a confirmation dialog before selling.
        /// </summary>
        public void Sell()
        {
            if (cfg == null) return;
            if (UpgradeLevel == 3)
            {
                CrowdDefense.UI.Confirm.Show(
                    "Vendre une tour L3 ?",
                    "Vraiment vendre cette tour L3 ?",
                    () => DoSell());
                return;
            }
            DoSell();
        }

        private void DoSell()
        {
            if (cfg == null) return;
            var bal = BalanceConfig.Get();
            int refund = Mathf.RoundToInt(CumulativeCost * bal.SellRefundRatio);
            Economy.Instance?.AddGold(refund);
            PlacementController.Instance?.UnregisterTower(this);
            Synergies.Instance?.MarkDirty();
#if UNITY_EDITOR
            Debug.Log($"[Tower] Sell cumul={CumulativeCost} refund={refund} ratio={bal.SellRefundRatio:F2}");
#endif
            Vector3 pos = transform.position;
            JuiceFX.Instance?.PunchScale(transform, 0.7f, 0.3f);
            VfxPool.Instance?.SpawnImpact(pos + Vector3.up * 0.5f, new Color(0.55f, 0.55f, 0.55f));
            AudioController.Instance?.Play3D("tower_sold", pos);
            AudioController.Instance?.Play3D("powerup", pos);
            for (int i = 0; i < 5; i++)
            {
                Vector3 offset = Random.insideUnitSphere * 0.5f;
                CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                    "\U0001F4B0", pos + offset, Color.yellow);
            }
            StartCoroutine(PlayDestroyAnim());
        }

        private IEnumerator PlayDestroyAnim()
        {
            _destroyStarted = true;
            if (_windupRoutine != null) { StopCoroutine(_windupRoutine); _windupRoutine = null; }
            if (_selectionRing != null) _selectionRing.gameObject.SetActive(false);
            Vector3 startScale = transform.localScale;
            float targetRotZ = Random.Range(-45f, 45f);
            float startRotZ = transform.localEulerAngles.z;
            Vector3 pos = transform.position;
            VfxPool.Instance?.SpawnDeathPuff(pos + Vector3.up * 0.5f, tier: 1);
            VfxPool.Instance?.SpawnImpact(pos + Vector3.up * 0.3f, new Color(0.65f, 0.55f, 0.4f));
            float elapsed = 0f;
            const float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - t * t;
                transform.localScale = startScale * eased;
                float rot = Mathf.LerpAngle(startRotZ, targetRotZ, t);
                var e = transform.localEulerAngles;
                e.z = rot;
                transform.localEulerAngles = e;
                yield return null;
            }
            Destroy(gameObject);
        }

        private void TakeDamage(int amount)
        {
            _hp = Mathf.Max(0, _hp - amount);
            UpdateHpAlpha();
            if (_hp <= 0 && !_destroyStarted)
            {
                PlacementController.Instance?.UnregisterTower(this);
                Synergies.Instance?.MarkDirty();
                StartCoroutine(PlayDestroyAnim());
            }
        }

        public void ReceiveEnemySplash(int amount)
        {
            TakeDamage(amount);
            VfxPool.Instance?.SpawnImpact(transform.position + Vector3.up * 0.8f, new Color(1f, 0.2f, 0f));
        }

        /// <summary>
        /// Repair : restore HP to max, spend gold (10¢ per 5 HP missing, rounded up).
        /// No-op if full HP or insufficient gold.
        /// </summary>
        public bool Repair()
        {
            if (_hp >= _maxHp) return false;
            int cost = RepairCost;
            if (cost <= 0) return false;
            if (Economy.Instance == null || !Economy.Instance.TrySpend(cost)) return false;
            _hp = _maxHp;
            UpdateHpAlpha();
            VfxPool.Instance?.SpawnImpact(transform.position + Vector3.up * 1.2f, new Color(0.3f, 1f, 0.4f));
            AudioController.Instance?.Play3D("powerup", transform.position);
            CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                "+HP", transform.position + Vector3.up * 2f, new Color(0.3f, 1f, 0.4f));
            return true;
        }

        // Alpha range : 1.0 (full HP) → 0.5 (0 HP), linear.
        private void UpdateHpAlpha()
        {
            float ratio = _maxHp > 0 ? (float)_hp / _maxHp : 1f;
            float alpha = 0.5f + ratio * 0.5f;
            var root = _meshChild != null ? _meshChild : gameObject;
            foreach (var r in root.GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials)
                    if (m != null && m.HasProperty("_BaseColor"))
                    {
                        Color c = m.GetColor("_BaseColor");
                        c.a = alpha;
                        m.SetColor("_BaseColor", c);
                    }
        }

        private void Update()
        {
            if (cfg == null) return;

            TickIdleAnim();
            TickHeadAim();
            TickAimLine();
            TickSelectionRing();

            // Reset bulwark protection each frame; re-set by nearby Bulwark towers below.
            _bulwarkProtected = false;

            // L3 Tank DoT aura — continuous damage to enemies in radius (V5 _tankBlockAura)
            if (L3TankBlockAura) TickTankBlockAura();

            // L3 Tank Bulwark aura — protect adjacent towers -20% dmg (D1-03)
            if (L3BulwarkAura) TickBulwarkAura();

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
            {
                target?.SetTargetedBy(false);
                target = AcquireTarget();
                target?.SetTargetedBy(true);
            }

            if (target != null && cooldown <= 0f)
            {
                if (_windupRoutine != null) StopCoroutine(_windupRoutine);
                _windupRoutine = StartCoroutine(WindupFire(target));
                // L3FireRateMul >1 ralentit la cadence (sniper L3-DPS archer = x2)
                float rateMs = cfg!.FireRateMs * L3FireRateMul * ResearchFireRateMul;
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
            float eff1Range = cfg.Range * ResearchRangeMul;
            float rangeSq = eff1Range * eff1Range;
            var enemies = WaveManager.Instance.ActiveEnemies;
            bool hitAny = false;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - transform.position).sqrMagnitude > rangeSq) continue;
                SlowEffectManager.Instance.ApplySlow(e, cfg.SlowMul, cfg.SlowDurationMs);
                hitAny = true;
            }

            bool isFrost = cfg.Id == "frost" || cfg.Id.Contains("ice");
            if (isFrost && hitAny)
                VfxPool.Instance?.SpawnFrost(transform.position, cfg.Range * 0.5f);
        }

        // ── CoinPull (Magnet) ────────────────────────────────────────────────
        private void UpdateCoinPull()
        {
            if (cfg == null || CoinPullManager.Instance == null) return;
            CoinPullManager.Instance.RegisterSource(
                transform.position,
                cfg.Range,
                cfg.CoinMul > 0f ? cfg.CoinMul : BalanceConfig.Get().MagnetCoinMul);

            // Slow aura 0.7× (V4 synergy) — always active, independent of magnet+tank synergy
            if (SlowEffectManager.Instance == null || WaveManager.Instance == null) return;
            float slowR = BalanceConfig.Get().MagnetSlowRadius;
            float slowR2 = slowR * slowR;
            var myPos = transform.position;
            var active = WaveManager.Instance.ActiveEnemies;
            for (int i = 0; i < active.Count; i++)
            {
                var e = active[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - myPos).sqrMagnitude <= slowR2)
                    SlowEffectManager.Instance.ApplySlow(e, 0.7f, 500);
            }
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
            float effRange = cfg.Range * ResearchRangeMul;
            float rangeSq = effRange * effRange;
            Enemy? best = null;
            float bestScore = float.MinValue;
            var enemies = WaveManager.Instance.ActiveEnemies;
            Vector3 myPos = transform.position;

            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                float distSq = (e.transform.position - myPos).sqrMagnitude;
                if (distSq > rangeSq) continue;

                if (cfg.FlyerOnly && !e.IsFlyer) continue;
                if (e.IsFlyer && !cfg.FlyerOnly && !cfg.CanHitFlyers) continue;
                if (e.StealthAlpha < 0.4f) continue;

                if (_guardMode == GuardMode.AirOnly    && !e.IsFlyer) continue;
                if (_guardMode == GuardMode.GroundOnly &&  e.IsFlyer) continue;

                float score;
                if (e.IsFlyer)
                {
                    // Flyers: always pick closest to castle regardless of player priority
                    float castleDstSq = Castle.Instance != null
                        ? (e.transform.position - Castle.Instance.transform.position).sqrMagnitude
                        : float.MaxValue;
                    score = -castleDstSq;
                }
                else
                {
                    score = _targetPriority switch
                    {
                        TargetPriority.First     => e.CurrentWaypoint,
                        TargetPriority.Last      => -e.CurrentWaypoint,
                        TargetPriority.Strongest => e.HpRatio,
                        TargetPriority.Weakest   => -e.HpRatio,
                        TargetPriority.Closest   => -distSq,
                        _                         => e.CurrentWaypoint,
                    };
                }

                if (best == null || score > bestScore)
                {
                    bestScore = score;
                    best = e;
                }
            }
            return best;
        }

        // ── Windup animation (squash + twitch 0.1 s before firing) ──────────────
        private IEnumerator WindupFire(Enemy t)
        {
            if (_meshChild != null && !_destroyStarted)
            {
                var visual = _meshChild.transform;
                Vector3 baseScale = visual.localScale;
                float twitch = Random.value > 0.5f ? 5f : -5f;

                // Phase 1 (0–0.05 s): squash Y 1→0.85, tilt +5°
                float elapsed = 0f;
                while (elapsed < 0.05f)
                {
                    if (_destroyStarted) { visual.localScale = baseScale; yield break; }
                    elapsed += Time.deltaTime;
                    float t01 = Mathf.Clamp01(elapsed / 0.05f);
                    float sy = Mathf.Lerp(1f, 0.85f, t01);
                    visual.localScale = new Vector3(baseScale.x, baseScale.y * sy, baseScale.z);
                    visual.localRotation = Quaternion.Euler(0f, twitch * t01, 0f);
                    yield return null;
                }

                // Phase 2 (0.05–0.10 s): stretch Y 0.85→1.05, rotation back
                elapsed = 0f;
                while (elapsed < 0.05f)
                {
                    if (_destroyStarted) { visual.localScale = baseScale; yield break; }
                    elapsed += Time.deltaTime;
                    float t01 = Mathf.Clamp01(elapsed / 0.05f);
                    float sy = Mathf.Lerp(0.85f, 1.05f, t01);
                    visual.localScale = new Vector3(baseScale.x, baseScale.y * sy, baseScale.z);
                    visual.localRotation = Quaternion.Euler(0f, twitch * (1f - t01), 0f);
                    yield return null;
                }
                visual.localRotation = Quaternion.identity;
            }

            // Phase 3 (0.10 s): actual shot
            if (!_destroyStarted) ExecuteFire(t);

            // Phase 4 (0.10–0.15 s): settle 1.05→1.0
            if (_meshChild != null && !_destroyStarted)
            {
                var visual = _meshChild.transform;
                Vector3 baseScale = visual.localScale;
                float elapsed = 0f;
                while (elapsed < 0.05f)
                {
                    if (_destroyStarted) { visual.localScale = baseScale; yield break; }
                    elapsed += Time.deltaTime;
                    float t01 = Mathf.Clamp01(elapsed / 0.05f);
                    float sy = Mathf.Lerp(1.05f, 1f, t01);
                    visual.localScale = new Vector3(baseScale.x, baseScale.y * sy, baseScale.z);
                    yield return null;
                }
                visual.localScale = baseScale;
            }

            _windupRoutine = null;
        }

        private void ExecuteFire(Enemy t)
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
            // L1 = default pitch, L2 = deeper (0.9), L3 = deep + reverb key distinct
            string fireSfxKey = UpgradeLevel switch
            {
                2 => "tower_fire_l2",
                3 => "tower_fire_l3",
                _ => "tower_fire_l1",
            };
            float firePitch = UpgradeLevel switch { 2 => 0.9f, 3 => 0.8f, _ => 1.0f };
            // Try tier-specific key; fall back to generic "tower_fire" if missing
            var ac = AudioController.Instance;
            if (ac != null)
            {
                bool hasTierClip = ac.GetClip(fireSfxKey) != null;
                ac.Play3DPitched(
                    hasTierClip ? fireSfxKey : "tower_fire",
                    transform.position,
                    0.60f,
                    firePitch);
                ac.Play3D("tower_shoot", transform.position, 0.55f);
            }
            TriggerCannonShake();
            Vector3 muzzlePos = _barrelTip != null
                ? _barrelTip.position
                : transform.position + Vector3.up * 0.5f;
            VfxPool.Instance?.SpawnImpact(muzzlePos, cfg.ProjectileColor);
            if (Time.time - _lastMuzzleFlashAt >= 0.05f)
            {
                Vector3 flashPos = _barrelTip?.position ?? transform.position + Vector3.up * 0.8f;
                VfxPool.Instance?.SpawnMuzzleFlash(flashPos, cfg.ProjectileColor);
                bool isFrost = cfg.Id == "frost" || cfg.Id.Contains("ice");
                if (!isFrost)
                {
                    var yellowOrange = new Color(1f, 0.65f, 0.1f);
                    VfxPool.Instance?.SpawnSpark(flashPos, yellowOrange);
                    StartCoroutine(MuzzleFlashLightRoutine(flashPos));
                    if (ac != null)
                    {
                        float muzzlePitch = 0.9f + Random.value * 0.2f;
                        bool hasMuzzleClip = ac.GetClip("muzzle_pop") != null;
                        if (hasMuzzleClip)
                            ac.Play3DPitched("muzzle_pop", flashPos, 0.4f, muzzlePitch);
                    }
                }
                _lastMuzzleFlashAt = Time.time;
            }
            VfxPool.Instance?.SpawnAttackStream(muzzlePos, t.transform.position, cfg.ProjectileColor);
            if (_animator != null) _animator.SetTrigger("attackTrigger");
            if (!_recoiling) StartCoroutine(RecoilRoutine());
            _lastFireAt = Time.time;

            // _levelDmgScale encode le scaling Phaser : L1=0.75, L2=1.0, L3=1.30
            // L3DmgMul applique la divergence de branche (D1-03)
            // _heroBuffDmgMul: aura du Hero (ApplyHeroBuff / ClearHeroBuff)
            // _xpDmgMul: XP level-up bonus (+5% per 10 kills, max 50%)
            float dmg = cfg.Damage * BalanceConfig.Get().TowerDamageMul * TalentSystem.TowerDamageMul * ResearchDamageMul * _buffMul * _heroBuffDmgMul * _xpDmgMul * _levelDmgScale * L3DmgMul;
            // Kill streak combo : +5% per stack, max 10 stacks (50%) — decays if gap > 0.5 s
            if (_streakCount > 0 && Time.time - _lastKillTime < StreakWindow * 4f)
                dmg *= 1f + _streakCount * 0.05f;

            // L3 Berserker (tank DPS) : x2 dmg when castle HP ratio < threshold (D1-03)
            if (L3BerserkerActive && Castle.Instance != null && Castle.Instance.HPMax > 0)
            {
                float hpRatio = (float)Castle.Instance.HP / Castle.Instance.HPMax;
                if (hpRatio < L3BerserkerHpThreshold) dmg *= L3BerserkerDmgMul;
            }

            // Base crit (all towers) — modifiable via Talent + Research
            {
                var bal = BalanceConfig.Get();
                float baseCrit = bal.CritChance
                    + TalentSystem.CritChanceBonus
                    + TowerResearchTree.CritChanceBonus(cfg.Id);
                if (baseCrit > 0f && Random.value < baseCrit)
                {
                    dmg *= bal.CritDmgMul;
                    _lastKillWasCrit = true;
                    CrowdDefense.UI.FloatingPopupController.Instance?.SpawnCrit(dmg, t.transform.position);
                    VfxPool.Instance?.SpawnConfetti(t.transform.position, 0.5f, new Color(1f, 0.9f, 0.1f));
                    ac?.Play3DPitched("tower_fire", transform.position, 0.45f, firePitch * 1.35f);
                }
            }

            // L3 Ranger Marksman (archer Utility) : crit hit (D1-03)
            if (L3CritChance > 0f && Random.value < L3CritChance)
            {
                dmg *= L3CritMul;
                _lastKillWasCrit = true;
                VfxPool.Instance?.SpawnConfetti(t.transform.position, 0.5f, new Color(1f, 0.9f, 0.1f));
                ac?.Play3DPitched("tower_fire", transform.position, 0.45f, firePitch * 1.35f);
            }

            if (t.IsFlyer && !t.ImmuneToFlyerBonus)
            {
                float flyMul = Mathf.Max(cfg.FlyerDmgMul, _flyerDmgBonus);
                if (flyMul > 1f) dmg *= flyMul;
            }

            // Track damage for live DPS window (5s rolling) + cumulative stat
            _damageLogTimes.Add(Time.time);
            _damageLogValues.Add(dmg);
            TotalDamageDealt += dmg;

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
            // Archer Master Hunter (L3 DPS) uses 15 degree spread; others default 12 degrees (D1-03)
            int extraShots = _multiShotBonus + L3MultiShot;
            if (extraShots > 0)
            {
                float spreadStep = (cfg.Id == "archer" && UpgradeBranch == TowerBranch.Dps) ? 15f : 12f;
                Vector3 baseDir = (t.transform.position - transform.position).normalized;
                for (int i = 0; i < extraShots; i++)
                {
                    float spreadAngle = (i + 1) * spreadStep;
                    float sign = (i % 2 == 0) ? 1f : -1f;
                    Vector3 spread = Quaternion.Euler(0f, spreadAngle * sign, 0f) * baseDir;
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

            // L3 Frostmage (mage Utility) : freeze on hit 0.5s (D1-03)
            if (L3FreezeOnHit && SlowEffectManager.Instance != null)
                SlowEffectManager.Instance.ApplySlow(t, 0f, L3FreezeDurMs);

            // DOT feedback popups — throttled per tower (0.5s cooldown)
            if (Time.time - _lastDotPopupAt >= 0.5f)
            {
                string? dotIcon = null;
                Color dotColor = Color.white;
                if (L3BurnDot)          { dotIcon = "\U0001F525"; dotColor = new Color(1f, 0.45f, 0.05f); }
                else if (L3FreezeOnHit) { dotIcon = "❄️"; dotColor = new Color(0.4f, 0.9f, 1f); }
                else if (L3SlowOnHit)   { dotIcon = "❄️"; dotColor = new Color(0.4f, 0.9f, 1f); }
                if (dotIcon != null)
                {
                    CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                        dotIcon, transform.position + Vector3.up * 1.5f, dotColor);
                    _lastDotPopupAt = Time.time;
                }
            }

            // L3 Archmage (mage DPS) : chain lightning jumps to nearby enemies (D1-03)
            if (L3ChainLightningJumps > 0)
                FireChainLightning(t, dmg * 0.6f, L3ChainLightningJumps, L3ChainLightningRange);
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

        public void SetTargetPriority(TargetPriority priority)
        {
            _targetPriority = priority;
            target?.SetTargetedBy(false);
            target = null; // force re-acquire with new priority
        }

        public void SetGuardMode(GuardMode mode)
        {
            _guardMode = mode;
            target?.SetTargetedBy(false);
            target = null; // force re-acquire with new filter
        }

        // ── Aim Line ──────────────────────────────────────────────────────────

        private void BuildAimLine()
        {
            var go = new GameObject("AimLine");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            _aimLine = go.AddComponent<LineRenderer>();
            _aimLine.positionCount = 2;
            _aimLine.useWorldSpace = true;
            _aimLine.startWidth = 0.05f;
            _aimLine.endWidth   = 0.02f;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(1f, 1f, 1f, 0.5f);
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }
            _aimLine.material = mat;
            go.SetActive(false);
        }

        private static Color DamageTypeColor(DamageType dt) => dt switch
        {
            DamageType.Physical => new Color(0.75f, 0.75f, 0.75f), // silver
            DamageType.Magic    => new Color(0.60f, 0.20f, 0.90f), // violet
            DamageType.Frost    => new Color(0.20f, 0.85f, 1.00f), // cyan
            DamageType.Fire     => new Color(1.00f, 0.35f, 0.05f), // red-orange
            _                   => Color.white,
        };

        private void BuildDamageIcon(DamageType dt)
        {
            if (_damageIconQuad != null) Destroy(_damageIconQuad);
            _damageIconQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _damageIconQuad.name = "DamageIcon";
            // Remove collider — decorative only.
            Destroy(_damageIconQuad.GetComponent<Collider>());
            var t = _damageIconQuad.transform;
            t.SetParent(transform, worldPositionStays: false);
            t.localPosition = new Vector3(0f, 0.55f, 0f);
            t.localRotation = Quaternion.Euler(90f, 0f, 0f); // face camera (top-down)
            t.localScale    = new Vector3(0.15f, 0.15f, 0.15f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = DamageTypeColor(dt);
            _damageIconQuad.GetComponent<MeshRenderer>().material = mat;
            var settings = CrowdDefense.UI.SettingsRegistry.Instance;
            bool show = settings != null && settings.ShowDamageIcons;
            _damageIconQuad.SetActive(show);
            if (settings != null)
                settings.OnSettingsChanged += RefreshDamageIconVisibility;
        }

        private void RefreshDamageIconVisibility()
        {
            if (_damageIconQuad == null) return;
            var settings = CrowdDefense.UI.SettingsRegistry.Instance;
            bool show = settings != null && settings.ShowDamageIcons;
            _damageIconQuad.SetActive(show);
        }

        private void TickAimLine()
        {
            if (_aimLine == null) return;
            if (!ShowTargetLine || target == null || target.IsDead)
            {
                if (_aimLine.gameObject.activeSelf) _aimLine.gameObject.SetActive(false);
                return;
            }
            if (!_aimLine.gameObject.activeSelf) _aimLine.gameObject.SetActive(true);
            Vector3 start = _barrelTip != null
                ? _barrelTip.position
                : transform.position + Vector3.up * 1f;
            _aimLine.SetPosition(0, start);
            _aimLine.SetPosition(1, target.transform.position + Vector3.up * 0.5f);
            Color c = cooldown <= 0.1f ? Color.red : new Color(1f, 1f, 1f, 0.5f);
            _aimLine.startColor = c;
            _aimLine.endColor   = new Color(c.r, c.g, c.b, 0f);
        }

        // ── Selection Ring ────────────────────────────────────────────────────

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (_selectionRing != null)
                _selectionRing.gameObject.SetActive(selected && !_destroyStarted);
        }

        private void BuildSelectionRing()
        {
            if (_selectionRing != null)
            {
                Destroy(_selectionRing.gameObject);
                _selectionRing = null;
            }

            const int segments = 32;
            var go = new GameObject("SelectionRing");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = segments;
            lr.startWidth = 0.07f;
            lr.endWidth   = 0.07f;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3002;
            }
            lr.material = mat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            const float baseRadius = 0.8f;
            float step = 2f * Mathf.PI / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * step;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * baseRadius, 0f, Mathf.Sin(angle) * baseRadius));
            }

            go.SetActive(false);
            _selectionRing = lr;
        }

        private void TickSelectionRing()
        {
            if (_selectionRing == null || !_isSelected || _destroyStarted) return;

            float t = Time.time * 3f;
            float radius = 0.8f + Mathf.Sin(t) * 0.1f;
            float alpha  = 0.6f + Mathf.Sin(t) * 0.3f;

            var c = new Color(0.2f, 0.9f, 1f, alpha);
            _selectionRing.startColor = c;
            _selectionRing.endColor   = c;

            const int segments = 32;
            float step = 2f * Mathf.PI / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * step;
                _selectionRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }

        // ── Range Ring ────────────────────────────────────────────────────────

        /// <summary>
        /// Shows or hides the range ring circle (useful during placement).
        /// </summary>
        public void ShowRangeRing(bool visible)
        {
            if (_rangeRing != null)
                _rangeRing.SetActive(visible);
            if (_rangeCircle != null)
                _rangeCircle.enabled = visible;
        }

        private void BuildRangeRing(float range)
        {
            if (_rangeRing != null)
                Destroy(_rangeRing);

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "RangeRing";
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float diameter = range * 2f;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);
            Object.Destroy(go.GetComponent<Collider>());

            const int texSize = 64;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[texSize * texSize];
            float half = texSize * 0.5f;
            for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                // smoothstep fade : opaque near center, transparent at edge
                float alpha = Mathf.SmoothStep(1f, 0f, dist) * 0.4f;
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
                pixels[y * texSize + x] = new Color32(102, 222, 255, a);
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.mainTexture = tex;
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.material = mat;

            go.SetActive(false);
            _rangeRing = go;
        }

        private void BuildRangeCircle(float range)
        {
            if (_rangeCircle != null)
            {
                Destroy(_rangeCircle.gameObject);
                _rangeCircle = null;
            }

            var go = new GameObject("RangeCircle");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 64;
            lr.startWidth = 0.06f;
            lr.endWidth   = 0.06f;
            lr.startColor = new Color(1f, 1f, 1f, 0.4f);
            lr.endColor   = new Color(1f, 1f, 1f, 0.4f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3001;
            }
            mat.color = new Color(1f, 1f, 1f, 0.4f);
            lr.material = mat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            const int segments = 64;
            float step = 2f * Mathf.PI / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * step;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * range, 0f, Mathf.Sin(angle) * range));
            }

            lr.enabled = false;
            _rangeCircle = lr;
        }

        // ── Cluster Highlight ─────────────────────────────────────────────────

        public void ShowClusterHighlight(bool visible)
        {
            if (_clusterHighlight != null)
                _clusterHighlight.SetActive(visible);
        }

        private void BuildClusterHighlight()
        {
            if (_clusterHighlight != null)
                Destroy(_clusterHighlight);

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "ClusterHighlight";
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
            Object.Destroy(go.GetComponent<Collider>());

            const int texSize = 64;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[texSize * texSize];
            float half = texSize * 0.5f;
            for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                // Ring : visible seulement près du bord (0.75..1.0)
                float ring = Mathf.SmoothStep(0f, 1f, (dist - 0.72f) / 0.12f)
                           * Mathf.SmoothStep(0f, 1f, (1f - dist) / 0.08f);
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(ring * 0.75f) * 255f);
                pixels[y * texSize + x] = new Color32(255, 220, 40, a);   // jaune subtil
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.mainTexture = tex;
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3001;
            }
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.material = mat;

            go.SetActive(false);
            _clusterHighlight = go;
        }

        // ── Magnet Slow Aura Circle ───────────────────────────────────────────

        private void BuildMagnetAuraCircle(float radius)
        {
            if (_magnetAuraCircle != null)
            {
                Destroy(_magnetAuraCircle.gameObject);
                _magnetAuraCircle = null;
            }

            var go = new GameObject("MagnetAuraCircle");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 48;
            lr.startWidth = 0.08f;
            lr.endWidth   = 0.08f;
            var auColor = new Color(0.55f, 0.2f, 0.9f, 0.55f);  // violet translucide
            lr.startColor = auColor;
            lr.endColor   = auColor;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = auColor;
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3001;
            }
            lr.material = mat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            const int segs = 48;
            float step = 2f * Mathf.PI / segs;
            for (int i = 0; i < segs; i++)
            {
                float a = i * step;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }

            lr.enabled = true;
            _magnetAuraCircle = lr;
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

        // ── LateUpdate — billboard stars toward camera ────────────────────────

        private void LateUpdate()
        {
            if (_starRow == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            // Billboard: rotate star row to face the camera each frame.
            _starRow.transform.rotation = cam.transform.rotation;
            // Animate star 3 (index 2) rainbow when L3 active — no alloc, cached material.
            if (UpgradeLevel >= 3 && _starMaterials[2] != null)
            {
                Color rainbow = Color.HSVToRGB(Time.time * 0.5f % 1f, 0.7f, 1f);
                _starMaterials[2]!.color = rainbow;
            }
        }

        // ── Star Row ──────────────────────────────────────────────────────────

        private void BuildStarRow()
        {
            if (_starRow != null) Destroy(_starRow);

            _starRow = new GameObject("StarRow");
            _starRow.transform.SetParent(transform, false);
            _starRow.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            var spriteMat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));

            float spacing = 0.38f;
            float startX  = -(spacing);   // -0.38, 0, +0.38

            for (int i = 0; i < 3; i++)
            {
                var star = GameObject.CreatePrimitive(PrimitiveType.Quad);
                star.name = "Star_" + (i + 1);
                Object.Destroy(star.GetComponent<Collider>());
                star.transform.SetParent(_starRow.transform, false);
                star.transform.localPosition = new Vector3(startX + i * spacing, 0f, 0f);
                star.transform.localScale = Vector3.one * 0.28f;

                var mat = new Material(spriteMat);
                mat.color = new Color(StarSilver.r, StarSilver.g, StarSilver.b, StarDimAlpha);
                var rend = star.GetComponent<Renderer>();
                if (rend != null) rend.material = mat;

                _starRenderers[i] = rend;
                _starMaterials[i] = mat;
            }

            // Destroy the shared template material — each star has its own copy above.
            Object.Destroy(spriteMat);
            UpdateStarRow();
        }

        private void UpdateStarRow()
        {
            for (int i = 0; i < 3; i++)
            {
                var mat = _starMaterials[i];
                if (mat == null) continue;

                bool lit = UpgradeLevel > i;   // star[0] lit at L1+, star[1] at L2+, star[2] at L3+
                if (!lit)
                {
                    // Colour the dim star its target colour but at low alpha so shape is visible.
                    Color dimBase = i == 0 ? StarSilver : i == 1 ? StarGold : StarGold;
                    mat.color = new Color(dimBase.r, dimBase.g, dimBase.b, StarDimAlpha);
                }
                else if (i == 0)
                {
                    mat.color = StarSilver;
                }
                else if (i == 1)
                {
                    mat.color = StarGold;
                }
                else
                {
                    // Star 3 (L3 rainbow): initial colour — will be updated each frame in LateUpdate.
                    mat.color = StarGold;
                }
            }
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

        // ── L3 Archmage Chain Lightning ───────────────────────────────────────
        // Jumps from initial target to up to jumps additional nearby enemies (D1-03)
        private void FireChainLightning(Enemy origin, float dmg, int jumps, float range)
        {
            if (WaveManager.Instance == null || jumps <= 0) return;
            float rangeSq = range * range;
            _chainHitBuffer.Clear();
            _chainHitBuffer.Add(origin);
            var hit = _chainHitBuffer;
            Enemy? current = origin;

            VfxPool.Instance?.SpawnImpact(origin.transform.position, new Color(0.4f, 0.7f, 1f));

            for (int j = 0; j < jumps; j++)
            {
                Enemy? next = null;
                float bestDist = rangeSq;
                var enemies = WaveManager.Instance.ActiveEnemies;
                for (int i = 0; i < enemies.Count; i++)
                {
                    var e = enemies[i];
                    if (e == null || e.IsDead || hit.Contains(e)) continue;
                    float d = (e.transform.position - current!.transform.position).sqrMagnitude;
                    if (d < bestDist) { bestDist = d; next = e; }
                }
                if (next == null) break;
                next.TakeDamage(dmg);
                VfxPool.Instance?.SpawnImpact(next.transform.position, new Color(0.4f, 0.7f, 1f));
                hit.Add(next);
                current = next;
                dmg *= 0.8f; // each jump loses 20% dmg
            }
        }

        // ── L3 Tank Bulwark Aura ──────────────────────────────────────────────
        // Sets _bulwarkProtected on adjacent towers each frame; towers read flag in damage calc (D1-03)
        private void TickBulwarkAura()
        {
            if (PlacementController.Instance == null) return;
            float rangeSq = L3BulwarkAuraRange * L3BulwarkAuraRange;
            Vector3 myPos = transform.position;
            foreach (var t in PlacementController.Instance.PlacedTowers)
            {
                if (t == null || t == this) continue;
                t._bulwarkProtected = (t.transform.position - myPos).sqrMagnitude < rangeSq;
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
            TickAffordableHighlight();
            TickUpgradeArrow();
            TickHitFlash();

            bool isSelected = PlacementController.Instance?.SelectedTower == this;
            bool recentFire = (Time.time - _lastFireAt) < 0.2f;

            if (!isSelected && !recentFire)
            {
                float bobY = _basePos.y + Mathf.Sin(Time.time * 5f + _idlePhase) * 0.05f;
                transform.position = new Vector3(_basePos.x, bobY, _basePos.z);
            }

            if (_meshChild == null) return;

            TickBreathing(recentFire);

            if (recentFire) return;

            float t = Time.time;
            float phase = _idlePhase;
            _meshChild.transform.localPosition = new Vector3(
                0f,
                Mathf.Sin(t * 5f + phase) * 0.05f,
                0f);
            _meshChild.transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Sin(t * 0.8f + phase) * 1.7f);
        }

        private void TickBreathing(bool isFiring)
        {
            if (_meshChild == null) return;
            float amp = isFiring ? 0.025f : 0.015f;
            float s = 1f + Mathf.Sin(Time.time * 1.5f + _breathOffset) * amp;
            _meshChild.transform.localScale = new Vector3(s, s, s);
        }

        // Called each frame from TickIdleAnim; drives hit-confirmation emission flash.
        public void FlashHitConfirmation() => _hitFlashElapsed = 0f; // reset / retrigger

        private void TickHitFlash()
        {
            if (_hitFlashElapsed >= HitFlashTotal) return;
            _hitFlashElapsed += Time.deltaTime;

            float intensity;
            if (_hitFlashElapsed < HitFlashPeak)
                intensity = Mathf.Lerp(0.5f, 0.5f, _hitFlashElapsed / HitFlashPeak); // hold at peak
            else
                intensity = Mathf.Lerp(0.5f, 0f, (_hitFlashElapsed - HitFlashPeak) / (HitFlashTotal - HitFlashPeak));

            var root = _meshChild != null ? _meshChild : gameObject;
            var renderers = root.GetComponentsInChildren<Renderer>();
            _hitFlashMpb ??= new MaterialPropertyBlock();
            var emission = Color.white * intensity;
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(_hitFlashMpb);
                _hitFlashMpb.SetColor(_emissionColorId, emission);
                r.SetPropertyBlock(_hitFlashMpb);
            }

            // Clear emission once animation completes
            if (_hitFlashElapsed >= HitFlashTotal)
            {
                foreach (var r in renderers)
                {
                    r.GetPropertyBlock(_hitFlashMpb);
                    _hitFlashMpb.SetColor(_emissionColorId, Color.black);
                    r.SetPropertyBlock(_hitFlashMpb);
                }
            }
        }

        private IEnumerator ShimmerRoutine()
        {
            var wait3 = new WaitForSeconds(3f);
            while (true)
            {
                yield return wait3;
                var root = _meshChild != null ? _meshChild : gameObject;
                var renderers = root.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                _shimmerMpb ??= new MaterialPropertyBlock();
                const float duration = 0.4f;
                float half = duration * 0.5f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    float alpha = elapsed < half
                        ? Mathf.Lerp(0f, 0.2f, elapsed / half)
                        : Mathf.Lerp(0.2f, 0f, (elapsed - half) / half);
                    var tint = new Color(1f, 1f, 1f, alpha);
                    foreach (var r in renderers)
                    {
                        r.GetPropertyBlock(_shimmerMpb);
                        _shimmerMpb.SetColor(_shimmerColorId, tint);
                        r.SetPropertyBlock(_shimmerMpb);
                    }
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                // restore original property block state (alpha 0 = invisible tint)
                var clear = new Color(1f, 1f, 1f, 0f);
                foreach (var r in renderers)
                {
                    r.GetPropertyBlock(_shimmerMpb);
                    _shimmerMpb.SetColor(_shimmerColorId, clear);
                    r.SetPropertyBlock(_shimmerMpb);
                }
            }
        }

        private void TickSynergyHalo()
        {
            if (_synergyHaloRenderer == null) return;
            _haloMpb ??= new MaterialPropertyBlock();
            _synergyHaloRenderer.GetPropertyBlock(_haloMpb);
            float prevAlpha = _haloMpb.GetColor(_haloColorId).a;
            float targetAlpha = _synergyActive
                ? 0.5f + 0.3f * Mathf.Sin(Time.time * 2f * Mathf.PI)
                : 0f;
            float nextAlpha = Mathf.Lerp(prevAlpha, targetAlpha, 0.15f);
            var baseColor = new Color(0.4f, 0.8f, 1f, nextAlpha); // cyan 1Hz pulse
            _haloMpb.SetColor(_haloColorId, baseColor);
            _synergyHaloRenderer.SetPropertyBlock(_haloMpb);
        }

        // ── Affordable Upgrade Highlight ──────────────────────────────────────

        private void BuildAffordableHighlight(float range)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "AffordableHighlight";
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float size = range * 0.3f;
            go.transform.localScale = new Vector3(size, size, 1f);
            Object.Destroy(go.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(1f, 0.85f, 0.3f, 0f);
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }
            _affordHighlightRenderer = go.GetComponent<Renderer>();
            if (_affordHighlightRenderer != null) _affordHighlightRenderer.material = mat;
            _affordMpb = new MaterialPropertyBlock();
            go.SetActive(false);
            _affordableHighlight = go;
        }

        private void TickAffordableHighlight()
        {
            if (_affordableHighlight == null || cfg == null || UpgradeLevel >= 3) return;
            _affordCheckTimer -= Time.deltaTime;
            if (_affordCheckTimer > 0f)
            {
                // Still animate alpha when visible
                if (_affordableHighlight.activeSelf && _affordHighlightRenderer != null && _affordMpb != null)
                {
                    float alpha = 0.2f + 0.3f * (0.5f + 0.5f * Mathf.Sin(Time.time * 3.5f));
                    _affordMpb.SetColor("_BaseColor", new Color(1f, 0.85f, 0.3f, alpha));
                    _affordHighlightRenderer.SetPropertyBlock(_affordMpb);
                }
                return;
            }
            _affordCheckTimer = 0.5f;

            var bal = BalanceConfig.Get();
            float mul = UpgradeLevel == 1 ? bal.UpgradeMulL2 : bal.UpgradeMulL3;
            int cost = Mathf.RoundToInt(cfg.Cost * mul);
            bool canAfford = Economy.Instance != null && Economy.Instance.Gold >= cost;
            _affordableHighlight.SetActive(canAfford);
        }

        private void BuildUpgradeArrow()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "UpgradeArrow";
            go.transform.SetParent(transform);
            // Start 2.2 units above tower, faces camera-up (rotated flat then billboarded via Y bob)
            _upgradeArrowBaseLocalPos = new Vector3(0f, 2.2f, 0f);
            go.transform.localPosition = _upgradeArrowBaseLocalPos;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            go.transform.localScale = new Vector3(0.45f, 0.55f, 1f);
            Object.Destroy(go.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            // Gold color
            mat.color = new Color(1f, 0.82f, 0.1f, 1f);
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }
            _upgradeArrowRenderer = go.GetComponent<Renderer>();
            if (_upgradeArrowRenderer != null) _upgradeArrowRenderer.material = mat;
            _upgradeArrowMpb = new MaterialPropertyBlock();
            go.SetActive(false);
            _upgradeArrow = go;
        }

        private void TickUpgradeArrow()
        {
            if (_upgradeArrow == null || cfg == null || UpgradeLevel >= 3) return;
            _upgradeArrowCheckTimer -= Time.deltaTime;
            if (_upgradeArrowCheckTimer <= 0f)
            {
                _upgradeArrowCheckTimer = 0.5f;
                int cost = GetUpgradeCost();
                bool canAfford = cost > 0 && Economy.Instance != null && Economy.Instance.Gold >= cost;
                _upgradeArrow.SetActive(canAfford);
            }

            if (!_upgradeArrow.activeSelf) return;

            // Bob up/down at 0.5 Hz (1 full cycle / 2 s), amplitude ±0.15
            float bobY = Mathf.Sin(Time.time * Mathf.PI) * 0.15f; // 0.5 Hz = PI rad/s
            _upgradeArrow.transform.localPosition = _upgradeArrowBaseLocalPos + new Vector3(0f, bobY, 0f);

            // Pulse alpha 0.7–1.0 in sync with bob
            if (_upgradeArrowRenderer != null && _upgradeArrowMpb != null)
            {
                float alpha = 0.7f + 0.3f * (0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI));
                _upgradeArrowMpb.SetColor("_BaseColor", new Color(1f, 0.82f, 0.1f, alpha));
                _upgradeArrowRenderer.SetPropertyBlock(_upgradeArrowMpb);
            }
        }

        // ── Head Aim ──────────────────────────────────────────────────────────

        // Smoothly rotates _meshHead toward the current target each frame (Y-axis only).
        // On target switch : snaps instantly for quick reaction.
        // Boss towers (L3) rotate at 12 deg/sec ; others at 8 deg/sec.
        // Passive towers (frost/magnet/portal) skip aim entirely.
        private void TickHeadAim()
        {
            if (_meshHead == null || target == null || target.IsDead) return;
            if (cfg == null) return;

            // Passive towers have no aim — skip wobble too.
            bool isPassive = cfg.Id == "frost" || cfg.Id.Contains("ice")
                          || cfg.Id == "magnet"
                          || cfg.Id == "portal";
            if (isPassive) return;

            Vector3 dir = target.transform.position - _meshHead.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion desired = Quaternion.LookRotation(dir);

            bool switched = !ReferenceEquals(target, _prevAimTarget);
            _prevAimTarget = target;

            if (switched)
            {
                _meshHead.transform.rotation = desired;
                return;
            }

            float degsPerSec = UpgradeLevel >= 3 ? 12f : 8f;
            Quaternion tracked = Quaternion.RotateTowards(
                _meshHead.transform.rotation, desired, degsPerSec * Time.deltaTime);

            // Visual-only wobble — does NOT affect projectile direction (callers use `desired`).
            // ±1° idle, ±2° in the 0.6 s window after a shot (residual recoil tremor).
            float amp   = (Time.time - _lastFireAt) < 0.6f ? 2f : 1f;
            float speed = 0.8f;
            float wx = (Mathf.PerlinNoise(Time.time * speed + _wobbleSeed,        0f) * 2f - 1f) * amp;
            float wy = (Mathf.PerlinNoise(0f, Time.time * speed + _wobbleSeed + 3.7f) * 2f - 1f) * amp;
            _meshHead.transform.rotation = Quaternion.Euler(wx, 0f, 0f) * tracked * Quaternion.Euler(0f, wy, 0f);
        }

        // Lerp head -0.5 local-Z then back to 0 for a snap recoil feel.
        private IEnumerator RecoilRoutine()
        {
            _recoiling = true;
            if (_meshHead == null) { _recoiling = false; yield break; }

            Vector3 origin = _meshHead.transform.localPosition;
            Vector3 back   = origin + _meshHead.transform.localRotation * new Vector3(0f, 0f, -0.5f);

            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(t + Time.deltaTime / 0.06f, 1f);
                _meshHead.transform.localPosition = Vector3.Lerp(origin, back, t);
                yield return null;
            }
            t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(t + Time.deltaTime / 0.12f, 1f);
                _meshHead.transform.localPosition = Vector3.Lerp(back, origin, t);
                yield return null;
            }
            _meshHead.transform.localPosition = origin;
            _recoiling = false;
        }

        // ── Muzzle Flash Light ────────────────────────────────────────────────

        // Spawns a short-lived point light at the muzzle position; intensity lerps 4→0 over 0.1s then destroys GO.
        private IEnumerator MuzzleFlashLightRoutine(Vector3 worldPos)
        {
            var go = new GameObject("MuzzleFlash");
            go.transform.position = worldPos;
            var light = go.AddComponent<Light>();
            light.type      = LightType.Point;
            light.color     = new Color(1f, 0.95f, 0.5f);
            light.range     = 2f;
            light.intensity = 4f;
            light.shadows   = LightShadows.None;

            float elapsed = 0f;
            const float Duration = 0.1f;
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                light.intensity = Mathf.Lerp(4f, 0f, elapsed / Duration);
                yield return null;
            }
            Destroy(go);
        }

        // ── Utility ───────────────────────────────────────────────────────────

        private static GameObject? FindChildNamed(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == childName) return child.gameObject;
                var found = FindChildNamed(child, childName);
                if (found != null) return found;
            }
            return null;
        }

        // ── Cannon Camera Shake ───────────────────────────────────────────────

        // Only cannon / ballista L3 multishot trigger a camera shake; all other towers stay silent.
        // Global 50 ms throttle prevents spam when multiple cannons fire the same frame.
        private void TriggerCannonShake()
        {
            if (cfg == null) return;
            bool isCannon = cfg.Id.Contains("cannon");
            bool isHeavy  = isCannon && (UpgradeLevel >= 3 || cfg.Id != "cannon");
            bool isBallistaL3Multishot = cfg.Id == "ballista" && UpgradeLevel >= 3 && L3MultiShot > 0;
            if (!isCannon && !isBallistaL3Multishot) return;

            if (Time.unscaledTime - _lastCamShakeAt < 0.05f) return;
            _lastCamShakeAt = Time.unscaledTime;

            var jc = JuiceConfig.Get();
            if (isHeavy || isBallistaL3Multishot)
                JuiceFX.Instance?.Shake(jc?.TowerFireShakeAmp ?? 0.30f, jc?.TowerFireShakeMs ?? 150);
            else
                JuiceFX.Instance?.Shake((jc?.TowerFireShakeAmp ?? 0.30f) * 0.5f, jc?.TowerFireShakeMs ?? 100);
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

            float dmg = cfg.Damage * BalanceConfig.Get().TowerDamageMul * TalentSystem.TowerDamageMul * ResearchDamageMul * _buffMul * _heroBuffDmgMul * _xpDmgMul * _levelDmgScale * L3DmgMul;
            if (_streakCount > 0 && Time.time - _lastKillTime < StreakWindow * 4f)
                dmg *= 1f + _streakCount * 0.05f;

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
            UpdateStarRow();
            if (cfg != null)
            {
                BuildRangeRing(cfg.Range);
                BuildRangeCircle(cfg.Range);
            }
            ApplyTierSkin(level);
            if (level >= 3) TryApplyEliteL4();
            SpawnUpgradeRing(level);
        }

        private void SpawnUpgradeRing(int newLevel)
        {
            const int Count = 16;
            const float Radius = 0.5f;
            const float Duration = 0.6f;
            const float Speed = 3f;

            var origin = transform.position + Vector3.up * 0.05f;

            for (int i = 0; i < Count; i++)
            {
                float angle = i / (float)Count * 2f * Mathf.PI;
                var startPos = origin + new Vector3(Mathf.Cos(angle) * Radius, 0f, Mathf.Sin(angle) * Radius);
                var velocity = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Speed;

                Color color = newLevel switch
                {
                    3 => Color.HSVToRGB(i / (float)Count, 0.7f, 1f),
                    2 => new Color(1f, 0.85f, 0.2f),
                    _ => new Color(0.8f, 0.8f, 0.85f)
                };

                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "UpgradeRing_Particle";
                go.transform.position = startPos;
                go.transform.localScale = Vector3.one * 0.3f;
                Object.Destroy(go.GetComponent<Collider>());

                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    var baseMat = rend.sharedMaterial != null ? rend.sharedMaterial : new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit")!);
                    var mat = new Material(baseMat);
                    mat.color = color;
                    rend.material = mat;
                }

                StartCoroutine(AnimateRingParticle(go, velocity, Duration));
            }

            var ac = AudioController.Instance;
            if (ac != null)
            {
                float pitch = newLevel switch { 2 => 1.1f, 3 => 1.35f, _ => 1f };
                ac.Play3DPitched("upgrade_ring_chime", transform.position, 1f, pitch);
            }
        }

        private static IEnumerator AnimateRingParticle(GameObject go, Vector3 velocity, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (go == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                go.transform.position += velocity * Time.deltaTime;
                float scale = Mathf.Lerp(0.3f, 0f, t);
                go.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            if (go != null) Object.Destroy(go);
        }

        /// <summary>
        /// Tier 1 = default (set at Init). Tier 2 = try _t2 mesh swap, fallback silver tint.
        /// Tier 3 = try _t3 mesh swap, fallback gold tint + emission.
        /// </summary>
        public void ApplyTierSkin(int tier)
        {
            if (cfg == null || tier <= 1) return;

            string suffix = tier == 2 ? "_t2" : "_t3";
            string variantKey = cfg.AssetKey + suffix;

            var registry = Resources.Load<AssetRegistry>("AssetRegistry");
            if (registry == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[Tower] AssetRegistry not found in ApplyTierSkin — using color tint fallback");
#endif
                var tintRoot = _meshChild != null ? _meshChild : gameObject;
                var fallbackColor = tier == 2 ? new Color(0.95f, 0.90f, 0.80f) : new Color(1f, 0.8f, 0.3f);
                MaterialController.UpdateTint(tintRoot, fallbackColor);
                if (tier == 3) SpawnGlowRing();
                return;
            }
            var variantPrefab = registry.Get(variantKey);

            if (variantPrefab != null)
            {
                SwapMeshChild(variantPrefab, variantKey);
            }
            else
            {
                // Fallback tint on existing mesh subtree
                var tintRoot = _meshChild != null ? _meshChild : gameObject;
                if (tier == 2)
                {
                    // L2 : gold trim — base tint brighter +0.1 (warm highlight)
                    MaterialController.UpdateTint(tintRoot, new Color(0.95f, 0.90f, 0.80f));
                }
                else
                {
                    // Gold + emission
                    Color gold = new Color(1f, 0.8f, 0.3f);
                    MaterialController.UpdateTint(tintRoot, gold);
                    foreach (var r in tintRoot.GetComponentsInChildren<Renderer>())
                    {
                        foreach (var m in r.materials)
                        {
                            if (m == null) continue;
                            if (m.HasProperty("_EmissionColor"))
                            {
                                m.SetColor("_EmissionColor", gold * 0.3f);
                                m.EnableKeyword("_EMISSION");
                            }
                        }
                    }
                    SpawnGlowRing();
                }
            }
        }

        private void SwapMeshChild(GameObject variantPrefab, string variantKey)
        {
            if (_meshChild != null)
                Destroy(_meshChild);

            // Disable placeholder primitives (Base + Top)
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == "Base" || child.name == "Top")
                    child.gameObject.SetActive(false);
            }

            var inst = Object.Instantiate(variantPrefab, transform);
            inst.name = "Mesh_" + variantKey;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            _meshChild = inst;

            // Reapply toon + outline on new mesh
            Color bodyColor = cfg != null ? cfg.BodyColor : Color.white;
            MaterialController.ApplyToon(_meshChild, bodyColor);
            Outline.ApplyToHierarchy(_meshChild.transform);

            // Relink animator
            _animator = AnimationController.SetupAnimator(_meshChild, "Idle", null);

            // Re-discover turret head
            _meshHead = FindChildNamed(_meshChild.transform, "Head")
                     ?? FindChildNamed(_meshChild.transform, "Turret");
            _barrelTip = _meshHead != null
                ? FindChildNamed(_meshHead.transform, "BarrelTip")?.transform
                : FindChildNamed(_meshChild.transform, "BarrelTip")?.transform;
        }

        private void SpawnGlowRing()
        {
            if (_glowRing != null) return;

            const int verts = 24;
            const float radius = 0.75f;

            _glowRing = new GameObject("GlowRing_L3");
            _glowRing.transform.SetParent(transform, false);
            _glowRing.transform.localPosition = new Vector3(0f, 0.1f, 0f);

            var lr = _glowRing.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = verts;
            lr.startWidth = 0.08f;
            lr.endWidth = 0.08f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            for (int i = 0; i < verts; i++)
            {
                float angle = i / (float)verts * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            lr.material = mat;

            _glowPulseRoutine = StartCoroutine(GlowPulseRoutine(lr, mat));
        }

        private IEnumerator GlowPulseRoutine(LineRenderer lr, Material mat)
        {
            Color baseColor = mat.color;
            while (true)
            {
                float t = Mathf.Sin(Time.time * Mathf.PI * 2f) * 0.5f + 0.5f;
                float alpha = Mathf.Lerp(0.4f, 0.95f, t);
                float width = Mathf.Lerp(0.05f, 0.12f, t);
                mat.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                lr.startWidth = width;
                lr.endWidth = width;
                yield return null;
            }
        }

        /// <summary>
        /// Promotes tower to elite L4 tier when UpgradeLevel >= 3 and all research nodes unlocked.
        /// Idempotent — safe to call multiple times.
        /// Effects : deeper gold tint, bigger glow halo (radius 1.1), sparkle particles (10/s).
        /// </summary>
        private void TryApplyEliteL4()
        {
            if (_l4EliteApplied || !IsEliteL4) return;
            _l4EliteApplied = true;

            // Deep gold tint — stronger than L3 standard gold
            var tintRoot = _meshChild != null ? _meshChild : gameObject;
            Color eliteGold = new Color(1f, 0.72f, 0.05f);
            MaterialController.UpdateTint(tintRoot, eliteGold);
            foreach (var r in tintRoot.GetComponentsInChildren<Renderer>())
            {
                foreach (var m in r.materials)
                {
                    if (m == null) continue;
                    if (m.HasProperty("_EmissionColor"))
                    {
                        m.SetColor("_EmissionColor", eliteGold * 0.55f);
                        m.EnableKeyword("_EMISSION");
                    }
                }
            }

            // Upgrade glow halo : destroy L3 ring, spawn bigger L4 ring
            if (_glowPulseRoutine != null) { StopCoroutine(_glowPulseRoutine); _glowPulseRoutine = null; }
            if (_glowRing != null) { Destroy(_glowRing); _glowRing = null; }
            SpawnEliteGlowRing();

            // Sparkle particles emitted 10/s around the tower
            _sparkleRoutine = StartCoroutine(SparkleRoutine());

            // VFX fanfare + popup
            var pos = transform.position + Vector3.up * 1.5f;
            VfxPool.Instance?.SpawnConfetti(pos, 1.8f, new Color(1f, 0.85f, 0.1f));
            JuiceFX.Instance?.PunchScale(transform, 1.3f, 0.45f);
            CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                "ELITE", pos, new Color(1f, 0.82f, 0.08f));
        }

        private void SpawnEliteGlowRing()
        {
            const int verts = 32;
            const float radius = 1.1f; // bigger than L3 (0.75)

            _glowRing = new GameObject("GlowRing_L4");
            _glowRing.transform.SetParent(transform, false);
            _glowRing.transform.localPosition = new Vector3(0f, 0.12f, 0f);

            var lr = _glowRing.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = verts;
            lr.startWidth = 0.14f;
            lr.endWidth = 0.14f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            for (int i = 0; i < verts; i++)
            {
                float angle = i / (float)verts * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(1f, 0.78f, 0.05f, 1f);
            lr.material = mat;

            _glowPulseRoutine = StartCoroutine(GlowPulseRoutine(lr, mat));
        }

        private IEnumerator SparkleRoutine()
        {
            const float interval = 0.1f; // 10 sparkles/s
            while (true)
            {
                yield return new WaitForSeconds(interval);
                // Random position on a circle around the tower at mid-height
                float angle = Random.value * Mathf.PI * 2f;
                float dist = Random.Range(0.4f, 1.0f);
                var offset = new Vector3(Mathf.Cos(angle) * dist, Random.Range(0.3f, 1.6f), Mathf.Sin(angle) * dist);
                VfxPool.Instance?.SpawnImpact(transform.position + offset, new Color(1f, 0.92f, 0.25f));
            }
        }

        // Fired by TowerResearchTree.TryUnlock for any tower type.
        // Only the first live tower of the matching type plays the fanfare.
        private void OnResearchUnlocked(string towerId, int node)
        {
            if (cfg == null || cfg.Id != towerId) return;
            var pos = transform.position + Vector3.up * 1.5f;
            VfxPool.Instance?.SpawnConfetti(pos, 1.2f, new Color(0.4f, 0.8f, 1f));
            Toast.Show("Recherche debloquee", $"{cfg.DisplayName} — {TowerResearchTree.NodeLabel(node)}", 3500, null, ToastType.Achievement);
            // Unsubscribe so only one tower fires the fanfare per unlock event.
            TowerResearchTree.OnResearchUnlocked -= OnResearchUnlocked;
            // All research nodes just unlocked for this tower — check elite L4 promotion.
            TryApplyEliteL4();
        }

        private void OnDestroy()
        {
            target?.SetTargetedBy(false);
            var settings = CrowdDefense.UI.SettingsRegistry.Instance;
            if (settings != null) settings.OnSettingsChanged -= RefreshDamageIconVisibility;
            TowerResearchTree.OnResearchUnlocked -= OnResearchUnlocked;
            if (_glowPulseRoutine != null) StopCoroutine(_glowPulseRoutine);
            if (_glowRing != null) Destroy(_glowRing);
            if (_sparkleRoutine != null) StopCoroutine(_sparkleRoutine);
            if (_starRow != null) Destroy(_starRow);
            for (int i = 0; i < 3; i++)
            {
                if (_starMaterials[i] != null) Object.Destroy(_starMaterials[i]);
                _starMaterials[i] = null;
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
