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

        // Set by BossSystem when enraged threshold is crossed
        private float _enragedSpeedMul    = 1f;
        private float _enragedSummonCdMul = 1f;

        // Child GO holding the spawned GLTF mesh (null = using capsule primitive)
        private GameObject? _meshChild;

        // Cached once in Init — avoids GetComponentsInChildren alloc every frame
        private Renderer[]? _cachedRenderers;
        // Reused MPB — zero alloc per-frame tint via SetPropertyBlock
        private MaterialPropertyBlock? _mpb;
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId     = Shader.PropertyToID("_Color");
        private static readonly int _emissiveId  = Shader.PropertyToID("_EmissionColor");

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

        // ── Fiery trail (imp, dragon, etc.) ───────────────────────────────────
        private float _fieryTimer = 0f;
        private const float FieryInterval = 0.08f;

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

        // World pressure scaling (D1-04) — set once in Init
        private float pressureSpeedMul = 1f;

        // Called once by EnemyPool after Instantiate to back-link the pool
        public void SetPool(EnemyPool p) => pool = p;

        // Called by BossSystem when enraged phase threshold is crossed
        public void ApplyEnragedPhase(float speedMul, float summonCdMul)
        {
            _enragedSpeedMul    = speedMul;
            _enragedSummonCdMul = summonCdMul;
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
            _hitFlashTimer    = 0f;
            _freezeUntilTime  = 0f;
            _frozenTinted     = false;
            _bossEncounteredPublished = false;
            _apocPhase        = 0;
            _invulUntilTime   = 0f;
            _summonHordePending = false;
            _summonHordeTime  = 0f;
            _dustTimer        = 0f;
            _fieryTimer       = 0f;
            _static           = false;
            _staticRotY       = 0f;
            _wasWalking       = false;

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

        // ── Spawn GLTF child ──────────────────────────────────────────────────

        private GameObject? SpawnMeshChild(string assetKey)
        {
            if (string.IsNullOrEmpty(assetKey)) return null;

            var registry = Resources.Load<AssetRegistry>("AssetRegistry");
            if (registry == null) return null;

            var prefab = registry.Get(assetKey);
            if (prefab == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[Enemy] AssetRegistry missing key '{assetKey}' — fallback capsule");
#endif
                return null;
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

        private GameObject? SpawnSkinMeshChild(GameObject skinPrefab)
        {
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
                    _bossAuraMR.material.color = new Color(c.r, c.g, c.b, 0.5f);
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
            _mpb.SetColor(_emissiveId, Color.black);
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
            _bossAuraMR.material.color = new Color(c.r, c.g, c.b, alpha);
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
                _bossAuraMR.material.color = new Color(c.r, c.g, c.b, 0.7f + 0.3f * Mathf.Sin(Time.time * 15f));
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
                if (visible)
                {
                    float pulse = 1f + 0.2f * Mathf.Sin(Time.time * 8f);
                    _stealthRingGO!.transform.localScale = Vector3.one * (StealthRingRadius * 2f * pulse);
                    Color rc = _stealthRingMR.material.color;
                    _stealthRingMR.material.color = new Color(1f, 0.53f, 0.13f,
                        0.45f + 0.35f * (Mathf.Sin(Time.time * 8f) * 0.5f + 0.5f));
                }
                else
                {
                    _stealthRingGO!.transform.localScale = Vector3.one * (StealthRingRadius * 2f);
                    _stealthRingMR.material.color = new Color(1f, 1f, 1f, 0.25f);
                }
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

        public void TakeDamage(float dmg)
        {
            if (IsDead || _dying) return;

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

            // Dynamic HP bar color (green → yellow → red) — port of Enemy.js hpBar color update
            float ratio = HpRatio;

            // Boss HP bar tracking
            if (cfg != null && cfg.IsBoss)
                EventManager.Instance?.Publish(new BossHpChangedEvent(ratio));

            // Hit flash + particles
            TriggerHitFlash();
            AudioController.Instance?.Play("enemy_hit", 0.4f);
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
            _dyingTimer = DeathDuration;

            bool isBoss   = cfg != null && (cfg.IsBoss || cfg.IsApocalypseBoss);
            bool isMedium = cfg != null && cfg.IsMidBoss;

            if (_animator != null)
                _animator.SetTrigger("dieTrigger");

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
                int reward = Mathf.Max(1, Mathf.RoundToInt(baseReward * coinMul * streakMul));
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

            // Delayed release (let death animation finish)
            Invoke(nameof(ReleaseToPool), DeathDuration);
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

            var minion = EnemyPool.Instance.Get();
            minion.transform.position = transform.position + Vector3.forward * 0.5f;
            minion.transform.rotation = Quaternion.identity;
            minion.Init(cfg.SummonType, pathIdx);
            WaveManager.Instance?.RegisterSpawnedEnemy(minion);
#if UNITY_EDITOR
            Debug.Log($"[Enemy] boss {cfg.Id} summons {cfg.SummonType.Id}");
#endif
        }

        private void SpawnMinionByType(string typeId)
        {
            if (EnemyPool.Instance == null) return;
            var minion = EnemyPool.Instance.Get();
            if (minion == null) return;
            // Try to find type by id from enemy pool's registry
            // Best-effort: use summon type if available, else current summon type
            var spawnType = cfg?.SummonType;
            if (spawnType == null) return;
            minion.transform.position = transform.position + Vector3.forward * 0.5f;
            minion.transform.rotation = Quaternion.identity;
            minion.Init(spawnType, pathIdx);
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
                pool.Release(this);
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
