#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    public static class LevelDataValidator
    {
        [MenuItem("Tools/CrowdDefense/Validate LevelData")]
        public static void ValidateAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[LevelDataValidator] No LevelData assets found.");
                return;
            }

            int valid = 0;
            int invalid = 0;
            var invalidNames = new List<string>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData? ld = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (ld == null) continue;

                List<string> errors = Validate(ld);

                if (errors.Count == 0)
                {
                    valid++;
                }
                else
                {
                    invalid++;
                    invalidNames.Add(ld.name);
                    foreach (string err in errors)
                        Debug.LogError($"[LevelDataValidator] {ld.name}: {err}");
                }
            }

            string summary = invalid == 0
                ? $"[LevelDataValidator] ✅ {valid} valid / ❌ {invalid} invalid"
                : $"[LevelDataValidator] ✅ {valid} valid / ❌ {invalid} invalid : {string.Join(", ", invalidNames)}";
            Debug.Log(summary);
        }

        private static List<string> Validate(LevelData ld)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(ld.Id))
                errors.Add("missing Id");

            if (ld.World <= 0)
                errors.Add("World must be > 0");

            if (ld.MapRows == null || ld.MapRows.Count == 0)
                errors.Add("missing MapRows");
            else
            {
                List<string> mapErrors = MapValidator.Validate(ld);
                foreach (string e in mapErrors)
                {
                    if (!e.StartsWith("WARN:"))
                        errors.Add($"map: {e}");
                }
            }

            if (ld.Waves == null || ld.Waves.Count == 0)
            {
                if (!ld.IsEndless)
                    errors.Add("no waves");
            }
            else
            {
                for (int wi = 0; wi < ld.Waves.Count; wi++)
                {
                    var wave = ld.Waves[wi];
                    if (wave.entries == null) continue;
                    for (int ei = 0; ei < wave.entries.Count; ei++)
                    {
                        if (wave.entries[ei].type == null)
                            errors.Add($"wave[{wi}].entries[{ei}] has null type");
                    }
                }
            }

            return errors;
        }
    }
}
