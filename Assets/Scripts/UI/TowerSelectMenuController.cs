#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    /// <summary>
    /// Popup de sélection de tour sur cellule vide.
    /// Ouvert par PlacementController.OnEmptyBuildableTileClick.
    /// 4 boutons : Archer / Mage / Cannon / Frost.
    /// Fermeture : click outside (PointerDown sur root) ou ESC.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TowerSelectMenuController : MonoBehaviour
    {
        [SerializeField] private TowerRegistry? towerRegistry;

        private static readonly string[] QuickPickIds = { "archer", "mage", "cannon", "frost" };

        private VisualElement? menuRoot;
        private readonly List<TowerType> menuTowers = new();
        private readonly List<Button> menuButtons = new();

        private bool _isOpen;

        private void Start()
        {
            var doc = GetComponent<UIDocument>() ?? FindFirstObjectByType<UIDocument>();
            if (doc == null) { enabled = false; return; }

            var root = doc.rootVisualElement;
            menuRoot = root.Q<VisualElement>("tower-select-menu");
            if (menuRoot == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[TowerSelect] 'tower-select-menu' element not found in UIDocument.");
#endif
                enabled = false;
                return;
            }

            BuildMenuContent();

            // Close on click anywhere outside the menu
            root.RegisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);

            if (PlacementController.Instance != null)
                PlacementController.Instance.OnEmptyBuildableTileClick += OpenAt;

            if (Economy.Instance != null)
                Economy.Instance.OnGoldChanged += OnGoldChanged;

            Hide();
        }

        private void OnDestroy()
        {
            if (PlacementController.Instance != null)
                PlacementController.Instance.OnEmptyBuildableTileClick -= OpenAt;
            if (Economy.Instance != null)
                Economy.Instance.OnGoldChanged -= OnGoldChanged;
        }

        private void Update()
        {
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
                Hide();
        }

        // Called by PlacementController with world-space cell center
        public void OpenAt(Vector3 worldPos)
        {
            if (menuRoot == null) return;

            RefreshAffordability(Economy.Instance?.Gold ?? 0);

            // World → screen → UI Toolkit coords (Y flipped)
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 screen = cam.WorldToScreenPoint(worldPos + Vector3.up * 0.5f);
            if (screen.z < 0f) return;

            var panel = menuRoot.panel;
            if (panel == null) { Show(); return; }

            float panelH = panel.visualTree.layout.height;
            float panelW = panel.visualTree.layout.width;

            // Defer layout query one frame — menu may not have been measured yet
            menuRoot.schedule.Execute(() =>
            {
                float w = menuRoot.layout.width;
                float h = menuRoot.layout.height;
                float left = Mathf.Clamp(screen.x - w * 0.5f, 4f, panelW - w - 4f);
                float top  = Mathf.Clamp(panelH - screen.y - h - 20f, 4f, panelH - h - 4f);
                menuRoot.style.left = left;
                menuRoot.style.top  = top;
            });

            Show();
        }

        private void BuildMenuContent()
        {
            if (menuRoot == null) return;
            menuRoot.Clear();
            menuTowers.Clear();
            menuButtons.Clear();

            var title = new Label(L.Get("ui.tower_select.title"));
            title.AddToClassList("tower-select-title");
            menuRoot.Add(title);

            var row = new VisualElement();
            row.AddToClassList("tower-select-row");
            menuRoot.Add(row);

            TowerType[]? allTowers = towerRegistry?.Towers;

            foreach (var pickId in QuickPickIds)
            {
                TowerType? type = null;
                if (allTowers != null)
                {
                    foreach (var t in allTowers)
                        if (t != null && t.Id == pickId) { type = t; break; }
                }

                var btn = new Button();
                btn.AddToClassList("tower-select-btn");

                string icon = type?.IconEmoji ?? PickFallbackIcon(pickId);
                string name = type?.DisplayName ?? Capitalize(pickId);
                int cost = type?.Cost ?? 0;

                var iconLbl = new Label(icon);
                iconLbl.AddToClassList("tower-select-icon");
                btn.Add(iconLbl);

                var nameLbl = new Label(name);
                nameLbl.AddToClassList("tower-select-name");
                btn.Add(nameLbl);

                if (cost > 0)
                {
                    var costLbl = new Label($"{cost}g");
                    costLbl.AddToClassList("tower-select-cost");
                    btn.Add(costLbl);
                }

                int capturedIdx = menuTowers.Count;
                btn.RegisterCallback<ClickEvent>(_ => OnButtonClick(capturedIdx));

                row.Add(btn);
                menuTowers.Add(type!);
                menuButtons.Add(btn);
            }
        }

        private void OnButtonClick(int idx)
        {
            if (idx < 0 || idx >= menuTowers.Count) return;
            var type = menuTowers[idx];
            if (type == null) return;
            PlacementController.Instance?.SelectTowerForPlacement(type);
            Hide();
        }

        private void OnRootPointerDown(PointerDownEvent evt)
        {
            if (!_isOpen) return;
            if (menuRoot == null) return;
            // If the click target is inside the menu, let it propagate normally
            var target = evt.target as VisualElement;
            if (target != null && (target == menuRoot || menuRoot.Contains(target))) return;
            Hide();
        }

        private void OnGoldChanged(int gold) => RefreshAffordability(gold);

        private void RefreshAffordability(int gold)
        {
            for (int i = 0; i < menuButtons.Count; i++)
            {
                if (i >= menuTowers.Count) break;
                var t = menuTowers[i];
                bool poor = t != null && t.Cost > gold;
                if (poor) menuButtons[i].AddToClassList("tower-select-btn--poor");
                else menuButtons[i].RemoveFromClassList("tower-select-btn--poor");
            }
        }

        private void Show()
        {
            menuRoot?.RemoveFromClassList("hidden");
            _isOpen = true;
        }

        private void Hide()
        {
            menuRoot?.AddToClassList("hidden");
            _isOpen = false;
        }

        private static string PickFallbackIcon(string id) => id switch
        {
            "archer"   => "\U0001F3F9",
            "tank"     => "\U0001F6E1",
            "mage"     => "\U0001F52E",
            "ballista" => "\U00002694",
            "cannon"   => "\U0001F4A3",
            "frost"    => "\U00002744",
            "crossbow" => "\U000026A1",
            "skyguard" => "\U00002708",
            "mine"     => "\U0001F4A5",
            "acid"     => "\U0001F9EA",
            "fan"      => "\U0001F32C",
            "portal"   => "\U0001F300",
            "magnet"   => "\U0001F9F2",
            _          => "\U00002753",
        };

        private static string Capitalize(string s) =>
            s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
    }
}
