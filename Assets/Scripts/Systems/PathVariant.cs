#nullable enable
using System;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    // Pure selector: picks one of the variant grids stored in LevelData.
    // Each variant is a full grid encoded as newline-separated rows (same format as MapRows).
    public static class PathVariant
    {
        public static int CountVariants(LevelData level)
        {
            var variants = level.GridVariants;
            if (variants == null) return 0;
            int count = 0;
            foreach (var v in variants)
                if (!string.IsNullOrEmpty(v)) count++;
            return count;
        }

        // Returns the rows for the chosen variant.
        // Falls back to variant 0, then to LevelData.MapRows if no variants defined.
        public static string[] PickVariant(LevelData level, int variantIdx)
        {
            var variants = level.GridVariants;
            int total = CountVariants(level);

            if (total == 0)
                return ToArray(level.MapRows);

            int idx = Math.Clamp(variantIdx, 0, total - 1);
            string raw = variants![idx];
            return raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }

        private static string[] ToArray(System.Collections.Generic.IReadOnlyList<string> rows)
        {
            var arr = new string[rows.Count];
            for (int i = 0; i < rows.Count; i++) arr[i] = rows[i];
            return arr;
        }
    }
}
