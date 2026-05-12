#nullable enable
using System.Collections.Generic;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using UnityEngine;

namespace CrowdDefense.Visual
{
    // Water + lava tile 8-frame colour animation (8 fps = 125 ms/frame).
    // No frame textures required — uses MaterialPropertyBlock colour palettes as
    // placeholder animation until water_01..08 / lava_01..08 PNGs are imported.
    // Lava tiles also receive a sine-wave emissive pulse (0.5–2.0 intensity).
    // Attach to any persistent GameObject in the scene (e.g. MapRenderer parent).
    [DefaultExecutionOrder(60)]   // after MapRenderer (50) + PathTilesController (55)
    public class WaterLavaAnimController : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------

        public static WaterLavaAnimController? Instance { get; private set; }

        // -------------------------------------------------------------------------
        // 8-frame colour palettes (placeholder — swap to texture frames later)
        // -------------------------------------------------------------------------

        // Water: gentle oscillation from deep blue → cyan-teal
        private static readonly Color[] _waterFrames =
        {
            new Color(0.15f, 0.35f, 0.75f),
            new Color(0.18f, 0.40f, 0.80f),
            new Color(0.20f, 0.48f, 0.82f),
            new Color(0.22f, 0.55f, 0.78f),
            new Color(0.20f, 0.52f, 0.72f),
            new Color(0.18f, 0.45f, 0.76f),
            new Color(0.16f, 0.38f, 0.78f),
            new Color(0.15f, 0.35f, 0.75f),
        };

        // Lava: orange-red flicker cycle
        private static readonly Color[] _lavaFrames =
        {
            new Color(0.95f, 0.30f, 0.05f),
            new Color(1.00f, 0.40f, 0.08f),
            new Color(0.98f, 0.50f, 0.10f),
            new Color(0.90f, 0.35f, 0.06f),
            new Color(0.85f, 0.28f, 0.04f),
            new Color(0.92f, 0.32f, 0.07f),
            new Color(0.98f, 0.42f, 0.09f),
            new Color(0.95f, 0.30f, 0.05f),
        };

        // Lava emissive colour (orange glow, intensity modulated by sine)
        private static readonly Color _lavaEmissiveBase = new Color(1.0f, 0.25f, 0.05f);
        private const float LavaEmissiveMin = 0.5f;
        private const float LavaEmissiveMax = 2.0f;
        private const float LavaEmissivePeriod = 1.6f; // seconds per cycle

        // -------------------------------------------------------------------------
        // Runtime state
        // -------------------------------------------------------------------------

        private readonly List<MeshRenderer> _waterRenderers = new();
        private readonly List<MeshRenderer> _lavaRenderers  = new();
        private MaterialPropertyBlock? _mpb;

        private const float FrameInterval = 1f / 8f;  // 125 ms
        private float _frameTimer;
        private int  _frameIndex;

        private bool _active;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            LevelEvents.OnLevelStart += OnLevelStart;
            LevelEvents.OnLevelEnd   += OnLevelEnd;
        }

        private void OnDisable()
        {
            LevelEvents.OnLevelStart -= OnLevelStart;
            LevelEvents.OnLevelEnd   -= OnLevelEnd;
        }

        private void Update()
        {
            if (!_active) return;

            _frameTimer += Time.deltaTime;
            if (_frameTimer >= FrameInterval)
            {
                _frameTimer -= FrameInterval;
                _frameIndex = (_frameIndex + 1) & 7; // modulo 8 via bitmask
                ApplyWaterFrame(_frameIndex);
            }

            // Lava emissive pulse runs at continuous sine rate (independent of frame clock)
            ApplyLavaEmissive(Time.time);
        }

        // -------------------------------------------------------------------------
        // Public API — MapRenderer registers slabs after spawn
        // -------------------------------------------------------------------------

        public void RegisterWater(MeshRenderer mr) => _waterRenderers.Add(mr);
        public void RegisterLava(MeshRenderer mr)  => _lavaRenderers.Add(mr);

        // -------------------------------------------------------------------------
        // Event handlers
        // -------------------------------------------------------------------------

        private void OnLevelStart(LevelData data, Bounds bounds)
        {
            // Attempt to collect any water/lava renderers not yet registered via
            // RegisterWater/RegisterLava (fallback scan of MapRenderer children).
            if (_waterRenderers.Count == 0 && _lavaRenderers.Count == 0)
                ScanMapRendererChildren();

            _frameTimer = 0f;
            _frameIndex = 0;
            _active = _waterRenderers.Count > 0 || _lavaRenderers.Count > 0;

#if UNITY_EDITOR
            Debug.Log($"[WaterLavaAnim] active={_active} water={_waterRenderers.Count} lava={_lavaRenderers.Count}");
#endif
        }

        private void OnLevelEnd()
        {
            _active = false;
            _waterRenderers.Clear();
            _lavaRenderers.Clear();
        }

        // -------------------------------------------------------------------------
        // Fallback scan — walks MapRenderer hierarchy when registration was missed
        // -------------------------------------------------------------------------

        private void ScanMapRendererChildren()
        {
            var mapRenderer = FindFirstObjectByType<MapRenderer>();
            if (mapRenderer == null) return;

            var grid = PathManager.Instance?.Grid;
            if (grid == null) return;

            // MapRenderer names slabs "Cell_c_r" and stores them as direct children.
            foreach (Transform child in mapRenderer.transform)
            {
                var mr = child.GetComponent<MeshRenderer>();
                if (mr == null) continue;

                // Parse "Cell_c_r" to get grid coords
                if (!TryParseCellName(child.name, out int col, out int row)) continue;
                if (col < 0 || col >= grid.Width || row < 0 || row >= grid.Height) continue;

                char ch = grid.At(col, row);
                if (ch == GridCoords.WATER) _waterRenderers.Add(mr);
                else if (ch == GridCoords.LAVA) _lavaRenderers.Add(mr);
            }
        }

        private static bool TryParseCellName(string name, out int col, out int row)
        {
            col = 0; row = 0;
            // Expected format: "Cell_<col>_<row>"
            if (!name.StartsWith("Cell_")) return false;
            var parts = name.Split('_');
            if (parts.Length < 3) return false;
            return int.TryParse(parts[1], out col) && int.TryParse(parts[2], out row);
        }

        // -------------------------------------------------------------------------
        // Animation helpers
        // -------------------------------------------------------------------------

        private void ApplyWaterFrame(int frame)
        {
            var color = _waterFrames[frame & 7];
            foreach (var mr in _waterRenderers)
            {
                if (mr == null) continue;
                _mpb ??= new MaterialPropertyBlock();
                mr.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", color);
                mr.SetPropertyBlock(_mpb);
            }
        }

        private void ApplyLavaEmissive(float time)
        {
            if (_lavaRenderers.Count == 0) return;

            float t = (Mathf.Sin(time * (Mathf.PI * 2f / LavaEmissivePeriod)) + 1f) * 0.5f;
            float intensity = Mathf.Lerp(LavaEmissiveMin, LavaEmissiveMax, t);

            int frame = _frameIndex & 7;
            var baseColor = _lavaFrames[frame];
            var emissive  = _lavaEmissiveBase * intensity;

            foreach (var mr in _lavaRenderers)
            {
                if (mr == null) continue;
                _mpb ??= new MaterialPropertyBlock();
                mr.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", baseColor);
                _mpb.SetColor("_EmissionColor", emissive);
                mr.SetPropertyBlock(_mpb);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
