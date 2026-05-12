#nullable enable
using UnityEngine;
using CrowdDefense.Systems;

namespace CrowdDefense.Visual
{
    // Subscribes to LevelEvents and wires Weather + SceneDecor lifecycle.
    // Place this MonoBehaviour in Main.unity alongside WeatherController and SceneDecor.
    // LevelRunner does NOT need modification — it raises LevelEvents which this bridge consumes.
    [DefaultExecutionOrder(50)]
    public class LevelVisualBridge : MonoBehaviour
    {
        private void OnEnable()
        {
            LevelEvents.OnLevelStart += HandleLevelStart;
            LevelEvents.OnLevelEnd   += HandleLevelEnd;
        }

        private void OnDisable()
        {
            LevelEvents.OnLevelStart -= HandleLevelStart;
            LevelEvents.OnLevelEnd   -= HandleLevelEnd;
        }

        private static void HandleLevelStart(Data.LevelData level, Bounds gridBounds)
        {
            var theme = level.LevelTheme;
            WeatherController.Instance?.ApplyTheme(theme);
            WeatherController.ApplySkyGradient(theme);
            PostProcessController.Instance?.ApplyTheme(theme);
            SceneDecor.Instance?.SpawnForLevel(theme, level.Id, gridBounds);

            var pm = Systems.PathManager.Instance;
            if (pm?.Grid != null)
                PathTiles.Instance?.BuildForLevel(pm.Grid, theme);
        }

        private static void HandleLevelEnd()
        {
            WeatherController.Instance?.StopAll();
            SceneDecor.Instance?.ClearAll();
            PathTiles.Instance?.ClearAll();
        }
    }
}
