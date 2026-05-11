# MIGRATE-POC-09 — Polish visuel POC (camera + MapRenderer + sol + HUD font WebGL)

> Ticket POST-POC-08, fix les 3 bugs visuels remontés par Mike au playtest du build live `/v6/` : caméra cadre pas toute la grille (castle hors-frame), HUD pills text invisible WebGL (font issue), sol uniforme gris sans cells visibles (Gizmos = Editor-only).

## Type & Effort

- **Type** : feature-dev
- **Estimé** : 3-4 commits, ~60 min code + 5 min rebuild + 2 min redeploy
- **Bloqué par** : POC-08 ✅
- **Branch** : `main` direct
- **Working directory** : `/Users/mike/Work/crowd-defense/`

## Objectifs

1. **Re-cadrer caméra** pour voir toute la grille 60×28 (15 cols × 7 rows × cellSize 4.0).
2. **MapRenderer.cs** : instancie un slab Quad/Cube par cell avec material color selon cell type (port partiel de `MapRenderer.js` Phaser, mais simplifié primitives 3D Unity).
3. **Fix HUD font WebGL** : les pills sont visibles (background noir) mais le text est invisible. Probable cause = UI Toolkit default font pas embedded dans le WebGL build. Solution : assigner explicitement une font asset Unity built-in via USS ou Theme Style Sheet.
4. **Rebuild WebGL** + **redeploy `/v6/`** sur gh-pages.

## Décisions techniques

### Caméra (Q-Camera)

Position actuelle : `(0, 12, -6)` rotation `(60, 0, 0)` FOV 60.
Problème : ground intersection à ~(0, 0, 0.93), visible vertical ~16 units, grille vertical 28 units → 12 units hors-frame.

**Nouveau** : position `(0, 30, -25)` rotation `(55, 0, 0)` FOV 50.
- Camera à 30 units au-dessus du sol, 25 units en arrière (regarde vers +Z).
- Forward = `(0, -sin(55°), cos(55°)) = (0, -0.819, 0.574)`.
- Ground intersection at t = 30/0.819 = 36.6 → world `(0, 0, -25 + 36.6×0.574) = (0, 0, -4)`.
- Distance camera→look-at = √(30² + (-4-(-25))²) = √(900 + 441) = √1341 ≈ 36.6.
- Visible vertical at look-at = 2 × 36.6 × tan(25°) = 2 × 36.6 × 0.466 = 34.1.
- 34.1 vertical > 28 grid height ✓ — toute la grille rentre verticalement.
- Horizontal aspect 16/9 = 34.1 × 16/9 = 60.6 ≈ grille width 60 ✓.

Modifier scene Main.unity Main Camera transform via `mcp__UnityMCP__manage_components` ou execute_code SerializedObject.

### MapRenderer (Q-MapRenderer)

**Approche** : MonoBehaviour `MapRenderer` attaché à un GameObject `MapRoot` dans la scene. À `Start()` :
- Lit `PathManager.Instance.Grid` (parsé déjà par PathManager).
- Itère cells, instancie un primitive Cube par cell (Y=0, scale `(cellSize×0.95, 0.1, cellSize×0.95)` → slab plat).
- Applique material color selon cell type.
- Parent tous les slabs à `MapRoot` (pour ne pas polluer hierarchy).
- Skip cells VOID (rien à afficher).

**Color palette** (port indicatif depuis MapRenderer.js Phaser, simplifié) :

```csharp
private static Color CellColor(char ch) => ch switch
{
    GridCoords.GRASS        => new Color(0.30f, 0.55f, 0.25f), // vert herbe
    GridCoords.PATH         => new Color(0.75f, 0.65f, 0.45f), // beige path
    GridCoords.PORTAL       => new Color(0.90f, 0.30f, 0.30f), // rouge portail
    GridCoords.CASTLE       => new Color(0.30f, 0.45f, 0.90f), // bleu castle (sol sous le castle GO)
    GridCoords.WATER        => new Color(0.20f, 0.40f, 0.75f), // bleu eau
    GridCoords.LAVA         => new Color(0.95f, 0.35f, 0.10f), // orange lave
    GridCoords.BRIDGE_WATER => new Color(0.55f, 0.40f, 0.25f), // marron pont
    GridCoords.BRIDGE_LAVA  => new Color(0.45f, 0.30f, 0.20f), // marron foncé pont lave
    GridCoords.DECOR        => new Color(0.40f, 0.40f, 0.40f), // gris décor
    _                       => new Color(0.20f, 0.20f, 0.20f), // gris foncé fallback
};
```

**Performance** : 15×7 = 105 cells = 105 GameObjects. Acceptable POC. Phase 2 optim possible via mesh combining ou Tilemap.

### Plane sol global (Q-GroundPlane)

OPTIONNEL : ajouter un large Quad sous la grille (Y=-0.05, scale 100) pour avoir un sol gris foncé en background. Évite que le frustum "passe à travers" quand on est en bord de grille. Si MapRenderer couvre déjà 105 cells, peut-être pas nécessaire. Sonnet décide.

### HUD font WebGL (Q-FontWebGL)

**Diagnostic** : UI Toolkit utilise par défaut un font "LiberationSans" embed dans Unity Editor mais pas toujours dans le WebGL build. Symptôme = pills background visible mais Label.text rendu en transparent ou couleur invisible.

**Fix recommandé** : créer un Theme Style Sheet (TSS) explicite et l'assigner au PanelSettings :

1. Créer `Assets/UI/HUDTheme.tss` via Editor menu `Assets > Create > UI Toolkit > Theme Style Sheet`.
2. Dans le TSS, override `:root` avec `-unity-font: url("project://database/Library/unity default resources?fileID=10102&type=3");` (référence LiberationSans built-in).
3. Assigner ce TSS au `HUDPanelSettings.asset` field `themeStyleSheet`.

**Fix alternatif plus simple** : éditer `HUD.uss` pour ajouter explicitement un font via :
```css
.pill-value {
    -unity-font: resource("LegacyRuntime");
    -unity-font-definition: initial;
    color: white;
    font-size: 24px;
}
```

Et tester si ça résoud. Sinon, escalader vers TSS.

**Alternative robuste** : importer un .ttf custom dans `Assets/Fonts/` (ex: Roboto-Regular.ttf depuis Google Fonts), créer un Font Asset (TextMeshPro Font Asset OU UI Toolkit Font), et assigner explicitement dans USS. Plus de boulot mais 100% fiable WebGL.

Sonnet : essaie d'abord le fix simple (`-unity-font: resource("LegacyRuntime")` dans USS). Si ça marche, commit. Sinon escalade vers Theme Style Sheet ou .ttf import.

### Rebuild + redeploy

1. **Rebuild WebGL** via batch mode CLI :
   ```bash
   /Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app/Contents/MacOS/Unity \
     -batchmode -nographics -projectPath /Users/mike/Work/crowd-defense \
     -executeMethod CrowdDefense.Build.BuildScript.BuildWebGL -quit \
     -logFile /tmp/unity-build-poc09.log
   ```
   Use `run_in_background: true` + Monitor (~2-3 min build, IL2CPP cache déjà chaud).

2. **Redeploy gh-pages** :
   ```bash
   git worktree add /tmp/gh-pages-poc09 gh-pages
   rm -rf /tmp/gh-pages-poc09/v6
   mkdir -p /tmp/gh-pages-poc09/v6
   cp -r Builds/WebGL/* /tmp/gh-pages-poc09/v6/
   cd /tmp/gh-pages-poc09
   git add v6
   git commit -m "deploy v6 POC-09 polish build $(date +%Y-%m-%d-%H%M)"
   unset GITHUB_TOKEN && git push origin gh-pages
   cd /Users/mike/Work/crowd-defense
   git worktree remove /tmp/gh-pages-poc09
   ```

3. **Vérifier URL** : attendre ~30s puis curl. Doit retourner 200 sur `https://michaelchevallier.github.io/crowd-defense/v6/`.

## Commits attendus

1. `feat(camera): re-position Main Camera to frame full 15×7 grid (pos 0,30,-25 rot 55 FOV 50)`
2. `feat(systems): add MapRenderer with per-cell colored slabs (grass/path/water/lava/bridge/decor)`
3. `fix(ui): assign explicit -unity-font in HUD.uss for WebGL font rendering`
4. `chore(deploy): rebuild WebGL + redeploy v6 POC-09 polish`

## Critères succès

- Caméra Main Camera transform updated (vérifier via `manage_components` get).
- MapRenderer.cs créé + GameObject `MapRoot` dans la scene + 105 cell slabs visibles en Play mode (vert grass, beige path, bleu water col 5, marron bridge `~` col 5 row 1, gris decor `D`, orange lava col 14, etc.).
- HUD pills affichent les valeurs `120` / `1/4` / `120/120` en blanc (text visible WebGL).
- Castle (Cube bleu) visible en bas droite, sur la cell (10, 6).
- Rebuild WebGL OK (Brotli ~6 MB).
- Redeploy gh-pages OK, URL `/v6/` retourne 200, jeu jouable avec visuel complet.
- 3-4 commits pushed sur `main` + 1 sur `gh-pages`.

## Pièges anticipés

1. **`Resources.Load` ou shader.Find URP** : MapRenderer instancie 105 Cubes — utilise `GameObject.CreatePrimitive(PrimitiveType.Cube)` + `Destroy(Collider)` (pas besoin de colliders sur les slabs sol).
2. **Materials partagés** : créer 10 Materials une fois (1 par cell type) et les assigner par référence aux MeshRenderers. PAS un material par cell (leak GPU). Pattern :
   ```csharp
   private static readonly Dictionary<char, Material> _matCache = new();
   private static Material GetMat(char ch) {
       if (!_matCache.TryGetValue(ch, out var m)) {
           m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
           m.color = CellColor(ch);
           _matCache[ch] = m;
       }
       return m;
   }
   ```
3. **Camera change might break PlacementController raycast** : le raycast écran→plane Y=0 reste valide quelle que soit la caméra. OK.
4. **PathManager.Instance peut être null** si MapRenderer.Awake tourne avant PathManager.Awake. Solution : Script Execution Order MapRenderer = +50 (après PathManager qui est défaut 0). OU MapRenderer.Start (Start tourne après tous Awake).
5. **HUD font fix** peut nécessiter plusieurs essais (resource vs themeStyleSheet vs .ttf import). Commencer simple, escalader si ne marche pas.
6. **Build WebGL cache chaud** : Unity garde IL2CPP cache → 2e build ~2-3 min vs 1er build 5-15 min. Bon.
7. **GITHUB_TOKEN piège #1** : `unset GITHUB_TOKEN` avant tout `git push` ou `gh`.

## Quand fini

3-4 commits + redeploy `/v6/` + URL 200 + jeu visuel complet jouable. Termine.
