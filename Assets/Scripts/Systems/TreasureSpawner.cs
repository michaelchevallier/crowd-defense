#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Visual;

namespace CrowdDefense.Systems
{
    // D1-01 §3.6 — spawns TreasureTile entities.
    //
    // Two spawn modes:
    //   A) Level load  : one tile per '*' cell in the current level map.
    //      Enemy proximity breaks the chest; intact tiles pay out at wave end.
    //
    //   B) Wave break  : 20% chance (BalanceConfig.BreakTreasureChance) to spawn one
    //      bonus tile on a random path waypoint during the wave break window.
    //      Hero auto-collects by walking within HeroCollectRadius metres.
    [DefaultExecutionOrder(60)]
    public class TreasureSpawner : MonoBehaviour
    {
        public static TreasureSpawner? Instance { get; private set; }

        private const float HeroCollectRadius = 1.5f;

        private readonly List<TreasureTile> _tiles      = new();
        private readonly List<TreasureTile> _breakTiles = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            SpawnStaticTiles();

            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveCleared += OnWaveCleared;
        }

        private void OnDestroy()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveCleared -= OnWaveCleared;
        }

        // ── A) Static tiles from '*' map cells ────────────────────────────────
        private void SpawnStaticTiles()
        {
            var grid = PathManager.Instance?.Grid;
            if (grid == null) return;

            foreach (var cell in grid.Treasures)
            {
                Vector3 center = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize);
                var tile = CreateTileGo(center);
                tile.Init(center, grid.CellSize);
                _tiles.Add(tile);
            }

#if UNITY_EDITOR
            if (_tiles.Count > 0)
                Debug.Log($"[TreasureSpawner] spawned {_tiles.Count} static treasure tile(s)");
#endif
        }

        // ── B) Wave-break random treasure (20% chance, on random path waypoint) ──
        private void TrySpawnBreakTreasure()
        {
            var cfg = BalanceConfig.Get();
            if (Random.value > cfg.BreakTreasureChance) return;

            var pm = PathManager.Instance;
            if (pm == null || pm.Paths.Count == 0) return;

            // Collect mid-path waypoints (skip portal at [0] and castle at [last])
            var candidates = new List<Vector3>();
            foreach (var path in pm.Paths)
            {
                int count = path.Count;
                for (int i = 1; i < count - 1; i++)
                    candidates.Add(path[i]);
            }
            if (candidates.Count == 0) return;

            Vector3 pos   = candidates[Random.Range(0, candidates.Count)];
            int     value = cfg.RollTreasureValue();

            var tile = CreateTileGo(pos);
            tile.InitBreakTile(value, pos);
            _tiles.Add(tile);
            _breakTiles.Add(tile);

#if UNITY_EDITOR
            Debug.Log($"[TreasureSpawner] break-treasure at {pos} value={value}¢");
#endif
        }

        private TreasureTile CreateTileGo(Vector3 pos)
        {
            var go = new GameObject("Treasure");
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            return go.AddComponent<TreasureTile>();
        }

        // ── Update: proximity checks ──────────────────────────────────────────
        private void Update()
        {
            // Enemy proximity for static tiles (only during active waves)
            var enemies = WaveManager.Instance?.ActiveEnemies;
            if (enemies != null && enemies.Count > 0)
            {
                foreach (var tile in _tiles)
                {
                    if (tile == null || tile.IsConsumed) continue;
                    foreach (var enemy in enemies)
                    {
                        if (enemy == null || !enemy.isActiveAndEnabled) continue;
                        tile.CheckEnemyProximity(enemy.transform.position);
                        if (tile.IsConsumed) break;
                    }
                }
            }

            // Hero collect for break-tiles
            if (_breakTiles.Count == 0) return;
            var hero = LevelRunner.Instance?.Hero;
            if (hero == null) return;

            float r2     = HeroCollectRadius * HeroCollectRadius;
            var heroPos  = hero.transform.position;

            for (int i = _breakTiles.Count - 1; i >= 0; i--)
            {
                var tile = _breakTiles[i];
                if (tile == null || tile.IsConsumed)
                {
                    _breakTiles.RemoveAt(i);
                    continue;
                }
                if ((tile.CellCenter - heroPos).sqrMagnitude < r2)
                {
                    tile.HeroCollect();
                    _breakTiles.RemoveAt(i);
                }
            }
        }

        // ── Wave cleared: pay out static tiles + maybe spawn break treasure ────
        private void OnWaveCleared(int waveIdx)
        {
            PayOutIntactStaticTiles();
            CleanupConsumedTiles();
            TrySpawnBreakTreasure();
        }

        private void PayOutIntactStaticTiles()
        {
            var cfg   = BalanceConfig.Get();
            int total = 0;
            foreach (var tile in _tiles)
            {
                if (tile == null || tile.IsConsumed) continue;
                int gain = cfg.RollTreasureValue();
                total += gain;
                CrowdDefense.UI.FloatingPopupController.Instance?.SpawnCoin(
                    gain, tile.CellCenter + Vector3.up * 1.2f);
                VfxPool.Instance?.SpawnCoinBurst(tile.CellCenter + Vector3.up * 0.5f);
            }

            if (total > 0)
            {
                Economy.Instance?.AddGold(total);
#if UNITY_EDITOR
                Debug.Log($"[TreasureSpawner] intact static tile(s) → +{total}¢");
#endif
            }
        }

        private void CleanupConsumedTiles()
        {
            for (int i = _tiles.Count - 1; i >= 0; i--)
            {
                var tile = _tiles[i];
                if (tile == null || tile.IsConsumed) _tiles.RemoveAt(i);
            }
        }

        public int IntactCount()
        {
            int n = 0;
            foreach (var t in _tiles) if (t != null && !t.IsConsumed) n++;
            return n;
        }
    }
}
