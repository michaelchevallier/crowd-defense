#nullable enable
using CrowdDefense.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class StatisticsController : MonoBehaviour
    {
        private VisualElement? _root;
        private VisualElement? _statsRoot;

        private Button? _tabRunBtn;
        private Button? _tabLifetimeBtn;
        private VisualElement? _tabRunContent;
        private VisualElement? _tabLifetimeContent;

        // Run stat labels
        private Label? _noRunLabel;
        private VisualElement? _runStatsList;
        private Label? _runKillsLabel;
        private Label? _runKillsValue;
        private Label? _runGoldLabel;
        private Label? _runGoldValue;
        private Label? _runWavesLabel;
        private Label? _runWavesValue;
        private Label? _runStreakLabel;
        private Label? _runStreakValue;
        private Label? _runPlaytimeLabel;
        private Label? _runPlaytimeValue;
        private Label? _runTowersLabel;
        private Label? _runTowersValue;
        private Label? _runPerksLabel;
        private Label? _runPerksValue;

        // Lifetime stat labels
        private Label? _ltKillsLabel;
        private Label? _ltKillsValue;
        private Label? _ltGoldLabel;
        private Label? _ltGoldValue;
        private Label? _ltWavesLabel;
        private Label? _ltWavesValue;
        private Label? _ltStreakLabel;
        private Label? _ltStreakValue;
        private Label? _ltPlaytimeLabel;
        private Label? _ltPlaytimeValue;
        private Label? _ltTowersLabel;
        private Label? _ltTowersValue;
        private Label? _ltPerksLabel;
        private Label? _ltPerksValue;

        private Button?        _resetBtn;
        private VisualElement? _resetDialog;
        private Label?         _resetConfirmLabel;
        private Button?        _resetYesBtn;
        private Button?        _resetNoBtn;

        private Label?  _titleLabel;
        private Button? _closeBtn;

        private bool _showingRun = true;

        private void Start()
        {
            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null)
            {
                Debug.LogError("[StatisticsController] UIDocument not found");
                return;
            }
            _root = uiDoc.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("[StatisticsController] rootVisualElement is null — UXML failed to load");
                return;
            }
            _statsRoot = _root.Q<VisualElement>("stats-root");

            _titleLabel = _root.Q<Label>("stats-title");
            _closeBtn   = _root.Q<Button>("stats-close-btn");

            _tabRunBtn      = _root.Q<Button>("tab-run-btn");
            _tabLifetimeBtn = _root.Q<Button>("tab-lifetime-btn");
            _tabRunContent      = _root.Q<VisualElement>("tab-run-content");
            _tabLifetimeContent = _root.Q<VisualElement>("tab-lifetime-content");

            _noRunLabel   = _root.Q<Label>("stats-no-run-label");
            _runStatsList = _root.Q<VisualElement>("run-stats-list");

            _runKillsLabel    = _root.Q<Label>("run-kills-label");
            _runKillsValue    = _root.Q<Label>("run-kills-value");
            _runGoldLabel     = _root.Q<Label>("run-gold-label");
            _runGoldValue     = _root.Q<Label>("run-gold-value");
            _runWavesLabel    = _root.Q<Label>("run-waves-label");
            _runWavesValue    = _root.Q<Label>("run-waves-value");
            _runStreakLabel   = _root.Q<Label>("run-streak-label");
            _runStreakValue   = _root.Q<Label>("run-streak-value");
            _runPlaytimeLabel = _root.Q<Label>("run-playtime-label");
            _runPlaytimeValue = _root.Q<Label>("run-playtime-value");
            _runTowersLabel   = _root.Q<Label>("run-towers-label");
            _runTowersValue   = _root.Q<Label>("run-towers-value");
            _runPerksLabel    = _root.Q<Label>("run-perks-label");
            _runPerksValue    = _root.Q<Label>("run-perks-value");

            _ltKillsLabel    = _root.Q<Label>("lt-kills-label");
            _ltKillsValue    = _root.Q<Label>("lt-kills-value");
            _ltGoldLabel     = _root.Q<Label>("lt-gold-label");
            _ltGoldValue     = _root.Q<Label>("lt-gold-value");
            _ltWavesLabel    = _root.Q<Label>("lt-waves-label");
            _ltWavesValue    = _root.Q<Label>("lt-waves-value");
            _ltStreakLabel   = _root.Q<Label>("lt-streak-label");
            _ltStreakValue   = _root.Q<Label>("lt-streak-value");
            _ltPlaytimeLabel = _root.Q<Label>("lt-playtime-label");
            _ltPlaytimeValue = _root.Q<Label>("lt-playtime-value");
            _ltTowersLabel   = _root.Q<Label>("lt-towers-label");
            _ltTowersValue   = _root.Q<Label>("lt-towers-value");
            _ltPerksLabel    = _root.Q<Label>("lt-perks-label");
            _ltPerksValue    = _root.Q<Label>("lt-perks-value");

            _resetBtn           = _root.Q<Button>("stats-reset-btn");
            _resetDialog        = _root.Q<VisualElement>("stats-reset-dialog");
            _resetConfirmLabel  = _root.Q<Label>("stats-reset-confirm-label");
            _resetYesBtn        = _root.Q<Button>("stats-reset-yes-btn");
            _resetNoBtn         = _root.Q<Button>("stats-reset-no-btn");

            _tabRunBtn?.RegisterCallback<ClickEvent>(_ => SwitchTab(true));
            _tabLifetimeBtn?.RegisterCallback<ClickEvent>(_ => SwitchTab(false));
            _closeBtn?.RegisterCallback<ClickEvent>(_ => Hide());
            _resetBtn?.RegisterCallback<ClickEvent>(_ => ShowResetDialog());
            _resetYesBtn?.RegisterCallback<ClickEvent>(_ => ConfirmReset());
            _resetNoBtn?.RegisterCallback<ClickEvent>(_ => HideResetDialog());

            L.OnLocaleChanged += RefreshAll;
        }

        private void OnDestroy() => L.OnLocaleChanged -= RefreshAll;

        public void Show()
        {
            RefreshAll();
            SwitchTab(true);
            _statsRoot?.RemoveFromClassList("hidden");
        }

        public void Hide() => _statsRoot?.AddToClassList("hidden");

        private void SwitchTab(bool showRun)
        {
            _showingRun = showRun;
            if (_tabRunContent != null)
            {
                if (showRun) _tabRunContent.RemoveFromClassList("hidden");
                else _tabRunContent.AddToClassList("hidden");
            }
            if (_tabLifetimeContent != null)
            {
                if (!showRun) _tabLifetimeContent.RemoveFromClassList("hidden");
                else _tabLifetimeContent.AddToClassList("hidden");
            }
            _tabRunBtn?.EnableInClassList("tab-btn-active", showRun);
            _tabLifetimeBtn?.EnableInClassList("tab-btn-active", !showRun);

            if (showRun)  PopulateRunTab();
            else          PopulateLifetimeTab();
        }

        private void RefreshAll()
        {
            ApplyLocalizedLabels();
            if (_showingRun) PopulateRunTab();
            else             PopulateLifetimeTab();
        }

        private void ApplyLocalizedLabels()
        {
            if (_titleLabel != null)        _titleLabel.text        = L.Get("stats.title");
            if (_closeBtn   != null)        _closeBtn.text          = L.Get("stats.close");
            if (_tabRunBtn  != null)        _tabRunBtn.text         = L.Get("stats.tab_run");
            if (_tabLifetimeBtn != null)    _tabLifetimeBtn.text    = L.Get("stats.tab_lifetime");
            if (_noRunLabel != null)        _noRunLabel.text        = L.Get("stats.no_run");
            if (_resetBtn != null)          _resetBtn.text          = L.Get("stats.reset_btn");
            if (_resetConfirmLabel != null) _resetConfirmLabel.text = L.Get("stats.reset_confirm");
            if (_resetYesBtn != null)       _resetYesBtn.text       = L.Get("stats.reset_yes");
            if (_resetNoBtn  != null)       _resetNoBtn.text        = L.Get("stats.reset_no");

            SetLabel(_runKillsLabel,    "stats.kills");
            SetLabel(_runGoldLabel,     "stats.gold");
            SetLabel(_runWavesLabel,    "stats.waves");
            SetLabel(_runStreakLabel,   "stats.streak");
            SetLabel(_runPlaytimeLabel, "stats.playtime");
            SetLabel(_runTowersLabel,   "stats.towers");
            SetLabel(_runPerksLabel,    "stats.perks");

            SetLabel(_ltKillsLabel,    "stats.kills");
            SetLabel(_ltGoldLabel,     "stats.gold");
            SetLabel(_ltWavesLabel,    "stats.waves");
            SetLabel(_ltStreakLabel,   "stats.streak");
            SetLabel(_ltPlaytimeLabel, "stats.playtime");
            SetLabel(_ltTowersLabel,   "stats.towers");
            SetLabel(_ltPerksLabel,    "stats.perks");
        }

        private static void SetLabel(Label? label, string key)
        {
            if (label != null) label.text = L.Get(key);
        }

        private void PopulateRunTab()
        {
            var rs = SaveSystem.GetRunState();
            bool hasRun = rs.heroLevel > 1 || rs.runKills > 0 || rs.runWavesCleared > 0;

            if (_noRunLabel   != null) _noRunLabel.style.display   = hasRun ? DisplayStyle.None : DisplayStyle.Flex;
            if (_runStatsList != null) _runStatsList.style.display = hasRun ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasRun) return;

            SetValue(_runKillsValue,    rs.runKills.ToString());
            SetValue(_runGoldValue,     rs.runGoldEarned.ToString());
            SetValue(_runWavesValue,    rs.runWavesCleared.ToString());
            SetValue(_runStreakValue,   rs.runStreak.ToString());
            SetValue(_runPlaytimeValue, FormatPlaytime(rs.runPlaytime));
            SetValue(_runTowersValue,   rs.runTowersPlaced.ToString());
            SetValue(_runPerksValue,    rs.runPerksAcquired.ToString());
        }

        private void PopulateLifetimeTab()
        {
            var data = SaveSystem.Load();

            SetValue(_ltKillsValue,    data.totalKills.ToString());
            SetValue(_ltGoldValue,     data.totalGoldEarned.ToString());
            SetValue(_ltWavesValue,    data.totalWavesCleared.ToString());
            SetValue(_ltStreakValue,   data.bestStreak.ToString());
            SetValue(_ltPlaytimeValue, FormatPlaytime(data.playtime));
            SetValue(_ltTowersValue,   data.towersPlaced.ToString());
            SetValue(_ltPerksValue,    data.perksAcquired.ToString());
        }

        private static void SetValue(Label? label, string value)
        {
            if (label != null) label.text = value;
        }

        private string FormatPlaytime(float seconds)
        {
            int total = Mathf.RoundToInt(seconds);
            int hours = total / 3600;
            int mins  = (total % 3600) / 60;
            return L.Get("stats.playtime_fmt", hours, mins);
        }

        private void ShowResetDialog()  => _resetDialog?.RemoveFromClassList("hidden");
        private void HideResetDialog()  => _resetDialog?.AddToClassList("hidden");

        private void ConfirmReset()
        {
            SaveSystem.ResetLifetimeStats();
            HideResetDialog();
            PopulateLifetimeTab();
        }
    }
}
