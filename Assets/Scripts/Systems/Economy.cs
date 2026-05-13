#nullable enable
using System;
using System.Collections;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    [DefaultExecutionOrder(-100)]
    public class Economy : MonoSingleton<Economy>
    {
        public int Gold { get; private set; }

        // Interest bank state (D1-01 §3.5 / V4 parity)
        // Bank is a separate gold pool; interest earned per wave-clear is transferred into it,
        // then immediately paid out to Gold (bank acts as an accounting ledger, not a locked vault).
        public int Bank { get; private set; }
        public bool CastleDamagedThisWave { get; private set; }
        public int TotalBankAccumulated { get; private set; }

        public event Action<int>? OnGoldChanged;
        // Fired with (gain, totalAccumulated) on interest tick; gain=0 means reset (leak)
        public event Action<int, int>? OnBankTick;

        // Debounce gold popup — groups rapid gains within 0.05s into a single popup
        private int     _pendingPopupGold;
        private Vector3 _pendingPopupPos;
        private float   _pendingPopupUntil = -1f;
        private const float PopupDebounceS = 0.05f;

        private void Start()
        {
            var bonuses = MetaUpgradeSystem.Instance?.ActiveBonuses;
            int baseCoins = LevelRunner.Instance?.CurrentLevel?.StartCoins ?? 120;
            int startCoins = baseCoins + (bonuses?.startCoinsBonus ?? 0);
            SetGold(startCoins);

            int pendingAchGold = PlayerPrefs.GetInt("cd.gold.pending", 0);
            if (pendingAchGold > 0)
            {
                PlayerPrefs.SetInt("cd.gold.pending", 0);
                PlayerPrefs.Save();
                AddGold(pendingAchGold);
                CrowdDefense.UI.Toast.Show("Achievement", $"+{pendingAchGold} or (succes)", 4000, "trophy", CrowdDefense.UI.ToastType.Achievement);
            }
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
            int effective = Mathf.RoundToInt(amount * SupportMode.GoldMultiplier);
            SetGold(Gold + effective);
            LifetimeStats.Instance?.AddGold(amount);
            Achievements.Instance?.TrackEvent("gold_earned", amount);
        }

        // Called by Enemy.Die after EnemyKilledEvent has been published (so ComboSystem
        // has already updated its ActiveMultiplier for this kill).
        public void AddGoldFromKill(int baseReward)
        {
            float comboMul      = ComboSystem.Instance?.ActiveMultiplier ?? 1f;
            float metaCoinMul   = MetaUpgradeSystem.Instance?.ActiveBonuses.coinGainMul ?? 1f;
            float endlessGoldMul = WaveManager.Instance?.EndlessGoldMul ?? 1f;
            int finalReward = Mathf.Max(1, Mathf.RoundToInt(baseReward * comboMul * metaCoinMul * endlessGoldMul * TalentSystem.GoldIncomeMul));
            AddGold(finalReward);
        }

        // Overload with world position — spawns tiered gold popup above kill site (debounced).
        public void AddGoldFromKill(int baseReward, Vector3 worldPos)
        {
            float comboMul      = ComboSystem.Instance?.ActiveMultiplier ?? 1f;
            float metaCoinMul   = MetaUpgradeSystem.Instance?.ActiveBonuses.coinGainMul ?? 1f;
            float endlessGoldMul = WaveManager.Instance?.EndlessGoldMul ?? 1f;
            int finalReward = Mathf.Max(1, Mathf.RoundToInt(baseReward * comboMul * metaCoinMul * endlessGoldMul * TalentSystem.GoldIncomeMul));
            AddGold(finalReward);
            AccumulateGoldPopup(finalReward, worldPos);
        }

        // Accumulates gold gains occurring within PopupDebounceS into one popup.
        private void AccumulateGoldPopup(int amount, Vector3 worldPos)
        {
            if (Time.timeScale <= 0f) return;
            _pendingPopupGold += amount;
            // Use latest position (closest kill dominates)
            _pendingPopupPos  = worldPos;
            if (_pendingPopupUntil < 0f)
            {
                _pendingPopupUntil = Time.unscaledTime + PopupDebounceS;
                StartCoroutine(FlushGoldPopup());
            }
        }

        private System.Collections.IEnumerator FlushGoldPopup()
        {
            // Wait until real-time debounce window closes (unscaled — works during slow-mo)
            while (Time.unscaledTime < _pendingPopupUntil)
                yield return null;

            int   total = _pendingPopupGold;
            var   pos   = _pendingPopupPos;
            _pendingPopupGold  = 0;
            _pendingPopupUntil = -1f;

            CrowdDefense.UI.FloatingPopupController.Instance?.SpawnGoldReward(total, pos);
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

        // Moves amount into the bank pool (does not affect Gold).
        public void TransferToBank(int amount)
        {
            if (amount <= 0) return;
            Bank += amount;
        }

        // Withdraws amount from Bank into Gold.  Clamps to available balance.
        public void WithdrawFromBank(int amount)
        {
            if (amount <= 0) return;
            int actual = Mathf.Min(amount, Bank);
            if (actual <= 0) return;
            Bank -= actual;
            AddGold(actual);
        }

        // Called by WaveManager when a wave is cleared
        public void ProcessInterestBank()
        {
            if (CastleDamagedThisWave)
            {
                Bank = 0;
                TotalBankAccumulated = 0;
                OnBankTick?.Invoke(0, 0);
#if UNITY_EDITOR
                Debug.Log("[Economy] Interest bank RESET — castle took damage this wave");
#endif
            }
            else
            {
                // D1-01 §3.5 V4: +5% of current gold, capped at BankInterestGainCap per wave
                var cfg = BalanceConfig.Get();
                int gain = Mathf.Min(Mathf.RoundToInt(Gold * cfg.BankInterestRate), cfg.BankInterestGainCap);
                if (gain > 0)
                {
                    TransferToBank(gain);
                    TotalBankAccumulated += gain;
                    // Pay out immediately: bank → gold
                    WithdrawFromBank(gain);
                    OnBankTick?.Invoke(gain, TotalBankAccumulated);

                    Vector3 popupPos = Castle.Instance != null
                        ? Castle.Instance.transform.position + Vector3.up * 1.5f
                        : Vector3.up * 1.5f;
                    CrowdDefense.UI.FloatingPopupController.Instance?.SpawnBankInterest(gain, popupPos);
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
