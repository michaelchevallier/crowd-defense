#nullable enable
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.Entities
{
    public partial class Enemy : MonoBehaviour
    {
        // ── OnDestroy cleanup ─────────────────────────────────────────────────

        private void OnDestroy()
        {
            CancelInvoke();
        }

#if UNITY_EDITOR
        // ── Editor gizmos — aggro debug visualizer ────────────────────────────

        private void OnDrawGizmosSelected()
        {
            var hero = LevelRunner.Instance?.Hero;
            bool chasingHero = _chaseHero && hero != null && hero.gameObject.activeInHierarchy;

            if (chasingHero && hero != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, hero.transform.position);
                Gizmos.DrawSphere(hero.transform.position, 0.2f);
            }
            else if (pathManager != null)
            {
                int wpCount = pathManager.WaypointCountOnPath(pathIdx);
                if (currentWaypoint < wpCount)
                {
                    Vector3 waypointPos = pathManager.GetWaypointOnPath(pathIdx, currentWaypoint) + Vector3.up * 0.5f;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(transform.position, waypointPos);
                    Gizmos.DrawSphere(waypointPos, 0.2f);
                }
            }
        }
#endif
    }
}
