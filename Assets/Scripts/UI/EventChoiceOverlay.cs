#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;

namespace CrowdDefense.UI
{
    // Port du flow event V5 : overlay 2-3 boutons apres un niveau ou une vague (30% chance via EventSystem).
    // Attacher a un GameObject avec UIDocument + EventChoice.uxml.
    public class EventChoiceOverlay : UIControllerBase
    {
        private VisualElement? _root;
        private Label? _titleLabel;
        private Label? _bodyLabel;
        private Button?[] _btns = new Button?[3];

        private EventDef? _currentEvent;
        private Action<EventDef, int>? _onPicked;

        private void Awake()
        {
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            if (Root == null) return;

            _root       = Root.Q<VisualElement>("event-root");
            _titleLabel = Root.Q<Label>("event-title");
            _bodyLabel  = Root.Q<Label>("event-body");
            _btns[0]    = Root.Q<Button>("choice-btn-0");
            _btns[1]    = Root.Q<Button>("choice-btn-1");
            _btns[2]    = Root.Q<Button>("choice-btn-2");

            for (int i = 0; i < _btns.Length; i++)
            {
                int idx = i;
                _btns[i]?.RegisterCallback<ClickEvent>(_ => Pick(idx));
            }

            Hide();
        }

        public void Show(EventDef evt, Action<EventDef, int> onPicked)
        {
            _currentEvent = evt;
            _onPicked = onPicked;

            if (_titleLabel != null) _titleLabel.text = evt.Title;
            if (_bodyLabel  != null) _bodyLabel.text  = evt.Body;

            for (int i = 0; i < _btns.Length; i++)
            {
                var btn = _btns[i];
                if (btn == null) continue;
                if (i < evt.Choices.Length)
                {
                    btn.text = evt.Choices[i].label;
                    btn.RemoveFromClassList("hidden");
                }
                else
                {
                    btn.AddToClassList("hidden");
                }
            }

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
