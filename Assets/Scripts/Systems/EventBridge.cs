#nullable enable
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using UnityEngine;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Translates existing C# Action events on game systems into typed EventManager.Publish calls.
    /// Avoids touching entity CORE files (Tower, Enemy, Hero, Castle) directly.
    /// Subscribe in Start (after singletons awake) and unsubscribe in OnDestroy.
    /// </summary>
    public class EventBridge : MonoBehaviour
    {
        private void Start()
        {
            if (PlacementController.Instance != null)
            {
                PlacementController.Instance.OnTowerPlaced    += HandleTowerPlaced;
                PlacementController.Instance.OnTowerUpgraded  += HandleTowerUpgraded;
                PlacementController.Instance.OnTowerSold      += HandleTowerSold;
            }

            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStart        += HandleWaveStart;
                WaveManager.Instance.OnWaveCleared      += HandleWaveCleared;
                WaveManager.Instance.OnAllWavesCompleted += HandleAllWavesCompleted;
            }

            if (LevelRunner.Instance != null)
            {
                LevelRunner.Instance.OnLevelComplete += HandleLevelComplete;
                LevelRunner.Instance.OnLevelLost     += HandleLevelLost;
            }

            if (Economy.Instance != null)
                Economy.Instance.OnGoldChanged += HandleGoldChanged;

            if (Synergies.Instance != null)
                Synergies.Instance.OnSynergyActivated += HandleSynergyActivated;

            if (PerkSystem.Instance != null)
                PerkSystem.Instance.OnPerkApplied += HandlePerkApplied;

            SubscribeToHeroLevelUp();
        }

        private void SubscribeToHeroLevelUp()
        {
            var hero = LevelRunner.Instance?.Hero;
            if (hero != null)
                hero.OnLevelUp += HandleHeroLevelUp;
        }

        private void OnDestroy()
        {
            if (PlacementController.Instance != null)
            {
                PlacementController.Instance.OnTowerPlaced    -= HandleTowerPlaced;
                PlacementController.Instance.OnTowerUpgraded  -= HandleTowerUpgraded;
                PlacementController.Instance.OnTowerSold      -= HandleTowerSold;
            }

            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveStart        -= HandleWaveStart;
                WaveManager.Instance.OnWaveCleared      -= HandleWaveCleared;
                WaveManager.Instance.OnAllWavesCompleted -= HandleAllWavesCompleted;
            }

            if (LevelRunner.Instance != null)
            {
                LevelRunner.Instance.OnLevelComplete -= HandleLevelComplete;
                LevelRunner.Instance.OnLevelLost     -= HandleLevelLost;
            }

            if (Economy.Instance != null)
                Economy.Instance.OnGoldChanged -= HandleGoldChanged;

            if (Synergies.Instance != null)
                Synergies.Instance.OnSynergyActivated -= HandleSynergyActivated;

            if (PerkSystem.Instance != null)
                PerkSystem.Instance.OnPerkApplied -= HandlePerkApplied;

            var hero = LevelRunner.Instance?.Hero;
            if (hero != null)
                hero.OnLevelUp -= HandleHeroLevelUp;
        }

        // ── Tower ────────────────────────────────────────────────────────────

        private void HandleTowerPlaced(Tower t) =>
            EventManager.Instance?.Publish(new TowerPlacedEvent(t));

        private void HandleTowerUpgraded(Tower t, int newLevel) =>
            EventManager.Instance?.Publish(new TowerUpgradedEvent(t, newLevel));

        private void HandleTowerSold(Tower t, int refund) =>
            EventManager.Instance?.Publish(new TowerSoldEvent(t, refund));

        // ── Wave ─────────────────────────────────────────────────────────────

        private void HandleWaveStart(int idx) =>
            EventManager.Instance?.Publish(new WaveStartedEvent(idx + 1));

        private void HandleWaveCleared(int idx) =>
            EventManager.Instance?.Publish(new WaveCompletedEvent(idx + 1));

        private void HandleAllWavesCompleted() =>
            EventManager.Instance?.Publish(new AllWavesCompletedEvent());

        // ── Level ────────────────────────────────────────────────────────────

        private void HandleLevelComplete()
        {
            int levelIndex = LevelRunner.Instance?.CurrentLevel?.Level ?? 0;
            EventManager.Instance?.Publish(new LevelEndedEvent(levelIndex, true));
        }

        private void HandleLevelLost()
        {
            int levelIndex = LevelRunner.Instance?.CurrentLevel?.Level ?? 0;
            EventManager.Instance?.Publish(new LevelEndedEvent(levelIndex, false));
        }

        // ── Economy ──────────────────────────────────────────────────────────

        private int _lastGold;

        private void HandleGoldChanged(int newGold)
        {
            int delta = newGold - _lastGold;
            EventManager.Instance?.Publish(new CoinsChangedEvent(_lastGold, newGold, delta));
            _lastGold = newGold;
        }

        // ── Synergies ────────────────────────────────────────────────────────

        private void HandleSynergyActivated(SynergyActivatedInfo info) =>
            EventManager.Instance?.Publish(new SynergyPairActivatedEvent(info.FromType, info.ToType, info.Label));

        // ── Perks ────────────────────────────────────────────────────────────

        private void HandlePerkApplied(Hero hero, PerkDef def)
        {
            EventManager.Instance?.Publish(new PerkPickedEvent(def.id, "hero"));
            string label = !string.IsNullOrEmpty(def.displayName) ? def.displayName : def.id;
            WaveHistoryLog.Instance?.Log("perk", $"Perk : {label}");
        }

        // ── Hero ─────────────────────────────────────────────────────────────

        private void HandleHeroLevelUp(int level, int xp, int xpToNext) =>
            EventManager.Instance?.Publish(new HeroLevelUpEvent(level, xp, xpToNext));
    }
}
