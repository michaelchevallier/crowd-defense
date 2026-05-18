#nullable enable
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public partial class Hero : MonoBehaviour
    {
        // ── Movement ──────────────────────────────────────────────────────────
        protected Vector2 _moveDir;
        protected Vector2 _smoothedMoveDir;
        private const float MoveAccel = 8f;

        // ── Idle dance ────────────────────────────────────────────────────────
        private float _idleSeconds;
        private float _idleBaseY;
        private bool _idleBaseYCaptured;
        private const float IdleDanceDelay  = 5f;
        private const float DanceRotAmp     = 10f;
        private const float DanceRotHz      = 0.8f;
        private const float DanceBobAmp     = 0.05f;
        private const float DanceBobHz      = 0.6f;

        public void SetMove(float dx, float dz)
        {
            float len = Mathf.Sqrt(dx * dx + dz * dz);
            _moveDir = len > 0.05f ? new Vector2(dx / len, dz / len) : Vector2.zero;
        }

        public void SetRunning(bool running) => _running = running;

        private void UpdateMovement(float dt)
        {
            if (cfg == null) return;

            _smoothedMoveDir = Vector2.MoveTowards(_smoothedMoveDir, _moveDir, MoveAccel * dt);
            bool moving = _smoothedMoveDir.sqrMagnitude > 0.01f;

            if (moving)
            {
                _idleSeconds = 0f;
                _idleBaseYCaptured = false;

                float speed = cfg.MoveSpeed * MoveSpeedMul * (_running ? 1.8f : 1f);
                var oldPos = transform.position;
                var pos = oldPos;
                pos.x += _smoothedMoveDir.x * speed * dt;
                pos.z += _smoothedMoveDir.y * speed * dt;
                pos.x = Mathf.Clamp(pos.x, -_maxX, _maxX);
                pos.z = Mathf.Clamp(pos.z, -_maxZ, _maxZ);

                if (!IsWalkableWorldPos(pos))
                    pos = oldPos;

                transform.position = pos;

                if (_attackAnimTimer <= 0f && _animator != null)
                    AnimationController.SetWalking(_animator, true);
            }
            else
            {
                _idleSeconds += dt;

                if (_attackAnimTimer <= 0f && _animator != null)
                    AnimationController.SetWalking(_animator, false);

                if (_idleSeconds >= IdleDanceDelay)
                {
                    if (!_idleBaseYCaptured)
                    {
                        _idleBaseY = transform.position.y;
                        _idleBaseYCaptured = true;
                    }
                    float t = Time.time;
                    float bobY = DanceBobAmp * Mathf.Sin(t * DanceBobHz * 2f * Mathf.PI);
                    var basePos = transform.position;
                    basePos.y = _idleBaseY + bobY;
                    transform.position = basePos;
                }
                else
                {
                    _idleBaseYCaptured = false;
                }
            }

            if (_smoothedMoveDir.sqrMagnitude > 0.01f)
            {
                var fwd = new Vector3(_smoothedMoveDir.x, 0f, _smoothedMoveDir.y);
                if (fwd != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(fwd);
            }
        }

        private static bool IsWalkableWorldPos(Vector3 worldPos)
        {
            var grid = PathManager.Instance?.Grid;
            if (grid == null) return true;
            var cell = GridCoords.WorldToCell(worldPos, grid.Width, grid.Height, grid.CellSize);
            if (cell.x < 0 || cell.x >= grid.Width || cell.y < 0 || cell.y >= grid.Height) return true;
            char ch = grid.At(cell.x, cell.y);
            return ch != GridCoords.WATER && ch != GridCoords.LAVA;
        }
    }
}
