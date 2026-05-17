#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Visual
{
    // Port of Weather.js (V4) → Unity Shuriken (CPU) ParticleSystem.
    // 11 themed presets driven by LevelEvents.OnLevelStart.
    // SpawnPreset(wt) is the public API for runtime callers (R6-PARITY-012).
    public class WeatherController : MonoSingleton<WeatherController>
    {
        [Header("Weather Prefabs (assign Inspector or null = procedural fallback)")]
        [SerializeField] private GameObject? cloudsPrefab;
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
        [SerializeField] private GameObject? mistPrefab;
        [SerializeField] private GameObject? firefliesPrefab;
        [SerializeField] private GameObject? neonRainPrefab;

        private static readonly Dictionary<WeatherType, string> AmbientClips = new()
        {
            { WeatherType.Pollen,    "ambient_forest"   },
            { WeatherType.Rain,      "ambient_forest"   },
            { WeatherType.Dust,      "ambient_wind"     },
            { WeatherType.Wind,      "ambient_wind"     },
            { WeatherType.Embers,    "ambient_volcano"  },
            { WeatherType.Ash,       "ambient_volcano"  },
            { WeatherType.Confetti,  "ambient_calm"     },
            { WeatherType.Bubbles,   "ambient_calm"     },
            { WeatherType.Snow,      "ambient_blizzard" },
            { WeatherType.Stars,     "ambient_blizzard" },
            { WeatherType.Mist,      "ambient_calm"     },
            { WeatherType.NeonRain,  "ambient_wind"     },
        };

        private static readonly Dictionary<WeatherType, Color> SkyTints = new()
        {
            { WeatherType.Clouds,   new Color(0.78f, 0.88f, 1.00f) },
            { WeatherType.Dust,     new Color(0.90f, 0.65f, 0.30f) },
            { WeatherType.Wind,     new Color(0.90f, 0.65f, 0.30f) },
            { WeatherType.Snow,     new Color(0.70f, 0.82f, 1.00f) },
            { WeatherType.Stars,    new Color(0.70f, 0.82f, 1.00f) },
            { WeatherType.Embers,   new Color(1.00f, 0.28f, 0.10f) },
            { WeatherType.Ash,      new Color(1.00f, 0.28f, 0.10f) },
            { WeatherType.Pollen,   new Color(0.35f, 0.72f, 0.30f) },
            { WeatherType.Rain,     new Color(0.35f, 0.72f, 0.30f) },
            { WeatherType.Confetti, new Color(0.90f, 0.85f, 1.00f) },
            { WeatherType.Bubbles,  new Color(0.40f, 0.70f, 1.00f) },
            { WeatherType.Mist,     new Color(0.55f, 0.75f, 0.45f) },
            { WeatherType.NeonRain, new Color(0.40f, 0.20f, 0.80f) },
        };

        private const string PrefabRoot = "Prefabs/Weather/";
        private static readonly Dictionary<WeatherType, string> PrefabPaths = new()
        {
            { WeatherType.Clouds,    PrefabRoot + "Clouds"    },
            { WeatherType.Rain,      PrefabRoot + "Rain"      },
            { WeatherType.Snow,      PrefabRoot + "Snow"      },
            { WeatherType.Wind,      PrefabRoot + "Wind"      },
            { WeatherType.Embers,    PrefabRoot + "Embers"    },
            { WeatherType.Pollen,    PrefabRoot + "Pollen"    },
            { WeatherType.Dust,      PrefabRoot + "Dust"      },
            { WeatherType.Confetti,  PrefabRoot + "Confetti"  },
            { WeatherType.Bubbles,   PrefabRoot + "Bubbles"   },
            { WeatherType.Stars,     PrefabRoot + "Stars"     },
            { WeatherType.Ash,       PrefabRoot + "Ash"       },
            { WeatherType.Mist,      PrefabRoot + "Mist"      },
            { WeatherType.Fireflies, PrefabRoot + "Fireflies" },
            { WeatherType.NeonRain,  PrefabRoot + "NeonRain"  },
        };

        private readonly List<ParticleSystem> _active = new();

        // 10 LevelTheme enum values → 11 spec presets (Apocalypse = DesertStorm dense).
        private static readonly Dictionary<LevelTheme, WeatherType[]> ThemeWeather = new()
        {
            { LevelTheme.Plaine,     new[] { WeatherType.Clouds                          } },
            { LevelTheme.Foret,      new[] { WeatherType.Pollen, WeatherType.Mist        } },
            { LevelTheme.Desert,     new[] { WeatherType.Dust,   WeatherType.Wind        } },
            { LevelTheme.Apocalypse, new[] { WeatherType.Dust,   WeatherType.Ash, WeatherType.Wind } }, // DesertStorm dense
            { LevelTheme.Volcan,     new[] { WeatherType.Embers, WeatherType.Ash         } },
            { LevelTheme.Espace,     new[] { WeatherType.Stars,  WeatherType.Snow        } },
            { LevelTheme.Submarin,   new[] { WeatherType.Bubbles                         } },
            { LevelTheme.Medieval,   new[] { WeatherType.Dust,   WeatherType.Wind        } },
            { LevelTheme.Cyberpunk,  new[] { WeatherType.NeonRain, WeatherType.Wind      } },
            { LevelTheme.Foire,      new[] { WeatherType.Confetti, WeatherType.Pollen    } },
        };

        private readonly struct WeatherConfig
        {
            public readonly Color  color;
            public readonly float  emissionRate;
            public readonly float  gravity;
            public readonly float  speed;
            public readonly float  size;
            public readonly float  lifetime;
            public readonly bool   noiseEnabled;

            public WeatherConfig(Color col, float rate, float grav, float spd, float sz, float life, bool noise = false)
            {
                color = col; emissionRate = rate; gravity = grav;
                speed = spd; size = sz; lifetime = life; noiseEnabled = noise;
            }
        }

        private static readonly Dictionary<WeatherType, WeatherConfig> Configs = new()
        {
            // plaine — high drifting clouds (large, slow, fade)
            { WeatherType.Clouds,    new WeatherConfig(new Color(0.95f, 0.97f, 1f, 0.7f), 1f,  0f,    0.4f, 2.5f, 12f)       },
            // foret spores
            { WeatherType.Pollen,    new WeatherConfig(new Color(0.61f, 1f,   0.48f),     5f,  0.2f,  0.3f, 0.15f, 4f, true)  },
            // rain
            { WeatherType.Rain,      new WeatherConfig(new Color(0.6f,  0.8f, 1f),        12f, 2.0f,  2f,   0.08f, 1.5f)      },
            // volcan embers — rising
            { WeatherType.Embers,    new WeatherConfig(new Color(1f,    0.33f,0.13f),     8f, -1.5f,  1f,   0.18f, 2f,  true)  },
            // ash — slow fall
            { WeatherType.Ash,       new WeatherConfig(new Color(0.6f,  0.6f, 0.6f),     6f,  0.4f,  0.5f, 0.12f, 3f)         },
            // desert/storm dust — ground-level tan horizontal
            { WeatherType.Dust,      new WeatherConfig(new Color(1f,    0.88f,0.69f),     8f,  0f,    1.5f, 0.18f, 2.5f)       },
            // wind streaks translucent
            { WeatherType.Wind,      new WeatherConfig(new Color(1f,    1f,   1f, 0.3f), 10f,  0f,    3f,   0.06f, 1f)         },
            // espace stars — distant slow scroll
            { WeatherType.Stars,     new WeatherConfig(Color.white,                        2f,  0.1f,  0.2f, 0.1f,  5f)         },
            // submarin bubbles — rising
            { WeatherType.Bubbles,   new WeatherConfig(new Color(0.8f,  0.9f, 1f, 0.7f), 6f, -0.8f,  0.4f, 0.15f, 3f,  true)  },
            // glacier snow — slow fall
            { WeatherType.Snow,      new WeatherConfig(Color.white,                       10f,  0.3f,  0.3f, 0.12f, 4f)         },
            // marais mist — low ground fog
            { WeatherType.Mist,      new WeatherConfig(new Color(0.75f, 0.90f,0.75f,0.4f),3f,  0f,    0.2f, 1.5f,  6f,  true)  },
            // marais fireflies — sparse rising glow
            { WeatherType.Fireflies, new WeatherConfig(new Color(0.85f, 1f,   0.2f, 0.8f),1f, -0.2f, 0.15f,0.12f, 5f,  true)  },
            // cyberpunk neon rain — fast, tinted magenta
            { WeatherType.NeonRain,  new WeatherConfig(new Color(0.8f,  0.2f, 1f, 0.9f), 20f,  3f,    3f,   0.06f, 0.8f)       },
            // foire confetti gradient (color overridden via BuildRainbow below)
            { WeatherType.Confetti,  new WeatherConfig(Color.white,                        8f,  0.5f,  0.8f, 0.12f, 3f)         },
        };

        // ── Lifecycle ────────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            LevelEvents.OnLevelStart += HandleLevelStart;
            LevelEvents.OnLevelEnd   += StopAll;
        }

        private void OnDisable()
        {
            LevelEvents.OnLevelStart -= HandleLevelStart;
            LevelEvents.OnLevelEnd   -= StopAll;
        }

        private void HandleLevelStart(LevelData level, UnityEngine.Bounds _) => ApplyTheme(level.LevelTheme);

        // ── Public API ───────────────────────────────────────────────────────────────

        // Called by R6-PARITY-012 to spawn a specific preset at runtime (e.g. SandStorm event).
        public ParticleSystem? SpawnPreset(WeatherType wt)
        {
            var ps = SpawnEffect(wt);
            if (ps != null) _active.Add(ps);
            return ps;
        }

        public void SetWeather(LevelTheme theme) => ApplyTheme(theme);

        public void ApplyAmbient(int worldId)
        {
            StopAll();
            var settings = UI.SettingsRegistry.Instance;
            if (settings != null && !settings.WeatherEnabled) return;

            WeatherType[] types = worldId switch
            {
                1 or 2 => new[] { WeatherType.Clouds },
                3 or 4 => new[] { WeatherType.Rain   },
                5 or 6 => new[] { WeatherType.Snow   },
                7 or 8 => new[] { WeatherType.Embers, WeatherType.Ash },
                _      => new[] { WeatherType.Stars,  WeatherType.Snow },
            };

            foreach (var wt in types)
            {
                var ps = SpawnEffect(wt);
                if (ps != null) _active.Add(ps);
            }
            foreach (var wt in types) { if (AmbientClips.ContainsKey(wt)) { PlayAmbientAudio(wt); break; } }
            foreach (var wt in types) { if (SkyTints.ContainsKey(wt))     { ApplySkyGradient(wt);  break; } }
        }

        public void ApplyTheme(LevelTheme theme)
        {
            StopAll();
            var settings = UI.SettingsRegistry.Instance;
            if (settings != null && !settings.WeatherEnabled) return;

            if (!ThemeWeather.TryGetValue(theme, out var types)) return;

            foreach (var wt in types)
            {
                var ps = SpawnEffect(wt);
                if (ps != null) _active.Add(ps);
            }
            foreach (var wt in types) { if (AmbientClips.ContainsKey(wt)) { PlayAmbientAudio(wt); break; } }
            foreach (var wt in types) { if (SkyTints.ContainsKey(wt))     { ApplySkyGradient(wt);  break; } }
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
            StopAmbientAudio();
        }

        // ── Audio / sky helpers ──────────────────────────────────────────────────────

        public void PlayAmbientAudio(WeatherType type)
        {
            if (!AmbientClips.TryGetValue(type, out var key)) return;
            var clip = Resources.Load<AudioClip>($"Audio/Ambient/{key}");
            if (clip == null) return;
            AudioController.Instance?.PlayLoop(clip, "ambient", 0.4f);
        }

        public void StopAmbientAudio() => AudioController.Instance?.StopChannel("ambient");

        // R2-recovery : Camera uses Skybox clearFlags (was SolidColor → flat blue bug),
        // so the equirectangular skybox material renders directly. The theme tint is
        // applied via _Tint on the skybox material when supported. Background color is
        // still set as a fallback if no skybox is assigned.
        private const float SkyBackgroundDarken = 0.55f;

        public static void ApplySkyGradient(WeatherType type)
        {
            if (!SkyTints.TryGetValue(type, out var tint)) return;
            RenderSettings.ambientLight = tint;
            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Tint"))
                RenderSettings.skybox.SetColor("_Tint", tint);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.backgroundColor = new Color(
                    tint.r * SkyBackgroundDarken,
                    tint.g * SkyBackgroundDarken,
                    tint.b * SkyBackgroundDarken,
                    1f);
            }
        }

        public static void ApplySkyGradient(LevelTheme theme)
        {
            if (!ThemeWeather.TryGetValue(theme, out var types)) return;
            foreach (var wt in types)
                if (SkyTints.ContainsKey(wt)) { ApplySkyGradient(wt); return; }
        }

        // ── Particle spawning ────────────────────────────────────────────────────────

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
            return BuildProcedural(wt);
        }

        private ParticleSystem? BuildProcedural(WeatherType wt)
        {
            if (!Configs.TryGetValue(wt, out var cfg)) return null;

            var go = new GameObject($"Weather_{wt}");
            go.transform.SetParent(transform);
            go.transform.localPosition = wt switch
            {
                WeatherType.Clouds   => new Vector3(0f, 20f, -1f),  // high
                WeatherType.Mist     => new Vector3(0f,  0f, -1f),  // ground level
                WeatherType.Dust     => new Vector3(0f,  1f, -1f),  // low
                WeatherType.Fireflies=> new Vector3(0f,  2f, -1f),  // low
                WeatherType.Bubbles  => new Vector3(0f,  0f, -1f),  // seafloor
                WeatherType.Embers   => new Vector3(0f,  1f, -1f),  // ground rising
                _                   => new Vector3(0f, 12f, -1f),  // default top
            };

            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop            = true;
            main.startLifetime   = cfg.lifetime;
            main.startSpeed      = cfg.speed;
            main.startSize       = wt == WeatherType.Clouds
                                    ? new ParticleSystem.MinMaxCurve(1.5f, 3f)
                                    : new ParticleSystem.MinMaxCurve(cfg.size);
            main.startColor      = wt == WeatherType.Confetti
                                    ? new ParticleSystem.MinMaxGradient(BuildRainbow())
                                    : new ParticleSystem.MinMaxGradient(cfg.color);
            main.gravityModifier = cfg.gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles    = Mathf.Max(1, Mathf.CeilToInt(cfg.emissionRate * cfg.lifetime * 2f));

            var em = ps.emission;
            em.enabled      = true;
            em.rateOverTime = cfg.emissionRate;

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = wt == WeatherType.Clouds
                                ? new Vector3(40f, 1f, 0.1f)
                                : new Vector3(20f, 0.1f, 0.1f);

            // Horizontal drift — dust/wind/spores/mist/clouds
            if (wt is WeatherType.Dust or WeatherType.Wind or WeatherType.Pollen
                    or WeatherType.Mist or WeatherType.Clouds)
            {
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.x       = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            }

            // Noise module — wobble for organic effects
            if (cfg.noiseEnabled)
            {
                var noise = ps.noise;
                noise.enabled     = true;
                noise.strength    = wt == WeatherType.Mist ? 0.15f : 0.30f;
                noise.frequency   = 0.5f;
                noise.scrollSpeed = 0.5f;
            }

            // Color over lifetime — fade out tail
            var col  = ps.colorOverLifetime;
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
                    new GradientColorKey(Color.red,     0f  ),
                    new GradientColorKey(Color.yellow,  0.2f),
                    new GradientColorKey(Color.green,   0.4f),
                    new GradientColorKey(Color.cyan,    0.6f),
                    new GradientColorKey(Color.blue,    0.8f),
                    new GradientColorKey(Color.magenta, 1f  ),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            return g;
        }

        private GameObject? ResolvePrefab(WeatherType wt)
        {
            var fromInspector = wt switch
            {
                WeatherType.Clouds    => cloudsPrefab,
                WeatherType.Rain      => rainPrefab,
                WeatherType.Snow      => snowPrefab,
                WeatherType.Wind      => windPrefab,
                WeatherType.Embers    => embersPrefab,
                WeatherType.Pollen    => pollenPrefab,
                WeatherType.Dust      => dustPrefab,
                WeatherType.Confetti  => confettiPrefab,
                WeatherType.Bubbles   => bubblesPrefab,
                WeatherType.Stars     => starsPrefab,
                WeatherType.Ash       => ashPrefab,
                WeatherType.Mist      => mistPrefab,
                WeatherType.Fireflies => firefliesPrefab,
                WeatherType.NeonRain  => neonRainPrefab,
                _ => null
            };
            if (fromInspector != null) return fromInspector;
            if (!PrefabPaths.TryGetValue(wt, out var path)) return null;
            return Resources.Load<GameObject>(path);
        }
    }

    public enum WeatherType
    {
        Clouds,
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
        Mist,
        Fireflies,
        NeonRain,
    }
}
