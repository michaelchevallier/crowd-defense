#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class RuntimeProfilePanel : UIControllerBase
    {
        private Label? _label;
        private Label? _spawnLabel;
        private bool _visible;
        private bool _spawnVisible;
        private float _fpsAccum;
        private int _frameCount;
        private float _avgFps;

        private void Start()
        {
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            _label = Root?.Q<Label>("profile-overlay");
            _spawnLabel = Root?.Q<Label>("spawn-overlay");

#if UNITY_WEBGL && !UNITY_EDITOR
            var url = Application.absoluteURL;
            if (url.Contains("debug=1"))
                SetVisible(true);
#endif
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
                SetVisible(!_visible);

            if (Input.GetKeyDown(KeyCode.F4))
                SetSpawnVisible(!_spawnVisible);

            _fpsAccum += Time.unscaledDeltaTime;
            _frameCount++;
            if (_fpsAccum >= 0.5f)
            {
                _avgFps = _frameCount / _fpsAccum;
                _fpsAccum = 0f;
                _frameCount = 0;
            }

            if (_visible && _label != null)
            {
                int enemies = WaveManager.Instance?.ActiveEnemies.Count ?? 0;
                int towers = FindObjectsByType<Tower>(FindObjectsInactive.Exclude).Length;
                long memBytes = GC.GetTotalMemory(false);
                int memMb = (int)(memBytes / 1024 / 1024);
                _label.text = $"FPS {_avgFps:F0} | E:{enemies} T:{towers} | Mem {memMb}MB";
            }

            if (_spawnVisible && _spawnLabel != null)
            {
                var wm = WaveManager.Instance;
                if (wm != null)
                {
                    int wave = wm.WaveDisplayNumber;
                    int total = wm.TotalWaves;
                    int queued = wm.PendingSpawnCount;
                    int alive = wm.ActiveEnemies.Count;
                    float intervalMs = wm.SpawnIntervalMs;
                    float timerMs = wm.SpawnTimerMs;
                    float ttns = wm.IsWaveActive && queued > 0
                        ? (intervalMs - timerMs) / 1000f
                        : -1f;
                    string state = wm.IsWaveActive ? "ACTIVE" : (wm.IsWaitingForPlayerStart ? "BREAK" : "IDLE");
                    string ttnStr = ttns >= 0f ? $"{ttns:F2}s" : "--";
                    _spawnLabel.text = $"[SPAWN] W{wave}/{total} {state} | Q:{queued} A:{alive} | TTN:{ttnStr}";
                }
                else
                {
                    _spawnLabel.text = "[SPAWN] WaveManager not found";
                }
            }
        }

        private void SetVisible(bool show)
        {
            _visible = show;
            if (_label == null) return;
            if (show) _label.RemoveFromClassList("hidden");
            else _label.AddToClassList("hidden");
        }

        private void SetSpawnVisible(bool show)
        {
            _spawnVisible = show;
            if (_spawnLabel == null) return;
            if (show) _spawnLabel.RemoveFromClassList("hidden");
            else _spawnLabel.AddToClassList("hidden");
        }
    }
}
