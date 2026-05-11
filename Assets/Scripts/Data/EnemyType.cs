#nullable enable
using UnityEngine;

namespace CrowdDefense.Data
{
    [CreateAssetMenu(fileName = "EnemyType", menuName = "CrowdDefense/EnemyType")]
    public class EnemyType : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";

        [Header("Stats")]
        [SerializeField] private float hp = 3f;
        [SerializeField] private float speed = 1.2f;
        [SerializeField] private int damage = 5;
        [SerializeField] private int reward = 2;

        [Header("Visual")]
        [SerializeField] private float scale = 0.55f;
        [SerializeField] private Color bodyColor = Color.red;

        public string Id => id;
        public string DisplayName => displayName;
        public float Hp => hp;
        public float Speed => speed;
        public int Damage => damage;
        public int Reward => reward;
        public float Scale => scale;
        public Color BodyColor => bodyColor;
    }
}
