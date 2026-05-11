#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(fileName = "TowerRegistry", menuName = "CrowdDefense/TowerRegistry")]
    public class TowerRegistry : ScriptableObject
    {
        [SerializeField] private TowerType[] towers = System.Array.Empty<TowerType>();

        public TowerType[] Towers => towers;
    }
}
