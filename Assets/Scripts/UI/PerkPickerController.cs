#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Attached to same GameObject as UIDocument for PerkPicker.uxml.
    // LevelRunner.OnLevelComplete hooks ShowAndWait(onDone).
    [RequireComponent(typeof(UIDocument))]
    public class PerkPickerController : MonoBehaviour
    {
        private static readonly string[] PlaceholderIds =
        {
            "range", "fire_rate", "pierce", "lifesteal", "move_speed",
            "dmg_bonus", "gold_bonus", "castle_regen"
        };

        private VisualElement? root;
        private Label? titleLabel;
        private Label? subtitleLabel;
        private VisualElement? cardsRow;
        private Action? onSelectionDone;

        private void Start()
        {
            var doc = GetComponent<UIDocument>();
            root = doc.rootVisualElement.Q<VisualElement>("perk-picker-root");
            titleLabel = doc.rootVisualElement.Q<Label>("perk-title");
            subtitleLabel = doc.rootVisualElement.Q<Label>("perk-subtitle");
            cardsRow = doc.rootVisualElement.Q<VisualElement>("perk-cards-row");

            if (root != null) root.AddToClassList("hidden");

            L.OnLocaleChanged += RefreshLabels;
            RefreshLabels();
        }

        private void OnDestroy()
        {
            L.OnLocaleChanged -= RefreshLabels;
        }

        private void RefreshLabels()
        {
            if (titleLabel != null) titleLabel.text = L.Get("perk.pick_title");
            if (subtitleLabel != null) subtitleLabel.text = L.Get("perk.pick_subtitle");
        }

        // Called by LevelRunner (or HudController) on victory to show the picker.
        // onDone fires after a card is selected.
        public void ShowAndWait(Action onDone)
        {
            onSelectionDone = onDone;
            var offers = BuildOffers();
            BuildCards(offers);
            if (root != null) root.RemoveFromClassList("hidden");
            Time.timeScale = 0f;
        }

        private List<PerkDef?> BuildOffers()
        {
            var reg = PerkRegistry.Get();
            if (reg != null)
            {
                var defs = reg.GetRandom(3);
                if (defs.Count > 0)
                {
                    var result = new List<PerkDef?>();
                    for (int i = 0; i < defs.Count; i++) result.Add(defs[i]);
                    return result;
                }
            }
            // Fallback: placeholder ids
            var pool = new List<string>(PlaceholderIds);
            var fallback = new List<PerkDef?>();
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                string id = pool[idx];
                pool.RemoveAt(idx);
                // Create a transient PerkDef (not a SO asset) for display only
                var def = ScriptableObject.CreateInstance<PerkDef>();
                def.id = id;
                def.displayName = L.Get("perk.placeholder_name");
                def.description = L.Get("perk.placeholder_desc");
                def.rarity = PerkRarity.Common;
                fallback.Add(def);
            }
            return fallback;
        }

        private void BuildCards(List<PerkDef?> offers)
        {
            if (cardsRow == null) return;
            cardsRow.Clear();
            for (int i = 0; i < offers.Count; i++)
            {
                var def = offers[i];
                var card = CreateCard(def);
                cardsRow.Add(card);
            }
        }

        private VisualElement CreateCard(PerkDef? def)
        {
            var card = new VisualElement();
            card.AddToClassList("perk-card");
            if (def != null)
                card.AddToClassList(RarityClass(def.rarity));

            // Icon placeholder
            var icon = new VisualElement();
            icon.AddToClassList("perk-card-icon");
            if (def?.icon != null)
                icon.style.backgroundImage = new StyleBackground(def.icon);
            card.Add(icon);

            // Rarity label
            var rarityLabel = new Label();
            rarityLabel.AddToClassList("perk-card-rarity");
            rarityLabel.text = def != null ? L.Get(RarityKey(def.rarity)) : "";
            card.Add(rarityLabel);

            // Name
            var nameLabel = new Label();
            nameLabel.AddToClassList("perk-card-name");
            nameLabel.text = def?.displayName ?? L.Get("perk.placeholder_name");
            card.Add(nameLabel);

            // Description
            var descLabel = new Label();
            descLabel.AddToClassList("perk-card-desc");
            descLabel.text = def?.description ?? L.Get("perk.placeholder_desc");
            card.Add(descLabel);

            string perkId = def?.id ?? "";
            card.RegisterCallback<ClickEvent>(_ => SelectPerk(perkId));
            return card;
        }

        private void SelectPerk(string perkId)
        {
            if (root != null) root.AddToClassList("hidden");
            Time.timeScale = 1f;

            if (!string.IsNullOrEmpty(perkId))
            {
                var ctx = RunContext.Instance;
                ctx?.AddPerk(perkId);
            }

            onSelectionDone?.Invoke();
            onSelectionDone = null;
        }

        private static string RarityClass(PerkRarity rarity) => rarity switch
        {
            PerkRarity.Common => "rarity-common",
            PerkRarity.Rare => "rarity-rare",
            PerkRarity.Epic => "rarity-epic",
            PerkRarity.Legendary => "rarity-legendary",
            _ => "rarity-common"
        };

        private static string RarityKey(PerkRarity rarity) => rarity switch
        {
            PerkRarity.Common => "perk.rarity_common",
            PerkRarity.Rare => "perk.rarity_rare",
            PerkRarity.Epic => "perk.rarity_epic",
            PerkRarity.Legendary => "perk.rarity_legendary",
            _ => "perk.rarity_common"
        };
    }
}
