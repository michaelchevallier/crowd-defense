#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.UI;
using CrowdDefense.Visual;

namespace CrowdDefense.Systems
{
    public class BossSystem : MonoSingleton<BossSystem>
    {
        [SerializeField] private List<BossDef> registry = new();

        private readonly Dictionary<string, BossDef> _byEnemyId = new();
        private Enemy? _currentBoss;
        private BossDef? _currentDef;
        private float _lastRatio = 1f;
        private int _currentPhase = 0; // 0=normal, 1=enraged, 2=desperate
        private bool _defeatedPublished = false;

        protected override void OnAwakeSingleton()
        {
            RebuildDict();
        }

        private void Start()
        {
            // Rebuild in Start too — guards against domain reload emptying the dict (R4)
            RebuildDict();
            var em = EventManager.Instance;
            if (em == null) return;
            em.Subscribe<EnemySpawnedEvent>(OnEnemySpawned);
            em.Subscribe<LevelEndedEvent>(OnLevelEnded);
        }

        protected override void OnDestroySingleton()
        {
            var em = EventManager.Instance;
            if (em == null) return;
            em.Unsubscribe<EnemySpawnedEvent>(OnEnemySpawned);
            em.Unsubscribe<LevelEndedEvent>(OnLevelEnded);
        }

        private void RebuildDict()
        {
            _byEnemyId.Clear();
            foreach (var def in registry)
            {
                if (def == null || def.EnemyType == null) continue;
                _byEnemyId[def.EnemyType.Id] = def;
            }
#if UNITY_EDITOR
            if (_byEnemyId.Count == 0 && registry.Count > 0)
                Debug.LogWarning("[BossSystem] dict empty after rebuild — check BossDef.EnemyType refs in Inspector");
#endif
        }

        private void OnLevelEnded(LevelEndedEvent _) => ResetBoss();

        private void OnEnemySpawned(EnemySpawnedEvent e)
        {
            var cfg = e.Enemy.Config;
            if (cfg == null || !cfg.IsBoss) return;
            if (!_byEnemyId.TryGetValue(cfg.Id, out var def))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[BossSystem] No BossDef for enemy id='{cfg.Id}' — add to registry");
#endif
                return;
            }

            // R6: if a previous boss is still alive, publish defeated for it first
            PublishDefeatOnce();

            _currentBoss = e.Enemy;
            _currentDef = def;
            _currentPhase = 0;
            _lastRatio = 1f;
            _defeatedPublished = false;

            var juice = JuiceConfig.Get();
            JuiceFX.Instance?.Shake(juice.BossSpawnShakeAmp, Mathf.RoundToInt(juice.BossSpawnShakeDur * 1000f));
            JuiceFX.Instance?.SlowMo(juice.BossSpawnSlowMoScale, juice.BossSpawnSlowMoDurMs);
            EventManager.Instance?.Publish(new BossEncounteredEvent(
                def.DisplayNameFr, cfg.Hp, def.AuraColor, e.Enemy.transform.position));
        }

        private void LateUpdate()
        {
            if (_currentBoss == null) return;

            if (_currentBoss.IsDead)
            {
                PublishDefeatOnce();
                return;
            }

            float ratio = _currentBoss.HpRatio;
            // Debounce: only publish if delta exceeds threshold (zero-alloc: struct record)
            if (Mathf.Abs(ratio - _lastRatio) > 0.005f)
            {
                _lastRatio = ratio;
                EventManager.Instance?.Publish(new BossHpChangedEvent(ratio));
            }

            // Enraged threshold — trigger once only
            if (_currentPhase == 0 && _currentDef != null && ratio <= _currentDef.EnragedAt)
            {
                _currentPhase = 1;
                _currentBoss.ApplyEnragedPhase(_currentDef.EnragedSpeedMul, _currentDef.EnragedSummonCdMul);
                EventManager.Instance?.Publish(new BossPhaseChangedEvent("enraged", 1));
            }

            // Desperate threshold — trigger once only
            if (_currentPhase == 1 && _currentDef != null && ratio <= _currentDef.DesperateAt)
            {
                _currentPhase = 2;
                EventManager.Instance?.Publish(new BossPhaseChangedEvent("desperate", 2));
            }
        }

        private void PublishDefeatOnce()
        {
            if (_defeatedPublished || _currentDef == null) return;
            _defeatedPublished = true;
            EventManager.Instance?.Publish(new BossDefeatedEvent(_currentDef.DisplayNameFr));
            Toast.Show("Boss Defeated", _currentDef.DisplayNameFr, 4000, null, ToastType.Achievement);

            Vector3 pos = _currentBoss != null ? _currentBoss.transform.position : Vector3.zero;
            VfxPool.Instance?.SpawnConfetti(pos, 2f);
            JuiceFX.Instance?.Flash(new Color(1f, 0.9f, 0.2f, 0.55f), 400);
        }

        private void ResetBoss()
        {
            PublishDefeatOnce();
            _currentBoss = null;
            _currentDef = null;
            _currentPhase = 0;
            _lastRatio = 1f;
            _defeatedPublished = false;
        }
    }
}
