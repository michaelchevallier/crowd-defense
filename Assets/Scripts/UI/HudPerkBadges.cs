#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // HUD row showing active perk badges + set bonus progress (X/3 per tag).
    // Attaches to the same GameObject as HudController / UIDocument.
    [RequireComponent(typeof(UIDocument))]
    public class HudPerkBadges : MonoBehaviour
    {
        private VisualElement? _row;
        private VisualElement? _setProgressRow;

        // badge id -> (badge element, stack count label)
        private readonly Dictionary<string, (VisualElement badge, Label count)> _badges = new();

        private Hero? _hero;
        private PerkRegistry? _registry;

        public void Init(VisualElement hudRoot, Hero hero)
        {
            _hero = hero;
            _registry = PerkRegistry.Load();
            _row = hudRoot.Q<VisualElement>("perk-badges-row");
            _setProgressRow = hudRoot.Q<VisualElement>("perk-set-progress-row");

            if (_row == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[HudPerkBadges] #perk-badges-row not found in HUD.uxml");
#endif
                return;
            }

            SubscribeEvents();
            RebuildAll();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (PerkSystem.Instance == null) return;
            PerkSystem.Instance.OnPerkApplied       += OnPerkApplied;
            PerkSystem.Instance.OnSetBonusActivated += OnSetBonusActivated;
        }

        private void UnsubscribeEvents()
        {
            if (PerkSystem.Instance == null) return;
            PerkSystem.Instance.OnPerkApplied       -= OnPerkApplied;
            PerkSystem.Instance.OnSetBonusActivated -= OnSetBonusActivated;
        }

        private void OnPerkApplied(Hero hero, PerkDef def)
        {
            if (hero != _hero || _row == null) return;
            UpsertBadge(def);
            RefreshSetProgress();
        }

        private void OnSetBonusActivated(Hero hero, PerkSetBonusDef bonus)
        {
            if (hero != _hero || _row == null) return;
            UpsertSetBonusBadge(bonus);
            RefreshSetProgress();
        }

        private void UpsertBadge(PerkDef def)
        {
            if (_row == null) return;

            if (_badges.TryGetValue(def.id, out var existing))
            {
                int stacks = 0;
                if (_hero != null)
                    foreach (var id in _hero.Perks)
                        if (id == def.id) stacks++;
                existing.count.text = stacks > 1 ? stacks.ToString() : "";
                SetVisible(existing.count, stacks > 1);
                return;
            }

            var badge = BuildBadge(def.iconEmoji, def.nameKey, def.descKey);
            var countLabel = badge.Q<Label>("perk-badge-count");
            _row.Add(badge);
            _badges[def.id] = (badge, countLabel);
        }

        private void UpsertSetBonusBadge(PerkSetBonusDef bonus)
        {
            if (_row == null) return;
            string key = "set_" + bonus.nameKey;
            if (_badges.ContainsKey(key)) return;
            var badge = BuildBadge("★", bonus.nameKey, "");
            badge.AddToClassList("perk-badge-setbonus");
            var countLabel = badge.Q<Label>("perk-badge-count");
            _row.Add(badge);
            _badges[key] = (badge, countLabel);
        }

        private static VisualElement BuildBadge(string emoji, string tooltipName, string tooltipDesc)
        {
            var badge = new VisualElement();
            badge.AddToClassList("perk-badge");

            var icon = new Label { text = emoji };
            icon.AddToClassList("perk-badge-icon");
            badge.Add(icon);

            var count = new Label { text = "" };
            count.name = "perk-badge-count";
            count.AddToClassList("perk-badge-count");
            SetVisible(count, false);
            badge.Add(count);

            if (!string.IsNullOrEmpty(tooltipName))
            {
                string tip = string.IsNullOrEmpty(tooltipDesc) ? tooltipName : $"{tooltipName}\n{tooltipDesc}";
                badge.tooltip = tip;
            }

            return badge;
        }

        private void RefreshSetProgress()
        {
            if (_setProgressRow == null || _hero == null || _registry == null) return;

            var counts = new Dictionary<PerkTag, int>();
            foreach (var id in _hero.Perks)
            {
                var def = _registry.Get(id);
                if (def == null || def.tag == PerkTag.None) continue;
                counts.TryGetValue(def.tag, out int c);
                counts[def.tag] = c + 1;
            }

            _setProgressRow.Clear();

            foreach (var kv in counts)
            {
                if (kv.Value <= 0) continue;
                var bonus = _registry.GetBonus(kv.Key);
                int threshold = bonus != null ? bonus.threshold : 3;

                var chip = new VisualElement();
                chip.AddToClassList("perk-set-chip");

                var iconLbl = new Label { text = TagEmoji(kv.Key) };
                iconLbl.AddToClassList("perk-set-chip-icon");
                chip.Add(iconLbl);

                var progressLbl = new Label { text = $"{kv.Value}/{threshold}" };
                progressLbl.AddToClassList("perk-set-chip-count");
                if (kv.Value >= threshold)
                    progressLbl.AddToClassList("perk-set-chip-count--complete");
                chip.Add(progressLbl);

                _setProgressRow.Add(chip);
            }
        }

        private void RebuildAll()
        {
            if (_row == null || _hero == null || _registry == null) return;
            _row.Clear();
            _badges.Clear();

            var stackCounts = new Dictionary<string, int>();
            foreach (var id in _hero.Perks)
            {
                stackCounts.TryGetValue(id, out int c);
                stackCounts[id] = c + 1;
            }

            var seen = new HashSet<string>();
            foreach (var id in _hero.Perks)
            {
                if (!seen.Add(id)) continue;
                var def = _registry.Get(id);
                if (def == null) continue;
                var badge = BuildBadge(def.iconEmoji, def.nameKey, def.descKey);
                var countLabel = badge.Q<Label>("perk-badge-count");
                int stacks = stackCounts[id];
                countLabel.text = stacks > 1 ? stacks.ToString() : "";
                SetVisible(countLabel, stacks > 1);
                _row.Add(badge);
                _badges[id] = (badge, countLabel);
            }

            RefreshSetProgress();
        }

        private static string TagEmoji(PerkTag tag) => tag switch
        {
            PerkTag.Foudre => "⚡",
            PerkTag.Sang   => "🩸",
            PerkTag.Pierre => "🪨",
            PerkTag.Feu    => "🔥",
            PerkTag.Vide   => "🌀",
            PerkTag.Or     => "🪙",
            _              => ""
        };

        private static void SetVisible(VisualElement el, bool visible)
        {
            if (visible) el.RemoveFromClassList("hidden");
            else el.AddToClassList("hidden");
        }
    }
}
