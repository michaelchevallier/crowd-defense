#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    public class PathManager : MonoBehaviour
    {
        public static PathManager? Instance { get; private set; }

        [SerializeField] private LevelData? levelData;

        public GridData? Grid { get; private set; }
        public List<Vector3> Waypoints { get; private set; } = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Build();
        }

        public void Build()
        {
            if (levelData == null)
            {
                Debug.LogError("[PathManager] No LevelData assigned");
                return;
            }

            Grid = GridData.Parse(levelData);
            Waypoints.Clear();

            if (Grid.Portals.Count == 0 || Grid.Castles.Count == 0)
            {
                Debug.LogError($"[PathManager] No portal or castle (portals={Grid.Portals.Count}, castles={Grid.Castles.Count})");
                return;
            }

            // POC : first portal × first castle only.
            var start = Grid.Portals[0];
            var end = Grid.Castles[0];
            var cells = Grid.BfsShortestPath(start, end);

            if (cells == null || cells.Count < 2)
            {
                Debug.LogError($"[PathManager] No path from portal {start} to castle {end}");
                return;
            }

            foreach (var cell in cells)
                Waypoints.Add(GridCoords.CellToWorld(cell.x, cell.y, Grid.Width, Grid.Height, Grid.CellSize));

#if UNITY_EDITOR
            Debug.Log($"[PathManager] grid {Grid.Width}x{Grid.Height}, {Waypoints.Count} waypoints from {start} to {end}");
#endif
        }

        public Vector3 GetWaypoint(int index)
        {
            if (index < 0 || index >= Waypoints.Count) return Vector3.zero;
            return Waypoints[index];
        }

        public int WaypointCount => Waypoints.Count;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (levelData == null) return;

            // Parse fresh in Editor (cheap : 15×7 = 105 cells).
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

            // Waypoint polyline + endpoints
            var waypoints = Application.isPlaying ? Waypoints : ComputeWaypointsEditor(grid);
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

        private static List<Vector3> ComputeWaypointsEditor(GridData grid)
        {
            var list = new List<Vector3>();
            if (grid.Portals.Count == 0 || grid.Castles.Count == 0) return list;
            var cells = grid.BfsShortestPath(grid.Portals[0], grid.Castles[0]);
            if (cells == null) return list;
            foreach (var cell in cells)
                list.Add(GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize));
            return list;
        }

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
