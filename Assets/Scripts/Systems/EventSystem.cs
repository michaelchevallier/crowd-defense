#nullable enable
using System;
using System.Collections;
using UnityEngine;
using CrowdDefense.Common;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.Systems
{
    // Port de pickRandomEvent() + apply callbacks V5 events.js.
    // Rolls un event aleatoire apres chaque vague ou niveau (30% chance).
    // Souscrit a LevelRunner.OnLevelComplete et WaveManager.OnWaveCleared.
    // Attend 1 frame pour laisser PerkPickerController fermer en premier.
    public class EventSystem : MonoSingleton<EventSystem>
    {
        private const float EventChance = 0.30f;
        private const int ExclusionWindow = 3;

        protected override void OnAwakeSingleton() { }

        private void Start()
        {
            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnLevelComplete += OnLevelComplete;
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveCleared += OnWaveCleared;
        }

        protected override void OnDestroySingleton()
        {
            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnLevelComplete -= OnLevelComplete;
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveCleared -= OnWaveCleared;
        }

        private void OnLevelComplete() => StartCoroutine(RollAfterDelay());

        private void OnWaveCleared(int waveIdx) => StartCoroutine(RollAfterDelay());

        private IEnumerator RollAfterDelay()
        {
            yield return null; // laisse PerkPickerController reagir en premier

            if (UnityEngine.Random.value > EventChance) yield break;

            var reg = EventRegistry.Get();
            if (reg == null) yield break;

            var ctx = RunContext.Instance;
            var evt = reg.PickRandom(ctx?.RecentEventIds);
            if (evt == null) yield break;

            if (ctx != null)
            {
                ctx.RecentEventIds.Add(evt.Id);
                if (ctx.RecentEventIds.Count > ExclusionWindow)
                    ctx.RecentEventIds.RemoveAt(0);
            }

            var overlay = UnityEngine.Object.FindFirstObjectByType<UI.EventChoiceOverlay>();
            overlay?.Show(evt, OnChoicePicked);
        }

        private void OnChoicePicked(Data.EventDef evt, int choiceIndex)
        {
            if (choiceIndex < 0 || choiceIndex >= evt.Choices.Length) return;
            ApplyAction(evt.Choices[choiceIndex].applyAction);
        }

        // Action parser: "coins+N", "castleHP-N", "pendingPerk=tag", "skipNextPerk",
        //                "modifier=<id>" (lookup ModifierRegistry, apply + save in RunContext)
        //                "A|B" (pipe-separator: apply both), "random50:A:B" (50/50 choice)
        internal static void ApplyAction(string action, int depth = 0)
        {
            if (string.IsNullOrEmpty(action)) return;

            // Recursion guard: prevent infinite loops
            if (depth > 8)
            {
                Debug.LogError("[EventSystem] Action recursion > 8 in: " + action);
                return;
            }

            // Pipe-separator: apply each action sequentially
            if (action.Contains("|"))
            {
                var tokens = action.Split('|');
                foreach (var token in tokens)
                {
                    ApplyAction(token.Trim(), depth + 1);
                }
                return;
            }

            // random50: prefix: 50/50 choice between two options
            if (action.StartsWith("random50:", StringComparison.OrdinalIgnoreCase))
            {
                var suffix = action.Substring("random50:".Length);
                var options = suffix.Split(':');
                if (options.Length >= 2)
                {
                    var chosen = UnityEngine.Random.value < 0.5f ? options[0] : options[1];
                    ApplyAction(chosen.Trim(), depth + 1);
                }
                return;
            }

            var ctx = RunContext.Instance;
            var castle = Castle.Instance;

            if (action == "skipNextPerk")         { if (ctx != null) ctx.SkipNextPerk = true; return; }
            if (action == "bonusNextPerk")         { if (ctx != null) ctx.BonusNextPerk = true; return; }
            if (action == "skipNextStarterTower")  { if (ctx != null) ctx.SkipNextStarterTower = true; return; }
            if (action == "cursedNextCombat")      { if (ctx != null) ctx.CursedNextCombat = true; return; }
            if (action == "revealNextRowTypes")    { if (ctx != null) ctx.RevealNextRowTypes = true; return; }

            if (action.StartsWith("pendingPerk=", StringComparison.OrdinalIgnoreCase))
            {
                if (ctx != null) ctx.PendingPerkOffer = action.Substring("pendingPerk=".Length);
                return;
            }

            // RunModifierDef : "modifier=<id>" — lookup + apply applyAction + save in RunContext
            if (action.StartsWith("modifier=", StringComparison.OrdinalIgnoreCase))
            {
                var modId = action.Substring("modifier=".Length);
                var modReg = ModifierRegistry.Get();
                var mod = modReg?.FindById(modId);
                if (mod != null)
                {
                    ctx?.AddModifier(mod.Id);
                    if (!string.IsNullOrEmpty(mod.ApplyAction))
                        ApplyAction(mod.ApplyAction, depth + 1);
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                else Debug.LogWarning($"[EventSystem] modifier '{modId}' non trouvé dans ModifierRegistry");
#endif
                return;
            }

            if (action.StartsWith("coins+", StringComparison.Ordinal) &&
                int.TryParse(action.Substring(6), out int coinsAdd))
            { Economy.Instance?.AddGold(coinsAdd); return; }

            if (action.StartsWith("coins-", StringComparison.Ordinal) &&
                int.TryParse(action.Substring(6), out int coinsSub))
            { Economy.Instance?.TrySpend(coinsSub); return; }

            if (action.StartsWith("castleHP+", StringComparison.Ordinal) &&
                int.TryParse(action.Substring(9), out int hpAdd))
            { castle?.Regen(hpAdd); return; }

            if (action.StartsWith("castleHP-", StringComparison.Ordinal) &&
                int.TryParse(action.Substring(9), out int hpSub))
            { castle?.TakeDamage(hpSub); return; }

            if (action.StartsWith("castleHPMax+", StringComparison.Ordinal) &&
                int.TryParse(action.Substring(12), out int hpMaxAdd))
            { castle?.GrantBonusHP(hpMaxAdd); return; }

            if (action.StartsWith("heroXP+", StringComparison.Ordinal) &&
                int.TryParse(action.Substring(7), out int xpAdd))
            { LevelRunner.Instance?.Hero?.GainXp(xpAdd); return; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[EventSystem] ApplyAction: action non reconnue '{action}'");
#endif
        }
    }
}
