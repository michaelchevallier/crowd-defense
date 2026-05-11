#nullable enable
using CrowdDefense.Common;
using UnityEngine;

namespace CrowdDefense.Entities
{
    [RequireComponent(typeof(MeshRenderer))]
    public class Projectile : MonoBehaviour
    {
        private Enemy? target;
        private float damage;
        private float speed;
        private float lifetimeSec = 3f;

        public void Init(Enemy target, float damage, float speed, Color color)
        {
            this.target = target;
            this.damage = damage;
            this.speed = speed;
            var rend = GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.material = new Material(ShaderUtil.GetLitShader());
                rend.material.color = color;
                rend.material.SetFloat("_Smoothness", 0.9f);
            }
        }

        private void Update()
        {
            lifetimeSec -= Time.deltaTime;
            if (lifetimeSec <= 0f || target == null || target.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 targetPos = target.transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            if ((transform.position - targetPos).sqrMagnitude < 0.04f)
            {
                target.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
