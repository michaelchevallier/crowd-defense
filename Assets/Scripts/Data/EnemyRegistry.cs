#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(fileName = "EnemyRegistry", menuName = "CrowdDefense/EnemyRegistry")]
    public class EnemyRegistry : ScriptableObject
    {
        [SerializeField] private EnemyType[] enemies = System.Array.Empty<EnemyType>();

        public EnemyType[] Enemies => enemies;
    }
}
