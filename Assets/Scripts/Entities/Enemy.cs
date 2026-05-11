#nullable enable
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class Enemy : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────
        private const float HitFlashDuration  = 0.09f;
        private const float DeathDuration     = 0.55f;
        private const float DustInterval      = 1.2f;
        private const float StealthRingRadius = 0.33f;

        // ── Core data ─────────────────────────────────────────────────────────
        private EnemyType? cfg;
        private float hp;
        private float maxHp;
        private float shieldHp;
        private int currentWaypoint;
        private int pathIdx;
        private PathManager? pathManager;
        private MeshRenderer? rend;
        private Color baseColor;
        private GameObject? shieldHalo;
        private EnemyPool? pool;

        // Timers (ms for consistency with JS source)
        private float summonTimer  = 0f;
        private float blastTimer   = 0f;
        private float chargeTimer  = 0f;
        private bool  _chargeActive = false;
        private float _chargeActiveTimer = 0f;

        // Death / dying
        private bool  _dying      = false;
        private float _dyingTimer = 0f;

        // Ragdoll
        private const float RagdollFadeDuration = 3f;
        private Vector3 _lastDamageDirection = Vector3.back;

        // Set by BossSystem when enraged threshold is crossed (also by self-trigger 50% HP fallback)
        private float _enragedSpeedMul    = 1f;
        private float _enragedSummonCdMul = 1f;
        private bool  _enragedSelfTriggered = false;

        // Armor break — temporary damage taken multiplier (Ballista L3 / synergy)
        private float _dmgTakenMul        = 1f;
        private float _dmgTakenMulUntil   = 0f;

        // Child GO holding the spawned GLTF mesh (null = using capsule primitive)
        private GameObject? _meshChild;

        // Cached once in Init — avoids GetComponentsInChildren alloc every frame
        private Renderer[]? _cachedRenderers;
        // Reused MPB — zero alloc per-frame tint via SetPropertyBlock
        private MaterialPropertyBlock? _mpb;
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId     = Shader.PropertyToID("_Color");
        private static readonly int _emissiveId  = Shader.PropertyToID("_EmissionColor");

        // MPBs for boss aura + stealth ring — avoid .material alloc per frame
        private MaterialPropertyBlock? _auraMpb;
        private MaterialPropertyBlock? _stealthRingMpb;

        // Animator configured by AnimationController.SetupAnimator at Init
        private Animator? _animator;
        private bool _wasWalking = false;

        // ── Hit flash ─────────────────────────────────────────────────────────
        private float _hitFlashTimer = 0f;

        // ── Freeze effect (from slow/freeze towers) ───────────────────────────
        // freeze = speed 0; slowUntilTime != 0 means active
        private float _freezeUntilTime = 0f;
        private bool  _frozenTinted    = false;

        // ── Boss aura (pulsing ring child GO) ─────────────────────────────────
        private GameObject?  _bossAuraGO;
        private MeshRenderer? _bossAuraMR;
        private bool _bossEncounteredPublished = false;

        // ── Boss skin phases (visual: scale + tint + emission) ─────────────────
        private int   _bossPhase        = 0;   // 0=init, 1=default, 2=darkred, 3=fire
        private float _bossBaseScale    = 1f;  // type.Scale captured at Init, before elite mul
        private Color _bossPhaseEmission = Color.black; // current phase emission, restored after hit flash

        // ── Apocalypse boss phases ─────────────────────────────────────────────
        private int   _apocPhase        = 0;
        private float _invulUntilTime   = 0f;
        private bool  _summonHordePending = false;
        private float _summonHordeTime  = 0f;

        // ── Stealth visual ring ────────────────────────────────────────────────
        private GameObject?  _stealthRingGO;
        private MeshRenderer? _stealthRingMR;

        // ── Dust trail ────────────────────────────────────────────────────────
        private float _dustTimer = 0f;

        // ── Footstep audio ────────────────────────────────────────────────────
        private const float StepInterval = 0.4f;
        private float _stepTimer = 0f;

        // ── Fiery trail (imp, dragon, etc.) ───────────────────────────────────
        private float _fieryTimer = 0f;
        private const float FieryInterval = 0.08f;

        // ── Fire breath (boss dragon/fire/infernal) ────────────────────────────
        private float _fireBreathTimer = 0f;
        private const float FireBreathCooldown = 3.5f;

        // ── Static mode (decoration preview) ─────────────────────────────────
        private bool  _static     = false;
        private float _staticRotY = 0f;

        // ── Public API ────────────────────────────────────────────────────────
        public EnemyType? Config              => cfg;
        public int  CurrentWaypoint           => currentWaypoint;
        public int  PathIdx                   => pathIdx;
        public bool IsDead                    { get; private set; }
        public bool IsFlyer                   => cfg?.IsFlyer ?? false;
        public bool ImmuneToFlyerBonus        => cfg?.ImmuneToFlyerBonus ?? false;
        public float HpRatio                  => maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;

        // Expose alpha so Tower can test stealth phase
        public float StealthAlpha { get; private set; } = 1f;

        // Modified by SlowEffectManager each frame (slow) or by Freeze logic below
        public float currentSpeedMul = 1f;

        // Used by EnemyPathingSystem — exposes speed without touching Transform
        public float GetEffectiveSpeed() => ComputeEffectiveSpeed();

        // Used by EnemyPathingSystem — advances waypoint index after position is applied externally
        public void AdvanceWaypoint()
        {
            if (pathManager == null || cfg == null) return;
            int wpCount = pathManager.WaypointCountOnPath(pathIdx);
            if (currentWaypoint < wpCount)
                currentWaypoint++;
        }

        // Used by EnemyPathingSystem — true when enemy is suitable for external pathing tick
        public bool IsPathable => !IsDead && !_dying && cfg != null && !cfg.IsFlyer && !_static && pathManager != null;

        // World pressure scaling (D1-04) — set once in Init
        private float pressureSpeedMul = 1f;

        // Tracks which per-type sub-pool this instance belongs to (set by EnemyPool.SpawnFromType)
        internal string _poolTypeId = "";

        // Set by EnemyPool when this instance spawns as an elite variant
        internal bool _isElite = false;

        // Called once by EnemyPool after Instantiate to back-link the pool
        public void SetPool(EnemyPool p) => pool = p;

        // Called by EnemyPool after Init when the 5% elite roll succeeds.
        public void ApplyElite()
        {
            _isElite = true;
            transform.localScale *= 1.3f;
            hp    *= 2.5f;
            maxHp *= 2.5f;
            // Gold tint via MPB — reuses the already-allocated _mpb from Init
            if (_cachedRenderers != null)
            {
                _mpb ??= new MaterialPropertyBlock();
                var gold = new Color(1f, 0.84f, 0f);
                _mpb.SetColor(_baseColorId, gold);
                _mpb.SetColor(_colorId,     gold);
                for (int i = 0; i < _cachedRenderers.Length; i++)
                    _cachedRenderers[i].SetPropertyBlock(_mpb);
            }
        }

        // Called by BossSystem when enraged phase threshold is crossed
        public void ApplyEnragedPhase(float speedMul, float summonCdMul)
        {
            _enragedSpeedMul    = speedMul;
            _enragedSummonCdMul = summonCdMul;
        }

        // Armor break — boost incoming damage for a duration (ms). Caller picks max if already active.
        public void ApplyArmorBreak(float dmgTakenMul, int durMs)
        {
            if (dmgTakenMul <= 1f || durMs <= 0) return;
            _dmgTakenMul       = Mathf.Max(_dmgTakenMul, dmgTakenMul);
            float until        = Time.time + durMs / 1000f;
            _dmgTakenMulUntil  = Mathf.Max(_dmgTakenMulUntil, until);
        }

        // Boss skin phase (apocalypse visual: scale + tint + emission). phase 1=default, 2=darkred, 3=fire.
        public void ApplyBossPhase(int phase)
        {
            if (_cachedRenderers == null || _mpb == null) return;
            if (_bossPhase == phase) return;
            _bossPhase = phase;

            float scaleMul;
            Color tint;
            Color emission;

            switch (phase)
            {
                case 2:
                    scaleMul = 1.15f;
                    tint     = new Color(0.7f, 0.3f, 0.3f);
                    emission = new Color(0.3f, 0f, 0f);
                    break;
                case 3:
                    scaleMul = 1.3f;
                    tint     = new Color(1f, 0.4f, 0.1f);
                    emission = new Color(0.8f, 0.24f, 0f);
                    break;
                default: // phase 1 — restore defaults
                    scaleMul = 1f;
                    tint     = baseColor;
                    emission = Color.black;
                    break;
            }

            // Scale applied relative to base (includes elite mul already on localScale at this point)
            float eliteMul = _isElite ? (_bossBaseScale > 0f ? transform.localScale.x / _bossBaseScale : 1f) : 1f;
            transform.localScale = Vector3.one * (_bossBaseScale * scaleMul * eliteMul);

            // Phase color BEFORE flash — flash only writes _emissiveId, tint keys are separate
            _mpb.SetColor(_baseColorId, tint);
            _mpb.SetColor(_colorId,     tint);
            _bossPhaseEmission = emission;
            // Write emission only if no hit flash is currently running (flash takes priority)
            if (_hitFlashTimer <= 0f)
                _mpb.SetColor(_emissiveId, emission);

            for (int i = 0; i < _cachedRenderers.Length; i++)
                _cachedRenderers[i].SetPropertyBlock(_mpb);

            // Phase 3: ember burst via VfxPool if available
            if (phase == 3)
                VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 0.8f, 1.8f);
        }

        // Knockback along path — rewind current waypoint progress to push enemy back.
        public void ApplyKnockback(float strength)
        {
            if (strength <= 0f || _dying || IsDead || cfg == null || cfg.IsFlyer) return;
            if (pathManager == null) return;
            int steps = Mathf.Max(1, Mathf.RoundToInt(strength));
            currentWaypoint = Mathf.Max(1, currentWaypoint - steps);
        }

        // Applied by freeze towers — stops movement and adds cyan emissive tint
        public void ApplyFreeze(float durationSec)
        {
            float until = Time.time + durationSec;
            if (until > _freezeUntilTime)
                _freezeUntilTime = until;
        }

        // ── Init ──────────────────────────────────────────────────────────────
        public void Init(EnemyType type, int assignedPathIdx = 0)
        {
            cfg = type;
            IsDead       = false;
            _dying       = false;
            _dyingTimer  = 0f;
            currentSpeedMul   = 1f;
            StealthAlpha      = 1f;
            summonTimer       = 0f;
            blastTimer        = 0f;
            chargeTimer       = 0f;
            _chargeActive     = false;
            _chargeActiveTimer = 0f;
            _enragedSpeedMul  = 1f;
            _enragedSummonCdMul = 1f;
            _enragedSelfTriggered = false;
            _dmgTakenMul      = 1f;
            _dmgTakenMulUntil = 0f;
            _hitFlashTimer    = 0f;
            _freezeUntilTime  = 0f;
            _frozenTinted     = false;
            _bossEncounteredPublished = false;
            _bossPhase        = 0;
            _bossPhaseEmission = Color.black;
            _apocPhase        = 0;
            _invulUntilTime   = 0f;
            _summonHordePending = false;
            _summonHordeTime  = 0f;
            _dustTimer        = 0f;
            _fieryTimer       = 0f;
            _stepTimer        = 0f;
            _fireBreathTimer  = 0f;
            _static           = false;
            _staticRotY       = 0f;
            _wasWalking       = false;
            _lastDamageDirection = Vector3.back;
            _isElite = false;

            // Clean up any Rigidbodies/CapsuleColliders added by ragdoll on previous life
            CleanupRagdoll();

            // D1-04 mob pressure: scale HP and speed by world pressure
            int currentWorld = LevelRunner.Instance?.CurrentLevel?.World ?? 1;
            var pressure = BalanceConfig.Get().GetPressure(currentWorld);
            hp       = type.Hp * pressure.mobHpMul;
            maxHp    = hp;
            pressureSpeedMul = pressure.mobSpeedMul;
            shieldHp = type.ShieldHP;
            pathIdx  = assignedPathIdx;
            currentWaypoint = 1; // 0 = spawn point, start moving toward 1
            transform.localScale = Vector3.one * type.Scale;
            _bossBaseScale = type.Scale;

            rend      = GetComponent<MeshRenderer>();
            baseColor = type.BodyColor;

            // Check active skin before spawning GLTF mesh
            string assetKey  = type.AssetKey;
            Color  bodyColor = type.BodyColor;
            Material? skinMat = null;

            var activeSkin = SkinSystem.Instance?.GetActiveSkin(SkinTargetType.Enemy, type.Id);
            if (activeSkin != null)
            {
                if (activeSkin.AlternateGLTF != null)
                    assetKey = activeSkin.Id;
                if (activeSkin.AlternateMaterial != null)
                    skinMat = activeSkin.AlternateMaterial;
                if (activeSkin.UseBodyColorOverride)
                    bodyColor = activeSkin.BodyColorOverride;
            }

            _meshChild = activeSkin?.AlternateGLTF != null
                ? SpawnSkinMeshChild(activeSkin.AlternateGLTF)
                : SpawnMeshChild(assetKey);
            baseColor = bodyColor;

            // Cache renderers once — hot path (UpdateStealth + hit flash run every frame per enemy)
            var meshRoot = _meshChild != null ? _meshChild : gameObject;
            _cachedRenderers = meshRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
            _mpb ??= new MaterialPropertyBlock();

            // Cel-shading toon material
            var toonRoot = meshRoot;
            if (skinMat != null)
                MaterialController.ApplyOverrideMaterial(toonRoot, skinMat);
            else
                MaterialController.ApplyToon(toonRoot, bodyColor, type.IsStealth);
            // If GLTF spawned, disable the root capsule MeshRenderer (keep collider)
            if (_meshChild != null && rend != null)
                rend.enabled = false;

            // Outline silhouette
            Outline.ApplyToHierarchy(toonRoot.transform);

            // Boss shader overlay (jellyfish / hologram)
            if (!string.IsNullOrEmpty(type.ShaderOverlay) && type.ShaderOverlay != "none")
                MaterialController.ApplyShaderOverlay(toonRoot, type.ShaderOverlay, type.BodyColor);

            // AssetVariants palette swap post-toon
            if (activeSkin != null && activeSkin.ThemeIndex >= 0)
                AssetVariants.ApplyThemeIndex(toonRoot, activeSkin.ThemeIndex);
            else if (activeSkin != null && activeSkin.UseBodyColorOverride)
                AssetVariants.ApplySkin(toonRoot, activeSkin);

            // Animations Mechanim: Idle + Walk via bool isWalking
            _animator = AnimationController.SetupAnimator(toonRoot, "Idle", type.WalkAnim);

            var col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius    = 0.3f;
                col.height    = 1f;
            }

            // Reset shield halo
            if (shieldHalo != null)
                shieldHalo.SetActive(false);
            if (shieldHp > 0f)
            {
                if (shieldHalo == null)
                    BuildShieldHalo();
                else
                    shieldHalo.SetActive(true);
            }

            // Boss aura ring
            EnsureBossAura();

            // Position + path setup
            pathManager = PathManager.Instance;
            if (type.IsFlyer)
            {
                if (Castle.Instance != null)
                    transform.position = new Vector3(transform.position.x, type.FlyHeight, transform.position.z);
                return;
            }

            if (pathManager == null || pathManager.Paths.Count == 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[Enemy] No PathManager or no paths");
#endif
                ReleaseToPool();
                return;
            }
            if (pathManager.WaypointCountOnPath(pathIdx) < 2)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"[Enemy] Path {pathIdx} too short");
#endif
                ReleaseToPool();
                return;
            }
            transform.position = pathManager.GetWaypointOnPath(pathIdx, 0) + Vector3.up * 0.5f;
        }

        // ── Ragdoll cleanup (pool reuse) ──────────────────────────────────────

        private void CleanupRagdoll()
        {
            // Re-enable root CapsuleCollider that was disabled for ragdoll
            var rootCol = GetComponent<CapsuleCollider>();
            if (rootCol != null)
                rootCol.enabled = true;

            // Re-enable Animator (will be reconfigured by AnimationController.SetupAnimator in Init)
            if (_animator != null)
                _animator.enabled = true;

            // Remove Rigidbody + dynamic CapsuleColliders from bones added during ragdoll
            // Only touch _meshChild subtree — root GO keeps its permanent CapsuleCollider
            if (_meshChild == null) return;
            var bones = _meshChild.GetComponentsInChildren<Rigidbody>(includeInactive: true);
            foreach (var rb in bones)
            {
                // Remove the companion CapsuleCollider we added (bones have no static collider originally)
                var bc = rb.GetComponent<CapsuleCollider>();
                if (bc != null)
                    Destroy(bc);
                Destroy(rb);
            }
        }

        // ── Spawn GLTF child ──────────────────────────────────────────────────

        private GameObject? SpawnMeshChild(string assetKey)
        {
            if (string.IsNullOrEmpty(assetKey)) return null;

            var registry = Resources.Load<AssetRegistry>("AssetRegistry");
            if (registry == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[Enemy] AssetRegistry not found — fallback capsule");
#endif
                return null;
            }

            var prefab = registry.Get(assetKey);
            if (prefab == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[Enemy] GLTF prefab missing for assetKey='{assetKey}' — using blue capsule fallback");
#endif
                return CreateColoredFallback(new Color(0f, 0f, 1f)); // Blue capsule
            }

            // Re-use existing GLTF child if same prefab (pool reuse: same cfg → keep mesh)
            if (_meshChild != null)
            {
                _meshChild.SetActive(true);
                return _meshChild;
            }

            var instance = Object.Instantiate(prefab, transform);
            instance.name = "Mesh_" + assetKey;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale    = Vector3.one;
            return instance;
        }

        private GameObject CreateColoredFallback(Color color)
        {
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = "FallbackCapsule";
            fallback.transform.SetParent(transform);
            fallback.transform.localPosition = Vector3.zero;
            fallback.transform.localRotation = Quaternion.identity;
            fallback.transform.localScale = Vector3.one;
            var rend = fallback.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                    { color = color };
            }
            Object.Destroy(fallback.GetComponent<Collider>());
            return fallback;
        }

        private GameObject? SpawnSkinMeshChild(GameObject? skinPrefab)
        {
            if (skinPrefab == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[Enemy] SpawnSkinMeshChild called with null skinPrefab — skipping GLTF spawn");
#endif
                return null;
            }
            if (_meshChild != null)
            {
                _meshChild.SetActive(true);
                return _meshChild;
            }
            var inst = Object.Instantiate(skinPrefab, transform);
            inst.name = "Skin_" + skinPrefab.name;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale    = Vector3.one;
            return inst;
        }

        // ── Visual helpers ────────────────────────────────────────────────────

        private void BuildShieldHalo()
        {
            shieldHalo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldHalo.name = "ShieldHalo";
            shieldHalo.transform.SetParent(transform, false);
            shieldHalo.transform.localScale = Vector3.one * 1.2f;
            Destroy(shieldHalo.GetComponent<Collider>());
            var haloRend = shieldHalo.GetComponent<MeshRenderer>();
            if (haloRend != null)
            {
                var mat = new Material(ShaderUtil.GetLitShader());
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.renderQueue = 3001;
                mat.color = new Color(1f, 0.85f, 0.1f, 0.35f);
                haloRend.material = mat;
            }
        }

        private void EnsureBossAura()
        {
            if (cfg == null || !cfg.IsBoss)
            {
                if (_bossAuraGO != null) _bossAuraGO.SetActive(false);
                return;
            }

            if (_bossAuraGO == null)
            {
                _bossAuraGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _bossAuraGO.name = "BossAura";
                _bossAuraGO.transform.SetParent(transform, false);
                _bossAuraGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                _bossAuraGO.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                _bossAuraGO.transform.localScale    = new Vector3(2.8f, 2.8f, 1f);
                Destroy(_bossAuraGO.GetComponent<Collider>());
                _bossAuraMR = _bossAuraGO.GetComponent<MeshRenderer>();
                if (_bossAuraMR != null)
                {
                    var mat = new Material(ShaderUtil.GetLitShader());
                    mat.SetFloat("_Surface", 1f);
                    mat.renderQueue = 2999;
                    mat.color = new Color(cfg.BossAuraColor.r, cfg.BossAuraColor.g, cfg.BossAuraColor.b, 0.5f);
                    _bossAuraMR.material = mat;
                    _bossAuraMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _bossAuraMR.receiveShadows = false;
                }
            }
            else
            {
                _bossAuraGO.SetActive(true);
                if (_bossAuraMR != null)
                {
                    var c = cfg.BossAuraColor;
                    _auraMpb ??= new MaterialPropertyBlock();
                    _auraMpb.SetColor(_baseColorId, new Color(c.r, c.g, c.b, 0.5f));
                    _bossAuraMR.SetPropertyBlock(_auraMpb);
                }
            }
        }

        private void EnsureStealthRing()
        {
            if (_stealthRingGO != null) return;
            _stealthRingGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _stealthRingGO.name = "StealthRing";
            _stealthRingGO.transform.SetParent(transform, false);
            _stealthRingGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _stealthRingGO.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            _stealthRingGO.transform.localScale    = Vector3.one * (StealthRingRadius * 2f);
            Destroy(_stealthRingGO.GetComponent<Collider>());
            _stealthRingMR = _stealthRingGO.GetComponent<MeshRenderer>();
            if (_stealthRingMR != null)
            {
                var mat = new Material(ShaderUtil.GetLitShader());
                mat.SetFloat("_Surface", 1f);
                mat.renderQueue = 3000;
                mat.color = new Color(1f, 0.53f, 0.13f, 0.7f);
                _stealthRingMR.material = mat;
                _stealthRingMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _stealthRingMR.receiveShadows = false;
            }
        }

        private void ApplyTint(Color tint)
        {
            if (_cachedRenderers == null || _mpb == null) return;
            _mpb.SetColor(_baseColorId, tint);
            _mpb.SetColor(_colorId, tint);
            for (int i = 0; i < _cachedRenderers.Length; i++)
                _cachedRenderers[i].SetPropertyBlock(_mpb);
        }

        // Port of _triggerHitFlash / _tickHitFeedback from Enemy.js
        private void TriggerHitFlash()
        {
            if (_cachedRenderers == null || _mpb == null) return;
            _mpb.SetColor(_emissiveId, new Color(1f, 0.125f, 0.125f));
            for (int i = 0; i < _cachedRenderers.Length; i++)
                _cachedRenderers[i].SetPropertyBlock(_mpb);
            _hitFlashTimer = HitFlashDuration;
        }

        private void ClearHitFlash()
        {
            if (_cachedRenderers == null || _mpb == null) return;
            // Restore boss phase emission (if any) instead of hard black
            _mpb.SetColor(_emissiveId, _bossPhaseEmission);
            for (int i = 0; i < _cachedRenderers.Length; i++)
                _cachedRenderers[i].SetPropertyBlock(_mpb);
        }

        private void ApplyFreezeEmissive(bool frozen)
        {
            if (_cachedRenderers == null || _mpb == null) return;
            Color emissive = frozen ? new Color(0.4f, 0.87f, 1f) : Color.black;
            _mpb.SetColor(_emissiveId, emissive);
            for (int i = 0; i < _cachedRenderers.Length; i++)
                _cachedRenderers[i].SetPropertyBlock(_mpb);
        }

        // ── Update lifecycle ──────────────────────────────────────────────────

        private void Start() { }

        private void Update()
        {
            if (cfg == null || IsDead) return;

            // Static mode: animate in place, no movement
            if (_static)
            {
                AnimationController.SetWalking(_animator, false);
                transform.rotation = Quaternion.Euler(0f, _staticRotY, 0f);
                return;
            }

            TickHitFlash();
            TickBossAura();
            TickBossEncounterPublish();
            TickApocalypseBoss();
            UpdateStealth();
            UpdateSummons();
            UpdateAoeBlast();
            UpdateCharge();
            UpdateFireBreath();
            UpdateFreeze();

            if (_dying)
            {
                _dyingTimer -= Time.deltaTime;
                if (_dyingTimer <= 0f)
                    IsDead = true;
                return;
            }

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
            Vector3 target = pathManager.GetWaypointOnPath(pathIdx, currentWaypoint) + Vector3.up * 0.5f;
            transform.position = Vector3.MoveTowards(transform.position, target, effSpeed * Time.deltaTime);

            if ((transform.position - target).sqrMagnitude < 0.01f)
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
            if (_animator != null && _animator.runtimeAnimatorController != null && cfg != null && cfg.Speed > 0f)
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
            if (cfg.IsFiery)
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

        private float ComputeEffectiveSpeed()
        {
            if (cfg == null) return 0f;
            float speed = cfg.Speed * currentSpeedMul * pressureSpeedMul * _enragedSpeedMul;
            if (_freezeUntilTime > 0f && Time.time < _freezeUntilTime)
                speed = 0f;
            if (_chargeActive)
                speed = cfg.Speed * cfg.ChargeMul * pressureSpeedMul * _enragedSpeedMul;
            return speed;
        }

        private void UpdateFlyer()
        {
            if (cfg == null) return;
            if (Castle.Instance == null) return;

            Vector3 castlePos = Castle.Instance.transform.position;
            float bob = Mathf.Sin(Time.time * 3f) * 0.15f;
            Vector3 flyTarget = new Vector3(castlePos.x, cfg.FlyHeight + bob, castlePos.z);
            float effSpeed = ComputeEffectiveSpeed();
            transform.position = Vector3.MoveTowards(transform.position, flyTarget, effSpeed * Time.deltaTime);

            // Lock Y at fly height + bob
            var pos = transform.position;
            pos.y = cfg.FlyHeight + bob;
            transform.position = pos;

            // Face castle
            Vector3 dir = castlePos - transform.position;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));

            // Fire trail in air
            if (cfg.IsFiery)
            {
                _fieryTimer -= Time.deltaTime;
                if (_fieryTimer <= 0f)
                {
                    _fieryTimer = FieryInterval;
                    VfxPool.Instance?.SpawnImpact(transform.position + Vector3.down * 0.1f,
                        new Color(1f, 0.23f, 0.063f));
                }
            }

            if ((transform.position - new Vector3(castlePos.x, transform.position.y, castlePos.z)).sqrMagnitude < 0.25f)
                OnReachedCastle();
        }

        private void TickHitFlash()
        {
            if (_hitFlashTimer <= 0f) return;
            _hitFlashTimer -= Time.deltaTime;
            if (_hitFlashTimer <= 0f)
                ClearHitFlash();
        }

        private void TickBossAura()
        {
            if (_bossAuraGO == null || _bossAuraMR == null || cfg == null) return;
            float t = Time.time * 3f;
            float scale = 1f + 0.15f * Mathf.Sin(t);
            _bossAuraGO.transform.localScale = new Vector3(scale * 2.8f, scale * 2.8f, 1f);
            Color c = cfg.BossAuraColor;
            float alpha = 0.38f + 0.27f * (Mathf.Sin(t) * 0.5f + 0.5f);
            _auraMpb ??= new MaterialPropertyBlock();
            _auraMpb.SetColor(_baseColorId, new Color(c.r, c.g, c.b, alpha));
            _bossAuraMR.SetPropertyBlock(_auraMpb);
        }

        private void TickBossEncounterPublish()
        {
            if (_bossEncounteredPublished || cfg == null || !cfg.IsBoss) return;
            _bossEncounteredPublished = true;
            EventManager.Instance?.Publish(new EnemySpawnedEvent(this));
        }

        private void TickApocalypseBoss()
        {
            if (cfg == null || !cfg.IsApocalypseBoss) return;

            // Summon horde triggered from takeDamage phase transitions
            if (_summonHordePending && Time.time >= _summonHordeTime)
            {
                _summonHordePending = false;
                for (int i = 0; i < 8; i++)
                    SpawnMinionByType("imp");
                VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 0.8f, 3f);
            }

            // Pulse aura harder during invulnerability window
            if (_invulUntilTime > 0f && Time.time < _invulUntilTime && _bossAuraMR != null && cfg != null)
            {
                float pulse = 1f + 0.5f * Mathf.Sin(Time.time * 15f);
                _bossAuraGO!.transform.localScale = new Vector3(pulse * 4.2f, pulse * 4.2f, 1f);
                Color c = cfg.BossAuraColor;
                _auraMpb ??= new MaterialPropertyBlock();
                _auraMpb.SetColor(_baseColorId, new Color(c.r, c.g, c.b, 0.7f + 0.3f * Mathf.Sin(Time.time * 15f)));
                _bossAuraMR.SetPropertyBlock(_auraMpb);
            }
        }

        private void UpdateStealth()
        {
            if (cfg == null || !cfg.IsStealth) return;
            float cycleS = cfg.StealthCycleMs > 0 ? cfg.StealthCycleMs / 1000f : 2.2f;
            float phase  = (Time.time % cycleS) / cycleS;
            bool visible = phase < 0.5f;
            float alpha  = visible ? 1f : cfg.StealthOpacity;
            StealthAlpha = alpha;
            ApplyTint(new Color(baseColor.r, baseColor.g, baseColor.b, alpha));

            // Pulsing ground ring indicator
            EnsureStealthRing();
            if (_stealthRingMR != null)
            {
                _stealthRingMpb ??= new MaterialPropertyBlock();
                if (visible)
                {
                    float pulse = 1f + 0.2f * Mathf.Sin(Time.time * 8f);
                    _stealthRingGO!.transform.localScale = Vector3.one * (StealthRingRadius * 2f * pulse);
                    _stealthRingMpb.SetColor(_baseColorId, new Color(1f, 0.53f, 0.13f,
                        0.45f + 0.35f * (Mathf.Sin(Time.time * 8f) * 0.5f + 0.5f)));
                }
                else
                {
                    _stealthRingGO!.transform.localScale = Vector3.one * (StealthRingRadius * 2f);
                    _stealthRingMpb.SetColor(_baseColorId, new Color(1f, 1f, 1f, 0.25f));
                }
                _stealthRingMR.SetPropertyBlock(_stealthRingMpb);
            }
        }

        private void UpdateSummons()
        {
            if (cfg == null || !cfg.SummonsMinions || cfg.SummonType == null) return;
            summonTimer += Time.deltaTime * 1000f;
            float effectiveCooldown = cfg.SummonCooldownMs * _enragedSummonCdMul;
            if (summonTimer >= effectiveCooldown)
            {
                summonTimer = 0f;
                SpawnMinion();
            }
        }

        private void UpdateAoeBlast()
        {
            if (cfg == null || cfg.AoeBlastMs <= 0) return;
            blastTimer += Time.deltaTime * 1000f;
            if (blastTimer >= cfg.AoeBlastMs)
            {
                blastTimer = 0f;
                EmitAoeBlast();
            }
        }

        private void UpdateCharge()
        {
            if (cfg == null || !cfg.IsBrigand || cfg.ChargeCooldownMs <= 0) return;

            if (_chargeActive)
            {
                // Charge particles
                VfxPool.Instance?.SpawnImpact(
                    transform.position + Vector3.up * 0.4f,
                    new Color(1f, 0.23f, 0.063f));

                _chargeActiveTimer -= Time.deltaTime * 1000f;
                if (_chargeActiveTimer <= 0f)
                    _chargeActive = false;
            }
            else
            {
                chargeTimer += Time.deltaTime * 1000f;
                if (chargeTimer >= cfg.ChargeCooldownMs)
                {
                    chargeTimer = 0f;
                    _chargeActive = true;
                    _chargeActiveTimer = cfg.ChargeMs;
                    VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 0.6f, 1.5f);
                    EventManager.Instance?.Publish(new BossChargeWarningEvent());
                }
            }
        }

        private void UpdateFireBreath()
        {
            if (cfg == null || !cfg.IsBoss) return;

            string id = cfg.Id ?? "";
            bool isDragonBoss = id.IndexOf("dragon", System.StringComparison.OrdinalIgnoreCase) >= 0
                             || id.IndexOf("fire",   System.StringComparison.OrdinalIgnoreCase) >= 0
                             || id.IndexOf("infernal", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isDragonBoss) return;

            _fireBreathTimer -= Time.deltaTime;
            if (_fireBreathTimer > 0f) return;

            _fireBreathTimer = FireBreathCooldown;

            // Direction vers le Castle; fallback -forward si Castle absent
            Vector3 target = Castle.Instance != null
                ? Castle.Instance.transform.position
                : transform.position + transform.forward * 8f;
            Vector3 dir = (target - transform.position).normalized;

            VfxPool.Instance?.SpawnFireBreath(
                transform.position + Vector3.up * 1.5f,
                dir,
                8f);
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

        // ── TakeDamage ────────────────────────────────────────────────────────

        public void TakeDamage(float dmg, Vector3 hitOrigin = default)
        {
            if (IsDead || _dying) return;
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
            CrowdDefense.UI.FloatingPopupController.Instance?.SpawnDamage(
                actualDmg, transform.position + Vector3.up * 1.2f, gameObject.GetInstanceID());

            // Juice screen shake on hit for bosses
            if (cfg != null && cfg.IsBoss)
                JuiceFX.Instance?.Shake(0.08f, 100);

            // Apocalypse boss phase transitions
            if (cfg != null && cfg.IsApocalypseBoss)
                TickApocalypseBossPhases(ratio);

            if (hp <= 0f)
                HandleDeath();
        }

        private void TickApocalypseBossPhases(float ratio)
        {
            if (_apocPhase < 2 && ratio <= 0.75f)
            {
                _apocPhase = 2;
                _invulUntilTime = Time.time + 5f;
                _summonHordePending = true;
                _summonHordeTime = Time.time + 1f;
                VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 1f, 4f);
                JuiceFX.Instance?.Shake(0.3f, 400);
                EventManager.Instance?.Publish(new BossPhaseChangedEvent("L'Apocalypse — Phase 2 : Invulnérable !", 2));
            }
            if (_apocPhase < 3 && ratio <= 0.50f)
            {
                _apocPhase = 3;
                // Double speed — only apply once
                pressureSpeedMul *= 2f;
                VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 1f, 4.5f);
                JuiceFX.Instance?.Shake(0.35f, 500);
                EventManager.Instance?.Publish(new BossPhaseChangedEvent("L'Apocalypse — Phase 3 : Double vitesse !", 3));
            }
            if (_apocPhase < 4 && ratio <= 0.25f)
            {
                _apocPhase = 4;
                VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 1f, 5f);
                JuiceFX.Instance?.Shake(0.5f, 700);
                EventManager.Instance?.Publish(new BossPhaseChangedEvent("L'Apocalypse — Phase 4 : ENRAGE FINAL !", 4));
                // Phase 4: start repeating aoe pulse
                InvokeRepeating(nameof(EmitAoePulse), 0f, 2f);
            }
        }

        private void EmitAoePulse()
        {
            if (_dying || IsDead) { CancelInvoke(nameof(EmitAoePulse)); return; }
            if (PlacementController.Instance == null || cfg == null) return;
            var towers = PlacementController.Instance.PlacedTowers;
            float radiusSq = 4f * 4f;
            for (int i = towers.Count - 1; i >= 0; i--)
            {
                var tower = towers[i];
                if (tower == null) continue;
                if ((tower.transform.position - transform.position).sqrMagnitude < radiusSq)
                    PlacementController.Instance.RemoveTower(tower);
            }
            VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 0.6f, 4f);
        }

        private void HandleDeath()
        {
            _dying = true;
            _dyingTimer = RagdollFadeDuration + 0.1f;

            bool isBoss   = cfg != null && (cfg.IsBoss || cfg.IsApocalypseBoss);
            bool isMedium = cfg != null && cfg.IsMidBoss;

            string deathClip = isBoss ? "enemy_die_boss" : (isMedium ? "enemy_die_medium" : "enemy_die_basic");
            AudioController.Instance?.Play(deathClip, isBoss ? 1f : 0.5f);
            VfxPool.Instance?.SpawnDeath(transform.position, baseColor, isBoss);

            if (isBoss)
            {
                JuiceFX.Instance?.Shake(0.3f, 400);
                JuiceFX.Instance?.SlowMo(0.3f, 800);
                JuiceFX.Instance?.Flash(Color.white, 250);
            }

            // Boss reward = 0× (D1-01 §3.3)
            if (!(isBoss || isMedium))
            {
                int baseReward = cfg?.Reward ?? 0;
                float coinMul  = CoinPullManager.Instance?.GetCoinMulAt(transform.position) ?? 1f;
                float streakMul = WaveManager.Instance?.StreakRewardMul ?? 1f;
                float eliteMul = _isElite ? 3f : 1f;
                int reward = Mathf.Max(1, Mathf.RoundToInt(baseReward * coinMul * streakMul * eliteMul));
#if UNITY_EDITOR
                Debug.Log($"[Enemy] killed type={cfg?.Id} baseReward={baseReward} coinMul={coinMul:F2} streakMul={streakMul:F2} reward={reward}");
#endif
                EventManager.Instance?.Publish(new EnemyKilledEvent(this, reward));
                CoinPullManager.Instance?.SpawnCoinFlyTo(transform.position, reward);
                Economy.Instance?.AddGoldFromKill(reward, transform.position + Vector3.up * 1.2f);
            }
#if UNITY_EDITOR
            else Debug.Log($"[Enemy] boss killed type={cfg?.Id} reward=0 (D1-01 boss=0x)");
#endif

            CancelInvoke(nameof(EmitAoePulse));
            WaveManager.Instance?.NotifyEnemyDied(this);

            if (this != null && gameObject != null)
                StartCoroutine(RagdollThenRelease(_lastDamageDirection));
            else
                ReleaseToPool();
        }

        // ── Ragdoll ───────────────────────────────────────────────────────────

        private System.Collections.IEnumerator RagdollThenRelease(Vector3 hitDir)
        {
            var meshRoot = _meshChild != null ? _meshChild : gameObject;

            // Disable Animator so physics drives the bones
            if (_animator != null)
                _animator.enabled = false;

            // Disable root CapsuleCollider (was isTrigger) so it doesn't interfere with ragdoll
            var rootCol = GetComponent<CapsuleCollider>();
            if (rootCol != null)
                rootCol.enabled = false;

            // Add Rigidbodies + colliders to all sub-bones that have a Transform (skip root)
            var bones = meshRoot.GetComponentsInChildren<Transform>(includeInactive: true);
            float impulseStrength = 4.5f;
            bool firstBone = true;
            foreach (var bone in bones)
            {
                // Skip the meshRoot itself and bones without a visible renderer (attach anyway for structure)
                if (bone == meshRoot.transform) continue;

                var rb = bone.gameObject.GetComponent<Rigidbody>();
                if (rb == null)
                    rb = bone.gameObject.AddComponent<Rigidbody>();
                rb.mass = 0.2f;
                rb.linearDamping = 0.5f;
                rb.angularDamping = 0.8f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                // Add a small capsule collider per bone to prevent floor clipping
                if (bone.gameObject.GetComponent<Collider>() == null)
                {
                    var bc = bone.gameObject.AddComponent<CapsuleCollider>();
                    bc.radius = 0.08f;
                    bc.height = 0.22f;
                }

                // Primary impulse on the first (topmost) bone — others get a lesser scatter
                Vector3 scatter = new Vector3(
                    UnityEngine.Random.Range(-0.4f, 0.4f),
                    UnityEngine.Random.Range(0.6f, 1.4f),
                    UnityEngine.Random.Range(-0.4f, 0.4f));
                float scale = firstBone ? 1f : 0.45f;
                rb.AddForce((hitDir * 0.7f + scatter) * impulseStrength * scale, ForceMode.Impulse);

                firstBone = false;
            }

            // Fade all renderers over RagdollFadeDuration
            float elapsed = 0f;
            while (elapsed < RagdollFadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / RagdollFadeDuration);
                if (_cachedRenderers != null && _mpb != null)
                {
                    Color faded = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                    _mpb.SetColor(_baseColorId, faded);
                    _mpb.SetColor(_colorId, faded);
                    for (int i = 0; i < _cachedRenderers.Length; i++)
                        _cachedRenderers[i].SetPropertyBlock(_mpb);
                }
                yield return null;
            }

            ReleaseToPool();
        }

        // ── Tint helpers ──────────────────────────────────────────────────────

        // Tint cyan during slow, restore base color on expiration — preserves stealth alpha
        public void SetSlowTint(bool slowed)
        {
            float a = (cfg?.IsStealth == true) ? StealthAlpha : 1f;
            Color tint = slowed
                ? new Color(0.4f, 0.9f, 1.0f, a)
                : new Color(baseColor.r, baseColor.g, baseColor.b, a);
            ApplyTint(tint);
        }

        // ── SetStatic (decoration / preview mode) ────────────────────────────
        // Freezes enemy in world space, switches to Idle animation
        public void SetStatic(float worldX, float worldZ, float rotY = 0f)
        {
            _static     = true;
            _staticRotY = rotY;
            float y = cfg != null && cfg.IsFlyer ? cfg.FlyHeight : 0f;
            transform.position = new Vector3(worldX, y, worldZ);
            transform.rotation  = Quaternion.Euler(0f, rotY, 0f);
            AnimationController.SetWalking(_animator, false);
        }

        // ── Castle reached ────────────────────────────────────────────────────

        private void OnReachedCastle()
        {
            if (IsDead || _dying) return;
            int dmg = cfg?.Damage ?? 0;
#if UNITY_EDITOR
            Debug.Log($"[Enemy] reached castle type={cfg?.Id} dmg={dmg} pathIdx={pathIdx}");
#endif
            Castle.Instance?.TakeDamage(dmg);
            EventManager.Instance?.Publish(new EnemyReachedCastleEvent(this, dmg));
            WaveManager.Instance?.NotifyEnemyDied(this);
            ReleaseToPool();
        }

        private void SpawnMinion()
        {
            if (cfg?.SummonType == null) return;
            if (EnemyPool.Instance == null) return;
            if (PathManager.Instance == null || PathManager.Instance.Paths.Count == 0) return;

            Vector3 spawnPos = transform.position + Vector3.forward * 0.5f;
            var minion = EnemyPool.Instance.SpawnFromType(cfg.SummonType, spawnPos, pathIdx);
            WaveManager.Instance?.RegisterSpawnedEnemy(minion);
#if UNITY_EDITOR
            Debug.Log($"[Enemy] boss {cfg.Id} summons {cfg.SummonType.Id}");
#endif
        }

        private void SpawnMinionByType(string typeId)
        {
            if (EnemyPool.Instance == null) return;
            var spawnType = cfg?.SummonType;
            if (spawnType == null) return;
            Vector3 spawnPos = transform.position + Vector3.forward * 0.5f;
            var minion = EnemyPool.Instance.SpawnFromType(spawnType, spawnPos, pathIdx);
            WaveManager.Instance?.RegisterSpawnedEnemy(minion);
        }

        private void EmitAoeBlast()
        {
            if (cfg == null) return;
            if (PlacementController.Instance == null) return;
            var towers = PlacementController.Instance.PlacedTowers;
            float radiusSq = cfg.AoeBlastRadius * cfg.AoeBlastRadius;
            int hit = 0;
            for (int i = towers.Count - 1; i >= 0; i--)
            {
                var tower = towers[i];
                if (tower == null) continue;
                if ((tower.transform.position - transform.position).sqrMagnitude < radiusSq)
                {
                    PlacementController.Instance.RemoveTower(tower);
                    hit++;
                }
            }
            VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 0.6f, cfg.AoeBlastRadius);
#if UNITY_EDITOR
            Debug.Log($"[Enemy] boss {cfg.Id} AoE blast radius={cfg.AoeBlastRadius} hit {hit} towers");
#endif
        }

        // ── Pool release ──────────────────────────────────────────────────────

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

        private void OnDestroy()
        {
            CancelInvoke();
        }
    }
}
