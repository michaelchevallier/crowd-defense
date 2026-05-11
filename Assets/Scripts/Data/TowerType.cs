#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    public enum TowerBehavior { Attack, Cluster, Slow, BuffAura, CoinPull }

    public enum SynergyType { Aura, CrossEffect, ApplyToEnemy, Passive }

    [Serializable]
    public struct SynergyDef
    {
        public SynergyType type;
        public string from;
        public string effectKey;
        public float effectValue;
        public float range;
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

        [Header("Visual")]
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
        public Color BodyColor => bodyColor;
        public Color ProjectileColor => projectileColor;
        public float SizeMultiplier => sizeMultiplier;
    }
}
