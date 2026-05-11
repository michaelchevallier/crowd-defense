#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(fileName = "HeroType", menuName = "CrowdDefense/HeroType")]
    public class HeroType : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "knight";
        [SerializeField] private string displayName = "Knight";
        [SerializeField] private string assetKey = "knight";

        [Header("Base Stats")]
        [SerializeField] private float damage = 0.45f;
        [SerializeField] private float range = 12f;
        [SerializeField] private int fireRateMs = 600;
        [SerializeField] private float projectileSpeed = 22f;
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float modelScale = 0.6f;

        [Header("Ultimate")]
        [SerializeField] private int ultCooldownMs = 30000;
        [SerializeField] private float ultAoeRadius = 6.5f;
        [SerializeField] private float ultAoeDamage = 15f;
        [SerializeField] private int ultFanShotCount = 8;
        [SerializeField] private int ultFanPierceBonus = 3;
        [SerializeField] private float ultFanDamageMul = 1.5f;

        [Header("XP Curve")]
        // xpToNextLevel[i] = XP needed to go from level (i+1) → (i+2)
        [SerializeField] private int[] xpCurve = { 20, 45, 75, 100, 130 };

        [Header("Aura Gizmo Colors")]
        [SerializeField] private Color auraColor = new Color(1f, 0.843f, 0f, 0.5f);
        [SerializeField] private Color haloColor = new Color(1f, 0.96f, 0.69f, 0.35f);

        [Header("Visual")]
        [SerializeField] private Color bodyColor = new Color(0.227f, 0.416f, 0.749f, 1f);

        public string Id             => id;
        public string DisplayName    => displayName;
        public string AssetKey       => assetKey;
        public float  Damage         => damage;
        public float  Range          => range;
        public int    FireRateMs     => fireRateMs;
        public float  ProjectileSpeed => projectileSpeed;
        public float  MoveSpeed      => moveSpeed;
        public float  ModelScale     => modelScale;
        public int    UltCooldownMs  => ultCooldownMs;
        public float  UltAoeRadius   => ultAoeRadius;
        public float  UltAoeDamage   => ultAoeDamage;
        public int    UltFanShotCount  => ultFanShotCount;
        public int    UltFanPierceBonus => ultFanPierceBonus;
        public float  UltFanDamageMul  => ultFanDamageMul;
        public int[]  XpCurve        => xpCurve;
        public Color  AuraColor      => auraColor;
        public Color  HaloColor      => haloColor;
        public Color  BodyColor      => bodyColor;

        public int MaxLevel => 1 + xpCurve.Length;

        public int XpToNext(int level)
        {
            int idx = level - 1;
            if (idx < 0 || idx >= xpCurve.Length) return int.MaxValue;
            return xpCurve[idx];
        }
    }
}
