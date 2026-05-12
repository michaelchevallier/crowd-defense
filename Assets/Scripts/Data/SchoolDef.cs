#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(menuName = "CrowdDefense/SchoolDef", fileName = "SchoolDef")]
    public class SchoolDef : ScriptableObject
    {
        [SerializeField] public string id          = "";
        [SerializeField] public string displayName = "";
        [SerializeField] public string description = "";
        [SerializeField] public Color  theme       = Color.white;
        [SerializeField] public Sprite? icon;
        [SerializeField] public List<PerkDef> perks = new();
        [SerializeField] public int    unlockCost  = 0;
    }
}
