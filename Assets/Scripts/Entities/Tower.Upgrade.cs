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
        public void RegisterKill()
        {
            // N38: belt-and-braces guard — if this is called via a stale reference after
            // PlayDestroyAnim, skip silently. N33 in callers prevents this, but multiple
            // event subscribers + projectile-in-flight chains can race past those guards.
            if (this == null || _destroyStarted) return;
            TotalKills++;
            TakeDamage(1);
            if (Time.time - _lastKillTime < StreakWindow)
                _streakCount = Mathf.Min(_streakCount + 1, StreakMax);
            else
                _streakCount = 1;
            _lastKillTime = Time.time;
            if (_streakCount > 3)
                CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                    $"x{_streakCount}", transform.position + Vector3.up * 2f, new Color(1f, 0.85f, 0.1f));

            SpawnKillFloatText();
        }

        private void SpawnKillFloatText()
        {
            if (!gameObject.activeInHierarchy) return;
            var popup = CrowdDefense.UI.FloatingPopupController.Instance;
            if (popup == null) return;

            Vector3 pos = transform.position + Vector3.up * 1.5f;
            bool isCrit = _lastKillWasCrit;
            _lastKillWasCrit = false;

            if (_streakCount >= 5)
            {
                popup.SpawnReward($"+1 STREAK x{_streakCount}!", pos, new Color(1f, 0.75f, 0f));
            }
            else if (isCrit)
            {
                popup.SpawnReward("+1 CRIT!", pos, new Color(1f, 0.9f, 0.1f));
            }
            else
            {
                popup.SpawnReward("+1", pos, Color.white);
            }
        }

        private void TakeDamage(int amount)
        {
            _hp = Mathf.Max(0, _hp - amount);
            UpdateHpAlpha();
            if (_hp <= 0 && !_destroyStarted)
            {
                PlacementController.Instance?.UnregisterTower(this);
                Synergies.Instance?.MarkDirty();
                StartCoroutine(PlayDestroyAnim());
            }
        }

        public void ReceiveEnemySplash(int amount)
        {
            TakeDamage(amount);
            VfxPool.Instance?.SpawnImpact(transform.position + Vector3.up * 0.8f, new Color(1f, 0.2f, 0f));
        }

        public bool Repair()
        {
            if (_hp >= _maxHp) return false;
            int cost = RepairCost;
            if (cost <= 0) return false;
            if (Economy.Instance == null || !Economy.Instance.TrySpend(cost)) return false;
            _hp = _maxHp;
            UpdateHpAlpha();
            VfxPool.Instance?.SpawnImpact(transform.position + Vector3.up * 1.2f, new Color(0.3f, 1f, 0.4f));
            AudioController.Instance?.Play3D("powerup", transform.position);
            CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                "+HP", transform.position + Vector3.up * 2f, new Color(0.3f, 1f, 0.4f));
            return true;
        }

        private void UpdateHpAlpha()
        {
            float ratio = _maxHp > 0 ? (float)_hp / _maxHp : 1f;
            float alpha = 0.5f + ratio * 0.5f;
            var root = _meshChild != null ? _meshChild : gameObject;
            foreach (var r in root.GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials)
                    if (m != null && m.HasProperty("_BaseColor"))
                    {
                        Color c = m.GetColor("_BaseColor");
                        c.a = alpha;
                        m.SetColor("_BaseColor", c);
                    }
        }

        public bool UpgradeTo(int level, TowerBranch branch = TowerBranch.None)
        {
            if (cfg == null || Economy.Instance == null) return false;
            if (level != UpgradeLevel + 1) return false;
            if (level < 2 || level > 3) return false;

            var bal = BalanceConfig.Get();
            float mul = level == 2 ? bal.UpgradeMulL2 : bal.UpgradeMulL3;
            int cost = Mathf.RoundToInt(cfg.Cost * mul);

            if (!Economy.Instance.TrySpend(cost)) return false;

            CumulativeCost += cost;
            UpgradeLevel = level;

            float[] scales = bal.LevelScale;
            int scaleIdx = Mathf.Clamp(level - 1, 0, scales.Length - 1);
            _levelDmgScale = scales.Length > scaleIdx ? scales[scaleIdx] : 1f;

            if (level == 3)
            {
                ApplyL3Branch(branch);
                Achievements.Instance?.Unlock("max_upgrade_tower");
            }

            PostUpgradeVisuals(level);

            VfxPool.Instance?.SpawnUpgradeBurst(transform.position + Vector3.up * 1.5f, level);
            AudioController.Instance?.Play3D("tower_upgrade", transform.position);
            AudioController.Instance?.Play3D("powerup", transform.position);
            JuiceFX.Instance?.PunchScale(transform, 1.25f, 0.4f);
            var jcUpgrade = JuiceConfig.Get();
            JuiceFX.Instance?.Flash(
                new Color(1f, 0.84f, 0f, jcUpgrade.TowerUpgradeFlashAlpha),
                jcUpgrade.TowerUpgradeFlashMs);
            CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                $"L{level}!", transform.position + Vector3.up * 2f, Color.cyan);

#if UNITY_EDITOR
            Debug.Log($"[Tower] UpgradeTo L{level} cost={cost} cumul={CumulativeCost} dmgScale={_levelDmgScale:F2} branch={branch}");
#endif
            Synergies.Instance?.MarkDirty();
            return true;
        }

        private void ApplyL3Branch(TowerBranch branch)
        {
            if (cfg == null) return;

            bool isSignature = cfg.Id is "archer" or "crossbow" or "tank" or "mage";

            if (isSignature && branch == TowerBranch.None)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[Tower] L3 signature {cfg.Id} sans branche — fallback Dps");
#endif
                branch = TowerBranch.Dps;
            }

            UpgradeBranch = isSignature ? branch : TowerBranch.None;

            if (!isSignature) return;

            if (_l3StatsTable.TryGetValue((cfg.Id, branch), out var stats))
            {
                ApplyL3Stats(stats);
            }
        }

        private void ApplyL3Stats(L3Stats stats)
        {
            if (stats.MultiShot != 0) L3MultiShot = stats.MultiShot;
            if (stats.CascadeRadius != 0f) L3CascadeRadius = stats.CascadeRadius;
            if (stats.FinalExplosion) L3FinalExplosion = true;
            if (stats.FinalExplosionAoe != 0) L3FinalExplosionAoe = stats.FinalExplosionAoe;
            if (stats.FinalExplosion && cfg != null)
            {
                L3FinalExplosionDmg = cfg.Damage * BalanceConfig.Get().TowerDamageMul * TalentSystem.TowerDamageMul * 2.5f;
            }
            if (stats.Pierce != 0 && cfg != null) L3Pierce = cfg.Pierce + stats.Pierce;
            if (stats.CritChance != 0) L3CritChance = stats.CritChance;
            if (stats.CritMul != 0) L3CritMul = stats.CritMul;
            if (stats.ChainLightningJumps != 0) L3ChainLightningJumps = stats.ChainLightningJumps;
            if (stats.ChainLightningRange != 0) L3ChainLightningRange = stats.ChainLightningRange;
            if (stats.FreezeOnHit) L3FreezeOnHit = true;
            if (stats.FreezeDurMs != 0) L3FreezeDurMs = stats.FreezeDurMs;
            if (stats.BerserkerActive) L3BerserkerActive = true;
            if (stats.BerserkerDmgMul != 0) L3BerserkerDmgMul = stats.BerserkerDmgMul;
            if (stats.BerserkerHpThreshold != 0) L3BerserkerHpThreshold = stats.BerserkerHpThreshold;
            if (stats.BulwarkAura) L3BulwarkAura = true;
            if (stats.BulwarkAuraRange != 0) L3BulwarkAuraRange = stats.BulwarkAuraRange;
            if (stats.BulwarkDmgReduction != 0) L3BulwarkDmgReduction = stats.BulwarkDmgReduction;
        }

        public void Sell()
        {
            if (cfg == null) return;
            if (UpgradeLevel == 3)
            {
                CrowdDefense.UI.Confirm.Show(
                    "Vendre une tour L3 ?",
                    "Vraiment vendre cette tour L3 ?",
                    () => DoSell());
                return;
            }
            DoSell();
        }

        private void DoSell()
        {
            if (cfg == null) return;
            var bal = BalanceConfig.Get();
            int refund = Mathf.RoundToInt(CumulativeCost * bal.SellRefundRatio);
            Economy.Instance?.AddGold(refund);
            PlacementController.Instance?.UnregisterTower(this);
            Synergies.Instance?.MarkDirty();
#if UNITY_EDITOR
            Debug.Log($"[Tower] Sell cumul={CumulativeCost} refund={refund} ratio={bal.SellRefundRatio:F2}");
#endif
            Vector3 pos = transform.position;
            JuiceFX.Instance?.PunchScale(transform, 0.7f, 0.3f);
            VfxPool.Instance?.SpawnImpact(pos + Vector3.up * 0.5f, new Color(0.55f, 0.55f, 0.55f));
            AudioController.Instance?.Play3D("tower_sold", pos);
            AudioController.Instance?.Play3D("powerup", pos);
            for (int i = 0; i < 5; i++)
            {
                Vector3 offset = Random.insideUnitSphere * 0.5f;
                CrowdDefense.UI.FloatingPopupController.Instance?.SpawnReward(
                    "\U0001F4B0", pos + offset, Color.yellow);
            }
            StartCoroutine(PlayDestroyAnim());
        }

        private IEnumerator PlayDestroyAnim()
        {
            _destroyStarted = true;
            Vector3 startScale = transform.localScale;
            float targetRotZ = Random.Range(-45f, 45f);
            float startRotZ = transform.localEulerAngles.z;
            Vector3 pos = transform.position;
            VfxPool.Instance?.SpawnDeathPuff(pos + Vector3.up * 0.5f, tier: 1);
            VfxPool.Instance?.SpawnImpact(pos + Vector3.up * 0.3f, new Color(0.65f, 0.55f, 0.4f));
            float elapsed = 0f;
            const float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - t * t;
                transform.localScale = startScale * eased;
                float rot = Mathf.LerpAngle(startRotZ, targetRotZ, t);
                var e = transform.localEulerAngles;
                e.z = rot;
                transform.localEulerAngles = e;
                yield return null;
            }
            Destroy(gameObject);
        }

        private void PostUpgradeVisuals(int level)
        {
            DrawTierPips(level);
            SpawnUpgradeRing(level);
            SpawnUpgradeConfetti(level);
            ApplyL3Tint();
        }

        private void SpawnUpgradeRing(int newLevel)
        {
            const float Duration = 0.6f;
            const int RingCount = 4;
            float ringSpacing = 0.15f;
            for (int i = 0; i < RingCount; i++)
            {
                float delay = i * ringSpacing;
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "UpgradeRing_" + i;
                go.transform.position = transform.position + Vector3.up * 0.5f;
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                go.transform.localScale = Vector3.one * 0.1f;
                Object.Destroy(go.GetComponent<Collider>());

                Color color = newLevel switch
                {
                    2 => new Color(0.3f, 0.8f, 1f),
                    3 => new Color(1f, 0.8f, 0.3f),
                    _ => Color.white,
                };
                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    var baseMat = rend.sharedMaterial != null ? rend.sharedMaterial : new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit")!);
                    var mat = new Material(baseMat);
                    mat.color = color;
                    rend.material = mat;
                }

                StartCoroutine(AnimateRingParticle(go, Vector3.up * 2f, Duration));
            }

            var ac = AudioController.Instance;
            if (ac != null)
            {
                float pitch = newLevel switch { 2 => 1.1f, 3 => 1.35f, _ => 1f };
                ac.Play3DPitched("upgrade_ring_chime", transform.position, 1f, pitch);
            }
        }

        private static IEnumerator AnimateRingParticle(GameObject go, Vector3 velocity, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (go == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                go.transform.position += velocity * Time.deltaTime;
                float scale = Mathf.Lerp(0.3f, 0f, t);
                go.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            if (go != null) Object.Destroy(go);
        }

        private void SpawnUpgradeConfetti(int newLevel)
        {
            var vfx = VfxPool.Instance;
            if (vfx != null)
            {
                vfx.SpawnUpgradeConfetti(transform.position + Vector3.up * 1.5f, newLevel);
            }

            var ac = AudioController.Instance;
            if (ac != null)
            {
                float pitch = 1f + newLevel * 0.1f;
                ac.Play3DPitched("tower_upgrade_celebration", transform.position, 0.8f, pitch);
            }
        }

        public void ApplyTierSkin(int tier)
        {
            if (cfg == null || tier <= 1) return;

            string suffix = tier == 2 ? "_t2" : "_t3";
            string variantKey = cfg.AssetKey + suffix;

            var registry = Resources.Load<AssetRegistry>("AssetRegistry");
            if (registry == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[Tower] AssetRegistry not found in ApplyTierSkin — using color tint fallback");
#endif
                var tintRoot = _meshChild != null ? _meshChild : gameObject;
                var fallbackColor = tier == 2 ? new Color(0.95f, 0.90f, 0.80f) : new Color(1f, 0.8f, 0.3f);
                MaterialController.UpdateTint(tintRoot, fallbackColor);
                if (tier == 3) SpawnGlowRing();
                return;
            }
            var variantPrefab = registry.Get(variantKey);

            if (variantPrefab != null)
            {
                SwapMeshChild(variantPrefab, variantKey);
            }
            else
            {
                var tintRoot = _meshChild != null ? _meshChild : gameObject;
                if (tier == 2)
                {
                    MaterialController.UpdateTint(tintRoot, new Color(0.95f, 0.90f, 0.80f));
                }
                else
                {
                    Color gold = new Color(1f, 0.8f, 0.3f);
                    MaterialController.UpdateTint(tintRoot, gold);
                    foreach (var r in tintRoot.GetComponentsInChildren<Renderer>())
                    {
                        foreach (var m in r.materials)
                        {
                            if (m == null) continue;
                            if (m.HasProperty("_EmissionColor"))
                            {
                                m.SetColor("_EmissionColor", gold * 0.3f);
                                m.EnableKeyword("_EMISSION");
                            }
                        }
                    }
                    SpawnGlowRing();
                }
            }
        }

        private void SwapMeshChild(GameObject variantPrefab, string variantKey)
        {
            if (_meshChild != null)
                Destroy(_meshChild);

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == "Base" || child.name == "Top")
                    child.gameObject.SetActive(false);
            }

            var inst = Object.Instantiate(variantPrefab, transform);
            inst.name = "Mesh_" + variantKey;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            _meshChild = inst;

            Color bodyColor = cfg != null ? cfg.BodyColor : Color.white;
            MaterialController.ApplyToon(_meshChild, bodyColor);
            Outline.ApplyToHierarchy(_meshChild.transform);

            _animator = AnimationController.SetupAnimator(_meshChild, "Idle", null);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_animator != null && !AnimationController.ValidateAnimatorSetup(_animator, $"Tower_Upgrade_{cfg?.Id}"))
                Debug.LogWarning($"[Tower.Upgrade] {cfg?.Id} animator validation failed.");
#endif

            _meshHead = FindChildNamed(_meshChild.transform, "Head")
                     ?? FindChildNamed(_meshChild.transform, "Turret");
            _barrelTip = _meshHead != null
                ? FindChildNamed(_meshHead.transform, "BarrelTip")?.transform
                : FindChildNamed(_meshChild.transform, "BarrelTip")?.transform;
        }

        public void ApplyL3Tint()
        {
            if (_l3TintApplied || UpgradeLevel < 3) return;
            Color tint = UpgradeBranch == TowerBranch.Dps
                ? new Color(0.9f, 0.15f, 0.10f)
                : new Color(0.10f, 0.80f, 0.85f);
            MaterialController.UpdateTint(gameObject, tint);
            _l3TintApplied = true;
        }
    }
}
