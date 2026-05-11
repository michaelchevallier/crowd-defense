#nullable enable
using System;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public class Castle : MonoSingleton<Castle>
    {
        public int HP { get; private set; }
        public int HPMax { get; private set; }
        public bool IsDead => HP <= 0;

        public event Action<int, int>? OnHPChanged;
        public event Action<Castle>? OnCastleDied;

        public void Init(int hp)
        {
            HP = HPMax = hp;

            if (PathManager.Instance?.Grid != null)
            {
                var grid = PathManager.Instance.Grid;
                if (grid.Castles.Count > 0)
                {
                    var cell = grid.Castles[0];
                    transform.position = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize) + Vector3.up * 0.5f;
                }
            }

            OnHPChanged?.Invoke(HP, HPMax);
        }

        public void TakeDamage(int dmg)
        {
            if (IsDead || dmg <= 0) return;
            HP = Mathf.Max(0, HP - dmg);
            // Flag interest bank — any hit resets bank for this wave (D1-01 §3.5)
            Economy.Instance?.FlagCastleDamaged();
            OnHPChanged?.Invoke(HP, HPMax);

            // Stage B integration hooks (audio + juice)
            AudioController.Instance?.Play("castle_hit", 0.65f);
            JuiceFX.Instance?.Shake(0.1f, 200);
            JuiceFX.Instance?.Flash(new Color(1f, 0.2f, 0.2f, 0.4f), 150);

            if (HP == 0)
            {
                OnCastleDied?.Invoke(this);
                // Stage B integration : dramatic game-over feedback
                AudioController.Instance?.Play("enemy_die_boss", 1f);
                JuiceFX.Instance?.SlowMo(0.2f, 1500);
                JuiceFX.Instance?.Flash(new Color(0f, 0f, 0f, 0.7f), 1000);
#if UNITY_EDITOR
                Debug.Log("[Castle] destroyed");
#endif
            }
        }

        // D1-04 Q11 regen hook (no-op POC — Phase 3 passera valeur réelle)
        public void Regen(int amount)
        {
            if (IsDead || amount <= 0) return;
            HP = Mathf.Min(HPMax, HP + amount);
            OnHPChanged?.Invoke(HP, HPMax);
        }
    }
}
