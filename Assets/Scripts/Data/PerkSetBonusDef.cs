#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(menuName = "CrowdDefense/PerkSetBonusDef", fileName = "PerkSetBonusDef")]
    public class PerkSetBonusDef : ScriptableObject
    {
        [SerializeField] public PerkTag tag;
        [SerializeField] public int     threshold = 3;
        [SerializeField] public string  nameKey = "";
        [SerializeField] public string  descKey = "";
        [SerializeField] public float   addCritChance;
        [SerializeField] public int     addLifesteal;
        [SerializeField] public float   castleHPMaxMul = 1f;
        [SerializeField] public float   fireRateMul = 1f;
        [SerializeField] public int     aoeOnNthProjectile;
        [SerializeField] public float   coinGainMul = 1f;
    }
}
