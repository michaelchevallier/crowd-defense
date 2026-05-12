#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Visual;

namespace CrowdDefense.Systems
{
    [DefaultExecutionOrder(-50)]
    public class WaveManager : MonoSingleton<WaveManager>
    {
        [SerializeField] private LevelData? levelData;

        private int currentWaveIdx = 0;
        private int nextWaveToStart = 0;      // index of the wave that will begin on next StartNextWave()
        private float spawnTimerMs = 0f;
        private bool waveActive = false;
        private int spawnCounter = 0;
        private float _currentWaveScaleMul = 1f;  // endless mode: applied per-spawn (HP-based)
        private float _specialSpawnRateMul = 1f;   // endless special wave: spawn rate modifier
        private float _specialCountMul = 1f;        // endless special wave: enemy count modifier
        private Queue<EnemyType> pendingSpawns = new();
        private List<Enemy> activeEnemies = new();

        // D1-02 pacing state
        private bool waitingForPlayerStart = false;
        private float skipWindowTimer = 0f;   // seconds remaining in the 5s skip bonus window
        private int streakCount = 0;           // consecutive skip-bonus claims
        private int _lastBreakSecond = -1;     // throttle: last whole-second value fired to OnBreakStateChanged

        public IReadOnlyList<Enemy> ActiveEnemies => activeEnemies;
        public int CurrentWaveIdx => currentWaveIdx;
        public int WaveDisplayNumber => currentWaveIdx + 1;
        public int TotalWaves => levelData?.Waves.Count ?? 0;
        public bool IsWaitingForPlayerStart => waitingForPlayerStart;
        public float SkipWindowSecondsRemaining => skipWindowTimer;
        public int StreakCount => streakCount;
        // Display number of the wave that will start when the player clicks (1-based)
        public int NextWaveDisplayNumber => nextWaveToStart + 1;

        // Multiplier applied to kill rewards for the current wave (1 + streak * 0.05, cap 1.25)
        public float StreakRewardMul { get; private set; } = 1f;

        // Endless gold reward multiplier: 1.05^(waveIdx - 10) for waveIdx >= 10, else 1.
        public float EndlessGoldMul => (levelData?.IsEndless == true || LevelRunner.Instance?.IsEndlessRun == true)
            ? (currentWaveIdx >= 10 ? Mathf.Pow(1.05f, currentWaveIdx - 10) : 1f)
            : 1f;

        public event Action<int>? OnWaveStart;
        public event Action<int>? OnWaveCleared;
        public event Action? OnAllWavesCompleted;
        // Fired when the break/skip window state changes (HUD updates pill + button)
        public event Action? OnBreakStateChanged;
        // Fired each time an enemy is killed during the current wave (kill count, total spawned)
        public event Action<int, int>? OnKillCountChanged;

        private int _waveKillCount = 0;
        private int _waveTotalSpawned = 0;
        public int WaveKillCount => _waveKillCount;
        public int WaveTotalSpawned => _waveTotalSpawned;

        private void Start()
        {
            // Fallback to LevelRunner's currentLevel if this component's levelData is not assigned
            if (levelData == null && LevelRunner.Instance?.CurrentLevel != null)
                levelData = LevelRunner.Instance.CurrentLevel;

            if (levelData == null || levelData.Waves.Count == 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[WaveManager] No LevelData or no waves");
#endif
                return;
            }
            // D1-02: first wave waits for player click/N — show button immediately
            nextWaveToStart = 0;
            waitingForPlayerStart = true;
            skipWindowTimer = 0f; // no skip bonus for wave 1 (no prior wave)
            OnBreakStateChanged?.Invoke();
        }

        // Returns HP multiplier for endless wave idx.
        // Exponential first 10 waves, linear after.
        private float ComputeEndlessHpMul(int waveIdx)
        {
            if (waveIdx < 10) return Mathf.Pow(1.15f, waveIdx);
            return Mathf.Pow(1.15f, 10) * (1f + (waveIdx - 10) * 0.05f);
        }

        // Overwrites _currentWaveScaleMul based on endless formulas.
        private void ApplyEndlessScaling(int waveIdx)
        {
            _currentWaveScaleMul = ComputeEndlessHpMul(waveIdx);
        }

        // Special modifiers every 5 waves after wave 10.
        private void ApplySpecialWaveModifier(int waveIdx)
        {
            int special = ((waveIdx - 10) / 5) % 3; // cycles: 0=elite swarm, 1=boss rush, 2=chaos
            switch (special)
            {
                case 0: // elite swarm — spawn rate 40% faster, count ×1.5
                    _specialSpawnRateMul = 0.6f;
                    _specialCountMul = 1.5f;
#if UNITY_EDITOR
                    Debug.Log($"[WaveManager] Endless special wave {waveIdx + 1}: ELITE SWARM");
#endif
                    break;
                case 1: // boss rush — scale ×1.5 on top of endless mul
                    _currentWaveScaleMul *= 1.5f;
                    _specialSpawnRateMul = 1.5f;
                    _specialCountMul = 0.5f; // fewer but stronger
#if UNITY_EDITOR
                    Debug.Log($"[WaveManager] Endless special wave {waveIdx + 1}: BOSS RUSH");
#endif
                    break;
                case 2: // chaos — random rate ±30%, count ×1.25
                    _specialSpawnRateMul = UnityEngine.Random.Range(0.7f, 1.3f);
                    _specialCountMul = 1.25f;
#if UNITY_EDITOR
                    Debug.Log($"[WaveManager] Endless special wave {waveIdx + 1}: CHAOS");
#endif
                    break;
            }
        }

        private void BeginWave(int idx)
        {
            currentWaveIdx = idx;
            spawnCounter = 0;
            _waveKillCount = 0;
            _waveTotalSpawned = 0;
            _specialSpawnRateMul = 1f;
            _specialCountMul = 1f;
            var wave = levelData!.Waves[idx];
            var cfg = BalanceConfig.Get();
            float swarmMul = cfg.SwarmMul;
            // D1-04 mob pressure : mobCountMul par world
            int currentWorld = LevelRunner.Instance?.CurrentLevel?.World ?? 1;
            float countMul = cfg.GetPressure(currentWorld).mobCountMul;
            // Endless scaling: override scaleMul with spec formula, then check special waves.
            bool isEndless = levelData!.IsEndless || LevelRunner.Instance?.IsEndlessRun == true;
            if (isEndless)
            {
                ApplyEndlessScaling(idx);
                if (idx > 10 && idx % 5 == 0)
                    ApplySpecialWaveModifier(idx);
            }

            var list = new List<EnemyType>();
            foreach (var entry in wave.entries)
            {
                if (entry.type == null) continue;
                int count = Mathf.Max(1, Mathf.RoundToInt(entry.count * swarmMul * countMul * _specialCountMul));
                for (int i = 0; i < count; i++) list.Add(entry.type);
            }
            // Fisher-Yates shuffle
            var rng = new System.Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            pendingSpawns = new Queue<EnemyType>(list);
            spawnTimerMs = 0f;
            // Endless scaling already set by ApplyEndlessScaling(); only use WaveDef value for normal levels.
            if (!isEndless)
                _currentWaveScaleMul = wave.scaleMul > 0f ? wave.scaleMul : 1f;
            waveActive = true;
            // D1-01 §3.5: reset castle-damage flag at wave start so bank can accumulate if clean
            Economy.Instance?.ResetWaveDamageFlag();
            // Stage B integration : wave start feedback
            AudioController.Instance?.Play("wave_start", 0.85f);

#if UNITY_EDITOR
            Debug.Log($"[WaveManager] Wave {idx + 1}/{TotalWaves} start : {list.Count} enemies, streakRewardMul={StreakRewardMul:F2}");
#endif
            if (idx == 4) Achievements.Instance?.Unlock("wave_5_reached");
            OnWaveStart?.Invoke(idx);
            SpawnPressureMob(idx, currentWorld);
        }

        private void Update()
        {
            if (levelData == null) return;
            float dt = Time.deltaTime;
            float dtMs = dt * 1000f;

            if (waveActive)
            {
                spawnTimerMs += dtMs;
                var wave = levelData.Waves[currentWaveIdx];
                if (spawnTimerMs >= wave.spawnRateMs * _specialSpawnRateMul && pendingSpawns.Count > 0)
                {
                    spawnTimerMs = 0f;
                    SpawnEnemy(pendingSpawns.Dequeue(), wave.portalIdx);
                }
                if (pendingSpawns.Count == 0 && activeEnemies.Count == 0)
                {
                    waveActive = false;
                    HandleWaveClearedRegen();
                    // D1-01 §3.5: process interest bank before notifying listeners
                    Economy.Instance?.ProcessInterestBank();

                    // Stage B integration : wave cleared feedback
                    AudioController.Instance?.Play("wave_clear", 0.7f);
                    JuiceFX.Instance?.Flash(new Color(0.4f, 1f, 0.4f, 0.25f), 300);

                    Achievements.Instance?.TrackEvent("wave_cleared", 1);
                    OnWaveCleared?.Invoke(currentWaveIdx);
#if UNITY_EDITOR
                    Debug.Log($"[WaveManager] Wave {currentWaveIdx + 1} cleared — awaiting player start");
#endif
                    // D1-02: open skip bonus window and wait for player
                    nextWaveToStart = currentWaveIdx + 1;
                    waitingForPlayerStart = true;
                    skipWindowTimer = BalanceConfig.Get().SkipWindowSeconds;
                    _lastBreakSecond = -1;
                    OnBreakStateChanged?.Invoke();
                }
            }
            else if (waitingForPlayerStart)
            {
                // Tick the skip bonus window (no auto-start — player must click/press N)
                if (skipWindowTimer > 0f)
                {
                    skipWindowTimer = Mathf.Max(0f, skipWindowTimer - dt);
                    // Q6: streak resets only when the window expires without a claim
                    if (skipWindowTimer <= 0f && streakCount > 0)
                    {
                        streakCount = 0;
                        StreakRewardMul = 1f;
#if UNITY_EDITOR
                        Debug.Log("[WaveManager] Skip window expired — streak reset");
#endif
                        // Window just expired — force one final fire regardless of second boundary
                        _lastBreakSecond = -1;
                    }
                    // Fire only when the displayed countdown integer changes (saves ~60 repaints/s)
                    int secondNow = Mathf.FloorToInt(skipWindowTimer);
                    if (secondNow != _lastBreakSecond)
                    {
                        _lastBreakSecond = secondNow;
                        OnBreakStateChanged?.Invoke();
                    }
                }
            }
        }

        // Called by HudController on button click or N keypress (debounced externally)
        public void StartNextWave()
        {
            if (!waitingForPlayerStart) return;

            bool inWindow = skipWindowTimer > 0f;
            if (inWindow)
            {
                // Claim skip bonus
                var cfg = BalanceConfig.Get();
                streakCount = Mathf.Min(cfg.StreakCap, streakCount + 1);
                StreakRewardMul = 1f + streakCount * cfg.StreakBonusPerWave;
                Economy.Instance?.AddGold(cfg.SkipBonusGold);
#if UNITY_EDITOR
                Debug.Log($"[WaveManager] Skip bonus claimed — +{cfg.SkipBonusGold}¢ streak={streakCount} rewardMul={StreakRewardMul:F2}");
#endif
            }
            // Q6=B: streak reset only when the 5s window expires without a claim.
            // If player clicks after the window expired, streak was already reset in Update().
            // If it's wave 1 (no prior wave, window never opened), streakCount stays 0.

            skipWindowTimer = 0f;
            waitingForPlayerStart = false;
            OnBreakStateChanged?.Invoke();

            // Endless mode: extend wave list before the boundary check.
            if (LevelRunner.Instance?.IsEndlessRun == true && nextWaveToStart >= levelData!.Waves.Count)
                EndlessMode.Instance?.AppendNextWave(levelData, nextWaveToStart);

            if (nextWaveToStart < levelData!.Waves.Count)
            {
                BeginWave(nextWaveToStart);
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("[WaveManager] All waves completed — victory");
#endif
                OnAllWavesCompleted?.Invoke();
                enabled = false;
            }
        }

        private void SpawnEnemy(EnemyType type, int wavePortalIdx)
        {
            if (PathManager.Instance == null) return;
            var pm = PathManager.Instance;
            if (pm.Paths.Count == 0) return;

            if (EnemyPool.Instance == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[WaveManager] EnemyPool.Instance is null — enemy not spawned");
#endif
                return;
            }

            int resolvedPathIdx = ResolvePathIdx(wavePortalIdx);
            Vector3 spawnPos = pm.GetWaypointOnPath(resolvedPathIdx, 0) + Vector3.up * 0.5f;
            var enemy = EnemyPool.Instance.SpawnFromType(type, spawnPos, resolvedPathIdx, _currentWaveScaleMul);
            activeEnemies.Add(enemy);
            spawnCounter++;
            _waveTotalSpawned++;
            EventManager.Instance?.Publish(new EnemySpawnedEvent(enemy));
#if UNITY_EDITOR
            Debug.Log($"[WaveManager] spawned {type.Id} pathIdx={resolvedPathIdx} active={activeEnemies.Count}");
#endif
        }

        // wavePortalIdx == -1 → round-robin; >= 0 → match portal index, fallback round-robin
        private int ResolvePathIdx(int wavePortalIdx)
        {
            var pm = PathManager.Instance!;
            int pathCount = pm.Paths.Count;

            if (wavePortalIdx < 0)
                return spawnCounter % pathCount;

            var meta = pm.PathsMeta;
            for (int i = 0; i < meta.Count; i++)
                if (meta[i].PortalIdx == wavePortalIdx) return i;

            return spawnCounter % pathCount;
        }

        // D1-04: +5 HP/wave W1-5, no regen W6+ (threshold in BalanceConfig)
        private void HandleWaveClearedRegen()
        {
            int currentWorld = LevelRunner.Instance?.CurrentLevel?.World ?? 1;
            if (currentWorld < BalanceConfig.Get().NoRegenWorldThreshold)
                LevelRunner.Instance?.PrimaryCastle?.Regen(5);
        }

        // D1-04: pressure mob — linear rate 0% W1 → 60% W10, spawned 3s after wave start, speed ×1.5
        private void SpawnPressureMob(int waveIdx, int worldId)
        {
            float pressureRate = Mathf.Clamp01((worldId - 1) / 9f * 0.6f);
            if (UnityEngine.Random.value > pressureRate) return;
            StartCoroutine(SpawnPressureDelayed(3f, 1.5f, waveIdx, worldId));
        }

        private System.Collections.IEnumerator SpawnPressureDelayed(float delaySec, float speedMul, int waveIdx, int worldId)
        {
            yield return new WaitForSeconds(delaySec);
            if (!waveActive) yield break;
            if (levelData == null || currentWaveIdx < 0 || currentWaveIdx >= levelData.Waves.Count) yield break;
            var wave = levelData.Waves[currentWaveIdx];

            // Pick a random non-null enemy type from the wave
            EnemyType? pick = null;
            foreach (var entry in wave.entries)
            {
                if (entry.type != null) { pick = entry.type; break; }
            }
            if (pick == null) yield break;

            int resolvedPathIdx = ResolvePathIdx(wave.portalIdx);
            if (PathManager.Instance == null || EnemyPool.Instance == null) yield break;
            Vector3 spawnPos = PathManager.Instance.GetWaypointOnPath(resolvedPathIdx, 0) + Vector3.up * 0.5f;
            var enemy = EnemyPool.Instance.SpawnFromType(pick, spawnPos, resolvedPathIdx, _currentWaveScaleMul);
            enemy.ApplySpeedMultiplier(speedMul);
            activeEnemies.Add(enemy);
            _waveTotalSpawned++;
            EventManager.Instance?.Publish(new EnemySpawnedEvent(enemy));
#if UNITY_EDITOR
            Debug.Log($"[WaveManager] PressureMob spawned {pick.Id} speed×{speedMul} W{worldId} wave{waveIdx + 1}");
#endif
        }

        public WaveDef? GetNextWaveDef()
        {
            if (levelData == null || nextWaveToStart >= levelData.Waves.Count) return null;
            return levelData.Waves[nextWaveToStart];
        }

        public void NotifyEnemyDied(Enemy e)
        {
            activeEnemies.Remove(e);
            if (waveActive || pendingSpawns.Count > 0)
            {
                _waveKillCount++;
                OnKillCountChanged?.Invoke(_waveKillCount, _waveTotalSpawned);
            }
        }

        // Called by boss enemies when they summon a minion mid-wave.
        public void RegisterSpawnedEnemy(Enemy e) => activeEnemies.Add(e);

        // Called by Castle.TakeDamage — streak is broken if castle leaks during the break window.
        public void NotifyCastleDamaged()
        {
            if (!waitingForPlayerStart || streakCount == 0) return;
            streakCount = 0;
            StreakRewardMul = 1f;
#if UNITY_EDITOR
            Debug.Log("[WaveManager] Castle damaged — streak reset");
#endif
            OnBreakStateChanged?.Invoke();
        }
    }
}
