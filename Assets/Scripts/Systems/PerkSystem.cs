#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.UI;
using CrowdDefense.Visual;

namespace CrowdDefense.Systems
{
    public class PerkSystem : MonoSingleton<PerkSystem>
    {
        // Legendary perk — runtime-only instance, never stored as SO asset.
        private PerkDef? _legendaryPerk;

        private PerkRegistry? _registry;

        // Schools picked by the player at run start (ids). Empty = no filter (all perks available).
        private readonly List<string> _pickedSchools = new();

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
            var tag = SchoolToTag(ParseSchool(schoolId));
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

        // ── School filter (set once at run start) ────────────────────────────

        public void SetPickedSchools(IEnumerable<string> schoolIds)
        {
            _pickedSchools.Clear();
            _pickedSchools.AddRange(schoolIds);
        }

        // ── Roll (V5 rollPerkChoices port) ────────────────────────────────────

        public List<PerkDef> RollChoices(Hero hero, int count, int levelUpsLeft, string schoolId)
        {
            if (_registry == null) return new List<PerkDef>();

            var basePool = new List<PerkDef>();
            foreach (var p in _registry.Standard)
            {
                // If player picked schools, only include perks that belong to a picked school
                // (perks with empty school string are always included — generic pool)
                if (_pickedSchools.Count > 0 && !string.IsNullOrEmpty(p.school) && !_pickedSchools.Contains(p.school))
                    continue;
                basePool.Add(p);
            }
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

        // ── Legendary (all achievements unlocked) ────────────────────────────

        public void UnlockLegendaryPerk()
        {
            var hero = LevelRunner.Instance?.Hero;
            if (hero == null) return;

            _legendaryPerk ??= CreateLegendaryDef();
            if (!CanApply(hero, _legendaryPerk)) return;

            var tagCounts = BuildTagCounts(hero);
            ApplyOne(hero, _legendaryPerk, tagCounts);
        }

        private static PerkDef CreateLegendaryDef()
        {
            var def = ScriptableObject.CreateInstance<PerkDef>();
            def.id          = "legendary";
            def.nameKey     = "Legendaire";
            def.descKey     = "Crit +20%  Gains x2";
            def.iconEmoji   = "trophy";
            def.rarity      = PerkRarity.Legendary;
            def.critChance  = 0.20f;
            def.coinGain    = 1.00f;   // +100% = double coin gain
            def.stackable   = false;
            def.maxStacks   = 1;
            return def;
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
                var cfg = BalanceConfig.Get();
                hero.TowerAuraRange = def.towerAuraRange > 0f ? def.towerAuraRange : cfg.DefaultTowerAuraRange;
            }
            if (def.combustion)     hero.Combustion     = true;
            if (def.pyromancie)     hero.Pyromancie     = true;
            if (def.glaciation)     hero.Glaciation     = true;
            if (def.cristalGlace)   hero.CristalGlace   = true;
            if (def.mursPierre)     hero.MursPierre     = true;
            if (def.forteressePerk) { hero.ForteressePerk = true; hero.CastleHPMaxMul *= BalanceConfig.Get().ForteresseCastleHpMul; }

            // Magnet perk (D1-01 Q3): boosts coin pull range + fly speed via CoinPullManager
            if (def.magnetRangeMul  > 1f) CoinPullManager.Instance?.ApplyMagnetRangeMul(def.magnetRangeMul);
            if (def.coinFlySpeedMul > 1f) CoinPullManager.Instance?.ApplyCoinFlySpeedMul(def.coinFlySpeedMul);

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

            string perkName = !string.IsNullOrEmpty(def.displayName) ? def.displayName : def.id;
            Toast.Show("Perk Acquired", perkName, 3000, null, ToastType.Perk);

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

        private static School ParseSchool(string schoolId) => schoolId switch
        {
            "elementaire" => School.Elementaire,
            "mecanique"   => School.Mecanique,
            "mystique"    => School.Mystique,
            "bestiaire"   => School.Bestiaire,
            "strategie"   => School.Strategie,
            _             => School.None,
        };

        private static PerkTag SchoolToTag(School school) => school switch
        {
            School.Elementaire => PerkTag.Feu,
            School.Mecanique   => PerkTag.Pierre,
            School.Mystique    => PerkTag.Vide,
            School.Bestiaire   => PerkTag.Sang,
            School.Strategie   => PerkTag.Or,
            _                  => PerkTag.None,
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
