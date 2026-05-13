#nullable enable
using System;
using CrowdDefense.Common;
using CrowdDefense.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SchoolSelectController : MonoSingleton<SchoolSelectController>
    {
        [SerializeField] private MagicSchoolDef? fireDef;
        [SerializeField] private MagicSchoolDef? frostDef;
        [SerializeField] private MagicSchoolDef? stoneworkDef;

        private VisualElement? _overlay;
        private Action<MagicSchool>? _onSelected;

        protected override void OnAwakeSingleton()
        {
            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null) return;

            var root = uiDoc.rootVisualElement;
            if (root == null) return;

            _overlay = root.Q<VisualElement>("school-select-overlay");

            BindCard(root, MagicSchool.Fire,      "fire",       fireDef);
            BindCard(root, MagicSchool.Frost,     "frost",      frostDef);
            BindCard(root, MagicSchool.Stonework, "stonework",  stoneworkDef);

            HideOverlay();
        }

        public void Show(Action<MagicSchool> onSelected)
        {
            _onSelected = onSelected;

            if (_overlay == null) return;
            _overlay.RemoveFromClassList("hidden");
        }

        public void Hide()
        {
            HideOverlay();
            _onSelected = null;
        }

        private void HideOverlay() => _overlay?.AddToClassList("hidden");

        private void BindCard(VisualElement root, MagicSchool school, string slug, MagicSchoolDef? def)
        {
            var btn  = root.Q<Button>($"school-card-{slug}");
            var name = root.Q<Label>($"school-name-{slug}");
            var desc = root.Q<Label>($"school-desc-{slug}");

            if (def != null)
            {
                if (name != null) name.text = def.displayName;
                if (desc != null) desc.text = def.description;
            }

            if (btn == null) return;

            btn.RegisterCallback<ClickEvent>(_ =>
            {
                var callback = _onSelected;
                Hide();
                callback?.Invoke(school);
            });
        }
    }
}
