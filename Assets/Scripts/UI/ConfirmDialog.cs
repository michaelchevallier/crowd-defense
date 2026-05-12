#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;

namespace CrowdDefense.UI
{
    // Static facade — callers use Confirm.Show(...) without needing a reference to the MonoBehaviour.
    public static class Confirm
    {
        public static void Show(string title, string message, Action onConfirm, Action? onCancel = null) =>
            ConfirmDialog.Instance?.Show(title, message, onConfirm, onCancel);
    }

    [RequireComponent(typeof(UIDocument))]
    public class ConfirmDialog : MonoSingleton<ConfirmDialog>
    {
        private VisualElement? _overlay;
        private Label?         _titleLabel;
        private Label?         _messageLabel;
        private Button?        _btnConfirm;
        private Button?        _btnCancel;

        private Action? _onConfirm;
        private Action? _onCancel;

        protected override void OnAwakeSingleton()
        {
            var uiDoc = GetComponent<UIDocument>();

            if (uiDoc == null) return;

            var root = uiDoc.rootVisualElement;

            if (root == null) return;
            _overlay      = root.Q<VisualElement>("confirm-overlay");
            _titleLabel   = root.Q<Label>("confirm-title");
            _messageLabel = root.Q<Label>("confirm-message");
            _btnConfirm   = root.Q<Button>("btn-confirm");
            _btnCancel    = root.Q<Button>("btn-cancel");

            _btnConfirm?.RegisterCallback<ClickEvent>(_ => OnConfirmClicked());
            _btnCancel?.RegisterCallback<ClickEvent>(_ => OnCancelClicked());
        }

        private void Update()
        {
            if (_overlay == null || _overlay.ClassListContains("hidden")) return;
            if (Input.GetKeyDown(KeyCode.Escape))
                OnCancelClicked();
        }

        public void Show(string title, string message, Action onConfirm, Action? onCancel = null)
        {
            if (_overlay == null) return;

            _onConfirm = onConfirm;
            _onCancel  = onCancel;

            if (_titleLabel   != null) _titleLabel.text   = title;
            if (_messageLabel != null) _messageLabel.text = message;

            _overlay.RemoveFromClassList("hidden");
        }

        private void OnConfirmClicked()
        {
            Close();
            _onConfirm?.Invoke();
        }

        private void OnCancelClicked()
        {
            Close();
            _onCancel?.Invoke();
        }

        private void Close()
        {
            _overlay?.AddToClassList("hidden");
            _onConfirm = null;
            _onCancel  = null;
        }
    }
}
