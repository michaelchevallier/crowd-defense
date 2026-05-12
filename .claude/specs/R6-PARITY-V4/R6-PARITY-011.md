# R6-PARITY-011 — Castle/VFX skins (12 manquants : 8 castle + 4 vfx)

**Sprint** : R6-PARITY-V4 (Batch P1-B, WT4')
**Type** : feature-dev (Sonnet, worktree)
**Priorité** : P1 #8
**Source** : audit V4 parity gap C — look&feel diff

## Contexte

V4 a 12 skins thématiques manquants en V6 :
- 8 castle skins (1 par thème core : plaine, foret, desert, glacier, volcan, marais, espace, cyberpunk)
- 4 vfx-themed pickup skins (gold variant per faction)

V6 actuel : 1 castle mesh générique pour tous thèmes.

## Task

1. Inventaire V4 castle/vfx skin sources :
   - `/Users/mike/Work/milan project/src-v3/data/assets/castles/` ou similar
   - `ls` pour confirmer exact paths + count
2. Pour castle skins :
   - **Option A — Texture swap only (placeholder-first)** : si V6 castle mesh générique a un Material principal, swap texture per thème via `CastleSkinController.cs` (Subscribe OnLevelStart, set `_MainTex` matching thème). Si textures castle V4 existent dans `Assets/Textures/Castle/` (R6-PARITY-001 import), wire directement.
   - **Option B — Custom mesh per thème** : requires Blender MCP server `uvx blender-mcp` start. Importer GLTF V4 + bake textures + assign Materials. Plus lourd, à faire en cas d'option A insuffisante.
   - **Décision agent** : start placeholder A, escalate B si textures absentes ou résultat insuffisant (commit + flag dans self-report pour follow-up R6-PARITY-011-CUSTOM-MESH).
3. Pour 4 VFX gold pickup skins : variants `coin_gold_<faction>` (gold base + faction tint). Wire dans VfxPool `SpawnCoinBurst(faction)` overload.
4. Créer `Assets/Scripts/Visual/CastleSkinController.cs` (cap ≤300 LOC attendu).

## Blender MCP décision (Mike instruction)

- État : `claude mcp list` → `blender: uvx blender-mcp ✗` offline par défaut
- **Décision exec** : ne PAS start Blender MCP en pré-flight. Agent dispatched débute en option A (texture swap). Si agent identifie besoin custom mesh, agent flag dans self-report → ticket follow-up `R6-PARITY-011-CUSTOM-MESH` batch P2.

## Exploit Unity

- `MaterialPropertyBlock` pour swap texture sans instancier nouveau Material
- `Texture2DArray` si beaucoup de textures (perf GPU instancing)

## Hard rules

- Cap 500 LOC CastleSkinController.cs
- No feature creep (juste skin swaps + 4 vfx variants, pas refonte castle gameplay)
- Compile gate
- Self-report 100 mots max

## Deliverable

- Commit `feat(parity-v4-011): Castle/VFX skins — 8 castle theme swaps + 4 vfx faction variants (placeholder-first)`
- Self-report : 8 castle skins (X/8 fonctionnels), 4 vfx variants (X/4), Blender MCP needed y/n (justify), custom mesh follow-up flagged y/n, compile OK, commit hash

## Time estimate

~5h placeholder-first (option A), ~8h si option B Blender MCP custom mesh
