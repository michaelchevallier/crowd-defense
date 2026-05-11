#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Systems;
using CrowdDefense.UI;

namespace CrowdDefense.Visual
{
    public class JuiceFX : MonoSingleton<JuiceFX>
    {
        Camera? _cam;
        Vector3 _baseCamPos;

        // Shake state — 1 active slot, re-trigger overrides
        float _shakeIntensity;
        float _shakeEndTime;
        float _shakeDuration;

        // Flash state
        VisualElement? _flashOverlay;
        VisualElement? _hudRoot;
        bool _hudRootSearched;
        Coroutine? _flashCoroutine;

        // SlowMo state
        Coroutine? _slowMoCoroutine;

        protected override void OnAwakeSingleton()
        {
            _cam = Camera.main;
            if (_cam != null)
                _baseCamPos = _cam.transform.position;

            var em = EventManager.Instance;
            if (em == null) return;
            em.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            em.Subscribe<BossDefeatedEvent>(OnBossDefeated);
            em.Subscribe<LevelEndedEvent>(OnLevelEnded);
        }

        protected override void OnDestroySingleton()
        {
            var em = EventManager.Instance;
            if (em == null) return;
            em.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            em.Unsubscribe<BossDefeatedEvent>(OnBossDefeated);
            em.Unsubscribe<LevelEndedEvent>(OnLevelEnded);
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnEnemyKilled(EnemyKilledEvent _) => Shake(0.04f, 120);

        private void OnBossDefeated(BossDefeatedEvent _)
        {
            Shake(0.30f, 600);
            SlowMo(0.05f, 100);
        }

        private void OnLevelEnded(LevelEndedEvent evt)
        {
            if (evt.Victory)
                Flash(Color.white, 400);
        }

        // ── Public API ────────────────────────────────────────────────────────

        // Trigger camera shake. Re-calling overrides current shake.
        // Gated by Settings.ShakeEnabled.
        public void Shake(float intensity, int durationMs)
        {
            if (SettingsRegistry.Instance != null && !SettingsRegistry.Instance.ShakeEnabled) return;
            float duration = Mathf.Max(0.016f, durationMs / 1000f);
            _shakeIntensity = intensity;
            _shakeEndTime = Time.unscaledTime + duration;
            _shakeDuration = duration;
        }

        // Trigger fullscreen flash overlay. Re-calling overrides current flash.
        public void Flash(Color color, int durationMs)
        {
            float duration = Mathf.Max(0.08f, durationMs / 1000f);

            EnsureFlashOverlay();

            if (_flashOverlay == null) return;

            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);

            _flashOverlay.style.backgroundColor = color;
            _flashCoroutine = StartCoroutine(FlashRoutine(_flashOverlay, duration));
        }

        // Punch-scale a transform (pop bounce: scale up then back to 1).
        public void PunchScale(Transform target, float peakScale, float durationS)
        {
            if (target == null) return;
            StartCoroutine(PunchScaleRoutine(target, peakScale, durationS));
        }

        private static IEnumerator PunchScaleRoutine(Transform target, float peakScale, float durationS)
        {
            if (target == null) yield break;
            var original = target.localScale;
            float half = durationS * 0.5f;
            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1f, peakScale, t / half);
                if (target != null) target.localScale = original * s;
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(peakScale, 1f, t / half);
                if (target != null) target.localScale = original * s;
                yield return null;
            }
            if (target != null) target.localScale = original;
        }

        // Trigger slow-motion. Re-calling overrides current slow-mo.
        public void SlowMo(float timeScale, int durationMs)
        {
            float duration = Mathf.Max(0.016f, durationMs / 1000f);

            if (_slowMoCoroutine != null)
                StopCoroutine(_slowMoCoroutine);

            _slowMoCoroutine = StartCoroutine(SlowMoRoutine(timeScale, duration));
        }

        private void LateUpdate()
        {
            if (_cam == null) return;

            float now = Time.unscaledTime;
            if (now < _shakeEndTime && _shakeDuration > 0f)
            {
                float remaining = _shakeEndTime - now;
                float fade = remaining / _shakeDuration;
                float k = _shakeIntensity * fade;
                Vector3 offset = new Vector3(
                    Random.Range(-1f, 1f) * k,
                    Random.Range(-1f, 1f) * k * 0.4f,
                    Random.Range(-1f, 1f) * k
                );
                _cam.transform.position = _baseCamPos + offset;
            }
            else
            {
                // Restore base position once shake expires
                if (_cam.transform.position != _baseCamPos)
                    _cam.transform.position = _baseCamPos;
                _shakeEndTime = 0f;
            }
        }

        // Keep _baseCamPos in sync when camera is moved externally (e.g. follow scripts)
        public void SetBaseCamPos(Vector3 pos) => _baseCamPos = pos;

        private void EnsureFlashOverlay()
        {
            if (_flashOverlay != null) return;

            if (!_hudRootSearched)
            {
                _hudRootSearched = true;
                _hudRoot = Object.FindFirstObjectByType<UIDocument>()?.rootVisualElement;
                if (_hudRoot == null)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("[JuiceFX] Flash: no UIDocument found in scene — flash is a no-op.");
#endif
                    return;
                }
            }

            if (_hudRoot == null) return;

            _flashOverlay = new VisualElement
            {
                name = "juice-flash-overlay",
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    left = 0, top = 0, right = 0, bottom = 0,
                    opacity = 0f
                }
            };
            _hudRoot.Add(_flashOverlay);
        }

        private static IEnumerator FlashRoutine(VisualElement overlay, float duration)
        {
            // Ramp up to 0.45 opacity at 32% of duration, then fade to 0
            float peakTime = duration * 0.32f;
            float decayTime = duration - peakTime;

            float t = 0f;
            while (t < peakTime)
            {
                t += Time.unscaledDeltaTime;
                overlay.style.opacity = Mathf.Lerp(0f, 0.45f, t / peakTime);
                yield return null;
            }

            overlay.style.opacity = 0.45f;
            t = 0f;
            while (t < decayTime)
            {
                t += Time.unscaledDeltaTime;
                overlay.style.opacity = Mathf.Lerp(0.45f, 0f, t / decayTime);
                yield return null;
            }

            overlay.style.opacity = 0f;
        }

        private static IEnumerator SlowMoRoutine(float timeScale, float duration)
        {
            Time.timeScale = timeScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Time.timeScale = 1f;
        }
    }
}
