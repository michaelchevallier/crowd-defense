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
        private int pathIdx;
        private PathManager? pathManager;

        public EnemyType? Config => cfg;
        public int CurrentWaypoint => currentWaypoint;
        public int PathIdx => pathIdx;
        public bool IsDead { get; private set; }

        public void Init(EnemyType type, int assignedPathIdx = 0)
        {
            cfg = type;
            hp = type.Hp;
            pathIdx = assignedPathIdx;
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
            pathManager = PathManager.Instance;
            if (pathManager == null || pathManager.Paths.Count == 0)
            {
                Debug.LogError("[Enemy] No PathManager or no paths");
                Destroy(gameObject);
                return;
            }
            if (pathManager.WaypointCountOnPath(pathIdx) < 2)
            {
                Debug.LogError($"[Enemy] Path {pathIdx} too short");
                Destroy(gameObject);
                return;
            }
            transform.position = pathManager.GetWaypointOnPath(pathIdx, 0) + Vector3.up * 0.5f;
        }

        private void Update()
        {
            if (cfg == null || pathManager == null || IsDead) return;
            int wpCount = pathManager.WaypointCountOnPath(pathIdx);
            if (currentWaypoint >= wpCount)
            {
                OnReachedCastle();
                return;
            }

            Vector3 target = pathManager.GetWaypointOnPath(pathIdx, currentWaypoint) + Vector3.up * 0.5f;
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
            Debug.Log($"[Enemy] reached castle type={cfg?.Id} dmg={dmg} pathIdx={pathIdx}");
#endif
            Castle.Instance?.TakeDamage(dmg);
            WaveManager.Instance?.NotifyEnemyDied(this);
            Destroy(gameObject);
        }
    }
}
