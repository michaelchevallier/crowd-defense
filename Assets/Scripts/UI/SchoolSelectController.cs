#nullable enable
using System;
using CrowdDefense.Common;
using CrowdDefense.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    public class SchoolSelectController : MonoSingleton<SchoolSelectController>
    {
        [SerializeField] private VisualTreeAsset? overlayAsset;
        [SerializeField] private MagicSchoolDef? fireDef;
        [SerializeField] private MagicSchoolDef? frostDef;
        [SerializeField] private MagicSchoolDef? stoneworkDef;

        private VisualElement? _overlay;
        private Action<MagicSchool>? _onSelected;

        protected override void OnAwakeSingleton()
        {
            TryBindOverlay();
        }

        // Self-bootstraps a UIDocument from Inspector overlayAsset OR Resources/UI/SchoolSelectScreen
        // so MenuController.OnNewRun can drive an overlay even when no scene-wired UIDocument exists.
        private void TryBindOverlay()
        {
            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null) uiDoc = gameObject.AddComponent<UIDocument>();

            if (uiDoc.visualTreeAsset == null)
            {
                uiDoc.visualTreeAsset = overlayAsset
                    ?? Resources.Load<VisualTreeAsset>("UI/SchoolSelectScreen")
                    ?? Resources.Load<VisualTreeAsset>("SchoolSelectScreen");
            }

            if (uiDoc.panelSettings == null)
            {
                var others = FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude);
                foreach (var doc in others)
                {
                    if (doc != uiDoc && doc.panelSettings != null)
                    {
                        uiDoc.panelSettings = doc.panelSettings;
                        break;
                    }
                }
            }

            var root = uiDoc.visualTreeAsset != null ? uiDoc.rootVisualElement : null;
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

            // Late bootstrap retry if Awake-time bind failed (e.g. PanelSettings not yet ready).
            if (_overlay == null) TryBindOverlay();

            if (_overlay == null)
            {
                // No overlay wired in scene → unblock NEW GAME flow with Fire default.
                Debug.LogWarning("[SchoolSelectController] overlay not bound — defaulting to Fire.");
                _onSelected = null;
                onSelected?.Invoke(MagicSchool.Fire);
                return;
            }
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
