#nullable enable
using System;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    public enum TutorialStep
    {
        NotStarted          = -1,
        Step1_PlaceTower    = 0,
        Step2_StartWave     = 1,
        Step3_KillEnemy     = 2,
        Step4_PlaceHero     = 3,
        Step5_CollectCoins  = 4,
        Step6_ChoosePerk    = 5,
        Done                = 6,
    }

    public class TutorialState : MonoSingleton<TutorialState>
    {
        private const string TutorialLevelId = "world1-1";

        public bool IsActive { get; private set; }
        public TutorialStep CurrentStep { get; private set; } = TutorialStep.NotStarted;
        public TutorialStepDef? CurrentStepDef { get; private set; }

        // Compat shims for existing callers (TutorialOverlayController old bindings)
        public bool IsTutorialActive => IsActive;
        public int CurrentPhase => (int)CurrentStep;

        public event Action<TutorialStep>? OnStepChanged;
        public event Action? OnTutorialCompleted;
        public event Action? OnTutorialSkipped;

        private TutorialRegistry? _registry;
        private int _coinsAtStep5Entry;
        private Vector3? _proximityTarget;
        private const float ProximityRadius = 3f;

        public static bool IsCompleted() => SaveSystem.IsTutorialCompleted();
        public static bool HasCompletedBefore => PlayerPrefs.GetInt("tutorial_done_v1", 0) > 0;

        protected override void OnAwakeSingleton()
        {
            if (IsCompleted())
            {
                IsActive = false;
                CurrentStep = TutorialStep.Done;
            }
        }

        private void Start()
        {
            if (IsCompleted()) return;

            var levelId = LevelRunner.Instance?.CurrentLevel?.Id;
            if (levelId != TutorialLevelId) return;

            if (HasCompletedBefore)
            {
                SetStep(TutorialStep.Done);
                return;
            }

            _registry = TutorialRegistry.Get();
            IsActive = true;
            EnterStep(TutorialStep.Step1_PlaceTower);
            SubscribeToGameEvents();
        }

        protected override void OnDestroySingleton()
        {
            UnsubscribeFromGameEvents();
        }

        // ── Public API ─────────────────────────────────────────────────────────────

        public void AdvanceStep()
        {
            if (!IsActive) return;
            var next = CurrentStep + 1;
            if (next >= TutorialStep.Done)
                Complete();
            else
                EnterStep(next);
        }

        // Legacy compat: TutorialOverlayController calls AdvancePhase()
        public void AdvancePhase() => AdvanceStep();

        public void SetStep(TutorialStep step)
        {
            IsActive = false;
            CurrentStep = step;
            SaveSystem.SetTutorialCompleted();
            PlayerPrefs.SetInt("tutorial_done_v1", 1);
            PlayerPrefs.Save();
        }

        public void SkipTutorial()
        {
            if (!IsActive) return;
            IsActive = false;
            CurrentStep = TutorialStep.Done;
            SaveSystem.SetTutorialCompleted();
            PlayerPrefs.SetInt("tutorial_done_v1", 1);
            PlayerPrefs.Save();
            UnsubscribeFromGameEvents();
            OnTutorialSkipped?.Invoke();
        }

        public void OnLevelLoaded(string levelId)
        {
            if (IsCompleted()) { IsActive = false; return; }
            if (levelId != TutorialLevelId) { IsActive = false; return; }
            _registry = TutorialRegistry.Get();
            IsActive = true;
            EnterStep(TutorialStep.Step1_PlaceTower);
        }

        // Wired externally when an enemy is killed (no generic event on WaveManager yet)
        public void NotifyEnemyKilled(Enemy enemy)
        {
            if (!IsActive || CurrentStep != TutorialStep.Step3_KillEnemy) return;
            AdvanceStep();
        }

        // Wired externally when the hero is placed on the grid
        public void NotifyHeroPlaced()
        {
            if (!IsActive || CurrentStep != TutorialStep.Step4_PlaceHero) return;
            AdvanceStep();
        }

        // Wired externally when a wave-end perk card is chosen
        public void NotifyPerkChosen()
        {
            if (!IsActive || CurrentStep != TutorialStep.Step6_ChoosePerk) return;
            AdvanceStep();
        }

        // World-space position the player must approach to auto-advance a Proximity-trigger step
        public void SetProximityTarget(Vector3 worldPos) => _proximityTarget = worldPos;
        public void ClearProximityTarget() => _proximityTarget = null;

        // ── Proximity auto-advance ────────────────────────────────────────────────

        private void Update()
        {
            if (!IsActive) return;
            var hero = Hero.Current;
            if (hero != null) CheckProximity(hero.transform.position);
        }

        private void CheckProximity(Vector3 playerPos)
        {
            if (!_proximityTarget.HasValue) return;
            if (CurrentStepDef?.advanceTrigger != TutorialAdvanceTrigger.Proximity) return;
            if (Vector3.Distance(playerPos, _proximityTarget.Value) < ProximityRadius)
            {
                _proximityTarget = null;
                AdvanceStep();
            }
        }

        // ── FSM ────────────────────────────────────────────────────────────────────

        private void EnterStep(TutorialStep step)
        {
            CurrentStep = step;
            CurrentStepDef = StepDef(step);
            if (step == TutorialStep.Step5_CollectCoins)
                _coinsAtStep5Entry = Economy.Instance?.Gold ?? 0;
            OnStepChanged?.Invoke(step);
        }

        private void Complete()
        {
            IsActive = false;
            CurrentStep = TutorialStep.Done;
            SaveSystem.SetTutorialCompleted();
            UnsubscribeFromGameEvents();
            OnTutorialCompleted?.Invoke();
        }

        private TutorialStepDef? StepDef(TutorialStep step)
        {
            if (_registry == null) return null;
            int idx = (int)step;
            var arr = _registry.Steps;
            return idx >= 0 && idx < arr.Length ? arr[idx] : null;
        }

        // ── Game-event auto-advance ────────────────────────────────────────────────

        private void OnTowerPlacedHandler(Tower _)
        {
            if (!IsActive || CurrentStep != TutorialStep.Step1_PlaceTower) return;
            AdvanceStep();
        }

        private void OnWaveStartHandler(int _)
        {
            if (!IsActive || CurrentStep != TutorialStep.Step2_StartWave) return;
            AdvanceStep();
        }

        private void OnGoldChangedHandler(int _)
        {
            if (!IsActive || CurrentStep != TutorialStep.Step5_CollectCoins) return;
            if (Economy.Instance != null && Economy.Instance.Gold > _coinsAtStep5Entry)
                AdvanceStep();
        }

        private void SubscribeToGameEvents()
        {
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnTowerPlaced += OnTowerPlacedHandler;
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveStart += OnWaveStartHandler;
            if (Economy.Instance != null)
                Economy.Instance.OnGoldChanged += OnGoldChangedHandler;
        }

        private void UnsubscribeFromGameEvents()
        {
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnTowerPlaced -= OnTowerPlacedHandler;
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveStart -= OnWaveStartHandler;
            if (Economy.Instance != null)
                Economy.Instance.OnGoldChanged -= OnGoldChangedHandler;
        }
    }
}
