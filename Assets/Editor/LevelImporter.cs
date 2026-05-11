#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    public static class LevelImporter
    {
        private const string JsonDir = "Assets/Editor/LevelsRaw";
        private const string OutputDir = "Assets/ScriptableObjects/Levels";
        private const string EnemyDir = "Assets/ScriptableObjects/Enemies";

        // Phaser enemyId string -> Unity asset name (without .asset extension)
        private static readonly Dictionary<string, string> EnemyNameMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "basic",           "Basic" },
            { "runner",          "Runner" },
            { "brute",           "Brute" },
            { "shielded",        "Shielded" },
            { "assassin",        "Assassin" },
            { "flyer",           "Flyer" },
            { "imp",             "Imp" },
            { "midboss",         "Midboss" },
            { "boss",            "Boss" },
            { "skeleton_minion", "SkeletonMinion" },
            { "skeleton",        "SkeletonMinion" },
            { "brigand_boss",    "BrigandBoss" },
            { "warlord_boss",    "WarlordBoss" },
            { "dragon_boss",     "DragonBoss" },
            { "corsair_boss",    "CorsairBoss" },
            { "apocalypse_boss", "ApocalypseBoss" },
            { "cosmic_boss",     "CosmicBoss" },
            { "kraken_boss",     "KrakenBoss" },
            { "ai_hub",          "AiHub" },
            { "cyber_basic",     "CyberBasic" },
            { "cyber_runner",    "CyberRunner" },
            { "cyber_brute",     "CyberBrute" },
            { "cyber_flyer",     "CyberFlyer" },
            { "desert_runner",   "DesertRunner" },
            { "forest_brute",    "ForestBrute" },
            { "forest_bee",      "ForestBee" },
            { "plaine_pigeon",   "PlainePigeon" },
            { "pigeon",          "PlainePigeon" },
            { "wizard_king",     "WizardKing" },
            { "submarin_runner", "SubmarinRunner" },
        };

        [MenuItem("CrowdDefense/Import Levels from JSON")]
        public static void ImportAll()
        {
            if (!Directory.Exists(JsonDir))
            {
                Debug.LogError($"[LevelImporter] JSON directory not found: {JsonDir}. Run tools/extract_levels.js first.");
                return;
            }

            if (!Directory.Exists(OutputDir))
                Directory.CreateDirectory(OutputDir);

            string[] jsonFiles = Directory.GetFiles(JsonDir, "*.json");
            Array.Sort(jsonFiles);

            int created = 0;
            int updated = 0;
            int skipped = 0;
            var missingEnemies = new HashSet<string>();

            foreach (string jsonPath in jsonFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(jsonPath);

                // world1-1 is overwrite-protected (hand-tweaked asset)
                if (fileName == "world1-1")
                {
                    Debug.Log("[LevelImporter] Skipping world1-1 (overwrite-protected).");
                    skipped++;
                    continue;
                }

                string json = File.ReadAllText(jsonPath);
                LevelJson? data;
                try
                {
                    data = JsonUtility.FromJson<LevelJson>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LevelImporter] Parse error on {fileName}.json: {ex.Message}");
                    skipped++;
                    continue;
                }

                if (data == null)
                {
                    Debug.LogWarning($"[LevelImporter] Null parse result for {fileName}.json");
                    skipped++;
                    continue;
                }

                // Asset name: worldX-Y -> WX-Y
                string assetName = AssetNameFromId(data.id);
                string assetPath = $"{OutputDir}/{assetName}.asset";

                LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath)
                    ?? CreateNewAsset(assetPath, ref created);

                PopulateLevelData(levelData, data, missingEnemies);
                EditorUtility.SetDirty(levelData);

                if (AssetDatabase.LoadAssetAtPath<LevelData>(assetPath) != null)
                    updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (missingEnemies.Count > 0)
                Debug.LogWarning($"[LevelImporter] Unknown enemy IDs (no asset found): {string.Join(", ", missingEnemies)}");

            Debug.Log($"[LevelImporter] Done. Created: {created}, Updated: {updated}, Skipped: {skipped}. Total JSON files: {jsonFiles.Length}");
        }

        private static LevelData CreateNewAsset(string assetPath, ref int created)
        {
            var so = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(so, assetPath);
            created++;
            return so;
        }

        private static void PopulateLevelData(LevelData levelData, LevelJson data, HashSet<string> missingEnemies)
        {
            var so = new SerializedObject(levelData);

            SetString(so, "id", data.id);
            SetString(so, "displayName", data.displayName);
            SetString(so, "theme", data.theme);
            SetInt(so, "world", data.world);
            SetInt(so, "level", data.level);
            SetFloat(so, "cellSize", data.cellSize);
            SetInt(so, "startCoins", data.startCoins);
            SetBool(so, "overrideCastleHP", data.overrideCastleHP);
            SetInt(so, "castleHPOverride", data.castleHPOverride);
            SetBool(so, "allowMultiMagnet", data.allowMultiMagnet);

            // Map rows
            var mapRowsProp = so.FindProperty("mapRows");
            if (mapRowsProp != null)
            {
                mapRowsProp.arraySize = data.mapRows?.Length ?? 0;
                for (int i = 0; i < (data.mapRows?.Length ?? 0); i++)
                    mapRowsProp.GetArrayElementAtIndex(i).stringValue = data.mapRows![i];
            }

            // Waves
            var wavesProp = so.FindProperty("waves");
            if (wavesProp != null && data.waves != null)
            {
                wavesProp.arraySize = data.waves.Length;
                for (int wi = 0; wi < data.waves.Length; wi++)
                {
                    var waveJson = data.waves[wi];
                    var waveProp = wavesProp.GetArrayElementAtIndex(wi);

                    var spawnRateProp = waveProp.FindPropertyRelative("spawnRateMs");
                    if (spawnRateProp != null) spawnRateProp.intValue = waveJson.spawnRateMs;

                    var breakMsProp = waveProp.FindPropertyRelative("breakMs");
                    if (breakMsProp != null) breakMsProp.intValue = waveJson.breakMs;

                    var portalIdxProp = waveProp.FindPropertyRelative("portalIdx");
                    if (portalIdxProp != null) portalIdxProp.intValue = -1;

                    var entriesProp = waveProp.FindPropertyRelative("entries");
                    if (entriesProp == null) continue;

                    var typeEntries = new List<(EnemyType type, int count)>();
                    if (waveJson.typeKeys != null && waveJson.typeCounts != null)
                    {
                        int pairCount = Math.Min(waveJson.typeKeys.Length, waveJson.typeCounts.Length);
                        for (int ti = 0; ti < pairCount; ti++)
                        {
                            string enemyId = waveJson.typeKeys[ti];
                            int count = waveJson.typeCounts[ti];
                            EnemyType? enemyType = ResolveEnemy(enemyId, missingEnemies);
                            if (enemyType != null)
                                typeEntries.Add((enemyType, count));
                        }
                    }

                    entriesProp.arraySize = typeEntries.Count;
                    for (int ei = 0; ei < typeEntries.Count; ei++)
                    {
                        var entryProp = entriesProp.GetArrayElementAtIndex(ei);
                        var typeProp = entryProp.FindPropertyRelative("type");
                        var countProp = entryProp.FindPropertyRelative("count");
                        if (typeProp != null) typeProp.objectReferenceValue = typeEntries[ei].type;
                        if (countProp != null) countProp.intValue = typeEntries[ei].count;
                    }
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static EnemyType? ResolveEnemy(string enemyId, HashSet<string> missingEnemies)
        {
            if (!EnemyNameMap.TryGetValue(enemyId, out string? assetName))
            {
                // Try PascalCase fallback for unknown ids
                assetName = ToPascalCase(enemyId);
            }

            string path = $"{EnemyDir}/{assetName}.asset";
            var et = AssetDatabase.LoadAssetAtPath<EnemyType>(path);
            if (et == null)
            {
                missingEnemies.Add($"{enemyId}→{assetName}");
            }
            return et;
        }

        private static string AssetNameFromId(string id)
        {
            // "world1-2" -> "W1-2"
            if (id.StartsWith("world", StringComparison.OrdinalIgnoreCase))
                return "W" + id.Substring(5);
            return id;
        }

        private static string ToPascalCase(string s)
        {
            // "brigand_boss" -> "BrigandBoss"
            return string.Join("", s.Split('_').Select(w => w.Length > 0 ? char.ToUpper(w[0]) + w.Substring(1) : ""));
        }

        // SerializedProperty helpers
        private static void SetString(SerializedObject so, string prop, string value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.stringValue = value ?? "";
        }

        private static void SetInt(SerializedObject so, string prop, int value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.intValue = value;
        }

        private static void SetFloat(SerializedObject so, string prop, float value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.floatValue = value;
        }

        private static void SetBool(SerializedObject so, string prop, bool value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.boolValue = value;
        }

        // Intermediate JSON deserialization structures
        // JsonUtility doesn't support Dictionary, so we use parallel arrays
        [Serializable]
        private class LevelJson
        {
            public string id = "";
            public string displayName = "";
            public string theme = "";
            public int world;
            public int level;
            public float cellSize = 4f;
            public string[]? mapRows;
            public int startCoins;
            public bool overrideCastleHP;
            public int castleHPOverride;
            public bool allowMultiMagnet;
            public WaveJson[]? waves;
        }

        [Serializable]
        private class WaveJson
        {
            public int index;
            public int spawnRateMs;
            public int breakMs;
            // Parallel arrays for types dict (JsonUtility can't deserialize Dictionary)
            public string[]? typeKeys;
            public int[]? typeCounts;
        }
    }
}
