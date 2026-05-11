#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Shown when Application.absoluteURL contains "debug=1" or when running a debug build.
    // Displays FPS, entity counts, wave info.  Port of src-v3/ui/TickMetrics.js.
    [RequireComponent(typeof(UIDocument))]
    public class DebugHudController : MonoBehaviour
    {
        private VisualElement? _panel;
        private Label? _line1; // FPS + frame time
        private Label? _line2; // enemies / towers
        private Label? _line3; // wave / state
        private Label? _line4; // gold

        // Rolling FPS average over 30 frames
        private readonly float[] _frameTimes = new float[30];
        private int _frameIdx;

        private bool _active;

        private void Start()
        {
            _active = Debug.isDebugBuild ||
#if UNITY_WEBGL && !UNITY_EDITOR
                Application.absoluteURL.Contains("debug=1");
#else
                false;
#endif

#if UNITY_EDITOR
            _active = true;
#endif

            var root = GetComponent<UIDocument>().rootVisualElement;
            _panel = root.Q<VisualElement>("debug-hud");

            if (_panel == null)
            {
                // Panel not present in UXML — create programmatically
                _panel = BuildPanel();
                root.Add(_panel);
            }

            if (!_active)
            {
                _panel.AddToClassList("hidden");
                enabled = false;
                return;
            }

            _line1 = _panel.Q<Label>("dbg-fps");
            _line2 = _panel.Q<Label>("dbg-entities");
            _line3 = _panel.Q<Label>("dbg-wave");
            _line4 = _panel.Q<Label>("dbg-gold");
        }

        private void Update()
        {
            if (!_active) return;

            // Rolling FPS
            _frameTimes[_frameIdx % _frameTimes.Length] = Time.unscaledDeltaTime;
            _frameIdx++;
            float avg = 0f;
            int count = Mathf.Min(_frameIdx, _frameTimes.Length);
            for (int i = 0; i < count; i++) avg += _frameTimes[i];
            avg /= count;
            float fps = avg > 0f ? 1f / avg : 0f;

            if (_line1 != null)
                _line1.text = $"FPS {fps:F0}  ({avg * 1000f:F1}ms)";

            int enemyCount = WaveManager.Instance?.ActiveEnemies.Count ?? 0;
            int towerCount = PlacementController.Instance?.PlacedTowers.Count ?? 0;
            if (_line2 != null)
                _line2.text = $"Enemies {enemyCount}  Towers {towerCount}";

            int waveIdx   = WaveManager.Instance?.CurrentWaveIdx ?? 0;
            int totalWave = WaveManager.Instance?.TotalWaves ?? 0;
            string state  = LevelRunner.Instance?.State.ToString() ?? "—";
            if (_line3 != null)
                _line3.text = $"Wave {waveIdx + 1}/{totalWave}  [{state}]";

            int gold = Economy.Instance?.Gold ?? 0;
            if (_line4 != null)
                _line4.text = $"Gold {gold}";
        }

        // Build the debug panel dynamically when it doesn't exist in the UXML
        private static VisualElement BuildPanel()
        {
            var panel = new VisualElement { name = "debug-hud" };
            panel.style.position = Position.Absolute;
            panel.style.left = 8;
            panel.style.top  = 8;
            panel.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.65f));
            panel.style.paddingLeft   = 8;
            panel.style.paddingRight  = 8;
            panel.style.paddingTop    = 6;
            panel.style.paddingBottom = 6;
            panel.style.borderTopLeftRadius     = 6;
            panel.style.borderTopRightRadius    = 6;
            panel.style.borderBottomLeftRadius  = 6;
            panel.style.borderBottomRightRadius = 6;

            string[] names = { "dbg-fps", "dbg-entities", "dbg-wave", "dbg-gold" };
            foreach (var name in names)
            {
                var lbl = new Label { name = name, text = "" };
                lbl.style.color    = new StyleColor(new Color(0.6f, 1f, 0.6f));
                lbl.style.fontSize = 11;
                panel.Add(lbl);
            }

            return panel;
        }
    }
}
