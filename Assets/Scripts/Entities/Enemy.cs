#nullable enable
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Entities
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class Enemy : MonoBehaviour
    {
        private EnemyType? cfg;
        private float hp;
        private int currentWaypoint;
        private PathManager? path;

        public EnemyType? Config => cfg;
        public int CurrentWaypoint => currentWaypoint;
        public bool IsDead { get; private set; }

        public void Init(EnemyType type)
        {
            cfg = type;
            hp = type.Hp;
            currentWaypoint = 1; // 0 = spawn point, start moving toward 1
            transform.localScale = Vector3.one * type.Scale;

            var rend = GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rend.material.color = type.BodyColor;
            }

            var col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = 0.3f;
                col.height = 1f;
            }
        }

        private void Start()
        {
            path = PathManager.Instance;
            if (path == null || path.WaypointCount < 2)
            {
                Debug.LogError("[Enemy] No PathManager or path too short");
                Destroy(gameObject);
                return;
            }
            transform.position = path.GetWaypoint(0) + Vector3.up * 0.5f;
        }

        private void Update()
        {
            if (cfg == null || path == null || IsDead) return;
            if (currentWaypoint >= path.WaypointCount)
            {
                OnReachedCastle();
                return;
            }

            Vector3 target = path.GetWaypoint(currentWaypoint) + Vector3.up * 0.5f;
            transform.position = Vector3.MoveTowards(transform.position, target, cfg.Speed * Time.deltaTime);

            if ((transform.position - target).sqrMagnitude < 0.01f)
                currentWaypoint++;
        }

        public void TakeDamage(float dmg)
        {
            if (IsDead) return;
            hp -= dmg;
            if (hp <= 0f)
            {
                IsDead = true;
                int reward = cfg?.Reward ?? 0;
#if UNITY_EDITOR
                Debug.Log($"[Enemy] killed type={cfg?.Id} reward={reward}");
#endif
                Economy.Instance?.AddGold(reward);
                WaveManager.Instance?.NotifyEnemyDied(this);
                Destroy(gameObject);
            }
        }

        private void OnReachedCastle()
        {
            int dmg = cfg?.Damage ?? 0;
#if UNITY_EDITOR
            Debug.Log($"[Enemy] reached castle type={cfg?.Id} dmg={dmg}");
#endif
            Castle.Instance?.TakeDamage(dmg);
            WaveManager.Instance?.NotifyEnemyDied(this);
            Destroy(gameObject);
        }
    }
}
