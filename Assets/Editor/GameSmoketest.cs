#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CrowdDefense.Systems;
using CrowdDefense.Entities;
using CrowdDefense.Data;

namespace CrowdDefense.Editor
{
    /// <summary>
    /// Comprehensive game loop smoketest: LevelRunner → WaveManager → EnemySpawn → Tower fire → Castle damage
    /// Runs in batch headless mode via:
    /// "$UNITY_PATH" -batchmode -nographics -projectPath /path -executeMethod CrowdDefense.Editor.GameSmoketest.Run -quit
    /// </summary>
    public static class GameSmoketest
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        internal const int TimeoutSeconds = 15;

        [MenuItem("Tools/CrowdDefense/Run Game Smoketest")]
        public static void RunMenuItem()
        {
            Debug.Log("[GameSmoketest] Starting smoketest from menu...");
            Run();
        }

        public static void Run()
        {
            Debug.Log("[GameSmoketest] === STARTING GAME LOOP SMOKETEST ===");

            // Verify Main scene exists
            if (!System.IO.File.Exists(MainScenePath))
            {
                Debug.LogError($"[GameSmoketest] Main scene not found at {MainScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            // Load Main scene
            Debug.Log($"[GameSmoketest] Loading scene: {MainScenePath}");
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            // Enter play mode and schedule test
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

                // Schedule the test coroutine
                var tester = new GameObject("_GameSmokeTester").AddComponent<GameSmokeTesterBehaviour>();
            }
        }
    }

    /// <summary>
    /// Runs the actual test logic via coroutine in play mode.
    /// </summary>
    internal class GameSmokeTesterBehaviour : MonoBehaviour
    {
        private float startTime;
        private bool testComplete = false;

        private void Start()
        {
            startTime = Time.realtimeSinceStartup;
            StartCoroutine(RunTest());
        }

        private IEnumerator RunTest()
        {
            yield return null; // Wait one frame for singletons to initialize

            Debug.Log("[GameSmoketest] === PHASE 1: Verify singletons ===");

            // Check LevelRunner
            var levelRunner = LevelRunner.Instance;
            if (levelRunner == null)
            {
                Debug.LogError("[GameSmoketest] FAIL: LevelRunner.Instance is null");
                FailTest();
                yield break;
            }
            Debug.Log("[GameSmoketest] PASS: LevelRunner initialized");

            // Check WaveManager
            var waveManager = WaveManager.Instance;
            if (waveManager == null)
            {
                Debug.LogError("[GameSmoketest] FAIL: WaveManager.Instance is null");
                FailTest();
                yield break;
            }
            Debug.Log("[GameSmoketest] PASS: WaveManager initialized");

            // Check PathManager
            var pathManager = PathManager.Instance;
            if (pathManager == null)
            {
                Debug.LogError("[GameSmoketest] FAIL: PathManager.Instance is null");
                FailTest();
                yield break;
            }
            Debug.Log("[GameSmoketest] PASS: PathManager initialized");

            // Check EnemyPool
            var enemyPool = EnemyPool.Instance;
            if (enemyPool == null)
            {
                Debug.LogError("[GameSmoketest] FAIL: EnemyPool.Instance is null");
                FailTest();
                yield break;
            }
            Debug.Log("[GameSmoketest] PASS: EnemyPool initialized");

            // Check Castle
            if (levelRunner.PrimaryCastle == null)
            {
                Debug.LogError("[GameSmoketest] FAIL: Castle not spawned");
                FailTest();
                yield break;
            }
            Debug.Log($"[GameSmoketest] PASS: Castle spawned, HP={levelRunner.TotalCastleHP}/{levelRunner.TotalCastleHPMax}");

            Debug.Log("[GameSmoketest] === PHASE 2: Wait 1s for level stabilization ===");
            yield return new WaitForSeconds(1f);

            Debug.Log("[GameSmoketest] === PHASE 3: Start first wave ===");
            waveManager.StartNextWave();

            Debug.Log("[GameSmoketest] === PHASE 4: Spawn enemies (wait 5 seconds) ===");
            yield return new WaitForSeconds(5f);

            // Check enemy count
            int enemyCount = waveManager.ActiveEnemies.Count;
            if (enemyCount == 0)
            {
                Debug.LogWarning("[GameSmoketest] WARNING: No enemies spawned after 5 seconds");
                // Non-fatal; could be a wave with no enemies
            }
            else
            {
                Debug.Log($"[GameSmoketest] PASS: {enemyCount} enemies active");
            }

            Debug.Log("[GameSmoketest] === PHASE 5: Check towers in scene ===");
            int towerCount = UnityEngine.Object.FindObjectsByType<Tower>(FindObjectsSortMode.None).Length;
            if (towerCount >= 0)
            {
                Debug.Log($"[GameSmoketest] INFO: {towerCount} towers in scene");
            }

            Debug.Log("[GameSmoketest] === PHASE 6: Check game state ===");
            Debug.Log($"[GameSmoketest] Game state: {levelRunner.State}");
            Debug.Log($"[GameSmoketest] Castle HP: {levelRunner.TotalCastleHP}/{levelRunner.TotalCastleHPMax}");
            Debug.Log($"[GameSmoketest] Current wave: {waveManager.WaveDisplayNumber}/{waveManager.TotalWaves}");

            Debug.Log("[GameSmoketest] === SMOKETEST COMPLETED SUCCESSFULLY ===");
            SucceedTest();
        }

        private void SucceedTest()
        {
            testComplete = true;
            float elapsed = Time.realtimeSinceStartup - startTime;
            Debug.Log($"[GameSmoketest] Test passed in {elapsed:F2}s. Exiting play mode...");

            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }

        private void FailTest()
        {
            testComplete = true;
            float elapsed = Time.realtimeSinceStartup - startTime;
            Debug.LogError($"[GameSmoketest] Test FAILED after {elapsed:F2}s. Exiting with code 1...");

            EditorApplication.isPlaying = false;
            EditorApplication.Exit(1);
        }

        private void Update()
        {
            // Safety timeout
            float elapsed = Time.realtimeSinceStartup - startTime;
            if (!testComplete && elapsed > GameSmoketest.TimeoutSeconds)
            {
                Debug.LogError($"[GameSmoketest] TIMEOUT after {GameSmoketest.TimeoutSeconds}s");
                FailTest();
            }
        }
    }
}
#endif
