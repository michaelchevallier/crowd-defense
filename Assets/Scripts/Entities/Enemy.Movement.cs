#nullable enable
using System.Collections;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public partial class Enemy : MonoBehaviour
    {
        private float ComputeEffectiveSpeed()
        {
            if (cfg == null) return 0f;
            float speed = cfg.Speed * currentSpeedMul * pressureSpeedMul * _enragedSpeedMul * _variantSpeedMul;
            if (_freezeUntilTime > 0f && Time.time < _freezeUntilTime)
                speed = 0f;
            if (_chargeActive)
                speed = cfg.Speed * cfg.ChargeMul * pressureSpeedMul * _enragedSpeedMul * _variantSpeedMul;
            return speed;
        }
        private void UpdateFlyer()
        {
            if (cfg == null) return;
            if (Castle.Instance == null) return;

            Vector3 castlePos = Castle.Instance.transform.position;
            float bob = Mathf.Sin(Time.time * 3f) * 0.15f;
            Vector3 flyTarget = new Vector3(castlePos.x, cfg.FlyHeight + bob, castlePos.z);
            float effSpeed = ComputeEffectiveSpeed();
            transform.position = Vector3.MoveTowards(transform.position, flyTarget, effSpeed * Time.deltaTime);

            // Lock Y at fly height + bob
            var pos = transform.position;
            pos.y = cfg.FlyHeight + bob;
            transform.position = pos;

            // Face castle
            Vector3 dir = castlePos - transform.position;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));

            // Fire trail in air
            if (cfg.IsFiery)
            {
                _fieryTimer -= Time.deltaTime;
                if (_fieryTimer <= 0f)
                {
                    _fieryTimer = FieryInterval;
                    VfxPool.Instance?.SpawnImpact(transform.position + Vector3.down * 0.1f,
                        new Color(1f, 0.23f, 0.063f));
                }
            }

            if ((transform.position - new Vector3(castlePos.x, transform.position.y, castlePos.z)).sqrMagnitude < 0.25f)
                OnReachedCastle();
        }

        // Used by EnemyPathingSystem batched movement tick
        public float GetEffectiveSpeed() => ComputeEffectiveSpeed();

        // Used by EnemyPathingSystem — advances waypoint index after position is applied externally
        public void AdvanceWaypoint()
        {
            if (pathManager == null || cfg == null) return;
            int wpCount = pathManager.WaypointCountOnPath(pathIdx);
            if (currentWaypoint < wpCount)
                currentWaypoint++;
        }

    }
}
