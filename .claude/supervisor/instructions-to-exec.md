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

⏳ pending bug-fixer completion + exec ack STOP





