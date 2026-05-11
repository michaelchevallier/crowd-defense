#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    public enum PerkRarity { Common, Rare, Epic, Legendary }

    [CreateAssetMenu(menuName = "CrowdDefense/PerkDef", fileName = "PerkDef_")]
    public class PerkDef : ScriptableObject
    {
        [SerializeField] public string id = "";
        [SerializeField] public string displayName = "";
        [SerializeField] public string description = "";
        [SerializeField] public PerkRarity rarity = PerkRarity.Common;
        [SerializeField] public Sprite? icon;
    }
}
