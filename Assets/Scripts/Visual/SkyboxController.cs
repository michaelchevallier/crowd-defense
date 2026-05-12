#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Visual
{
    // Swaps RenderSettings.skybox per level theme using equirectangular panoramic materials.
    // Companion to ThemeAmbientController — handles the skybox image swap + ambient mode.
    // Attach to Main.unity (MonoSingleton auto-creates if missing).
    [DefaultExecutionOrder(52)]
    public class SkyboxController : MonoSingleton<SkyboxController>
    {
        [SerializeField] private Material? skyboxPlaine;
        [SerializeField] private Material? skyboxForet;
        [SerializeField] private Material? skyboxDesert;
        [SerializeField] private Material? skyboxVolcan;
        [SerializeField] private Material? skyboxApocalypse;
        [SerializeField] private Material? skyboxEspace;
        [SerializeField] private Material? skyboxSubmarin;
        [SerializeField] private Material? skyboxMedieval;
        [SerializeField] private Material? skyboxCyberpunk;
        [SerializeField] private Material? skyboxFoire;

        private Dictionary<LevelTheme, Material?> _skyboxMap = new();

        protected override void OnAwakeSingleton() => BuildMap();

        private void OnEnable()  => LevelEvents.OnLevelStart += HandleLevelStart;
        private void OnDisable() => LevelEvents.OnLevelStart -= HandleLevelStart;

        private void BuildMap()
        {
            _skyboxMap = new Dictionary<LevelTheme, Material?>
            {
                { LevelTheme.Plaine,     skyboxPlaine     },
                { LevelTheme.Foret,      skyboxForet      },
                { LevelTheme.Desert,     skyboxDesert     },
                { LevelTheme.Volcan,     skyboxVolcan     },
                { LevelTheme.Apocalypse, skyboxApocalypse },
                { LevelTheme.Espace,     skyboxEspace     },
                { LevelTheme.Submarin,   skyboxSubmarin   },
                { LevelTheme.Medieval,   skyboxMedieval   },
                { LevelTheme.Cyberpunk,  skyboxCyberpunk  },
                { LevelTheme.Foire,      skyboxFoire      },
            };
        }

        private void HandleLevelStart(LevelData level, Bounds _) => ApplyTheme(level.LevelTheme);

        public void ApplyTheme(LevelTheme theme)
        {
            if (_skyboxMap.Count == 0) BuildMap();

            var mat = _skyboxMap.TryGetValue(theme, out var m) ? m : null;

            if (mat != null)
            {
                RenderSettings.skybox = mat;
            }
            else
            {
                // Placeholder: keep current skybox if assigned, else leave as-is.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[SkyboxController] No material assigned for theme {theme} — using existing skybox.");
#endif
            }

            // Switch ambient to derive lighting from the skybox image.
            RenderSettings.ambientMode = AmbientMode.Skybox;

            // Trigger GI re-bake from new skybox so ambient + reflection match.
            DynamicGI.UpdateEnvironment();

            // If a reflection probe with Realtime mode is present, request a refresh.
            var probe = Object.FindFirstObjectByType<ReflectionProbe>();
            if (probe != null && probe.mode == ReflectionProbeMode.Realtime)
                probe.RenderProbe();
        }
    }
}
