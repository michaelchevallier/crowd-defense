#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Simple 3-column talent panel wired to a UIDocument.
    // Each column shows talent name, current level/5, % bonus, and an Upgrade button.
    // The panel is shown/hidden by MenuController via Show()/Hide().
    [RequireComponent(typeof(UIDocument))]
    public class TalentPanelController : UIControllerBase
    {
        public static TalentPanelController? Instance { get; private set; }

        private Label?  _lblPoints;

        // Per-column state
        private Label?  _lblTowerLvl;
        private Label?  _lblTowerPct;
        private Button? _btnTower;

        private Label?  _lblHeroLvl;
        private Label?  _lblHeroPct;
        private Button? _btnHero;

        private Label?  _lblGoldLvl;
        private Label?  _lblGoldPct;
        private Button? _btnGold;

        private Button? _btnClose;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            _lblPoints   = Root?.Q<Label>("lbl-talent-points");
            _lblTowerLvl = Root?.Q<Label>("lbl-tower-lvl");
            _lblTowerPct = Root?.Q<Label>("lbl-tower-pct");
            _btnTower    = Root?.Q<Button>("btn-upgrade-tower");
            _lblHeroLvl  = Root?.Q<Label>("lbl-hero-lvl");
            _lblHeroPct  = Root?.Q<Label>("lbl-hero-pct");
            _btnHero     = Root?.Q<Button>("btn-upgrade-hero");
            _lblGoldLvl  = Root?.Q<Label>("lbl-gold-lvl");
            _lblGoldPct  = Root?.Q<Label>("lbl-gold-pct");
            _btnGold     = Root?.Q<Button>("btn-upgrade-gold");
            _btnClose    = Root?.Q<Button>("btn-talent-close");

            if (_btnTower != null) _btnTower.clicked += () => OnUpgrade(TalentSystem.Tower);
            if (_btnHero  != null) _btnHero.clicked  += () => OnUpgrade(TalentSystem.Hero);
            if (_btnGold  != null) _btnGold.clicked  += () => OnUpgrade(TalentSystem.Gold);
            if (_btnClose != null) _btnClose.clicked += Hide;

            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show()
        {
            if (_root == null) return;
            _root.style.display = DisplayStyle.Flex;
            Refresh();
        }

        public void Hide()
        {
            if (_root == null) return;
            _root.style.display = DisplayStyle.None;
        }

        private void OnUpgrade(string key)
        {
            TalentSystem.TryUpgrade(key);
            Refresh();
        }

        private void Refresh()
        {
            int pts = TalentSystem.AvailablePoints;
            if (_lblPoints != null) _lblPoints.text = $"Points disponibles : {pts}";

            SetColumn(_lblTowerLvl, _lblTowerPct, _btnTower,
                "Tour Damage", TalentSystem.TowerDamageLevel, TalentSystem.Tower);
            SetColumn(_lblHeroLvl, _lblHeroPct, _btnHero,
                "Hero Power", TalentSystem.HeroPowerLevel, TalentSystem.Hero);
            SetColumn(_lblGoldLvl, _lblGoldPct, _btnGold,
                "Gold Income", TalentSystem.GoldIncomeLevel, TalentSystem.Gold);
        }

        private static void SetColumn(Label? lblLvl, Label? lblPct, Button? btn,
            string name, int lvl, string key)
        {
            if (lblLvl != null) lblLvl.text = $"{name} : Niv {lvl}/{TalentSystem.MaxLevel}";
            if (lblPct != null) lblPct.text = lvl == 0 ? "+0%" : $"+{lvl * 5}%";
            if (btn    != null)
            {
                btn.SetEnabled(TalentSystem.CanUpgrade(key));
                btn.text = lvl >= TalentSystem.MaxLevel ? "Max" : "Ameliorer";
            }
        }
    }
}
