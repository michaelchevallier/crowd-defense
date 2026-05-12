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

    public enum GuardMode { All, AirOnly, GroundOnly }

    public partial class Tower : MonoBehaviour
    {
        [SerializeField] private GameObject? projectilePrefab;

        private TowerType? cfg;
        private float cooldown;
        private Enemy? target;
        private Enemy? _prevAimTarget;

        private Animator? _animator;

        public float _buffMul = 1f;
        public int _pierceBonus = 0;
        public int _multiShotBonus = 0;
        public float _flyerDmgBonus = 1f;
        public bool _slowOnHitActive = false;
        public float _slowOnHitMul = 1f;
        public int _slowOnHitDurMs = 0;
        public bool _appliesSlowActive = false;
        public float _appliesSlowMul = 1f;
        public int _appliesSlowDurMs = 0;
        public bool _propagateAoEActive = false;
        public float _propagateAoERadius = 0f;
        public float _propagateAoEDmg = 0f;
        public float _cascadeRadius = 0f;
        public float _knockbackOnHit = 0f;
        public bool _pullActive = false;
        public bool _propagateDebuff = false;
        public bool _freezeOnHitActive = false;
        public int _freezeDurMs = 0;
        public bool _synergyActive = false;

        private float _heroBuffDmgMul = 1f;

        private bool _isSelected;

        private GameObject? _rangeRing;
        private LineRenderer? _rangeCircle;
        private LineRenderer? _magnetAuraCircle;
        private GameObject? _clusterHighlight;
        private Renderer? _synergyHaloRenderer;
        private MaterialPropertyBlock? _haloMpb;
        private static readonly int _haloColorId = Shader.PropertyToID("_BaseColor");

        private static readonly Dictionary<(string, TowerBranch), L3Stats> _l3StatsTable = new()
        {
            [("archer", TowerBranch.Dps)] = new L3Stats { MultiShot = 2 },
            [("archer", TowerBranch.Utility)] = new L3Stats { CritChance = 0.25f, CritMul = 3f },

            [("crossbow", TowerBranch.Dps)] = new L3Stats { FinalExplosion = true, FinalExplosionAoe = 2.5f },
            [("crossbow", TowerBranch.Utility)] = new L3Stats { Pierce = 3 },

            [("tank", TowerBranch.Dps)] = new L3Stats { BerserkerActive = true, BerserkerDmgMul = 2f, BerserkerHpThreshold = 0.5f },
            [("tank", TowerBranch.Utility)] = new L3Stats { BulwarkAura = true, BulwarkAuraRange = 4f, BulwarkDmgReduction = 0.20f },

            [("mage", TowerBranch.Dps)] = new L3Stats { ChainLightningJumps = 3, ChainLightningRange = 5f },
            [("mage", TowerBranch.Utility)] = new L3Stats { FreezeOnHit = true, FreezeDurMs = 500 },
        };

        private GameObject? _affordableHighlight;
        private Renderer? _affordHighlightRenderer;
        private MaterialPropertyBlock? _affordMpb;
        private float _affordCheckTimer;

        private readonly List<GameObject> _tierPips = new();

        private float _idlePhase;
        private Vector3 _basePos;
        private float _lastFireAt;

        private int   _streakCount   = 0;
        private float _lastKillTime  = -999f;
        private const float StreakWindow  = 0.5f;
        private const int   StreakMax     = 10;
        private bool _lastKillWasCrit = false;

        private float _lastDotPopupAt = -1f;

        private readonly List<float> _damageLogTimes  = new();
        private readonly List<float> _damageLogValues = new();

        private float _clusterTimer;

        private float _slowTickTimer;

        public int UpgradeLevel { get; private set; } = 1;
        public TowerBranch UpgradeBranch { get; private set; } = TowerBranch.None;

        public float L3DmgMul { get; private set; } = 1f;
        public float L3FireRateMul { get; private set; } = 1f;
        public float L3Aoe { get; private set; } = 0f;
        public int L3Pierce { get; private set; } = 0;
        public int L3MultiShot { get; private set; } = 0;
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

        public bool  L3TankBlockAura { get; private set; } = false;
        public float L3TankBlockAuraRange { get; private set; } = 5f;
        public float L3TankBlockAuraDps { get; private set; } = 0.6f;

        public bool  L3FinalExplosion { get; private set; } = false;
        public float L3FinalExplosionAoe { get; private set; } = 0f;
        public float L3FinalExplosionDmg { get; private set; } = 0f;

        public float L3CritChance { get; private set; } = 0f;
        public float L3CritMul { get; private set; } = 1f;

        public int   L3ChainLightningJumps { get; private set; } = 0;
        public float L3ChainLightningRange { get; private set; } = 5f;

        public bool L3FreezeOnHit { get; private set; } = false;
        public int  L3FreezeDurMs { get; private set; } = 0;

        public bool  L3BerserkerActive { get; private set; } = false;
        public float L3BerserkerDmgMul { get; private set; } = 2f;
        public float L3BerserkerHpThreshold { get; private set; } = 0.5f;

        public bool  L3BulwarkAura { get; private set; } = false;
        public float L3BulwarkAuraRange { get; private set; } = 4f;
        public float L3BulwarkDmgReduction { get; private set; } = 0.20f;

        public bool _bulwarkProtected = false;

        private bool _l3TintApplied = false;

        private GameObject? _glowRing;
        private Coroutine? _glowPulseRoutine;

        private static readonly int _emissionColorId  = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock? _hitFlashMpb;
        private float _hitFlashElapsed = 1f;
        private const float HitFlashPeak   = 0.08f;
        private const float HitFlashTotal  = 0.20f;

        private Color _projectileTint = Color.white;

        public Enemy? CurrentTarget => target;

        [SerializeField] private GuardMode _guardMode = GuardMode.All;
        public GuardMode CurrentGuardMode => _guardMode;

        public int CumulativeCost { get; private set; }

        public float TotalDamageDealt { get; private set; }
        public int   TotalKills       { get; private set; }

        public TowerType? Config => cfg;

        public float ResearchDamageMul  => cfg != null ? TowerResearchTree.DamageMul(cfg.Id)              : 1f;
        public float ResearchRangeMul   => cfg != null ? TowerResearchTree.RangeMul(cfg.Id)               : 1f;
        public float ResearchFireRateMul => cfg != null ? TowerResearchTree.FireRateIntervalMul(cfg.Id)   : 1f;

        public int Hp    => _hp;
        public int HpMax => _maxHp;
        public int RepairCost => Mathf.CeilToInt((_maxHp - _hp) / 5f) * 10;

        public float EventRangeMul       { get; set; } = 1f;
        public bool  IsDisabled          { get; set; } = false;
        public float TempDisabledUntilTime { get; set; } = 0f;
        public bool  FriendlyFireMode    { get; set; } = false;

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

        public float GetLiveDps()
        {
            float cutoff = Time.time - 5f;
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

        private GameObject? _meshChild;

        [SerializeField] private GameObject? _meshHead;

        private Transform? _barrelTip;

        private bool _recoiling;

        private static float _lastCamShakeAt = -1f;

        private float _lastMuzzleFlashAt = -1f;

        private LineRenderer? _aimLine;
        public bool ShowTargetLine = false;

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
            _basePos = transform.position;
            _lastFireAt = 0f;
            UpgradeLevel = 1;
            UpgradeBranch = TowerBranch.None;
            CumulativeCost = type.Cost;
            _l3TintApplied = false;
            TowerResearchTree.OnResearchUnlocked -= OnResearchUnlocked;
            TowerResearchTree.OnResearchUnlocked += OnResearchUnlocked;
            Achievements.Instance?.TrackEvent("tower_placed", 1);
            _levelDmgScale = BalanceConfig.Get().LevelScale.Length > 0
                ? BalanceConfig.Get().LevelScale[0]
                : 0.75f;

            transform.localScale = Vector3.one * type.SizeMultiplier;

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

            var toonRoot = _meshChild != null ? _meshChild : gameObject;
            if (skinMat != null)
                MaterialController.ApplyOverrideMaterial(toonRoot, skinMat);
            else
                MaterialController.ApplyToon(toonRoot, bodyColor);

            Outline.ApplyToHierarchy(toonRoot.transform);

            if (activeSkin != null && activeSkin.ThemeIndex >= 0)
                AssetVariants.ApplyThemeIndex(toonRoot, activeSkin.ThemeIndex);
            else if (activeSkin != null && activeSkin.UseBodyColorOverride)
                AssetVariants.ApplySkin(toonRoot, activeSkin);

            Visual.ColorblindPalette.ApplyToGameObject(toonRoot);

            _animator = AnimationController.SetupAnimator(toonRoot, "Idle", null);

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
            BuildClusterHighlight();
            BuildDamageIcon(type.DamageType);
            if (type.Behavior == TowerBehavior.CoinPull)
                BuildMagnetAuraCircle(BalanceConfig.Get().MagnetSlowRadius);

            _projectileTint = ProjectileTintForType(type);
        }

        private static Color ProjectileTintForType(TowerType t) => t.Id switch
        {
            "cannon"   or "mortar"    => new Color(1f,    0.4f,  0.1f),
            "frost"    or "ice"       => new Color(0.4f,  0.85f, 1f),
            "mage"     or "arcane"    => new Color(0.85f, 0.3f,  1f),
            "mine"     or "acid"      => new Color(0.4f,  0.95f, 0.4f),
            "ballista" or "lightning" => new Color(1f,    0.95f, 0.3f),
            _                        => t.DamageType switch
            {
                DamageType.Fire     => new Color(1f,    0.4f,  0.1f),
                DamageType.Frost    => new Color(0.4f,  0.85f, 1f),
                DamageType.Magic    => new Color(0.85f, 0.3f,  1f),
                _                  => Color.white,
            },
        };

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
                return CreateColoredFallback(new Color(1f, 0f, 0f));
            }

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

        private float _levelDmgScale = 1f;

        private int _hp    = 30;
        private int _maxHp = 30;
        private bool _destroyStarted;

        private void Update()
        {
            if (cfg == null) return;

            TickIdleAnim();
            TickHeadAim();
            TickAimLine();

            _bulwarkProtected = false;

            if (L3TankBlockAura) TickTankBlockAura();
            if (L3BulwarkAura) TickBulwarkAura();

            if (IsDisabled)
            {
                if (TempDisabledUntilTime > 0f && Time.time >= TempDisabledUntilTime)
                {
                    IsDisabled = false;
                    TempDisabledUntilTime = 0f;
                }
                return;
            }

            switch (cfg.Behavior)
            {
                case TowerBehavior.Attack:   UpdateAttack();   break;
                case TowerBehavior.Cluster:  UpdateCluster();  break;
                case TowerBehavior.Slow:     UpdateSlow();     break;
                case TowerBehavior.BuffAura: break;
                case TowerBehavior.CoinPull: UpdateCoinPull(); break;
            }
        }

        private void OnDestroy()
        {
            TowerResearchTree.OnResearchUnlocked -= OnResearchUnlocked;
            if (_glowPulseRoutine != null)
                StopCoroutine(_glowPulseRoutine);
            var settings = CrowdDefense.UI.SettingsRegistry.Instance;
            if (settings != null)
                settings.OnSettingsChanged -= RefreshDamageIconVisibility;
        }

        private void OnDrawGizmosSelected()
        {
            if (cfg == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, cfg.Range);
        }
    }
}
