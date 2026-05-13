#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;

namespace CrowdDefense.Systems
{
    public sealed class KeyBindings : MonoSingleton<KeyBindings>
    {
        private static readonly Dictionary<string, KeyCode> Defaults = new()
        {
            { "console",          KeyCode.F1 },
            { "pause",            KeyCode.Escape },
            { "pause_alt",        KeyCode.P },
            { "speed",            KeyCode.Space },
            { "mute",             KeyCode.M },
            { "debug",            KeyCode.F3 },
            { "birdseye",         KeyCode.V },
            { "follow",           KeyCode.F },
            { "help",             KeyCode.H },
            { "pathPreview",      KeyCode.P },
            { "save",             KeyCode.F5 },
            { "load",             KeyCode.F9 },
            { "reset",            KeyCode.R },
            { "restart_fast",     KeyCode.None }, // Shift+R handled in CameraController
            { "tower_select_1",   KeyCode.Alpha1 },
            { "tower_select_2",   KeyCode.Alpha2 },
            { "tower_select_3",   KeyCode.Alpha3 },
            { "tower_select_4",   KeyCode.Alpha4 },
            { "tower_select_5",   KeyCode.Alpha5 },
            { "tower_select_6",   KeyCode.Alpha6 },
            { "tower_select_7",   KeyCode.Alpha7 },
            { "tower_select_8",   KeyCode.Alpha8 },
            { "tower_select_9",   KeyCode.Alpha9 },
            { "tower_select_0",   KeyCode.Alpha0 },
            { "skill_q",          KeyCode.Q },
            { "skill_w",          KeyCode.W },
            { "skill_e",          KeyCode.E },
            { "skill_r",          KeyCode.R },
            { "launch_wave",      KeyCode.N },
            { "speed_adjust_up",  KeyCode.Equals },
            { "speed_adjust_down", KeyCode.Minus },
        };

        private readonly Dictionary<string, KeyCode> _bindings = new();

        protected override void OnAwakeSingleton()
        {
            foreach (var (action, defaultKey) in Defaults)
            {
                string prefKey = $"keybind_{action}_v1";
                int stored = PlayerPrefs.GetInt(prefKey, -1);
                _bindings[action] = stored >= 0 ? (KeyCode)stored : defaultKey;
            }
        }

        public static KeyCode GetKey(string action)
        {
            if (Instance == null) return GetDefault(action);
            if (Instance._bindings.TryGetValue(action, out var key)) return key;
            return GetDefault(action);
        }

        public static KeyCode GetDefault(string action) =>
            Defaults.TryGetValue(action, out var k) ? k : KeyCode.None;

        public static IReadOnlyDictionary<string, KeyCode> AllDefaults => Defaults;

        public static void SetKey(string action, KeyCode key)
        {
            if (Instance == null) return;
            Instance._bindings[action] = key;
            PlayerPrefs.SetInt($"keybind_{action}_v1", (int)key);
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            if (Instance == null) return;
            foreach (var (action, defaultKey) in Defaults)
            {
                Instance._bindings[action] = defaultKey;
                PlayerPrefs.DeleteKey($"keybind_{action}_v1");
            }
            PlayerPrefs.Save();
        }
    }
}
