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
        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            var titleLabel = root.Q<Label>("menu-title-label");
            if (titleLabel != null) titleLabel.text = L.Get("menu.game_title");

            var grid = root.Q<VisualElement>("level-grid");
            if (grid == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[LevelSelectController] level-grid VisualElement not found in UXML.");
#endif
                return;
            }

            var registry = LevelRegistry.Get();

            for (int w = 1; w <= 10; w++)
            {
                var row = new VisualElement();
                row.AddToClassList("world-row");

                var worldLabel = new Label(L.Get("menu.world_label", w));
                worldLabel.AddToClassList("world-label");
                row.Add(worldLabel);

                for (int l = 1; l <= 8; l++)
                {
                    string levelId = $"world{w}-{l}";
                    bool unlocked = SaveSystem.IsLevelUnlocked(levelId);
                    bool cleared = SaveSystem.IsLevelCleared(levelId);

                    var btn = new Button();
                    btn.text = L.Get("menu.level_btn", w, l);
                    btn.AddToClassList("level-btn");

                    if (cleared) btn.AddToClassList("cleared");
                    else if (!unlocked) btn.AddToClassList("locked");

                    if (unlocked)
                    {
                        // Capture for lambda
                        string id = levelId;
                        LevelData? levelData = registry?.FindById(id);
                        if (levelData != null)
                        {
                            btn.RegisterCallback<ClickEvent>(_ => OnLevelClicked(id));
                        }
                        else
                        {
                            btn.SetEnabled(false);
                        }
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

        private static void OnLevelClicked(string levelId)
        {
            LevelLoader.LoadLevel(levelId);
        }
    }
}
