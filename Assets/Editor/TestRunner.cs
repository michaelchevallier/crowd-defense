#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Editor
{
    /// <summary>
    /// Boot smoketest: opens Main.unity, validates all singletons + registries.
    /// MenuItem: Tools/CrowdDefense/Run Boot Smoketest
    /// Batch mode: -executeMethod CrowdDefense.Editor.TestRunner.RunBatch
    /// </summary>
    public static class TestRunner
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Tools/CrowdDefense/Run Boot Smoketest")]
        public static void RunMenuItem() => RunCore(batchMode: false);

        public static void RunBatch()
        {
            bool allPass = RunCore(batchMode: true);
            EditorApplication.Exit(allPass ? 0 : 1);
        }

        private static bool RunCore(bool batchMode)
        {
            int pass = 0, fail = 0;
            var sb = new StringBuilder();
            sb.AppendLine("=== BOOT SMOKETEST ===");

            // --- Open scene ---
            if (!System.IO.File.Exists(MainScenePath))
            {
                sb.AppendLine($"✗ Main.unity MISSING at {MainScenePath}");
                Flush(sb);
                Debug.LogError("[TestRunner] RESULT: 0/1 PASS — scene not found");
                return false;
            }
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            // --- Singletons ---
            CheckSingleton<LevelRunner>("LevelRunner",       sb, ref pass, ref fail);
            CheckSingleton<WaveManager>("WaveManager",       sb, ref pass, ref fail);
            CheckSingleton<Economy>("Economy",               sb, ref pass, ref fail);
            CheckSingleton<PerkSystem>("PerkSystem",         sb, ref pass, ref fail);
            CheckSingleton<Achievements>("Achievements",     sb, ref pass, ref fail);
            CheckSingleton<PathManager>("PathManager",       sb, ref pass, ref fail);
            CheckSingleton<EnemyPool>("EnemyPool",           sb, ref pass, ref fail);
            CheckSingleton<ProjectilePool>("ProjectilePool", sb, ref pass, ref fail);
            CheckSingleton<Synergies>("Synergies",           sb, ref pass, ref fail);
            CheckSingleton<BossSystem>("BossSystem",         sb, ref pass, ref fail);
            CheckSingleton<ComboSystem>("ComboSystem",       sb, ref pass, ref fail);
            CheckSingleton<RunContext>("RunContext",          sb, ref pass, ref fail);
            CheckSingleton<MapRenderer>("MapRenderer",       sb, ref pass, ref fail);
            CheckSingleton<SlowEffectManager>("SlowEffectManager", sb, ref pass, ref fail);

            // --- Registries ---
            CheckRegistry("LevelRegistry",       () => LevelRegistry.Get()?.Levels?.Count,             sb, ref pass, ref fail);
            CheckRegistry("PerkRegistry",        () => PerkRegistry.Get()?.Standard?.Length,            sb, ref pass, ref fail);
            CheckRegistry("EnemyRegistry",       () => Resources.Load<EnemyRegistry>("EnemyRegistry")?.Enemies?.Length, sb, ref pass, ref fail);
            CheckRegistry("TowerRegistry",       () => Resources.Load<TowerRegistry>("TowerRegistry")?.Towers?.Length,  sb, ref pass, ref fail);
            CheckRegistry("AchievementRegistry", () => Resources.Load<AchievementRegistry>("AchievementRegistry")?.All?.Length, sb, ref pass, ref fail);
            CheckRegistry("DoctrineRegistry",    () => DoctrineRegistry.Get()?.All?.Length,             sb, ref pass, ref fail);
            CheckRegistry("MetaUpgradeRegistry", () => MetaUpgradeRegistry.Get()?.All?.Length,          sb, ref pass, ref fail);
            CheckRegistry("ModifierRegistry",    () =>
            {
                var reg = ModifierRegistry.Get();
                if (reg == null) return null;
                var field = typeof(ModifierRegistry).GetField("modifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field?.GetValue(reg) is Array arr) return arr.Length;
                return -1;
            }, sb, ref pass, ref fail);
            CheckRegistry("SkinRegistry",        () => SkinRegistry.Get()?.All?.Count,                  sb, ref pass, ref fail);
            CheckRegistry("CutsceneRegistry",    () => CutsceneRegistry.Get()?.All?.Count,              sb, ref pass, ref fail);
            CheckRegistry("TutorialRegistry",    () => TutorialRegistry.Get()?.Steps?.Length,           sb, ref pass, ref fail);
            CheckRegistry("EventRegistry",       () =>
            {
                var reg = EventRegistry.Get();
                if (reg == null) return null;
                // EventRegistry has no Count — use reflection to read private field
                var field = typeof(EventRegistry).GetField("events", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field == null) field = typeof(EventRegistry).GetField("defs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field?.GetValue(reg) is Array arr) return arr.Length;
                return -1; // loaded but count unknown
            }, sb, ref pass, ref fail);
            CheckRegistry("AssetRegistry",       () =>
            {
                var reg = Resources.Load<AssetRegistry>("AssetRegistry");
                if (reg == null) return null;
                var field = typeof(AssetRegistry).GetField("entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field?.GetValue(reg) is Array arr) return arr.Length;
                return -1;
            }, sb, ref pass, ref fail);
            CheckRegistry("BalanceConfig",       () => BalanceConfig.Get() != null ? 1 : (int?)null, sb, ref pass, ref fail);

            int total = pass + fail;
            sb.AppendLine($"RESULT: {pass}/{total} PASS");
            Flush(sb);
            return fail == 0;
        }

        private static void CheckSingleton<T>(string label, StringBuilder sb, ref int pass, ref int fail)
            where T : UnityEngine.MonoBehaviour
        {
            var obj = UnityEngine.Object.FindFirstObjectByType<T>();
            if (obj != null)
            {
                sb.AppendLine($"✓ {label} singleton present");
                pass++;
            }
            else
            {
                sb.AppendLine($"✗ {label} singleton MISSING");
                fail++;
            }
        }

        private static void CheckRegistry(string label, Func<int?> counter, StringBuilder sb, ref int pass, ref int fail)
        {
            try
            {
                int? count = counter();
                if (count == null)
                {
                    sb.AppendLine($"✗ {label}: null (asset missing in Resources/)");
                    fail++;
                }
                else if (count == 0)
                {
                    sb.AppendLine($"✗ {label}: 0 entries (empty!)");
                    fail++;
                }
                else if (count < 0)
                {
                    sb.AppendLine($"✓ {label}: loaded (count unknown)");
                    pass++;
                }
                else
                {
                    sb.AppendLine($"✓ {label}: {count} entries");
                    pass++;
                }
            }
            catch (Exception e)
            {
                sb.AppendLine($"✗ {label}: EXCEPTION {e.Message}");
                fail++;
            }
        }

        private static void Flush(StringBuilder sb)
        {
            string output = sb.ToString();
            Debug.Log(output);
        }
    }
}
#endif
