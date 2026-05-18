#nullable enable
using System.Collections;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public partial class Enemy : MonoBehaviour
    {
        private void TickHitFlash()
        {
            if (_hitFlashTimer <= 0f) return;
            _hitFlashTimer -= Time.deltaTime;
            if (_hitFlashTimer <= 0f)
                ClearHitFlash();
        }

        public void TakeDamage(float dmg, Tower? sourceTower)
        {
            _lastDamageTower = sourceTower;
            TakeDamage(dmg);
        }

        public void TakeDamage(float dmg, Vector3 hitOrigin = default)
        {
            if (IsDead || _dying) return;

            // Heal (negative damage) — show green popup then bail
            if (dmg < 0f)
            {
                float heal = -dmg;
                hp = Mathf.Min(hp + heal, maxHp);
                if (heal >= 5f)
                    CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                        $"+{Mathf.RoundToInt(heal)}", transform.position + Vector3.up * 1.0f, Color.green);
                return;
            }

            if (hitOrigin != default)
                _lastDamageDirection = (transform.position - hitOrigin).normalized;
            else
                _lastDamageDirection = -transform.forward;

            float actualDmg = dmg;

            // Directional shield block (port of Enemy.js takeDamage shieldHP block)
            if (shieldHp > 0f)
            {
                shieldHp -= dmg;
                if (shieldHp <= 0f)
                {
                    shieldHp = 0f;
                    if (shieldHalo != null)
                        shieldHalo.SetActive(false);
                    VfxPool.Instance?.SpawnImpact(transform.position + Vector3.up * 0.6f,
                        new Color(1f, 0.82f, 0.24f));
                }
                // Shield fully absorbs hit when still positive
                if (shieldHp >= 0f) return;
                // Excess damage bleeds through
                actualDmg = -shieldHp;
                shieldHp = 0f;
            }

            if (actualDmg <= 0f) return;

            // Armor break — amplify incoming damage while active (Ballista L3 / synergy)
            if (_dmgTakenMulUntil > 0f)
            {
                if (Time.time < _dmgTakenMulUntil)
                    actualDmg *= _dmgTakenMul;
                else
                {
                    _dmgTakenMul      = 1f;
                    _dmgTakenMulUntil = 0f;
                }
            }

            // Armored variant: flat damage reduction
            if (_dmgReduction > 0f)
                actualDmg *= (1f - _dmgReduction);

            // Apocalypse boss phase invulnerability: clamp HP floor
            if (cfg != null && cfg.IsApocalypseBoss && _invulUntilTime > 0f && Time.time < _invulUntilTime)
            {
                float floor = maxHp * 0.76f;
                hp = Mathf.Max(hp - actualDmg, floor);
            }
            else
            {
                hp -= actualDmg;
            }

            // Fallback boss enrage @ 50% HP — fires once if BossSystem hasn't already triggered.
            // V5 parity: Enemy.js auto-enrages any boss without external orchestration.
            if (cfg != null && cfg.IsBoss && !cfg.IsApocalypseBoss
                && !_enragedSelfTriggered && _enragedSpeedMul == 1f && hp <= maxHp * 0.5f && hp > 0f)
            {
                _enragedSelfTriggered = true;
                _enragedSpeedMul      = 1.4f;
                _enragedSummonCdMul   = 0.6f;
                EventManager.Instance?.Publish(new BossPhaseChangedEvent("enraged", 1));
            }

            // 60% HP minion burst — spawn 3 fast mobs once (V4 parity bonus)
            if (cfg != null && cfg.IsBoss && !cfg.IsApocalypseBoss && !_minionsSummoned && hp <= maxHp * 0.60f && hp > 0f)
            {
                _minionsSummoned = true;
                AudioController.Instance?.Play3D("boss_roar", transform.position, 0.8f);
                SpawnMinionBurst();
            }

            // 30% HP enrage — red pulsing ring + castle dmg +30% (V4 parity)
            if (cfg != null && cfg.IsBoss && !_enrageActive && hp <= maxHp * 0.30f && hp > 0f)
            {
                _enrageActive = true;
                _damageMul   *= 1.3f;
                StartEnrageRing();
                AudioController.Instance?.Play3D("boss_roar", transform.position, 1f);
            }

            // Dynamic HP bar color (green → yellow → red) — port of Enemy.js hpBar color update
            float ratio = HpRatio;

            // Boss HP bar tracking
            if (cfg != null && cfg.IsBoss)
                EventManager.Instance?.Publish(new BossHpChangedEvent(ratio));

            // Boss skin phase transitions — check after HP update, before flash
            if (cfg != null && cfg.IsBoss)
            {
                if (ratio < 0.33f && _bossPhase < 3)
                    ApplyBossPhase(3);
                else if (ratio < 0.66f && _bossPhase < 2)
                    ApplyBossPhase(2);
                else if (_bossPhase == 0)
                    ApplyBossPhase(1);
            }

            // Hit flash + particles
            TriggerHitFlash();
            AudioController.Instance?.Play3D("enemy_hit", transform.position, 0.4f);
            VfxPool.Instance?.SpawnHitFlash(transform);
            bool isBossHit = cfg != null && (cfg.IsBoss || cfg.IsApocalypseBoss);
            var  popup     = CrowdDefense.UI.FloatingPopupController.Instance;
            if (actualDmg >= 5f)
            {
                Vector3 popupPos = transform.position + Vector3.up * 1.2f;
                if (isBossHit)
                    popup?.SpawnReward($"-{Mathf.RoundToInt(actualDmg)}", popupPos, Color.white, 1.2f);
                else
                    popup?.SpawnDamage(actualDmg, popupPos);
            }

            // Juice screen shake on hit for bosses
            if (cfg != null && cfg.IsBoss)
                JuiceFX.Instance?.Shake(0.08f, 100);

            // Apocalypse boss phase transitions
            if (cfg != null && cfg.IsApocalypseBoss)
                TickApocalypseBossPhases(ratio);

            if (hp <= 0f)
                HandleDeath();
        }
        private void HandleDeath()
        {
            _dying = true;

            bool isBoss   = cfg != null && (cfg.IsBoss || cfg.IsApocalypseBoss);
            bool isMedium = cfg != null && cfg.IsMidBoss;

            string deathClip = isBoss ? "enemy_die_boss" : (isMedium ? "enemy_die_medium" : "enemy_die_basic");
            AudioController.Instance?.Play(deathClip, isBoss ? 1f : 0.5f);

            float vfxIntensity = maxHp switch
            {
                <= 30f  => 0.6f,
                <= 100f => 1.0f,
                <= 300f => 1.5f,
                _       => 2.5f
            };
            if (isBoss) vfxIntensity = JuiceConfig.Get().BossDeathFlashScale;
            VfxPool.Instance?.SpawnDeath(transform.position, baseColor, vfxIntensity);

            if (isBoss)
            {
                var jcBoss = JuiceConfig.Get();
                JuiceFX.Instance?.Shake(jcBoss.BossHitShakeAmp, jcBoss.BossHitShakeMs);
                JuiceFX.Instance?.Flash(new Color(1f, 1f, 1f, jcBoss.BossHitFlashAlpha), jcBoss.BossHitFlashMs);
                StartCoroutine(BossCinematic());
            }
            else if (isMedium)
            {
                JuiceFX.Instance?.Shake(0.3f, 200);
            }

            // Boss reward = 0× (D1-01 §3.3)
            if (!(isBoss || isMedium))
            {
                int baseReward = cfg?.Reward ?? 0;
                float coinMul  = CoinPullManager.Instance?.GetCoinMulAt(transform.position) ?? 1f;
                float streakMul = WaveManager.Instance?.StreakRewardMul ?? 1f;
                float eliteMul = _isElite ? 2f : 1f;
                int reward = Mathf.Max(1, Mathf.RoundToInt(baseReward * coinMul * streakMul * eliteMul * _diffRewardMul));
#if UNITY_EDITOR
                Debug.Log($"[Enemy] killed type={cfg?.Id} baseReward={baseReward} coinMul={coinMul:F2} streakMul={streakMul:F2} reward={reward}");
#endif
                EventManager.Instance?.Publish(new EnemyKilledEvent(this, reward));
                CoinPullManager.Instance?.SpawnCoinFlyTo(transform.position, reward);
                Economy.Instance?.AddGoldFromKill(reward, transform.position + Vector3.up * 1.2f);
                // N31: Use tiered SpawnGoldReward (< 10 white, 10-30 yellow, > 30 gold + sparkle)
                // matching the V4 popup style. Economy.AddGoldFromKill already calls this internally
                // via SpawnGoldReward in Economy.cs:114 — duplicate popup removed.
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log($"[Enemy] boss killed type={cfg?.Id} reward=0 (D1-01 boss=0x)");
#endif
                // P1-EN-1.2: BOSS DOWN! gold popup + ring VFX on boss kill
                CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                    "BOSS DOWN!", transform.position + Vector3.up * 2f, new Color(1f, 0.82f, 0.1f), 1.4f);
                VfxPool.Instance?.SpawnExplosion(transform.position, 4f);
            }

            if (cfg != null) Bestiary.Instance?.RecordKill(cfg.Id);
            Achievements.Instance?.Unlock("first_blood");
            Achievements.Instance?.TrackEvent("enemy_killed", 1);
            LifetimeStats.Instance?.AddKill(1);
            WaveHistoryLog.Instance?.Log("kill", $"Mort : {cfg?.DisplayName ?? cfg?.Id ?? "?"}");

            StopEnrageVFX();
            CancelInvoke(nameof(EmitAoePulse));
            WaveManager.Instance?.NotifyEnemyDied(this);
            // N33: explicit Unity null check (== null is overloaded for destroyed objects; ?. is not)
            if (_lastDamageTower != null) _lastDamageTower.RegisterKill();

            OnDeathStatic?.Invoke(this, isBoss);

            if (this != null && gameObject != null)
                ReleaseToPool();
            else
                ReleaseToPool();
        }
        private void OnReachedCastle()
        {
            if (IsDead || _dying) return;
            StartCoroutine(CastleAttackWithTelegraph());
        }

        private IEnumerator CastleAttackWithTelegraph()
        {
            bool isAoe = cfg?.AoEAttack ?? false;
            float attackRange = isAoe ? (cfg?.AoEAttackRadius ?? 1.5f) : 1.2f;
            var circle = BuildTelegraphCircle(attackRange);
            if (isAoe) circle.GetComponent<LineRenderer>().material.color = new Color(1f, 0.1f, 0.1f, 0.9f);

            yield return new WaitForSeconds(AttackTelegraphDuration);

            if (circle != null) Object.Destroy(circle);

            int dmg = Mathf.RoundToInt((cfg?.Damage ?? 0) * _damageMul);
#if UNITY_EDITOR
            Debug.Log($"[Enemy] reached castle type={cfg?.Id} dmg={dmg} aoe={isAoe} pathIdx={pathIdx}");
#endif
            Castle.Instance?.TakeDamage(dmg);

            if (isAoe && cfg != null)
                SplashNearbyTowers(cfg.AoEAttackRadius, cfg.AoEAttackDamage);

            EventManager.Instance?.Publish(new EnemyReachedCastleEvent(this, dmg));
            if (dmg > 0)
                EventManager.Instance?.Publish(new HeroDamagedEvent(dmg));
            WaveManager.Instance?.NotifyEnemyDied(this);
            ReleaseToPool();
        }

        private void ReleaseToPool()
        {
            CancelInvoke();
            IsDead = true;
            if (pool != null)
                pool.ReleaseTyped(this);
            else
                Destroy(gameObject);
        }

        // ── OnDestroy cleanup ─────────────────────────────────────────────────

        public void ApplyKnockback(float strength)
        {
            if (strength <= 0f || _dying || IsDead || cfg == null || cfg.IsFlyer) return;
            if (pathManager == null) return;
            int steps = Mathf.Max(1, Mathf.RoundToInt(strength));
            currentWaypoint = Mathf.Max(1, currentWaypoint - steps);
        }

        // AoE attack on castle — splash damage to nearby towers within radius
        private void SplashNearbyTowers(float radius, int splashDmg)
        {
            if (PlacementController.Instance == null) return;
            var towers = PlacementController.Instance.PlacedTowers;
            float radiusSq = radius * radius;
            int hit = 0;
            for (int i = 0; i < towers.Count; i++)
            {
                var tower = towers[i];
                if (tower == null) continue;
                if ((tower.transform.position - transform.position).sqrMagnitude < radiusSq)
                {
                    tower.ReceiveEnemySplash(splashDmg);
                    hit++;
                }
            }
            VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 0.4f, radius * 0.5f);
            AudioController.Instance?.Play3D("boss_roar", transform.position, 0.6f);
#if UNITY_EDITOR
            Debug.Log($"[Enemy] AoE attack splash radius={radius} dmg={splashDmg} hit {hit} towers");
#endif
        }
    }
}
