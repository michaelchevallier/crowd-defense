#nullable enable
using System;
using System.Collections.Generic;
using CrowdDefense.Common;

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

        public event Action? OnPerksChanged;

        protected override void OnAwakeSingleton()
        {
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
        }

        public void StartRun(string firstLevelId)
        {
            ActivePerks.Clear();
            LevelChain.Clear();
            LevelChain.Add(firstLevelId);
            CurrentLevelIndex = 0;
            AccumulatedScore = 0;
        }

        public void AddPerk(string perkId)
        {
            ActivePerks.Add(perkId);
            SaveSystem.AppendRunPerk(perkId);
            OnPerksChanged?.Invoke();
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
    }
}
