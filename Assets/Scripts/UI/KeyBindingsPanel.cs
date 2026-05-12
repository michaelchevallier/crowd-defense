#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Modal scrollable list: action -> current key button -> press-to-remap.
    // Attached as sibling on the HUD UIDocument.
    [RequireComponent(typeof(UIDocument))]
    public class KeyBindingsPanel : MonoBehaviour
    {
        private static readonly (string action, string labelFr)[] ActionMeta =
        {
            ("pause",       "Pause"),
            ("speed",       "Vitesse cycle"),
            ("mute",        "Muet"),
            ("debug",       "Debug HUD"),
            ("birdseye",    "Vue aerienne"),
            ("follow",      "Suivre le hero"),
            ("help",        "Aide"),
            ("pathPreview", "Preview chemin"),
            ("save",        "Sauvegarde rapide"),
            ("load",        "Charger sauvegarde"),
            ("reset",       "Reset camera"),
        };

        private VisualElement? _root;
        private bool _visible;
        private string? _remappingAction;

        // Row buttons keyed by action
        private readonly Dictionary<string, Button> _rowBtns = new();

        private void Start()
        {
            var docRoot = GetComponent<UIDocument>().rootVisualElement;
            _root = BuildPanel(docRoot);
        }

        private void Update()
        {
            if (_remappingAction == null) return;

            // Capture any key press during remap mode
            foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (kc == KeyCode.None) continue;
                if (!Input.GetKeyDown(kc)) continue;

                if (kc == KeyCode.Escape)
                {
                    CancelRemap();
                    return;
                }

                CommitRemap(_remappingAction, kc);
                return;
            }
        }

        public void Show()
        {
            _visible = true;
            RefreshLabels();
            _root?.RemoveFromClassList("hidden");
        }

        public void Hide()
        {
            CancelRemap();
            _visible = false;
            _root?.AddToClassList("hidden");
        }

        private void CancelRemap()
        {
            if (_remappingAction == null) return;
            if (_rowBtns.TryGetValue(_remappingAction, out var btn))
                btn.text = FormatKey(KeyBindings.GetKey(_remappingAction));
            _remappingAction = null;
        }

        private void CommitRemap(string action, KeyCode key)
        {
            KeyBindings.SetKey(action, key);
            _remappingAction = null;
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            foreach (var (action, _) in ActionMeta)
            {
                if (_rowBtns.TryGetValue(action, out var btn))
                    btn.text = FormatKey(KeyBindings.GetKey(action));
            }
        }

        private static string FormatKey(KeyCode key) => key.ToString();

        // ── Dynamic panel build (no UXML) ────────────────────────────────────

        private VisualElement BuildPanel(VisualElement docRoot)
        {
            // Dim overlay
            var overlay = new VisualElement { name = "keybindings-overlay" };
            overlay.style.position          = Position.Absolute;
            overlay.style.left              = 0; overlay.style.top   = 0;
            overlay.style.right             = 0; overlay.style.bottom = 0;
            overlay.style.backgroundColor   = new StyleColor(new Color(0f, 0f, 0f, 0.55f));
            overlay.style.alignItems        = Align.Center;
            overlay.style.justifyContent    = Justify.Center;
            overlay.AddToClassList("hidden");

            // Modal box
            var box = new VisualElement();
            box.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.15f, 0.97f));
            box.style.borderTopLeftRadius = box.style.borderTopRightRadius =
            box.style.borderBottomLeftRadius = box.style.borderBottomRightRadius = 10;
            box.style.paddingLeft = box.style.paddingRight = 24;
            box.style.paddingTop  = box.style.paddingBottom = 20;
            box.style.minWidth    = 380;
            box.style.maxHeight   = new Length(85, LengthUnit.Percent);

            // Title row
            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.justifyContent = Justify.SpaceBetween;
            titleRow.style.marginBottom = 14;

            var title = new Label { text = "Raccourcis clavier" };
            title.style.color = new StyleColor(new Color(1f, 0.85f, 0.3f));
            title.style.fontSize = 16;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;

            var closeBtn = new Button { text = "X" };
            closeBtn.style.backgroundColor = new StyleColor(Color.clear);
            closeBtn.style.color = new StyleColor(Color.white);
            closeBtn.style.borderTopWidth = closeBtn.style.borderBottomWidth =
            closeBtn.style.borderLeftWidth = closeBtn.style.borderRightWidth = 0;
            closeBtn.clicked += Hide;

            titleRow.Add(title);
            titleRow.Add(closeBtn);

            // Scroll container
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.maxHeight = new Length(60, LengthUnit.Percent);

            foreach (var (action, labelFr) in ActionMeta)
            {
                var row = BuildRow(action, labelFr);
                scroll.Add(row);
            }

            // Reset all button
            var resetBtn = new Button { text = "Reinitialiser tout" };
            resetBtn.style.marginTop = 16;
            resetBtn.style.alignSelf = Align.Center;
            StyleButton(resetBtn, new Color(0.25f, 0.15f, 0.05f, 1f));
            resetBtn.clicked += () =>
            {
                KeyBindings.ResetAll();
                RefreshLabels();
            };

            box.Add(titleRow);
            box.Add(scroll);
            box.Add(resetBtn);
            overlay.Add(box);
            docRoot.Add(overlay);

            return overlay;
        }

        private VisualElement BuildRow(string action, string labelFr)
        {
            var row = new VisualElement();
            row.style.flexDirection  = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems     = Align.Center;
            row.style.paddingTop = row.style.paddingBottom = 5;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new StyleColor(new Color(1f, 1f, 1f, 0.08f));

            var lbl = new Label { text = labelFr };
            lbl.style.color = new StyleColor(Color.white);
            lbl.style.fontSize = 13;
            lbl.style.flexGrow = 1;

            var btn = new Button { text = FormatKey(KeyBindings.GetKey(action)) };
            btn.style.minWidth  = 110;
            btn.style.fontSize  = 12;
            StyleButton(btn, new Color(0.2f, 0.25f, 0.35f, 1f));

            btn.clicked += () => StartRemap(action, btn);

            _rowBtns[action] = btn;

            row.Add(lbl);
            row.Add(btn);
            return row;
        }

        private void StartRemap(string action, Button btn)
        {
            if (_remappingAction == action)
            {
                CancelRemap();
                return;
            }
            if (_remappingAction != null) CancelRemap();

            _remappingAction = action;
            btn.text = "Appuie sur une touche...";
        }

        private static void StyleButton(Button btn, Color bg)
        {
            btn.style.backgroundColor    = new StyleColor(bg);
            btn.style.color              = new StyleColor(Color.white);
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius =
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 5;
            btn.style.borderTopWidth = btn.style.borderBottomWidth =
            btn.style.borderLeftWidth = btn.style.borderRightWidth = 0;
            btn.style.paddingLeft = btn.style.paddingRight = 10;
            btn.style.paddingTop  = btn.style.paddingBottom = 5;
        }
    }
}
