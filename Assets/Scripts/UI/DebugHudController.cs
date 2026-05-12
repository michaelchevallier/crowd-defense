#nullable enable
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Shown when Application.absoluteURL contains "debug=1", in debug builds, or on F3 toggle.
    // Displays FPS, GC memory, entity counts, wave state, 60-frame spike graph.
    // Port of src-v3/ui/TickMetrics.js.
    public class DebugHudController : UIControllerBase
    {
        private const int   HISTORY  = 60;
        private const float INTERVAL = 0.25f;   // text refresh 4x per second

        private VisualElement? _panel;
        private Label?         _lblFps;
        private Label?         _lblEntities;
        private Label?         _lblWave;
        private Label?         _lblMemory;
        private Label?         _lblGraph;

        // 60-frame rolling delta-time history for spike graph
        private readonly float[] _history    = new float[HISTORY];
        private int   _histIdx;
        private int   _histCount;

        // FPS rolling average (30 batches of INTERVAL duration)
        private readonly float[] _fpsSamples = new float[30];
        private int   _fpsIdx;
        private float _fpsAccum;
        private int   _fpsBatch;
        private float _timer;

        private bool _active;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            ResolveUI();
        }

        private void Start()
        {
            _active = IsDebugEnabled();

            if (Root == null) return;
            _panel = Root.Q<VisualElement>("debug-hud") ?? BuildPanel(Root);

            _lblFps      = _panel?.Q<Label>("dbg-fps");
            _lblEntities = _panel?.Q<Label>("dbg-entities");
            _lblWave     = _panel?.Q<Label>("dbg-wave");
            _lblMemory   = _panel?.Q<Label>("dbg-memory");
            _lblGraph    = _panel.Q<Label>("dbg-graph");

            ApplyVisibility();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyBindings.GetKey("debug")))
            {
                _active = !_active;
                ApplyVisibility();
            }

            float dt = Time.unscaledDeltaTime;

            // Always record tick so history is live when panel is toggled on
            _history[_histIdx % HISTORY] = dt;
            _histIdx++;
            _histCount = Mathf.Min(_histCount + 1, HISTORY);

            if (!_active) return;

            _fpsAccum += dt;
            _fpsBatch++;
            _timer    += dt;

            if (_timer >= INTERVAL)
            {
                _timer = 0f;
                Refresh();
                _fpsAccum = 0f;
                _fpsBatch = 0;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void ApplyVisibility() =>
            _panel!.style.display = _active ? DisplayStyle.Flex : DisplayStyle.None;

        private static bool IsDebugEnabled()
        {
#if UNITY_EDITOR
            return true;
#elif UNITY_WEBGL
            try { return Application.absoluteURL.Contains("debug=1"); }
            catch { return false; }
#else
            return Debug.isDebugBuild;
#endif
        }

        private void Refresh()
        {
            // Store batch average into FPS rolling buffer
            float batchDt = _fpsBatch > 0 ? _fpsAccum / _fpsBatch : Time.unscaledDeltaTime;
            _fpsSamples[_fpsIdx % _fpsSamples.Length] = batchDt;
            _fpsIdx++;
            int n = Mathf.Min(_fpsIdx, _fpsSamples.Length);
            float avgDt = 0f;
            for (int i = 0; i < n; i++) avgDt += _fpsSamples[i];
            avgDt /= n;
            float fps = avgDt > 0f ? 1f / avgDt : 0f;

            if (_lblFps != null)
                _lblFps.text = $"FPS {fps:F0}  dt {avgDt * 1000f:F1}ms";

            // Entity counts
            int enemies     = WaveManager.Instance?.ActiveEnemies.Count ?? 0;
            int towers      = PlacementController.Instance?.PlacedTowers.Count ?? 0;
            int projectiles = ProjectilePool.Instance?.ActiveCount ?? 0;
            if (_lblEntities != null)
                _lblEntities.text = $"Enemies {enemies}  Towers {towers}  Proj {projectiles}";

            // Wave + game state + gold
            int waveIdx  = WaveManager.Instance?.CurrentWaveIdx ?? 0;
            int total    = WaveManager.Instance?.TotalWaves ?? 0;
            string state = LevelRunner.Instance?.State.ToString() ?? "-";
            int gold     = Economy.Instance?.Gold ?? 0;
            if (_lblWave != null)
                _lblWave.text = $"Wave {waveIdx + 1}/{total}  [{state}]  G:{gold}";

            // GC memory + frame counter
            float memMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
            if (_lblMemory != null)
                _lblMemory.text = $"Mem {memMB:F1} MB  Frame #{Time.frameCount}";

            if (_lblGraph != null)
                _lblGraph.text = BuildGraph();
        }

        // 60-char ASCII bar: _=<16ms  -=16-33ms  +=33-50ms  #=>50ms
        private string BuildGraph()
        {
            int count = Mathf.Min(_histCount, HISTORY);
            if (count == 0) return string.Empty;

            var sb    = new StringBuilder(count);
            int start = _histIdx - count;
            for (int i = 0; i < count; i++)
            {
                float ms = _history[(start + i) % HISTORY] * 1000f;
                sb.Append(ms < 16f ? '_' : ms < 33f ? '-' : ms < 50f ? '+' : '#');
            }
            return sb.ToString();
        }

        // ── Dynamic panel (no UXML needed) ───────────────────────────────────

        private static VisualElement BuildPanel(VisualElement root)
        {
            var panel = new VisualElement { name = "debug-hud" };
            panel.style.position        = Position.Absolute;
            panel.style.right           = 8;
            panel.style.top             = 8;
            panel.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.70f));
            panel.style.paddingLeft     = 8;
            panel.style.paddingRight    = 8;
            panel.style.paddingTop      = 6;
            panel.style.paddingBottom   = 6;
            panel.style.borderTopLeftRadius     = 6;
            panel.style.borderTopRightRadius    = 6;
            panel.style.borderBottomLeftRadius  = 6;
            panel.style.borderBottomRightRadius = 6;
            panel.style.minWidth = 270;

            (string id, bool isGraph)[] rows =
            {
                ("dbg-fps",      false),
                ("dbg-entities", false),
                ("dbg-wave",     false),
                ("dbg-memory",   false),
                ("dbg-graph",    true),
            };

            foreach (var (id, isGraph) in rows)
            {
                var lbl = new Label { name = id, text = "" };
                lbl.style.color                   = new StyleColor(isGraph
                    ? new Color(1f, 0.85f, 0.3f)
                    : new Color(0.55f, 1f, 0.55f));
                lbl.style.fontSize                = isGraph ? 9 : 10;
                lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                panel.Add(lbl);
            }

            root.Add(panel);
            return panel;
        }
    }
}
