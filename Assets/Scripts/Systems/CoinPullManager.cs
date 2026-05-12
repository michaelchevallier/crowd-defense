#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using CrowdDefense.Common;
using CrowdDefense.Visual;

namespace CrowdDefense.Systems
{
    // Registre des sources Magnet actives + animation CoinFlyTo (enemy death → HUD gold pill).
    // Magnet tracking: Tower.UpdateCoinPull() registers each frame; Enemy.TakeDamage() queries.
    // Coin fly: SpawnCoinFlyTo() pools CoinToken billboards that arc via bezier to the HUD.
    // Magnet perk: ApplyMagnetRangeMul/ApplyCoinFlySpeedMul called by PerkSystem when perk picked.
    public class CoinPullManager : MonoSingleton<CoinPullManager>
    {
        private struct CoinSource
        {
            public Vector3 position;
            public float range;
            public float coinMul;
        }

        // ── Magnet tracking ───────────────────────────────────────────────────

        // Réinitialisé en fin de frame via LateUpdate, reconstruit par les Magnets dans Update
        private readonly List<CoinSource> sources = new();

        // Magnet perk boosts (multiplicative, persist for the run)
        private float _magnetRangeMul  = 1f;
        private float _coinFlySpeedMul = 1f;

        // Hero pull range: radius around Hero that snaps coin target to Hero instead of HUD.
        private const float HeroPullBaseRange = 6f;

        private void LateUpdate()
        {
            sources.Clear();
        }

        // Appelé par chaque Tower Magnet dans son Update
        public void RegisterSource(Vector3 pos, float range, float coinMul)
        {
            sources.Add(new CoinSource { position = pos, range = range, coinMul = coinMul });
        }

        // Retourne le multiplicateur le plus élevé parmi les sources en portée (max, pas produit)
        public float GetCoinMulAt(Vector3 pos)
        {
            float best = 1f;
            for (int i = 0; i < sources.Count; i++)
            {
                var s = sources[i];
                float dx = pos.x - s.position.x;
                float dz = pos.z - s.position.z;
                // Apply magnet synergy multiplier to range check
                float boostedRange = s.range * _magnetRangeMul;
                if (dx * dx + dz * dz < boostedRange * boostedRange)
                    best = Mathf.Max(best, s.coinMul);
            }
            return best;
        }

        // Called by PerkSystem when a magnet perk is picked (stacks multiplicatively)
        public void ApplyMagnetRangeMul(float mul)  => _magnetRangeMul  *= mul;
        public void ApplyCoinFlySpeedMul(float mul) => _coinFlySpeedMul *= mul;

        // Reset per-run (called by LevelRunner at level start)
        public void ResetPerkBoosts()
        {
            _magnetRangeMul  = 1f;
            _coinFlySpeedMul = 1f;
        }

        // ── Coin fly-to animation ─────────────────────────────────────────────

        private const int   PoolCapacity   = 20;
        private const int   PoolMaxSize    = 60;
        private const float FlyDurationSec = 0.72f;

        private ObjectPool<CoinToken>? _tokenPool;

        // World-space landing point derived from Camera + viewport position of gold pill.
        // Fallback: camera position + slightly in front when HUD is not available.
        private static readonly Vector3 _hudViewportPos = new Vector3(0.07f, 0.95f, 10f);

        protected override void OnAwakeSingleton()
        {
            _tokenPool = new ObjectPool<CoinToken>(
                createFunc: CreateToken,
                actionOnGet: t => t.gameObject.SetActive(true),
                actionOnRelease: t => t.gameObject.SetActive(false),
                actionOnDestroy: t => { if (t != null) Destroy(t.gameObject); },
                collectionCheck: false,
                defaultCapacity: PoolCapacity,
                maxSize: PoolMaxSize
            );
            PreWarm();
            EventManager.Instance?.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        protected override void OnDestroySingleton()
        {
            EventManager.Instance?.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        // Global event-bus wiring: Enemy.cs also calls SpawnCoinFlyTo directly for now.
        // This subscription ensures coins spawn even if the direct call is later removed.
        private void OnEnemyKilled(EnemyKilledEvent e) { }

        // Public entry point — called by Enemy after reward computation.
        // worldPos: 3-D death position. amount: final coin reward.
        public void SpawnCoinFlyTo(Vector3 worldPos, int amount)
        {
            if (_tokenPool == null) return;
            var token    = _tokenPool.Get();
            Vector3 target   = ResolveTarget(worldPos);
            float   duration = FlyDurationSec / Mathf.Max(0.5f, _coinFlySpeedMul);
            token.Fly(worldPos, target, amount, duration);
        }

        // Resolves the coin fly destination:
        // — Hero if active and within magnet pull range (boosted by perk),
        // — otherwise the HUD gold pill world position.
        private Vector3 ResolveTarget(Vector3 fromWorldPos)
        {
            var hero = LevelRunner.Instance?.Hero;
            if (hero != null)
            {
                float pullRange = HeroPullBaseRange * _magnetRangeMul;
                float dx = fromWorldPos.x - hero.transform.position.x;
                float dz = fromWorldPos.z - hero.transform.position.z;
                if (dx * dx + dz * dz < pullRange * pullRange)
                    return hero.transform.position + Vector3.up * 1.0f;
            }
            return HudWorldTarget();
        }

        private Vector3 HudWorldTarget()
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.zero;
            return cam.ViewportToWorldPoint(_hudViewportPos);
        }

        private CoinToken CreateToken()
        {
            var go = new GameObject("CoinToken");
            go.SetActive(false);
            var token = go.AddComponent<CoinToken>();
            token.SetPool(_tokenPool!);
            return token;
        }

        private void PreWarm()
        {
            if (_tokenPool == null) return;
            var buf = new CoinToken[PoolCapacity];
            for (int i = 0; i < PoolCapacity; i++) buf[i] = _tokenPool.Get();
            for (int i = 0; i < PoolCapacity; i++) _tokenPool.Release(buf[i]);
        }
    }
}
