#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrowdDefense.Editor
{
    [InitializeOnLoad]
    public static class SceneValidator
    {
        private readonly struct Entry
        {
            public readonly string FullType;
            public readonly bool   Optional;
            public Entry(string fullType, bool optional = false) { FullType = fullType; Optional = optional; }
        }

        private static readonly Dictionary<string, Entry[]> s_Expected = new()
        {
            ["Main"] = new Entry[]
            {
                new("CrowdDefense.Systems.LevelRunner, Assembly-CSharp"),
                new("CrowdDefense.Systems.Economy, Assembly-CSharp"),
                new("CrowdDefense.Systems.WaveManager, Assembly-CSharp"),
                new("CrowdDefense.Systems.PlacementController, Assembly-CSharp"),
                new("CrowdDefense.Systems.MapRenderer, Assembly-CSharp"),
                new("CrowdDefense.Systems.EnemyPathingSystem, Assembly-CSharp"),
                new("CrowdDefense.Systems.Synergies, Assembly-CSharp"),
                new("CrowdDefense.Systems.AudioController, Assembly-CSharp"),
                new("CrowdDefense.Systems.MusicManager, Assembly-CSharp"),
                new("CrowdDefense.UI.HudController, Assembly-CSharp"),
                new("CrowdDefense.Entities.Castle, Assembly-CSharp"),
                new("CrowdDefense.Entities.Hero, Assembly-CSharp"),
            },
            ["Menu"] = new Entry[]
            {
                new("CrowdDefense.Systems.AudioController, Assembly-CSharp"),
                new("CrowdDefense.Systems.MusicManager, Assembly-CSharp"),
                new("CrowdDefense.UI.MenuController, Assembly-CSharp"),
                new("CrowdDefense.UI.HudController, Assembly-CSharp", optional: true),
            },
            ["Loader"] = new Entry[]
            {
                new("CrowdDefense.Systems.LoaderToMenu, Assembly-CSharp"),
            },
            ["WorldMap"] = new Entry[]
            {
                new("CrowdDefense.UI.WorldMapController, Assembly-CSharp"),
                new("CrowdDefense.Systems.AudioController, Assembly-CSharp"),
                new("CrowdDefense.Systems.MusicManager, Assembly-CSharp"),
            },
        };

        static SceneValidator()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!s_Expected.TryGetValue(scene.name, out var entries)) return;

            var missing   = new List<string>();
            var optional  = new List<string>();
            int verified  = 0;

            foreach (var entry in entries)
            {
                var type = Type.GetType(entry.FullType);
                if (type == null)
                {
                    if (!entry.Optional) missing.Add($"{entry.FullType} (type not found)");
                    continue;
                }

#pragma warning disable CS0618
                var found = UnityEngine.Object.FindObjectOfType(type);
#pragma warning restore CS0618

                if (found == null)
                {
                    if (entry.Optional) optional.Add(ShortName(entry.FullType));
                    else                missing.Add(ShortName(entry.FullType));
                }
                else
                {
                    verified++;
                }
            }

            if (missing.Count > 0)
            {
                Debug.LogWarning(
                    $"[SceneValidator] Scene '{scene.name}' — {missing.Count} singleton(s) MISSING: " +
                    string.Join(", ", missing));
            }
            else
            {
                var optInfo = optional.Count > 0 ? $" | optional absent: {string.Join(", ", optional)}" : "";
                Debug.Log(
                    $"[SceneValidator] Scene '{scene.name}' OK — {verified} singleton(s) verified{optInfo}");
            }
        }

        private static string ShortName(string fullType)
        {
            var comma = fullType.IndexOf(',');
            var name  = comma >= 0 ? fullType[..comma] : fullType;
            var dot   = name.LastIndexOf('.');
            return dot >= 0 ? name[(dot + 1)..] : name;
        }
    }
}
#endif
