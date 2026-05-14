#nullable enable
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.UI;
using CrowdDefense.Visual;

namespace CrowdDefense.Tests.Runtime.Scenarios
{
    // End-to-end smoke: loads Main.unity programmatically and asserts that
    // 15 critical scene singletons resolve via FindFirstObjectByType (bypassing
    // MonoSingleton lazy auto-create) and that 13 ScriptableObject registries
    // load from Resources/. Would have caught the P0-1 scene-drift incident where
    // singletons silently vanished from Main.unity yet the build still shipped.
    public class SmokeTests
    {
        [UnityTest]
        public IEnumerator MainScene_BootsWith15SingletonsAnd13Registries()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null; // Awake + OnEnable
            yield return null; // Start chain

            // --- 15 scene singletons ---
            AssertSingleton<LevelRunner>();
            AssertSingleton<WaveManager>();
            AssertSingleton<Economy>();
            AssertSingleton<PathManager>();
            AssertSingleton<PlacementController>();
            AssertSingleton<EnemyPool>();
            AssertSingleton<ProjectilePool>();
            AssertSingleton<SlowEffectManager>();
            AssertSingleton<CoinPullManager>();
            AssertSingleton<Synergies>();
            AssertSingleton<PerkSystem>();
            AssertSingleton<Achievements>();
            AssertSingleton<AudioController>();
            AssertSingleton<JuiceFX>();
            AssertSingleton<VfxPool>();

            // --- 13 ScriptableObject registries (Resources/-loaded) ---
            AssertRegistryLoaded("LevelRegistry",       () => LevelRegistry.Get());
            AssertRegistryLoaded("PerkRegistry",        () => PerkRegistry.Get());
            AssertRegistryLoaded("EnemyRegistry",       () => Resources.Load<EnemyRegistry>("EnemyRegistry"));
            AssertRegistryLoaded("TowerRegistry",       () => Resources.Load<TowerRegistry>("TowerRegistry"));
            AssertRegistryLoaded("AchievementRegistry", () => Resources.Load<AchievementRegistry>("AchievementRegistry"));
            AssertRegistryLoaded("DoctrineRegistry",    () => DoctrineRegistry.Get());
            AssertRegistryLoaded("MetaUpgradeRegistry", () => MetaUpgradeRegistry.Get());
            AssertRegistryLoaded("ModifierRegistry",    () => ModifierRegistry.Get());
            AssertRegistryLoaded("SkinRegistry",        () => SkinRegistry.Get());
            AssertRegistryLoaded("CutsceneRegistry",    () => CutsceneRegistry.Get());
            AssertRegistryLoaded("TutorialRegistry",    () => TutorialRegistry.Get());
            AssertRegistryLoaded("EventRegistry",       () => EventRegistry.Get());
            AssertRegistryLoaded("AssetRegistry",       () => Resources.Load<AssetRegistry>("AssetRegistry"));
        }

        // FindFirstObjectByType bypasses MonoSingleton.Instance lazy creation —
        // a missing scene GameObject must produce a real assertion failure here.
        private static void AssertSingleton<T>() where T : MonoBehaviour
        {
            var found = UnityEngine.Object.FindFirstObjectByType<T>();
            Assert.IsNotNull(found, $"{typeof(T).Name} missing from Main.unity (FindFirstObjectByType returned null)");
        }

        private static void AssertRegistryLoaded(string label, Func<UnityEngine.Object?> loader)
        {
            UnityEngine.Object? asset;
            try
            {
                asset = loader();
            }
            catch (Exception e)
            {
                Assert.Fail($"{label} threw while loading: {e.Message}");
                return;
            }
            Assert.IsNotNull(asset, $"{label} not found in Resources/ — registry asset missing");
        }
    }
}
