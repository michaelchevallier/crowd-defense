#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    public class DoctrineController : UIControllerBase
    {
        private VisualElement? _doctrineRoot;
        private Label? _titleLabel;
        private Label? _activeLabel;
        private VisualElement? _listContainer;
        private Label? _gemsLabel;
        private Button? _closeBtn;

        private readonly List<VisualElement> _cards = new();

        private void Start()
        {
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            if (Root == null) return;

            _doctrineRoot  = Root.Q<VisualElement>("doctrine-root");
            _titleLabel    = Root.Q<Label>("doctrine-title");
            _activeLabel   = Root.Q<Label>("doctrine-active-label");
            _listContainer = Root.Q<VisualElement>("doctrine-list");
            _gemsLabel     = Root.Q<Label>("doctrine-gems-label");
            _closeBtn      = Root.Q<Button>("doctrine-close-btn");

            _closeBtn?.RegisterCallback<ClickEvent>(_ => Hide());

            ApplyLocalizedTexts();
            L.OnLocaleChanged += ApplyLocalizedTexts;

            if (DoctrineSystem.Instance != null)
                DoctrineSystem.Instance.OnDoctrineChanged += HandleDoctrineChanged;
        }

        private void OnDestroy()
        {
            L.OnLocaleChanged -= ApplyLocalizedTexts;
            if (DoctrineSystem.Instance != null)
                DoctrineSystem.Instance.OnDoctrineChanged -= HandleDoctrineChanged;
        }

        private void HandleDoctrineChanged(DoctrineDef? _) => RefreshCards();

        private void ApplyLocalizedTexts()
        {
            if (_titleLabel != null) _titleLabel.text = L.Get("doctrine.title");
            if (_closeBtn != null)   _closeBtn.text   = L.Get("doctrine.close");
            RefreshCards();
        }

        public void Show()
        {
            RefreshCards();
            _doctrineRoot?.RemoveFromClassList("hidden");
        }

        public void Hide()
        {
            _doctrineRoot?.AddToClassList("hidden");
        }

        private void RefreshCards()
        {
            if (_listContainer == null) return;
            _listContainer.Clear();
            _cards.Clear();

            var system = DoctrineSystem.Instance;
            var active = system?.ActiveDoctrine;
            int gems   = system?.GetGems() ?? 0;

            if (_activeLabel != null)
                _activeLabel.text = active != null
                    ? L.Get("doctrine.active_label", active.iconEmoji, active.displayName)
                    : L.Get("doctrine.none_active");

            if (_gemsLabel != null)
                _gemsLabel.text = L.Get("doctrine.gems_label", gems);

            var reg = DoctrineRegistry.Get();
            foreach (var def in reg.All)
            {
                if (def == null) continue;
                var card = BuildCard(def, active, gems);
                _cards.Add(card);
                _listContainer.Add(card);
            }
        }

        private VisualElement BuildCard(DoctrineDef def, DoctrineDef? active, int gems)
        {
            bool isActive  = active != null && active.id == def.id;
            bool canAfford = gems >= def.gemCost;

            var card = new VisualElement();
            card.AddToClassList("doctrine-card");
            if (isActive)        card.AddToClassList("doctrine-card--active");
            else if (!canAfford) card.AddToClassList("doctrine-card--locked");

            var emoji = new Label { text = def.iconEmoji };
            emoji.AddToClassList("doctrine-card-emoji");
            card.Add(emoji);

            var body = new VisualElement();
            body.AddToClassList("doctrine-card-body");

            var nameLabel = new Label { text = def.displayName };
            nameLabel.AddToClassList("doctrine-card-name");
            body.Add(nameLabel);

            var descLabel = new Label { text = def.description };
            descLabel.AddToClassList("doctrine-card-desc");
            body.Add(descLabel);

            card.Add(body);

            var action = new VisualElement();
            action.AddToClassList("doctrine-card-action");

            var btn = new Button();
            if (isActive)
            {
                btn.text = L.Get("doctrine.btn_active");
                btn.AddToClassList("doctrine-btn-active");
            }
            else
            {
                btn.text = L.Get("doctrine.btn_select");
                btn.AddToClassList("doctrine-btn-select");
                if (!canAfford) btn.SetEnabled(false);
                var capturedId = def.id;
                btn.RegisterCallback<ClickEvent>(_ => TryActivate(capturedId));
            }
            action.Add(btn);

            if (!isActive)
            {
                var costLbl = new Label { text = L.Get("doctrine.cost_label", def.gemCost) };
                costLbl.AddToClassList("doctrine-cost-label");
                action.Add(costLbl);
            }

            card.Add(action);
            return card;
        }

        private void TryActivate(string doctrineId)
        {
            if (DoctrineSystem.Instance == null) return;
            DoctrineSystem.Instance.TryActivate(doctrineId, out _);
            RefreshCards();
        }
    }
}
