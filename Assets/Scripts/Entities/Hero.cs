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
    /// Integration hooks for LevelRunner / WaveManager are documented at the bottom of
    /// this file (region INTEGRATION HOOKS).
    /// </summary>
    public class Hero : MonoBehaviour
    {
        // ── Singleton-ish access (cached on Awake/OnDestroy, no auto-create) ──
        public static Hero? Current { get; private set; }

        // ── Serialized ────────────────────────────────────────────────────────
        [SerializeField] private GameObject? projectilePrefab;

        // ── Config ────────────────────────────────────────────────────────────
        private HeroType? cfg;

        // ── Runtime bounds (set by LevelRunner from grid bbox) ────────────────
        private float _maxX = 59.5f;
        private float _maxZ = 59.5f;

        // ── Combat state ──────────────────────────────────────────────────────
        private float _cooldown;
        private float _attackAnimTimer;
        private bool  _running;
        private bool  _autoAttack = true;

        private const string AutoAttackPrefsKey = "hero_auto_attack_v1";

        // ── Movement ──────────────────────────────────────────────────────────
        private Vector2 _moveDir;
        private Vector2 _smoothedMoveDir;
        private const float MoveAccel = 8f;

        // ── Idle dance ────────────────────────────────────────────────────────
        private float _idleSeconds;
        private const float IdleDanceDelay  = 5f;
        private const float DanceRotAmp     = 10f;   // ±10° Y
        private const float DanceRotHz      = 0.8f;
        private const float DanceBobAmp     = 0.1f;  // ±0.1 Y
        private const float DanceBobHz      = 0.6f;

        // ── XP / Level ────────────────────────────────────────────────────────
        public int   Level     { get; private set; } = 1;
        public int   Xp        { get; private set; }
        public int   XpToNext  { get; private set; }
        public int   MaxLevel  { get; private set; }

        // ── Kill counter ──────────────────────────────────────────────────────
        private int _killCount;
        public  int KillCount => _killCount;

        // ── Perk multipliers (reset + reapplied by ApplyRunContext) ───────────
        public float FireRateMul      { get; internal set; } = 1f;
        public float RangeMul         { get; internal set; } = 1f;
        public float DamageMul        { get; internal set; } = 1f;
        public float MoveSpeedMul     { get; internal set; } = 1f;
        public float CoinGainMul      { get; internal set; } = 1f;
        public float XpMul            { get; internal set; } = 1f;
        public float CritChance       { get; internal set; }
        public float CritMul          { get; internal set; } = 2f;
        public int   CritStaggerMs    { get; internal set; }
        public int   MultiShot        { get; internal set; }
        public int   PierceCount      { get; internal set; }
        public float Lifesteal        { get; internal set; }
        public float WaveRegen        { get; internal set; }
        public int   MoveAttackPierceBonus { get; internal set; }
        public float CastleHPMaxMul   { get; internal set; } = 1f;
        public float CoinRewardMul    { get; internal set; } = 1f;
        public float TowerCostMul     { get; internal set; } = 1f;
        public float TowerFireRateAuraMul { get; internal set; } = 1f;
        public float TowerAuraRange   { get; internal set; }
        public int   AoeOnNthProjectile { get; internal set; }

        // School-specific flags (tracked for future gameplay hooks)
        public bool  ForteressePerk      { get; internal set; }
        public bool  MursPierre         { get; internal set; }
        public bool  CristalGlace       { get; internal set; }

        // Auto-attack toggle — persisted in PlayerPrefs hero_auto_attack_v1
        public bool AutoAttack
        {
            get => _autoAttack;
            set
            {
                _autoAttack = value;
                PlayerPrefs.SetInt(AutoAttackPrefsKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        // Channeling state (set externally by BluePill/item system — mirrors V5 channelingPill)
        public bool  ChannelingPill      { get; set; }

        // Projectile modifiers
        public bool  Fireball            { get; internal set; }
        public float FireballRadius      { get; internal set; } = 2f;
        public float FireballDmgMul      { get; internal set; } = 0.7f;
        public bool  Ricochet            { get; internal set; }
        public int   RicochetBounces     { get; internal set; } = 3;
        public float RicochetDecay       { get; internal set; } = 0.8f;
        public bool  Lightning           { get; internal set; }
        public int   LightningTargets    { get; internal set; } = 1;
        public float LightningDmgMul     { get; internal set; } = 0.6f;
        public bool  PierceExplode       { get; internal set; }
        public float PierceExplodeRadius { get; internal set; } = 1.5f;
        public float PierceExplodeDmgMul { get; internal set; } = 0.5f;
        public bool  Glaciation          { get; internal set; }
        public bool  Combustion          { get; internal set; }
        public bool  Pyromancie          { get; internal set; }

        // FirstTowerFree gate (consumed once)
        public bool  FirstTowerFree      { get; internal set; }
        public bool  FirstTowerFreeUsed  { get; set; }

        // ── Perk tracking ─────────────────────────────────────────────────────
        public IReadOnlyList<string> Perks => _perks;
        private readonly List<string> _perks = new();
        private readonly Dictionary<string, int> _activeTagsCount = new();
        private bool _suppressPerkVfx;

        // ── HP / respawn ──────────────────────────────────────────────────────
        private const float DefaultMaxHp    = 100f;
        private const float RespawnDelay    = 15f;
        private const float InvulDuration   = 2f;

        private float _hp;
        private float _maxHp = DefaultMaxHp;
        private bool  _isDead;
        private float _invulTimer;
        private Coroutine? _respawnRoutine;

        public float HP    => _hp;
        public float HPMax => _maxHp;
        public bool  IsAlive => !_isDead;

        public void TakeDamage(float dmg)
        {
            if (_isDead || _invulTimer > 0f || dmg <= 0f) return;
            _hp -= dmg;
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
                // Cancel if level ended (Lost or LevelComplete)
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

            // Cancel if level ended during last second
            var finalState = LevelRunner.Instance?.State;
            if (finalState == GameState.Lost || finalState == GameState.LevelComplete
                || finalState == GameState.Summary)
                yield break;

            RespawnAtCastle();
        }

        private void RespawnAtCastle()
        {
            _isDead = false;

            // Respawn at castle position or fallback to current position
            var castlePos = Castle.Instance != null
                ? Castle.Instance.transform.position
                : transform.position;
            transform.position = castlePos + Vector3.up * 0.1f;

            _hp = _maxHp * 0.5f;
            _invulTimer = InvulDuration;

            gameObject.SetActive(true);

            // Visual feedback
            JuiceFX.Instance?.Flash(new Color(0.3f, 1f, 0.4f, 0.35f), 400);
            VfxPool.Instance?.SpawnLevelUp(transform.position + Vector3.up * 1f);
            AudioController.Instance?.Play("hero_levelup", 0.9f);
            JuiceFX.Instance?.PunchScale(transform, 1.4f, 0.3f);
            FloatingPopupController.Instance?.SpawnReward(
                "RESPAWN!", transform.position + Vector3.up * 2f, Color.green);
            StartCoroutine(InvulFlashRoutine());
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
        private float _ultCooldown;

        // ── Ultimate slot 3 (R — unlock level 10, 60s CD, 5× dmg AoE r=4) ────
        private const float UltimateCooldown    = 60f;
        private const float UltimateAoeRadius   = 4f;
        private const float UltimateDmgMul      = 5f;
        private const int   UltimateUnlockLevel = 10;
        private float _ultimateCooldown;

        // ── Aura pulse ────────────────────────────────────────────────────────
        private float _heroPulseT;
        private Renderer? _auraRenderer;
        private Renderer? _haloRenderer;

        // ── Animator ──────────────────────────────────────────────────────────
        private Animator? _animator;

        // ── Perk icons (world-space quads around head) ────────────────────────
        private readonly GameObject?[] _perkIcons = new GameObject?[6];

        // ── Crown milestone visual (gold quad above head at level 10/20/30) ────
        private GameObject? _crownGo;
        private static readonly int[] CrownMilestones = { 10, 20, 30 };

        // ── Active projectile tracking (pool manages lifetime, this list for IsDone poll) ──
        private readonly List<HeroProjectile> _projectiles = new();

        // ── Fire-trail (Combustion perk) ──────────────────────────────────────
        private readonly List<FireTrail> _fireTrails = new();

        // ── Events ────────────────────────────────────────────────────────────
        public event System.Action<int, int, int>? OnLevelUp;   // (level, xp, xpToNext)
        public event System.Action?                OnUltFired;

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

            var meshChild = SpawnMeshChild(type.AssetKey);
            var toonRoot  = meshChild != null ? meshChild : gameObject;

            MaterialController.ApplyToon(toonRoot, type.BodyColor);
            Outline.ApplyToHierarchy(toonRoot.transform);

            int skinTier = (_perks?.Count ?? 0) >= 5 ? 2 : ((_perks?.Count ?? 0) >= 2 ? 1 : 0);
            ApplySkin(skinTier);
            ApplyTintFromPrefs(type.Id);

            _animator = AnimationController.SetupAnimator(toonRoot, "Idle", "Walk");

            BuildAuraDecals();
            BuildPerkIcons();
            BuildCrownQuad();
        }

        // ── Mesh spawn (mirrors Tower.SpawnMeshChild) ─────────────────────────
        private GameObject? SpawnMeshChild(string assetKey)
        {
            if (string.IsNullOrEmpty(assetKey)) return null;
            var registry = Resources.Load<AssetRegistry>("AssetRegistry");
            if (registry == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[Hero] AssetRegistry not found — using fallback primitives");
#endif
                BuildFallbackMesh();
                return null;
            }
            var prefab = registry.Get(assetKey);
            if (prefab == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[Hero] GLTF prefab missing for assetKey='{assetKey}' — using colored fallback primitives");
#endif
                BuildFallbackMesh();
                return null;
            }
            var inst = Object.Instantiate(prefab, transform);
            inst.name = "Mesh_" + assetKey;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale    = Vector3.one;
            SetupCapeCloth(inst);
            return inst;
        }

        private static void SetupCapeCloth(GameObject meshRoot)
        {
            foreach (Transform t in meshRoot.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (!n.Contains("cape") && !n.Contains("cloak")) continue;
                var smr = t.GetComponent<SkinnedMeshRenderer>();
                if (smr == null) continue;

                var cloth = t.gameObject.AddComponent<Cloth>();
                cloth.useGravity        = true;
                cloth.damping           = 0.1f;
                cloth.stretchingStiffness = 0.5f;
                cloth.bendingStiffness    = 0.1f;

                // 5 sphere colliders auto-positioned along the torso spine
                var spheres = new ClothSphereColliderPair[5];
                var root = meshRoot.transform;
                float[] offsets = { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
                for (int i = 0; i < 5; i++)
                {
                    var col = new GameObject($"CapeCollider_{i}").AddComponent<SphereCollider>();
                    col.transform.SetParent(root);
                    col.transform.localPosition = new Vector3(0f, offsets[i] * 1.4f + 0.3f, 0f);
                    col.radius = 0.18f;
                    spheres[i] = new ClothSphereColliderPair(col, null);
                }
                cloth.sphereColliders = spheres;
            }
        }

        private void BuildFallbackMesh()
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(transform);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            body.transform.localScale    = new Vector3(0.55f, 0.85f, 0.5f);
            var bodyRend = body.GetComponent<MeshRenderer>();
            if (bodyRend != null) bodyRend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                { color = new Color(0f, 1f, 0f) }; // Green cube for hero fallback
            Object.Destroy(body.GetComponent<Collider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(transform);
            head.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            head.transform.localScale    = new Vector3(0.38f, 0.35f, 0.38f);
            var headRend = head.GetComponent<MeshRenderer>();
            if (headRend != null) headRend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                { color = new Color(0f, 0.8f, 0f) }; // Dark green for head
            Object.Destroy(head.GetComponent<Collider>());
        }

        // ── Perk icons world-space ────────────────────────────────────────────
        private void BuildPerkIcons()
        {
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            for (int i = 0; i < _perkIcons.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = $"PerkIcon{i}";
                go.transform.SetParent(transform);
                go.transform.localScale = Vector3.one * 0.2f;
                Object.Destroy(go.GetComponent<Collider>());
                var mat = new Material(unlitShader != null ? unlitShader : Shader.Find("Standard")!) { color = Color.white };
                go.GetComponent<MeshRenderer>().material = mat;
                go.SetActive(false);
                _perkIcons[i] = go;
            }
        }

        private void UpdatePerkIcons()
        {
            int count = Mathf.Min(_perks.Count, _perkIcons.Length);
            for (int i = 0; i < _perkIcons.Length; i++)
            {
                var icon = _perkIcons[i];
                if (icon == null) continue;
                icon.SetActive(i < count);
                if (i >= count) continue;
                icon.GetComponent<MeshRenderer>().material.color = PerkColor(_perks[i]);
                float angle = i * (360f / Mathf.Max(count, 1)) * Mathf.Deg2Rad;
                icon.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 0.35f, 1.8f, Mathf.Sin(angle) * 0.35f);
            }
        }

        private void UpdatePerkIconsBillboard()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var rot = cam.transform.rotation;
            for (int i = 0; i < _perkIcons.Length; i++)
            {
                var icon = _perkIcons[i];
                if (icon != null && icon.activeSelf) icon.transform.rotation = rot;
            }
            if (_crownGo != null && _crownGo.activeSelf) _crownGo.transform.rotation = rot;
        }

        private static Color PerkColor(string perkId) => perkId switch
        {
            var s when s.Contains("damage") || s.Contains("crit") || s.Contains("fireball")
                    || s.Contains("multi") || s.Contains("pierce") || s.Contains("lightning")
                    || s.Contains("combustion") || s.Contains("pyromancie") => new Color(1f, 0.2f, 0.2f),
            var s when s.Contains("defense") || s.Contains("forteresse") || s.Contains("murs")
                    || s.Contains("cristal") || s.Contains("castle") => new Color(0.3f, 0.5f, 1f),
            var s when s.Contains("heal") || s.Contains("lifesteal") || s.Contains("regen")
                    || s.Contains("wave_regen") => new Color(0.2f, 0.9f, 0.3f),
            var s when s.Contains("speed") || s.Contains("move") || s.Contains("fire_rate") => new Color(1f, 0.9f, 0.1f),
            var s when s.Contains("coin") || s.Contains("xp") || s.Contains("tower") => new Color(1f, 0.6f, 0.1f),
            _ => new Color(0.9f, 0.3f, 1f),
        };

        // ── Ground aura + halo decals (ring + glow disc) ──────────────────────
        private void BuildAuraDecals()
        {
            if (cfg == null) return;

            var auraGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            auraGo.name = "HeroAura";
            auraGo.transform.SetParent(transform);
            auraGo.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            auraGo.transform.localScale    = new Vector3(0.675f * 2f, 0.01f, 0.675f * 2f);
            Object.Destroy(auraGo.GetComponent<Collider>());
            var c = cfg.AuraColor;
            var auraMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            auraMat.color = c;
            // Transparency
            SetMaterialTransparent(auraMat, c.a);
            _auraRenderer = auraGo.GetComponent<Renderer>();
            if (_auraRenderer != null) _auraRenderer.material = auraMat;

            var haloGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            haloGo.name = "HeroHalo";
            haloGo.transform.SetParent(transform);
            haloGo.transform.localPosition = new Vector3(0f, 0.21f, 0f);
            haloGo.transform.localScale    = new Vector3(1.2f * 2f, 0.01f, 1.2f * 2f);
            Object.Destroy(haloGo.GetComponent<Collider>());
            var h = cfg.HaloColor;
            var haloMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            haloMat.color = h;
            SetMaterialTransparent(haloMat, h.a);
            _haloRenderer = haloGo.GetComponent<Renderer>();
            if (_haloRenderer != null) _haloRenderer.material = haloMat;
        }

        private static void SetMaterialTransparent(Material mat, float alpha)
        {
            mat.SetFloat("_Surface", 1f);   // URP Transparent
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            var c = mat.color;
            c.a = alpha;
            mat.color = c;
            mat.renderQueue = 3000;
        }

        // ── Movement input (called by LevelRunner.UpdateHeroInput) ──────────
        public void SetMove(float dx, float dz)
        {
            float len = Mathf.Sqrt(dx * dx + dz * dz);
            _moveDir = len > 0.05f ? new Vector2(dx / len, dz / len) : Vector2.zero;
        }

        public void SetRunning(bool running) => _running = running;

        // ── Perk accumulation ─────────────────────────────────────────────────

        /// <summary>
        /// Réinitialise les stats et réapplique la liste de perks depuis zéro.
        /// Appelé par LevelRunner en début de run (applyRunContext V5 pattern).
        /// </summary>
        public void ApplyRunContext(IReadOnlyList<string> perkIds, int level = 1, int xp = 0)
        {
            ResetPerkStats();

            Level    = level;
            Xp       = xp;
            if (cfg != null) XpToNext = cfg.XpToNext(level);

            _suppressPerkVfx = true;
            PerkSystem.Instance?.ApplyPerkList(this, perkIds);
            _suppressPerkVfx = false;
            UpdatePerkIcons();
        }

        internal void AddPerkId(string id)
        {
            _perks.Add(id);
            UpdatePerkIcons();
            if (_suppressPerkVfx) return;

            var pos = transform.position;
            VfxPool.Instance?.SpawnLevelUp(pos + Vector3.up * 1.5f);
            AudioController.Instance?.Play("hero_levelup");
            JuiceFX.Instance?.PunchScale(transform, 1.3f, 0.3f);
            FloatingPopupController.Instance?.SpawnReward("LEVEL UP!", pos + Vector3.up * 2f, Color.yellow);
        }

        /// <summary>
        /// Applique les bonus méta (méta-progression, run entre sessions).
        /// Pattern direct depuis V5 applyMetaBonuses.
        /// </summary>
        public void ApplyMetaBonuses(float heroDamageMul = 1f, float heroRangeMul = 1f,
            float heroFireRateMul = 1f, float coinGainMul = 1f, float xpMul = 1f)
        {
            DamageMul   *= heroDamageMul * CrowdDefense.Systems.TalentSystem.HeroPowerMul;
            RangeMul    *= heroRangeMul;
            FireRateMul *= 1f / Mathf.Max(heroFireRateMul, 0.01f);
            CoinGainMul *= coinGainMul;
            XpMul       *= xpMul;
        }

        /// <summary>
        /// Applique les bonus d'un skin héros (V5 applySkinBonuses).
        /// Doit être appelé APRÈS ApplyRunContext (les muls sont cumulatifs).
        /// </summary>
        public void ApplySkinBonuses(SkinDef skin)
        {
            if (skin == null) return;
            if (skin.DamageMul    != 1f) DamageMul    *= skin.DamageMul;
            if (skin.RangeMul     != 1f) RangeMul     *= skin.RangeMul;
            if (skin.FireRateMul  != 1f) FireRateMul  *= 1f / Mathf.Max(skin.FireRateMul, 0.01f);
            if (skin.MoveSpeedMul != 1f) MoveSpeedMul *= skin.MoveSpeedMul;
            if (skin.CoinGainMul  != 1f) CoinGainMul  *= skin.CoinGainMul;
            if (skin.XpMul        != 1f) XpMul        *= skin.XpMul;
        }

        // ── XP / Level-up ─────────────────────────────────────────────────────
        public void GainXp(float amount)
        {
            if (Level >= MaxLevel) return;
            int earned = Mathf.Max(1, Mathf.RoundToInt(amount * XpMul));
            Xp += earned;
            CrowdDefense.UI.HeroPortraitController.Instance?.AnimateXpGain(earned);
            while (Xp >= XpToNext && Level < MaxLevel)
            {
                Xp -= XpToNext;
                Level++;
                if (cfg != null) XpToNext = cfg.XpToNext(Level);
                OnLevelUp?.Invoke(Level, Xp, XpToNext);
                CheckCrownMilestone(Level);

                var levelUpPos = transform.position;
                VfxPool.Instance?.SpawnLevelUp(levelUpPos + Vector3.up * 1.5f);
                VfxPool.Instance?.SpawnLevelUp(levelUpPos + Vector3.up * 1.8f);
                VfxPool.Instance?.SpawnConfetti(levelUpPos + Vector3.up * 1.5f, 1.5f);
                FloatingPopupController.Instance?.SpawnReward("LEVEL UP!", levelUpPos + Vector3.up * 2.5f, new Color(1f, 0.84f, 0f));
                AudioController.Instance?.Play("hero_levelup", 1f);
                JuiceFX.Instance?.SlowMo(0.5f, 500);
                JuiceFX.Instance?.Flash(new Color(1f, 0.84f, 0f, 0.4f), 200);
                JuiceFX.Instance?.PunchScale(transform, 1.5f, 0.4f);
                StartCoroutine(RimLightGlowRoutine());
            }
        }

        private System.Collections.IEnumerator RimLightGlowRoutine()
        {
            var lightGo = new GameObject("LevelUpRimLight");
            lightGo.transform.SetParent(transform);
            lightGo.transform.localPosition = Vector3.up * 1.0f;
            var light = lightGo.AddComponent<Light>();
            light.type      = LightType.Point;
            light.color     = new Color(1f, 0.84f, 0.1f);
            light.intensity = 5f;
            light.range     = 3f;

            const float duration = 0.8f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                light.intensity = Mathf.Lerp(5f, 0f, elapsed / duration);
                yield return null;
            }
            Destroy(lightGo);
        }

        // ── Crown milestone ───────────────────────────────────────────────────
        private void BuildCrownQuad()
        {
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            _crownGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _crownGo.name = "CrownMilestone";
            _crownGo.transform.SetParent(transform);
            _crownGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            _crownGo.transform.localScale    = Vector3.one * 0.45f;
            Object.Destroy(_crownGo.GetComponent<Collider>());
            var mat = new Material(unlitShader != null ? unlitShader : Shader.Find("Standard")!)
                { color = new Color(1f, 0.84f, 0f) };   // gold
            _crownGo.GetComponent<MeshRenderer>().material = mat;
            _crownGo.SetActive(false);
        }

        private void CheckCrownMilestone(int level)
        {
            bool isMilestone = System.Array.IndexOf(CrownMilestones, level) >= 0;
            if (!isMilestone) return;

            if (_crownGo != null) _crownGo.SetActive(true);

            FloatingPopupController.Instance?.SpawnReward(
                "+CROWN",
                transform.position + Vector3.up * 2.8f,
                new Color(1f, 0.84f, 0f));
        }

        // ── Ultimate ──────────────────────────────────────────────────────────
        public bool TryUlt()
        {
            if (_ultCooldown > 0f) return false;
            if (cfg == null) return false;

            _ultCooldown = cfg.UltCooldownMs / 1000f;
            FireUltFan();
            FireUltAoe();
            TriggerUltVfx();

            AudioController.Instance?.Play("hero_ult", 1f);
            JuiceFX.Instance?.Flash(new Color(1f, 0.88f, 0.5f, 0.38f), 380);
            JuiceFX.Instance?.Shake(0.4f, 450);
            OnUltFired?.Invoke();
            return true;
        }

        public float UltCooldownRemaining => _ultCooldown;

        /// <summary>
        /// 0 = ult ready; 1 = full cooldown. Useful for UI progress rings.
        /// </summary>
        public float UltCooldownFraction =>
            cfg != null && cfg.UltCooldownMs > 0
                ? _ultCooldown / (cfg.UltCooldownMs / 1000f)
                : 0f;

        // ── Skill bar API (HeroSkillBarController) ────────────────────────────

        public float GetCooldownRatio(int slotIndex) => slotIndex switch
        {
            0 => cfg != null && cfg.FireRateMs > 0 ? _cooldown / (cfg.FireRateMs / 1000f) : 0f,
            2 => UltCooldownFraction,
            3 => UltimateCooldownFraction,
            _ => 0f,
        };

        public float GetCooldownRemaining(int slotIndex) => slotIndex switch
        {
            0 => _cooldown,
            2 => _ultCooldown,
            3 => _ultimateCooldown,
            _ => 0f,
        };

        public void Cast(int slotIndex)
        {
            if (slotIndex == 0 && !_autoAttack) TryManualFire();
            if (slotIndex == 2) TryUlt();
            if (slotIndex == 3) TryUltimate();
            StartCoroutine(CastSweepRoutine(slotIndex));
        }

        // ── Ability cast radial sweep VFX ─────────────────────────────────────
        // Q(0)=red  W(1)=blue  E(2)=green — LineRenderer 64 verts, 0.4s, r 0.5→1.5, alpha 1→0
        private System.Collections.IEnumerator CastSweepRoutine(int slotIndex)
        {
            Color sweepColor = slotIndex switch
            {
                1 => new Color(0.2f, 0.4f, 1f),
                2 => new Color(0.15f, 0.9f, 0.25f),
                _ => new Color(1f, 0.18f, 0.18f),
            };

            const int   Verts    = 64;
            const float Duration = 0.4f;
            const float RadiusStart = 0.5f;
            const float RadiusEnd   = 1.5f;
            const float HeightY     = 0.15f; // slightly above ground

            var go = new GameObject("CastSweep_VFX");
            go.transform.SetParent(null); // world space, not child of hero
            go.transform.position = transform.position;

            var lr = go.AddComponent<LineRenderer>();
            lr.loop           = true;
            lr.positionCount  = Verts;
            lr.useWorldSpace  = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")
                               ?? Shader.Find("Sprites/Default")
                               ?? Shader.Find("Standard")!)
            {
                color = sweepColor
            };
            mat.SetFloat("_Surface", 1f);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            lr.material = mat;

            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                float t      = Mathf.Clamp01(elapsed / Duration);
                float radius = Mathf.Lerp(RadiusStart, RadiusEnd, t);
                float alpha  = Mathf.Lerp(1f, 0f, t);
                float width  = Mathf.Lerp(0.06f, 0.02f, t);

                lr.startWidth = width;
                lr.endWidth   = width;

                var c = sweepColor;
                c.a = alpha;
                lr.startColor = c;
                lr.endColor   = c;

                var origin = transform.position;
                for (int i = 0; i < Verts; i++)
                {
                    float angle = i * (2f * Mathf.PI / Verts);
                    lr.SetPosition(i, new Vector3(
                        origin.x + Mathf.Cos(angle) * radius,
                        origin.y + HeightY,
                        origin.z + Mathf.Sin(angle) * radius));
                }
                yield return null;
            }

            Destroy(go);
        }

        private void TryManualFire()
        {
            if (cfg == null || _running || ChannelingPill || _cooldown > 0f) return;

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
                    if (d2 < range2 && d2 < bestDist2) { bestDist2 = d2; target = e; }
                }
            }

            if (target == null) return;

            Vector3 toTarget = target.transform.position - myPos;
            toTarget.y = 0f;
            if (toTarget != Vector3.zero) transform.rotation = Quaternion.LookRotation(toTarget);

            float rateMs = cfg.FireRateMs * FireRateMul;
            _cooldown = rateMs / 1000f;
            Fire(target);
            if (Lightning) LightningStrike(target);
        }

        /// <summary>
        /// Consumes the FirstTowerFree token. Returns true and marks used if available.
        /// Called by Economy.cs when the player places the first tower.
        /// </summary>
        public bool TryConsumeFirstTowerFree()
        {
            if (!FirstTowerFree || FirstTowerFreeUsed) return false;
            FirstTowerFreeUsed = true;
            return true;
        }

        private void FireUltFan()
        {
            if (cfg == null) return;
            float baseAngle = transform.eulerAngles.y * Mathf.Deg2Rad;
            int shots = cfg.UltFanShotCount;
            float spreadRad = 52f * Mathf.Deg2Rad;
            for (int i = 0; i < shots; i++)
            {
                float t = shots > 1 ? (float)i / (shots - 1) : 0.5f;
                float angle = baseAngle - spreadRad + t * spreadRad * 2f;
                SpawnProjectileAt(angle, pieceBonus: cfg.UltFanPierceBonus,
                    damageMul: cfg.UltFanDamageMul, lifetime: 2.0f);
            }
        }

        private void FireUltAoe()
        {
            if (cfg == null || WaveManager.Instance == null) return;
            float r2 = cfg.UltAoeRadius * cfg.UltAoeRadius;
            var pos = transform.position;
            var active = WaveManager.Instance.ActiveEnemies;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var e = active[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - pos).sqrMagnitude < r2)
                    e.TakeDamage(cfg.UltAoeDamage);
            }
        }

        private void TriggerUltVfx()
        {
            if (VfxPool.Instance == null) return;
            VfxPool.Instance.SpawnImpact(transform.position + Vector3.up, new Color(1f, 0.82f, 0.22f));
        }

        // ── Ultimate ability R (slot 3) ───────────────────────────────────────
        public bool IsUltimateUnlocked => Level >= UltimateUnlockLevel;

        public float UltimateCooldownRemaining  => _ultimateCooldown;
        public float UltimateCooldownFraction   =>
            _ultimateCooldown / UltimateCooldown;

        public bool TryUltimate()
        {
            if (!IsUltimateUnlocked) return false;
            if (_ultimateCooldown > 0f) return false;
            if (WaveManager.Instance == null) return false;

            _ultimateCooldown = UltimateCooldown;

            float baseDmg = cfg != null ? cfg.UltAoeDamage : 15f;
            float dmg = baseDmg * UltimateDmgMul * DamageMul;
            float r2  = UltimateAoeRadius * UltimateAoeRadius;
            var   pos = transform.position;
            var   active = WaveManager.Instance.ActiveEnemies;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var e = active[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - pos).sqrMagnitude < r2)
                    e.TakeDamage(dmg);
            }

            VfxPool.Instance?.SpawnDeath(pos + Vector3.up, new Color(0.9f, 0.3f, 1f), intensityMul: 3.0f);
            VfxPool.Instance?.SpawnExplosion(pos, UltimateAoeRadius);
            AudioController.Instance?.Play("hero_ult", 1.2f);
            JuiceFX.Instance?.Flash(new Color(0.8f, 0.2f, 1f, 0.45f), 450);
            JuiceFX.Instance?.Shake(0.3f, 400);
            FloatingPopupController.Instance?.SpawnReward("ULTIMATE!", pos + Vector3.up * 2.5f, new Color(0.9f, 0.3f, 1f));
            OnUltFired?.Invoke();
            return true;
        }

        // ── Tower aura query (used by Synergies.cs) ───────────────────────────
        /// <summary>
        /// Returns damage and fire-rate multipliers that the hero aura grants
        /// to nearby towers. Range returned in world units.
        /// Synergies.cs queries this each LateUpdate.
        /// </summary>
        public (float dmgMul, float fireRateMul, float auraRange) GetTowerAuraBuffs()
        {
            float dmg   = 1f;
            float rate  = 1f;
            float range = 6f;

            if (_perks.Contains("coin_gain")) dmg  += 0.15f;
            if (_perks.Contains("lifesteal")) rate += 0.10f;
            if (_perks.Contains("surveillant"))
            {
                // Use the actual TowerFireRateAuraMul value set by the perk (V5 pattern)
                rate  = Mathf.Max(rate, TowerFireRateAuraMul > 1f ? TowerFireRateAuraMul : 1.20f);
                range = Mathf.Max(range, TowerAuraRange > 0f ? TowerAuraRange : 8f);
            }
            return (dmg, rate, range);
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Update()
        {
            if (cfg == null) return;
            float dt = Time.deltaTime;

            _ultCooldown      = Mathf.Max(0f, _ultCooldown - dt);
            _ultimateCooldown = Mathf.Max(0f, _ultimateCooldown - dt);
            _cooldown         = Mathf.Max(0f, _cooldown - dt);
            if (_invulTimer > 0f) _invulTimer = Mathf.Max(0f, _invulTimer - dt);

            UpdateAuraPulse(dt);
            UpdatePerkIconsBillboard();
            UpdateAttackAnimTimer(dt);
            UpdateMovement(dt);
            UpdateCombat();
            UpdateProjectiles(dt);
        }

        // ── Aura pulse ────────────────────────────────────────────────────────
        private void UpdateAuraPulse(float dt)
        {
            if (_auraRenderer == null) return;
            _heroPulseT += dt;
            float pulse = 0.5f + 0.25f * Mathf.Sin(_heroPulseT * 2.5f);
            var c = _auraRenderer.material.color;
            c.a = pulse * (cfg?.AuraColor.a ?? 0.5f);
            _auraRenderer.material.color = c;
        }

        // ── Animation timer for attack clip ──────────────────────────────────
        private void UpdateAttackAnimTimer(float dt)
        {
            if (_attackAnimTimer <= 0f) return;
            _attackAnimTimer -= dt;
            if (_attackAnimTimer <= 0f && _animator != null)
                AnimationController.SetWalking(_animator, _smoothedMoveDir.sqrMagnitude > 0.01f);
        }

        // ── Movement + bounds ─────────────────────────────────────────────────
        private void UpdateMovement(float dt)
        {
            if (cfg == null) return;

            _smoothedMoveDir = Vector2.MoveTowards(_smoothedMoveDir, _moveDir, MoveAccel * dt);
            bool moving = _smoothedMoveDir.sqrMagnitude > 0.01f;

            if (moving)
            {
                _idleSeconds = 0f;

                float speed = cfg.MoveSpeed * MoveSpeedMul * (_running ? 1.8f : 1f);
                var oldPos = transform.position;
                var pos = oldPos;
                pos.x += _smoothedMoveDir.x * speed * dt;
                pos.z += _smoothedMoveDir.y * speed * dt;
                pos.x = Mathf.Clamp(pos.x, -_maxX, _maxX);
                pos.z = Mathf.Clamp(pos.z, -_maxZ, _maxZ);

                // Grid collision: block movement onto W/L cells (V5 parity)
                if (!IsWalkableWorldPos(pos))
                    pos = oldPos;

                transform.position = pos;

                if (_attackAnimTimer <= 0f && _animator != null)
                    AnimationController.SetWalking(_animator, true);
            }
            else
            {
                _idleSeconds += dt;

                if (_attackAnimTimer <= 0f && _animator != null)
                    AnimationController.SetWalking(_animator, false);

                if (_idleSeconds >= IdleDanceDelay)
                {
                    float t = Time.time;
                    float rotY  = DanceRotAmp * Mathf.Sin(t * DanceRotHz * 2f * Mathf.PI);
                    float bobY  = DanceBobAmp * Mathf.Sin(t * DanceBobHz * 2f * Mathf.PI);
                    var basePos = transform.position;
                    basePos.y   = Mathf.Round(basePos.y * 100f) / 100f; // snap to avoid drift
                    basePos.y  += bobY;
                    transform.position = basePos;
                    var euler = transform.eulerAngles;
                    euler.y   += rotY;
                    transform.eulerAngles = euler;
                }
            }

            // Rotate toward smoothed direction (for idle orientation + attack aiming)
            if (_smoothedMoveDir.sqrMagnitude > 0.01f)
            {
                var fwd = new Vector3(_smoothedMoveDir.x, 0f, _smoothedMoveDir.y);
                if (fwd != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(fwd);
            }
        }

        // Returns false if the world position falls on a non-walkable blocking cell (W or L)
        private static bool IsWalkableWorldPos(Vector3 worldPos)
        {
            var grid = PathManager.Instance?.Grid;
            if (grid == null) return true;
            var cell = GridCoords.WorldToCell(worldPos, grid.Width, grid.Height, grid.CellSize);
            if (cell.x < 0 || cell.x >= grid.Width || cell.y < 0 || cell.y >= grid.Height) return true;
            char ch = grid.At(cell.x, cell.y);
            return ch != GridCoords.WATER && ch != GridCoords.LAVA;
        }

        // ── Combat: acquire target + shoot ───────────────────────────────────
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
                    // No target, no pending attack anim → explicit Idle
                    AnimationController.SetIdle(_animator);
                }
            }
        }

        // ── Fire ─────────────────────────────────────────────────────────────
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

            // AoeOnNthProjectile set-bonus: every N-th primary shot fires an AoE at the target
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

            // Muzzle VFX
            VfxPool.Instance?.SpawnImpact(start + baseDir * 0.4f, new Color(1f, 0.957f, 0.835f));

            // Audio canonical key from Audio.js: "hero_shoot"
            AudioController.Instance?.Play("hero_shoot", 0.7f);
        }

        // ── Lightning perk extra-hit ──────────────────────────────────────────
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
            // Shuffle Fisher-Yates
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

        // ── Projectile helpers ────────────────────────────────────────────────
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

            HeroProjectile proj;
            if (HeroProjectilePool.Instance != null)
            {
                proj = HeroProjectilePool.Instance.Get(origin);
            }
            else
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "HeroProj";
                go.transform.localScale = Vector3.one * 0.22f;
                go.transform.position   = origin;
                Object.Destroy(go.GetComponent<Collider>());
                proj = go.AddComponent<HeroProjectile>();
            }

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

        // ── Projectile update ─────────────────────────────────────────────────
        private void UpdateProjectiles(float dt)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                if (_projectiles[i] == null || _projectiles[i].IsDone)
                    _projectiles.RemoveAt(i);
            }
        }

        public void RegisterKill() => _killCount++;

        private void OnProjKill(Vector3 killPos)
        {
            RegisterKill();

            // Lifesteal: each kill restores HP to the castle (V5 pattern — lifesteal unit = HP pts)
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

        // ── Death cinematic (triggered by LevelRunner on castle death) ──────────
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

        // ── Wave regen hook (called by WaveManager at wave end) ───────────────
        /// <summary>
        /// Callback from WaveManager when a wave ends.
        /// Currently notifies Castle.Regen for the waveRegen perk stat.
        /// </summary>
        public void OnWaveEnd()
        {
            if (WaveRegen > 0 && Castle.Instance != null)
                Castle.Instance.Regen(Mathf.RoundToInt(WaveRegen));
        }

        // ── AOE blast helper (fireball / ult) ─────────────────────────────────
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

        // ── Reset helper ──────────────────────────────────────────────────────
        private void ResetPerkStats()
        {
            _perks.Clear();
            _activeTagsCount.Clear();
            FireRateMul           = 1f;
            RangeMul              = 1f;
            DamageMul             = 1f;
            MoveSpeedMul          = 1f;
            CoinGainMul           = 1f;
            XpMul                 = 1f;
            CritChance            = 0f;
            CritMul               = 2f;
            CritStaggerMs         = 0;
            MultiShot             = 0;
            PierceCount           = 0;
            Lifesteal             = 0f;
            WaveRegen             = 0f;
            MoveAttackPierceBonus = 0;
            CastleHPMaxMul        = 1f;
            CoinRewardMul         = 1f;
            TowerCostMul          = 1f;
            TowerFireRateAuraMul  = 1f;
            TowerAuraRange        = 0f;
            AoeOnNthProjectile    = 0;
            Fireball              = false;
            FireballRadius        = 2f;
            FireballDmgMul        = 0.7f;
            Ricochet              = false;
            RicochetBounces       = 3;
            RicochetDecay         = 0.8f;
            Lightning             = false;
            LightningTargets      = 1;
            LightningDmgMul       = 0.6f;
            PierceExplode         = false;
            PierceExplodeRadius   = 1.5f;
            PierceExplodeDmgMul   = 0.5f;
            Glaciation            = false;
            Combustion            = false;
            Pyromancie            = false;
            ForteressePerk        = false;
            MursPierre            = false;
            CristalGlace          = false;
            ChannelingPill        = false;
            FirstTowerFree        = false;
            FirstTowerFreeUsed    = false;
            _projFiredCount       = 0;
            _killCount            = 0;
            _fireTrails.Clear();
        }

        private void Awake()
        {
            Current = this;
            Enemy.OnDeathStatic += OnEnemyKilled;
        }

        private void OnEnemyKilled(Enemy enemy, bool isBoss)
        {
            if (!isBoss) return;
            TriggerFinisherCinematic(enemy.transform.position);
        }

        public void TriggerFinisherCinematic(Vector3 bossPos)
        {
            JuiceFX.Instance?.SlowMo(0.5f, 1000);
            VfxPool.Instance?.SpawnConfetti(bossPos + Vector3.up * 0.5f, 2.5f);
            VfxPool.Instance?.SpawnConfetti(bossPos + Vector3.up * 1.0f, 2.0f);
            AudioController.Instance?.Play("hero_ult", 1.1f);
            JuiceFX.Instance?.Flash(new Color(1f, 0.92f, 0.2f, 0.35f), 400);
            StartCoroutine(FinisherCameraZoom(bossPos));
        }

        private System.Collections.IEnumerator FinisherCameraZoom(Vector3 bossPos)
        {
            var cam = MainCameraCache.Main;
            if (cam == null) yield break;

            float origFOV = cam.fieldOfView;
            float zoomFOV = origFOV * 0.72f;
            var origPos   = cam.transform.position;

            Vector3 dirToBoss = (bossPos - cam.transform.position).normalized;
            Vector3 zoomPos   = origPos + dirToBoss * 4f;

            float t = 0f;
            const float ZoomIn  = 0.35f;
            const float Hold    = 0.65f;
            const float ZoomOut = 0.50f;

            // zoom in (unscaled — SlowMo affects Time.timeScale)
            while (t < ZoomIn)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / ZoomIn);
                cam.fieldOfView   = Mathf.Lerp(origFOV, zoomFOV, k);
                cam.transform.position = Vector3.Lerp(origPos, zoomPos, k);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(Hold);

            // zoom out
            t = 0f;
            while (t < ZoomOut)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / ZoomOut);
                cam.fieldOfView   = Mathf.Lerp(zoomFOV, origFOV, k);
                cam.transform.position = Vector3.Lerp(zoomPos, origPos, k);
                yield return null;
            }

            cam.fieldOfView        = origFOV;
            cam.transform.position = origPos;
        }

        // ── OnDestroy — return dangling projectiles to pool ──────────────────
        private void OnDestroy()
        {
            Enemy.OnDeathStatic -= OnEnemyKilled;
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

        // ── Debug stats accessor (for DebugHudController) ────────────────────
        /// <summary>
        /// Returns a human-readable snapshot of key hero stats.
        /// Only called in editor / development builds.
        /// </summary>
        public string GetDebugStats()
        {
            return $"Lv{Level} XP:{Xp}/{XpToNext} " +
                   $"dmg×{DamageMul:F2} rng×{RangeMul:F2} fr×{FireRateMul:F2} " +
                   $"ms:{MultiShot} pc:{PierceCount} crit:{CritChance:P0} " +
                   $"ult:{_ultCooldown:F1}s perks:{_perks.Count}";
        }

        // ── Tier skin: color tint via MaterialPropertyBlock (zero draw call cost) ──
        /// <summary>
        /// Applies a visual tint to all SkinnedMeshRenderer and MeshRenderer children.
        /// tier 0 = Basic (no tint), tier 1 = Veteran (silver), tier 2 = Master (gold + emission).
        /// Uses MaterialPropertyBlock — does NOT create new material instances.
        /// </summary>
        public void ApplySkin(int tier)
        {
            bool hasTint = tier > 0;
            var tintColor = tier switch
            {
                1 => new Color(0.8f, 0.8f, 0.9f),
                2 => new Color(1f, 0.85f, 0.3f),
                _ => Color.white,
            };

            var block = new MaterialPropertyBlock();

            var skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
            foreach (var r in skinnedRenderers)
            {
                r.GetPropertyBlock(block);
                if (hasTint)
                {
                    block.SetColor("_BaseColor", tintColor);
                    block.SetColor("_Color", tintColor);
                    if (tier == 2)
                        block.SetColor("_EmissionColor", tintColor * 0.2f);
                }
                else
                {
                    block.Clear();
                }
                r.SetPropertyBlock(block);
            }

            var meshRenderers = GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            foreach (var r in meshRenderers)
            {
                r.GetPropertyBlock(block);
                if (hasTint)
                {
                    block.SetColor("_BaseColor", tintColor);
                    block.SetColor("_Color", tintColor);
                    if (tier == 2)
                        block.SetColor("_EmissionColor", tintColor * 0.2f);
                }
                else
                {
                    block.Clear();
                }
                r.SetPropertyBlock(block);
            }
        }

        // Reads the persisted hero_tint PlayerPrefs key and overlays _BaseColor via MaterialPropertyBlock.
        // Called after ApplySkin so the custom tint always wins over the tier tint.
        private void ApplyTintFromPrefs(string heroId)
        {
            string hex = PlayerPrefs.GetString(UI.HeroPickScreen.TintPrefsKey(heroId), "");
            if (string.IsNullOrEmpty(hex)) return;
            if (!ColorUtility.TryParseHtmlString(hex, out var tint)) return;

            var block = new MaterialPropertyBlock();

            foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true))
            {
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", tint);
                block.SetColor("_Color", tint);
                r.SetPropertyBlock(block);
            }
            foreach (var r in GetComponentsInChildren<MeshRenderer>(includeInactive: true))
            {
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", tint);
                block.SetColor("_Color", tint);
                r.SetPropertyBlock(block);
            }
        }

        // ── Skin asset key override (for SkinSystem to redirect mesh spawn) ──
        /// <summary>
        /// Re-initializes the visual mesh child using an alternate skin asset key.
        /// Called by SkinSystem after Init if a hero skin is equipped.
        /// Does NOT reset perk stats.
        /// </summary>
        public void ApplySkinVisual(string assetKey)
        {
            // Remove existing Mesh_ children
            var toRemove = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var ch = transform.GetChild(i);
                if (ch.name.StartsWith("Mesh_")) toRemove.Add(ch);
            }
            foreach (var ch in toRemove) Destroy(ch.gameObject);

            _animator = null;
            var meshChild = SpawnMeshChild(assetKey);
            var toonRoot  = meshChild != null ? meshChild : gameObject;
            MaterialController.ApplyToon(toonRoot, cfg?.BodyColor ?? Color.white);
            Outline.ApplyToHierarchy(toonRoot.transform);
            _animator = AnimationController.SetupAnimator(toonRoot, "Idle", "Walk");
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Always-visible red sphere so Hero is easy to spot in Scene view
            Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.8f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 1.2f, 0.35f);
        }

        private void OnDrawGizmosSelected()
        {
            if (cfg == null) return;
            // Attack range
            Gizmos.color = new Color(1f, 0.84f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, cfg.Range * RangeMul);
            // Tower aura range (when active)
            if (TowerAuraRange > 0f)
            {
                Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.15f);
                Gizmos.DrawWireSphere(transform.position, TowerAuraRange);
            }
        }
#endif

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
    internal sealed class FireTrail
    {
        public Vector3 Position;
        public float   ExpiresAt;
        public FireTrail(Vector3 pos, float expiresAt) { Position = pos; ExpiresAt = expiresAt; }
    }
}
