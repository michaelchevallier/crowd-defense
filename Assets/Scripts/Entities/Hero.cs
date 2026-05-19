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
    /// <summary>
    /// Port from V5 Hero.js.
    /// Player-controlled unit: moves freely on the map, auto-attacks nearest enemy
    /// within range, accumulates XP, holds runtime perk multipliers.
    ///
    /// Spawn: instantiate prefab, call Init(cfg, spawnPos).
    /// Movement: each frame call SetMove(dx, dz) with world-space direction from input.
    ///
    /// Partial class split across 5 files:
    /// - Hero.cs (core, Init, lifecycle, Update dispatcher)
    /// - Hero.Combat.cs (combat, projectiles, ultimates, HP)
    /// - Hero.Movement.cs (movement input, pathing)
    /// - Hero.Perks.cs (perk accumulation, XP, level-up)
    /// - Hero.Anim.cs (animator, visuals, mesh spawn)
    /// </summary>
    public partial class Hero : MonoBehaviour
    {
        // ── Singleton-ish access (cached on Awake/OnDestroy, no auto-create) ──
        public static Hero? Current { get; private set; }

        // ── Config ────────────────────────────────────────────────────────────
        protected HeroType? cfg;

        // ── Runtime bounds (set by LevelRunner from grid bbox) ────────────────
        protected float _maxX = 59.5f;
        protected float _maxZ = 59.5f;

        // ── Combat state ──────────────────────────────────────────────────────
        protected float _cooldown;
        protected float _attackAnimTimer;
        protected bool  _running;
        protected bool  _autoAttack = true;

        private const string AutoAttackPrefsKey = "hero_auto_attack_v1";

        // ── Active projectile tracking (pool manages lifetime, this list for IsDone poll) ──
        protected readonly List<HeroProjectile> _projectiles = new();

        // ── Fire-trail (Combustion perk) ──────────────────────────────────────
        protected readonly List<FireTrail> _fireTrails = new();

        // ── Events ────────────────────────────────────────────────────────────
        public event System.Action<int, int, int>? OnLevelUp;   // (level, xp, xpToNext)
        public event System.Action?                OnUltFired;
        public static event System.Action<float>?  OnHeroDamaged;  // (dmg) — fired on each effective hit
        public static event System.Action?         OnHeroRespawned; // fired after RespawnAtCastle completes

        // ─────────────────────────────────────────────────────────────────────

        public void Init(HeroType type, Vector3 spawnPos, float maxX = 59.5f, float maxZ = 59.5f)
        {
            cfg      = type;
            _maxX    = maxX;
            _maxZ    = maxZ;
            _cooldown         = 0f;
            _ultCooldown      = 0f;
            _ultimateCooldown = 0f;

            _autoAttack  = PlayerPrefs.GetInt(AutoAttackPrefsKey, 1) != 0;
            _maxHp       = DefaultMaxHp;
            _hp          = _maxHp;
            _isDead      = false;
            _invulTimer  = 0f;
            _respawnRoutine = null;

            transform.position = spawnPos;
            transform.localScale = Vector3.one * type.ModelScale;

            MaxLevel = type.MaxLevel;
            Level    = 1;
            Xp       = 0;
            XpToNext = type.XpToNext(1);

            ResetPerkStats();

            // V6 T22-A: disable Body capsule placeholder before mesh swap.
            // Mirrors Tower.prefab Base/Top SetActive(false) pattern (Wave-7 V) and
            // Enemy.prefab MeshRenderer disable (Wave-8 X).
            var bodyChild = transform.Find("Body");
            if (bodyChild != null) bodyChild.gameObject.SetActive(false);

            var meshChild = SpawnMeshChild(type.AssetKey);
            var toonRoot  = meshChild != null ? meshChild : gameObject;

            // V6 W1-AG: scan ENTIRE hero hierarchy (Body is direct child of Hero(Clone), not toonRoot)
            FixNullMaterials(gameObject);
            MaterialController.ApplyToon(toonRoot, type.BodyColor);
            Outline.ApplyToHierarchy(toonRoot.transform);

            int skinTier = (_perks?.Count ?? 0) >= 5 ? 2 : ((_perks?.Count ?? 0) >= 2 ? 1 : 0);
            ApplySkin(skinTier);
            ApplyTintFromPrefs(type.Id);

            _animator = AnimationController.SetupAnimator(toonRoot, "Idle", "Walk");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Runtime validation: ensure animator is properly configured (controller loaded, state set, etc.)
            if (_animator != null && !AnimationController.ValidateAnimatorSetup(_animator, "Hero"))
                Debug.LogWarning("[Hero.Init] Animator validation failed — may show T-pose or stuck animation.");
#endif

            BuildAuraDecals();
            BuildPerkIcons();
        }

        private void Awake()
        {
            Current = this;
        }

        private void OnDestroy()
        {
            if (_respawnRoutine != null) StopCoroutine(_respawnRoutine);
            if (Current == this) Current = null;
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                var p = _projectiles[i];
                if (p == null) continue;
                if (HeroProjectilePool.Instance != null)
                    HeroProjectilePool.Instance.Return(p);
                else
                    Destroy(p.gameObject);
            }
            _projectiles.Clear();
            _fireTrails.Clear();
        }

        private void Update()
        {
            if (cfg == null) return;
            float dt = Time.deltaTime;

            _ultCooldown      = Mathf.Max(0f, _ultCooldown - dt);
            _ultimateCooldown = Mathf.Max(0f, _ultimateCooldown - dt);
            _cooldown         = Mathf.Max(0f, _cooldown - dt);
            if (_invulTimer > 0f) _invulTimer = Mathf.Max(0f, _invulTimer - dt);

            HandleUltimateInput();

            UpdateAuraPulse(dt);
            UpdatePerkIconsBillboard();
            UpdateAttackAnimTimer(dt);
            UpdateMovement(dt);
            UpdateCombat();
            UpdateProjectiles(dt);
        }

        // ─────────────────────────────────────────────────────────────────────
        // INTEGRATION HOOKS — résumé pour LevelRunner / WaveManager
        //
        // LevelRunner (Assets/Scripts/Systems/LevelRunner.cs) :
        //   Spawn :
        //     var heroGo = Instantiate(heroPrefab);
        //     _hero = heroGo.GetComponent<Hero>() ?? heroGo.AddComponent<Hero>();
        //     _hero.Init(heroTypeSO, spawnPos, maxX, maxZ);
        //     _hero.ApplyMetaBonuses(metaBonuses.heroDamageMul, ...);
        //     _hero.ApplySkinBonuses(equippedSkin);   // si skin equipé
        //
        //   Input forward (HeroInputAdapter ou HudController) :
        //     _hero.SetMove(inputDir.x, inputDir.y);  // appelé chaque frame
        //     _hero.ChannelingPill = pillActive;       // bloquer tir pendant pill
        //     if (ultButtonPressed) _hero.TryUlt();
        //
        //   Access for Synergies.cs :
        //     var (dmgMul, rateMul, range) = LevelRunner.Instance.Hero!.GetTowerAuraBuffs();
        //
        //   Skin visual swap (SkinSystem) :
        //     _hero.ApplySkinVisual(skinDef.AlternateGLTF != null ? skinDef.Id : heroType.AssetKey);
        //     _hero.ApplySkinBonuses(skinDef);
        //
        // WaveManager (Assets/Scripts/Systems/WaveManager.cs) :
        //   À la fin de chaque vague :
        //     LevelRunner.Instance.Hero?.OnWaveEnd();
        //
        // Economy.cs — CoinGainMul + FirstTowerFree :
        //   Quand un ennemi meurt, multiplicateur de récompense :
        //     int reward = Mathf.RoundToInt(e.Reward * hero.CoinGainMul);
        //   Quand le joueur pose une tour :
        //     bool free = hero.TryConsumeFirstTowerFree();
        //
        // Castle.cs — CastleHPMaxMul :
        //   Lors du Init castle :
        //     int hp = Mathf.RoundToInt(baseHp * hero.CastleHPMaxMul);
        // ─────────────────────────────────────────────────────────────────────
    }

    // ── Fire trail data struct (Combustion perk) ───────────────────────────────
    public sealed class FireTrail
    {
        public Vector3 Position;
        public float   ExpiresAt;
        public FireTrail(Vector3 pos, float expiresAt) { Position = pos; ExpiresAt = expiresAt; }
    }
}
