#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Common;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Overlay modal shown when the player clicks "Defi Quotidien".
    // Attach to a GameObject with UIDocument (same one as LevelSelectController is fine).
    // Call DailyChallengeModal.Instance.Show() from LevelSelectController.
    [RequireComponent(typeof(UIDocument))]
    public class DailyChallengeModal : MonoSingleton<DailyChallengeModal>
    {
        private VisualElement? _root;
        private Label?         _descLabel;
        private Label?         _statusLabel;
        private Button?        _acceptBtn;
        private bool           _built;

        protected override void OnAwakeSingleton() => TryBuild();

        private void Start() => TryBuild();

        private void TryBuild()
        {
            if (_built) return;
            var doc = GetComponent<UIDocument>();
            if (doc?.rootVisualElement == null) return;
            _built = true;
            BuildUI(doc.rootVisualElement);
            Hide();
        }

        private void BuildUI(VisualElement docRoot)
        {
            _root = new VisualElement();
            _root.name = "daily-challenge-overlay";
            _root.style.position        = Position.Absolute;
            _root.style.left            = 0;
            _root.style.top             = 0;
            _root.style.right           = 0;
            _root.style.bottom          = 0;
            _root.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.78f));
            _root.style.alignItems      = Align.Center;
            _root.style.justifyContent  = Justify.Center;

            var panel = new VisualElement();
            panel.style.backgroundColor         = new StyleColor(new Color(0.12f, 0.10f, 0.20f, 1f));
            panel.style.borderTopLeftRadius     = 12;
            panel.style.borderTopRightRadius    = 12;
            panel.style.borderBottomLeftRadius  = 12;
            panel.style.borderBottomRightRadius = 12;
            panel.style.paddingTop    = 32;
            panel.style.paddingBottom = 32;
            panel.style.paddingLeft   = 40;
            panel.style.paddingRight  = 40;
            panel.style.maxWidth      = 480;
            panel.style.alignItems    = Align.Center;

            var title = new Label("Defi Quotidien");
            title.style.fontSize     = 26;
            title.style.color        = new StyleColor(new Color(1f, 0.85f, 0.2f));
            title.style.marginBottom = 16;
            panel.Add(title);

            _descLabel = new Label();
            _descLabel.style.fontSize   = 15;
            _descLabel.style.color      = new StyleColor(Color.white);
            _descLabel.style.whiteSpace = WhiteSpace.Normal;
            _descLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _descLabel.style.marginBottom   = 10;
            panel.Add(_descLabel);

            _statusLabel = new Label();
            _statusLabel.style.fontSize   = 14;
            _statusLabel.style.color      = new StyleColor(new Color(0.4f, 1f, 0.4f));
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            _statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _statusLabel.style.marginBottom   = 20;
            panel.Add(_statusLabel);

            var btnRow = new VisualElement();
            btnRow.style.flexDirection  = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.Center;

            _acceptBtn = new Button();
            _acceptBtn.text = "Accepter";
            StylePrimaryBtn(_acceptBtn);
            _acceptBtn.style.marginRight = 12;
            _acceptBtn.RegisterCallback<ClickEvent>(_ => OnAccept());
            btnRow.Add(_acceptBtn);

            var cancelBtn = new Button();
            cancelBtn.text = "Annuler";
            StyleSecondaryBtn(cancelBtn);
            cancelBtn.RegisterCallback<ClickEvent>(_ => Hide());
            btnRow.Add(cancelBtn);

            panel.Add(btnRow);
            _root.Add(panel);
            docRoot.Add(_root);
        }

        public void Show()
        {
            TryBuild();
            if (_root == null) return;

            var challenge = DailyChallenge.Instance!.GetTodayChallenge();
            bool completed = DailyChallenge.Instance!.HasCompletedToday();

            if (_descLabel != null)
                _descLabel.text = challenge.Description();

            if (_statusLabel != null)
                _statusLabel.text = completed ? "Deja termine aujourd'hui !" : "";

            if (_acceptBtn != null)
                _acceptBtn.SetEnabled(!completed);

            _root.style.display = DisplayStyle.Flex;
        }

        private void Hide()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
        }

        private static void OnAccept()
        {
            var dc = DailyChallenge.Instance;
            if (dc == null) return;

            var challenge  = dc.GetTodayChallenge();
            var runContext = RunContext.Instance;

            if (runContext != null)
            {
                runContext.CoinMul = challenge.GoldMul();
                if (challenge.PerksDisabled()) runContext.SkipNextPerk = true;
                // BannedTowerId is passed via LevelLoader.NextDailyChallenge for LevelRunner to enforce.
            }

            LevelLoader.NextDailyChallenge = challenge;
            LevelLoader.LoadDailyLevel();
        }

        private static void StylePrimaryBtn(Button btn)
        {
            btn.style.backgroundColor         = new StyleColor(new Color(0.85f, 0.65f, 0.05f));
            btn.style.color                   = new StyleColor(Color.black);
            btn.style.fontSize                = 16;
            btn.style.paddingTop              = 12;
            btn.style.paddingBottom           = 12;
            btn.style.paddingLeft             = 28;
            btn.style.paddingRight            = 28;
            btn.style.borderTopLeftRadius     = 6;
            btn.style.borderTopRightRadius    = 6;
            btn.style.borderBottomLeftRadius  = 6;
            btn.style.borderBottomRightRadius = 6;
        }

        private static void StyleSecondaryBtn(Button btn)
        {
            btn.style.backgroundColor         = new StyleColor(new Color(0.25f, 0.25f, 0.25f));
            btn.style.color                   = new StyleColor(Color.white);
            btn.style.fontSize                = 16;
            btn.style.paddingTop              = 12;
            btn.style.paddingBottom           = 12;
            btn.style.paddingLeft             = 28;
            btn.style.paddingRight            = 28;
            btn.style.borderTopLeftRadius     = 6;
            btn.style.borderTopRightRadius    = 6;
            btn.style.borderBottomLeftRadius  = 6;
            btn.style.borderBottomRightRadius = 6;
        }
    }
}
