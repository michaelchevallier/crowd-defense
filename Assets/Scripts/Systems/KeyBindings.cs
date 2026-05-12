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
            { "pause",       KeyCode.Escape },
            { "speed",       KeyCode.Space },
            { "mute",        KeyCode.M },
            { "debug",       KeyCode.F3 },
            { "birdseye",    KeyCode.V },
            { "follow",      KeyCode.F },
            { "help",        KeyCode.H },
            { "pathPreview", KeyCode.P },
            { "save",        KeyCode.F5 },
            { "load",        KeyCode.F9 },
            { "reset",       KeyCode.R },
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
