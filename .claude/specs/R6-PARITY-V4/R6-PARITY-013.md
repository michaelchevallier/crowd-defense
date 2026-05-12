# R6-PARITY-013 — SceneDecor port (placeNatureProp arbres/rochers/buissons)

**Sprint** : R6-PARITY-V4 (Batch P1-B, WT3')
**Type** : feature-dev (Sonnet, worktree)
**Priorité** : P1 #7
**Source V4** : `/Users/mike/Work/milan project/src-v3/systems/SceneDecor.js` (333 LOC)

## Contexte

V4 SceneDecor place props nature (arbres, rochers, buissons) sur cells D (decor) ou T (tile-decor) du grid map, selon thème + THEME_PALETTE. V6 probablement utilise Quaternius GLTF directs sans système de placement seeded → manque de variation thématique.

## Task

1. Read source V4 `/Users/mike/Work/milan project/src-v3/systems/SceneDecor.js` (333 LOC) — comprendre :
   - `placeNatureProp(x, y, theme, seed)` : choisit prop random parmi pool thématique
   - `THEME_PALETTE` : pour chaque thème, liste de prefab keys + scale + tint variations
   - Placement seeded par cell + theme + level seed pour déterminisme replay
2. Créer `Assets/Scripts/Systems/SceneDecorController.cs` (cap ≤500 LOC) :
   - Subscribe `LevelEvents.OnLevelStart`
   - Loop cells map, pour chaque cell type D ou T :
     - Si cell type D : place 1 prop random thématique (PRNG seeded)
     - Si cell type T : place 1 ou 2 props plus petits (buissons, herbe)
   - THEME_PALETTE C# : `Dictionary<LevelTheme, DecorPaletteSO>` ou ScriptableObject `DecorPalette.asset` par thème
3. **Assets** :
   - Si V6 a déjà Quaternius GLTF dans `Assets/Models/` ou `Resources/Prefabs/Decor/`, wire ces prefabs dans DecorPalette.
   - Sinon, placeholder : `GameObject.CreatePrimitive(PrimitiveType.Cylinder)` colorisé selon thème.
4. **Exploit Unity** : GPU instancing pour décor count élevé, MaterialPropertyBlock pour tint variations sans Material instances multiples.

## Hard rules

- Cap 500 LOC SceneDecorController.cs
- No feature creep (juste placement props nature, pas castle/tower skins = autre ticket R6-PARITY-011)
- Compile gate (GPU instancing config via Material asset, vérifier shader supporte)
- Self-report 100 mots max

## Deliverable

- Commit `feat(parity-v4-013): SceneDecor port — placeNatureProp + 10 theme palettes + seeded placement`
- Self-report : themes covered (X/10), props variations per theme (count), GPU instancing y/n, compile OK, commit hash

## Time estimate

~4h
