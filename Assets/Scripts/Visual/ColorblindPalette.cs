#nullable enable
using CrowdDefense.Common;
using CrowdDefense.UI;
using UnityEngine;

namespace CrowdDefense.Visual
{
    // Deuteranopia palette swap : red-ish → orange-yellow, green-ish → blue-green.
    // Applied via MaterialPropertyBlock so no material instances are leaked.
    // Subscribes to SettingsRegistry.OnSettingsChanged and re-applies on scene change.
    public class ColorblindPalette : MonoSingleton<ColorblindPalette>
    {
        // Hue ranges that define "red" and "green" for remapping (HSV hue 0-1)
        private const float RedHueCenter   = 0f;    // hue ~0 = red
        private const float RedHueRange    = 0.08f; // ±8% → catches red/crimson/maroon
        private const float GreenHueCenter = 0.33f; // hue ~0.33 = green
        private const float GreenHueRange  = 0.10f; // ±10% → catches lime/olive/teal-green

        // Replacement hues (Deuteranopia-friendly)
        private const float OrangeYellowHue = 0.10f; // orange-yellow
        private const float BlueCyanHue     = 0.50f; // blue-green / cyan

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId     = Shader.PropertyToID("_Color");

        protected override void OnAwakeSingleton()
        {
            if (SettingsRegistry.Instance != null)
                SettingsRegistry.Instance.OnSettingsChanged += OnSettingsChanged;
        }

        private void Start()
        {
            // Apply on first frame so state matches PlayerPrefs on scene load
            ApplyCurrentState();
        }

        private void OnEnable()
        {
            if (SettingsRegistry.Instance != null)
                SettingsRegistry.Instance.OnSettingsChanged += OnSettingsChanged;
        }

        private void OnDisable()
        {
            if (SettingsRegistry.Instance != null)
                SettingsRegistry.Instance.OnSettingsChanged -= OnSettingsChanged;
        }

        private void OnSettingsChanged() => ApplyCurrentState();

        private void ApplyCurrentState()
        {
            bool enabled = SettingsRegistry.Instance?.ColorblindMode ?? false;
            if (enabled)
                ApplyToScene();
            else
                RevertScene();
        }

        // Traverse every Renderer in scene, remap red/green via MPB.
        private static void ApplyToScene()
        {
            var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            var mpb = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(mpb);
                Color col = GetRendererBaseColor(r, mpb);
                Color remapped = RemapColor(col);
                if (remapped == col) continue;
                mpb.SetColor(BaseColorId, remapped);
                r.SetPropertyBlock(mpb);
            }
        }

        // Clear MPB overrides set by this system to restore original colors.
        private static void RevertScene()
        {
            var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            var mpb = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(mpb);
                // Only clear if we had set BaseColor (non-empty MPB that contains it)
                if (!mpb.isEmpty)
                {
                    mpb.Clear();
                    r.SetPropertyBlock(mpb);
                }
            }
        }

        private static Color GetRendererBaseColor(Renderer r, MaterialPropertyBlock mpb)
        {
            // Prefer MPB override, then shared material property
            if (!mpb.isEmpty)
            {
                Color c = mpb.GetColor(BaseColorId);
                if (c != default) return c;
            }
            var mat = r.sharedMaterial;
            if (mat == null) return Color.white;
            if (mat.HasProperty(BaseColorId)) return mat.GetColor(BaseColorId);
            if (mat.HasProperty(ColorId))     return mat.GetColor(ColorId);
            return Color.white;
        }

        // Remap: red-ish → orange-yellow, green-ish → blue-green. Other hues unchanged.
        internal static Color RemapColor(Color col)
        {
            Color.RGBToHSV(col, out float h, out float s, out float v);
            if (s < 0.15f) return col; // near-white/grey/black — skip

            float newHue = h;
            float delta = Mathf.DeltaAngle(h * 360f, RedHueCenter * 360f);
            if (Mathf.Abs(delta) <= RedHueRange * 360f)
            {
                newHue = OrangeYellowHue;
            }
            else
            {
                delta = Mathf.DeltaAngle(h * 360f, GreenHueCenter * 360f);
                if (Mathf.Abs(delta) <= GreenHueRange * 360f)
                    newHue = BlueCyanHue;
            }

            if (Mathf.Approximately(newHue, h)) return col;
            Color remapped = Color.HSVToRGB(newHue, s, v);
            remapped.a = col.a;
            return remapped;
        }

        // Called by Tower/Enemy Init after materials are applied so new entities respect the mode.
        public static void ApplyToGameObject(GameObject go)
        {
            if (Instance == null || !(SettingsRegistry.Instance?.ColorblindMode ?? false)) return;
            var mpb = new MaterialPropertyBlock();
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(mpb);
                Color col = GetRendererBaseColor(r, mpb);
                Color remapped = RemapColor(col);
                if (remapped == col) continue;
                mpb.SetColor(BaseColorId, remapped);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
