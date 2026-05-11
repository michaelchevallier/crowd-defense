#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    /// <summary>
    /// UI controller for the Skin Picker panel.
    /// Can be opened from Settings or Shop via Show()/Hide().
    /// Displays skins grouped by SkinTargetType, marks locked/equipped state.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SkinPickerController : MonoBehaviour
    {
        private UIDocument? _doc;
        private VisualElement? _root;
        private VisualElement? _panel;
        private VisualElement? _tabBar;
        private ScrollView? _skinList;
        private Label? _titleLabel;
        private Button? _closeButton;

        private SkinTargetType _activeTab = SkinTargetType.Tower;

        private static readonly SkinTargetType[] Tabs =
        {
            SkinTargetType.Tower,
            SkinTargetType.Hero,
            SkinTargetType.Enemy,
        };

        private void Awake()
        {
            _doc  = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;

            _panel      = _root.Q<VisualElement>("skin-picker-panel");
            _tabBar     = _root.Q<VisualElement>("skin-tab-bar");
            _skinList   = _root.Q<ScrollView>("skin-list");
            _titleLabel = _root.Q<Label>("skin-title");
            _closeButton = _root.Q<Button>("skin-close-btn");

            _closeButton?.RegisterCallback<ClickEvent>(_ => Hide());

            BuildTabs();
            _panel?.AddToClassList("hidden");

            L.OnLocaleChanged += RefreshLocale;
        }

        private void OnDestroy()
        {
            L.OnLocaleChanged -= RefreshLocale;
        }

        public void Show()
        {
            _panel?.RemoveFromClassList("hidden");
            if (_titleLabel != null) _titleLabel.text = L.Get("skin.title");
            SelectTab(_activeTab);
        }

        public void Hide() => _panel?.AddToClassList("hidden");

        public bool IsVisible => _panel != null && !_panel.ClassListContains("hidden");

        private void BuildTabs()
        {
            if (_tabBar == null) return;
            _tabBar.Clear();
            foreach (var tab in Tabs)
            {
                var btn = new Button { name = "tab-" + tab };
                btn.text = TabLabel(tab);
                btn.AddToClassList("skin-tab-btn");
                var captured = tab;
                btn.RegisterCallback<ClickEvent>(_ => SelectTab(captured));
                _tabBar.Add(btn);
            }
        }

        private void SelectTab(SkinTargetType tab)
        {
            _activeTab = tab;

            if (_tabBar != null)
            {
                foreach (var child in _tabBar.Children())
                {
                    child.RemoveFromClassList("active");
                    if (child.name == "tab-" + tab)
                        child.AddToClassList("active");
                }
            }

            PopulateList(tab);
        }

        private void PopulateList(SkinTargetType tab)
        {
            if (_skinList == null) return;
            _skinList.Clear();

            var reg = SkinRegistry.Get();
            if (reg == null) return;

            var all = reg.All;
            for (int i = 0; i < all.Count; i++)
            {
                var skin = all[i];
                if (skin.TargetType != tab) continue;

                bool owned = skin.IsDefault || SaveSystem.IsSkinOwned(skin.Id);
                string equippedId = SaveSystem.GetEquippedSkin(tab.ToString(), skin.TargetId) ?? "";
                bool equipped = equippedId == skin.Id;

                var card = BuildCard(skin, owned, equipped);
                _skinList.Add(card);
            }
        }

        private VisualElement BuildCard(SkinDef skin, bool owned, bool equipped)
        {
            var card = new VisualElement();
            card.AddToClassList("skin-card");
            if (!owned) card.AddToClassList("skin-card-locked");
            if (equipped) card.AddToClassList("skin-card-equipped");

            if (skin.Icon != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList("skin-icon");
                icon.style.backgroundImage = new StyleBackground(skin.Icon);
                card.Add(icon);
            }

            var info = new VisualElement();
            info.AddToClassList("skin-info");

            var name = new Label(L.Get(skin.DisplayNameKey));
            name.AddToClassList("skin-name");
            info.Add(name);

            if (!string.IsNullOrEmpty(skin.DescriptionKey))
            {
                var desc = new Label(L.Get(skin.DescriptionKey));
                desc.AddToClassList("skin-desc");
                info.Add(desc);
            }

            card.Add(info);

            var btnArea = new VisualElement();
            btnArea.AddToClassList("skin-btn-area");

            if (equipped)
            {
                var lbl = new Label(L.Get("skin.equipped"));
                lbl.AddToClassList("skin-equipped-label");
                btnArea.Add(lbl);
            }
            else if (owned)
            {
                var btn = new Button();
                btn.text = L.Get("skin.equip_btn");
                btn.AddToClassList("skin-equip-btn");
                var captured = skin;
                btn.RegisterCallback<ClickEvent>(_ => OnEquip(captured));
                btnArea.Add(btn);
            }
            else
            {
                var lbl = new Label(L.Get("skin.locked"));
                lbl.AddToClassList("skin-locked-label");
                btnArea.Add(lbl);
            }

            card.Add(btnArea);
            return card;
        }

        private void OnEquip(SkinDef skin)
        {
            if (SkinSystem.Instance == null) return;
            bool equipped = SkinSystem.Instance.EquipSkin(skin.Id, skin.TargetType, skin.TargetId);
            if (!equipped) return;

            // Live-apply to active scene entities
            if (skin.TargetType == SkinTargetType.Hero)
            {
                var hero = LevelRunner.Instance?.Hero;
                if (hero != null) SkinSystem.Instance.ApplyToHero(hero);
            }
            else if (skin.TargetType == SkinTargetType.Tower)
            {
                var towers = PlacementController.Instance?.PlacedTowers;
                if (towers != null) SkinSystem.Instance.ApplyToTowers(towers);
            }

            PopulateList(_activeTab);
        }

        private void RefreshLocale()
        {
            if (_titleLabel != null) _titleLabel.text = L.Get("skin.title");
            if (_closeButton != null) _closeButton.text = L.Get("skin.close");
            BuildTabs();
            if (IsVisible) PopulateList(_activeTab);
        }

        private static string TabLabel(SkinTargetType t) => t switch
        {
            SkinTargetType.Tower  => L.Get("skin.category_vfx"),
            SkinTargetType.Hero   => L.Get("skin.category_hero"),
            SkinTargetType.Enemy  => L.Get("skin.category_castle"),
            _ => t.ToString(),
        };
    }
}
