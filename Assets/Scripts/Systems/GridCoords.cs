#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Systems
{
    public static class GridCoords
    {
        public const char GRASS = '0';
        public const char GRASS_BLOCK = '.';
        public const char PATH = '1';
        public const char PORTAL = 'P';
        public const char CASTLE = 'C';
        public const char TREE = 'T';
        public const char ROCK = 'R';
        public const char WATER = 'W';
        public const char BUSH = 'B';
        public const char VOID = ' ';
        public const char DECOR = 'D';
        public const char LAVA = 'L';
        public const char BRIDGE_WATER = '~';
        public const char BRIDGE_LAVA = '^';
        // D1-01 §3.6 — treasure tile: non-walkable, non-buildable, collectable bonus at wave end
        public const char TREASURE = '*';

        public static readonly HashSet<char> Walkable = new() { PATH, PORTAL, CASTLE, BRIDGE_WATER, BRIDGE_LAVA };
        public static readonly HashSet<char> Buildable = new() { GRASS };

        // Cell (col, row) → world Vector3 on XZ plane (Y=0.05f to place entities on slab top), origin centered, Z inverted for row=0 visually on top.
        public static Vector3 CellToWorld(int col, int row, int gridW, int gridH, float cellSize)
        {
            float x = (col - (gridW - 1) / 2f) * cellSize;
            float z = -((row - (gridH - 1) / 2f) * cellSize);
            return new Vector3(x, 0.05f, z);
        }

        public static Vector2Int WorldToCell(Vector3 world, int gridW, int gridH, float cellSize)
        {
            int col = Mathf.RoundToInt(world.x / cellSize + (gridW - 1) / 2f);
            int row = Mathf.RoundToInt(-(world.z / cellSize) + (gridH - 1) / 2f);
            return new Vector2Int(col, row);
        }

        // 4-connected neighbors of (col, row) within bounds.
        public static IEnumerable<Vector2Int> Neighbors(int col, int row, int gridW, int gridH)
        {
            int[] dc = { -1, 1, 0, 0 };
            int[] dr = { 0, 0, -1, 1 };
            for (int i = 0; i < 4; i++)
            {
                int nc = col + dc[i];
                int nr = row + dr[i];
                if (nc >= 0 && nc < gridW && nr >= 0 && nr < gridH)
                    yield return new Vector2Int(nc, nr);
            }
        }
    }
}
