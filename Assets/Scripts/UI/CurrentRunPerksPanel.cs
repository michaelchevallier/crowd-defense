#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Left-middle sidebar showing all active perks of the current run:
    // icon (emoji) | name | stack count badge (when > 1).
    // Attaches to the same GameObject as HudController / UIDocument.
    [RequireComponent(typeof(UIDocument))]
    public class CurrentRunPerksPanel : MonoBehaviour
    {
        private VisualElement? _panel;
        private ScrollView?    _scroll;

        // perk id -> row element
        private readonly Dictionary<string, VisualElement> _rows = new();

        private Hero?         _hero;
        private PerkRegistry? _registry;

        // Called by HudController once the UIDocument root is ready and hero is known.
        public void Init(VisualElement hudRoot, Hero hero)
        {
            _hero     = hero;
            _registry = PerkRegistry.Load();

            BuildPanel(hudRoot);
            SubscribeEvents();
            RebuildAll();
        }

        private void OnDestroy() => UnsubscribeEvents();

        // ── Panel construction ────────────────────────────────────────────────

        private void BuildPanel(VisualElement hudRoot)
        {
            _panel = new VisualElement { name = "current-run-perks-panel" };
            _panel.style.position        = Position.Absolute;
            _panel.style.left            = 8f;
            _panel.style.top             = new StyleLength(new Length(50f, LengthUnit.Percent));
            _panel.style.translate       = new StyleTranslate(new Translate(0f, new Length(-50f, LengthUnit.Percent)));
            _panel.style.width           = 200f;
            _panel.style.maxHeight       = 400f;
            _panel.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
            _panel.style.borderTopLeftRadius     = 8f;
            _panel.style.borderTopRightRadius    = 8f;
            _panel.style.borderBottomLeftRadius  = 8f;
            _panel.style.borderBottomRightRadius = 8f;
            _panel.style.paddingTop    = 6f;
            _panel.style.paddingBottom = 6f;
            _panel.style.paddingLeft   = 6f;
            _panel.style.paddingRight  = 6f;

            var title = new Label { text = "Perks actifs" };
            title.style.color     = new Color(1f, 0.88f, 0.4f);
            title.style.fontSize  = 11f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 4f;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            _panel.Add(title);

            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.style.flexGrow = 1f;
            _scroll.style.maxHeight = 370f;
            _scroll.verticalScrollerVisibility   = ScrollerVisibility.Auto;
            _scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _panel.Add(_scroll);

            hudRoot.Add(_panel);
        }

        // ── Event wiring ──────────────────────────────────────────────────────

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
            if (hero != _hero) return;
            UpsertRow(def.id, def.iconEmoji, def.nameKey);
        }

        private void OnSetBonusActivated(Hero hero, PerkSetBonusDef bonus)
        {
            if (hero != _hero) return;
            string key = "set_" + bonus.nameKey;
            UpsertRow(key, "★", bonus.nameKey);
        }

        // ── Row helpers ───────────────────────────────────────────────────────

        private void UpsertRow(string id, string emoji, string name)
        {
            if (_scroll == null) return;

            if (_rows.TryGetValue(id, out var existing))
            {
                int stacks = CountStacks(id);
                var countLbl = existing.Q<Label>("perk-row-count");
                if (countLbl != null)
                {
                    countLbl.text = stacks > 1 ? stacks.ToString() : "";
                    SetVisible(countLbl, stacks > 1);
                }
                return;
            }

            var row = BuildRow(emoji, name, 1);
            _scroll.Add(row);
            _rows[id] = row;
        }

        private static VisualElement BuildRow(string emoji, string name, int stacks)
        {
            var row = new VisualElement();
            row.style.flexDirection  = FlexDirection.Row;
            row.style.alignItems     = Align.Center;
            row.style.marginBottom   = 3f;

            var iconLbl = new Label { text = emoji };
            iconLbl.style.fontSize  = 14f;
            iconLbl.style.minWidth  = 22f;
            iconLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            row.Add(iconLbl);

            var nameLbl = new Label { text = name };
            nameLbl.style.color     = Color.white;
            nameLbl.style.fontSize  = 11f;
            nameLbl.style.flexGrow  = 1f;
            nameLbl.style.unityTextAlign = TextAnchor.MiddleLeft;
            row.Add(nameLbl);

            var countLbl = new Label { name = "perk-row-count", text = stacks > 1 ? stacks.ToString() : "" };
            countLbl.style.color           = new Color(1f, 0.88f, 0.4f);
            countLbl.style.fontSize        = 10f;
            countLbl.style.minWidth        = 16f;
            countLbl.style.unityTextAlign  = TextAnchor.MiddleCenter;
            countLbl.style.backgroundColor = new Color(0f, 0f, 0f, 0.4f);
            countLbl.style.borderTopLeftRadius     = 4f;
            countLbl.style.borderTopRightRadius    = 4f;
            countLbl.style.borderBottomLeftRadius  = 4f;
            countLbl.style.borderBottomRightRadius = 4f;
            SetVisible(countLbl, stacks > 1);
            row.Add(countLbl);

            return row;
        }

        private void RebuildAll()
        {
            if (_scroll == null || _hero == null || _registry == null) return;
            _scroll.Clear();
            _rows.Clear();

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
                int stacks = stackCounts[id];
                var row = BuildRow(def.iconEmoji, def.nameKey, stacks);
                _scroll.Add(row);
                _rows[id] = row;
            }
        }

        private int CountStacks(string id)
        {
            if (_hero == null) return 0;
            int n = 0;
            foreach (var p in _hero.Perks)
                if (p == id) n++;
            return n;
        }

        private static void SetVisible(VisualElement el, bool visible)
        {
            if (visible) el.RemoveFromClassList("hidden");
            else el.AddToClassList("hidden");
        }
    }
}
