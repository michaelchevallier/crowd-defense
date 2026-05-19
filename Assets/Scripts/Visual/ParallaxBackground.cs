#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;

namespace CrowdDefense.Visual
{
    // Spawns 3 large quad layers (far/mid/near) that slide with the camera at
    // fractional speed to simulate parallax depth.
    // Attach to ParallaxBackgroundGO and call Init(theme) from LevelVisualBridge.
    [DefaultExecutionOrder(60)]
    public sealed class ParallaxBackground : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [SerializeField] private float layerY   = -1f;   // world Y for quad placement
        [SerializeField] private float quadSize = 500f;  // large enough to fill any viewport

        // ── Per-theme sky / ground top-bottom colors ───────────────────────────
        private struct ThemeColorPair
        {
            public Color sky;
            public Color ground;
            public ThemeColorPair(Color s, Color g) { sky = s; ground = g; }
        }

        private static readonly Dictionary<LevelTheme, ThemeColorPair> ThemeColors = new()
        {
            { LevelTheme.Plaine,     new ThemeColorPair(new Color(0.45f, 0.75f, 1.00f), new Color(0.60f, 0.85f, 0.50f)) },
            { LevelTheme.Foret,      new ThemeColorPair(new Color(0.20f, 0.50f, 0.20f), new Color(0.35f, 0.65f, 0.25f)) },
            { LevelTheme.Desert,     new ThemeColorPair(new Color(0.90f, 0.70f, 0.30f), new Color(0.95f, 0.85f, 0.55f)) },
            { LevelTheme.Volcan,     new ThemeColorPair(new Color(0.25f, 0.05f, 0.00f), new Color(0.80f, 0.25f, 0.05f)) },
            { LevelTheme.Apocalypse, new ThemeColorPair(new Color(0.30f, 0.15f, 0.05f), new Color(0.55f, 0.30f, 0.10f)) },
            { LevelTheme.Espace,     new ThemeColorPair(new Color(0.02f, 0.02f, 0.10f), new Color(0.08f, 0.08f, 0.25f)) },
            { LevelTheme.Submarin,   new ThemeColorPair(new Color(0.00f, 0.20f, 0.50f), new Color(0.05f, 0.40f, 0.70f)) },
            { LevelTheme.Medieval,   new ThemeColorPair(new Color(0.40f, 0.55f, 0.80f), new Color(0.55f, 0.70f, 0.55f)) },
            { LevelTheme.Cyberpunk,  new ThemeColorPair(new Color(0.05f, 0.00f, 0.15f), new Color(0.50f, 0.00f, 0.80f)) },
            { LevelTheme.Foire,      new ThemeColorPair(new Color(0.90f, 0.60f, 0.90f), new Color(1.00f, 0.85f, 0.40f)) },
        };

        private struct Layer
        {
            public Transform quad;
            public float     speedFactor;
        }

        private struct LayerConfig
        {
            public float speed;
            public float zOff;
            public float scale;
            public LayerConfig(float s, float z, float sc) { speed = s; zOff = z; scale = sc; }
        }

        private Layer[]   _layers = System.Array.Empty<Layer>();
        private Vector3   _prevCamPos;
        private Camera?   _cam;

        // ── (speed factor, local Z offset, local scale) ────────────────────────
        private static readonly LayerConfig[] LayerDef =
        {
            new LayerConfig(0.1f, 120f, 1.5f),   // far
            new LayerConfig(0.4f,  80f, 1.2f),   // mid
            new LayerConfig(0.8f,  40f, 1.0f),   // near
        };

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            _cam = Camera.main;
            if (_cam != null) _prevCamPos = _cam.transform.position;
        }

        private void LateUpdate()
        {
            if (_cam == null || _layers.Length == 0) return;

            Vector3 camPos = _cam.transform.position;
            float   dx     = camPos.x - _prevCamPos.x;
            float   dz     = camPos.z - _prevCamPos.z;
            _prevCamPos = camPos;

            if (Mathf.Abs(dx) < 0.0001f && Mathf.Abs(dz) < 0.0001f) return;

            foreach (var layer in _layers)
            {
                var p = layer.quad.position;
                p.x += dx * layer.speedFactor;
                p.z += dz * layer.speedFactor;
                layer.quad.position = p;
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────
        public void Init(LevelTheme theme)
        {
            ClearAll();

            _cam = Camera.main;
            if (_cam != null) _prevCamPos = _cam.transform.position;

            if (!ThemeColors.TryGetValue(theme, out var colors))
                colors = new ThemeColorPair(new Color(0.45f, 0.75f, 1.00f), new Color(0.60f, 0.85f, 0.50f));

            _layers = new Layer[LayerDef.Length];
            for (int i = 0; i < LayerDef.Length; i++)
            {
                float speed = LayerDef[i].speed;
                float zOff  = LayerDef[i].zOff;
                float scale = LayerDef[i].scale;

                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = $"ParallaxLayer_{(i == 0 ? "Far" : i == 1 ? "Mid" : "Near")}";
                go.transform.SetParent(transform, worldPositionStays: false);

                // Flatten horizontal (XZ plane), tilt to face camera-ish
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                go.transform.localScale    = new Vector3(quadSize * scale, quadSize * scale, 1f);

                // Start at camera XZ, shifted back by zOff, layerY height
                Vector3 camStart = _cam != null ? _cam.transform.position : Vector3.zero;
                go.transform.position = new Vector3(camStart.x, layerY, camStart.z + zOff);

                // Destroy auto-collider (quad primitive adds one)
                var col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);

                // Gradient material via vertex colors or plain colored unlit mat
                var mr  = go.GetComponent<MeshRenderer>();
                var mat = BuildGradientMat(colors.sky, colors.ground, i);  // sky/ground from ThemeColorPair
                mr.sharedMaterial = mat;

                // Push far layers to render first (render queue offset)
                mat.renderQueue = 1000 + i;

                _layers[i] = new Layer { quad = go.transform, speedFactor = speed };
            }
        }

        public void ClearAll()
        {
            foreach (var layer in _layers)
                if (layer.quad != null)
                    Object.Destroy(layer.quad.gameObject);
            _layers = System.Array.Empty<Layer>();
        }

        private void OnDestroy() => ClearAll();

        // ── Material helpers ───────────────────────────────────────────────────
        private static Material BuildGradientMat(Color sky, Color ground, int layerIndex)
        {
            // Use built-in Unlit/Color as minimal fallback; blend sky/ground per layer
            float t = layerIndex / (float)(LayerDef.Length - 1);  // 0=far, 1=near
            Color lerped = Color.Lerp(sky, ground, t * 0.5f);

            // Attempt URP Unlit, fall back to Legacy Unlit/Color
            Shader? shader = Shader.Find("Universal Render Pipeline/Unlit")
                          ?? Shader.Find("Unlit/Color");

            var mat = new Material(shader ?? ShaderUtil.GetUnlitShader());
            mat.name = $"ParallaxMat_L{layerIndex}";

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", lerped);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", lerped);

            // Slight alpha fade for far layers
            var c = mat.HasProperty("_BaseColor")
                ? mat.GetColor("_BaseColor")
                : mat.GetColor("_Color");
            c.a = 1f - layerIndex * 0.08f;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            else mat.SetColor("_Color", c);

            return mat;
        }
    }
}
