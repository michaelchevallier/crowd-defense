#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class WorldMapController : MonoBehaviour
    {
        private const int WorldCount     = 8;
        private const int LevelsPerWorld = 10;

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
        };

        private VisualElement? _root;
        private VisualElement? _levelGrid;
        private Label?         _starsLabel;
        private Label?         _completionLabel;
        private int            _activeWorld = 1;
        private LevelRegistry? _registry;

        private void Start()
        {
            _root     = GetComponent<UIDocument>().rootVisualElement;
            _registry = LevelRegistry.Get();
            BuildUI();
        }

        private void BuildUI()
        {
            if (_root == null) return;

            var titleLabel = _root.Q<Label>("worldmap-title");
            if (titleLabel != null) titleLabel.text = L.Get("worldmap.title");

            _starsLabel      = _root.Q<Label>("worldmap-stars-label");
            _completionLabel = _root.Q<Label>("worldmap-completion-label");

            BuildWorldTabs();

            _levelGrid = _root.Q<VisualElement>("worldmap-level-grid");
            if (_levelGrid == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[WorldMapController] worldmap-level-grid not found in UXML.");
#endif
                return;
            }

            RefreshHeader();
            ShowWorld(_activeWorld);
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

        private void ShowWorld(int worldIndex)
        {
            if (_levelGrid == null) return;
            _levelGrid.Clear();

            for (int l = 1; l <= LevelsPerWorld; l++)
            {
                string levelId  = $"world{worldIndex}-{l}";
                bool unlocked   = SaveSystem.IsLevelUnlocked(levelId);
                bool cleared    = SaveSystem.IsLevelCleared(levelId);
                int  stars      = SaveSystem.GetStars(levelId);
                bool isShowcase = l == LevelsPerWorld - 1;
                bool isBoss     = l == LevelsPerWorld;
                LevelData? data = _registry?.FindById(levelId);

                var tile = BuildLevelTile(levelId, l, unlocked, cleared, stars, isShowcase, isBoss, data);
                _levelGrid.Add(tile);
            }

            RefreshHeader();
        }

        private VisualElement BuildLevelTile(
            string levelId,
            int levelNum,
            bool unlocked,
            bool cleared,
            int stars,
            bool isShowcase,
            bool isBoss,
            LevelData? data)
        {
            var tile = new VisualElement();
            tile.AddToClassList("level-tile");

            if (!unlocked)       tile.AddToClassList("locked");
            else if (cleared)    tile.AddToClassList("cleared");
            if (isShowcase)      tile.AddToClassList("showcase");
            if (isBoss)          tile.AddToClassList("boss");

            var numLabel = new Label(levelNum.ToString());
            numLabel.AddToClassList("tile-number");
            tile.Add(numLabel);

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

            if (unlocked && data != null)
            {
                string id = levelId;
                tile.RegisterCallback<ClickEvent>(_ => LevelLoader.LoadLevel(id));
            }
            else if (!unlocked)
            {
                var lockLabel = new Label(L.Get("worldmap.locked"));
                lockLabel.AddToClassList("tile-lock-icon");
                tile.Add(lockLabel);
                tile.RegisterCallback<ClickEvent>(_ =>
                    Toast.Show("Niveau verrouille", "Terminez le niveau precedent.", 2500, null, ToastType.Generic));
            }

            return tile;
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
