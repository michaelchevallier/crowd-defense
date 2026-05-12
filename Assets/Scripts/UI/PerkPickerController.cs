#nullable enable
using System;
using System.Collections;
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
        // Delay between each card slide-in (stagger)
        private const float CardRevealStagger = 0.12f;

        private static readonly string[] PlaceholderIds =
        {
            "range", "fire_rate", "pierce", "lifesteal", "move_speed",
            "dmg_bonus", "gold_bonus", "castle_regen"
        };

        private VisualElement? root;
        private Label? titleLabel;
        private Label? subtitleLabel;
        private VisualElement? cardsRow;
        private Button? rerollButton;
        private Action? onSelectionDone;

        // Tracks current offers so reroll can re-roll them
        private List<PerkDef?> currentOffers = new();
        private int heroLevel = 1;

        private void Start()
        {
            var doc = GetComponent<UIDocument>();
            root         = doc.rootVisualElement.Q<VisualElement>("perk-picker-root");
            titleLabel   = doc.rootVisualElement.Q<Label>("perk-title");
            subtitleLabel = doc.rootVisualElement.Q<Label>("perk-subtitle");
            cardsRow     = doc.rootVisualElement.Q<VisualElement>("perk-cards-row");
            rerollButton = doc.rootVisualElement.Q<Button>("reroll-button");

            if (root != null) root.AddToClassList("hidden");

            if (rerollButton != null)
                rerollButton.RegisterCallback<ClickEvent>(_ => DoReroll());

            L.OnLocaleChanged += RefreshLabels;
            RefreshLabels();

            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnLevelComplete += HandleLevelComplete;
        }

        private void OnDestroy()
        {
            L.OnLocaleChanged -= RefreshLabels;
            if (LevelRunner.Instance != null)
                LevelRunner.Instance.OnLevelComplete -= HandleLevelComplete;
        }

        private void RefreshLabels()
        {
            if (titleLabel != null)    titleLabel.text    = L.Get("perk.pick_title");
            if (subtitleLabel != null) subtitleLabel.text = L.Get("perk.pick_subtitle");
            if (rerollButton != null)  rerollButton.text  = L.Get("perk.reroll");
        }

        private void HandleLevelComplete() =>
            ShowAndWait(() => LevelLoader.GoToWorldMap());

        // Called by LevelRunner (or HudController) on victory to show the picker.
        // onDone fires after a card is selected.
        public void ShowAndWait(Action onDone)
        {
            onSelectionDone = onDone;
            heroLevel = LevelRunner.Instance?.Hero?.Level ?? 1;

            var offers = BuildOffers();
            currentOffers = offers;
            BuildCards(offers, animate: true);

            SetRerollVisible(HasLuckyPerk());

            if (root != null) root.RemoveFromClassList("hidden");
            Time.timeScale = 0f;
        }

        private void DoReroll()
        {
            var offers = BuildOffers();
            currentOffers = offers;
            BuildCards(offers, animate: true);
        }

        private bool HasLuckyPerk()
        {
            var ctx = RunContext.Instance;
            if (ctx == null) return false;
            return ctx.ActivePerks.Contains("lucky");
        }

        private void SetRerollVisible(bool visible)
        {
            if (rerollButton == null) return;
            if (visible) rerollButton.RemoveFromClassList("hidden");
            else         rerollButton.AddToClassList("hidden");
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
                var def = ScriptableObject.CreateInstance<PerkDef>();
                def.id          = id;
                def.displayName = L.Get("perk.placeholder_name");
                def.description = L.Get("perk.placeholder_desc");
                def.rarity      = PerkRarity.Common;
                fallback.Add(def);
            }
            return fallback;
        }

        private void BuildCards(List<PerkDef?> offers, bool animate)
        {
            if (cardsRow == null) return;
            cardsRow.Clear();
            var cards = new List<VisualElement>();
            for (int i = 0; i < offers.Count; i++)
            {
                var def  = offers[i];
                bool locked = def != null && def.unlockLevel > heroLevel;
                var card = CreateCard(def, locked);
                cardsRow.Add(card);
                cards.Add(card);
            }

            if (animate)
                StartCoroutine(AnimateCardsEntrance(cards));
        }

        private IEnumerator AnimateCardsEntrance(List<VisualElement> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                card.AddToClassList("card-hidden");
                // Set initial position: offscreen bottom (120% of own height via translate Y)
                card.style.translate = new StyleTranslate(new Translate(0, new Length(120f, LengthUnit.Percent)));
            }

            for (int i = 0; i < cards.Count; i++)
            {
                StartCoroutine(AnimateCardEnter(cards[i], i));
            }
            yield break;
        }

        private IEnumerator AnimateCardEnter(VisualElement card, int idx)
        {
            yield return new WaitForSecondsRealtime(idx * 0.1f);
            card.RemoveFromClassList("card-hidden");
            card.AddToClassList("card-visible");
            card.style.translate = new StyleTranslate(new Translate(0, new Length(120f, LengthUnit.Percent)));
            float t = 0f;
            const float duration = 0.3f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 3f);
                card.style.translate = new StyleTranslate(new Translate(0, new Length(120f * (1f - k), LengthUnit.Percent)));
                yield return null;
            }
            card.style.translate = new StyleTranslate(new Translate(0, 0));
        }

        private VisualElement CreateCard(PerkDef? def, bool locked)
        {
            var card = new VisualElement();
            card.AddToClassList("perk-card");
            if (def != null)
                card.AddToClassList(RarityClass(def.rarity));
            if (locked)
                card.AddToClassList("locked");

            // Icon
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

            // Lock badge (shown over locked cards)
            if (locked && def != null)
            {
                var lockBadge = new VisualElement();
                lockBadge.AddToClassList("perk-card-lock-badge");
                var lockText = new Label();
                lockText.AddToClassList("perk-card-lock-text");
                lockText.text = $"Lv{def.unlockLevel}";
                lockBadge.Add(lockText);
                card.Add(lockBadge);
            }

            string perkId = def?.id ?? "";
            if (!locked)
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
            PerkRarity.Common    => "rarity-common",
            PerkRarity.Uncommon  => "rarity-uncommon",
            PerkRarity.Rare      => "rarity-rare",
            PerkRarity.Epic      => "rarity-epic",
            PerkRarity.Legendary => "rarity-legendary",
            _ => "rarity-common"
        };

        private static string RarityKey(PerkRarity rarity) => rarity switch
        {
            PerkRarity.Common    => "perk.rarity_common",
            PerkRarity.Uncommon  => "perk.rarity_uncommon",
            PerkRarity.Rare      => "perk.rarity_rare",
            PerkRarity.Epic      => "perk.rarity_epic",
            PerkRarity.Legendary => "perk.rarity_legendary",
            _ => "perk.rarity_common"
        };
    }
}
