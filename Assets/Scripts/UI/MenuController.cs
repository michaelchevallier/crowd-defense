#nullable enable
using CrowdDefense.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MenuController : MonoBehaviour
    {
        public static MenuController? Instance { get; private set; }

        private Button? _btnContinue;
        private Button? _btnNewRun;
        private Button? _btnSettings;
        private Button? _btnCredits;
        private Button? _btnQuit;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _btnContinue = root.Q<Button>("btn-continue");
            _btnNewRun   = root.Q<Button>("btn-newrun");
            _btnSettings = root.Q<Button>("btn-settings");
            _btnCredits  = root.Q<Button>("btn-credits");
            _btnQuit     = root.Q<Button>("btn-quit");

            if (_btnContinue != null) _btnContinue.clicked += OnContinue;
            if (_btnNewRun   != null) _btnNewRun.clicked   += OnNewRun;
            if (_btnSettings != null) _btnSettings.clicked += OnSettings;
            if (_btnCredits  != null) _btnCredits.clicked  += OnCredits;
            if (_btnQuit     != null) _btnQuit.clicked     += OnQuit;

            RefreshContinueButton();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void RefreshContinueButton()
        {
            if (_btnContinue == null) return;
            bool hasSave = SaveSystem.SlotHasData(0) || SaveSystem.SlotHasData(1) || SaveSystem.SlotHasData(2);
            _btnContinue.SetEnabled(hasSave);
            if (!hasSave)
                _btnContinue.AddToClassList("menu-btn-disabled");
            else
                _btnContinue.RemoveFromClassList("menu-btn-disabled");
        }

        private static void OnContinue()
        {
            SaveSlotController.Instance?.Show();
        }

        private static void OnNewRun()
        {
            LevelLoader.GoToWorldMap();
        }

        private static void OnSettings()
        {
            SettingsPanelController.Instance?.Show();
        }

        private static void OnCredits()
        {
            CreditsScreen.Instance?.Show();
        }

        private static void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
