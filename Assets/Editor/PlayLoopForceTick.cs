using System;
using UnityEditor;
using UnityEngine;

namespace CrowdDefense.EditorTools
{
    /// <summary>
    /// Forces the Unity Editor to tick its Player Loop while in Play mode even when
    /// the Game/Editor window is unfocused. Required by the night-swarm QA harness
    /// (Unity-MCP execute_code) so coroutines + Time.deltaTime keep flowing while a
    /// remote process is the foreground app.
    ///
    /// Behavior:
    ///   - When Play mode starts: install an EditorApplication.update callback.
    ///   - Each callback (~60Hz from the editor thread) queues a Player Loop update.
    ///   - When Play mode ends: uninstall.
    ///
    /// Disable with Tools/CrowdDefense/QA/Force-Tick/Off, re-enable with /On.
    /// Toggle persists via EditorPrefs (key = cd_force_tick_enabled).
    /// </summary>
    [InitializeOnLoad]
    public static class PlayLoopForceTick
    {
        private const string Pref = "cd_force_tick_enabled";
        private static bool _installed;

        static PlayLoopForceTick()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            // If already in play mode (script reload), install immediately.
            if (EditorApplication.isPlayingOrWillChangePlaymode && EditorPrefs.GetBool(Pref, true))
                Install();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!EditorPrefs.GetBool(Pref, true)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) Install();
            if (state == PlayModeStateChange.ExitingPlayMode) Uninstall();
        }

        private static void Install()
        {
            if (_installed) return;
            EditorApplication.update += Tick;
            _installed = true;
        }

        private static void Uninstall()
        {
            if (!_installed) return;
            EditorApplication.update -= Tick;
            _installed = false;
        }

        private static void Tick()
        {
            // Only inside Play mode — outside, editor ticks normally on its own.
            if (!EditorApplication.isPlaying) return;
            EditorApplication.QueuePlayerLoopUpdate();
        }

        [MenuItem("Tools/CrowdDefense/QA/Force-Tick/On")]
        public static void TurnOn()
        {
            EditorPrefs.SetBool(Pref, true);
            if (EditorApplication.isPlaying) Install();
            Debug.Log("[PlayLoopForceTick] ON — editor will tick player loop in background.");
        }

        [MenuItem("Tools/CrowdDefense/QA/Force-Tick/Off")]
        public static void TurnOff()
        {
            EditorPrefs.SetBool(Pref, false);
            Uninstall();
            Debug.Log("[PlayLoopForceTick] OFF.");
        }

        [MenuItem("Tools/CrowdDefense/QA/Force-Tick/Status")]
        public static void Status()
        {
            Debug.Log($"[PlayLoopForceTick] enabled={EditorPrefs.GetBool(Pref, true)} installed={_installed} isPlaying={EditorApplication.isPlaying}");
        }
    }
}
