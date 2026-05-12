#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Minimap split into two VisualElement layers:
    //   - StaticLayer  : background + path lines + castle/portal dots — painted ONCE on OnLevelStart.
    //   - DynamicLayer : enemy + tower dots                           — painted at 10 Hz.
    // Eliminates ~3000 redundant Painter2D commands/second for geometry that never changes.
    // Port of src-v3/ui/Minimap.js.
    [RequireComponent(typeof(UIDocument))]
    public class MinimapController : UIControllerBase
    {
        private const int BASE_W = 200;
        private const int BASE_H = 200;
        private const int PAD    = 6;

        private const float ZOOM_MIN  = 0.5f;
        private const float ZOOM_MAX  = 2.0f;
        private const float ZOOM_STEP = 0.15f;
        private const string PREFS_ZOOM = "Minimap_ZoomLevel";
        private float _zoom = 1f;

        private VisualElement? _container;
        private Button?         _toggleBtn;
        private StaticLayer?    _staticLayer;
        private DynamicLayer?   _dynamicLayer;
        private IVisualElementScheduledItem? _scheduledPaint;

        private Slider?         _zoomSlider;
        private bool _boundsReady;
        private bool _visible;

        private void Awake()
        {
            ResolveUI();
        }

        protected override void OnUIReady()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            if (Root == null) return;

            _zoom = Mathf.Clamp(PlayerPrefs.GetFloat(PREFS_ZOOM, 1f), ZOOM_MIN, ZOOM_MAX);

            _toggleBtn = Root.Q<Button>("minimap-toggle-btn");
            var containerEl = Root.Q<VisualElement>("minimap-container");
            if (containerEl == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[Minimap] minimap-container element not found.");
#endif
                return;
            }

            // Inner wrapper holding both layers
            _container = new VisualElement();
            _container.style.width    = BASE_W;
            _container.style.height   = BASE_H;
            _container.style.position = Position.Relative;
            containerEl.Add(_container);

            _staticLayer = new StaticLayer(BASE_W, BASE_H, PAD);
            _staticLayer.style.width    = BASE_W;
            _staticLayer.style.height   = BASE_H;
            _staticLayer.style.position = Position.Absolute;
            _container.Add(_staticLayer);

            _dynamicLayer = new DynamicLayer(BASE_W, BASE_H, PAD);
            _dynamicLayer.style.width    = BASE_W;
            _dynamicLayer.style.height   = BASE_H;
            _dynamicLayer.style.position = Position.Absolute;
            _container.Add(_dynamicLayer);

            // Register click-to-pan on the static layer (captures world position)
            _staticLayer.RegisterCallback<ClickEvent>(OnMinimapClick);

            // Toggle button
            if (_toggleBtn != null)
                _toggleBtn.clicked += OnToggleClicked;

            // Zoom slider (0.5×–2.0×) embedded below the map canvas
            _zoomSlider = new Slider(ZOOM_MIN, ZOOM_MAX) { value = _zoom };
            _zoomSlider.style.width = BASE_W;
            _zoomSlider.style.marginTop = 4;
            _zoomSlider.RegisterValueChangedCallback(evt =>
            {
                _zoom = evt.newValue;
                ApplyZoom();
                PlayerPrefs.SetFloat(PREFS_ZOOM, _zoom);
                PlayerPrefs.Save();
            });
            containerEl.Add(_zoomSlider);

            SetVisible(false);

            // Only the dynamic layer repaints at 10 Hz
            _scheduledPaint = _dynamicLayer.schedule.Execute(RepaintDynamic).Every(100);
        }

        private void OnEnable()
        {
            LevelEvents.OnLevelStart += OnLevelStart;
            LevelEvents.OnLevelEnd   += OnLevelEnd;
        }

        private void OnDisable()
        {
            LevelEvents.OnLevelStart -= OnLevelStart;
            LevelEvents.OnLevelEnd   -= OnLevelEnd;
            _scheduledPaint?.Pause();
        }

        private void Update()
        {
            if (!_visible || _container == null) return;

            float scroll = Input.mouseScrollDelta.y;
            if (scroll == 0f) return;

            // Only respond when cursor is over the minimap container
            // Check via screen position vs container world bounds
            var containerEl = _container.parent;
            if (containerEl == null) return;

            Vector2 mousePos = Input.mousePosition;
            // Convert to UIToolkit coords (Y flipped)
            mousePos.y = Screen.height - mousePos.y;
            Rect worldBound = containerEl.worldBound;
            if (!worldBound.Contains(mousePos)) return;

            _zoom = Mathf.Clamp(_zoom + scroll * ZOOM_STEP, ZOOM_MIN, ZOOM_MAX);
            if (_zoomSlider != null)
                _zoomSlider.SetValueWithoutNotify(_zoom);
            PlayerPrefs.SetFloat(PREFS_ZOOM, _zoom);
            PlayerPrefs.Save();
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (_container == null) return;
            _container.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50), 0f);
            _container.style.scale = new Scale(new Vector3(_zoom, _zoom, 1f));
        }

        private void OnLevelStart(LevelData levelData, Bounds gridBounds)
        {
            _boundsReady = true;
            _staticLayer?.MarkDirtyRepaint();

            bool show = !Device.IsSmallScreen || !Device.IsPortrait;
            SetVisible(show);
        }

        private void OnLevelEnd()
        {
            SetVisible(false);
            _boundsReady = false;
        }

        private void OnToggleClicked()
        {
            SetVisible(!_visible);
        }

        private void RepaintDynamic()
        {
            if (!_boundsReady || _dynamicLayer == null) return;
            _dynamicLayer.MarkDirtyRepaint();
        }

        // Click on the minimap: convert 2D minimap coords → world XZ → pan main camera
        private void OnMinimapClick(ClickEvent evt)
        {
            if (!_boundsReady) return;
            var grid = PathManager.Instance?.Grid;
            if (grid == null) return;

            var target = evt.currentTarget as VisualElement;
            if (target == null) return;
            Rect wb = target.worldBound;
            if (wb.width <= 0f || wb.height <= 0f) return;

            // Normalize [0,1] within the layer
            float nx = (evt.position.x - wb.xMin) / wb.width;
            float ny = (evt.position.y - wb.yMin) / wb.height;

            var (offX, offY, s) = ComputeLayout(grid, BASE_W, BASE_H, PAD);

            // Invert the W2M transform to get world XZ
            float gw = grid.Width  * grid.CellSize;
            float gh = grid.Height * grid.CellSize;

            float mapPx = nx * BASE_W;
            float mapPy = ny * BASE_H;

            // px = offX + (col - (gridW-1)/2f) * cellSize * s  →  col = (px - offX)/s + (gridW-1)/2
            float col = (mapPx - offX) / s + (grid.Width  - 1) / 2f;
            float row = (mapPy - offY) / s + (grid.Height - 1) / 2f;

            float worldX = (col - (grid.Width  - 1) / 2f) * grid.CellSize;
            float worldZ = -((row - (grid.Height - 1) / 2f) * grid.CellSize);

            var cam = Camera.main;
            if (cam == null) return;

            // Pan: keep camera Y + offset, move XZ
            Vector3 pos = cam.transform.position;
            cam.transform.position = new Vector3(worldX, pos.y, worldZ);
        }

        private void SetVisible(bool v)
        {
            _visible = v;
            var containerEl = _container?.parent;
            if (containerEl != null)
            {
                if (v) containerEl.RemoveFromClassList("hidden");
                else   containerEl.AddToClassList("hidden");
            }
        }

        // ─── Shared geometry helpers ─────────────────────────────────────────

        private static (float offX, float offY, float s) ComputeLayout(
            GridData grid, int w, int h, int pad)
        {
            float gw = grid.Width  * grid.CellSize;
            float gh = grid.Height * grid.CellSize;
            float usableW = w - pad * 2;
            float usableH = h - pad * 2;
            float s = Mathf.Min(usableW / Mathf.Max(1f, gw), usableH / Mathf.Max(1f, gh));
            float offX = pad + (usableW - gw * s) / 2f;
            float offY = pad + (usableH - gh * s) / 2f;
            return (offX, offY, s);
        }

        private static Vector2 W2M(Vector3 world, GridData grid, float offX, float offY, float s)
        {
            float px = world.x / grid.CellSize + (grid.Width  - 1) / 2f;
            float py = -(world.z / grid.CellSize) + (grid.Height - 1) / 2f;
            return new Vector2(offX + px * s, offY + py * s);
        }

        private static void DrawDot(Painter2D p, Vector2 center, float r)
        {
            p.BeginPath();
            p.Arc(center, r, 0f, 360f);
            p.Fill();
        }

        // ─── Static layer: background + path polylines + castle/portal dots ─
        // MarkDirtyRepaint called exactly once per level load.

        private sealed class StaticLayer : VisualElement
        {
            private readonly int _w;
            private readonly int _h;
            private readonly int _pad;

            public StaticLayer(int w, int h, int pad)
            {
                _w = w; _h = h; _pad = pad;
                generateVisualContent += OnGenerateVisualContent;
                pickingMode = PickingMode.Position;
            }

            private void OnGenerateVisualContent(MeshGenerationContext ctx)
            {
                var grid = PathManager.Instance?.Grid;
                if (grid == null) return;

                var (offX, offY, s) = ComputeLayout(grid, _w, _h, _pad);
                var painter = ctx.painter2D;

                // Background panel
                painter.fillColor = new Color(0.08f, 0.11f, 0.14f, 0.88f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, 0));
                painter.LineTo(new Vector2(_w, 0));
                painter.LineTo(new Vector2(_w, _h));
                painter.LineTo(new Vector2(0, _h));
                painter.ClosePath();
                painter.Fill();

                // Path polylines — one polyline per path (from PathManager.Paths)
                var pm = PathManager.Instance;
                if (pm != null && pm.Paths.Count > 0)
                {
                    Color[] pathColors = new Color[]
                    {
                        new Color(1f, 0.82f, 0.25f, 0.9f),
                        new Color(0.4f, 0.9f, 1f, 0.9f),
                        new Color(1f, 0.4f, 1f, 0.9f),
                        new Color(0.4f, 1f, 0.55f, 0.9f),
                    };

                    for (int pi = 0; pi < pm.Paths.Count; pi++)
                    {
                        var waypoints = pm.Paths[pi];
                        if (waypoints.Count < 2) continue;

                        painter.strokeColor = pathColors[pi % pathColors.Length];
                        painter.lineWidth   = 2f;
                        painter.BeginPath();
                        var first = W2M(waypoints[0], grid, offX, offY, s);
                        painter.MoveTo(first);
                        for (int wi = 1; wi < waypoints.Count; wi++)
                            painter.LineTo(W2M(waypoints[wi], grid, offX, offY, s));
                        painter.Stroke();

                        // Portal dot (path entry) — white circle
                        painter.fillColor = new Color(1f, 1f, 1f, 0.9f);
                        DrawDot(painter, W2M(waypoints[0], grid, offX, offY, s), 4f);
                    }
                }
                else
                {
                    // Fallback: walkable cells as small yellow squares when no paths computed yet
                    painter.fillColor = new Color(1f, 0.82f, 0.25f, 0.35f);
                    float sz = Mathf.Max(1.5f, s * 0.85f);
                    for (int row = 0; row < grid.Height; row++)
                    {
                        for (int col = 0; col < grid.Width; col++)
                        {
                            char c = grid.At(col, row);
                            if (!GridCoords.Walkable.Contains(c)) continue;
                            var pos = GridCoords.CellToWorld(col, row, grid.Width, grid.Height, grid.CellSize);
                            var mp = W2M(pos, grid, offX, offY, s);
                            painter.BeginPath();
                            painter.MoveTo(mp + new Vector2(-sz / 2f, -sz / 2f));
                            painter.LineTo(mp + new Vector2( sz / 2f, -sz / 2f));
                            painter.LineTo(mp + new Vector2( sz / 2f,  sz / 2f));
                            painter.LineTo(mp + new Vector2(-sz / 2f,  sz / 2f));
                            painter.ClosePath();
                            painter.Fill();
                        }
                    }
                }

                // Castle cells (blue)
                painter.fillColor = new Color(0.3f, 0.55f, 1f, 0.9f);
                foreach (var cell in grid.Castles)
                {
                    var pos = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize);
                    DrawDot(painter, W2M(pos, grid, offX, offY, s), 4f);
                }

                // Portal cells (green)
                painter.fillColor = new Color(0.3f, 1f, 0.5f, 0.9f);
                foreach (var cell in grid.Portals)
                {
                    var pos = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize);
                    DrawDot(painter, W2M(pos, grid, offX, offY, s), 3f);
                }
            }
        }

        // ─── Dynamic layer: enemy + tower dots at 10 Hz ──────────────────────

        private sealed class DynamicLayer : VisualElement
        {
            private readonly int _w;
            private readonly int _h;
            private readonly int _pad;

            public DynamicLayer(int w, int h, int pad)
            {
                _w = w; _h = h; _pad = pad;
                generateVisualContent += OnGenerateVisualContent;
                pickingMode = PickingMode.Ignore;
            }

            private void OnGenerateVisualContent(MeshGenerationContext ctx)
            {
                var grid = PathManager.Instance?.Grid;
                if (grid == null) return;

                var (offX, offY, s) = ComputeLayout(grid, _w, _h, _pad);
                var painter = ctx.painter2D;

                // Placed towers (color from TowerType.BodyColor)
                if (PlacementController.Instance != null)
                {
                    foreach (var tower in PlacementController.Instance.PlacedTowers)
                    {
                        if (tower == null) continue;
                        painter.fillColor = tower.Config?.BodyColor ?? Color.cyan;
                        DrawDot(painter, W2M(tower.transform.position, grid, offX, offY, s), 3.5f);
                    }
                }

                // Enemies (red / gold for boss)
                var wm = WaveManager.Instance;
                if (wm != null)
                {
                    var enemies = wm.ActiveEnemies;
                    if (enemies != null)
                    {
                        foreach (var enemy in enemies)
                        {
                            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                            bool isBoss = enemy.Config?.IsBoss ?? false;
                            painter.fillColor = isBoss
                                ? new Color(1f, 0.82f, 0.25f, 1f)
                                : new Color(1f, 0.25f, 0.25f, 1f);
                            DrawDot(painter, W2M(enemy.transform.position, grid, offX, offY, s), isBoss ? 5f : 3f);
                        }
                    }
                }

                // Hero (green)
                var hero = LevelRunner.Instance?.Hero;
                if (hero != null && hero.gameObject.activeInHierarchy)
                {
                    painter.fillColor = new Color(0.2f, 1f, 0.35f, 1f);
                    DrawDot(painter, W2M(hero.transform.position, grid, offX, offY, s), 5f);
                }

                // Castle (yellow)
                var castle = LevelRunner.Instance?.PrimaryCastle;
                if (castle != null && !castle.IsDead)
                {
                    painter.fillColor = new Color(1f, 0.88f, 0.1f, 1f);
                    DrawDot(painter, W2M(castle.transform.position, grid, offX, offY, s), 6f);
                }
            }
        }
    }
}
