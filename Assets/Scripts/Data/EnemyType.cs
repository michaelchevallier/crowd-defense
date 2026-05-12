#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    public enum EnemyVariant { Normal, Fast, Tough, Regen, Armored }

    [CreateAssetMenu(fileName = "EnemyType", menuName = "CrowdDefense/EnemyType")]
    public class EnemyType : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";

        [Header("Stats")]
        [SerializeField] private float hp = 3f;
        [SerializeField] private float speed = 1.2f;
        [SerializeField] private int damage = 5;
        [SerializeField] private int reward = 2;

        [Header("Visual")]
        [SerializeField] private float scale = 0.55f;
        [SerializeField] private Color bodyColor = Color.red;
        [SerializeField] private string walkAnim = "Walk";
        [SerializeField] private string assetKey = "";
        [SerializeField] private string iconEmoji = "";

        [Header("Movement")]
        [SerializeField] private bool isFlyer = false;
        [SerializeField] private float flyHeight = 0f;
        [SerializeField] private bool ignorePath = false;

        [Header("Stealth")]
        [SerializeField] private bool isStealth = false;
        [SerializeField] private int stealthCycleMs = 0;
        [SerializeField] private float stealthOpacity = 0.25f;

        [Header("Shield")]
        [SerializeField] private float shieldHP = 0f;

        [Header("Boss")]
        [SerializeField] private bool isBoss = false;
        [SerializeField] private bool isMidBoss = false;
        [SerializeField] private bool isApocalypseBoss = false;
        [SerializeField] private bool isBrigand = false;
        [SerializeField] private bool isCorsair = false;
        [SerializeField] private bool isFiery = false;
        [SerializeField] private bool immuneToFlyerBonus = false;
        [SerializeField] private string bossName = "";
        [SerializeField] private Color bossAuraColor = Color.clear;

        [Header("Charge (Brigand/Warlord)")]
        [SerializeField] private bool enableCharge = false;
        [SerializeField] private int chargeMs = 0;
        [SerializeField] private int chargeCooldownMs = 0;
        [SerializeField] private float chargeMul = 1f;

        [Header("Fire Breath (Dragon)")]
        [SerializeField] private bool hasFireBreath = false;

        [Header("Summons")]
        [SerializeField] private bool summonsMinions = false;
        [SerializeField] private int summonCooldownMs = 0;
        [SerializeField] private EnemyType? summonType = null;

        [Header("AoE Blast")]
        [SerializeField] private int aoeBlastMs = 0;
        [SerializeField] private float aoeBlastRadius = 0f;
        [SerializeField] private int aoeBlastDamage = 0;

        [Header("AoE Attack (castle reach)")]
        [SerializeField] private bool aoeAttack = false;
        [SerializeField] private float aoeAttackRadius = 1.5f;
        [SerializeField] private int aoeAttackDamage = 3;

        [Header("Teleport (WizardKing)")]
        [SerializeField] private bool canTeleport = false;
        [SerializeField] private float teleportCooldown = 8f;
        [SerializeField] private int projectileRainCount = 0;

        [Header("Drone Burst (AI Hub)")]
        [SerializeField] private bool isBurstSummoner = false;
        [SerializeField] private int burstCount = 5;
        [SerializeField] private float burstAngleStep = 72f;

        [Header("Tentacle Slam (Kraken)")]
        [SerializeField] private bool hasTentacleSlam = false;
        [SerializeField] private int tentacleCount = 4;
        [SerializeField] private int tentacleDamage = 15;
        [SerializeField] private float tentacleRadius = 3.5f;

        [Header("Shader")]
        [SerializeField] private string shaderOverlay = "";

        public string Id => id;
        public string DisplayName => displayName;
        public float Hp => hp;
        public float Speed => speed;
        public int Damage => damage;
        public int Reward => reward;
        public float Scale => scale;
        public Color BodyColor => bodyColor;
        public string WalkAnim => walkAnim;
        public string AssetKey => assetKey;
        public string IconEmoji => string.IsNullOrEmpty(iconEmoji) ? displayName : iconEmoji;
        public bool IsFlyer => isFlyer;
        public float FlyHeight => flyHeight;
        public bool IgnorePath => ignorePath;
        public bool IsStealth => isStealth;
        public int StealthCycleMs => stealthCycleMs;
        public float StealthOpacity => stealthOpacity;
        public float ShieldHP => shieldHP;
        public bool IsBoss => isBoss;
        public bool IsMidBoss => isMidBoss;
        public bool IsApocalypseBoss => isApocalypseBoss;
        public bool IsBrigand => isBrigand;
        public bool IsCorsair => isCorsair;
        public bool IsFiery => isFiery;
        public bool ImmuneToFlyerBonus => immuneToFlyerBonus;
        public string BossName => bossName;
        public Color BossAuraColor => bossAuraColor;
        public bool EnableCharge => enableCharge || isBrigand;
        public bool HasFireBreath => hasFireBreath;
        public int ChargeMs => chargeMs;
        public int ChargeCooldownMs => chargeCooldownMs;
        public float ChargeMul => chargeMul;
        public bool SummonsMinions => summonsMinions;
        public int SummonCooldownMs => summonCooldownMs;
        public EnemyType? SummonType => summonType;
        public int AoeBlastMs => aoeBlastMs;
        public float AoeBlastRadius => aoeBlastRadius;
        public int AoeBlastDamage => aoeBlastDamage;
        public bool AoEAttack => aoeAttack || isBoss || isMidBoss;
        public float AoEAttackRadius => aoeAttackRadius;
        public int AoEAttackDamage => aoeAttackDamage;
        public string ShaderOverlay => shaderOverlay;

        // Teleport (WizardKing)
        public bool CanTeleport => canTeleport;
        public float TeleportCooldown => teleportCooldown;
        public int ProjectileRainCount => projectileRainCount;

        // Drone Burst (AI Hub)
        public bool IsBurstSummoner => isBurstSummoner;
        public int BurstCount => burstCount;
        public float BurstAngleStep => burstAngleStep;

        // Tentacle Slam (Kraken)
        public bool HasTentacleSlam => hasTentacleSlam;
        public int TentacleCount => tentacleCount;
        public int TentacleDamage => tentacleDamage;
        public float TentacleRadius => tentacleRadius;
    }
}
