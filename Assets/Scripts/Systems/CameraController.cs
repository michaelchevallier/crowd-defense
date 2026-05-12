#nullable enable
using UnityEngine;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Top-down camera with optional smooth follow on Hero.
    /// Toggle via PlayerPrefs key "cd.cam.follow_hero" (1 = on, 0 = off).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        private const string PrefKey = "cd.cam.follow_hero";

        [SerializeField] private Vector3 followOffset = new(0f, 12f, -6f);

        private Vector3 _defaultPosition;
        private Quaternion _defaultRotation;
        private bool _followEnabled;

        private void Awake()
        {
            _defaultPosition = transform.position;
            _defaultRotation = transform.rotation;
            _followEnabled = PlayerPrefs.GetInt(PrefKey, 0) == 1;
        }

        private void LateUpdate()
        {
            if (!_followEnabled || Hero.Current == null)
                return;

            Vector3 target = Hero.Current.transform.position + followOffset;
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 5f);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public bool FollowHero
        {
            get => _followEnabled;
            set
            {
                _followEnabled = value;
                PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
                PlayerPrefs.Save();

                if (!value)
                {
                    transform.position = _defaultPosition;
                    transform.rotation = _defaultRotation;
                }
            }
        }

        public void ToggleFollowHero() => FollowHero = !_followEnabled;
    }
}
