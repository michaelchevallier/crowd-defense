#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    // Computed bonuses applied at run start from all purchased MetaUpgrades.
    // Clone-and-apply pattern: BalanceConfig SO is NOT mutated.
    public class RunBonuses
    {
        public float castleHPMul = 1f;
        public float heroDamageMul = 1f;
        public int startCoinsBonus = 0;
        public float xpMul = 1f;
        public float heroRangeMul = 1f;
        public float coinGainMul = 1f;
        public int perkChoiceCountBonus = 0;
        public float heroFireRateMul = 1f;
        public float gemGainMul = 1f;
        public float towerUpgradeDiscount = 0f;
    }

    [DefaultExecutionOrder(-50)]
    public class MetaUpgradeSystem : MonoSingleton<MetaUpgradeSystem>
    {
        public RunBonuses ActiveBonuses { get; private set; } = new();

        protected override void OnAwakeSingleton() => ComputeBonuses();

        private void Start()
        {
            // Apply castleHPMul once the level is fully set up (Castle.Init already ran).
            // LevelEvents.OnLevelStart fires from LevelRunner.Start() after SpawnCastle().
            LevelEvents.OnLevelStart += HandleLevelStart;
        }

        protected override void OnDestroySingleton()
        {
            LevelEvents.OnLevelStart -= HandleLevelStart;
        }

        private void HandleLevelStart(LevelData _, UnityEngine.Bounds __)
        {
            var castle = Castle.Instance;
            if (castle == null) return;
            float mul = ActiveBonuses.castleHPMul;
            if (mul <= 1f) return;
            int bonus = Mathf.RoundToInt(castle.HPMax * (mul - 1f));
            if (bonus > 0) castle.GrantBonusHP(bonus);
        }

        // Called once at run start (also called after shop purchase for immediate preview)
        public void ComputeBonuses()
        {
            var registry = MetaUpgradeRegistry.Get();
            var bonuses = new RunBonuses();
            if (registry == null) { ActiveBonuses = bonuses; return; }

            foreach (var def in registry.All)
            {
                int lvl = SaveSystem.GetMetaUpgradeLevel(def.id);
                if (lvl <= 0) continue;
                ApplyDef(def, lvl, bonuses);
            }

            ActiveBonuses = bonuses;
        }

        private static void ApplyDef(MetaUpgradeDef def, int lvl, RunBonuses b)
        {
            foreach (var fx in def.effects)
            {
                if (!System.Enum.TryParse<MetaUpgradeEffectKey>(fx.key, true, out var key))
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[MetaUpgradeSystem] Unknown effect key '{fx.key}' in {def.id}");
#endif
                    continue;
                }
                float v = fx.valuePerLevel * lvl;
                switch (key)
                {
                    case MetaUpgradeEffectKey.CastleHPMul:          b.castleHPMul         *= 1f + v; break;
                    case MetaUpgradeEffectKey.HeroDamageMul:        b.heroDamageMul       *= 1f + v; break;
                    case MetaUpgradeEffectKey.StartCoinsBonus:      b.startCoinsBonus     += Mathf.RoundToInt(v); break;
                    case MetaUpgradeEffectKey.XpMul:                b.xpMul               *= 1f + v; break;
                    case MetaUpgradeEffectKey.HeroRangeMul:         b.heroRangeMul        *= 1f + v; break;
                    case MetaUpgradeEffectKey.CoinGainMul:          b.coinGainMul         *= 1f + v; break;
                    case MetaUpgradeEffectKey.PerkChoiceCountBonus: b.perkChoiceCountBonus+= Mathf.RoundToInt(v); break;
                    case MetaUpgradeEffectKey.HeroFireRateMul:      b.heroFireRateMul     *= 1f + v; break;
                    case MetaUpgradeEffectKey.GemGainMul:           b.gemGainMul          *= 1f + v; break;
                    case MetaUpgradeEffectKey.TowerUpgradeDiscount:
                        b.towerUpgradeDiscount = Mathf.Max(b.towerUpgradeDiscount, v);
                        break;
                }
            }
        }
    }
}
