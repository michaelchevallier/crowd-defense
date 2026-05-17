# Phase 5 Recovery — runtime broken malgré code green (2026-05-17)

> **Statut** : Recovery plan validé Mike 2026-05-17 12h25.
> **Mode** : autonomie totale, parallélisation maximale, full completion.
> **Source** consultable par Sonnet feature-dev / bug-fixer.

---

## Context

Phase 5 PARITY-V4 = code work strict GREEN (89 commits mergés, hard #3 console clean PASS Mike 2026-05-14, 5 audits livrés). Mais Play mode runtime 2026-05-17 exhibe :

- ~25 warnings `MonoSingleton auto-created — missing in scene`.
- 4 errors `Exposed name does not exist: Master_Volume / SFX_Volume / Music_Volume / UI_Volume`.
- 2× `[WaveManager] No LevelData or no waves`.
- 2× `Setting the parent of a transform which resides in a Prefab Asset is disabled to prevent data corruption` (VfxPool).
- Matériaux "rose partout" (URP shader fallback).
- WorldMapController `Root name=HUD-container` en scène Main (pollution HUD).
- `level-grid VisualElement not found in UXML`.
- Caméra Main scene mal calée + sensitivity ×N.
- Mob spawn = 0 dans Wave 1.

**Drift D11 SÉVÈRE confirmé** côté charter §2 (1 check suffit D10-D11).

## Cause racine (audit Explore Main.unity)

- Main.unity contient **39 GameObjects** d'un état pré-Phase 5.
- **`Assets/Editor/BuildMainSceneTool.cs` (70+ `EnsureSingleton`) n'a JAMAIS été run** sur Main.unity post-Phase 5.
- BuildMainSceneTool ne couvre PAS les 6 contrôleurs UI Phase 5 (HeroPortraitController, TowerHoverController, GhostPreviewController, PlacementHighlight, SchoolSelectController, RunModeController).
- AudioController.cs appelle `SetFloat("Master_Volume")` mais le mixer expose `MasterVol` (mismatch nommage).
- WorldMapController lazy-loaded se rattache au HUD UIDocument quand Main scène se charge.
- Matériaux Toon URP : shaders cassés post-migration URP 17.
- Main Camera follow sensitivity ×N (non-1.0).
- LevelSelectController cherche un VisualElement `level-grid` renommé/supprimé.

Mike memory `feedback_no_code_bypass_wiring.md` applicable : wire la scène (Inspector + GameObjects), pas refactor code.

---

## Tickets Wave R — 6 P0 parallèles (worktrees disjoints)

### R0-1 — BuildMainSceneTool : 6 UI controllers Phase 5

**Type** : feature-dev | **Effort** : 1 commit, ~30 min | **Zone** : Editor

**Files** :
- `Assets/Editor/BuildMainSceneTool.cs`

**Action** :
1. Ajouter `EnsureSingleton<T>("Systems/X")` pour : HeroPortraitController, TowerHoverController, GhostPreviewController, PlacementHighlight, SchoolSelectController, RunModeController.
2. Grep `MonoSingleton<` dans `Assets/Scripts/` → identifier tout singleton manquant du tool actuel. Ajouter les manquants.
3. Le tool est idempotent (`FindAnyObjectByType` skip si présent). OK pour re-run.

**Verification** : `npm run test:crowdef` ou compile clean Unity. Pas de wiring scene faisable depuis CLI — Mike click le menu Editor après merge (cf §"Étape Mike").

**Commit** : `feat(editor)(R0-1): BuildMainSceneTool add 6 Phase 5 UI singletons + missing systems`

---

### R0-2 — AudioMixer Exposed Name mismatch fix

**Type** : bug-fixer | **Effort** : 1 commit, ~15 min | **Zone** : Audio

**Files** :
- `Assets/Scripts/Systems/AudioController.cs` (lignes 316, 322, 332, 338, 345 environ)

**Action** :
Remplacer les string literals des 4 SetFloat() calls :
- `"Master_Volume"` → `"MasterVol"`
- `"SFX_Volume"` → `"SFXVol"`
- `"Music_Volume"` → `"MusicVol"`
- `"UI_Volume"` → `"UIVol"`

Vérifier match avec `Assets/AudioMixer/MixerGroups.mixer` (les 4 ExposedParameters sont nommés `MasterVol`, `MusicVol`, `SFXVol`, `UIVol`).

**Verification** : Play mode → 0 console error `Exposed name does not exist`.

**Commit** : `fix(audio)(R0-2): AudioController SetFloat match mixer ExposedParameters (MasterVol etc.)`

---

### R0-3 — WorldMapController guard scene scope

**Type** : bug-fixer | **Effort** : 1 commit, ~30 min | **Zone** : UI

**Files** :
- `Assets/Scripts/UI/WorldMapController.cs` (Start ~ligne 85)

**Action** :
Au début de `Start()`, ajouter :
```csharp
using UnityEngine.SceneManagement;
// ...
private void Start()
{
    var sceneName = SceneManager.GetActiveScene().name;
    if (sceneName != "WorldMap")
    {
        // controller misplaced (e.g. HUD UIDocument carrying it inadvertently)
        gameObject.SetActive(false);
        return;
    }
    // ... reste du Start existant
}
```

**Alternative** : retirer WorldMapController de toute UIDocument shared, le placer UNIQUEMENT sur l'UIDocument de WorldMap.uxml. Si nécessitant Inspector edit → laisser à Mike post-merge (mais bug-fixer doit au moins poser le guard runtime).

**Verification** : Play mode Main → 0 log `[WorldMapController] Start()`, 0 log `[WorldMap] Root name=HUD-container`.

**Commit** : `fix(ui)(R0-3): WorldMapController guard scene scope — skip if not in WorldMap scene`

---

### R0-4 — Matériaux Toon URP "rose"

**Type** : bug-fixer | **Effort** : 2-3 commits, ~1-2h | **Zone** : Visual

**Files** :
- `Assets/Materials/Toon/*.mat`
- `Assets/Materials/**/*.mat` (broader audit)
- `Assets/Shaders/Toon/*.shader`

**Action** :
1. **Audit** : `grep -r "m_Shader: {fileID: 0, guid: 0" Assets/Materials/` → liste les .mat avec shader pointer cassé.
2. **Vérifier shaders Toon** : `Toon.shader`, `Toon_Water.shader`, `Toon_Lava.shader`, `ToonURP.shadergraph` (etc.) compilent en URP 17.3.0. Si shader supprimé/renommé post-migration, restaurer (cf `git log -- Assets/Shaders/Toon/`).
3. **Retarget** : pour chaque .mat orphelin, soit pointer vers le shader correct, soit reimport avec script Editor.
4. **Reimport batch** : créer un Editor menu `Tools > CrowdDefense > Reimport Toon Materials` qui force `AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate)` pour tous les .mat Toon. Mike clique ce menu après merge.

**Verification** : Play mode → 3D viewport non-rose (vert grass, jaune sand, gris stone selon thème).

**Commits** :
- `fix(materials)(R0-4.1): retarget broken shader pointers in Toon .mat files`
- `feat(editor)(R0-4.2): add Tools > Reimport Toon Materials menu helper`

---

### R0-5 — Main Camera follow sensitivity fix

**Type** : bug-fixer | **Effort** : 1 commit, ~30 min | **Zone** : Systems

**Files** :
- `Assets/Scripts/Systems/CameraFollow.cs` (ou similaire — chercher composant attaché à Main Camera dans Main.unity)

**Action** :
1. `grep -rn "class.*Camera.*Follow\|class CameraController\|class IsoCamera" Assets/Scripts/`.
2. Audit follow logic : actuel = caméra suit hero mais ×plus rapide que hero. Probable cause : `transform.position += direction * speed * Time.deltaTime` au lieu de `Vector3.Lerp(transform.position, target, smoothFactor)`.
3. Default value sensitivity : 1.0 (caméra 1:1 avec hero), smoothDamp 0.2s.
4. Si SettingsRegistry expose un slider sensitivity, default 1.0 et clamp [0.5, 2.0].

**Verification** : Play mode → caméra suit hero 1:1, pas de glissement ×N. Hero stop → caméra stop dans 0.2s.

**Commit** : `fix(systems)(R0-5): CameraFollow sensitivity 1:1 hero, smoothDamp 0.2s`

---

### R0-6 — LevelSelect `level-grid` UXML element

**Type** : bug-fixer | **Effort** : 1 commit, ~20 min | **Zone** : UI

**Files** :
- `Assets/Scripts/UI/LevelSelectController.cs` (ligne 69)
- `Assets/UI/WorldMap.uxml` ou `Assets/UI/LevelSelect.uxml` ou `Assets/UI/MenuLevelSelect.uxml`

**Action** :
1. `grep -rn "level-grid" Assets/UI/*.uxml` → identifier où l'élément vit OU s'il a été renommé (e.g. `worldmap-tile-grid`).
2. Soit ajuster le `Q<VisualElement>("level-grid")` dans le controller pour pointer vers le nouveau nom, soit recréer le `<VisualElement name="level-grid"/>` dans l'UXML.

**Verification** : Play mode WorldMap → 0 error `level-grid VisualElement not found in UXML`.

**Commit** : `fix(ui)(R0-6): LevelSelect level-grid VisualElement query match UXML`

---

## Étape Mike post-Wave R (5-10 min)

Une fois les 6 tickets mergés sur main (notif T1 ou Mike check `git log`) :

```bash
cd /Users/mike/Work/crowd-defense
git pull --rebase
```

Dans Unity Editor (déjà ouvert pid 91607 ou équivalent) :

1. **Menu `Tools > CrowdDefense > Build Main Scene`** (R0-1 a complété le tool).
2. Vérifier dans Hierarchy que les 6 nouveaux UI controllers + ~20 singletons additionnels sont apparus comme GameObjects sous Systems/.
3. (Optionnel) Menu `Tools > CrowdDefense > Reimport Toon Materials` (R0-4.2).
4. Inspect Main Camera → vérifier component CameraFollow sensitivity = 1.0.
5. `Cmd+S` save Main.unity.
6. Commit + push :
   ```bash
   git add Assets/Scenes/Main.unity
   git commit -m "wire(scene)(R0-run): execute BuildMainSceneTool to wire Phase 5 singletons in Main.unity"
   git push
   ```

## Checklist verification Mike (Play mode)

- [ ] 0 warning `MonoSingleton auto-created`.
- [ ] 0 error `Exposed name does not exist`.
- [ ] 0 error `WaveManager No LevelData`.
- [ ] 0 error `level-grid not found`.
- [ ] 0 log `[WorldMapController] Root name=HUD-container` (en scène Main).
- [ ] Materials non-rose dans 3D viewport.
- [ ] Caméra cohérente (suit hero 1:1).
- [ ] Mobs spawn dans Wave 1 (clic VAGUE → ennemis arrivent du portail).
- [ ] HUD lisible (pas d'overlay debug obscur).

Si tout OK → **Phase 5 Recovery DONE**.
Si warnings résiduels → bug-fixer round 2 dispatched par Opus.

---

## Notes / contraintes

- **6 worktrees parallèles** — zones disjointes (Editor / Audio / UI / Materials / Camera / UXML), pas de conflit prévisible.
- **Unity-MCP HS** (`[MCP-FOR-UNITY] Connection failed` dans paste Mike) — pas de validation auto Editor possible, Mike valide manuellement.
- **Mike memory `feedback_no_webgl_test_for_now.md`** : Recovery validé en Editor Play mode uniquement.
- **Mike memory `feedback_no_code_bypass_wiring.md`** : wiring scène est correct ici, code refactor est minimal (just BuildMainSceneTool extension).

---

## /goal Recovery à coller dans nouvelle session crowd-defense

```
/goal Recovery Phase 5 runtime — runtime broken malgré code green, autonomie totale.

Source : .claude/plans/phase5-recovery-2026-05-17.md (lire en premier, en entier).

Mission : fix les ~10 problèmes runtime observés au Play mode (cf paste Mike 2026-05-17, drift D11 SÉVÈRE confirmé) en dispatch 6 tickets P0 R0-1..R0-6 EN PARALLÈLE (1 message, 6 Agent tool calls simultanés, subagent_type=bug-fixer ou feature-dev selon ticket, isolation=worktree, run_in_background=true).

Briefings autosuffisants dans le plan §"Tickets Wave R — 6 P0 parallèles".

Stop conditions :
- DONE : 6/6 tickets mergés sur main → notify Mike T1 "Phase 5 Recovery code work DONE — Mike action requise : Build Main Scene menu + save Main.unity + Play mode verification".
- Bloquage dur (matériaux Toon shader manquant non-trouvable, conflit worktree) → notify Mike T1 + STOP avec instructions reprise.
- Mike chat live override → comply immédiatement, puis reprendre autonomie.

Mode : autonomie totale, parallélisation 6 worktrees, pas de validation au fil de l'eau. Estimation ~3-5h wall-clock.

Supervisor /loop 30m reste actif côté autre session milan project — drift check charter §2.
```
