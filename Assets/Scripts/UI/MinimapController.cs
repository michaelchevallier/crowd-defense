#nullable enable
using UnityEngine;
using UnityEngine.UIElements;
using CrowdDefense.Data;
using CrowdDefense.Entities;
using CrowdDefense.Systems;

namespace CrowdDefense.UI
{
    // Minimap rendered at 10 Hz via IVisualElementScheduledItem into a VisualElement
    // that draws itself with IGenerateVisualContent (no secondary camera, no RenderTexture).
    // Port of src-v3/ui/Minimap.js.
    [RequireComponent(typeof(UIDocument))]
    public class MinimapController : MonoBehaviour
    {
        private const int MAP_W = 200;
        private const int MAP_H = 120;
        private const int PAD  = 6;

        private VisualElement? _root;
        private MinimapDrawer? _drawer;
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
            _root = doc.rootVisualElement.Q<VisualElement>("minimap-container");
            if (_root == null) return;

            _drawer = new MinimapDrawer(MAP_W, MAP_H, PAD);
            _drawer.style.width  = MAP_W;
            _drawer.style.height = MAP_H;
            _root.Add(_drawer);

            // Hide until a level is loaded
            SetVisible(false);

            // 10 Hz repaint — low-freq is fine for a minimap
            _scheduledPaint = _drawer.schedule.Execute(Repaint).Every(100);
        }

        private void OnLevelStart(LevelData levelData, Bounds gridBounds)
        {
            _boundsReady = true;
            bool show = !Device.IsSmallScreen || !Device.IsPortrait;
            SetVisible(show);
        }

        private void OnLevelEnd()
        {
            SetVisible(false);
            _boundsReady = false;
        }

        private void Repaint()
        {
            if (!_boundsReady || _drawer == null) return;
            _drawer.MarkDirtyRepaint();
        }

        private void SetVisible(bool v)
        {
            if (_root == null) return;
            if (v) _root.RemoveFromClassList("hidden");
            else   _root.AddToClassList("hidden");
        }

        // ─── Inner VisualElement that does the actual drawing ───────────────

        private sealed class MinimapDrawer : VisualElement
        {
            private readonly int _w;
            private readonly int _h;
            private readonly int _pad;

            public MinimapDrawer(int w, int h, int pad)
            {
                _w = w; _h = h; _pad = pad;
                generateVisualContent += OnGenerateVisualContent;
            }

            private void OnGenerateVisualContent(MeshGenerationContext ctx)
            {
                var grid = PathManager.Instance?.Grid;
                if (grid == null) return;

                float gw = grid.Width  * grid.CellSize;
                float gh = grid.Height * grid.CellSize;

                float usableW = _w - _pad * 2;
                float usableH = _h - _pad * 2;
                float sx = usableW / Mathf.Max(1f, gw);
                float sy = usableH / Mathf.Max(1f, gh);
                float s  = Mathf.Min(sx, sy);

                // Center the scaled map in the available area
                float offX = _pad + (usableW - gw * s) / 2f;
                float offY = _pad + (usableH - gh * s) / 2f;

                // World XZ → minimap UV (Y is row, inverted Z)
                Vector2 W2M(Vector3 world)
                {
                    // GridCoords: origin at center, Z = -(row - (h-1)/2)*cellSize
                    float px = world.x / grid.CellSize + (grid.Width  - 1) / 2f;
                    float py = -(world.z / grid.CellSize) + (grid.Height - 1) / 2f;
                    return new Vector2(offX + px * s, offY + py * s);
                }

                var painter = ctx.painter2D;

                // Background
                painter.fillColor = new Color(0.08f, 0.11f, 0.14f, 0.88f);
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, 0));
                painter.LineTo(new Vector2(_w, 0));
                painter.LineTo(new Vector2(_w, _h));
                painter.LineTo(new Vector2(0, _h));
                painter.ClosePath();
                painter.Fill();

                // Draw path cells as small yellow squares
                painter.fillColor = new Color(1f, 0.82f, 0.25f, 0.35f);
                for (int row = 0; row < grid.Height; row++)
                {
                    for (int col = 0; col < grid.Width; col++)
                    {
                        char c = grid.At(col, row);
                        if (!GridCoords.Walkable.Contains(c)) continue;
                        var pos = GridCoords.CellToWorld(col, row, grid.Width, grid.Height, grid.CellSize);
                        var mp = W2M(pos);
                        float sz = Mathf.Max(1.5f, s * 0.85f);
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
                    DrawDot(painter, W2M(pos), 4f);
                }

                // Portal cells (green)
                painter.fillColor = new Color(0.3f, 1f, 0.5f, 0.9f);
                foreach (var cell in grid.Portals)
                {
                    var pos = GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize);
                    DrawDot(painter, W2M(pos), 3f);
                }

                // Placed towers (color from TowerType.BodyColor)
                if (PlacementController.Instance != null)
                {
                    foreach (var tower in PlacementController.Instance.PlacedTowers)
                    {
                        if (tower == null) continue;
                        var mp = W2M(tower.transform.position);
                        painter.fillColor = tower.Config?.BodyColor ?? Color.cyan;
                        DrawDot(painter, mp, 3.5f);
                    }
                }

                // Enemies (red / gold for boss)
                if (WaveManager.Instance != null)
                {
                    foreach (var enemy in WaveManager.Instance.ActiveEnemies)
                    {
                        if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                        var mp = W2M(enemy.transform.position);
                        bool isBoss = enemy.Config?.IsBoss ?? false;
                        painter.fillColor = isBoss
                            ? new Color(1f, 0.82f, 0.25f, 1f)
                            : new Color(1f, 0.25f, 0.25f, 1f);
                        DrawDot(painter, mp, isBoss ? 5f : 3f);
                    }
                }
            }

            private static void DrawDot(Painter2D p, Vector2 center, float r)
            {
                p.BeginPath();
                p.Arc(center, r, 0f, 360f);
                p.Fill();
            }
        }
    }
}
