#nullable enable
using System;
using UnityEngine;

namespace CrowdDefense.Data
{
    public enum MetaUpgradeCategory { Combat, Economy, Utility }

    public enum MetaUpgradeEffectKey
    {
        CastleHPMul,
        HeroDamageMul,
        StartCoinsBonus,
        XpMul,
        HeroRangeMul,
        CoinGainMul,
        PerkChoiceCountBonus,
        HeroFireRateMul,
        GemGainMul,
        TowerUpgradeDiscount
    }

    [Serializable]
    public struct MetaUpgradeEffect
    {
        public string key;
        public float valuePerLevel;
    }

    [CreateAssetMenu(menuName = "CrowdDefense/MetaUpgradeDef", fileName = "MetaUpgradeDef")]
    public class MetaUpgradeDef : ScriptableObject
    {
        [SerializeField] public string id = "";
        [SerializeField] public string displayName = "";
        [SerializeField] public string description = "";
        [SerializeField] public Sprite? icon;
        [SerializeField] public string iconEmoji = "";
        [SerializeField] public MetaUpgradeCategory category = MetaUpgradeCategory.Combat;
        [SerializeField] public int tier = 1;
        [SerializeField] public int maxLevel = 3;
        [SerializeField] public int[] costsPerLevel = { 5, 15, 40 };
        [SerializeField] public string[] perLevelLabels = Array.Empty<string>();
        [SerializeField] public MetaUpgradeEffect[] effects = Array.Empty<MetaUpgradeEffect>();

        public int CostForLevel(int currentLevel)
        {
            if (currentLevel < 0 || currentLevel >= costsPerLevel.Length) return 999999;
            return costsPerLevel[currentLevel];
        }
    }
}
