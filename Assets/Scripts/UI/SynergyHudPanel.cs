#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Left-side synergy panel: lists active (gold) + inactive (greyed) synergies.
    /// Each row has a hover tooltip showing bonus description + towers-required count.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SynergyHudPanel : MonoBehaviour
    {
        private VisualElement? _panel;
        private VisualElement? _list;

        // All known synergy definitions for display (active+inactive)
        private static readonly SynergyInfo[] AllSynergies = new SynergyInfo[]
        {
            new("archer",   "syn.archer.name",   "syn.archer.desc",   requiredCount: 3),
            new("mage",     "syn.mage.name",     "syn.mage.desc",     requiredCount: 3),
            new("ballista", "syn.ballista.name", "syn.ballista.desc", requiredCount: 2),
            new("cannon",   "syn.cannon.name",   "syn.cannon.desc",   requiredCount: 2),
            new("crossbow", "syn.crossbow.name", "syn.crossbow.desc", requiredCount: 3),
            new("frost",    "syn.frost.name",    "syn.frost.desc",    requiredCount: 2),
            new("fan",      "syn.fan.name",      "syn.fan.desc",      requiredCount: 2),
            new("acid",     "syn.acid.name",     "syn.acid.desc",     requiredCount: 2),
            new("mine",     "syn.mine.name",     "syn.mine.desc",     requiredCount: 2),
            new("portal",   "syn.portal.name",   "syn.portal.desc",   requiredCount: 2),
            new("magnet",   "syn.magnet.name",   "syn.magnet.desc",   requiredCount: 1),
            new("skyguard", "syn.skyguard.name", "syn.skyguard.desc", requiredCount: 2),
            new("tank",     "syn.tank.name",     "syn.tank.desc",     requiredCount: 2),
        };

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
            // Build active set for fast lookup
            var activeMap = new Dictionary<string, int>();
            if (synergies != null)
            {
                var badges = synergies.ActiveBadges;
                for (int i = 0; i < badges.Count; i++)
                    activeMap[badges[i].TowerId] = badges[i].Count;
            }

            bool anyVisible = false;
            foreach (var info in AllSynergies)
            {
                activeMap.TryGetValue(info.TowerId, out int count);
                bool active = count > 0;
                _list.Add(BuildRow(info, count, active));
                anyVisible = true;
            }

            if (anyVisible)
                _panel.RemoveFromClassList("hidden");
            else
                _panel.AddToClassList("hidden");
        }

        private static VisualElement BuildRow(SynergyInfo info, int count, bool active)
        {
            string icon  = L.Get($"syn.icon.{info.TowerId}");
            string name  = L.Get(info.NameKey, "UI");
            if (string.IsNullOrEmpty(name)) name = info.TowerId;

            string bonusDesc = L.Get(info.DescKey, "UI");
            if (string.IsNullOrEmpty(bonusDesc)) bonusDesc = "Bonus actif";

            string countLabel = active
                ? $"{icon} {name} x{count}"
                : $"{icon} {name} (x{info.RequiredCount})";

            // Wrapper for hover detection
            var wrap = new VisualElement();
            wrap.AddToClassList("synergy-hud-row");
            if (!active) wrap.AddToClassList("synergy-hud-row-inactive");

            var lbl = new Label(countLabel);
            lbl.AddToClassList("synergy-hud-row-label");
            wrap.Add(lbl);

            // Tooltip element (hidden by default, shown on hover)
            var tooltip = new VisualElement();
            tooltip.AddToClassList("synergy-hud-tooltip");
            tooltip.AddToClassList("hidden");

            var ttTitle = new Label(name);
            ttTitle.AddToClassList("synergy-hud-tooltip-title");
            tooltip.Add(ttTitle);

            var ttDesc = new Label(bonusDesc);
            ttDesc.AddToClassList("synergy-hud-tooltip-desc");
            tooltip.Add(ttDesc);

            string reqText = $"Tours requises : {info.RequiredCount}";
            var ttReq = new Label(reqText);
            ttReq.AddToClassList("synergy-hud-tooltip-req");
            tooltip.Add(ttReq);

            wrap.Add(tooltip);

            wrap.RegisterCallback<MouseEnterEvent>(_ =>
                tooltip.RemoveFromClassList("hidden"));
            wrap.RegisterCallback<MouseLeaveEvent>(_ =>
                tooltip.AddToClassList("hidden"));

            return wrap;
        }

        private readonly struct SynergyInfo
        {
            public readonly string TowerId;
            public readonly string NameKey;
            public readonly string DescKey;
            public readonly int RequiredCount;

            public SynergyInfo(string towerId, string nameKey, string descKey, int requiredCount)
            {
                TowerId       = towerId;
                NameKey       = nameKey;
                DescKey       = descKey;
                RequiredCount = requiredCount;
            }
        }
    }
}
