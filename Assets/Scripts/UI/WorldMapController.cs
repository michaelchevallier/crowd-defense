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
        private static readonly string[] WorldThemeKeys =
        {
            "worldmap.world1",  // W1 — Plaine
            "worldmap.world2",  // W2 — Foret
            "worldmap.world3",  // W3 — Desert
            "worldmap.world4",  // W4 — Volcan
            "worldmap.world5",  // W5 — Foire
            "worldmap.world6",  // W6 — Apocalypse
            "worldmap.world7",  // W7 — Espace
            "worldmap.world8",  // W8 — Submarin
            "worldmap.world9",  // W9 — Medieval
            "worldmap.world10", // W10 — Cyberpunk
        };

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            BuildUI(root);
        }

        private void BuildUI(VisualElement root)
        {
            var titleLabel = root.Q<Label>("worldmap-title");
            if (titleLabel != null) titleLabel.text = L.Get("worldmap.title");

            var starsLabel = root.Q<Label>("worldmap-stars-label");
            if (starsLabel != null) starsLabel.text = L.Get("worldmap.total_stars", SaveSystem.TotalStars());

            var content = root.Q<VisualElement>("worldmap-content");
            if (content == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[WorldMapController] worldmap-content VisualElement not found in UXML.");
#endif
                return;
            }

            var registry = LevelRegistry.Get();

            for (int w = 1; w <= 10; w++)
            {
                var section = BuildWorldSection(w, registry);
                content.Add(section);
            }
        }

        private VisualElement BuildWorldSection(int worldIndex, LevelRegistry? registry)
        {
            var section = new VisualElement();
            section.AddToClassList("world-section");

            var header = new VisualElement();
            header.AddToClassList("world-section-header");

            var numLabel = new Label($"W{worldIndex}");
            numLabel.AddToClassList("world-section-number");

            string themeKey = worldIndex <= WorldThemeKeys.Length ? WorldThemeKeys[worldIndex - 1] : $"worldmap.world{worldIndex}";
            var nameLabel = new Label(L.Get(themeKey));
            nameLabel.AddToClassList("world-section-name");

            int worldStars = 0;
            for (int l = 1; l <= 8; l++)
                worldStars += SaveSystem.GetStars($"world{worldIndex}-{l}");
            int maxWorldStars = 8 * 3;

            var worldStarsLabel = new Label($"{worldStars}/{maxWorldStars}");
            worldStarsLabel.AddToClassList("world-section-stars");

            header.Add(numLabel);
            header.Add(nameLabel);
            header.Add(worldStarsLabel);
            section.Add(header);

            var nodeRow = new VisualElement();
            nodeRow.AddToClassList("world-node-row");

            for (int l = 1; l <= 8; l++)
            {
                string levelId = $"world{worldIndex}-{l}";
                bool unlocked = SaveSystem.IsLevelUnlocked(levelId);
                bool cleared = SaveSystem.IsLevelCleared(levelId);
                int stars = SaveSystem.GetStars(levelId);
                LevelData? levelData = registry?.FindById(levelId);

                var node = BuildLevelNode(levelId, l.ToString(), unlocked, cleared, stars, false, false, levelData);
                nodeRow.Add(node);

                if (l < 8)
                {
                    var connector = new VisualElement();
                    connector.AddToClassList("node-connector");
                    if (cleared) connector.AddToClassList("passed");
                    nodeRow.Add(connector);
                }
            }

            // Showcase level (W*-9)
            {
                string showcaseId = $"world{worldIndex}-9";
                bool showcaseUnlocked = SaveSystem.IsLevelUnlocked(showcaseId);
                bool showcaseCleared = SaveSystem.IsLevelCleared(showcaseId);
                int showcaseStars = SaveSystem.GetStars(showcaseId);
                LevelData? showcaseData = registry?.FindById(showcaseId);

                var connector = new VisualElement();
                connector.AddToClassList("node-connector");
                if (SaveSystem.IsLevelCleared($"world{worldIndex}-8")) connector.AddToClassList("passed");
                nodeRow.Add(connector);

                var showcaseNode = BuildLevelNode(showcaseId, L.Get("worldmap.level_showcase"), showcaseUnlocked, showcaseCleared, showcaseStars, true, false, showcaseData);
                nodeRow.Add(showcaseNode);
            }

            // Boss level (W*-10)
            {
                string bossId = $"world{worldIndex}-10";
                bool bossUnlocked = SaveSystem.IsLevelUnlocked(bossId);
                bool bossCleared = SaveSystem.IsLevelCleared(bossId);
                int bossStars = SaveSystem.GetStars(bossId);
                LevelData? bossData = registry?.FindById(bossId);

                var connector = new VisualElement();
                connector.AddToClassList("node-connector");
                if (SaveSystem.IsLevelCleared($"world{worldIndex}-9")) connector.AddToClassList("passed");
                nodeRow.Add(connector);

                var bossNode = BuildLevelNode(bossId, L.Get("worldmap.level_boss"), bossUnlocked, bossCleared, bossStars, false, true, bossData);
                nodeRow.Add(bossNode);
            }

            section.Add(nodeRow);
            return section;
        }

        private VisualElement BuildLevelNode(
            string levelId,
            string label,
            bool unlocked,
            bool cleared,
            int stars,
            bool isShowcase,
            bool isBoss,
            LevelData? levelData)
        {
            var node = new VisualElement();
            node.AddToClassList("level-node");

            if (!unlocked) node.AddToClassList("locked");
            else if (cleared) node.AddToClassList("cleared");

            if (isShowcase) node.AddToClassList("showcase");
            if (isBoss) node.AddToClassList("boss");

            var numLabel = new Label(label);
            numLabel.AddToClassList("level-node-number");
            node.Add(numLabel);

            var starsLabel = new Label(StarsDisplay(stars, unlocked));
            starsLabel.AddToClassList("level-node-stars");
            node.Add(starsLabel);

            if (unlocked && levelData != null)
            {
                string id = levelId;
                node.RegisterCallback<ClickEvent>(_ => LevelLoader.LoadLevel(id));
            }

            return node;
        }

        private static string StarsDisplay(int stars, bool unlocked)
        {
            if (!unlocked) return L.Get("worldmap.locked");
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
