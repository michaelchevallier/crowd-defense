#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    public class HelpOverlayController : UIControllerBase
    {
        static readonly (string key, string desc)[] Shortcuts =
        {
            ("1 - 9",              "Selectionner tour 1-9"),
            ("WASD / Fleches",     "Deplacer le heros"),
            ("Q  W  E",            "Competences heros"),
            ("R",                  "Ultime heros"),
            ("Construction",       "Selectionne tour [1-9] puis marche jusqu au disque jaune"),
            ("N",                  "Lancer la vague"),
            ("Espace",             "Basculer vitesse jeu"),
            ("+ / -",              "Ajuster vitesse de jeu"),
            ("ESC / P",            "Pause"),
            ("M",                  "Mute audio"),
            ("F",                  "Suivi camera (toggle)"),
            ("V",                  "Vue aerienne"),
            ("Backspace",          "Reinitialiser camera"),
            ("Tab",                "Alterner cible hero"),
            ("Shift+R",            "Recommencer le niveau"),
            ("I",                  "Encyclopedie"),
            ("F1 / H / ?",         "Cette aide"),
            ("F3",                 "HUD debug / FPS"),
            ("F4",                 "Spawn ennemi (debug)"),
            ("F5",                 "Sauvegarde rapide"),
            ("F8",                 "Panneau profil"),
            ("F9",                 "Chargement rapide"),
            ("B",                  "Bluepill (debug)"),
            ("U",                  "Ameliorer tour selectionnee"),
        };

        private VisualElement? _root;
        private Button? _closeBtn;
        private bool _visible;

        private void Awake()
        {
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            if (Root == null) return;
            _root = Root.Q("help-root");
            _closeBtn = Root.Q<Button>("help-close-btn");

            if (_closeBtn != null) _closeBtn.clicked += Hide;

            var questionBtn = Root.Q<Button>("btn-help");
            if (questionBtn != null) questionBtn.clicked += Toggle;

            PopulateRows(Root);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyBindings.GetKey("help")) || Input.GetKeyDown(KeyCode.Slash) || Input.GetKeyDown(KeyCode.F1))
                Toggle();
            else if (_visible && Input.GetKeyDown(KeyCode.Escape))
                Hide();
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
