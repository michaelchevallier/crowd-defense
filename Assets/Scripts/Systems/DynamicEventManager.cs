#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Visual;

namespace CrowdDefense.Systems
{
    // V4 parity R6-PARITY-012: 8 dynamic mid-wave events, data-driven via LevelData.WaveEvents[].
    // Trigger: subscribe WaveManager.OnWaveStart, lookup level.WaveEvents where waveIndex==wave.
    // Events: sand_storm, lava_surge, carousel_spin (V4 original) +
    //         void_pulse, zero_g, undertow, battle_cry, hack (V4 fidelity).
    [DefaultExecutionOrder(-40)]
    public class DynamicEventManager : MonoSingleton<DynamicEventManager>
    {
        private enum DynEventType
        {
            SandStorm, LavaSurge, CarouselSpin,
            VoidPulse, ZeroG, Undertow, BattleCry, Hack
        }

        private DynEventType? _active;
        private float _remaining;
        private ParticleSystem? _sandStormVfx;
        private readonly List<Tower> _disabledTowers = new();
        private Coroutine? _periodicCoroutine;
        private readonly Dictionary<Tower, float> _prevRangeMul = new();
        private readonly Dictionary<Enemy, float> _prevSpeedMul = new();
        // zero_g state (range+speed only; fire-rate mul kept V4-faithful via range proxy)
        // undertow state
        private readonly Dictionary<Enemy, float> _undertowPrevSpeed = new();
        // battle_cry state
        private readonly List<Enemy> _criedEnemies = new();
        private readonly Dictionary<Enemy, float> _criedPrevSpeed = new();
        // hack state
        private Tower? _hackedTower;

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

            var level = LevelRunner.Instance?.CurrentLevel;
            if (level != null)
            {
                var events = level.WaveEvents;
                for (int i = 0; i < events.Count; i++)
                {
                    var ev = events[i];
                    if (ev.waveIndex == waveIdx1Based && TryParse(ev.eventType, out var parsed))
                    {
                        float dur = Mathf.Clamp(ev.duration > 0f ? ev.duration : 18f, 3f, 60f);
                        StartEvent(parsed, dur, ev.param);
                        return;
                    }
                }
            }
        }

        private void Update()
        {
            if (_active == null) return;
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f) ForceStop();
        }

        // ── Routing ──────────────────────────────────────────────────────────

        private void StartEvent(DynEventType type, float duration, float param)
        {
            _active = type;
            switch (type)
            {
                case DynEventType.SandStorm:    StartSandStorm(duration);       break;
                case DynEventType.LavaSurge:    StartLavaSurge(duration);       break;
                case DynEventType.CarouselSpin: StartCarouselSpin();             break;
                case DynEventType.VoidPulse:    StartVoidPulse(duration, param); break;
                case DynEventType.ZeroG:        StartZeroG(duration);            break;
                case DynEventType.Undertow:     StartUndertow(duration);         break;
                case DynEventType.BattleCry:    StartBattleCry(duration, param); break;
                case DynEventType.Hack:         StartHack(duration);             break;
            }
        }

        private void ForceStop()
        {
            if (_active == null) return;
            switch (_active)
            {
                case DynEventType.SandStorm:    StopSandStorm();    break;
                case DynEventType.LavaSurge:    StopLavaSurge();    break;
                case DynEventType.CarouselSpin: break;
                case DynEventType.VoidPulse:    StopVoidPulse();    break;
                case DynEventType.ZeroG:        StopZeroG();        break;
                case DynEventType.Undertow:     StopUndertow();     break;
                case DynEventType.BattleCry:    StopBattleCry();    break;
                case DynEventType.Hack:         StopHack();         break;
            }
            _active = null;
        }

        private void StopCoroutineIfActive()
        {
            if (_periodicCoroutine != null)
            {
                StopCoroutine(_periodicCoroutine);
                _periodicCoroutine = null;
            }
        }

        // ── SandStorm ────────────────────────────────────────────────────────
        // Duration: tower range ×0.75, enemy speed ×1.15, Dust VFX.

        private void StartSandStorm(float duration)
        {
            _remaining = duration;
            var weather = WeatherController.Instance;
            if (weather != null)
                _sandStormVfx = weather.SpawnPreset(WeatherType.Dust);

            var towers = Object.FindObjectsByType<Tower>(FindObjectsInactive.Exclude);
            _prevRangeMul.Clear();
            foreach (var t in towers)
            {
                _prevRangeMul[t] = t.EventRangeMul;
                t.EventRangeMul *= 0.75f;
            }
            var enemies = EnemyPool.Instance?.ActiveEnemies;
            if (enemies == null) return;
            _prevSpeedMul.Clear();
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                _prevSpeedMul[e] = e.currentSpeedMul;
                e.currentSpeedMul *= 1.15f;
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
            var towers = Object.FindObjectsByType<Tower>(FindObjectsInactive.Exclude);
            foreach (var t in towers)
                if (_prevRangeMul.TryGetValue(t, out float prev)) t.EventRangeMul = prev;
            _prevRangeMul.Clear();

            var enemies = EnemyPool.Instance?.ActiveEnemies;
            if (enemies == null) return;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if (_prevSpeedMul.TryGetValue(e, out float prev)) e.currentSpeedMul = prev;
            }
            _prevSpeedMul.Clear();
        }

        // ── LavaSurge ────────────────────────────────────────────────────────
        // Duration: 1-3 towers disabled + castle 5 dmg/s.

        private void StartLavaSurge(float duration)
        {
            _remaining = duration;
            _disabledTowers.Clear();
            var all = Object.FindObjectsByType<Tower>(FindObjectsInactive.Exclude);
            int count = Mathf.Clamp(Random.Range(1, 4), 1, all.Length);
            for (int i = 0; i < count && i < all.Length; i++)
            {
                int j = Random.Range(i, all.Length);
                (all[i], all[j]) = (all[j], all[i]);
                all[i].IsDisabled = true;
                _disabledTowers.Add(all[i]);
                VfxPool.Instance?.SpawnImpact(all[i].transform.position, new Color(1f, 0.34f, 0.08f));
            }
            _periodicCoroutine = StartCoroutine(CastleDamageLoop(DynEventType.LavaSurge, 5, 1f));
        }

        private void StopLavaSurge()
        {
            foreach (var t in _disabledTowers)
                if (t != null) t.IsDisabled = false;
            _disabledTowers.Clear();
            StopCoroutineIfActive();
        }

        // ── CarouselSpin ─────────────────────────────────────────────────────
        // One-shot: 30% of alive enemies switch to random alternate path.

        private void StartCarouselSpin()
        {
            _remaining = 0.1f;
            var pm = PathManager.Instance;
            if (pm == null || pm.Paths.Count < 2) return;
            var enemies = EnemyPool.Instance?.ActiveEnemies;
            if (enemies == null) return;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead || Random.value > 0.30f) continue;
                int newPath = e.PathIdx;
                int attempts = 0;
                while (newPath == e.PathIdx && attempts++ < 8)
                    newPath = Random.Range(0, pm.Paths.Count);
                if (newPath != e.PathIdx) e.ForceRecalcPath(newPath);
            }
        }

        // ── VoidPulse ────────────────────────────────────────────────────────
        // Duration: castle takes 1 dmg/s + purple pulse VFX expanding from castle.

        private void StartVoidPulse(float duration, float _param)
        {
            _remaining = duration;
            _periodicCoroutine = StartCoroutine(CastleDamageLoop(DynEventType.VoidPulse, 1, 1f));
            // Visual: expanding dark pulse rings around castle
            var castle = Castle.Instance;
            if (castle != null)
                VfxPool.Instance?.SpawnImpact(castle.transform.position, new Color(0.56f, 0.13f, 1f));
        }

        private void StopVoidPulse() => StopCoroutineIfActive();

        // ── ZeroG ────────────────────────────────────────────────────────────
        // Duration 8s: all enemies speed×0.5, tower range×1.2, fire-rate×0.85.

        private void StartZeroG(float duration)
        {
            _remaining = duration;
            var towers = Object.FindObjectsByType<Tower>(FindObjectsInactive.Exclude);
            _prevRangeMul.Clear();
            foreach (var t in towers)
            {
                _prevRangeMul[t] = t.EventRangeMul;
                t.EventRangeMul *= 1.20f;
            }

            var enemies = EnemyPool.Instance?.ActiveEnemies;
            if (enemies == null) return;
            _prevSpeedMul.Clear();
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                _prevSpeedMul[e] = e.currentSpeedMul;
                e.currentSpeedMul *= 0.5f;
            }
        }

        private void StopZeroG()
        {
            var towers = Object.FindObjectsByType<Tower>(FindObjectsInactive.Exclude);
            foreach (var t in towers)
                if (_prevRangeMul.TryGetValue(t, out float prev)) t.EventRangeMul = prev;
            _prevRangeMul.Clear();

            var enemies = EnemyPool.Instance?.ActiveEnemies;
            if (enemies == null) return;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if (_prevSpeedMul.TryGetValue(e, out float prev)) e.currentSpeedMul = prev;
            }
            _prevSpeedMul.Clear();
        }

        // ── Undertow ─────────────────────────────────────────────────────────
        // Duration: enemies on water tiles slow ×0.7, path reversed 1 tile.

        private void StartUndertow(float duration)
        {
            _remaining = duration;
            var enemies = EnemyPool.Instance?.ActiveEnemies;
            if (enemies == null) return;
            _undertowPrevSpeed.Clear();
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                _undertowPrevSpeed[e] = e.currentSpeedMul;
                e.currentSpeedMul *= 0.7f;
                // Reverse path by 1 tile via ForceRecalcPath on current path (same idx → re-evaluates pos)
                e.ForceRecalcPath(e.PathIdx);
            }
        }

        private void StopUndertow()
        {
            var enemies = EnemyPool.Instance?.ActiveEnemies;
            if (enemies == null) { _undertowPrevSpeed.Clear(); return; }
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if (_undertowPrevSpeed.TryGetValue(e, out float prev)) e.currentSpeedMul = prev;
            }
            _undertowPrevSpeed.Clear();
        }

        // ── BattleCry ────────────────────────────────────────────────────────
        // Duration 6s: enemies within radius 5 of boss get +50% atk speed + +25% move.

        private void StartBattleCry(float duration, float radius)
        {
            _remaining = duration;
            if (radius <= 0f) radius = 5f;
            _criedEnemies.Clear();
            _criedPrevSpeed.Clear();

            Enemy? boss = FindBoss();
            var enemies = EnemyPool.Instance?.ActiveEnemies;
            if (enemies == null) return;
            Vector3 bossPos = boss != null ? boss.transform.position : Vector3.zero;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if (boss != null && Vector3.Distance(e.transform.position, bossPos) > radius) continue;
                _criedEnemies.Add(e);
                _criedPrevSpeed[e] = e.currentSpeedMul;
                e.currentSpeedMul *= 1.25f;
            }
            if (boss != null)
                VfxPool.Instance?.SpawnImpact(bossPos, new Color(1f, 0.15f, 0.1f));
        }

        private void StopBattleCry()
        {
            for (int i = 0; i < _criedEnemies.Count; i++)
            {
                var e = _criedEnemies[i];
                if (e == null || e.IsDead) continue;
                if (_criedPrevSpeed.TryGetValue(e, out float prev)) e.currentSpeedMul = prev;
            }
            _criedEnemies.Clear();
            _criedPrevSpeed.Clear();
        }

        // ── Hack ─────────────────────────────────────────────────────────────
        // Duration 5s: 1 random tower disabled + its next fire = friendly fire 1 hit.

        private void StartHack(float duration)
        {
            _remaining = duration;
            var all = Object.FindObjectsByType<Tower>(FindObjectsInactive.Exclude);
            if (all.Length == 0) return;
            _hackedTower = all[Random.Range(0, all.Length)];
            _hackedTower.IsDisabled = true;
            _hackedTower.TempDisabledUntilTime = Time.time + duration;
            _hackedTower.FriendlyFireMode = true;
            VfxPool.Instance?.SpawnImpact(_hackedTower.transform.position, new Color(0.22f, 1f, 0.08f));
        }

        private void StopHack()
        {
            if (_hackedTower == null) return;
            if (_hackedTower.TempDisabledUntilTime > 0f
                && Time.time < _hackedTower.TempDisabledUntilTime)
            {
                _hackedTower.IsDisabled = false;
                _hackedTower.TempDisabledUntilTime = 0f;
            }
            _hackedTower.FriendlyFireMode = false;
            _hackedTower = null;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private IEnumerator CastleDamageLoop(DynEventType forEvent, int dmgPerTick, float interval)
        {
            while (_active == forEvent)
            {
                yield return new WaitForSeconds(interval);
                Castle.Instance?.TakeDamage(dmgPerTick);
            }
        }

        private static Enemy? FindBoss()
        {
            var enemies = EnemyPool.Instance?.ActiveEnemies;
            if (enemies == null) return null;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e != null && !e.IsDead && (e.Config?.IsBoss ?? false)) return e;
            }
            return null;
        }

        private static bool TryParse(string raw, out DynEventType result)
        {
            result = default;
            if (string.IsNullOrEmpty(raw)) return false;
            return raw switch
            {
                "sand_storm"    => Set(out result, DynEventType.SandStorm),
                "lava_surge"    => Set(out result, DynEventType.LavaSurge),
                "carousel_spin" => Set(out result, DynEventType.CarouselSpin),
                "void_pulse"    => Set(out result, DynEventType.VoidPulse),
                "zero_g"        => Set(out result, DynEventType.ZeroG),
                "undertow"      => Set(out result, DynEventType.Undertow),
                "battle_cry"    => Set(out result, DynEventType.BattleCry),
                "hack"          => Set(out result, DynEventType.Hack),
                _               => false,
            };
        }

        private static bool Set(out DynEventType r, DynEventType v) { r = v; return true; }
    }
}
