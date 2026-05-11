#nullable enable
using System;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    public class DoctrineSystem : MonoSingleton<DoctrineSystem>
    {
        // Persisted independently of SaveSystem to avoid coupling with its field set.
        private const string DOCTRINE_KEY = "cd_active_doctrine_v1";
        private const string GEMS_KEY     = "cd_gems_v1";

        public DoctrineDef? ActiveDoctrine { get; private set; }

        public event Action<DoctrineDef?>? OnDoctrineChanged;

        protected override void OnAwakeSingleton()
        {
            string saved = PlayerPrefs.GetString(DOCTRINE_KEY, "");
            if (!string.IsNullOrEmpty(saved))
                ActiveDoctrine = DoctrineRegistry.Get().Find(saved);
        }

        public int GetGems() => PlayerPrefs.GetInt(GEMS_KEY, 0);

        public void AddGems(int amount)
        {
            if (amount <= 0) return;
            PlayerPrefs.SetInt(GEMS_KEY, GetGems() + amount);
            PlayerPrefs.Save();
        }

        public bool TryActivate(string doctrineId, out int gemsAfter)
        {
            int gems = GetGems();
            gemsAfter = gems;
            var def = DoctrineRegistry.Get().Find(doctrineId);
            if (def == null) return false;
            if (gems < def.gemCost) return false;

            gemsAfter = gems - def.gemCost;
            PlayerPrefs.SetInt(GEMS_KEY, gemsAfter);
            PlayerPrefs.SetString(DOCTRINE_KEY, doctrineId);
            PlayerPrefs.Save();

            ActiveDoctrine = def;
            OnDoctrineChanged?.Invoke(ActiveDoctrine);
            return true;
        }

        public void Deactivate()
        {
            ActiveDoctrine = null;
            PlayerPrefs.SetString(DOCTRINE_KEY, "");
            PlayerPrefs.Save();
            OnDoctrineChanged?.Invoke(null);
        }

        // Returns a BalanceConfig clone with doctrine modifiers applied — call at run start.
        public BalanceConfig BuildRunConfig(BalanceConfig source)
        {
            var clone = Instantiate(source);
            if (ActiveDoctrine == null) return clone;
            foreach (var mod in ActiveDoctrine.modifiers)
                ApplyModifier(clone, mod);
            return clone;
        }

        private static void ApplyModifier(BalanceConfig cfg, DoctrineModifier mod)
        {
            switch (mod.key)
            {
                case "TowerDamageMul":      cfg.TowerDamageMul      *= mod.value; break;
                case "SwarmMul":            cfg.SwarmMul            *= mod.value; break;
                case "MagnetRange":         cfg.MagnetRange         *= mod.value; break;
                case "MagnetCoinMul":       cfg.MagnetCoinMul       *= mod.value; break;
                case "BankInterestRate":    cfg.BankInterestRate    *= mod.value; break;
                case "SkipBonusGold":       cfg.SkipBonusGold       = Mathf.RoundToInt(cfg.SkipBonusGold * mod.value); break;
                case "StreakBonusPerWave":  cfg.StreakBonusPerWave  *= mod.value; break;
                case "SellRefundRatio":     cfg.SellRefundRatio     *= mod.value; break;
                case "CastleHPBase":        cfg.CastleHPBase        *= mod.value; break;
                default:
#if UNITY_EDITOR
                    Debug.LogWarning($"[DoctrineSystem] Unknown modifier key '{mod.key}' — ignored.");
#endif
                    break;
            }
        }

        public float GetModifierValue(string key)
        {
            if (ActiveDoctrine == null) return 1f;
            foreach (var mod in ActiveDoctrine.modifiers)
                if (mod.key == key) return mod.value;
            return 1f;
        }
    }
}
