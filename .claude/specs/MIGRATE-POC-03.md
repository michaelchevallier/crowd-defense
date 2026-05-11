# MIGRATE-POC-03 — GridCoords + GridData parser + PathManager BFS

> Ticket 3/8 du Phase 1 POC. Porte le grid + BFS pathfinding depuis Phaser.

## Type & Effort

- **Type** : feature-dev
- **Estimé** : 2 commits, ~80 min
- **Bloqué par** : MIGRATE-POC-01, MIGRATE-POC-02 (LevelData SO requis) ✅ done
- **Branch** : `main` direct (pas worktree)
- **Working directory** : `/Users/mike/Work/crowd-defense/`

## Objectif

Implémenter le pathfinding pour qu'un enemy puisse suivre une polyline portal→castle. 3 composants :

1. **`GridCoords`** : helpers statiques cell↔world (plan XZ Y=0).
2. **`GridData`** : POCO parsing `LevelData.mapRows` → grid 2D + listes portals/castles.
3. **`PathManager`** : MonoBehaviour singleton qui parse grid + run BFS + expose `Waypoints` (List<Vector3>).

Bonus : Gizmos debug visuel (essentiel sans HUD).

## Décisions techniques imposées

- **Plan XZ avec Y=0** pour le sol gameplay. Towers/enemies Y>0 selon mesh.
- **Convention Z-flip** : `z = -(row - (gridH-1)/2f) * cellSize`. Raison : caméra perspective tilted (60° X) à pos `(0, 12, -6)` regarde vers +Z, donc on inverse Z pour que row=0 soit visuellement en haut de l'écran (loin de la caméra).
- **Linear waypoints** : pas de smoothing Catmull-Rom/Chaikin (skipped pour POC, ajout Phase 2).
- **POC simplification** : 1 path par level (le premier portail × premier castle). W1-1 a 1×1 donc trivial. Multi-portal/castle = Phase 2.
- **PathManager.Awake** lit `[SerializeField] LevelData levelData;` direct (LevelRunner pas encore créé). En POC-06 on refactorera pour lire depuis `LevelRunner.Instance.CurrentLevel`.
- **Gizmos** : draw cells colorées + waypoint polyline en Editor scene view (visible sans Play mode).

## Source canonique à porter

Lis intégralement AVANT de commencer :

1. `/Users/mike/Work/milan project/src-v3/systems/MapGrid.js` (127 lignes complète, focus : `CELL` enum L7-22, `WALKABLE`/`BUILDABLE` L24-25, `cellToWorld` L31-35, `neighbors` L56-69, `parseMap` L77-113).
2. `/Users/mike/Work/milan project/src-v3/systems/MapPathfinder.js` lignes 26-66 (`bfsShortestPerCastle` + `reconstructPath`). **Ignore** le smoothing Catmull-Rom (lignes 80+), pas pour POC.

## Path attendu W1-1

W1-1 = grid 7 rows × 15 cols. Portal `(col=0, row=1)`, castle `(col=10, row=6)`. BFS trouve un L-shape : horizontal row 1 col 0→10 (via bridge `~` col 5), puis vertical col 10 row 1→6. Total ~16 waypoints.

---

## Commit 1 — `feat(systems): add GridCoords helper + GridData parser (port from MapGrid.js)`

### Fichier 1 : `Assets/Scripts/Systems/GridCoords.cs`

```csharp
#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CrowdDefense.Systems
{
    public static class GridCoords
    {
        public const char GRASS = '0';
        public const char GRASS_BLOCK = '.';
        public const char PATH = '1';
        public const char PORTAL = 'P';
        public const char CASTLE = 'C';
        public const char TREE = 'T';
        public const char ROCK = 'R';
        public const char WATER = 'W';
        public const char BUSH = 'B';
        public const char VOID = ' ';
        public const char DECOR = 'D';
        public const char LAVA = 'L';
        public const char BRIDGE_WATER = '~';
        public const char BRIDGE_LAVA = '^';

        public static readonly HashSet<char> Walkable = new() { PATH, PORTAL, CASTLE, BRIDGE_WATER, BRIDGE_LAVA };
        public static readonly HashSet<char> Buildable = new() { GRASS };

        // Cell (col, row) → world Vector3 on XZ plane (Y=0), origin centered, Z inverted for row=0 visually on top.
        public static Vector3 CellToWorld(int col, int row, int gridW, int gridH, float cellSize)
        {
            float x = (col - (gridW - 1) / 2f) * cellSize;
            float z = -((row - (gridH - 1) / 2f) * cellSize);
            return new Vector3(x, 0f, z);
        }

        public static Vector2Int WorldToCell(Vector3 world, int gridW, int gridH, float cellSize)
        {
            int col = Mathf.RoundToInt(world.x / cellSize + (gridW - 1) / 2f);
            int row = Mathf.RoundToInt(-(world.z / cellSize) + (gridH - 1) / 2f);
            return new Vector2Int(col, row);
        }

        // 4-connected neighbors of (col, row) within bounds.
        public static IEnumerable<Vector2Int> Neighbors(int col, int row, int gridW, int gridH)
        {
            int[] dc = { -1, 1, 0, 0 };
            int[] dr = { 0, 0, -1, 1 };
            for (int i = 0; i < 4; i++)
            {
                int nc = col + dc[i];
                int nr = row + dr[i];
                if (nc >= 0 && nc < gridW && nr >= 0 && nr < gridH)
                    yield return new Vector2Int(nc, nr);
            }
        }
    }
}
```

### Fichier 2 : `Assets/Scripts/Systems/GridData.cs`

```csharp
#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    public class GridData
    {
        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }
        public char[,] Cells { get; }
        public List<Vector2Int> Portals { get; } = new();
        public List<Vector2Int> Castles { get; } = new();

        private GridData(int w, int h, float cellSize)
        {
            Width = w;
            Height = h;
            CellSize = cellSize;
            Cells = new char[h, w];
        }

        public char At(int col, int row)
        {
            if (col < 0 || col >= Width || row < 0 || row >= Height) return GridCoords.VOID;
            return Cells[row, col];
        }

        public bool IsWalkable(int col, int row) => GridCoords.Walkable.Contains(At(col, row));
        public bool IsBuildable(int col, int row) => GridCoords.Buildable.Contains(At(col, row));

        public static GridData Parse(LevelData level)
        {
            var rows = level.MapRows;
            int h = rows.Count;
            int w = 0;
            for (int i = 0; i < rows.Count; i++) if (rows[i].Length > w) w = rows[i].Length;

            var gd = new GridData(w, h, level.CellSize);

            for (int r = 0; r < h; r++)
            {
                string row = rows[r];
                for (int c = 0; c < w; c++)
                {
                    char ch = c < row.Length ? row[c] : GridCoords.VOID;
                    gd.Cells[r, c] = ch;
                    if (ch == GridCoords.PORTAL) gd.Portals.Add(new Vector2Int(c, r));
                    else if (ch == GridCoords.CASTLE) gd.Castles.Add(new Vector2Int(c, r));
                }
            }

            return gd;
        }

        // BFS from start to first castle in castles list. Returns cell sequence (start to castle), or null if no path.
        public List<Vector2Int>? BfsShortestPath(Vector2Int start, Vector2Int target)
        {
            var parent = new Dictionary<Vector2Int, Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            parent[start] = new Vector2Int(-1, -1);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur == target) return ReconstructPath(parent, start, target);

                foreach (var nb in GridCoords.Neighbors(cur.x, cur.y, Width, Height))
                {
                    if (parent.ContainsKey(nb)) continue;
                    if (!IsWalkable(nb.x, nb.y)) continue;
                    parent[nb] = cur;
                    queue.Enqueue(nb);
                }
            }
            return null;
        }

        private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> parent, Vector2Int start, Vector2Int end)
        {
            var cells = new List<Vector2Int>();
            var cur = end;
            while (cur.x != -1 || cur.y != -1)
            {
                cells.Add(cur);
                if (cur == start) break;
                cur = parent[cur];
            }
            cells.Reverse();
            return cells;
        }
    }
}
```

### Process commit 1

1. Crée les 2 fichiers via `Write` tool.
2. `mcp__UnityMCP__refresh_unity` → AssetDatabase refresh.
3. `mcp__UnityMCP__read_console` + `manage_editor get_state` → no compile errors, `isCompiling=false`.
4. `git add Assets/Scripts/Systems/` + commit `feat(systems): add GridCoords helper + GridData parser (port from MapGrid.js)`.

---

## Commit 2 — `feat(systems): add PathManager with BFS portal→castle + Gizmos debug`

### Fichier 3 : `Assets/Scripts/Systems/PathManager.cs`

```csharp
#nullable enable
using System.Collections.Generic;
using UnityEngine;
using CrowdDefense.Data;

namespace CrowdDefense.Systems
{
    public class PathManager : MonoBehaviour
    {
        public static PathManager? Instance { get; private set; }

        [SerializeField] private LevelData? levelData;

        public GridData? Grid { get; private set; }
        public List<Vector3> Waypoints { get; private set; } = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Build();
        }

        public void Build()
        {
            if (levelData == null)
            {
                Debug.LogError("[PathManager] No LevelData assigned");
                return;
            }

            Grid = GridData.Parse(levelData);
            Waypoints.Clear();

            if (Grid.Portals.Count == 0 || Grid.Castles.Count == 0)
            {
                Debug.LogError($"[PathManager] No portal or castle (portals={Grid.Portals.Count}, castles={Grid.Castles.Count})");
                return;
            }

            // POC : first portal × first castle only.
            var start = Grid.Portals[0];
            var end = Grid.Castles[0];
            var cells = Grid.BfsShortestPath(start, end);

            if (cells == null || cells.Count < 2)
            {
                Debug.LogError($"[PathManager] No path from portal {start} to castle {end}");
                return;
            }

            foreach (var cell in cells)
                Waypoints.Add(GridCoords.CellToWorld(cell.x, cell.y, Grid.Width, Grid.Height, Grid.CellSize));

#if UNITY_EDITOR
            Debug.Log($"[PathManager] grid {Grid.Width}×{Grid.Height}, {Waypoints.Count} waypoints from {start} to {end}");
#endif
        }

        public Vector3 GetWaypoint(int index)
        {
            if (index < 0 || index >= Waypoints.Count) return Vector3.zero;
            return Waypoints[index];
        }

        public int WaypointCount => Waypoints.Count;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (levelData == null) return;

            // Parse fresh in Editor (cheap : 15×7 = 105 cells).
            var grid = Application.isPlaying && Grid != null ? Grid : GridData.Parse(levelData);

            for (int r = 0; r < grid.Height; r++)
            {
                for (int c = 0; c < grid.Width; c++)
                {
                    char ch = grid.At(c, r);
                    Color color = CellGizmoColor(ch);
                    if (color.a == 0f) continue;
                    Vector3 pos = GridCoords.CellToWorld(c, r, grid.Width, grid.Height, grid.CellSize);
                    Gizmos.color = color;
                    Gizmos.DrawCube(pos, new Vector3(grid.CellSize * 0.9f, 0.05f, grid.CellSize * 0.9f));
                }
            }

            // Waypoint polyline + endpoints
            var waypoints = Application.isPlaying ? Waypoints : ComputeWaypointsEditor(grid);
            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Count - 1; i++)
                Gizmos.DrawLine(waypoints[i] + Vector3.up * 0.1f, waypoints[i + 1] + Vector3.up * 0.1f);

            if (waypoints.Count > 0)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(waypoints[0], grid.CellSize * 0.3f);
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(waypoints[waypoints.Count - 1], grid.CellSize * 0.3f);
            }
        }

        private static List<Vector3> ComputeWaypointsEditor(GridData grid)
        {
            var list = new List<Vector3>();
            if (grid.Portals.Count == 0 || grid.Castles.Count == 0) return list;
            var cells = grid.BfsShortestPath(grid.Portals[0], grid.Castles[0]);
            if (cells == null) return list;
            foreach (var cell in cells)
                list.Add(GridCoords.CellToWorld(cell.x, cell.y, grid.Width, grid.Height, grid.CellSize));
            return list;
        }

        private static Color CellGizmoColor(char ch) => ch switch
        {
            GridCoords.GRASS => new Color(0.3f, 0.6f, 0.2f, 0.8f),
            GridCoords.PATH => new Color(0.8f, 0.7f, 0.4f, 0.8f),
            GridCoords.PORTAL => new Color(0.9f, 0.2f, 0.2f, 0.9f),
            GridCoords.CASTLE => new Color(0.2f, 0.4f, 0.9f, 0.9f),
            GridCoords.WATER => new Color(0.2f, 0.4f, 0.8f, 0.7f),
            GridCoords.LAVA => new Color(0.9f, 0.3f, 0.1f, 0.7f),
            GridCoords.BRIDGE_WATER => new Color(0.6f, 0.4f, 0.2f, 0.8f),
            GridCoords.BRIDGE_LAVA => new Color(0.6f, 0.4f, 0.2f, 0.8f),
            GridCoords.DECOR => new Color(0.4f, 0.4f, 0.4f, 0.7f),
            _ => new Color(0f, 0f, 0f, 0f),
        };
#endif
    }
}
```

### Scene setup via MCP

1. `mcp__UnityMCP__manage_gameobject action=create name="PathManager" parent="Systems"` (créer le GO sous `Systems` GameObject racine).
2. Attacher le component PathManager : `mcp__UnityMCP__manage_components action=add component_type="CrowdDefense.Systems.PathManager"`.
3. Assigner `levelData` field via `mcp__UnityMCP__manage_components action=set` ou `execute_code` Editor :
   ```csharp
   // execute_code fallback
   var go = GameObject.Find("Systems/PathManager");
   var pm = go.GetComponent<PathManager>();
   var so = new SerializedObject(pm);
   so.FindProperty("levelData").objectReferenceValue =
       AssetDatabase.LoadAssetAtPath<LevelData>("Assets/ScriptableObjects/Levels/W1-1.asset");
   so.ApplyModifiedProperties();
   EditorSceneManager.MarkSceneDirty(go.scene);
   EditorSceneManager.SaveScene(go.scene);
   ```
4. `mcp__UnityMCP__manage_scene action=save` pour persister.

### Process commit 2

1. Crée le fichier `PathManager.cs` via `Write` tool.
2. Wait compile (`mcp__UnityMCP__manage_editor get_state` → `isCompiling=false`).
3. Setup le GameObject `PathManager` sous `Systems` dans la scene (MCP commands ci-dessus).
4. Assign `levelData` field → `W1-1.asset`.
5. Save scene.
6. **Test play mode** : `mcp__UnityMCP__manage_editor action=play` (start), wait 3s, check `read_console` pour log `[PathManager] grid 15×7, 16 waypoints from (0,1) to (10,6)` (ou similaire).
7. `mcp__UnityMCP__manage_editor action=stop` (stop play).
8. `git add Assets/Scripts/Systems/ Assets/Scenes/Main.unity` + commit `feat(systems): add PathManager with BFS portal→castle + Gizmos debug`.
9. `git push origin main` (si 401 → `unset GITHUB_TOKEN && git push origin main`).

---

## Verification finale

```bash
find Assets/Scripts/Systems -name "*.cs" | wc -l    # 3 (GridCoords, GridData, PathManager)
git log --oneline -3                                # 2 nouveaux commits
```

Via MCP :
```
mcp__UnityMCP__manage_editor get_state          # isCompiling=false, isPlaying=false
mcp__UnityMCP__read_console                     # no errors
mcp__UnityMCP__find_gameobjects                 # PathManager sous Systems
```

Visuel attendu (Scene view Unity Editor) :
- Grille 15×7 visible avec cellules colorées par type (vert pour grass, beige pour path, eau bleu colonne 5, sphère rouge à portal (col 0 row 1), sphère bleue à castle (col 10 row 6)).
- Polyline jaune reliant portal → row 1 col 1-10 → col 10 row 2-6 → castle. Total ~16 segments.

**Critères succès** :
- 3 .cs files (`GridCoords`, `GridData`, `PathManager`) compilent sans erreur.
- PathManager GameObject existe sous Systems avec ref `LevelData` = `W1-1.asset`.
- Play mode log `[PathManager] grid 15×7, 16 waypoints from (0,1) to (10,6)`.
- Gizmos visibles en Scene view (sans Play).
- 2 commits pushed sur `origin/main`.

## Pièges anticipés

1. **Vector2Int sentinel `(-1,-1)`** pour parent root dans BFS : pas idéal (collision si portail à (-1,-1) impossible mais conceptually fragile). Si Sonnet veut un `Vector2Int?` (nullable struct), c'est OK aussi mais plus verbeux.
2. **Z-flip dans CellToWorld** : si tu vois la polyline inversée (portail apparaît en bas écran), c'est que tu as oublié le `-` devant le calcul Z.
3. **`OnDrawGizmos` appelle GridData.Parse à chaque frame Editor** : ~105 cells = négligeable. Mais si tu vois lag, mets en cache + invalide via `OnValidate()`.
4. **Refs croisées dans Scene** : assigner `levelData` field via MCP peut nécessiter fallback `execute_code` (cf POC-02 expérience).
5. **`UnityEditor` namespace** : dans `OnDrawGizmos` j'utilise `Application.isPlaying` qui est ok runtime + editor. Pas besoin de wrap `#if UNITY_EDITOR` sur les `_ComputeWaypointsEditor` car ce sont des appels privés. Mais wrap quand même le bloc entier `#if UNITY_EDITOR` pour exclure les Gizmos du build (pas critique mais clean).
6. **`Application.isPlaying` is true pendant Edit mode si tu as paused play** : ok, ça utilise le Grid déjà parsé. Coverage OK.
7. **Test play mode** : si tu lances play mode et que rien dans la scene (pas d'Enemy/Tower), c'est normal. PathManager log seul suffit comme verif.

## Quand tu as fini

Push 2 commits, termine ton tour. Si Gizmos visuel ne show pas le bon path ou crash, arrête net, explique.
