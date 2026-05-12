#nullable enable
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Systems;
using CrowdDefense.UI;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    // D1-01 §3.6 — treasure tile spawned by TreasureSpawner.
    //
    // Two modes:
    //   Static  — Init(cellCenter, cellSize)  : placed at level load from '*' map cells.
    //             Enemy proximity < 0.6 cells breaks the chest.
    //             Intact at wave end → WaveManager collects bonus via IsConsumed check.
    //
    //   Break   — InitBreakTile(value, pos)   : spawned during wave break (20% chance).
    //             Hero walks within 1.5 m → autocollect reward + destroy.
    public class TreasureTile : MonoBehaviour
    {
        public bool IsConsumed { get; private set; }
        public Vector3 CellCenter { get; private set; }

        private float _cellSize;
        private bool  _isBreakTile;
        private int   _breakValue;

        // ── Static-tile init (legacy flow) ─────────────────────────────────────
        public void Init(Vector3 cellCenter, float cellSize)
        {
            CellCenter = cellCenter;
            _cellSize  = cellSize;
            transform.position = cellCenter + Vector3.up * 0.25f;
            BuildVisual(cellSize * 0.5f);
        }

        // ── Break-tile init (wave-break 20%-chance flow) ───────────────────────
        public void InitBreakTile(int value, Vector3 worldPos)
        {
            _isBreakTile = true;
            _breakValue  = value;
            CellCenter   = worldPos;
            _cellSize    = PathManager.Instance?.Grid?.CellSize ?? 1f;
            transform.position = worldPos + Vector3.up * 0.3f;

            float s = _cellSize * 0.4f;
            BuildVisual(s);
            AddSparkleParticles();
        }

        // ── Hero proximity collect (polled from TreasureSpawner.Update OverlapSphere) ──
        public void HeroCollect()
        {
            if (IsConsumed) return;
            IsConsumed = true;

            Economy.Instance?.AddGold(_breakValue);
            FloatingPopupController.Instance?.SpawnReward(
                $"+{_breakValue}¢",
                transform.position + Vector3.up * 1.2f,
                new Color(1f, 0.85f, 0.1f));
            FloatingPopupController.Instance?.SpawnCoin(_breakValue, transform.position + Vector3.up * 1.2f);
            VfxPool.Instance?.SpawnCoinBurst(transform.position + Vector3.up * 0.5f);

            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }

        // Called each frame by TreasureSpawner when enemies are nearby (static tiles only).
        public void CheckEnemyProximity(Vector3 enemyPos)
        {
            if (IsConsumed || _isBreakTile) return;
            float threshold = _cellSize * 0.6f;
            float dx = enemyPos.x - CellCenter.x;
            float dz = enemyPos.z - CellCenter.z;
            if (dx * dx + dz * dz < threshold * threshold)
                ConsumeStatic();
        }

        private void ConsumeStatic()
        {
            IsConsumed = true;
            gameObject.SetActive(false);
#if UNITY_EDITOR
            Debug.Log($"[TreasureTile] enemy-consumed at {CellCenter}");
#endif
        }

        // ── Visual helpers ────────────────────────────────────────────────────
        private void BuildVisual(float size)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "TreasureBody";
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(size, size, size);
            Object.Destroy(body.GetComponent<Collider>());

            var mr  = body.GetComponent<MeshRenderer>();
            var mat = new Material(ShaderUtil.GetLitShader())
            {
                color = new Color(1.00f, 0.82f, 0.12f)
            };
            mr.sharedMaterial = mat;
        }

        private void AddSparkleParticles()
        {
            var psGo = new GameObject("Sparkles");
            psGo.transform.SetParent(transform, false);
            psGo.transform.localPosition = Vector3.up * 0.3f;

            var ps = psGo.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop            = true;
            main.startLifetime   = 1.0f;
            main.startSpeed      = 0.8f;
            main.startSize       = 0.08f;
            main.maxParticles    = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor      = new Color(1f, 0.92f, 0.2f, 1f);

            var emission = ps.emission;
            emission.rateOverTime = 10f;

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.15f;

            ps.Play();
        }
    }
}
