#nullable enable
using System;
using UnityEngine;

namespace CrowdDefense.Data
{
    public enum DoctrineEffectKey
    {
        TowerDamageMul,
        SwarmMul,
        MagnetRange,
        MagnetCoinMul,
        BankInterestRate,
        SkipBonusGold,
        StreakBonusPerWave,
        SellRefundRatio,
        CastleHPBase
    }

    [Serializable]
    public struct DoctrineModifier
    {
        // Keys mirror BalanceConfig field names — applied multiplicatively by DoctrineSystem.
        public string key;
        public float value;
    }

    [CreateAssetMenu(menuName = "CrowdDefense/DoctrineDef", fileName = "DoctrineDef")]
    public class DoctrineDef : ScriptableObject
    {
        [SerializeField] public string id = "";
        [SerializeField] public string displayName = "";
        [SerializeField] public string description = "";
        [SerializeField] public Sprite? icon;
        [SerializeField] public string iconEmoji = "";
        [SerializeField] public int gemCost = 10;
        [SerializeField] public DoctrineModifier[] modifiers = Array.Empty<DoctrineModifier>();
    }
}
