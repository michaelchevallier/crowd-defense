#nullable enable
using System;
using UnityEngine;
using CrowdDefense.Common;

namespace CrowdDefense.Systems
{
    /// <summary>
    /// Persistent singleton tracking W1-1 tutorial progress.
    /// Active only when the current level is "world1-1".
    /// PlayerPrefs key "tutorial_skipped" persists across sessions.
    /// </summary>
    public class TutorialState : MonoSingleton<TutorialState>
    {
        private const string PrefKey = "tutorial_skipped";
        private const string TutorialLevelId = "world1-1";

        // Total phases defined in W1-1 spec (0..4 inclusive)
        public const int PhaseCount = 5;

        public bool IsTutorialActive { get; private set; }
        public int CurrentPhase { get; private set; }
        public bool PersistedSkippedTutorial => PlayerPrefs.GetInt(PrefKey, 0) == 1;

        public event Action<int>? OnPhaseChanged;
        public event Action? OnTutorialCompleted;
        public event Action? OnTutorialSkipped;

        protected override void OnAwakeSingleton()
        {
            // Don't activate tutorial if it was already skipped/completed
            if (PersistedSkippedTutorial)
            {
                IsTutorialActive = false;
                return;
            }

            // Defer level check to Start (LevelRunner not yet available in Awake)
        }

        private void Start()
        {
            var levelId = LevelRunner.Instance?.CurrentLevel?.Id;
            IsTutorialActive = levelId == TutorialLevelId && !PersistedSkippedTutorial;
            CurrentPhase = 0;

            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnStateChanged += OnGameStateChanged;
        }

        protected override void OnDestroySingleton()
        {
            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnStateChanged -= OnGameStateChanged;
        }

        public void AdvancePhase()
        {
            if (!IsTutorialActive) return;
            CurrentPhase++;
            if (CurrentPhase >= PhaseCount)
            {
                CompleteTutorial();
                return;
            }
            OnPhaseChanged?.Invoke(CurrentPhase);
        }

        public void SkipTutorial()
        {
            if (!IsTutorialActive) return;
            IsTutorialActive = false;
            PlayerPrefs.SetInt(PrefKey, 1);
            PlayerPrefs.Save();
            OnTutorialSkipped?.Invoke();
        }

        private void CompleteTutorial()
        {
            IsTutorialActive = false;
            PlayerPrefs.SetInt(PrefKey, 1);
            PlayerPrefs.Save();
            OnTutorialCompleted?.Invoke();
        }

        private void OnGameStateChanged(GameState state)
        {
            // Wave cleared = phase 4 (congrats) if we haven't reached it yet
            if (!IsTutorialActive) return;
            if (state == GameState.Victory && CurrentPhase < PhaseCount - 1)
                AdvancePhase();
        }

        // Called externally when a level is hot-reloaded (editor / restart) — resets to phase 0
        public void OnLevelLoaded(string levelId)
        {
            IsTutorialActive = levelId == TutorialLevelId && !PersistedSkippedTutorial;
            CurrentPhase = 0;
        }
    }
}
