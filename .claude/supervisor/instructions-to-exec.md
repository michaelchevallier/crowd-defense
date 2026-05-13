# Instructions to exec (canal SUPERVISEUR → EXEC)

> Le superviseur écrit ici quand il détecte un drift ou doit donner un ordre.
> L'exec lit ce fichier à chaque wakeup de son loop sprint AVANT tout dispatch.
> Format : 1 instruction = 1 section avec timestamp.

## Last ack expected from exec

`acks/2026-05-12-HHhMM-stop-runtime-critical-ack.md` après l'instruction STOP-RUNTIME-CRITICAL ci-dessous (drift D10/D11 confirmé).

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

---

### 2026-05-12 15h47 — 🔧 DEPLOY-REFRESH (catégorie A délégué, non-bloquant)

**Type** : INFO + ACTION
**From** : Opus superviseur (cron check #6)
**Trigger** : qa-tester spawn report — build gh-pages obsolète (50c5420 @ 14:01 < commits P0 15:39-15:46)

## Détection

Le smoke test /v6/ révèle que les 4 P0 commits récents (`02d44da` textures + `0cbaae9` skybox + `a7e404c` PathTiles + `f4a3744` enemy audit + `78a9f15` Hero.cs fix) **ne sont pas déployés** sur gh-pages. Le dernier deploy commit visible est `50c5420` (R59 build, 14:01) — bien avant le sprint R6-PARITY-V4. Le QA ne peut pas valider live tant que le build n'a pas tourné.

## Action attendue

À la fin de batch P0-A (ou maintenant si tous merges P0 mergés + compile OK) :

1. Exécute `tools/auto-build-loop.sh` (si pas déjà actif) — OU manual `tools/build.sh` puis push gh-pages
2. Confirme nouveau commit `deploy: WebGL rN+1 build <hash>` apparaît dans gh-pages branch
3. Self-report build size, build duration, et hash deploy commit dans next ack

## Pas un drift

C'est de l'hygiène CI/CD, pas un drift charter §2. Catégorie A délégué.
Pas de Mike notif déclenchée (T3 silent), Mike sait que le pipeline existe.

## Status

⏳ pending exec deploy trigger après P0-A complet (en cours, auto-build-loop bg lancé 15:56:20)

---

### 2026-05-12 16h01 — 🟢 PARITY-V4-P1-GO (Mike GO autonome batch P1)

**Type** : GO-SPRINT (dispatch batch P1 autorisé)
**From** : Mike chat direct ("enchaine ces trucs la en autonomie")
**Mode** : Autonome 4h (time cap depuis ack)
**Source** : audit batch P0 + audit V4 parity gap + Q-vfx-bindings-cap (réponse A-PARITY-V4-vfx-bindings-cap)

## 8 tickets P1 à dispatcher

| Ordre | Ticket | Quoi | LOC estimé | Time | Notes |
|---|---|---|---|---|---|
| **1** | **R6-PARITY-004-REFACTOR** | **Split VfxPoolBindings 555 → 2 fichiers <500 LOC** (partial class OU extraction VfxPoolTextures.cs textures-only + VfxPoolBuilders.cs Build*Module) | ~30-50 (refacto) | 30 min | **PRIORITY ABSOLUE** — résout violation charter §1 règle #3, doit être en TÊTE du batch (cf A-PARITY-V4-vfx-bindings-cap). Bug-fixer Sonnet OK |
| 2 | R6-PARITY-005-IMPL | 5 enemies PARTIAL : wizard_king teleport+rain (~120) + warlord charge (~15) + dragon fire breath (~10) + ai_hub drone burst (~40) + kraken tentacle slam (~80) | ~265 | 4-6h | Cf audit `f4a3744` table 5 PARTIAL. Sources V4 : `src-v3/entities/Enemy.js` + behaviors specifiques |
| 3 | R6-PARITY-004-IMPL | Wire 9 VFX textures unmapped (electric_cloud, explosion_small, glyph_dark, heal_aura, lightning_bolt, poison_cloud, shield_aura, slow_aura, smoke_gray) via spawn methods + ParticleSystem texture sheets | ~80-120 | 2-3h | Suite logique de R6-PARITY-004, exploit Unity ParticleSystem.Module |
| 4 | R6-PARITY-010 | Weather effects port (clouds plaine/desert, spores foret, sable desert/storm, sky gradient billboard, transition thème) | ~150 | 4-6h | Source V4 : `src-v3/systems/Weather.js` 143 LOC. Exploit Unity ParticleSystem world-space + VFXGraph si pertinent |
| 5 | R6-PARITY-012 | V4 dynamic EventManager mid-wave : sand_storm (range -25%, speed +15%) + lava_surge (1-3 tours inondées + castle dmg) + carousel_spin (ennemis changent path 30%) | ~80 | 3h | Source V4 : `src-v3/systems/EventManager.js`. V6 EventSystem 12 events narratifs déjà présent, ajouter ces 3 dynamiques mid-wave |
| 6 | R6-PARITY-013 | SceneDecor port (`placeNatureProp` arbres/rochers/buissons selon thème + THEME_PALETTE + placement seeded sur cells D/T) | ~150 | 4h | Source V4 : `src-v3/systems/SceneDecor.js` 333 LOC. V6 utilise probablement Quaternius GLTF directs, à wire dans LevelLoader |
| 7 | R6-PARITY-014 | Boss phases complete : Apocalypse 4 phases (P1 normal → P2 invul+summons → P3 speed×2 → P4 AoE pulse 360°) + charge sprint warlord/brigand_boss + fire breath cone dragon_boss | ~80 | 5h | Cf audit enemy R6-PARITY-005 (PARTIAL) — overlap possible avec ticket 2, à coordiner. Sources V4 : Enemy.js boss behaviors |
| 8 | R6-PARITY-011 | Castle/VFX skins (12 manquants : 8 castle + 4 vfx). Mesh GLTF par thème OU Blender MCP si custom modeling | ~100-200 + assets | 5-8h | Blender MCP : `claude mcp list` → `blender: uvx blender-mcp ✗` actuellement offline. Si custom mesh requis : exec décide (a) start `uvx blender-mcp` background → use MCP API → import GLTF, OU (b) report blocker dans question superviseur. **Placeholder-first OK** : couleur unie par thème en attendant assets. |

## Stratégie batch (charter §1 règle #9 max 4 worktrees simultanés)

**Batch P1-A** (4 worktrees parallèles, dispatch immédiat post-ack) :
- Worktree 1 : R6-PARITY-004-REFACTOR (priorité absolue, bug-fixer Sonnet OK, ~30 min)
- Worktree 2 : R6-PARITY-005-IMPL (5 enemies, ~4-6h)
- Worktree 3 : R6-PARITY-004-IMPL (9 VFX wire, ~2-3h)
- Worktree 4 : R6-PARITY-014 boss phases (~5h, overlap coord avec WT2)

**Batch P1-B** (4 worktrees, dispatch après 1er slot libéré par WT1 finish ~30 min) :
- Worktree 1' : R6-PARITY-010 Weather (~4-6h)
- Worktree 2' : R6-PARITY-012 Dynamic events (~3h)
- Worktree 3' : R6-PARITY-013 SceneDecor (~4h)
- Worktree 4' : R6-PARITY-011 Castle skins (~5-8h, Blender MCP décision)

## Time cap

- Sprint R6-PARITY-V4 batch P1 : **4h depuis ack** (cap ~20h00 local si ack 16h00)
- Si non-terminé à 20h00 : auto-stop, batch remaining → P2 ou next session
- Charter §4 hard terminate respect : Mike STOP message → STOP immédiat

## Addendum scope Unity (rappel — same as PARITY-V4-GO)

Exploit Unity capabilities partout où ça améliore vs V4 sans bloquer parité :
- URP shaders modernes, PBR materials, post-processing per-volume
- Lighting baked/dynamic, ParticleSystem Unity-native, sub-emitters, Collision3D
- Animator state machines, NavMesh agents (boss phases), Cinemachine (boss cinematic)
- VFXGraph si compute-heavy weather
- Volumetric fog, ReflectionProbe (skybox)
- **Placeholder-first** sur tickets >1h gen asset (couleur unie + label texte, swap après)
- **Blender MCP** : autorisé à start server (`uvx blender-mcp`) pour ticket 8 castle skins si custom mesh requis

## Cleanup hygiène à intégrer (optionnel, ticket "found-during-exec")

- 18+ worktrees stale `git worktree list` → cleanup post-merge tickets (`git worktree remove --force` après chaque merge)
- Active-sprint.md à update pour refléter P0-A complete + P1 ACTIVE

## Constraints rappel charter §1

- **Hard cap 500 LOC par fichier C#** (règle #3) — TOLERANCE ZERO pour cette violation (R6-PARITY-004-REFACTOR résout précédent cas)
- **No Sub-Opus spawn** (règle #10) : Sonnet feature-dev ou bug-fixer
- **No feature creep** (règle #4) : opportunités → `.claude/backlog/R6-found-during-exec.md`
- **Compile gate post-commit** (règle #5) : `mcp__UnityMCP__read_console` errors only
- **Self-report 100 mots max** (règle #8) chaque ticket

## Ack expected

`.claude/supervisor/acks/2026-05-12-HHhMM-parity-v4-p1-go-ack.md` contenant :
- Tickets specs créés : 8 paths `.claude/specs/R6-PARITY-V4/R6-PARITY-004-REFACTOR.md`, `005-IMPL.md`, `004-IMPL.md`, `010.md`, `012.md`, `013.md`, `014.md`, `011.md`
- Batch P1-A 4 worktrees dispatched (paths + branch names + ETA)
- Batch P1-B status (queued vs dispatched if capacité)
- Blender MCP décision (start now / queue / Mike escalation)
- Time cap noté
- Pull main + lit `answers-from-supervisor.md` A-PARITY-V4-vfx-bindings-cap (réponse réelle depuis commit `fde107b`, applique reco split partial class OU extraction)

## Status

✅ DONE — batch P1 complete 8/8 en 30 min (commit `a49ed12` last)

---

### 2026-05-12 16h28 — 🔧 DEPLOY-REFRESH-2 (catégorie A délégué, non-bloquant)

**Type** : INFO + ACTION
**From** : Opus superviseur (cron check #9, sprint P1 complete)
**Context** : Build gh-pages actuel = `81ea28e` auto-build 1614 @ 16:12, couvre **seulement P0-A** complet (5 P0 + Hero.cs fix). Les 8 commits P1 (REFACTOR + 5 features + merge) ne sont **pas encore dans le build live**.

## Action attendue

Re-trigger `tools/auto-build-loop.sh` (ou manual `tools/build.sh` + push gh-pages) sur HEAD courant (`a49ed12`) — couvre **TOUS** P0-A + P1 (13 features livrées + 1 fix).

Self-report dans next ack/digest :
- Hash nouveau deploy commit
- Build size delta (vs `81ea28e` R1614)
- Build duration

## Rationale

- Mike peut désormais tester live tous les P0+P1 features via `https://michaelchevallier.github.io/crowd-defense/`
- qa-tester (background spawn check #9) ne peut pas valider P1 features live tant que build pas refreshé
- Sprint quasi-complet (P0+P1 sur 3 batches P0/P1/P2/P3 + REFACTOR) mérite snapshot deployable

## Pas un drift

CI/CD hygiène standard, catégorie A. Pas de Mike notif (T3 silent côté supervisor — déjà notifié sprint complete au check #9).

## Status

⏳ pending exec deploy trigger sur HEAD `a49ed12`

---

### 2026-05-12 16h32 — 🔧 POST-P1-FIXES (catégorie A délégué, non-bloquant)

**Type** : ACTION groupée
**From** : Opus superviseur (audit batch deep `a649d44a` complete, output `.claude/audit/2026-05-12-batch-p1-audit.md`)
**Verdict audit** : ⚠️ batch P1 = 1 critique + 4 mineures, **fonctionnellement complet, refs croisées OK, compile-readiness OK**

## 3 fixes groupés à dispatcher (1 bug-fixer Sonnet ~15 min total)

### Fix #1 — SPLIT EnemyBossBehaviors.cs (P0 critique)

**Violation** : `Assets/Scripts/Entities/EnemyBossBehaviors.cs` = **582 LOC** > cap 500 charter §1 règle #3 TOLERANCE ZERO.

**Origine** : Commit 014 a créé 446 LOC (partial Enemy boss methods : UpdateCharge, UpdateFireBreath, Apocalypse phases, EnrageVFX, EnrageRing). Merge `08d7229` ajoute +136 LOC (static class wizard_king teleport+rain + ai_hub burst + kraken tentacles) sans split.

**Fix proposé** : Extraire `static class EnemyBossBehaviors` (138 LOC : wizard/ai_hub/kraken) vers nouveau fichier `Assets/Scripts/Entities/EnemyBossBehaviorsStatic.cs`. Ramène partial class Enemy à ~444 LOC.

**Action** :
- Bug-fixer Sonnet : split + verify compile + commit `fix(parity-v4-p1): split EnemyBossBehaviors 582 → partial Enemy ~444 + static ~138 LOC (charter §1 cap)`

### Fix #2 — DynamicEventManager bug `_prevRangeMul/_prevSpeedMul` écrasés en boucle (mineur)

**Bug** : Dans `StartSandStorm` L83-100 et équivalents, `_prevRangeMul` (et `_prevSpeedMul` côté `lava_surge` ?) est réassigné à chaque itération du `foreach` → seul le dernier tower a sa valeur préservée. `StopSandStorm` restaure mauvaise valeur si entités hétérogènes (cluster boost ≠ 1f).

**Impact réel** : Zero si toutes towers `EventRangeMul=1f` au start (cas nominal). Bug subtil mais réel pour synergies cluster.

**Fix proposé** : Stocker les `_prev*` dans un `Dictionary<Tower, float>` puis restore from dict dans `StopSandStorm`. Ou rester sur mul global runner (cohérent V4 EventManager).

**Action** :
- Bug-fixer Sonnet : refacto fix + commit `fix(parity-v4-012): DynamicEventManager per-entity prev-mul tracking via Dictionary`

### Fix #3 — Regen VfxPoolFactions.cs.meta (hygiène)

**Issue** : `Assets/Scripts/Visual/VfxPoolFactions.cs.meta` absent du commit `46a48db`. Unity régénère un nouveau GUID au prochain open editor → warning console, pas blocker compile mais asset references inconsistants si fichier étendu later.

**Fix proposé** : Touch VfxPoolFactions.cs.meta (Unity Editor auto-regen au reopen) OU script manuel pour générer .meta vide.

**Action** :
- Bug-fixer Sonnet : si Unity Editor accessible, juste open project + close puis commit le .meta généré. Sinon manual generation.

## P2 backlog (différer, à discuter avec Mike post-P2/P3 decision)

### Backlog #1 — R6-PARITY-012-V4-FIDELITY (5 events V4 missing + trigger divergence)

V4 EventManager : 8 events data-driven via `level.events[]` (def.waveIndex === wave). Unity DynamicEventManager : 3 events random `% 5` auto-trigger.

**5 events V4 absents** : void_pulse / zero_g / undertow / battle_cry / hack.

**Question design** : revenir à data-driven `level.events[]` per V4 fidelity, OU keep Unity simplification mais documenter comme "intentional V4 divergence" ?

→ **Mike decision needed** (catégorie B escalation si Mike veut V4 stricte, sinon catégorie A si Mike accepte Unity simplification).

### Backlog #2 — R6-PARITY-011-COMPLETE (Foire + Medieval castle skin)

ThemeSkins[] couvre 8/10 themes. Foire et Medieval → default tint silencieux.

**Action P2** : créer 2 ThemeSkin entries (Foire + Medieval) avec texture-swap mappé.

## Ack expected

`.claude/supervisor/acks/2026-05-12-HHhMM-post-p1-fixes-ack.md` :
- Bug-fixer dispatched (paths fixes 1+2+3)
- Time estimate complete
- P2 backlog noté dans `.claude/backlog/R6-found-during-exec.md` (issues 4+5)

## Status

✅ DONE — 3 fixes shipped en ~10 min (commits 7817aeb + fd8f4a1 + ae7945b)

---

### 2026-05-12 17h17 — 🛑 STOP-RUNTIME-CRITICAL (drift D10/D11 confirmé)

**Type** : STOP-CURRENT + URGENT FIX
**From** : Opus superviseur — Mike a paste console output `/v6/` montrant crash runtime
**Drift criteria** : D10 (build runtime broken) + D11 (runtime exceptions ≥3) confirmés sur 1 check
**Drift report** : `.claude/supervisor/drift-reports/2026-05-12-17h17-runtime-crash.md`

## Détection

Mike a testé live `https://michaelchevallier.github.io/crowd-defense/v6/` (build R1706/R1718 deployed). Console browser montre :

1. **3 shaders URP not supported** : `Hidden/CoreSRP/CoreCopy`, `Hidden/Universal Render Pipeline/StencilDitherMaskSeed`, `Hidden/Universal/HDRDebugView`
2. **5× ArgumentNullException UIElements.Q[T]** (VisualElement target null)
3. **1× NullReferenceException** (stack stripped)
4. **🛑 Uncaught RuntimeError: table index is out of bounds → HALTING PROGRAM** — jeu crashed

## Actions immédiates pour exec

### 1. STOP toute autre activité

- **Aucun dispatch nouveau ticket** P2/P3 jusqu'à fix
- **Aucun feature creep** ni cleanup hygiène pour l'instant
- **Aucun déploiement** tant que runtime pas fixé (auto-build-loop peut continuer mais pas trigger manuel)

### 2. Bug-fixer URGENT déjà dispatched par superviseur

**Agent superviseur-spawned** : `bug-fixer` ID `ab94607c0d28cb1fb`, background, ETA 30-60 min.

Mission : diagnose + fix les 4 problèmes par ordre criticité :
1. RuntimeError table index out of bounds (CRITICAL crash)
2. ArgumentNullException UIElements.Q (5×)
3. 3 shaders URP not supported (build inclusion)
4. NullRef collateral

L'agent investigate les commits suspects R6-PARITY-V4 (top 3 risk : `7817aeb` SPLIT EnemyBossBehaviors + `a49ed12` Dynamic events + `08d7229` merge 014+005-IMPL boss).

### 3. Exec collabore si nécessaire

Si bug-fixer pose une question dans `questions-to-supervisor.md` (catégorie B escalation Mike), réveille toi rapidement + relay à Mike.

Sinon : exec attend bug-fixer completion + relit ce canal.

### 4. Pas de revert partial sans Mike validation

Charter §3 D10/D11 action : "STOP, build broken, run bug-fixer" ✅ déjà fait. Pas de revert spontané — bug-fixer doit trouver root cause.

Si bug-fixer suggère revert (ex : `git revert 7817aeb`) → Mike notif T1 + attendre validation.

## Hypothèses top 3 (cf drift report)

1. **`7817aeb` SPLIT EnemyBossBehaviors** : extraction static class — possible binding internal field access perdu post-split → tick boss → table index OOB
2. **`a49ed12` Dynamic events** : `% 5` auto-trigger sur Tower/Enemy/Castle → index OOB possible dans loop `foreach (var t in TowerPool.Active)` si pool reordered pendant event
3. **`a502416` 014 Boss phases** + merge : Apocalypse 4 phases avec timers → array OOB possible sur phases array si phase index dépasse `phases.Length`

## Ack expected

`.claude/supervisor/acks/2026-05-12-HHhMM-stop-runtime-critical-ack.md` :
- Confirmation STOP toute activité P2/P3
- Aware bug-fixer en cours (ID `ab94607c0d28cb1fb`)
- Status build/deploy actuel (continue OR pause auto-build-loop ?)
- Si bug-fixer trouve solution, post-fix verify cycle plan

## Status

✅ bug-fixer 2 (URGENT UXML root cause) COMPLETE @ 17h38 — voir instruction suivante POST-RUNTIME-FIX-V2

---

### 2026-05-12 17h38 — 🟢 POST-RUNTIME-FIX-V2 (root cause identifié + 3 commits defensive + decision gate Mike)

**Type** : INFO + DECISION-GATE
**From** : Opus superviseur (bug-fixer 2 `a8993f6473547200f` completion)
**Drift remediation status** : ✅ defensive complete, ⏳ pending Mike retest live pour LIFT D10/D11
**Full findings** : `.claude/supervisor/drift-reports/2026-05-12-17h38-bug-fixer-2-complete.md`

## Root cause identifié

**Fichier** : `Assets/Scenes/Main.unity`
**GameObject** : `#228555127 FloatingPopupController`
**Component** : `&228555130` UIDocument **fantôme** (`sourceAsset {fileID: 0}` + `m_PanelSettings {fileID: 0}`)

`GetComponent<UIDocument>()` retournait parfois ce ghost → `rootVisualElement` null → `Q<>()` ArgNull → WASM RuntimeError table OOB → Halting Program.

## 3 commits defensive shipped

- `443c816` : 33 UI controllers bulk null-check (pattern défensif systématique)
- `e82d6e7` : FloatingPopupController fallback → emprunte HudController singleton UIDocument
- `ef28060` : 7 controllers final sweep (patterns variés)

**Total** : ~40 UI controllers patchés. ~15-20 restent (acceptable follow-up).

## Action exec immédiate

### 1. **LIFT STOP-RUNTIME-CRITICAL conditional**

- Si `git log --oneline -5 origin/main` confirme HEAD ≥ `ef28060` → exec peut **reprendre activité limitée** :
  - Auto-build-loop continue (no manual pause)
  - Watch for next gh-pages deploy (commit R>1718 attendu)
  - **No nouveau dispatch P2/P3 tant que retest Mike pas validé**
- Aucun revert spontané. Aucun feature creep.

### 2. **Attendre Mike retest live**

Auto-build-loop devrait avoir re-deployé `/v6/` avec les 3 commits defensive (`443c816 + e82d6e7 + ef28060`).

Mike retestera (cf chat live monitoring). 3 outcomes possibles :

**Outcome A — Crash résolu** (softfail OK) :
- Defensive patches travaillent, RuntimeError disparaît
- HUD/UI peuvent avoir des features dégradées (FloatingPopup) mais jeu fonctionne
- Sprint R6-PARITY-V4 = effectively complete. Mike décide P2/P3 OR stop sprint OR scene edit pour full fix.

**Outcome B — Crash résolu mais FloatingPopup features broken** :
- Defensive patches OK, mais une feature spécifique (popup info, dialogs ?) ne fonctionne plus
- Decision Mike : scene edit Unity Editor (5 min manual) OR auto-edit YAML par superviseur (risqué)

**Outcome C — Crash persiste** :
- Defensive patches insuffisants, autre root cause non-identifié
- Escalation T1 Mike obligatoire + propose plan (revert partial commits P1 `08d7229` / `7817aeb` / `a49ed12` ?)

### 3. **Pas d'action proactive nouvelle**

Sauf relay si bug-fixer ou autre agent pose une question via `questions-to-supervisor.md` catégorie B.

## Si Mike demande scene edit

**Option A — Unity Editor manual** (recommandé, plus sûr) :
1. Mike ouvre projet Unity 6000.3.15f1
2. Open `Assets/Scenes/Main.unity`
3. Hierarchy → find `FloatingPopupController`
4. Inspector → Remove component UIDocument (le vide, sourceAsset = None)
5. Save scene + commit + push

**Option B — Auto YAML edit par superviseur** (risque modéré) :
- Superviseur édit direct `Main.unity` YAML pour supprimer le block component `&228555130` (16 lignes à supprimer)
- Verify scene parse OK (no broken refs)
- Commit `fix(scene): remove ghost UIDocument FloatingPopupController`
- Risk : YAML corruption si parse off-by-one. Backup .unity avant.

À faire seulement si Mike valide une option.

## Ack expected

`.claude/supervisor/acks/2026-05-12-HHhMM-post-runtime-fix-v2-ack.md` :
- Confirmation LIFT STOP-RUNTIME-CRITICAL (conditional)
- Status auto-build-loop deploy (commit hash / R-number)
- Attente retest Mike live
- Acknowledge 3 outcomes possibles A/B/C

## Status

⏳ pending Mike retest live `/v6/` après next gh-pages deploy R>1718

---

### 2026-05-12 17h45 — 🟢 BACKLOG-FOURNI-AUTONOMOUS (13 tickets queue, exec dispatch ses parallèles)

**Type** : GO-SPRINT + BACKLOG-CONTINU
**From** : Opus superviseur (correction process Mike chat : "son backlog doit jamais être vide, juste tu réajuste ses prios")
**Mode** : Autonome — exec gère ses slots worktrees (charter §1 max 4 parallèles), pioche dans le backlog priorisé ci-dessous au fur et à mesure
**Préemption** : si Mike retest /v6/ outcome B/C → re-prioriser P0 fix scene OR revert partial avant continuer backlog

## Principe (LIRE ATTENTIVEMENT)

**Tu es libre.** Backlog priorisé P1 → P3 ci-dessous = **guidance prios**, pas un script à suivre à la lettre. **Tu gères ton propre backlog**, ta propre cadence, ton propre niveau de parallélisme.

**Tu peux** :
- Lancer **autant d'agents en parallèle** que tu veux (4 worktrees, 6, 8, illimité — à toi de juger ta capacité de tracking + merge conflict risk).
- Choisir ton ordre de dispatch (le P1.1/.2/.3/.4 que j'ai mis = ma reco mais tu re-prioritises si tu vois une dépendance ou un risque).
- Insérer de la dette technique (P3) entre des tickets P1 si tu veux paralléliser un refacto pendant qu'un feature long tourne.
- Insérer ton propre cleanup hygiène (P2.1 worktrees, ou autre) sans dispatch agent.
- Découper un gros ticket en sous-tickets si tu juges la complexité plus élevée que mon estimation.
- Reporter un ticket si tu trouves un blocker (ouvre Q catégorie A ou B selon urgence).

**Tu dois** :
- **Backlog jamais vide** : dès qu'un slot worktree libère, pioche le ticket suivant dans la queue. Pas d'idle wait sauf préemption P0.
- **Respecter charter §1** : cap 500 LOC, no Sub-Opus spawn, no feature creep, compile gate post-commit, self-report 100 mots max.
- **Préemption P0 immédiate** : si Mike retest /v6/ outcome B (Floating Popup degraded) ou C (crash persiste), tu pauses backlog 30 min pour scene edit ou revert, puis tu resumes.
- **Ack après chaque dispatch batch** (pour que je puisse suivre).

**Mon rôle (superviseur PO/PM)** :
- Je fournis le backlog priorisé et le contexte.
- Je **corrige si tu vas trop loin** : scope creep, cap violations, dispatch d'un ticket fragile (touch fichiers risqués), dispatch trop parallèles si je vois conflits merges, etc.
- Je relais Mike decisions + écris nouvelles instructions si stratégie change.
- Je ne te micro-manage pas. Tu es senior, tu juges les tradeoffs.

**Anti-pattern à éviter** :
- ❌ Attendre Mike validation entre chaque ticket (mode autonome activé, default = dispatch ask later)
- ❌ Slot vide pendant > 5 min sans raison (cleanup OU dispatch backlog suivant)
- ❌ Lancer 10 agents tous sur Tower.cs en parallèle (merge cauchemar)
- ❌ Ignorer la queue P3 dette technique si tu as 6h consecutives free (insère 1 refacto)

---

## P1 — Backlog actif (dispatcher en parallèle dès maintenant)

### P1.1 — R6-PARITY-UI-HARDENING-FINAL (bug-fixer Sonnet, 30 min)

Finir le bulk null-check pattern sur les ~15-20 UI controllers restants non patchés par bug-fixer 2.

**Action** :
1. `grep -rln "\.Q<\|UQueryExtensions" Assets/Scripts/UI/ | sort -u` pour lister tous les fichiers qui utilisent UIDocument queries
2. Cross-référence avec les fichiers déjà patched (cf commits `443c816 + e82d6e7 + ef28060`) — soit ~40 fichiers
3. Pour les ~15-20 restants : applique pattern défensif identique :
   ```csharp
   var uiDoc = GetComponent<UIDocument>();
   if (uiDoc == null) { Debug.LogError("[XxxController] UIDocument null"); return; }
   var root = uiDoc.rootVisualElement;
   if (root == null) { Debug.LogError("[XxxController] rootVisualElement null"); return; }
   ```
4. Commit `fix(runtime-crash-3): final UI hardening sweep — N controllers (defend-in-depth complete)`
5. Push autonome

**Files candidats probables** (à vérifier) : HelpOverlayController, MinimapController, TowerInfoPanel, ChallengeListController, CalibrationOverlay, TutorialController, RunIntroController, NewsPanel, FloatingTooltipController, ContextMenuController, etc.

**Cap** : 500 LOC strict (mais pattern défensif = +5 LOC par fichier, sans risque)

### P1.2 — R6-PARITY-012-V4-FIDELITY (feature-dev Sonnet, 4-5h)

Port 5 V4 events dynamiques manquants + trigger model V4 data-driven.

**Source V4** : `/Users/mike/Work/milan project/src-v3/systems/EventManager.js`
**V6 target** : `Assets/Scripts/Systems/DynamicEventManager.cs` (218 LOC actuels → ~350 LOC, watch cap 500)

**5 events à porter** :
1. **void_pulse** : pulse circulaire centrée castle, vide [tile inside radius].coin pickups (V4 visuel : dark spiral expanding)
2. **zero_g** : enemies floating mode 8s, speed×0.5 + ignore path collisions (V4 visuel : enemies levitating sprite)
3. **undertow** : sur water tiles, slow pull current 30%, enemies traversant water = path reversed 1 tile
4. **battle_cry** : enemies in radius 5 around boss = +50% atk speed + +25% movement 6s (V4 visuel : red shockwave)
5. **hack** : 1 tower disabled random 5s + tower target enemy = friendly fire 1 hit (V4 visuel : glitch overlay)

**Trigger model** : remplacer `% 5` random par `level.events[]` data-driven (V4 fidelity). Chaque LevelData a une liste `WaveEvent[]` avec `waveIndex` + `eventType` + `duration` + params.

**Refacto léger Tower.cs** : 2-3 nouvelles propriétés (TempDisabledUntilTime, FriendlyFireMode) wiré dans Update/AcquireTarget. Pas de cap 500 risk car Tower.cs déjà 2254 LOC (legacy hors-scope cap).

**Coordination** : ne touche pas EnemyBossBehaviors (déjà fragile post-split).

### P1.3 — R6-PARITY-011-COMPLETE (feature-dev Sonnet, 1-2h)

Foire + Medieval castle skins (2/10 themes missing dans `CastleSkinController.cs` `ThemeSkins[]`).

**Action** : Placeholder-first
1. Ouvrir `Assets/Scripts/Visual/CastleSkinController.cs` (77 LOC)
2. Ajouter 2 entrées dans `ThemeSkins[]` :
   - `Theme.Foire` → `Color.HotPink` tint + emissive yellow (carnival vibes) + scale 1.1
   - `Theme.Medieval` → `Color.SaddleBrown` tint + emissive none + scale 1.0 + add stone material variation if available
3. Si textures `castle_foire.png` / `castle_medieval.png` présentes dans `Assets/Textures/Castles/` : use them, sinon placeholder couleur unie acceptable
4. Commit `feat(parity-v4-011-complete): Foire + Medieval castle skins (placeholder-first)`

**Cap** : 500 LOC strict (CastleSkinController.cs 77 → ~120 LOC, safe)

### P1.4 — R6-PARITY-015-BOSS-UI-CUTSCENE (feature-dev Sonnet, 2-3h)

Port BossUI cutscene de V4 : intro 4-line text overlay quand boss spawn, dim BG, skip button, fade out après 5s ou tap.

**Source V4** : `/Users/mike/Work/milan project/src-v3/systems/BossIntro.js` OU `src-v3/entities/Visitor.js` (look for boss spawn cinematic logic) OU `src-v3/ui/CutsceneScene.js`

**V6 target** : `Assets/Scripts/UI/BossUI.cs` (à étendre) + `Assets/UI/BossCutsceneOverlay.uxml` (nouveau)

**Implementation** :
1. Subscribe `Enemy.OnBossSpawn` event dans BossUI
2. Show overlay UXML (existant `boss-cutscene` element ? sinon ajouter)
3. Animate text intro (4 lines from EnemyType.cutsceneText[] field — à ajouter dans EnemyType SO)
4. Auto-fade après 5s ou skip button
5. Use Unity Animator OR Tween (`DOTween` if installed) pour smooth in/out

**Exploit Unity** : URP volume bloom + tonemapping per boss spawn (impacte mood). Si Volume not setup, skip.

**Cap** : 500 LOC strict (BossUI.cs current LOC = check first)

### P1.5 — R6-PARITY-016-LIGHTING-AMBIENT (feature-dev Sonnet, 1-2h)

Hemisphere ambient lighting per-theme (sky color + ground color + intensity).

**Action** :
1. Créer `Assets/Scripts/Visual/ThemeAmbientConfig.cs` (ScriptableObject, ~80 LOC) avec champs `skyColor`, `equatorColor`, `groundColor`, `intensity`, `Theme theme`
2. Pour chaque thème (10 themes) : créer une asset SO `ThemeAmbient_<Theme>.asset` dans `Assets/Resources/Lighting/`
3. Modifier `LevelLoader.cs` (ou équivalent) : à `OnLevelStart`, load `ThemeAmbient_<currentTheme>` et apply :
   ```csharp
   RenderSettings.ambientMode = AmbientMode.Trilight;
   RenderSettings.ambientSkyColor = config.skyColor;
   RenderSettings.ambientEquatorColor = config.equatorColor;
   RenderSettings.ambientGroundColor = config.groundColor;
   RenderSettings.ambientIntensity = config.intensity;
   ```
4. Commit `feat(parity-v4-016): hemisphere ambient lighting per-theme (10 themes)`

**Cap** : safe (1 nouveau fichier SO + 1 modif LevelLoader).

### P1.6 — R6-PARITY-017-WATER-LAVA-ANIM (feature-dev Sonnet, 2h)

Water + lava tile frame animation (8-frame loop @ 8fps comme V4).

**Action** :
1. Vérifier `Assets/Textures/Tiles/` contient water_01..water_08 + lava_01..lava_08 frame textures (déjà importés via P0 textures port ?). Si non : flag dans self-report.
2. Créer 2 `AnimatorController` : `WaterTileAnim.controller` + `LavaTileAnim.controller` avec state Loop 8 frames
3. Modifier `PathTilesController.cs` : pour chaque cellule water/lava, instantiate prefab avec Animator attaché
4. Alternative simpler : use `Material.SetTexture` swap par tick (8fps timer) si Animator overkill

**Exploit Unity** : `ParticleSystem` water ripples on top (sub-emitter), `Light` emissive pulse pour lava

**Cap** : safe.

### P1.7 — R6-PARITY-018-CASTLE-POINTLIGHT (feature-dev Sonnet, 1h)

PointLight enfant du Castle prefab, intensité scalée par HP%, color shift red quand <30% HP.

**Action** :
1. Modifier prefab `Castle.prefab` : ajouter child GameObject `CastleAura` avec component `Light` type Point, range 5, intensity 2
2. Modifier `Castle.cs` : ajouter `OnHPChange` callback (ou subscribe existing event) → update light :
   ```csharp
   var pct = (float)CurrentHP / MaxHP;
   _light.intensity = Mathf.Lerp(0.5f, 2f, pct);
   _light.color = pct < 0.3f ? Color.red : Color.Lerp(Color.red, Color.white, pct);
   ```
3. Commit `feat(parity-v4-018): castle PointLight HP-aware (intensity + color shift)`

**Cap** : safe (Castle.cs +20 LOC).

### P1.8 — R6-PARITY-019-SCHOOLS-MAPPING-AUDIT (general-purpose Sonnet audit, 1h)

Audit confirmation Mike "Keep V6 5 schools" : vérifier mapping vs V4 6 schools.

**Action** :
1. Read V4 schools : `/Users/mike/Work/milan project/src-v3/data/Schools.js` ou équivalent
2. Read V6 schools : `Assets/Scripts/Data/School.cs` ou `Towers/SchoolType.cs`
3. Produire `.claude/audit/2026-05-12-schools-mapping.md` avec table :
   | V4 school | V6 school | Mapping | Notes |
4. Recommander : keep 5 V6 (Mike décision validée) OU propose 6e school si gap critique
5. Pas de commit code, juste audit MD

---

## P2 — Backlog secondaire (dispatcher après slots P1 libérés, faible urgence)

### P2.1 — R6-CLEANUP-WORKTREES (exec direct bash, 15 min)

`git worktree list` → identifier worktrees stale (>1h inactives) → `git worktree remove --force <path>` chacun.

Limit cleanup à 5 par batch (éviter mass churn). Pas d'agent nécessaire, exec exécute directement.

Commit unique : `chore(hygiene): cleanup N stale worktrees (1h+ inactive)`

### P2.2 — R6-PARITY-FLOATING-POPUP-SCENE-FIX (catégorie B escalation Mike OR auto YAML)

Vrai fix pour le ghost UIDocument dans `Main.unity`.

**Option A — manual Unity Editor (Mike)** :
- Mike open project + scène Main.unity + remove component UIDocument `&228555130` from FloatingPopupController + save + commit

**Option B — auto YAML edit (exec, risque modéré)** :
- Backup `Main.unity` → `Main.unity.bak`
- Edit YAML : supprimer le block `&228555130` UIDocument + remove ref de la liste `m_Components` du GameObject #228555127
- Verify scene parse OK (compile test)
- Commit `fix(scene): remove ghost UIDocument FloatingPopupController (resolves runtime crash root cause)`
- Si parse fail : restore .bak + escalation Mike

**Decision** : exec choose Option B si confident YAML edit, sinon ack escalation Mike pour Option A.

---

## P3 — Dette technique (background, sans urgence — dispatch seulement si slots free + P1/P2 empty)

### P3.1 — R6-REFACTO-ENEMY (bug-fixer Sonnet, 6-8h)

`Assets/Scripts/Entities/Enemy.cs` 2051 LOC → split partial files par responsabilité.

**Plan split** :
- `Enemy.cs` core (300 LOC) : fields, Awake, Update dispatch, public API
- `Enemy.Movement.cs` (400 LOC) : path follow, waypoint logic, currentSpeedMul
- `Enemy.Combat.cs` (400 LOC) : TakeDamage, OnHit, OnKilled, drop loot
- `Enemy.Behaviors.cs` (400 LOC) : per-type AI (assassin, dragon, kraken, wizard_king...)
- `Enemy.Stats.cs` (200 LOC) : HP, atk, def, school resists
- `Enemy.Anim.cs` (300 LOC) : Animator state, sprite swap, VFX hooks

**Cap** : 500 LOC strict chaque fichier.

### P3.2 — R6-REFACTO-TOWER (bug-fixer Sonnet, 6-8h)

`Assets/Scripts/Entities/Tower.cs` 2254 LOC → split partial.

**Plan split** : `Tower.cs` core + `Tower.Combat.cs` + `Tower.Placement.cs` + `Tower.Upgrade.cs` + `Tower.Effects.cs` + `Tower.Anim.cs`

**Cap** : 500 LOC strict.

### P3.3 — R6-REFACTO-CASTLE (bug-fixer Sonnet, 2h)

`Assets/Scripts/Entities/Castle.cs` 762 LOC → split partial (Castle.cs core + Castle.HP.cs + Castle.VFX.cs).

**Cap** : 500 LOC strict.

---

## Coordination cross-tickets

- **Conflits prévisibles** :
  - P1.4 BossUI + P3.1 Enemy refacto → BossUI subscribe OnBossSpawn event (Enemy partial OK)
  - P1.2 events + Tower.cs additions → P3.2 Tower refacto (do P1.2 d'abord, P3.2 plus tard)
  - P1.5 ambient lighting + LevelLoader → si LevelLoader stable, no conflict ; si LevelLoader splitting en // : coord
- **Pas de touch simultané** : EnemyBossBehaviors.cs (split fragile post-7817aeb), DynamicEventManager.cs (sauf P1.2 owner), PathTilesController.cs (sauf P1.6 owner)

## Dispatch strategy recommandée

**Immediate (slot 1-4)** :
- Slot 1 : P1.1 UI HARDENING (bug-fixer, 30 min — premier slot court pour libérer rapidement)
- Slot 2 : P1.2 events V4 FIDELITY (feature-dev, 4-5h)
- Slot 3 : P1.3 Castle skins (feature-dev, 1-2h)
- Slot 4 : P1.4 BossUI cutscene (feature-dev, 2-3h)

**Cascade (slot libère)** :
- Slot 1 free (30 min) → dispatch P1.5 ambient
- Slot 3 free (1.5h) → dispatch P1.6 water/lava anim
- Slot 4 free (2.5h) → dispatch P1.7 castle PointLight
- Slot 1 free again (2h) → dispatch P1.8 schools audit
- Au cours de la cascade : P2.1 cleanup à insérer sans agent (exec bash direct entre 2 commits)
- Slots P3.x dispatched seulement si tous P1+P2 done OU exec ≥6h sans interruption

**Adaptive** :
- Si Mike retest crash résolu → continue backlog comme prévu
- Si Mike retest crash persiste → P2.2 Floating Popup scene fix devient P0, push en tête, autres slots continuent backlog normalement
- Si Mike demande revert partial → exec pause backlog 30 min pour exécuter revert, puis resume

## Time cap

**Sprint R6-PARITY-V4 P1 actif** : cap d'origine 20h00 local (ack 16h00 + 4h). Mike a accordé autonomous mode = soft cap (peut être étendu sans nouvelle validation explicite).

**Backlog completion estimé** : P1 (8 tickets) + P2 (2 tickets) = ~20h total séquentiel, ~5-6h en 4-parallel. P3 (3 tickets refacto) = ~15h additionnel hors sprint actuel.

**Sprint R6-PARITY-V4 effective complete = P1 done** (~5-6h depuis maintenant ~17h45 → cap ~23h45). P2-P3 = follow-up sprints futurs.

## Ack expected

`.claude/supervisor/acks/2026-05-12-HHhMM-backlog-fourni-autonomous-ack.md` contenant :
- Confirmation 8 specs tickets P1 créés (paths `.claude/specs/R6-PARITY-V4/R6-PARITY-XXX.md` ou inline dans backlog file)
- 4 worktrees Slot 1-4 dispatched (paths + agent IDs)
- Acknowledgment cascade strategy (pioche dans backlog au fur et à mesure)
- Time cap noté (soft, autonomous mode)
- Pas d'attente nouvelle décision Mike sauf préemption retest

## Constraints rappel charter §1

- Hard cap 500 LOC par fichier C# (règle #3) — TOLERANCE ZERO (sauf legacy déjà >500 hors scope)
- No Sub-Opus spawn (règle #10) : Sonnet feature-dev ou bug-fixer
- No feature creep (règle #4) : opportunités → `.claude/backlog/R6-found-during-exec.md`
- Compile gate post-commit (règle #5)
- Self-report 100 mots max (règle #8) chaque ticket

## Status

✅ exec free to dispatch — outcome A confirmé 17h47, backlog actif

---

### 2026-05-12 17h47 — 🟢 RETEST-OUTCOME-A-CONFIRMED + 3 issues mineurs à intégrer P1.1

**Type** : ✅ MILESTONE-CONFIRMED + REPRIORITIZATION-LEGÈRE
**From** : Mike retest live `/v6/` (paste console output @ 17h47, build post auto-build-loop deploy avec commits 443c816+e82d6e7+ef28060)
**Drift D10/D11** : **LIFTED** ✅ (no more RuntimeError, no more Halting Program)

## Console observation

**ABSENT** ✅ :
- `RuntimeError: table index is out of bounds → Halting Program` ✅
- 5× ArgumentNullException UIElements.Q (down to 2 maintenant)

**PRÉSENT (defensive patches travaillent)** ✅ :
- `[HudController] rootVisualElement is null — HUD UXML failed to load` (log défensif, pas crash)
- `[HudController] rootVisualElement is null in WireCallbacks — UXML failed to load` (log défensif, pas crash)
- `Audio context resumed after 4.819 seconds` (audio fonctionne)
- WebGL 2.0 init OK, Physics PhysX init OK, Input System init OK

**ISSUES MINEURS RESTANTS (3 à intégrer P1.1)** ⚠️ :
1. **3 shaders URP not supported** (toujours présent) : `Hidden/CoreSRP/CoreCopy`, `Hidden/Universal Render Pipeline/StencilDitherMaskSeed`, `Hidden/Universal/HDRDebugView` — non-bloquant gameplay mais visuel possible dégradé (post-FX, debug HDR view)
2. **1× ArgumentNullException** `Parameter name: e` (down from 5×) : un controller pas encore patché par bug-fixer 2
3. **1× ArgumentNullException** `Parameter name: collection` **NOUVEAU SYMPTÔME** : un appel LINQ `.Sum/.Where/.OrderBy(null collection)` ou iterable nullé. Pas un Q<T> issue donc HORS du pattern P1.1 UI hardening. Possible : score panel, leaderboard, stats agrégation.
4. **1× NullReferenceException** stack stripped (Bindings.ThrowHelper) — probable collateral des autres exceptions

## Action exec — UPDATE P1.1 + add P1.0

### P1.0 NEW — R6-FIX-URP-SHADERS (bug-fixer Sonnet, 15-30 min)

**Priorité absolue (intervertir avec P1.1)** : ajouter les 3 shaders URP aux Always Included Shaders du projet.

**Action** :
1. Ouvrir `ProjectSettings/GraphicsSettings.asset` (YAML)
2. Sous `m_AlwaysIncludedShaders:` ajouter 3 entries :
   ```yaml
   - {fileID: 4800000, guid: <CoreCopy-shader-guid>, type: 3}
   - {fileID: 4800000, guid: <StencilDitherMaskSeed-guid>, type: 3}
   - {fileID: 4800000, guid: <HDRDebugView-guid>, type: 3}
   ```
3. Find shader GUIDs via : `grep -r "Hidden/CoreSRP/CoreCopy" Packages/com.unity.render-pipelines.universal/` ou via Resources scan
4. Alternative : modifier `Assets/Resources/ShaderInclude.shadervariants` (Unity ShaderVariantCollection) pour inclure ces 3
5. Commit `fix(urp): include 3 shaders in build (CoreCopy + StencilDitherMaskSeed + HDRDebugView)`
6. Build + verify console post-deploy ne montre plus l'erreur

**Si shader GUID introuvable** : ack escalation Mike catégorie B (peut-être URP package version mismatch Unity 6000.3.15f1).

### P1.1 UPDATE — R6-PARITY-UI-HARDENING-FINAL (extend scope)

Identique au scope précédent (15-20 controllers restants null-check pattern) **MAIS ajouter** :

**Sous-ticket P1.1b** : Trace + fix le `ArgumentNullException Parameter name: collection` :
- Grep `Linq` operations sur potentially null collections : `.Sum(\|.Average(\|.Min(\|.Max(\|.OrderBy(\|.Where(\|.Select(\|.Aggregate(`
- Filtrer pour les usages dans UI controllers ou stats/score aggregation
- Pour chaque match : null-check avant LINQ call
- Likely culprits : ScorePanel, LeaderboardController, StatisticsController, RunSummaryController (déjà patched pour UIDocument null mais pas pour collection null)
- Commit `fix(runtime-crash-3b): null-check collections avant LINQ aggregation (N call sites)`

### Backlog P1.2-P1.8 + P2 + P3 : INCHANGÉ

Continue ton cascade dispatch normal. P1.0 + P1.1b en supplément.

## Sprint R6-PARITY-V4 status update

- **D10/D11 LIFTED** ✅ (crash résolu, defensive OK)
- **Sprint effectif unblocked** : continue backlog P1 normal
- **Outcome A** path validé : pas de scene edit FloatingPopup obligatoire pour l'instant (P2.2 reste en backlog low-prio)
- Pas de revert nécessaire
- **Mike satisfaction** : confirmer post-P1.0 deploy (3 shaders URP fix) que console est clean (ou quasi clean : 0-1 ArgNull restants OK)

## Mike's chat directive (rappel)

> "il est libre de remplir ses slots a l'infini ou pas et de gérer son backlog, le corriger si il va trop loin"

**Exec est autonome.** Le backlog P1/P2/P3 est ton guide, pas ta cage. Empile parallèles comme tu juges, pioche dans la queue dès qu'un slot libère. Je corrige seulement si je vois drift (charter §1 violations, scope creep, conflicts merge prévisibles).

## Ack expected (cumulé avec BACKLOG-FOURNI-AUTONOMOUS)

`.claude/supervisor/acks/2026-05-12-HHhMM-backlog-fourni-autonomous-ack.md` peut être unique (ack le backlog complet + l'update outcome A en 1 fichier).

## Status

✅ D10/D11 LIFTED + backlog P1 actif, exec dispatch ses parallèles

---

### 2026-05-12 17h54 — 🛑 PAUSE-PORT-PIVOT-WIRING (Mike feedback critique rendu)

**Type** : STOP-CURRENT + PIVOT-PROCESS
**From** : Mike chat direct ("on est tellement loin du résultat attendu, comment ça se fait qu'on avance pas comme ça ? d'un point de vue rendu j'ai l'impression que ça s'empire plutôt que ça s'améliore")
**Cause root identifiée** : process error supervisor — j'ai dispatché 8+ tickets port code C# en parallèle sans valider à chaque ticket que la feature est **wired dans gameplay loop + visible côté joueur**. Code existe dans `Assets/Scripts/` mais possibly pas invoqué par LevelLoader/WaveManager/etc. Résultat : 3500 LOC nouveaux mais rendu joueur stagne.

## STOP IMMÉDIAT

### 1. Pause dispatch nouveau port code

- **Aucun dispatch P1.4 (BossUI cutscene), P1.5 (ambient), P1.6 (water/lava anim), P1.7 (PointLight), P1.8 (schools)**.
- **Aucun dispatch P3.x (refacto god classes)**.
- Si tu as déjà dispatché P1.4-P1.8 en parallèle pendant que j'écrivais le backlog initial : **let them finish their current commit** (don't kill mid-flight), MAIS no new dispatch.

### 2. Continue/finis ce qui est en flight UNIQUEMENT

- P1.1 (UI hardening final 15-20 controllers) : OK finir
- P1.1b (LINQ null-check) : OK finir
- P1.2 (5 events V4 missing) : OK finir mais flag dans commit "wiring pending"

### 3. Aucun marquage "done" sans live test

Pour chaque ticket en flight ou done depuis 14h48 (pivot V4 parity), exec doit auditer :
1. Le code C# existe ?
2. Le code est **wired** dans le gameplay loop ? (LevelLoader, WaveManager, OnLevelStart, Update tick, etc.)
3. Le code est **visible côté joueur** dans /v6/ build ? Screenshot ou console log "feature X triggered" obligatoire.
4. Diff /v4/ vs /v6/ confirme parité visuelle (au moins placeholder-equivalent) ?

Sans ces 4 critères, le ticket retourne en queue "wiring pending".

## NEW BACKLOG P1-WIRING (priorité absolue, replace P1.4-P1.8 + P3.x)

### P1-WIRING.0 — AUDIT VISUAL DIFF /v4/ vs /v6/ (qa-tester URGENT)

**Action** : Spawn qa-tester en background pour audit visuel side-by-side.

Mission qa-tester :
1. Chrome MCP ouvre 2 tabs : `https://michaelchevallier.github.io/lava_game/v4/` + `https://michaelchevallier.github.io/crowd-defense/v6/`
2. Pour /v4/ : load level world1-1 (ou équivalent), play 60 sec, screenshot HUD + map + visitors + tower placement + wave intro + boss spawn (si arrive)
3. Pour /v6/ : load level équivalent, play 60 sec, mêmes screenshots
4. Pour chaque "feature V4 portée" depuis 14h48 : verify visible /v6/. Liste :
   - **Textures** : skybox per-theme switch ? Path tile textures ? VFX sprite sheets ?
   - **PathTiles** : water bridges + lava crossings rendu ? Corner/T-junction tiles ?
   - **Skybox** : transition smooth per level theme ?
   - **VFX** : 9 nouveaux spawn methods triggered visible ? (heal_aura, lightning_chain, shield_aura, slow_field, impact, explosion, portal, fire_breath)
   - **Weather** : particles (clouds, snow, rain, sandstorm) per theme visible ?
   - **Dynamic events** : `% 5` trigger visible (sand_storm dust + tower range debuff overlay) ?
   - **SceneDecor** : trees/rocks/bushes placed visible ?
   - **Boss phases** : Apocalypse 4 phases switch + VFX rage ring visible ?
   - **Castle skins** : 8/10 + Foire/Medieval visible mesh+color per theme ?
   - **UI** : HUD render OK ? Console softfail logs non-bloquants ?
5. Produit `.claude/audit/2026-05-12-17h54-visual-diff-v4-v6.md` avec :
   - Table 20-30 features V4 portées : status V6 visible OUI/PARTIEL/NON
   - Pour chaque NON : root cause hypothèse (pas wired ? Asset missing ? Asset assigned mais component disabled ?)
   - Reco priorité fix wiring P1-WIRING.1+

**Time cap** : 30-45 min audit. Findings drive le backlog suivant.

### P1-WIRING.1 → N — Tickets WIRING par feature non-visible

Une fois audit livré, je publie nouveau backlog wiring focused. Estimation : 8-15 tickets wire (chacun 20-80 LOC, ~30 min-1h).

Exemple ticket type :
- **R6-WIRE-WEATHER** : verify WeatherController.OnLevelStart subscribe LevelEvents.OnLevelStart. If not wired : add subscription + load WeatherConfig per theme.

## Constraints rappel charter §1

- Hard cap 500 LOC strict
- No Sub-Opus spawn
- **NEW** : feature "done" requires wire + live test + diff /v4/ ≤ /v6/ confirmed (cf feedback Mike 17h54)
- Compile gate post-commit
- Self-report 100 mots max

## Mike's empowerment toujours valide

Tu restes libre de gérer ton backlog wiring + cascade dispatch + parallélisme niveau. Je corrige seulement si tu repars en mode "port code sans wire" ou si tu dispatches >5 agents sur même fichier (merge cauchemar).

## Ack expected

`.claude/supervisor/acks/2026-05-12-HHhMM-pause-port-pivot-wiring-ack.md` :
- Confirmation pause P1.4-P1.8 + P3.x
- Status P1.1 + P1.1b + P1.2 in-flight (let finish ou stopped ?)
- Acknowledge nouveau critère "done" = wire + live test
- qa-tester audit dispatch confirmé (ID + ETA)

## Status

✅ DONE — qa-tester audit livré + bug-fixer 3 YAML edit shipped commit `4288157` + V6 attendu jump 65→85% parité

---

### 2026-05-12 18h05 — 🟢 LIFT-PAUSE + BACKLOG-WAVE-2 (post-wiring success)

**Type** : GO-SPRINT + BACKLOG-CONTINU
**From** : Opus superviseur — wiring critique shipped, exec autonome reprend
**Drift** : 0/12, deploy R1803 live attendu @ 85% parité

## Lift PAUSE-PORT-PIVOT-WIRING

PAUSE levée. Tu peux reprendre dispatch P1 + P3 backlog. **MAIS** nouveau critère done = wire + visible + diff /v4/ confirmé (cf feedback_wire_as_you_go.md).

Aussi : **vérification qa-tester R1803 en cours** (ID `ae24dd2a2d95a4112`, ETA 20-30 min) — verify les 3 GameObjects wired sont visibles live. Tu peux continuer dispatch en parallèle, les findings qa-tester guideront re-prioritisation si besoin.

## Mike's directive critique : PARALLEL/SERIAL Unity

**Règle** : "parallelization is king. mais pour certaines choses tu as peut-être envie de sérialiser et c'est ok. Notamment si les fichiers Unity sont comme les scenes Xcode il faut faire attention".

### 🟢 PARALLELIZABLE — dispatch en masse OK (différents fichiers .cs)
- C# scripts dans différents fichiers
- Audit / docs / markdown

### 🟡 PARALLELIZABLE si fichiers différents (1 agent par fichier max)
- 1 prefab par agent
- 1 material par agent
- 1 ScriptableObject par agent
- 1 .anim par agent

### 🔴 SERIAL OBLIGATOIRE — 1 agent à la fois
- `Assets/Scenes/Main.unity` — SÉRIALISER tous les GameObject add/modify
- `Assets/Scenes/*.unity` autres
- `ProjectSettings/GraphicsSettings.asset` — shaders shared global
- `ProjectSettings/QualitySettings.asset`
- `ProjectSettings/TagManager.asset`
- `Packages/manifest.json` + `packages-lock.json`

**Si 2 tickets touchent Main.unity** : dispatch séquentiel (B après A push), JAMAIS en parallèle. Risque YAML corruption analogue Xcode .xib/.storyboard.

## Backlog WAVE-2 (ordre suggéré, exec libre de re-prioriser)

### 🟢 P1.5 — R6-PARITY-016-LIGHTING-AMBIENT (PARALLELIZABLE, feature-dev Sonnet, 1-2h)

`ThemeAmbientConfig.cs` + `Assets/Editor/CreateThemeAmbientAssets.cs` + `Assets/Resources/PerformanceTestRunInfo.json` semblent déjà commitées (via mon git add -A accident commit `14fa54b`).

**Action** :
1. Audit le code existant : `ThemeAmbientConfig` SO + Editor menu auto-create ?
2. Verify les 10 `ThemeAmbient_<Theme>.asset` existent dans `Assets/Resources/Lighting/` (sinon Editor menu generate)
3. Wire dans `LevelLoader.cs` (ou équivalent) → à `OnLevelStart` apply RenderSettings.ambient* per theme
4. Live test : sky/equator/ground ambient colors changent per niveau

**Files touchés** : `ThemeAmbientConfig.cs` + `LevelLoader.cs` (ou `LevelVisualBridge.cs`) — PARALLEL OK avec autres tickets pas même fichier.

### 🟢 P1.6 — R6-PARITY-017-WATER-LAVA-ANIM (PARALLELIZABLE, feature-dev Sonnet, 2h)

`WaterLavaAnimController.cs` semble déjà commited via mon accident. Verify wiring.

**Action** :
1. Read `WaterLavaAnimController.cs` actuel
2. Verify il y a (a) controller component sur water/lava tiles ou (b) frame swap par Material.SetTexture
3. Si pas wired : ajouter dans `PathTilesController.cs` pour instantier l'anim controller sur chaque water/lava tile
4. Verify Assets/Textures/Tiles/water_01..water_08 + lava_01..lava_08 existent (sinon flag asset gap)
5. Live test : water tiles flow animé + lava tiles bubble animé

**Files touchés** : `WaterLavaAnimController.cs` + `PathTilesController.cs` — PARALLEL OK.

### 🟢 P1.1b — R6-FIX-LINQ-NULL-CHECK (PARALLELIZABLE, bug-fixer Sonnet, 30 min-1h)

Trace + fix `ArgumentNullException Parameter name: collection` observé console retest 17h47.

**Action** :
1. `grep -rn "\.Sum(\|\.Average(\|\.Min(\|\.Max(\|\.OrderBy(\|\.Where(\|\.Select(\|\.Aggregate(\|\.Count(\|\.First(\|\.Last(" Assets/Scripts/UI/ Assets/Scripts/Systems/ Assets/Scripts/Data/`
2. Filter pour callers in stats/score/leaderboard/run summary aggregation (UI panels)
3. Pour chaque match : null-check ou null-coalesce avant LINQ call (`collection ?? Array.Empty<T>()`)
4. Commit `fix(runtime-crash-3b): null-check collections avant LINQ aggregation (N call sites)`

**Files touchés** : multiples `.cs` UI/Systems/Data — PARALLEL OK avec autres tickets.

### 🟢 P3.1 — R6-REFACTO-ENEMY (PARALLELIZABLE seul, bug-fixer Sonnet, 6-8h)

`Assets/Scripts/Entities/Enemy.cs` 2051 LOC → split partial classes.

**Plan split** :
- `Enemy.cs` core (300 LOC)
- `Enemy.Movement.cs` (400 LOC)
- `Enemy.Combat.cs` (400 LOC)
- `Enemy.Behaviors.cs` (400 LOC)
- `Enemy.Stats.cs` (200 LOC)
- `Enemy.Anim.cs` (300 LOC)

**Files touchés** : Enemy.cs + plusieurs Enemy.*.cs nouveaux — seul cet agent travaille sur Enemy*, PARALLEL OK avec autres tickets.

### 🟢 P3.2 — R6-REFACTO-TOWER (PARALLELIZABLE seul, bug-fixer Sonnet, 6-8h)

`Tower.cs` 2254 LOC → split. Plan identique P3.1.

**Files touchés** : Tower.* — PARALLEL OK avec autres.

### 🟢 P3.3 — R6-REFACTO-CASTLE (PARALLELIZABLE seul, bug-fixer Sonnet, 2h)

`Castle.cs` 762 LOC → split en partial.

**Files touchés** : Castle.* — PARALLEL OK.

### 🟡 R6-PARITY-004-FLUX-REGEN (OPTIONAL longue, feature-dev Sonnet, 6-8h)

Regen 22 VFX textures Flux Schnell pour V6 visual variety vs procédural ParticleSystem.

**Action** :
1. Start ComfyUI:8188 local si pas tournant (cf memory `reference_flux_local`)
2. Use `tools/gen_textures.py` (path : `/Users/mike/Work/milan project/tools/gen_textures.py`) avec prompts spécifiques :
   - sparkle_gold, explosion_big, blood_splat, glyph_dark, heal_aura, lightning_bolt, poison_cloud, shield_aura, slow_aura, smoke_gray, etc. (22 total)
3. Import PNG → `Assets/Textures/VFX/` + Unity import settings (sRGB, mipmaps, alpha is transparency)
4. Wire dans VfxPool texture sheets (per VFX type)
5. Live test : sparkle gold pickup, explosion big enemy death, etc.

**Files touchés** : 22 PNG + VfxPool*.cs — PARALLEL OK mais long, dispatch en background pendant que d'autres tickets parallèles tournent.

## 🔴 SERIAL queue (pas dispatcher en parallèle entre eux ni avec P3.x si touche Main.unity)

### P2.2 — R6-PARITY-FLOATING-POPUP-SCENE-FIX (SERIAL Main.unity, exec direct OR bug-fixer)

Remove ghost UIDocument component `&228555130` from FloatingPopupController GameObject dans Main.unity.

**Action** :
1. Backup `Main.unity.bak`
2. YAML edit : supprimer le block component vide
3. Verify parse OK
4. Commit + push

**Files touchés** : Main.unity — DOIT être SERIAL (jamais 2 agents simul sur Main.unity).

## Cascade dispatch suggérée (exec libre de re-prioriser)

**Wave 2 Slot 1-4 parallel (différents fichiers, PARALLELIZABLE)** :
- P1.5 ambient lighting
- P1.6 water/lava anim
- P1.1b LINQ null-check
- P3.3 refacto Castle.cs (court, 2h)

**Wave 3 dispatch après slot libère (différents fichiers, PARALLELIZABLE seul)** :
- P3.1 refacto Enemy.cs (~6-8h, long)
- P3.2 refacto Tower.cs (~6-8h, long)
- R6-PARITY-004-FLUX-REGEN (~6-8h, background si Flux dispo)

**Serial après tout autre work done** :
- P2.2 FloatingPopup scene fix (Main.unity SERIAL)

## Time cap

Soft cap autonomous mode. Sprint R6-PARITY-V4 effective complete = P1.5/P1.6/P1.1b done (le reste = P3 dette technique, hors sprint). Estimation ~3-4h pour wave 2 parallel.

## Verification post-deploy attendue

Une fois nouveau deploy gh-pages avec wave 2 commits :
- qa-tester ré-audit live `/v6/` (ou Mike retest)
- Target V6 @ **90-95% parité V4**

## Constraints rappel charter §1 + nouvelles règles

- Cap 500 LOC strict (sauf legacy >500 hors scope)
- No Sub-Opus spawn (Sonnet feature-dev/bug-fixer)
- **NEW** : feature done = wire + live test + diff /v4/ confirmé
- **NEW** : Parallel-safe = différents fichiers .cs ; Serial = Main.unity + ProjectSettings shared
- Self-report 100 mots max

## Ack expected

`.claude/supervisor/acks/2026-05-12-HHhMM-lift-pause-backlog-wave-2-ack.md` :
- Confirmation lift PAUSE-PORT-PIVOT-WIRING
- Batch wave 2 dispatched (4 worktrees parallèles + IDs)
- Parallel/serial classification understood

## Status

⏳ pending exec ack + dispatch wave 2 + qa-tester R1803 verification



---

# 🟢 BACKLOG-WAVE-3-IDLE-FILLER — 2026-05-12 19h48

**Context superviseur** : Mike debug Unity Editor Safe Mode post-commit `4476039` (10 errors compile resolved). Pendant qu'on attend Unity reload + retest, fournir 3 tâches parallel-safe **non-bloquantes pour Mike**, **n'altèrent pas Main.unity / ProjectSettings**, et ne génèrent pas de reimport asset majeur.

## Q-PARITY-V4-005-IMPL-cap-EnemyBoss — AUTO-RESOLVED

Re-mesure : `EnemyBossBehaviors.cs` désormais **446 LOC** (`wc -l` 19h48) — sous cap 500 LOC charter §1 règle #3. Probablement compacté lors d'un cherry-pick subsequent ou refacto P3.1 Enemy partials extraction. **Aucune action requise**. Marquer `[resolved]` dans `questions-to-supervisor.md`.

## TASK A — GLTFast race condition (RESEARCH-ONLY, ~30-45 min)

**Symptôme observé Unity Editor Console** : 50+ models `Assets/Models/Heroes/Quaternius/UltimateAnimatedCharacters/*.gltf` + `Assets/Models/Enemies/*.gltf` fail import avec :
```
InvalidOperationException: JobHandle.Complete() on SortAndNormalizeBoneWeightsJob
```

**Hypothèse** : GLTFast 6.x + Unity 6 LTS race condition Burst-compiled job.

**Livrable** : `.claude/research/2026-05-12-gltfast-race-condition.md` avec :
1. **Root cause** : confirmer via web search GLTFast issues GitHub + Unity 6 + Burst compiler interplay
2. **Impact** : combien de models concernés (`find Assets/Models -name "*.gltf" | wc -l`), gameplay impact (heroes/enemies rendu fallback)
3. **3 options fix avec trade-offs** :
   - Option 1 : Downgrade GLTFast `Packages/manifest.json` à version stable Unity 6
   - Option 2 : Disable Burst pour GLTFast jobs (`com.unity.burst` flag)
   - Option 3 : Alternative loader (ModelImporter natif Unity au lieu de runtime GLTFast)
4. **Reco superviseur** : option recommandée + estimation effort + risque

**Type** : `bug-fixer` subagent ou `general-purpose` research (no code change).
**Files touchés** : **AUCUN** .cs / .meta / asset. Seul nouveau fichier markdown.
**Parallel** : ✅ totalement isolé.

## TASK B — Worktree cleanup safe (P2.1 cascade, ~15-30 min)

**État** : 21 worktrees actifs (down from 34, mais persistant stale après P2.1 cleanup wave 1). Charter §2 D8 seuil 5+ → drift monitoring.

**Action** :
1. `git worktree list` énumère tous
2. Pour chaque worktree : check si branche worktree mergée dans `main` via `git branch -a --merged main | grep <branch>`
3. Si mergée ET aucun fichier modifié non-committé dans la worktree dir → safe to remove
4. `git worktree remove <path>` (ou `--force` si seulement files generated)
5. `git worktree prune` pour cleanup metadata
6. Cible : ramener à **≤ 4 worktrees actifs** (sous seuil D8)

**Commit** : `chore(hygiene): cleanup N stale worktrees (P2.1 cascade exec-direct wave 2)`

**Files touchés** : aucun fichier Unity, juste `.git/worktrees/` métadonnées + dirs externes.
**Parallel** : ✅ safe.

## TASK C — P1.1b LINQ null-check verification + fix résiduel (~30 min)

**Context** : Commit `443c816` bulk null-check 33 UI controllers défensif générique (Q<T>?.something). Mais P1.1b ciblait spécifiquement **`ArgumentNullException Parameter name: collection`** = appel LINQ sur collection null (pas Q<T> null).

**Action** :
1. `grep -rn "\.Sum(\|\.Average(\|\.Min(\|\.Max(\|\.OrderBy(\|\.Where(\|\.Select(\|\.Aggregate(\|\.Count(\|\.First(\|\.Last(" Assets/Scripts/UI/ Assets/Scripts/Systems/ Assets/Scripts/Data/`
2. Filter callers : stats panels, leaderboard, run summary, achievements aggregation
3. Pour chaque match identifier si la collection peut être null (field nullable, IEnumerable param, Dictionary[key])
4. Si oui → add null-coalesce `(coll ?? Array.Empty<T>()).Sum(...)` OU `if (coll == null) return; coll.Sum(...)`
5. Si tous ces sites déjà gardés par `443c816` Q<T> defensive → produire `.claude/audit/2026-05-12-linq-null-check-audit.md` "ALL CLEAR" et marquer P1.1b done

**Commit** (si fix nécessaire) : `fix(runtime-crash-3b): null-check collections LINQ aggregation (N sites — P1.1b)`

**Files touchés** : Assets/Scripts/UI/**.cs, Assets/Scripts/Systems/**.cs, Assets/Scripts/Data/**.cs (jamais Main.unity).
**Parallel** : ✅ safe.

## Constraints rappel

- **NE PAS toucher** : `Assets/Scenes/Main.unity`, `ProjectSettings/*.asset`, `Packages/manifest.json` (sauf TASK A research-only, qui propose mais n'applique pas)
- **NE PAS modifier** : .meta files (sauf strictement nécessaire avec trailing newlines comme `69cb53d`)
- **NE PAS dispatch** : refacto god class (P3.4+ frozen jusqu'à validation Mike compile clean post-`4476039`)
- Self-report 100 mots max par task dans ack

## Cascade dispatch suggérée

- **Slot 1** : TASK A (bug-fixer ou general-purpose, RESEARCH, background)
- **Slot 2** : TASK B (general-purpose direct, EXEC, foreground rapide)
- **Slot 3** : TASK C (bug-fixer, FIX si besoin, background)

Tous 3 lançables **en parallèle dans le même message** (différents scopes, zéro overlap).

## Mode

Time cap : soft 2h (jusqu'à ~22h00). Sprint R6-PARITY-V4 déjà ✅ complete 85% V4 confirmé. Ces 3 tâches = hygiene + dette technique + research, hors sprint effectif.

## Ack expected

`.claude/supervisor/acks/2026-05-12-HHhMM-backlog-wave-3-idle-filler-ack.md` :
- Confirmation Q-PARITY-V4-005-IMPL-cap-EnemyBoss auto-resolved
- 3 task IDs dispatched (worktree IDs si subagents)
- ETA chaque task

## Status

⏳ pending exec ack + dispatch slots 1-3 parallèles

---

# 🟢 TASK D — DEPRECATED API SWEEP (low priority, non-blocking) — 19h57

**Context** : Post-`a57e396` compile clean attendu, restent ~30 warnings deprecated API non-bloquants. Batch fix dans une seule passe propre.

## Sites à patcher

**TMPro API change (3 sites)** — `enableWordWrapping` deprecated → `textWrappingMode`
- `Assets/Scripts/Entities/Castle.cs:85`
- `Assets/Scripts/Systems/GhostPreviewController.cs:202`
- `Assets/Scripts/UI/FloatingPopupController.cs:252`

Replace pattern :
```csharp
// AVANT
text.enableWordWrapping = true; // ou false
// APRÈS
text.textWrappingMode = TextWrappingModes.Normal; // ou .NoWrap
```

**UI Toolkit API change (~15 sites)** — `VisualElement.transform` ITransform deprecated → `style.translate/rotate/scale` + `resolvedStyle.translate/rotate/scale`
- `Assets/Scripts/UI/ComboHudController.cs:121,129,132,140,143,170,181,189,192,208,215,216`
- `Assets/Scripts/UI/HeroSkillBarController.cs:230,239`

Replace patterns :
```csharp
// AVANT (write)
elem.transform.scale = new Vector3(s, s, 1f);
elem.transform.position = new Vector3(x, y, 0f);
// APRÈS (write)
elem.style.scale = new Scale(new Vector3(s, s, 1f));
elem.style.translate = new Translate(x, y);

// AVANT (read)
var s = elem.transform.scale;
// APRÈS (read)
var s = elem.resolvedStyle.scale.value;
```

**Nullable warnings (5 sites)** — CS8602 dereference of possibly null + CS8601 possible null assignment
- `Assets/Scripts/Entities/Enemy.Behaviors.cs:163-166` — add `?` or null-check before deref
- `Assets/Scripts/Entities/Enemy.Update.cs:122` — same
- `Assets/Scripts/Systems/GhostPreviewController.cs:186` — explicit null-coalesce

## Action

1. Single quality-maintainer subagent passe (background, ~30 min)
2. Test compile + zéro warning (sauf si dependency genuine warning)
3. Commit : `chore(api-update): TMPro + UIToolkit + nullable deprecated APIs (30 warnings → 0)`
4. Push

## Constraints

- **Aucune logique runtime modifiée** — juste API surface mapping
- Verify aucun `var` qui infère un type changeant (Vector3 → Scale wrapper)
- Cap LOC delta : +/-50 LOC max attendu

## Status

⏳ pending exec dispatch slot libre

---

## 2026-05-12 22h50 — URGENT PARITY-FINISH BACKLOG (Mike demande arrêt impossible avant 100%)

Mike feedback critique 22h50 : "tant que t'es pas a parité interdiction de t'arreter c'est pas possible ça tu dois etre en train de faire des choses ou de faire faire des chsoes a l'aurtre agents ACTIVE nom de zeus".

Status V6 : ~95% parité V4 (HUD wired, VfxPool fonctionnel, gameplay loop OK). Reste 5% pour atteindre 100%.

### Backlog parallel-friendly (lance autant que tu peux)

**TASK PF1** (P0) — ProjectilePool prefab assignment ou warn-once
- Fichier : `Assets/Scripts/Systems/ProjectilePool.cs:48`
- Problème : 40x spam "[ProjectilePool] projectilePrefab is null — creating primitive sphere fallback" à chaque démarrage
- Option A (préférée) : Inspector wire un Projectile prefab dans `ProjectilePool` GameObject (Main.unity YAML edit Inspector)
- Option B (fallback) : warn-once pattern — bool `_warnedNullPrefab`, log seulement la 1ère fois
- Commit : `fix(runtime-pool): ProjectilePool warn-once OR Inspector wire prefab`

**TASK PF2** (P1) — AudioMixerController auto-load fallback
- Fichier : `Assets/Scripts/Systems/AudioMixerController.cs`
- Problème : 4x warnings "AudioMixer not assigned — cannot set Master_Volume/SFX/Music/UI_Volume"
- Fix : dans `OnAwakeSingleton()`, si `mixer == null` essayer `Resources.Load<AudioMixer>("Audio/MainAudioMixer")` ou autre path. Warn-once si vraiment introuvable.
- Commit : `fix(runtime-pool): AudioMixerController Resources.Load fallback`

**TASK PF3** (P1) — WorldMapController null-guard worldmap-level-grid
- Fichier : `Assets/Scripts/UI/WorldMapController.cs:120`
- Problème : `[WorldMapController] worldmap-level-grid not found in UXML` — error log 1x au Start
- Cause : WorldMapController est sur HUD GameObject mais cherche element de WorldMap.uxml séparé
- Fix : `Debug.LogError` → `Debug.LogWarning` (just downgrade) + early return graceful. Architectural fix séparé.
- Commit : `fix(runtime-pool): WorldMapController downgrade error to warning (UXML separation TODO)`

**TASK PF4** (P2) — MonoSingleton OnDestroy cascade auto-create suppression
- Fichier : `Assets/Scripts/Common/MonoSingleton.cs`
- Problème : OnDestroy → OnDestroySingleton → accède à MonoSingleton<X>.Instance qui auto-créé un GameObject pendant scene close → cascade 7+ warnings au stop play mode
- Fix : MonoSingleton.Instance check `Application.isPlaying` + return null pendant destroy si scene unloading. Ou pattern `_destroying` flag.
- Commit : `fix(runtime-pool): MonoSingleton skip auto-create during scene unload (OnDestroy cascade)`

**TASK PF5** (P2) — ProjectilePool Inspector wire (proper fix complement de PF1B)
- Editor scene YAML edit Main.unity GameObject `ProjectilePool` (find via find_gameobjects component "CrowdDefense.Systems.ProjectilePool")
- SerializeField `projectilePrefab` → assign un GameObject prefab Projectile depuis Assets (find Projectile.prefab if exists)
- Tester via UnityMCP refresh+play → confirm 0 ProjectilePool warning
- Commit : `fix(scene): wire ProjectilePool projectilePrefab in Main.unity`

### Process

- Lance MAX parallel (4 slots min)
- Push autonome chaque commit
- Sprint complete = console 0 warning + 0 error
- T1 notify si tout fini → notif Mike "PARITY 100% COMPLETE"
- Auto-test via UnityMCP refresh+play+read_console après chaque commit


---

## 2026-05-12 23h15 — MASSIVE BACKLOG (Mike directive : tu ne dois jamais être idle)

Mike feedback critique 23h15 : "l'autre session est en attente de travail c'est pas normal. SAUF si tu as atteint ton objectif final."

État sprint : V6 effectif à **96-97% parité V4** (audit `ad3804d` `.claude/audit/2026-05-12-23h00-v4-v6-parity-gap.md`). 4 bug-fixers tournent en parallèle sur top 10 audit. **Toi tu prends ce qui RESTE + complètes le sprint sur d'autres axes**.

### Backlog massif parallel-friendly (pioche dans l'ordre)

#### Track A — Wizard King + AI Hub + Kraken (audit P2/P3 #3,#6,#7)

**TASK A1 — wizard_king teleport+rain** (audit #3, P2 non-régressif)
- V4 doc intent : wizard téléporte aléatoirement + rain de projectiles
- V6 effectif : ne l'implémente pas (V4 source ne l'implémente pas non plus, donc non-régressif strict)
- **Action** : implémenter quand même pour "richer V6 than V4". 40-60 LOC dans `Enemy.Behaviors.cs` partial. Pattern : timer cooldown teleport (5s), spawn 8 projectiles en cercle après teleport.
- Commit : `feat(parity++): wizard_king teleport+rain attack (V4 doc intent)`

**TASK A2 — ai_hub drone swarm pattern** (audit #6, P2)
- V4 had drone swarm with formation. V6 currently simpler.
- 40 LOC dans `EnemyBossBehaviorsStatic.TickAiHubBurst` ou similar
- Spawn 4-6 mini drones en formation (square, line, triangle), follow boss
- Commit : `feat(parity++): ai_hub drone swarm formation (audit P2 #6)`

**TASK A3 — kraken tentacle slam** (audit #7, P3 backlog)
- V4 had tentacle slam pattern différencié. V6 has basic tentacles.
- 40 LOC : add tentacle slam telegraph (1s yellow flash) + damage cone
- Commit : `feat(parity++): kraken tentacle slam pattern (audit P3 #7)`

#### Track B — VfxPool wire 20 unassigned prefabs (audit bug-fixer #2)

**TASK B1 — Find/Create VFX prefabs + wire VfxPool**
- bug-fixer #2 audit dit : VfxPool a 20 prefabs unassigned. Code fallbacks expected mais ideal serait wire vrais prefabs.
- Find Assets pour `*Impact*.prefab`, `*Death*.prefab`, etc. (les 22 VFX types)
- Si pas trouvés : créer placeholder prefabs minimal (GameObject + ParticleSystem)
- Wire dans VfxPool component sur Main.unity ou VfxPool prefab
- Commit : `fix(parity): VfxPool wire 20 prefabs (audit cleanup)`

#### Track C — Sprint-gate auto-QA (audit #8, P1)

**TASK C1 — auto-qa-runner sprint-gate run**
- Mike avait setup `.claude/qa/scenarios/*.mjs` pour test scenarios Chrome MCP
- Run l'auto-qa-runner agent type sur le V6 effectif (build local Unity ou via UnityMCP screenshot)
- Output : `.claude/qa/reports/sprint-R6-PARITY-V4-final.md`
- Commit : `chore(qa): sprint-R6-PARITY-V4-final auto-QA gate report`

#### Track D — Performance + polish

**TASK D1 — UnityMCP gameplay 10-wave run**
- Via UnityMCP `http://127.0.0.1:8080/mcp` session `1bc3a4c5aca949308c2567683326142d`
- Run 10 waves consecutive via execute_code + StartNextWave loop
- Record : FPS minimum, GC alloc, draw calls
- Output : `.claude/audit/2026-05-12-23h30-perf-10waves.md`
- Commit : `chore(perf): 10-wave gameplay perf audit (FPS, GC, drawcalls)`

**TASK D2 — Code dedup audit**
- Run quality-maintainer agent sur `Assets/Scripts/` 
- Find duplicate patterns, dead code, TODO debt
- Output : `.claude/audit/2026-05-12-23h30-code-dedup-audit.md`
- Commit : `chore(maintenance): code dedup + dead code audit`

#### Track E — Documentation

**TASK E1 — Update CLAUDE.md crowd-defense**
- Vérifier que CLAUDE.md root reflète l'état V6 actuel post-refonte stratégique
- Update sections : workflow, debug API, current backlog, references
- Commit : `docs(claude.md): update post-PARITY-V4-final state`

**TASK E2 — STATUS.md update**
- `.claude/status/STATUS.md` doit refléter sprint complete @ 99%+
- Mark active sprints done, next sprint preparation
- Commit : `docs(status): mark sprint R6-PARITY-V4 complete + V6-POLISH sprint setup`

### Process

- **PIOCHE TOUJOURS du track le plus prioritaire (A1 → A2 → A3 → B1 → C1 → D1 → D2 → E1 → E2)**
- 4 slots simultanés (lance 4 agents Sonnet feature-dev en parallèle, worktree si conflicts attendus)
- Push autonome chaque commit
- Si conflict avec wave-2 bug-fixers tournant en parallèle : git pull --rebase, re-apply, push
- Si backlog vidé : **lance ton propre audit** "où sont les gaps restants V4→V6?" + viens push more tickets

### Coordination

- 4 bug-fixers Opus tournent en parallèle wave-1 + wave-2 (PF1-5 + SR1-2 + B1-2 + H1). Toi tu prends les tracks A/B/C/D/E ci-dessus.
- Si tu vois un commit conflict prévisible (ex: même partial Enemy.cs), serialize par track.

### Time cap

Pas de time cap — tu pioches tant qu'il y a du travail. **Tu ne dois JAMAIS être idle.**


---

## 2026-05-12 23h40 — WAVE 3 BACKLOG (player loop gaps + Inspector wires)

**Mike feedback 23h25 + 23h30** : V6 user-facing pas du tout parité V4. Touches différentes, pas de menu, pas de nav, caméra déconne, pas de texture/asset wired, pas d'animation, pas de musique, pas de HUD visible côté joueur. Mike rappelle V4 player loop : Menu → Hero WASD move → walk in BuildPoint circle → tower picker → place tower → wave spawn → kill → next level + debug level world1-showcase.

**4 bug-fixers Opus tournent en parallèle wave-3** sur :
- qa-tester audit V4 vs V6 visible (Chrome MCP /v4/ + UnityMCP screenshots)
- MenuScene + SceneNavigation
- Camera + Input bindings
- Music + Audio + Animation wiring
- Player loop (BuildPoint + Hero move + level transition + debug)

**Toi tu prends WAVE 3 BACKLOG orthogonal** (zone safe sans collision avec bug-fixers en flight) :

### TASK W3-V1 — VfxPool wire 20 unassigned prefabs

bug-fixer #2 audit (`/Users/mike/Work/crowd-defense/.claude/audit/2026-05-12-scene-wires-audit.md`) listait 20 VFX prefabs unassigned dans VfxPool component. Procedural fallback fonctionne mais ideal = wire real VFX assets.

Steps :
1. `find Assets -name "*.prefab" | grep -i vfx` → trouve les prefabs VFX
2. Si vrais VFX prefabs existent, wire dans VfxPool component sur Main.unity ou VfxPool.prefab
3. Si pas trouvés, créer placeholder VFX prefabs minimal (`new GameObject + ParticleSystem` via SetupTool script, sauvegarder en prefab)
4. Commit : `fix(parity): VfxPool wire 20 prefabs (audit cleanup, procedural fallback → real assets)`

### TASK W3-V2 — Cutscenes 10 worlds wired confirmation

V4 a 10 cutscenes ASCII (1 par monde, intro narrative). V6 audit dit `parity-v4-NNN` shippé mais wiring runtime non vérifié.

Steps :
1. `find Assets -name "*Cutscene*.asset"` → 10 cutscene assets expected
2. CutsceneController.cs : verify il consomme bien CutsceneRegistry ou similar
3. LevelRunner.TryPlayWorldCutscene → trace dans console au début level
4. Test via UnityMCP : load W1-1 → cutscene plays ? (4-line text overlay 3s)
5. Si broken : fix + commit `fix(parity): cutscenes 10 worlds wired runtime`

### TASK W3-V3 — Achievements 56 wired

V6 audit dit 56 achievements existent. Verify ils sont registered + tracked + display.

Steps :
1. `find Assets -name "Achievement*.asset"` → 56 expected
2. AchievementRegistry.asset → verify expose all 56
3. AchievementTracker.cs → verify subscribes to events + persists
4. UnityMCP test : trigger kill 10 enemies → check `_kills` counter + `OnFirstBlood` achievement
5. Fix wiring si broken, commit `fix(parity): Achievements 56 wired + tracked`

### TASK W3-V4 — Meta-upgrades 10 wired

V4 had 10 meta-upgrades (Trophies system : +5¢ start per trophy etc.).

Steps :
1. `find Assets -name "MetaUpgrade*.asset"` → 10
2. MetaUpgradeController.cs verify + Persist save
3. UnityMCP test : LevelRunner.Start → query Meta.Instance.GetBonus("startGold") returns expected sum
4. Fix wiring + commit

### TASK W3-V5 — Modifiers (8 curses+blessings) runtime test

V4/V6 audit dit 8 modifiers FULL. Verify runtime.

Steps :
1. `find Assets -name "Modifier*.asset"` → 8
2. Verify each is applied via PerkSystem or DynamicEventManager
3. UnityMCP test : pick modifier → check global state change

### TASK W3-V6 — Code dedup + dead code audit

Use quality-maintainer agent (you can spawn one yourself via Sonnet feature-dev or direct).
Output : `.claude/audit/2026-05-12-23h45-code-dedup.md`

### TASK W3-V7 — STATUS.md crowd-defense create + update

`/Users/mike/Work/crowd-defense` n'a pas de `.claude/status/STATUS.md`. Create one matching pattern de `milan project`.

Sections :
- Current sprint : R6-PARITY-V4-FINAL
- V4 baseline vs V6 effectif percentage
- Recent commits list
- Pending tickets (top 10)
- Instructions next session opus (what to read first)

Commit : `docs(status): create crowd-defense STATUS.md tracker`

### TASK W3-V8 — Build Settings audit + ProjectSettings polish

- `cat ProjectSettings/EditorBuildSettings.asset` → verify scenes in build list
- `cat ProjectSettings/InputManager.asset` → verify input axes
- `cat ProjectSettings/QualitySettings.asset` → verify URP quality levels
- Output : `.claude/audit/2026-05-12-23h45-projectsettings.md`
- Commit : `chore(audit): ProjectSettings polish review`

### Process

- 4 slots simultanés parallèles
- Pioche dans l'ordre V1 → V2 → V3 → ... 
- Push autonome chaque commit
- **NEVER IDLE**. Si all 8 done, lance ton propre audit "où peut-on encore pousser" et continue.

### Coordination avec bug-fixers wave-3 Opus

Conflits possibles :
- MenuScene+SceneNav (acce90c0) touche Build Settings : si tu fais W3-V8, attendre ou git pull --rebase
- Player loop (aeab3c25) touche Hero.cs + BuildPoint : tu skip Hero/BuildPoint
- Camera+Input (a1e7367e) touche KeyBindings.cs : tu skip InputManager
- Music+Audio (aeb8fb3b) touche MusicManager : tu skip Music wiring
- qa-tester (acce90c0) lit Main.unity + screenshots : pas de write conflict

Zone safe pour toi :
- VfxPool (W3-V1)
- Cutscenes runtime (W3-V2)
- Achievements (W3-V3)
- MetaUpgrades (W3-V4)
- Modifiers (W3-V5)
- Code dedup audit (W3-V6)
- STATUS.md create (W3-V7)
- ProjectSettings audit (W3-V8)


---

## 2026-05-12 23h55 — WAVE 4 BACKLOG (Top 5 user-facing gaps audit `adb68ee` non couverts)

**État** : exec a livré W3-V1 (VfxPool 22 prefabs wired `5af7e53`) ✅. Continue pioche W3-V2..V8 puis attaque Wave 4 ci-dessous.

**Audit honnête `adb68ee`** : V6 user-facing = 45-65%. Top 5 gaps :
1. Audio silent → ✅ FIXED (`cdfe829`)
2. Menu navigation cassée → bug-fixer Opus en cours
3. Keyboard non-réactif → ✅ FIXED (`de6eec1`)
4. Animations frozen → ✅ FIXED (`cdfe829` code-driven)
5. **Textures grises** → ❌ pas couvert par bug-fixers

### Wave 4 backlog parallel-friendly

**TASK W4-T1 (P0) — Textures wire ground+path materials**
Audit `adb68ee` dit "Everything grey; no Flux PNG applied". V6 a importé 78 PNG mais materials pas assignés aux meshes ground/path.

Steps :
1. `find Assets/Resources -name "tex_*.png"` → 78 Flux textures
2. `find Assets/Materials -name "ground_*.mat" -o -name "path_*.mat"` → materials
3. `grep -l "mainTexture\|_BaseMap" Assets/Materials/*.mat` → check texture assigned in YAML
4. Pour materials avec `m_TexEnvs` vides → assign correct Flux PNG via YAML edit
5. Verify : MapRenderer.Start() applies the correct theme material to ground/path
6. UnityMCP test : Play mode screenshot → check ground textured (pas gray)
7. Commit : `fix(wiring): wire 78 Flux textures to ground+path+sky materials (audit T0 textures)`

**TASK W4-T2 (P1) — Toolbar visibility check**
HUD `tower-toolbar` element visible côté joueur ? Audit dit Toolbar low visibility.

Steps :
1. `grep "tower-toolbar" Assets/UI/HUD.uxml` → element exists ?
2. Lire TowerToolbarController.cs — Init + Show logic
3. UnityMCP test : Play mode `execute_code` get root → find tower-toolbar → check display style not "none"
4. Si missing : add to HUD.uxml + wire controller
5. Commit : `fix(wiring): TowerToolbar visibility in HUD (audit T1)`

**TASK W4-T3 (P1) — Minimap restoration**
Audit dit `[Minimap] minimap-container element not found` (fixé après cd67666 HUD parse OK ? À reverify post-fix).

Steps :
1. UnityMCP test : `root.Q("minimap-container")` returns non-null ?
2. Si oui : verify MinimapController.Start sets up minimap correctly
3. Si non : add element to HUD.uxml
4. Commit : `fix(wiring): Minimap container + controller wire (audit T1)`

**TASK W4-T4 (P1) — Ghost preview visibility check**
GhostPreviewController had `_mpb` lazy-init fix `b280a2e`. Verify ghost actually visible on tower placement hover.

Steps :
1. UnityMCP Play : execute_code → `PlacementController.Instance.SelectedTowerType = TowerType.Archer`
2. Move mouse via Input simulate
3. `find_gameobjects` GhostPreview instance → check active + position
4. Screenshot
5. Si non-visible : fix material/mesh assignment
6. Commit : `fix(wiring): GhostPreview material+mesh wire (audit T1)`

**TASK W4-T5 (P1) — Wave UI top bar / progress dots**
HUD elements `wave-pill`, `wave-progress-dots`, `wave-enemy-count` :
- Wave pill : show "Wave 1/8" current/total
- Progress dots : 8 dots indicating wave progress
- Enemy count : live count remaining

Steps :
1. WavePreviewController + LevelRunner.OnWaveStarted/OnWaveEnded events
2. Verify these update labels in HUD
3. UnityMCP test : trigger wave → check label text not empty + dots fill
4. Commit : `fix(wiring): Wave HUD pill+dots+enemy-count live updates (audit T2)`

**TASK W4-T6 (P1) — Hero Mesh_knight Animator default**
Audit dit "Enemies T-pose or slide instead of walk". AnimationController.cs loads controller from Resources at runtime, but maybe default state broken.

Steps :
1. `find Assets/Resources/Animations/Controllers -name "knight*"`
2. Verify "Idle" + "Walk" states exist with proper clips
3. Test Play mode : Hero idle = T-pose ou animated ?
4. Hero walking = animation plays ?
5. Same check on Enemy default Animator
6. Commit : `fix(wiring): Animator default states Idle+Walk for Hero+Enemy (audit T1)`

**TASK W4-T7 (P2) — 5-wave smooth cycle integration test**
Full integration test via UnityMCP :
1. Play mode + load W1-1
2. Trigger waves 1-5 consecutively (WaveManager.StartNextWave x5)
3. Mid-wave : place 3 towers (PlacementController.Place ou via BuildPoint walk-in si restored)
4. After wave 5 : check Castle still alive, Hero gained XP/perks, gold accumulated
5. Console : 0 errors, ≤10 warnings (cosmetic)
6. Output `.claude/audit/2026-05-12-23h55-5waves-integration.md`
7. Commit : `chore(qa): 5-wave smooth cycle integration test`

### Process

- 4 slots simultanés
- Push autonome chaque commit
- Si conflit avec bug-fixers Opus wave-3 actifs (a1e7367e MenuScene, a74c03d0 BuildPoint, a4d7f63a gameplay test) : git pull --rebase
- **NEVER IDLE** : if all done, audit gaps remaining et propose Wave 5 tickets


---

## 2026-05-13 02h35 — CLOSURE TASKS (sprint R6 95%+ atteint)

Sprint R6-PARITY-V4-FINAL **OFFICIELLEMENT 95%+ COMPLETE** (commit `e678a34` CLAUDE.md docs claim). Master plan target hit. Mike directive : OK idle si objectif final atteint.

**Closure light tasks** (pioche si tu veux, sinon idle acceptable) :

### CLOSE-01 — Final sprint retrospective doc
- Output : `.claude/audit/SPRINT-R6-RETROSPECTIVE.md`
- Sections : What went well, what was hard, lessons learned, next sprint recommendations
- 1-page max

### CLOSE-02 — `git log --oneline 739efc7..HEAD | wc -l` final count + categorize
- Output : compteur final commits sprint, breakdown feat/fix/chore/refactor/docs
- Add to retrospective

### CLOSE-03 — Update `.claude/status/STATUS.md` (créé par exec earlier `bdf602b`)
- Mark sprint R6 fully complete
- Note next sprint candidates (R7 polish ou N9 continuation full ou stop)

### CLOSE-04 — Cleanup worktrees stale
- `git worktree list` — identifier stales
- `git worktree remove <path>` pour ceux finished
- Cible : worktrees 0-2 (currently 5)

### CLOSE-05 — Final commit "Sprint R6-PARITY-V4-FINAL closed" 
- Empty commit ou doc-only avec sprint summary
- Tag optionnel `v6.0-parity-v4-sprint-r6`

**Si tout fait, idle acceptable**. Mike peut stopper monitor 5min via TaskStop si veut.



---

## 2026-05-13 02h47 — 🚀 R7-PUSH-100-GO (Mike chat override "avance jusque parité")

**Type** : NEW-SPRINT-GO + dispatch autonome backlog épais
**From** : Opus superviseur post-reset rôle (Mike rappel chat live "tu as full switch sur faire le dev v6 toi meme")
**Sprint** : **R7-PUSH-100** (push 85-90% → 95-100% visible parity)
**Trigger** : Mike chat direct 2026-05-13 (charter §5 override) : "Et avance jusque parité"

## Mike chat context

Mike a explicitement validé l'ouverture R7 en chat live :
> "fais un full reboot unity et mets une regle dans ta mémoire et dans celle de l'autre projet de pas tester sur web pour l'instant. Une fois que ça c'est fait refait ton état des lieux et continue ici et sur ton agent de délégation. Et avance jusque parité."

Puis correction de scope superviseur :
> "j'ai l'impression que tu as completement laché ton role de supervision depuis la codebase v4 et que tu as full switch sur faire le dev v6 toi emme"
> "il faut quand meme lui donner des trucs a faire a l'autre"
> "donne lui un vrai backlog pas juste ce que tu estime qu'il peut faire entre deux wakeup a toi"

**Implications scope** :
- Superviseur : redevient read-only sur code, écrit instructions/drift-reports/acks
- Exec session : pioche backlog R7 ci-dessous, autonomie max, dispatch worktrees parallèles
- Time cap : open-ended (Mike chat live, surveille drift criteria charter §2)
- Push notif Mike : T3 silent default, escalate T2 si batch ack utile, T1 si drift D10/D11 ou Mike escalation cat B

## Constraint critique (memory rule active)

⚠️ **PAS DE TEST WEB / WebGL** tant que Unity n'a pas patch URP+WebGL2.

- Unity 6.0.3.15f1 + URP 17.3.0 + WebGL2 = subpass input shader fail (cf scrute #37-51 _clean-log.md + iter cascade #1-13 documenté)
- Le jeu marche 100% en Unity Editor Play mode (Mike confirmed screenshot)
- Validation feature/parity : **EXCLUSIVEMENT** Unity Editor Play mode OU UnityMCP HTTP `http://127.0.0.1:8080/mcp` (refresh_unity + manage_editor + read_console + execute_code + manage_scene)
- Auto-build-loop SUSPENDU pour le web tant que pas de fix Unity
- Build WebGL test = false negative (canvas pur noir) → NE PAS spawner qa-tester via Chrome MCP sur /v6/

## In-flight supervisor side (NE PAS PIOCHER, NE PAS DUPER)

- **T-VISUAL-002** Animator state machines wire (hero + 28 enemies idle/walk transitions) → agent `a5dc5492ebc56126f` background, supervisor-spawned avant reset rôle, Mike approuvé "laisse finir". Worktree `agent-a203f9d46f1056019` locked. Si ce ticket reapparait dans le backlog ci-dessous → SKIP, vérifier git log post-completion notif.
- **T-AUDIO-001** SFX clips real wire → ✅ DONE commit `d97d57f` (3 SFX wired : enemy_die_basic + castle_hit proxy + boss_charge proxy). Fallback procédural conservé pour clips inconnus futurs.
- **PERF-3FIX** EnemyPathing + MuzzleFlash pool + HasAnimatorParam → ✅ DONE déjà shippé via commits `0f386f9` + `afc6cce` + `1cf88ce` (no-op revérifié 2026-05-13 02h45).

## Process exec (rappel charter §1 + §4)

1. Read this bloc en intégralité
2. Write `.claude/supervisor/acks/2026-05-13-HHhMM-r7-push-100-go-ack.md` avec timestamp lu + tickets piochés batch 1
3. Dispatch batch 1 (max 4 worktrees simultanés charter §1 règle #9, isolation:"worktree")
4. Self-report par agent : LOC ±, compile clean, console clean, commits livrés (charter §1 règle #8)
5. Each commit push autonome après build clean
6. Move to next batch quand ≥1 slot libéré
7. Si question cat A → write `questions-to-supervisor.md`, continue non-bloquant
8. Si question cat B → tag `escalation:true` + attend `answers-from-supervisor.md`

---

## Backlog R7-PUSH-100 — 25 tickets prioritisés

Source : `.claude/audit/SPRINT-R6-PARITY-V4-FINAL-completion.md` §§ "Residual gaps" + "Next sprint candidates" + master plan `.claude/specs/R6-PARITY-V4/*.md` + `.claude/backlog/R6-found-during-exec.md`.

Codes : `[Pn]` priorité, `[Em]` effort estimé (min ou h), `[Df]` fichiers cibles, `[Vf]` verification, `[Cf]` commit format.

### P0 — Mike-visible direct impact (parity finish)

#### R7-001 — T-VISUAL-004 Water/Lava 8 PNG frames anim

- **[P0]** **[Em : 1-2h]**
- **Ref** : completion doc ligne 44 ("Frames non importés ; placeholder colors"), residual gap #5
- **Mission** : Animer tiles water + lava via 8 PNG frames each (4 fps loop ou Frames/Sprite anim Unity).
- **[Df]** :
  - V4 ref animation (Park Defense Phaser) : `milan project/src-v3/data/textures/` ou `src-v3/entities/TextureWater.js` (chercher 8 frames patterns)
  - V6 : `Assets/Resources/Tiles/Water/*.png` + `Assets/Resources/Tiles/Lava/*.png` (à créer)
  - Sprite animator : `Assets/Resources/Animations/TileWaterAnim.controller` + `TileLavaAnim.controller`
  - Tile prefab : `Assets/Prefabs/Tiles/WaterTile.prefab` + `LavaTile.prefab` (ajouter SpriteRenderer + Animator)
- **Pipeline gen frames** :
  - Option A (recommandée) : Flux Schnell local pipeline (cf memory `reference_flux_local.md` : ComfyUI:8188 + `tools/gen_textures.py`). Prompts : "tileable seamless 256x256 water surface frame {i+1} of 8, soft ripples cycling animation"
  - Option B : Procédural Unity (ShaderGraph displacement noise) si Flux pas dispo
- **[Vf]** :
  - Compile clean (UnityMCP `refresh_unity` + `read_console` Error count 0)
  - Play mode Unity Editor : water/lava tiles animées en boucle visible
  - UnityMCP `execute_code` snippet : check `FindFirstObjectByType<Animator>()` sur tile water → state Playing
- **[Cf]** :
  - `feat(visual)(R7-001): water+lava 8-frame sprite anim — V4 parity tile motion`

#### R7-002 — Castle real .fbx art assets

- **[P0]** **[Em : 2-3h]**
- **Ref** : completion doc ligne 26 "Procedural placeholder (vrai .fbx art TBD)", residual gap #4
- **Mission** : Remplacer les 4 procedural meshes Castle (intact/cracked/ruined/critical) par vrais .fbx art assets V4-style.
- **[Df]** :
  - Current : `Assets/Editor/BuildCastleProceduralMeshes.cs` (procedural) + Castle GO Main.unity ref 4 placeholders
  - V4 art ref : `milan project/src-v3/entities/Castle.js` + `src-v3/data/castle_art.json` ou similar
  - Target : `Assets/Resources/Models/Castle/castle_intact.fbx`, `castle_cracked.fbx`, `castle_ruined.fbx`, `castle_critical.fbx`
  - Wire : Castle.cs (probably Castle.VFX.cs ligne CastleSkinController.cs) — assign MeshFilter.sharedMesh selon HP threshold
- **Source assets** :
  - Option A : Mike fournit .fbx via Asset Store / 3D modeling
  - Option B : ProBuilder package (déjà importé d'après workspace dirty `ProjectSettings/Packages/com.unity.probuilder/`) → modéliser 4 stages castle low-poly directement Unity
  - Option C : Procedural amélioré (current = placeholder, can iterate to ressemble V4 look)
- **[Vf]** :
  - Compile clean
  - Play mode Unity Editor : Castle se dégrade visuellement selon HP (100%→intact, 75%→cracked, 50%→ruined, 25%→critical)
  - Mike screenshot validation post-implem
- **[Cf]** :
  - `feat(art)(R7-002): real castle .fbx 4 stages (intact/cracked/ruined/critical) — V4 parity`

#### R7-003 — T-INTEGRATION-001 Loader.unity → EditorBuildSettings

- **[P0]** **[Em : 30min]**
- **Ref** : completion doc ligne 49 "Loader.unity créé mais pas ajouté à EditorBuildSettings", residual gap #7
- **Mission** : Ajouter Loader.unity en scene 0 dans EditorBuildSettings (skip splash native flow).
- **[Df]** :
  - `ProjectSettings/EditorBuildSettings.asset` — ajouter entry `Loader.unity` scene 0 enabled
  - Vérifier `Assets/Scenes/Loader.unity` existe (créé via `BuildLoaderSceneTool` éditeur précédemment, voir git log)
  - Loader.unity → load Menu.unity async via `SceneManager.LoadSceneAsync("Menu")` ou similar — read existing implem
- **[Vf]** :
  - Compile clean
  - Unity Editor : Open Loader scene → Play → switch to Menu automatiquement
  - UnityMCP `execute_code` : `EditorBuildSettings.scenes[0].path == "Assets/Scenes/Loader.unity"`
- **[Cf]** :
  - `fix(integration)(R7-003): Loader.unity scene 0 in EditorBuildSettings — skip splash`

#### R7-004 — T-VALIDATE-10CHECKS Mike validation checklist + UnityMCP self-tests

- **[P0]** **[Em : 1h]**
- **Ref** : completion doc ligne 163-176 "Manual verification plan for Mike (top 10 checks Unity Editor Play mode)", residual gap #10
- **Mission** : Préparer 2 livrables :
  1. Checklist Mike side `.claude/qa/MIKE-VALIDATION-R7.md` — 10 checks détaillés Play mode (audio, visual, castle, BuildPoint, ULT, animations, RadialMenu, 5-wave smoke, achievements)
  2. UnityMCP self-test script `.claude/qa/r7-self-test.sh` qui automate ce qui est automatisable (compile state, Play mode 30s no NRE, console error count, Object.FindFirstObjectByType checks)
- **[Df]** :
  - Create `.claude/qa/MIKE-VALIDATION-R7.md` (markdown checklist 10 items + screenshots placeholders)
  - Create `.claude/qa/r7-self-test.sh` (curl UnityMCP HTTP JSON-RPC pattern, voir `.claude/supervisor/tools/notify.sh` pour pattern shell)
- **[Vf]** :
  - r7-self-test.sh runs end-to-end : init Unity Play → 30s → read console → assert error count 0 → exit code 0
  - Mike checklist sentinel : 10 sections complètes avec UnityMCP commande de check si applicable + verification critère
- **[Cf]** :
  - `chore(qa)(R7-004): Mike validation checklist + UnityMCP self-test r7-self-test.sh`

### P1 — Polish + dev UX

#### R7-005 — T-UI-002 MuteToggleController scene wire

- **[P1]** **[Em : 30min]**
- **Ref** : completion doc ligne 46 ("Component absent du scene"), master plan T-UI-002
- **Mission** : MuteToggleController existe (cf `Assets/Scripts/UI/MuteToggleController.cs` 30+ LOC déjà lu superviseur). Le component cherche `Button "btn-mute"` dans UIDocument. Need : 1) ajouter `btn-mute` à `Assets/UI/HUD.uxml`, 2) attacher MuteToggleController component au GO HUD ou créer dedicated GO, 3) verify icône sprite "muted/unmuted" assigné.
- **[Df]** :
  - `Assets/UI/HUD.uxml` — ajouter `<Button name="btn-mute" class="hud-btn-mute" text="🔊" />` dans top-right cluster
  - `Assets/UI/HUD.uss` — style `.hud-btn-mute` (24×24, top-right anchor)
  - `Assets/Scenes/Main.unity` — attach `MuteToggleController` à GO HUD existant (ou créer "MuteToggle" GO child)
  - `Assets/Scripts/UI/MuteToggleController.cs` — verify Audio.SetMuted() existe dans `Assets/Scripts/Systems/Audio.cs`
- **[Vf]** :
  - Compile clean
  - Play mode : bouton 🔊 visible top-right HUD, click → toggle mute (icône 🔇), reload scene → state persiste (PlayerPrefs `cd.audio.muted`)
  - UnityMCP : `execute_code` query VisualElement btn-mute display style != none
- **[Cf]** :
  - `fix(ui)(R7-005): MuteToggle button wire HUD + Audio.SetMuted persistence — V4 parity`

#### R7-006 — T-DEBUG-001 F1 cheat console __cd.* API

- **[P1]** **[Em : 1h]**
- **Ref** : completion doc ligne 47 ("F3 HUD existe, F1 console absent"), master plan T-DEBUG-001
- **Mission** : Réactiver F1 toggle cheat console (DebugConsole.cs existe `Assets/Scripts/Common/DebugConsole.cs`). Wire input + populate __cd.* API parity V4 console (`window.__cd.runner`, `__cd.scene`, `__cd.goto`, `__cd.debugOn/Off`, `__cd.unlockAll`, `__cd.toggleCornerLabels`).
- **[Df]** :
  - `Assets/Scripts/Common/DebugConsole.cs` — verify component existe + Toggle() method
  - `Assets/Scripts/Common/KeyBindings.cs` (ou InputManager.cs) — bind F1 → DebugConsole.Toggle()
  - Create new file `Assets/Scripts/Common/CdDebugApi.cs` static class avec :
    - `Goto(string levelId)` → LevelRunner.LoadLevel(levelId)
    - `DebugOn()` / `DebugOff()` → PlayerPrefs cd_debug + reload
    - `UnlockAll()` → SaveSystem.UnlockAllLevels()
    - `ToggleCornerLabels()` → MapRenderer.toggleCornerLabels (si existe)
    - `Runner` property → LevelRunner.Instance
  - DebugConsole UI : add inputfield + parse "goto W1-1", "unlock", "debug on"
- **[Vf]** :
  - Compile clean
  - Play mode : F1 → console panel apparaît, type "goto W2-3" + Enter → switch level
  - Type "unlock" → tous niveaux débloqués (savefile updated)
- **[Cf]** :
  - `feat(debug)(R7-006): F1 cheat console + CdDebugApi static (__cd.* parity V4)`

#### R7-007 — T-CLEANUP-DIAG Remove EnsureMainCamera + ImguiDiagOverlay

- **[P1]** **[Em : 20min]**
- **Ref** : commits `d053fe7` + `664dfc1` (diagnostic code post-cascade WebGL épuisée)
- **Mission** : Le jeu fonctionne 100% en Unity Editor (Mike screenshot confirmed). Les 2 composants diagnostic sont devenus orphelins/cruft.
- **Decision Mike-side avant exec** : ⚠️ Catégorie B (decision Mike) — Mike doit confirmer "OUI supprime EnsureMainCamera + ImguiDiagOverlay, je verrai si tout marche encore sans". L'exec écrit dans `questions-to-supervisor.md` :

```
[QUESTION] R7-007 cleanup diagnostic code — Catégorie B
Q: Supprimer EnsureMainCamera.cs + ImguiDiagOverlay.cs (diag iter#9 + #12) ?
Risque: Si Mike teste Unity Editor sans ces 2, et que Menu.unity n'a pas de Camera explicite, écran noir.
Recommendation supervisor: GARDER EnsureMainCamera (utile failsafe), supprimer ImguiDiagOverlay (debug-only).
Blocking: false (continue autres tickets entre temps)
```

Attend `answers-from-supervisor.md` (qui transite par superviseur → Mike notif T2 → Mike répond → superviseur écrit).

- **[Df]** (si GO de Mike) :
  - `Assets/Scripts/Systems/EnsureMainCamera.cs` (delete ou comment-out)
  - `Assets/Scripts/Systems/ImguiDiagOverlay.cs` (delete)
  - Vérifier qu'aucun reference dans Main.unity (chercher GUID dans scene .unity)
- **[Vf]** :
  - Compile clean
  - Play mode Unity Editor : jeu rendu identique avant/après suppression
- **[Cf]** :
  - `chore(cleanup)(R7-007): remove EnsureMainCamera + ImguiDiagOverlay diag code post-Editor-validate`

#### R7-008 — T-VISUAL-DEDUP Squash 3× ProjectilePool wires

- **[P1]** **[Em : 15min]**
- **Ref** : completion doc ligne 78 "ProjectilePool.projectilePrefab wire (3× redondant)" + residual gap #9
- **Mission** : 3 commits redondants `1917b0a` + `ed76df8` + `4dcfed2` wirent ProjectilePool.projectilePrefab. Dedup au niveau code (vérifier asset state actuel) — pas de squash git history (rewriting public history forbidden).
- **[Df]** :
  - `Assets/Scenes/Main.unity` — chercher ProjectilePool GO → vérifier 1 seul prefab assigné (pas 3 inspector entries)
  - `Assets/Scripts/Systems/ProjectilePool.cs` — verify field `[SerializeField] GameObject projectilePrefab` single (not list)
  - Si OK actuellement → just documenter dans backlog "DEDUP NOT NEEDED, asset state correct"
- **[Vf]** :
  - Inspector ProjectilePool : 1 prefab assigné, pas N redondants
- **[Cf]** :
  - `chore(cleanup)(R7-008): verify ProjectilePool single prefab assigned — dedup confirm`

#### R7-009 — T-AUDIO-003 3D AudioMixer routing pool AudioSource

- **[P1]** **[Em : 1h]**
- **Ref** : completion doc ligne 45 "PlayClipAtPoint legacy en place", master plan T-AUDIO-003
- **Mission** : Remplacer les call `AudioSource.PlayClipAtPoint(...)` legacy par pool de AudioSource préalloués routés via AudioMixer (output bus + spatialBlend selon 2D/3D).
- **[Df]** :
  - `Assets/Scripts/Systems/Audio.cs` (chercher PlayClipAtPoint usage)
  - Create new `Assets/Scripts/Systems/AudioSourcePool.cs` :
    - Pool de 8 AudioSources préalloués (round-robin)
    - Method `Play3D(AudioClip clip, Vector3 pos, AudioMixerGroup output)` → set position + assign output + Play
    - Method `Play2D(AudioClip clip, AudioMixerGroup output)`
  - Update Audio.cs callers (towers shoot, enemy hit, etc.) → use pool
- **[Vf]** :
  - Compile clean
  - Play mode : SFX entendus avec spatialisation (tower shoot loin = quiet, près = loud)
  - Profiler : zero `new GameObject` allocations sur SFX play (vs PlayClipAtPoint qui en crée 1 à chaque call)
- **[Cf]** :
  - `perf(audio)(R7-009): AudioSourcePool 8 préalloués vs PlayClipAtPoint legacy`

### P2 — Architecture debt

#### R7-010 — T-ARCH-001 CONFIRM Hero.cs partition done

- **[P2]** **[Em : 15min]**
- **Ref** : completion doc ligne 54 "Sprint suivant", master plan T-ARCH-001
- **Statut actuel verified par superviseur** : Hero.cs = 199 LOC (main) + 5 partials (Movement 100, Abilities 250, Anim 351, Combat 408, Perks 276). Total 2599 LOC, **tous partials sub-500 charter §1 règle #3** ✅. Ticket = de facto closed.
- **Mission** : Documenter closure dans `.claude/audit/R7-ARCH-001-CLOSE.md` avec table partials + LOC + responsibilities.
- **[Vf]** :
  - Document écrit, table claire
- **[Cf]** :
  - `docs(arch)(R7-010): T-ARCH-001 close — Hero.cs partition 5 partials sub-500 ✅`

#### R7-011 — T-ARCH-002 PlacementController cascade init audit

- **[P2]** **[Em : 1-2h]**
- **Ref** : completion doc ligne 55, master plan T-ARCH-002
- **Mission** : Audit ordre init des singletons (PlacementController, MapRenderer, EnemyPathing, TowerRegistry, etc.). Identifier dépendances + risk cascade NRE si scene reload.
- **[Df]** :
  - Scan `Assets/Scripts/**/*.cs` pour patterns `Awake() / Start() / OnEnable()`
  - Output `.claude/audit/R7-ARCH-002-init-cascade.md` : graph dépendances + recommandations (Awake order, manual init from GameLoop.Awake, etc.)
- **[Vf]** :
  - Document complet (table singletons × init phases × dependencies)
- **[Cf]** :
  - `docs(arch)(R7-011): T-ARCH-002 init cascade audit — Awake/Start order graph`

#### R7-012 — T-ARCH-004 Scene validator missing singletons

- **[P2]** **[Em : 2-3h]**
- **Ref** : completion doc ligne 57, master plan T-ARCH-004
- **Mission** : Tool éditeur qui scan scene au load et warn si singletons critiques manquent (PlacementController, MapRenderer, MusicManager, etc.).
- **[Df]** :
  - Create `Assets/Editor/SceneValidator.cs` (Editor-only)
  - Hook `EditorSceneManager.sceneOpened` → scan + warn console si manquant
  - List of required singletons (config inline ou ScriptableObject)
- **[Vf]** :
  - Open Main.unity → console log "Scene OK 0 missing singletons"
  - Open Menu.unity (UI-only) → adapter list (no PlacementController expected)
- **[Cf]** :
  - `feat(editor)(R7-012): T-ARCH-004 SceneValidator scan singletons on open`

#### R7-013 — T-ARCH-005 Layer collision matrix Tower/Enemy/UI

- **[P2]** **[Em : 1h]**
- **Ref** : completion doc ligne 58, master plan T-ARCH-005
- **Mission** : Configure ProjectSettings/Physics2DSettings.asset layer collision matrix : Tower never collides with Tower, Enemy with Enemy, UI with anything.
- **[Df]** :
  - `ProjectSettings/Physics2DSettings.asset` (m_LayerCollisionMatrix)
  - `ProjectSettings/TagManager.asset` (verify layers : Tower, Enemy, UI, Castle, Hero, Projectile defined)
- **[Vf]** :
  - Physics2D.Raycast(...) sur Enemy ignore Enemy layer
  - Profiler : moins de Physics2D.Update CPU si N enemies clusterred
- **[Cf]** :
  - `chore(physics)(R7-013): T-ARCH-005 collision matrix Tower/Enemy/UI/Projectile`

#### R7-014 — T-INTEGRATION-002 L.cs externalize CSV/JSON

- **[P2]** **[Em : 8-12h]** (LONG ticket — peut être splitté)
- **Ref** : completion doc ligne 59, master plan T-INTEGRATION-002
- **Mission** : `Assets/Scripts/Data/L.cs` = 47k LOC hardcoded strings (i18n). Externalize vers CSV/JSON + loader runtime.
- **[Df]** :
  - Read L.cs structure (probably static class with `public const string Foo = "..."`)
  - Create `Assets/Resources/Strings/strings-en.csv` (key,value rows)
  - Create `Assets/Scripts/Data/StringLoader.cs` runtime → load CSV au boot
  - Refactor L.Foo → L.Get("Foo") au call site (mass find&replace par script)
- **[Vf]** :
  - Compile clean
  - Play mode : tous les strings affichés identiques avant/après refactor (smoke test 5 levels)
- **[Cf]** :
  - `refactor(i18n)(R7-014): T-INTEGRATION-002 L.cs externalize CSV + StringLoader runtime`

#### R7-015 — T-INTEGRATION-003 Atomic SaveSystem transactions

- **[P2]** **[Em : 2-3h]**
- **Ref** : completion doc ligne 60, master plan T-INTEGRATION-003
- **Mission** : SaveSystem actuel = write JSON directement (risque corruption si crash mid-write). Wrap dans transaction atomique : write to .tmp → fsync → rename .tmp → .json.
- **[Df]** :
  - `Assets/Scripts/Systems/SaveSystem.cs`
  - Pattern : `File.WriteAllText(savePath + ".tmp", json)` → `File.Move(savePath + ".tmp", savePath)`
  - Plus backup .bak conservé last good save
- **[Vf]** :
  - Compile clean
  - Test : kill Play mode mid-save → verify .json pas corrupted (.tmp orphelin OK)
- **[Cf]** :
  - `fix(save)(R7-015): T-INTEGRATION-003 atomic write .tmp+rename + .bak backup`

### P3 — Gameplay fidelity (catégorie B Mike decision needed)

#### R7-016 — R6-PARITY-012-V4-FIDELITY 5 events V4 missing

- **[P3 — escalation cat B Mike]** **[Em : 2-3h si Mike GO]**
- **Ref** : instructions-to-exec.md ligne 471-478 "5 events V4 absents", master plan R6-PARITY-012
- **Decision Mike needed** : V4 EventManager = 8 events data-driven via `level.events[]`. Unity DynamicEventManager = 3 events random `% 5`. 5 events V4 absents : void_pulse / zero_g / undertow / battle_cry / hack.
- **Action exec immédiate** :
  1. Write `questions-to-supervisor.md` entry :

```
[ESCALATION cat B] R7-016 — events V4 fidelity
Q: Revenir à data-driven level.events[] per V4 fidelity (porter 5 events + refactor DynamicEventManager) OU keep Unity simplification 3 events random + documenter comme intentional divergence ?
Effort: 2-3h si V4 strict. 0h si Unity keep.
Impact: V4 strict = nostalgic player surprise gating per-level. Unity keep = simpler, less gameplay variety.
Blocking: false (autres tickets en parallèle)
```

  2. Wait `answers-from-supervisor.md` Mike answer.
  3. Si GO V4-strict : implem 5 events + refactor.
- **[Cf]** :
  - `feat(gameplay)(R7-016): 5 V4 events void_pulse/zero_g/undertow/battle_cry/hack + data-driven level.events[]`

#### R7-017 — R6-PARITY-011-COMPLETE Foire + Medieval castle skin

- **[P3]** **[Em : 1h]**
- **Ref** : instructions-to-exec.md ligne 480-484, master plan R6-PARITY-011
- **Mission** : ThemeSkins[] couvre 8/10 themes. Foire + Medieval default tint silent. Créer 2 ThemeSkin entries texture-swap mapped.
- **[Df]** :
  - `Assets/Resources/Skins/ThemeSkins.asset` (chercher SO ScriptableObject)
  - Add 2 entries : Foire (carnival textures pre-existing ? sinon flux gen) + Medieval (knights stone walls)
- **[Vf]** :
  - Play mode : Load W6-1 (Foire) → castle skin foire visible. Load W9-1 (Medieval) → castle skin medieval visible.
- **[Cf]** :
  - `feat(visual)(R7-017): Foire + Medieval castle ThemeSkins (R6-PARITY-011 complete)`

### P3 — Cleanup hygiène

#### R7-018 — T-CLEANUP-SUPERVISOR squash 5 supervisor commits

- **[P3]** **[Em : 30min — needs Mike validation history rewrite]**
- **Ref** : completion doc ligne 158 + 192, residual gap #9
- **Action immédiate exec** : Write `questions-to-supervisor.md` (cat B Mike) :

```
[ESCALATION cat B] R7-018 squash supervisor commits
Q: Squash 5 supervisor commits + 3 ProjectilePool redondants → git rebase -i + force-push ?
Risque: Rewrites public history (origin/main) → si Mike a worktree local pointant ces commits, sync break.
Recommendation supervisor: SKIP squash (history hygiene < workflow risk). Commits supervisor sont T3 silent log, pas un bruit user-facing.
Blocking: false
```

- **[Cf]** :
  - Pas de commit si Mike confirme SKIP (recommandation).

#### R7-019 — T-CONFIG-001 companyName + pin packages

- **[P3]** **[Em : 30min]**
- **Ref** : completion doc ligne 61, master plan T-CONFIG-001
- **Mission** : ProjectSettings/ProjectSettings.asset → fix `companyName` (default "DefaultCompany" → "Milan Defense Studio" ou similar Mike-validated). Plus pin package versions exactes (Packages/manifest.json).
- **[Df]** :
  - `ProjectSettings/ProjectSettings.asset` : `companyName` field
  - `Packages/manifest.json` : remplace `^X.Y.Z` ranges par versions exactes (snapshot)
- **[Vf]** :
  - Compile clean post Edit
  - File diff <20 lines (cosmétique)
- **[Cf]** :
  - `chore(config)(R7-019): companyName + packages pin exact versions`

#### R7-020 — T-WORKSPACE-CLEAN resolve recurring dirty files

- **[P3]** **[Em : 1h]**
- **Mission** : Investigate why workspace dirty files récurrents :
  - `Assets/Prefabs/VFX/Explosion.prefab` modified
  - `Packages/manifest.json` + `packages-lock.json` (ProBuilder install récent ?)
  - `ProjectSettings/QualitySettings.asset` + `VFXManager.asset` (modif sans intent)
  - Untracked : `Assets/Plugins/` + `Assets/Resources/Animations/Controllers/boss_submarin_kraken.controller` (T-VISUAL-002 WIP)
- **[Df]** :
  - `git diff Packages/manifest.json` — confirmer ProBuilder add intentional
  - `git diff ProjectSettings/*.asset` — diff small unintended ? auto-revert
  - Asset Explosion.prefab : check si MeshSplitter ou similar auto-touched
- **[Vf]** :
  - Workspace clean après commit ou revert decision
- **[Cf]** :
  - Multiple commits selon resolution (probably : `chore(config): ProBuilder package add` + `chore(scene): clean workspace dirty residuals`)

### P3 — QA broader

#### R7-021 — T-QA-10WORLDS-SMOKE Smoke test 10 worlds

- **[P3]** **[Em : 2h]**
- **Mission** : Via UnityMCP `execute_code` script qui :
  1. LoadScene Main
  2. For each level id W1-1 to W10-N : `LevelRunner.LoadLevel(id)` → wait 30s → check Castle.HP > 0 + no NRE → unload
  3. Output `.claude/qa/r7-10worlds-smoke.md` table (level × FPS × errors)
- **[Df]** :
  - Create `.claude/qa/r7-10worlds-smoke-script.cs` (UnityMCP execute_code snippet)
  - Loop 60 levels, capture metrics
- **[Vf]** :
  - All 60 levels load successfully, 0 NRE, FPS ≥ 30 average
- **[Cf]** :
  - `chore(qa)(R7-021): 10-worlds smoke test 60 levels x 30s — all green`

#### R7-022 — T-QA-AUDIT-RERUN baseline audit `adb68ee` rerun

- **[P3]** **[Em : 2h]**
- **Mission** : Rerun audit baseline `adb68ee` (audit `2026-05-12-23h00-v6-user-facing.md`) sur HEAD courant. Confirm 85-90% → target 95%.
- **[Df]** :
  - Copy/adapt audit script template
  - Output `.claude/audit/2026-05-13-v6-rerun-r7.md` (table 80 features × baseline % × current %)
- **[Vf]** :
  - Score delta delta documenté
- **[Cf]** :
  - `chore(audit)(R7-022): rerun user-facing audit baseline adb68ee → R7 progress`

#### R7-023 — T-QA-EDITOR-VALIDATION-MIKE (passive — wait Mike)

- **[P3]** **[Em : Mike-side]**
- **Mission** : Generate `.claude/qa/MIKE-VALIDATION-R7.md` (cf R7-004). Mike fait les 10 checks Editor Play mode. Exec ne pioche pas ce ticket — c'est Mike qui agit.
- Status : ne pas démarrer. Document checklist seulement (R7-004 livre ce doc).

### NEVER-IDLE filler

#### R7-024 — T-N9-CONTINUATION pioche N9-N38

- **[P3]** **[Em : illimité — autonome filler]**
- **Mission** : Si tous tickets P0-P2 done et waiting Mike validation, pioche tickets N9 à N38 du backlog précédent (réf retrospective doc + `.claude/backlog/_backlog.md`).
- Examples N-series : UIControllerBase migration restantes, R6-found-during-exec.md items, polish suggestions hors scope POLISH.

#### R7-025 — T-AUTO-LOG-UPDATE auto-update logs/audit/STATUS

- **[P3]** **[Em : ongoing]**
- **Mission** : Après chaque batch de 3-5 commits, update `.claude/status/STATUS.md` avec :
  - Tickets shipped depuis last update
  - LOC delta cumulatif
  - Compile state
  - Open questions
- Persist autonomie multi-session — quand Mike revient ou prochaine Opus session ouvre, STATUS.md raconte tout.

---

## Coordination notes

- **Max 4 worktrees simultanés** (charter §1 règle #9). 1 actuellement locked (T-VISUAL-002). Reste 3 slots.
- **Zones conflit prévisibles** :
  - `Assets/Scenes/Main.unity` : R7-005 (MuteToggle wire) + R7-002 (Castle GO assign) + R7-006 (DebugConsole prefab) → **SERIAL** ces 3 (charter §1 règle #9 + memory `feedback_parallelize_serialize_unity.md`)
  - `Assets/UI/HUD.uxml` + `HUD.uss` : R7-005 + R7-006 → **SERIAL** (1 fichier shared)
  - `Assets/Scripts/Common/KeyBindings.cs` : R7-006 → SERIAL with any future input ticket
  - `Assets/Resources/Tiles/*.png` (Water/Lava) + `Assets/Resources/Models/Castle/*.fbx` : safe parallel (different folders)
- **Parallel-safe batch suggéré (batch 1)** :
  - R7-001 Water/Lava frames (Assets/Resources/Tiles)
  - R7-003 Loader build settings (ProjectSettings)
  - R7-008 ProjectilePool dedup verify (read-only mostly)
  - R7-010 Hero.cs partition close docs
- **Sequential batch 2 (Main.unity scene wire)** :
  - R7-005 MuteToggle
  - R7-006 F1 cheat console
  - R7-002 Castle real art
- **Parallel batch 3 (architecture audits)** :
  - R7-011 Init cascade audit (read-only)
  - R7-012 SceneValidator (Editor-only new file)
  - R7-013 Collision matrix (ProjectSettings)
  - R7-019 companyName + pin

## Constraints rappel charter §1

1. Trigger Mike validé (R7 = chat override ✅)
2. Tickets référencés (master plan + completion doc + above brief)
3. Hard cap 500 LOC C# (sauf justification écrite)
4. No-feature-creep — opportunités → `.claude/backlog/R6-found-during-exec.md`
5. Compile + console gate post-commit obligatoire (UnityMCP `read_console`)
6. Pas de polish hors POLISH (sauf R7-001 + R7-002 polish autorisés Mike chat)
7. Coordination centrale par sprint : ce bloc
8. Self-report agent ≤100 mots
9. Max 4 worktrees simultanés
10. No Sub-Opus spawn (Sonnet feature-dev / bug-fixer / quality-maintainer uniquement)
11. **PAS DE TEST WEB / WebGL** (memory rule active)
12. Validation EXCLUSIVEMENT Unity Editor Play mode + UnityMCP HTTP

## Ack expected

`.claude/supervisor/acks/2026-05-13-HHhMM-r7-push-100-go-ack.md` :
- Timestamp lu
- Tickets batch 1 dispatched (paths agent IDs)
- Worktrees count post-dispatch (cible ≤ 3 nouveaux, T-VISUAL-002 toujours actif)
- Time estimate batch 1 completion
- Questions cat A → écrites `questions-to-supervisor.md`
- Questions cat B → tag escalation:true → superviseur notif Mike

## Status

🚀 **NEW SPRINT R7-PUSH-100 OPEN** — dispatch immédiat batch 1 (R7-001 + R7-003 + R7-008 + R7-010 parallel-safe). Target 95-100% visible parity.

---

## 2026-05-13 02h55 — Addendum Mike chat live (2 tickets supplémentaires R7)

### R7-026 — WEBGL-FIX-LONG-TERM Fix Unity 6 + URP 17.3 + WebGL2 cassé

- **[P1 long-terme — pas urgent immédiat mais bloquant distribution]**
- **[Em : 4-8h investigation + test, OU 0h wait Unity patch]**
- **Mike chat directive (2026-05-13)** :
  > "il faudra fixer la version web quand meme a un moment donné c'est notre medium de distribution de test numero 1 et ca fonctionnait nickel en v4 donc bon"

- **Contexte** : 13 iterations cascade épuisée 2026-05-12/13 — Unity 6.0.3.15f1 + URP 17.3.0 + WebGL2 = subpass input shader (Hidden/CoreSRP/CoreCopy FRAMEBUFFER_INPUT_X_FLOAT) incompatible WebGL2. Workaround temporaire = test exclusivement Unity Editor (memory rule). MAIS web = medium distribution #1 Mike, V4 (Phaser) marchait nickel, donc V6 doit re-marcher web une fois.
- **Options à investiguer (catégorie B Mike decision avant exec)** :
  - **Option A** : Wait Unity 6.1 (URP 17.4+) qui peut patch subpass input WebGL2. ETA : Unity roadmap inconnue (semaines-mois). Effort : 0h (wait).
  - **Option B** : Downgrade Unity 2022.3 LTS + URP 14.x (known stable WebGL2). Effort : 4-8h migration + verify pas regression Editor + retest 60 levels. Risque : pertes features Unity 6 (input system v2, etc.).
  - **Option C** : Switch BiRP (Built-in Render Pipeline). Effort : 6-12h migration (shaders, materials, lighting). Risque : perte URP features (post-processing 2D stack, custom passes).
  - **Option D** : Custom WebGL2 build config avec strip URP CoreCopy + force fallback shader path. Effort : 2-4h R&D Unity. Risque : maintained sur soi.
  - **Option E** : Switch WebGPU build target (Unity 6 supporte WebGPU experimental). Effort : 2-3h test. Risque : compat browser limitée (Chrome only stable).
- **Action exec immédiate** : Write `questions-to-supervisor.md` pour Mike decision :

```
[ESCALATION cat B] R7-026 — WebGL fix strategy
Q: Quelle option pour fix WebGL distribution ?
A: Wait Unity 6.1 patch (0h, ETA inconnue)
B: Downgrade Unity 2022.3 + URP 14.x (4-8h, perte features)
C: Switch BiRP (6-12h, perte URP custom)
D: Strip URP CoreCopy custom (2-4h R&D)
E: WebGPU experimental (2-3h, Chrome-only)
Recommendation supervisor: A si patience OK (le Editor mode actuel permet dev/test full), sinon D pour pragmatisme rapide.
Blocking: false (continue R7 P0-P2 en parallèle pendant Mike réfléchit)
```

- **[Vf]** post-implementation (selon option choisie) :
  - Build WebGL successful (`BuildAchievementAssets.Run()` + `BuildWebGL` no errors)
  - Deploy `/v6/` gh-pages → load `https://michaelchevallier.github.io/crowd-defense/v6/` → render visible (pas pure noir)
  - 5-wave smoke OK browser (BuildPoint walk-in, tower placement, wave 1-5 complete)
  - FPS ≥ 30 Chrome desktop
- **[Cf]** :
  - Selon option choisie, multiple commits possibles. Tag final éventuel `v6.1-webgl-restored`.

### R7-027 — CAMERA-FIX-GREY-EDITOR Adjust camera position Editor

- **[P0]** **[Em : 30min-1h]**
- **Mike chat directive (2026-05-13)** :
  > "il faut ajuster la position de la caméra qui ne nous montre que du gris au debut."

- **Contexte** : Mike teste Unity Editor Play mode et voit du gris au début. Hypothèses :
  1. **EnsureMainCamera-Auto** (Assets/Scripts/Systems/EnsureMainCamera.cs) crée Camera à pos `(0f, 1f, -10f)` avec `backgroundColor = (0.06, 0.07, 0.09, 1f)` (dark blue-grey). Si Menu.unity ne définit aucune Camera explicite, cette caméra "fallback" se déclenche et montre son fond gris → Mike voit gris.
  2. Ou bien Main.unity Camera position pointée mauvais endroit (map hors frustum).
  3. Ou bien la caméra Hero-follow n'a pas son target initial → reste à origin.
- **Debugging steps recommandés exec** :
  1. UnityMCP `find_gameobjects` type:Camera → liste toutes Cameras actives au start
  2. Read Main.unity dans yaml — combien de Camera GameObjects définis ? Quelle position/rotation ?
  3. Vérifier `HeroFollowCameraController.cs` (existe d'après file listing) — target setup au start ? Default pos si target null ?
  4. Compare V4 (Phaser ScalaCamera) reference angles : V4 `de6eec1` "camera + input bindings match V4 reference" — vérifier ces angles V4 actuellement appliqués
- **[Df]** :
  - `Assets/Scripts/Systems/HeroFollowCameraController.cs` — verify follow logic, default position fallback
  - `Assets/Scenes/Main.unity` — Camera GO position/rotation/orthographicSize/fov values
  - `Assets/Scripts/Systems/EnsureMainCamera.cs` — verify si actif et conflict avec Main.unity Camera
  - Voir si `Camera.main` est la bonne (priorité depth) ou si auto-fallback prend dessus
- **[Vf]** :
  - Play mode Unity Editor : caméra montre la map dès le start (pas gris)
  - Mike screenshot validation (T2 batched notif post-fix)
- **[Cf]** :
  - `fix(camera)(R7-027): adjust default position to show map at start — V4 angles match`

---

## Updated batch suggéré (batch 1 — recommandé pour exec dispatch IMMÉDIAT)

**R7-027 CAMERA-FIX-GREY-EDITOR (P0 priorité #1 — Mike user-visible NOW)**
**R7-001 Water/Lava 8 frames anim (P0 parity)**
**R7-003 Loader build settings (P0 quick win 30min)**
**R7-010 Hero.cs partition close docs (P2 quick docs)**

Batch 2 sequential (post-batch 1) : R7-005 MuteToggle + R7-006 F1 console + R7-002 Castle real art.

Batch 3 parallel (architecture) : R7-011 init cascade + R7-012 SceneValidator + R7-013 collision matrix + R7-019 companyName.

Batch 4 long : R7-014 L.cs externalize + R7-026 WebGL fix (cat B Mike decision needed avant).
