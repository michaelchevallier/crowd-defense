#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;

namespace CrowdDefense.Visual
{
    // Port of Weather.js (V5) → Unity Shuriken (CPU) ParticleSystem singletons.
    // Picks the correct ambient effect(s) based on LevelData.LevelTheme and drives
    // their emission rate. Prefabs can be assigned in Inspector; falls back to
    // procedural ParticleSystem built from WeatherParticleConfig when not found.
    public class WeatherController : MonoSingleton<WeatherController>
    {
        [Header("Weather Prefabs (assign Inspector or left null = procedural fallback)")]
        [SerializeField] private GameObject? rainPrefab;
        [SerializeField] private GameObject? snowPrefab;
        [SerializeField] private GameObject? windPrefab;
        [SerializeField] private GameObject? embersPrefab;
        [SerializeField] private GameObject? pollenPrefab;
        [SerializeField] private GameObject? dustPrefab;
        [SerializeField] private GameObject? confettiPrefab;
        [SerializeField] private GameObject? bubblesPrefab;
        [SerializeField] private GameObject? starsPrefab;
        [SerializeField] private GameObject? ashPrefab;

        // Fallback resource paths — loaded at runtime if the Inspector slots are empty.
        private const string PrefabRoot = "Prefabs/Weather/";
        private static readonly Dictionary<WeatherType, string> PrefabPaths = new()
        {
            { WeatherType.Rain,     PrefabRoot + "Rain"     },
            { WeatherType.Snow,     PrefabRoot + "Snow"     },
            { WeatherType.Wind,     PrefabRoot + "Wind"     },
            { WeatherType.Embers,   PrefabRoot + "Embers"   },
            { WeatherType.Pollen,   PrefabRoot + "Pollen"   },
            { WeatherType.Dust,     PrefabRoot + "Dust"     },
            { WeatherType.Confetti, PrefabRoot + "Confetti" },
            { WeatherType.Bubbles,  PrefabRoot + "Bubbles"  },
            { WeatherType.Stars,    PrefabRoot + "Stars"    },
            { WeatherType.Ash,      PrefabRoot + "Ash"      },
        };

        // Active ParticleSystems for the current level (may be more than one per theme)
        private readonly List<ParticleSystem> _active = new();

        // Theme → weather type(s) mapping (mirrors V5 theme.weather field)
        private static readonly Dictionary<LevelTheme, WeatherType[]> ThemeWeather = new()
        {
            { LevelTheme.Plaine,     new[] { WeatherType.Pollen }                       },
            { LevelTheme.Foret,      new[] { WeatherType.Rain, WeatherType.Pollen }     },
            { LevelTheme.Desert,     new[] { WeatherType.Dust, WeatherType.Wind }       },
            { LevelTheme.Volcan,     new[] { WeatherType.Embers, WeatherType.Ash }      },
            { LevelTheme.Apocalypse, new[] { WeatherType.Ash, WeatherType.Dust }        },
            { LevelTheme.Espace,     new[] { WeatherType.Stars, WeatherType.Snow }      },
            { LevelTheme.Submarin,   new[] { WeatherType.Bubbles }                      },
            { LevelTheme.Medieval,   new[] { WeatherType.Rain, WeatherType.Wind }       },
            { LevelTheme.Cyberpunk,  new[] { WeatherType.Snow, WeatherType.Wind }       },
            { LevelTheme.Foire,      new[] { WeatherType.Confetti, WeatherType.Pollen } },
        };

        // V4-parity particle configs per weather type (color, emission/sec, gravity, speed, size)
        private readonly struct WeatherConfig
        {
            public readonly Color color;
            public readonly float emissionRate;
            public readonly float gravity;       // negative = rise, positive = fall
            public readonly float speed;
            public readonly float size;
            public readonly float lifetime;

            public WeatherConfig(Color col, float rate, float grav, float spd, float sz, float life)
            {
                color = col; emissionRate = rate; gravity = grav;
                speed = spd; size = sz; lifetime = life;
            }
        }

        private static readonly Dictionary<WeatherType, WeatherConfig> Configs = new()
        {
            // Foret spores : green, slow fall, drift
            { WeatherType.Pollen,   new WeatherConfig(new Color(0.61f, 1f, 0.48f), 5f,  0.2f,  0.3f, 0.15f, 4f)  },
            // Rain : light blue, medium fall
            { WeatherType.Rain,     new WeatherConfig(new Color(0.6f,  0.8f, 1f),  12f, 2.0f,  2f,   0.08f, 1.5f)},
            // Volcan embers : orange, rise
            { WeatherType.Embers,   new WeatherConfig(new Color(1f,    0.33f,0.13f),8f, -1.5f, 1f,   0.18f, 2f)  },
            // Ash : grey, slow fall
            { WeatherType.Ash,      new WeatherConfig(new Color(0.6f,  0.6f, 0.6f),6f,  0.4f,  0.5f, 0.12f, 3f)  },
            // Desert/Apocalypse dust : tan
            { WeatherType.Dust,     new WeatherConfig(new Color(1f,    0.88f,0.69f),8f,  0f,    1.5f, 0.18f, 2.5f)},
            // Wind streaks : white translucent
            { WeatherType.Wind,     new WeatherConfig(new Color(1f,    1f,   1f,0.3f),10f,0f,  3f,   0.06f, 1f)  },
            // Espace stars : white, slow scroll down
            { WeatherType.Stars,    new WeatherConfig(Color.white,      2f,  0.1f,  0.2f, 0.1f,  5f)  },
            // Submarin bubbles : white, rise
            { WeatherType.Bubbles,  new WeatherConfig(new Color(0.8f,  0.9f, 1f,0.7f),6f,-0.8f, 0.4f, 0.15f, 3f)  },
            // Snow : white, slow fall
            { WeatherType.Snow,     new WeatherConfig(Color.white,      10f,  0.3f,  0.3f, 0.12f, 4f)  },
            // Foire confetti : randomized via gradient below
            { WeatherType.Confetti, new WeatherConfig(Color.white,      8f,   0.5f,  0.8f, 0.12f, 3f)  },
        };

        // Entry point named per brief spec; delegates to ApplyTheme.
        public void SetWeather(LevelTheme theme) => ApplyTheme(theme);

        public void ApplyTheme(LevelTheme theme)
        {
            StopAll();

            // Respect player setting — cd.gfx.weather (default on)
            var settings = UI.SettingsRegistry.Instance;
            if (settings != null && !settings.WeatherEnabled) return;

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
            if (prefab != null)
            {
                var go = Instantiate(prefab, transform);
                go.name = $"Weather_{wt}";
                var ps = go.GetComponent<ParticleSystem>();
                if (ps != null) { ps.Play(true); return ps; }
                Destroy(go);
            }

            // Procedural fallback — build ParticleSystem from config data
            return BuildProcedural(wt);
        }

        private ParticleSystem? BuildProcedural(WeatherType wt)
        {
            if (!Configs.TryGetValue(wt, out var cfg)) return null;

            var go = new GameObject($"Weather_{wt}");
            go.transform.SetParent(transform);
            // Position well above gameplay area so particles fall through the screen
            go.transform.localPosition = new Vector3(0f, 12f, -1f);

            var ps = go.AddComponent<ParticleSystem>();

            // --- Main module ---
            var main = ps.main;
            main.loop = true;
            main.startLifetime = cfg.lifetime;
            main.startSpeed = cfg.speed;
            main.startSize = cfg.size;
            main.startColor = wt == WeatherType.Confetti
                ? new ParticleSystem.MinMaxGradient(BuildRainbow())
                : new ParticleSystem.MinMaxGradient(cfg.color);
            main.gravityModifier = cfg.gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.CeilToInt(cfg.emissionRate * cfg.lifetime * 2f);

            // --- Emission ---
            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = cfg.emissionRate;

            // --- Shape : box across top of screen ---
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 0.1f, 0.1f);

            // --- Velocity over lifetime : horizontal drift for dust/wind/spores ---
            if (wt is WeatherType.Dust or WeatherType.Wind or WeatherType.Pollen)
            {
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            }

            // --- Bubbles & embers : wobble on X ---
            if (wt is WeatherType.Bubbles or WeatherType.Embers)
            {
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = 0.3f;
                noise.frequency = 0.5f;
                noise.scrollSpeed = 0.5f;
            }

            // --- Color over lifetime : fade out at end ---
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(cfg.color, 0f), new GradientColorKey(cfg.color, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            ps.Play(true);
            return ps;
        }

        private static Gradient BuildRainbow()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.red,     0f),
                    new GradientColorKey(Color.yellow,  0.2f),
                    new GradientColorKey(Color.green,   0.4f),
                    new GradientColorKey(Color.cyan,    0.6f),
                    new GradientColorKey(Color.blue,    0.8f),
                    new GradientColorKey(Color.magenta, 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            return g;
        }

        private GameObject? ResolvePrefab(WeatherType wt)
        {
            var fromInspector = wt switch
            {
                WeatherType.Rain     => rainPrefab,
                WeatherType.Snow     => snowPrefab,
                WeatherType.Wind     => windPrefab,
                WeatherType.Embers   => embersPrefab,
                WeatherType.Pollen   => pollenPrefab,
                WeatherType.Dust     => dustPrefab,
                WeatherType.Confetti => confettiPrefab,
                WeatherType.Bubbles  => bubblesPrefab,
                WeatherType.Stars    => starsPrefab,
                WeatherType.Ash      => ashPrefab,
                _ => null
            };
            if (fromInspector != null) return fromInspector;

            if (!PrefabPaths.TryGetValue(wt, out var path)) return null;
            return Resources.Load<GameObject>(path);
        }

        // Phase-4 stubs — ambient audio layer (not yet wired)
        public void PlayAmbientAudio(WeatherType wt) { /* TODO Phase 4 */ }
        public void StopAmbientAudio() { /* TODO Phase 4 */ }
    }

    public enum WeatherType
    {
        Rain,
        Snow,
        Wind,
        Embers,
        Pollen,
        Dust,
        Confetti,
        Bubbles,
        Stars,
        Ash,
    }
}
