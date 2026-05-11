#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;

namespace CrowdDefense.Visual
{
    // Port of Weather.js (V5) → Unity Shuriken (CPU) ParticleSystem singletons.
    // Picks the correct ambient effect(s) based on LevelData.LevelTheme and drives
    // their emission rate. Audio ambient layer is Phase-4 polish (stubs provided).
    public class WeatherController : MonoSingleton<WeatherController>
    {
        [Header("Weather Prefabs (assign Inspector or left null = no effect)")]
        [SerializeField] private GameObject? rainPrefab;
        [SerializeField] private GameObject? snowPrefab;
        [SerializeField] private GameObject? embersPrefab;
        [SerializeField] private GameObject? pollenPrefab;
        [SerializeField] private GameObject? dustPrefab;

        // Fallback resource paths — loaded at runtime if the Inspector slots are empty.
        private const string PrefabRoot = "Prefabs/Weather/";
        private static readonly Dictionary<WeatherType, string> PrefabPaths = new()
        {
            { WeatherType.Rain,   PrefabRoot + "Rain"   },
            { WeatherType.Snow,   PrefabRoot + "Snow"   },
            { WeatherType.Embers, PrefabRoot + "Embers" },
            { WeatherType.Pollen, PrefabRoot + "Pollen" },
            { WeatherType.Dust,   PrefabRoot + "Dust"   },
        };

        // Active ParticleSystems for the current level (may be more than one per theme)
        private readonly List<ParticleSystem> _active = new();

        // Theme → weather type(s) mapping (mirrors V5 theme.weather field)
        private static readonly Dictionary<LevelTheme, WeatherType[]> ThemeWeather = new()
        {
            { LevelTheme.Plaine,     new[] { WeatherType.Pollen }           },
            { LevelTheme.Foret,      new[] { WeatherType.Rain, WeatherType.Pollen } },
            { LevelTheme.Desert,     new[] { WeatherType.Dust }             },
            { LevelTheme.Volcan,     new[] { WeatherType.Embers }           },
            { LevelTheme.Apocalypse, new[] { WeatherType.Dust }             },
            { LevelTheme.Espace,     new[] { WeatherType.Snow }             },
            { LevelTheme.Submarin,   System.Array.Empty<WeatherType>()      },
            { LevelTheme.Medieval,   new[] { WeatherType.Rain }             },
            { LevelTheme.Cyberpunk,  new[] { WeatherType.Snow }             },
            { LevelTheme.Foire,      new[] { WeatherType.Pollen }           },
        };

        public void ApplyTheme(LevelTheme theme)
        {
            StopAll();

            if (!ThemeWeather.TryGetValue(theme, out var types)) return;

            foreach (var wt in types)
            {
                var ps = SpawnEffect(wt);
                if (ps != null) _active.Add(ps);
            }
        }

        public void StopAll()
        {
            foreach (var ps in _active)
            {
                if (ps == null) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Destroy(ps.gameObject);
            }
            _active.Clear();
        }

        private ParticleSystem? SpawnEffect(WeatherType wt)
        {
            var prefab = ResolvePrefab(wt);
            if (prefab == null) return null;

            var go = Instantiate(prefab, transform);
            go.name = $"Weather_{wt}";
            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[WeatherController] Prefab '{wt}' has no ParticleSystem component.");
#endif
                Destroy(go);
                return null;
            }
            ps.Play(true);
            return ps;
        }

        private GameObject? ResolvePrefab(WeatherType wt)
        {
            // Inspector slot takes precedence
            var fromInspector = wt switch
            {
                WeatherType.Rain   => rainPrefab,
                WeatherType.Snow   => snowPrefab,
                WeatherType.Embers => embersPrefab,
                WeatherType.Pollen => pollenPrefab,
                WeatherType.Dust   => dustPrefab,
                _ => null
            };
            if (fromInspector != null) return fromInspector;

            // Fallback: Resources.Load
            if (!PrefabPaths.TryGetValue(wt, out var path)) return null;
            var loaded = Resources.Load<GameObject>(path);
#if UNITY_EDITOR
            if (loaded == null)
                Debug.LogWarning($"[WeatherController] Weather prefab not found at Resources/{path}. " +
                                 "Create it via Assets/Prefabs/Weather/ or assign in Inspector.");
#endif
            return loaded;
        }

        // Phase-4 stubs — ambient audio layer (not yet wired)
        public void PlayAmbientAudio(WeatherType wt) { /* TODO Phase 4 */ }
        public void StopAmbientAudio() { /* TODO Phase 4 */ }
    }

    public enum WeatherType
    {
        Rain,
        Snow,
        Embers,
        Pollen,
        Dust,
    }
}
