#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class WorldMapController : MonoBehaviour
    {
        // ── RunMap node-graph icons (ASCII, no special chars) ──────────────
        private static readonly Dictionary<RunMapNodeType, string> NodeIcons = new()
        {
            { RunMapNodeType.Combat,  "[C]" },
            { RunMapNodeType.Elite,   "[E]" },
            { RunMapNodeType.Mystery, "[?]" },
            { RunMapNodeType.Shop,    "[$]" },
            { RunMapNodeType.Rest,    "[R]" },
            { RunMapNodeType.Boss,    "[B]" },
        };
        private static readonly Dictionary<RunMapNodeType, string> NodeTypeNames = new()
        {
            { RunMapNodeType.Combat,  "Combat" },
            { RunMapNodeType.Elite,   "Elite" },
            { RunMapNodeType.Mystery, "Mystere" },
            { RunMapNodeType.Shop,    "Boutique" },
            { RunMapNodeType.Rest,    "Repos" },
            { RunMapNodeType.Boss,    "BOSS" },
        };

        private const int WorldCount     = 10;
        private const int LevelsPerWorld = 9;

        private static readonly string[] WorldThemeKeys =
        {
            "worldmap.world1",
            "worldmap.world2",
            "worldmap.world3",
            "worldmap.world4",
            "worldmap.world5",
            "worldmap.world6",
            "worldmap.world7",
            "worldmap.world8",
            "worldmap.world9",
            "worldmap.world10",
        };

        private static readonly Color[] WorldThemeColors =
        {
            new Color(0.27f, 0.62f, 0.22f), // W1 Plaine  — vert
            new Color(0.78f, 0.55f, 0.20f), // W2 Desert  — sable
            new Color(0.18f, 0.48f, 0.78f), // W3 Ocean   — bleu
            new Color(0.85f, 0.22f, 0.12f), // W4 Volcan  — rouge
            new Color(0.58f, 0.38f, 0.68f), // W5 Foret   — mauve
            new Color(0.75f, 0.88f, 0.95f), // W6 Glace   — bleu pale
            new Color(0.38f, 0.25f, 0.12f), // W7 Marais  — brun
            new Color(0.12f, 0.12f, 0.18f), // W8 Nebula  — noir bleu
            new Color(0.85f, 0.62f, 0.28f), // W9 Apocalypse — ambre
            new Color(0.15f, 0.45f, 0.65f), // W10 Espace — cyan foncé
        };

        private static readonly string[] WorldThemeNames =
        {
            "Plaine", "Desert", "Ocean", "Volcan",
            "Foret",  "Glace",  "Marais", "Nebula",
            "Apocalypse", "Espace",
        };

        private VisualElement? _root;
        private VisualElement? _levelGrid;
        private Label?         _starsLabel;
        private Label?         _completionLabel;
        private int            _activeWorld = 1;
        private LevelRegistry? _registry;

        // RunMap node-graph state
        private RunMap?         _runMap;
        private HashSet<string> _availableIds = new();
        private Label?          _runmapHintLabel;

        private void Start()
        {
            var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName != "WorldMap")
            {
#if UNITY_EDITOR
                // N28: expected behavior in Main scene per R2-recovery comment; demote
                // LogWarning to Log to reduce console noise. Still kept in EDITOR for traceability.
                Debug.Log($"[WorldMapController] Misplaced controller in scene '{sceneName}' — disabling component (R2-recovery : NOT the GameObject — siblings like HudController must keep running).");
#endif
                // R2-recovery : was gameObject.SetActive(false), which deactivated the entire
                // HUD GameObject and dragged every sibling controller down (HudController,
                // TowerToolbarController, etc.). Disable just this component instead.
                this.enabled = false;
                return;
            }

            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null) return;
            _root     = uiDoc.rootVisualElement;
            if (_root == null) return;
#if UNITY_EDITOR
            Debug.Log($"[WorldMapController] root={_root.name} panelSettings={uiDoc.panelSettings?.name ?? "NULL"}");
#endif

            _registry = LevelRegistry.Get();
            if (_registry == null) return;
            _runMap   = RunMap.Instance;
            if (_runMap == null) return;
            BuildUI();
        }

        private void BuildUI()
        {
            if (_root == null) return;

            // Wire back-to-menu button
            var btnMenu = _root.Q<Button>("btn-menu");
            if (btnMenu != null)
                btnMenu.clicked += OnMenuClicked;

            var titleLabel = _root.Q<Label>("worldmap-title");
            if (titleLabel != null) titleLabel.text = L.Get("worldmap.title");

            _starsLabel      = _root.Q<Label>("worldmap-stars-label");
            _completionLabel = _root.Q<Label>("worldmap-completion-label");

            // When a roguelike run is active, show the node-graph instead of linear select.
            if (_runMap != null && _runMap.HasActiveMap())
            {
                BuildRunMapView();
                return;
            }

            BuildWorldTabs();

            _levelGrid = _root.Q<VisualElement>("worldmap-level-grid");
            if (_levelGrid == null) return;

            RefreshHeader();
            ShowWorld(_activeWorld);
            WireSpecialTiles();

        }

        private void OnMenuClicked()
        {
            AudioController.Instance?.Play("menu_button_hover", 0.5f);
            LevelLoader.GoToMenu();
        }

        // ── RunMap node-graph view ────────────────────────────────────────

        private void BuildRunMapView()
        {
            if (_root == null || _runMap == null) return;

            // Hide the linear grid + world tabs + header + special tiles when entering run mode.
            // RunMap occupies the full screen — leave only its own header/graph/footer visible.
            // V6 fix: also hide worldmap-root entirely — it has height:100% and bg sombre,
            // which stacked above runmap elements (added directly to _root) and broke layout.
            var worldmapRoot = _root.Q<VisualElement>("worldmap-root");
            if (worldmapRoot != null) worldmapRoot.style.display = DisplayStyle.None;
            var levelGrid = _root.Q<VisualElement>("worldmap-level-grid");
            if (levelGrid != null) levelGrid.style.display = DisplayStyle.None;
            var tabBar = _root.Q<VisualElement>("worldmap-tabs");
            if (tabBar != null) tabBar.style.display = DisplayStyle.None;
            var header = _root.Q<VisualElement>("worldmap-header");
            if (header != null) header.style.display = DisplayStyle.None;
            var specialTiles = _root.Q<VisualElement>("special-tiles-row");
            if (specialTiles != null) specialTiles.style.display = DisplayStyle.None;

            // Root must fill the screen so runmap-* children can layout correctly.
            _root.style.flexGrow = 1;
            _root.style.flexDirection = FlexDirection.Column;
            _root.style.backgroundColor = new StyleColor(new Color(0.03f, 0.04f, 0.09f, 1f));

            // Act label.
            var actLabel = _root.Q<Label>("runmap-act-label");
            if (actLabel == null)
            {
                actLabel = new Label();
                actLabel.name = "runmap-act-label";
                actLabel.AddToClassList("runmap-act-label");
                _root.Add(actLabel);
            }
            if (_runMap.State != null)
                actLabel.text = $"Acte {_runMap.State.worldId}";

            // Hint label.
            _runmapHintLabel = _root.Q<Label>("runmap-node-hint");
            if (_runmapHintLabel == null)
            {
                _runmapHintLabel = new Label();
                _runmapHintLabel.name = "runmap-node-hint";
                _runmapHintLabel.AddToClassList("runmap-node-hint");
                _root.Add(_runmapHintLabel);
            }

            // Graph container — reuse existing or create.
            var graphContainer = _root.Q<VisualElement>("runmap-graph");
            if (graphContainer == null)
            {
                graphContainer = new VisualElement();
                graphContainer.name = "runmap-graph";
                graphContainer.AddToClassList("runmap-graph");
                _root.Add(graphContainer);
            }

            RebuildRunMapGraph(graphContainer);
        }

        private void RebuildRunMapGraph(VisualElement graphContainer)
        {
            if (_runMap == null) return;
            graphContainer.Clear();

            _availableIds = new HashSet<string>();
            foreach (var n in _runMap.GetAvailableNextNodes())
                _availableIds.Add(n.id);

            bool atStart = _runMap.GetCurrentNode() == null;
            if (atStart)
                foreach (var n in _runMap.GetStartNodes())
                    _availableIds.Add(n.id);

            var byRow = _runMap.GetNodesByRow();
            for (int r = 0; r <= 6; r++)
            {
                if (!byRow.TryGetValue(r, out var rowNodes)) continue;
                var rowEl = new VisualElement();
                rowEl.AddToClassList("runmap-row");
                foreach (var node in rowNodes)
                    rowEl.Add(BuildRunMapNodeEl(node, graphContainer));
                graphContainer.Add(rowEl);
            }
        }

        private VisualElement BuildRunMapNodeEl(RunMapNode node, VisualElement graphContainer)
        {
            var el = new VisualElement();
            el.AddToClassList("runmap-node");
            el.AddToClassList(node.type.ToString().ToLower());

            bool isCurrent   = _runMap?.IsNodeCurrent(node.id) ?? false;
            bool isVisited   = _runMap?.IsNodeVisited(node.id) ?? false;
            bool isAvailable = _availableIds.Contains(node.id);

            if (isCurrent)        el.AddToClassList("current");
            else if (isVisited)   el.AddToClassList("visited");
            else if (isAvailable)
            {
                el.AddToClassList("available");
                string nodeId = node.id;
                var capturedNode = node;
                el.RegisterCallback<ClickEvent>(_ => OnRunMapNodeClicked(nodeId, graphContainer));
                el.RegisterCallback<MouseEnterEvent>(_ => ShowRunMapHint(capturedNode));
                el.RegisterCallback<MouseLeaveEvent>(_ => ClearRunMapHint());
            }
            else el.AddToClassList("hidden");

            var icon = new Label(NodeIcons.TryGetValue(node.type, out var ico) ? ico : "?");
            icon.AddToClassList("runmap-node-icon");
            el.Add(icon);

            var label = new Label(NodeTypeNames.TryGetValue(node.type, out var nm) ? nm : "");
            label.AddToClassList("runmap-node-label");
            el.Add(label);

            return el;
        }

        private void OnRunMapNodeClicked(string nodeId, VisualElement graphContainer)
        {
            if (_runMap == null) return;
            RunMapNode? node = null;
            if (_runMap.State != null)
                for (int i = 0; i < _runMap.State.nodes.Count; i++)
                    if (_runMap.State.nodes[i].id == nodeId) { node = _runMap.State.nodes[i]; break; }
            if (node == null) return;

            _runMap.MoveTo(nodeId);

            if (node.type == RunMapNodeType.Boss)
            {
                if (!string.IsNullOrEmpty(node.bossId)) LevelLoader.LoadLevel(node.bossId);
                else LevelLoader.GoToWorldMap();
            }
            else if ((node.type == RunMapNodeType.Combat || node.type == RunMapNodeType.Elite)
                     && !string.IsNullOrEmpty(node.combatLevelId))
            {
                LevelLoader.LoadLevel(node.combatLevelId);
            }
            else
            {
                RebuildRunMapGraph(graphContainer);
            }
        }

        private void ShowRunMapHint(RunMapNode node)
        {
            if (_runmapHintLabel == null) return;
            string type = NodeTypeNames.TryGetValue(node.type, out var t) ? t : "";
            string extra = node.type == RunMapNodeType.Elite
                ? $" (x{node.swarmMul:F1} vagues, x{node.rewardMul:F1} recompense)"
                : "";
            _runmapHintLabel.text = $"{type}{extra}";
        }

        private void ClearRunMapHint()
        {
            if (_runmapHintLabel != null) _runmapHintLabel.text = "";
        }

        private void BuildWorldTabs()
        {
            var tabBar = _root?.Q<VisualElement>("worldmap-tabs");
            if (tabBar == null) return;

            tabBar.Clear();
            for (int w = 1; w <= WorldCount; w++)
            {
                int capturedW = w;
                var tab = new Button(() => OnTabClicked(capturedW));
                tab.name = $"world-tab-{w}";
                tab.AddToClassList("world-tab");

                int worldStars = 0;
                for (int l = 1; l <= LevelsPerWorld; l++)
                    worldStars += SaveSystem.GetStars($"world{w}-{l}");

                bool worldUnlocked = SaveSystem.IsLevelUnlocked($"world{w}-1");
                string themeKey    = WorldThemeKeys[w - 1];
                tab.text = worldUnlocked ? $"W{w}" : "?";

                if (!worldUnlocked) tab.AddToClassList("locked");
                if (worldStars == LevelsPerWorld * 3) tab.AddToClassList("perfect");

                tabBar.Add(tab);
            }
        }

        private void OnTabClicked(int worldIndex)
        {
            _activeWorld = worldIndex;
            RefreshTabs();
            ShowWorld(worldIndex);
        }

        private void RefreshTabs()
        {
            var tabBar = _root?.Q<VisualElement>("worldmap-tabs");
            if (tabBar == null) return;

            for (int w = 1; w <= WorldCount; w++)
            {
                var tab = tabBar.Q<Button>($"world-tab-{w}");
                if (tab == null) continue;
                if (w == _activeWorld) tab.AddToClassList("active");
                else tab.RemoveFromClassList("active");
            }
        }

        private static string BookmarkKey(string levelId) => $"level_bookmark_{levelId}_v1";

        private static bool IsBookmarked(string levelId) =>
            PlayerPrefs.GetInt(BookmarkKey(levelId), 0) == 1;

        private void ToggleBookmark(string levelId)
        {
            int next = IsBookmarked(levelId) ? 0 : 1;
            PlayerPrefs.SetInt(BookmarkKey(levelId), next);
            PlayerPrefs.Save();
            ShowWorld(_activeWorld);
        }

        private void ShowWorld(int worldIndex)
        {
            if (_levelGrid == null) return;
            _levelGrid.Clear();

            var bookmarked = new System.Collections.Generic.List<int>();
            var normal     = new System.Collections.Generic.List<int>();
            for (int l = 1; l <= LevelsPerWorld; l++)
            {
                if (IsBookmarked($"world{worldIndex}-{l}")) bookmarked.Add(l);
                else normal.Add(l);
            }
            var order = new System.Collections.Generic.List<int>(bookmarked);
            order.AddRange(normal);

            foreach (int l in order)
            {
                string levelId  = $"world{worldIndex}-{l}";
                bool unlocked   = SaveSystem.IsLevelUnlocked(levelId);
                bool cleared    = SaveSystem.IsLevelCleared(levelId);
                int  stars      = SaveSystem.GetStars(levelId);
                bool isShowcase = l == LevelsPerWorld - 1;
                bool isBoss     = l == LevelsPerWorld;
                bool bookmark   = IsBookmarked(levelId);
                LevelData? data = _registry?.FindById(levelId);

                var tile = BuildLevelTile(levelId, l, worldIndex, unlocked, cleared, stars, isShowcase, isBoss, bookmark, data);
                _levelGrid.Add(tile);
            }

            RefreshHeader();
        }

        private VisualElement BuildLevelTile(
            string levelId,
            int levelNum,
            int worldIndex,
            bool unlocked,
            bool cleared,
            int stars,
            bool isShowcase,
            bool isBoss,
            bool bookmarked,
            LevelData? data)
        {
            // V8F FIX: use Button instead of VisualElement so built-in Clickable manipulator
            // dispatches click events reliably in WebGL UI Toolkit. Tabs (also Button) work
            // while bare VisualElement+AddManipulator(Clickable) was silently dropping clicks.
            string id = levelId;
            bool isUnlocked = unlocked;
            bool hasData = data != null;
            var tile = new Button(() =>
            {
                if (isUnlocked && hasData) LevelLoader.LoadLevel(id);
                else if (!isUnlocked) Toast.Show("Niveau verrouille", "Terminez le niveau precedent.", 2500, null, ToastType.Generic);
            });
            tile.AddToClassList("level-tile");

            if (!unlocked)       tile.AddToClassList("locked");
            else if (cleared)    tile.AddToClassList("cleared");
            if (isShowcase)      tile.AddToClassList("showcase");
            if (isBoss)          tile.AddToClassList("boss");
            if (bookmarked)      tile.AddToClassList("bookmarked");

            var header = new VisualElement();
            header.AddToClassList("tile-header");

            var numLabel = new Label(levelNum.ToString());
            numLabel.AddToClassList("tile-number");
            header.Add(numLabel);

            var starBtn = new Button(() => ToggleBookmark(id));
            starBtn.AddToClassList("tile-star-btn");
            starBtn.text = bookmarked ? "S" : "s";
            header.Add(starBtn);

            tile.Add(header);

            var thumb = new VisualElement();
            thumb.AddToClassList("tile-thumb");
            int themeIdx = Mathf.Clamp(worldIndex - 1, 0, WorldThemeColors.Length - 1);
            Color baseColor = unlocked ? WorldThemeColors[themeIdx] : new Color(0.25f, 0.25f, 0.25f);
            thumb.style.backgroundColor = new StyleColor(baseColor);
            var themeLabel = new Label(unlocked ? WorldThemeNames[themeIdx] : "???");
            themeLabel.AddToClassList("tile-thumb-label");
            thumb.Add(themeLabel);
            tile.Add(thumb);

            if (isBoss)
            {
                var bossIcon = new Label("BOSS");
                bossIcon.AddToClassList("tile-boss-label");
                tile.Add(bossIcon);
            }
            else if (isShowcase)
            {
                var showIcon = new Label("EX");
                showIcon.AddToClassList("tile-showcase-label");
                tile.Add(showIcon);
            }

            var starsRow = new Label(StarsText(stars, unlocked));
            starsRow.AddToClassList("tile-stars");
            if (stars == 3)      starsRow.AddToClassList("stars-gold");
            else if (stars == 2) starsRow.AddToClassList("stars-silver");
            else if (stars == 1) starsRow.AddToClassList("stars-bronze");
            tile.Add(starsRow);

            if (cleared)
            {
                var hs = HighScores.Instance?.GetHighScore(levelId);
                if (hs != null)
                {
                    string bestText = $"Meilleur: {HighScores.FormatTime(hs.bestTimeSec)}";
                    var hsLabel = new Label(bestText);
                    hsLabel.AddToClassList("tile-highscore");
                    tile.Add(hsLabel);
                }
            }

            if (!unlocked)
            {
                var lockLabel = new Label(L.Get("worldmap.locked"));
                lockLabel.AddToClassList("tile-lock-icon");
                tile.Add(lockLabel);
            }
            // Click handler is now wired into the Button constructor above (V8F).

            return tile;
        }

        private void WireSpecialTiles()
        {
            if (_root == null) return;

            var tileEndless = _root.Q<Button>("tile-endless");
            if (tileEndless != null)
            {
                tileEndless.clicked += LoadEndlessMode;
                var bestLabel = _root.Q<Label>("endless-best");
                if (bestLabel != null)
                    bestLabel.text = $"Best: V{EndlessMode.Instance?.BestWave ?? 0}";
            }

            var tileDaily = _root.Q<Button>("tile-daily");
            if (tileDaily != null)
            {
                tileDaily.clicked += () => DailyChallengeModal.Instance?.Show();
                var streakLabel = _root.Q<Label>("daily-streak");
                if (streakLabel != null)
                {
                    int streak = PlayerPrefs.GetInt("daily_streak", 0);
                    streakLabel.text = streak > 0 ? $"{streak} jours" : "0 jours";
                }
            }

            var tileBossRush = _root.Q<Button>("tile-boss-rush");
            if (tileBossRush != null)
            {
                tileBossRush.SetEnabled(true);
                tileBossRush.tooltip = null;
                tileBossRush.clicked += () => BossRushMode.Instance?.StartBossRush();
            }
        }

        private static void LoadEndlessMode()
        {
            EndlessMode.Instance?.StartEndless();
        }

        private void RefreshHeader()
        {
            int total = SaveSystem.TotalStars();
            if (_starsLabel != null)
                _starsLabel.text = L.Get("worldmap.total_stars", total);

            int clearedCount = 0;
            for (int w = 1; w <= WorldCount; w++)
                for (int l = 1; l <= LevelsPerWorld; l++)
                    if (SaveSystem.IsLevelCleared($"world{w}-{l}")) clearedCount++;

            int maxLevels = WorldCount * LevelsPerWorld;
            int pct = maxLevels > 0 ? Mathf.RoundToInt(clearedCount * 100f / maxLevels) : 0;
            if (_completionLabel != null)
                _completionLabel.text = L.Get("worldmap.completion", pct);
        }

        private static string StarsText(int stars, bool unlocked)
        {
            if (!unlocked) return "?";
            return stars switch
            {
                0 => "- - -",
                1 => "* - -",
                2 => "* * -",
                3 => "* * *",
                _ => "* * *",
            };
        }
    }
}
