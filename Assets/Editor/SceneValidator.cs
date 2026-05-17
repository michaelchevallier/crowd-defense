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
                new("CrowdDefense.Systems.LevelRunner, CrowdDefense"),
                new("CrowdDefense.Systems.Economy, CrowdDefense"),
                new("CrowdDefense.Systems.WaveManager, CrowdDefense"),
                new("CrowdDefense.Systems.PlacementController, CrowdDefense"),
                new("CrowdDefense.Systems.MapRenderer, CrowdDefense"),
                new("CrowdDefense.Systems.EnemyPathingSystem, CrowdDefense"),
                new("CrowdDefense.Systems.Synergies, CrowdDefense"),
                new("CrowdDefense.Systems.AudioController, CrowdDefense"),
                new("CrowdDefense.Systems.MusicManager, CrowdDefense"),
                new("CrowdDefense.UI.HudController, CrowdDefense"),
                new("CrowdDefense.Entities.Castle, CrowdDefense"),
                new("CrowdDefense.Entities.Hero, CrowdDefense"),
            },
            ["Menu"] = new Entry[]
            {
                new("CrowdDefense.Systems.AudioController, CrowdDefense"),
                new("CrowdDefense.Systems.MusicManager, CrowdDefense"),
                new("CrowdDefense.UI.MenuController, CrowdDefense"),
                new("CrowdDefense.UI.HudController, CrowdDefense", optional: true),
            },
            ["Loader"] = new Entry[]
            {
                new("CrowdDefense.Systems.LoaderToMenu, CrowdDefense"),
            },
            ["WorldMap"] = new Entry[]
            {
                new("CrowdDefense.UI.WorldMapController, CrowdDefense"),
                new("CrowdDefense.Systems.AudioController, CrowdDefense"),
                new("CrowdDefense.Systems.MusicManager, CrowdDefense"),
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
