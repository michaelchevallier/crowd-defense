#nullable enable
using System;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    // Thin static event bus for level lifecycle.
    // Avoids direct coupling to LevelRunner hot zone — subscribers (Weather, SceneDecor,
    // future systems) hook here instead of touching LevelRunner.cs.
    public static class LevelEvents
    {
        // Fired when a level is fully loaded and play begins.
        // Carries LevelData + the world-space bounds of the playable grid.
        public static event Action<LevelData, Bounds>? OnLevelStart;

        // Fired on cleanup (GameOver, Victory, scene unload).
        public static event Action? OnLevelEnd;

        public static void RaiseLevelStart(LevelData data, Bounds gridBounds) =>
            OnLevelStart?.Invoke(data, gridBounds);

        public static void RaiseLevelEnd() =>
            OnLevelEnd?.Invoke();
    }
}
