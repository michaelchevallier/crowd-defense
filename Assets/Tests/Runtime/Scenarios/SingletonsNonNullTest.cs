#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CrowdDefense.Systems;
using CrowdDefense.Visual;
using CrowdDefense.UI;

namespace CrowdDefense.Tests.Runtime.Scenarios
{
    // Verifies that all critical MonoSingleton instances are present as actual
    // GameObjects in Main.unity after load — not silently auto-created by the
    // MonoSingleton fallback (which would mask scene-setup drift).
    public class SingletonsNonNullTest
    {
        [UnityTest]
        public IEnumerator AllSingletonsNonNullAfterMainSceneLoads()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null; // Awake chain
            yield return null; // Start chain

            AssertPresent<WaveManager>("WaveManager");
            AssertPresent<LevelRunner>("LevelRunner");
            AssertPresent<Economy>("Economy");
            AssertPresent<PathManager>("PathManager");
            AssertPresent<PlacementController>("PlacementController");
            AssertPresent<AudioController>("AudioController");
            AssertPresent<JuiceFX>("JuiceFX");
            AssertPresent<VfxPool>("VfxPool");
            AssertPresent<EnemyPool>("EnemyPool");
            AssertPresent<ProjectilePool>("ProjectilePool");
            AssertPresent<SlowEffectManager>("SlowEffectManager");
            AssertPresent<CoinPullManager>("CoinPullManager");
            AssertPresent<SettingsRegistry>("SettingsRegistry");
        }

        // Uses FindFirstObjectByType directly — bypasses MonoSingleton.Instance lazy
        // auto-creation so a missing scene GameObject causes a real assertion failure.
        private static void AssertPresent<T>(string label) where T : MonoBehaviour
        {
            var found = Object.FindFirstObjectByType<T>();
            Assert.IsNotNull(found, $"{label}.Instance is null after Main.unity load — add {label} GameObject to the scene");
        }
    }
}
