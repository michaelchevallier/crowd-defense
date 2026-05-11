#nullable enable
using UnityEngine;
using UnityEngine.Rendering;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.Visual
{
    // Applies per-theme sun rotation, ambient light color and skybox tint
    // at LevelStart. Attach to Main.unity (auto-created if missing via MonoSingleton).
    [DefaultExecutionOrder(51)]
    public class ThemeAmbientController : MonoSingleton<ThemeAmbientController>
    {
        // Cached reference to the scene directional light (named "Sun" or first found).
        private Light? _sun;

        protected override void OnAwakeSingleton() => CacheSun();

        private void OnEnable()  => LevelEvents.OnLevelStart += HandleLevelStart;
        private void OnDisable() => LevelEvents.OnLevelStart -= HandleLevelStart;

        private void CacheSun()
        {
            // Prefer the GameObject named "Sun"; fall back to first Directional in scene.
            var sunGo = GameObject.Find("Sun");
            if (sunGo != null)
                _sun = sunGo.GetComponent<Light>();

            if (_sun == null)
            {
                var all = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var l in all)
                {
                    if (l.type == LightType.Directional) { _sun = l; break; }
                }
            }
        }

        private void HandleLevelStart(LevelData level, Bounds _)
        {
            if (_sun == null) CacheSun();
            ApplyTheme(level.LevelTheme);
        }

        public void ApplyTheme(LevelTheme theme)
        {
            ApplySun(theme);
            ApplyAmbient(theme);
            ApplySkyboxTint(theme);
            DynamicGI.UpdateEnvironment();
        }

        // ── Sun ─────────────────────────────────────────────────────────────────

        private void ApplySun(LevelTheme theme)
        {
            if (_sun == null) return;
            var (rot, color, intensity) = SunParams(theme);
            _sun.transform.rotation = Quaternion.Euler(rot);
            _sun.color = color;
            _sun.intensity = intensity;
        }

        // Returns (eulerAngles, color, intensity) per theme.
        private static (Vector3 rot, Color color, float intensity) SunParams(LevelTheme theme) => theme switch
        {
            LevelTheme.Espace     => (new Vector3(85f,  -30f, 0f), new Color(0.6f, 0.6f,  1.0f), 0.6f),  // quasi-vertical, bleuté
            LevelTheme.Volcan     => (new Vector3(60f,  -20f, 0f), new Color(1.0f, 0.45f, 0.1f), 1.8f),  // rouge-orange vif
            LevelTheme.Foret      => (new Vector3(45f,  -30f, 0f), new Color(0.8f, 0.95f, 0.6f), 1.2f),  // lumière tamisée verte
            LevelTheme.Desert     => (new Vector3(70f,   10f, 0f), new Color(1.0f, 0.92f, 0.7f), 2.0f),  // zénith, très chaud
            LevelTheme.Apocalypse => (new Vector3(25f,  -45f, 0f), new Color(0.7f, 0.55f, 0.4f), 0.8f),  // rasant, brun cendres
            LevelTheme.Submarin   => (new Vector3(55f,  -30f, 0f), new Color(0.3f, 0.8f,  1.0f), 0.7f),  // bleu-vert filtré
            LevelTheme.Medieval   => (new Vector3(50f,  -30f, 0f), new Color(1.0f, 0.95f, 0.8f), 1.4f),  // lumière chaude classique
            LevelTheme.Cyberpunk  => (new Vector3(30f,  -60f, 0f), new Color(0.6f, 0.3f,  1.0f), 0.5f),  // neon violet rasant
            LevelTheme.Foire      => (new Vector3(65f,    0f, 0f), new Color(1.0f, 0.98f, 0.7f), 1.6f),  // midi festif
            _                     => (new Vector3(50f,  -30f, 0f), new Color(1.0f, 0.95f, 0.9f), 1.5f),  // Plaine — défaut
        };

        // ── Ambient ─────────────────────────────────────────────────────────────

        private static void ApplyAmbient(LevelTheme theme)
        {
            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = AmbientColor(theme);
        }

        private static Color AmbientColor(LevelTheme theme) => theme switch
        {
            LevelTheme.Foret      => new Color(0.45f, 0.60f, 0.38f),
            LevelTheme.Desert     => new Color(0.80f, 0.68f, 0.42f),
            LevelTheme.Volcan     => new Color(0.70f, 0.28f, 0.10f),
            LevelTheme.Apocalypse => new Color(0.35f, 0.28f, 0.25f),
            LevelTheme.Espace     => new Color(0.10f, 0.10f, 0.25f),
            LevelTheme.Submarin   => new Color(0.10f, 0.35f, 0.55f),
            LevelTheme.Medieval   => new Color(0.52f, 0.48f, 0.38f),
            LevelTheme.Cyberpunk  => new Color(0.15f, 0.08f, 0.35f),
            LevelTheme.Foire      => new Color(0.72f, 0.55f, 0.20f),
            _                     => new Color(0.55f, 0.62f, 0.70f),
        };

        // ── Skybox tint ─────────────────────────────────────────────────────────

        private static void ApplySkyboxTint(LevelTheme theme)
        {
            var mat = RenderSettings.skybox;
            if (mat == null) return;
            if (!mat.HasProperty("_SkyTint")) return;
            mat.SetColor("_SkyTint", SkyTint(theme));
        }

        private static Color SkyTint(LevelTheme theme) => theme switch
        {
            LevelTheme.Espace     => new Color(0.02f, 0.02f, 0.05f),  // nuit quasi noire
            LevelTheme.Volcan     => new Color(0.25f, 0.08f, 0.02f),  // ciel rouge fumée
            LevelTheme.Foret      => new Color(0.30f, 0.55f, 0.30f),  // vert canopée
            LevelTheme.Desert     => new Color(0.85f, 0.72f, 0.45f),  // ocre chaud
            LevelTheme.Apocalypse => new Color(0.20f, 0.15f, 0.10f),  // brun cendres
            LevelTheme.Submarin   => new Color(0.05f, 0.20f, 0.45f),  // bleu profond
            LevelTheme.Medieval   => new Color(0.50f, 0.65f, 0.80f),  // ciel médiéval clair
            LevelTheme.Cyberpunk  => new Color(0.05f, 0.02f, 0.15f),  // nuit violette
            LevelTheme.Foire      => new Color(0.70f, 0.85f, 1.00f),  // ciel bleu clair festif
            _                     => new Color(0.50f, 0.70f, 1.00f),  // Plaine — bleu standard
        };
    }
}
