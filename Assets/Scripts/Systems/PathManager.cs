#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Path metadata linking a path index to its portal and castle sources.
    /// </summary>
    public readonly struct PathMeta
    {
        public readonly int PortalIdx;
        public readonly int CastleIdx;
        public PathMeta(int portalIdx, int castleIdx) { PortalIdx = portalIdx; CastleIdx = castleIdx; }
    }

    [DefaultExecutionOrder(-100)]
    public class PathManager : MonoSingleton<PathManager>
    {
        [SerializeField] private LevelData? levelData;

        public GridData? Grid { get; private set; }

        /// <summary>
        /// All paths : cross-product of portals × castles (BFS shortest path for each tuple).
        /// Paths[i] corresponds to PathsMeta[i].
        /// </summary>
        public IReadOnlyList<IReadOnlyList<Vector3>> Paths { get; private set; } = new List<IReadOnlyList<Vector3>>();
        public IReadOnlyList<PathMeta> PathsMeta { get; private set; } = new List<PathMeta>();

        // Backward-compat : single-path access for POC code still using Waypoints/WaypointCount/GetWaypoint.
        public IReadOnlyList<Vector3> Waypoints => Paths.Count > 0 ? Paths[0] : _empty;
        public int WaypointCount => Waypoints.Count;
        private static readonly IReadOnlyList<Vector3> _empty = new List<Vector3>();

        protected override void OnAwakeSingleton() => Build();

        public void Build()
        {
            if (levelData == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[PathManager] No LevelData assigned");
#endif
                return;
            }

            Grid = GridData.Parse(levelData);
            var paths = new List<IReadOnlyList<Vector3>>();
            var meta = new List<PathMeta>();

            if (Grid.Portals.Count == 0 || Grid.Castles.Count == 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"[PathManager] No portal or castle (portals={Grid.Portals.Count}, castles={Grid.Castles.Count})");
#endif
                Paths = paths;
                PathsMeta = meta;
                return;
            }

            for (int pi = 0; pi < Grid.Portals.Count; pi++)
            {
                for (int ci = 0; ci < Grid.Castles.Count; ci++)
                {
                    var start = Grid.Portals[pi];
                    var end = Grid.Castles[ci];
                    var cells = Grid.BfsShortestPath(start, end);

                    if (cells == null || cells.Count < 2)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.LogWarning($"[PathManager] No path from portal[{pi}]={start} to castle[{ci}]={end}");
#endif
                        continue;
                    }

                    var waypoints = new List<Vector3>(cells.Count);
                    foreach (var cell in cells)
                        waypoints.Add(GridCoords.CellToWorld(cell.x, cell.y, Grid.Width, Grid.Height, Grid.CellSize));

                    paths.Add(waypoints);
                    meta.Add(new PathMeta(pi, ci));
                }
            }

            Paths = paths;
            PathsMeta = meta;

#if UNITY_EDITOR
            Debug.Log($"[PathManager] grid {Grid.Width}x{Grid.Height}, {paths.Count} paths built (portals={Grid.Portals.Count}, castles={Grid.Castles.Count})");
#endif
        }

        public Vector3 GetWaypoint(int index) => GetWaypointOnPath(0, index);

        public Vector3 GetWaypointOnPath(int pathIdx, int waypointIdx)
        {
            if (pathIdx < 0 || pathIdx >= Paths.Count) return Vector3.zero;
            var wp = Paths[pathIdx];
            if (waypointIdx < 0 || waypointIdx >= wp.Count) return Vector3.zero;
            return wp[waypointIdx];
        }

        public int WaypointCountOnPath(int pathIdx)
        {
            if (pathIdx < 0 || pathIdx >= Paths.Count) return 0;
            return Paths[pathIdx].Count;
        }

        // Called by EnemyPool when no real paths exist (missing LevelData / portal / castle).
        // Injects a two-waypoint straight line so enemies can move and the issue is visible.
        public void InjectFallbackPath()
        {
            var fallback = new List<Vector3> { Vector3.zero, new Vector3(10f, 0f, 0f) };
            var paths = new List<IReadOnlyList<Vector3>> { fallback };
            var meta  = new List<PathMeta> { new PathMeta(0, 0) };
            Paths     = paths;
            PathsMeta = meta;
        }

        /// <summary>
        /// Returns all path indices whose castle end matches the given castle index.
        /// </summary>
        public List<int> PathsForCastle(int castleIdx)
        {
            var result = new List<int>();
            for (int i = 0; i < PathsMeta.Count; i++)
                if (PathsMeta[i].CastleIdx == castleIdx) result.Add(i);
            return result;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (levelData == null) return;

            var grid = Application.isPlaying && Grid != null ? Grid : GridData.Parse(levelData);

            for (int r = 0; r < grid.Height; r++)
            {
                for (int c = 0; c < grid.Width; c++)
                {
                    char ch = grid.At(c, r);
                    Color color = CellGizmoColor(ch);
                    if (color.a == 0f) continue;
                    Vector3 pos = GridCoords.CellToWorld(c, r, grid.Width, grid.Height, grid.CellSize);
                    Gizmos.color = color;
                    Gizmos.DrawCube(pos, new Vector3(grid.CellSize * 0.9f, 0.05f, grid.CellSize * 0.9f));
                }
            }

            // Draw all paths with distinct colors
            if (Application.isPlaying)
            {
                for (int pi = 0; pi < Paths.Count; pi++)
                {
                    var wp = Paths[pi];
                    Gizmos.color = PathGizmoColor(pi);
                    for (int i = 0; i < wp.Count - 1; i++)
                        Gizmos.DrawLine(wp[i] + Vector3.up * 0.1f, wp[i + 1] + Vector3.up * 0.1f);
                    if (wp.Count > 0)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(wp[0] + Vector3.up * 0.1f, grid.CellSize * 0.25f);
                        Gizmos.color = Color.blue;
                        Gizmos.DrawSphere(wp[wp.Count - 1] + Vector3.up * 0.1f, grid.CellSize * 0.25f);
                    }
                }
            }
            else
            {
                // Editor preview : first path only
                var waypoints = ComputePathEditor(grid, 0, 0);
                Gizmos.color = Color.yellow;
                for (int i = 0; i < waypoints.Count - 1; i++)
                    Gizmos.DrawLine(waypoints[i] + Vector3.up * 0.1f, waypoints[i + 1] + Vector3.up * 0.1f);
                if (waypoints.Count > 0)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(waypoints[0], grid.CellSize * 0.3f);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(waypoints[waypoints.Count - 1], grid.CellSize * 0.3f);
                }
            }
        }

        private static List<Vector3> ComputePathEditor(GridData grid, int portalIdx, int castleIdx)
        {
            var list = new List<Vector3>();
            if (portalIdx >= grid.Portals.Count || castleIdx >= grid.Castles.Count) return list;
            var cells = grid.BfsShortestPath(grid.Portals[portalIdx], grid.Castles[castleIdx]);
            if (cells == null) return list;
            foreach (var cell in cells)
                list.Add(GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize));
            return list;
        }

        private static Color PathGizmoColor(int idx) => idx switch
        {
            0 => Color.yellow,
            1 => Color.cyan,
            2 => Color.magenta,
            3 => Color.green,
            _ => Color.white,
        };

        private static Color CellGizmoColor(char ch) => ch switch
        {
            GridCoords.GRASS => new Color(0.3f, 0.6f, 0.2f, 0.8f),
            GridCoords.PATH => new Color(0.8f, 0.7f, 0.4f, 0.8f),
            GridCoords.PORTAL => new Color(0.9f, 0.2f, 0.2f, 0.9f),
            GridCoords.CASTLE => new Color(0.2f, 0.4f, 0.9f, 0.9f),
            GridCoords.WATER => new Color(0.2f, 0.4f, 0.8f, 0.7f),
            GridCoords.LAVA => new Color(0.9f, 0.3f, 0.1f, 0.7f),
            GridCoords.BRIDGE_WATER => new Color(0.6f, 0.4f, 0.2f, 0.8f),
            GridCoords.BRIDGE_LAVA => new Color(0.6f, 0.4f, 0.2f, 0.8f),
            GridCoords.DECOR => new Color(0.4f, 0.4f, 0.4f, 0.7f),
            _ => new Color(0f, 0f, 0f, 0f),
        };
#endif
    }
}
