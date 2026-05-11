#nullable enable
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;

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

        public void Init(EnemyType type, int assignedPathIdx = 0)
        {
            cfg = type;
            // D1-04 mob pressure : scale HP and speed by world pressure
            int currentWorld = LevelRunner.Instance?.CurrentLevel?.World ?? 1;
            var pressure = BalanceConfig.Get().GetPressure(currentWorld);
            hp = type.Hp * pressure.mobHpMul;
            pressureSpeedMul = pressure.mobSpeedMul;
            shieldHp = type.ShieldHP;
            pathIdx = assignedPathIdx;
            currentWaypoint = 1; // 0 = spawn point, start moving toward 1
            currentSpeedMul = 1f;
            transform.localScale = Vector3.one * type.Scale;

            rend = GetComponent<MeshRenderer>();
            if (rend != null)
            {
                var mat = new Material(ShaderUtil.GetLitShader());
                if (type.IsStealth)
                {
                    // Transparency activée pour cycle stealth
                    mat.SetFloat("_Surface", 1f);
                    mat.SetFloat("_Blend", 0f);
                    mat.renderQueue = 3000;
                }
                mat.color = type.BodyColor;
                rend.material = mat;
                baseColor = type.BodyColor;
            }

            var col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = 0.3f;
                col.height = 1f;
            }

            if (shieldHp > 0f)
                BuildShieldHalo();
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

        private void Start()
        {
            pathManager = PathManager.Instance;
            if (cfg != null && cfg.IsFlyer)
            {
                // Flyers spawn at fly height, ignore path validation
                if (Castle.Instance != null)
                    transform.position = new Vector3(transform.position.x, cfg.FlyHeight, transform.position.z);
                return;
            }

            if (pathManager == null || pathManager.Paths.Count == 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[Enemy] No PathManager or no paths");
#endif
                Destroy(gameObject);
                return;
            }
            if (pathManager.WaypointCountOnPath(pathIdx) < 2)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"[Enemy] Path {pathIdx} too short");
#endif
                Destroy(gameObject);
                return;
            }
            transform.position = pathManager.GetWaypointOnPath(pathIdx, 0) + Vector3.up * 0.5f;
        }

        private void Update()
        {
            if (cfg == null || IsDead) return;

            UpdateStealth();

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
            if (cfg == null || !cfg.IsStealth || rend == null) return;
            float cycleS = cfg.StealthCycleMs > 0 ? cfg.StealthCycleMs / 1000f : 2.2f;
            float alpha = cfg.StealthOpacity + (1f - cfg.StealthOpacity)
                * Mathf.Abs(Mathf.Sin(Time.time / cycleS * Mathf.PI));
            StealthAlpha = alpha;
            rend.material.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
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
            if (hp <= 0f)
            {
                IsDead = true;
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
                WaveManager.Instance?.NotifyEnemyDied(this);
                Destroy(gameObject);
            }
        }

        // Tint cyan pendant slow, retour couleur base à l'expiration — préserve alpha stealth
        public void SetSlowTint(bool slowed)
        {
            if (rend == null) return;
            float a = (cfg?.IsStealth == true) ? StealthAlpha : 1f;
            rend.material.color = slowed
                ? new Color(0.4f, 0.9f, 1.0f, a)
                : new Color(baseColor.r, baseColor.g, baseColor.b, a);
        }

        private void OnReachedCastle()
        {
            int dmg = cfg?.Damage ?? 0;
#if UNITY_EDITOR
            Debug.Log($"[Enemy] reached castle type={cfg?.Id} dmg={dmg} pathIdx={pathIdx}");
#endif
            Castle.Instance?.TakeDamage(dmg);
            WaveManager.Instance?.NotifyEnemyDied(this);
            Destroy(gameObject);
        }
    }
}
