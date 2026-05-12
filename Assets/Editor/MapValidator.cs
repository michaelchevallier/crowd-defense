#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    public static class MapValidator
    {
        private const string LevelsFolder = "Assets/ScriptableObjects/Levels";

        // Cells that enemies walk on (path-like)
        private static readonly HashSet<char> PathLike = new() { '1', 'P', 'C', '~', '^' };
        // All cells passable by enemies (walkable includes grass for lenient BFS)
        private static readonly HashSet<char> Walkable = new() { '1', 'P', 'C', '~', '^', '0' };

        [MenuItem("Tools/CrowdDefense/Validate All Levels")]
        public static void ValidateAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { LevelsFolder });
            if (guids.Length == 0)
            {
                Debug.LogWarning("[MapValidator] No LevelData assets found in " + LevelsFolder);
                return;
            }

            int errorCount = 0;
            int warnCount = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData? level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level == null) continue;

                List<string> errors = Validate(level);
                if (errors.Count == 0)
                {
                    Debug.Log($"[MapValidator] OK — {level.name}");
                }
                else
                {
                    foreach (string e in errors)
                    {
                        bool isWarn = e.StartsWith("WARN:");
                        if (isWarn)
                        {
                            Debug.LogWarning($"[MapValidator] {level.name} — {e}");
                            warnCount++;
                        }
                        else
                        {
                            Debug.LogError($"[MapValidator] {level.name} — {e}");
                            errorCount++;
                        }
                    }
                }
            }
            Debug.Log($"[MapValidator] Done — {guids.Length} levels, {errorCount} errors, {warnCount} warnings.");
        }

        /// Returns a list of error/warning strings. Empty = valid.
        /// Errors are plain strings; warnings are prefixed "WARN:".
        public static List<string> Validate(LevelData level)
        {
            var errors = new List<string>();
            IReadOnlyList<string> mapRows = level.MapRows;

            if (mapRows.Count == 0)
            {
                errors.Add("map has no rows");
                return errors;
            }

            int h = mapRows.Count;
            int w = 0;
            foreach (string row in mapRows)
                if (row.Length > w) w = row.Length;

            // Build 2D char grid (pad short rows with space)
            char[,] grid = new char[h, w];
            for (int r = 0; r < h; r++)
            {
                string row = mapRows[r];
                for (int c = 0; c < w; c++)
                    grid[r, c] = c < row.Length ? row[c] : ' ';
            }

            // Collect portals and castles
            var portals = new List<(int col, int row)>();
            var castles = new List<(int col, int row)>();
            int buildSlots = 0;

            for (int r = 0; r < h; r++)
            {
                for (int c = 0; c < w; c++)
                {
                    char ch = grid[r, c];
                    if (ch == 'P') portals.Add((c, r));
                    else if (ch == 'C') castles.Add((c, r));
                    else if (ch == '0') buildSlots++;
                }
            }

            if (portals.Count == 0) errors.Add("no portal (P) found");
            if (castles.Count == 0) errors.Add("no castle (C) found");

            // Portals must be on grid edge
            foreach (var (col, row) in portals)
            {
                if (col != 0 && col != w - 1 && row != 0 && row != h - 1)
                    errors.Add($"portal at ({col},{row}) is not on grid edge");
            }

            // Adjacent portal pairs
            for (int i = 0; i < portals.Count; i++)
            {
                for (int j = i + 1; j < portals.Count; j++)
                {
                    int dist = Mathf.Abs(portals[i].col - portals[j].col) + Mathf.Abs(portals[i].row - portals[j].row);
                    if (dist == 1)
                        errors.Add($"portals at ({portals[i].col},{portals[i].row}) and ({portals[j].col},{portals[j].row}) are adjacent");
                }
            }

            // Adjacent castle pairs
            for (int i = 0; i < castles.Count; i++)
            {
                for (int j = i + 1; j < castles.Count; j++)
                {
                    int dist = Mathf.Abs(castles[i].col - castles[j].col) + Mathf.Abs(castles[i].row - castles[j].row);
                    if (dist == 1)
                        errors.Add($"castles at ({castles[i].col},{castles[i].row}) and ({castles[j].col},{castles[j].row}) are adjacent");
                }
            }

            // Each portal must have at least one walkable neighbor
            foreach (var (col, row) in portals)
            {
                bool hasWalkable = false;
                foreach (var (nc, nr) in CardinalNeighbors(col, row, w, h))
                {
                    if (Walkable.Contains(grid[nr, nc])) { hasWalkable = true; break; }
                }
                if (!hasWalkable)
                    errors.Add($"portal at ({col},{row}) has no walkable neighbor");
            }

            // Strict BFS: path-only (no grass) from all portals
            var visitedStrict = BfsPathStrict(grid, w, h, portals);

            // All PATH cells must be reachable
            for (int r = 0; r < h; r++)
            {
                for (int c = 0; c < w; c++)
                {
                    if (grid[r, c] == '1' && !visitedStrict.Contains((c, r)))
                        errors.Add($"PATH cell ({c},{r}) is disconnected from any portal");
                }
            }

            // All castles must be reachable via path-only route
            foreach (var (col, row) in castles)
            {
                if (!visitedStrict.Contains((col, row)))
                    errors.Add($"castle at ({col},{row}) has no contiguous path from any portal");
            }

            // Build slots warning (spec: ≥ N depending on level, default 4)
            int minSlots = Mathf.Max(4, level.World * 2);
            if (buildSlots < minSlots)
                errors.Add($"WARN: only {buildSlots} build slots (expected ≥{minSlots} for world {level.World})");

            // Decor density warning
            var decorSet = new HashSet<char> { 'D', 'T', 'R', 'B' };
            var nonBuildable = new HashSet<char> { '1', 'P', 'C', '~', '^', 'W', 'L', ' ' };
            int decorCount = 0;
            int nonPathCount = 0;
            for (int r = 0; r < h; r++)
            {
                for (int c = 0; c < w; c++)
                {
                    char ch = grid[r, c];
                    if (nonBuildable.Contains(ch)) continue;
                    nonPathCount++;
                    if (decorSet.Contains(ch)) decorCount++;
                }
            }
            float ratio = nonPathCount > 0 ? (float)decorCount / nonPathCount : 0f;
            if (ratio > 0.25f)
                errors.Add($"WARN: decor density {ratio * 100:F0}% ({decorCount}/{nonPathCount}) too high — aim ≤20%");

            return errors;
        }

        // BFS over PathLike cells from all portals simultaneously
        private static HashSet<(int col, int row)> BfsPathStrict(char[,] grid, int w, int h, List<(int col, int row)> portals)
        {
            var visited = new HashSet<(int, int)>();
            var queue = new Queue<(int col, int row)>();

            foreach (var p in portals)
            {
                if (!visited.Contains(p)) { visited.Add(p); queue.Enqueue(p); }
            }

            while (queue.Count > 0)
            {
                var (col, row) = queue.Dequeue();
                foreach (var (nc, nr) in CardinalNeighbors(col, row, w, h))
                {
                    var nb = (nc, nr);
                    if (!visited.Contains(nb) && PathLike.Contains(grid[nr, nc]))
                    {
                        visited.Add(nb);
                        queue.Enqueue(nb);
                    }
                }
            }

            return visited;
        }

        private static IEnumerable<(int col, int row)> CardinalNeighbors(int col, int row, int w, int h)
        {
            if (col > 0)     yield return (col - 1, row);
            if (col < w - 1) yield return (col + 1, row);
            if (row > 0)     yield return (col, row - 1);
            if (row < h - 1) yield return (col, row + 1);
        }
    }
}
