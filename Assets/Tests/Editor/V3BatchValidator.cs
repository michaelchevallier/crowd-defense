#nullable enable
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.UI;

namespace CrowdDefense.EditorTools
{
    public static class V3BatchValidator
    {
        [MenuItem("Tools/CrowdDefense/QA/V3Batch/RunAll")]
        public static void RunAll()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== V3 Batch Validator ===");
            sb.AppendLine($"Date: {DateTime.UtcNow:O}");
            sb.AppendLine();

            int passed = 0, failed = 0;

            Action<StringBuilder>[] tests = new Action<StringBuilder>[]
            {
                Test_Singletons,
                Test_LevelDataLoad,
                Test_PathfindingGrid,
                Test_TowerData,
                Test_EnemyData,
                Test_AudioRegistry,
                Test_Shaders,
                Test_UIDocuments,
                Test_Resources,
                Test_LevelRegistry,
            };

            foreach (var test in tests)
            {
                try
                {
                    test(sb);
                    passed++;
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"FAIL: {test.Method.Name} — {ex.Message}");
                    failed++;
                }
            }

            sb.AppendLine();
            sb.AppendLine($"=== Summary: {passed} PASSED, {failed} FAILED ===");

            Directory.CreateDirectory("Library/V3BatchReports");
            var ts = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            File.WriteAllText($"Library/V3BatchReports/edit-mode-{ts}.txt", sb.ToString());
            File.WriteAllText("Library/V3BatchReports/edit-mode-latest.txt", sb.ToString());
            Debug.Log(sb.ToString());

            if (Application.isBatchMode && failed > 0)
                EditorApplication.Exit(1);
        }

        private static void Test_Singletons(StringBuilder sb)
        {
            Debug.Log("[V3Batch] Test_Singletons ...");

            var types = new Type[]
            {
                typeof(AudioController),
                typeof(MusicManager),
                typeof(KeyBindings),
                typeof(EventManager),
                typeof(PerkSystem),
                typeof(MetaUpgradeSystem),
                typeof(LifetimeStats),
                typeof(Achievements),
                typeof(SettingsRegistry),
            };

            var failedTypes = new System.Collections.Generic.List<string>();

            foreach (var t in types)
            {
                GameObject? go = null;
                try
                {
                    go = new GameObject($"[V3BatchTest_{t.Name}]");
                    var comp = go.AddComponent(t) as MonoBehaviour;
                    if (comp == null)
                        failedTypes.Add($"{t.Name}: AddComponent returned null");
                }
                catch (Exception ex)
                {
                    failedTypes.Add($"{t.Name}: {ex.Message}");
                }
                finally
                {
                    if (go != null)
                        GameObject.DestroyImmediate(go);
                }
            }

            if (failedTypes.Count > 0)
                throw new Exception($"{failedTypes.Count} singleton(s) failed: {string.Join(", ", failedTypes)}");

            sb.AppendLine($"PASS: Test_Singletons");
        }

        private static void Test_LevelDataLoad(StringBuilder sb)
        {
            Debug.Log("[V3Batch] Test_LevelDataLoad ...");

            var registry = LevelRegistry.Get();
            if (registry == null)
                throw new Exception("LevelRegistry.Get() returned null");

            int count = registry.Levels.Count;
            if (count <= 50)
                throw new Exception($"Expected > 50 levels, got {count}");

            sb.AppendLine($"PASS: Test_LevelDataLoad ({count} levels)");
        }

        private static void Test_PathfindingGrid(StringBuilder sb)
        {
            Debug.Log("[V3Batch] Test_PathfindingGrid ...");

            var registry = LevelRegistry.Get();
            if (registry == null)
                throw new Exception("LevelRegistry.Get() returned null");

            LevelData? w11 = registry.FindById("W1-1");
            if (w11 == null)
                throw new Exception("LevelData W1-1 not found in registry");

            GameObject? go = null;
            try
            {
                go = new GameObject("[V3BatchTest_PathManager]");
                var pm = go.AddComponent<PathManager>();
                pm.Build();

                if (pm.Grid == null)
                    throw new Exception("PathManager.Grid is null after Build() (no LevelData wired — expected in Edit Mode)");

                if (pm.Paths.Count < 1)
                    throw new Exception($"Expected paths.Count >= 1, got {pm.Paths.Count}");
            }
            finally
            {
                if (go != null)
                    GameObject.DestroyImmediate(go);
            }

            sb.AppendLine($"PASS: Test_PathfindingGrid");
        }

        private static void Test_TowerData(StringBuilder sb)
        {
            Debug.Log("[V3Batch] Test_TowerData ...");

            var registry = Resources.Load<TowerRegistry>("TowerRegistry");
            if (registry == null)
                throw new Exception("Resources.Load<TowerRegistry>(\"TowerRegistry\") returned null");

            int count = registry.Towers.Length;
            if (count < 10)
                throw new Exception($"Expected >= 10 tower types, got {count}");

            sb.AppendLine($"PASS: Test_TowerData ({count} towers)");
        }

        private static void Test_EnemyData(StringBuilder sb)
        {
            Debug.Log("[V3Batch] Test_EnemyData ...");

            var registry = Resources.Load<EnemyRegistry>("EnemyRegistry");
            if (registry == null)
                throw new Exception("Resources.Load<EnemyRegistry>(\"EnemyRegistry\") returned null");

            int count = registry.Enemies.Length;
            if (count < 10)
                throw new Exception($"Expected >= 10 enemy types, got {count}");

            sb.AppendLine($"PASS: Test_EnemyData ({count} enemies)");
        }

        private static void Test_AudioRegistry(StringBuilder sb)
        {
            Debug.Log("[V3Batch] Test_AudioRegistry ...");

            var reg = Resources.Load<AudioClipRegistry>("AudioClipRegistry");
            if (reg == null)
                throw new Exception("Resources.Load<AudioClipRegistry>(\"AudioClipRegistry\") returned null");

            sb.AppendLine($"PASS: Test_AudioRegistry");
        }

        private static void Test_Shaders(StringBuilder sb)
        {
            Debug.Log("[V3Batch] Test_Shaders ...");

            var shaderNames = new string[]
            {
                "CrowdDefense/Toon/Lit",
                "CrowdDefense/Toon/Water",
                "CrowdDefense/Toon/Lava",
                "CrowdDefense/OutlineInvertedHull",
            };

            var missing = new System.Collections.Generic.List<string>();
            foreach (var name in shaderNames)
            {
                var s = Shader.Find(name);
                if (s == null)
                    missing.Add(name);
            }

            if (missing.Count > 0)
                throw new Exception($"Missing shaders: {string.Join(", ", missing)}");

            sb.AppendLine($"PASS: Test_Shaders (all 4 found)");
        }

        private static void Test_UIDocuments(StringBuilder sb)
        {
            Debug.Log("[V3Batch] Test_UIDocuments ...");

            var docs = new string[]
            {
                "Assets/UI/HUD.uxml",
                "Assets/UI/MainMenu.uxml",
                "Assets/UI/WorldMap.uxml",
                "Assets/UI/RunMap.uxml",
                "Assets/UI/Loader.uxml",
            };

            var missing = new System.Collections.Generic.List<string>();
            foreach (var path in docs)
            {
                var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
                if (asset == null)
                    missing.Add(path);
            }

            if (missing.Count > 0)
                throw new Exception($"Missing UXML: {string.Join(", ", missing)}");

            sb.AppendLine($"PASS: Test_UIDocuments (all 5 found)");
        }

        private static void Test_Resources(StringBuilder sb)
        {
            Debug.Log("[V3Batch] Test_Resources ...");

            var heroes = Resources.LoadAll<HeroType>("Heroes");
            int count = heroes.Length;
            if (count != 5)
                throw new Exception($"Expected 5 hero types (Barbarian/Knight/Mage/Ranger/Rogue), got {count}");

            sb.AppendLine($"PASS: Test_Resources ({count} heroes)");
        }

        private static void Test_LevelRegistry(StringBuilder sb)
        {
            Debug.Log("[V3Batch] Test_LevelRegistry ...");

            var registry = LevelRegistry.Get();
            if (registry == null)
                throw new Exception("LevelRegistry.Get() returned null");

            int count = registry.Levels.Count;
            if (count < 50)
                throw new Exception($"Expected >= 50 levels, got {count}");

            sb.AppendLine($"PASS: Test_LevelRegistry ({count} levels)");
        }
    }
}
