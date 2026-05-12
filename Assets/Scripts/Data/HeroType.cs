#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    public enum HeroAvatar { Warrior, Mage, Ranger }

    [CreateAssetMenu(fileName = "HeroType", menuName = "CrowdDefense/HeroType")]
    public class HeroType : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "knight";
        [SerializeField] private string displayName = "Knight";
        [SerializeField] private string assetKey = "knight";
        [SerializeField][TextArea(2, 4)] private string description = "";

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
        public string Description    => description;
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

        public static Color AvatarColor(HeroAvatar avatar) => avatar switch
        {
            HeroAvatar.Warrior => new Color(0.784f, 0.235f, 0.235f, 1f),  // rouge
            HeroAvatar.Mage    => new Color(0.412f, 0.235f, 0.784f, 1f),  // violet
            HeroAvatar.Ranger  => new Color(0.235f, 0.627f, 0.235f, 1f),  // vert
            _                  => Color.white,
        };

        public static string AvatarLabel(HeroAvatar avatar) => avatar switch
        {
            HeroAvatar.Warrior => "Guerrier",
            HeroAvatar.Mage    => "Mage",
            HeroAvatar.Ranger  => "Archer",
            _                  => "?",
        };

        // Stat lines shown on the pick card (localised French).
        public static string[] AvatarStatLines(HeroAvatar avatar) => avatar switch
        {
            HeroAvatar.Warrior => new[] { "+20% PV", "+10% degats melee" },
            HeroAvatar.Mage    => new[] { "+50% portee sorts", "Recharge acceleree" },
            HeroAvatar.Ranger  => new[] { "+30% deplacement", "+15% vitesse attaque" },
            _                  => System.Array.Empty<string>(),
        };

        // Tooltip description shown on hover.
        public static string AvatarTooltip(HeroAvatar avatar) => avatar switch
        {
            HeroAvatar.Warrior => "Tank de melee solide. Bonus de vie et de degats au corps a corps.",
            HeroAvatar.Mage    => "Controleur a distance. Grande portee et cadence de sorts elevee.",
            HeroAvatar.Ranger  => "Tireur mobile. Se deplace et attaque plus vite que les autres.",
            _                  => "",
        };

        // Applies archetype multipliers to a Hero's perk fields at game start.
        // hero.*Mul fields are already initialised to 1f by Hero.cs.
        public static void ApplyArchetype(HeroAvatar avatar, Entities.Hero hero)
        {
            switch (avatar)
            {
                case HeroAvatar.Warrior:
                    hero.DamageMul    *= 1.10f;
                    hero.CastleHPMaxMul *= 1.20f;
                    break;
                case HeroAvatar.Mage:
                    hero.RangeMul     *= 1.50f;
                    hero.FireRateMul  *= 1.25f;
                    break;
                case HeroAvatar.Ranger:
                    hero.MoveSpeedMul *= 1.30f;
                    hero.FireRateMul  *= 1.15f;
                    break;
            }
        }
    }
}
