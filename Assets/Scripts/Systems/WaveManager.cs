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

        private int _currentWaveIdx = 0;
        private int _nextWaveToStart = 0;     // index of the wave that will begin on next StartNextWave()
        private float _spawnTimerMs = 0f;
        private bool _waveActive = false;
        private int _spawnCounter = 0;
        private float _currentWaveScaleMul = 1f;  // endless mode: applied per-spawn (HP-based)
        private float _specialSpawnRateMul = 1f;   // endless special wave: spawn rate modifier
        private float _specialCountMul = 1f;        // endless special wave: enemy count modifier
        private float _varSpawnRateMul = 1f;        // BalanceConfig variance: per-wave spawn jitter
        private Queue<(EnemyType type, EnemyVariant variant)> _pendingSpawns = new();
        private List<Enemy> _activeEnemies = new();

        // D1-02 pacing state
        private bool _waitingForPlayerStart = false;
        private float _skipWindowTimer = 0f;  // seconds remaining in the 5s skip bonus window
        private int _streakCount = 0;          // consecutive skip-bonus claims
        private int _lastBreakSecond = -1;     // throttle: last whole-second value fired to OnBreakStateChanged

        public IReadOnlyList<Enemy> ActiveEnemies => _activeEnemies;
        public int CurrentWaveIdx => _currentWaveIdx;
        public int WaveDisplayNumber => _currentWaveIdx + 1;
        public int TotalWaves => levelData?.Waves.Count ?? 0;
        public bool IsWaitingForPlayerStart => _waitingForPlayerStart;
        public bool IsWaveActive => _waveActive;
        public int PendingSpawnCount => _pendingSpawns.Count;
        public float SpawnTimerMs => _spawnTimerMs;
        public float SpawnIntervalMs => levelData != null && _waveActive
            ? levelData.Waves[_currentWaveIdx].spawnRateMs * _specialSpawnRateMul * _varSpawnRateMul
                * GetSpawnIntervalMul(levelData.Waves[_currentWaveIdx].pattern, _spawnCounter)
            : 0f;
        public float SkipWindowSecondsRemaining => _skipWindowTimer;
        public int StreakCount => _streakCount;
        // Display number of the wave that will start when the player clicks (1-based)
        public int NextWaveDisplayNumber => _nextWaveToStart + 1;

        // Multiplier applied to kill rewards for the current wave (1 + streak * 0.05, cap 1.25)
        public float StreakRewardMul { get; private set; } = 1f;

        // Endless gold reward multiplier: 1.05^(waveIdx - 10) for waveIdx >= 10, else 1.
        public float EndlessGoldMul => (levelData?.IsEndless == true || LevelRunner.Instance?.IsEndlessRun == true)
            ? (_currentWaveIdx >= 10 ? Mathf.Pow(1.05f, _currentWaveIdx - 10) : 1f)
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

        // Stats snapshotted at the moment OnWaveCleared fires (read by HUD summary popup)
        private int   _goldAtWaveStart        = 0;
        private float _waveStartTimeUnscaled   = 0f;
        public  int   LastWaveGoldEarned       { get; private set; }
        public  int   LastWaveKillCount        { get; private set; }
        public  float LastWaveElapsedSeconds   { get; private set; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _debugPause = false;
#endif

        private void Start()
        {
            // Fallback to LevelRunner's currentLevel if this component's levelData is not assigned
            if (levelData == null && LevelRunner.Instance?.CurrentLevel != null)
                levelData = LevelRunner.Instance.CurrentLevel;

            // Final fallback: load first level WITH waves from registry if still null OR empty.
            // Historically W1-1 had 0 waves due to a YAML blank-line stripping the waves list
            // (fixed in N3); the fallback stays as a safety net for any future tutorial level.
            if (levelData == null || levelData.Waves.Count == 0)
            {
                var reg = LevelRegistry.Get();
                if (reg != null)
                {
                    for (int i = 0; i < reg.Levels.Count; i++)
                    {
                        var lvl = reg.Levels[i];
                        if (lvl != null && lvl.Waves.Count > 0)
                        {
                            levelData = lvl;
                            break;
                        }
                    }
                }
            }

            if (levelData == null || levelData.Waves.Count == 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // Silent in non-play scenes (WorldMap/Menu) — Castle absence is the marker.
                if (Castle.Instance != null)
                    Debug.LogWarning("[WaveManager] No LevelData or no waves — wave events disabled this scene.");
#endif
                return;
            }
            // D1-02: first wave waits for player click/N — show button immediately
            _nextWaveToStart = 0;
            _waitingForPlayerStart = true;
            _skipWindowTimer = 0f; // no skip bonus for wave 1 (no prior wave)
            OnBreakStateChanged?.Invoke();
        }

        // Returns the combined scale multiplier (HP-dominant) for endless wave idx.
        // W0-29  : exponential 1.15^wave (early climb)
        // W30-49 : compounded from W29 base × 1.10 HP / 1.08 dmg per wave (avg 1.09)
        // W50+   : compounded from W49 base × 1.15 HP / 1.12 dmg per wave (avg 1.135)
        // Note: _currentWaveScaleMul feeds a single HP+dmg multiplier at spawn time;
        //       HP/dmg distinction is preserved in EndlessMode constants for future split.
        private float ComputeEndlessHpMul(int waveIdx)
        {
            const int thresholdMid  = EndlessMode.WaveThresholdMid;
            const int thresholdHard = EndlessMode.WaveThresholdHard;

            if (waveIdx < thresholdMid)
                return Mathf.Pow(1.15f, waveIdx);

            float baseMid = Mathf.Pow(1.15f, thresholdMid);
            if (waveIdx < thresholdHard)
            {
                // avg of HP×1.10 and dmg×1.08 = 1.09 per wave
                return baseMid * Mathf.Pow(1.09f, waveIdx - thresholdMid);
            }

            float baseHard = baseMid * Mathf.Pow(1.09f, thresholdHard - thresholdMid);
            // avg of HP×1.15 and dmg×1.12 = 1.135 per wave
            return baseHard * Mathf.Pow(1.135f, waveIdx - thresholdHard);
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
            _currentWaveIdx = idx;
            _spawnCounter = 0;
            _waveKillCount = 0;
            _waveTotalSpawned = 0;
            _goldAtWaveStart      = Economy.Instance?.Gold ?? 0;
            _waveStartTimeUnscaled = Time.unscaledTime;
            _specialSpawnRateMul = 1f;
            _specialCountMul = 1f;
            _varSpawnRateMul = 1f;
            var wave = levelData!.Waves[idx];
            var cfg = BalanceConfig.Get();
            float swarmMul = cfg.SwarmMul;
            // D1-04 mob pressure : mobCountMul par world
            int currentWorld = LevelRunner.Instance?.CurrentLevel?.World ?? 1;
            float countMul = cfg.GetPressure(currentWorld).mobCountMul;
            // Wave variance: seeded by levelId ^ waveIdx for deterministic replayability
            var seed = (levelData!.Id?.GetHashCode() ?? 0) ^ idx;
            var varianceRng = new System.Random(seed);
            float varCountMul = 1f + (float)(varianceRng.NextDouble() * 2 - 1) * cfg.WaveCountVariance;
            float varSpawnMul = 1f + (float)(varianceRng.NextDouble() * 2 - 1) * cfg.WaveSpawnVariance;
            countMul *= varCountMul;
            _varSpawnRateMul = varSpawnMul;
            // Endless scaling: override scaleMul with spec formula, then check special waves.
            bool isEndless = levelData!.IsEndless || LevelRunner.Instance?.IsEndlessRun == true;
            if (isEndless)
            {
                ApplyEndlessScaling(idx);
                if (idx > 10 && idx % 5 == 0)
                    ApplySpecialWaveModifier(idx);
            }

            var list = new List<(EnemyType type, EnemyVariant variant)>();
            foreach (var entry in wave.entries)
            {
                if (entry.type == null) continue;
                int count = Mathf.Max(1, Mathf.RoundToInt(entry.count * swarmMul * countMul * _specialCountMul));
                for (int i = 0; i < count; i++) list.Add((entry.type, entry.variant));
            }
            // Prewarm per-type sub-pools before wave starts — avoids mid-wave Instantiate spikes.
            if (EnemyPool.Instance != null)
            {
                foreach (var entry in wave.entries)
                {
                    if (entry.type == null) continue;
                    int count = Mathf.Max(1, Mathf.RoundToInt(entry.count * swarmMul * countMul * _specialCountMul));
                    EnemyPool.Instance.PrewarmType(entry.type, count);
                }
            }
            // Fisher-Yates shuffle
            var rng = new System.Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            _pendingSpawns = new Queue<(EnemyType type, EnemyVariant variant)>(list);
            _spawnTimerMs = 0f;
            // Endless scaling already set by ApplyEndlessScaling(); only use WaveDef value for normal levels.
            if (!isEndless)
                _currentWaveScaleMul = wave.scaleMul > 0f ? wave.scaleMul : 1f;
            _waveActive = true;
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
            TriggerBossWarningIfNeeded(wave);
        }

        private float GetSpawnIntervalMul(SpawnPattern p, int counter)
        {
            switch (p)
            {
                case SpawnPattern.Sparse: return 2f;
                case SpawnPattern.Cluster: return (counter % 5 == 0) ? 4f : 0.1f;
                case SpawnPattern.VFormation: return counter < 5 ? 0.05f : 4f;
                default: return 1f;
            }
        }

        private void Update()
        {
            if (levelData == null) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.F5))
                _debugPause = !_debugPause;
            if (_debugPause) return;
#endif

            float dt = Time.deltaTime;
            float dtMs = dt * 1000f;

            if (_waveActive)
            {
                _spawnTimerMs += dtMs;
                var wave = levelData.Waves[_currentWaveIdx];
                float patternMul = GetSpawnIntervalMul(wave.pattern, _spawnCounter);
                float actualInterval = wave.spawnRateMs * _specialSpawnRateMul * _varSpawnRateMul * patternMul;
                if (_spawnTimerMs >= actualInterval && _pendingSpawns.Count > 0)
                {
                    _spawnTimerMs = 0f;
                    var (spawnType, spawnVariant) = _pendingSpawns.Dequeue();
                    SpawnEnemy(spawnType, wave.portalIdx, spawnVariant);
                }
                if (_pendingSpawns.Count == 0 && _activeEnemies.Count == 0)
                {
                    _waveActive = false;
                    HandleWaveClearedRegen();
                    // D1-01 §3.5: process interest bank before notifying listeners
                    Economy.Instance?.ProcessInterestBank();

                    // Stage B integration : wave cleared feedback
                    AudioController.Instance?.Play("wave_clear", 0.7f);
                    JuiceFX.Instance?.Flash(new Color(0.4f, 1f, 0.4f, 0.25f), 300);

                    Achievements.Instance?.TrackEvent("wave_cleared", 1);
                    // Snapshot per-wave stats before listeners read them
                    LastWaveKillCount      = _waveKillCount;
                    LastWaveElapsedSeconds = Time.unscaledTime - _waveStartTimeUnscaled;
                    LastWaveGoldEarned     = Mathf.Max(0, (Economy.Instance?.Gold ?? 0) - _goldAtWaveStart);

                    if (LastWaveGoldEarned > 0)
                    {
                        var castle = LevelRunner.Instance?.PrimaryCastle;
                        Vector3 castlePos = castle != null ? castle.transform.position : Vector3.zero;
                        CrowdDefense.UI.FloatingPopupController.Instance?.SpawnGoldReward(
                            LastWaveGoldEarned, castlePos);
                    }

                    OnWaveCleared?.Invoke(_currentWaveIdx);
#if UNITY_EDITOR
                    Debug.Log($"[WaveManager] Wave {_currentWaveIdx + 1} cleared — awaiting player start");
#endif
                    // D1-02: open skip bonus window and wait for player
                    _nextWaveToStart = _currentWaveIdx + 1;
                    _waitingForPlayerStart = true;
                    _skipWindowTimer = BalanceConfig.Get().SkipWindowSeconds;
                    _lastBreakSecond = -1;
                    OnBreakStateChanged?.Invoke();
                }
            }
            else if (_waitingForPlayerStart)
            {
                // Tick the skip bonus window (no auto-start — player must click/press N)
                if (_skipWindowTimer > 0f)
                {
                    _skipWindowTimer = Mathf.Max(0f, _skipWindowTimer - dt);
                    // Q6: streak resets only when the window expires without a claim
                    if (_skipWindowTimer <= 0f && _streakCount > 0)
                    {
                        _streakCount = 0;
                        StreakRewardMul = 1f;
#if UNITY_EDITOR
                        Debug.Log("[WaveManager] Skip window expired — streak reset");
#endif
                        // Window just expired — force one final fire regardless of second boundary
                        _lastBreakSecond = -1;
                    }
                    // Fire only when the displayed countdown integer changes (saves ~60 repaints/s)
                    int secondNow = Mathf.FloorToInt(_skipWindowTimer);
                    if (secondNow != _lastBreakSecond)
                    {
                        _lastBreakSecond = secondNow;
                        OnBreakStateChanged?.Invoke();
                    }
                }
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!_debugPause) return;
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(1f, 0.85f, 0f) }
            };
            GUI.Label(new Rect(0, 8, Screen.width, 36), "WAVE PAUSED [F5]", style);
        }
#endif

        // Called by HudController on button click or N keypress (debounced externally)
        public void StartNextWave()
        {
            if (!_waitingForPlayerStart) return;

            bool inWindow = _skipWindowTimer > 0f;
            if (inWindow)
            {
                // Claim skip bonus
                var cfg = BalanceConfig.Get();
                _streakCount = Mathf.Min(cfg.StreakCap, _streakCount + 1);
                StreakRewardMul = 1f + _streakCount * cfg.StreakBonusPerWave;
                Economy.Instance?.AddGold(cfg.SkipBonusGold);
#if UNITY_EDITOR
                Debug.Log($"[WaveManager] Skip bonus claimed — +{cfg.SkipBonusGold}¢ streak={_streakCount} rewardMul={StreakRewardMul:F2}");
#endif
            }
            // Q6=B: streak reset only when the 5s window expires without a claim.
            // If player clicks after the window expired, streak was already reset in Update().
            // If it's wave 1 (no prior wave, window never opened), _streakCount stays 0.

            _skipWindowTimer = 0f;
            _waitingForPlayerStart = false;
            OnBreakStateChanged?.Invoke();

            // Endless mode: extend wave list before the boundary check.
            if (LevelRunner.Instance?.IsEndlessRun == true && _nextWaveToStart >= levelData!.Waves.Count)
                EndlessMode.Instance?.AppendNextWave(levelData, _nextWaveToStart);

            if (_nextWaveToStart < levelData!.Waves.Count)
            {
                BeginWave(_nextWaveToStart);
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

        private void SpawnEnemy(EnemyType type, int wavePortalIdx, EnemyVariant variant = EnemyVariant.Normal)
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
            Vector3 spawnPos = pm.GetWaypointOnPath(resolvedPathIdx, 0);

            _spawnCounter++;
            _waveTotalSpawned++;

            var enemy = EnemyPool.Instance!.SpawnFromType(type, spawnPos, resolvedPathIdx, _currentWaveScaleMul, variant);
            _activeEnemies.Add(enemy);
            EventManager.Instance?.Publish(new EnemySpawnedEvent(enemy));
#if UNITY_EDITOR
            Debug.Log($"[WaveManager] spawned {type.Id} pathIdx={resolvedPathIdx} active={_activeEnemies.Count}");
#endif
        }

        // wavePortalIdx == -1 → round-robin; >= 0 → match portal index, fallback round-robin
        private int ResolvePathIdx(int wavePortalIdx)
        {
            var pm = PathManager.Instance!;
            int pathCount = pm.Paths.Count;

            if (wavePortalIdx < 0)
                return _spawnCounter % pathCount;

            var meta = pm.PathsMeta;
            for (int i = 0; i < meta.Count; i++)
                if (meta[i].PortalIdx == wavePortalIdx) return i;

            return _spawnCounter % pathCount;
        }

        // D1-04: +5 HP/wave W1-5, no regen W6+ (threshold in BalanceConfig)
        private void HandleWaveClearedRegen()
        {
            int currentWorld = LevelRunner.Instance?.CurrentLevel?.World ?? 1;
            if (currentWorld < BalanceConfig.Get().NoRegenWorldThreshold)
                LevelRunner.Instance?.PrimaryCastle?.Regen(5);
        }

        // D1-04: pressure mob — world-based curve 0% W1 → 60% W10, + per-wave-cleared boost
        // (+5%/wave depuis W1 si currentWorld >= 1), spawned 3s after wave start, speed ×1.5
        private void SpawnPressureMob(int waveIdx, int worldId)
        {
            float worldRate = Mathf.Clamp01((worldId - 1) / 9f * 0.6f);
            float waveBoost = worldId >= 1 ? waveIdx * 0.05f : 0f;
            float pressureRate = Mathf.Clamp01(worldRate + waveBoost);
            if (UnityEngine.Random.value > pressureRate) return;
            StartCoroutine(SpawnPressureDelayed(3f, 1.5f, waveIdx, worldId));
        }

        private System.Collections.IEnumerator SpawnPressureDelayed(float delaySec, float speedMul, int waveIdx, int worldId)
        {
            yield return new WaitForSeconds(delaySec);
            if (!_waveActive) yield break;
            if (levelData == null || _currentWaveIdx < 0 || _currentWaveIdx >= levelData.Waves.Count) yield break;
            var wave = levelData.Waves[_currentWaveIdx];

            // Pick a random non-null enemy type from the wave
            EnemyType? pick = null;
            foreach (var entry in wave.entries)
            {
                if (entry.type != null) { pick = entry.type; break; }
            }
            if (pick == null) yield break;

            int resolvedPathIdx = ResolvePathIdx(wave.portalIdx);
            if (PathManager.Instance == null || EnemyPool.Instance == null) yield break;
            Vector3 spawnPos = PathManager.Instance.GetWaypointOnPath(resolvedPathIdx, 0);
            var enemy = EnemyPool.Instance.SpawnFromType(pick, spawnPos, resolvedPathIdx, _currentWaveScaleMul);
            enemy.ApplySpeedMultiplier(speedMul);
            _activeEnemies.Add(enemy);
            _waveTotalSpawned++;
            EventManager.Instance?.Publish(new EnemySpawnedEvent(enemy));
#if UNITY_EDITOR
            Debug.Log($"[WaveManager] PressureMob spawned {pick.Id} speed×{speedMul} W{worldId} wave{waveIdx + 1}");
#endif
        }

        public WaveDef? GetNextWaveDef()
        {
            if (levelData == null || _nextWaveToStart >= levelData.Waves.Count) return null;
            return levelData.Waves[_nextWaveToStart];
        }

        public WaveDef? GetWaveDef(int idx)
        {
            if (levelData == null || idx < 0 || idx >= levelData.Waves.Count) return null;
            return levelData.Waves[idx];
        }

        public void NotifyEnemyDied(Enemy e)
        {
            _activeEnemies.Remove(e);
            if (_waveActive || _pendingSpawns.Count > 0)
            {
                _waveKillCount++;
                OnKillCountChanged?.Invoke(_waveKillCount, _waveTotalSpawned);
            }
        }

        private void TriggerBossWarningIfNeeded(WaveDef wave)
        {
            foreach (var entry in wave.entries)
            {
                if (entry.type != null && entry.type.IsBoss)
                {
                    StartCoroutine(BossWarnCoroutine(entry.type.DisplayName, wave.spawnRateMs / 1000f));
                    return;
                }
            }
        }

        private System.Collections.IEnumerator BossWarnCoroutine(string displayName, float firstSpawnDelaySec)
        {
            float warnDelay = Mathf.Max(0f, firstSpawnDelaySec - 3f);
            if (warnDelay > 0f) yield return new WaitForSeconds(warnDelay);
            if (!_waveActive) yield break;
            EventManager.Instance?.Publish(new BossWarningEvent(displayName));
        }

        // Called by boss enemies when they summon a minion mid-wave.
        public void RegisterSpawnedEnemy(Enemy e) => _activeEnemies.Add(e);

        // Called by Castle.TakeDamage — streak is broken if castle leaks during the break window.
        public void NotifyCastleDamaged()
        {
            if (!_waitingForPlayerStart || _streakCount == 0) return;
            _streakCount = 0;
            StreakRewardMul = 1f;
#if UNITY_EDITOR
            Debug.Log("[WaveManager] Castle damaged — streak reset");
#endif
            OnBreakStateChanged?.Invoke();
        }

        // N42: Defensive prune — remove any null/destroyed Enemy entries from _activeEnemies.
        // Called once per frame from LateUpdate to ensure ActiveEnemies is iteration-safe.
        // Normally NotifyEnemyDied removes enemies cleanly, but if an Enemy is destroyed
        // externally (e.g. via Destroy() without HandleDeath chain), the list would hold
        // a dead reference that throws MissingRef on subsequent transform reads.
        private void LateUpdate()
        {
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                var e = _activeEnemies[i];
                if (e == null) _activeEnemies.RemoveAt(i);
            }
        }
    }
}
