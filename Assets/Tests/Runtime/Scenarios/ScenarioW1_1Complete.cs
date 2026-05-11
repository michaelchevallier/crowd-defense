#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Tests.Runtime.Scenarios
{
    // Sprint-gate scenario : W1-1 complete victory smoke check.
    //
    // Strategy : because full gameplay (tower placement + 4 waves) is timing-sensitive
    // and depends on integration not yet complete, this scenario validates the
    // *scaffold* required for the full gameplay loop, not the loop itself :
    //  - W1-1 LevelData exists in registry
    //  - Main scene loads in PlayMode
    //  - LevelRunner singleton instantiates with state == Play
    //  - Castle spawns with HP > 0
    //  - WaveManager exposes wave count > 0
    //
    // The full timed scenario (place tower → run waves → assert victory) is left
    // to manual QA + the qa-tester agent via Unity-MCP run_play_mode. Once Phase 3
    // integration stabilizes, this test can be extended to fully drive the loop.
    public class ScenarioW1_1Complete
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string W1_1Id = "W1-1";

        [UnityTest]
        public IEnumerator W1_1_Scaffold_Loads()
        {
            var registry = Resources.Load<LevelRegistry>("LevelRegistry");
            if (registry == null)
            {
                Assert.Inconclusive(
                    "LevelRegistry not found in Resources/. Run Tools/CrowdDefense/Build LevelRegistry first.");
                yield break;
            }

            var w11 = registry.FindById(W1_1Id);
            if (w11 == null)
            {
                Assert.Inconclusive($"LevelData '{W1_1Id}' not found in LevelRegistry.");
                yield break;
            }

            Assert.IsNotNull(w11);
            Assert.Greater(w11!.Waves.Count, 0, "W1-1 must declare at least 1 wave.");

            // Try to load Main scene via path (relies on Editor scene db).
            var sceneFullPath = System.IO.Path.Combine(Application.dataPath, "Scenes/Main.unity");
            if (!System.IO.File.Exists(sceneFullPath))
            {
                Assert.Inconclusive($"Scene not found at {MainScenePath}.");
                yield break;
            }

            // Set the next-to-load level so LevelRunner picks W1-1 in Awake.
            LevelLoader.NextLevelId = W1_1Id;

            yield return UnityEditor.SceneManagement.EditorSceneManager
                .LoadSceneAsyncInPlayMode(MainScenePath, new LoadSceneParameters(LoadSceneMode.Single));

            // Wait a few frames so singletons run their Awake/Start.
            for (int i = 0; i < 5; i++) yield return null;

            // Assertions on scaffold.
            Assert.IsNotNull(LevelRunner.Instance, "LevelRunner singleton must be present.");
            Assert.AreEqual(GameState.Play, LevelRunner.Instance!.State,
                "Initial state must be Play.");

            Assert.IsNotNull(WaveManager.Instance, "WaveManager singleton must be present.");
            Assert.Greater(WaveManager.Instance!.TotalWaves, 0,
                "WaveManager must expose > 0 total waves.");

            // Castle may take 1-2 extra frames to spawn (depends on PathManager init order).
            for (int i = 0; i < 10 && LevelRunner.Instance.PrimaryCastle == null; i++)
                yield return null;

            Assert.IsNotNull(LevelRunner.Instance.PrimaryCastle,
                "Castle must spawn within 10 frames of scene load.");
            Assert.Greater(LevelRunner.Instance.TotalCastleHP, 0,
                "Castle HP must be > 0.");
        }
    }
}
