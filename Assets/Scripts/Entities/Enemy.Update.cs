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
        // ── Update lifecycle ──────────────────────────────────────────────────

        private void Update()
        {
            if (cfg == null || IsDead) return;

            // Static mode: animate in place, no movement
            if (_static)
            {
                if (_wasWalking) { AnimationController.SetWalking(_animator, false); _wasWalking = false; }
                transform.rotation = Quaternion.Euler(0f, _staticRotY, 0f);
                return;
            }

            TickHitFlash();
            UpdateHpBar();
            TickBossAura();
            TickBossEncounterPublish();
            TickApocalypseBoss();
            TickEnrageLight();
            UpdateStealth();
            UpdateSummons();
            UpdateAoeBlast();
            UpdateCharge();
            UpdateFireBreath();
            EnemyBossBehaviorsStatic.TickWizardKing(this);
            EnemyBossBehaviorsStatic.TickWarlordCharge(this);
            EnemyBossBehaviorsStatic.TickAiHubBurst(this);
            EnemyBossBehaviorsStatic.TickKrakenTentacles(this);
            UpdateFreeze();
            UpdateDebuffIcons();
            UpdateGroundDecals();

            if (_dying) return;

            if (_regenPerSec > 0f)
                hp = Mathf.Min(hp + _regenPerSec * Time.deltaTime, maxHp);

            // Lock movement during spawn pop-in animation
            if (_popInCoroutine != null) return;

            if (cfg.IsFlyer)
            {
                UpdateFlyer();
                return;
            }

            if (pathManager == null) return;
            int wpCount = pathManager.WaypointCountOnPath(pathIdx);
            if (currentWaypoint >= wpCount)
            {
                OnReachedCastle();
                return;
            }

            float effSpeed = ComputeEffectiveSpeed();
            var heroInst = LevelRunner.Instance?.Hero;
            bool chasingHeroNow = _chaseHero && heroInst != null && heroInst.gameObject.activeInHierarchy;
            Vector3 target = chasingHeroNow
                ? heroInst!.transform.position
                : pathManager.GetWaypointOnPath(pathIdx, currentWaypoint) + Vector3.up * 0.5f;
            transform.position = Vector3.MoveTowards(transform.position, target, effSpeed * Time.deltaTime);

            if (!chasingHeroNow && (transform.position - target).sqrMagnitude < 0.01f)
                currentWaypoint++;

            // Face movement direction
            Vector3 dir = (target - transform.position);
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));

            bool nowWalking = effSpeed > 0.01f;
            if (nowWalking != _wasWalking)
            {
                AnimationController.SetWalking(_animator, nowWalking);
                _wasWalking = nowWalking;
            }

            // Walk anim blend + footstep audio
            if (_animator != null && _animator.runtimeAnimatorController != null && cfg!.Speed > 0f)
                _animator.SetFloat("Speed", effSpeed / cfg.Speed);
            if (nowWalking)
            {
                _stepTimer -= Time.deltaTime;
                if (_stepTimer <= 0f)
                {
                    _stepTimer = StepInterval;
                    AudioController.Instance?.Play3D("step_dirt", transform.position, 0.55f);
                }
            }
            else
            {
                _stepTimer = 0f;
            }

            // Dust trail for ground enemies
            if (effSpeed > 0.01f)
            {
                _dustTimer -= Time.deltaTime;
                if (_dustTimer <= 0f)
                {
                    _dustTimer = DustInterval;
                    VfxPool.Instance?.SpawnImpact(
                        new Vector3(transform.position.x, 0.05f, transform.position.z),
                        new Color(0.78f, 0.66f, 0.47f));
                }
            }

            // Fire trail for fiery enemies (imp, dragon, etc.)
            if (cfg!.IsFiery)
            {
                _fieryTimer -= Time.deltaTime;
                if (_fieryTimer <= 0f)
                {
                    _fieryTimer = FieryInterval;
                    VfxPool.Instance?.SpawnImpact(
                        transform.position + Vector3.up * 0.3f,
                        new Color(1f, 0.23f, 0.063f));
                }
            }
        }

        private void TickBossEncounterPublish()
        {
            if (_bossEncounteredPublished || cfg == null || !cfg.IsBoss) return;
            _bossEncounteredPublished = true;
            EventManager.Instance?.Publish(new EnemySpawnedEvent(this));
        }

        private void UpdateFreeze()
        {
            if (_freezeUntilTime <= 0f) return;
            bool frozen = Time.time < _freezeUntilTime;
            if (frozen && !_frozenTinted)
            {
                ApplyFreezeEmissive(true);
                _frozenTinted = true;
            }
            else if (!frozen && _frozenTinted)
            {
                ApplyFreezeEmissive(false);
                _frozenTinted = false;
                _freezeUntilTime = 0f;
            }
        }
    }
}
