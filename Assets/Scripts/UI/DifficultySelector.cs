#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Data;

namespace CrowdDefense.UI
{
    // Fullscreen difficulty picker shown between level selection and level load.
    // Call Show(onConfirmed) from LevelSelectController; the panel saves the choice
    // to PlayerPrefs("difficulty_v1") then invokes onConfirmed.
    [RequireComponent(typeof(UIDocument))]
    public class DifficultySelector : MonoSingleton<DifficultySelector>
    {
        public const string PrefKey = "difficulty_v1";

        internal static readonly (Difficulty diff, string label, Color bg)[] Options =
        {
            (Difficulty.Easy,   "Facile\nx0.7 HP/Dmg",   new Color(0.18f, 0.62f, 0.18f, 1f)),
            (Difficulty.Normal, "Normal\nequilibre",      new Color(0.70f, 0.62f, 0.05f, 1f)),
            (Difficulty.Hard,   "Difficile\nx1.3 HP/Dmg", new Color(0.75f, 0.38f, 0.05f, 1f)),
            (Difficulty.Brutal, "Brutal\nx1.6 HP/Dmg",    new Color(0.72f, 0.10f, 0.10f, 1f)),
        };

        private VisualElement? _root;
        private Action?        _onConfirmed;
        private bool           _built;

        protected override void OnAwakeSingleton()
        {
            TryBuild();
        }

        private void Start()
        {
            // Fallback in case rootVisualElement was not ready during Awake (PanelSettings not yet set).
            TryBuild();
        }

        private void TryBuild()
        {
            if (_built) return;
            var doc = GetComponent<UIDocument>();
            if (doc == null) return;
            var docRoot = doc.rootVisualElement;
            if (docRoot == null) return;
            _built = true;
            BuildUI(docRoot);
            Hide();
        }

        private void BuildUI(VisualElement docRoot)
        {
            _root = new VisualElement();
            _root.name = "difficulty-overlay";
            _root.style.position          = Position.Absolute;
            _root.style.left              = 0;
            _root.style.top               = 0;
            _root.style.right             = 0;
            _root.style.bottom            = 0;
            _root.style.backgroundColor   = new StyleColor(new Color(0f, 0f, 0f, 0.75f));
            _root.style.alignItems        = Align.Center;
            _root.style.justifyContent    = Justify.Center;
            _root.style.flexDirection     = FlexDirection.Column;

            var title = new Label("Choisissez la difficulte");
            title.style.fontSize        = 28;
            title.style.color           = new StyleColor(Color.white);
            title.style.marginBottom    = 24;
            _root.Add(title);

            int savedDiff = PlayerPrefs.GetInt(PrefKey, (int)Difficulty.Normal);
            var btnRow = new VisualElement();
            btnRow.style.flexDirection  = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.Center;

            var buttons = new Button[Options.Length];
            for (int i = 0; i < Options.Length; i++)
            {
                var (diff, label, bg) = Options[i];
                var btn = new Button();
                btn.text = label;
                btn.AddToClassList("diff-btn");
                btn.style.marginLeft    = 10;
                btn.style.marginRight   = 10;
                btn.style.paddingTop    = 14;
                btn.style.paddingBottom = 14;
                btn.style.paddingLeft   = 20;
                btn.style.paddingRight  = 20;
                btn.style.fontSize      = 15;
                btn.style.whiteSpace    = WhiteSpace.Normal;
                btn.style.maxWidth      = 160;
                btn.style.minWidth      = 100;
                ApplyDiffStyle(btn, bg, (int)diff == savedDiff);

                var captured = diff;
                var capturedBg = bg;
                var capturedBtns = buttons;
                btn.RegisterCallback<ClickEvent>(_ =>
                {
                    for (int j = 0; j < capturedBtns.Length; j++)
                        if (capturedBtns[j] != null)
                            ApplyDiffStyle(capturedBtns[j], Options[j].bg, (int)Options[j].diff == (int)captured);
                    OnPicked(captured);
                });
                buttons[i] = btn;
                btnRow.Add(btn);
            }

            _root.Add(btnRow);
            docRoot.Add(_root);
        }

        private static void ApplyDiffStyle(Button btn, Color bg, bool selected)
        {
            btn.style.backgroundColor = new StyleColor(bg);
            btn.style.color           = new StyleColor(Color.white);
            btn.style.borderTopLeftRadius     = 6;
            btn.style.borderTopRightRadius    = 6;
            btn.style.borderBottomLeftRadius  = 6;
            btn.style.borderBottomRightRadius = 6;
            float borderW = selected ? 3f : 1f;
            btn.style.borderTopWidth    = borderW;
            btn.style.borderBottomWidth = borderW;
            btn.style.borderLeftWidth   = borderW;
            btn.style.borderRightWidth  = borderW;
            var borderCol = selected ? Color.white : new Color(1f, 1f, 1f, 0.25f);
            btn.style.borderTopColor    = new StyleColor(borderCol);
            btn.style.borderBottomColor = new StyleColor(borderCol);
            btn.style.borderLeftColor   = new StyleColor(borderCol);
            btn.style.borderRightColor  = new StyleColor(borderCol);
            btn.style.scale             = selected
                ? new StyleScale(new Scale(new Vector2(1.05f, 1.05f)))
                : new StyleScale(new Scale(Vector2.one));
        }

        private void OnPicked(Difficulty diff)
        {
            PlayerPrefs.SetInt(PrefKey, (int)diff);
            PlayerPrefs.Save();
            Hide();
            _onConfirmed?.Invoke();
            _onConfirmed = null;
        }

        public void Show(Action onConfirmed)
        {
            TryBuild();
            _onConfirmed = onConfirmed;
            if (_root != null)
                _root.style.display = DisplayStyle.Flex;
            else
                // UI not ready (no UIDocument in scene) — skip panel, load immediately.
                onConfirmed();
        }

        private void Hide()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
        }

        // Helper: returns the display name for a given Difficulty value.
        public static string DifficultyName(Difficulty d) => d switch
        {
            Difficulty.Easy   => "FACILE",
            Difficulty.Normal => "NORMAL",
            Difficulty.Hard   => "DIFFICILE",
            Difficulty.Brutal => "BRUTAL",
            _                 => "NORMAL",
        };

        // Helper: returns color for a given Difficulty value.
        public static Color DifficultyColor(Difficulty d) => d switch
        {
            Difficulty.Easy   => new Color(0.18f, 0.62f, 0.18f, 1f),
            Difficulty.Normal => new Color(0.70f, 0.62f, 0.05f, 1f),
            Difficulty.Hard   => new Color(0.75f, 0.38f, 0.05f, 1f),
            Difficulty.Brutal => new Color(0.72f, 0.10f, 0.10f, 1f),
            _                 => Color.gray,
        };
    }
}
