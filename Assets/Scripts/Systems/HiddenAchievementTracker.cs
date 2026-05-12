#nullable enable
using CrowdDefense.Common;
using CrowdDefense.Data;
using UnityEngine;

namespace CrowdDefense.Systems
{
    // Tracks conditions for the 5 hidden achievements and fires Achievements.Unlock
    // or Achievements.TrackEvent at the appropriate moment.
    //
    // Event keys match the .asset eventKey fields:
    //   pacifist_win   – EventPredicate, fired once on victory with exactly 1 tower active
    //   speedrun_win   – EventPredicate, fired once on victory in <60 s
    //   gold_snapshot  – Counter(1000), sampled every time gold changes
    //   boss_killed    – Counter(5),    fired by BossDefeatedEvent
    //   bankrupt_win   – EventPredicate, fired once on victory with 0 gold
    [DefaultExecutionOrder(10)]
    public class HiddenAchievementTracker : MonoBehaviour
    {
        private int   _activeTowers;
        private float _levelStartTime;

        private void OnEnable()
        {
            var em = EventManager.Instance;
            if (em != null)
            {
                em.Subscribe<TowerPlacedEvent>(OnTowerPlaced);
                em.Subscribe<TowerSoldEvent>(OnTowerSold);
                em.Subscribe<CoinsChangedEvent>(OnCoinsChanged);
                em.Subscribe<BossDefeatedEvent>(OnBossDefeated);
            }

            LevelEvents.OnLevelStart += OnLevelStart;

            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnLevelComplete += OnLevelComplete;
        }

        private void OnDisable()
        {
            var em = EventManager.Instance;
            if (em != null)
            {
                em.Unsubscribe<TowerPlacedEvent>(OnTowerPlaced);
                em.Unsubscribe<TowerSoldEvent>(OnTowerSold);
                em.Unsubscribe<CoinsChangedEvent>(OnCoinsChanged);
                em.Unsubscribe<BossDefeatedEvent>(OnBossDefeated);
            }

            LevelEvents.OnLevelStart -= OnLevelStart;

            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnLevelComplete -= OnLevelComplete;
        }

        private void OnLevelStart(LevelData _, Bounds __)
        {
            _activeTowers    = 0;
            _levelStartTime  = Time.time;
        }

        private void OnTowerPlaced(TowerPlacedEvent _) => _activeTowers++;
        private void OnTowerSold(TowerSoldEvent _)     => _activeTowers = Mathf.Max(0, _activeTowers - 1);

        private static void OnCoinsChanged(CoinsChangedEvent evt)
        {
            if (evt.Current >= 1000)
                Achievements.Instance?.TrackEvent("gold_snapshot", 0);

            // Ensure counter reaches threshold in one shot when condition is met.
            // TrackEvent with delta=0 won't accumulate; use absolute set via counter trick:
            // Reset to 0 first if needed, then add threshold.
            CheckHoarder(evt.Current);
        }

        private static void CheckHoarder(int current)
        {
            if (current < 1000) return;
            var ach = Achievements.Instance;
            if (ach == null) return;
            if (ach.IsUnlocked("hidden_hoarder")) return;
            // Force counter to threshold so TrackEvent triggers unlock.
            int existing = ach.GetEventCount("gold_snapshot");
            if (existing < 1000)
                ach.TrackEvent("gold_snapshot", 1000 - existing);
        }

        private static void OnBossDefeated(BossDefeatedEvent _) =>
            Achievements.Instance?.TrackEvent("boss_killed", 1);

        private void OnLevelComplete()
        {
            float elapsed  = Time.time - _levelStartTime;
            int   gold     = Economy.Instance?.Gold ?? 0;
            var   ach      = Achievements.Instance;
            if (ach == null) return;

            if (_activeTowers == 1)
                ach.Unlock("hidden_pacifist");

            if (elapsed < 60f)
                ach.Unlock("hidden_speedrun");

            if (gold == 0)
                ach.Unlock("hidden_bankrupt");
        }
    }
}
