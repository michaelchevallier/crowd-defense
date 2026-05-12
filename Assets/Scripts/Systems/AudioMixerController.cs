#nullable enable
using CrowdDefense.Common;
using UnityEngine;
using UnityEngine.Audio;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Helper singleton wrapping AudioMixer for volume-group management.
    /// Exposed param names: Master_Volume, SFX_Volume, Music_Volume, Ambient_Volume, UI_Volume.
    /// Assign the AudioMixer asset via Inspector; gracefully no-ops if null.
    /// </summary>
    [DefaultExecutionOrder(-45)]
    public class AudioMixerController : MonoSingleton<AudioMixerController>
    {
        private const float MinDb = -80f;
        private const float MaxDb = 0f;

        [SerializeField] private AudioMixer? mixer;
        private bool _warnedNoMixer;

        [Header("Group references (assign Inspector)")]
        [SerializeField] private AudioMixerGroup? masterGroup;
        [SerializeField] private AudioMixerGroup? sfxGroup;
        [SerializeField] private AudioMixerGroup? musicGroup;
        [SerializeField] private AudioMixerGroup? ambientGroup;
        [SerializeField] private AudioMixerGroup? uiGroup;

        protected override void OnAwakeSingleton()
        {
            if (mixer == null)
            {
                // Try Resources.Load fallback paths
                mixer = Resources.Load<AudioMixer>("Audio/MainAudioMixer")
                     ?? Resources.Load<AudioMixer>("MainAudioMixer")
                     ?? Resources.Load<AudioMixer>("Audio/AudioMixer");
            }
            if (mixer == null && !_warnedNoMixer)
            {
                Debug.LogWarning("[AudioMixerController] No AudioMixer found in Inspector or Resources/ — volume controls disabled.");
                _warnedNoMixer = true;
            }
        }

        /// <summary>
        /// Sets the exposed parameter "<paramref name="groupName"/>_Volume" on the mixer
        /// using a linear [0..1] → dB conversion.
        /// </summary>
        public void SetGroupVolume(string groupName, float linear)
        {
            if (mixer == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[AudioMixerController] AudioMixer not assigned — cannot set {groupName}_Volume.");
#endif
                return;
            }
            float dB = linear > 0.001f ? Mathf.Clamp(Mathf.Log10(linear) * 20f, MinDb, MaxDb) : MinDb;
            mixer.SetFloat(groupName + "_Volume", dB);
        }

        /// <summary>Returns the named AudioMixerGroup, or null if unknown / mixer not assigned.</summary>
        public AudioMixerGroup? GetGroup(string name) => name switch
        {
            "Master"  => masterGroup,
            "SFX"     => sfxGroup,
            "Music"   => musicGroup,
            "Ambient" => ambientGroup,
            "UI"      => uiGroup,
            _ => LookupGroupFromMixer(name),
        };

        private AudioMixerGroup? LookupGroupFromMixer(string name)
        {
            if (mixer == null) return null;
            var results = mixer.FindMatchingGroups(name);
            return results is { Length: > 0 } ? results[0] : null;
        }
    }
}
