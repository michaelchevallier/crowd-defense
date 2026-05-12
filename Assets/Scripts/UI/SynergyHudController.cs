#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;
using static CrowdDefense.UI.Toast;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SynergyHudController : UIControllerBase
    {
        private VisualElement? _container;

        private void Start()
        {
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            _container = Root?.Q<VisualElement>("synergy-badges");

            if (Synergies.Instance != null)
            {
                Synergies.Instance.OnSynergyChanged  += Redraw;
                Synergies.Instance.OnSynergyActivated += OnPairActivated;
                Redraw();
            }
        }

        private void OnDestroy()
        {
            if (Synergies.Instance != null)
            {
                Synergies.Instance.OnSynergyChanged  -= Redraw;
                Synergies.Instance.OnSynergyActivated -= OnPairActivated;
            }
        }

        private static void OnPairActivated(SynergyActivatedInfo info)
        {
            string label = L.Get($"synergy.{info.Label}", "Synergies");
            if (string.IsNullOrEmpty(label)) label = info.Label;
            Toast.Show(label, string.Empty, 2500, null, ToastType.Synergy);
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
