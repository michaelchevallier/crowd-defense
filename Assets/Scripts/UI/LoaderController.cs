#nullable enable
using System.Collections;
using CrowdDefense.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class LoaderController : MonoBehaviour
    {
        private const float FadeInDuration = 1.2f;

        private VisualElement? _fadeOverlay;
        private Label?         _versionLabel;
        private Button?        _btnContinue;
        private Button?        _btnNewRun;
        private Button?        _btnSettings;
        private Button?        _btnQuit;

        [SerializeField] private SettingsPanelController? settingsPanel;

        private void Start()
        {
#if !UNITY_EDITOR
            // WebGL / runtime skip-arg support: ?skip=1 in URL → jump directly to WorldMap
            if (Application.absoluteURL.Contains("skip=1"))
            {
                LevelLoader.GoToWorldMap();
                return;
            }
#endif
            var root = GetComponent<UIDocument>().rootVisualElement;
            _fadeOverlay  = root.Q<VisualElement>("fade-overlay");
            _versionLabel = root.Q<Label>("version-label");
            _btnContinue  = root.Q<Button>("btn-continue");
            _btnNewRun    = root.Q<Button>("btn-newrun");
            _btnSettings  = root.Q<Button>("btn-settings");
            _btnQuit      = root.Q<Button>("btn-quit");

            if (_versionLabel != null)
                _versionLabel.text = "v" + Application.version;

            RefreshContinueState();
            BindButtons();

            MusicManager.Instance?.Play("menu");
            StartCoroutine(FadeInCo());
        }

        private void BindButtons()
        {
            _btnContinue?.RegisterCallback<ClickEvent>(_ => OnContinue());
            _btnNewRun?.RegisterCallback<ClickEvent>(_ => OnNewRun());
            _btnSettings?.RegisterCallback<ClickEvent>(_ => OnSettings());
            _btnQuit?.RegisterCallback<ClickEvent>(_ => OnQuit());
        }

        private void RefreshContinueState()
        {
            bool hasAnySave = SaveSystem.SlotHasData(0)
                           || SaveSystem.SlotHasData(1)
                           || SaveSystem.SlotHasData(2);
            if (_btnContinue == null) return;
            if (hasAnySave)
                _btnContinue.RemoveFromClassList("menu-btn-disabled");
            else
                _btnContinue.AddToClassList("menu-btn-disabled");
            _btnContinue.SetEnabled(hasAnySave);
        }

        // ── Button handlers ──────────────────────────────────────────────────

        private void OnContinue()
        {
            // Find the most-recently-played slot and go directly to WorldMap
            int slot = MostRecentSlot();
            SaveSystem.SelectSlot(slot);
            LevelLoader.GoToWorldMap();
        }

        private void OnNewRun()
        {
            // Delete slot 0 (single-slot fast-path; slot picker can be added later)
            SaveSystem.DeleteSlot(0);
            SaveSystem.SelectSlot(0);
            LevelLoader.GoToWorldMap();
        }

        private void OnSettings()
        {
            settingsPanel?.Show();
        }

        private static void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Fade-in coroutine ────────────────────────────────────────────────

        private IEnumerator FadeInCo()
        {
            if (_fadeOverlay == null) yield break;

            float t = 0f;
            while (t < FadeInDuration)
            {
                t += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.Clamp01(t / FadeInDuration);
                _fadeOverlay.style.opacity = alpha;
                yield return null;
            }
            _fadeOverlay.style.opacity = 0f;
            _fadeOverlay.style.display = DisplayStyle.None;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static int MostRecentSlot()
        {
            // Return first occupied slot (slot 0 priority; could compare dates later)
            for (int i = 0; i < 3; i++)
                if (SaveSystem.SlotHasData(i)) return i;
            return 0;
        }
    }
}
