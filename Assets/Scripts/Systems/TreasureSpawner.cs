#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    // D1-01 §3.6 — spawns TreasureTile entities for every '*' cell in the current level.
    // Monitors enemy proximity each frame; collects intact treasures at wave end.
    [DefaultExecutionOrder(60)]
    public class TreasureSpawner : MonoBehaviour
    {
        public static TreasureSpawner? Instance { get; private set; }

        private readonly List<TreasureTile> _tiles = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            var grid = PathManager.Instance?.Grid;
            if (grid == null) return;

            foreach (var cell in grid.Treasures)
            {
                Vector3 center = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize);
                SpawnTile(center, grid.CellSize);
            }

            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveCleared += OnWaveCleared;

#if UNITY_EDITOR
            if (_tiles.Count > 0)
                Debug.Log($"[TreasureSpawner] spawned {_tiles.Count} treasure tile(s)");
#endif
        }

        private void OnDestroy()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveCleared -= OnWaveCleared;
        }

        private void SpawnTile(Vector3 center, float cellSize)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "TreasureTile";
            go.transform.SetParent(transform, false);
            float s = cellSize * 0.5f;
            go.transform.localScale = new Vector3(s, s, s);

            // Golden material
            var mr = go.GetComponent<MeshRenderer>();
            var mat = new Material(Common.ShaderUtil.GetLitShader())
            {
                color = new Color(1.00f, 0.80f, 0.10f)
            };
            mr.sharedMaterial = mat;

            // Remove collider — proximity check done manually
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var tile = go.AddComponent<TreasureTile>();
            tile.Init(center, cellSize);
            _tiles.Add(tile);
        }

        private void Update()
        {
            if (_tiles.Count == 0) return;
            var enemies = WaveManager.Instance?.ActiveEnemies;
            if (enemies == null || enemies.Count == 0) return;

            foreach (var tile in _tiles)
            {
                if (tile.IsConsumed) continue;
                foreach (var enemy in enemies)
                {
                    if (enemy == null || !enemy.isActiveAndEnabled) continue;
                    tile.CheckEnemyProximity(enemy.transform.position);
                    if (tile.IsConsumed) break;
                }
            }
        }

        private void OnWaveCleared(int _waveIdx)
        {
            int intact = 0;
            foreach (var tile in _tiles)
                if (!tile.IsConsumed) intact++;

            if (intact <= 0) return;

            var cfg = BalanceConfig.Get();
            int total = 0;
            for (int i = 0; i < intact; i++)
                total += cfg.RollTreasureValue();

            Economy.Instance?.AddGold(total);

#if UNITY_EDITOR
            Debug.Log($"[TreasureSpawner] {intact} intact treasure(s) → +{total} gold");
#endif
        }

        // Expose for external query (e.g. HUD)
        public int IntactCount()
        {
            int n = 0;
            foreach (var t in _tiles) if (!t.IsConsumed) n++;
            return n;
        }
    }
}
