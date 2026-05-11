#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // BluePill debug panel — visible only in Editor or debug builds.
    [RequireComponent(typeof(UIDocument))]
    public class BluePillController : MonoBehaviour
    {
        private VisualElement? _root;
        private VisualElement? _bluepillRoot;

        private Label?  _titleLabel;
        private Label?  _descLabel;
        private Label?  _toggleLabel;
        private Toggle? _toggle;
        private Label?  _killsLabel;
        private Label?  _killsValue;
        private Label?  _usesLabel;
        private Label?  _usesValue;
        private Button? _closeBtn;

        private void Start()
        {
#if !UNITY_EDITOR
            if (!Debug.isDebugBuild)
            {
                gameObject.SetActive(false);
                return;
            }
#endif
            _root         = GetComponent<UIDocument>().rootVisualElement;
            _bluepillRoot = _root.Q<VisualElement>("bluepill-root");

            _titleLabel  = _root.Q<Label>("bluepill-title");
            _descLabel   = _root.Q<Label>("bluepill-description");
            _toggleLabel = _root.Q<Label>("bluepill-toggle-label");
            _toggle      = _root.Q<Toggle>("bluepill-toggle");
            _killsLabel  = _root.Q<Label>("bluepill-kills-label");
            _killsValue  = _root.Q<Label>("bluepill-kills-value");
            _usesLabel   = _root.Q<Label>("bluepill-uses-label");
            _usesValue   = _root.Q<Label>("bluepill-uses-value");
            _closeBtn    = _root.Q<Button>("bluepill-close-btn");

            ApplyLocalizedTexts();
            L.OnLocaleChanged += ApplyLocalizedTexts;

            _toggle?.RegisterValueChangedCallback(evt =>
            {
                BluePill.Instance?.Enable(evt.newValue);
            });

            _closeBtn?.RegisterCallback<ClickEvent>(_ => Hide());
        }

        private void OnDestroy()
        {
            L.OnLocaleChanged -= ApplyLocalizedTexts;
        }

        private void ApplyLocalizedTexts()
        {
            if (_titleLabel != null)  _titleLabel.text  = L.Get("bluepill.title");
            if (_descLabel != null)   _descLabel.text   = L.Get("bluepill.description");
            if (_toggleLabel != null) _toggleLabel.text = L.Get("bluepill.toggle_label");
            if (_killsLabel != null)  _killsLabel.text  = L.Get("bluepill.kills_label");
            if (_usesLabel != null)   _usesLabel.text   = L.Get("bluepill.levels_label");
            if (_closeBtn != null)    _closeBtn.text    = L.Get("settings.close");

            RefreshStats();
        }

        private void RefreshStats()
        {
            if (_toggle != null && BluePill.Instance != null)
                _toggle.SetValueWithoutNotify(BluePill.Instance.IsEnabled);

            if (_killsValue != null)
                _killsValue.text = BluePill.StoredKills.ToString();
            if (_usesValue != null)
                _usesValue.text = BluePill.StoredUses.ToString();
        }

        public void Show()
        {
            RefreshStats();
            _bluepillRoot?.RemoveFromClassList("hidden");
        }

        public void Hide()
        {
            _bluepillRoot?.AddToClassList("hidden");
        }
    }
}
