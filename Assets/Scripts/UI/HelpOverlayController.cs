#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class HelpOverlayController : MonoBehaviour
    {
        static readonly (string key, string desc)[] Shortcuts =
        {
            ("Clic / Select", "Placer une tour / selectionner"),
            ("Space",         "Lancer la vague"),
            ("1 - 4",         "Selectionner type de tour"),
            ("Q / W / E",     "Capacites du heros"),
            ("M",             "Mute audio"),
            ("F3",            "Compteur FPS / Profil"),
            ("ESC",           "Menu pause"),
            ("F1 / H / ?",    "Cette aide"),
        };

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

            PopulateRows(ve);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyBindings.GetKey("help")) || Input.GetKeyDown(KeyCode.Slash) || Input.GetKeyDown(KeyCode.F1))
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

        // Replaces UXML static list with the canonical Shortcuts table above.
        private static void PopulateRows(VisualElement ve)
        {
            var list = ve.Q("help-root")?.Q(className: "help-list");
            if (list == null) return;

            list.Clear();
            foreach (var (key, desc) in Shortcuts)
            {
                var row = new VisualElement();
                row.AddToClassList("help-row");
                var keyLabel = new Label(key);
                keyLabel.AddToClassList("help-key");
                var descLabel = new Label(desc);
                descLabel.AddToClassList("help-desc");
                row.Add(keyLabel);
                row.Add(descLabel);
                list.Add(row);
            }
        }
    }
}
