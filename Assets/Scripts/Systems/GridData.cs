#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    public class GridData
    {
        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }
        public char[,] Cells { get; }
        public List<Vector2Int> Portals { get; } = new();
        public List<Vector2Int> Castles { get; } = new();

        private GridData(int w, int h, float cellSize)
        {
            Width = w;
            Height = h;
            CellSize = cellSize;
            Cells = new char[h, w];
        }

        public char At(int col, int row)
        {
            if (col < 0 || col >= Width || row < 0 || row >= Height) return GridCoords.VOID;
            return Cells[row, col];
        }

        public bool IsWalkable(int col, int row) => GridCoords.Walkable.Contains(At(col, row));
        public bool IsBuildable(int col, int row) => GridCoords.Buildable.Contains(At(col, row));

        public static GridData Parse(LevelData level)
        {
            var rows = level.MapRows;
            int h = rows.Count;
            int w = 0;
            for (int i = 0; i < rows.Count; i++) if (rows[i].Length > w) w = rows[i].Length;

            var gd = new GridData(w, h, level.CellSize);

            for (int r = 0; r < h; r++)
            {
                string row = rows[r];
                for (int c = 0; c < w; c++)
                {
                    char ch = c < row.Length ? row[c] : GridCoords.VOID;
                    gd.Cells[r, c] = ch;
                    if (ch == GridCoords.PORTAL) gd.Portals.Add(new Vector2Int(c, r));
                    else if (ch == GridCoords.CASTLE) gd.Castles.Add(new Vector2Int(c, r));
                }
            }

            return gd;
        }

        // BFS from start to target. Returns cell sequence (start to target), or null if no path.
        public List<Vector2Int>? BfsShortestPath(Vector2Int start, Vector2Int target)
        {
            var parent = new Dictionary<Vector2Int, Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            parent[start] = new Vector2Int(-1, -1);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur == target) return ReconstructPath(parent, start, target);

                foreach (var nb in GridCoords.Neighbors(cur.x, cur.y, Width, Height))
                {
                    if (parent.ContainsKey(nb)) continue;
                    if (!IsWalkable(nb.x, nb.y)) continue;
                    parent[nb] = cur;
                    queue.Enqueue(nb);
                }
            }
            return null;
        }

        private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> parent, Vector2Int start, Vector2Int end)
        {
            var cells = new List<Vector2Int>();
            var cur = end;
            while (cur.x != -1 || cur.y != -1)
            {
                cells.Add(cur);
                if (cur == start) break;
                cur = parent[cur];
            }
            cells.Reverse();
            return cells;
        }
    }
}
