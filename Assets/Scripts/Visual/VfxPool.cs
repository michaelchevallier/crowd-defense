#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using CrowdDefense.Common;

namespace CrowdDefense.Visual
{
    // Port de Particles.js (Phaser pool 400 sprites radial) → Unity ObjectPool<ParticleSystem>.
    // Singleton avec 4 pools séparés : Impact / Death / Aura / CoinPickup.
    // API match api-contracts.md C3 (canon). Tint via MainModule.startColor.
    public class VfxPool : MonoSingleton<VfxPool>
    {
        private const int DefaultCapacity = 50;
        private const int MaxPoolSize = 200;

        [SerializeField] private GameObject? impactPrefab;
        [SerializeField] private GameObject? deathPrefab;
        [SerializeField] private GameObject? auraPrefab;
        [SerializeField] private GameObject? coinPickupPrefab;

        private ObjectPool<ParticleSystem>? _impactPool;
        private ObjectPool<ParticleSystem>? _deathPool;
        private ObjectPool<ParticleSystem>? _auraPool;
        private ObjectPool<ParticleSystem>? _coinPickupPool;

        private Transform? _root;

        protected override void OnAwakeSingleton()
        {
            _root = transform;
            _impactPool = BuildPool(impactPrefab, "Impact");
            _deathPool = BuildPool(deathPrefab, "Death");
            _auraPool = BuildPool(auraPrefab, "Aura");
            _coinPickupPool = BuildPool(coinPickupPrefab, "CoinPickup");
        }

        // === API canon (C3) ===

        public void SpawnImpact(Vector3 worldPos, Color tint)
        {
            if (!IsVfxEnabled() || _impactPool == null) return;
            var ps = _impactPool.Get();
            if (ps == null) return;
            ps.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            ApplyTint(ps, tint);
            PlayAndAutoRelease(ps, _impactPool);
        }

        public void SpawnDeath(Vector3 worldPos, Color tint, bool isBoss = false)
        {
            if (!IsVfxEnabled() || _deathPool == null) return;
            var ps = _deathPool.Get();
            if (ps == null) return;
            ps.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            ps.transform.localScale = isBoss ? Vector3.one * 2f : Vector3.one;
            ApplyTint(ps, tint);
            PlayAndAutoRelease(ps, _deathPool);
        }

        public ParticleSystem? SpawnAura(Transform parent, Color tint, bool isBoss = false)
        {
            if (!IsVfxEnabled() || _auraPool == null) return null;
            var ps = _auraPool.Get();
            if (ps == null) return null;
            ps.transform.SetParent(parent, false);
            ps.transform.localPosition = Vector3.zero;
            ps.transform.localRotation = Quaternion.identity;
            ps.transform.localScale = isBoss ? Vector3.one * 1.8f : Vector3.one;
            ApplyTint(ps, tint);
            ps.Play(true);
            // Aura is continuous : caller must call ReleaseAura when done
            return ps;
        }

        public void ReleaseAura(ParticleSystem? ps)
        {
            if (ps == null || _auraPool == null) return;
            // Re-parent to pool root so SetActive(false) ne crash pas si parent destroyed
            ps.transform.SetParent(_root, true);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _auraPool.Release(ps);
        }

        public void SpawnCoinPickup(Vector3 worldPos)
        {
            if (!IsVfxEnabled() || _coinPickupPool == null) return;
            var ps = _coinPickupPool.Get();
            if (ps == null) return;
            ps.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            // Coin pickup has fixed gold color baked into prefab — no tint override.
            PlayAndAutoRelease(ps, _coinPickupPool);
        }

        // === Internals ===

        private ObjectPool<ParticleSystem>? BuildPool(GameObject? prefab, string label)
        {
            if (prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[VfxPool] Prefab '{label}' not assigned — pool disabled.");
#endif
                return null;
            }

            return new ObjectPool<ParticleSystem>(
                createFunc: () => CreateInstance(prefab, label),
                actionOnGet: ps => ps.gameObject.SetActive(true),
                actionOnRelease: ps =>
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.transform.SetParent(_root, false);
                    ps.gameObject.SetActive(false);
                },
                actionOnDestroy: ps => { if (ps != null) Destroy(ps.gameObject); },
                collectionCheck: false,
                defaultCapacity: DefaultCapacity,
                maxSize: MaxPoolSize
            );
        }

        private ParticleSystem CreateInstance(GameObject prefab, string label)
        {
            var go = Instantiate(prefab, _root);
            go.name = $"{label}_VFX";
            go.SetActive(false);
            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"[VfxPool] Prefab '{label}' has no ParticleSystem component.");
#endif
                ps = go.AddComponent<ParticleSystem>();
            }
            return ps;
        }

        private static void ApplyTint(ParticleSystem ps, Color tint)
        {
            var main = ps.main;
            main.startColor = tint;
        }

        private void PlayAndAutoRelease(ParticleSystem ps, ObjectPool<ParticleSystem> pool)
        {
            ps.Play(true);
            StartCoroutine(AutoReleaseRoutine(ps, pool));
        }

        private static IEnumerator AutoReleaseRoutine(ParticleSystem ps, ObjectPool<ParticleSystem> pool)
        {
            var main = ps.main;
            float waitTime = main.startLifetime.constantMax + main.duration + 0.05f;
            yield return new WaitForSeconds(waitTime);
            if (ps != null && ps.gameObject.activeSelf) pool.Release(ps);
        }

        // SettingsRegistry n'existe pas encore (Axis F UX livre plus tard).
        // Defensive null-safe : default true si pas livré.
        private static bool IsVfxEnabled()
        {
            // TODO: replace with SettingsRegistry.Instance?.VFXEnabled ?? true
            // when CrowdDefense.UI.SettingsRegistry is live (Axis F UX).
            return true;
        }
    }
}
