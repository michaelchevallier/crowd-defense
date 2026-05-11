#nullable enable
using System;
using UnityEngine;
using CrowdDefense.Systems;

namespace CrowdDefense.Entities
{
    public class Castle : MonoBehaviour
    {
        public int CastleIdx { get; private set; } = 0;
        public int HP { get; private set; }
        public int HPMax { get; private set; }
        public bool IsDead => HP <= 0;

        public event Action<int, int>? OnHPChanged;
        public event Action<Castle>? OnCastleDied;

        public void Init(int castleIdx, int hp)
        {
            CastleIdx = castleIdx;
            HP = HPMax = hp;

            if (PathManager.Instance?.Grid != null)
            {
                var grid = PathManager.Instance.Grid;
                if (castleIdx < grid.Castles.Count)
                {
                    var cell = grid.Castles[castleIdx];
                    transform.position = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize) + Vector3.up * 0.5f;
                }
            }

            OnHPChanged?.Invoke(HP, HPMax);
        }

        public void TakeDamage(int dmg)
        {
            if (IsDead || dmg <= 0) return;
            HP = Mathf.Max(0, HP - dmg);
            OnHPChanged?.Invoke(HP, HPMax);
            if (HP == 0)
            {
                OnCastleDied?.Invoke(this);
#if UNITY_EDITOR
                Debug.Log($"[Castle] idx={CastleIdx} destroyed");
#endif
            }
        }
    }
}
