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

        [Header("Unlock")]
        [SerializeField] private SkinUnlockType unlockType = SkinUnlockType.Default;
        [SerializeField] private int purchaseCostGems = 0;
        [SerializeField] private string unlockConditionId = "";

        [Header("Bonus")]
        [SerializeField] private bool hasStatBonus = false;
        [SerializeField] private string bonusDescKey = "";

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
        public SkinUnlockType UnlockType => unlockType;
        public int PurchaseCostGems => purchaseCostGems;
        public string UnlockConditionId => unlockConditionId;
        public bool HasStatBonus => hasStatBonus;
        public string BonusDescKey => bonusDescKey;
        public bool IsDefault => unlockType == SkinUnlockType.Default;
    }
}
