#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Visual
{
    // Tile-by-tile reveal animation at level start.
    // Port of V5 PathTiles.js distance-sorted sequential pop-in.
    // Place alongside MapRenderer in the scene. MapRenderer must run first (order 50),
    // this runs at 60 so cell GameObjects are already created when we search them.
    [DefaultExecutionOrder(60)]
    public class PathRevealAnimator : MonoBehaviour
    {
        private const float AnimDuration = 0.20f;   // seconds per tile scale-up
        private const float StaggerDelay = 0.060f;  // seconds between tiles
        private const float FastTimeScale = 1f;     // above this → instant reveal

        private Coroutine? _runningAnim;

        private void OnEnable()  => LevelEvents.OnLevelStart += HandleLevelStart;
        private void OnDisable() => LevelEvents.OnLevelStart -= HandleLevelStart;

        private void HandleLevelStart(LevelData _, Bounds __)
        {
            if (_runningAnim != null) StopCoroutine(_runningAnim);
            _runningAnim = StartCoroutine(RevealAll());
        }

        private IEnumerator RevealAll()
        {
            var pm = PathManager.Instance;
            if (pm?.Grid == null) yield break;

            var grid = pm.Grid;
            var tiles = CollectPathTilesSortedByDistance(grid);
            if (tiles.Count == 0) yield break;

            // Instant reveal when time-skipping (fast-forward mode)
            if (Time.timeScale > FastTimeScale)
            {
                foreach (var (go, _) in tiles)
                    go.transform.localScale = Vector3.one;
                yield break;
            }

            // Hide all tiles first
            foreach (var (go, _) in tiles)
                go.transform.localScale = Vector3.zero;

            AudioController.Instance?.Play("path_reveal");

            // Staggered reveal
            float elapsed = 0f;
            int next = 0;

            while (next < tiles.Count)
            {
                // Trigger all tiles whose stagger offset has been reached
                while (next < tiles.Count && elapsed >= next * StaggerDelay)
                {
                    var go = tiles[next].Item1;
                    StartCoroutine(ScaleUp(go.transform));
                    next++;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Wait for the last tile's scale animation to finish
            yield return new WaitForSeconds(AnimDuration);
            _runningAnim = null;
        }

        private static IEnumerator ScaleUp(Transform t)
        {
            float elapsed = 0f;
            while (elapsed < AnimDuration)
            {
                float progress = elapsed / AnimDuration;
                // Smooth overshoot spring: cubic ease-out with slight bounce
                float s = EaseOutBack(progress);
                t.localScale = Vector3.one * s;
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        // Ease-out with subtle overshoot (spring feel matching V5 pop-in)
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float t1 = t - 1f;
            return 1f + c3 * t1 * t1 * t1 + c1 * t1 * t1;
        }

        // BFS from the first portal cell, visiting all walkable cells.
        // Returns (GameObject, bfsDepth) sorted ascending by depth.
        private static List<(GameObject, int)> CollectPathTilesSortedByDistance(GridData grid)
        {
            if (grid.Portals.Count == 0) return new List<(GameObject, int)>();

            var origin = grid.Portals[0];
            var dist = new Dictionary<Vector2Int, int>();
            var queue = new Queue<Vector2Int>();

            dist[origin] = 0;
            queue.Enqueue(origin);

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                int d = dist[cell];
                foreach (var nb in GridCoords.Neighbors(cell.x, cell.y, grid.Width, grid.Height))
                {
                    if (dist.ContainsKey(nb)) continue;
                    if (!grid.IsWalkable(nb.x, nb.y)) continue;
                    dist[nb] = d + 1;
                    queue.Enqueue(nb);
                }
            }

            // Build sorted list
            var sortedCells = new List<(Vector2Int cell, int depth)>(dist.Count);
            foreach (var kv in dist)
                sortedCells.Add((kv.Key, kv.Value));
            sortedCells.Sort((a, b) => a.depth.CompareTo(b.depth));

            // Match to GameObjects — MapRenderer names them Cell_{c}_{r}
            var result = new List<(GameObject, int)>(sortedCells.Count);
            var mapRenderer = Object.FindAnyObjectByType<MapRenderer>();
            if (mapRenderer == null) return result;

            var lookup = BuildCellLookup(mapRenderer.transform);

            foreach (var (cell, depth) in sortedCells)
            {
                string key = $"Cell_{cell.x}_{cell.y}";
                if (lookup.TryGetValue(key, out var go))
                    result.Add((go, depth));
            }

            return result;
        }

        // Build name → GameObject map once rather than calling Find per tile
        private static Dictionary<string, GameObject> BuildCellLookup(Transform parent)
        {
            var map = new Dictionary<string, GameObject>(parent.childCount);
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i).gameObject;
                map[child.name] = child;
            }
            return map;
        }
    }
}
