#nullable enable
using UnityEngine;

namespace CrowdDefense.Visual
{
    public sealed class CameraController : MonoBehaviour
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
        private bool        _orbitDrag;       // space + left-drag
        private bool        _rightDrag;       // right-click orbit around castle
        private Vector3     _dragOrigin;
        private Vector3     _orbitPivot;

        // ── Public API ────────────────────────────────────────────────────────
        public void SetHero(Transform hero)    => _hero   = hero;
        public void SetCastle(Transform castle) => _castle = castle;
        public void SetMapBounds(float halfX, float halfZ) { mapHalfX = halfX; mapHalfZ = halfZ; }

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Update()
        {
            HandleZoom();
            HandleToggleFollow();
            HandlePan();
            HandleSpaceDrag();
            HandleRightClickOrbit();

            if (_followHero && _hero != null)
                SmoothFollowHero();

            ClampPosition();
        }

        // ── Zoom (mouse wheel → camera Y) ─────────────────────────────────────
        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.0001f) return;

            var pos = transform.position;
            pos.y -= scroll * zoomSpeed * 10f;
            pos.y  = Mathf.Clamp(pos.y, minY, maxY);
            transform.position = pos;
        }

        // ── Toggle follow Hero (C key) ─────────────────────────────────────────
        private void HandleToggleFollow()
        {
            if (Input.GetKeyDown(KeyCode.C))
                _followHero = !_followHero;
        }

        // ── WASD / Arrow keys pan ─────────────────────────────────────────────
        private void HandlePan()
        {
            if (_followHero) return;

            float h = Input.GetAxisRaw("Horizontal");   // A/D + Left/Right
            float v = Input.GetAxisRaw("Vertical");     // W/S + Up/Down

            if (Mathf.Abs(h) < 0.001f && Mathf.Abs(v) < 0.001f) return;

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

        // ── Clamp within map bounds ───────────────────────────────────────────
        private void ClampPosition()
        {
            var pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, -mapHalfX, mapHalfX);
            pos.z = Mathf.Clamp(pos.z, -mapHalfZ - 14f, mapHalfZ);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            transform.position = pos;
        }
    }
}
