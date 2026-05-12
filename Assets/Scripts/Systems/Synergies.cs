#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    public readonly struct SynergyBadge
    {
        public readonly string TowerId;
        public readonly int Count;
        public SynergyBadge(string towerId, int count) { TowerId = towerId; Count = count; }
    }

    public readonly struct SynergyActivatedInfo
    {
        public readonly string FromType;
        public readonly string ToType;
        public readonly string Label;
        public readonly Vector3 MidPoint;
        public SynergyActivatedInfo(string from, string to, Vector3 mid)
        {
            FromType = from;
            ToType   = to;
            Label    = $"{from}+{to}";
            MidPoint = mid;
        }
    }

    /// <summary>
    /// Singleton MonoBehaviour — resolves all tower synergies every ~200 ms (LateUpdate tick).
    /// Resets synergy output fields on every Tower before recomputing, preventing stale accumulation.
    /// Covers all 15 synergies ported from Phaser Synergies.js.
    /// </summary>
    public class Synergies : MonoSingleton<Synergies>
    {
        private const float TickInterval = 0.2f;
        private float _tickTimer;
        private bool _dirty = true;
        private float _recomputeTimer = 0f;

        // Fired after each Resolve tick when active-synergy set changes.
        public event Action? OnSynergyChanged;

        // Fired once per cross-effect pair the first time it becomes active.
        public event Action<SynergyActivatedInfo>? OnSynergyActivated;

        // Read-only snapshot updated each tick — one badge per tower type that has synergyActive towers.
        public IReadOnlyList<SynergyBadge> ActiveBadges => _activeBadges;
        private readonly List<SynergyBadge> _activeBadges = new();
        private readonly List<SynergyBadge> _prevBadges = new();
        private readonly Dictionary<string, int> _countMap = new();

        // cross-effect activation state: key = "sourceInstanceId:syn.from" → currently active?
        // Allows firing OnSynergyActivated exactly once per pair becoming active.
        private readonly Dictionary<string, bool> _crossActiveKeys = new();

        public void MarkDirty() => _dirty = true;

        private void LateUpdate()
        {
            if (!_dirty || Time.time < _recomputeTimer)
                return;

            if (PlacementController.Instance == null) return;
            var towers = PlacementController.Instance.PlacedTowers;
            var enemies = WaveManager.Instance?.ActiveEnemies;

            Resolve(towers, enemies, this);
            UpdateBadges(towers);
            _dirty = false;
            _recomputeTimer = Time.time + TickInterval;
        }

        private void UpdateBadges(IReadOnlyList<Tower> towers)
        {
            _countMap.Clear();
            for (int i = 0; i < towers.Count; i++)
            {
                var t = towers[i];
                if (t == null || !t._synergyActive || t.Config == null) continue;
                var id = t.Config.Id;
                if (string.IsNullOrEmpty(id)) continue;
                _countMap.TryGetValue(id, out int c);
                _countMap[id] = c + 1;
            }

            _prevBadges.Clear();
            _prevBadges.AddRange(_activeBadges);
            _activeBadges.Clear();
            foreach (var kv in _countMap)
                _activeBadges.Add(new SynergyBadge(kv.Key, kv.Value));

            if (!BadgesEqual(_prevBadges, _activeBadges))
                OnSynergyChanged?.Invoke();
        }

        private static bool BadgesEqual(List<SynergyBadge> a, List<SynergyBadge> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i].TowerId != b[i].TowerId || a[i].Count != b[i].Count) return false;
            return true;
        }

        // internal for unit tests
        internal void Resolve(IReadOnlyList<Tower> towers, IReadOnlyList<Enemy>? enemies, Synergies? owner = null)
        {
            // ── 1. Reset all synergy output fields ────────────────────────────
            for (int i = 0; i < towers.Count; i++)
            {
                var t = towers[i];
                if (t == null) continue;
                t._buffMul            = 1f;
                t._pierceBonus        = 0;
                t._multiShotBonus     = 0;
                t._flyerDmgBonus      = 1f;
                t._slowOnHitActive    = false;
                t._slowOnHitMul       = 1f;
                t._slowOnHitDurMs     = 0;
                t._appliesSlowActive  = false;
                t._appliesSlowMul     = 1f;
                t._appliesSlowDurMs   = 0;
                t._propagateAoEActive = false;
                t._propagateAoERadius = 0f;
                t._propagateAoEDmg    = 0f;
                t._cascadeRadius      = 0f;
                t._knockbackOnHit     = 0f;
                t._pullActive         = false;
                t._propagateDebuff    = false;
                t._freezeOnHitActive  = false;
                t._freezeDurMs        = 0;
                t._synergyActive      = false;
            }

            // ── 2. Iterate synergy declarations on each tower ─────────────────
            for (int i = 0; i < towers.Count; i++)
            {
                var source = towers[i];
                if (source == null) continue;
                var cfg = source.Config;
                if (cfg == null) continue;

                var syns = cfg.Synergies;
                if (syns == null || syns.Count == 0)
                {
                    // Fallback : portal behavior buffAura (no synergy declared)
                    if (cfg.Behavior == TowerBehavior.BuffAura)
                        ApplyFallbackBuffAura(source, cfg, towers);
                    continue;
                }

                for (int s = 0; s < syns.Count; s++)
                {
                    var syn = syns[s];
                    switch (syn.type)
                    {
                        case SynergyType.Aura:
                            ApplyAura(source, syn, towers);
                            break;
                        case SynergyType.CrossEffect:
                            ApplyCrossEffect(source, syn, towers, owner);
                            break;
                        case SynergyType.ApplyToEnemy:
                            if (enemies != null)
                                ApplyToEnemies(source, syn, enemies);
                            break;
                        case SynergyType.Passive:
                            RegisterPassive(source, syn);
                            break;
                    }
                }
            }

            // ── 3. Apply pull-to-tank after all cross-effects resolved ────────
            if (enemies != null)
                ApplyPullActive(towers, enemies);

            // ── 4. Apply Hero aura buff to nearby towers ──────────────────────
            ApplyHeroAuraBuff(towers);
        }

        // ── Aura ──────────────────────────────────────────────────────────────
        private static void ApplyAura(Tower source, SynergyDef syn, IReadOnlyList<Tower> towers)
        {
            float r = syn.range > 0f ? syn.range : (source.Config?.Range ?? 4f);
            float r2 = r * r;
            Vector3 srcPos = source.transform.position;

            for (int i = 0; i < towers.Count; i++)
            {
                var t = towers[i];
                if (t == null || t == source) continue;

                // Filter : hasPierceOrAoe
                if (syn.filterPierceOrAoe != 0)
                {
                    var tc = t.Config;
                    bool hasPierceOrAoe = tc != null && (tc.Pierce > 0 || tc.Aoe > 0f);
                    if (syn.filterPierceOrAoe == 1 && !hasPierceOrAoe) continue;
                    if (syn.filterPierceOrAoe == -1 && hasPierceOrAoe) continue;
                }

                if ((t.transform.position - srcPos).sqrMagnitude > r2) continue;

                if (syn.dmgMul > 0f)
                    t._buffMul = Mathf.Max(t._buffMul, syn.dmgMul);
                if (syn.pierceBonus > 0 || syn.pierceMega)
                    t._pierceBonus = Mathf.Max(t._pierceBonus, syn.pierceMega ? 99 : syn.pierceBonus);

                t._synergyActive  = true;
                source._synergyActive = true;
            }
        }

        // ── CrossEffect ───────────────────────────────────────────────────────
        private static void ApplyCrossEffect(Tower source, SynergyDef syn, IReadOnlyList<Tower> towers, Synergies? owner)
        {
            if (string.IsNullOrEmpty(syn.from)) return;
            float r = syn.range > 0f ? syn.range : 4f;
            float r2 = r * r;
            Vector3 srcPos = source.transform.position;

            Tower? nearFrom = null;
            for (int i = 0; i < towers.Count; i++)
            {
                var t = towers[i];
                if (t == null || t == source) continue;
                if (t.Config?.Id != syn.from) continue;
                if ((t.transform.position - srcPos).sqrMagnitude < r2)
                {
                    nearFrom = t;
                    break;
                }
            }

            // Track first-activation per (source instance, syn.from) pair
            string pairKey = $"{source.GetInstanceID()}:{syn.from}";
            bool wasActive = owner != null && owner._crossActiveKeys.TryGetValue(pairKey, out bool prev) && prev;

            if (nearFrom == null)
            {
                if (owner != null) owner._crossActiveKeys[pairKey] = false;
                return;
            }

            source._synergyActive = true;
            nearFrom._synergyActive = true;

            // Fire first-activation event exactly once per pair becoming active
            if (!wasActive && owner != null)
            {
                owner._crossActiveKeys[pairKey] = true;
                var fromId  = nearFrom.Config?.Id ?? syn.from;
                var sourceId = source.Config?.Id ?? "?";
                Vector3 mid = (srcPos + nearFrom.transform.position) * 0.5f;
                owner.OnSynergyActivated?.Invoke(new SynergyActivatedInfo(fromId, sourceId, mid));
                Achievements.Instance?.TrackEvent("synergy_activated", 1);
            }
            else if (owner != null)
            {
                owner._crossActiveKeys[pairKey] = true;
            }

            if (syn.pierceMega)
                source._pierceBonus = Mathf.Max(source._pierceBonus, 99);
            if (syn.pierceBonus > 0)
                source._pierceBonus = Mathf.Max(source._pierceBonus, syn.pierceBonus);
            if (syn.multiShotBonus > 0)
                source._multiShotBonus = Mathf.Max(source._multiShotBonus, syn.multiShotBonus);
            if (syn.flyerDmgBonus > 1f)
                source._flyerDmgBonus = Mathf.Max(source._flyerDmgBonus, syn.flyerDmgBonus);
            if (syn.cascadeRadius > 0f)
                source._cascadeRadius = Mathf.Max(source._cascadeRadius, syn.cascadeRadius);
            if (syn.knockbackOnHit > 0f)
                source._knockbackOnHit = Mathf.Max(source._knockbackOnHit, syn.knockbackOnHit);
            if (syn.pullToTank)
                nearFrom._pullActive = true;
            if (syn.propagateDebuff)
                source._propagateDebuff = true;

            if (syn.freezeOnHit)
            {
                source._freezeOnHitActive = true;
                source._freezeDurMs = Mathf.Max(source._freezeDurMs, syn.freezeDurMs > 0 ? syn.freezeDurMs : 800);
            }

            if (syn.slowOnHit.durMs > 0 || syn.slowOnHit.mul > 0f)
            {
                source._slowOnHitActive = true;
                source._slowOnHitMul    = syn.slowOnHit.mul > 0f ? syn.slowOnHit.mul : 0.5f;
                source._slowOnHitDurMs  = syn.slowOnHit.durMs > 0 ? syn.slowOnHit.durMs : 2000;
            }

            if (syn.appliesSlow.durMs > 0 || syn.appliesSlow.mul > 0f)
            {
                source._appliesSlowActive = true;
                source._appliesSlowMul    = syn.appliesSlow.mul > 0f ? syn.appliesSlow.mul : 0.7f;
                source._appliesSlowDurMs  = syn.appliesSlow.durMs > 0 ? syn.appliesSlow.durMs : 1500;
            }

            if (syn.propagateAoE.radius > 0f || syn.propagateAoE.dmg > 0f)
            {
                source._propagateAoEActive = true;
                source._propagateAoERadius = syn.propagateAoE.radius;
                source._propagateAoEDmg    = syn.propagateAoE.dmg;
            }
        }

        // ── ApplyToEnemy ──────────────────────────────────────────────────────
        private static void ApplyToEnemies(Tower source, SynergyDef syn, IReadOnlyList<Enemy> enemies)
        {
            if (SlowEffectManager.Instance == null) return;
            float r = syn.range > 0f ? syn.range : (source.Config?.Range ?? 4f);
            float r2 = r * r;
            Vector3 srcPos = source.transform.position;

            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - srcPos).sqrMagnitude > r2) continue;

                if (syn.slowArea.durMs > 0 || syn.slowArea.mul > 0f)
                {
                    float mul = syn.slowArea.mul > 0f ? syn.slowArea.mul : 0.5f;
                    int dur = syn.slowArea.durMs > 0 ? syn.slowArea.durMs : 4000;
                    SlowEffectManager.Instance.ApplySlow(e, mul, dur);
                }
            }

            source._synergyActive = true;
        }

        // ── Passive ───────────────────────────────────────────────────────────
        // coinMul passive : Tower.UpdateCoinPull (Update) gère déjà CoinPullManager.
        // Synergies marque simplement _synergyActive pour le retour visuel.
        private static void RegisterPassive(Tower source, SynergyDef syn)
        {
            if (syn.coinMul > 0f)
                source._synergyActive = true;
        }

        // ── Fallback BuffAura (Portal sans synergy déclarée) ─────────────────
        private static void ApplyFallbackBuffAura(Tower source, TowerType cfg, IReadOnlyList<Tower> towers)
        {
            float rangeSq = cfg.Range * cfg.Range;
            Vector3 srcPos = source.transform.position;
            for (int i = 0; i < towers.Count; i++)
            {
                var t = towers[i];
                if (t == null || t == source) continue;
                if ((t.transform.position - srcPos).sqrMagnitude > rangeSq) continue;
                t._buffMul = Mathf.Max(t._buffMul, cfg.BuffMul);
                t._synergyActive  = true;
                source._synergyActive = true;
            }
        }

        // ── Hero aura buff on nearby towers ───────────────────────────────────
        private static void ApplyHeroAuraBuff(IReadOnlyList<Tower> towers)
        {
            var hero = LevelRunner.Instance?.Hero;
            if (hero == null)
            {
                for (int i = 0; i < towers.Count; i++)
                    towers[i]?.ClearHeroBuff();
                return;
            }

            var (dmgMul, _, auraRange) = hero.GetTowerAuraBuffs();
            float r2 = auraRange * auraRange;
            Vector3 heroPos = hero.transform.position;

            for (int i = 0; i < towers.Count; i++)
            {
                var t = towers[i];
                if (t == null) continue;
                if ((t.transform.position - heroPos).sqrMagnitude <= r2)
                    t.ApplyHeroBuff(dmgMul);
                else
                    t.ClearHeroBuff();
            }
        }

        // ── Pull to tank (magnet+tank cross-effect consequence) ───────────────
        private static void ApplyPullActive(IReadOnlyList<Tower> towers, IReadOnlyList<Enemy> enemies)
        {
            for (int ti = 0; ti < towers.Count; ti++)
            {
                var tank = towers[ti];
                if (tank == null || !tank._pullActive) continue;
                if (tank.Config == null) continue;

                float rangeSq = tank.Config.Range * tank.Config.Range;
                Vector3 tankPos = tank.transform.position;

                for (int ei = 0; ei < enemies.Count; ei++)
                {
                    var e = enemies[ei];
                    if (e == null || e.IsDead) continue;
                    if ((e.transform.position - tankPos).sqrMagnitude > rangeSq) continue;
                    // Nudge enemy slightly toward tank by reducing its waypoint progress
                    // Uses currentSpeedMul as a proxy pull (reduce forward speed by 0.5%)
                    e.currentSpeedMul = Mathf.Max(0.05f, e.currentSpeedMul * 0.995f);
                }
            }
        }
    }
}
