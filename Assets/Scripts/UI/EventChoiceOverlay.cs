#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;

namespace CrowdDefense.UI
{
    // Port du flow event V5 : overlay 2-boutons apres un niveau (30% chance via EventSystem).
    // Attacher a un GameObject avec UIDocument + EventChoice.uxml.
    [RequireComponent(typeof(UIDocument))]
    public class EventChoiceOverlay : MonoBehaviour
    {
        private VisualElement? _root;
        private Label? _titleLabel;
        private Label? _bodyLabel;
        private Button? _btn0;
        private Button? _btn1;

        private EventDef? _currentEvent;
        private Action<EventDef, int>? _onPicked;

        private void Awake()
        {
            var doc = GetComponent<UIDocument>();
            var ve = doc.rootVisualElement;
            _root       = ve.Q<VisualElement>("event-root");
            _titleLabel = ve.Q<Label>("event-title");
            _bodyLabel  = ve.Q<Label>("event-body");
            _btn0       = ve.Q<Button>("choice-btn-0");
            _btn1       = ve.Q<Button>("choice-btn-1");

            _btn0?.RegisterCallback<ClickEvent>(_ => Pick(0));
            _btn1?.RegisterCallback<ClickEvent>(_ => Pick(1));

            Hide();
        }

        public void Show(EventDef evt, Action<EventDef, int> onPicked)
        {
            _currentEvent = evt;
            _onPicked = onPicked;

            if (_titleLabel != null) _titleLabel.text = evt.Title;
            if (_bodyLabel  != null) _bodyLabel.text  = evt.Body;

            if (_btn0 != null && evt.Choices.Length > 0)
                _btn0.text = evt.Choices[0].label;
            if (_btn1 != null && evt.Choices.Length > 1)
                _btn1.text = evt.Choices[1].label;

            if (_root != null) _root.RemoveFromClassList("hidden");
            Time.timeScale = 0f;
        }

        private void Pick(int index)
        {
            if (_currentEvent == null) return;
            var evt = _currentEvent;
            _currentEvent = null;

            Hide();
            Time.timeScale = 1f;
            _onPicked?.Invoke(evt, index);
            _onPicked = null;
        }

        private void Hide()
        {
            if (_root != null) _root.AddToClassList("hidden");
        }
    }
}
