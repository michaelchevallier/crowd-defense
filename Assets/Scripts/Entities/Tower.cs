#nullable enable
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Entities
{
    public class Tower : MonoBehaviour
    {
        private const float TOWER_DAMAGE_MUL = 1.6f;

        [SerializeField] private GameObject? projectilePrefab;

        private TowerType? cfg;
        private float cooldown;
        private Enemy? target;

        public TowerType? Config => cfg;

        public void Init(TowerType type, GameObject? projPrefab)
        {
            cfg = type;
            cooldown = 0f;
            projectilePrefab = projPrefab;

            var renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in renderers)
            {
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rend.material.color = type.BodyColor;
            }

            transform.localScale = Vector3.one * type.SizeMultiplier;
        }

        private void Update()
        {
            if (cfg == null) return;
            cooldown -= Time.deltaTime;

            if (target == null || target.IsDead || OutOfRange(target))
                target = AcquireTarget();

            if (target != null && cooldown <= 0f)
            {
                Fire(target);
                cooldown = cfg.FireRateMs / 1000f;
            }
        }

        private bool OutOfRange(Enemy e)
        {
            if (cfg == null || e == null) return true;
            return (e.transform.position - transform.position).sqrMagnitude > cfg.Range * cfg.Range;
        }

        private Enemy? AcquireTarget()
        {
            if (cfg == null || WaveManager.Instance == null) return null;
            float rangeSq = cfg.Range * cfg.Range;
            Enemy? best = null;
            int bestWp = -1;
            var enemies = WaveManager.Instance.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - transform.position).sqrMagnitude > rangeSq) continue;
                if (e.CurrentWaypoint > bestWp)
                {
                    bestWp = e.CurrentWaypoint;
                    best = e;
                }
            }
            return best;
        }

        private void Fire(Enemy t)
        {
            if (projectilePrefab == null || cfg == null) return;
            var go = Instantiate(projectilePrefab, transform.position + Vector3.up * 1.0f, Quaternion.identity);
            var proj = go.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Init(t, cfg.Damage * TOWER_DAMAGE_MUL, cfg.ProjectileSpeed, cfg.ProjectileColor);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (cfg == null) return;
            Gizmos.color = new Color(0.3f, 0.6f, 0.9f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, cfg.Range);
        }
#endif
    }
}
