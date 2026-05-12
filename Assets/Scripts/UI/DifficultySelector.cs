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
        private const string PrefKey = "difficulty_v1";

        private static readonly (Difficulty diff, string label, string colorClass)[] Options =
        {
            (Difficulty.Easy,   "Faible (x0.7 HP/Dmg, x1.2 recompenses)",  "diff-easy"),
            (Difficulty.Normal, "Normal (equilibre)",                         "diff-normal"),
            (Difficulty.Hard,   "Difficile (x1.5 HP/Dmg, x0.8 recompenses)", "diff-hard"),
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

            var btnRow = new VisualElement();
            btnRow.style.flexDirection  = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.Center;

            foreach (var (diff, label, colorClass) in Options)
            {
                var btn = new Button();
                btn.text = label;
                btn.AddToClassList("diff-btn");
                btn.AddToClassList(colorClass);
                btn.style.marginLeft    = 10;
                btn.style.marginRight   = 10;
                btn.style.paddingTop    = 14;
                btn.style.paddingBottom = 14;
                btn.style.paddingLeft   = 20;
                btn.style.paddingRight  = 20;
                btn.style.fontSize      = 15;
                btn.style.whiteSpace    = WhiteSpace.Normal;
                btn.style.maxWidth      = 200;
                ApplyDiffColor(btn, diff);

                var captured = diff;
                btn.RegisterCallback<ClickEvent>(_ => OnPicked(captured));
                btnRow.Add(btn);
            }

            _root.Add(btnRow);
            docRoot.Add(_root);
        }

        private static void ApplyDiffColor(Button btn, Difficulty diff)
        {
            var bg = diff switch
            {
                Difficulty.Easy   => new Color(0.18f, 0.62f, 0.18f, 1f),
                Difficulty.Normal => new Color(0.80f, 0.70f, 0.05f, 1f),
                Difficulty.Hard   => new Color(0.75f, 0.15f, 0.15f, 1f),
                _                 => Color.gray,
            };
            btn.style.backgroundColor = new StyleColor(bg);
            btn.style.color           = new StyleColor(Color.white);
            btn.style.borderTopLeftRadius     = 6;
            btn.style.borderTopRightRadius    = 6;
            btn.style.borderBottomLeftRadius  = 6;
            btn.style.borderBottomRightRadius = 6;
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
    }
}
