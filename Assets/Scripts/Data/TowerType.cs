#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    public enum TowerBehavior { Attack, Cluster, Slow, BuffAura, CoinPull }

    public enum DamageType { Physical, Magic, Frost, Fire }

    public enum SynergyType { Aura, CrossEffect, ApplyToEnemy, Passive }

    public enum TowerId
    {
        Unknown  = 0,
        Archer   = 1,
        Tank     = 2,
        Mage     = 3,
        Ballista = 4,
        Cannon   = 5,
        Frost    = 6,
        Crossbow = 7,
        Skyguard = 8,
        Mine     = 9,
        Acid     = 10,
        Fan      = 11,
        Portal   = 12,
        Magnet   = 13,
    }

    public static class TowerIdExtensions
    {
        public static string ToKey(this TowerId id) => id switch
        {
            TowerId.Archer   => "archer",
            TowerId.Tank     => "tank",
            TowerId.Mage     => "mage",
            TowerId.Ballista => "ballista",
            TowerId.Cannon   => "cannon",
            TowerId.Frost    => "frost",
            TowerId.Crossbow => "crossbow",
            TowerId.Skyguard => "skyguard",
            TowerId.Mine     => "mine",
            TowerId.Acid     => "acid",
            TowerId.Fan      => "fan",
            TowerId.Portal   => "portal",
            TowerId.Magnet   => "magnet",
            _                => "unknown",
        };

        public static TowerId FromKey(string key) => key switch
        {
            "archer"   => TowerId.Archer,
            "tank"     => TowerId.Tank,
            "mage"     => TowerId.Mage,
            "ballista" => TowerId.Ballista,
            "cannon"   => TowerId.Cannon,
            "frost"    => TowerId.Frost,
            "crossbow" => TowerId.Crossbow,
            "skyguard" => TowerId.Skyguard,
            "mine"     => TowerId.Mine,
            "acid"     => TowerId.Acid,
            "fan"      => TowerId.Fan,
            "portal"   => TowerId.Portal,
            "magnet"   => TowerId.Magnet,
            _          => TowerId.Unknown,
        };
    }

    /// <summary>
    /// Inline sub-structs for complex synergy effects (SlowOnHit, AppliesSlow, PropagateAoE).
    /// Serializable so they appear in the Inspector.
    /// </summary>
    [Serializable]
    public struct SlowEffectDef
    {
        public float mul;
        public int durMs;
    }

    [Serializable]
    public struct PropagateAoEDef
    {
        public float radius;
        public float dmg;
    }

    [Serializable]
    public struct SynergyDef
    {
        public SynergyType type;
        // CrossEffect only : id of the tower type that must be nearby
        public string from;
        public float range;

        // --- Aura / CrossEffect scalar effects ---
        public float dmgMul;          // Portal aura : buff dmg
        public int   pierceBonus;     // Portal aura pierce | ballista+portal = 99
        public bool  pierceMega;      // Shorthand for pierceBonus=99
        public int   multiShotBonus;  // archer+frost
        public float flyerDmgBonus;   // mage+skyguard
        public float cascadeRadius;   // mine+cannon
        public float knockbackOnHit;  // crossbow+fan
        public bool  pullToTank;      // magnet+tank
        public bool  propagateDebuff; // acid+ballista
        public bool  freezeOnHit;     // skyguard+frost (uses freezeDurMs)
        public int   freezeDurMs;

        // --- Composite effect structs ---
        public SlowEffectDef   slowOnHit;      // cannon+frost
        public SlowEffectDef   appliesSlow;    // crossbow+frost
        public PropagateAoEDef propagateAoE;   // crossbow+mage

        // --- Passive / ApplyToEnemy ---
        public float coinMul;         // magnet passive
        public SlowEffectDef slowArea; // fan/frost applyToEnemy

        // --- Aura filter ---
        // 0=unset  1=hasPierceOrAoe  -1=noPierceOrAoe
        public int filterPierceOrAoe;
    }

    [Serializable]
    public struct ArmorBreakDef
    {
        public float dmgTakenMul;
        public int durMs;
    }

    [CreateAssetMenu(fileName = "TowerType", menuName = "CrowdDefense/TowerType")]
    public class TowerType : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private string icon = "";
        [SerializeField] private int unlockWorld = 1;
        [SerializeField] private int cost = 30;

        [Header("Combat")]
        [SerializeField] private float damage = 1f;
        [SerializeField] private float range = 8f;
        [SerializeField] private int fireRateMs = 700;
        [SerializeField] private float projectileSpeed = 22f;
        [SerializeField] private float aoe = 0f;
        [SerializeField] private int pierce = 0;

        [Header("Behavior")]
        [SerializeField] private TowerBehavior behavior = TowerBehavior.Attack;
        [SerializeField] private int clusterCount = 0;
        [SerializeField] private int cooldownMs = 0;
        [SerializeField] private float slowMul = 1f;
        [SerializeField] private int slowDurationMs = 0;
        [SerializeField] private float buffMul = 1f;
        [SerializeField] private float coinMul = 1f;
        [SerializeField] private float pullSlow = 1f;
        [SerializeField] private bool parabolic = false;

        [Header("Flyer targeting")]
        [SerializeField] private bool flyerOnly = false;
        [SerializeField] private float flyerDmgMul = 1f;
        [SerializeField] private bool canHitFlyers = false;

        [Header("Armor break (Acid)")]
        [SerializeField] private bool hasArmorBreak = false;
        [SerializeField] private ArmorBreakDef armorBreak;

        [Header("Synergies")]
        [SerializeField] private List<SynergyDef> synergies = new();

        [Header("Damage Type")]
        [SerializeField] private DamageType damageType = DamageType.Physical;

        [Header("Visual")]
        [SerializeField] private string assetKey = "";
        [SerializeField] private Color bodyColor = Color.blue;
        [SerializeField] private Color projectileColor = Color.yellow;
        [SerializeField] private float sizeMultiplier = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public string Icon => icon;
        public int UnlockWorld => unlockWorld;
        public int Cost => cost;
        public float Damage => damage;
        public float Range => range;
        public int FireRateMs => fireRateMs;
        public float ProjectileSpeed => projectileSpeed;
        public float Aoe => aoe;
        public int Pierce => pierce;
        public TowerBehavior Behavior => behavior;
        public int ClusterCount => clusterCount;
        public int CooldownMs => cooldownMs;
        public float SlowMul => slowMul;
        public int SlowDurationMs => slowDurationMs;
        public float BuffMul => buffMul;
        public float CoinMul => coinMul;
        public float PullSlow => pullSlow;
        public bool Parabolic => parabolic;
        public bool FlyerOnly => flyerOnly;
        public float FlyerDmgMul => flyerDmgMul;
        public bool CanHitFlyers => canHitFlyers;
        public bool HasArmorBreak => hasArmorBreak;
        public ArmorBreakDef ArmorBreak => armorBreak;
        public IReadOnlyList<SynergyDef> Synergies => synergies;
        public DamageType DamageType => damageType;
        public string AssetKey => assetKey;
        public Color BodyColor => bodyColor;
        public Color ProjectileColor => projectileColor;
        public float SizeMultiplier => sizeMultiplier;
    }
}
