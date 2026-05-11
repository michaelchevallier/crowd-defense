#nullable enable
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Entities
{
    // Entity statique posée par Tower.SpawnMineRing().
    // Loop manuellement les ActiveEnemies chaque frame ; explose dès qu'un ennemi entre en range.
    public class MineExplosive : MonoBehaviour
    {
        private float damage;
        private float aoeRadius;
        private bool exploded;

        private MeshRenderer? rend;

        public void Init(float dmg, float radius)
        {
            damage = dmg * BalanceConfig.Get().TowerDamageMul;
            aoeRadius = radius;

            // Visuel minimal : capsule rouge foncé
            var mf = gameObject.AddComponent<MeshFilter>();
            mf.mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
            rend = gameObject.AddComponent<MeshRenderer>();
            rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            rend.material.color = new Color(0.67f, 0.13f, 0.13f);
            transform.localScale = Vector3.one * 0.35f;
        }

        private void Update()
        {
            if (exploded || WaveManager.Instance == null) return;
            float rangeSq = aoeRadius * aoeRadius;
            var enemies = WaveManager.Instance.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - transform.position).sqrMagnitude <= rangeSq)
                {
                    Explode();
                    return;
                }
            }
        }

        private void Explode()
        {
            exploded = true;
            if (WaveManager.Instance == null) { Destroy(gameObject); return; }

            float rangeSq = aoeRadius * aoeRadius;
            var enemies = WaveManager.Instance.ActiveEnemies;
            // Copie locale car TakeDamage peut modifier la liste via NotifyEnemyDied
            var snapshot = new System.Collections.Generic.List<Enemy>(enemies);
            for (int i = 0; i < snapshot.Count; i++)
            {
                var e = snapshot[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - transform.position).sqrMagnitude <= rangeSq)
                    e.TakeDamage(damage);
            }
#if UNITY_EDITOR
            Debug.Log($"[MineExplosive] exploded at {transform.position} aoe={aoeRadius}");
#endif
            Destroy(gameObject);
        }
    }
}
