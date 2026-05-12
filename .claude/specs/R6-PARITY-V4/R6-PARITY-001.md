# R6-PARITY-001 — Textures Flux Schnell port V4 → Unity

**Sprint** : R6-PARITY-V4 (autonome 4h, cap ~19h30)
**Type** : feature-dev (Sonnet, worktree)
**Priorité** : P0 (look & feel critical)
**Audit ref** : `.claude/audit/2026-05-12-v4-parity-gap.md` row "Textures Flux Schnell"

## Contexte

V4 a 75 PNG textures Flux Schnell custom (10 thèmes × ground v1+v2 + 10 path tiles + 10 skybox + 21 VFX sprites + 8 water/lava anim frames + 4 pilote refs + 2 bridges). V6 Unity n'a actuellement que 3 PNG dans `Assets/Textures/`. C'est le gap visuel #1 (Mike pivot 14h48).

## Path V4 source confirmé

```
/Users/mike/Work/milan project/src-v3/public/textures/
├── anim/      (8 files)   — water_anim_01..04.png + lava_anim_01..04.png
├── ground/    (20 files)  — ground_{theme}_v1.png + _v2.png × 10 thèmes
├── sky/       (10 files)  — sky_{theme}.png × 10 thèmes
├── tiles/     (10 files)  — path_{theme}.png × 10 thèmes
├── vfx/       (21 files)  — sparkle/explosion/blood_splat/glyph/etc.
├── pilote/    (4 files)   — reference dev
├── bridge_wood.png
└── path_tile_reference.png
```

Total : **75 PNG**.

## Task

1. **Copy direct** (Mike validé décision 1) : `cp -r` les 75 PNG depuis V4 vers V6 `Assets/Textures/{Anim,Sky,Ground,VFX,Tiles,Pilote,Bridges}/`. Préserve la structure subdirectorielle.

2. **Unity import settings** (`.meta` files) configure pour chaque famille :
   - Ground (1024 tileable) : sRGB ON, mipmaps ON, wrap mode REPEAT, compression DXT5
   - Path tiles (512 tileable) : idem ground
   - Sky (2048 equirectangular) : sRGB ON, mipmaps OFF, wrap mode CLAMP, compression DXT5/BC7
   - VFX (sprites) : sRGB ON, mipmaps OFF, wrap mode CLAMP, alpha is transparency ON
   - Anim (frames 512) : sRGB ON, mipmaps OFF, compression default
   - Pilote/Bridges : import standard

3. **Wire dans registries V6** :
   - Skybox : si `SkyboxRegistry` existe, wire ; sinon création stub (P0-A-003 fait ça spécifiquement, juste import textures dans ce ticket)
   - VFX : wire dans `VfxPool` si possible (target `Assets/Scripts/Visual/VfxPool.cs`) — set texture sheet sur ParticleSystem renderer pour effets sparkle/explosion/etc.
   - Ground : wire dans `MapRenderer.ApplyWorldTheme()` si possible — set texture sur material per thème

4. **Audit qualité** post-copy :
   - Vérifier résolutions Unity OK (1024/2048 OK, pas de downscale forcé)
   - Si textures sub-optimales (PBR maps missing : normal/roughness/metallic), **FLAG** dans self-report — pas de regen Flux dans ce ticket (séparé)

## Placeholder-first

Pas applicable ici — copy direct fichiers donc l'asset EST le deliverable. Mais le wire dans registries peut être placeholder (juste path reference) si le full wiring est complexe.

## Hard rules

- Hard cap 500 LOC C# par fichier modifié
- No feature creep (juste copy + wire basique)
- Self-report 100 mots max
- Charter §1 règles applicables

## Deliverable

- Commit `feat(parity-v4-001): textures Flux Schnell 75 PNG ported V4 → Unity Assets/Textures/`
- Self-report :
  - Files copied : N PNG (par sous-dossier count)
  - Files wired : registries modifiés
  - Audit qualité : OK / FLAG regen Flux for {liste}
  - Compile OK : y/n
  - Commit hash

## Time estimate

~30-60 min copy + import settings + wire basique.