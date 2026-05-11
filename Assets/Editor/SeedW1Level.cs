#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    /// <summary>
    /// Generates (or overwrites) Assets/ScriptableObjects/Levels/W1-1.asset
    /// with 5 balanced waves for World 1 Level 1.
    /// </summary>
    public static class SeedW1Level
    {
        private const string OutputPath = "Assets/ScriptableObjects/Levels/W1-1.asset";
        private const string EnemyDir   = "Assets/ScriptableObjects/Enemies";

        [MenuItem("Tools/CrowdDefense/Seed W1-1")]
        public static void SeedW11()
        {
            string dir = Path.GetDirectoryName(OutputPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var level = AssetDatabase.LoadAssetAtPath<LevelData>(OutputPath);
            bool isNew = level == null;
            if (isNew)
            {
                level = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(level, OutputPath);
            }

            var so = new SerializedObject(level);

            // ── Identity ──────────────────────────────────────────────────────
            Set(so, "id",          "world1-1");
            Set(so, "displayName", "Plaine — 1");
            Set(so, "theme",       "plaine");
            Set(so, "world",       1);
            Set(so, "level",       1);
            Set(so, "cellSize",    4f);
            Set(so, "startCoins",  120);
            Set(so, "briefing",    "Des ennemis approchent par la plaine !");

            // ── Map (12×8 simple straight path) ──────────────────────────────
            string[] rows =
            {
                "00000W0000000DL",
                "P1111~111110D0L",
                "00000W000010DDL",
                "00000W0000100DL",
                "00000W000010D0L",
                "00000W000010DDL",
                "0DD0DWD0D0C0DDL",
            };
            var mapRowsProp = so.FindProperty("mapRows");
            if (mapRowsProp != null)
            {
                mapRowsProp.arraySize = rows.Length;
                for (int i = 0; i < rows.Length; i++)
                    mapRowsProp.GetArrayElementAtIndex(i).stringValue = rows[i];
            }

            // ── Enemy refs ────────────────────────────────────────────────────
            EnemyType? basic    = Load("Basic");
            EnemyType? runner   = Load("Runner");
            EnemyType? brute    = Load("Brute");
            EnemyType? midboss  = Load("Midboss");
            EnemyType? boss     = Load("Boss");

            // ── Wave definitions ──────────────────────────────────────────────
            // Wave 1 : 5 basics (tutoriel)
            // Wave 2 : 10 basics
            // Wave 3 : 5 runners + 5 basics
            // Wave 4 : 1 midboss + 8 basics
            // Wave 5 : 1 boss (boss final)
            var waveDefs = new List<(int spawnRateMs, int breakMs, List<(EnemyType? type, int count)> entries)>
            {
                (1200, 5000, new() { (basic,   5) }),
                (1000, 5000, new() { (basic,  10) }),
                ( 900, 5000, new() { (runner,  5), (basic, 5) }),
                ( 800, 5000, new() { (basic,   8), (midboss, 1) }),
                (2000,    0, new() { (boss,     1) }),
            };

            var wavesProp = so.FindProperty("waves");
            if (wavesProp != null)
            {
                wavesProp.arraySize = waveDefs.Count;
                for (int wi = 0; wi < waveDefs.Count; wi++)
                {
                    var (spawnRateMs, breakMs, entries) = waveDefs[wi];
                    var waveProp = wavesProp.GetArrayElementAtIndex(wi);

                    SetRel(waveProp, "spawnRateMs", spawnRateMs);
                    SetRel(waveProp, "breakMs",     breakMs);
                    SetRel(waveProp, "portalIdx",   -1);

                    var entriesProp = waveProp.FindPropertyRelative("entries");
                    if (entriesProp == null) continue;

                    var validEntries = entries.FindAll(e => e.type != null);
                    entriesProp.arraySize = validEntries.Count;
                    for (int ei = 0; ei < validEntries.Count; ei++)
                    {
                        var entryProp  = entriesProp.GetArrayElementAtIndex(ei);
                        var typeProp   = entryProp.FindPropertyRelative("type");
                        var countProp  = entryProp.FindPropertyRelative("count");
                        if (typeProp  != null) typeProp.objectReferenceValue = validEntries[ei].type;
                        if (countProp != null) countProp.intValue = validEntries[ei].count;
                    }
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SeedW1Level] W1-1.asset {(isNew ? "created" : "updated")} — 5 waves, {rows.Length} map rows.");
        }

        private static EnemyType? Load(string name)
        {
            string path = $"{EnemyDir}/{name}.asset";
            var et = AssetDatabase.LoadAssetAtPath<EnemyType>(path);
            if (et == null)
                Debug.LogWarning($"[SeedW1Level] EnemyType not found: {path}");
            return et;
        }

        private static void Set(SerializedObject so, string prop, string value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.stringValue = value;
        }

        private static void Set(SerializedObject so, string prop, int value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.intValue = value;
        }

        private static void Set(SerializedObject so, string prop, float value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.floatValue = value;
        }

        private static void SetRel(SerializedProperty parent, string rel, int value)
        {
            var p = parent.FindPropertyRelative(rel);
            if (p != null) p.intValue = value;
        }
    }
}
