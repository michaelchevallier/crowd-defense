#nullable enable
using UnityEngine;

namespace CrowdDefense.Entities
{
    // D1-01 §3.6 — treasure tile spawned by TreasureSpawner at level load.
    // Non-walkable (BFS ignores '*' cells). Enemy proximity < 0.6 cells breaks the chest.
    // If intact at wave end → WaveManager collects bonus via IsConsumed check.
    public class TreasureTile : MonoBehaviour
    {
        public bool IsConsumed { get; private set; }

        // World-space cell center (set by TreasureSpawner)
        public Vector3 CellCenter { get; private set; }

        private float _cellSize;

        public void Init(Vector3 cellCenter, float cellSize)
        {
            CellCenter = cellCenter;
            _cellSize = cellSize;
            transform.position = cellCenter + Vector3.up * 0.25f;
        }

        // Called each frame by TreasureSpawner when enemies are nearby.
        // Proximity threshold: half a cell (mob footprint overlaps the tile).
        public void CheckEnemyProximity(Vector3 enemyPos)
        {
            if (IsConsumed) return;
            float threshold = _cellSize * 0.6f;
            float dx = enemyPos.x - CellCenter.x;
            float dz = enemyPos.z - CellCenter.z;
            if (dx * dx + dz * dz < threshold * threshold)
                Consume();
        }

        private void Consume()
        {
            IsConsumed = true;
            gameObject.SetActive(false);
#if UNITY_EDITOR
            Debug.Log($"[TreasureTile] consumed at {CellCenter}");
#endif
        }
    }
}
