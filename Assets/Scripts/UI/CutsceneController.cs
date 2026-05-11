#nullable enable
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Manages the full-screen cutscene overlay.
    /// Add this component to the same GameObject that holds the UIDocument for the cutscene panel.
    /// Call Play(cutsceneId, onDone) to start; LevelRunner freezes Time.timeScale until callback.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CutsceneController : MonoBehaviour
    {
        private UIDocument? _doc;
        private VisualElement? _root;
        private VisualElement? _panel;
        private VisualElement? _portraitLeft;
        private VisualElement? _portraitRight;
        private Label? _speakerLabel;
        private Label? _textLabel;
        private Label? _titleLabel;
        private Label? _continueHint;

        private CutsceneDef? _currentDef;
        private int _lineIndex;
        private Action? _onDone;
        private bool _typewriting;
        private Coroutine? _typewriteCoroutine;

        private const float TypewriteSpeed = 40f;

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;

            _panel         = _root.Q<VisualElement>("cutscene-panel");
            _portraitLeft  = _root.Q<VisualElement>("portrait-left");
            _portraitRight = _root.Q<VisualElement>("portrait-right");
            _speakerLabel  = _root.Q<Label>("cutscene-speaker");
            _textLabel     = _root.Q<Label>("cutscene-text");
            _titleLabel    = _root.Q<Label>("cutscene-title");
            _continueHint  = _root.Q<Label>("cutscene-hint");

            _panel?.AddToClassList("hidden");
        }

        public void Play(string cutsceneId, Action? onDone = null)
        {
            var reg = CutsceneRegistry.Get();
            if (reg == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[CutsceneController] CutsceneRegistry not found — skipping '{cutsceneId}'");
#endif
                onDone?.Invoke();
                return;
            }

            var def = reg.FindById(cutsceneId);
            if (def == null || def.Lines.Count == 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[CutsceneController] Cutscene '{cutsceneId}' not found or empty — skipping");
#endif
                onDone?.Invoke();
                return;
            }

            _currentDef = def;
            _lineIndex  = 0;
            _onDone     = onDone;

            if (_titleLabel != null)
                _titleLabel.text = L.Get(def.TitleKey);

            _panel?.RemoveFromClassList("hidden");
            ShowLine(_lineIndex);
        }

        private void ShowLine(int idx)
        {
            if (_currentDef == null) return;
            if (idx >= _currentDef.Lines.Count)
            {
                Finish();
                return;
            }

            var line = _currentDef.Lines[idx];

            if (_speakerLabel != null)
                _speakerLabel.text = string.IsNullOrEmpty(line.speaker) ? "" : L.Get(line.speaker);

            if (_portraitLeft  != null) _portraitLeft.style.backgroundImage  = null;
            if (_portraitRight != null) _portraitRight.style.backgroundImage = null;

            if (line.portrait != null)
            {
                var target = line.side == PortraitSide.Left ? _portraitLeft : _portraitRight;
                if (target != null)
                    target.style.backgroundImage = new StyleBackground(line.portrait);
            }

            string fullText = L.Get(line.textKey);

            if (_typewriteCoroutine != null)
                StopCoroutine(_typewriteCoroutine);
            _typewriteCoroutine = StartCoroutine(TypewriteLine(fullText));
        }

        private IEnumerator TypewriteLine(string text)
        {
            _typewriting = true;
            if (_textLabel    != null) _textLabel.text = "";
            if (_continueHint != null) _continueHint.AddToClassList("hidden");

            float delay = 1f / TypewriteSpeed;
            for (int i = 1; i <= text.Length; i++)
            {
                if (_textLabel != null)
                    _textLabel.text = text.Substring(0, i);
                yield return new WaitForSecondsRealtime(delay);
            }

            _typewriting = false;
            if (_continueHint != null)
                _continueHint.RemoveFromClassList("hidden");
        }

        private void Update()
        {
            if (_currentDef == null || _panel == null) return;
            if (_panel.ClassListContains("hidden")) return;

            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                Advance();
        }

        private void Advance()
        {
            if (_typewriting)
            {
                if (_typewriteCoroutine != null)
                {
                    StopCoroutine(_typewriteCoroutine);
                    _typewriteCoroutine = null;
                }
                _typewriting = false;
                if (_currentDef != null && _lineIndex < _currentDef.Lines.Count)
                {
                    string fullText = L.Get(_currentDef.Lines[_lineIndex].textKey);
                    if (_textLabel != null) _textLabel.text = fullText;
                }
                if (_continueHint != null)
                    _continueHint.RemoveFromClassList("hidden");
                return;
            }

            _lineIndex++;
            ShowLine(_lineIndex);
        }

        private void Finish()
        {
            _panel?.AddToClassList("hidden");
            _currentDef = null;
            var cb = _onDone;
            _onDone = null;
            cb?.Invoke();
        }
    }
}
