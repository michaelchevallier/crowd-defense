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
    public partial class Enemy : MonoBehaviour
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
        private bool  _fireBreathTelegraphActive = false;

        // ── Charge wind-up telegraph ───────────────────────────────────────────
        private bool  _chargeWindUpActive = false;
        private float _chargeWindUpTimer  = 0f;

        // ── Apocalypse P2 periodic imp summons (3 imps / 2s during 6s window) ──
        private float _apocImpSummonTimer = 0f;
        private float _apocImpSummonEndTime = 0f;

        // ── Boss special behavior timers (EnemyBossBehaviorsStatic.cs) ────────
        internal float _teleportTimer     = 0f;
        internal float _burstSummonTimer  = 0f;
        internal float _tentacleSlamTimer = 0f;

        // ── Static mode (decoration preview) ─────────────────────────────────
        private bool  _static     = false;
        private float _staticRotY = 0f;

        // ── Target reticle marker ─────────────────────────────────────────────
        private int _targetedByCount = 0;
        private GameObject? _reticleGO;

        // ── Hover target highlight (yellow ring shown while tower is hovered) ──
        private GameObject? _targetHighlightGO;

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

        // DynamicEventManager hook (R6-PARITY-012) — reassign to alternate path mid-wave.
        public void ForceRecalcPath(int newPathIdx)
        {
            var pm = Systems.PathManager.Instance;
            if (pm == null || newPathIdx < 0 || newPathIdx >= pm.Paths.Count) return;
            pathIdx = newPathIdx;
            currentWaypoint = 1;
        }

        // Used by EnemyPathingSystem — exposes speed without touching Transform

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


        // Called by EnemyPool after Init when the 10% elite roll succeeds (W5+, non-boss).


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


        // Knockback along path — rewind current waypoint progress to push enemy back.

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
        // Moved to Enemy.Init.cs partial for cap compliance




        // ── Visual helpers ────────────────────────────────────────────────────







        // Port of _triggerHitFlash / _tickHitFeedback from Enemy.js



        // ── Update lifecycle ──────────────────────────────────────────────────
        // Moved to Enemy.Update.cs partial for cap compliance




        // ── TakeDamage ────────────────────────────────────────────────────────




        // ── Boss death cinematic ──────────────────────────────────────────────






        // 60% HP trigger — spawns 3 fast minions at random offsets with red portal rings.
        // Uses cfg.SummonType if set; otherwise spawns the same type with Fast variant applied.


        // Spawns a minion at worldPos. Used by phase 3 and EnemyBossBehaviors burst patterns.


        // ── Pool release ──────────────────────────────────────────────────────


        // ── OnDestroy cleanup ─────────────────────────────────────────────────
        // Moved to Enemy.Lifecycle.cs partial for cap compliance
    }
}
