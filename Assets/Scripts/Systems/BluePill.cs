#nullable enable
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Entities;
using CrowdDefense.Visual;

namespace CrowdDefense.Systems
{
    // Port of V5 BluePill.js — Matrix easter egg.
    // Hero holds still for CHANNEL_SEC → blue light build-up → teleport to nearest live castle.
    // Input: call StartChannel() from hero input code; movement auto-cancels.
    // Requires: AudioController (sfx "blue_pill"), LevelRunner (castle list).
    public class BluePill : MonoSingleton<BluePill>
    {
        private const float ChannelSec = 2f;
        private const float RingIntervalSec = 0.22f;
        private const float SparkleIntervalSec = 0.05f;

        [Header("VFX (optional — graceful fallback if null)")]
        [SerializeField] private GameObject? ringPrefab;
        [SerializeField] private Light?      channelLight;

        public bool IsChanneling { get; private set; }

        private float _elapsed;
        private float _lastRingAt;
        private float _lastSparkleAt;
        private Transform? _heroTransform;

        // ---------------------------------------------------------------
        // Public API (called from Hero input layer)
        // ---------------------------------------------------------------

        public void StartChannel(Transform heroTransform)
        {
            if (IsChanneling) return;
            if (!AnyCastleAlive()) return;

            _heroTransform = heroTransform;
            _elapsed = 0f;
            _lastRingAt = -1f;
            _lastSparkleAt = -1f;
            IsChanneling = true;

            if (channelLight != null)
            {
                channelLight.color = new Color(0.27f, 0.53f, 1f);
                channelLight.range = 10f;
                channelLight.intensity = 0f;
                channelLight.transform.position = heroTransform.position + Vector3.up;
                channelLight.enabled = true;
            }

            AudioController.Instance?.Play("blue_pill");
        }

        public void CancelChannel(bool dueToMovement = false)
        {
            if (!IsChanneling) return;
            IsChanneling = false;
            _heroTransform = null;

            if (channelLight != null)
            {
                channelLight.intensity = 0f;
                channelLight.enabled = false;
            }

            if (dueToMovement)
                AudioController.Instance?.Play("cancel");
        }

        // Called by Hero.Update when hero moves (mirrors V5 "movement" cancel)
        public void NotifyHeroMoved()
        {
            if (IsChanneling) CancelChannel(dueToMovement: true);
        }

        // ---------------------------------------------------------------
        // Update
        // ---------------------------------------------------------------

        private void Update()
        {
            if (!IsChanneling || _heroTransform == null) return;

            _elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / ChannelSec);

            TickLight(progress);
            TickRings();
            TickSparkles();

            if (_elapsed >= ChannelSec)
            {
                TeleportToNearestCastle();
                CancelChannel();
            }
        }

        // ---------------------------------------------------------------
        // Teleport
        // ---------------------------------------------------------------

        private void TeleportToNearestCastle()
        {
            if (_heroTransform == null) return;

            Castle? best = FindNearestCastle();
            if (best == null) return;

            Vector3 heroPos = _heroTransform.position;
            Vector3 castlePos = best.transform.position;
            Vector3 dir = (heroPos - castlePos);
            dir.y = 0f;
            float len = dir.magnitude;
            dir = len > 0.001f ? dir / len : Vector3.back;

            Vector3 dest = castlePos + dir * 3.5f;
            dest.y = _heroTransform.position.y;
            _heroTransform.position = dest;

            AudioController.Instance?.Play("level_up");
        }

        // ---------------------------------------------------------------
        // VFX helpers
        // ---------------------------------------------------------------

        private void TickLight(float progress)
        {
            if (channelLight == null || _heroTransform == null) return;
            channelLight.transform.position = _heroTransform.position + Vector3.up;
            float flicker = 0.85f + Mathf.Sin(Time.time * 14f) * 0.15f;
            channelLight.intensity = (2f + progress * 5f) * flicker;
        }

        private void TickRings()
        {
            if (ringPrefab == null || _heroTransform == null) return;
            if (Time.time - _lastRingAt < RingIntervalSec) return;
            _lastRingAt = Time.time;

            var ring = Instantiate(ringPrefab, _heroTransform.position, Quaternion.identity);
            Destroy(ring, 1.2f);
        }

        private void TickSparkles()
        {
            if (_heroTransform == null) return;
            if (Time.time - _lastSparkleAt < SparkleIntervalSec) return;
            _lastSparkleAt = Time.time;

            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float radius = 0.6f + UnityEngine.Random.Range(0f, 0.6f);
            Vector3 pos = _heroTransform.position + new Vector3(
                Mathf.Cos(angle) * radius, 0.1f, Mathf.Sin(angle) * radius);

            VfxPool.Instance?.SpawnImpact(pos, new Color(0.4f, 0.8f, 1f));
        }

        // ---------------------------------------------------------------
        // Castle helpers
        // ---------------------------------------------------------------

        private static bool AnyCastleAlive()
        {
            var runner = LevelRunner.Instance;
            return runner?.PrimaryCastle != null && !runner.PrimaryCastle.IsDead;
        }

        private static Castle? FindNearestCastle()
        {
            var runner = LevelRunner.Instance;
            if (runner?.PrimaryCastle == null) return null;
            return runner.PrimaryCastle.IsDead ? null : runner.PrimaryCastle;
        }
    }
}
