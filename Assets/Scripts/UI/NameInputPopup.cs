#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class NameInputPopup : MonoBehaviour
    {
        private VisualElement? _root;
        private TextField?     _nameField;
        private Button?        _confirmBtn;
        private Action?        _onConfirm;

        private void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _nameField  = _root.Q<TextField>("name-input");
            _confirmBtn = _root.Q<Button>("name-confirm-btn");

            _confirmBtn?.RegisterCallback<ClickEvent>(_ => Confirm());

            // Also confirm on Enter key from the text field
            _nameField?.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    Confirm();
            });

            if (PlayerProfile.Instance != null && PlayerProfile.Instance.IsFirstRun())
                Show(null);
            else
                Hide();
        }

        public void Show(Action? onConfirm)
        {
            _onConfirm = onConfirm;
            if (_nameField != null)
                _nameField.value = PlayerProfile.Instance?.GetName() ?? "Joueur";
            _root?.RemoveFromClassList("hidden");
            _nameField?.Focus();
        }

        public void Hide()
        {
            _root?.AddToClassList("hidden");
        }

        private void Confirm()
        {
            string name = _nameField?.value ?? "Joueur";
            PlayerProfile.Instance?.SetName(name);
            PlayerProfile.Instance?.MarkFirstRunDone();
            Hide();
            _onConfirm?.Invoke();
        }
    }
}
