#nullable enable
using CrowdDefense.Common;
using UnityEngine;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Boss Rush mode: chains the 9 world boss levels (W*-8) back-to-back.
    /// Castle HP is carried over between fights (no economy reset between fights).
    /// Victory on the 9th boss awards 500 bonus gems and returns to WorldMap.
    /// </summary>
    public class BossRushMode : MonoSingleton<BossRushMode>
    {
        public bool IsActive         { get; private set; }
        public int  CurrentBossIndex { get; private set; }
        public int  InitialCastleHp  { get; private set; }
        public int  CarriedCastleHp  { get; private set; }

        private static readonly string[] BossLevelIds =
        {
            "world1-8", "world2-8", "world3-8", "world4-8", "world5-8",
            "world6-8", "world7-8", "world8-8", "world9-8"
        };

        public void StartBossRush()
        {
            IsActive         = true;
            CurrentBossIndex = 0;
            CarriedCastleHp  = 0;
            LoadCurrentBoss();
        }

        public void OnBossDefeated(int remainingHp)
        {
            CarriedCastleHp = remainingHp;
            CurrentBossIndex++;

            if (CurrentBossIndex >= BossLevelIds.Length)
            {
                OnBossRushComplete();
                return;
            }

            LoadCurrentBoss();
        }

        public void Abandon()
        {
            IsActive = false;
            LevelLoader.GoToWorldMap();
        }

        private void LoadCurrentBoss()
        {
            var levelId = BossLevelIds[CurrentBossIndex];
            LevelLoader.LoadLevel(levelId);
        }

        private void OnBossRushComplete()
        {
            IsActive = false;
            SaveSystem.AddGems(500);
            LevelLoader.GoToWorldMap();
        }
    }
}
