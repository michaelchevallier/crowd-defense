# Instructions to exec (canal SUPERVISEUR → EXEC)

> Le superviseur écrit ici quand il détecte un drift ou doit donner un ordre.
> L'exec lit ce fichier à chaque wakeup de son loop sprint AVANT tout dispatch.
> Format : 1 instruction = 1 section avec timestamp.

## Last ack expected from exec

`acks/2026-05-12-HHhMM-parity-v4-go-ack.md` après l'instruction PARITY-V4-GO ci-dessous (Mike valide 5 recos + addendum Unity capabilities).

---

## Instructions

### 2026-05-12 14h00 — BOOTSTRAP

**Type** : SETUP
**From** : Mike (via Opus session milan project)
**Action** : Tu lis ce fichier à chaque wakeup. Tu écris un ack dans
`.claude/supervisor/acks/` après lecture. Cf `charter.md` pour règles
complètes. Cf `README.md` pour le mécanisme global. Continue ton sprint
R6-01 normalement.

**Ack expected** : `.claude/supervisor/acks/2026-05-12-HHhMM-bootstrap-ack.md`
avec confirmation lecture charter + README + intention de check ce fichier
à chaque wakeup futur.

**Status** : ✅ Done (ack `2026-05-12-14h05-bootstrap-ack.md`)

---

### 2026-05-12 14h12 — CRON-AUDIT

**Type** : STOP-CONDITIONAL + AUDIT
**From** : Opus superviseur (cron 8a918f1a actif côté milan project, interval 30 min)
**Trigger** : Mike a demandé explicitement "de l'autre côté on a des crons qui sont mis en place aussi je pense qu'il faut les faire sauter non ?"

**Action obligatoire AVANT tout autre traitement** :

1. **Liste TOUS les crons actifs dans ta session** via `CronList` tool :
   - Note ID + cron expression + prompt + recurring + créé quand
2. **Liste TOUS les `ScheduleWakeup` actifs** :
   - Check via `TaskList` + `Monitor` outputs si applicable
   - Note les wakeups scheduled dans le futur
3. **Pour chaque cron / wakeup détecté, classe** :
   - **AUTHORIZED** : cron qui correspond à un sprint actif validé par Mike
     ET listé dans `.claude/supervisor/active-sprint.md` section "Time cap" ou similaire
   - **UNAUTHORIZED** : tous les autres (legacy ScheduleWakeup 270s drift,
     polish loops, etc.)
4. **`CronDelete` / `TaskStop` chaque UNAUTHORIZED**.
5. **Confirme zéro cron actif non-autorisé.**

**Pourquoi** : la cause root du drift précédent (1012 commits dont 60%
inventions hors V4) était un `ScheduleWakeup 270s` hardcodé qui dispatchait
"more polish" sans gate Q-N. Charter §1 règle #1 : "Aucun ScheduleWakeup,
/loop ou cron pour démarrer un sprint sans message Mike explicite".

**Ack expected** : `.claude/supervisor/acks/2026-05-12-HHhMM-cron-audit-ack.md`
contenant :
- Liste exhaustive crons + wakeups détectés (ID + raison classification)
- Crons supprimés (avec leur ancien ID)
- Crons conservés (avec justification AUTHORIZED + ref active-sprint.md)
- Status (DONE / BLOCKED + raison)

**Si tu trouves un cron AUTHORIZED qui correspond à un sprint en cours** :
ce sprint a probablement été lancé sans validation Mike chat. Mark
BLOCKED + push notif Mike pour clarification — ne kill PAS si ambigu,
mais ne re-dispatch PAS non plus tant que Mike clarifie.

**Si zéro cron / wakeup actif** : ack avec "0 crons, 0 wakeups, charter
§1 règle #1 respectée". Status DONE.

**Pas d'autre action ce wakeup** (pas de dispatch agent, pas de nouveau
commit feature). Mike attend toujours validation triage table.

---

### 2026-05-12 14h48 — 🛑 PIVOT-V4-PARITY (priorité ABSOLUE, override R6-02)

**Type** : STOP-CURRENT + PIVOT-STRATEGY
**From** : Mike chat direct (via superviseur)
**Urgence** : T1 (priorité absolue, lis-moi avant tout autre action)

## Mike's clarification de stratégie (chat direct 14h47)

> "Ce qui me gêne c'est le drift du projet Unity qui pour le coup a pas la
> moitié de ce que l'on a dans le projet V4. À la limite si c'est plus joli
> et propre sur certains sujet ça me dérange pas. Par contre ce que je veux
> c'est qu'on arrive à parité V4 le plus vite possible.
> Après ce qui m'intéresse c'est d'arriver à cette parité le plus possible
> mais c'est OK pour V6 d'avoir des features en + ou de la déco en +."

**Nouvelle stratégie validée Mike** :
- Priorité absolue : **parité V4 le plus vite possible**
- Features V6 en + : OK si plus joli/propre (pas un drift à corriger)
- Bloat LOC : pas le problème principal, accept temporary
- DELETE pass agressif R6-02 : **CONTRAIRE à la nouvelle priorité**

## Actions obligatoires

### 1. PAUSE R6-02 immédiatement

- Stop tous worktrees actifs sur tickets DELETE restants (14 tickets non-faits)
- Ne pas revert les 10 DELETE déjà committed (perte mineure visuelle, ne
  vaut pas le travail de revert)
- Marque les 14 tickets restants `[FROZEN]` dans `.claude/specs/R6-EXEC/_backlog.md`
  avec note "frozen — Mike pivot V4 parity 14h48"
- ScheduleWakeup loop R6-02 : EXIT, no rescheduleWakeup
- Update `.claude/supervisor/active-sprint.md` :
  - Sprint : R6-02 → PAUSED
  - Nouveau sprint : R6-PARITY-V4 (en préparation, attente backlog)

### 2. Produit un audit V4 parity GAP (priorité immédiate)

Mike confirmation additionnelle (14h49) : **look & feel V4 >> V6, surtout
textures + niveau de finition**. Audit doit inclure cette dimension visuelle.

Spawn 3 agents en parallèle (charter §1 règle #9 max 4 autorisée) :

- **Audit A — Inventaire V4 features (gameplay)** : Read tous les fichiers
  `/Users/mike/Work/milan project/src-v3/entities/*.js` +
  `systems/*.js` + `data/*.js` + `data/levels/world*.js`. Produit liste
  exhaustive des features V4 (1 ligne par feature, ~30-80 features estimées
  d'après plan archive et triage table).
- **Audit B — Inventaire V6 features (gameplay)** : Read tous les fichiers
  `Assets/Scripts/Entities/*.cs` + `Systems/*.cs` + `Data/*.cs` + scenes +
  prefabs registries. Produit liste exhaustive des features V6.
- **Audit C — Look & feel diff (visuel)** : Read et compare :
  - V4 textures : `/Users/mike/Work/milan project/public/textures/` +
    `tools/textures_manifest.json` (cf memory reference_flux_local) — où
    sont stockées, combien, quels assets, pipeline Flux Schnell utilisé
  - V6 textures : `crowd-defense/Assets/Textures/` (existe ?) +
    `Assets/Resources/` + scene materials
  - V4 visual : index.html theme system, scene gradients, particles,
    shaders procéduraux JS (`src-v3/Theme.js`, `src-v3/Particles.js`,
    `src-v3/JuiceFX.js`, etc.)
  - V6 visual : URP setup, materials, post-processing volumes, lighting,
    Quaternius .gltf assets, shaders custom (Toon_Water, etc.)
  - Liste les éléments visuels V4 absents/inférieurs V6 (textures par
    famille, lighting setup, post-processing, materials quality, etc.)

- **Diff consolidé** : Produit `.claude/audit/2026-05-12-v4-parity-gap.md`
  avec 3 sections :
  1. **Features gameplay** table : Feature V4 → Status V6 (`PRESENT` /
     `PARTIAL` / `MISSING` / `INVENTED-V6-ONLY`) + LOC estimé port + complexité
  2. **Look & feel** table : élément visuel V4 → status V6 (port effort
     estimé) — textures, lighting, materials, post-fx, particles, shaders
  3. **Synthèse + priorisation** : tri par 1) MISSING + high gameplay
     impact, 2) PARTIAL + high impact, 3) MISSING look&feel high visual
     impact, 4) MISSING + low impact, 5) INVENTED-V6-ONLY garde sauf si
     nuisible

Cible : audit livré en <30 min, ~50-100 rows total (60% gameplay + 40%
look&feel).

### 3. Backlog R6-PARITY-V4 (à construire post-audit)

Une fois audit livré + Mike valide priorités → produit
`.claude/specs/R6-EXEC/_backlog-parity.md` avec :

- 1 ticket par feature V4 MISSING ou PARTIAL
- Prioritization : high gameplay impact d'abord
- Estimation LOC par ticket
- Dependencies si applicable

Soumets liste à Mike pour validation avant dispatch.

### 4. Mode dispatch (à valider Mike post-backlog)

Mike décidera : supervisé batch ou autonome.

### 5. R6-03 god class refacto + R6-04 Q1-Q18 implem

- R6-03 (refacto god classes) : PARK (peut attendre, pas urgent vs parité)
- R6-04 (Q1-Q18 implem) : à fusionner dans R6-PARITY-V4 (les Q-N qui
  correspondent à V4 features manquantes deviennent des tickets parity)

## Ack expected

`.claude/supervisor/acks/2026-05-12-HHhMM-pivot-v4-parity-ack.md`
contenant :
- Confirmation PAUSE R6-02 effective (worktrees stoppés, backlog frozen)
- Status agents Audit A + B (dispatched, ETA)
- Path audit final attendu
- Status workflow update active-sprint

## Status

⏳ pending exec ack + audit V4 parity livré

## NE PAS

- Continuer R6-02 DELETE tickets
- Spawner agent VFX/polish
- Modifier god classes (R6-03 parked)
- Ajouter feature non-V4 (sauf si Mike valide explicitement)

---

### 2026-05-12 15h30 — 🟢 PARITY-V4-GO (Mike valide 5 recos + addendum scope Unity)

**Type** : GO-SPRINT (dispatch autorisé R6-PARITY-V4 P0)
**From** : Mike chat direct (via superviseur)
**Audit source** : `.claude/audit/2026-05-12-v4-parity-gap.md`

## Mike's 5 décisions validées en bloc

| # | Décision | Verdict |
|---|---|---|
| 1 | Pipeline textures Flux | **Copy direct PNG** depuis `/Users/mike/Work/milan project/src-v3/public/textures/` (75 PNG confirmés présents : anim×8 + sky×10 + ground×N + vfx×22 + tiles×N + pilote×5) → `crowd-defense/Assets/Textures/{Anim,Sky,Ground,VFX,Tiles,Pilote}/`. Si qualité Unity sub-optimale (résolution, normal maps), regen Flux Schnell autorisée (cf addendum) |
| 2 | PathTiles 600 LOC port | **OUI P0** — visuel #1 du jeu, worth 6-8h |
| 3 | Castle/VFX skins (12 manquants) | **Tous P1, après P0** |
| 4 | Schools mapping | **Keep V6 5 schools** (extension propre, Mike OK avec features en +) |
| 5 | Mode dispatch | **Autonome 4h sur P0 1-5** (cap fin ~19h30 local) |

## Addendum CRITIQUE Mike (scope extends V4 strict)

> "Unity offre des capacités nouvelles si ça permet de rendre le jeu plus beau
> et plus sympa il faut les exploiter c'est 50% de l'intéret initial de la
> migration donc il faut prendre ça en compte dans le scope. Si il faut reviser
> le download de texture et autres c'est possible aussi. Souviens toi qu'on a
> acces au blender mcp. Tout ce qui est generation d'asset si c'est long ne
> doit pas etre bloquant (ie. placeholder en attendant l'asset)"

**Implications scope R6-PARITY-V4** :

1. **Exploit Unity capabilities** — URP shaders modernes, PBR materials, post-processing per-volume, lighting baked/dynamic, ParticleSystem Unity-native, Animator state machines, NavMesh, Cinemachine, Volumetric fog : utilise partout où ça **améliore vs V4 sans bloquer parité**. C'est 50% intérêt migration → scope inclut Unity-native quality, pas seulement port 1-1.

2. **Textures révision OK** — copy direct est la base, MAIS si PNGs V4 sub-optimaux Unity (résolution 1024 vs 2048, PNG sans alpha proper, manque normal/roughness maps PBR), regen Flux Schnell autorisée avec prompts adaptés Unity. Pipeline `/Users/mike/Work/milan project/tools/gen_textures.py` ComfyUI:8188 local (cf memory reference_flux_local).

3. **Blender MCP disponible** (état actuel : `claude mcp list` → `blender: uvx blender-mcp - ✗ Failed to connect` — serveur MCP installé mais offline). Si tu as besoin de mesh custom (castle skins per thème, decor props enrichis), ack et propose plan court : (a) start Blender MCP server (`uvx blender-mcp` background), (b) generate mesh via MCP API, (c) import .blend → .gltf → Unity.

4. **Asset gen non-bloquant** — placeholder-first architecture obligatoire sur tickets >1h gen :
   - Implémente système (PathTiles, weather, skybox material, VFX wiring) avec **placeholder simple** : couleur unie + label texte + bounding box visible
   - Commit ticket avec placeholders → unblock dispatch suivant
   - Asset swap en parallèle (worktree séparé ou batch ultérieur)
   - Mike doit pouvoir tester gameplay/scope dès commit, look final swap ensuite

## Action exec dispatch immediate

### Batch P0-A (4 agents feature-dev parallèles worktree, charter §1 max 4 OK)

1. **R6-PARITY-001 textures port** : copy 75 PNG V4 → `Assets/Textures/{Anim,Sky,Ground,VFX,Tiles,Pilote}/` + audit qualité résolution Unity + dossier `.meta` Unity import settings (sRGB, mipmaps, compression). Wire dans MaterialRegistry / SkyboxRegistry / VfxPool. Si qualité OK pas de regen. Si gap : flag + propose regen Flux dans self-report.

2. **R6-PARITY-002 PathTiles fidèle V4** : port `src-v3/systems/PathTiles.js` 600 LOC → Unity. Segments droits + courbes (radius cell) + T-junctions + cross + bridges wood (sur water) + bridges lava-crossing. **Exploit Unity** : URP shader animé sur water bridges + emissive lava bridges. **Placeholder OK** sur bridges visuels complexes : couleur unie d'abord, swap shader après.

3. **R6-PARITY-003 Skybox per-theme** : import 10 skybox PNG equirectangular Flux V4 → `Assets/Textures/Sky/` + 10 Unity Skybox materials (shader `Skybox/Panoramic`) + `SkyboxController` auto-switch on level theme change. **Exploit Unity** : Skybox cubemap convolution pour reflections + ambient indirect lighting. **Placeholder OK** : couleur uniforme par thème en attendant cubemap conv.

4. **R6-PARITY-004 VFX sprites import** : import 22 PNG VFX V4 → `Assets/Textures/VFX/` + Unity ParticleSystem texture sheet animation (sparkle_gold/explosion_big/blood_splat/glyph/...). Wire dans VfxPool / SpawnX. **Exploit Unity** : Particle System sub-emitters + collision modules + noise modules pour qualité supérieure V4.

### Batch P0-B (1 agent après P0-A start, ou en parallèle si capacité)

5. **R6-PARITY-005 Enemy types audit complet** : vérifier les 28 enemy types V4 + leur behaviors specifiques :
   - assassin, warlord_boss (charge sprint), corsair_boss, imp, dragon_boss (fire breath cone)
   - apocalypse_boss (4 phases : P1 normal → P2 invul + summons → P3 speed×2 → P4 AoE pulse 360°)
   - cosmic_boss, kraken_boss (tentacle slam), wizard_king (teleport + projectile rain), ai_hub (drone summons)
   Pour chaque type manquant ou behavior incomplet : ticket fix dans batch P0-B implementation.

## Time cap

- Sprint R6-PARITY-V4 batch P0 : **4h depuis ack** (cap ~19h30 local si ack 15h30)
- Self-report obligatoire chaque commit (charter §1 règle #8, 100 mots max)
- Compile gate post-commit (`mcp__UnityMCP__read_console` errors only)
- Push ack après dispatch B5 (5 tickets en route)

## Ack expected

`.claude/supervisor/acks/2026-05-12-HHhMM-parity-v4-go-ack.md` contenant :
- Tickets specs créés : 5 paths `.claude/specs/R6-PARITY-V4/R6-PARITY-001..005.md`
- Batch P0-A 4 worktrees créées + dispatched (paths + branch names + ETA)
- Batch P0-B status (dispatched parallèle ou queued post-P0-A first commits)
- Confirmation placeholder-first architecture documentée dans chaque ticket
- Blender MCP : décision (start server now si besoin mesh / not needed pour P0-A 1-5)
- Backlog R6-PARITY-V4 P1/P2/P3 esquisse (10 tickets restants pour batches suivants)

## Constraints rappel

- **Hard cap 500 LOC par fichier C#** (charter §1 règle #3)
- **No-feature-creep clause** (§1 règle #4) : opportunités → `.claude/backlog/R6-found-during-exec.md`
- **No Sub-Opus spawn** (§1 règle #10) : Sonnet feature-dev uniquement
- **Self-report 100 mots max** (§1 règle #8)

## Status

⏳ pending exec ack + dispatch batch P0-A

