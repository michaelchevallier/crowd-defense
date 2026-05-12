#nullable enable
using System.Collections;
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
        private Tower?  _lastDamageTower;

        // Set by BossSystem when enraged threshold is crossed (also by self-trigger 50% HP fallback)
        private float _enragedSpeedMul    = 1f;
        private float _enragedSummonCdMul = 1f;
        private bool  _enragedSelfTriggered = false;

        // Armor break — temporary damage taken multiplier (Ballista L3 / synergy)
        private float _dmgTakenMul        = 1f;
        private float _dmgTakenMulUntil   = 0f;

        // Variant modifiers (set once in ApplyVariant after Init)
        private float _variantSpeedMul    = 1f;
        private float _regenPerSec        = 0f;
        private float _dmgReduction       = 0f;
        private TrailRenderer? _variantTrail;

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

        // ── Burn DOT (from Fireball L3 / synergy) ─────────────────────────────
        private float _burnUntilTime = 0f;

        // ── Debuff icons (world-space billboard quads above HP bar) ──────────
        // Slots: 0=slow(cyan) 1=burn(orange) 2=freeze(ice blue) 3=armorBreak(purple)
        private readonly GameObject?[] _debuffIcons = new GameObject?[4];
        private static readonly Color[] DebuffColors =
        {
            new Color(0f,   1f,   1f),   // slow  — cyan
            new Color(1f,   0.5f, 0f),   // burn  — orange
            new Color(0.5f, 0.8f, 1f),   // freeze — ice blue
            new Color(0.7f, 0f,   1f),   // armor break — purple
        };

        // ── Ground decals (slow cyan / burn orange quads at feet) ────────────
        private GameObject?    _decalSlow;
        private GameObject?    _decalBurn;
        private int            _decalFrame = 0;
        private MeshRenderer?  _decalSlowRend;
        private MeshRenderer?  _decalBurnRend;
        private MaterialPropertyBlock _decalMpb = new MaterialPropertyBlock();

        // ── Spawn pop-in state ────────────────────────────────────────────────
        private Coroutine?   _popInCoroutine;

        // ── Boss aura (pulsing ring child GO) ─────────────────────────────────
        private GameObject?  _bossAuraGO;
        private MeshRenderer? _bossAuraMR;
        private bool _bossEncounteredPublished = false;

        // ── Boss skin phases (visual: scale + tint + emission) ─────────────────
        private int   _bossPhase        = 0;   // 0=init, 1=default, 2=darkred, 3=fire
        private float _bossBaseScale    = 1f;  // type.Scale captured at Init, before elite mul
        private Color _bossPhaseEmission = Color.black; // current phase emission, restored after hit flash
        private Color _currentBossTint  = Color.white;
        private Coroutine? _bossTintLerp;

        // ── Apocalypse boss phases ─────────────────────────────────────────────
        private int   _apocPhase        = 0;
        private float _invulUntilTime   = 0f;
        private bool  _summonHordePending = false;
        private float _summonHordeTime  = 0f;
        private float _damageMul        = 1f;   // Phase 4 enrage: outgoing castle damage ×2
        private float _diffRewardMul    = 1f;   // Difficulty inverse reward mul, cached at Init
        private float _aoePulseTimer    = 0f;   // Phase 4: AOE pulse every 3s

        // ── Phase 4 enrage VFX (aura particles + point light + audio loop) ────
        private ParticleSystem? _enragePS;
        private Light?          _enrageLight;
        private AudioSource?    _enrageAudio;
        private float           _enrageLightBaseIntensity = 4f;

        // ── 60% HP minion burst — 3 fast mobs spawned once (V4 parity bonus) ──
        private bool             _minionsSummoned     = false;

        // ── 30% HP enrage — red pulsing ring + castle dmg +30% ───────────────
        private bool             _enrageActive        = false;
        private LineRenderer?    _enrageRing;
        private Coroutine?       _enrageRingCoroutine;

        // ── HP bar (world-space billboard) ────────────────────────────────────
        private Transform?    _hpBarRoot;
        private Transform?    _hpBarFg;
        private MeshRenderer? _hpBarFgMR;
        private MaterialPropertyBlock? _hpBarMpb;

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

        // ── Target reticle marker ─────────────────────────────────────────────
        private int _targetedByCount = 0;
        private GameObject? _reticleGO;

        // ── Hover target highlight (yellow ring shown while tower is hovered) ──
        private GameObject? _targetHighlightGO;

        // ── Hit splash VFX (blood/dust particles on damage) ──────────────────
        private Color _hitSplashColor;
        private float _lastHitSplashTime = -1f;
        private const float HitSplashCooldown = 0.05f;

        // ── Public API ────────────────────────────────────────────────────────
        public EnemyType? Config              => cfg;
        public int  CurrentWaypoint           => currentWaypoint;
        public int  PathIdx                   => pathIdx;
        public bool IsDead                    { get; private set; }
        public bool IsFlyer                   => cfg?.IsFlyer ?? false;
        public bool ImmuneToFlyerBonus        => cfg?.ImmuneToFlyerBonus ?? false;
        public float HpRatio                  => maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
        public float CurrentHp                => hp;
        public float MaxHp                    => maxHp;

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

        // Called by Tower when this enemy becomes or stops being a target.
        public void SetTargetedBy(bool targeted)
        {
            _targetedByCount = Mathf.Max(0, _targetedByCount + (targeted ? 1 : -1));
            if (_targetedByCount > 0)
                EnsureReticle();
            else
                HideReticle();
        }

        private void EnsureReticle()
        {
            if (_reticleGO != null) { _reticleGO.SetActive(true); return; }
            _reticleGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _reticleGO.name = "ReticleMarker";
            if (_reticleGO.TryGetComponent<Collider>(out var col))
                Object.Destroy(col);
            _reticleGO.transform.SetParent(transform, false);
            float h = GetComponent<CapsuleCollider>() is CapsuleCollider cc ? cc.height : 2f;
            _reticleGO.transform.localPosition = new Vector3(0f, h * 0.55f + 0.3f, 0f);
            _reticleGO.transform.localScale = Vector3.one * 0.22f;
            var mr = _reticleGO.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(1f, 1f, 1f, 0.9f)
            };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private void HideReticle()
        {
            if (_reticleGO != null) _reticleGO.SetActive(false);
        }

        public void ShowTargetHighlight(bool visible)
        {
            if (!visible) { if (_targetHighlightGO != null) _targetHighlightGO.SetActive(false); return; }
            if (_targetHighlightGO != null) { _targetHighlightGO.SetActive(true); return; }

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "TargetHighlight";
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(2f, 2f, 1f);
            if (go.TryGetComponent<Collider>(out var col)) Object.Destroy(col);

            const int texSize = 64;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[texSize * texSize];
            float half = texSize * 0.5f;
            for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float ring = Mathf.SmoothStep(0f, 1f, (dist - 0.72f) / 0.12f)
                           * Mathf.SmoothStep(0f, 1f, (1f - dist) / 0.08f);
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(ring) * 255f);
                pixels[y * texSize + x] = new Color32(255, 220, 0, a);
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            var mr = go.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent"));
            mat.mainTexture = tex;
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3001;
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _targetHighlightGO = go;
        }

        // Fired by HandleDeath — (enemy, isBoss). LootSpawner subscribes.
        public static event System.Action<Enemy, bool>? OnDeathStatic;

        // World pressure scaling (D1-04) — set once in Init
        private float pressureSpeedMul = 1f;

        // Tracks which per-type sub-pool this instance belongs to (set by EnemyPool.SpawnFromType)
        internal string _poolTypeId = "";

        // Set by EnemyPool when this instance spawns as an elite variant
        internal bool _isElite = false;

        // 10% of non-boss enemies chase the Hero instead of the castle
        private bool _chaseHero = false;

        // Called once by EnemyPool after Instantiate to back-link the pool
        public void SetPool(EnemyPool p) => pool = p;

        // D1-04 pressure mob — multiply movement speed after spawn (stacks with pressureSpeedMul)
        public void ApplySpeedMultiplier(float mul) => pressureSpeedMul *= mul;

        void SetTint(Color c)
        {
            if (_cachedRenderers == null) return;
            _mpb ??= new MaterialPropertyBlock();
            _mpb.SetColor(_baseColorId, c);
            _mpb.SetColor(_colorId,     c);
            for (int i = 0; i < _cachedRenderers.Length; i++)
                _cachedRenderers[i].SetPropertyBlock(_mpb);
        }

        public void ApplyVariant(CrowdDefense.Data.EnemyVariant v)
        {
            switch (v)
            {
                case CrowdDefense.Data.EnemyVariant.Fast:
                    _variantSpeedMul *= 1.5f;
                    SetTint(Color.yellow);
                    SpawnVariantTrail(new Color(0.2f, 0.5f, 1f, 0.8f));   // blue
                    break;
                case CrowdDefense.Data.EnemyVariant.Tough:
                    maxHp = maxHp * 1.5f;
                    hp    = maxHp;
                    SetTint(new Color(0.6f, 0.3f, 0.1f));
                    SpawnVariantTrail(new Color(0.45f, 0.25f, 0.1f, 0.8f)); // brown
                    break;
                case CrowdDefense.Data.EnemyVariant.Regen:
                    _regenPerSec = 2f;
                    SetTint(Color.green);
                    SpawnVariantTrail(new Color(0.1f, 0.8f, 0.2f, 0.8f));  // green
                    break;
                case CrowdDefense.Data.EnemyVariant.Armored:
                    _dmgReduction = 0.3f;
                    SetTint(new Color(0.7f, 0.7f, 0.8f));
                    SpawnVariantTrail(new Color(0.6f, 0.6f, 0.65f, 0.8f)); // grey
                    break;
            }
        }

        private void SpawnVariantTrail(Color color)
        {
            if (_variantTrail != null)
                Destroy(_variantTrail.gameObject);

            var go = new GameObject("VariantTrail");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            _variantTrail               = go.AddComponent<TrailRenderer>();
            _variantTrail.time          = 0.15f;
            _variantTrail.startWidth    = 0.18f;
            _variantTrail.endWidth      = 0f;
            _variantTrail.minVertexDistance = 0.05f;

            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(0f, 1f) }
            );
            _variantTrail.colorGradient = grad;

            // Use default unlit material to avoid shader errors on trail
            _variantTrail.material = new Material(Shader.Find("Sprites/Default"));
        }

        // Called by EnemyPool after Init when the 10% elite roll succeeds (W5+, non-boss).
        public void ApplyElite()
        {
            _isElite = true;
            // Scale x1.15 — elite tier is visually distinct but not boss-sized
            float eliteScale = _bossBaseScale * 1.15f;
            if (_popInCoroutine != null)
            {
                StopCoroutine(_popInCoroutine);
                _popInCoroutine = StartCoroutine(SpawnPopIn(eliteScale, cfg?.IsBoss ?? false));
            }
            else
            {
                transform.localScale = Vector3.one * eliteScale;
            }
            // HP +50%
            hp    *= 1.5f;
            maxHp *= 1.5f;
            // Gold tint via MPB — reuses the already-allocated _mpb from Init
            if (_cachedRenderers != null)
            {
                _mpb ??= new MaterialPropertyBlock();
                var gold = new Color(1f, 0.85f, 0.2f, 1f);
                _mpb.SetColor(_baseColorId, gold);
                _mpb.SetColor(_colorId,     gold);
                for (int i = 0; i < _cachedRenderers.Length; i++)
                    _cachedRenderers[i].SetPropertyBlock(_mpb);
            }
            // Yellow scintillating trail particle
            SpawnEliteTrail();
            StartCoroutine(SpawnGroundCrack(isBoss: false));
        }

        private void SpawnEliteTrail()
        {
            var go = new GameObject("EliteTrail");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop              = true;
            main.startLifetime     = 0.5f;
            main.startSpeed        = 0.6f;
            main.startSize         = 0.08f;
            main.startColor        = new Color(1f, 0.85f, 0.2f, 0.85f);
            main.simulationSpace   = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime  = 18f;
            var shape = ps.shape;
            shape.enabled          = true;
            shape.shapeType        = ParticleSystemShapeType.Sphere;
            shape.radius           = 0.2f;
            var colorOverLifetime  = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0f), new GradientColorKey(new Color(1f, 0.6f, 0f), 1f) },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;
            ps.Play();
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

            // Look up tint from AssetVariants.BossTints registry; fallback to legacy hard-coded values.
            string bossId = cfg?.Id ?? "";
            if (AssetVariants.BossTints.TryGetValue((bossId, phase), out Color registryTint))
            {
                scaleMul = phase switch { 2 => 1.15f, 3 => 1.3f, 4 => 1.4f, _ => 1f };
                tint     = registryTint;
                emission = phase >= 2 ? registryTint * 0.35f : Color.black;
            }
            else
            {
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
            }

            // Scale applied relative to base (includes elite mul already on localScale at this point)
            float eliteMul = _isElite ? (_bossBaseScale > 0f ? transform.localScale.x / _bossBaseScale : 1f) : 1f;
            transform.localScale = Vector3.one * (_bossBaseScale * scaleMul * eliteMul);

            // Phase color — smooth lerp to avoid jarring switch on HP threshold
            _bossPhaseEmission = emission;
            if (_bossTintLerp != null) StopCoroutine(_bossTintLerp);
            _bossTintLerp = StartCoroutine(LerpBossTint(tint, emission, 0.3f));

            // Phase 3: ember burst via VfxPool if available
            if (phase == 3)
                VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 0.8f, 1.8f);
        }

        private IEnumerator LerpBossTint(Color target, Color emission, float dur)
        {
            if (_cachedRenderers == null || _mpb == null) yield break;
            Color from = _currentBossTint;
            float t    = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                Color c = Color.Lerp(from, target, t / dur);
                _mpb.SetColor(_baseColorId, c);
                _mpb.SetColor(_colorId,     c);
                if (_hitFlashTimer <= 0f)
                    _mpb.SetColor(_emissiveId, Color.Lerp(Color.black, emission, t / dur));
                for (int i = 0; i < _cachedRenderers.Length; i++)
                    _cachedRenderers[i].SetPropertyBlock(_mpb);
                yield return null;
            }
            _currentBossTint = target;
            _mpb.SetColor(_baseColorId, target);
            _mpb.SetColor(_colorId,     target);
            if (_hitFlashTimer <= 0f)
                _mpb.SetColor(_emissiveId, emission);
            for (int i = 0; i < _cachedRenderers.Length; i++)
                _cachedRenderers[i].SetPropertyBlock(_mpb);
            _bossTintLerp = null;
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

        // Applied by Fireball L3 / synergy — burn DOT tracks active duration
        public void ApplyBurn(float durationSec)
        {
            float until = Time.time + durationSec;
            if (until > _burnUntilTime)
                _burnUntilTime = until;
        }

        // ── Init ──────────────────────────────────────────────────────────────
        public void Init(EnemyType type, int assignedPathIdx = 0, float endlessMul = 1f)
        {
            cfg = type;
            _targetedByCount = 0;
            HideReticle();
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
            _variantSpeedMul  = 1f;
            _regenPerSec      = 0f;
            _dmgReduction     = 0f;
            _hitFlashTimer    = 0f;
            _freezeUntilTime  = 0f;
            _frozenTinted     = false;
            _burnUntilTime    = 0f;
            _bossEncounteredPublished = false;
            _bossPhase        = 0;
            _bossPhaseEmission = Color.black;
            _apocPhase        = 0;
            StopEnrageVFX();
            StopEnrageRing();
            _enrageActive     = false;
            _minionsSummoned  = false;
            _invulUntilTime   = 0f;
            _summonHordePending = false;
            _summonHordeTime  = 0f;
            _damageMul        = 1f;
            _diffRewardMul    = 1f;
            _aoePulseTimer    = 0f;
            _dustTimer        = 0f;
            _fieryTimer       = 0f;
            _stepTimer        = 0f;
            _fireBreathTimer  = 0f;
            _static           = false;
            _staticRotY       = 0f;
            _wasWalking       = false;
            _lastDamageDirection = Vector3.back;
            _isElite = false;
            _chaseHero = !type.IsBoss && Random.value < 0.1f;
            if (_popInCoroutine != null) { StopCoroutine(_popInCoroutine); _popInCoroutine = null; }

            // Clean up any Rigidbodies/CapsuleColliders added by ragdoll on previous life
            CleanupRagdoll();

            // D1-04 mob pressure: scale HP and speed by world pressure
            int currentWorld = LevelRunner.Instance?.CurrentLevel?.World ?? 1;
            var pressure = BalanceConfig.Get().GetPressure(currentWorld);
            float diffMul = BalanceConfig.DifficultyHpDmgMul();
            hp       = type.Hp * pressure.mobHpMul * diffMul * endlessMul;
            maxHp    = hp;
            pressureSpeedMul = pressure.mobSpeedMul;

            // Cache hit splash color once — grey dust for mechs/skeletons, red blood otherwise
            string typeIdLower = (type.Id ?? "").ToLowerInvariant();
            bool isMechanic = typeIdLower.Contains("skeleton") || typeIdLower.Contains("mech") || typeIdLower.Contains("robot");
            _hitSplashColor = isMechanic ? new Color(0.7f, 0.6f, 0.5f) : new Color(0.6f, 0.1f, 0.1f);
            _lastHitSplashTime = -1f;
            shieldHp = type.ShieldHP * diffMul * endlessMul;
            _damageMul     = diffMul * endlessMul;
            _diffRewardMul = BalanceConfig.DifficultyRewardMul();
            pathIdx  = assignedPathIdx;
            currentWaypoint = 1; // 0 = spawn point, start moving toward 1
            float typeScale  = type.Scale;
            _bossBaseScale   = typeScale;
            transform.localScale = Vector3.zero;
            _popInCoroutine  = StartCoroutine(SpawnPopIn(typeScale, type.IsBoss));

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

            // Colorblind Deuteranopia palette swap (no-op when mode is off)
            Visual.ColorblindPalette.ApplyToGameObject(toonRoot);

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

            // World-space HP bar
            BuildHpBar();
            BuildDebuffIcons();

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

            // Chase-hero tint: slight red overlay so player can spot the threat
            if (_chaseHero && _cachedRenderers != null)
            {
                _mpb ??= new MaterialPropertyBlock();
                var red = new Color(1f, 0.35f, 0.35f);
                _mpb.SetColor(_baseColorId, red);
                _mpb.SetColor(_colorId,     red);
                for (int i = 0; i < _cachedRenderers.Length; i++)
                    _cachedRenderers[i].SetPropertyBlock(_mpb);
            }

            if (type.IsBoss)
            {
                StartCoroutine(BossSpawnCinematic());
                StartCoroutine(SpawnGroundCrack(isBoss: true));
            }
        }

        // ── Ground crack VFX — boss (scale 4) or elite (scale 2) ─────────────
        private System.Collections.IEnumerator SpawnGroundCrack(bool isBoss)
        {
            const float ScaleInDuration  = 0.3f;
            const float HoldDuration     = 1.0f;
            const float FadeOutDuration  = 1.5f;

            float targetScale = isBoss ? 4f : 2f;

            var crackGo = new GameObject("GroundCrack");
            crackGo.transform.SetParent(null);
            crackGo.transform.SetPositionAndRotation(
                transform.position + Vector3.up * 0.05f,
                Quaternion.Euler(90f, 0f, 0f));
            crackGo.transform.localScale = Vector3.zero;

            var mf  = crackGo.AddComponent<MeshFilter>();
            mf.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Sprites/Default")
                      ?? Shader.Find("Hidden/InternalErrorShader")!;
            var mat = new Material(shader)
            {
                name       = "GroundCrack_Mat",
                renderQueue = 3000
            };
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend",   3f);
            mat.SetInt("_ZWrite",    0);
            mat.SetInt("_SrcBlend",  5);
            mat.SetInt("_DstBlend",  10);
            var crackColor = new Color(1f, 0.3f, 0.1f, 0.8f);
            mat.color = crackColor;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", crackColor);

            var mr = crackGo.AddComponent<MeshRenderer>();
            mr.material = mat;

            // Scale-in ease-out
            float elapsed = 0f;
            while (elapsed < ScaleInDuration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / ScaleInDuration);
                float s  = Mathf.Sin(t * Mathf.PI * 0.5f) * targetScale; // ease-out sine
                crackGo.transform.localScale = new Vector3(s, s, s);
                yield return null;
            }
            crackGo.transform.localScale = Vector3.one * targetScale;

            // Hold
            yield return new WaitForSeconds(HoldDuration);

            // Fade-out alpha 0.8 → 0
            elapsed = 0f;
            while (elapsed < FadeOutDuration)
            {
                if (crackGo == null) yield break;
                elapsed += Time.deltaTime;
                float alpha   = Mathf.Lerp(0.8f, 0f, elapsed / FadeOutDuration);
                var   fadeCol = new Color(1f, 0.3f, 0.1f, alpha);
                mat.color = fadeCol;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", fadeCol);
                yield return null;
            }

            if (crackGo != null) Destroy(crackGo);
        }

        // ── Boss spawn cinematic (1.2 s spotlight rays + bass drone) ─────────
        private System.Collections.IEnumerator BossSpawnCinematic()
        {
            const float Duration     = 1.2f;
            const int   RayCount     = 5;
            const float RayHeight    = 8f;
            const float RotSpeed     = 90f;   // deg/s

            var bossPos = transform.position;

            // ── 5 spotlight rays ─────────────────────────────────────────────
            var rays = new GameObject[RayCount];
            for (int i = 0; i < RayCount; i++)
            {
                float angle  = i * (360f / RayCount) * Mathf.Deg2Rad;
                float radius = 0.8f;
                var   offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                var go = new GameObject($"BossSpawnRay_{i}");
                go.transform.position = bossPos + offset;
                rays[i] = go;

                var lr = go.AddComponent<LineRenderer>();
                lr.positionCount  = 2;
                lr.SetPosition(0, bossPos + offset);
                lr.SetPosition(1, bossPos + offset + Vector3.up * RayHeight);
                lr.startWidth     = 0.18f;
                lr.endWidth       = 0.04f;
                lr.useWorldSpace  = true;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = Color.yellow * 2.5f;
                lr.material       = mat;
            }

            // ── Bass drone AudioSource ────────────────────────────────────────
            AudioSource? droneSource = null;
            var droneGO = new GameObject("BossSpawnDrone");
            droneSource = droneGO.AddComponent<AudioSource>();
            droneSource.loop        = false;
            droneSource.spatialBlend = 0f;
            droneSource.volume      = 1.0f;
            droneSource.pitch       = 0.6f;

            // Lowpass filter for deep rumble feel
            var lowpass = droneGO.AddComponent<AudioLowPassFilter>();
            lowpass.cutoffFrequency = 800f;
            lowpass.lowpassResonanceQ = 1.5f;

            // Try to play a boss spawn stinger; skip silently if clip absent
            AudioController.Instance?.PlayPitched("boss_spawn_drone", 1.5f, 0.6f);

            // ── Rotate rays + lerp drone pitch over duration ──────────────────
            float elapsed = 0f;
            while (elapsed < Duration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                float rot = RotSpeed * dt;
                for (int i = 0; i < RayCount; i++)
                {
                    if (rays[i] == null) continue;
                    rays[i].transform.RotateAround(bossPos, Vector3.up, rot);
                    // Update LineRenderer endpoints to match rotated position
                    var lr = rays[i].GetComponent<LineRenderer>();
                    if (lr != null)
                    {
                        var p0 = rays[i].transform.position;
                        lr.SetPosition(0, p0);
                        lr.SetPosition(1, p0 + Vector3.up * RayHeight);
                    }
                }

                // Lerp drone pitch 0.6 → 0.8
                if (droneSource != null)
                    droneSource.pitch = Mathf.Lerp(0.6f, 0.8f, elapsed / Duration);

                // Fade out rays in last 0.2 s
                if (elapsed > Duration - 0.2f)
                {
                    float alpha = (Duration - elapsed) / 0.2f;
                    for (int i = 0; i < RayCount; i++)
                    {
                        if (rays[i] == null) continue;
                        var lr = rays[i].GetComponent<LineRenderer>();
                        if (lr?.material != null)
                        {
                            var c = lr.material.color;
                            c.a = alpha;
                            lr.material.color = c;
                        }
                    }
                }

                yield return null;
            }

            // ── Cleanup ────────────────────────────────────────────────────────
            for (int i = 0; i < RayCount; i++)
                if (rays[i] != null) Object.Destroy(rays[i]);
            if (droneGO != null) Object.Destroy(droneGO);
        }

        // ── Spawn pop-in animation ────────────────────────────────────────────
        private System.Collections.IEnumerator SpawnPopIn(float targetScale, bool isBoss)
        {
            float duration  = isBoss ? 0.6f : 0.3f;
            float overshoot = isBoss ? 1.3f : 1.1f;
            float elapsed   = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / duration);
                // EaseOutBack: cubic overshoot then settle to targetScale
                float c1 = overshoot - 1f;
                float c3 = c1 + 1f;
                float s  = c3 * t * t * t - c1 * t * t;
                transform.localScale = Vector3.one * (targetScale * Mathf.Max(0f, s));
                yield return null;
            }
            transform.localScale = Vector3.one * targetScale;
            _popInCoroutine = null;
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
            if (_meshChild != null && _meshChild.name == "Mesh_" + assetKey)
            {
                _meshChild.SetActive(true);
                return _meshChild;
            }

            // Mismatch: destroy stale mesh from previous EnemyType
            if (_meshChild != null)
                Object.Destroy(_meshChild);
            _meshChild = null;

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
            if (_meshChild != null && _meshChild.name == "Skin_" + skinPrefab.name)
            {
                _meshChild.SetActive(true);
                return _meshChild;
            }

            // Mismatch: destroy stale mesh from previous skin
            if (_meshChild != null)
                Object.Destroy(_meshChild);
            _meshChild = null;

            var inst = Object.Instantiate(skinPrefab, transform);
            inst.name = "Skin_" + skinPrefab.name;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale    = Vector3.one;
            return inst;
        }

        // ── Visual helpers ────────────────────────────────────────────────────

        private void BuildHpBar()
        {
            bool isBoss = cfg != null && (cfg.IsBoss || cfg.IsApocalypseBoss);
            float barScale = isBoss ? 2f : 1f;

            // Re-use existing bar if already built (pool reuse)
            if (_hpBarRoot == null)
            {
                // Background (red)
                var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
                bg.name = "HPBarBg";
                Object.Destroy(bg.GetComponent<Collider>());
                _hpBarRoot = bg.transform;
                _hpBarRoot.SetParent(transform, false);

                var bgMR = bg.GetComponent<MeshRenderer>();
                if (bgMR != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                    mat.color = Color.red;
                    bgMR.material = mat;
                    bgMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    bgMR.receiveShadows = false;
                }

                // Foreground (green, child of bg)
                var fg = GameObject.CreatePrimitive(PrimitiveType.Quad);
                fg.name = "HPBarFg";
                Object.Destroy(fg.GetComponent<Collider>());
                _hpBarFg = fg.transform;
                _hpBarFg.SetParent(_hpBarRoot, false);
                _hpBarFg.localPosition = Vector3.zero;
                _hpBarFg.localScale    = Vector3.one;

                _hpBarFgMR = fg.GetComponent<MeshRenderer>();
                if (_hpBarFgMR != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                    mat.color = Color.green;
                    _hpBarFgMR.material = mat;
                    _hpBarFgMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _hpBarFgMR.receiveShadows = false;
                }

                _hpBarMpb = new MaterialPropertyBlock();
            }

            _hpBarRoot.localPosition = new Vector3(0f, 1.5f, 0f);
            _hpBarRoot.localScale    = new Vector3(barScale, barScale * 0.1f, 1f);
            _hpBarRoot.gameObject.SetActive(false); // hidden at full HP
        }

        private void UpdateHpBar()
        {
            if (_hpBarRoot == null || _hpBarFg == null || _hpBarFgMR == null || _hpBarMpb == null) return;

            float ratio = HpRatio;

            // Visibility
            bool visible = ratio < 1f;
            if (_hpBarRoot.gameObject.activeSelf != visible)
                _hpBarRoot.gameObject.SetActive(visible);

            if (!visible) return;

            // Width: pivot is center — shift fg so left edge stays fixed
            _hpBarFg.localScale    = new Vector3(ratio, 1f, 1f);
            _hpBarFg.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, -0.001f);

            // Color: red → green
            Color barColor = Color.Lerp(Color.red, Color.green, ratio);
            _hpBarMpb.SetColor(_baseColorId, barColor);
            _hpBarMpb.SetColor(_colorId,     barColor);
            _hpBarFgMR.SetPropertyBlock(_hpBarMpb);

            // Billboard: face camera
            var camFwd = MainCameraCache.Main;
            if (camFwd != null)
                _hpBarRoot.rotation = Quaternion.LookRotation(camFwd.transform.forward);
        }

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
            UpdateFreeze();
            UpdateDebuffIcons();
            UpdateGroundDecals();

            if (_dying)
            {
                _dyingTimer -= Time.deltaTime;
                if (_dyingTimer <= 0f)
                    IsDead = true;
                return;
            }

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
            float speed = cfg.Speed * currentSpeedMul * pressureSpeedMul * _enragedSpeedMul * _variantSpeedMul;
            if (_freezeUntilTime > 0f && Time.time < _freezeUntilTime)
                speed = 0f;
            if (_chargeActive)
                speed = cfg.Speed * cfg.ChargeMul * pressureSpeedMul * _enragedSpeedMul * _variantSpeedMul;
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

        private void TickEnrageLight()
        {
            if (_enrageLight == null || !_enrageLight.gameObject.activeSelf) return;
            _enrageLight.intensity = _enrageLightBaseIntensity + Random.Range(-1.2f, 1.2f);
        }

        private void TickBossEncounterPublish()
        {
            if (_bossEncounteredPublished || cfg == null || !cfg.IsBoss) return;
            _bossEncounteredPublished = true;
            EventManager.Instance?.Publish(new EnemySpawnedEvent(this));
        }

        private void StartEnrageVFX()
        {
            // Particle aura — red/orange flames radius 1.5m, 50/sec
            if (_enragePS == null)
            {
                var psGO = new GameObject("EnrageAura");
                psGO.transform.SetParent(transform, false);
                psGO.transform.localPosition = Vector3.up * 0.5f;
                _enragePS = psGO.AddComponent<ParticleSystem>();
                var main = _enragePS.main;
                main.loop           = true;
                main.startLifetime  = 0.6f;
                main.startSpeed     = 1.8f;
                main.startSize      = 0.35f;
                main.startColor     = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.15f, 0f, 0.9f), new Color(1f, 0.55f, 0.05f, 0.7f));
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = _enragePS.emission;
                emission.rateOverTime = 50f;
                var shape = _enragePS.shape;
                shape.enabled     = true;
                shape.shapeType   = ParticleSystemShapeType.Sphere;
                shape.radius      = 1.5f;
            }
            _enragePS.gameObject.SetActive(true);
            if (!_enragePS.isPlaying) _enragePS.Play();

            // Point light — red, intensity 4, range 5m, flicker via Random in Update
            if (_enrageLight == null)
            {
                var lightGO = new GameObject("EnrageLight");
                lightGO.transform.SetParent(transform, false);
                lightGO.transform.localPosition = Vector3.up * 1f;
                _enrageLight = lightGO.AddComponent<Light>();
                _enrageLight.type      = LightType.Point;
                _enrageLight.color     = new Color(1f, 0.15f, 0.05f);
                _enrageLight.range     = 5f;
                _enrageLight.intensity = _enrageLightBaseIntensity;
                _enrageLight.shadows   = LightShadows.None;
            }
            _enrageLight.gameObject.SetActive(true);

            // Audio loop — child AudioSource playing boss_enrage_loop
            if (_enrageAudio == null)
            {
                var audioGO = new GameObject("EnrageAudio");
                audioGO.transform.SetParent(transform, false);
                _enrageAudio             = audioGO.AddComponent<AudioSource>();
                _enrageAudio.spatialBlend = 1f;
                _enrageAudio.loop        = true;
                _enrageAudio.volume      = 0.7f;
                _enrageAudio.maxDistance = 20f;
                _enrageAudio.rolloffMode = AudioRolloffMode.Linear;
                var clip = AudioController.Instance?.GetClip("boss_enrage_loop");
                if (clip != null) _enrageAudio.clip = clip;
            }
            _enrageAudio.gameObject.SetActive(true);
            if (_enrageAudio.clip != null && !_enrageAudio.isPlaying) _enrageAudio.Play();
        }

        private void StopEnrageVFX()
        {
            if (_enragePS != null)   { _enragePS.Stop(true, ParticleSystemStopBehavior.StopEmitting); _enragePS.gameObject.SetActive(false); }
            if (_enrageLight != null) _enrageLight.gameObject.SetActive(false);
            if (_enrageAudio != null) { _enrageAudio.Stop(); _enrageAudio.gameObject.SetActive(false); }
        }

        private void StartEnrageRing()
        {
            const int   Verts  = 32;
            const float Radius = 2f;

            var ringGO = new GameObject("EnrageRing");
            ringGO.transform.SetParent(transform, false);
            ringGO.transform.localPosition = Vector3.zero;

            _enrageRing                  = ringGO.AddComponent<LineRenderer>();
            _enrageRing.loop             = true;
            _enrageRing.positionCount    = Verts;
            _enrageRing.startWidth       = 0.12f;
            _enrageRing.endWidth         = 0.12f;
            _enrageRing.useWorldSpace    = false;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")
                               ?? Shader.Find("Sprites/Default")
                               ?? Shader.Find("Standard")!) { name = "EnrageRing_Mat" };
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            _enrageRing.material = mat;

            for (int i = 0; i < Verts; i++)
            {
                float angle = i / (float)Verts * Mathf.PI * 2f;
                _enrageRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * Radius, 0.1f, Mathf.Sin(angle) * Radius));
            }

            _enrageRing.startColor = new Color(1f, 0.05f, 0.05f, 0.5f);
            _enrageRing.endColor   = new Color(1f, 0.05f, 0.05f, 0.5f);

            _enrageRingCoroutine = StartCoroutine(PulseEnrageRing());
        }

        private IEnumerator PulseEnrageRing()
        {
            const float PulseHz = 2f;
            while (_enrageRing != null)
            {
                float alpha = 0.25f + 0.25f * Mathf.Sin(Time.time * Mathf.PI * 2f * PulseHz);
                var col = new Color(1f, 0.05f, 0.05f, alpha);
                _enrageRing.startColor = col;
                _enrageRing.endColor   = col;
                yield return null;
            }
        }

        private void StopEnrageRing()
        {
            if (_enrageRingCoroutine != null) { StopCoroutine(_enrageRingCoroutine); _enrageRingCoroutine = null; }
            if (_enrageRing != null)
            {
                if (_enrageRing.gameObject != null) Object.Destroy(_enrageRing.gameObject);
                _enrageRing = null;
            }
        }

        private void TickApocalypseBoss()
        {
            if (cfg == null || !cfg.IsApocalypseBoss) return;

            // Phase 3 skeleton summons (delayed 1s after phase entry)
            if (_summonHordePending && Time.time >= _summonHordeTime)
            {
                _summonHordePending = false;
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * 90f * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2f;
                    SpawnMinionAt(transform.position + offset);
                }
                VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 0.8f, 3f);
            }

            // Phase 4 AOE pulse timer (every 3s)
            if (_apocPhase >= 4 && !_dying && !IsDead)
            {
                _aoePulseTimer -= Time.deltaTime;
                if (_aoePulseTimer <= 0f)
                {
                    _aoePulseTimer = 3f;
                    TelegraphAoePulse();
                }
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

        private void BuildDebuffIcons()
        {
            if (_debuffIcons[0] != null) return; // already built (pool reuse)
            float spread = 0.25f;
            float startX = -spread * 1.5f;
            for (int i = 0; i < 4; i++)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = $"DebuffIcon{i}";
                Object.Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(transform, false);
                quad.transform.localPosition = new Vector3(startX + i * spread, 1.7f, 0f);
                quad.transform.localScale    = Vector3.one * 0.2f;
                var mr = quad.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                    mat.color = DebuffColors[i];
                    mr.material = mat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }
                quad.SetActive(false);
                _debuffIcons[i] = quad;
            }
        }

        private void UpdateDebuffIcons()
        {
            if (_debuffIcons[0] == null) return;
            float now = Time.time;
            bool slow  = currentSpeedMul < 0.99f;
            bool burn  = _burnUntilTime > 0f && now < _burnUntilTime;
            bool freeze = _freezeUntilTime > 0f && now < _freezeUntilTime;
            bool armor = _dmgTakenMulUntil > 0f && now < _dmgTakenMulUntil;
            bool[] active = { slow, burn, freeze, armor };
            for (int i = 0; i < 4; i++)
            {
                if (_debuffIcons[i] == null) continue;
                bool show = active[i];
                if (_debuffIcons[i].activeSelf != show)
                    _debuffIcons[i].SetActive(show);
                if (show && MainCameraCache.Main != null)
                    _debuffIcons[i].transform.rotation = Quaternion.LookRotation(MainCameraCache.Main.transform.forward);
            }
        }

        private void UpdateGroundDecals()
        {
            if (++_decalFrame % 2 != 0) return; // throttle: every 2 frames

            float now  = Time.time;
            bool slow  = currentSpeedMul < 0.99f;
            bool burn  = _burnUntilTime > 0f && now < _burnUntilTime;

            // ── Slow decal (cyan glow) ──────────────────────────────────────
            if (slow)
            {
                if (_decalSlow == null)
                {
                    _decalSlow = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    _decalSlow.name = "DecalSlow";
                    Object.Destroy(_decalSlow.GetComponent<Collider>());
                    _decalSlow.transform.SetParent(transform, false);
                    _decalSlow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    _decalSlow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    _decalSlow.transform.localScale    = Vector3.one * 1.5f;
                    _decalSlowRend = _decalSlow.GetComponent<MeshRenderer>();
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                    mat.color = new Color(0f, 1f, 1f, 0.45f);
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // transparent
                    _decalSlowRend.material = mat;
                    _decalSlowRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _decalSlowRend.receiveShadows = false;
                }
                if (!_decalSlow.activeSelf) _decalSlow.SetActive(true);
                // alpha pulse 0.3-0.6
                float alpha = 0.3f + 0.3f * (0.5f + 0.5f * Mathf.Sin(now * 4f));
                _decalMpb.Clear();
                _decalMpb.SetColor("_BaseColor", new Color(0f, 1f, 1f, alpha));
                _decalSlowRend!.SetPropertyBlock(_decalMpb);
            }
            else if (_decalSlow != null && _decalSlow.activeSelf)
                _decalSlow.SetActive(false);

            // ── Burn decal (orange-red) + sparks ───────────────────────────
            if (burn)
            {
                if (_decalBurn == null)
                {
                    _decalBurn = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    _decalBurn.name = "DecalBurn";
                    Object.Destroy(_decalBurn.GetComponent<Collider>());
                    _decalBurn.transform.SetParent(transform, false);
                    _decalBurn.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                    _decalBurn.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    _decalBurn.transform.localScale    = Vector3.one * 1.5f;
                    _decalBurnRend = _decalBurn.GetComponent<MeshRenderer>();
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                    mat.color = new Color(1f, 0.3f, 0f, 0.4f);
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                    _decalBurnRend.material = mat;
                    _decalBurnRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _decalBurnRend.receiveShadows = false;
                }
                if (!_decalBurn.activeSelf) _decalBurn.SetActive(true);
                // occasional spark at feet
                if (UnityEngine.Random.value < 0.15f)
                    VfxPool.Instance?.SpawnImpact(transform.position + Vector3.up * 0.1f, new Color(1f, 0.45f, 0f));
            }
            else if (_decalBurn != null && _decalBurn.activeSelf)
                _decalBurn.SetActive(false);
        }

        // ── TakeDamage ────────────────────────────────────────────────────────

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
            bool isCrit    = actualDmg > maxHp * 0.08f;
            var  popup     = CrowdDefense.UI.FloatingPopupController.Instance;
            if (actualDmg >= 5f)
            {
                if (isBossHit)
                    popup?.SpawnReward($"-{Mathf.RoundToInt(actualDmg)}", transform.position + Vector3.up * 1.2f, Color.white);
                else if (isCrit)
                {
                    popup?.SpawnCrit(actualDmg, transform.position + Vector3.up * 1.2f, gameObject.GetInstanceID());
                    JuiceFX.Instance?.ShakeOnCritHit();
                }
                else
                    popup?.SpawnReward($"-{Mathf.RoundToInt(actualDmg)}", transform.position + Vector3.up * 1.2f, Color.white);
            }

            // Juice screen shake on hit for bosses
            if (cfg != null && cfg.IsBoss)
                JuiceFX.Instance?.Shake(0.08f, 100);

            // Apocalypse boss phase transitions
            if (cfg != null && cfg.IsApocalypseBoss)
                TickApocalypseBossPhases(ratio);

            // Blood/dust splash particles on hit
            SpawnHitSplash(actualDmg);

            if (hp <= 0f)
                HandleDeath();
        }

        private void SpawnHitSplash(float actualDmg)
        {
            if (VfxPool.Instance == null) return;
            float now = Time.time;
            if (now - _lastHitSplashTime < HitSplashCooldown) return;
            _lastHitSplashTime = now;

            Vector3 splashPos = transform.position + Vector3.up * 0.5f;
            VfxPool.Instance.SpawnSpark(splashPos, _hitSplashColor);
            VfxPool.Instance.SpawnSpark(splashPos + Vector3.up * 0.1f, _hitSplashColor);
            VfxPool.Instance.SpawnSpark(splashPos + _lastDamageDirection * 0.15f, _hitSplashColor);

            if (actualDmg > 50f)
            {
                VfxPool.Instance.SpawnSpark(splashPos, _hitSplashColor);
                VfxPool.Instance.SpawnSpark(splashPos + Vector3.up * 0.2f, _hitSplashColor);
                VfxPool.Instance.SpawnSpark(splashPos + _lastDamageDirection * 0.25f, _hitSplashColor);
                VfxPool.Instance.SpawnSpark(splashPos - _lastDamageDirection * 0.15f, _hitSplashColor);
                VfxPool.Instance.SpawnSpark(splashPos + Vector3.right * 0.1f, _hitSplashColor);
                VfxPool.Instance.SpawnSpark(splashPos + Vector3.left  * 0.1f, _hitSplashColor);
                StartCoroutine(CritSlowMoCoroutine());
            }
        }

        private IEnumerator CritSlowMoCoroutine()
        {
            Time.timeScale = 0.85f;
            yield return new WaitForSecondsRealtime(0.05f);
            Time.timeScale = 1f;
        }

        private void TickApocalypseBossPhases(float ratio)
        {
            // Phase 2 (HP 75-50%): invulnerable 2s, then speed ×1.5
            if (_apocPhase < 2 && ratio <= 0.75f)
            {
                _apocPhase = 2;
                _invulUntilTime = Time.time + 2f;
                StartCoroutine(ApplySpeedAfterInvul(2f, 1.5f));
                VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 1f, 4f);
                JuiceFX.Instance?.Shake(0.3f, 400);
                EventManager.Instance?.Publish(new BossPhaseChangedEvent("L'Apocalypse — Phase 2 : Invulnérable !", 2));
            }
            // Phase 3 (HP 50-25%): summon 4 skeletons in 2m radius circle
            if (_apocPhase < 3 && ratio <= 0.50f)
            {
                _apocPhase = 3;
                _summonHordePending = true;
                _summonHordeTime = Time.time + 1f;
                VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 1f, 4.5f);
                JuiceFX.Instance?.Shake(0.35f, 500);
                EventManager.Instance?.Publish(new BossPhaseChangedEvent("L'Apocalypse — Phase 3 : Invocation !", 3));
            }
            // Phase 4 (HP < 25%): damage ×2 + AOE pulse every 3s + enrage VFX
            if (_apocPhase < 4 && ratio <= 0.25f)
            {
                _apocPhase = 4;
                _damageMul = 2f;
                _aoePulseTimer = 0f; // fire immediately on next Update tick
                VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 1f, 5f);
                JuiceFX.Instance?.Shake(0.5f, 700);
                EventManager.Instance?.Publish(new BossPhaseChangedEvent("L'Apocalypse — Phase 4 : ENRAGE FINAL !", 4));
                StartEnrageVFX();
            }
        }

        private System.Collections.IEnumerator ApplySpeedAfterInvul(float delaySec, float speedMul)
        {
            yield return new WaitForSeconds(delaySec);
            pressureSpeedMul *= speedMul;
        }

        private void TelegraphAoePulse()
        {
            if (_dying || IsDead) return;
            StartCoroutine(AoeTelegraphCoroutine());
        }

        private System.Collections.IEnumerator AoeTelegraphCoroutine()
        {
            const float TelegraphDuration = 0.8f;
            const float AoePulseRadius    = 4f;

            // spawn flat red quad at ground level
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(AoePulseRadius * 2f, AoePulseRadius * 2f, 1f);

            var mr  = quad.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = new Color(1f, 0f, 0f, 0.4f);
            mat.SetFloat("_Surface", 1f);       // transparent surface type
            mat.SetFloat("_Blend",   2f);        // alpha blend
            mat.renderQueue = 3000;
            mr.material = mat;

            float elapsed = 0f;
            while (elapsed < TelegraphDuration)
            {
                if (_dying || IsDead) { Object.Destroy(quad); yield break; }
                float t     = (elapsed % 0.2f) / 0.2f;
                float alpha = Mathf.Lerp(0.4f, 0.7f, Mathf.PingPong(t * 2f, 1f));
                mat.color = new Color(1f, 0f, 0f, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Object.Destroy(quad);
            EmitAoePulse();
        }

        private void EmitAoePulse()
        {
            if (_dying || IsDead) return;
            if (PlacementController.Instance == null || cfg == null) return;
            const float AoePulseRadius = 4f;
            const int   AoePulseDamage = 30;
            var towers = PlacementController.Instance.PlacedTowers;
            float radiusSq = AoePulseRadius * AoePulseRadius;
            for (int i = towers.Count - 1; i >= 0; i--)
            {
                var tower = towers[i];
                if (tower == null) continue;
                if ((tower.transform.position - transform.position).sqrMagnitude < radiusSq)
                    PlacementController.Instance.RemoveTower(tower);
            }
            if (Castle.Instance != null
                && (Castle.Instance.transform.position - transform.position).sqrMagnitude < radiusSq)
                Castle.Instance.TakeDamage(AoePulseDamage);
            VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 0.6f, AoePulseRadius);
        }

        private void HandleDeath()
        {
            _dying = true;
            _dyingTimer = RagdollFadeDuration + 0.1f;

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
                JuiceFX.Instance?.Shake(0.6f, 400);
                JuiceFX.Instance?.Flash(Color.white, 250);
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
                CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                    $"+{reward}¢", transform.position + Vector3.up * 1f, Color.yellow);
            }
#if UNITY_EDITOR
            else Debug.Log($"[Enemy] boss killed type={cfg?.Id} reward=0 (D1-01 boss=0x)");
#endif

            if (cfg != null) Bestiary.Instance?.RecordKill(cfg.Id);
            Achievements.Instance?.Unlock("first_blood");
            Achievements.Instance?.TrackEvent("enemy_killed", 1);
            LifetimeStats.Instance?.AddKill(1);
            WaveHistoryLog.Instance?.Log("kill", $"Mort : {cfg?.DisplayName ?? cfg?.Id ?? "?"}");

            StopEnrageVFX();
            CancelInvoke(nameof(EmitAoePulse));
            WaveManager.Instance?.NotifyEnemyDied(this);
            _lastDamageTower?.RegisterKill();

            OnDeathStatic?.Invoke(this, isBoss);

            if (this != null && gameObject != null)
            {
                if (!isBoss && _meshChild == null)
                {
                    // Capsule fallback — no GLTF bones, use procedural collapse instead of physics ragdoll
                    SpawnDeathDustPuff();
                    StartCoroutine(CollapseAndRelease());
                }
                else
                {
                    if (!isBoss) SpawnDeathDustPuff();
                    StartCoroutine(RagdollThenRelease(_lastDamageDirection));
                }
            }
            else
                ReleaseToPool();
        }

        // ── Boss death cinematic ──────────────────────────────────────────────

        private System.Collections.IEnumerator BossCinematic()
        {
            const float CinematicDuration = 1.5f;

            var pos = transform.position;

            // Triple-burst explosion — radius ×3, red-orange-gold
            VfxPool.Instance?.SpawnExplosion(pos + Vector3.up * 0.5f, 3f);
            VfxPool.Instance?.SpawnImpact(pos + Vector3.up * 0.8f, new Color(1f, 0.4f, 0f));
            VfxPool.Instance?.SpawnImpact(pos + Vector3.up * 1.2f, new Color(1f, 0.85f, 0f));

            // Rainbow confetti x2 intensity (no tint = gradient path)
            VfxPool.Instance?.SpawnConfetti(pos + Vector3.up * 1f, 2f);

            // Camera zoom on death position
            CameraController.Instance?.ZoomOnDeathPos(pos, CinematicDuration);

            // Screen shake + audio
            CameraController.Instance?.Shake(1.5f, CinematicDuration);
            AudioController.Instance?.Play("boss_death_roar", 1f);
            AudioController.Instance?.Play("boss_defeated", 1f);
            // Tower kill bonus : louder pitched-up explosion + reverb cue
            if (_lastDamageTower != null)
                AudioController.Instance?.PlayPitched("boss_kill_special", 1.4f, 1.25f);

            // "BOSS VAINCU !" popup
            CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                "BOSS VAINCU !", pos + Vector3.up * 2f, new Color(1f, 0.85f, 0f));

            // SlowMo via JuiceFX (handles ramp-down safely; unscaled wait keeps coroutine alive)
            JuiceFX.Instance?.SlowMo(0.4f, (int)(CinematicDuration * 1000f));
            yield return new WaitForSecondsRealtime(CinematicDuration);
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

        // ── Collapse ragdoll (capsule fallback — no GLTF bones) ──────────────

        private const float CollapseDuration = 0.4f;
        private const float CollapseFadeStart = 0.25f;  // alpha fade begins at 62.5% of duration
        private static readonly Color DustGrey = new Color(0.55f, 0.50f, 0.45f);

        private void SpawnDeathDustPuff()
        {
            var dustPos = transform.position + Vector3.up * 0.2f;
            VfxPool.Instance?.SpawnSpark(dustPos, DustGrey);
            VfxPool.Instance?.SpawnSpark(dustPos + Vector3.right  * 0.15f, DustGrey);
            VfxPool.Instance?.SpawnSpark(dustPos + Vector3.forward * 0.15f, DustGrey);
            VfxPool.Instance?.SpawnSpark(dustPos - Vector3.right  * 0.15f, DustGrey);
        }

        private System.Collections.IEnumerator CollapseAndRelease()
        {
            // Disable collider so dead body doesn't block gameplay
            var col = GetComponent<CapsuleCollider>();
            if (col != null) col.enabled = false;

            // Disable Animator to stop walk cycle during collapse
            if (_animator != null) _animator.enabled = false;

            var startPos   = transform.position;
            var groundPos  = new Vector3(startPos.x, startPos.y - 0.5f, startPos.z);
            var startScale = transform.localScale;
            // Random tilt axis perpendicular to up
            var fallAxis   = Vector3.Cross(Vector3.up, new Vector3(
                UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)).normalized);
            if (fallAxis == Vector3.zero) fallAxis = Vector3.right;

            float elapsed = 0f;
            while (elapsed < CollapseDuration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / CollapseDuration);
                float tE = t * t; // ease-in

                // Rotate on random axis up to ~80°
                transform.rotation = Quaternion.AngleAxis(80f * tE, fallAxis);

                // Scale Y collapse 1→0.2, XZ expand slightly for squash feel
                float scaleY = Mathf.Lerp(startScale.y, startScale.y * 0.2f, tE);
                float scaleXZ = Mathf.Lerp(startScale.x, startScale.x * 1.15f, tE);
                transform.localScale = new Vector3(scaleXZ, scaleY, scaleXZ);

                // Sink Y toward ground
                transform.position = Vector3.Lerp(startPos, groundPos, tE);

                // Alpha fade in final portion
                if (elapsed > CollapseFadeStart && _cachedRenderers != null && _mpb != null)
                {
                    float fadeT = Mathf.Clamp01((elapsed - CollapseFadeStart) / (CollapseDuration - CollapseFadeStart));
                    float alpha = 1f - fadeT;
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

        private const float AttackTelegraphDuration = 0.5f;
        private const int   TelegraphSegments       = 32;

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

        private GameObject BuildTelegraphCircle(float radius)
        {
            var go = new GameObject("AttackTelegraph");
            go.transform.position = transform.position;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop          = true;
            lr.positionCount = TelegraphSegments;
            lr.startWidth    = 0.08f;
            lr.endWidth      = 0.08f;

            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1f, 0f, 0f, 0.75f);
            lr.material = mat;

            for (int i = 0; i < TelegraphSegments; i++)
            {
                float a = i / (float)TelegraphSegments * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0.05f, Mathf.Sin(a) * radius));
            }

            return go;
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

        // 60% HP trigger — spawns 3 fast minions at random offsets with red portal rings.
        // Uses cfg.SummonType if set; otherwise spawns the same type with Fast variant applied.
        private void SpawnMinionBurst()
        {
            if (cfg == null || EnemyPool.Instance == null) return;
            if (PathManager.Instance == null || PathManager.Instance.Paths.Count == 0) return;

            var spawnType = cfg.SummonType ?? cfg;
            bool applyFast = cfg.SummonType == null;

            for (int i = 0; i < 3; i++)
            {
                float angle  = i * 120f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * 1.5f, 0f, Mathf.Sin(angle) * 1.5f);
                Vector3 spawnPos = transform.position + offset;

                VfxPool.Instance?.SpawnPortal(spawnPos);

                var minion = EnemyPool.Instance.SpawnFromType(spawnType, spawnPos, pathIdx);
                if (applyFast) minion.ApplyVariant(CrowdDefense.Data.EnemyVariant.Fast);
                WaveManager.Instance?.RegisterSpawnedEnemy(minion);
            }
#if UNITY_EDITOR
            Debug.Log($"[Enemy] Boss {cfg.Id} 60% HP burst — 3 {spawnType.Id} minions summoned");
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

        // Spawns a skeleton minion at worldPos for phase 3.
        // Uses cfg.SummonType (configured on the apocalypse boss SO as mob_skeleton).
        private void SpawnMinionAt(Vector3 worldPos)
        {
            if (EnemyPool.Instance == null) return;
            var spawnType = cfg?.SummonType;
            if (spawnType == null) return;
            var minion = EnemyPool.Instance.SpawnFromType(spawnType, worldPos, pathIdx);
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

#if UNITY_EDITOR
        // ── Editor gizmos — aggro debug visualizer ────────────────────────────

        private void OnDrawGizmosSelected()
        {
            var hero = LevelRunner.Instance?.Hero;
            bool chasingHero = _chaseHero && hero != null && hero.gameObject.activeInHierarchy;

            if (chasingHero && hero != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, hero.transform.position);
                Gizmos.DrawSphere(hero.transform.position, 0.2f);
            }
            else if (pathManager != null)
            {
                int wpCount = pathManager.WaypointCountOnPath(pathIdx);
                if (currentWaypoint < wpCount)
                {
                    Vector3 waypointPos = pathManager.GetWaypointOnPath(pathIdx, currentWaypoint) + Vector3.up * 0.5f;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(transform.position, waypointPos);
                    Gizmos.DrawSphere(waypointPos, 0.2f);
                }
            }
        }
#endif
    }
}
