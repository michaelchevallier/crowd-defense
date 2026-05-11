#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // HUD row showing active perk badges + set bonus progress (X/3 per tag).
    // Place under HudController Canvas. Requires badgeContainer + badgePrefab.
    public class HudPerkBadges : MonoBehaviour
    {
        [SerializeField] private RectTransform? badgeContainer;
        [SerializeField] private GameObject?    badgePrefab;
        [SerializeField] private Text?          setProgressText;

        private Hero? _hero;
        private PerkSystem? _system;

        private void Start()
        {
            _hero   = LevelRunner.Instance?.Hero;
            _system = PerkSystem.Instance;

            if (_system != null)
            {
                _system.OnPerkApplied        += OnPerkApplied;
                _system.OnSetBonusActivated  += OnSetBonusActivated;
            }
        }

        private void OnDestroy()
        {
            if (_system != null)
            {
                _system.OnPerkApplied        -= OnPerkApplied;
                _system.OnSetBonusActivated  -= OnSetBonusActivated;
            }
        }

        private void OnPerkApplied(Hero hero, PerkDef def)
        {
            if (hero != _hero) return;
            AddBadge(def.iconEmoji, def.nameKey);
            RefreshSetProgress();
        }

        private void OnSetBonusActivated(Hero hero, PerkSetBonusDef bonus)
        {
            if (hero != _hero) return;
            AddBadge("", bonus.nameKey);
            RefreshSetProgress();
        }

        private void AddBadge(string emoji, string label)
        {
            if (badgePrefab == null || badgeContainer == null) return;
            var go  = Instantiate(badgePrefab, badgeContainer);
            var txt = go.GetComponentInChildren<Text>();
            if (txt != null) txt.text = emoji;
        }

        private void RefreshSetProgress()
        {
            if (_hero == null || setProgressText == null) return;
            var registry = PerkRegistry.Load();
            if (registry == null) return;

            var counts = new Dictionary<PerkTag, int>();
            foreach (var id in _hero.Perks)
            {
                var def = registry.Get(id);
                if (def == null || def.tag == PerkTag.None) continue;
                counts.TryGetValue(def.tag, out int c);
                counts[def.tag] = c + 1;
            }

            var parts = new List<string>();
            foreach (var kv in counts)
            {
                if (kv.Value > 0)
                    parts.Add($"{TagEmoji(kv.Key)}{kv.Value}/3");
            }
            setProgressText.text = string.Join("  ", parts);
        }

        private static string TagEmoji(PerkTag tag) => tag switch
        {
            PerkTag.Foudre => "E",
            PerkTag.Sang   => "S",
            PerkTag.Pierre => "P",
            PerkTag.Feu    => "F",
            PerkTag.Vide   => "V",
            PerkTag.Or     => "O",
            _              => ""
        };
    }
}
