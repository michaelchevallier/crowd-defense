#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;
using CrowdDefense.Data;

namespace CrowdDefense.UI
{
    // Optional: attach to a card prefab for HeroPickScreen.
    // If no prefab is wired, HeroPickScreen builds cards dynamically.
    public class HeroCardView : MonoBehaviour
    {
        [SerializeField] private Image?  portrait;
        [SerializeField] private Text?   nameLabel;
        [SerializeField] private Text?   descLabel;
        [SerializeField] private Button? chooseButton;

        private HeroType?          _hero;
        private Action<HeroType>?  _onChosen;

        public void Bind(HeroType hero, Action<HeroType> onChosen)
        {
            _hero     = hero;
            _onChosen = onChosen;

            if (portrait  != null) portrait.color  = hero.BodyColor;
            if (nameLabel != null) nameLabel.text   = hero.DisplayName;
            if (descLabel != null)
                descLabel.text = string.IsNullOrEmpty(hero.Description)
                    ? $"DMG {hero.Damage:F2}  SPD {hero.MoveSpeed:F0}"
                    : hero.Description;

            if (chooseButton != null)
            {
                chooseButton.onClick.RemoveAllListeners();
                chooseButton.onClick.AddListener(OnChooseClicked);
            }
        }

        private void OnChooseClicked()
        {
            if (_hero != null) _onChosen?.Invoke(_hero);
        }
    }
}
