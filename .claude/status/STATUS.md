# Crowd Defense — STATUS.md tracker (multi-session)

> Source of truth multi-session pour le sprint en cours. À lire en début de chaque session opus, à updater en fin.
> Pattern emprunté à `milan project` repo. Fichier root `STATUS.md` reste le récit long-form du sprint MIGRATE ; ce fichier-ci est le **tracker court** pour orientation rapide.

---

## Current sprint

**R7-PUSH-100** — 🚀 **ACTIF** (push 85-90% → 95-100% visible parity, Mike chat override "avance jusque parité").

- Sprint open : 2026-05-13 02h47 (supervisor instruction R7-PUSH-100-GO)
- **20 R7 tickets shippés** batch 1-7 (P0 + P1 + P2 + P3 + Q9 balance + AUDIO-REMAP + Castle procedural improved)
- Audit rerun R7-022 (`b0ab333`) : baseline 33% raw / 65% weighted → HEAD **80% raw / 85% weighted** (+47 pts raw)
- Audit category breakdown : Visual 35→83% / Audio 4→72% / Gameplay 50→84% / UI 30→87%
- Top 3 gaps remaining : Animator runtime verify (T-VISUAL-002 in-flight) + Castle real .fbx art deferred Mike asset (R7-002 procedural amélioré shipped `ef1b419` comme intérim) + A-05 castle alarm SFX (low prio)

### Tickets R7 shippés (chronologique)

| # | Ticket | Type | Commit |
|---|---|---|---|
| 1 | R7-027 | Camera grey Editor fix (P0) | `35e8877` |
| 2 | R7-001 | Water/Lava 8 frames anim (P0) | `d745c1d` |
| 3 | R7-003 | Loader BuildSettings scene 0 (P0) | `c38e790` |
| 4 | R7-010 | Hero.cs partition close docs (P2) | `7db9f2c` |
| 5 | R7-005 | MuteToggle HUD wire (P1) | `8de976e` |
| 6 | R7-011 | Init cascade audit (P2) | `d42b014` |
| 7 | R7-011-FIX | NRE quick wins R1+R2 (P1) | `f5dfd15` |
| 8 | R7-013 | Collision matrix layers (P2) | `a584720` |
| 9 | R7-019 | companyName + packages pin (P3) | `33f0c76` |
| 10 | R7-006 | F1 console + CdDebugApi (P1) | `c2a6730` |
| 11 | R7-012 | SceneValidator tool (P2) | `4518717` |
| 12 | R7-009 | AudioSourcePool 3D (P1) | `ebeef5a` |
| 13 | R7-017 | Foire+Medieval skins (P3) | `7270690` |
| 14 | R7-015 | atomic SaveSystem (P2) | `c9e3dc3` |
| 15 | R7-Q9-BALANCE | Q9-1..Q9-4 balance fixes (P1) | `45a2a87` + `50b6af9` |
| 16 | R7-022 | audit rerun baseline (P3) | `b0ab333` |
| 17 | R7-020 | workspace clean (P3) | `6ee82db` |
| 18 | R7-008 | ProjectilePool dedup verify (P1) | `6ee82db` |
| 19 | R7-AUDIO-REMAP | 3 SFX semantic fixes (P1) | `ab8c212` |
| 20 | R7-002 | Castle improved procedural 4 stages (P0) | `ef1b419` |
| - | T-VISUAL-002 | Animator state machines (P0 supervisor) | (in-flight a203f9d) |

### Tickets R7 cat B Mike pending (4 questions)

- **Q-R7-007** Supprimer EnsureMainCamera + ImguiDiagOverlay diag code ?
- **Q-R7-016** Events V4 fidelity strict 8 events vs Unity simplification 3 random ?
- **Q-R7-018** Squash 5 supervisor commits + 3 ProjectilePool redondants ?
- **Q-R7-026** WebGL fix strategy (5 options A-E) ?

### Tickets R7 remaining backlog

- R7-002 Castle real .fbx art (P0, needs Mike asset / improved procedural)
- R7-004 Mike validation checklist (partial done supervisor `70c49d7` + `872cd8d`)
- R7-014 L.cs externalize CSV/JSON (P2, 8-12h LONG, defer R8/R10)
- R7-021 10worlds smoke test (P3, UnityMCP needed exec direct)

## Next sprint candidates

- **R7-FINISH** (continuation) : Castle real art + R7-021 smoke test + 4 cat B Mike answers
- **R8 perf** : 3 perf-3fix profilage live (60 FPS desktop / 30 FPS mobile)
- **R9 playable MVP** : 5-wave loop polish + onboarding tutorial + FTUE
- **R10 i18n** : L.cs externalize CSV/JSON (R7-014 deferred)
- **R11 WebGL restore** : selon Mike answer Q-R7-026 (wait Unity patch ou strip URP)

---

## R6 closed sprint summary

**R6-PARITY-V4-FINAL** — ✅ COMPLETE (95%+ parity, closure `16fe830`).

- Sprint dates : 2026-05-12 16h05 → 2026-05-13 02h35 (~10h30 wall)
- Total commits : 98 (`739efc7..fbf17fe`)
- Retrospective : `.claude/audit/SPRINT-R6-RETROSPECTIVE.md`
- Tag : `v6.0-parity-v4-sprint-r6`
- Live build : `https://michaelchevallier.github.io/crowd-defense/v6/` (⚠️ WebGL broken Unity 6+URP 17.3, voir Q-R7-026)

---

## V4 baseline vs V6 effectif (par axe)

| Axe | Code-level parity | User-facing visible | Gap principal |
|---|---|---|---|
| Gameplay (loop, towers, enemies, wave) | 96-97% | **85-95%** | Player loop BuildPoint flow partiel |
| Visuel (textures, materials, FX) | 95% | **40-60%** | Materials gris (78 PNG Flux non-wired) |
| UI / HUD | 95% | **50-70%** | Toolbar/minimap wiring fragile, positioning broken |
| Audio | 100% | **0-30%** | MusicManager wired (`cdfe829`), SFX en cours |
| Animations | 90% | **10-20%** | Animators T-pose, controllers default broken |
| Menu / Nav | 90% | **30-50%** | MenuScene + SceneNav bug-fixer en cours |
| Camera / Input | 100% | **40-60%** | Bindings fixés (`de6eec1`), Cinemachine setup unclear |

**Note** : "Le code existe à 96%, mais le wiring Unity Editor est ~40% complet" (audit `adb68ee`). Pas un problème de code, un problème workflow.

---

## Recent commits (last 20 origin/main)

```
cc3bcf2 supervisor: WAVE 4 BACKLOG (T1-T7) — textures/toolbar/minimap/ghost/wave-UI/animator/integration
5af7e53 fix(parity): wire 22 VFX prefabs in VfxPool (eliminate fallback null-guard)
cdfe829 fix(wiring): add MusicManager to Main.unity scene
a76b81c supervisor: WAVE 3 BACKLOG (V1-V8) — orthogonal aux 4 bug-fixers Opus wave-3 player-loop
2b53896 fix(wiring): wire MusicManager audio clips (menu/calm/intense/boss)
de6eec1 fix(parity): camera + input bindings match V4 reference
adb68ee chore(audit): TRUE v4↔v6 visible-parity (honest user-facing assessment)
1e07f7c fix(parity): Hero ULT V4 mode (30s/8 proj/dmg=15 all heroes) — audit P1 #2
a296016 fix(parity): Enable warlord charge + dragon fire breath (P1 audit)
cc37b8b fix(parity): Add SchoolDef.starterTowerType field (P2 audit)
99c3c3b fix(parity): SkinRegistry wire 10 castle + 4 vfx skins (P0 audit)
12c7694 supervisor: MASSIVE backlog tracks A-E (Mike no-idle directive) — 9 tracks pour 99%→100%+ parité
ad3804d chore(audit): V4↔V6 parity gap final 5% audit
4dcfed2 fix(scene): wire ProjectilePool projectilePrefab in Main.unity
ed76df8 fix(scene): wire ProjectilePool projectilePrefab in Main.unity
3cb134b fix(runtime-pool): AudioMixerController add Assets/Resources/Audio.meta
792f5e9 fix(runtime): MonoSingleton skip auto-create during scene unload (OnDestroy cascade)
1917b0a fix(wiring): ProjectilePool.projectilePrefab wire to Prefabs/Projectile.prefab in Main.unity
b9f6685 fix(runtime-pool): ProjectilePool warn-once null prefab (40x spam → 1x)
a8f887e supervisor: URGENT PARITY-FINISH backlog (5 tickets) — Mike no-stop directive
```

---

## Pending tickets (top 10)

WAVE 3 (orthogonal aux 4 bug-fixers wave-3 player-loop) :

1. **W3-V1** ✅ DONE (`5af7e53`) — VfxPool wire 22 prefabs
2. **W3-V2** — Cutscenes 10 worlds wired runtime confirmation
3. **W3-V3** — Achievements 56 wired + tracked
4. **W3-V4** — Meta-upgrades 10 wired + Persist
5. **W3-V5** — Modifiers (8 curses+blessings) runtime test
6. **W3-V6** — Code dedup + dead code audit
7. **W3-V7** ✅ DONE (this file) — STATUS.md crowd-defense create
8. **W3-V8** — Build Settings audit + ProjectSettings polish

WAVE 4 (Top 5 user-facing gaps audit `adb68ee` non couverts) :

9. **W4-T1 (P0)** — Textures wire 78 Flux PNG → ground+path materials
10. **W4-T2..T7 (P1-P2)** — Toolbar / Minimap / Ghost preview / Wave UI / Animator defaults / 5-wave integration test

---

## Instructions next session opus

### Sprint R6-PARITY-V4-FINAL = OFFICIELLEMENT CLOSED (2026-05-13 02h35)

98 commits, V6 user-facing parity 45-65% → 95%+, master plan 11/11 P0+P1 + 13 bonus + UI HUD fix + N9 content. **NE PAS relancer ce sprint** — il est fini.

### À lire dans cet ordre

1. **`.claude/audit/SPRINT-R6-PARITY-V4-FINAL-completion.md`** — rapport final agent (a92d06b69, ~6KB, table 22 tickets + Mike complaints resolution + V6 parity recalc + verification plan).
2. **`.claude/audit/MASTER-PLAN-tickets-swarm.md`** — agrégation 10-layer audit + 22 tickets P0-P3.
3. **`.claude/audit/v6-layer-01..10-*.md`** — 10 audits orthogonaux par couche (Build/Scenes/Prefabs/Scripts/UI/Gameplay/Audio/Visual/Levels/Integration).
4. **`STATUS.md`** (ce fichier) — sections "Architecture state" + "V4 baseline vs V6" + "Known issues".
5. **`CLAUDE.md`** (root) — Unity context + workflow Opus orchestre / Sonnet exécute.
6. **`.claude/supervisor/questions-to-supervisor.md`** — Q-N9 Q9-1/Q9-2/Q9-3 PENDING Mike (Q9-4 auto-shipped `2ecd3cd`).
7. **`/Users/mike/.claude/projects/-Users-mike-Work-milan-project/memory/MEMORY.md`** — feedbacks Mike (feedback_no_priority_questions, feedback_test_autonomously_unitymcp, feedback_structured_audit_then_swarm, feedback_wire_as_you_go, etc.).

### Actions immédiates à faire

1. **PAS de spawn agents** par défaut. Sprint R6 CLOSED, exec idle acceptable per Mike "SAUF si objectif final atteint".
2. **Si Mike pose nouvelle question** sur état V6 → lire `SPRINT-R6-PARITY-V4-FINAL-completion.md` + répondre depuis sources de vérité.
3. **Si Mike déclare next sprint R7/R8/R9** :
   - R7 polish : Foire+Medieval castle textures + 80 levels art + L3 upgrade branching
   - R8 perf : 3 perf-3fix profilage live (60 FPS desktop / 30 FPS mobile)
   - R9 playable MVP : 5-wave loop polish + onboarding tutorial + FTUE
4. **Si Mike veut Q-N9 décisions** :
   - Q9-1 endurance W*-9 keep 10 waves (V4 parity, BTD6 pattern) — reco YES
   - Q9-2 ramp factor W9/W10 wave1 → 0.45 hybride (medium-conservatif)
   - Q9-3 LevelDifficultyMul overrides cleanup uniformly — reco YES

### Workflow autonome établi

- **UnityMCP HTTP** `http://127.0.0.1:8080/mcp` pour test runtime Play mode (`refresh_unity`, `manage_editor`, `read_console`, `execute_code`). Init session via curl `initialize` puis `notifications/initialized`.
- **Approche structurée** quand bugs reviennent : 10-layer audit orthogonal → master plan → swarm waves parallèles (cf memory `feedback_structured_audit_then_swarm.md`).
- **Pas de questions priorité** : décider et exécuter (memory `feedback_no_priority_questions.md`).

### Background tasks status post-sprint

- Monitor 5min `bpwquirb0` : **STOPPÉ** (TaskStop, plus d'alerts spam, objective reached).
- Cron supervisor /loop 30m : **ACTIVE** (cron `82d133e3 13,28,43,58`) — continue scrutes périodiques.
- Tous les bug-fixers/Explore agents background : **COMPLETED** (cf `_clean-log.md` scrute logs).

---

## Architecture state

**Partial classes split done** (17 fichiers, cap 500 LOC strict charter §1 règle #3) :

- **Castle** (3 fichiers) : `Castle.cs` + `Castle.HP.cs` + `Castle.VFX.cs`
- **Tower** (6 fichiers) : `Tower.cs` + `Tower.Anim.cs` + `Tower.Combat.cs` + `Tower.Effects.cs` + `Tower.Placement.cs` + `Tower.Upgrade.cs`
- **Enemy** (8 fichiers) : `Enemy.cs` (491 LOC under cap) + `Enemy.Anim.cs` + `Enemy.Behaviors.cs` + `Enemy.Combat.cs` + `Enemy.Init.cs` + `Enemy.Lifecycle.cs` + `Enemy.Movement.cs` + `Enemy.Stats.cs` + `Enemy.Update.cs`
- Plus `EnemyBossBehaviors.cs` 446 LOC (re-mesuré sous cap après wave-3 `6dac3cd`)

**Runtime infra acquis** :

- `VfxPool` : 22 prefabs wired (`5af7e53`) — élimine null-guard fallback
- `ProjectilePool` : projectilePrefab wired (`1917b0a`, fixé `4dcfed2` via Main.unity)
- `MonoSingleton` : `_destroying` flag skip auto-create durant OnDestroy cascade scene unload (`792f5e9`)
- `MusicManager` : ajouté Main.unity scene (`cdfe829`) + audio clips wired menu/calm/intense/boss (`2b53896`)
- `AudioMixerController` : `Assets/Resources/Audio.meta` ajouté (`3cb134b`)
- `Camera + Input` bindings match V4 reference (`de6eec1`)

**Outline.cs** static inverted hull scale 1.02 back-face material wire post-GLTF spawn (`79b56f1`..`59785a6`).

---

## Known issues (questions en cours)

Cf `.claude/supervisor/questions-to-supervisor.md` :

- Toutes les Q ouvertes courant 16h-18h sont `[resolved]` (vfx-bindings-cap, stash-WIP, enemy-refacto-cap, cap-enemy auto-resolved par BACKLOG-WAVE-3).
- Pas de Q `[pending]` active à ce stade.

Risks transverse (cf root STATUS.md "Risks raised") :
- Unity-MCP edge cases (jeune, peut buguer)
- WebGL bundle ~5-15 MB compressé vs 395 KB Phaser
- Steamworks/Apple Developer/Google Play setup nécessite Mike (credentials)
- Mike skill Unity/C# = zéro (vulgarisation systématique)

---

*Dernière update : 2026-05-13 02h35 — sprint R6-PARITY-V4-FINAL marked COMPLETE (closure `16fe830`).*
