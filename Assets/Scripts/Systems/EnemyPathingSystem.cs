#nullable enable
using System.Threading.Tasks;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    // Parallel waypoint advancement for large enemy crowds.
    // Call Tick() each frame to drive pathing for all active enemies.
    // When active enemy count exceeds ParallelThreshold, position targets are
    // computed on worker threads via Parallel.For (pure math, no Unity API),
    // then applied to Transform on the main thread.
    // Below the threshold a plain for loop is used to avoid thread overhead.
    [DefaultExecutionOrder(-40)]
    public class EnemyPathingSystem : MonoSingleton<EnemyPathingSystem>
    {
        private const int ParallelThreshold = 100;

        // Reusable arrays — grown on demand, never shrunk (amortised alloc)
        private Vector3[] _targetPositions = new Vector3[128];
        private bool[]    _shouldAdvance   = new bool[128];
        private bool[]    _pathable        = new bool[128];
        private float[]   _speeds          = new float[128];

        // Snapshot arrays filled once per Tick — lets Parallel.For read safely
        private Vector3[] _currentPositions = new Vector3[128];
        private Vector3[] _waypointTargets  = new Vector3[128];

        public void Tick()
        {
            var pool = EnemyPool.Instance;
            if (pool == null) return;

            var pm = PathManager.Instance;
            if (pm == null) return;

            var enemies = pool.ActiveEnemies;
            int count   = enemies.Count;
            if (count == 0) return;

            float dt = Time.deltaTime;

            EnsureCapacity(count);

            // ── Snapshot phase (main thread) ──────────────────────────────────
            // Read all Unity-side data before handing off to worker threads.
            for (int i = 0; i < count; i++)
            {
                var e = enemies[i];
                bool pathable = e.IsPathable;
                _pathable[i] = pathable;
                if (!pathable) continue;

                int wpIdx  = e.CurrentWaypoint;
                int piIdx  = e.PathIdx;
                int wpCount = pm.WaypointCountOnPath(piIdx);

                if (wpIdx >= wpCount)
                {
                    _pathable[i] = false;
                    continue;
                }

                _currentPositions[i] = e.transform.position;
                _waypointTargets[i]  = pm.GetWaypointOnPath(piIdx, wpIdx) + Vector3.up * 0.5f;
                _speeds[i]           = e.GetEffectiveSpeed();
            }

            // ── Compute phase ─────────────────────────────────────────────────
            if (count > ParallelThreshold)
            {
                Parallel.For(0, count, i =>
                {
                    if (!_pathable[i]) return;
                    float speed = _speeds[i];
                    Vector3 cur    = _currentPositions[i];
                    Vector3 target = _waypointTargets[i];
                    // MoveTowards — pure math, no Unity main-thread API
                    float maxDist   = speed * dt;
                    Vector3 delta   = target - cur;
                    float sqDist    = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                    if (maxDist * maxDist >= sqDist)
                    {
                        _targetPositions[i] = target;
                        _shouldAdvance[i]   = true;
                    }
                    else
                    {
                        float dist = System.MathF.Sqrt(sqDist);
                        _targetPositions[i] = new Vector3(
                            cur.x + delta.x / dist * maxDist,
                            cur.y + delta.y / dist * maxDist,
                            cur.z + delta.z / dist * maxDist);
                        _shouldAdvance[i] = false;
                    }
                });
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    if (!_pathable[i]) continue;
                    float speed = _speeds[i];
                    Vector3 cur    = _currentPositions[i];
                    Vector3 target = _waypointTargets[i];
                    float maxDist  = speed * dt;
                    Vector3 delta  = target - cur;
                    float sqDist   = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                    if (maxDist * maxDist >= sqDist)
                    {
                        _targetPositions[i] = target;
                        _shouldAdvance[i]   = true;
                    }
                    else
                    {
                        float dist = Mathf.Sqrt(sqDist);
                        _targetPositions[i] = new Vector3(
                            cur.x + delta.x / dist * maxDist,
                            cur.y + delta.y / dist * maxDist,
                            cur.z + delta.z / dist * maxDist);
                        _shouldAdvance[i] = false;
                    }
                }
            }

            // ── Apply phase (main thread) ─────────────────────────────────────
            for (int i = 0; i < count; i++)
            {
                if (!_pathable[i]) continue;
                var e = enemies[i];
                e.transform.position = _targetPositions[i];

                if (_shouldAdvance[i])
                    e.AdvanceWaypoint();

                // Face movement direction
                Vector3 dir = _waypointTargets[i] - _targetPositions[i];
                if (dir.x * dir.x + dir.z * dir.z > 0.0001f)
                    e.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
            }
        }

        private void EnsureCapacity(int required)
        {
            if (_targetPositions.Length >= required) return;
            int newSize = Mathf.NextPowerOfTwo(required);
            _targetPositions  = new Vector3[newSize];
            _shouldAdvance    = new bool[newSize];
            _pathable         = new bool[newSize];
            _speeds           = new float[newSize];
            _currentPositions = new Vector3[newSize];
            _waypointTargets  = new Vector3[newSize];
        }
    }
}
