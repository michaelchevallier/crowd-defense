#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Singleton MonoBehaviour — resolves all tower synergies every ~200 ms (LateUpdate tick).
    /// Resets synergy output fields on every Tower before recomputing, preventing stale accumulation.
    /// Covers all 15 synergies ported from Phaser Synergies.js.
    /// </summary>
    public class Synergies : MonoSingleton<Synergies>
    {
        private const float TickInterval = 0.2f;
        private float _tickTimer;

        private void LateUpdate()
        {
            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f) return;
            _tickTimer = TickInterval;

            if (PlacementController.Instance == null) return;
            var towers = PlacementController.Instance.PlacedTowers;
            var enemies = WaveManager.Instance?.ActiveEnemies;

            Resolve(towers, enemies);
        }

        // internal for unit tests
        internal void Resolve(IReadOnlyList<Tower> towers, IReadOnlyList<Enemy>? enemies)
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
                            ApplyCrossEffect(source, syn, towers);
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
        private static void ApplyCrossEffect(Tower source, SynergyDef syn, IReadOnlyList<Tower> towers)
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

            if (nearFrom == null) return;

            source._synergyActive = true;
            nearFrom._synergyActive = true;

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
