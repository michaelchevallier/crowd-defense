#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;
using CrowdDefense.Data;
using CrowdDefense.Entities;

namespace CrowdDefense.UI
{
    // Bound to each card instantiated by PerkChoiceOverlay.
    // Requires: Text (nameText), Text (descText), Button (button) on GameObject.
    public class PerkCardView : MonoBehaviour
    {
        [SerializeField] private Text?   nameText;
        [SerializeField] private Text?   descText;
        [SerializeField] private Text?   iconText;
        [SerializeField] private Button? button;

        private PerkDef? _def;
        private Action<PerkDef>? _onPicked;

        public void Bind(PerkDef def, Hero hero, Action<PerkDef> onPicked)
        {
            _def      = def;
            _onPicked = onPicked;

            if (nameText != null) nameText.text = def.nameKey;
            if (descText  != null) descText.text  = def.descKey;
            if (iconText  != null) iconText.text  = def.iconEmoji;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                bool canApply = Systems.PerkSystem.Instance?.CanApply(hero, def) ?? true;
                button.interactable = canApply;
                button.onClick.AddListener(OnClick);
            }
        }

        private void OnClick()
        {
            if (_def != null) _onPicked?.Invoke(_def);
        }
    }
}
