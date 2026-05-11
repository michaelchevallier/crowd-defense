#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SynergyHudController : MonoBehaviour
    {
        private VisualElement? _container;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _container = root.Q<VisualElement>("synergy-badges");

            if (Synergies.Instance != null)
            {
                Synergies.Instance.OnSynergyChanged += Redraw;
                Redraw();
            }
        }

        private void OnDestroy()
        {
            if (Synergies.Instance != null)
                Synergies.Instance.OnSynergyChanged -= Redraw;
        }

        private void Redraw()
        {
            if (_container == null) return;
            _container.Clear();

            if (Synergies.Instance == null) return;
            var badges = Synergies.Instance.ActiveBadges;
            for (int i = 0; i < badges.Count; i++)
                _container.Add(BuildBadge(badges[i]));
        }

        private static VisualElement BuildBadge(SynergyBadge badge)
        {
            string towerId = badge.TowerId;
            string icon    = L.Get($"syn.icon.{towerId}");
            string name    = L.Get($"tower.{towerId}.name", "Towers");

            var pill = new VisualElement();
            pill.AddToClassList("synergy-badge");

            var iconLabel = new Label(icon);
            iconLabel.AddToClassList("synergy-badge-icon");
            pill.Add(iconLabel);

            var textLabel = new Label($"x{badge.Count} {name}");
            textLabel.AddToClassList("synergy-badge-label");
            pill.Add(textLabel);

            return pill;
        }
    }
}
