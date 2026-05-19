#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using CrowdDefense.Common;
using CrowdDefense.Data;
using UnityEngine;

namespace CrowdDefense.Systems
{
    // Per-run state singleton. Survives level-to-level transitions via DontDestroyOnLoad.
    // Owns active perk list, level chain, and accumulated score for the current run.
    public class RunContext : MonoSingleton<RunContext>
    {
        public List<string> ActivePerks { get; } = new();
        public List<string> LevelChain { get; } = new();
        public int CurrentLevelIndex { get; private set; }
        public int AccumulatedScore { get; private set; }

        // Modifier state (V5 runner fields)
        public List<string> ActiveModifierIds { get; } = new();
        public List<string> RecentEventIds    { get; } = new();

        public float CoinMul               { get; set; } = 1f;
        public float TowerRangeMul         { get; set; } = 1f;
        public float TowerFireRateMul      { get; set; } = 1f;
        public float ProjectileDeviation   { get; set; } = 0f;
        public bool  SkipNextPerk          { get; set; } = false;
        public bool  BonusNextPerk         { get; set; } = false;
        public bool  SkipNextStarterTower  { get; set; } = false;
        public bool  CursedNextCombat      { get; set; } = false;
        public bool  RevealNextRowTypes    { get; set; } = false;
        public string PendingPerkOffer     { get; set; } = "";

        public event Action? OnPerksChanged;
        public event Action<string>? OnModifierAdded;

        protected override void OnAwakeSingleton()
        {
            if (transform.parent != null) transform.SetParent(null);
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
        }

        public void StartRun(string firstLevelId)
        {
            ActivePerks.Clear();
            LevelChain.Clear();
            LevelChain.Add(firstLevelId);
            CurrentLevelIndex = 0;
            AccumulatedScore = 0;
            ResetModifierState();
        }

        private void ResetModifierState()
        {
            ActiveModifierIds.Clear();
            RecentEventIds.Clear();
            CoinMul = 1f;
            TowerRangeMul = 1f;
            TowerFireRateMul = 1f;
            ProjectileDeviation = 0f;
            SkipNextPerk = false;
            BonusNextPerk = false;
            SkipNextStarterTower = false;
            CursedNextCombat = false;
            RevealNextRowTypes = false;
            PendingPerkOffer = "";
        }

        public void AddModifier(string modifierId)
        {
            if (!ActiveModifierIds.Contains(modifierId))
                ActiveModifierIds.Add(modifierId);
            OnModifierAdded?.Invoke(modifierId);
        }

        public void AddPerk(string perkId)
        {
            ActivePerks.Add(perkId);
            SaveSystem.AppendRunPerk(perkId);
            OnPerksChanged?.Invoke();
            Achievements.Instance?.TrackEvent("perk_collected", 1);
        }

        public void AdvanceLevel(string nextLevelId)
        {
            LevelChain.Add(nextLevelId);
            CurrentLevelIndex++;
        }

        public void AddScore(int delta) => AccumulatedScore += delta;

        public string? NextLevelId => CurrentLevelIndex + 1 < LevelChain.Count
            ? LevelChain[CurrentLevelIndex + 1]
            : null;

        // Persist hero level + xp into RunState so the next level load restores them.
        public void SnapshotHero(CrowdDefense.Entities.Hero hero)
        {
            var rs = SaveSystem.GetRunState();
            rs.heroLevel = hero.Level;
            rs.heroXP    = hero.Xp;
            SaveSystem.SetRunState(rs);
        }

        // Restore hero stats + perks from RunState. Delegates to Hero.ApplyRunContext.
        public void ApplyToHero(CrowdDefense.Entities.Hero hero)
        {
            var rs = SaveSystem.GetRunState();
            hero.ApplyRunContext(rs.heroPerks, rs.heroLevel, rs.heroXP);
        }

        // Returns the HeroType matching the PlayerPrefs selection, or null if no prefs key is set.
        // Caller should fall back to the Inspector-assigned heroType field when this returns null.
        public static HeroType? GetSelectedHero()
        {
            var assetKey = PlayerPrefs.GetString(UI.HeroPickScreen.PrefsKey, "");
            if (string.IsNullOrEmpty(assetKey)) return null;

            var heroes = Resources.LoadAll<HeroType>("Heroes");
            return heroes.FirstOrDefault(h => h.AssetKey == assetKey);
        }
    }
}
