#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    public enum PerkCategory { Offensive, Economy, Mobility, Transform, Support }
    public enum PerkTag      { None, Foudre, Sang, Pierre, Feu, Vide, Or }
    public enum PerkRarity   { Common, Rare, Epic, Legendary }

    public enum School { None, Feu, Givre, Maconnerie }

    [CreateAssetMenu(menuName = "CrowdDefense/PerkDef", fileName = "PerkDef")]
    public class PerkDef : ScriptableObject
    {
        [SerializeField] public string id = "";
        [SerializeField] public string nameKey = "";
        [SerializeField] public string descKey = "";
        [SerializeField] public string iconEmoji = "";
        [SerializeField] public Sprite? icon;
        [SerializeField] public PerkCategory category;
        [SerializeField] public PerkTag tag;
        [SerializeField] public PerkRarity rarity;
        [SerializeField] public bool   stackable;
        [SerializeField] public int    maxStacks;
        [SerializeField] public string school = "";
        [SerializeField] public bool   transform;

        [SerializeField] public float range;
        [SerializeField] public float fireRate;
        [SerializeField] public float damage;
        [SerializeField] public float moveSpeed;
        [SerializeField] public int   moveAttackPierceBonus;
        [SerializeField] public float coinGain;
        [SerializeField] public float critChance;
        [SerializeField] public float critMul;
        [SerializeField] public int   critStaggerMs;
        [SerializeField] public int   multiShot;
        [SerializeField] public int   pierceCount;
        [SerializeField] public int   lifesteal;
        [SerializeField] public float waveRegen;

        [SerializeField] public bool  fireball;
        [SerializeField] public float fireballRadius = 2f;
        [SerializeField] public float fireballDmgMul = 0.8f;
        [SerializeField] public bool  ricochet;
        [SerializeField] public int   ricochetBounces = 3;
        [SerializeField] public float ricochetDecay = 0.85f;
        [SerializeField] public bool  lightning;
        [SerializeField] public int   lightningTargets = 2;
        [SerializeField] public float lightningDmgMul = 0.7f;
        [SerializeField] public bool  pierceExplode;
        [SerializeField] public float pierceExplodeRadius = 2f;
        [SerializeField] public float pierceExplodeDmgMul = 1f;

        [SerializeField] public float towerCostMul = 1f;
        [SerializeField] public bool  firstTowerFree;
        [SerializeField] public float towerFireRateAura = 1f;
        [SerializeField] public float towerAuraRange;

        [SerializeField] public bool combustion;
        [SerializeField] public bool pyromancie;
        [SerializeField] public bool glaciation;
        [SerializeField] public bool cristalGlace;
        [SerializeField] public bool forteressePerk;
        [SerializeField] public bool mursPierre;

        // Magnet perk (D1-01 Q3)
        [SerializeField] public float magnetRangeMul  = 1f;
        [SerializeField] public float coinFlySpeedMul = 1f;

        [SerializeField] public float downRange;
        [SerializeField] public float downDamage;
        [SerializeField] public float downFireRate;
        [SerializeField] public float downCoinReward;

        // Backwards-compat shims for PerkPickerController (Speed/RunMode agent expected simpler API)
        public string displayName { get => nameKey; set => nameKey = value; }
        public string description { get => descKey; set => descKey = value; }
    }
}
