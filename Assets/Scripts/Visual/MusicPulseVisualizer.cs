#nullable enable
using CrowdDefense.Common;
using CrowdDefense.Systems;
using CrowdDefense.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CrowdDefense.Visual
{
    [RequireComponent(typeof(RectTransform))]
    public class MusicPulseVisualizer : MonoSingleton<MusicPulseVisualizer>
    {
        private const int SampleCount = 256;
        private const float PulseAmplitude = 0.5f;
        private const float SmoothSpeed = 12f;

        [SerializeField] private float baseSize = 60f;

        private readonly float[] _samples = new float[SampleCount];
        private RectTransform? _rect;
        private CanvasGroup? _group;
        private float _smoothRms;

        protected override void OnAwakeSingleton()
        {
            _rect = GetComponent<RectTransform>();
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

            if (_rect != null)
            {
                _rect.anchorMin = Vector2.zero;
                _rect.anchorMax = Vector2.zero;
                _rect.pivot = Vector2.zero;
                _rect.anchoredPosition = new Vector2(16f, 16f);
                _rect.sizeDelta = new Vector2(baseSize, baseSize);
            }

            // Ensure there is an Image component for the ring visual
            if (GetComponent<Image>() == null)
            {
                var img = gameObject.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.55f);
            }
        }

        private void Update()
        {
            bool enabled = SettingsRegistry.Instance?.MusicPulseEnabled ?? false;

            if (_group != null)
                _group.alpha = enabled ? 1f : 0f;

            if (!enabled) return;

            var src = MusicManager.Instance?.ActiveAudioSource;
            float rms = 0f;
            if (src != null && src.isPlaying)
            {
                src.GetOutputData(_samples, 0);
                float sum = 0f;
                for (int i = 0; i < _samples.Length; i++)
                    sum += _samples[i] * _samples[i];
                rms = Mathf.Sqrt(sum / _samples.Length);
            }

            _smoothRms = Mathf.Lerp(_smoothRms, rms, Time.unscaledDeltaTime * SmoothSpeed);

            if (_rect != null)
            {
                float size = baseSize * (1f + _smoothRms * PulseAmplitude);
                _rect.sizeDelta = new Vector2(size, size);
            }
        }
    }
}
