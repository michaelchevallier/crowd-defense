#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SynergyHudPanel : MonoBehaviour
    {
        private VisualElement? _panel;
        private VisualElement? _list;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _panel = root.Q<VisualElement>("synergy-hud-panel");
            _list  = root.Q<VisualElement>("synergy-hud-list");
            StartCoroutine(PollLoop());
        }

        private IEnumerator PollLoop()
        {
            var wait = new WaitForSeconds(1f);
            while (true)
            {
                Redraw();
                yield return wait;
            }
        }

        private void Redraw()
        {
            if (_panel == null || _list == null) return;

            _list.Clear();

            var synergies = Synergies.Instance;
            if (synergies == null || synergies.ActiveBadges.Count == 0)
            {
                _panel.AddToClassList("hidden");
                return;
            }

            _panel.RemoveFromClassList("hidden");

            var badges = synergies.ActiveBadges;
            for (int i = 0; i < badges.Count; i++)
            {
                var badge = badges[i];
                string icon  = L.Get($"syn.icon.{badge.TowerId}");
                string name  = L.Get($"tower.{badge.TowerId}.name", "Towers");
                string label = string.IsNullOrEmpty(name) ? badge.TowerId : name;

                var row = new VisualElement();
                row.AddToClassList("synergy-hud-row");

                var lbl = new Label($"{icon} {label} x{badge.Count}");
                lbl.AddToClassList("synergy-hud-row-label");
                row.Add(lbl);
                _list.Add(row);
            }

            if (badges.Count == 0)
            {
                var empty = new Label("Pas de synergie active");
                empty.AddToClassList("synergy-hud-empty");
                _list.Add(empty);
            }
        }
    }
}
