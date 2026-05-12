#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using CrowdDefense.UI;
using CrowdDefense.Visual;

namespace CrowdDefense.Entities
{
    public partial class Hero : MonoBehaviour
    {
        // ── XP / Level ────────────────────────────────────────────────────────
        public int   Level     { get; private set; } = 1;
        public int   Xp        { get; private set; }
        public int   XpToNext  { get; private set; }
        public int   MaxLevel  { get; private set; }

        // ── Perk multipliers ──────────────────────────────────────────────────
        public float FireRateMul      { get; internal set; } = 1f;
        public float RangeMul         { get; internal set; } = 1f;
        public float DamageMul        { get; internal set; } = 1f;
        public float MoveSpeedMul     { get; internal set; } = 1f;
        public float CoinGainMul      { get; internal set; } = 1f;
        public float XpMul            { get; internal set; } = 1f;
        public float CritChance       { get; internal set; }
        public float CritMul          { get; internal set; } = 2f;
        public int   CritStaggerMs    { get; internal set; }
        public int   MultiShot        { get; internal set; }
        public int   PierceCount      { get; internal set; }
        public float Lifesteal        { get; internal set; }
        public float WaveRegen        { get; internal set; }
        public int   MoveAttackPierceBonus { get; internal set; }
        public float CastleHPMaxMul   { get; internal set; } = 1f;
        public float CoinRewardMul    { get; internal set; } = 1f;
        public float TowerCostMul     { get; internal set; } = 1f;
        public float TowerFireRateAuraMul { get; internal set; } = 1f;
        public float TowerAuraRange   { get; internal set; }
        public int   AoeOnNthProjectile { get; internal set; }

        // School-specific flags
        public bool  ForteressePerk      { get; internal set; }
        public bool  MursPierre         { get; internal set; }
        public bool  CristalGlace       { get; internal set; }

        // Auto-attack toggle
        public bool AutoAttack
        {
            get => _autoAttack;
            set
            {
                _autoAttack = value;
                PlayerPrefs.SetInt(AutoAttackPrefsKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        // Channeling state
        public bool  ChannelingPill      { get; set; }

        // Projectile modifiers
        public bool  Fireball            { get; internal set; }
        public float FireballRadius      { get; internal set; } = 2f;
        public float FireballDmgMul      { get; internal set; } = 0.7f;
        public bool  Ricochet            { get; internal set; }
        public int   RicochetBounces     { get; internal set; } = 3;
        public float RicochetDecay       { get; internal set; } = 0.8f;
        public bool  Lightning           { get; internal set; }
        public int   LightningTargets    { get; internal set; } = 1;
        public float LightningDmgMul     { get; internal set; } = 0.6f;
        public bool  PierceExplode       { get; internal set; }
        public float PierceExplodeRadius { get; internal set; } = 1.5f;
        public float PierceExplodeDmgMul { get; internal set; } = 0.5f;
        public bool  Glaciation          { get; internal set; }
        public bool  Combustion          { get; internal set; }
        public bool  Pyromancie          { get; internal set; }

        // FirstTowerFree gate
        public bool  FirstTowerFree      { get; internal set; }
        public bool  FirstTowerFreeUsed  { get; set; }

        // ── Perk tracking ─────────────────────────────────────────────────────
        public IReadOnlyList<string> Perks => _perks;
        private readonly List<string> _perks = new();
        private readonly Dictionary<string, int> _activeTagsCount = new();
        private bool _suppressPerkVfx;

        /// <summary>
        /// Réinitialise les stats et réapplique la liste de perks depuis zéro.
        /// </summary>
        public void ApplyRunContext(IReadOnlyList<string> perkIds, int level = 1, int xp = 0)
        {
            ResetPerkStats();

            Level    = level;
            Xp       = xp;
            if (cfg != null) XpToNext = cfg.XpToNext(level);

            _suppressPerkVfx = true;
            PerkSystem.Instance?.ApplyPerkList(this, perkIds);
            _suppressPerkVfx = false;
            UpdatePerkIcons();
        }

        internal void AddPerkId(string id)
        {
            _perks.Add(id);
            UpdatePerkIcons();
            if (_suppressPerkVfx) return;

            var pos = transform.position;
            VfxPool.Instance?.SpawnLevelUp(pos + Vector3.up * 1.5f);
            AudioController.Instance?.Play("hero_levelup");
            JuiceFX.Instance?.PunchScale(transform, 1.3f, 0.3f);
            FloatingPopupController.Instance?.SpawnReward("LEVEL UP!", pos + Vector3.up * 2f, Color.yellow);
        }

        /// <summary>
        /// Applique les bonus méta (méta-progression).
        /// </summary>
        public void ApplyMetaBonuses(float heroDamageMul = 1f, float heroRangeMul = 1f,
            float heroFireRateMul = 1f, float coinGainMul = 1f, float xpMul = 1f)
        {
            DamageMul   *= heroDamageMul * CrowdDefense.Systems.TalentSystem.HeroPowerMul;
            RangeMul    *= heroRangeMul;
            FireRateMul *= 1f / Mathf.Max(heroFireRateMul, 0.01f);
            CoinGainMul *= coinGainMul;
            XpMul       *= xpMul;
        }

        /// <summary>
        /// Applique les bonus d'un skin héros.
        /// </summary>
        public void ApplySkinBonuses(SkinDef skin)
        {
            if (skin == null) return;
            if (skin.DamageMul    != 1f) DamageMul    *= skin.DamageMul;
            if (skin.RangeMul     != 1f) RangeMul     *= skin.RangeMul;
            if (skin.FireRateMul  != 1f) FireRateMul  *= 1f / Mathf.Max(skin.FireRateMul, 0.01f);
            if (skin.MoveSpeedMul != 1f) MoveSpeedMul *= skin.MoveSpeedMul;
            if (skin.CoinGainMul  != 1f) CoinGainMul  *= skin.CoinGainMul;
            if (skin.XpMul        != 1f) XpMul        *= skin.XpMul;
        }

        /// <summary>
        /// XP gain and level-up progression.
        /// </summary>
        public void GainXp(float amount)
        {
            if (Level >= MaxLevel) return;
            int earned = Mathf.Max(1, Mathf.RoundToInt(amount * XpMul));
            Xp += earned;
            CrowdDefense.UI.HeroPortraitController.Instance?.AnimateXpGain(earned);
            while (Xp >= XpToNext && Level < MaxLevel)
            {
                Xp -= XpToNext;
                Level++;
                if (cfg != null) XpToNext = cfg.XpToNext(Level);
                OnLevelUp?.Invoke(Level, Xp, XpToNext);

                var levelUpPos = transform.position;
                VfxPool.Instance?.SpawnLevelUp(levelUpPos + Vector3.up * 1.5f);
                VfxPool.Instance?.SpawnLevelUp(levelUpPos + Vector3.up * 1.8f);
                VfxPool.Instance?.SpawnConfetti(levelUpPos + Vector3.up * 1.5f, 1.5f);
                VfxPool.Instance?.SpawnUpgradeBurst(levelUpPos + Vector3.up * 1.0f, Level);
                FloatingPopupController.Instance?.SpawnReward("LEVEL UP!", levelUpPos + Vector3.up * 2.5f, new Color(1f, 0.84f, 0f));
                AudioController.Instance?.PlayPitched("hero_levelup", 1.2f, 1.1f);
                Toast.Show($"Hero Level {Level}!", "Vie restauree au maximum", 2000, null, ToastType.Achievement);
                _hp = _maxHp;
                JuiceFX.Instance?.SlowMo(0.5f, 500);
                JuiceFX.Instance?.Flash(new Color(1f, 0.84f, 0f, 0.4f), 200);
                JuiceFX.Instance?.PunchScale(transform, 1.5f, 0.4f);
                StartCoroutine(RimLightGlowRoutine());
                StartCoroutine(WhiteLightFlashRoutine());
            }
        }

        private System.Collections.IEnumerator RimLightGlowRoutine()
        {
            var lightGo = new GameObject("LevelUpRimLight");
            lightGo.transform.SetParent(transform);
            lightGo.transform.localPosition = Vector3.up * 1.0f;
            var light = lightGo.AddComponent<Light>();
            light.type      = LightType.Point;
            light.color     = new Color(1f, 0.84f, 0.1f);
            light.intensity = 5f;
            light.range     = 3f;

            const float duration = 0.8f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                light.intensity = Mathf.Lerp(5f, 0f, elapsed / duration);
                yield return null;
            }
            Destroy(lightGo);
        }

        private System.Collections.IEnumerator WhiteLightFlashRoutine()
        {
            var go = new GameObject("LevelUpWhiteFlash");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.up * 1.0f;
            var light = go.AddComponent<Light>();
            light.type      = LightType.Point;
            light.color     = Color.white;
            light.range     = 5f;

            const float half = 0.25f;
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                light.intensity = Mathf.Lerp(0f, 3f, t / half);
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                light.intensity = Mathf.Lerp(3f, 0f, t / half);
                yield return null;
            }
            Destroy(go);
        }

        private void ResetPerkStats()
        {
            _perks.Clear();
            _activeTagsCount.Clear();
            FireRateMul           = 1f;
            RangeMul              = 1f;
            DamageMul             = 1f;
            MoveSpeedMul          = 1f;
            CoinGainMul           = 1f;
            XpMul                 = 1f;
            CritChance            = 0f;
            CritMul               = 2f;
            CritStaggerMs         = 0;
            MultiShot             = 0;
            PierceCount           = 0;
            Lifesteal             = 0f;
            WaveRegen             = 0f;
            MoveAttackPierceBonus = 0;
            CastleHPMaxMul        = 1f;
            CoinRewardMul         = 1f;
            TowerCostMul          = 1f;
            TowerFireRateAuraMul  = 1f;
            TowerAuraRange        = 0f;
            AoeOnNthProjectile    = 0;
            Fireball              = false;
            FireballRadius        = 2f;
            FireballDmgMul        = 0.7f;
            Ricochet              = false;
            RicochetBounces       = 3;
            RicochetDecay         = 0.8f;
            Lightning             = false;
            LightningTargets      = 1;
            LightningDmgMul       = 0.6f;
            PierceExplode         = false;
            PierceExplodeRadius   = 1.5f;
            PierceExplodeDmgMul   = 0.5f;
            Glaciation            = false;
            Combustion            = false;
            Pyromancie            = false;
            ForteressePerk        = false;
            MursPierre            = false;
            CristalGlace          = false;
            ChannelingPill        = false;
            FirstTowerFree        = false;
            FirstTowerFreeUsed    = false;
            _fireTrails.Clear();
        }
    }
}
