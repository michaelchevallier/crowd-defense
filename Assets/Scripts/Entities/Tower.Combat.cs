#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;
using CrowdDefense.UI;

namespace CrowdDefense.Entities
{
    public partial class Tower
    {
        private void UpdateAttack()
        {
            cooldown -= Time.deltaTime;

            if (target == null || target.IsDead || OutOfRange(target))
            {
                target?.SetTargetedBy(false);
                target = AcquireTarget();
                target?.SetTargetedBy(true);
            }

            if (target != null && cooldown <= 0f)
            {
                ExecuteFire(target);
                float rateMs = cfg!.FireRateMs * L3FireRateMul * ResearchFireRateMul;
                cooldown = rateMs / 1000f;
            }
        }

        private bool OutOfRange(Enemy e)
        {
            if (cfg == null || e == null) return true;
            float r = cfg.Range * EventRangeMul;
            return (e.transform.position - transform.position).sqrMagnitude > r * r;
        }

        private Enemy? AcquireTarget()
        {
            if (cfg == null || WaveManager.Instance == null) return null;
            float effRange = cfg.Range * ResearchRangeMul * EventRangeMul;
            float rangeSq = effRange * effRange;
            Enemy? best = null;
            float bestScore = float.MinValue;
            var enemies = WaveManager.Instance.ActiveEnemies;
            Vector3 myPos = transform.position;

            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                float distSq = (e.transform.position - myPos).sqrMagnitude;
                if (distSq > rangeSq) continue;

                if (cfg.FlyerOnly && !e.IsFlyer) continue;
                if (e.IsFlyer && !cfg.FlyerOnly && !cfg.CanHitFlyers) continue;
                if (e.StealthAlpha < 0.4f) continue;

                if (_guardMode == GuardMode.AirOnly    && !e.IsFlyer) continue;
                if (_guardMode == GuardMode.GroundOnly &&  e.IsFlyer) continue;

                float score;
                if (e.IsFlyer)
                {
                    float castleDstSq = Castle.Instance != null
                        ? (e.transform.position - Castle.Instance.transform.position).sqrMagnitude
                        : float.MaxValue;
                    score = -castleDstSq;
                }
                else
                {
                    score = -distSq;
                }

                if (best == null || score > bestScore)
                {
                    bestScore = score;
                    best = e;
                }
            }
            return best;
        }

        private void ExecuteFire(Enemy t)
        {
            if (cfg == null) return;

            if (FriendlyFireMode)
            {
                FriendlyFireMode = false;
                Castle.Instance?.TakeDamage(1);
                return;
            }

            if (ProjectilePool.Instance == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[Tower] ProjectilePool.Instance is null — projectile not fired");
#endif
                return;
            }

            string fireSfxKey = UpgradeLevel switch { 2 => "tower_fire_l2", 3 => "tower_fire_l3", _ => "tower_fire_l1", };
            float firePitch = UpgradeLevel switch { 2 => 0.9f, 3 => 0.8f, _ => 1.0f };
            var ac = AudioController.Instance;
            if (ac != null)
            {
                bool hasTierClip = ac.GetClip(fireSfxKey) != null;
                ac.Play3DPitched(hasTierClip ? fireSfxKey : "tower_fire", transform.position, 0.60f, firePitch);
                ac.Play3D("tower_shoot", transform.position, 0.55f);
            }
            TriggerCannonShake();
            Vector3 muzzlePos = _barrelTip != null ? _barrelTip.position : transform.position + Vector3.up * 0.5f;
            VfxPool.Instance?.SpawnImpact(muzzlePos, cfg.ProjectileColor);
            if (Time.time - _lastMuzzleFlashAt >= 0.05f)
            {
                Vector3 flashPos = _barrelTip?.position ?? transform.position + Vector3.up * 0.8f;
                VfxPool.Instance?.SpawnMuzzleFlash(flashPos, cfg.ProjectileColor);
                bool isFrost = cfg.Id == "frost" || cfg.Id.Contains("ice");
                if (!isFrost)
                {
                    var yellowOrange = new Color(1f, 0.65f, 0.1f);
                    VfxPool.Instance?.SpawnSpark(flashPos, yellowOrange);
                    StartCoroutine(MuzzleFlashLightRoutine(flashPos));
                    if (ac != null)
                    {
                        float muzzlePitch = 0.9f + Random.value * 0.2f;
                        bool hasMuzzleClip = ac.GetClip("muzzle_pop") != null;
                        if (hasMuzzleClip)
                            ac.Play3DPitched("muzzle_pop", flashPos, 0.4f, muzzlePitch);
                    }
                }
                _lastMuzzleFlashAt = Time.time;
            }
            VfxPool.Instance?.SpawnAttackStream(muzzlePos, t.transform.position, cfg.ProjectileColor);
            if (_animator != null) _animator.SetTrigger("attackTrigger");
            if (!_recoiling) StartCoroutine(RecoilRoutine());
            _lastFireAt = Time.time;

            float dmg = cfg.Damage * BalanceConfig.Get().TowerDamageMul * TalentSystem.TowerDamageMul * ResearchDamageMul * _buffMul * _heroBuffDmgMul * _levelDmgScale * L3DmgMul;
            if (_streakCount > 0 && Time.time - _lastKillTime < StreakWindow * 4f)
                dmg *= 1f + _streakCount * 0.05f;

            if (L3BerserkerActive && Castle.Instance != null && Castle.Instance.HPMax > 0)
            {
                float hpRatio = (float)Castle.Instance.HP / Castle.Instance.HPMax;
                if (hpRatio < L3BerserkerHpThreshold) dmg *= L3BerserkerDmgMul;
            }

            {
                var bal = BalanceConfig.Get();
                float baseCrit = bal.CritChance + TalentSystem.CritChanceBonus + TowerResearchTree.CritChanceBonus(cfg.Id);
                if (baseCrit > 0f && Random.value < baseCrit)
                {
                    dmg *= bal.CritDmgMul;
                    _lastKillWasCrit = true;
                    CrowdDefense.UI.FloatingPopupController.Instance?.SpawnCrit(dmg, t.transform.position);
                    VfxPool.Instance?.SpawnConfetti(t.transform.position, 0.5f, new Color(1f, 0.9f, 0.1f));
                    ac?.Play3DPitched("tower_fire", transform.position, 0.45f, firePitch * 1.35f);
                }
            }

            if (L3CritChance > 0f && Random.value < L3CritChance)
            {
                dmg *= L3CritMul;
                _lastKillWasCrit = true;
                VfxPool.Instance?.SpawnConfetti(t.transform.position, 0.5f, new Color(1f, 0.9f, 0.1f));
                ac?.Play3DPitched("tower_fire", transform.position, 0.45f, firePitch * 1.35f);
            }

            if (t.IsFlyer && !t.ImmuneToFlyerBonus)
            {
                float flyMul = Mathf.Max(cfg.FlyerDmgMul, _flyerDmgBonus);
                if (flyMul > 1f) dmg *= flyMul;
            }

            _damageLogTimes.Enqueue(Time.time);
            _damageLogValues.Enqueue(dmg);
            TotalDamageDealt += dmg;

            int effectivePierce = L3Pierce > 0 ? L3Pierce : cfg.Pierce + _pierceBonus;
            float effectiveAoe = L3Aoe > 0f ? L3Aoe : cfg.Aoe;

            float dist = (t.transform.position - (transform.position + Vector3.up * 1.0f)).magnitude;
            float flightDur = cfg.Parabolic ? dist / Mathf.Max(cfg.ProjectileSpeed, 1f) : 0f;
            float arcH = cfg.Parabolic ? dist / 3f : 0f;

            var proj = ProjectilePool.Instance.Get();
            if (proj == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[Tower] ProjectilePool.Get() returned null — skipping fire");
#endif
                return;
            }
            proj.transform.position = transform.position + Vector3.up * 1.0f;
            proj.transform.rotation = Quaternion.identity;
            proj.Init(t, dmg, cfg.ProjectileSpeed, cfg.ProjectileColor, effectivePierce, effectiveAoe, cfg.Parabolic, flightDur, arcH, this);
            proj.SetElementTint(_projectileTint);

            int extraShots = _multiShotBonus + L3MultiShot;
            if (extraShots > 0)
            {
                float spreadStep = (cfg.Id == "archer" && UpgradeBranch == TowerBranch.Dps) ? 15f : 12f;
                Vector3 baseDir = (t.transform.position - transform.position).normalized;
                for (int i = 0; i < extraShots; i++)
                {
                    float spreadAngle = (i + 1) * spreadStep;
                    float sign = (i % 2 == 0) ? 1f : -1f;
                    Vector3 spread = Quaternion.Euler(0f, spreadAngle * sign, 0f) * baseDir;
                    var proj2 = ProjectilePool.Instance.Get();
                    if (proj2 == null) continue;
                    proj2.transform.position = transform.position + Vector3.up * 1.0f;
                    proj2.transform.rotation = Quaternion.LookRotation(spread);
                    proj2.Init(t, dmg, cfg.ProjectileSpeed, cfg.ProjectileColor, effectivePierce, effectiveAoe, cfg.Parabolic, flightDur, arcH, this);
                    proj2.SetElementTint(_projectileTint);
                }
            }

            if (L3SlowOnHit && SlowEffectManager.Instance != null)
                SlowEffectManager.Instance.ApplySlow(t, L3SlowMul, L3SlowDurMs);

            if (L3FreezeOnHit && SlowEffectManager.Instance != null)
                SlowEffectManager.Instance.ApplySlow(t, 0f, L3FreezeDurMs);

            if (Time.time - _lastDotPopupAt >= 0.5f)
            {
                string? dotIcon = null;
                Color dotColor = Color.white;
                if (L3BurnDot)          { dotIcon = "\U0001F525"; dotColor = new Color(1f, 0.45f, 0.05f); }
                else if (L3FreezeOnHit) { dotIcon = "❄️"; dotColor = new Color(0.4f, 0.9f, 1f); }
                else if (L3SlowOnHit)   { dotIcon = "❄️"; dotColor = new Color(0.4f, 0.9f, 1f); }
                if (dotIcon != null)
                {
                    CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(dotIcon, transform.position + Vector3.up * 1.5f, dotColor);
                    _lastDotPopupAt = Time.time;
                }
            }

            if (L3ChainLightningJumps > 0)
                FireChainLightning(t, dmg * 0.6f, L3ChainLightningJumps, L3ChainLightningRange);
        }

        private void FireChainLightning(Enemy origin, float dmg, int jumps, float range)
        {
            if (WaveManager.Instance == null || jumps <= 0) return;
            float rangeSq = range * range;
            _chainLightningHit.Clear();
            _chainLightningHit.Add(origin);
            var hit = _chainLightningHit;
            Enemy? current = origin;

            VfxPool.Instance?.SpawnLightningChain(origin.transform.position);

            for (int j = 0; j < jumps; j++)
            {
                Enemy? next = null;
                float bestDist = rangeSq;
                var enemies = WaveManager.Instance.ActiveEnemies;
                for (int i = 0; i < enemies.Count; i++)
                {
                    var e = enemies[i];
                    if (e == null || e.IsDead || hit.Contains(e)) continue;
                    float d = (e.transform.position - current!.transform.position).sqrMagnitude;
                    if (d < bestDist) { bestDist = d; next = e; }
                }
                if (next == null) break;
                next.TakeDamage(dmg);
                VfxPool.Instance?.SpawnLightningChain(next.transform.position);
                hit.Add(next);
                current = next;
                dmg *= 0.8f;
            }
        }

        public void ApplyHeroBuff(float dmgMul)
        {
            _heroBuffDmgMul = Mathf.Max(1f, dmgMul);
        }

        public void ClearHeroBuff()
        {
            _heroBuffDmgMul = 1f;
        }

        public void SetGuardMode(GuardMode mode)
        {
            _guardMode = mode;
            target?.SetTargetedBy(false);
            target = null;
        }

        public void FireAngled(Enemy t, float angleDeg)
        {
            if (cfg == null) return;
            if (ProjectilePool.Instance == null) return;

            float dmg = cfg.Damage * BalanceConfig.Get().TowerDamageMul * TalentSystem.TowerDamageMul * ResearchDamageMul * _buffMul * _heroBuffDmgMul * _levelDmgScale * L3DmgMul;
            if (_streakCount > 0 && Time.time - _lastKillTime < StreakWindow * 4f)
                dmg *= 1f + _streakCount * 0.05f;

            Vector3 baseDir = (t.transform.position - transform.position).normalized;
            Vector3 angledDir = Quaternion.Euler(0f, angleDeg, 0f) * baseDir;

            float dist = (t.transform.position - transform.position).magnitude;
            float flightDur = cfg.Parabolic ? dist / Mathf.Max(cfg.ProjectileSpeed, 1f) : 0f;
            float arcH = cfg.Parabolic ? dist / 3f : 0f;

            int effectivePierce = L3Pierce > 0 ? L3Pierce : cfg.Pierce + _pierceBonus;
            float effectiveAoe = L3Aoe > 0f ? L3Aoe : cfg.Aoe;

            var proj = ProjectilePool.Instance.Get();
            if (proj == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[Tower] ProjectilePool.Get() returned null in FireAngled");
#endif
                return;
            }
            proj.transform.position = transform.position + Vector3.up * 1.0f;
            proj.transform.rotation = Quaternion.LookRotation(angledDir);
            proj.Init(t, dmg, cfg.ProjectileSpeed, cfg.ProjectileColor, effectivePierce, effectiveAoe, cfg.Parabolic, flightDur, arcH, this);
            proj.SetElementTint(_projectileTint);
        }

        // ── MuzzleFlash light pool (pre-allocated, zero GC after warmup) ─────────
        private const int MuzzleFlashPoolSize = 6;
        private (GameObject go, Light light)[]? _muzzleFlashPool;
        private int _muzzleFlashNext;

        private void EnsureMuzzleFlashPool()
        {
            if (_muzzleFlashPool != null) return;
            _muzzleFlashPool = new (GameObject, Light)[MuzzleFlashPoolSize];
            for (int i = 0; i < MuzzleFlashPoolSize; i++)
            {
                var go = new GameObject("MuzzleFlash");
                go.transform.SetParent(transform, false);
                var l = go.AddComponent<Light>();
                l.type      = LightType.Point;
                l.color     = new Color(1f, 0.95f, 0.5f);
                l.range     = 2f;
                l.intensity = 0f;
                l.shadows   = LightShadows.None;
                go.SetActive(false);
                _muzzleFlashPool[i] = (go, l);
            }
        }

        private IEnumerator MuzzleFlashLightRoutine(Vector3 worldPos)
        {
            EnsureMuzzleFlashPool();
            var (go, light) = _muzzleFlashPool![_muzzleFlashNext % MuzzleFlashPoolSize];
            _muzzleFlashNext++;

            go.transform.position = worldPos;
            go.SetActive(true);
            light.intensity = 4f;

            float elapsed = 0f;
            const float Duration = 0.1f;
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                light.intensity = Mathf.Lerp(4f, 0f, elapsed / Duration);
                yield return null;
            }
            light.intensity = 0f;
            go.SetActive(false);
        }

        private void TriggerCannonShake()
        {
            if (cfg == null) return;
            bool isCannon = cfg.Id.Contains("cannon");
            bool isHeavy  = isCannon && (UpgradeLevel >= 3 || cfg.Id != "cannon");
            bool isBallistaL3Multishot = cfg.Id == "ballista" && UpgradeLevel >= 3 && L3MultiShot > 0;
            if (!isCannon && !isBallistaL3Multishot) return;

            if (Time.unscaledTime - _lastCamShakeAt < 0.05f) return;
            _lastCamShakeAt = Time.unscaledTime;

            var jc = JuiceConfig.Get();
            if (isHeavy || isBallistaL3Multishot)
                JuiceFX.Instance?.Shake(jc?.TowerFireShakeAmp ?? 0.30f, jc?.TowerFireShakeMs ?? 150);
            else
                JuiceFX.Instance?.Shake((jc?.TowerFireShakeAmp ?? 0.30f) * 0.5f, jc?.TowerFireShakeMs ?? 100);
        }
    }
}
