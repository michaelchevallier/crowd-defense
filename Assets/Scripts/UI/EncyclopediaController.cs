#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;

namespace CrowdDefense.UI
{
    public class EncyclopediaController : MonoBehaviour
    {
        [SerializeField] private UIDocument? uiDocument;

        private enum Tab { Towers, Enemies, Perks }
        private Tab _currentTab = Tab.Towers;
        private Button? _selectedBtn;

        public static EncyclopediaController? Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            var root = uiDocument?.rootVisualElement;
            if (root == null) return;

            root.Q<Button>("btn-close-encyclopedia")?.RegisterCallback<ClickEvent>(_ => Hide());
            root.Q<Button>("tab-towers")?.RegisterCallback<ClickEvent>(_ => SwitchTab(Tab.Towers));
            root.Q<Button>("tab-enemies")?.RegisterCallback<ClickEvent>(_ => SwitchTab(Tab.Enemies));
            root.Q<Button>("tab-perks")?.RegisterCallback<ClickEvent>(_ => SwitchTab(Tab.Perks));

            Hide();
        }

        public bool IsOpen
        {
            get
            {
                var overlay = uiDocument?.rootVisualElement?.Q<VisualElement>("encyclopedia-overlay");
                return overlay != null && !overlay.ClassListContains("hidden");
            }
        }

        public void Show()
        {
            ApplyVisible(true);
            SwitchTab(Tab.Towers);
        }

        public void Hide() => ApplyVisible(false);

        private void ApplyVisible(bool visible)
        {
            var overlay = uiDocument?.rootVisualElement?.Q<VisualElement>("encyclopedia-overlay");
            if (overlay == null) return;
            overlay.EnableInClassList("hidden", !visible);
        }

        private void SwitchTab(Tab tab)
        {
            _currentTab = tab;
            var root = uiDocument?.rootVisualElement;
            if (root == null) return;

            root.Q<Button>("tab-towers")?.EnableInClassList("enc-tab--active", tab == Tab.Towers);
            root.Q<Button>("tab-enemies")?.EnableInClassList("enc-tab--active", tab == Tab.Enemies);
            root.Q<Button>("tab-perks")?.EnableInClassList("enc-tab--active", tab == Tab.Perks);

            _selectedBtn = null;
            ClearDetail();
            BuildList();
        }

        private void BuildList()
        {
            var list = uiDocument?.rootVisualElement?.Q<ScrollView>("entry-list");
            if (list == null) return;
            list.Clear();

            switch (_currentTab)
            {
                case Tab.Towers:
                {
                    var reg = Resources.Load<TowerRegistry>("TowerRegistry");
                    if (reg != null)
                        foreach (var t in reg.Towers)
                            if (t != null) list.Add(MakeEntryBtn(t.IconEmoji + " " + (string.IsNullOrEmpty(t.DisplayName) ? t.Id : t.DisplayName), () => ShowTowerDetail(t)));
                    break;
                }
                case Tab.Enemies:
                {
                    var reg = Resources.Load<EnemyRegistry>("EnemyRegistry");
                    if (reg != null)
                        foreach (var e in reg.Enemies)
                            if (e != null) list.Add(MakeEntryBtn((string.IsNullOrEmpty(e.DisplayName) ? e.Id : e.DisplayName), () => ShowEnemyDetail(e)));
                    break;
                }
                case Tab.Perks:
                {
                    var reg = PerkRegistry.Get();
                    if (reg != null)
                    {
                        foreach (var p in reg.Standard)
                            if (p != null) list.Add(MakeEntryBtn(p.iconEmoji + " " + (string.IsNullOrEmpty(p.nameKey) ? p.id : p.nameKey), () => ShowPerkDetail(p)));
                        foreach (var p in reg.AllSchool)
                            if (p != null) list.Add(MakeEntryBtn(p.iconEmoji + " " + (string.IsNullOrEmpty(p.nameKey) ? p.id : p.nameKey), () => ShowPerkDetail(p)));
                    }
                    break;
                }
            }
        }

        private Button MakeEntryBtn(string label, System.Action onClick)
        {
            var btn = new Button();
            btn.text = label;
            btn.AddToClassList("enc-entry-btn");
            btn.clicked += () =>
            {
                _selectedBtn?.RemoveFromClassList("enc-entry-btn--selected");
                _selectedBtn = btn;
                btn.AddToClassList("enc-entry-btn--selected");
                onClick();
            };
            return btn;
        }

        private void ClearDetail()
        {
            var detail = uiDocument?.rootVisualElement?.Q<VisualElement>("entry-detail");
            if (detail == null) return;
            detail.Clear();
            var placeholder = new Label("Selectionnez une entree");
            placeholder.name = "detail-placeholder";
            placeholder.AddToClassList("enc-detail-placeholder");
            detail.Add(placeholder);
        }

        private void ShowTowerDetail(TowerType t)
        {
            var detail = uiDocument?.rootVisualElement?.Q<VisualElement>("entry-detail");
            if (detail == null) return;
            detail.Clear();

            var name = new Label(t.IconEmoji + " " + (string.IsNullOrEmpty(t.DisplayName) ? t.Id : t.DisplayName));
            name.AddToClassList("enc-detail-name");
            detail.Add(name);

            var cat = new Label("Tour  |  Cout : " + t.Cost + "c  |  Monde " + t.UnlockWorld + "+");
            cat.AddToClassList("enc-detail-category");
            detail.Add(cat);

            var block = new VisualElement();
            block.AddToClassList("enc-detail-stats-block");
            AddStatRow(block, "Degats", t.Damage.ToString("0.#"));
            AddStatRow(block, "Portee", t.Range.ToString("0.#"));
            AddStatRow(block, "Cadence", (1000f / Mathf.Max(1, t.FireRateMs)).ToString("0.##") + "/s");
            if (t.Aoe > 0f) AddStatRow(block, "AoE", t.Aoe.ToString("0.#"));
            if (t.Pierce > 0) AddStatRow(block, "Percement", t.Pierce.ToString());
            if (t.SlowMul < 1f) AddStatRow(block, "Ralentissement", ((1f - t.SlowMul) * 100f).ToString("0") + "%");
            detail.Add(block);

            var desc = new Label(t.Behavior.ToString() + " — " + t.DamageType.ToString());
            desc.AddToClassList("enc-detail-desc");
            detail.Add(desc);
        }

        private void ShowEnemyDetail(EnemyType e)
        {
            var detail = uiDocument?.rootVisualElement?.Q<VisualElement>("entry-detail");
            if (detail == null) return;
            detail.Clear();

            var name = new Label(string.IsNullOrEmpty(e.DisplayName) ? e.Id : e.DisplayName);
            name.AddToClassList("enc-detail-name");
            detail.Add(name);

            var traits = BuildEnemyTraits(e);
            var cat = new Label("Ennemi" + (string.IsNullOrEmpty(traits) ? "" : "  |  " + traits));
            cat.AddToClassList("enc-detail-category");
            detail.Add(cat);

            var block = new VisualElement();
            block.AddToClassList("enc-detail-stats-block");
            AddStatRow(block, "PV", e.Hp.ToString("0"));
            AddStatRow(block, "Vitesse", e.Speed.ToString("0.0"));
            AddStatRow(block, "Degats", e.Damage.ToString());
            AddStatRow(block, "Recompense", e.Reward + "c");
            if (e.ShieldHP > 0f) AddStatRow(block, "Bouclier", e.ShieldHP.ToString("0"));
            detail.Add(block);
        }

        private void ShowPerkDetail(PerkDef p)
        {
            var detail = uiDocument?.rootVisualElement?.Q<VisualElement>("entry-detail");
            if (detail == null) return;
            detail.Clear();

            var name = new Label(p.iconEmoji + " " + (string.IsNullOrEmpty(p.nameKey) ? p.id : p.nameKey));
            name.AddToClassList("enc-detail-name");
            detail.Add(name);

            var rarityStr = p.rarity.ToString();
            var catStr = p.category.ToString();
            var cat = new Label(rarityStr + "  |  " + catStr + (string.IsNullOrEmpty(p.school) ? "" : "  |  " + p.school));
            cat.AddToClassList("enc-detail-category");
            detail.Add(cat);

            if (!string.IsNullOrEmpty(p.descKey))
            {
                var desc = new Label(p.descKey);
                desc.AddToClassList("enc-detail-desc");
                detail.Add(desc);
            }

            var block = new VisualElement();
            block.AddToClassList("enc-detail-stats-block");
            if (p.damage != 0f)        AddStatRow(block, "Degats", FormatMul(p.damage));
            if (p.range != 0f)         AddStatRow(block, "Portee", FormatMul(p.range));
            if (p.fireRate != 0f)      AddStatRow(block, "Cadence", FormatMul(p.fireRate));
            if (p.coinGain != 0f)      AddStatRow(block, "Gain or", FormatMul(p.coinGain));
            if (p.critChance != 0f)    AddStatRow(block, "Crit chance", (p.critChance * 100f).ToString("0") + "%");
            if (p.stackable)           AddStatRow(block, "Stackable", "oui (max " + p.maxStacks + ")");
            detail.Add(block);
        }

        private static void AddStatRow(VisualElement parent, string label, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("enc-detail-stat-row");
            var lbl = new Label(label);
            lbl.AddToClassList("enc-detail-stat-label");
            var val = new Label(value);
            val.AddToClassList("enc-detail-stat-value");
            row.Add(lbl);
            row.Add(val);
            parent.Add(row);
        }

        private static string FormatMul(float v) =>
            v >= 0 ? "+" + (v * 100f).ToString("0") + "%" : (v * 100f).ToString("0") + "%";

        private static string BuildEnemyTraits(EnemyType e)
        {
            var tags = new System.Collections.Generic.List<string>(4);
            if (e.IsBoss || e.IsMidBoss || e.IsApocalypseBoss) tags.Add("Boss");
            if (e.IsFlyer)    tags.Add("Volant");
            if (e.IsStealth)  tags.Add("Furtif");
            if (e.ShieldHP > 0f) tags.Add("Bouclier");
            if (e.SummonsMinions) tags.Add("Invocateur");
            if (e.IsBrigand)  tags.Add("Charge");
            return string.Join(" | ", tags);
        }
    }
}
