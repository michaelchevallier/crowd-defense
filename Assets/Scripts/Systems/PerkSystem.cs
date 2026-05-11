#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Visual;

namespace CrowdDefense.Systems
{
    public class PerkSystem : MonoSingleton<PerkSystem>
    {
        private const float ForteresseHPMul     = 1.5f;
        private const float DefaultTowerAuraRange = 8f;

        private PerkRegistry? _registry;

        public event Action<Hero, PerkDef>?         OnPerkApplied;
        public event Action<Hero, PerkSetBonusDef>? OnSetBonusActivated;

        protected override void OnAwakeSingleton()
        {
            _registry = PerkRegistry.Load();
#if UNITY_EDITOR
            if (_registry == null)
                Debug.LogWarning("[PerkSystem] Resources/PerkRegistry.asset manquant — perks inactifs");
#endif
        }

        // ── Bulk apply (used by ApplyRunContext on level load) ────────────────

        public void ApplyPerkList(Hero hero, IReadOnlyList<string> perkIds)
        {
            if (_registry == null) return;
            var tagCounts = new Dictionary<PerkTag, int>();
            foreach (var id in perkIds)
            {
                var def = _registry.Get(id);
                if (def == null) continue;
                ApplyOne(hero, def, tagCounts);
            }
        }

        // ── Single apply (level-up pick) ──────────────────────────────────────

        public void ApplyPerk(Hero hero, PerkDef def)
        {
            var tagCounts = BuildTagCounts(hero);
            ApplyOne(hero, def, tagCounts);
        }

        // ── School free set bonus (schoolId present in RunState) ─────────────

        public void ApplyFreeSetBonus(Hero hero, string schoolId)
        {
            if (_registry == null) return;
            var tag = SchoolToTag(schoolId);
            if (tag == PerkTag.None) return;
            var bonus = _registry.GetBonus(tag);
            if (bonus == null) return;
            ApplySetBonus(hero, bonus);
            OnSetBonusActivated?.Invoke(hero, bonus);
        }

        public bool CanApply(Hero hero, PerkDef def)
        {
            // Transforms are mutually exclusive (V5 hasTransform guard)
            if (def.transform)
            {
                foreach (var id in hero.Perks)
                {
                    var existing = _registry?.Get(id);
                    if (existing != null && existing.transform && existing.id != def.id) return false;
                }
            }
            if (!def.stackable && hero.Perks.Contains(def.id)) return false;
            if (def.maxStacks > 0)
            {
                int stacks = 0;
                foreach (var id in hero.Perks)
                    if (id == def.id) stacks++;
                return stacks < def.maxStacks;
            }
            return true;
        }

        // ── Roll (V5 rollPerkChoices port) ────────────────────────────────────

        public List<PerkDef> RollChoices(Hero hero, int count, int levelUpsLeft, string schoolId)
        {
            if (_registry == null) return new List<PerkDef>();

            var basePool = new List<PerkDef>(_registry.Standard);
            foreach (var sp in _registry.GetSchoolPerks(schoolId)) basePool.Add(sp);

            var available = new List<PerkDef>();
            foreach (var p in basePool)
            {
                if (CanApply(hero, p)) available.Add(p);
            }

            if (available.Count == 0) return new List<PerkDef>();

            bool hasTransform = false;
            foreach (var id in hero.Perks)
            {
                var def = _registry.Get(id);
                if (def != null && def.transform) { hasTransform = true; break; }
            }

            bool lastChance = levelUpsLeft <= 1;
            if (!hasTransform && lastChance)
            {
                var transforms = new List<PerkDef>();
                var others     = new List<PerkDef>();
                foreach (var p in available)
                {
                    if (p.transform) transforms.Add(p);
                    else             others.Add(p);
                }
                if (transforms.Count > 0)
                {
                    var guaranteed = transforms[UnityEngine.Random.Range(0, transforms.Count)];
                    FisherYates(others);
                    var result = new List<PerkDef> { guaranteed };
                    int fill = Mathf.Min(count - 1, others.Count);
                    for (int i = 0; i < fill; i++) result.Add(others[i]);
                    return result;
                }
            }

            FisherYates(available);
            int take = Mathf.Min(count, available.Count);
            return available.GetRange(0, take);
        }

        // ── Internals ─────────────────────────────────────────────────────────

        private void ApplyOne(Hero hero, PerkDef def, Dictionary<PerkTag, int> tagCounts)
        {
            hero.AddPerkId(def.id);

            if (def.range != 0f)       hero.RangeMul    *= 1f + def.range;
            if (def.fireRate != 0f)    hero.FireRateMul *= 1f - def.fireRate;
            if (def.damage != 0f)      hero.DamageMul   *= 1f + def.damage;
            if (def.moveSpeed != 0f)   hero.MoveSpeedMul *= 1f + def.moveSpeed;
            if (def.coinGain != 0f)    hero.CoinGainMul *= 1f + def.coinGain;
            hero.CritChance         += def.critChance;
            if (def.critMul > 0f)      hero.CritMul = def.critMul;
            if (def.critStaggerMs > 0) hero.CritStaggerMs = def.critStaggerMs;
            hero.MultiShot          += def.multiShot;
            hero.PierceCount        += def.pierceCount;
            hero.Lifesteal          += def.lifesteal;
            hero.WaveRegen          += def.waveRegen;
            hero.MoveAttackPierceBonus += def.moveAttackPierceBonus;

            if (def.fireball)
            {
                hero.Fireball = true;
                hero.FireballRadius = def.fireballRadius;
                hero.FireballDmgMul = def.fireballDmgMul;
            }
            if (def.ricochet)
            {
                hero.Ricochet = true;
                hero.RicochetBounces = def.ricochetBounces;
                hero.RicochetDecay   = def.ricochetDecay;
            }
            if (def.lightning)
            {
                hero.Lightning = true;
                hero.LightningTargets = def.lightningTargets;
                hero.LightningDmgMul  = def.lightningDmgMul;
            }
            if (def.pierceExplode)
            {
                hero.PierceExplode = true;
                hero.PierceExplodeRadius = def.pierceExplodeRadius;
                hero.PierceExplodeDmgMul = def.pierceExplodeDmgMul;
            }
            if (def.towerCostMul != 1f)       hero.TowerCostMul *= def.towerCostMul;
            if (def.firstTowerFree)           { hero.FirstTowerFree = true; hero.FirstTowerFreeUsed = false; }
            if (def.towerFireRateAura != 1f)
            {
                hero.TowerFireRateAuraMul = def.towerFireRateAura;
                hero.TowerAuraRange = def.towerAuraRange > 0f ? def.towerAuraRange : 8f;
            }
            if (def.combustion)     hero.Combustion = true;
            if (def.pyromancie)     hero.Pyromancie = true;
            if (def.glaciation)     hero.Glaciation = true;
            if (def.forteressePerk) hero.CastleHPMaxMul *= ForteresseHPMul;

            // Downsides
            if (def.downRange != 0f)      hero.RangeMul    *= 1f + def.downRange;
            if (def.downDamage != 0f)     hero.DamageMul   *= 1f + def.downDamage;
            if (def.downFireRate != 0f)   hero.FireRateMul *= 1f - def.downFireRate;
            if (def.downCoinReward != 0f) hero.CoinRewardMul *= 1f + def.downCoinReward;

            // Set bonus tracking
            if (def.tag != PerkTag.None)
            {
                tagCounts.TryGetValue(def.tag, out int c);
                tagCounts[def.tag] = ++c;
                var bonus = _registry?.GetBonus(def.tag);
                if (bonus != null && c == bonus.threshold)
                {
                    ApplySetBonus(hero, bonus);
                    OnSetBonusActivated?.Invoke(hero, bonus);
                }
            }

            OnPerkApplied?.Invoke(hero, def);

            AudioController.Instance?.Play("perk_pick", 0.9f);
            JuiceFX.Instance?.Flash(new Color(1f, 0.84f, 0f, 0.35f), 200);
        }

        private void ApplySetBonus(Hero hero, PerkSetBonusDef b)
        {
            hero.CritChance      += b.addCritChance;
            hero.Lifesteal       += b.addLifesteal;
            hero.CastleHPMaxMul  *= b.castleHPMaxMul;
            hero.FireRateMul     *= b.fireRateMul;
            hero.CoinGainMul     *= b.coinGainMul;
            if (b.aoeOnNthProjectile > 0)
                hero.AoeOnNthProjectile = b.aoeOnNthProjectile;

            AudioController.Instance?.Play("set_bonus", 1f);
        }

        private Dictionary<PerkTag, int> BuildTagCounts(Hero hero)
        {
            var counts = new Dictionary<PerkTag, int>();
            if (_registry == null) return counts;
            foreach (var id in hero.Perks)
            {
                var def = _registry.Get(id);
                if (def == null || def.tag == PerkTag.None) continue;
                counts.TryGetValue(def.tag, out int c);
                counts[def.tag] = c + 1;
            }
            return counts;
        }

        private static PerkTag SchoolToTag(string schoolId) => schoolId switch
        {
            "feu"        => PerkTag.Feu,
            "givre"      => PerkTag.Vide,
            "maconnerie" => PerkTag.Pierre,
            _            => PerkTag.None,
        };

        private static void FisherYates<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
