#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.UI;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public partial class Hero : MonoBehaviour
    {
        // ── HP / respawn ──────────────────────────────────────────────────────
        private const float DefaultMaxHp    = 100f;
        private const float RespawnDelay    = 15f;
        private const float InvulDuration   = 2f;

        protected float _hp;
        protected float _maxHp = DefaultMaxHp;
        protected bool  _isDead;
        protected float _invulTimer;
        protected Coroutine? _respawnRoutine;

        public float HP    => _hp;
        public float HPMax => _maxHp;
        public bool  IsAlive => !_isDead;

        public void TakeDamage(float dmg)
        {
            if (_isDead || _invulTimer > 0f || dmg <= 0f) return;
            _hp -= dmg;
            OnHeroDamaged?.Invoke(dmg);
            AudioController.Instance?.Play3DPitched("enemy_hit", transform.position, 0.7f, 1.2f);
            if (_hp <= 0f)
            {
                _hp = 0f;
                TriggerMidLevelDeath();
            }
        }

        public void TriggerMidLevelDeath()
        {
            if (_isDead) return;
            _isDead = true;

            gameObject.SetActive(false);
            JuiceFX.Instance?.Flash(new Color(1f, 0f, 0f, 0.45f), 600);
            AudioController.Instance?.Play("hero_death", 1.2f);
            VfxPool.Instance?.SpawnImpact(transform.position + Vector3.up, Color.red);

            _respawnRoutine = StartCoroutine(RespawnCountdownRoutine());
        }

        private System.Collections.IEnumerator RespawnCountdownRoutine()
        {
            float remaining = RespawnDelay;
            while (remaining > 0f)
            {
                var state = LevelRunner.Instance?.State;
                if (state == GameState.Lost || state == GameState.LevelComplete
                    || state == GameState.Summary)
                    yield break;

                FloatingPopupController.Instance?.SpawnReward(
                    $"Respawn {Mathf.CeilToInt(remaining)}s",
                    Camera.main != null
                        ? Camera.main.transform.position + Camera.main.transform.forward * 5f + Vector3.up * 1f
                        : Vector3.up * 3f,
                    new Color(1f, 0.5f, 0.5f));

                yield return new WaitForSecondsRealtime(1f);
                remaining -= 1f;
            }

            var finalState = LevelRunner.Instance?.State;
            if (finalState == GameState.Lost || finalState == GameState.LevelComplete
                || finalState == GameState.Summary)
                yield break;

            RespawnAtCastle();
        }

        private void RespawnAtCastle()
        {
            _isDead = false;

            var castlePos = Castle.Instance != null
                ? Castle.Instance.transform.position
                : transform.position;
            transform.position   = castlePos + Vector3.up * 0.1f;
            transform.localScale = Vector3.one;

            _hp         = _maxHp * 0.5f;
            _invulTimer = InvulDuration;

            gameObject.SetActive(true);

            JuiceFX.Instance?.Flash(new Color(0.3f, 1f, 0.4f, 0.35f), 400);
            VfxPool.Instance?.SpawnLevelUp(transform.position + Vector3.up * 1f);
            AudioController.Instance?.Play("hero_levelup", 0.9f);
            JuiceFX.Instance?.PunchScale(transform, 1.4f, 0.3f);
            FloatingPopupController.Instance?.SpawnReward(
                "RESPAWN!", transform.position + Vector3.up * 2f, Color.green);
            StartCoroutine(InvulFlashRoutine());
            OnHeroRespawned?.Invoke();
        }

        private System.Collections.IEnumerator InvulFlashRoutine()
        {
            var renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            float elapsed = 0f;
            while (elapsed < InvulDuration)
            {
                bool visible = Mathf.FloorToInt(elapsed / 0.15f) % 2 == 0;
                foreach (var r in renderers) r.enabled = visible;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            foreach (var r in renderers) r.enabled = true;
        }

        // ── Nth-projectile AoE counter ────────────────────────────────────────
        private int _projFiredCount;

        // ── Ultimate (slot 2 / E — existing) ─────────────────────────────────
        protected float _ultCooldown;

        // ── Ultimate slot 3 (R — unlock level 10, 60s CD, 5× dmg AoE r=4) ────
        protected const float UltimateCooldown    = 60f;
        protected const float UltimateAoeRadius   = 4f;
        protected const float UltimateDmgMul      = 5f;
        protected const int   UltimateUnlockLevel = 10;
        protected float _ultimateCooldown;


        public (float dmgMul, float fireRateMul, float auraRange) GetTowerAuraBuffs()
        {
            float dmg   = 1f;
            float rate  = 1f;
            float range = 6f;

            if (_perks.Contains("coin_gain")) dmg  += 0.15f;
            if (_perks.Contains("lifesteal")) rate += 0.10f;
            if (_perks.Contains("surveillant"))
            {
                rate  = Mathf.Max(rate, TowerFireRateAuraMul > 1f ? TowerFireRateAuraMul : 1.20f);
                range = Mathf.Max(range, TowerAuraRange > 0f ? TowerAuraRange : 8f);
            }
            return (dmg, rate, range);
        }

        private void UpdateCombat()
        {
            if (cfg == null || _running || ChannelingPill) return;

            float range2 = cfg.Range * RangeMul;
            range2 *= range2;
            var myPos = transform.position;

            Enemy? target = null;
            float bestDist2 = range2 + 1f;

            if (WaveManager.Instance != null)
            {
                var active = WaveManager.Instance.ActiveEnemies;
                for (int i = 0; i < active.Count; i++)
                {
                    var e = active[i];
                    if (e == null || e.IsDead) continue;
                    float d2 = (e.transform.position - myPos).sqrMagnitude;
                    if (d2 < range2 && d2 < bestDist2)
                    {
                        bestDist2 = d2;
                        target    = e;
                    }
                }
            }

            if (target != null)
            {
                Vector3 toTarget = target.transform.position - myPos;
                toTarget.y = 0f;
                if (toTarget != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(toTarget);

                if (_autoAttack && _cooldown <= 0f)
                {
                    float rateMs = cfg.FireRateMs * FireRateMul;
                    _cooldown = rateMs / 1000f;
                    Fire(target);
                    if (Lightning) LightningStrike(target);
                }
            }
            else
            {
                if (_attackAnimTimer <= 0f)
                {
                    AnimationController.SetIdle(_animator);
                }
            }
        }

        private void Fire(Enemy target)
        {
            if (cfg == null) return;

            AnimationController.TriggerAttack(_animator);
            _attackAnimTimer = 0.25f;

            var start = transform.position + Vector3.up * 0.9f;
            Vector3 toTarget = target.transform.position - start;
            toTarget.y = 0f;
            float baseLen = toTarget.magnitude;
            if (baseLen < 0.001f) return;
            var baseDir = toTarget / baseLen;

            int shots    = 1 + MultiShot;
            float spread = shots > 1 ? 12f * Mathf.Deg2Rad : 0f;

            for (int s = 0; s < shots; s++)
            {
                float angle = shots > 1 ? (s - (shots - 1) / 2f) * spread : 0f;
                var dir = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f) * baseDir;

                int movePierce = (MoveAttackPierceBonus > 0 && _moveDir.sqrMagnitude > 0.01f)
                    ? MoveAttackPierceBonus : 0;

                SpawnProjectileDirectional(start, dir,
                    pierceLeft: PierceCount + movePierce,
                    ricochetLeft: Ricochet ? RicochetBounces : 0,
                    aoeOnHit: Fireball,
                    explodeOnConsume: PierceExplode,
                    lifetime: 1.4f,
                    damageMul: 1f);
            }

            if (AoeOnNthProjectile > 0)
            {
                _projFiredCount++;
                if (_projFiredCount >= AoeOnNthProjectile)
                {
                    _projFiredCount = 0;
                    if (cfg != null)
                        AoeBlast(start, FireballRadius, cfg.Damage * DamageMul * FireballDmgMul);
                }
            }

            VfxPool.Instance?.SpawnImpact(start + baseDir * 0.4f, new Color(1f, 0.957f, 0.835f));
            AudioController.Instance?.Play("hero_shoot", 0.7f);
        }

        private void LightningStrike(Enemy primary)
        {
            if (cfg == null || WaveManager.Instance == null) return;
            float range2 = (cfg.Range * RangeMul) * (cfg.Range * RangeMul);
            var myPos = transform.position;
            float dmg = cfg.Damage * DamageMul * LightningDmgMul;

            var candidates = new List<Enemy>();
            var active = WaveManager.Instance.ActiveEnemies;
            for (int i = 0; i < active.Count; i++)
            {
                var e = active[i];
                if (e == null || e.IsDead || e == primary) continue;
                if ((e.transform.position - myPos).sqrMagnitude < range2) candidates.Add(e);
            }

            int count = Mathf.Min(LightningTargets, candidates.Count);
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }
            for (int i = 0; i < count; i++)
            {
                var t = candidates[i];
                t.TakeDamage(dmg);
                VfxPool.Instance?.SpawnImpact(
                    t.transform.position + Vector3.up * 1.2f,
                    new Color(0.659f, 0.878f, 1f));
            }
        }

        private void SpawnProjectileAt(float angleRad, int pieceBonus, float damageMul, float lifetime)
        {
            var start = transform.position + Vector3.up * 0.9f;
            var dir   = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
            SpawnProjectileDirectional(start, dir,
                pierceLeft: PierceCount + pieceBonus,
                ricochetLeft: 0, aoeOnHit: false, explodeOnConsume: false,
                lifetime: lifetime, damageMul: damageMul);
        }

        private void SpawnProjectileDirectional(Vector3 origin, Vector3 dir,
            int pierceLeft, int ricochetLeft, bool aoeOnHit, bool explodeOnConsume,
            float lifetime, float damageMul)
        {
            if (cfg == null) return;

            var pool = HeroProjectilePool.Instance;
            if (pool == null) return;

            var proj = pool.Get(origin);
            proj.Init(
                speed:          cfg.ProjectileSpeed,
                dir:            dir,
                damage:         cfg.Damage * DamageMul * damageMul,
                lifetime:       lifetime,
                pierceLeft:     pierceLeft,
                ricochetLeft:   ricochetLeft,
                ricochetDecay:  RicochetDecay,
                aoeOnHit:       aoeOnHit,
                fireballRadius: FireballRadius,
                fireballDmgMul: FireballDmgMul,
                explodeOnConsume: explodeOnConsume,
                pierceExplodeRadius: PierceExplodeRadius,
                pierceExplodeDmgMul: PierceExplodeDmgMul,
                critChance:     CritChance,
                critMul:        CritMul,
                critStaggerMs:  CritStaggerMs,
                glaciation:     Glaciation,
                onEnemyKilled:  OnProjKill);

            _projectiles.Add(proj);
        }

        private void UpdateProjectiles(float dt)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                if (_projectiles[i] == null || _projectiles[i].IsDone)
                    _projectiles.RemoveAt(i);
            }
        }

        private void OnProjKill(Vector3 killPos, int xp)
        {
            GainXp(xp);

            if (Lifesteal > 0f && Castle.Instance != null)
                Castle.Instance.Regen(Mathf.Max(1, Mathf.RoundToInt(Lifesteal)));

            if (Combustion)
                _fireTrails.Add(new FireTrail(killPos, Time.time + 2f));

            if (Pyromancie && Random.value < 0.1f)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                SpawnProjectileAt(angle, 0, DamageMul, 2.0f);
            }
        }

        public void TriggerDeathCinematic()
        {
            JuiceFX.Instance?.SlowMo(0.3f, 2000);
            JuiceFX.Instance?.Flash(new Color(1f, 0f, 0f, 0.45f), 800);
            AudioController.Instance?.Play("hero_death", 1.2f);
            StartCoroutine(CameraDeathZoom());
        }

        private System.Collections.IEnumerator CameraDeathZoom()
        {
            var cam = MainCameraCache.Main;
            if (cam == null) yield break;
            float origFOV = cam.fieldOfView;
            float target = origFOV * 0.6f;
            float t = 0f;
            while (t < 1.5f)
            {
                t += Time.unscaledDeltaTime;
                cam.fieldOfView = Mathf.Lerp(origFOV, target, t / 1.5f);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.5f);
            cam.fieldOfView = origFOV;
        }

        public void OnWaveEnd()
        {
            if (WaveRegen > 0 && Castle.Instance != null)
                Castle.Instance.Regen(Mathf.RoundToInt(WaveRegen));
        }

        public void AoeBlast(Vector3 center, float radius, float dmg)
        {
            if (WaveManager.Instance == null) return;
            float r2 = radius * radius;
            var active = WaveManager.Instance.ActiveEnemies;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var e = active[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - center).sqrMagnitude < r2)
                    e.TakeDamage(dmg);
            }
            VfxPool.Instance?.SpawnImpact(center + Vector3.up * 0.2f, new Color(1f, 0.54f, 0.19f));
        }
    }
}
