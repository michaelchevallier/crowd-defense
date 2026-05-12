#nullable enable
using System.Collections;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.Visual
{
    public sealed class CameraController : MonoSingleton<CameraController>
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [SerializeField] private float panSpeed        = 20f;
        [SerializeField] private float zoomSpeed       = 5f;
        [SerializeField] private float orbitSpeed      = 120f;
        [SerializeField] private float followLerpSpeed = 8f;

        [SerializeField] private float minY = 10f;
        [SerializeField] private float maxY = 30f;

        // Map bounds — set from outside (LevelRunner/BuildMainSceneTool)
        [SerializeField] private float mapHalfX = 59.5f;
        [SerializeField] private float mapHalfZ = 59.5f;

        // ── Runtime ───────────────────────────────────────────────────────────
        private Transform?  _hero;
        private Transform?  _castle;
        private bool        _followHero;
        private float       _followDisabledTimer; // seconds remaining before follow re-engages after manual pan
        private bool        _orbitDrag;       // space + left-drag
        private bool        _rightDrag;       // right-click orbit around castle
        private Vector3     _dragOrigin;
        private Vector3     _orbitPivot;
        private bool        _zooming;         // boss intro in progress
        private float       _baseY;           // Y at scene start — clamp = [baseY*0.5, baseY*2]
        private float       _prevPinchDist;   // touch pinch previous frame distance
        private Vector2     _prevTouchPos;    // 1-finger pan previous frame position
        private bool        _birdsEye;        // V hotkey overview mode
        private Vector3     _savedPos;        // position before bird's eye
        private Quaternion  _savedRot;        // rotation before bird's eye
        private Coroutine?  _birdsEyeRoutine;

        private const string KFollowHero = "camera_follow_hero_v1";
        private const float  FollowResumeDelay = 5f;

        // ── Public API ─────────────────────────────────────────────────────────
        public bool FollowHero
        {
            get => _followHero;
            set
            {
                _followHero = value;
                _followDisabledTimer = 0f;
                PlayerPrefs.SetInt(KFollowHero, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        // ── Public setters ────────────────────────────────────────────────────
        public void SetHero(Transform hero)    => _hero   = hero;
        public void SetCastle(Transform castle) => _castle = castle;
        public void SetMapBounds(float halfX, float halfZ) { mapHalfX = halfX; mapHalfZ = halfZ; }

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Start()
        {
            _baseY = Mathf.Clamp(transform.position.y, minY, maxY);
            _followHero = PlayerPrefs.GetInt(KFollowHero, 0) == 1;
            EventManager.Instance?.Subscribe<BossEncounteredEvent>(OnBossSpawn);
        }
        protected override void OnDestroySingleton() =>
            EventManager.Instance?.Unsubscribe<BossEncounteredEvent>(OnBossSpawn);

        private void OnBossSpawn(BossEncounteredEvent e) => StartCoroutine(BossZoomIntro(e.BossPos));

        private IEnumerator BossZoomIntro(Vector3 bossPos)
        {
            _zooming = true;
            var origin = transform.position;
            var target = new Vector3(bossPos.x - 5f, 0f, bossPos.z - 8f);
            target.y = Mathf.Clamp(minY, minY, maxY);

            // Lerp in (0.6 s) — unscaledDeltaTime: survives timeScale=0 boss cutscene
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.6f)
            {
                transform.position = Vector3.Lerp(origin, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            transform.position = target;

            yield return new WaitForSecondsRealtime(1.4f);  // hold: total intro = 2 s

            // Lerp back (0.6 s)
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.6f)
            {
                transform.position = Vector3.Lerp(target, origin, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            transform.position = origin;
            _zooming = false;
        }

        private void Update()
        {
            if (_zooming) return;
            if (Input.GetKeyDown(KeyCode.V)) ToggleBirdsEye();
            if (_birdsEye) return;
            HandleZoom();
            HandlePinchZoom();
            HandleTouchPan();
            HandleToggleFollow();
            HandlePan();
            HandleSpaceDrag();
            HandleRightClickOrbit();

            if (_followDisabledTimer > 0f)
                _followDisabledTimer -= Time.deltaTime;

            if (_followHero && _followDisabledTimer <= 0f && _hero != null)
                SmoothFollowHero();

            ClampPosition();
        }

        // ── Zoom (mouse wheel → camera Y, range 0.5x–2x base) ───────────────
        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.0001f) return;
            ApplyZoomDelta(-scroll * zoomSpeed * 10f);
        }

        // ── 1-finger touch pan ───────────────────────────────────────────────
        private void HandleTouchPan()
        {
            if (!Input.touchSupported || Input.touchCount != 1) { _prevTouchPos = Vector2.zero; return; }

            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) { _prevTouchPos = t.position; return; }
            if (t.phase != TouchPhase.Moved) return;
            if (_followHero) _followDisabledTimer = FollowResumeDelay;

            Vector2 delta = t.position - _prevTouchPos;
            _prevTouchPos = t.position;

            // Scale drag delta by camera height so pan feels consistent at all zoom levels
            float scale = (transform.position.y / (_baseY > 0.001f ? _baseY : minY)) * 0.012f;

            var right   = transform.right;   right.y = 0f; right.Normalize();
            var forward = transform.forward; forward.y = 0f; forward.Normalize();

            var pos = transform.position;
            pos -= (right * delta.x + forward * delta.y) * scale;
            transform.position = pos;
        }

        // ── Pinch zoom (2 fingers touch) ──────────────────────────────────────
        private void HandlePinchZoom()
        {
            if (Input.touchCount != 2) { _prevPinchDist = 0f; return; }

            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float dist = Vector2.Distance(t0.position, t1.position);

            if (_prevPinchDist < 0.001f) { _prevPinchDist = dist; return; }

            float delta = _prevPinchDist - dist;   // positive = fingers closing = zoom out = raise Y
            _prevPinchDist = dist;
            ApplyZoomDelta(delta * zoomSpeed * 0.05f);
        }

        private void ApplyZoomDelta(float deltaY)
        {
            // Guard: _baseY may be 0 on first frame before Start if called early
            float baseRef = _baseY > 0.001f ? _baseY : minY;
            float zoomMin = Mathf.Max(minY, baseRef * 0.5f);
            float zoomMax = Mathf.Min(maxY, baseRef * 2f);

            var pos = transform.position;
            pos.y = Mathf.Clamp(pos.y + deltaY, zoomMin, zoomMax);
            transform.position = pos;
        }

        // ── Toggle follow Hero (F key) ─────────────────────────────────────────
        private void HandleToggleFollow()
        {
            if (Input.GetKeyDown(KeyCode.F))
                FollowHero = !_followHero;
        }

        // ── WASD / Arrow keys pan ─────────────────────────────────────────────
        private void HandlePan()
        {
            float h = Input.GetAxisRaw("Horizontal");   // A/D + Left/Right
            float v = Input.GetAxisRaw("Vertical");     // W/S + Up/Down

            if (Mathf.Abs(h) < 0.001f && Mathf.Abs(v) < 0.001f) return;

            if (_followHero) _followDisabledTimer = FollowResumeDelay;

            var right   = transform.right;   right.y = 0f; right.Normalize();
            var forward = transform.forward; forward.y = 0f; forward.Normalize();

            var pos = transform.position;
            pos += (right * h + forward * v) * panSpeed * Time.deltaTime;
            transform.position = pos;
        }

        // ── Space + left-drag free orbit ──────────────────────────────────────
        private void HandleSpaceDrag()
        {
            bool spaceHeld = Input.GetKey(KeyCode.Space);

            if (spaceHeld && Input.GetMouseButtonDown(0))
            {
                _orbitDrag  = true;
                _dragOrigin = Input.mousePosition;
                _orbitPivot = transform.position + transform.forward * 15f;
                _orbitPivot.y = 0f;
            }
            if (Input.GetMouseButtonUp(0)) _orbitDrag = false;

            if (!_orbitDrag || !spaceHeld) return;

            Vector3 delta = Input.mousePosition - _dragOrigin;
            _dragOrigin = Input.mousePosition;

            float yaw   = delta.x * orbitSpeed * Time.deltaTime;
            float pitch = -delta.y * orbitSpeed * 0.5f * Time.deltaTime;

            transform.RotateAround(_orbitPivot, Vector3.up,    yaw);
            transform.RotateAround(_orbitPivot, transform.right, pitch);

            // Keep pitch sane (5 – 85 deg elevation)
            var angles = transform.eulerAngles;
            float x = angles.x > 180f ? angles.x - 360f : angles.x;
            x = Mathf.Clamp(x, 5f, 85f);
            transform.eulerAngles = new Vector3(x, angles.y, 0f);
        }

        // ── Right-click drag → orbit around Castle ────────────────────────────
        private void HandleRightClickOrbit()
        {
            if (Input.GetMouseButtonDown(1))
            {
                _rightDrag  = true;
                _dragOrigin = Input.mousePosition;
                _orbitPivot = _castle != null ? _castle.position : Vector3.zero;
            }
            if (Input.GetMouseButtonUp(1)) _rightDrag = false;
            if (!_rightDrag) return;

            Vector3 delta = Input.mousePosition - _dragOrigin;
            _dragOrigin = Input.mousePosition;

            float yaw   = delta.x * orbitSpeed * Time.deltaTime;
            float pitch = -delta.y * orbitSpeed * 0.5f * Time.deltaTime;

            transform.RotateAround(_orbitPivot, Vector3.up,    yaw);
            transform.RotateAround(_orbitPivot, transform.right, pitch);

            var angles = transform.eulerAngles;
            float x = angles.x > 180f ? angles.x - 360f : angles.x;
            x = Mathf.Clamp(x, 5f, 85f);
            transform.eulerAngles = new Vector3(x, angles.y, 0f);
        }

        // ── Smooth follow Hero ────────────────────────────────────────────────
        private void SmoothFollowHero()
        {
            if (_hero == null) return;
            var target = _hero.position;
            var desired = new Vector3(target.x, transform.position.y, target.z - 12f);
            transform.position = Vector3.Lerp(transform.position, desired,
                followLerpSpeed * Time.deltaTime);
        }

        // ── Screen shake ─────────────────────────────────────────────────────
        public void Shake(float intensity, float duration) =>
            StartCoroutine(ShakeRoutine(intensity, duration));

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            var origin = transform.position;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float remaining = 1f - t / duration;
                transform.position = origin + (Vector3)Random.insideUnitCircle * intensity * remaining;
                yield return null;
            }
            transform.position = origin;
        }

        // ── Bird's eye toggle (V key) ─────────────────────────────────────────
        private void ToggleBirdsEye()
        {
            if (_birdsEyeRoutine != null) StopCoroutine(_birdsEyeRoutine);
            _birdsEye = !_birdsEye;
            _birdsEyeRoutine = _birdsEye
                ? StartCoroutine(BirdsEyeEnter())
                : StartCoroutine(BirdsEyeExit());
        }

        private IEnumerator BirdsEyeEnter()
        {
            _savedPos = transform.position;
            _savedRot = transform.rotation;

            float overviewY = Mathf.Max(maxY, _baseY * 2f) * 1.5f;
            var   targetPos = new Vector3(0f, overviewY, 0f);
            var   targetRot = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.4f)
            {
                float s = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(_savedPos, targetPos, s);
                transform.rotation = Quaternion.Slerp(_savedRot, targetRot, s);
                yield return null;
            }
            transform.position = targetPos;
            transform.rotation = targetRot;
            _birdsEyeRoutine = null;
        }

        private IEnumerator BirdsEyeExit()
        {
            var fromPos = transform.position;
            var fromRot = transform.rotation;

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.4f)
            {
                float s = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(fromPos, _savedPos, s);
                transform.rotation = Quaternion.Slerp(fromRot, _savedRot, s);
                yield return null;
            }
            transform.position = _savedPos;
            transform.rotation = _savedRot;
            _birdsEyeRoutine = null;
        }

        // ── Clamp within map bounds ───────────────────────────────────────────
        private void ClampPosition()
        {
            float baseRef = _baseY > 0.001f ? _baseY : minY;
            float zoomMin = Mathf.Max(minY, baseRef * 0.5f);
            float zoomMax = Mathf.Min(maxY, baseRef * 2f);

            var pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, -mapHalfX, mapHalfX);
            pos.z = Mathf.Clamp(pos.z, -mapHalfZ - 14f, mapHalfZ);
            pos.y = Mathf.Clamp(pos.y, zoomMin, zoomMax);
            transform.position = pos;
        }
    }
}
