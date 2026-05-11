#nullable enable
using System;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    public class Economy : MonoSingleton<Economy>
    {
        public int Gold { get; private set; }

        // Interest bank state (D1-01 §3.5)
        public bool CastleDamagedThisWave { get; private set; }
        public int TotalBankAccumulated { get; private set; }

        public event Action<int>? OnGoldChanged;
        // Fired with (gain, totalAccumulated) on interest tick; gain=0 means reset (leak)
        public event Action<int, int>? OnBankTick;

        private void Start()
        {
            if (LevelRunner.Instance?.CurrentLevel != null)
                SetGold(LevelRunner.Instance.CurrentLevel.StartCoins);
            else
                SetGold(120);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0) return false;
            if (Gold < amount) return false;
            SetGold(Gold - amount);
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            SetGold(Gold + amount);
        }

        // Called by Enemy.Die after EnemyKilledEvent has been published (so ComboSystem
        // has already updated its ActiveMultiplier for this kill).
        public void AddGoldFromKill(int baseReward)
        {
            float comboMul = ComboSystem.Instance?.ActiveMultiplier ?? 1f;
            int finalReward = Mathf.Max(1, Mathf.RoundToInt(baseReward * comboMul));
            AddGold(finalReward);
        }

        // Called by Castle.TakeDamage to flag a leak this wave
        public void FlagCastleDamaged()
        {
            CastleDamagedThisWave = true;
        }

        // Called by WaveManager at the start of each wave (after OnWaveStart fires)
        public void ResetWaveDamageFlag()
        {
            CastleDamagedThisWave = false;
        }

        // Called by WaveManager when a wave is cleared
        public void ProcessInterestBank()
        {
            if (CastleDamagedThisWave)
            {
                TotalBankAccumulated = 0;
                OnBankTick?.Invoke(0, 0);
#if UNITY_EDITOR
                Debug.Log("[Economy] Interest bank RESET — castle took damage this wave");
#endif
            }
            else
            {
                int gain = Mathf.RoundToInt(Gold * BalanceConfig.Get().BankInterestRate);
                if (gain > 0)
                {
                    TotalBankAccumulated += gain;
                    AddGold(gain);
                    OnBankTick?.Invoke(gain, TotalBankAccumulated);
#if UNITY_EDITOR
                    Debug.Log($"[Economy] Interest bank +{gain}¢ (total accumulated={TotalBankAccumulated})");
#endif
                }
            }
        }

        private void SetGold(int v)
        {
            Gold = v;
            OnGoldChanged?.Invoke(Gold);
        }
    }
}
