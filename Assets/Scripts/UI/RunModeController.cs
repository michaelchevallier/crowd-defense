#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrowdDefense.UI
{
    public enum RunModeState { Idle, MapView, NodeCombat, NodeBoss, Victory, Defeat }

    // Central state machine for the roguelike Run Mode.
    // Survives scene transitions via DontDestroyOnLoad.
    // Coordinates: map generation, node dispatch, victory/defeat/abandon flows.
    public class RunModeController : MonoSingleton<RunModeController>
    {
        // Gems awarded on full victory / consolation on defeat / abandon.
        private const int GemsVictory = 30;
        private const int GemsDefeat  =  5;
        private const int GemsAbandon =  5;

        // Gold bonus per completed combat/boss node (accumulated in RunState.runGoldEarned).
        private const int GoldCombatReward = 30;
        private const int GoldBossReward   = 50;

        // Max act index for a full run (3 acts in V4).
        private const int MaxActs = 3;

        public RunModeState CurrentState { get; private set; } = RunModeState.Idle;

        // Node type being processed (used after returning from Main scene).
        private RunMapNodeType _activeNodeType;

        public event Action<RunModeState>? OnStateChanged;

        protected override void OnAwakeSingleton()
        {
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            base.OnDestroy();
        }

        // ── Scene lifecycle hook ─────────────────────────────────────────────

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Main")
                StartCoroutine(HookLevelRunnerNextFrame());
        }

        private IEnumerator HookLevelRunnerNextFrame()
        {
            yield return null;
            var runner = LevelRunner.Instance;
            if (runner == null) yield break;
            // Only subscribe when a run is active so normal campaign is unaffected.
            if (!IsRunActive()) yield break;
            runner.OnLevelComplete += OnRunNodeComplete;
            runner.OnLevelLost     += OnRunNodeLost;
        }

        // ── Public API ───────────────────────────────────────────────────────

        public bool IsRunActive() => RunMap.Instance?.HasActiveMap() ?? false;

        // Entry point: start a new run from the given school.
        // schoolId: "feu" | "givre" | "maconnerie"
        public void StartRun(string schoolId)
        {
            var runMap = RunMap.Instance;
            if (runMap == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[RunModeController] RunMap singleton not found.");
#endif
                return;
            }

            int seed = (int)(DateTime.UtcNow.Ticks % int.MaxValue);
            runMap.Generate(1, seed);

            var rs = SaveSystem.GetRunState();
            rs.schoolId        = schoolId;
            rs.heroLevel       = 1;
            rs.heroXP          = 0;
            rs.heroPerks       = new List<string>();
            rs.runKills        = 0;
            rs.runGoldEarned   = 0;
            rs.runWavesCleared = 0;
            rs.runStreak       = 0;
            rs.runPlaytime     = 0f;
            rs.runTowersPlaced = 0;
            rs.runPerksAcquired = 0;
            SaveSystem.SetRunState(rs);

            SetState(RunModeState.MapView);
            LevelLoader.GoToRunMap();
        }

        // Convenience overload for MagicSchool enum.
        public void StartRun(MagicSchool school) => StartRun(SchoolToId(school));

        // Called by RunMapController when the player clicks an available node.
        public void OnNodeClick(RunMapNode node)
        {
            if (!IsRunActive()) return;
            _activeNodeType = node.type;

            switch (node.type)
            {
                case RunMapNodeType.Combat:
                case RunMapNodeType.Elite:
                    EnterCombatNode(node);
                    break;
                case RunMapNodeType.Boss:
                    EnterBossNode(node);
                    break;
                case RunMapNodeType.Shop:
                    EnterShopNode(node);
                    break;
                case RunMapNodeType.Rest:
                    EnterRestNode(node);
                    break;
                case RunMapNodeType.Mystery:
                    EnterMysteryNode(node);
                    break;
            }
        }

        // Abandon the active run (pause menu / run-map close button).
        public void Abandon()
        {
            if (!IsRunActive()) return;
            SaveSystem.AddGems(GemsAbandon);
            ClearRun();
            Toast.Show("Run abandonnee", $"+{GemsAbandon} gemmes de consolation", 3000);
            LevelLoader.GoToMenu();
        }

        // ── Node handlers ────────────────────────────────────────────────────

        private void EnterCombatNode(RunMapNode node)
        {
            if (string.IsNullOrEmpty(node.combatLevelId))
            {
                RunMap.Instance?.MoveTo(node.id);
                return;
            }
            SetState(RunModeState.NodeCombat);
            RunMap.Instance?.MoveTo(node.id);
            LevelLoader.LoadLevel(node.combatLevelId);
        }

        private void EnterBossNode(RunMapNode node)
        {
            if (string.IsNullOrEmpty(node.bossId))
            {
                RunMap.Instance?.MoveTo(node.id);
                return;
            }
            SetState(RunModeState.NodeBoss);
            RunMap.Instance?.MoveTo(node.id);
            LevelLoader.LoadLevel(node.bossId);
        }

        private void EnterShopNode(RunMapNode node)
        {
            RunMap.Instance?.MoveTo(node.id);
            var shop = FindAnyObjectByType<ShopController>();
            if (shop != null)
            {
                shop.Show();
            }
            else
            {
                const int FallbackGold = 20;
                var rs = SaveSystem.GetRunState();
                rs.runGoldEarned += FallbackGold;
                SaveSystem.SetRunState(rs);
                Toast.Show("Boutique fermee", $"+{FallbackGold}c de dedommagment", 2500);
            }
        }

        private void EnterRestNode(RunMapNode node)
        {
            RunMap.Instance?.MoveTo(node.id);
            // Heal note: actual HP restore on next level-load is handled by LevelRunner
            // reading a pending-heal flag; for now we toast the player.
            Toast.Show("Repos", "Chateau restaure au prochain combat (+30% PV)", 3000);
        }

        private void EnterMysteryNode(RunMapNode node)
        {
            RunMap.Instance?.MoveTo(node.id);
            var reg = EventRegistry.Get();
            var evt = reg?.PickRandom();
            if (evt == null)
            {
                Toast.Show("Mystere", "Rien ne se passe...", 2000);
                return;
            }

            var overlay = FindAnyObjectByType<EventChoiceOverlay>();
            if (overlay != null)
            {
                overlay.Show(evt, (pickedEvt, choiceIdx) => ApplyMysteryChoice(pickedEvt, choiceIdx));
            }
            else
            {
                // Fallback: auto-apply first choice action.
                ApplyMysteryChoice(evt, 0);
            }
        }

        private static void ApplyMysteryChoice(EventDef evt, int choiceIdx)
        {
            if (choiceIdx >= 0 && choiceIdx < evt.Choices.Length)
                EventSystem.ApplyAction(evt.Choices[choiceIdx].applyAction);
            Toast.Show(evt.Title, "Choix applique.", 2500);
        }

        // ── Post-combat callbacks (Main scene) ───────────────────────────────

        private void OnRunNodeComplete()
        {
            var runner = LevelRunner.Instance;
            if (runner == null) return;

            runner.OnLevelComplete -= OnRunNodeComplete;
            runner.OnLevelLost     -= OnRunNodeLost;

            if (runner.Hero != null)
                RunContext.Instance?.SnapshotHero(runner.Hero);

            bool isBoss = _activeNodeType == RunMapNodeType.Boss
                       || CurrentState == RunModeState.NodeBoss;
            int goldBonus = isBoss ? GoldBossReward : GoldCombatReward;

            var rs = SaveSystem.GetRunState();
            rs.runGoldEarned += goldBonus;
            rs.runKills      += runner.KillsThisLevel;
            SaveSystem.SetRunState(rs);

            var runMap = RunMap.Instance;
            if (runMap != null && runMap.IsComplete())
            {
                var mapState = runMap.State;
                if (mapState != null && mapState.worldId < MaxActs)
                {
                    runMap.Generate(mapState.worldId + 1, mapState.seed + mapState.worldId);
                    SetState(RunModeState.MapView);
                    Toast.Show($"Acte {mapState.worldId} termine !", "Prochain acte...", 3000);
                    LevelLoader.GoToRunMap();
                }
                else
                {
                    HandleVictory();
                }
            }
            else
            {
                SetState(RunModeState.MapView);
                LevelLoader.GoToRunMap();
            }
        }

        private void OnRunNodeLost()
        {
            var runner = LevelRunner.Instance;
            if (runner != null)
            {
                runner.OnLevelComplete -= OnRunNodeComplete;
                runner.OnLevelLost     -= OnRunNodeLost;
            }
            HandleDefeat();
        }

        // ── Terminal states ──────────────────────────────────────────────────

        private void HandleVictory()
        {
            SetState(RunModeState.Victory);
            SaveSystem.AddGems(GemsVictory);
            Toast.Show("VICTOIRE !", $"+{GemsVictory} gemmes ! Run complete.", 5000, null, ToastType.Success);
            ClearRun();
            LevelLoader.GoToMenu();
        }

        private void HandleDefeat()
        {
            SetState(RunModeState.Defeat);
            SaveSystem.AddGems(GemsDefeat);
            Toast.Show("Defaite", $"+{GemsDefeat} gemmes de consolation.", 4000, null, ToastType.Warning);
            ClearRun();
            LevelLoader.GoToWorldMap();
        }

        private void ClearRun()
        {
            RunMap.Instance?.ClearMap();
            SaveSystem.ClearRunState();
            SetState(RunModeState.Idle);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void SetState(RunModeState next)
        {
            if (CurrentState == next) return;
            CurrentState = next;
            OnStateChanged?.Invoke(next);
        }

        private static string SchoolToId(MagicSchool school) => school switch
        {
            MagicSchool.Fire      => "feu",
            MagicSchool.Frost     => "givre",
            MagicSchool.Stonework => "maconnerie",
            _                     => "feu",
        };
    }
}
