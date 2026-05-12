# R6-PARITY-002 — PathTiles fidèle V4 port

**Sprint** : R6-PARITY-V4
**Type** : feature-dev (Sonnet, worktree)
**Priorité** : P0 (visuel #1 du jeu, Mike validé)
**Audit ref** : `.claude/audit/2026-05-12-v4-parity-gap.md` row "PathTiles segments visual"

## Contexte

V4 `src-v3/systems/PathTiles.js` 600 LOC = rendu visuel chemins entre ennemis spawn → castle. Segments droits + courbes + T-junctions + cross + bridges wood (sur water) + bridges lava-crossing. V6 actuel : `Assets/Scripts/Systems/PathVariant.cs` 30 LOC seulement = **gap massif** rendu chemins.

## Task

1. **Read V4 source** : `/Users/mike/Work/milan project/src-v3/systems/PathTiles.js` (600 LOC).

2. **Port vers Unity** : créer `Assets/Scripts/Systems/PathTilesController.cs` (cible ≤500 LOC, charter §1 règle #3) :
   - Détection topologie cellule path basé sur voisins N/S/E/W (segment droit, courbe, T, cross)
   - Mesh selection par topologie (8 segments distincts au minimum : NS, EW, NE, NW, SE, SW, T, +)
   - Wood bridges : si segment traverse cell water (`W`) avec marker pont
   - Lava bridges : si segment traverse cell lava (`L`) avec marker pont
   - Material per thème (10 thèmes : plaine/foret/desert/marais/glacier/volcan/foire/espace/submarin/medieval/cyberpunk)

3. **Exploit Unity capabilities** (Mike addendum) :
   - URP shader animé sur water bridges (Toon_Water_Animated shader existant V6)
   - Emissive material sur lava bridges
   - URP fallback Toon_Lit pour bridges wood standard

4. **Placeholder-first** : si textures path V4 (`Assets/Textures/Tiles/path_{theme}.png` après R6-PARITY-001) **pas encore importées** : utilise couleur unie material par thème en placeholder.

5. **Integration** : wire dans `Assets/Scripts/Systems/MapRenderer.cs` ou `Assets/Scripts/Systems/PathManager.cs` pour spawn meshes sur cells path détectées.

## Hard rules

- Cap 500 LOC `PathTilesController.cs` (charter §1)
- No feature creep (juste segments + bridges)
- Self-report 100 mots max

## Deliverable

- Commit `feat(parity-v4-002): PathTiles V4 port — 8 segment types + wood/lava bridges + 10 themes materials`
- Self-report :
  - LOC ajoutées
  - Files modifiés
  - Topologies implémentées (8/8 ?)
  - Bridges types (wood + lava OK ?)
  - Placeholder used (couleur unie ?) ou full texture
  - Compile OK : y/n
  - Commit hash

## Time estimate

~3-6h (high complexity port 600 LOC JS → Unity C#). Placeholder-first permet commit court si gen texture longue.