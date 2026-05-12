#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class HelpOverlayController : MonoBehaviour
    {
        private VisualElement? _root;
        private Button? _closeBtn;
        private bool _visible;

        private void Awake()
        {
            var doc = GetComponent<UIDocument>();
            var ve = doc.rootVisualElement;
            _root = ve.Q("help-root");
            _closeBtn = ve.Q<Button>("help-close-btn");

            if (_closeBtn != null) _closeBtn.clicked += Hide;

            var questionBtn = ve.Q<Button>("btn-help");
            if (questionBtn != null) questionBtn.clicked += Toggle;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.Slash))
                Toggle();
        }

        public void Show()
        {
            if (_root == null) return;
            _visible = true;
            _root.RemoveFromClassList("hidden");
        }

        public void Hide()
        {
            if (_root == null) return;
            _visible = false;
            _root.AddToClassList("hidden");
        }

        public void Toggle()
        {
            if (_visible) Hide(); else Show();
        }
    }
}
