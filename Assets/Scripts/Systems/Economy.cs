#nullable enable
using System;
using UnityEngine;

namespace CrowdDefense.Systems
{
    public class Economy : MonoBehaviour
    {
        public static Economy? Instance { get; private set; }

        public int Gold { get; private set; }
        public event Action<int>? OnGoldChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

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

        private void SetGold(int v)
        {
            Gold = v;
            OnGoldChanged?.Invoke(Gold);
        }
    }
}
