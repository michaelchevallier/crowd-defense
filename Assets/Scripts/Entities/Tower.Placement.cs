#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;
using CrowdDefense.UI;

namespace CrowdDefense.Entities
{
    public partial class Tower
    {
        private void UpdateCluster()
        {
            if (cfg == null) return;
            _clusterTimer -= Time.deltaTime;
            if (_clusterTimer <= 0f)
            {
                SpawnMineRing();
                _clusterTimer = cfg.CooldownMs / 1000f;
                if (_clusterTimer <= 0f) _clusterTimer = 12f;
            }
        }

        private void SpawnMineRing()
        {
            if (cfg == null) return;
            int count = cfg.ClusterCount > 0 ? cfg.ClusterCount : 3;
            float spawnRadius = cfg.Range / 2f;
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnRadius;
                Vector3 pos = transform.position + offset;

                var go = new GameObject("MineExplosive");
                go.transform.position = pos;
                var mine = go.AddComponent<MineExplosive>();
                mine.Init(cfg.Damage, cfg.Aoe > 0f ? cfg.Aoe : 2.5f);
            }
        }

        private void UpdateSlow()
        {
            if (cfg == null) return;
            _slowTickTimer -= Time.deltaTime;
            if (_slowTickTimer > 0f) return;
            _slowTickTimer = 0.15f;

            if (WaveManager.Instance == null || SlowEffectManager.Instance == null) return;
            float eff1Range = cfg.Range * ResearchRangeMul;
            float rangeSq = eff1Range * eff1Range;
            var enemies = WaveManager.Instance.ActiveEnemies;
            bool hitAny = false;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - transform.position).sqrMagnitude > rangeSq) continue;
                SlowEffectManager.Instance.ApplySlow(e, cfg.SlowMul, cfg.SlowDurationMs);
                hitAny = true;
            }

            bool isFrost = cfg.Id == "frost" || cfg.Id.Contains("ice");
            if (hitAny)
            {
                if (isFrost)
                    VfxPool.Instance?.SpawnFrost(transform.position, cfg.Range * 0.5f);
                else
                    VfxPool.Instance?.SpawnSlowField(transform.position, cfg.Range * 0.5f);
            }
        }

        private void UpdateCoinPull()
        {
            if (cfg == null || CoinPullManager.Instance == null) return;
            CoinPullManager.Instance.RegisterSource(
                transform.position,
                cfg.Range,
                cfg.CoinMul > 0f ? cfg.CoinMul : BalanceConfig.Get().MagnetCoinMul);

            if (SlowEffectManager.Instance == null || WaveManager.Instance == null) return;
            float slowR = BalanceConfig.Get().MagnetSlowRadius;
            float slowR2 = slowR * slowR;
            var myPos = transform.position;
            var active = WaveManager.Instance.ActiveEnemies;
            for (int i = 0; i < active.Count; i++)
            {
                var e = active[i];
                if (e == null || e.IsDead) continue;
                if ((e.transform.position - myPos).sqrMagnitude <= slowR2)
                    SlowEffectManager.Instance.ApplySlow(e, 0.7f, 500);
            }
        }
    }
}
