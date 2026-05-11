#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Mounted on a UIDocument GameObject that holds Shop.uxml.
    // TODO: WorldMapController should call Show() after level victory, or add a shop button on the map.
    [RequireComponent(typeof(UIDocument))]
    public class ShopController : MonoBehaviour
    {
        private VisualElement? _overlay;
        private Label? _gemsValue;
        private Button? _closeBtn;
        private Button? _tab1;
        private Button? _tab2;
        private Button? _tab3;
        private Label? _tierHint;
        private VisualElement? _cardsGrid;
        private Label? _resetHint;

        private int _activeTier = 1;

        private void Awake()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _overlay   = root.Q<VisualElement>("shop-overlay");
            _gemsValue = root.Q<Label>("shop-gems-value");
            _closeBtn  = root.Q<Button>("shop-close-btn");
            _tab1      = root.Q<Button>("shop-tab-1");
            _tab2      = root.Q<Button>("shop-tab-2");
            _tab3      = root.Q<Button>("shop-tab-3");
            _tierHint  = root.Q<Label>("shop-tier-hint");
            _cardsGrid = root.Q<VisualElement>("shop-cards-grid");
            _resetHint = root.Q<Label>("shop-reset-hint");

            _closeBtn?.RegisterCallback<ClickEvent>(_ => Hide());
            _tab1?.RegisterCallback<ClickEvent>(_ => SwitchTier(1));
            _tab2?.RegisterCallback<ClickEvent>(_ => SwitchTier(2));
            _tab3?.RegisterCallback<ClickEvent>(_ => SwitchTier(3));
        }

        public void Show()
        {
            Refresh();
            _overlay?.RemoveFromClassList("hidden");
        }

        public void Hide()
        {
            _overlay?.AddToClassList("hidden");
        }

        public bool IsVisible => _overlay != null && !_overlay.ClassListContains("hidden");

        private void SwitchTier(int tier)
        {
            _activeTier = tier;
            UpdateTabStyles();
            RebuildCards();
        }

        private void UpdateTabStyles()
        {
            SetTabActive(_tab1, _activeTier == 1);
            SetTabActive(_tab2, _activeTier == 2);
            SetTabActive(_tab3, _activeTier == 3);
        }

        private static void SetTabActive(Button? btn, bool active)
        {
            if (btn == null) return;
            if (active) btn.AddToClassList("shop-tab-active");
            else btn.RemoveFromClassList("shop-tab-active");
        }

        private void Refresh()
        {
            if (_gemsValue != null)
                _gemsValue.text = SaveSystem.GetGems().ToString();
            UpdateTabStyles();
            RebuildCards();
        }

        private void RebuildCards()
        {
            if (_cardsGrid == null) return;
            _cardsGrid.Clear();

            var registry = MetaUpgradeRegistry.Get();
            if (registry == null) return;

            int worldsCleared = SaveSystem.WorldsCleared();
            bool tierUnlocked = registry.IsTierUnlocked(_activeTier, worldsCleared);

            if (_tierHint != null)
            {
                if (!tierUnlocked)
                {
                    int required = _activeTier == 2 ? registry.tier2UnlockWorldsCleared : registry.tier3UnlockWorldsCleared;
                    _tierHint.text = $"Debloque apres avoir termine le Monde {required}";
                    _tierHint.RemoveFromClassList("hidden");
                }
                else
                {
                    _tierHint.AddToClassList("hidden");
                }
            }

            if (_resetHint != null)
                _resetHint.text = $"Reset = {registry.resetCostGems} gemmes (rembourse 50%)";

            foreach (var def in registry.All)
            {
                if (def.tier != _activeTier) continue;
                _cardsGrid.Add(BuildCard(def, tierUnlocked, registry.resetCostGems));
            }
        }

        private VisualElement BuildCard(MetaUpgradeDef def, bool tierUnlocked, int resetCost)
        {
            int gems  = SaveSystem.GetGems();
            int lvl   = SaveSystem.GetMetaUpgradeLevel(def.id);
            bool maxed = lvl >= def.maxLevel;
            int nextCost = maxed ? 0 : def.CostForLevel(lvl);
            bool canAfford = !maxed && gems >= nextCost;
            bool canBuy = tierUnlocked && !maxed && canAfford;
            bool canReset = tierUnlocked && lvl > 0 && gems >= resetCost;

            var card = new VisualElement();
            card.AddToClassList("upgrade-card");
            if (maxed) card.AddToClassList("maxed");
            if (!tierUnlocked) card.AddToClassList("tier-locked");

            // Header row
            var header = new VisualElement();
            header.AddToClassList("card-header");

            var emoji = new Label(def.iconEmoji.Length > 0 ? def.iconEmoji : "?");
            emoji.AddToClassList("card-emoji");

            var nameLabel = new Label(def.displayName);
            nameLabel.AddToClassList("card-name");

            header.Add(emoji);
            header.Add(nameLabel);
            card.Add(header);

            // Description
            var desc = new Label(def.description);
            desc.AddToClassList("card-desc");
            card.Add(desc);

            // Effect row
            string currentEffect = lvl > 0 && lvl - 1 < def.perLevelLabels.Length ? def.perLevelLabels[lvl - 1] : "Inactif";
            string nextEffect = maxed ? "MAX" : (lvl < def.perLevelLabels.Length ? def.perLevelLabels[lvl] : "?");

            var effectRow = new VisualElement();
            effectRow.AddToClassList("card-effect-row");
            var cur = new Label($"Actuel: {currentEffect}");
            cur.AddToClassList("card-effect-current");
            var nxt = new Label($"Prochain: {nextEffect}");
            nxt.AddToClassList("card-effect-next");
            effectRow.Add(cur);
            effectRow.Add(nxt);
            card.Add(effectRow);

            // Pips
            var pips = new VisualElement();
            pips.AddToClassList("card-pips");
            for (int i = 0; i < def.maxLevel; i++)
            {
                var pip = new VisualElement();
                pip.AddToClassList("pip");
                if (i < lvl) pip.AddToClassList(maxed ? "pip-maxed" : "pip-filled");
                pips.Add(pip);
            }
            card.Add(pips);

            // Actions
            var actions = new VisualElement();
            actions.AddToClassList("card-actions");

            var upgradeBtn = new Button();
            upgradeBtn.AddToClassList("btn-upgrade");
            if (maxed)
            {
                upgradeBtn.text = "MAX";
                upgradeBtn.AddToClassList("maxed-label");
                upgradeBtn.SetEnabled(false);
            }
            else
            {
                upgradeBtn.text = $"+{nextCost} gemmes";
                upgradeBtn.SetEnabled(canBuy);
                upgradeBtn.RegisterCallback<ClickEvent>(_ => OnUpgradeClick(def, lvl));
            }

            var resetBtn = new Button();
            resetBtn.AddToClassList("btn-reset");
            resetBtn.text = "R";
            resetBtn.tooltip = $"Reset ({resetCost} gemmes)";
            resetBtn.SetEnabled(canReset);
            resetBtn.RegisterCallback<ClickEvent>(_ => OnResetClick(def, lvl, resetCost));

            actions.Add(upgradeBtn);
            actions.Add(resetBtn);
            card.Add(actions);

            return card;
        }

        private void OnUpgradeClick(MetaUpgradeDef def, int currentLevel)
        {
            int cost = def.CostForLevel(currentLevel);
            if (!SaveSystem.SpendGems(cost)) return;
            int newLevel = currentLevel + 1;
            SaveSystem.SetMetaUpgradeLevel(def.id, newLevel);
            MetaUpgradeSystem.Instance?.ComputeBonuses();
            Refresh();
        }

        private void OnResetClick(MetaUpgradeDef def, int currentLevel, int resetCost)
        {
            if (!SaveSystem.SpendGems(resetCost)) return;
            // Refund 50% of spent gems (costs are additive: sum of tiers 0..level-1)
            int refund = 0;
            for (int i = 0; i < currentLevel; i++)
                refund += def.CostForLevel(i);
            refund = Mathf.RoundToInt(refund * 0.5f);
            if (refund > 0) SaveSystem.AddGems(refund);
            SaveSystem.ResetMetaUpgrade(def.id);
            MetaUpgradeSystem.Instance?.ComputeBonuses();
            Refresh();
        }
    }
}
