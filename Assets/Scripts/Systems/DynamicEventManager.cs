#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Entities;
using CrowdDefense.Visual;

namespace CrowdDefense.Systems
{
    // V4 parity: 3 dynamic mid-wave events (R6-PARITY-012).
    // Triggers one random event at wave-start when waveIdx % 5 == 0.
    // Events: sand_storm (30s), lava_surge (20s), carousel_spin (one-shot).
    [DefaultExecutionOrder(-40)]
    public class DynamicEventManager : MonoSingleton<DynamicEventManager>
    {
        private enum DynEventType { SandStorm, LavaSurge, CarouselSpin }

        private DynEventType? _active;
        private float _remaining;
        private ParticleSystem? _sandStormVfx;
        private readonly List<Tower> _disabledTowers = new();
        private Coroutine? _castleDmgCoroutine;
        private float _prevRangeMul = 1f;
        private float _prevSpeedMul = 1f;

        protected override void OnAwakeSingleton() { }

        private void Start()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveStart += OnWaveStart;
        }

        protected override void OnDestroySingleton()
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveStart -= OnWaveStart;
            ForceStop();
        }

        private void OnWaveStart(int waveIdx1Based)
        {
            if (_active != null) ForceStop();
            if (waveIdx1Based % 5 != 0) return;

            var roll = (DynEventType)Random.Range(0, 3);
            StartEvent(roll);
        }

        private void Update()
        {
            if (_active == null) return;
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f) ForceStop();
        }

        // ── Start ────────────────────────────────────────────────────────────────

        private void StartEvent(DynEventType type)
        {
            _active = type;
            switch (type)
            {
                case DynEventType.SandStorm:    StartSandStorm();   break;
                case DynEventType.LavaSurge:    StartLavaSurge();   break;
                case DynEventType.CarouselSpin: StartCarouselSpin(); break;
            }
        }

        // ── SandStorm ────────────────────────────────────────────────────────────
        // 30s: tower range x0.75, enemy speed x1.15, Dust visual via WeatherController.

        private void StartSandStorm()
        {
            _remaining = 30f;

            var weather = WeatherController.Instance;
            if (weather != null)
                _sandStormVfx = weather.SpawnPreset(WeatherType.Dust);

            var towers = Object.FindObjectsByType<Tower>(FindObjectsSortMode.None);
            _prevRangeMul = 1f;
            foreach (var t in towers)
            {
                _prevRangeMul = t.EventRangeMul;
                t.EventRangeMul *= 0.75f;
            }

            var enemies = EnemyPool.Instance?.ActiveEnemies;
            if (enemies != null)
            {
                _prevSpeedMul = 1f;
                for (int i = 0; i < enemies.Count; i++)
                {
                    var e = enemies[i];
                    if (e == null || e.IsDead) continue;
                    _prevSpeedMul = e.currentSpeedMul;
                    e.currentSpeedMul *= 1.15f;
                }
            }
        }

        private void StopSandStorm()
        {
            if (_sandStormVfx != null)
            {
                _sandStormVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Destroy(_sandStormVfx.gameObject);
                _sandStormVfx = null;
            }

            var towers = Object.FindObjectsByType<Tower>(FindObjectsSortMode.None);
            foreach (var t in towers)
                t.EventRangeMul = _prevRangeMul;

            var enemies = EnemyPool.Instance?.ActiveEnemies;
            if (enemies != null)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    var e = enemies[i];
                    if (e == null || e.IsDead) continue;
                    e.currentSpeedMul = Mathf.Max(e.currentSpeedMul / 1.15f, 0f);
                }
            }
        }

        // ── LavaSurge ────────────────────────────────────────────────────────────
        // 20s: 1-3 random towers disabled + castle 5 dmg/s.

        private void StartLavaSurge()
        {
            _remaining = 20f;
            _disabledTowers.Clear();

            var all = Object.FindObjectsByType<Tower>(FindObjectsSortMode.None);
            int count = Mathf.Clamp(Random.Range(1, 4), 1, all.Length);
            for (int i = 0; i < count && i < all.Length; i++)
            {
                int j = Random.Range(i, all.Length);
                (all[i], all[j]) = (all[j], all[i]);
                all[i].IsDisabled = true;
                _disabledTowers.Add(all[i]);
                VfxPool.Instance?.SpawnImpact(all[i].transform.position, new Color(1f, 0.34f, 0.08f));
            }

            _castleDmgCoroutine = StartCoroutine(CastleDamageLoop());
        }

        private void StopLavaSurge()
        {
            foreach (var t in _disabledTowers)
                if (t != null) t.IsDisabled = false;
            _disabledTowers.Clear();

            if (_castleDmgCoroutine != null)
            {
                StopCoroutine(_castleDmgCoroutine);
                _castleDmgCoroutine = null;
            }
        }

        private IEnumerator CastleDamageLoop()
        {
            while (_active == DynEventType.LavaSurge)
            {
                yield return new WaitForSeconds(1f);
                Castle.Instance?.TakeDamage(5);
            }
        }

        // ── CarouselSpin ─────────────────────────────────────────────────────────
        // One-shot: 30% of alive enemies switch to a random alternate path.

        private void StartCarouselSpin()
        {
            _remaining = 0.1f; // one-shot — expires next Update

            var pm = PathManager.Instance;
            if (pm == null || pm.Paths.Count < 2) return;

            var enemies = EnemyPool.Instance?.ActiveEnemies;
            if (enemies == null) return;

            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if (Random.value > 0.30f) continue;

                int newPath = e.PathIdx;
                int attempts = 0;
                while (newPath == e.PathIdx && attempts < 8)
                {
                    newPath = Random.Range(0, pm.Paths.Count);
                    attempts++;
                }
                if (newPath != e.PathIdx)
                    e.ForceRecalcPath(newPath);
            }
        }

        // ── Stop ─────────────────────────────────────────────────────────────────

        private void ForceStop()
        {
            if (_active == null) return;
            switch (_active)
            {
                case DynEventType.SandStorm:    StopSandStorm();  break;
                case DynEventType.LavaSurge:    StopLavaSurge();  break;
                case DynEventType.CarouselSpin: break; // one-shot, nothing to revert
            }
            _active = null;
        }
    }
}
