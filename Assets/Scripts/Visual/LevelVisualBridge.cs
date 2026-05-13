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
            EnsureParallax().Init(theme);

            var pm = Systems.PathManager.Instance;
            if (pm?.Grid == null) return;

            var grid = pm.Grid;
            // Use first portal as BFS origin for stagger; fall back to (0,0) if none.
            var spawnPos = grid.Portals.Count > 0 ? grid.Portals[0] : UnityEngine.Vector2Int.zero;

            // Stream + bridge visual layers spawn instantly; the reveal animation is
            // owned by MapRenderer, which hides path slabs and re-activates them
            // grouped by Manhattan distance × 60ms from the portal.
            PathTiles.Instance?.BuildForLevel(grid, theme);
            Systems.MapRenderer.Instance?.RevealFromSpawn(spawnPos);
        }

        private static void HandleLevelEnd()
        {
            WeatherController.Instance?.StopAll();
            SceneDecor.Instance?.ClearAll();
            PathTiles.Instance?.ClearAll();
            if (_parallax != null) _parallax.ClearAll();
        }

        private static ParallaxBackground? _parallax;

        private static ParallaxBackground EnsureParallax()
        {
            if (_parallax != null) return _parallax;
            var go = new GameObject("ParallaxBackgroundGO");
            _parallax = go.AddComponent<ParallaxBackground>();
            return _parallax;
        }
    }
}
