#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(fileName = "TowerType", menuName = "CrowdDefense/TowerType")]
    public class TowerType : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private int unlockWorld = 1;
        [SerializeField] private int cost = 30;

        [Header("Combat")]
        [SerializeField] private float damage = 1f;
        [SerializeField] private float range = 8f;
        [SerializeField] private int fireRateMs = 700;
        [SerializeField] private float projectileSpeed = 22f;
        [SerializeField] private float aoe = 0f;
        [SerializeField] private int pierce = 0;

        [Header("Visual")]
        [SerializeField] private Color bodyColor = Color.blue;
        [SerializeField] private Color projectileColor = Color.yellow;
        [SerializeField] private float sizeMultiplier = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public int UnlockWorld => unlockWorld;
        public int Cost => cost;
        public float Damage => damage;
        public float Range => range;
        public int FireRateMs => fireRateMs;
        public float ProjectileSpeed => projectileSpeed;
        public float Aoe => aoe;
        public int Pierce => pierce;
        public Color BodyColor => bodyColor;
        public Color ProjectileColor => projectileColor;
        public float SizeMultiplier => sizeMultiplier;
    }
}
