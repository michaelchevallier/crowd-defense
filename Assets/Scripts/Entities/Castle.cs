#nullable enable
using System;
using UnityEngine;
using CrowdDefense.Systems;

namespace CrowdDefense.Entities
{
    public class Castle : MonoBehaviour
    {
        public static Castle? Instance { get; private set; }

        public int HP { get; private set; }
        public int HPMax { get; private set; }
        public bool IsDead => HP <= 0;

        public event Action<int, int>? OnHPChanged;
        public event Action? OnDestroyed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            int max = LevelRunner.Instance?.CurrentLevel?.CastleHP ?? 120;
            HP = HPMax = max;
            OnHPChanged?.Invoke(HP, HPMax);

            if (PathManager.Instance?.Grid != null && PathManager.Instance.Grid.Castles.Count > 0)
            {
                var grid = PathManager.Instance.Grid;
                var castleCell = grid.Castles[0];
                transform.position = GridCoords.CellToWorld(castleCell.x, castleCell.y, grid.Width, grid.Height, grid.CellSize) + Vector3.up * 0.5f;
            }
        }

        public void TakeDamage(int dmg)
        {
            if (IsDead || dmg <= 0) return;
            HP = Mathf.Max(0, HP - dmg);
            OnHPChanged?.Invoke(HP, HPMax);
            if (HP == 0)
            {
                OnDestroyed?.Invoke();
                LevelRunner.Instance?.SetState(GameState.GameOver);
#if UNITY_EDITOR
                Debug.Log("[Castle] destroyed → GameOver");
#endif
            }
        }
    }
}
