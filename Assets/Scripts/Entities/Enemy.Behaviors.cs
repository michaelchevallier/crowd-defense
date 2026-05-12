#nullable enable
using System.Collections;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public partial class Enemy : MonoBehaviour
    {
        public void ApplyVariant(CrowdDefense.Data.EnemyVariant v)
        {
            switch (v)
            {
                case CrowdDefense.Data.EnemyVariant.Fast:
                    _variantSpeedMul *= 1.5f;
                    SetTint(Color.yellow);
                    break;
                case CrowdDefense.Data.EnemyVariant.Tough:
                    maxHp = maxHp * 1.5f;
                    hp    = maxHp;
                    SetTint(new Color(0.6f, 0.3f, 0.1f));
                    break;
                case CrowdDefense.Data.EnemyVariant.Regen:
                    _regenPerSec = 2f;
                    SetTint(Color.green);
                    break;
                case CrowdDefense.Data.EnemyVariant.Armored:
                    _dmgReduction = 0.3f;
                    SetTint(new Color(0.7f, 0.7f, 0.8f));
                    break;
            }
        }

        // Called by EnemyPool after Init when the 10% elite roll succeeds (W5+, non-boss).
        public void ApplyElite()
        {
            _isElite = true;
            // Scale x1.15 — elite tier is visually distinct but not boss-sized
            float eliteScale = _bossBaseScale * 1.15f;
            if (_popInCoroutine != null)
            {
                StopCoroutine(_popInCoroutine);
                _popInCoroutine = StartCoroutine(SpawnPopIn(eliteScale, cfg?.IsBoss ?? false));
            }
            else
            {
                transform.localScale = Vector3.one * eliteScale;
            }
            // HP +50%
            hp    *= 1.5f;
            maxHp *= 1.5f;
            // Gold tint via MPB — reuses the already-allocated _mpb from Init
            if (_cachedRenderers != null)
            {
                _mpb ??= new MaterialPropertyBlock();
                var gold = new Color(1f, 0.85f, 0.2f, 1f);
                _mpb.SetColor(_baseColorId, gold);
                _mpb.SetColor(_colorId,     gold);
                for (int i = 0; i < _cachedRenderers.Length; i++)
                    _cachedRenderers[i].SetPropertyBlock(_mpb);
            }
            // Yellow scintillating trail particle
            SpawnEliteTrail();
        }

        private void UpdateStealth()
        {
            if (cfg == null || !cfg.IsStealth) return;
            float cycleS = cfg.StealthCycleMs > 0 ? cfg.StealthCycleMs / 1000f : 2.2f;
            float phase  = (Time.time % cycleS) / cycleS;
            bool visible = phase < 0.5f;
            float alpha  = visible ? 1f : cfg.StealthOpacity;
            StealthAlpha = alpha;
            ApplyTint(new Color(baseColor.r, baseColor.g, baseColor.b, alpha));

            // Pulsing ground ring indicator
            EnsureStealthRing();
            if (_stealthRingMR != null)
            {
                _stealthRingMpb ??= new MaterialPropertyBlock();
                if (visible)
                {
                    float pulse = 1f + 0.2f * Mathf.Sin(Time.time * 8f);
                    _stealthRingGO!.transform.localScale = Vector3.one * (StealthRingRadius * 2f * pulse);
                    _stealthRingMpb.SetColor(_baseColorId, new Color(1f, 0.53f, 0.13f,
                        0.45f + 0.35f * (Mathf.Sin(Time.time * 8f) * 0.5f + 0.5f)));
                }
                else
                {
                    _stealthRingGO!.transform.localScale = Vector3.one * (StealthRingRadius * 2f);
                    _stealthRingMpb.SetColor(_baseColorId, new Color(1f, 1f, 1f, 0.25f));
                }
                _stealthRingMR.SetPropertyBlock(_stealthRingMpb);
            }
        }

        private void UpdateSummons()
        {
            if (cfg == null || !cfg.SummonsMinions || cfg.SummonType == null) return;
            summonTimer += Time.deltaTime * 1000f;
            float effectiveCooldown = cfg.SummonCooldownMs * _enragedSummonCdMul;
            if (summonTimer >= effectiveCooldown)
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

        private void BuildDebuffIcons()
        {
            if (_debuffIcons[0] != null) return; // already built (pool reuse)
            float spread = 0.25f;
            float startX = -spread * 1.5f;
            for (int i = 0; i < 4; i++)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = $"DebuffIcon{i}";
                Object.Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(transform, false);
                quad.transform.localPosition = new Vector3(startX + i * spread, 1.7f, 0f);
                quad.transform.localScale    = Vector3.one * 0.2f;
                var mr = quad.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                    mat.color = DebuffColors[i];
                    mr.material = mat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }
                quad.SetActive(false);
                _debuffIcons[i] = quad;
            }
        }

        private void UpdateDebuffIcons()
        {
            if (_debuffIcons[0] == null) return;
            float now = Time.time;
            bool slow  = currentSpeedMul < 0.99f;
            bool burn  = _burnUntilTime > 0f && now < _burnUntilTime;
            bool freeze = _freezeUntilTime > 0f && now < _freezeUntilTime;
            bool armor = _dmgTakenMulUntil > 0f && now < _dmgTakenMulUntil;
            bool[] active = { slow, burn, freeze, armor };
            for (int i = 0; i < 4; i++)
            {
                var icon = _debuffIcons[i];
                if (icon == null) continue;
                bool show = active[i];
                if (icon.activeSelf != show)
                    icon.SetActive(show);
                if (show && MainCameraCache.Main != null)
                    icon.transform.rotation = Quaternion.LookRotation(MainCameraCache.Main.transform.forward);
            }
        }

        private void UpdateGroundDecals()
        {
            if (++_decalFrame % 2 != 0) return; // throttle: every 2 frames

            float now  = Time.time;
            bool slow  = currentSpeedMul < 0.99f;
            bool burn  = _burnUntilTime > 0f && now < _burnUntilTime;

            // ── Slow decal (cyan glow) ──────────────────────────────────────
            if (slow)
            {
                if (_decalSlow == null)
                {
                    _decalSlow = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    _decalSlow.name = "DecalSlow";
                    Object.Destroy(_decalSlow.GetComponent<Collider>());
                    _decalSlow.transform.SetParent(transform, false);
                    _decalSlow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    _decalSlow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    _decalSlow.transform.localScale    = Vector3.one * 1.5f;
                    _decalSlowRend = _decalSlow.GetComponent<MeshRenderer>();
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                    mat.color = new Color(0f, 1f, 1f, 0.45f);
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // transparent
                    _decalSlowRend.material = mat;
                    _decalSlowRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _decalSlowRend.receiveShadows = false;
                }
                if (!_decalSlow.activeSelf) _decalSlow.SetActive(true);
                _decalMpb ??= new MaterialPropertyBlock();
                // alpha pulse 0.3-0.6
                float alpha = 0.3f + 0.3f * (0.5f + 0.5f * Mathf.Sin(now * 4f));
                _decalMpb.Clear();
                _decalMpb.SetColor("_BaseColor", new Color(0f, 1f, 1f, alpha));
                _decalSlowRend!.SetPropertyBlock(_decalMpb);
            }
            else if (_decalSlow != null && _decalSlow.activeSelf)
                _decalSlow.SetActive(false);

            // ── Burn decal (orange-red) + sparks ───────────────────────────
            if (burn)
            {
                if (_decalBurn == null)
                {
                    _decalBurn = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    _decalBurn.name = "DecalBurn";
                    Object.Destroy(_decalBurn.GetComponent<Collider>());
                    _decalBurn.transform.SetParent(transform, false);
                    _decalBurn.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                    _decalBurn.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    _decalBurn.transform.localScale    = Vector3.one * 1.5f;
                    _decalBurnRend = _decalBurn.GetComponent<MeshRenderer>();
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                    mat.color = new Color(1f, 0.3f, 0f, 0.4f);
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                    _decalBurnRend.material = mat;
                    _decalBurnRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _decalBurnRend.receiveShadows = false;
                }
                if (!_decalBurn.activeSelf) _decalBurn.SetActive(true);
                // occasional spark at feet
                if (UnityEngine.Random.value < 0.15f)
                    VfxPool.Instance?.SpawnImpact(transform.position + Vector3.up * 0.1f, new Color(1f, 0.45f, 0f));
            }
            else if (_decalBurn != null && _decalBurn.activeSelf)
                _decalBurn.SetActive(false);
        }

        private void SpawnMinion()
        {
            if (cfg?.SummonType == null) return;
            if (EnemyPool.Instance == null) return;
            if (PathManager.Instance == null || PathManager.Instance.Paths.Count == 0) return;

            Vector3 spawnPos = transform.position + Vector3.forward * 0.5f;
            var minion = EnemyPool.Instance.SpawnFromType(cfg.SummonType, spawnPos, pathIdx);
            WaveManager.Instance?.RegisterSpawnedEnemy(minion);
#if UNITY_EDITOR
            Debug.Log($"[Enemy] boss {cfg.Id} summons {cfg.SummonType.Id}");
#endif
        }

        // 60% HP trigger — spawns 3 fast minions at random offsets with red portal rings.
        // Uses cfg.SummonType if set; otherwise spawns the same type with Fast variant applied.
        private void SpawnMinionBurst()
        {
            if (cfg == null || EnemyPool.Instance == null) return;
            if (PathManager.Instance == null || PathManager.Instance.Paths.Count == 0) return;

            var spawnType = cfg.SummonType ?? cfg;
            bool applyFast = cfg.SummonType == null;

            for (int i = 0; i < 3; i++)
            {
                float angle  = i * 120f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * 1.5f, 0f, Mathf.Sin(angle) * 1.5f);
                Vector3 spawnPos = transform.position + offset;

                VfxPool.Instance?.SpawnPortal(spawnPos);

                var minion = EnemyPool.Instance.SpawnFromType(spawnType, spawnPos, pathIdx);
                if (applyFast) minion.ApplyVariant(CrowdDefense.Data.EnemyVariant.Fast);
                WaveManager.Instance?.RegisterSpawnedEnemy(minion);
            }
#if UNITY_EDITOR
            Debug.Log($"[Enemy] Boss {cfg.Id} 60% HP burst — 3 {spawnType.Id} minions summoned");
#endif
        }

        private void SpawnMinionByType(string typeId)
        {
            if (EnemyPool.Instance == null) return;
            var spawnType = cfg?.SummonType;
            if (spawnType == null) return;
            Vector3 spawnPos = transform.position + Vector3.forward * 0.5f;
            var minion = EnemyPool.Instance.SpawnFromType(spawnType, spawnPos, pathIdx);
            WaveManager.Instance?.RegisterSpawnedEnemy(minion);
        }

        // Spawns a minion at worldPos. Used by phase 3 and EnemyBossBehaviors burst patterns.
        internal void SpawnMinionAt(Vector3 worldPos)
        {
            if (EnemyPool.Instance == null) return;
            var spawnType = cfg?.SummonType;
            if (spawnType == null) return;
            var minion = EnemyPool.Instance.SpawnFromType(spawnType, worldPos, pathIdx);
            WaveManager.Instance?.RegisterSpawnedEnemy(minion);
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
            VfxPool.Instance?.SpawnExplosion(transform.position + Vector3.up * 0.6f, cfg.AoeBlastRadius);
#if UNITY_EDITOR
            Debug.Log($"[Enemy] boss {cfg.Id} AoE blast radius={cfg.AoeBlastRadius} hit {hit} towers");
#endif
        }

        // ── Pool release ──────────────────────────────────────────────────────

    }
}
