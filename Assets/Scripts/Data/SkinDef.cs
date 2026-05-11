#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    public enum SkinTargetType { Tower, Hero, Enemy }

    public enum SkinUnlockType { Default, Purchase, Achievement, Drop }

    [CreateAssetMenu(fileName = "SkinDef", menuName = "CrowdDefense/SkinDef")]
    public class SkinDef : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "";
        [SerializeField] private string displayNameKey = "";
        [SerializeField] private string descriptionKey = "";
        [SerializeField] private Sprite? icon;

        [Header("Target")]
        [SerializeField] private SkinTargetType targetType = SkinTargetType.Tower;
        [SerializeField] private string targetId = "";

        [Header("Visual Override")]
        [SerializeField] private GameObject? alternateGLTF;
        [SerializeField] private Material? alternateMaterial;
        [SerializeField] private Color bodyColorOverride = Color.white;
        [SerializeField] private bool useBodyColorOverride = false;
        // 0-9: ThemePalette (plaine/foret/desert/volcan/foire/apocalypse/espace/submarin/medieval/cyberpunk). -1 = none.
        [SerializeField] private int themeIndex = -1;

        [Header("Unlock")]
        [SerializeField] private SkinUnlockType unlockType = SkinUnlockType.Default;
        [SerializeField] private int purchaseCostGems = 0;
        [SerializeField] private string unlockConditionId = "";

        [Header("Bonus")]
        [SerializeField] private bool hasStatBonus = false;
        [SerializeField] private string bonusDescKey = "";

        [Header("Stat Bonuses (Hero skin — mirror V5 applySkinBonuses)")]
        [SerializeField] private float damageMul     = 1f;
        [SerializeField] private float rangeMul      = 1f;
        [SerializeField] private float fireRateMul   = 1f;   // applied as 1/fireRateMul on cooldown
        [SerializeField] private float moveSpeedMul  = 1f;
        [SerializeField] private float coinGainMul   = 1f;
        [SerializeField] private float xpMul         = 1f;

        public float DamageMul    => damageMul;
        public float RangeMul     => rangeMul;
        public float FireRateMul  => fireRateMul;
        public float MoveSpeedMul => moveSpeedMul;
        public float CoinGainMul  => coinGainMul;
        public float XpMul        => xpMul;

        public string Id => id;
        public string DisplayNameKey => displayNameKey;
        public string DescriptionKey => descriptionKey;
        public Sprite? Icon => icon;
        public SkinTargetType TargetType => targetType;
        public string TargetId => targetId;
        public GameObject? AlternateGLTF => alternateGLTF;
        public Material? AlternateMaterial => alternateMaterial;
        public Color BodyColorOverride => bodyColorOverride;
        public bool UseBodyColorOverride => useBodyColorOverride;
        public int ThemeIndex => themeIndex;
        public SkinUnlockType UnlockType => unlockType;
        public int PurchaseCostGems => purchaseCostGems;
        public string UnlockConditionId => unlockConditionId;
        public bool HasStatBonus => hasStatBonus;
        public string BonusDescKey => bonusDescKey;
        public bool IsDefault => unlockType == SkinUnlockType.Default;
    }
}
