#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class LevelSelectController : MonoBehaviour
    {
        private const int WorldCount     = 8;
        private const int LevelsPerWorld = 10;

        private VisualElement? _root;

        private void OnDestroy() =>
            PlayerProfile.OnNameChanged -= OnPlayerNameChanged;

        private void OnPlayerNameChanged(string _) => RefreshGreeting(_root!);

        private void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            var root = _root;
            PlayerProfile.OnNameChanged += OnPlayerNameChanged;

            var titleLabel = root.Q<Label>("menu-title-label");
            if (titleLabel != null) titleLabel.text = L.Get("menu.game_title");

            RefreshGreeting(root);

            var btnSlots = root.Q<Button>("btn-open-slots");
            if (btnSlots != null)
                btnSlots.clicked += () => SaveSlotController.Instance?.Show();

            var btnBestiary = root.Q<Button>("btn-open-bestiary");
            if (btnBestiary != null)
                btnBestiary.clicked += () => BestiaryPanel.Instance?.Show();

            var btnAchievements = root.Q<Button>("btn-open-achievements");
            if (btnAchievements != null)
                btnAchievements.clicked += () => AchievementsPanel.Instance?.Show();

            var btnCredits = root.Q<Button>("btn-open-credits");
            if (btnCredits != null)
                btnCredits.clicked += () => CreditsScreen.Instance?.Show();

            var btnStats = root.Q<Button>("btn-open-stats");
            if (btnStats != null)
                btnStats.clicked += () => StatsLifetimePanel.Instance?.Show();

            var versionFooter = root.Q<Label>("menu-version-footer");
            if (versionFooter != null)
            {
                string buildHash = string.IsNullOrEmpty(Application.version) ? "dev" : Application.version;
                versionFooter.text = $"Crowd Defense v6.0 — Unity {Application.unityVersion} — {buildHash}";
            }

            var grid = root.Q<VisualElement>("level-grid");
            if (grid == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[LevelSelectController] level-grid VisualElement not found in UXML.");
#endif
                return;
            }

            AddContinueRow(grid);
            AddEndlessRow(grid);
            AddDailyRow(grid);

            var registry = LevelRegistry.Get();

            for (int w = 1; w <= WorldCount; w++)
            {
                var row = new VisualElement();
                row.AddToClassList("world-row");

                var worldLabel = new Label(L.Get("menu.world_label", w));
                worldLabel.AddToClassList("world-label");
                row.Add(worldLabel);

                for (int l = 1; l <= LevelsPerWorld; l++)
                {
                    string levelId = $"world{w}-{l}";
                    bool unlocked = SaveSystem.IsLevelUnlocked(levelId);
                    bool cleared  = SaveSystem.IsLevelCleared(levelId);

                    var btn = new Button();
                    btn.text = l.ToString();
                    btn.AddToClassList("level-btn");

                    if (cleared)        btn.AddToClassList("cleared");
                    else if (!unlocked) btn.AddToClassList("locked");

                    if (unlocked)
                    {
                        string id = levelId;
                        LevelData? levelData = registry?.FindById(id);
                        if (levelData != null)
                        {
                            btn.RegisterCallback<ClickEvent>(_ => ShowPreviewModal(root, id, levelData));
                            AttachMiniMapPreview(btn, levelData);
                        }
                        else
                            btn.SetEnabled(false);
                    }
                    else
                    {
                        btn.SetEnabled(false);
                    }

                    row.Add(btn);
                }

                grid.Add(row);
            }
        }

        private static void RefreshGreeting(VisualElement root)
        {
            var greetingLabel = root.Q<Label>("menu-greeting-label");
            if (greetingLabel == null) return;
            string name = PlayerProfile.Instance?.GetName() ?? "Joueur";
            greetingLabel.text = $"Bonjour, {name}";
        }

        private static void AddContinueRow(VisualElement grid)
        {
            if (!SaveSystem.HasRunState()) return;

            var mid = SaveSystem.LoadRunState();
            if (mid == null || string.IsNullOrEmpty(mid.levelId)) return;

            string display = $"{mid.levelId} V{mid.waveIdx}";
            var row = new VisualElement();
            row.AddToClassList("world-row");

            var btn = new Button();
            btn.text = $"Continuer ({display})";
            btn.AddToClassList("level-btn");
            btn.AddToClassList("continue-btn");

            string savedLevelId = mid.levelId;
            btn.RegisterCallback<ClickEvent>(_ => LevelLoader.LoadLevel(savedLevelId));
            row.Add(btn);

            grid.Add(row);
        }

        private static void OnLevelClicked(string levelId)
        {
            if (DifficultySelector.Instance != null)
                DifficultySelector.Instance.Show(() => LevelLoader.LoadLevel(levelId));
            else
                LevelLoader.LoadLevel(levelId);
        }

        private static void ShowPreviewModal(VisualElement root, string levelId, LevelData levelData)
        {
            // Remove any existing modal first
            var existing = root.Q<VisualElement>("preview-modal-overlay");
            existing?.RemoveFromHierarchy();

            // Full-screen overlay
            var overlay = new VisualElement();
            overlay.name = "preview-modal-overlay";
            overlay.style.position           = Position.Absolute;
            overlay.style.left               = 0;
            overlay.style.top                = 0;
            overlay.style.right              = 0;
            overlay.style.bottom             = 0;
            overlay.style.backgroundColor    = new StyleColor(new Color(0f, 0f, 0f, 0.75f));
            overlay.style.alignItems         = Align.Center;
            overlay.style.justifyContent     = Justify.Center;

            // Modal card
            var card = new VisualElement();
            card.style.backgroundColor    = new StyleColor(new Color(0.12f, 0.12f, 0.18f, 0.98f));
            card.style.borderTopLeftRadius     = 8;
            card.style.borderTopRightRadius    = 8;
            card.style.borderBottomLeftRadius  = 8;
            card.style.borderBottomRightRadius = 8;
            card.style.paddingTop    = 20;
            card.style.paddingBottom = 20;
            card.style.paddingLeft   = 24;
            card.style.paddingRight  = 24;
            card.style.alignItems    = Align.Center;
            card.style.minWidth      = 320;

            // Title
            var title = new Label(levelId);
            title.style.fontSize      = 22;
            title.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            title.style.color         = new StyleColor(Color.white);
            title.style.marginBottom  = 12;
            card.Add(title);

            // 256x256 map preview
            var map = BuildMiniMap(levelData, 256);
            map.style.marginBottom = 14;
            card.Add(map);

            // Level metadata
            string waves = levelData.Waves.Count > 0 ? $"{levelData.Waves.Count} vagues" : "";
            if (!string.IsNullOrEmpty(waves))
            {
                var info = new Label(waves);
                info.style.color        = new StyleColor(new Color(0.75f, 0.75f, 0.75f));
                info.style.fontSize     = 14;
                info.style.marginBottom = 12;
                card.Add(info);
            }

            // Difficulty selector row
            card.Add(BuildDifficultyRow());

            // Button row
            var btnRow = new VisualElement();
            btnRow.style.flexDirection  = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.Center;
            btnRow.style.marginTop      = 4;

            var btnClose = new Button();
            btnClose.text = "Fermer";
            btnClose.AddToClassList("level-btn");
            btnClose.style.marginRight = 12;
            btnClose.RegisterCallback<ClickEvent>(_ => overlay.RemoveFromHierarchy());
            btnRow.Add(btnClose);

            var btnPlay = new Button();
            btnPlay.text = "Jouer";
            btnPlay.AddToClassList("level-btn");
            btnPlay.AddToClassList("cleared");
            string id = levelId;
            btnPlay.RegisterCallback<ClickEvent>(_ =>
            {
                overlay.RemoveFromHierarchy();
                OnLevelClicked(id);
            });
            btnRow.Add(btnPlay);

            card.Add(btnRow);
            overlay.Add(card);

            // Click outside to dismiss
            overlay.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == overlay)
                    overlay.RemoveFromHierarchy();
            });

            root.Add(overlay);
        }

        private static VisualElement BuildDifficultyRow()
        {
            var container = new VisualElement();
            container.style.flexDirection   = FlexDirection.Column;
            container.style.alignItems      = Align.Center;
            container.style.marginTop       = 10;
            container.style.marginBottom    = 6;

            var label = new Label("DIFFICULTE");
            label.style.fontSize    = 12;
            label.style.color       = new StyleColor(new Color(0.65f, 0.65f, 0.65f));
            label.style.marginBottom = 6;
            container.Add(label);

            var row = new VisualElement();
            row.style.flexDirection  = FlexDirection.Row;
            row.style.justifyContent = Justify.Center;

            int savedIndex = PlayerPrefs.GetInt(DifficultySelector.PrefKey, (int)Difficulty.Normal);
            var btns = new Button[DifficultySelector.Options.Length];

            for (int i = 0; i < DifficultySelector.Options.Length; i++)
            {
                var (diff, diffLabel, bg) = DifficultySelector.Options[i];
                var btn = new Button();
                // Short name only (first line before \n)
                int nl = diffLabel.IndexOf('\n');
                btn.text = nl > 0 ? diffLabel.Substring(0, nl) : diffLabel;
                btn.style.marginLeft  = 4;
                btn.style.marginRight = 4;
                btn.style.paddingTop    = 6;
                btn.style.paddingBottom = 6;
                btn.style.paddingLeft   = 10;
                btn.style.paddingRight  = 10;
                btn.style.fontSize      = 13;
                btn.style.borderTopLeftRadius     = 5;
                btn.style.borderTopRightRadius    = 5;
                btn.style.borderBottomLeftRadius  = 5;
                btn.style.borderBottomRightRadius = 5;
                bool sel = (int)diff == savedIndex;
                ApplyDiffBtnStyle(btn, bg, sel);

                var capturedDiff = diff;
                var capturedBg   = bg;
                var capturedBtns = btns;
                btn.RegisterCallback<ClickEvent>(_ =>
                {
                    PlayerPrefs.SetInt(DifficultySelector.PrefKey, (int)capturedDiff);
                    PlayerPrefs.Save();
                    for (int j = 0; j < capturedBtns.Length; j++)
                        if (capturedBtns[j] != null)
                            ApplyDiffBtnStyle(capturedBtns[j], DifficultySelector.Options[j].bg,
                                (int)DifficultySelector.Options[j].diff == (int)capturedDiff);
                });
                btns[i] = btn;
                row.Add(btn);
            }

            container.Add(row);
            return container;
        }

        private static void ApplyDiffBtnStyle(Button btn, Color bg, bool selected)
        {
            btn.style.backgroundColor = new StyleColor(bg);
            btn.style.color           = new StyleColor(Color.white);
            float bw = selected ? 2.5f : 1f;
            btn.style.borderTopWidth    = bw;
            btn.style.borderBottomWidth = bw;
            btn.style.borderLeftWidth   = bw;
            btn.style.borderRightWidth  = bw;
            var bc = selected ? Color.white : new Color(1f, 1f, 1f, 0.2f);
            btn.style.borderTopColor    = new StyleColor(bc);
            btn.style.borderBottomColor = new StyleColor(bc);
            btn.style.borderLeftColor   = new StyleColor(bc);
            btn.style.borderRightColor  = new StyleColor(bc);
            btn.style.scale = selected
                ? new StyleScale(new Scale(new Vector2(1.05f, 1.05f)))
                : new StyleScale(new Scale(Vector2.one));
        }

        private static void AddEndlessRow(VisualElement grid)
        {
            var row = new VisualElement();
            row.AddToClassList("world-row");

            var label = new Label("Sans Fin");
            label.AddToClassList("world-label");
            row.Add(label);

            var btn = new Button();
            int best = EndlessMode.Instance?.BestWave ?? 0;
            btn.text = best > 0 ? $"Inf. Sans Fin  (Record : vague {best})" : "Inf. Sans Fin";
            btn.AddToClassList("level-btn");
            btn.AddToClassList("endless-btn");
            btn.RegisterCallback<ClickEvent>(_ => EndlessMode.Instance?.StartEndless());
            row.Add(btn);

            grid.Add(row);
        }

        private static void AttachMiniMapPreview(VisualElement anchor, LevelData levelData)
        {
            var preview = BuildMiniMap(levelData, 64);
            preview.style.position = Position.Absolute;
            preview.style.bottom = new StyleLength(new Length(105, LengthUnit.Percent));
            preview.style.left = 0;
            preview.style.display = DisplayStyle.None;
            preview.pickingMode = PickingMode.Ignore;
            anchor.Add(preview);
            preview.BringToFront();

            anchor.RegisterCallback<MouseEnterEvent>(_ => preview.style.display = DisplayStyle.Flex);
            anchor.RegisterCallback<MouseLeaveEvent>(_ => preview.style.display = DisplayStyle.None);
        }

        private static VisualElement BuildMiniMap(LevelData levelData, int previewSize)
        {
            var rows = levelData.MapRows;
            int rowCount = rows.Count;
            int colCount = 0;
            for (int r = 0; r < rowCount; r++)
                if (rows[r].Length > colCount) colCount = rows[r].Length;
            if (rowCount == 0 || colCount == 0) rowCount = colCount = 1;

            float cellW = previewSize / (float)colCount;
            float cellH = previewSize / (float)rowCount;

            var container = new VisualElement();
            container.style.width  = previewSize;
            container.style.height = previewSize;
            container.style.flexDirection = FlexDirection.Column;
            container.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 0.9f));
            container.style.borderTopLeftRadius     = 3;
            container.style.borderTopRightRadius    = 3;
            container.style.borderBottomLeftRadius  = 3;
            container.style.borderBottomRightRadius = 3;
            container.style.overflow = Overflow.Hidden;

            for (int r = 0; r < rowCount; r++)
            {
                string rowStr = r < rows.Count ? rows[r] : "";
                var rowEl = new VisualElement();
                rowEl.style.flexDirection = FlexDirection.Row;
                rowEl.style.height = cellH;

                for (int c = 0; c < colCount; c++)
                {
                    char ch = c < rowStr.Length ? rowStr[c] : '0';
                    Color color = CellColor(ch);

                    var cell = new VisualElement();
                    cell.style.width           = cellW;
                    cell.style.height          = cellH;
                    cell.style.backgroundColor = new StyleColor(color);
                    rowEl.Add(cell);
                }
                container.Add(rowEl);
            }

            return container;
        }

        private static Color CellColor(char ch) => ch switch
        {
            '1'  => new Color(0.15f, 0.15f, 0.15f), // wall
            'P'  => new Color(0.95f, 0.85f, 0.20f), // path
            'C'  => new Color(0.90f, 0.20f, 0.20f), // castle
            'L'  => new Color(0.20f, 0.80f, 0.30f), // portal/entry
            'T'  => new Color(0.20f, 0.55f, 0.90f), // tower slot
            'W'  => new Color(0.30f, 0.55f, 0.80f), // water
            'M'  => new Color(0.45f, 0.35f, 0.20f), // mountain
            _    => new Color(0.55f, 0.55f, 0.55f), // floor / unknown
        };

        private static void AddDailyRow(VisualElement grid)
        {
            var row = new VisualElement();
            row.AddToClassList("world-row");

            var label = new Label(L.Get("daily.title"));
            label.AddToClassList("world-label");
            row.Add(label);

            var btn = new Button();
            bool playedToday  = Daily.HasPlayedToday();
            bool doneChallenge = DailyChallenge.Instance?.HasCompletedToday() ?? false;
            int  bestScore    = Daily.GetStoredScore();
            string sub = (playedToday || doneChallenge)
                ? $"{L.Get("daily.played_today")} ({L.Get("daily.best_score_label")}: {bestScore})"
                : "Defi Quotidien";
            btn.text = sub;
            btn.AddToClassList("level-btn");
            if (playedToday || doneChallenge) btn.AddToClassList("cleared");

            btn.RegisterCallback<ClickEvent>(_ => DailyChallengeModal.Instance?.Show());
            row.Add(btn);

            grid.Add(row);
        }
    }
}
