#nullable enable
using System;
using System.Text;

namespace CrowdDefense.Systems
{
    /// Procedural map generator for Roguelike mode.
    /// Each call with the same seed returns identical mapRows (deterministic).
    ///
    /// Row format (matches LevelData.MapRows):
    ///   '1' = wall, '0' = floor, 'L' = spawn portal (left edge), 'C' = castle (right edge)
    ///
    /// Layout guarantees:
    ///   - Outer border = wall '1'.
    ///   - Interior = wall '1' by default, carved path '0' from L to C.
    ///   - Exactly one 'L' on col 0 (interior row), one 'C' on col cols-1 (interior row).
    ///   - Path is carved using a random-walk that biases right until it reaches col cols-1.
    public static class RandomMapGenerator
    {
        /// Generates mapRows for a procedural level.
        /// <param name="seed">Deterministic seed.</param>
        /// <param name="rows">Total rows including border walls (min 5).</param>
        /// <param name="cols">Total cols including border walls (min 7).</param>
        public static string[] GenerateMap(int seed, int rows = 10, int cols = 12)
        {
            rows = Math.Max(rows, 5);
            cols = Math.Max(cols, 7);

            var rng = new Random(seed);

            // Build grid as char[rows][cols], default wall '1'
            char[][] grid = new char[rows][];
            for (int r = 0; r < rows; r++)
            {
                grid[r] = new char[cols];
                for (int c = 0; c < cols; c++)
                    grid[r][c] = '1';
            }

            // Interior floor strip: leave borders as wall, fill interior as wall (carved below)
            // Pick spawn row — random interior row (row 1..rows-2)
            int spawnRow = 1 + rng.Next(rows - 2);

            // Place portal L at col 0 (left border)
            grid[spawnRow][0] = 'L';

            // Random-walk path carving from col 1 to col cols-2, then castle at cols-1
            int curRow = spawnRow;
            for (int c = 1; c < cols - 1; c++)
            {
                // At each column, optionally drift row up or down
                // Probability: 40% stay, 30% up, 30% down (clamped to interior)
                int drift = rng.Next(10);
                if (drift < 3 && curRow > 1)
                    curRow--;
                else if (drift < 6 && curRow < rows - 2)
                    curRow++;
                // else stay

                grid[curRow][c] = '0';
            }

            // Castle on same row as path exit, right border
            grid[curRow][cols - 1] = 'C';

            // Convert grid to string[]
            string[] mapRows = new string[rows];
            var sb = new StringBuilder(cols);
            for (int r = 0; r < rows; r++)
            {
                sb.Clear();
                sb.Append(grid[r]);
                mapRows[r] = sb.ToString();
            }

            return mapRows;
        }
    }
}
