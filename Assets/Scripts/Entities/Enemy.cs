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
        private EnemyType? cfg;
        private float hp;
        private float shieldHp;
        private int currentWaypoint;
        private int pathIdx;
        private PathManager? pathManager;
        private MeshRenderer? rend;
        private Color baseColor;
        private GameObject? shieldHalo;
        private EnemyPool? pool;
        private float summonTimer = 0f;
        private float blastTimer = 0f;

        // Child GO holding the spawned GLTF mesh (null = using capsule primitive)
        private GameObject? _meshChild;

        // Cached once in Init — avoids GetComponentsInChildren alloc every frame
        private Renderer[]? _cachedRenderers;
        // Reused MPB — zero alloc per-frame tint via SetPropertyBlock
        private MaterialPropertyBlock? _mpb;
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId = Shader.PropertyToID("_Color");

        // Animator configuré par AnimationController.SetupAnimator au Init.
        private Animator? _animator;
        // Flag dirty pour éviter SetBool chaque frame si state inchangé
        private bool _wasWalking = false;

        public EnemyType? Config => cfg;
        public int CurrentWaypoint => currentWaypoint;
        public int PathIdx => pathIdx;
        public bool IsDead { get; private set; }
        public bool IsFlyer => cfg?.IsFlyer ?? false;
        public bool ImmuneToFlyerBonus => cfg?.ImmuneToFlyerBonus ?? false;

        // Expose alpha pour que Tower puisse tester la phase stealth
        public float StealthAlpha { get; private set; } = 1f;

        // Modifié par SlowEffectManager chaque frame
        public float currentSpeedMul = 1f;

        // World pressure scaling (D1-04) — set once in Init, persists for lifetime of enemy
        private float pressureSpeedMul = 1f;

        // Called once by EnemyPool after Instantiate to back-link the pool
        public void SetPool(EnemyPool p) => pool = p;

        public void Init(EnemyType type, int assignedPathIdx = 0)
        {
            cfg = type;
            IsDead = false;
            currentSpeedMul = 1f;
            StealthAlpha = 1f;
            summonTimer = 0f;
            blastTimer = 0f;

            // D1-04 mob pressure : scale HP and speed by world pressure
            int currentWorld = LevelRunner.Instance?.CurrentLevel?.World ?? 1;
            var pressure = BalanceConfig.Get().GetPressure(currentWorld);
            hp = type.Hp * pressure.mobHpMul;
            pressureSpeedMul = pressure.mobSpeedMul;
            shieldHp = type.ShieldHP;
            pathIdx = assignedPathIdx;
            currentWaypoint = 1; // 0 = spawn point, start moving toward 1
            transform.localScale = Vector3.one * type.Scale;

            rend = GetComponent<MeshRenderer>();
            baseColor = type.BodyColor;

            _meshChild = SpawnMeshChild(type.AssetKey);

            // Cache renderers once — hot path (UpdateStealth runs every frame per enemy)
            var meshRoot = _meshChild != null ? _meshChild : gameObject;
            _cachedRenderers = meshRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
            _mpb ??= new MaterialPropertyBlock();

            // Cel-shading toon material — port de applyToonToScene() ToonMaterial.js
            // Apply on GLTF subtree if present, otherwise on root (capsule primitive)
            var toonRoot = meshRoot;
            MaterialController.ApplyToon(toonRoot, type.BodyColor, type.IsStealth);
            // If GLTF spawned, disable the root capsule MeshRenderer (keep collider)
            if (_meshChild != null && rend != null)
                rend.enabled = false;

            // Outline silhouette — applied after toon so outline mat is not overwritten
            Outline.ApplyToHierarchy(toonRoot.transform);

            // Animations Mechanim : Idle + Walk via bool isWalking.
            // WalkAnim = nom de clip hint (ex: "Walking_A") — résolution dans le .controller.
            _animator = AnimationController.SetupAnimator(toonRoot, "Idle", type.WalkAnim);
            _wasWalking = false;

            var col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = 0.3f;
                col.height = 1f;
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

            // Position + path setup (was in Start() — must run every Init for pooled reuse)
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

        /// <summary>
        /// Instancie le prefab GLTF depuis AssetRegistry si disponible.
        /// Retourne le GO enfant spawné, ou null si fallback capsule primitive.
        /// </summary>
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
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private void BuildShieldHalo()
        {
            shieldHalo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldHalo.name = "ShieldHalo";
            shieldHalo.transform.SetParent(transform, false);
            shieldHalo.transform.localScale = Vector3.one * 1.2f;
            // Destroy collider — halo is visual only
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

        // Start() is called once on first Instantiate — pool reuse skips Start.
        // Position/path setup is handled in Init() for correct pooled behavior.
        private void Start() { }

        private void Update()
        {
            if (cfg == null || IsDead) return;

            UpdateStealth();
            UpdateSummons();
            UpdateAoeBlast();

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

            Vector3 target = pathManager.GetWaypointOnPath(pathIdx, currentWaypoint) + Vector3.up * 0.5f;
            float effectiveSpeed = cfg.Speed * currentSpeedMul * pressureSpeedMul;
            transform.position = Vector3.MoveTowards(transform.position, target, effectiveSpeed * Time.deltaTime);

            if ((transform.position - target).sqrMagnitude < 0.01f)
                currentWaypoint++;

            // SetWalking avec dirty flag pour éviter SetBool inutile chaque frame
            bool nowWalking = effectiveSpeed > 0.01f;
            if (nowWalking != _wasWalking)
            {
                AnimationController.SetWalking(_animator, nowWalking);
                _wasWalking = nowWalking;
            }
        }

        private void UpdateFlyer()
        {
            if (cfg == null) return;
            if (Castle.Instance == null) return;

            Vector3 castlePos = Castle.Instance.transform.position;
            Vector3 flyTarget = new Vector3(castlePos.x, cfg.FlyHeight, castlePos.z);
            float effectiveSpeed = cfg.Speed * currentSpeedMul * pressureSpeedMul;
            transform.position = Vector3.MoveTowards(transform.position, flyTarget, effectiveSpeed * Time.deltaTime);
            // Lock Y at fly height during movement
            var pos = transform.position;
            pos.y = cfg.FlyHeight;
            transform.position = pos;

            if ((transform.position - flyTarget).sqrMagnitude < 0.25f)
                OnReachedCastle();
        }

        private void UpdateStealth()
        {
            if (cfg == null || !cfg.IsStealth) return;
            float cycleS = cfg.StealthCycleMs > 0 ? cfg.StealthCycleMs / 1000f : 2.2f;
            float alpha = cfg.StealthOpacity + (1f - cfg.StealthOpacity)
                * Mathf.Abs(Mathf.Sin(Time.time / cycleS * Mathf.PI));
            StealthAlpha = alpha;
            ApplyTint(new Color(baseColor.r, baseColor.g, baseColor.b, alpha));
        }

        private void ApplyTint(Color tint)
        {
            if (_cachedRenderers == null || _mpb == null) return;
            _mpb.SetColor(_baseColorId, tint);
            _mpb.SetColor(_colorId, tint);
            for (int i = 0; i < _cachedRenderers.Length; i++)
                _cachedRenderers[i].SetPropertyBlock(_mpb);
        }

        private void UpdateSummons()
        {
            if (cfg == null || !cfg.SummonsMinions || cfg.SummonType == null) return;
            summonTimer += Time.deltaTime * 1000f;
            if (summonTimer >= cfg.SummonCooldownMs)
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
#if UNITY_EDITOR
            Debug.Log($"[Enemy] boss {cfg.Id} AoE blast radius={cfg.AoeBlastRadius} hit {hit} towers");
#endif
        }

        public void TakeDamage(float dmg)
        {
            if (IsDead) return;
            if (shieldHp > 0f)
            {
                shieldHp -= dmg;
                if (shieldHp <= 0f)
                {
                    shieldHp = 0f;
                    if (shieldHalo != null)
                        shieldHalo.SetActive(false);
                }
                return;
            }
            hp -= dmg;

            // Stage B integration : non-fatal hit feedback (audio + VFX impact)
            if (hp > 0f)
            {
                AudioController.Instance?.Play("enemy_hit", 0.4f);
                VfxPool.Instance?.SpawnImpact(transform.position + Vector3.up * 0.3f, baseColor);
            }

            if (hp <= 0f)
            {
                IsDead = true;

                // Stage B integration : death feedback (audio tier + VFX + juice for boss)
                bool isBoss = cfg != null && (cfg.IsBoss || cfg.IsApocalypseBoss);
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
                if (_animator != null) _animator.SetTrigger("dieTrigger");

                // Boss reward = 0× (D1-01 §3.3, KR pattern P-U-3).
                if (!(isBoss || isMedium))
                {
                    int baseReward = cfg?.Reward ?? 0;
                    float coinMul = CoinPullManager.Instance != null
                        ? CoinPullManager.Instance.GetCoinMulAt(transform.position)
                        : 1f;
                    float streakMul = WaveManager.Instance?.StreakRewardMul ?? 1f;
                    int reward = Mathf.Max(1, Mathf.RoundToInt(baseReward * coinMul * streakMul));
#if UNITY_EDITOR
                    Debug.Log($"[Enemy] killed type={cfg?.Id} baseReward={baseReward} coinMul={coinMul:F2} streakMul={streakMul:F2} reward={reward}");
#endif
                    Economy.Instance?.AddGold(reward);
                }
#if UNITY_EDITOR
                else Debug.Log($"[Enemy] boss killed type={cfg?.Id} reward=0 (D1-01 boss=0x)");
#endif

                WaveManager.Instance?.NotifyEnemyDied(this);
                ReleaseToPool();
            }
        }

        // Tint cyan pendant slow, retour couleur base à l'expiration — préserve alpha stealth
        public void SetSlowTint(bool slowed)
        {
            float a = (cfg?.IsStealth == true) ? StealthAlpha : 1f;
            Color tint = slowed
                ? new Color(0.4f, 0.9f, 1.0f, a)
                : new Color(baseColor.r, baseColor.g, baseColor.b, a);
            ApplyTint(tint);
        }

        private void OnReachedCastle()
        {
            int dmg = cfg?.Damage ?? 0;
#if UNITY_EDITOR
            Debug.Log($"[Enemy] reached castle type={cfg?.Id} dmg={dmg} pathIdx={pathIdx}");
#endif
            Castle.Instance?.TakeDamage(dmg);
            WaveManager.Instance?.NotifyEnemyDied(this);
            ReleaseToPool();
        }

        private void ReleaseToPool()
        {
            if (pool != null)
                pool.Release(this);
            else
                Destroy(gameObject); // fallback si pas de pool (tests unitaires, cas edge)
        }
    }
}
