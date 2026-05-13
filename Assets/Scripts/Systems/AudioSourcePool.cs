#nullable enable
using UnityEngine;
using UnityEngine.Audio;

namespace CrowdDefense.Systems
{
    public sealed class AudioSourcePool : MonoBehaviour
    {
        public static AudioSourcePool? Instance { get; private set; }

        [SerializeField] private int _poolSize = 8;
        [SerializeField] private AudioMixerGroup? _sfx2DGroup;
        [SerializeField] private AudioMixerGroup? _sfx3DGroup;

        private AudioSource[] _pool = System.Array.Empty<AudioSource>();
        private int _next;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _pool = new AudioSource[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                var go = new GameObject($"PooledAudioSource_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _pool[i] = src;
            }
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Play2D(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            var src = NextSource();
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume);
            src.spatialBlend = 0f;
            src.outputAudioMixerGroup = _sfx2DGroup;
            src.transform.position = Vector3.zero;
            src.Play();
        }

        public void Play3D(AudioClip clip, Vector3 worldPos, float volume = 1f, float minDistance = 1f, float maxDistance = 30f)
        {
            if (clip == null) return;
            var src = NextSource();
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume);
            src.spatialBlend = 1f;
            src.outputAudioMixerGroup = _sfx3DGroup;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.transform.position = worldPos;
            src.Play();
        }

        private AudioSource NextSource()
        {
            var src = _pool[_next];
            _next = (_next + 1) % _pool.Length;
            return src;
        }
    }
}
