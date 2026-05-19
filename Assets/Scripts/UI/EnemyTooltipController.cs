#nullable enable
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Entities;

namespace CrowdDefense.UI
{
    // Affiche un tooltip compact (hover enemy) dans le panneau enemy-tooltip du HUD.
    // Pilote par EnemyHoverController via Show/Hide.
    // Partage le UIDocument de HudController (sibling component, meme GameObject).
    public class EnemyTooltipController : UIControllerBase
    {
        public static EnemyTooltipController? Instance { get; private set; }

        private const float OffsetX = 14f;
        private const float OffsetY = 14f;
        private const float TooltipWidth = 220f;

        private VisualElement? _root;
        private Label? _labelName;
        private Label? _labelStats;
        private Label? _labelSpecial;

        private bool _visible;
        private readonly StringBuilder _sb = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (!ResolveUI())
            {
                var doc = FindAnyObjectByType<UIDocument>();
                if (doc == null) return;
                var r = doc.rootVisualElement;
                _root        = r.Q<VisualElement>("enemy-tooltip");
                _labelName   = r.Q<Label>("enemy-tooltip-name");
                _labelStats  = r.Q<Label>("enemy-tooltip-stats");
                _labelSpecial = r.Q<Label>("enemy-tooltip-special");
            }
            Hide();
        }

        protected override void OnUIReady()
        {
            _root        = Root?.Q<VisualElement>("enemy-tooltip");
            _labelName   = Root?.Q<Label>("enemy-tooltip-name");
            _labelStats  = Root?.Q<Label>("enemy-tooltip-stats");
            _labelSpecial = Root?.Q<Label>("enemy-tooltip-special");
        }

        private void Update()
        {
            if (!_visible || _root == null) return;

            var mp = Input.mousePosition;
            float uiX = mp.x + OffsetX;
            float uiY = Screen.height - mp.y + OffsetY;

            if (uiX + TooltipWidth > Screen.width) uiX = mp.x - TooltipWidth - OffsetX;

            _root.style.left = new Length(uiX, LengthUnit.Pixel);
            _root.style.top  = new Length(uiY, LengthUnit.Pixel);
        }

        public void Show(Enemy enemy)
        {
            if (_root == null) return;
            Populate(enemy);
            _root.RemoveFromClassList("hidden");
            _visible = true;
        }

        public void Hide()
        {
            _root?.AddToClassList("hidden");
            _visible = false;
        }

        private void Populate(Enemy enemy)
        {
            var cfg = enemy.Config;
            if (cfg == null) return;

            // Name
            _sb.Clear();
            string name = string.IsNullOrEmpty(cfg.DisplayName) ? cfg.Id : cfg.DisplayName;
            if (cfg.IsBoss || cfg.IsMidBoss)
            {
                _sb.Append("BOSS ");
            }
            _sb.Append(name);
            if (_labelName != null) _labelName.text = _sb.ToString();

            // Stats
            _sb.Clear();
            _sb.Append("HP: ");
            _sb.Append(enemy.CurrentHp.ToString("F0"));
            _sb.Append(" / ");
            _sb.Append(enemy.MaxHp.ToString("F0"));
            _sb.Append('\n');
            _sb.Append("Vitesse: ");
            _sb.Append(cfg.Speed.ToString("F1"));
            _sb.Append('\n');
            _sb.Append("Récompense : ");
            _sb.Append(cfg.Reward);
            _sb.Append('c');
            if (_labelStats != null) _labelStats.text = _sb.ToString();

            // Special abilities
            _sb.Clear();
            if (cfg.IsBoss || cfg.IsMidBoss)       _sb.Append("BOSS\n");
            if (cfg.IsApocalypseBoss)               _sb.Append("Apocalypse\n");
            if (cfg.IsFlyer)                        _sb.Append("Volant\n");
            if (cfg.IsStealth)                      _sb.Append("Furtif\n");
            if (cfg.ShieldHP > 0f)                  _sb.Append("Bouclier\n");
            if (cfg.IsBrigand)                      _sb.Append("Charge\n");
            if (cfg.IsCorsair)                      _sb.Append("Corsaire\n");
            if (cfg.IsFiery)                        _sb.Append("Enflamme\n");
            if (cfg.SummonsMinions)                 _sb.Append("Invocateur\n");

            string special = _sb.ToString().TrimEnd('\n');
            if (_labelSpecial != null) _labelSpecial.text = special;
            if (_root != null)
            {
                if (string.IsNullOrEmpty(special))
                    _labelSpecial?.AddToClassList("hidden");
                else
                    _labelSpecial?.RemoveFromClassList("hidden");
            }
        }
    }
}
