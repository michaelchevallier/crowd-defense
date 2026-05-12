#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class RuntimeProfilePanel : MonoBehaviour
    {
        private Label? _label;
        private bool _visible;
        private float _fpsAccum;
        private int _frameCount;
        private float _avgFps;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _label = root.Q<Label>("profile-overlay");

#if UNITY_WEBGL && !UNITY_EDITOR
            // Check URL param ?debug=1
            var url = Application.absoluteURL;
            if (url.Contains("debug=1"))
                SetVisible(true);
#endif
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
                SetVisible(!_visible);

            _fpsAccum += Time.unscaledDeltaTime;
            _frameCount++;
            if (_fpsAccum >= 0.5f)
            {
                _avgFps = _frameCount / _fpsAccum;
                _fpsAccum = 0f;
                _frameCount = 0;
            }

            if (!_visible || _label == null) return;

            int enemies = WaveManager.Instance?.ActiveEnemies.Count ?? 0;
            int towers = FindObjectsByType<Tower>(FindObjectsSortMode.None).Length;
            long memBytes = GC.GetTotalMemory(false);
            int memMb = (int)(memBytes / 1024 / 1024);
            _label.text = $"FPS {_avgFps:F0} | E:{enemies} T:{towers} | Mem {memMb}MB";
        }

        private void SetVisible(bool show)
        {
            _visible = show;
            if (_label == null) return;
            if (show) _label.RemoveFromClassList("hidden");
            else _label.AddToClassList("hidden");
        }
    }
}
