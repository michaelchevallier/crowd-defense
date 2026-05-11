#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Minimap split into two VisualElement layers:
    //   - StaticLayer  : background + path/castle/portal cells — painted ONCE on OnLevelStart, never again.
    //   - DynamicLayer : enemy + tower dots                    — painted at 10 Hz.
    // Eliminates ~3000 redundant Painter2D commands/second for geometry that never changes.
    // Port of src-v3/ui/Minimap.js.
    [RequireComponent(typeof(UIDocument))]
    public class MinimapController : MonoBehaviour
    {
        private const int MAP_W = 200;
        private const int MAP_H = 120;
        private const int PAD  = 6;

        private VisualElement? _root;
        private VisualElement?  _container;
        private StaticLayer?    _staticLayer;
        private DynamicLayer?   _dynamicLayer;
        private IVisualElementScheduledItem? _scheduledPaint;

        private bool _boundsReady;

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

        private void Start()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[Minimap] No UIDocument component found.");
#endif
                return;
            }

            _root = doc.rootVisualElement.Q<VisualElement>("minimap-container");
            if (_root == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[Minimap] minimap-container element not found.");
#endif
                return;
            }

            // Container holding both layers at the same position
            _container = new VisualElement();
            _container.style.width  = MAP_W;
            _container.style.height = MAP_H;
            _container.style.position = Position.Relative;
            _root.Add(_container);

            _staticLayer = new StaticLayer(MAP_W, MAP_H, PAD);
            _staticLayer.style.width    = MAP_W;
            _staticLayer.style.height   = MAP_H;
            _staticLayer.style.position = Position.Absolute;
            _container.Add(_staticLayer);

            _dynamicLayer = new DynamicLayer(MAP_W, MAP_H, PAD);
            _dynamicLayer.style.width    = MAP_W;
            _dynamicLayer.style.height   = MAP_H;
            _dynamicLayer.style.position = Position.Absolute;
            _container.Add(_dynamicLayer);

            SetVisible(false);

            // Only the dynamic layer repaints at 10 Hz
            _scheduledPaint = _dynamicLayer.schedule.Execute(RepaintDynamic).Every(100);
        }

        private void OnLevelStart(LevelData levelData, Bounds gridBounds)
        {
            _boundsReady = true;
            // Bake static layer once — grid + path + castle + portals never change mid-level
            _staticLayer?.MarkDirtyRepaint();

            bool show = !Device.IsSmallScreen || !Device.IsPortrait;
            SetVisible(show);
        }

        private void OnLevelEnd()
        {
            SetVisible(false);
            _boundsReady = false;
        }

        private void RepaintDynamic()
        {
            if (!_boundsReady || _dynamicLayer == null) return;
            _dynamicLayer.MarkDirtyRepaint();
        }

        private void SetVisible(bool v)
        {
            if (_root == null) return;
            if (v) _root.RemoveFromClassList("hidden");
            else   _root.AddToClassList("hidden");
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

        // ─── Static layer: background + path/castle/portal cells ────────────
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
                pickingMode = PickingMode.Ignore;
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

                // Path / walkable cells as small yellow squares
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
                        painter.LineTo(mp + new Vector2(sz / 2f, -sz / 2f));
                        painter.LineTo(mp + new Vector2(sz / 2f, sz / 2f));
                        painter.LineTo(mp + new Vector2(-sz / 2f, sz / 2f));
                        painter.ClosePath();
                        painter.Fill();
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
                if (WaveManager.Instance != null)
                {
                    foreach (var enemy in WaveManager.Instance.ActiveEnemies)
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
        }
    }
}
