# MIGRATE-POC-01 — Scaffold project structure + scene 3D

> Ticket 1/8 de la Phase 1 POC de la migration Crowd Defense Unity. Pré-requis : Phase 0 setup terminée (Unity Editor running, MCP server up, projet Unity initialisé vide).

## Type & Effort

- **Type** : feature-dev (toi, Sonnet)
- **Estimé** : 2 commits, ~45 min
- **Bloqué par** : aucun (premier ticket POC)
- **Branch** : `main` (PAS worktree — voir section "Workflow git" ci-dessous)
- **Working directory** : `/Users/mike/Work/crowd-defense/`

## Contexte (zero-context)

Tu démarres la Phase 1 POC d'une migration Phaser/Three.js → Unity 6 LTS C#. Le projet Unity est vide (`Assets/` ne contient rien). Tu dois créer le scaffold (dossiers, scene 3D vide avec Camera + Light + Systems root, assembly definition).

**Phase 1 POC cible** : W1-1 jouable WebGL avec 1 tower (archer), 3 enemies (basic/runner/brute), 4 waves, économie/castle HP/HUD/build. Détails complets dans `/Users/mike/Work/crowd-defense/.claude/plans/phase1-poc-plan.md`.

**Ton ticket = juste le scaffold**, pas de gameplay code.

## Décisions techniques imposées

- **Render** : 2.5D top-down avec primitives 3D Unity (Cube/Sphere/Cylinder). Pas 2D sprites.
- **Coord system** : plan XZ (Unity 3D standard, Y=up). Gameplay objects au sol Y=0 (tower Y=0.5 si Cube hauteur 1, etc).
- **Camera** : perspective tilted top-down (~60° euler X, position `(0, 12, -6)`, target world origin). Field of view 60°.
- **Lighting** : 1 Directional Light (rotation par défaut Unity ~(50, -30, 0)), couleur warm white, intensity 1.
- **Assembly definition** : asmdef root unique `CrowdDefense` sous `Assets/Scripts/`, pas de split (Phase 2 fera split si besoin compile time).

## Workflow git — IMPORTANT

**PAS de worktree pour ce ticket**. Tu travailles directement sur `main` dans `/Users/mike/Work/crowd-defense/` parce que :
- Unity Editor running (PID 65016) tient un lock sur `Library/` du projet ouvert.
- Tu vas utiliser `mcp__UnityMCP__*` tools qui pointent sur le projet **ouvert** (`/Users/mike/Work/crowd-defense/`), pas un worktree ailleurs.
- Si tu créais un worktree `/tmp/poc-01/`, les modifications MCP iraient dans le projet principal et tu aurais un état incohérent.

**Risque conflict** : nul (un seul agent actif à la fois).

**Commits** : atomiques, conventionnels en français court, footer `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>`. Push direct vers `origin/main` à la fin.

**Gate avant push** : Unity Editor doit compiler sans erreur (`mcp__UnityMCP__read_console`). Si erreurs, fix avant push.

## État courant à vérifier au démarrage

```bash
ls Assets/          # doit être vide (ou juste un .gitkeep root déjà présent)
ls Packages/        # manifest.json existe avec com.coplaydev.unity-mcp
git status          # clean
git branch --show-current  # main
ps aux | grep "Unity.app/Contents/MacOS/Unity" | grep -v grep | head -1  # Editor running
```

Si Unity Editor n'est pas running : `open -na "/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app" --args -projectPath /Users/mike/Work/crowd-defense` puis attendre ~10 sec pour que HttpAutoStartHandler démarre le MCP server.

## Tools MCP UnityMCP à utiliser

Tu as accès aux tools via ToolSearch :
- `mcp__UnityMCP__manage_scene` — create/save/load scenes
- `mcp__UnityMCP__manage_gameobject` — create/modify GameObjects (camera, light, parent)
- `mcp__UnityMCP__manage_components` — add/configure components (Camera, Light)
- `mcp__UnityMCP__manage_editor` — query editor state
- `mcp__UnityMCP__read_console` — check Unity console for errors/logs
- `mcp__UnityMCP__find_gameobjects` — verify GameObjects exist

Charge-les via `ToolSearch select:mcp__UnityMCP__manage_scene,mcp__UnityMCP__manage_gameobject,...` puis utilise-les normalement.

---

## Commits à livrer

### Commit 1 — `chore: scaffold Unity folder structure (Scripts/Prefabs/ScriptableObjects/Scenes) + asmdef`

**Fichiers à créer** :

```
/Users/mike/Work/crowd-defense/Assets/Scripts/Data/.gitkeep
/Users/mike/Work/crowd-defense/Assets/Scripts/Entities/.gitkeep
/Users/mike/Work/crowd-defense/Assets/Scripts/Systems/.gitkeep
/Users/mike/Work/crowd-defense/Assets/Scripts/UI/.gitkeep
/Users/mike/Work/crowd-defense/Assets/Prefabs/Towers/.gitkeep
/Users/mike/Work/crowd-defense/Assets/Prefabs/Enemies/.gitkeep
/Users/mike/Work/crowd-defense/Assets/Prefabs/UI/.gitkeep
/Users/mike/Work/crowd-defense/Assets/ScriptableObjects/Towers/.gitkeep
/Users/mike/Work/crowd-defense/Assets/ScriptableObjects/Enemies/.gitkeep
/Users/mike/Work/crowd-defense/Assets/ScriptableObjects/Levels/.gitkeep
/Users/mike/Work/crowd-defense/Assets/Scenes/.gitkeep
/Users/mike/Work/crowd-defense/Assets/Scripts/CrowdDefense.asmdef
```

**Contenu `CrowdDefense.asmdef`** :

```json
{
    "name": "CrowdDefense",
    "rootNamespace": "CrowdDefense",
    "references": [
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

**Note** : la ref `Unity.TextMeshPro` est anticipative pour POC-07 (HUD). Si Unity ne la trouve pas immédiatement (TMP pas encore importé), tu peux la retirer pour ce commit et la rajouter en POC-07.

**Process** :
1. Crée les fichiers `.gitkeep` (Write tool, contenu vide).
2. Crée `CrowdDefense.asmdef` avec le JSON ci-dessus.
3. Wait quelques secondes pour qu'Unity détecte les nouveaux fichiers (auto-reload).
4. `mcp__UnityMCP__read_console` → vérifie no errors. Si erreur sur TMP ref → retire la ref, retry.
5. `git add Assets/`, `git commit` avec message conventionnel.

**Verification** :
```bash
find Assets -name "*.gitkeep" | wc -l   # 11
find Assets -name "*.asmdef" | wc -l    # 1
```

---

### Commit 2 — `chore: add Main.unity 3D scene with perspective camera + directional light + systems root`

**Fichier à créer** : `/Users/mike/Work/crowd-defense/Assets/Scenes/Main.unity`

**Méthode** : utilise `mcp__UnityMCP__manage_scene` avec action `create` (pas `Write` direct du YAML scene — corruption risk).

**Étapes via MCP** :

1. `mcp__UnityMCP__manage_scene action=create scene_name="Main" scene_path="Assets/Scenes/Main.unity"`
   - Crée la scene vide + Main Camera + Directional Light par défaut Unity (qui sont créés automatiquement par Unity quand `create` est appelé pour une scene 3D).

2. **Si la scene par défaut est 2D** (caméra orthographique) plutôt que 3D : modifie via `mcp__UnityMCP__manage_components` :
   - Sur `Main Camera` : set component `Camera`, props : `orthographic=false`, `fieldOfView=60`, `nearClipPlane=0.3`, `farClipPlane=100`.
   - Sur `Main Camera` Transform : `position=(0, 12, -6)`, `rotation=(60, 0, 0)` (euler, X tilt vers le bas).
   - Sur `Directional Light` : vérifie qu'il existe ; si non, `mcp__UnityMCP__manage_gameobject action=create name="Directional Light" + add Component Light type=Directional`.

3. **Ajouter le GameObject parent `Systems`** :
   - `mcp__UnityMCP__manage_gameobject action=create name="Systems" position=(0,0,0)`
   - Ce sera le parent pour `LevelRunner`, `Economy`, `WaveManager`, `PathManager`, `PlacementController` aux tickets suivants.

4. **Sauvegarder la scene** : `mcp__UnityMCP__manage_scene action=save`.

5. **Verification via MCP** :
   - `mcp__UnityMCP__manage_scene action=get_active` → name = `Main`, path = `Assets/Scenes/Main.unity`.
   - `mcp__UnityMCP__find_gameobjects` → contient `Main Camera`, `Directional Light`, `Systems`.
   - `mcp__UnityMCP__read_console` → no errors.

6. `git add Assets/Scenes/Main.unity Assets/Scenes/Main.unity.meta` + commit.

**Si MCP échoue sur create scene** (rare mais possible) : fallback Editor menu via `mcp__UnityMCP__execute_menu_item menu_path="File/New Scene"` puis `File/Save As` vers `Assets/Scenes/Main.unity`. Si même ça échoue, log les erreurs dans le commit message + arrête, je débuggerai.

---

## Verification finale

Après les 2 commits :

```bash
# Local checks
git log --oneline -3              # 2 nouveaux commits + handoff commits
git status                        # clean
ls Assets/Scripts                 # Data Entities Systems UI + CrowdDefense.asmdef
ls Assets/Scenes                  # Main.unity + Main.unity.meta
```

```
# Unity MCP checks
mcp__UnityMCP__manage_editor get_state          # isCompiling = false
mcp__UnityMCP__read_console                     # no errors
mcp__UnityMCP__manage_scene get_active          # Main
mcp__UnityMCP__find_gameobjects                 # Main Camera + Directional Light + Systems
```

**Critères succès** :
- 2 commits propres sur main avec messages conventionnels.
- Push réussi vers `origin/main` (si `gh` retourne 401 → `unset GITHUB_TOKEN && git push origin main`).
- Unity console clean (no compile errors, no missing refs).
- Scene `Main.unity` chargée par défaut, contient les 3 GameObjects attendus.
- Camera en perspective tilted top-down (visuel : si tu push play, la scene est vue d'en haut légèrement inclinée).

## Pièges anticipés

1. **Unity ne détecte pas les nouveaux fichiers** : MCP refresh auto, mais si bloqué → `mcp__UnityMCP__execute_menu_item menu_path="Assets/Refresh"`.
2. **`.gitkeep` ne sont pas .meta-générés** par Unity (commencent par `.`) — c'est OK, ils restent invisibles à Unity et servent juste à git pour préserver les folders.
3. **Asmdef referenced before importing TMP** : si Unity gueule sur `Unity.TextMeshPro` dans l'asmdef → retire la ref pour ce commit, je la rajouterai en POC-07.
4. **Scene par défaut Unity** peut être 2D (depending on Unity version + template) — si c'est 2D, force 3D via Camera config.
5. **`GITHUB_TOKEN` env var invalide** dans le shell parent Mike (cf STATUS.md L7) — si `git push` échoue avec 401 → `unset GITHUB_TOKEN && git push origin main`. Idem pour `gh` commands.

## Quand tu as fini

Push les 2 commits sur `origin/main`, puis termine ton tour. Je (Opus) prends le relais pour briefer POC-02. Tu n'as PAS besoin de continuer après POC-01.

Si tu rencontres un blocker non-trivial (MCP fails, scene corrupt, compile errors persistants), arrête, ne force pas, dis-moi exactement quoi a foiré et où tu es bloqué.
